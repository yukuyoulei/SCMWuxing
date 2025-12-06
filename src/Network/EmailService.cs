#if SERVER
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GameEntry.Server
{
    /// <summary>
    /// 邮件服务接口
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendVerificationCodeAsync(string email, string code);
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }

    /// <summary>
    /// SMTP邮件服务实现
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly string smtpHost;
        private readonly int smtpPort;
        private readonly string smtpUsername;
        private readonly string smtpPassword;
        private readonly string fromEmail;
        private readonly string fromName;

        public SmtpEmailService(EmailConfig config)
        {
            smtpHost = config.SmtpHost;
            smtpPort = config.SmtpPort;
            smtpUsername = config.SmtpUsername;
            smtpPassword = config.SmtpPassword;
            fromEmail = config.FromEmail;
            fromName = config.FromName;
        }

        /// <summary>
        /// 发送验证码邮件
        /// </summary>
        public async Task<bool> SendVerificationCodeAsync(string email, string code)
        {
            string subject = "【GameWuxing】您的登录验证码";
            string body = $@"
<html>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
        <div style=""background-color: #f8f9fa; padding: 30px; border-radius: 10px; text-align: center;"">
            <h2 style=""color: #007bff; margin-bottom: 20px;"">GameWuxing 登录验证</h2>
            
            <p style=""font-size: 16px; margin-bottom: 20px;"">您好！</p>
            
            <p style=""font-size: 16px; margin-bottom: 20px;"">您正在尝试登录 GameWuxing 游戏平台。请在5分钟内使用以下验证码完成登录：</p>
            
            <div style=""background-color: #ffffff; padding: 20px; border-radius: 8px; margin: 20px 0; border: 2px solid #007bff;"">
                <h1 style=""font-size: 32px; color: #007bff; margin: 0; letter-spacing: 4px;"">{code}</h1>
            </div>
            
            <p style=""font-size: 14px; color: #6c757d; margin-top: 20px;"">⚠️ 安全提示：</p>
            <ul style=""font-size: 14px; color: #6c757d; text-align: left; max-width: 400px; margin: 10px auto;"">
                <li>验证码有效期为5分钟</li>
                <li>请勿将验证码透露给他人</li>
                <li>如非本人操作，请忽略此邮件</li>
            </ul>
            
            <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;"">
            
            <p style=""font-size: 12px; color: #6c757d;"">
                此邮件由 GameWuxing 系统自动发送，请勿回复。<br>
                如有疑问，请联系客服支持。
            </p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(email, subject, body);
        }

        /// <summary>
        /// 发送普通邮件
        /// </summary>
        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail, fromName);
                    message.To.Add(to);
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(smtpHost, smtpPort))
                    {
                        client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                        client.EnableSsl = true;
                        
                        await client.SendMailAsync(message);
                        
                        Game.Logger.LogInformation($"[EmailService] Email sent successfully to: {to}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Game.Logger.LogError($"[EmailService] Failed to send email to {to}: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 模拟邮件服务（用于测试环境）
    /// </summary>
    public class MockEmailService : IEmailService
    {
        public async Task<bool> SendVerificationCodeAsync(string email, string code)
        {
            // 模拟邮件发送延迟
            await SendEmailAsync(email, "验证码", $"你的验证码是 {code}");
            
            // 记录到日志（测试用）
            Game.Logger.LogInformation($"[MockEmailService] Mock email sent to: {email}, Code: {code}");
            
            return true;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            await Game.Delay(500);
            Game.Logger.LogInformation($"[MockEmailService] Mock email: To={to}, Subject={subject}");
            return true;
        }
    }
}
#endif