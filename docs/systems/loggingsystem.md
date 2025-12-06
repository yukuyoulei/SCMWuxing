# 日志系统文档

## 概述

WasiCore 框架提供了统一的日志系统，用于记录游戏运行时的各种信息。该系统支持多种日志级别，提供结构化的日志输出，并能根据不同环境配置日志行为。

## ⚠️ 重要说明

**禁止使用 `Console.WriteLine`**，在 WasiCore 引擎中，`Console.WriteLine` 无法正常工作。推荐使用框架提供的统一日志系统，以确保日志的正确输出和管理。

## 基本用法

### 获取日志记录器

框架提供全局的日志记录器实例：

```csharp
// 使用全局日志记录器
Game.Logger.LogInformation("这是一条信息日志");
```

### 日志级别

框架支持四种主要的日志级别：

```csharp
// 调试信息 - 仅在开发模式下显示
Game.Logger.LogDebug("详细的调试信息");

// 一般信息 - 正常操作记录
Game.Logger.LogInformation("玩家已连接到服务器");

// 警告信息 - 需要注意但不致命的问题
Game.Logger.LogWarning("内存使用率较高");

// 错误信息 - 严重问题或异常
Game.Logger.LogError("数据库连接失败");
```

## 日志级别详解

### LogDebug

**用途**：详细的调试信息，通常包含程序执行的细节
**显示**：仅在 DEBUG 模式下输出
**性能**：在 Release 版本中会被编译器优化掉

```csharp
#if DEBUG
Game.Logger.LogDebug("属性计算详情: Base={BaseValue}, Bonus={BonusValue}, Final={FinalValue}", 
    baseValue, bonusValue, finalValue);
#endif

// 或者直接使用（框架会自动处理）
Game.Logger.LogDebug("当前状态: {EntityState}, 位置: {Position}", entity.State, entity.Position);
```

### LogInformation

**用途**：记录正常的业务操作和状态变化
**显示**：在所有模式下输出
**场景**：用户操作、系统状态变化、重要业务逻辑执行

```csharp
Game.Logger.LogInformation("玩家 {PlayerName} 进入游戏世界", playerName);
Game.Logger.LogInformation("技能 {SkillName} 施放成功", skillName);
Game.Logger.LogInformation("关卡 {LevelName} 加载完成", levelName);
```

### LogWarning

**用途**：记录需要关注但不会导致程序异常的问题
**显示**：在所有模式下输出
**场景**：性能警告、资源不足、配置问题

```csharp
Game.Logger.LogWarning("内存使用率达到 {MemoryUsage:P}，建议清理缓存", memoryUsage);
Game.Logger.LogWarning("玩家 {PlayerName} 尝试执行无效操作: {Action}", playerName, action);
Game.Logger.LogWarning("网络延迟较高: {Latency}ms", latency);
```

### LogError

**用途**：记录错误和异常情况
**显示**：在所有模式下输出
**场景**：异常捕获、操作失败、系统错误

```csharp
Game.Logger.LogError("技能施放失败: {SkillName}, 原因: {FailureReason}", skillName, failureReason);
Game.Logger.LogError(exception, "数据加载失败: {FilePath}", filePath);
Game.Logger.LogError(exception, "网络连接异常");
```

## 最佳实践

### 1. 推荐使用参数化消息

```csharp
// ✅ 推荐：使用参数化消息（结构化日志）
Game.Logger.LogInformation("玩家 {PlayerName} 获得了 {ItemName}", player.Name, item.Name);

// ⚠️ 可用但不推荐：字符串内插（在性能和结构化方面不如参数化消息）
Game.Logger.LogInformation($"玩家 {player.Name} 获得了 {item.Name}");

// ❌ 不推荐：字符串拼接（性能较差）
Game.Logger.LogInformation("玩家 " + player.Name + " 获得了 " + item.Name);
```

#### 参数化消息的优势

参数化消息相比字符串内插有以下优势：

