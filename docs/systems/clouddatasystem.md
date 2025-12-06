# 云数据系统 (CloudData System)

## 系统概述

云数据系统是一个企业级的用户数据管理框架，提供统一的API来处理用户数据的查询、存储和事务操作。该系统设计用于支持大规模多人在线游戏的用户数据管理需求。

### ⚠️ 重要概念区分：Player.Id vs User.UserId

在使用云数据系统之前，必须理解以下重要概念：

| 概念 | 类型 | 含义 | 持久性 | 用途 |
|------|------|------|--------|------|
| **Player.Id** | `int` | 当局游戏的槽位编号(0,1,2...) | ❌ 临时 | 当前游戏会话内的玩家标识 |
| **User.UserId** | `long` | 平台用户的全局唯一标识 | ✅ 持久 | 云数据存储的用户标识 |

**核心原则：**
- 🔹 **云数据必须使用 UserId**，因为它是持久的用户身份标识
- 🔹 **Player.Id 仅在当局游戏有意义**，不同游戏会话中同一用户的 Player.Id 可能不同
- 🔹 **便利API自动处理转换**，开发者可以直接传入 Player 对象，系统会自动提取对应的 UserId

### 核心特性

- **统一API入口** - 解决查询和修改操作分离的不一致问题
- **流式事务构建** - 类型安全的链式API设计
- **智能优化** - 自动合并和优化事务操作
- **异步编程模式** - 完整的async/await支持
- **类型安全** - 编译时错误检查和null安全
- **ACID事务** - 保证数据一致性和完整性
- **批量操作** - 支持高效的批量查询和更新

### 支持的数据类型

| 数据类型 | 描述 | 用途示例 |
|---------|------|---------|
| BigInt | 64位整数 | 玩家等级、经验值、积分 |
| VarChar255 | 可变长度字符串(≤255字符) | 玩家昵称、状态信息 |
| Blob | 二进制数据 | 序列化对象、配置数据 |
| Currency | 货币类型 | 金币、钻石、代币 |
| CappedData | 有上限数据（支持定时重置为0） | 体力、每日活跃度、周签到进度、月度任务积分 |
| ListItem | 列表项 | 背包物品、好友列表 |

## 架构设计

### 分层架构

```
┌─────────────────────────────────────────────────────────┐
│                   统一API入口层                          │
│              CloudDataApi / CloudData                   │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────┼───────────────────────────────────┐
│                 操作实现层                               │
│         CloudDataOperations + TransactionBuilder        │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────┼───────────────────────────────────┐
│                 管理服务层                               │
│              CloudDataManager                          │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────┼───────────────────────────────────┐
│                引擎接口层                               │
│           IUserCloudDataProvider                       │
└─────────────────────────────────────────────────────────┘
```

### 核心组件

#### 1. CloudDataApi
- **职责**: 统一的API入口点
- **特点**: 简化别名，委托实现
- **使用**: `CloudData.QueryUserDataAsync()`, `CloudData.ForUser()`

#### 2. CloudDataOperations  
- **职责**: 具体操作实现
- **特点**: 参数验证，错误处理，请求管理
- **功能**: 查询操作、事务执行、ID生成

#### 3. TransactionBuilder
- **职责**: 流式事务构建
- **特点**: 链式API，智能优化，类型安全
- **功能**: 事务构建、操作验证、批量处理

#### 4. CloudDataManager
- **职责**: 底层管理服务
- **特点**: Provider生命周期，异步响应处理
- **功能**: 事件管理、等待器管理、日志记录

#### 5. Internal组件
- **AsyncWaiter**: 异步操作等待器，支持直接await
- **IdGenerator**: 雪花算法ID生成器，全局唯一
- **ListItemReference**: 类型安全的列表项引用

## API参考

### 基础查询操作

#### 批量查询用户数据
```csharp
// 方式1: 使用 UserId (直接指定用户ID)
var result = await CloudData.QueryUserDataAsync(
    userIds: [123L, 456L], 
    keys: ["level", "score", "last_login"]
);

// 方式2: 使用 Player 对象 (便利方法，自动提取UserId)
var players = [player1, player2];
var playerResult = await CloudData.QueryPlayersDataAsync(
    players: players,
    keys: ["level", "score", "last_login"]
);

if (result.IsSuccess)
{
    foreach (var userResult in result.Data)
    {
        Game.Logger.LogInformation("User {UserId}:", userResult.UserId);
        Game.Logger.LogInformation("  Level: {Level}", userResult.BigIntData["level"]);
        Game.Logger.LogInformation("  Score: {Score}", userResult.BigIntData["score"]);
    }
}
```

