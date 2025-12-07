#if CLIENT
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;
using GameUI.DesignSystem;
using GameUI.Control.Primitive;
using GameUI.Struct;
using GameEntry.Data;
using GameEntry.Network;
using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace GameEntry.Client
{
    /// <summary>
    /// 主界面 - 五行挂机游戏主界面
    /// 包含: 玩家信息栏(头像/昵称/等级)、游戏主区域、底部导航栏
    /// </summary>
    public class MainPanel
    {
        private readonly GameEntry.Data.PlayerData _playerData;
        private Panel? _panel;

        // UI控件引用
        private Label? _levelLabel;
        private Label? _currencyLabel;

        public MainPanel(GameEntry.Data.PlayerData playerData)
        {
            _playerData = playerData;
            
            // 注册服务器消息监听
            RegisterServerMessageListener();
            
            // 构建UI
            BuildUI();
        }
        
        /// <summary>
        /// 注册服务器消息监听器
        /// </summary>
        private void RegisterServerMessageListener()
        {
            try
            {
                var trigger = new Trigger<EventServerMessage>(OnServerMessageReceived, keepReference: true);
                trigger.Register(Game.Instance);
                Game.Logger.LogInformation("[Client] MainPanel registered for server messages");
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[Client] Failed to register MainPanel server message listener");
            }
        }
        
        /// <summary>
        /// 处理服务器消息
        /// </summary>
        private Task<bool> OnServerMessageReceived(object sender, EventServerMessage eventArgs)
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Message);
                var messageData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                
                if (messageData != null && messageData.TryGetValue("type", out var typeObj))
                {
                    var messageType = typeObj.ToString();
                    
                    if (messageType == "gm_command_response")
                    {
                        Game.Logger.LogInformation($"[Client] MainPanel received GM response: {json}");
                        ProcessGMResponse(json);
                    }
                }
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[Client] Error processing server message in MainPanel");
                return Task.FromResult(false);
            }
        }
        
        /// <summary>
        /// 处理GM命令响应
        /// </summary>
        private void ProcessGMResponse(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                bool success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                string command = root.TryGetProperty("command", out var cmdProp) ? cmdProp.GetString() ?? "" : "";
                string message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() ?? "" : "";
                
                Game.Logger.LogInformation($"[Client] GM Response: {command} - {message} (success={success})");
                
                if (success && command == "level_up" && root.TryGetProperty("data", out var dataProp))
                {
                    // 更新等级显示
                    if (dataProp.TryGetProperty("level", out var levelProp))
                    {
                        int newLevel = levelProp.GetInt32();
                        UpdateLevel(newLevel);
                        Game.Logger.LogInformation($"[Client] Updated level display to {newLevel}");
                    }
                    
                    // 更新金币显示
                    if (dataProp.TryGetProperty("gold", out var goldProp))
                    {
                        long newGold = goldProp.GetInt64();
                        UpdateCurrency(newGold);
                        Game.Logger.LogInformation($"[Client] Updated gold display to {newGold}");
                    }
                }
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[Client] Error processing GM response");
            }
        }

        private void BuildUI()
        {
            // 主布局：使用绝对定位的浮动元素 + 中心内容
            _panel = Panel()
                .FillParent()
                .Background(Color.FromArgb(255, 45, 55, 72))  // 深蓝紫色背景
                .Add(
                    // 中心游戏内容区域
                    CreateCenterGameArea(),
                    
                    // 顶部栏（头像+货币）- 浮动在顶部
                    CreateTopBar(),
                    
                    // 左侧任务/事件面板 - 浮动在左边
                    CreateLeftEventPanel(),
                    
                    // 右侧快捷按钮 - 浮动在右边
                    CreateRightActionButtons(),
                    
                    // 底部任务进度栏 - 浮动在底部
                    CreateBottomTaskBar()
                );
            
            _panel.AddToVisualTree();
            Game.Logger.LogInformation($"[Client] MainPanel created with nickname: {_playerData.Nickname}, level: {_playerData.Level}, gold: {_playerData.Gold}");
        }

        /// <summary>
        /// 创建顶部栏 - 左侧头像等级，右侧货币
        /// </summary>
        private Panel CreateTopBar()
        {
            return Panel()
                .StretchHorizontal()
                .Height(60)
                .AlignTop()
                .Padding(8, 8)
                .Add(
                    HStack(8,
                        // 左侧：头像 + 等级
                        CreateAvatarWithLevel(),
                        
                        Spacer(),
                        
                        // 右侧：货币显示
                        CreateCurrencyBar()
                    ).StretchHorizontal()
                );
        }

        /// <summary>
        /// 创建头像带等级徽章
        /// </summary>
        private Panel CreateAvatarWithLevel()
        {
            return Panel()
                .Size(56, 56)
                .Add(
                    // 头像框背景
                    Panel()
                        .Size(50, 50)
                        .CornerRadius(8)
                        .Background(Color.FromArgb(200, 139, 90, 43))  // 棕色边框
                        .Add(
                            Panel()
                                .Size(44, 44)
                                .CornerRadius(6)
                                .Background(Color.FromArgb(255, 100, 120, 140))  // 头像占位
                                .Center()
                        )
                        .Center(),
                    
                    // 等级徽章 - 左下角
                    Panel()
                        .Size(24, 20)
                        .CornerRadius(4)
                        .Background(Color.FromArgb(255, 255, 193, 7))  // 金色
                        .AlignBottom()
                        .AlignLeft()
                        .Add(
                            _levelLabel = Label($"{_playerData.Level}")
                                .FontSize(12)
                                .Bold()
                                .TextColor(Color.FromArgb(255, 50, 50, 50))
                                .Center()
                        )
                );
        }

        /// <summary>
        /// 创建货币栏 - 横向排列的货币显示
        /// </summary>
        private Panel CreateCurrencyBar()
        {
            return HStack(12,
                // 金币
                CreateCurrencyItem("💰", FormatCurrency(_playerData.Gold), Color.FromArgb(200, 0, 0, 0)),
                // 五行元素1 (金)
                CreateCurrencyItem("🔶", "5.67k", Color.FromArgb(200, 0, 0, 0)),
                // 五行元素2 (木)
                CreateCurrencyItem("🟢", "5.67k", Color.FromArgb(200, 0, 0, 0))
            );
        }

        /// <summary>
        /// 创建单个货币显示项
        /// </summary>
        private Panel CreateCurrencyItem(string icon, string value, Color bgColor)
        {
            return Panel()
                .Height(28)
                .Padding(8, 4)
                .CornerRadius(14)
                .Background(bgColor)
                .Add(
                    HStack(4,
                        Label(icon).FontSize(14),
                        _currencyLabel = Label(value)
                            .FontSize(14)
                            .Bold()
                            .TextColor(Color.White)
                    ).Center()
                );
        }

        /// <summary>
        /// 创建左侧事件/任务面板
        /// </summary>
        private Panel CreateLeftEventPanel()
        {
            return Panel()
                .Width(140)
                .Height(70)
                .AlignTop()
                .AlignLeft()
                .Margin(8, 75, 0, 0)  // 在顶部栏下方
                .CornerRadius(8)
                .Background(Color.FromArgb(180, 0, 0, 0))
                .Add(
                    HStack(8,
                        // 事件图标
                        Panel()
                            .Size(45, 45)
                            .CornerRadius(6)
                            .Background(Color.FromArgb(255, 100, 149, 237))  // 占位图标
                            .Center(),
                        
                        // 事件信息
                        VStack(2,
                            Label("五行试炼")
                                .FontSize(12)
                                .Bold()
                                .TextColor(Color.White),
                            Label("01:05:00")
                                .FontSize(14)
                                .Bold()
                                .TextColor(Color.FromArgb(255, 255, 215, 0))
                        )
                    ).Padding(8).Center()
                );
        }

        /// <summary>
        /// 创建右侧快捷操作按钮
        /// </summary>
        private Panel CreateRightActionButtons()
        {
            return Panel()
                .Width(60)
                .AlignTop()
                .AlignRight()
                .Margin(0, 75, 8, 0)  // 在顶部栏下方
                .Add(
                    VStack(12,
                        CreateRightButton("⬆️", "升级", () => OnBackpackClicked()),
                        CreateRightButton("📦", "背包", () => OnSkillsClicked()),
                        CreateRightButton("🛒", "商店", () => OnShopClicked()),
                        CreateRightButton("⚙️", "设置", () => OnSettingsClicked())
                    )
                );
        }

        /// <summary>
        /// 创建右侧单个按钮
        /// </summary>
        private Panel CreateRightButton(string icon, string label, Action onClick)
        {
            return Panel()
                .Size(50, 55)
                .CornerRadius(8)
                .Background(Color.FromArgb(180, 0, 0, 0))
                .Click((s, e) => onClick())
                .Add(
                    VStack(2,
                        Label(icon).FontSize(20).Center(),
                        Label(label).FontSize(10).TextColor(Color.White).Center()
                    ).Center()
                );
        }

        /// <summary>
        /// 创建中心游戏区域
        /// </summary>
        private Panel CreateCenterGameArea()
        {
            return Panel()
                .Stretch()
                .Add(
                    VStack(24,
                        Spacer(),
                        
                        // 游戏标题
                        Label("五行挂机")
                            .FontSize(32)
                            .Bold()
                            .TextColor(Color.White)
                            .Center(),
                        
                        Label($"欢迎, {_playerData.Nickname}")
                            .FontSize(16)
                            .TextColor(Color.FromArgb(200, 255, 255, 255))
                            .Center(),
                        
                        Panel().Height(30),
                        
                        // 五行元素展示
                        HStack(16,
                            CreateElementIcon("金", Color.FromArgb(255, 255, 215, 0)),
                            CreateElementIcon("木", Color.FromArgb(255, 34, 139, 34)),
                            CreateElementIcon("水", Color.FromArgb(255, 30, 144, 255)),
                            CreateElementIcon("火", Color.FromArgb(255, 255, 69, 0)),
                            CreateElementIcon("土", Color.FromArgb(255, 139, 90, 43))
                        ).Center(),
                        
                        Spacer(),
                        Spacer()
                    ).Stretch()
                );
        }

        /// <summary>
        /// 创建底部任务进度栏
        /// </summary>
        private Panel CreateBottomTaskBar()
        {
            return Panel()
                .StretchHorizontal()
                .Height(50)
                .AlignBottom()
                .Padding(12, 8)
                .Background(Color.FromArgb(200, 0, 0, 0))
                .Add(
                    HStack(12,
                        // 任务图标
                        Panel()
                            .Size(32, 32)
                            .CornerRadius(6)
                            .Background(Color.FromArgb(255, 255, 152, 0)),
                        
                        // 任务文本
                        Label("当前任务: 修炼五行元素")
                            .FontSize(14)
                            .TextColor(Color.White),
                        
                        Spacer(),
                        
                        // 快捷入口按钮
                        Panel()
                            .Size(40, 32)
                            .CornerRadius(6)
                            .Background(Color.FromArgb(255, 76, 175, 80))
                            .Add(
                                Label("📋").FontSize(16).Center()
                            )
                    ).StretchHorizontal()
                );
        }

        /// <summary>
        /// 创建五行元素图标
        /// </summary>
        private Panel CreateElementIcon(string element, Color color)
        {
            return Panel()
                .Size(50, 50)
                .CornerRadius(25)
                .Background(color)
                .Add(
                    Label(element)
                        .FontSize(20)
                        .Bold()
                        .TextColor(Color.White)
                        .Center()
                );
        }

        // ==================== 按钮事件处理 ====================

        private void OnBackpackClicked()
        {
            Game.Logger.LogInformation("[Client] Level Up button clicked - sending GM command");
            // 发送升级命令到服务器
            SendGMCommand("level_up");
        }

        private void OnSkillsClicked()
        {
            Game.Logger.LogInformation("[Client] Skills button clicked");
            // TODO: 打开技能界面
        }

        private void OnShopClicked()
        {
            Game.Logger.LogInformation("[Client] Shop button clicked");
            // TODO: 打开商店界面
        }

        private void OnSettingsClicked()
        {
            Game.Logger.LogInformation("[Client] Settings button clicked");
            // TODO: 打开设置界面
        }

        // ==================== 公共方法 ====================

        /// <summary>
        /// 更新玩家等级显示
        /// </summary>
        public void UpdateLevel(int level)
        {
            if (_levelLabel != null)
            {
                _levelLabel.Text = $"{level}";
            }
        }

        /// <summary>
        /// 更新货币显示
        /// </summary>
        public void UpdateCurrency(long amount)
        {
            if (_currencyLabel != null)
            {
                _currencyLabel.Text = FormatCurrency(amount);
            }
        }

        /// <summary>
        /// 格式化货币显示 (超过10000显示为1万等)
        /// </summary>
        private string FormatCurrency(long amount)
        {
            if (amount >= 100000000)
                return $"{amount / 100000000}亿";
            if (amount >= 10000)
                return $"{amount / 10000}万";
            return amount.ToString();
        }

        // ==================== 网络通信 ====================

        /// <summary>
        /// 发送GM命令到服务器
        /// </summary>
        private void SendGMCommand(string command, System.Collections.Generic.Dictionary<string, object>? args = null)
        {
            try
            {
                var message = new
                {
                    type = "gm_command",
                    command = command,
                    args = args ?? new System.Collections.Generic.Dictionary<string, object>()
                };

                var json = System.Text.Json.JsonSerializer.Serialize(message);
                var messageBytes = System.Text.Encoding.UTF8.GetBytes(json);
                
                var protoMessage = new ProtoCustomMessage
                {
                    Message = messageBytes
                };
                
                protoMessage.SendToServer();
                Game.Logger.LogInformation($"[Client] Sent GM command: {command}");
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[Client] Failed to send GM command");
            }
        }

        /// <summary>
        /// 处理GM命令响应
        /// </summary>
        public void HandleGMResponse(bool success, string command, string message, System.Collections.Generic.Dictionary<string, object>? data)
        {
            Game.Logger.LogInformation($"[Client] GM Response: {command} - {message} (success={success})");
            
            if (success && command == "level_up" && data != null)
            {
                // 更新UI显示
                if (data.TryGetValue("level", out var levelObj))
                {
                    if (levelObj is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        UpdateLevel(je.GetInt32());
                    }
                }
                
                if (data.TryGetValue("gold", out var goldObj))
                {
                    if (goldObj is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        UpdateCurrency(je.GetInt64());
                    }
                }
            }
        }
    }
}
#endif
