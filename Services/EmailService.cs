using EconomyBackPortifolio.Enums;
using EconomyBackPortifolio.Settings;
using Resend;

namespace EconomyBackPortifolio.Services
{
    /// <summary>
    /// Serviço responsável pelo envio de e-mails transacionais via Resend API.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly IResend _resend;
        private readonly ILogger<EmailService> _logger;

        public EmailService(EmailSettings emailSettings, IResend resend, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings;
            _resend = resend;
            _logger = logger;
        }

        public async Task SendVerificationCodeAsync(string toEmail, string userName, string code, VerificationCodeType type)
        {
            var (subject, body) = BuildEmailContent(userName, code, type);

            var emailMessage = new EmailMessage
            {
                From = $"{_emailSettings.SenderName} <{_emailSettings.SenderEmail}>",
                Subject = subject,
                HtmlBody = body
            };
            emailMessage.To.Add(toEmail);

            try
            {
                var response = await _resend.EmailSendAsync(emailMessage);
                
                if (response.Id == null)
                {
                    _logger.LogError("Erro ao enviar e-mail via Resend: {Error}", response.Error?.Message ?? "Erro desconhecido");
                    throw new InvalidOperationException("Falha ao enviar e-mail de verificação. Tente novamente mais tarde.");
                }

                _logger.LogInformation("E-mail de verificação ({Type}) enviado para {Email} com ID {ResendId}", type, toEmail, response.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de verificação para {Email}", toEmail);
                throw new InvalidOperationException("Falha ao enviar e-mail de verificação. Tente novamente mais tarde.");
            }
        }

        private static (string Subject, string HtmlBody) BuildEmailContent(string userName, string code, VerificationCodeType type)
        {
            var (title, description) = type switch
            {
                VerificationCodeType.Login => (
                    "Código de Login",
                    "Utilize o código abaixo para concluir seu login:"
                ),
                VerificationCodeType.Registration => (
                    "Confirme seu Cadastro",
                    "Utilize o código abaixo para confirmar seu cadastro:"
                ),
                VerificationCodeType.PasswordReset => (
                    "Redefinição de Senha",
                    "Utilize o código abaixo para redefinir sua senha:"
                ),
                _ => ("Código de Verificação", "Utilize o código abaixo:")
            };

            var subject = $"Economy Portfolio - {title}";

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
</head>
<body style=""margin:0; padding:0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f7;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f7; padding: 40px 0;"">
        <tr>
            <td align=""center"">
                <table width=""480"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.08);"">
                    <tr>
                        <td style=""background-color: #1a1a2e; padding: 24px; text-align: center;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 22px;"">Economy Portfolio</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px 32px 16px;"">
                            <h2 style=""color: #1a1a2e; margin: 0 0 8px; font-size: 20px;"">{title}</h2>
                            <p style=""color: #555; font-size: 15px; margin: 0 0 24px;"">Olá, <strong>{userName}</strong>!</p>
                            <p style=""color: #555; font-size: 15px; margin: 0 0 24px;"">{description}</p>
                            <div style=""text-align: center; margin: 24px 0;"">
                                <span style=""display: inline-block; background-color: #f0f0f5; border: 2px dashed #1a1a2e; border-radius: 8px; padding: 16px 32px; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #1a1a2e;"">{code}</span>
                            </div>
                            <p style=""color: #999; font-size: 13px; margin: 24px 0 0; text-align: center;"">Este código expira em <strong>10 minutos</strong>.</p>
                            <p style=""color: #999; font-size: 13px; margin: 8px 0 0; text-align: center;"">Se você não solicitou este código, ignore este e-mail.</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f9f9fb; padding: 16px 32px; text-align: center; border-top: 1px solid #eee;"">
                            <p style=""color: #aaa; font-size: 12px; margin: 0;"">Economy Portfolio &copy; {DateTime.UtcNow.Year}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            return (subject, htmlBody);
        }
    }
}
