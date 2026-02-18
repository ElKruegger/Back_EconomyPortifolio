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

        public async Task<MessageResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await GetUserByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Email já está em uso");
            }

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

            // Criar wallet BRL automaticamente
            await _walletService.CreateWalletAsync(user.Id, new CreateWalletDto { Currency = "BRL" });

            // Gerar e enviar código de verificação por e-mail
            await _verificationCodeService.GenerateAndSendCodeAsync(
                user.Id, user.Email, user.Name, VerificationCodeType.Registration);

            return new MessageResponseDto("Código de verificação enviado para o seu e-mail. Verifique sua caixa de entrada.");
        }

        public async Task<MessageResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await GetUserByEmailAsync(loginDto.Email.ToLowerInvariant());

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Email ou senha inválidos");
            }

            // Gerar e enviar código de verificação por e-mail
            await _verificationCodeService.GenerateAndSendCodeAsync(
                user.Id, user.Email, user.Name, VerificationCodeType.Login);

            return new MessageResponseDto("Código de verificação enviado para o seu e-mail. Verifique sua caixa de entrada.");
        }

        public async Task<AuthResponseDto> VerifyCodeAsync(VerifyCodeDto verifyCodeDto)
        {
            var user = await _verificationCodeService.ValidateCodeAsync(
                verifyCodeDto.Email, verifyCodeDto.Code, verifyCodeDto.Type);

            // Se for verificação de registro, marcar e-mail como verificado
            if (verifyCodeDto.Type == VerificationCodeType.Registration)
            {
                user.EmailVerified = true;
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

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

        public async Task<MessageResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await GetUserByEmailAsync(forgotPasswordDto.Email.ToLowerInvariant());

            // Retornar mensagem genérica mesmo se o usuário não existir (segurança)
            if (user == null)
            {
                return new MessageResponseDto("Se o e-mail estiver cadastrado, você receberá um código de verificação.");
            }

            await _verificationCodeService.GenerateAndSendCodeAsync(
                user.Id, user.Email, user.Name, VerificationCodeType.PasswordReset);

            return new MessageResponseDto("Se o e-mail estiver cadastrado, você receberá um código de verificação.");
        }

        public async Task<MessageResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _verificationCodeService.ValidateCodeAsync(
                resetPasswordDto.Email, resetPasswordDto.Code, VerificationCodeType.PasswordReset);

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPasswordDto.NewPassword);
            await _context.SaveChangesAsync();

            return new MessageResponseDto("Senha redefinida com sucesso. Você já pode fazer login com sua nova senha.");
        }

        public async Task<bool> ValidateUserAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public async Task<Users?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        }

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

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private int GetJwtExpirationMinutes()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            return int.TryParse(jwtSettings["ExpirationMinutes"], out var minutes) ? minutes : 30;
        }
    }
}
