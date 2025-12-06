# CloudData系统最佳实践

## 概述

本文档总结了CloudData系统在实际开发中的最佳实践，包括性能优化、错误处理、安全性、可维护性等方面的指导原则。

### ⚠️ 重要概念提醒

在应用最佳实践之前，请确保理解：

- **Player.Id** (`int`)：临时的游戏槽位编号，不适用于云数据 ❌
- **User.UserId** (`long`)：持久的用户标识，云数据的正确选择 ✅

**🚨 参数命名规范：**
- 使用 `long userId` 参数表示 User.UserId
- 使用 `Player player` 参数在游戏逻辑中自动提取 UserId
- 避免使用 `long playerId` 参数名，容易误导为 Player.Id

**本文档示例优先展示便利的 Player 方法，同时明确标注 UserId 用法。**

### 🎯 返回类型重要说明

CloudData API 有两种不同的返回类型，**必须使用正确的检查方式**：

#### ✅ 查询方法返回 `UserCloudDataResult<T>` - 使用 `.IsSuccess`
```csharp
var queryResult = await CloudData.QueryUserDataAsync(userIds, keys);
if (queryResult.IsSuccess) { ... }  // ✅ 正确

var currencyResult = await CloudData.QueryCurrencyAsync(userIds, keys);
if (currencyResult.IsSuccess) { ... }  // ✅ 正确
```

#### ✅ 事务方法返回 `UserCloudDataResult` - 使用 `== Success`
```csharp
var transactionResult = await CloudData.ForUser(userId).ExecuteAsync();
if (transactionResult == UserCloudDataResult.Success) { ... }  // ✅ 正确
// if (transactionResult.IsSuccess) { ... }  // ❌ 编译错误！
```

## 🚀 性能优化

### 1. 批量操作优化

#### ✅ 推荐做法

```csharp
// 批量查询多个用户
var leaderboardData = await CloudData.QueryUserDataAsync(
    userIds: topUserIds,
    keys: ["level", "score", "last_active"]
);

// 批量事务操作
var result = await CloudData.ForUser(userId)
    .SetData("level", newLevel)
    .AddCurrency("gold", goldReward)
    .AddCurrency("experience", expReward)
    .SetData("last_level_up", DateTime.UtcNow.ToString())
    .WithDescription("等级提升奖励")
    .ExecuteAsync();
```

#### ❌ 避免的做法

```csharp
// 避免：循环单个查询
foreach (var userId in topUserIds)
{
    var userData = await CloudData.QueryUserDataAsync(
        [userId], 
        ["level", "score"]
    );
}

// 避免：拆分相关操作
await CloudData.ForUser(userId).SetData("level", newLevel).ExecuteAsync();
await CloudData.ForUser(userId).AddCurrency("gold", goldReward).ExecuteAsync();
await CloudData.ForUser(userId).AddCurrency("experience", expReward).ExecuteAsync();
```

### 2. 查询优化策略

#### 精确查询键值

```csharp
// ✅ 只查询需要的数据
var combatData = await CloudData.QueryUserDataAsync(
    userIds: [userId],
    keys: ["health", "mana", "level"]  // 明确指定需要的键
);

// ❌ 避免查询所有可能的数据
var allData = await CloudData.QueryUserDataAsync(
    userIds: [userId],
    keys: GetAllGameDataKeys()  // 可能包含大量不需要的数据
);
```

#### 合理使用maxCount

```csharp
// ✅ 大列表限制返回数量
var recentItems = await CloudData.QueryUserListItemsAsync(
    userId: userId,
    key: "inventory",
    maxCount: 50  // 避免一次性加载所有物品
);
```

### 3. 事务优化

#### 启用智能合并

```csharp
// ✅ 系统自动优化相同键的操作
var result = await CloudData.ForUser(userId)
    .AddCurrency("gold", 100)
    .AddCurrency("gold", 50)      // 自动合并为 +150
    .AddCurrency("gold", -20)     // 最终结果: +130
    .SetData("level", 10)
    .SetData("level", 11)         // 自动优化为最后一个值 11
    .WithOptimization(true)       // 默认启用
    .ExecuteAsync();
```

#### 操作排序优化

```csharp
// ✅ 推荐的操作顺序：检查 -> 消耗 -> 更新 -> 奖励
var result = await CloudData.ForUser(userId)
    // 1. 先消耗资源（防止重复操作）
    .CostCurrency("energy", actionCost)
    .CostCurrency("gold", upgradeCost)
    
    // 2. 更新状态
    .SetData("weapon_level", newLevel)
    .SetData("last_upgrade", DateTime.UtcNow.ToString())
    
    // 3. 给予奖励
    .AddCurrency("experience", expReward)
    .AddCurrency("prestige", prestigeReward)
    
    .WithDescription("武器升级")
    .ExecuteAsync();
```

