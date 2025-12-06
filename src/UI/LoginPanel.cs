#if CLIENT
using GameUI.Brush;
using GameUI.Control.Extensions;
using GameUI.Control.Primitive;
using GameUI.Data;
using GameUI.DesignSystem;
using GameUI.Enum;
using GameUI.Struct;
using GameCore.GameSystem;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using static GameUI.Control.Extensions.UI;
using GameEntry.Network;
using System.Text;
using GameEntry.Client;

namespace GameEntry.HelloWX.UI
{
    /// <summary>
    /// 登录界面 - 支持邮箱验证码登录流程
    /// 流程：输入邮箱 -> 发送验证码 -> 输入6位验证码 -> 验证登录
    /// </summary>
    internal class LoginPanel
    {
        private Panel? mainPanel;
        private Panel? emailStepPanel;
        private Panel? codeStepPanel;
        
        // Email step controls
        private Input? emailInputLabel;
        private Label? emailErrorLabel;
        
        // Code step controls
        private Input? codeInput1;
        private Input? codeInput2;
        private Input? codeInput3;
        private Input? codeInput4;
        private Input? codeInput5;
        private Input? codeInput6;
        private Label? codeErrorLabel;
        private Label? emailDisplayLabel;
        private Button? verifyButton;
        private Label? countdownLabel;
        
        private string currentEmail = "****************"; // 演示用
        private CancellationTokenSource? countdownCancellation;

        /// <summary>
        /// 初始化登录界面
        /// </summary>
        internal void Initialize()
        {
            Game.Logger.LogInformation("[Client] Starting LoginPanel initialization...");
            
            mainPanel = CreateMainPanel();
            mainPanel.AddToVisualTree();
            
            // 确保在游戏系统初始化完成后再注册事件
            if (Game.Instance != null)
            {
                Game.Logger.LogInformation("[Client] Game instance available, registering event listeners...");
                RegisterResponseListeners();
            }
            else
            {
                Game.Logger.LogWarning("[Client] Game instance not available yet, delaying event registration...");
                // 如果游戏实例还未准备好，延迟注册
                Game.Delay(1000).ContinueWith(_ =>
                {
                    if (Game.Instance != null)
                    {
                        Game.Logger.LogInformation("[Client] Game instance now available, registering event listeners...");
                        RegisterResponseListeners();
                    }
                    else
                    {
                        Game.Logger.LogError("[Client] Failed to register event listeners - Game instance still not available");
                    }
                });
            }
            
            Game.Logger.LogInformation("[Client] LoginPanel UI Initialized");
        }

        /// <summary>
        /// 创建主面板
        /// </summary>
        private Panel CreateMainPanel()
        {
            // 创建邮箱输入步骤
            emailStepPanel = CreateEmailStep();
            
            // 创建验证码输入步骤
            codeStepPanel = CreateCodeStep();
            codeStepPanel.Hide(); // 初始隐藏验证码步骤
            
            var mainContent = VStack(48,
                // Logo区域
                CreateLogoSection(),
                
                // 步骤容器
                Panel()
                    .Add(emailStepPanel)
                    .Add(codeStepPanel)
            );
            
            var mainPanel = Panel();
            mainPanel.Add(mainContent);
            mainPanel.Width = 440;
            mainPanel.Height = 600;
            mainPanel.HorizontalAlignment = HorizontalAlignment.Center;
            mainPanel.VerticalAlignment = VerticalAlignment.Center;
            mainPanel.Padding = new Thickness(48);
            mainPanel.Background = new SolidColorBrush(DesignColors.Surface);
            mainPanel.CornerRadius = 24;
            // Layer property doesn't exist on Panel, removing it
            return mainPanel;
        }