#### 查询货币数据
```csharp
var currencyResult = await CloudData.QueryCurrencyAsync(
    userIds: [userId],
    keys: ["gold", "diamond", "energy"]
);
```

#### 查询列表项
```csharp
var inventoryResult = await CloudData.QueryUserListItemsAsync(
    userId: userId,
    key: "inventory",
    maxCount: 50
);
```

#### 按ID查找列表项
```csharp
var itemResult = await CloudData.FindListItemByIdAsync(itemId);
if (itemResult.IsSuccess && itemResult.Data.IsFound)
{
    var item = itemResult.Data.Record;
    Game.Logger.LogInformation("Found item: {ItemUuid}", item.ItemUuid);
}
```

### 事务操作

#### 简单数据操作
```csharp
// 方式1: 使用 UserId (当你有User.UserId时)
var result = await CloudData.ForUser(userId)  // userId是User.UserId，不是Player.Id！
    .SetData("level", 25)
    .SetData("last_login", DateTime.UtcNow.ToString())
    .AddToData("experience", 1000)
    .ExecuteAsync();

// 方式2: 使用 Player 对象 (便利方法，推荐用于游戏逻辑)
var playerResult = await CloudData.ForPlayer(player)  // 自动提取User.UserId
    .SetData("level", 25)
    .SetData("last_login", DateTime.UtcNow.ToString())
    .AddToData("experience", 1000)
    .WithDescription("玩家升级")
    .ExecuteAsync();
```

#### 货币操作
```csharp
// 方式1: 使用 UserId
var result = await CloudData.ForUser(userId)
    .AddCurrency("gold", 100)
    .CostCurrency("energy", 10)
    .WithDescription("完成任务奖励")
    .ExecuteAsync();

// 方式2: 使用 Player 对象 (推荐)
var playerResult = await CloudData.ForPlayer(player)
    .AddCurrency("gold", 100)
    .CostCurrency("energy", 10)
    .WithDescription("完成任务奖励")
    .ExecuteAsync();
```

#### 列表项操作

##### 添加列表项
```csharp
var builder = CloudData.ForUser(userId);

// 准备新物品
var weaponRef = builder.PrepareListItem("inventory", weaponData);
var armorRef = builder.PrepareListItem("inventory", armorData);

// 执行事务
var result = await builder
    .AddListItem(weaponRef)
    .AddListItem(armorRef)
    .AddCurrency("gold", 500)
    .WithDescription("战斗胜利奖励")
    .ExecuteAsync();

// 获取生成的物品ID
var weaponId = weaponRef.Id;
var armorId = armorRef.Id;
```

##### 更新列表项
```csharp
// 方式1: 直接使用全局唯一ID更新
var updateResult = await CloudData.ForUser(userId)
    .UpdateListItem(itemId, newItemData)
    .WithDescription("升级武器")
    .ExecuteAsync();

// 方式2: 使用ListItemReference更新
var updateResult2 = await CloudData.ForUser(userId)
    .UpdateListItem(itemRef, enhancedItemData)
    .WithDescription("强化装备")
    .ExecuteAsync();
```

##### 删除列表项
```csharp
// 方式1: 直接使用全局唯一ID删除
var deleteResult = await CloudData.ForUser(userId)
    .DeleteListItem(itemId)
    .WithDescription("出售物品")
    .ExecuteAsync();

// 方式2: 使用ListItemReference删除
var deleteResult2 = await CloudData.ForUser(userId)
    .DeleteListItem(itemRef)
    .WithDescription("丢弃过期物品")
    .ExecuteAsync();

// 方式3: 批量删除
var deleteItemRefs = await GetExpiredItems(userId);
var batchDeleteResult = await CloudData.ForUser(userId)
    .DeleteListItems(deleteItemRefs)
    .WithDescription("清理过期物品")
    .ExecuteAsync();
```

##### 移动列表项
```csharp
// 将物品从背包移动到仓库
var moveResult = await CloudData.ForUser(userId)
    .MoveListItem(itemId, "warehouse")
    .WithDescription("存储物品到仓库")
    .ExecuteAsync();

// 使用引用移动
var moveResult2 = await CloudData.ForUser(userId)
    .MoveListItem(itemRef, "equipped_items")
    .WithDescription("装备物品")
    .ExecuteAsync();
```



#### 有上限数据操作

⚠️ **重要说明**：重置机制会将当前值重置为0。对于体力系统，这意味着**已消耗体力**重置为0，即体力恢复满值。