### 4. 有上限数据优化

⚠️ **重置机制重要说明**：
- 有上限数据的重置是将**当前值重置为0**
- 对于体力系统：存储的是"已消耗体力"，重置为0意味着体力恢复满值
- 消耗体力时增加数值，UI显示时用：剩余体力 = 最大体力 - 已消耗体力
- 这种设计同样适合任务次数、PVP次数等场景

#### 不同体力系统的实现策略

**1. 定时重置体力系统**（推荐CappedData）
```csharp
// 适合：每日0点恢复满体力的系统
.ModifyCappedData("energy_consumed", 20, 100, UserDataResetOption.Daily())
```

**2. 线性回复体力系统**（CappedData + LastUpdateTime）
```csharp
// 适合：每分钟回复1点体力的系统
public async Task<long> CalculateCurrentEnergy(IUserCappedDataRecord energyData)
{
    var timeSinceUpdate = DateTime.Now - energyData.LastUpdateTime;
    var recoveredEnergy = (long)(timeSinceUpdate.TotalMinutes * RECOVERY_RATE);
    var actualConsumed = Math.Max(0, energyData.Value - recoveredEnergy);
    return Math.Min(energyData.Cap, energyData.Cap - actualConsumed);
}
```

**3. 复杂回复体力系统**（普通数据 + 货币数据）
```csharp
// 适合：VIP加速、道具加速等复杂逻辑
.SetData("current_energy", calculatedEnergy)
.SetData("last_update_time", DateTime.UtcNow.ToString("O"))
```

#### 合理设置重置周期

```csharp
// ✅ 推荐：根据游戏玩法设置合适的重置周期
public static class GameDataResetConfig
{
    // 体力系统 - 每日重置为0（已消耗体力清零，体力恢复满值）
    public static readonly UserDataResetOption DailyEnergy = UserDataResetOption.Daily();
    
    // 每周活跃度 - 每周一重置为0，适合周常活动积分
    public static readonly UserDataResetOption WeeklyActivity = 
        UserDataResetOption.Weekly(1, DayOfWeek.Monday);
    
    // PVP积分 - 每月重置为0，适合赛季系统
    public static readonly UserDataResetOption MonthlyPvP = UserDataResetOption.Monthly();
    
    // 终身成就 - 永不重置
    public static readonly UserDataResetOption LifetimeAchievement = UserDataResetOption.Never;
}

// 使用配置
var result = await ForUser(userId)
    .ModifyCappedData("energy_consumed", 20, 100, GameDataResetConfig.DailyEnergy)    // 体力系统
    .ModifyCappedData("weekly_points", 10, 1000, GameDataResetConfig.WeeklyActivity)
    .ExecuteAsync();
```

#### 批量处理有上限数据

```csharp
// ✅ 推荐：批量查询和操作
public async Task<Dictionary<string, CappedDataInfo>> GetPlayerLimits(long userId)
{
    var result = await QueryCappedDataAsync(
        userIds: [userId],
        keys: ["energy", "weekly_activity", "monthly_pvp", "daily_quests"]
    );
    
    if (!result.IsSuccess) return new Dictionary<string, CappedDataInfo>();
    
    var playerData = result.Data.First();
    return playerData.CappedData.ToDictionary(
        kvp => kvp.Key,
        kvp => new CappedDataInfo 
        { 
            Current = kvp.Value.Value,
            Cap = kvp.Value.Cap,
            NextReset = kvp.Value.NextResetTime
        }
    );
}
```

#### 上限值动态调整策略

```csharp
// ✅ 推荐：基于玩家等级动态调整上限
public async Task AdjustEnergyCapByLevel(long userId, int playerLevel)
{
    // 基础体力100，每10级增加20点上限
    int energyCap = 100 + (playerLevel / 10) * 20;
    
    var result = await ForUser(userId)
        .ModifyCappedData("energy", 0, energyCap)  // 不修改当前值，只调整上限
        .WithDescription($"等级{playerLevel}体力上限调整")
        .ExecuteAsync();
}

// ✅ 推荐：VIP系统的上限加成
public async Task ApplyVipEnergyBonus(long userId, int vipLevel)
{
    int baseEnergyCap = await GetBaseEnergyCap(userId);
    int vipBonus = vipLevel * 10;  // 每级VIP增加10点体力上限
    
    var result = await ForUser(userId)
        .ModifyCappedData("energy", 0, baseEnergyCap + vipBonus)
        .WithDescription($"VIP{vipLevel}体力上限加成")
        .ExecuteAsync();
}
```

