#if SERVER
using GameEntry.Network;
using System.Text;

namespace GameEntry.Server
{
    public class LoginServerHandler : IGameClass
    {
        private static IEmailService emailService;

        public static void OnRegisterGameClass()
        {
            Game.OnGameTriggerInitialization += RegisterTriggers;

            // 初始化邮件服务配置
            var emailConfig = EmailConfig.LoadFromFile("email.config.json");

            if (emailConfig.UseMockService || !emailConfig.IsValid())
            {
                emailService = new MockEmailService();
                Game.Logger.LogInformation("[LoginServerHandler] Using MockEmailService");
            }
            else
            {
                emailService = new SmtpEmailService(emailConfig);
                Game.Logger.LogInformation("[LoginServerHandler] Using SmtpEmailService with config");
            }
        }

        private static void RegisterTriggers()
        {
            Game.Logger.LogInformation("[Server] Starting to register event triggers...");

            // 注册客户端消息事件触发器 - 使用ProtoCustomMessage系统
            Trigger<EventClientMessage> clientMessageTrigger = new(OnClientMessageReceived, keepReference: true);
            clientMessageTrigger.Register(Game.Instance);
            Game.Logger.LogInformation("[Server] Registered EventClientMessage trigger for ProtoCustomMessage system");

            Game.Logger.LogInformation("[Server] LoginServerHandler triggers registered successfully");
        }

        /// <summary>
        /// 处理来自客户端的ProtoCustomMessage消息
        /// </summary>
        private static async Task<bool> OnClientMessageReceived(object sender, EventClientMessage e)
        {
            var player = e.Player;
            var messageBytes = e.Message;
            
            Game.Logger.LogInformation($"[Server] Received message from player: {player.Id}");
            
            try
            {
                var json = Encoding.UTF8.GetString(messageBytes);
                Game.Logger.LogInformation($"[Server] Message content: {json}");
                
                // 解析消息类型
                var messageData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (messageData != null && messageData.ContainsKey("type"))
                {
                    var messageType = messageData["type"].ToString();
                    
                    if (messageType == "request_verification_code")
                    {
                        var email = messageData["email"].ToString() ?? string.Empty;
                        return await OnRequestVerificationCode(player, email);
                    }
                    else if (messageType == "verify_code")
                    {
                        var email = messageData["email"].ToString() ?? string.Empty;
                        var code = messageData["code"].ToString() ?? string.Empty;
                        // 更安全地转换时间戳
                        long timestamp = 0;
                        if (messageData.ContainsKey("timestamp"))
                        {
                            var timestampObj = messageData["timestamp"];
                            if (timestampObj != null)
                            {
                                // 尝试多种转换方式
                                if (timestampObj is JsonElement jsonElement)
                                {
                                    if (jsonElement.ValueKind == JsonValueKind.Number)
                                    {
                                        timestamp = jsonElement.GetInt64();
                                    }
                                    else if (jsonElement.ValueKind == JsonValueKind.String)
                                    {
                                        long.TryParse(jsonElement.GetString(), out timestamp);
                                    }
                                }
                                else
                                {
                                    // 尝试直接转换
                                    long.TryParse(timestampObj.ToString(), out timestamp);
                                }
                            }
                        }
                        return await OnVerifyCode(player, email, code, timestamp);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[Server] 处理客户端消息失败");
                return false;
            }
        }

        /// <summary>
        /// 处理请求验证码事件
        /// </summary>
        private static async Task<bool> OnRequestVerificationCode(Player player, string email)
        {
            Game.Logger.LogInformation($"[Server] Received verification code request for email: {email}");

            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    Game.Logger.LogWarning("[Server] Empty email provided in verification code request");
                    return false;
                }
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // 生成验证码
                string code = GenerateCode(email, timestamp);

                Game.Logger.LogInformation($"[Server] Generated code for {email}: {code} (Timestamp: {timestamp})");

                // 发送验证码邮件
                bool emailSent = await emailService.SendVerificationCodeAsync(email, code);

                if (!emailSent)
                {
                    Game.Logger.LogError($"[Server] Failed to send verification email to {email}");

                    var errorResponse = new EventVerificationCodeResponse
                    {
                        Timestamp = 0,
                        Success = false,
                        Message = "邮件发送失败，请稍后重试"
                    };

                    IEventSenderExtensions.Publish(Game.Instance, errorResponse);
                    return false;
                }

                Game.Logger.LogInformation($"[Server] Verification email sent successfully to {email}");

                // 使用ProtoCustomMessage发送响应回客户端
                var responseData = new EventVerificationCodeResponse()
                {
                    Timestamp = timestamp,
                    Success = true,
                    Message = "验证码已发送至您的邮箱"
                };

                var responseJson = JsonSerializer.Serialize(responseData);
                var responseMessage = new ProtoCustomMessage
                {
                    Message = Encoding.UTF8.GetBytes(responseJson)
                };

                // 发送响应到客户端
                // responseMessage.SendToClient();
                responseMessage.SendTo(player);
                Game.Logger.LogInformation($"[Server] Sent verification response via ProtoCustomMessage to client");

                return true;
            }
            catch (Exception ex)
            {
                Game.Logger.LogError($"[Server] Error handling verification request: {ex.Message}");

                var errorResponse = new EventVerificationCodeResponse
                {
                    Timestamp = 0,
                    Success = false,
                    Message = "服务器错误，请稍后重试"
                };

                IEventSenderExtensions.Publish(Game.Instance, errorResponse);
                return false;
            }
        }