**重置机制详解**：
- 重置时间到达时，系统会自动将当前值设置为 0
- 上限值保持不变  
- 存储的是"已消耗量"而不是"剩余量"

**体力系统的正确用法**：

💡 **核心理念**：存储"已消耗体力"而非"剩余体力"，重置为0 = 体力恢复满值

```csharp
// ✅ 体力系统：存储已消耗的体力，重置为0表示体力恢复满值
.ModifyCappedData("energy_consumed", 20, 100, UserDataResetOption.Daily())  // 消耗20点体力

// UI显示计算：剩余体力 = 最大体力(100) - 已消耗体力(energy_consumed)
var remainingEnergy = maxEnergy - energyConsumed;

// ✅ 其他适用场景：每日任务次数
.ModifyCappedData("daily_quest_count", 1, 10, UserDataResetOption.Daily())

// ✅ PVP挑战次数  
.ModifyCappedData("pvp_attempts", 1, 5, UserDataResetOption.Daily())
```

**为什么这样设计？**
- 🔄 **重置语义清晰**：重置为0 = "清空已消耗量" = 体力恢复满值
- 🛡️ **防护机制完善**：上限值防止过度消耗，事务自动失败
- 🎯 **逻辑统一**：所有有上限资源都遵循"重置为0"的统一语义

#### 线性回复体力系统的实现

对于需要**连续回复体力**的游戏（如每分钟回复1点），可以利用CappedData的`LastUpdateTime`字段：

```csharp
// 方案1：利用CappedData的LastUpdateTime实现线性回复
public async Task<EnergyInfo> GetCurrentEnergyWithRecovery(long userId)
{
    var result = await CloudData.QueryCappedDataAsync(
        userIds: [userId], 
        keys: ["energy_consumed"]
    );
    
    if (!result.IsSuccess) return null;
    
    var energyData = result.Data.First().CappedData["energy_consumed"];
    var maxEnergy = energyData.Cap;
    var consumedEnergy = energyData.Value;
    var lastUpdateTime = energyData.LastUpdateTime;
    
    // 计算自上次更新以来的回复量
    var timeSinceUpdate = DateTime.Now - lastUpdateTime;
    var recoveredEnergy = (long)(timeSinceUpdate.TotalMinutes * ENERGY_RECOVERY_RATE); // 每分钟回复率
    
    // 计算当前实际剩余体力
    var actualConsumed = Math.Max(0, consumedEnergy - recoveredEnergy);
    var currentEnergy = Math.Min(maxEnergy, maxEnergy - actualConsumed);
    
    return new EnergyInfo
    {
        CurrentEnergy = currentEnergy,
        MaxEnergy = maxEnergy,
        LastUpdateTime = lastUpdateTime
    };
}

// 消耗体力时更新LastUpdateTime
public async Task<UserCloudDataResult> ConsumeEnergyWithRecovery(long userId, long amount)
{
    // 先获取当前状态（包含回复计算）
    var currentState = await GetCurrentEnergyWithRecovery(userId);
    if (currentState.CurrentEnergy < amount)
        return UserCloudDataResult.InsufficientResources;
    
    // 计算消耗后的新已消耗总量（同步回复效果到数据库）
    var currentConsumed = currentState.MaxEnergy - currentState.CurrentEnergy;
    var newConsumed = currentConsumed + amount;
    
    // 先查询当前数据库值，然后设置为新的已消耗量
    var queryResult = await CloudData.QueryCappedDataAsync([userId], ["energy_consumed"]);
    if (!queryResult.IsSuccess) return queryResult;
    
    var oldConsumed = queryResult.Data.First().CappedData["energy_consumed"].Value;
    var deltaConsumed = newConsumed - oldConsumed;  // 需要调整的差值
    
    return await CloudData.ForUser(userId)
        .ModifyCappedData("energy_consumed", deltaConsumed, currentState.MaxEnergy)
        .WithDescription($"消耗体力 {amount}（同步回复效果）")
        .ExecuteAsync();
}
```

#### 混合方案：普通数据 + 货币数据

