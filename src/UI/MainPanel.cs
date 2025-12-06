#if CLIENT
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;
using GameUI.DesignSystem;
using GameUI.Control.Primitive;
using GameUI.Struct;
using System;
using System.Drawing;

namespace GameEntry.Client
{
    /// <summary>
    /// 主界面 - 五行挂机游戏主界面
    /// 包含: 玩家信息栏(头像/昵称/等级)、游戏主区域、底部导航栏
    /// </summary>
    public class MainPanel
    {
        private readonly string _nickname;
        private readonly int _level;
        private Panel? _panel;

        // UI控件引用
        private Label? _levelLabel;
        private Label? _currencyLabel;

        public MainPanel(string nickname, int level = 1)
        {
            _nickname = nickname;
            _level = level;
            
            // 构建UI
            BuildUI();
        }

        private void BuildUI()
        {
            _panel = Panel()
                .FillParent()
                .Background(DesignColors.Background)
                .Add(
                    VStack(0,
                        // 顶部玩家信息栏
                        CreateHeaderSection(),
                        
                        // 主游戏内容区域
                        CreateMainContentArea(),
                        
                        // 底部导航栏
                        CreateBottomNavBar()
                    ).Stretch()
                );
            
            // 添加到UI根节点
            _panel.AddToVisualTree();

            Game.Logger.LogInformation($"[Client] MainPanel created with nickname: {_nickname}, level: {_level}");
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
                Label(_nickname)
                    .FontSize(18)
                    .Bold()
                    .TextColor(DesignColors.OnSurface),
                
                // 弹性间距
                Spacer(),
                
                // 等级显示
                _levelLabel = Label($"Lv.{_level}")
                    .FontSize(16)
                    .Bold()
                    .TextColor(DesignColors.Primary),
                
                // 货币显示区域
                HStack(4,
                    Label("💰")
                        .FontSize(16),
                    _currencyLabel = Label("0")
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
            var content = VStack(20,
                // 欢迎信息 (临时)
                Label("五行挂机")
                    .FontSize(32)
                    .Bold()
                    .TextColor(DesignColors.Primary)
                    .Center(),
                
                Label("游戏内容区域")
                    .FontSize(16)
                    .TextColor(DesignColors.Secondary)
                    .Center(),
                
                // 五行元素展示区 (占位)
                HStack(16,
                    CreateElementIcon("金", Color.FromArgb(255, 255, 215, 0)),   // 金 - 金色
                    CreateElementIcon("木", Color.FromArgb(255, 34, 139, 34)),   // 木 - 绿色
                    CreateElementIcon("水", Color.FromArgb(255, 30, 144, 255)),  // 水 - 蓝色
                    CreateElementIcon("火", Color.FromArgb(255, 255, 69, 0)),    // 火 - 红色
                    CreateElementIcon("土", Color.FromArgb(255, 139, 90, 43))    // 土 - 棕色
                ).Center()
            ).Center();

            return Panel()
                .Add(content)
                .Stretch()
                .StretchHorizontal()
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
                CreateNavButton("🎒", "背包", () => OnBackpackClicked()),
                CreateNavButton("⚔️", "技能", () => OnSkillsClicked()),
                CreateNavButton("🛒", "商店", () => OnShopClicked()),
                CreateNavButton("⚙️", "设置", () => OnSettingsClicked())
            );

            return Panel()
                .Add(navContent)
                .Height(80)
                .StretchHorizontal()
                .Background(DesignColors.Surface);
        }

        /// <summary>
        /// 创建导航按钮
        /// </summary>
        private Panel CreateNavButton(string icon, string label, Action onClick)
        {
            var buttonContent = VStack(4,
                Label(icon)
                    .FontSize(24)
                    .Center(),
                Label(label)
                    .FontSize(12)
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
            Game.Logger.LogInformation("[Client] Backpack button clicked");
            // TODO: 打开背包界面
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
    }
}
#endif