## 🛡️ 错误处理与容错

### 1. 分层错误处理

#### 业务层错误处理

```csharp
public async Task<GameActionResult> PerformCombatAction(long userId, CombatAction action)
{
    try
    {
        // 预检查
        var playerData = await CloudData.QueryUserDataAsync(
                    [userId],
        ["health", "mana", "energy"]
        );

        if (!playerData.IsSuccess)
        {
            return GameActionResult.NetworkError("无法获取玩家数据");
        }

        var currentData = playerData.Data.First();
        var health = currentData.BigIntData["health"];
        var mana = currentData.BigIntData["mana"];

        // 业务逻辑验证
        if (health <= 0)
            return GameActionResult.InvalidState("玩家已死亡");
        
        if (mana < action.ManaCost)
            return GameActionResult.InsufficientResources("法力不足");

        // 执行操作
        var result = await CloudData.ForUser(userId)
            .CostCurrency("mana", action.ManaCost)
            .AddToData("damage_dealt", action.Damage)
            .SetData("last_action", action.Type.ToString())
            .WithDescription($"战斗行动: {action.Type}")
            .ExecuteAsync();

        return result == UserCloudDataResult.Success 
            ? GameActionResult.Success(action) 
            : GameActionResult.TransactionFailed(result.ToString());
    }
    catch (Exception ex)
    {
        // 记录详细错误信息
        Game.Logger.LogError(ex, "Combat action failed for player {UserId}", userId);
        return GameActionResult.UnexpectedError("系统错误，请稍后重试");
    }
}
```

#### 基础设施层错误处理

```csharp
public async Task<T> ExecuteWithRetry<T>(
    Func<Task<T>> operation,
    int maxRetries = 3,
    TimeSpan? baseDelay = null) where T : class
{
    baseDelay ??= TimeSpan.FromSeconds(1);
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (IsRetriableError(ex) && attempt < maxRetries)
        {
            var delay = TimeSpan.FromMilliseconds(
                baseDelay.Value.TotalMilliseconds * Math.Pow(2, attempt - 1)
            );
            
            Game.Logger.LogWarning("Attempt {Attempt} failed, retrying in {Delay}: {Message}", attempt, delay, ex.Message);
            await Game.Delay(delay);  // 使用框架推荐的延迟方法
        }
    }
    
    // 最后一次尝试，不捕获异常
    return await operation();
}

private static bool IsRetriableError(Exception ex)
{
    return ex is TimeoutException || 
           ex is HttpRequestException ||
           (ex is InvalidOperationException && ex.Message.Contains("network"));
}
```

### 2. 补偿事务模式

```csharp
public async Task<PurchaseResult> ExecutePurchaseWithCompensation(
    long userId, int itemId, int cost)
{
    var compensationActions = new List<Func<Task>>();
    
    try
    {
        // 1. 扣除货币
        var deductResult = await CloudData.ForUser(userId)
            .CostCurrency("gold", cost)
            .WithDescription($"购买物品 {itemId} - 扣款")
            .ExecuteAsync();

        if (deductResult != UserCloudDataResult.Success)
            return PurchaseResult.InsufficientFunds;

        // 记录补偿操作
        compensationActions.Add(async () =>
        {
            await CloudData.ForUser(userId)
                .AddCurrency("gold", cost)
                .WithDescription($"购买失败退款 - 物品 {itemId}")
                .ExecuteAsync();
        });

        // 2. 添加物品
        var builder = CloudData.ForUser(userId);
        var itemRef = builder.PrepareListItem("inventory", CreateItemData(itemId));
        
        var addItemResult = await builder
            .AddListItem(itemRef)
            .WithDescription($"购买物品 {itemId} - 发放")
            .ExecuteAsync();

        if (addItemResult != UserCloudDataResult.Success)
        {
            // 执行补偿操作
            await ExecuteCompensation(compensationActions);
            return PurchaseResult.ItemDeliveryFailed;
        }

        // 3. 记录购买历史
        var historyResult = await CloudData.ForUser(userId)
            .SetData($"purchase_history_{DateTime.UtcNow:yyyyMMdd}", itemId)
            .SetData("last_purchase", DateTime.UtcNow.ToString())
            .WithDescription($"购买记录 - 物品 {itemId}")
            .ExecuteAsync();

        // 历史记录失败不影响主要流程，但需要记录日志
        if (!historyResult.IsSuccess)
        {
            Game.Logger.LogWarning("Failed to record purchase history for player {UserId}, item {ItemId}", userId, itemId);
        }

        return new PurchaseResult
        {
            Success = true,
            ItemId = itemRef.Id,
            TransactionId = Guid.NewGuid().ToString()
        };
    }
    catch (Exception ex)
    {
        Game.Logger.LogError(ex, "Purchase failed for player {UserId}, item {ItemId}", userId, itemId);
        await ExecuteCompensation(compensationActions);
        return PurchaseResult.SystemError;
    }
}

private async Task ExecuteCompensation(List<Func<Task>> compensationActions)
{
    foreach (var action in compensationActions.AsEnumerable().Reverse())
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "Compensation action failed");
            // 补偿失败需要人工介入
        }
    }
}
```