```csharp
// 方案2：使用普通数据存储时间，货币数据控制消耗
public class HybridEnergySystem
{
    private const long ENERGY_RECOVERY_RATE = 1; // 每分钟回复1点
    private const long MAX_ENERGY = 100;
    
    public async Task<EnergyInfo> GetCurrentEnergy(long userId)
    {
        // 查询上次离线时间和当前体力
        var dataResult = await CloudData.QueryUserDataAsync(
            userIds: [userId], 
            keys: ["last_offline_time", "current_energy"]
        );
        
        if (!dataResult.IsSuccess) return null;
        
        var userData = dataResult.Data.First();
        var lastOfflineStr = userData.VarChar255Data.GetValueOrDefault("last_offline_time");
        var currentEnergy = userData.BigIntData.GetValueOrDefault("current_energy", MAX_ENERGY);
        
        if (DateTime.TryParse(lastOfflineStr, out var lastOfflineTime))
        {
            // 计算离线期间的回复量
            var offlineMinutes = (DateTime.UtcNow - lastOfflineTime).TotalMinutes;
            var recoveredEnergy = (long)(offlineMinutes * ENERGY_RECOVERY_RATE);
            currentEnergy = Math.Min(MAX_ENERGY, currentEnergy + recoveredEnergy);
        }
        
        return new EnergyInfo
        {
            CurrentEnergy = currentEnergy,
            MaxEnergy = MAX_ENERGY,
            LastUpdateTime = lastOfflineTime
        };
    }
    
    public async Task<UserCloudDataResult> ConsumeEnergy(long userId, long amount)
    {
        var currentState = await GetCurrentEnergy(userId);
        if (currentState.CurrentEnergy < amount)
            return UserCloudDataResult.InsufficientResources;
        
        var newEnergy = currentState.CurrentEnergy - amount;
        
        return await CloudData.ForUser(userId)
            .SetData("current_energy", newEnergy)
            .SetData("last_offline_time", DateTime.UtcNow.ToString("O"))
            .WithDescription($"消耗体力 {amount}")
            .ExecuteAsync();
    }
}
```

**三种体力系统方案对比**：

| 方案 | 适用场景 | 优势 | 注意事项 |
|------|----------|------|----------|
| **CappedData重置** | 每日重置体力 | 简单、自动重置 | 固定重置时间 |
| **CappedData+LastUpdateTime** | 线性回复 | 利用现有字段、防护完善 | 需要同步回复效果到数据库 |
| **普通数据+货币数据** | 复杂回复逻辑 | 灵活、可控 | 需要手动管理上限 |

💡 **选择建议**：
- 如果体力系统是**定时重置**（如每日0点满血复活），推荐使用**CappedData重置**方案
- 如果体力系统是**连续回复**（如每分钟回复1点），可以考虑**CappedData+LastUpdateTime**方案
- 如果需要复杂的回复逻辑（如VIP加速、道具加速），推荐使用**混合方案**

```csharp
// 基础体力系统 - 存储已消耗体力，每日0点重置为0（体力恢复满值），上限100
var result = await CloudData.ForUser(userId)
    .ModifyCappedData("energy_consumed", 20, 100, UserDataResetOption.Daily())
    .WithDescription("进入副本消耗体力")
    .ExecuteAsync();

// 每周活跃度 - 每周一重置为0，上限1000
var weeklyResult = await CloudData.ForUser(userId)
    .ModifyCappedData("weekly_activity", 50, 1000, UserDataResetOption.Weekly(1, DayOfWeek.Monday))
    .WithDescription("每周活跃度增加")
    .ExecuteAsync();

// 消耗体力（增加已消耗量）
var consumeResult = await CloudData.ForUser(userId)
    .ModifyCappedData("energy_consumed", 20, 100)  // 消耗20点体力（增加已消耗量）
    .WithDescription("进入副本")
    .ExecuteAsync();

// 调整体力上限值
var upgradeResult = await CloudData.ForUser(userId)
    .ModifyCappedData("energy_consumed", 0, 120)  // 不修改当前已消耗值，只提升体力上限
    .WithDescription("体力上限提升")
    .ExecuteAsync();

// 查询当前有上限数据
var cappedDataResult = await CloudData.QueryCappedDataAsync(
    userIds: [userId],
    keys: ["energy_consumed", "weekly_activity"]
);

// UI显示计算示例
if (cappedDataResult.IsSuccess)
{
    var userData = cappedDataResult.Data.First();
    var energyConsumed = userData.CappedData["energy_consumed"].Value;
    var maxEnergy = userData.CappedData["energy_consumed"].Cap;
    var remainingEnergy = maxEnergy - energyConsumed;  // 剩余体力
    Game.Logger.LogInformation("剩余体力: {RemainingEnergy}/{MaxEnergy}", remainingEnergy, maxEnergy);
}
```