        /// <summary>
        /// 创建Logo和标题区域
        /// </summary>
        private Panel CreateLogoSection()
        {
            var content = VStack(24,
                // Logo图标
                Panel()
                    .Size(80, 80)
                    .CornerRadius(20)
                    .Background(DesignColors.Primary),
                
                // 标题
                Title("欢迎回来", 32)
                    .Bold()
                    .TextColor(DesignColors.OnSurface),
                
                // 副标题
                Body("登录您的账户继续游戏", 15)
                    .TextColor(DesignColors.Secondary)
                    .Opacity(0.7f)
            );
            
            var panel = Panel();
            panel.Add(content);
            panel.HorizontalAlignment = HorizontalAlignment.Center;
            panel.VerticalAlignment = VerticalAlignment.Center;
            return panel;
        }

        /// <summary>
        /// 创建邮箱输入步骤
        /// </summary>
        private Panel CreateEmailStep()
        {
            // 邮箱输入框 - 带底图背景
            emailInputLabel = Input("2@163.com")
                .FontSize(15)
                .TextColor(DesignColors.OnSurface)
                .Padding(36, 14)
                .Background(Color.FromArgb(155, 199, 199, 199)) // 增强背景透明度
                .CornerRadius(12)
                .Size(344, 48);
            
            var content = VStack(24,
                // 邮箱输入组
                VStack(8,
                    // 标签
                    Body("邮箱地址", 14)
                        .Bold()
                        .TextColor(DesignColors.OnSurface),
                    
                    // 输入框（演示）
                    emailInputLabel,
                    
                    // 错误提示
                    emailErrorLabel = Label("")
                        .FontSize(13)
                        .TextColor(DesignColors.Error)
                        .MinHeight(18)
                ),
                
                // 发送验证码按钮
                Primary("发送验证码")
                    .Size(344, 48)
                    .FontSize(16)
                    .Bold()
                    .CornerRadius(12)
                    .Click(OnSendCodeClicked)
            );
            
            return Panel().Add(content);
        }

        /// <summary>
        /// 创建验证码输入步骤
        /// </summary>
        private Panel CreateCodeStep()
        {
            var content = VStack(32,
                // 返回按钮
                HStack(8,
                    Label("←")
                        .FontSize(18)
                        .TextColor(DesignColors.Secondary),
                    Body("返回", 14)
                        .TextColor(DesignColors.Secondary)
                )
                .Padding(8)
                .CornerRadius(8)
                .Click(OnBackClicked),
                
                // 邮箱显示
                VStack(4,
                    Caption("验证码已发送至")
                        .TextColor(DesignColors.Secondary)
                        .Center(),
                    emailDisplayLabel = Body("", 15)
                        .Bold()
                        .TextColor(DesignColors.Primary)
                        .Center()
                )
                .Padding(16)
                .CornerRadius(12)
                .Background(Color.FromArgb(26, 102, 126, 234)), // rgba(102,126,234,0.1)
                
                // 验证码输入组
                VStack(8,
                    Body("验证码", 14)
                        .Bold()
                        .TextColor(DesignColors.OnSurface),
                    
                    // 6个验证码输入框
                    CreateCodeInputs(),
                    
                    // 倒计时标签
                    countdownLabel = Caption("", 13)
                        .TextColor(DesignColors.Secondary)
                        .Center()
                        .MinHeight(18),
                    
                    // 错误提示
                    codeErrorLabel = Label("")
                        .FontSize(13)
                        .TextColor(DesignColors.Error)
                        .MinHeight(18)
                ),
                
                // 重新发送按钮
                Body("重新发送验证码", 14)
                    .TextColor(DesignColors.Primary)
                    .Bold()
                    .Center()
                    .Padding(8)
                    .CornerRadius(6)
                    .Click(OnResendClicked),
                
                // 验证按钮
                verifyButton = Primary("验证并登录")
                    .Size(344, 48)
                    .FontSize(16)
                    .Bold()
                    .CornerRadius(12)
                    .Click(OnVerifyClicked)
            );
            
            return Panel().Add(content);
        }