## 🔒 安全性最佳实践

### 1. 输入验证

```csharp
public static class CloudDataValidator
{
    public static void ValidateUserId(long userId)
    {
        if (userId <= 0)
            throw new ArgumentException("用户ID必须为正数", nameof(userId));
    }
    
    public static void ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("键名不能为空", nameof(key));
            
        if (key.Length > 180)
            throw new ArgumentException("键名长度不能超过180字符", nameof(key));
            
        if (key.Contains(" ") || key.Contains("\t") || key.Contains("\n"))
            throw new ArgumentException("键名不能包含空白字符", nameof(key));
    }
    
    public static void ValidateCurrencyAmount(long amount)
    {
        if (amount < 0)
            throw new ArgumentException("货币数量不能为负数", nameof(amount));
            
        if (amount > long.MaxValue / 2) // 防止溢出
            throw new ArgumentException("货币数量过大", nameof(amount));
    }
}

// 使用示例
public async Task<UserCloudDataResult> SafeAddCurrency(long userId, string currencyType, long amount)
{
    CloudDataValidator.ValidateUserId(userId);
    CloudDataValidator.ValidateKey(currencyType);
    CloudDataValidator.ValidateCurrencyAmount(amount);
    
    return await CloudData.ForUser(userId)
        .AddCurrency(currencyType, amount)
        .ExecuteAsync();
}
```

### 2. 权限检查

```csharp
public class PlayerActionAuthorizer
{
    public async Task<bool> CanPerformAction(long userId, string actionType, object actionData)
    {
        // 获取玩家当前状态
        var playerData = await CloudData.QueryUserDataAsync(
                    [userId],
        ["status", "ban_until", "level", "vip_level"]
        );

        if (!playerData.IsSuccess)
            return false;

        var data = playerData.Data.First();
        
        // 检查封禁状态
        if (data.VarChar255Data.TryGetValue("ban_until", out var banUntil))
        {
            if (DateTime.TryParse(banUntil, out var banDate) && banDate > DateTime.UtcNow)
                return false;
        }
        
        // 检查玩家状态
        if (data.VarChar255Data.TryGetValue("status", out var status) && status == "suspended")
            return false;
            
        // 根据行动类型检查权限
        return actionType switch
        {
            "admin_command" => await IsAdmin(userId),
            "vip_action" => data.BigIntData.GetValueOrDefault("vip_level", 0) > 0,
            "high_level_action" => data.BigIntData.GetValueOrDefault("level", 0) >= 10,
            _ => true
        };
    }
    
    private async Task<bool> IsAdmin(long userId)
    {
        var adminData = await CloudData.QueryUserDataAsync(
                    [userId],
        ["admin_level"]
        );
        
        return adminData.IsSuccess && 
               adminData.Data.First().BigIntData.GetValueOrDefault("admin_level", 0) > 0;
    }
}
```

### 3. 敏感数据处理

