# CloudData系统快速指南

## 概述

CloudData系统是WasiCore框架中的用户数据管理核心，提供统一、类型安全、高性能的云数据操作API。本指南将帮助您快速上手CloudData系统的基本使用。

### ⚠️ 重要概念区分

**在开始之前，必须理解这个核心区别：**

- **Player.Id** (`int`)：当局游戏的槽位编号(0,1,2...)，每次游戏会话可能不同 ❌
- **User.UserId** (`long`)：用户的全局唯一标识，持久不变 ✅

**云数据操作必须使用 UserId 或 Player 对象（会自动提取 UserId）**

### 📝 正确的参数类型

```csharp
// ✅ 正确：使用 UserId 参数
public async Task SavePlayerProgress(long userId, int level) 
{
    await CloudData.ForUser(userId).SetData("level", level).ExecuteAsync();
}

// ✅ 正确：使用 Player 对象参数（推荐用于游戏逻辑）
public async Task SavePlayerProgress(Player player, int level) 
{
    await CloudData.ForPlayer(player).SetData("level", level).ExecuteAsync();
}

// ❌ 错误：容易误导的参数名
public async Task SavePlayerProgress(long playerId, int level)  // 看起来像Player.Id！
{
    await CloudData.ForUser(playerId).SetData("level", level).ExecuteAsync();  // 易混淆
}
```

## 5分钟快速开始

### 1. 基础概念

CloudData系统支持以下数据类型：

| 类型 | 用途 | 示例 |
|------|------|------|
| **BigInt** | 整数数据 | 等级、经验、积分 |
| **VarChar255** | 字符串数据 | 昵称、状态 |
| **Currency** | 货币数据 | 金币、钻石 |
| **CappedData** | 有上限数据（可定时重置为0） | 体力、每日活跃度、签到进度 |
| **Blob** | 二进制数据 | 配置、存档 |
| **ListItem** | 列表数据 | 背包、好友 |

### 2. 引入命名空间

```csharp
using GameCore.UserCloudData;
using static GameCore.UserCloudData.CloudData; // 简化调用
```

### 3. 查询数据

```csharp
// 方式1: 使用 UserId（推荐用于存储的用户ID）
var result = await QueryUserDataAsync(
    userIds: [userId],
    keys: ["level", "experience", "gold"]
);

// 方式2: 使用 Player 对象（便利方法，游戏逻辑中常用）
var playerResult = await QueryPlayersDataAsync(
    players: [player],
    keys: ["level", "experience", "gold"]
);

if (playerResult.IsSuccess)
{
    var userData = playerResult.Data.First();
    var level = userData.BigIntData["level"];
    var exp = userData.BigIntData["experience"];
    var gold = userData.CurrencyData["gold"];
}
```

### 4. 基础事务操作

```csharp
// 方式1: 使用 UserId
var result = await ForUser(userId)
    .SetData("level", newLevel)
    .AddToData("experience", 1000)
    .AddCurrency("gold", 500)
    .WithDescription("升级奖励")
    .ExecuteAsync();

// 方式2: 使用 Player 对象（便利方法）
var playerResult = await ForPlayer(player)
    .SetData("level", newLevel)
    .AddToData("experience", 1000)
    .AddCurrency("gold", 500)
    .WithDescription("升级奖励")
    .ExecuteAsync();
```

### 5. 错误处理

```csharp
if (result == UserCloudDataResult.Success)
{
    Game.Logger.LogInformation("操作成功！");
}
else
{
    Game.Logger.LogError("操作失败: {Result}", result);
}
```

## 常见使用场景

### 场景1: 玩家登录

```csharp
public async Task<PlayerLoginInfo> HandlePlayerLogin(long userId)  // 注意：这是UserId，不是Player.Id
{
    // 查询玩家基础信息
    var playerData = await QueryUserDataAsync(
        userIds: [userId],  // userId是User.UserId
        keys: ["level", "experience", "last_login", "total_playtime"]
    );

    // 查询玩家货币
    var currencyData = await QueryCurrencyAsync(
        userIds: [userId],
        keys: ["gold", "diamond", "energy"]
    );

    // 更新登录时间
    var updateResult = await ForUser(userId)
        .SetData("last_login", DateTime.UtcNow.ToString())
        .AddToData("login_count", 1)
        .WithDescription("玩家登录")
        .ExecuteAsync();

    return new PlayerLoginInfo
    {
        Level = playerData.Data.First().BigIntData["level"],
        Experience = playerData.Data.First().BigIntData["experience"],
        Gold = currencyData.Data.First().CurrencyData["gold"],
        // ... 其他数据
    };
}
```

