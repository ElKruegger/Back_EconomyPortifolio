namespace EconomyBackPortifolio.Settings
{
    /// <summary>
    /// Configurações SMTP para envio de e-mails via MailKit.
    /// Mapeadas da seção "EmailSettings" do appsettings.json.
    /// </summary>
    public class EmailSettings
    {
        /// <summary>Endereço do servidor SMTP (ex: smtp.gmail.com).</summary>
        public string SmtpServer { get; set; } = string.Empty;

        /// <summary>Porta do servidor SMTP (ex: 587 para StartTLS).</summary>
        public int SmtpPort { get; set; }

        /// <summary>E-mail remetente.</summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>Nome de exibição do remetente.</summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>Senha ou App Password do remetente. Nunca commitar valor real.</summary>
        public string Password { get; set; } = string.Empty;
    }
}