```csharp
#if SERVER  // 敏感操作只在服务端执行
public class SecureCloudDataOperations
{
    public async Task<UserCloudDataResult> SecureTransferCurrency(
        long fromUserId, 
        long toUserId, 
        string currencyType, 
        long amount,
        string reason)
    {
        // 验证参数
        if (fromUserId == toUserId)
            throw new ArgumentException("不能向自己转账");
            
        if (amount <= 0)
            throw new ArgumentException("转账金额必须大于0");

        // 检查发送方余额
        var fromPlayerData = await CloudData.QueryCurrencyAsync(
                    [fromUserId],
        [currencyType]
        );

        if (!fromPlayerData.IsSuccess)
            return UserCloudDataResult.FailedToSend;

        var currentBalance = fromPlayerData.Data.First().CurrencyData.GetValueOrDefault(currencyType, 0);
        if (currentBalance < amount)
            return UserCloudDataResult.InsufficientFunds;

        // 记录转账日志
        var transferId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        
        // 执行转账（使用分布式事务思路）
        var fromResult = await CloudData.ForUser(fromUserId)
            .CostCurrency(currencyType, amount)
            .SetData($"transfer_out_{transferId}", $"{toUserId}:{amount}:{timestamp}")
            .WithDescription($"转出{currencyType}给玩家{toUserId}: {reason}")
            .ExecuteAsync();

        if (fromResult != UserCloudDataResult.Success)
            return fromResult;

        var toResult = await CloudData.ForUser(toUserId)
            .AddCurrency(currencyType, amount)
            .SetData($"transfer_in_{transferId}", $"{fromUserId}:{amount}:{timestamp}")
            .WithDescription($"从玩家{fromUserId}收到{currencyType}: {reason}")
            .ExecuteAsync();

        if (toResult != UserCloudDataResult.Success)
        {
            // 补偿：退还发送方的货币
            await CloudData.ForUser(fromUserId)
                .AddCurrency(currencyType, amount)
                .WithDescription($"转账失败退款 - {transferId}")
                .ExecuteAsync();
                
            return UserCloudDataResult.TransactionFailed;
        }

        return UserCloudDataResult.Success;
    }
}
#endif
```

## 📊 监控与日志

### 1. 操作日志记录

```csharp
public class CloudDataLogger
{
    public static async Task<UserCloudDataResult> LoggedExecute(
        TransactionBuilder builder,
        string operationName,
        long userId,
        Dictionary<string, object> context = null)
    {
        var startTime = DateTime.UtcNow;
        var operations = builder.Build();
        
        Game.Logger.LogDebug("[CloudData] Starting {OperationName} for player {UserId}", operationName, userId);
        Game.Logger.LogDebug("[CloudData] Operations count: {Count}", operations.Count());
        
        if (context != null)
        {
            foreach (var kvp in context)
            {
                Game.Logger.LogDebug("[CloudData] Context {Key}: {Value}", kvp.Key, kvp.Value);
            }
        }

        try
        {
            var result = await builder.ExecuteAsync();
            var duration = DateTime.UtcNow - startTime;
            
            Game.Logger.LogDebug("[CloudData] {OperationName} completed in {Duration}ms", operationName, duration.TotalMilliseconds);
            Game.Logger.LogDebug("[CloudData] Result: {Result}", result);
            
            // 记录性能指标
            if (duration.TotalSeconds > 5) // 超过5秒的操作需要关注
            {
                Game.Logger.LogWarning("[CloudData] SLOW OPERATION: {OperationName} took {Duration}s", operationName, duration.TotalSeconds);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Game.Logger.LogError(ex, "[CloudData] {OperationName} failed after {Duration}ms", operationName, duration.TotalMilliseconds);
            throw;
        }
    }
}

// 使用示例
var result = await CloudDataLogger.LoggedExecute(
    CloudData.ForUser(userId)
        .AddCurrency("gold", reward)
        .SetData("last_quest", questId),
    "CompleteQuest",
    userId,
    new Dictionary<string, object>
    {
        ["questId"] = questId,
        ["reward"] = reward,
        ["questType"] = quest.Type
    }
);
```

### 2. 性能监控