### 场景2: 完成任务

```csharp
public async Task<bool> CompleteQuest(long userId, QuestInfo quest)  // 注意：这是UserId
{
    var builder = ForUser(userId);  // userId是User.UserId，不是Player.Id

    // 添加经验和金币奖励
    builder.AddCurrency("experience", quest.ExpReward)
           .AddCurrency("gold", quest.GoldReward);

    // 添加物品奖励
    if (quest.ItemRewards.Any())
    {
        var itemRefs = builder.PrepareListItems("inventory", quest.ItemRewards);
        builder.AddListItems(itemRefs);
    }

    // 更新任务状态
    builder.SetData($"quest_{quest.Id}_completed", true)
           .SetData($"quest_{quest.Id}_complete_time", DateTime.UtcNow.ToString());

    var result = await builder
        .WithDescription($"完成任务: {quest.Name}")
        .ExecuteAsync();

    return result == UserCloudDataResult.Success;
}
```

### 场景3: 商店购买

```csharp
public async Task<PurchaseResult> PurchaseItem(long userId, int itemId, int cost)  // UserId参数
{
    // 查询玩家当前金币
    var currentGold = await QueryCurrencyAsync(
        userIds: [userId],  // userId是User.UserId
        keys: ["gold"]
    );

    if (!currentGold.IsSuccess)
        return PurchaseResult.NetworkError;

    var goldAmount = currentGold.Data.First().CurrencyData["gold"];
    if (goldAmount < cost)
        return PurchaseResult.InsufficientFunds;

    // 执行购买
    var builder = ForUser(userId);  // 使用UserId
    var itemRef = builder.PrepareListItem("inventory", CreateItemData(itemId));

    var result = await builder
        .CostCurrency("gold", cost)        // 扣除金币
        .AddListItem(itemRef)              // 添加物品
        .SetData("last_purchase", DateTime.UtcNow.ToString())
        .WithDescription($"购买物品 {itemId}")
        .ExecuteAsync();

    if (result == UserCloudDataResult.Success)
    {
        return new PurchaseResult
        {
            Success = true,
            ItemId = itemRef.Id  // 返回生成的物品ID
        };
    }

    return PurchaseResult.TransactionFailed;
}
```

### 场景4: 批量用户操作（每日奖励）

```csharp
public async Task DistributeDailyRewards(long[] userIds)  // 明确是UserId数组
{
    var tasks = userIds.Select(async userId =>
    {
        return await ForUser(userId)  // 使用UserId
            .AddCurrency("gold", 100)
            .AddCurrency("energy", 20)
            .SetData("last_daily_reward", DateTime.UtcNow.ToString())
            .WithDescription("每日登录奖励")
            .ExecuteAsync();
    });

    var results = await Task.WhenAll(tasks);
    
    // 统计成功和失败
    var successCount = results.Count(r => r == UserCloudDataResult.Success);
    Game.Logger.LogInformation("每日奖励发放: {SuccessCount}/{TotalCount} 成功", successCount, results.Length);
}
```

### 场景5: 体力系统管理

💡 **体力系统有多种实现方式**：
- **定时重置**：每日0点恢复满体力 → 使用CappedData重置机制
- **线性回复**：每分钟回复1点体力 → 使用CappedData + LastUpdateTime
- **复杂逻辑**：VIP加速、道具加速 → 使用普通数据 + 货币数据