##### 重置有上限数据
```csharp
// 立即重置体力（将已消耗体力重置为0，即恢复满体力）
var resetResult = await CloudData.ForUser(userId)
    .ResetCappedData("energy_consumed")
    .WithDescription("使用体力药水")
    .ExecuteAsync();

// 重置每日任务进度
var resetQuestResult = await CloudData.ForUser(userId)
    .ResetCappedData("daily_quest_progress")
    .WithDescription("每日重置")
    .ExecuteAsync();

// 批量重置多个有上限数据
var batchResetResult = await CloudData.ForUser(userId)
    .ResetCappedData("pvp_attempts")
    .ResetCappedData("daily_quest_count")
    .ResetCappedData("weekly_activity")
    .WithDescription("周度重置")
    .ExecuteAsync();
```

#### 批量用户操作
```csharp
var result = await CloudData.ForUsers(user1, user2, user3)
    .ForAllUsers(builder => builder
        .AddCurrency("daily_reward", 50)
        .SetData("last_daily_reward", DateTime.UtcNow.ToString())
    )
    .ExecuteAllAsync();
```

#### 复杂事务示例
```csharp
var builder = CloudData.ForUser(userId);

// 准备物品
var rewardItems = new[]
{
    ("sword", swordData),
    ("shield", shieldData),
    ("potion", potionData)
};

var itemRefs = builder.PrepareListItems("inventory", 
    rewardItems.Select(item => item.Item2).ToArray());

// 构建复杂事务
var result = await builder
    .AddListItems(itemRefs)
    .AddCurrency("gold", 1000)
    .AddCurrency("experience", 500)
    .CostCurrency("energy", 20)
    .SetData("level", newLevel)
    .SetData("last_quest_completion", DateTime.UtcNow.ToString())
    .WithDescription("完成史诗任务")
    .WithOptimization(true)
    .ExecuteAsync();

// 使用生成的物品ID
for (int i = 0; i < itemRefs.Length; i++)
{
    var itemName = rewardItems[i].Item1;
    var itemId = itemRefs[i].Id;
    Game.Logger.LogInformation("Created {ItemName} with ID: {ItemId}", itemName, itemId);
}
```

### 名称管理

#### 检查名称可用性
```csharp
var statusResult = await CloudData.CheckNameClaimedStatusAsync("guilds", "AwesomeGuild");
if (statusResult.IsSuccess)
{
    if (!statusResult.Data.IsClaimed)
    {
        // 名称可用，可以注册
    }
    else
    {
        // 名称已被占用
    }
}
```

#### 搜索相似名称
```csharp
var searchResult = await CloudData.SearchClaimedNamesAsync("guilds", "Awesome");
if (searchResult.IsSuccess)
{
    foreach (var name in searchResult.Data)
    {
        Game.Logger.LogInformation("Similar name: {Name}", name.Name);
    }
}
```

#### 注册新名称
```csharp
var result = await CloudData.ForUser(userId)
    .ClaimNewName("guilds", "MyGuildName")
    .SetData("guild_level", 1)
    .SetData("guild_created", DateTime.UtcNow.ToString())
    .WithDescription("创建公会")
    .ExecuteAsync();
```

#### 删除名称
```csharp
// 删除公会名称
var deleteResult = await CloudData.ForUser(userId)
    .DeleteName("guilds", "MyGuildName")
    .SetData("guild_deleted", DateTime.UtcNow.ToString())
    .WithDescription("解散公会")
    .ExecuteAsync();

// 删除玩家昵称
var nicknameDeleteResult = await CloudData.ForUser(userId)
    .DeleteName("player_nicknames", "CoolPlayer123")
    .WithDescription("更换昵称")
    .ExecuteAsync();

// 批量删除多个名称
var batchDeleteResult = await CloudData.ForUser(userId)
    .DeleteName("temp_channels", "TempChannel1")
    .DeleteName("temp_channels", "TempChannel2")
    .WithDescription("清理临时频道")
    .ExecuteAsync();
```

## 使用指南

### 快速开始

1. **引用命名空间**
```csharp
using GameCore.UserCloudData;
using static GameCore.UserCloudData.CloudData; // 简化调用
```

2. **基础查询**
```csharp
// 查询单个用户数据
var userData = await QueryUserDataAsync([userId], ["level", "score"]);

// 查询多个用户数据  
var usersData = await QueryUserDataAsync(userIds, keys);
```

3. **简单事务**
```csharp
// 基础数据更新
var result = await ForUser(userId)
    .SetData("level", 10)
    .AddCurrency("gold", 100)
    .ExecuteAsync();
```