        /// <summary>
        /// 创建6个验证码输入框
        /// </summary>
        private Panel CreateCodeInputs()
        {
            codeInput1 = CreateSingleCodeInput();
            codeInput2 = CreateSingleCodeInput();
            codeInput3 = CreateSingleCodeInput();
            codeInput4 = CreateSingleCodeInput();
            codeInput5 = CreateSingleCodeInput();
            codeInput6 = CreateSingleCodeInput();
            
            var content = HStack(12,
                codeInput1,
                codeInput2,
                codeInput3,
                codeInput4,
                codeInput5,
                codeInput6
            );
            
            return Panel().Add(content);
        }

        /// <summary>
        /// 创建单个验证码输入框
        /// </summary>
        private Input CreateSingleCodeInput()
        {
            var input = Input("")
                .Size(56, 64)
                .FontSize(24)
                .Bold()
                .TextColor(DesignColors.OnSurface)
                .Background(Color.FromArgb(30, 199, 199, 199)) // 增强背景透明度
                .CornerRadius(12)
                .ContentCenter()
                .Center(); // 限制只能输入1个字符
                
            // 添加文本变化事件监听
            input.OnInputTextChanged += OnCodeInputTextChanged;
            
            return input;
        }
        
        /// <summary>
        /// 验证码输入框文本变化事件
        /// </summary>
        private void OnCodeInputTextChanged(object? sender, EventArgs e)
        {
            if (sender is Input input)
            {
                // 限制只能输入一个字符
                if (input.Text.Length > 1)
                {
                    input.Text = input.Text.Substring(0, 1);
                }
                
                // 如果输入了字符，自动跳转到下一个输入框
                if (!string.IsNullOrEmpty(input.Text) && input.Text.Length == 1)
                {
                    FocusNextCodeInput(input);
                }
                // 如果清空了字符，可能需要移动到前一个输入框（支持退格键）
                else if (string.IsNullOrEmpty(input.Text))
                {
                    // We could implement backward focus shifting here if needed
                }
            }
        }
        
        /// <summary>
        /// 聚焦到下一个验证码输入框
        /// </summary>
        private void FocusNextCodeInput(Input currentInput)
        {
            // 确定当前输入框的索引
            int currentIndex = -1;
            if (currentInput == codeInput1) currentIndex = 0;
            else if (currentInput == codeInput2) currentIndex = 1;
            else if (currentInput == codeInput3) currentIndex = 2;
            else if (currentInput == codeInput4) currentIndex = 3;
            else if (currentInput == codeInput5) currentIndex = 4;
            else if (currentInput == codeInput6) currentIndex = 5;
            
            // 聚焦到下一个输入框
            if (currentIndex >= 0 && currentIndex < 5)
            {
                Input? nextInput = currentIndex switch
                {
                    0 => codeInput2,
                    1 => codeInput3,
                    2 => codeInput4,
                    3 => codeInput5,
                    4 => codeInput6,
                    _ => null
                };
                
                if (nextInput != null)
                {
                    // 延迟一下再聚焦，确保字符已经输入
                    Game.Delay(50).ContinueWith(_ =>
                    {
                        //nextInput.Focus();
                    });
                }
            }
            // 如果是最后一个输入框，则自动触发验证
            else if (currentIndex == 5)
            {
                // 自动触发验证
                if (verifyButton != null)
                {
                    Game.Delay(100).ContinueWith(_ => // 稍微延迟以确保所有输入已完成
                    {
                        OnVerifyClicked(null, EventArgs.Empty);
                    });
                }
            }
        }

        // ==================== 事件处理 ====================

        private long currentTimestamp; // 存储服务器返回的时间戳