```csharp
public async Task<EnergySystemResult> ManagePlayerEnergy(long userId, EnergyAction action, int amount = 0)  // UserId参数
{
    switch (action)
    {
        case EnergyAction.Consume:
            // 消耗体力：增加已消耗量，每日0点重置为0（体力恢复满值），上限100
            var consumeResult = await ForUser(userId)  // 使用UserId
                .ModifyCappedData("energy_consumed", amount, 100, UserDataResetOption.Daily())
                .WithDescription($"消耗体力 +{amount}")
                .ExecuteAsync();
            return EnergySystemResult.FromCloudResult(consumeResult);

        case EnergyAction.Query:
            // 查询体力状态
            var queryResult = await QueryCappedDataAsync([userId], ["energy_consumed"]);
            if (queryResult.IsSuccess)
            {
                var userData = queryResult.Data.First();
                var energyConsumed = userData.CappedData["energy_consumed"].Value;
                var maxEnergy = userData.CappedData["energy_consumed"].Cap;
                var remainingEnergy = maxEnergy - energyConsumed;  // 计算剩余体力
                return new EnergySystemResult { Success = true, RemainingEnergy = (int)remainingEnergy };
            }
            return EnergySystemResult.FromCloudResult(queryResult);

        case EnergyAction.UpgradeCapacity:
            // 提升体力上限（VIP特权）
            var upgradeResult = await ForUser(userId)
                .ModifyCappedData("energy_consumed", 0, 100 + amount)  // 新上限
                .WithDescription($"体力上限提升至 {100 + amount}")
                .ExecuteAsync();
            return EnergySystemResult.FromCloudResult(upgradeResult);

        case EnergyAction.Query:
        default:
            // 查询当前体力状态
            var queryResult = await QueryCappedDataAsync(
                userIds: [userId],  // 使用UserId
                keys: ["energy_consumed"]
            );
            
            if (queryResult.IsSuccess && queryResult.Data.Any())
            {
                var energyData = queryResult.Data.First();
                var energyConsumed = energyData.CappedData["energy_consumed"];
                var remainingEnergy = energyConsumed.Cap - energyConsumed.Value;  // 剩余体力
                return new EnergySystemResult
                {
                    Success = true,
                    CurrentEnergy = remainingEnergy,  // 剩余体力
                    MaxEnergy = energyConsumed.Cap,
                    NextResetTime = energyConsumed.NextResetTime
                };
            }
            return EnergySystemResult.QueryFailed;
    }
}

public enum EnergyAction
{
    Query,      // 查询当前状态
    Restore,    // 恢复体力  
    Consume,    // 消耗体力
    UpgradeCapacity  // 提升上限
}
```

### 场景6: 在游戏逻辑中正确使用Player对象

```csharp
// ✅ 推荐：在游戏事件处理中使用Player对象
public class GameEventHandler 
{
    // 玩家升级时保存数据
    public async Task OnPlayerLevelUp(Player player, int newLevel)
    {
        // 使用Player便利方法，自动提取User.UserId
        var result = await ForPlayer(player)
            .SetData("level", newLevel)
            .AddCurrency("skill_points", 1)
            .WithDescription($"升级到{newLevel}级")
            .ExecuteAsync();
    }
    
    // 玩家获得物品
    public async Task OnPlayerGetItem(Player player, ItemData item)
    {
        var builder = ForPlayer(player);  // 推荐：直接使用Player
        var itemRef = builder.PrepareListItem("inventory", item);
        
        await builder
            .AddListItem(itemRef)
            .SetData("last_item_time", DateTime.UtcNow.ToString())
            .ExecuteAsync();
    }
    
    // 从外部系统传入UserId的情况
    public async Task LoadUserProfile(long userId)  // 明确标注这是UserId
    {
        var userData = await QueryUserDataAsync(
            userIds: [userId],  // 这里确实需要UserId
            keys: ["level", "experience", "last_login"]
        );
        // 处理数据...
    }
}

// ✅ 批量玩家操作的正确方式
public async Task DistributeEventRewards(Player[] activePlayers)
{
    // 使用Player数组，自动过滤AI玩家
    var result = await ForPlayers(activePlayers)
        .ForAllUsers(builder => builder
            .AddCurrency("event_token", 10)
            .SetData("last_event_reward", DateTime.UtcNow.ToString())
        )
        .ExecuteAllAsync();
}
```

## 高级特性

### 1. 事务优化

```csharp
// 系统会自动合并相同键的操作
var result = await ForUser(userId)  // 使用UserId
    .AddCurrency("gold", 100)
    .AddCurrency("gold", 50)      // 自动合并为 +150
    .AddCurrency("gold", -20)     // 最终结果: +130
    .SetData("level", 10)
    .SetData("level", 11)         // 自动优化为最后一个值
    .WithOptimization(true)       // 默认启用
    .ExecuteAsync();
```

### 2. 列表项高级操作

```csharp
var builder = ForUser(userId);

// 批量准备物品
var lootItems = GenerateLootRewards();
var itemRefs = builder.PrepareListItems("inventory", lootItems);

// 在事务中使用新物品的ID
var firstItemId = itemRefs[0].Id;  // 在ExecuteAsync之前就能获取ID

var result = await builder
    .AddListItems(itemRefs)
    .SetData("last_loot_item_id", firstItemId)  // 记录最新物品ID
    .ExecuteAsync();
```

### 3. 错误处理最佳实践