- **更好的性能**：只有在日志级别启用时才会格式化字符串
- **结构化日志支持**：参数可以被日志系统解析为结构化数据
- **更强的可搜索性**：日志聚合系统可以更好地索引和搜索
- **更高的内存效率**：避免不必要的字符串分配

虽然字符串内插在功能上也能工作，但在日志记录场景下，参数化消息是更优的选择。

### 2. 提供足够的上下文信息

```csharp
// ✅ 好：包含足够的上下文
Game.Logger.LogError("属性设置失败 - 实体: {EntityId}, 属性: {PropertyName}, 值: {Value}, 原因: {Error}", 
    entity.Id, property.Name, value, error);

// ❌ 差：信息不足
Game.Logger.LogError("属性设置失败");
```

### 3. 建议避免在高频循环中记录日志

```csharp
// ⚠️ 需要注意：在循环中频繁记录日志可能影响性能
foreach (var entity in entities)
{
    Game.Logger.LogDebug("更新实体: {EntityId}", entity.Id);
    entity.Update();
}

// ✅ 推荐：记录汇总信息，性能更好
var updateCount = 0;
foreach (var entity in entities)
{
    entity.Update();
    updateCount++;
}
Game.Logger.LogDebug("批量更新完成，共更新 {UpdateCount} 个实体", updateCount);
```

### 4. 性能敏感代码中的条件编译建议

```csharp
// ✅ 推荐：对于昂贵操作，可以使用条件编译确保性能
#if DEBUG
Game.Logger.LogDebug("复杂计算结果: {Result}", CalculateComplexData());
#endif

// 通常情况下，框架的自动优化已足够（参数化消息本身具有性能优势）
Game.Logger.LogDebug("复杂计算结果: {Result}", CalculateComplexData()); // 只有在Debug级别启用时才会执行
```

### 5. 异常处理中的日志记录

```csharp
try
{
    var data = LoadGameData(fileName);
    Game.Logger.LogInformation("成功加载游戏数据: {FileName}", fileName);
    return data;
}
catch (FileNotFoundException ex)
{
    Game.Logger.LogError("游戏数据文件不存在: {FileName}, 错误: {ErrorMessage}", fileName, ex.Message);
    return null;
}
catch (Exception ex)
{
    Game.Logger.LogError(ex, "加载游戏数据时发生未知错误: {FileName}", fileName);
    throw;
}
```

## 常见应用场景

### 游戏实体管理

```csharp
public class EntityManager
{
    public Entity CreateEntity(string entityType)
    {
        try
        {
            var entity = EntityFactory.Create(entityType);
            Game.Logger.LogInformation("创建实体成功: 类型={EntityType}, ID={EntityId}", entityType, entity.Id);
            return entity;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "创建实体失败: 类型={EntityType}", entityType);
            throw;
        }
    }
    
    public void DestroyEntity(Entity entity)
    {
        Game.Logger.LogInformation("销毁实体: ID={EntityId}, 类型={EntityType}", entity.Id, entity.GetType().Name);
        entity.Dispose();
    }
}
```

### 属性系统调试

```csharp
public class PropertyDebugger
{
    public void LogPropertyState(UnitPropertyComplex propertyComplex, PropertyUnitNumeric property)
    {
        var baseValue = propertyComplex.GetFixed(property, PropertySubType.Base);
        var finalValue = propertyComplex.GetFinal(property);
        
        Game.Logger.LogDebug("属性状态 {PropertyName}: Base={BaseValue}, Final={FinalValue}", 
            property.FriendlyName, baseValue, finalValue);
        
        if (Math.Abs(finalValue - baseValue) > 0.01)
        {
            var difference = finalValue - baseValue;
            Game.Logger.LogDebug("属性 {PropertyName} 存在修正值，差异: {Difference}", 
                property.FriendlyName, difference);
        }
    }
}
```

### 网络通信