        /// <summary>
        /// 发送验证码按钮点击事件
        /// </summary>
        private void OnSendCodeClicked(object? sender, EventArgs e)
        {
            if (emailInputLabel == null || emailErrorLabel == null) return;
            
            var email = emailInputLabel.Text?.Trim() ?? string.Empty;
            
            // 验证邮箱
            if (string.IsNullOrEmpty(email))
            {
                emailErrorLabel.Text = "请输入邮箱地址";
                return;
            }

            if (!IsValidEmail(email))
            {
                emailErrorLabel.Text = "请输入有效的邮箱地址";
                return;
            }
            
            emailErrorLabel.Text = "";
            currentEmail = email;
            
            // 注册响应监听
            RegisterResponseListeners();
            Game.Logger.LogInformation("[Client] Response listeners registered for email verification");
            
            // 使用ProtoCustomMessage系统发送请求到服务器
            var requestData = new
            {
                type = "request_verification_code",
                email = email
            };
            
            var requestJson = System.Text.Json.JsonSerializer.Serialize(requestData);
            var requestMessage = new ProtoCustomMessage
            {
                Message = Encoding.UTF8.GetBytes(requestJson)
            };
            
            if (requestMessage.SendToServer())
            {
                Game.Logger.LogInformation($"[Client] Successfully sent verification code request to server for: {email}");
            }
            else
            {
                Game.Logger.LogError($"[Client] Failed to send verification code request to server for: {email}");
                emailErrorLabel.Text = "网络连接失败，请稍后重试";
                return;
            }
            
            Game.Logger.LogInformation($"[Client] Requesting verification code for: {email}");
            
            // 禁用按钮防止重复点击 (实际项目中应该有Loading状态)
            if (sender is Button btn)
            {
                // btn.Disable(); // 暂时没有Disable方法，可以加个标志位
            }
        }

