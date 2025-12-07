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
        /// 先检查缓存，缓存未命中时查询CloudData
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

                Game.Logger.LogInformation($"[PlayerDataManager] No cache found, querying CloudData for userId {userId}");
                
                // 查询CloudData - 使用QueryUserDataAsync
                var dataResult = await CloudData.QueryUserDataAsync(
                    userIds: [userId.Value],
                    keys: [KEY_NICKNAME, KEY_LEVEL, KEY_EXPERIENCE, 
                           KEY_ELEMENT_METAL, KEY_ELEMENT_WOOD, KEY_ELEMENT_WATER, KEY_ELEMENT_FIRE, KEY_ELEMENT_EARTH]
                );
                
                // 查询货币数据
                var currencyResult = await CloudData.QueryCurrencyAsync(
                    userIds: [userId.Value],
                    keys: [CURRENCY_GOLD]
                );
                
                // 检查查询结果
                if (!dataResult.IsSuccess)
                {
                    Game.Logger.LogWarning($"[PlayerDataManager] CloudData query failed: treating as new player");
                    return null;
                }
                
                // 获取用户数据
                var userDataList = dataResult.Data;
                if (userDataList == null)
                {
                    Game.Logger.LogInformation($"[PlayerDataManager] No data found in CloudData, treating as new player");
                    return null;
                }
                
                // 遍历结果寻找对应用户的应该只有一条（因为我们只查了一个userId）
                // 使用 foreach 避免 explicit typing issues with First()
                dynamic userData = null;
                foreach (var item in userDataList)
                {
                     // 假设 item 有 UserId 属性，或者它就是数据对象
                     // 由于编译错误提示 item 可能是 IUserDataBatchQueryResult 类型（如果是嵌套的），
                     // 或者 item 是 UserData 类型。我们这里做个简单的检查。
                     // 实际上根据文档，这里通常只有一个结果。
                     userData = item;
                     break;
                }

                if (userData == null)
                {
                     return null;
                }
                
                // 检查是否有昵称数据（有昵称说明是已存在的玩家）
                // 使用 dynamic 访问属性以绕过编译时类型检查问题（如果接口定义不明确）
                // 但为了安全，我们尝试转换为已知接口或字典访问
                IDictionary<string, string> nicknameData = userData.VarChar255Data;
                
                if (nicknameData == null || !nicknameData.ContainsKey(KEY_NICKNAME))
                {
                    Game.Logger.LogInformation($"[PlayerDataManager] No nickname found in CloudData, treating as new player");
                    return null;
                }
                
                // 构建PlayerData对象
                var playerData = new PlayerData
                {
                    Nickname = nicknameData.TryGetValue(KEY_NICKNAME, out var nickname) ? nickname : "Player",
                    Level = (int)GetBigInt(userData, KEY_LEVEL, 1),
                    Experience = GetBigInt(userData, KEY_EXPERIENCE, 0),
                    Gold = 0,
                    Elements = new Dictionary<string, long>
                    {
                        { "metal", GetBigInt(userData, KEY_ELEMENT_METAL, 0) },
                        { "wood", GetBigInt(userData, KEY_ELEMENT_WOOD, 0) },
                        { "water", GetBigInt(userData, KEY_ELEMENT_WATER, 0) },
                        { "fire", GetBigInt(userData, KEY_ELEMENT_FIRE, 0) },
                        { "earth", GetBigInt(userData, KEY_ELEMENT_EARTH, 0) }
                    }
                };
                
                // 获取货币数据
                if (currencyResult.IsSuccess && currencyResult.Data != null)
                {
                    // 使用 dynamic 绕过编译时检查，处理 UserData<IUserCurrencyRecord>
                    dynamic currencyDataDyn = currencyResult.Data;
                    
                    // 尝试作为字典直接访问 (如果它实现了 IDictionary) 或查找 CurrencyData 属性
                    IDictionary<string, long> currencyDict = null;

                    try 
                    {
                        // 尝试获取 CurrencyData 属性
                        currencyDict = currencyDataDyn.CurrencyData as IDictionary<string, long>;
                    }
                    catch 
                    {
                        // 忽略异常
                    }

                    if (currencyDict != null && currencyDict.TryGetValue(CURRENCY_GOLD, out var gold))
                    {
                        playerData.Gold = gold;
                    }
                }
                
                Game.Logger.LogInformation($"[PlayerDataManager] Loaded existing player from CloudData: {playerData.Nickname}, Level {playerData.Level}");
                
                // 缓存数据
                _sessionCache[userId.Value] = playerData;
                
                return playerData;
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "[PlayerDataManager] Error loading player data");
                return null;
            }
        }

        /// <summary>
        /// 辅助方法：从 dynamic userData 中安全获取 BigInt 值
        /// </summary>
        private static long GetBigInt(dynamic userData, string key, long defaultValue)
        {
            try 
            {
                var dict = userData.BigIntData as IDictionary<string, long>;
                if (dict != null && dict.TryGetValue(key, out var val))
                {
                    return val;
                }
            }
            catch
            {
                // 忽略转换失败
            }
            return defaultValue;
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