```csharp
public class CloudDataMetrics
{
    private static readonly Dictionary<string, List<TimeSpan>> _operationTimes = new();
    private static readonly Dictionary<string, int> _operationCounts = new();
    private static readonly object _lock = new();

    public static void RecordOperation(string operationType, TimeSpan duration, bool success)
    {
        lock (_lock)
        {
            if (!_operationTimes.ContainsKey(operationType))
            {
                _operationTimes[operationType] = new List<TimeSpan>();
                _operationCounts[operationType] = 0;
            }
            
            _operationTimes[operationType].Add(duration);
            _operationCounts[operationType]++;
            
            // 记录失败
            var resultKey = $"{operationType}_{(success ? "success" : "failure")}";
            _operationCounts[resultKey] = _operationCounts.GetValueOrDefault(resultKey, 0) + 1;
        }
    }

    public static void PrintStatistics()
    {
        lock (_lock)
        {
            Game.Logger.LogInformation("=== CloudData性能统计 ===");
            
            foreach (var kvp in _operationTimes)
            {
                var operationType = kvp.Key;
                var times = kvp.Value;
                
                if (times.Count == 0) continue;
                
                var avgMs = times.Average(t => t.TotalMilliseconds);
                var maxMs = times.Max(t => t.TotalMilliseconds);
                var minMs = times.Min(t => t.TotalMilliseconds);
                
                var successCount = _operationCounts.GetValueOrDefault($"{operationType}_success", 0);
                var failureCount = _operationCounts.GetValueOrDefault($"{operationType}_failure", 0);
                var successRate = (double)successCount / (successCount + failureCount) * 100;
                
                Game.Logger.LogInformation("{OperationType}:", operationType);
                Game.Logger.LogInformation("  次数: {Count}, 成功率: {SuccessRate:F1}%", times.Count, successRate);
                Game.Logger.LogInformation("  平均: {AvgMs:F1}ms, 最大: {MaxMs:F1}ms, 最小: {MinMs:F1}ms", avgMs, maxMs, minMs);
            }
        }
    }
}

// 集成到操作中
public static async Task<UserCloudDataResult> ExecuteWithMetrics(
    this TransactionBuilder builder, 
    string operationType)
{
    var startTime = DateTime.UtcNow;
    
    try
    {
        var result = await builder.ExecuteAsync();
        var duration = DateTime.UtcNow - startTime;
        
        CloudDataMetrics.RecordOperation(operationType, duration, result == UserCloudDataResult.Success);
        return result;
    }
    catch (Exception)
    {
        var duration = DateTime.UtcNow - startTime;
        CloudDataMetrics.RecordOperation(operationType, duration, false);
        throw;
    }
}
```

## 🧪 测试最佳实践

### 1. 单元测试模式

```csharp
public class CloudDataTestHelper
{
    public static TransactionBuilder CreateMockBuilder(long userId)
    {
        // 在测试环境中，可以创建一个不实际执行的构建器
        return new MockTransactionBuilder(userId);
    }
    
    public static void AssertTransactionContains(
        TransactionBuilder builder, 
        TransactionOperationType operationType,
        string key,
        object expectedValue)
    {
        var operations = builder.Build();
        var matchingOp = operations.FirstOrDefault(op => 
            op.Type == operationType && 
            op.Key.ToString() == key);
            
        if (matchingOp == null)
        {
            throw new AssertionException($"Transaction does not contain {operationType} operation for key '{key}'");
        }
        
        // 根据操作类型验证值
        var actualValue = operationType switch
        {
            TransactionOperationType.SetBigInt => matchingOp.BigIntValue,
            TransactionOperationType.AddCurrency => matchingOp.CurrencyValue,
            TransactionOperationType.SetVarChar255 => matchingOp.VarChar255Value,
            _ => throw new ArgumentException($"Unsupported operation type: {operationType}")
        };
        
        if (!actualValue.Equals(expectedValue))
        {
            throw new AssertionException($"Expected {expectedValue}, but got {actualValue}");
        }
    }
}

// 测试示例
[Test]
public void CompleteQuest_ShouldBuildCorrectTransaction()
{
    // Arrange
    var userId = 12345L;
    var quest = new Quest { ExpReward = 1000, GoldReward = 500 };
    
    // Act
    var builder = CloudData.ForUser(userId)
        .AddCurrency("experience", quest.ExpReward)
        .AddCurrency("gold", quest.GoldReward)
        .SetData("last_quest", quest.Id);
    
    // Assert
    CloudDataTestHelper.AssertTransactionContains(
        builder, TransactionOperationType.AddCurrency, "experience", 1000L);
    CloudDataTestHelper.AssertTransactionContains(
        builder, TransactionOperationType.AddCurrency, "gold", 500L);
    CloudDataTestHelper.AssertTransactionContains(
        builder, TransactionOperationType.SetBigInt, "last_quest", quest.Id);
}
```

### 2. 集成测试策略

