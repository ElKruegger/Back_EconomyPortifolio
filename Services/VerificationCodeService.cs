using EconomyBackPortifolio.Data;
using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EconomyBackPortifolio.Services
{
    public class VerificationCodeService : IVerificationCodeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<VerificationCodeService> _logger;

        public VerificationCodeService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<VerificationCodeService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task GenerateAndSendCodeAsync(Guid userId, string email, string userName, VerificationCodeType type)
        {
            // Invalidar códigos anteriores do mesmo tipo para o mesmo usuário
            var previousCodes = await _context.VerificationCodes
                .Where(vc => vc.UserId == userId && vc.Type == type && !vc.IsUsed)
                .ToListAsync();

            foreach (var prev in previousCodes)
            {
                prev.IsUsed = true;
            }

            // Gerar código de 6 dígitos criptograficamente seguro
            var code = GenerateSecureCode();

            var verificationCode = new VerificationCodes
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Code = code,
                Type = type,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.VerificationCodes.Add(verificationCode);
            await _context.SaveChangesAsync();

            // Enviar código por e-mail
            await _emailService.SendVerificationCodeAsync(email, userName, code, type);

            _logger.LogInformation("Código de verificação ({Type}) gerado para o usuário {UserId}", type, userId);
        }

        public async Task<Users> ValidateCodeAsync(string email, string code, VerificationCodeType type)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

            if (user == null)
            {
                throw new UnauthorizedAccessException("Código de verificação inválido ou expirado");
            }

            var verificationCode = await _context.VerificationCodes
                .Where(vc =>
                    vc.UserId == user.Id &&
                    vc.Code == code &&
                    vc.Type == type &&
                    !vc.IsUsed &&
                    vc.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(vc => vc.CreatedAt)
                .FirstOrDefaultAsync();

            if (verificationCode == null)
            {
                throw new UnauthorizedAccessException("Código de verificação inválido ou expirado");
            }

            // Marcar como usado
            verificationCode.IsUsed = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Código de verificação ({Type}) validado para o usuário {UserId}", type, user.Id);

            return user;
        }

        private static string GenerateSecureCode()
        {
            var randomNumber = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return randomNumber.ToString("D6");
        }
    }
}
