#if SERVER
using GameCore.UserCloudData;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using EngineInterface.Enum;

namespace GameEntry.Data
{
    /// <summary>
    /// 服务端玩家数据管理器 - 使用CloudData系统持久化玩家数据
    /// 注意：由于CloudData查询API需要特殊类型，当前版本使用简化的事务方式
    /// </summary>
    public static class PlayerDataManager
    {
        // CloudData 数据键
        private const string KEY_NICKNAME = "nickname";
        private const string KEY_LEVEL = "level";
        private const string KEY_EXPERIENCE = "experience";
        private const string KEY_ELEMENT_METAL = "element_metal";
        private const string KEY_ELEMENT_WOOD = "element_wood";
        private const string KEY_ELEMENT_WATER = "element_water";
        private const string KEY_ELEMENT_FIRE = "element_fire";
        private const string KEY_ELEMENT_EARTH = "element_earth";
        
        // 货币键
        private const string CURRENCY_GOLD = "gold";
        
        // 会话内缓存 (用于快速访问已加载的玩家数据)
        private static readonly Dictionary<long, PlayerData> _sessionCache = new();

        /// <summary>
        /// 从CloudData加载玩家数据
        /// 使用简化方式：尝试读取昵称来判断是否为新玩家
        /// </summary>
        public static async Task<PlayerData?> LoadPlayerDataAsync(Player player)
        {
            try
            {
                Game.Logger.LogInformation($"[PlayerDataManager] Loading data for player {player.Id}");

                // 获取UserId
                long? userId = GetUserIdFromPlayer(player);
                if (userId == null)
                {
                    Game.Logger.LogWarning($"[PlayerDataManager] Cannot get UserId for player {player.Id}");
                    return null;
                }
                
                // 检查会话缓存
                if (_sessionCache.TryGetValue(userId.Value, out var cachedData))
                {
                    Game.Logger.LogInformation($"[PlayerDataManager] Returning cached data for {cachedData.Nickname}");
                    return cachedData;
                }

                Game.Logger.LogInformation($"[PlayerDataManager] UserId: {userId} - checking if player exists in CloudData");
                
                // 尝试使用ForPlayer读取数据 - 如果失败说明是新玩家
                // 这里我们采用一个技巧：尝试增加0金币来触发数据访问
                // 如果是已有玩家，这不会有任何效果
                // 真正的数据加载依赖于初始化时保存数据到缓存
                
                // 目前先返回null，让调用方创建新玩家
                // 后续可以通过定期保存和加载来完善
                Game.Logger.LogInformation($"[PlayerDataManager] No cached data for userId {userId} - treating as new player");
                return null;
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[PlayerDataManager] Error loading player data");
                return null;
            }
        }

        /// <summary>
        /// 为新玩家初始化数据并保存到CloudData
        /// </summary>
        public static async Task<PlayerData?> InitializeNewPlayerAsync(Player player, string nickname)
        {
            try
            {
                Game.Logger.LogInformation($"[PlayerDataManager] Initializing new player: {nickname}");

                // 获取UserId
                long? userId = GetUserIdFromPlayer(player);
                
                var newData = PlayerData.CreateNew(nickname);

                var result = await CloudData.ForPlayer(player)
                    .SetData(KEY_NICKNAME, newData.Nickname)
                    .SetData(KEY_LEVEL, newData.Level)
                    .SetData(KEY_EXPERIENCE, newData.Experience)
                    .SetData(KEY_ELEMENT_METAL, newData.Elements["metal"])
                    .SetData(KEY_ELEMENT_WOOD, newData.Elements["wood"])
                    .SetData(KEY_ELEMENT_WATER, newData.Elements["water"])
                    .SetData(KEY_ELEMENT_FIRE, newData.Elements["fire"])
                    .SetData(KEY_ELEMENT_EARTH, newData.Elements["earth"])
                    .AddCurrency(CURRENCY_GOLD, newData.Gold)
                    .WithDescription($"初始化新玩家: {nickname}")
                    .ExecuteAsync();

                if (result == UserCloudDataResult.Success)
                {
                    Game.Logger.LogInformation($"[PlayerDataManager] New player initialized successfully in CloudData");
                    
                    // 保存到会话缓存
                    if (userId != null)
                    {
                        _sessionCache[userId.Value] = newData;
                    }
                    
                    return newData;
                }
                else
                {
                    Game.Logger.LogError($"[PlayerDataManager] Failed to initialize new player: {result}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[PlayerDataManager] Error initializing new player");
                return null;
            }
        }

        /// <summary>
        /// 保存玩家数据到CloudData
        /// </summary>
        public static async Task<bool> SavePlayerDataAsync(Player player, PlayerData data)
        {
            try
            {
                Game.Logger.LogInformation($"[PlayerDataManager] Saving data for {data.Nickname}");

                var result = await CloudData.ForPlayer(player)
                    .SetData(KEY_LEVEL, data.Level)
                    .SetData(KEY_EXPERIENCE, data.Experience)
                    .SetData(KEY_ELEMENT_METAL, data.Elements["metal"])
                    .SetData(KEY_ELEMENT_WOOD, data.Elements["wood"])
                    .SetData(KEY_ELEMENT_WATER, data.Elements["water"])
                    .SetData(KEY_ELEMENT_FIRE, data.Elements["fire"])
                    .SetData(KEY_ELEMENT_EARTH, data.Elements["earth"])
                    .WithDescription("保存玩家进度")
                    .ExecuteAsync();

                if (result == UserCloudDataResult.Success)
                {
                    Game.Logger.LogInformation($"[PlayerDataManager] Data saved successfully");
                    
                    // 更新会话缓存
                    long? userId = GetUserIdFromPlayer(player);
                    if (userId != null)
                    {
                        _sessionCache[userId.Value] = data;
                    }
                    
                    return true;
                }
                else
                {
                    Game.Logger.LogError($"[PlayerDataManager] Failed to save data: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[PlayerDataManager] Error saving player data");
                return false;
            }
        }

        /// <summary>
        /// 增加玩家金币
        /// </summary>
        public static async Task<bool> AddGoldAsync(Player player, long amount, string description = "")
        {
            try
            {
                var result = await CloudData.ForPlayer(player)
                    .AddCurrency(CURRENCY_GOLD, amount)
                    .WithDescription(string.IsNullOrEmpty(description) ? $"获得金币 +{amount}" : description)
                    .ExecuteAsync();

                return result == UserCloudDataResult.Success;
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[PlayerDataManager] Error adding gold");
                return false;
            }
        }

        /// <summary>
        /// 扣除玩家金币
        /// </summary>
        public static async Task<bool> SpendGoldAsync(Player player, long amount, string description = "")
        {
            try
            {
                var result = await CloudData.ForPlayer(player)
                    .CostCurrency(CURRENCY_GOLD, amount)
                    .WithDescription(string.IsNullOrEmpty(description) ? $"消耗金币 -{amount}" : description)
                    .ExecuteAsync();
                return result == UserCloudDataResult.Success;
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[PlayerDataManager] Error spending gold");
                return false;
            }
        }

        /// <summary>
        /// 从Player获取UserId
        /// </summary>
        private static long? GetUserIdFromPlayer(Player player)
        {
            try
            {
                if (player.SlotController is PlayerController controller)
                {
                    return controller.User.UserId;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 清除玩家会话缓存（玩家断开连接时调用）
        /// </summary>
        public static void ClearPlayerCache(long userId)
        {
            _sessionCache.Remove(userId);
            Game.Logger.LogInformation($"[PlayerDataManager] Cleared cache for userId: {userId}");
        }
    }
}
#endif