        /// <summary>
        /// 注册网络响应监听器 - 使用ProtoCustomMessage系统
        /// </summary>
        private void RegisterResponseListeners()
        {
            Game.Logger.LogInformation("[Client] Registering response listeners using ProtoCustomMessage system...");
            
            try
            {
                // 使用ProtoCustomMessage系统监听来自服务器的消息
                Trigger<EventServerMessage> serverMessageTrigger = new(OnServerMessageReceived, keepReference: true);
                serverMessageTrigger.Register(Game.Instance);
                Game.Logger.LogInformation("[Client] Registered EventServerMessage trigger for ProtoCustomMessage");
                
                Game.Logger.LogInformation("[Client] ProtoCustomMessage listeners registered successfully");
            }
            catch (Exception ex)
            {
                Game.Logger.LogError($"[Client] Failed to register response listeners: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理来自服务器的ProtoCustomMessage消息
        /// </summary>
        private async Task<bool> OnServerMessageReceived(object sender, EventServerMessage eventArgs)
        {
            var messageBytes = eventArgs.Message;
            
            try
            {
                var json = Encoding.UTF8.GetString(messageBytes);
                Game.Logger.LogInformation($"[Client] Received server message: {json}");
                
                // 解析消息类型
                var messageData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (messageData != null && messageData.ContainsKey("type"))
                {
                    var messageType = messageData["type"].ToString();
                    
                    if (messageType == "verification_code_response")
                    {
                        var codeResponse = JsonSerializer.Deserialize<EventVerificationCodeResponse>(json);
                        await OnVerificationCodeResponse(sender, codeResponse);
                    }
                    else if (messageType == "verify_code_response")
                    {
                        var verifyResponse = JsonSerializer.Deserialize<EventVerifyCodeResponse>(json);
                        await OnVerifyResultResponse(sender, verifyResponse);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[Client] 处理服务器消息失败");
                return false;
            }
        }

        /// <summary>
        /// 处理验证码发送响应
        /// </summary>
        private Task<bool> OnVerificationCodeResponse(object sender, GameEntry.Network.EventVerificationCodeResponse e)
        {
            Game.Logger.LogInformation($"[Client] Received verification code response: Success={e.Success}, Timestamp={e.Timestamp}, Message={e.Message}");
            
            if (e.Success)
            {
                currentTimestamp = e.Timestamp;
                Game.Logger.LogInformation($"[Client] Code sent successfully. Timestamp: {currentTimestamp}");
                
                // 切换到验证码步骤 (需要在主线程执行UI更新)
                // 注意：如果Trigger是在非UI线程触发，这里可能需要Dispatcher
                SwitchToCodeStep();
                
                // 开始倒计时
                StartCountdown();
            }
            else
            {
                Game.Logger.LogWarning($"[Client] Verification code request failed: {e.Message}");
                if (emailErrorLabel != null)
                    emailErrorLabel.Text = e.Message;
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// 处理验证结果响应
        /// </summary>
        private Task<bool> OnVerifyResultResponse(object sender, GameEntry.Network.EventVerifyCodeResponse e)
        {
            if (e.Success)
            {
                Game.Logger.LogInformation($"[Client] Login successful! Nickname: {e.Nickname}");
                // 停止倒计时
                StopCountdown();
                
                // 关闭当前登录界面
                mainPanel?.RemoveFromParent();
                
                // 打开主界面并显示昵称 (MainPanel构造函数内部会自动添加到UI Root)
                var mainPanelUI = new MainPanel(e.Nickname);
            }
            else
            {
                if (codeErrorLabel != null)
                    codeErrorLabel.Text = e.Message;
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// 验证按钮点击事件
        /// </summary>
        private void OnVerifyClicked(object? sender, EventArgs e)
        {
            var code = GetVerificationCode();
            
            if (code.Length != 6)
            {
                if (codeErrorLabel != null)
                    codeErrorLabel.Text = "请输入完整的6位验证码";
                return;
            }
            
            if (codeErrorLabel != null)
                codeErrorLabel.Text = "";
            
            // 使用ProtoCustomMessage系统发送验证请求到服务器
            var verifyRequestData = new
            {
                type = "verify_code",
                email = currentEmail,
                code = code,
                timestamp = currentTimestamp
            };
            
            var verifyRequestJson = System.Text.Json.JsonSerializer.Serialize(verifyRequestData);
            var verifyRequestMessage = new ProtoCustomMessage
            {
                Message = Encoding.UTF8.GetBytes(verifyRequestJson)
            };
            
            if (verifyRequestMessage.SendToServer())
            {
                Game.Logger.LogInformation($"[Client] Successfully sent verification request to server for: {currentEmail}");
            }
            else
            {
                Game.Logger.LogError($"[Client] Failed to send verification request to server for: {currentEmail}");
                if (codeErrorLabel != null)
                    codeErrorLabel.Text = "网络连接失败，请稍后重试";
                return;
            }
            
            Game.Logger.LogInformation($"[Client] Verifying code: {code} for {currentEmail}");
        }

        /// <summary>
        /// 返回按钮点击事件
        /// </summary>
        private void OnBackClicked(object? sender, EventArgs e)
        {
            SwitchToEmailStep();
            StopCountdown();
        }

        /// <summary>
        /// 重新发送按钮点击事件
        /// </summary>
        private void OnResendClicked(object? sender, EventArgs e)
        {
            // 使用ProtoCustomMessage系统重新发送验证码请求到服务器
            var resendRequestData = new
            {
                type = "request_verification_code",
                email = currentEmail
            };
            
            var resendRequestJson = System.Text.Json.JsonSerializer.Serialize(resendRequestData);
            var resendRequestMessage = new ProtoCustomMessage
            {
                Message = Encoding.UTF8.GetBytes(resendRequestJson)
            };
            
            if (resendRequestMessage.SendToServer())
            {
                Game.Logger.LogInformation($"[Client] Successfully re-sent verification code request to server for: {currentEmail}");
            }
            else
            {
                Game.Logger.LogError($"[Client] Failed to re-send verification code request to server for: {currentEmail}");
                if (emailErrorLabel != null)
                    emailErrorLabel.Text = "网络连接失败，请稍后重试";
                return;
            }
            
            Game.Logger.LogInformation($"[Client] Re-requesting verification code for: {currentEmail}");
            
            // 清空验证码输入
            ClearCodeInputs();
            
            // 重新开始倒计时
            StopCountdown();
            StartCountdown();
        }

        // ==================== 辅助方法 ====================

        /// <summary>
        /// 开始倒计时（5分钟）
        /// </summary>
        private async void StartCountdown()
        {
            StopCountdown(); // 先停止之前的倒计时
            
            countdownCancellation = new CancellationTokenSource();
            var token = countdownCancellation.Token;
            
            try
            {
                int totalSeconds = 300; // 5分钟
                
                while (totalSeconds > 0 && !token.IsCancellationRequested)
                {
                    int minutes = totalSeconds / 60;
                    int seconds = totalSeconds % 60;
                    string timeText = $"验证码有效期：{minutes:D2}:{seconds:D2}";
                    
                    // 更新UI（直接在主线程执行）
                    if (countdownLabel != null)
                    {
                        countdownLabel.Text = timeText;
                    }
                    
                    // 使用框架提供的Delay方法，而非Task.Delay
                    await Game.Delay(1000, token);
                    totalSeconds--;
                }
                
                if (!token.IsCancellationRequested && countdownLabel != null)
                {
                    countdownLabel.Text = "验证码已过期";
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，无需处理
            }
        }

        /// <summary>
        /// 停止倒计时
        /// </summary>
        private void StopCountdown()
        {
            countdownCancellation?.Cancel();
            countdownCancellation?.Dispose();
            countdownCancellation = null;
            
            if (countdownLabel != null)
            {
                countdownLabel.Text = "";
            }
        }

        /// <summary>
        /// 切换到验证码步骤
        /// </summary>
        private void SwitchToCodeStep()
        {
            {
                emailStepPanel.Hide();
                codeStepPanel.Show();
                emailDisplayLabel.Text = currentEmail;
            }
        }

        /// <summary>
        /// 切换到邮箱步骤
        /// </summary>
        private void SwitchToEmailStep()
        {
            if (emailStepPanel != null && codeStepPanel != null)
            {
                codeStepPanel.Hide();
                emailStepPanel.Show();
                ClearCodeInputs();
            }
        }

        /// <summary>
        /// 获取验证码
        /// </summary>
        private string GetVerificationCode()
        {
            var code = string.Empty;
            if (codeInput1 != null) code += codeInput1.Text ?? "";
            if (codeInput2 != null) code += codeInput2.Text ?? "";
            if (codeInput3 != null) code += codeInput3.Text ?? "";
            if (codeInput4 != null) code += codeInput4.Text ?? "";
            if (codeInput5 != null) code += codeInput5.Text ?? "";
            if (codeInput6 != null) code += codeInput6.Text ?? "";
            return code;
        }

        /// <summary>
        /// 清空验证码输入
        /// </summary>
        private void ClearCodeInputs()
        {
            if (codeInput1 != null) codeInput1.Text = "";
            if (codeInput2 != null) codeInput2.Text = "";
            if (codeInput3 != null) codeInput3.Text = "";
            if (codeInput4 != null) codeInput4.Text = "";
            if (codeInput5 != null) codeInput5.Text = "";
            if (codeInput6 != null) codeInput6.Text = "";
        }
        
        /// <summary>
        /// 验证邮箱格式
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // 使用正则表达式验证邮箱格式
                // 简单的邮箱验证规则：
                // 1. 用户名部分：允许字母、数字、点、下划线、连字符
                // 2. @ 符号
                // 3. 域名部分：允许字母、数字、连字符
                // 4. 顶级域名：至少2个字母
                var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return System.Text.RegularExpressions.Regex.IsMatch(email, pattern);
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif
