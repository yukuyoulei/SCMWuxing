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
                
                // 检查会话缓存 (优先使用UserId，其次使用PlayerId)
                if (_sessionCache.TryGetValue(userId.Value, out var cachedData))
                {
                    Game.Logger.LogInformation($"[PlayerDataManager] Returning cached data for {cachedData.Nickname}");
                    return cachedData;
                }
                
                // 也检查PlayerId作为fallback key
                if (_sessionCache.TryGetValue(player.Id, out cachedData))
                {
                    Game.Logger.LogInformation($"[PlayerDataManager] Returning cached data (by PlayerId) for {cachedData.Nickname}");
                    return cachedData;
                }

                Game.Logger.LogInformation($"[PlayerDataManager] UserId: {userId} - no cached data, treating as new player");
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
        /// 手动缓存玩家数据（用于CloudData不可用时的回退）
        /// </summary>
        public static void CachePlayerData(Player player, PlayerData data)
        {
            long? userId = GetUserIdFromPlayer(player);
            if (userId != null)
            {
                _sessionCache[userId.Value] = data;
                Game.Logger.LogInformation($"[PlayerDataManager] Manually cached data for {data.Nickname}");
            }
            else
            {
                // 如果无法获取UserId，使用PlayerId作为临时key
                _sessionCache[player.Id] = data;
                Game.Logger.LogWarning($"[PlayerDataManager] Cached data using PlayerId {player.Id} (fallback)");
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
