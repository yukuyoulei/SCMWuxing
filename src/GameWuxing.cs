#if CLIENT
using GameEntry.HelloWX.UI;

namespace GameEntry;

internal class GameWuxing : IGameClass
{
    LoginPanel panel;
    public static void OnRegisterGameClass()
    {
        Game.Logger.LogInformation("GameWuxing registered");
        Game.OnGameTriggerInitialization += RegisterAll;
    }

    private static void RegisterAll()
    {
        Game.Logger.LogInformation($"Game.GameModeLink is {Game.GameModeLink}");
        if (Game.GameModeLink != GameModes.GameWuxing)
        {
            return;
        }
        Trigger<EventGameStart> trigger = new((s, d) =>
        {
            Game.Logger.LogInformation("GameWuxing started");
            var game = new GameWuxing();
            game.Initialize();
            return Task.FromResult(true);
        });
        trigger.Register(Game.Instance);
    }

    private void InitializeUI()
    {
        Game.Logger.LogInformation("GameWuxing InitializeUI");
        panel = new LoginPanel();
        panel.Initialize();
    }

    private void Initialize()
    {
        Game.Logger.LogInformation("GameWuxing Initialize");
        InitializeUI();
    }
}
#endif