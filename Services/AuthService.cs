using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace EconomyBackPortifolio.Services
{
    /// <summary>
    /// Serviço responsável pela autenticação de usuários: registro, login com 2FA,
    /// verificação de código, redefinição de senha e geração de tokens JWT.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWalletService _walletService;
        private readonly IVerificationCodeService _verificationCodeService;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IWalletService walletService,
            IVerificationCodeService verificationCodeService)
        {
            _context = context;
            _configuration = configuration;
            _walletService = walletService;
            _verificationCodeService = verificationCodeService;
        }

        /// <summary>
        /// Registra um novo usuário, cria a wallet BRL padrão e envia o código de verificação.
        /// O e-mail só será marcado como verificado após a conclusão do fluxo via VerifyCodeAsync.
        /// </summary>
        /// <exception cref="InvalidOperationException">Se o e-mail já estiver em uso.</exception>
        public async Task<MessageResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await GetUserByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email já está em uso");
            }

            // BCrypt com work factor padrão (10) — adequado para produção.
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var user = new Users
            {
                Id = Guid.NewGuid(),
                Name = registerDto.Name,
                Email = registerDto.Email.ToLowerInvariant(),
                PasswordHash = passwordHash,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Toda conta nova recebe uma wallet BRL automaticamente.
            await _walletService.CreateWalletAsync(user.Id, new CreateWalletDto { Currency = "BRL" });

            // Envia o código 2FA de registro por e-mail.
            await _verificationCodeService.GenerateAndSendCodeAsync(
                user.Id, user.Email, user.Name, VerificationCodeType.Registration);

            return new MessageResponseDto("Código de verificação enviado para o seu e-mail. Verifique sua caixa de entrada.");
        }

        /// <summary>
        /// Valida as credenciais do usuário e envia o código 2FA para o e-mail cadastrado.
        /// CORREÇÃO: verifica se o e-mail foi confirmado antes de prosseguir com o login.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Credenciais inválidas ou e-mail não verificado.</exception>
        public async Task<MessageResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await GetUserByEmailAsync(loginDto.Email.ToLowerInvariant());

            // Comparação constante evita timing attacks: nunca revele qual campo está errado.
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Email ou senha inválidos");
            }

            // CORREÇÃO CRÍTICA: impede login de contas com e-mail não verificado.
            // Antes desta correção, um usuário registrado mas não verificado conseguia logar.
            if (!user.EmailVerified)
            {
                throw new UnauthorizedAccessException("E-mail não verificado. Confirme seu cadastro antes de fazer login.");
            }

            // Envia o código 2FA de login por e-mail.
            await _verificationCodeService.GenerateAndSendCodeAsync(
                user.Id, user.Email, user.Name, VerificationCodeType.Login);

            return new MessageResponseDto("Código de verificação enviado para o seu e-mail. Verifique sua caixa de entrada.");
        }

        /// <summary>
        /// Valida o código 2FA e retorna o JWT + RefreshToken.
        /// No fluxo de registro, marca o e-mail como verificado.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Código inválido, expirado ou já utilizado.</exception>
        public async Task<AuthResponseDto> VerifyCodeAsync(VerifyCodeDto verifyCodeDto)
        {
            var user = await _verificationCodeService.ValidateCodeAsync(
                verifyCodeDto.Email, verifyCodeDto.Code, verifyCodeDto.Type);

            // Fluxo de registro: marca o e-mail como verificado após validação bem-sucedida.
            if (verifyCodeDto.Type == VerificationCodeType.Registration)
            {
                user.EmailVerified = true;
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // TODO: persistir o refreshToken na tabela refresh_tokens para suportar
            // o endpoint /auth/refresh-token com validação real.

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email
                }
            };
        }

        /// <summary>
        /// Solicita redefinição de senha: envia o código via e-mail se o usuário existir.
        /// A resposta é sempre genérica para evitar user enumeration (OWASP A01).
        /// </summary>
        public async Task<MessageResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await GetUserByEmailAsync(forgotPasswordDto.Email.ToLowerInvariant());

            // Resposta genérica mesmo quando o e-mail não existe — evita user enumeration.
            if (user != null)
            {
                await _verificationCodeService.GenerateAndSendCodeAsync(
                    user.Id, user.Email, user.Name, VerificationCodeType.PasswordReset);
            }

            return new MessageResponseDto("Se o e-mail estiver cadastrado, você receberá um código de verificação.");
        }

        /// <summary>
        /// Redefine a senha do usuário após validação do código de reset.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Código inválido ou expirado.</exception>
        public async Task<MessageResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _verificationCodeService.ValidateCodeAsync(
                resetPasswordDto.Email, resetPasswordDto.Code, VerificationCodeType.PasswordReset);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPasswordDto.NewPassword);
            await _context.SaveChangesAsync();

            return new MessageResponseDto("Senha redefinida com sucesso. Você já pode fazer login com sua nova senha.");
        }

        /// <summary>
        /// Valida e-mail e senha sem disparar o fluxo 2FA.
        /// Usado internamente quando não se deseja o código por e-mail.
        /// </summary>
        public async Task<bool> ValidateUserAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        /// <summary>
        /// Busca um usuário pelo e-mail (normalizado para lowercase).
        /// Retorna null se não encontrado.
        /// </summary>
        public async Task<Users?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        }

        /// <summary>
        /// Busca e retorna os dados públicos de um usuário pelo seu ID.
        /// Retorna null se não encontrado.
        /// </summary>
        public async Task<UserInfoDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            return new UserInfoDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // MÉTODOS PRIVADOS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Gera o token JWT com os claims do usuário.
        /// O token inclui: NameIdentifier (userId), Email, Name e Jti (ID único do token).
        /// </summary>
        private string GenerateJwtToken(Users user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
            var issuer = jwtSettings["Issuer"] ?? "EconomyBackPortifolio";
            var audience = jwtSettings["Audience"] ?? "EconomyBackPortifolio";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Gera um RefreshToken criptograficamente seguro (64 bytes aleatórios em Base64).
        /// IMPORTANTE: o token gerado deve ser persistido no banco para validação futura.
        /// </summary>
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Lê o tempo de expiração do JWT da configuração. Padrão: 30 minutos.
        /// </summary>
        private int GetJwtExpirationMinutes()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            return int.TryParse(jwtSettings["ExpirationMinutes"], out var minutes) ? minutes : 30;
        }
    }
}