```csharp
public async Task<GameResult> SafeExecuteTransaction(
    long userId, 
    Func<TransactionBuilder, TransactionBuilder> buildTransaction)
{
    try
    {
        var builder = ForUser(userId);
        builder = buildTransaction(builder);
        
        var result = await builder.ExecuteAsync();
        
        if (result == UserCloudDataResult.Success)
        {
            return GameResult.Success;
        }

        // 根据错误类型返回不同结果
        return result switch
        {
            UserCloudDataResult.InsufficientFunds => GameResult.InsufficientResources,
            UserCloudDataResult.FailedToSend => GameResult.NetworkError,
            UserCloudDataResult.TransactionCommitEmpty => GameResult.InvalidOperation,
            _ => GameResult.UnknownError
        };
    }
    catch (ArgumentException)
    {
        return GameResult.InvalidArguments;
    }
    catch (InvalidOperationException)
    {
        return GameResult.InvalidOperation;
    }
}

// 使用示例
var result = await SafeExecuteTransaction(userId, builder =>
    builder.AddCurrency("gold", 100)
           .SetData("level", newLevel)
);
```

## 性能最佳实践

### 1. 批量查询优化

```csharp
// ✅ 推荐：一次查询多个用户
var usersData = await QueryUserDataAsync(
    userIds: allUserIds,
    keys: ["level", "experience"]
);

// ❌ 避免：循环单个查询
foreach (var userId in allUserIds)  // 注意：这里是UserId
{
    var userData = await QueryUserDataAsync([userId], keys);
}
```

### 2. 事务合并

```csharp
// ✅ 推荐：一个事务完成所有相关操作
var result = await ForUser(userId)  // 使用UserId
    .CostCurrency("energy", 10)      // 消耗
    .AddCurrency("experience", 100)  // 奖励
    .SetData("last_action", DateTime.UtcNow.ToString())
    .WithDescription("战斗结算")
    .ExecuteAsync();

// ❌ 避免：拆分成多个事务
await ForUser(userId).CostCurrency("energy", 10).ExecuteAsync();
await ForUser(userId).AddCurrency("experience", 100).ExecuteAsync();
```

### 3. 查询范围限制

```csharp
// 大列表使用maxCount限制
var recentItems = await QueryUserListItemsAsync(
    userId: userId,
    key: "inventory",
    maxCount: 50  // 只获取最新50个
);

// 小列表可以不限制
var friends = await QueryUserListItemsAsync(
    userId: userId,
    key: "friends"  // 好友列表通常较小
);
```

## 调试和故障排除

### 1. 启用详细日志

```csharp
var result = await ForUser(userId)
    .AddCurrency("gold", amount)
    .WithDescription($"操作详情 - 玩家:{userId}, 数量:{amount}, 时间:{DateTime.UtcNow}")
    .ExecuteAsync();
```

### 2. 事务内容检查

```csharp
var builder = ForUser(userId)
    .SetData("level", 10)
    .AddCurrency("gold", 100);

// 调试：检查将要执行的操作
var operations = builder.Build();
foreach (var op in operations)
{
    Game.Logger.LogDebug("操作: {Type}, 键: {Key}, 值: {Value}", op.Type, op.Key, op.Value);
}

var result = await ExecuteTransactionAsync(operations, "调试事务");
```

### 3. 超时处理

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var result = await ForUser(userId)
        .SetData("data", value)
        .ExecuteAsync()
        .WaitAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Game.Logger.LogWarning("操作超时，请检查网络连接");
}
```

## 常见错误和解决方案

| 错误 | 原因 | 解决方案 |
|------|------|----------|
| `ArgumentException: UserId must be positive` | 用户ID无效 | 确保用户ID > 0 |
| `ArgumentException: Key cannot be empty` | 键名为空 | 检查键名是否有效 |
| `UserCloudDataResult.InsufficientCurrency` | 货币不足 | 先查询当前货币再执行扣除 |
| `UserCloudDataResult.CapExceeded` | 上限数据超出设定上限 | 检查ModifyCappedData操作的增量值 |
| `UserCloudDataResult.TransactionCommitEmpty` | 空事务 | 确保事务包含有效操作 |
| `UserCloudDataResult.FailedToSend` | 网络问题 | 检查网络连接，考虑重试 |

## 下一步

- 阅读 [CloudData系统完整文档](../systems/CloudDataSystem.md)
- 了解 [异步编程最佳实践](../best-practices/AsyncProgramming.md)
- 查看 [框架测试指南](Testing.md) 学习如何测试CloudData操作 