```csharp
public class CloudDataIntegrationTest
{
    private static long _testUserId = 999999L; // 专用测试用户ID
    
    [SetUp]
    public async Task Setup()
    {
        // 清理测试数据
        await CloudData.ForUser(_testUserId)
            .SetData("level", 1)
            .SetData("experience", 0)
            .SetCurrency("gold", 1000)
            .SetCurrency("energy", 100)
            .WithDescription("测试初始化")
            .ExecuteAsync();
    }
    
    [Test]
    public async Task FullGameFlow_ShouldWorkCorrectly()
    {
        // 1. 完成战斗
        var combatResult = await CloudData.ForUser(_testUserId)
            .CostCurrency("energy", 10)
            .AddCurrency("experience", 100)
            .AddCurrency("gold", 50)
            .WithDescription("战斗胜利")
            .ExecuteAsync();
            
        Assert.IsTrue(combatResult == UserCloudDataResult.Success);
        
        // 2. 验证数据变化
        var userData = await CloudData.QueryUserDataAsync(
                    [_testUserId],
        ["experience"]
        );
        
        Assert.IsTrue(userData.IsSuccess);
        Assert.AreEqual(100, userData.Data.First().BigIntData["experience"]);
        
        // 3. 购买物品
        var builder = CloudData.ForUser(_testUserId);
        var itemRef = builder.PrepareListItem("inventory", new { type = "sword", level = 1 });
        
        var purchaseResult = await builder
            .CostCurrency("gold", 100)
            .AddListItem(itemRef)
            .WithDescription("购买武器")
            .ExecuteAsync();
            
        Assert.IsTrue(purchaseResult == UserCloudDataResult.Success);
        Assert.IsTrue(itemRef.Id > 0);
        
        // 4. 验证物品已添加
        var inventory = await CloudData.QueryUserListItemsAsync(
            userId: _testUserId,
            key: "inventory"
        );
        
        Assert.IsTrue(inventory.IsSuccess);
        Assert.IsTrue(inventory.Data.Any(item => item.ItemUuid == itemRef.Id));
    }
}
```

## 📋 代码规范

### 1. 命名约定

```csharp
// ✅ 推荐的键名规范
public static class CloudDataKeys
{
    // 基础属性
    public const string Level = "level";
    public const string Experience = "experience";
    public const string LastLogin = "last_login";
    
    // 货币类型
    public const string Gold = "gold";
    public const string Diamond = "diamond";
    
    // 有上限数据（重置为0机制）
    public const string DailyQuestAttempts = "daily_quest_attempts";  // 每日任务次数（重置为0）
    public const string WeeklyActivity = "weekly_activity";           // 每周活跃度（重置为0）
    public const string DailyQuests = "daily_quests";                // 每日任务进度（重置为0）
    public const string MonthlyPvP = "monthly_pvp_score";            // 月度PVP积分（重置为0）
    public const string DungeonAttempts = "dungeon_attempts";         // 副本挑战次数（重置为0）
    
    // 列表键
    public const string Inventory = "inventory";
    public const string Friends = "friends";
    public const string Achievements = "achievements";
    
    // 任务相关
    public static string QuestCompleted(int questId) => $"quest_{questId}_completed";
    public static string QuestProgress(int questId) => $"quest_{questId}_progress";
    
    // 时间戳
    public static string LastAction(string actionType) => $"last_{actionType}";
}

// 使用示例
var result = await CloudData.ForUser(userId)
    .SetData(CloudDataKeys.Level, newLevel)
    .AddCurrency(CloudDataKeys.Gold, reward)
    .ModifyCappedData(CloudDataKeys.DailyQuestAttempts, 1, 10, UserDataResetOption.Daily())
    .ModifyCappedData(CloudDataKeys.WeeklyActivity, 5, 1000, UserDataResetOption.Weekly())
    .SetData(CloudDataKeys.QuestCompleted(questId), true)
    .ExecuteAsync();
```

### 2. 事务描述规范

```csharp
public static class TransactionDescriptions
{
    public static string Combat(string result) => $"战斗{result}";
    public static string Quest(int questId, string questName) => $"完成任务 {questId}:{questName}";
    public static string Purchase(string itemName, int cost) => $"购买{itemName} (花费{cost})";
    public static string LevelUp(int fromLevel, int toLevel) => $"等级提升 {fromLevel}→{toLevel}";
    public static string DailyReward(DateTime date) => $"每日奖励 {date:yyyy-MM-dd}";
    public static string SystemCorrection(string reason) => $"系统修正: {reason}";
}
```

### 3. 扩展方法规范