```csharp
public class NetworkManager
{
    public void OnPlayerConnected(Player player)
    {
        Game.Logger.LogInformation("玩家连接: {PlayerName} ({PlayerId}) 从 {IPAddress}", 
            player.Name, player.Id, player.IPAddress);
    }
    
    public void OnPlayerDisconnected(Player player, string reason)
    {
        Game.Logger.LogInformation("玩家断开连接: {PlayerName}, 原因: {Reason}", 
            player.Name, reason);
    }
    
    public void OnNetworkError(Exception ex)
    {
        Game.Logger.LogError(ex, "网络错误");
        Game.Logger.LogDebug("网络错误详情: {Exception}", ex);
    }
}
```

### 技能系统

```csharp
public class SkillSystem
{
    public CmdResult CastSkill(Entity caster, Skill skill, ITarget target)
    {
        Game.Logger.LogDebug("尝试施放技能: 施法者={CasterId}, 技能={SkillName}, 目标={Target}", 
            caster.Id, skill.Name, target);
        
        if (!CanCastSkill(caster, skill))
        {
            Game.Logger.LogWarning("技能施放条件不满足: {SkillName}", skill.Name);
            return CmdError.SkillNotAvailable;
        }
        
        try
        {
            var result = ExecuteSkill(caster, skill, target);
            if (result.Success)
            {
                Game.Logger.LogInformation("技能施放成功: {SkillName}", skill.Name);
            }
            else
            {
                Game.Logger.LogWarning("技能施放失败: {SkillName}, 原因: {Reason}", 
                    skill.Name, result.Message);
            }
            return result;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "技能施放异常: {SkillName}", skill.Name);
            return CmdError.InternalError;
        }
    }
}
```

## 配置和自定义

### 环境相关配置

框架会根据编译配置自动调整日志行为：

- **DEBUG 模式**：输出所有级别的日志
- **RELEASE 模式**：优化 LogDebug 调用，只输出 Information、Warning、Error

### 日志格式

框架提供标准化的日志格式：

```
[时间戳] [级别] [线程] [类名] - 消息内容
[2024-01-20 14:30:15] [INFO] [Main] [EntityManager] - 创建实体成功: 类型=Player, ID=12345
[2024-01-20 14:30:16] [WARN] [GameLogic] [SkillSystem] - 技能冷却中: FireBall
[2024-01-20 14:30:17] [ERROR] [Network] [NetworkManager] - 连接超时: 192.168.1.100
```

## 性能考虑

### 1. 字符串插值的性能

```csharp
// 参数化消息的性能优势：只有在Debug级别启用时才会执行参数计算
Game.Logger.LogDebug("复杂计算: {Result}", ExpensiveCalculation());

// 在 Release 模式下，框架会优化掉 Debug 级别的日志，包括参数计算
// 但如果需要额外保险，可以使用条件编译：
#if DEBUG
Game.Logger.LogDebug("复杂计算: {Result}", ExpensiveCalculation());
#endif
```

### 2. 避免不必要的对象创建

```csharp
// ✅ 推荐：简单类型的参数化消息
Game.Logger.LogInformation("玩家等级: {PlayerLevel}", player.Level);

// ⚠️ 注意：复杂对象的ToString()可能有性能开销
Game.Logger.LogDebug("详细状态: {ComplexObject}", complexObject); // 确保complexObject.ToString()高效
```

## 故障排除

### 日志不输出

1. **检查日志级别**：确认当前环境是否支持该级别的日志
2. **检查编译配置**：DEBUG 模式下才会输出 LogDebug
3. **检查语法**：建议使用 `Game.Logger.LogXxx` 而不是 `Console.WriteLine`

### 性能问题

1. **建议避免高频日志**：尽量不要在游戏主循环中频繁记录日志
2. **考虑使用条件编译**：对于性能敏感的调试日志，可以使用 `#if DEBUG`
3. **简化日志内容**：建议避免在日志中执行复杂计算

## 相关文档

- [编码规范](../CONVENTIONS.md) - 包含详细的日志使用规范
- [快速开始指南](../guides/QuickStart.md) - 新手入门教程
- [性能优化](../best-practices/PerformanceOptimization.md) - 性能优化建议 