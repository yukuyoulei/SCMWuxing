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
            _panel = Panel()
                .FillParent()
                .Background(DesignColors.Background)
                .Add(
                    VStack(0,
                        // 顶部玩家信息栏 (固定高度)
                        CreateHeaderSection(),
                        
                        // 主游戏内容区域 (填充剩余空间)
                        CreateMainContentArea(),
                        
                        // 底部导航栏 (固定高度)
                        CreateBottomNavBar()
                    ).Stretch()
                );
            
            // 添加到UI根节点
            _panel.AddToVisualTree();

            Game.Logger.LogInformation($"[Client] MainPanel created with nickname: {_playerData.Nickname}, level: {_playerData.Level}, gold: {_playerData.Gold}");
        }

        /// <summary>
        /// 创建顶部玩家信息栏
        /// </summary>
        private Panel CreateHeaderSection()
        {
            var headerContent = HStack(12,
                // 头像 (圆形占位)
                Panel()
                    .Size(48, 48)
                    .CornerRadius(24)
                    .Background(DesignColors.Primary),
                
                // 昵称
                Label(_playerData.Nickname)
                    .FontSize(18)
                    .Bold()
                    .TextColor(DesignColors.OnSurface),
                
                // 弹性间距
                Spacer(),
                
                // 等级显示
                _levelLabel = Label($"Lv.{_playerData.Level}")
                    .FontSize(16)
                    .Bold()
                    .TextColor(DesignColors.Primary),
                
                // 货币显示区域
                HStack(4,
                    Label("💰")
                        .FontSize(16),
                    _currencyLabel = Label(FormatCurrency(_playerData.Gold))
                        .FontSize(16)
                        .TextColor(DesignColors.OnSurface)
                )
            );

            return Panel()
                .Add(headerContent)
                .Height(64)
                .StretchHorizontal()
                .Padding(16, 8)
                .Background(DesignColors.Surface);
        }

        /// <summary>
        /// 创建主游戏内容区域
        /// </summary>
        private Panel CreateMainContentArea()
        {
            var content = VStack(24,
                // 留出顶部空间
                Spacer(),
                
                // 欢迎信息
                Label("五行挂机")
                    .FontSize(36)
                    .Bold()
                    .TextColor(DesignColors.Primary)
                    .Center(),
                
                // 副标题
                Label($"欢迎, {_playerData.Nickname}")
                    .FontSize(18)
                    .TextColor(DesignColors.Secondary)
                    .Center(),
                
                // 间距
                Panel().Height(40),
                
                // 五行元素展示区
                Label("五行修炼")
                    .FontSize(16)
                    .TextColor(DesignColors.OnSurface)
                    .Center(),
                
                HStack(20,
                    CreateElementIcon("金", Color.FromArgb(255, 255, 215, 0)),   // 金 - 金色
                    CreateElementIcon("木", Color.FromArgb(255, 34, 139, 34)),   // 木 - 绿色
                    CreateElementIcon("水", Color.FromArgb(255, 30, 144, 255)),  // 水 - 蓝色
                    CreateElementIcon("火", Color.FromArgb(255, 255, 69, 0)),    // 火 - 红色
                    CreateElementIcon("土", Color.FromArgb(255, 139, 90, 43))    // 土 - 棕色
                ).Center(),
                
                // 留出底部空间
                Spacer()
            ).Stretch();

            return Panel()
                .Add(content)
                .HeightGrow(1)  // 填充剩余垂直空间
                .StretchHorizontal()
                .Padding(16)
                .Background(DesignColors.Background);
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

        /// <summary>
        /// 创建底部导航栏
        /// </summary>
        private Panel CreateBottomNavBar()
        {
            var navContent = HStack(0,
                CreateNavButton("⬆️", "升级", () => OnBackpackClicked()),
                CreateNavDivider(),
                CreateNavButton("⚔️", "技能", () => OnSkillsClicked()),
                CreateNavDivider(),
                CreateNavButton("🛒", "商店", () => OnShopClicked()),
                CreateNavDivider(),
                CreateNavButton("⚙️", "设置", () => OnSettingsClicked())
            ).StretchHorizontal();

            return Panel()
                .Add(navContent)
                .Height(80)
                .StretchHorizontal()
                .Padding(8, 0)
                .Background(DesignColors.Surface);
        }

        /// <summary>
        /// 创建导航按钮分隔线
        /// </summary>
        private Panel CreateNavDivider()
        {
            return Panel()
                .Width(1)
                .Height(40)
                .Background(Color.FromArgb(50, 128, 128, 128));
        }

        /// <summary>
        /// 创建导航按钮
        /// </summary>
        private Panel CreateNavButton(string icon, string label, Action onClick)
        {
            var buttonContent = VStack(6,
                Label(icon)
                    .FontSize(28)
                    .Center(),
                Label(label)
                    .FontSize(14)
                    .Bold()
                    .TextColor(DesignColors.OnSurface)
                    .Center()
            ).Center();

            var button = Panel()
                .Add(buttonContent)
                .WidthGrow(1)
                .Height(80)
                .Click((sender, e) => onClick());

            return button;
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
                _levelLabel.Text = $"Lv.{level}";
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