```csharp
public static class CloudDataExtensions
{
    /// <summary>
    /// 安全地添加经验值并检查升级
    /// </summary>
    public static TransactionBuilder AddExperienceWithLevelCheck(
        this TransactionBuilder builder,
        long experience,
        Func<long, int> calculateLevel)
    {
        // 这里可以添加升级逻辑
        return builder.AddCurrency("experience", experience);
    }
    
    /// <summary>
    /// 批量设置玩家状态
    /// </summary>
    public static TransactionBuilder SetPlayerStatus(
        this TransactionBuilder builder,
        PlayerStatus status)
    {
        return builder
            .SetData("status", status.ToString())
            .SetData("status_updated", DateTime.UtcNow.ToString())
            .SetData("last_active", DateTime.UtcNow.ToString());
    }
    
    /// <summary>
    /// 记录玩家行为
    /// </summary>
    public static TransactionBuilder LogPlayerAction(
        this TransactionBuilder builder,
        string actionType,
        Dictionary<string, object> actionData = null)
    {
        builder = builder.SetData($"action_last_{actionType}", DateTime.UtcNow.ToString());
        
        if (actionData != null)
        {
            foreach (var kvp in actionData)
            {
                builder = builder.SetData($"action_{actionType}_{kvp.Key}", kvp.Value);
            }
        }
        
        return builder;
    }
}

// 使用示例
var result = await CloudData.ForUser(userId)
    .AddExperienceWithLevelCheck(1000, exp => CalculatePlayerLevel(exp))
    .SetPlayerStatus(PlayerStatus.Online)
    .LogPlayerAction("quest_complete", new Dictionary<string, object>
    {
        ["quest_id"] = questId,
        ["reward_gold"] = goldReward
    })
    .WithDescription(TransactionDescriptions.Quest(questId, questName))
    .ExecuteAsync();
```

## 📋 API选择最佳实践

### 何时使用 Player 便利方法 vs UserId

#### ✅ 推荐使用 Player 方法的场景

```csharp
// 1. 游戏逻辑中直接操作玩家
public async Task OnPlayerLevelUp(Player player, int newLevel)
{
    var result = await CloudData.ForPlayer(player)
        .SetData("level", newLevel)
        .AddCurrency("gold", 100)
        .WithDescription($"升级到 {newLevel} 级")
        .ExecuteAsync();
}

// 2. 事件处理中的玩家操作
public void OnPlayerKill(Player killer, Player victim)
{
    // 使用 Player 对象更直观
    _ = CloudData.ForPlayer(killer)
        .AddCurrency("pvp_points", 10)
        .ExecuteAsync();
}

// 3. 批量玩家操作
public async Task DistributeRewards(Player[] winners)
{
    var result = await CloudData.ForPlayers(winners)
        .ForAllUsers(b => b.AddCurrency("tournament_reward", 500))
        .ExecuteAllAsync();
}
```

#### ✅ 推荐使用 UserId 的场景

```csharp
// 1. 存储的用户ID列表操作
public async Task ProcessOfflineRewards(long[] userIds)
{
    var result = await CloudData.ForUsers(userIds)
        .ForAllUsers(b => b.AddCurrency("offline_reward", 50))
        .ExecuteAllAsync();
}

// 2. 跨会话的用户数据查询
public async Task<PlayerProfile> LoadUserProfile(long userId)
{
    var result = await CloudData.QueryUserDataAsync(
        userIds: [userId],
        keys: ["level", "experience", "last_login"]
    );
    // ...
}

// 3. 系统管理操作
public async Task SystemCorrection(long userId, string reason)
{
    var result = await CloudData.ForUser(userId)
        .AddCurrency("gold", 1000)
        .WithDescription($"系统补偿: {reason}")
        .ExecuteAsync();
}
```

#### ⚠️ 注意事项

```csharp
// ❌ 错误：直接使用 Player.Id 
public async Task WrongExample(Player player)
{
    // 这是错误的！Player.Id 不是持久的用户标识
    var result = await CloudData.ForUser(player.Id)  // ❌
        .SetData("level", 10)
        .ExecuteAsync();
}

// ✅ 正确：使用便利方法或手动提取 UserId
public async Task CorrectExample(Player player)
{
    // 方式1: 使用便利方法（推荐）
    var result = await CloudData.ForPlayer(player)  // ✅
        .SetData("level", 10)
        .ExecuteAsync();
    
    // 方式2: 手动提取 UserId
    if (player.SlotController is PlayerController controller)
    {
        var userId = controller.User.UserId;
        var result2 = await CloudData.ForUser(userId)  // ✅
            .SetData("level", 10)
            .ExecuteAsync();
    }
}
```

## 总结

CloudData系统的最佳实践围绕以下核心原则：

1. **性能优先** - 批量操作、精确查询、智能优化
2. **安全第一** - 输入验证、权限检查、敏感数据保护
3. **容错设计** - 分层错误处理、补偿事务、重试机制
4. **可观测性** - 详细日志、性能监控、调试支持
5. **可测试性** - 单元测试友好、集成测试策略
6. **代码质量** - 命名规范、结构清晰、可维护性

遵循这些最佳实践，可以确保CloudData系统在生产环境中的稳定性、性能和可维护性。 