        /// <summary>
        /// 处理验证验证码事件
        /// </summary>
        private static Task<bool> OnVerifyCode(Player player, string email, string inputCode, long timestamp)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(inputCode))
                {
                    Game.Logger.LogWarning("[Server] Empty email or code provided in verification request");
                    return Task.FromResult(false);
                }

                // 验证时间戳有效期 (例如 5分钟内有效)
                long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (currentTimestamp - timestamp > 300) // 300秒 = 5分钟
                {
                    SendVerifyResponse(player, false, "验证码已过期");
                    return Task.FromResult(true);
                }

                // 重新生成验证码进行比对
                string expectedCode = GenerateCode(email, timestamp);

                if (inputCode == expectedCode)
                {
                    Game.Logger.LogInformation($"[Server] Verification successful for {email}");
                    // 模拟第一次登录，随机生成昵称
                    string nickname = "Player" + new Random().Next(1000, 9999);
                    SendVerifyResponse(player, true, "登录成功", nickname);
                }
                else
                {
                    Game.Logger.LogInformation($"[Server] Verification failed for {email}. Expected: {expectedCode}, Got: {inputCode}");
                    SendVerifyResponse(player, false, "验证码错误");
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Game.Logger.LogError($"[Server] Error verifying code: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private static void SendVerifyResponse(Player player, bool success, string message, string nickname = "")
        {
            // 使用ProtoCustomMessage发送验证结果响应
            var responseData = new
            {
                type = "verify_code_response",
                success = success,
                message = message,
                nickname = nickname
            };

            var responseJson = JsonSerializer.Serialize(responseData);
            var responseMessage = new ProtoCustomMessage
            {
                Message = Encoding.UTF8.GetBytes(responseJson)
            };

            // 发送响应到客户端
            // responseMessage.SendToClient();
            responseMessage.SendTo(player);
            Game.Logger.LogInformation($"[Server] Sent verification result via ProtoCustomMessage: success={success}, message={message}");
        }

        /// <summary>
        /// 根据邮箱和时间戳生成6位验证码
        /// </summary>
        private static string GenerateCode(string email, long timestamp)
        {
            // 组合邮箱和时间戳作为种子
            string seed = $"{email}:{timestamp}:GameWuxingSecretSalt";

            byte[] inputBytes = Encoding.UTF8.GetBytes(seed);

            // 取哈希值的前4个字节转换为整数
            int hashInt = Math.Abs(BitConverter.ToInt32(inputBytes, 0));

            // 取模得到6位数
            int codeInt = hashInt % 1000000;

            // 格式化为6位字符串，不足补0
            return codeInt.ToString("D6");
        }
    }
}
#endif