4. **错误处理**
```csharp
var result = await ForUser(userId).SetData("level", 10).ExecuteAsync();

if (result == UserCloudDataResult.Success)
{
    // 操作成功
    Game.Logger.LogInformation("数据更新成功");
}
else
{
    // 处理错误
    Game.Logger.LogError("操作失败: {Result}", result);
}
```

### 性能优化建议

#### 1. 批量操作
```csharp
// ✅ 推荐：批量查询
var result = await QueryUserDataAsync(
    userIds: allUserIds,
    keys: requiredKeys
);

// ❌ 避免：循环单个查询
foreach (var userId in allUserIds)
{
    await QueryUserDataAsync([userId], requiredKeys);
}
```

#### 2. 事务合并
```csharp
// ✅ 推荐：一个事务完成所有操作
var result = await ForUser(userId)
    .SetData("level", newLevel)
    .AddCurrency("gold", reward)
    .AddCurrency("experience", expReward)
    .ExecuteAsync();

// ❌ 避免：多个独立事务
await ForUser(userId).SetData("level", newLevel).ExecuteAsync();
await ForUser(userId).AddCurrency("gold", reward).ExecuteAsync();
await ForUser(userId).AddCurrency("experience", expReward).ExecuteAsync();
```

#### 3. 启用事务优化
```csharp
var result = await ForUser(userId)
    .AddCurrency("gold", 100)
    .AddCurrency("gold", 50)    // 会自动合并为 +150
    .AddCurrency("gold", -20)   // 最终结果: +130
    .WithOptimization(true)     // 启用优化（默认已启用）
    .ExecuteAsync();
```

### 错误处理策略

#### 1. 结果检查模式
```csharp
var result = await CloudData.ForUser(userId)
    .SetData("level", 10)
    .ExecuteAsync();

if (result == UserCloudDataResult.Success)
{
    // 成功处理
}
else
{
    // 根据错误类型处理
    switch (result)
    {
        case UserCloudDataResult.QueryUserIdMissing:
            Game.Logger.LogError("用户ID无效");
            break;
        case UserCloudDataResult.FailedToSend:
            Game.Logger.LogError("网络连接问题");
            break;
        case UserCloudDataResult.TransactionCommitEmpty:
            Game.Logger.LogError("空事务错误");
            break;
        case UserCloudDataResult.InsufficientCurrency:
            Game.Logger.LogError("货币不足");
            break;
        case UserCloudDataResult.CapExceeded:
            Game.Logger.LogError("上限数据值超出设定上限");
            break;
        default:
            Game.Logger.LogError("其他错误: {ErrorType}", result);
            break;
    }
}
```

#### 2. 异常处理模式
```csharp
try
{
    var builder = CloudData.ForUser(userId);
    builder.SetData("invalid_key_length_over_180_characters...", value); // 会抛出异常
    
    var result = await builder.ExecuteAsync();
}
catch (ArgumentException ex)
{
    // 参数验证错误
    Game.Logger.LogError(ex, "参数错误: {Message}", ex.Message);
}
catch (InvalidOperationException ex)
{
    // 操作逻辑错误
    Game.Logger.LogError(ex, "操作错误: {Message}", ex.Message);
}
```

### 数据类型最佳实践

#### 1. 选择合适的数据类型
```csharp
// ✅ 数值数据使用BigInt
.SetData("level", 25)
.SetData("experience", 15000L)

// ✅ 文本数据使用VarChar255
.SetData("player_name", "PlayerName")
.SetData("status", "online")

// ✅ 复杂对象使用Blob
var playerConfig = JsonSerializer.SerializeToUtf8Bytes(config);
.SetData("config", playerConfig)

// ✅ 货币使用专门的货币操作
.AddCurrency("gold", 100)
.CostCurrency("diamond", 10)

// ✅ 有上限数据使用ModifyCappedData，支持定时重置为0
.ModifyCappedData("energy_consumed", 20, 100, UserDataResetOption.Daily())   // 体力系统（存储已消耗量）
.ModifyCappedData("weekly_points", 5, 500, UserDataResetOption.Weekly())     // 每周积分
.ModifyCappedData("dungeon_attempts", 1, 5)  // 副本次数（无重置）
```

#### 2. 键名规范
```csharp
// ✅ 推荐的键名规范
"level"           // 简短明确
"last_login"      // 下划线分隔
"guild_id"        // 统一前缀
"inventory_size"  // 描述性名称

// ❌ 避免的键名
"l"              // 过于简短
"lastLogin"      // 驼峰式（不一致）
"LEVEL"          // 全大写
"user.level"     // 包含特殊字符
```

