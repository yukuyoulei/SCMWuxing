#if CLIENT
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;
using GameUI.DesignSystem;

namespace GameEntry.Client
{
    /// <summary>
    /// 主界面 - 显示登录后的用户信息
    /// </summary>
    public class MainPanel
    {
        private readonly string _nickname;
        private GameUI.Control.Panel? _panel;

        public MainPanel(string nickname)
        {
            _nickname = nickname;
            
            // 构建UI
            BuildUI();
        }

        private void BuildUI()
        {
            _panel = Panel()
                .FillParent()
                .Background(DesignColors.Background)
                .Add(
                    // 居中显示的欢迎信息
                    VStack(20,
                        Label("Welcome to GameWuxing")
                            .FontSize(32)
                            .Bold()
                            .TextColor(DesignColors.Primary)
                            .Center(),

                        Label(_nickname)
                            .FontSize(48)
                            .Bold()
                            .TextColor(DesignColors.OnSurface)
                            .Center(),

                        Label("Available Characters")
                            .FontSize(20)
                            .TextColor(DesignColors.Secondary)
                            .Center()
                            .Margin(0, 40, 0, 0)
                    ).Center()
                );
            
            // 添加到UI根节点
            Game.UI.Root.AddChild(_panel);

            Game.Logger.LogInformation($"[Client] MainPanel created with nickname: {_nickname}");
        }
    }
}
#endif
