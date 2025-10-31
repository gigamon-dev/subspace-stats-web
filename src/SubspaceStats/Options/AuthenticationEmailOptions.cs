namespace SubspaceStats.Options
{
    public class AuthenticationEmailOptions
    {
        public const string AuthenticationEmailSectionKey = "AuthenticationEmail";

        /// <summary>
        /// The "from" email address.
        /// </summary>
        public required string FromEmail { get; set; }

        /// <summary>
        /// The "from" name.
        /// </summary>
        public required string FromName { get; set; }

        /// <summary>
        /// The SendGrid key or <see langword="null"/> to use SMTP instead.
        /// </summary>
        /// <remarks>
        /// If using SMTP instead of SendGrid, configure the other options that are prefixed with Smtp.
        /// </remarks>
        public string? SendGridKey { get; set; }

        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool SmtpSSL { get; set; }
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
    }
}