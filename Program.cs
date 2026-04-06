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
// Documentação interativa da API com suporte a autenticação JWT.
// Disponível apenas em Development para não expor contratos em produção.
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
// A connection string é lida de variáveis de ambiente (Railway injeta automaticamente).
// IMPORTANTE: nunca commite credenciais reais no appsettings.json.
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─────────────────────────────────────────────────────────────────────────────
// UNIT OF WORK
// Padrão que encapsula transações de banco de dados.
// Garante atomicidade nas operações financeiras (buy/sell/deposit).
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ─────────────────────────────────────────────────────────────────────────────
// CONFIGURAÇÕES DE E-MAIL
// Lidas da seção "EmailSettings" e registradas como singleton para injeção.
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
// Valida o token em cada requisição protegida por [Authorize].
// ClockSkew = Zero elimina a tolerância de 5 min padrão na expiração do token.
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
// Com código de 6 dígitos (1.000.000 combinações), sem rate limit um atacante
// consegue tentar todos os valores em minutos.
// Política "auth": máximo 10 requisições por IP a cada 1 minuto.
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

    // Resposta padrão quando o limite é atingido.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ─────────────────────────────────────────────────────────────────────────────
// CORS — Cross-Origin Resource Sharing
// Origens permitidas lidas de variável de ambiente (Cors:AllowedOrigins).
// Em desenvolvimento, cai no fallback localhost:3000 e localhost:5173 (Vite).
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
// Aplica pendências do EF Core automaticamente na inicialização.
// Garante que o banco esteja sempre atualizado no Railway sem intervenção manual.
// ─────────────────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// ─────────────────────────────────────────────────────────────────────────────
// PIPELINE DE REQUISIÇÃO HTTP
// A ordem dos middlewares é fundamental — siga a sequência abaixo.
// ─────────────────────────────────────────────────────────────────────────────

// 1. Swagger — apenas em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2. Health check — endpoint público para monitoramento e Railway healthcheck
// Retorna 200 OK com status da aplicação. Não requer autenticação.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous()
   .WithTags("Health");

// 3. Middleware global de tratamento de exceções (retorna JSON padronizado)
app.UseMiddleware<EconomyBackPortifolio.Middleware.ExceptionHandlingMiddleware>();

// 4. Redirecionamento HTTPS
// Nota: no Railway o TLS é terminado no proxy — este middleware é seguro manter.
app.UseHttpsRedirection();

// 5. CORS — deve vir antes de Authentication/Authorization
app.UseCors("FrontendPolicy");

// 6. Rate Limiting
app.UseRateLimiter();

// 7. Autenticação e autorização

app.UseAuthentication();
app.UseAuthorization();

// 8. Mapeamento de controllers
app.MapControllers();

app.Run();
