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
// Configura a documentação interativa da API com suporte a JWT.
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
// A connection string é lida do appsettings.json (ou variáveis de ambiente).
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
    ?? throw new InvalidOperationException("EmailSettings não configurado no appsettings.json");
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
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");

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
// CORS — Cross-Origin Resource Sharing
// CORREÇÃO: sem CORS configurado, o front-end em domínio diferente seria bloqueado.
// Ajuste as origens permitidas conforme os ambientes (dev/staging/prod).
// ─────────────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:3000" };

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
// BUILD
// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

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

// 2. Middleware global de tratamento de exceções (retorna JSON padronizado)
app.UseMiddleware<EconomyBackPortifolio.Middleware.ExceptionHandlingMiddleware>();

// 3. Redirecionamento HTTPS
app.UseHttpsRedirection();

// 4. CORS — deve vir antes de Authentication/Authorization
app.UseCors("FrontendPolicy");

// 5. Rate Limiting
app.UseRateLimiter();

// 6. Autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// 7. Mapeamento de controllers
app.MapControllers();

app.Run();
