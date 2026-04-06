using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Threading.RateLimiting;
using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.Services;
using EconomyBackPortifolio.Settings;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// CONTROLLERS & API EXPLORER
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ─────────────────────────────────────────────────────────────────────────────
// SWAGGER / OPENAPI
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Economy Portfolio API",
        Version = "v1"
    });

    // Permite inserir o token JWT diretamente no Swagger UI para testar endpoints protegidos.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// BANCO DE DADOS — Entity Framework Core + PostgreSQL
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─────────────────────────────────────────────────────────────────────────────
// UNIT OF WORK
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ─────────────────────────────────────────────────────────────────────────────
// CONFIGURAÇÕES DE E-MAIL (MailKit via SMTP relay)
// ─────────────────────────────────────────────────────────────────────────────
var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>()
    ?? throw new InvalidOperationException("EmailSettings não configurado. Verifique as variáveis de ambiente.");
builder.Services.AddSingleton(emailSettings);

// ─────────────────────────────────────────────────────────────────────────────
// SERVIÇOS DE APLICAÇÃO (Scoped — uma instância por requisição HTTP)
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IVerificationCodeService, VerificationCodeService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IPositionService, PositionService>();

// ─────────────────────────────────────────────────────────────────────────────
// AUTENTICAÇÃO JWT
// ─────────────────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey não configurada. Verifique as variáveis de ambiente.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "EconomyBackPortifolio",
        ValidAudience = jwtSettings["Audience"] ?? "EconomyBackPortifolio",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────────────────────────
// RATE LIMITING — Proteção contra brute-force nos endpoints de autenticação.
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ─────────────────────────────────────────────────────────────────────────────
// CORS
// ─────────────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:3000", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ─────────────────────────────────────────────────────────────────────────────
// MIGRATIONS AUTOMÁTICAS
// ─────────────────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// ─────────────────────────────────────────────────────────────────────────────
// PIPELINE DE MIDDLEWARE
// ─────────────────────────────────────────────────────────────────────────────

// 1. Swagger — disponível em todos os ambientes para facilitar testes
app.UseSwagger();
app.UseSwaggerUI();

// 2. Healthcheck — usado pelo Railway para confirmar que o serviço está vivo
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// 3. HTTPS redirect
app.UseHttpsRedirection();

// 4. CORS — deve vir antes de autenticação/autorização
app.UseCors("FrontendPolicy");

// 5. Rate Limiting
app.UseRateLimiter();

// 6. Autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// 7. Mapeia os controllers
app.MapControllers();

app.Run();