## 最佳实践

### 1. 事务设计原则

#### 原子性操作
```csharp
// ✅ 相关操作放在同一事务中
var result = await CloudData.ForUser(userId)
    .CostCurrency("energy", 10)      // 消耗体力
    .AddCurrency("experience", 100)  // 获得经验
    .SetData("last_battle", DateTime.UtcNow.ToString()) // 更新时间
    .WithDescription("完成战斗")
    .ExecuteAsync();

// ❌ 避免拆分相关操作
await CloudData.ForUser(userId).CostCurrency("energy", 10).ExecuteAsync();
await CloudData.ForUser(userId).AddCurrency("experience", 100).ExecuteAsync();
```

#### 操作顺序
```csharp
// ✅ 先检查资源，再消耗资源，最后给予奖励
var result = await CloudData.ForUser(userId)
    .CostCurrency("gold", upgradeCost)    // 先消耗
    .SetData("weapon_level", newLevel)    // 再更新
    .AddCurrency("experience", expBonus)  // 最后奖励
    .ExecuteAsync();
```

### 2. 列表项管理

#### 准备-添加模式
```csharp
var builder = CloudData.ForUser(userId);

// ✅ 准备阶段：生成ID，不执行数据库操作
var newItems = new[]
{
    builder.PrepareListItem("inventory", weaponData),
    builder.PrepareListItem("inventory", armorData),
    builder.PrepareListItem("inventory", potionData)
};

// 执行阶段：原子性添加所有物品
var result = await builder
    .AddListItems(newItems)
    .AddCurrency("gold", 500)
    .WithDescription("任务奖励")
    .ExecuteAsync();

// 使用生成的ID
foreach (var item in newItems)
{
    Game.Logger.LogInformation("Created item with ID: {ItemId}", item.Id);
}
```

#### 批量操作
```csharp
// ✅ 批量准备物品
var itemsData = lootTable.GenerateRewards();
var itemRefs = builder.PrepareListItems("inventory", itemsData);

// 一次性添加所有物品
await builder.AddListItems(itemRefs).ExecuteAsync();
```

### 3. 查询优化

#### 合理使用maxCount
```csharp
// ✅ 限制返回数量，避免大量数据传输
var recentItems = await CloudData.QueryUserListItemsAsync(
    userId: userId,
    key: "inventory", 
    maxCount: 50  // 只获取最新的50个物品
);

// ✅ 不限制数量（小列表）
var friends = await CloudData.QueryUserListItemsAsync(
    userId: userId,
    key: "friends"  // maxCount: null，获取所有好友
);
```

#### 精确查询键
```csharp
// ✅ 只查询需要的数据
var essentialData = await CloudData.QueryUserDataAsync(
    userIds: [userId],
    keys: ["level", "experience", "gold"]  // 明确指定需要的键
);

// ❌ 避免查询不需要的数据
var allData = await CloudData.QueryUserDataAsync(
    userIds: [userId],
    keys: GetAllPossibleKeys()  // 查询所有可能的键
);
```

### 4. 错误恢复策略

#### 重试机制
```csharp
public async Task<UserCloudDataResult> ExecuteWithRetry(
    Func<Task<UserCloudDataResult>> operation, 
    int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var result = await operation();
            
            if (result.IsSuccess || result != UserCloudDataResult.FailedToSend)
            {
                return result; // 成功或非网络错误，不重试
            }
            
            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 指数退避
                await Game.Delay(delay);
            }
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            // 记录日志，继续重试
            Game.Logger.LogWarning("Attempt {Attempt} failed: {Message}", attempt, ex.Message);
        }
    }
    
    return UserCloudDataResult.FailedToSend;
}
```

## 性能指南

### 1. 查询性能

#### 批量查询优化
```csharp
// ✅ 一次查询多个用户
var leaderboard = await CloudData.QueryUserDataAsync(
    userIds: topUserIds,           // 批量用户ID
    keys: ["level", "score"] // 只查询需要的字段
);

// ❌ 循环单个查询（效率低）
var results = new List<UserData>();
foreach (var userId in topUserIds)
{
    var userData = await CloudData.QueryUserDataAsync(
        [userId], 
        ["level", "score"]
    );
    results.AddRange(userData.Data);
}
```


### 2. 事务性能

