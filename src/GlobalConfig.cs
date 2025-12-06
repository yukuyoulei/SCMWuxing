using GameCore.GameSystem.Data;

namespace GameEntry;

using static GameCore.ScopeData;
using DataGameMode = GameCore.GameSystem.Data.GameDataGameMode;
public static class GameModes
{
    public static readonly GameLink<DataGameMode, DataGameMode> GameWuxing = new("GameWuxing"u8);
}
public class GlobalConfig : IGameClass
{
    public static void OnRegisterGameClass()
    {
        // Register the game mod for the game system.
        // in non-testing (online) mode, the server will send game mode strings to the engine,
        // and the engine will use this to determine which game mode to use.
        GameDataGlobalConfig.AvailableGameModes = new()
        {
            // 默认游戏模式
            {"GameWuxing", GameModes.GameWuxing},
        };
        _ = new DataGameMode(GameModes.GameWuxing)
        {
            Name = "GameWuxing Mode",
            Gameplay = Gameplay.Default,
            PlayerSettings = GameEntry.ScopeData.GameDataPlayerSettings.PlayerSettings,
            SceneList = [
                   ],
        };
        GameDataGlobalConfig.TestGameMode = GameModes.GameWuxing;
        GameDataGlobalConfig.SinglePlayerTestSlotId = 1;
    }
}
