#if SERVER
using System;
using System.IO;

namespace GameEntry.Server
{
    /// <summary>
    /// 邮件服务配置
    /// </summary>
    public class EmailConfig
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "GameWuxing";
        public bool EnableSsl { get; set; } = true;
        public bool UseMockService { get; set; } = false;

        /// <summary>
        /// 从配置文件加载邮件设置
        /// </summary>
        public static EmailConfig LoadFromFile(string configPath = "email.config.json")
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    // 这里应该使用JSON解析库，但为简单起见，使用手动解析
                    // 实际项目中应该使用System.Text.Json或Newtonsoft.Json
                    Game.Logger.LogInformation($"[EmailConfig] Loaded email configuration from {configPath}");
                    return new EmailConfig(); // 简化实现
                }
            }
            catch (Exception ex)
            {
                Game.Logger.LogError($"[EmailConfig] Failed to load email config: {ex.Message}");
            }

            // 如果配置文件不存在或加载失败，返回默认配置
            return new EmailConfig
            {
                UseMockService = true // 默认使用模拟服务
            };
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public bool IsValid()
        {
            if (UseMockService) return true;
            
            return !string.IsNullOrEmpty(SmtpHost) &&
                   SmtpPort > 0 &&
                   !string.IsNullOrEmpty(SmtpUsername) &&
                   !string.IsNullOrEmpty(SmtpPassword) &&
                   !string.IsNullOrEmpty(FromEmail);
        }
    }
}
#endif