#### 操作合并
```csharp
// ✅ 智能合并（自动优化）
var result = await CloudData.ForUser(userId)
    .AddCurrency("gold", 100)
    .AddCurrency("gold", 50)     // 自动合并为 AddCurrency("gold", 150)
    .AddCurrency("silver", 200)
    .AddCurrency("silver", -50)  // 自动合并为 AddCurrency("silver", 150)
    .SetData("level", 10)
    .SetData("level", 11)        // 自动优化为 SetData("level", 11)
    .WithOptimization(true)      // 启用优化
    .ExecuteAsync();
```

#### 操作验证
```csharp
// ✅ 启用验证，检测冲突操作
var result = await CloudData.ForUser(userId)
    .SetData("status", "online")
    // .DeleteData("status")     // 如果取消注释，会抛出异常
    .WithValidation(true)        // 启用验证
    .ExecuteAsync();
```

### 3. 内存管理

#### 大数据对象处理
```csharp
// ✅ 使用流式处理大对象
public async Task SaveLargeGameData(long userId, GameSaveData saveData)
{
    // 分块存储
    var chunks = SplitIntoChunks(saveData, maxChunkSize: 64 * 1024); // 64KB chunks
    
    var builder = CloudData.ForUser(userId);
    
    for (int i = 0; i < chunks.Length; i++)
    {
        builder.SetData($"save_chunk_{i}", chunks[i]);
    }
    
    var result = await builder
        .SetData("save_chunk_count", chunks.Length)
        .SetData("save_timestamp", DateTime.UtcNow.ToString())
        .WithDescription("保存游戏数据")
        .ExecuteAsync();
}
```

## 故障排查

### 常见问题

#### 1. 事务失败
```csharp
// 问题：事务执行失败
var result = await CloudData.ForUser(userId)
    .CostCurrency("gold", 1000)
    .ExecuteAsync();

if (result != UserCloudDataResult.Success)
{
    // 检查具体错误
    switch (result)
    {
        case UserCloudDataResult.InsufficientFunds:
            Game.Logger.LogWarning("金币不足");
            break;
        case UserCloudDataResult.FailedToSend:
            Game.Logger.LogError("网络连接问题");
            break;
        case UserCloudDataResult.TransactionCommitEmpty:
            Game.Logger.LogError("空事务");
            break;
    }
}
```

#### 2. 参数验证错误
```csharp
try
{
    // 可能抛出参数异常的操作
    var result = await CloudData.ForUser(0)  // 无效的用户ID
        .SetData("", "value")                // 空键名
        .ExecuteAsync();
}
catch (ArgumentException ex)
{
    Game.Logger.LogError(ex, "参数错误: {Message}, 参数名: {ParamName}", ex.Message, ex.ParamName);
}
```

#### 3. 网络超时
```csharp
// 添加超时处理
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var result = await CloudData.ForUser(userId)
        .SetData("level", 10)
        .ExecuteAsync()
        .WaitAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Game.Logger.LogWarning("操作超时");
}
```

### 调试技巧

#### 1. 操作追踪
```csharp
// 添加详细描述便于追踪
var result = await CloudData.ForUser(userId)
    .AddCurrency("gold", rewardAmount)
    .WithDescription($"任务奖励 - QuestId:{questId}, Timestamp:{DateTime.UtcNow}")
    .ExecuteAsync();
```

#### 2. 事务内容检查
```csharp
var builder = CloudData.ForUser(userId)
    .SetData("level", 10)
    .AddCurrency("gold", 100);

// 检查构建的操作
var operations = builder.Build();
foreach (var op in operations)
{
    Game.Logger.LogDebug("Operation: {Type}, Key: {Key}, Value: {Value}", op.Type, op.Key, op.Value);
}

// 然后执行
var result = await CloudData.ExecuteTransactionAsync(operations, "调试事务");
```

#### 3. 日志记录
系统自动记录关键操作日志，可在CloudDataManager中查看：
- Provider初始化
- 请求ID映射
- 异步响应处理
- 错误和警告信息

## 版本历史

### v1.0.0 (当前版本)
- ✅ 统一API入口设计
- ✅ 流式事务构建器
- ✅ 智能事务优化
- ✅ 完整的异步编程支持
- ✅ 类型安全的列表项管理
- ✅ 批量操作支持
- ✅ 完整的错误处理和日志记录

### 后续计划
- 🔄 重试机制和熔断器
- 📊 性能监控和度量
- 🔧 配置驱动的服务选择
- 🧪 更完善的单元测试支持

## 参考资料

- [框架概述](../FRAMEWORK_OVERVIEW.md)
- [API参考](../API_REFERENCE.md)
- [最佳实践指南](../best-practices/)
- [项目结构指南](../guides/ProjectStructure.md) 