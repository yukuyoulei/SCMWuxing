# 🚀 指令便利API（Command API）

指令便利API是对WasiCore框架[指令系统](OrderSystem.md)的高级封装，提供了类型安全、简洁易用的流式API，大幅简化了常见游戏操作的实现。

## 📋 目录

- [🎯 设计目标](#设计目标)
- [🏗️ 核心组件](#核心组件)
- [📖 基础用法](#基础用法)
- [🚀 高级用法](#高级用法)
- [⚡ 服务端优化](#服务端优化)
- [💡 实际示例](#实际示例)
- [✅ 最佳实践](#最佳实践)
- [🔗 相关系统](#相关系统)

## 🎯 设计目标

### 问题背景

原始的Command系统虽然功能强大，但在日常开发中存在以下痛点：

```csharp
// ❌ 原始方式 - 冗长且容易出错
var command = new Command
{
    Index = CommandIndex.Execute,
    Type = ComponentTagEx.AbilityManager,
    AbilityLink = ScopeData.Ability.Fireball,
    Target = enemyUnit,
    Player = Player.LocalPlayer,
    Flag = CommandFlag.IsUser
};
var result = command.IssueOrder(playerUnit);
```

**主要问题：**
- 🔴 **重复的样板代码** - 每次创建Command都需要设置相同的基础字段
- 🔴 **易错的参数组合** - Index和Type容易配错，Flag和Player设置容易忘记
- 🔴 **缺乏类型安全** - 编译时无法检查配置的正确性
- 🔴 **学习成本高** - 需要深入了解底层Command结构

### 解决方案

便利API通过多层抽象提供了优雅的解决方案：

```csharp
// ✅ 便利API - 简洁且类型安全
var result = playerUnit.CastAbility(ScopeData.Ability.Fireball, enemyUnit);
```

**核心优势：**
- ✅ **简洁易用** - 一行代码完成复杂操作
- ✅ **类型安全** - 编译时验证，减少运行时错误
- ✅ **智能默认** - 自动设置合理的默认值
- ✅ **性能优化** - 服务端自动优化执行路径

## 🏗️ 核心组件

### 1. CommandBuilder - 流式构建器

提供类型安全的Command构建，支持链式调用：

```csharp
var command = CommandBuilder.Create()
    .WithIndex(CommandIndex.Execute)
    .WithType(ComponentTagEx.AbilityManager)
    .WithAbility(ScopeData.Ability.Fireball)
    .ToUnit(enemyUnit)
    .AsUser()
    .Build();
```

**主要特性：**
- 🔧 **流式API** - 支持方法链式调用
- 🛡️ **类型安全** - 编译时验证参数正确性
- 🎯 **智能推断** - 自动设置相关联的字段
- ⚡ **性能优化** - 服务端支持Ability实例直接使用

### 2. CommandExtensions - 扩展方法

为Entity和Player提供常用命令的快捷方法：

```csharp
// Entity扩展 - 单位操作
unit.CastAbility(abilityLink, target);      // 释放技能
unit.MoveInDirection(45f);                  // 朝指定角度移动
unit.MoveTo(position);                      // 移动到位置
unit.AttackTarget(enemy);                   // 攻击目标
unit.StopMovement();                        // 停止移动

// Player扩展 - 玩家操作
player.CastAbilityWithMainUnit(abilityLink, target);  // 主控单位释放技能
player.MoveMainUnitTo(position);                      // 主控单位移动
```

### 3. CommandContext - 上下文管理

提供上下文感知的命令创建，自动设置Player和Flag：

```csharp
// 创建不同类型的上下文
var userContext = CommandContext.ForUser(player);     // 用户操作
var aiContext = CommandContext.ForAI(aiPlayer);       // AI操作
var systemContext = CommandContext.ForSystem();       // 系统操作

// 使用上下文
userContext.CastAbility(abilityLink, target).Execute(unit);
```

## 📖 基础用法

### 技能释放

```csharp
// 方式1：最简单的扩展方法
var result = playerUnit.CastAbility(ScopeData.Ability.Fireball, enemyUnit);

// 方式2：使用构建器自定义
var result = CommandBuilder.Create()
    .WithIndex(CommandIndex.Execute)
    .WithType(ComponentTagEx.AbilityManager)
    .WithAbility(ScopeData.Ability.Fireball)
    .ToUnit(enemyUnit)
    .AsUser()
    .Execute(playerUnit);

// 方式3：使用上下文
var userContext = CommandContext.ForUser();
var result = userContext.CastAbility(ScopeData.Ability.Fireball, enemyUnit)
    .Execute(playerUnit);
```

### 单位移动

```csharp
// 向量移动（按角度）
unit.MoveInDirection(45f);                    // 45度方向
unit.MoveInDirection(new Angle(90f));         // 90度方向

// 移动到指定位置
var targetPosition = new ScenePoint(new Vector3(100, 200, 0), scene);
unit.MoveTo(targetPosition);

// 停止移动
unit.StopMovement();
unit.Stop();  // 完全停止（包括所有动作）
```

### 攻击指令

```csharp
// 攻击指定目标
unit.AttackTarget(enemyUnit);

// 使用AI上下文攻击
var aiContext = CommandContext.ForAI(aiPlayer);
aiContext.AttackTarget(enemyUnit).Execute(aiUnit);
```

### Toggle技能操作

```csharp
// 智能切换技能状态（开启→关闭，关闭→开启）
unit.ToggleAbility(ScopeData.Ability.ManaShield);

// 明确开启技能
unit.TurnOnAbility(ScopeData.Ability.ManaShield);

// 明确关闭技能
unit.TurnOffAbility(ScopeData.Ability.ManaShield);

// 使用CommandBuilder进行更精细控制
var result = CommandBuilder.Create()
    .WithToggle(ScopeData.Ability.ManaShield)
    .AsAI(aiPlayer)
    .Execute(unit);
```

### 物品操作

```csharp
// 拾取物品
unit.PickUpItem(groundItem);

// 使用物品（消耗品、装备等）
unit.UseItem(healthPotion);

// 丢弃物品
unit.DropItem(oldWeapon);

// 使用CommandBuilder进行复杂物品操作
var pickupResult = CommandBuilder.Create()
    .WithPickUp(rareItem)
    .AsUser()  // 作为用户操作
    .Execute(unit);

// 批量物品操作示例
var items = GetInventoryItems();
foreach (var item in items.Where(i => i.IsExpired))
{
    unit.DropItem(item, asUser: false, systemPlayer);
}
```

### 错误处理

```csharp
var result = unit.CastAbility(abilityLink, target);
if (!result.IsSuccess)
{
    switch (result.Error)
    {
        case CmdError.CannotFindCaster:
            Game.Logger.LogWarning("找不到施法者");
            break;
        case CmdError.AbilityNotFound:
            Game.Logger.LogWarning("技能不存在");
            break;
        default:
            Game.Logger.LogError("命令执行失败: {Error}", result.Error);
            break;
    }
}
```

## 🚀 高级用法

### 绑定上下文

避免重复传递entity参数：

```csharp
// 为特定实体绑定用户上下文
var boundUser = playerUnit.AsUser();
boundUser.CastAbility(ScopeData.Ability.Fireball, enemyUnit);
boundUser.MoveInDirection(45f);
boundUser.AttackTarget(enemyUnit);

// 为AI单位绑定AI上下文
var boundAI = aiUnit.AsAI(aiPlayer);
boundAI.CastAbility(ScopeData.Ability.Lightning, target);
boundAI.MoveTo(patrolPoint);
```

### 批量操作

```csharp
// 为多个AI单位下达相同命令
var aiContext = CommandContext.ForAI(aiPlayer);
var targetPosition = GetPatrolPoint();

foreach (var unit in aiUnits)
{
    aiContext.MoveTo(targetPosition).Execute(unit);
}

// 或使用并行处理
Parallel.ForEach(aiUnits, unit => 
{
    aiContext.MoveTo(targetPosition).Execute(unit);
});
```

### 条件构建

```csharp
var builder = CommandBuilder.Create()
    .WithIndex(CommandIndex.Execute)
    .WithType(ComponentTagEx.AbilityManager)
    .WithAbility(abilityLink);

// 根据条件设置目标
if (hasTarget)
{
    builder.ToUnit(target);
}
else if (hasPosition)
{
    builder.ToPosition(fallbackPosition);
}
else
{
    builder.ToAngle(facing);
}

var result = builder.AsUser().Execute(unit);
```

### 测试模式

```csharp
// 测试命令是否可以执行，但不实际执行
var result = unit.CastAbility(abilityLink, target, testOnly: true);
if (result.IsSuccess)
{
    // 可以执行，现在真正执行
    unit.CastAbility(abilityLink, target);
}
```

## ⚡ 服务端优化

### Ability实例直接使用

在服务端，CommandBuilder支持直接使用Ability实例，跳过AbilityLink查找过程：

```csharp
#if SERVER
// 客户端API - 使用AbilityLink
var result = unit.CastAbility(ScopeData.Ability.Fireball, target);

// 服务端优化API - 直接使用Ability实例（相同的方法名！）
var abilityInstance = GetFireballAbilityInstance();
var result = unit.CastAbility(abilityInstance, target);

// Toggle技能的服务端优化
var toggleAbility = GetManaShieldAbilityInstance();
unit.ToggleAbility(toggleAbility);        // 智能切换
unit.TurnOnAbility(toggleAbility);        // 开启
unit.TurnOffAbility(toggleAbility);       // 关闭

// CommandBuilder也支持Ability实例
var builderResult = CommandBuilder.Create()
    .WithToggle(toggleAbility)
    .AsAI(aiPlayer)
    .Execute(unit);
#endif
```

### 自动性能优化

CommandBuilder在服务端自动检测并优化执行路径：

```csharp
#if SERVER
// 内部实现（用户透明）
public CmdResult Execute(Entity unit, bool testOnly = false)
{
    // 如果是AbilityManager类型且有Ability实例，使用优化路径
    if (_command.Type == ComponentTagEx.AbilityManager && _abilityInstance != null)
    {
        return ExecuteWithAbilityInstance(unit, testOnly);
    }
    
    // 否则使用标准路径
    return _command.IssueOrder(unit, testOnly);
}
#endif
```

### 服务端专用重载

所有扩展方法都提供服务端专用的Ability实例重载：

```csharp
#if SERVER
// Entity扩展方法
public static CmdResult CastAbility(this Entity unit, Ability ability, ...)
public static CmdResult CastAbility(this Entity unit, IGameLink<GameDataAbility> abilityLink, ...)

// Toggle技能扩展方法
public static CmdResult ToggleAbility(this Entity unit, Ability ability, ...)
public static CmdResult ToggleAbility(this Entity unit, IGameLink<GameDataAbility> abilityLink, ...)
public static CmdResult TurnOnAbility(this Entity unit, Ability ability, ...)
public static CmdResult TurnOnAbility(this Entity unit, IGameLink<GameDataAbility> abilityLink, ...)
public static CmdResult TurnOffAbility(this Entity unit, Ability ability, ...)
public static CmdResult TurnOffAbility(this Entity unit, IGameLink<GameDataAbility> abilityLink, ...)

// 物品操作扩展方法（无需服务端专用重载，因为物品实例已经在运行时）
public static CmdResult PickUpItem(this Entity unit, IPickUpItem item, ...)
public static CmdResult DropItem(this Entity unit, ItemPickable item, ...)
public static CmdResult UseItem(this Entity unit, Item item, ...)

// Player扩展方法  
public static CmdResult CastAbilityWithMainUnit(this Player player, Ability ability, ...)
public static CmdResult CastAbilityWithMainUnit(this Player player, IGameLink<GameDataAbility> abilityLink, ...)

// CommandBuilder方法
public static CommandBuilder WithToggle(Ability ability)
public static CommandBuilder WithToggle(IGameLink<GameDataAbility> abilityLink)
public static CommandBuilder WithTurnOn(Ability ability)
public static CommandBuilder WithTurnOff(Ability ability)
public static CommandBuilder WithPickUp(IPickUpItem item)
public static CommandBuilder WithDrop(ItemPickable item)
public static CommandBuilder WithUse(Item item)
#endif
```

### 性能对比

```csharp
#if SERVER
// 传统方式：需要查找AbilityLink
// 1. 解析AbilityLink → 2. 查找Ability实例 → 3. 创建Order → 4. 执行
var result1 = unit.CastAbility(ScopeData.Ability.Fireball, target);

// 优化方式：直接使用Ability实例
// 1. 直接使用实例 → 2. 创建Order → 3. 执行
var fireballAbility = GetCachedAbility();
var result2 = unit.CastAbility(fireballAbility, target);  // 性能提升 ~20-30%
#endif
```

## 💡 实际示例

### 玩家控制器

```csharp
#if CLIENT
/// <summary>
/// 客户端玩家控制器
/// </summary>
public class PlayerController
{
    private Unit? playerUnit;
    private readonly Dictionary<KeyCode, IGameLink<GameDataAbility>> skillBindings;
    
    public PlayerController()
    {
        skillBindings = new Dictionary<KeyCode, IGameLink<GameDataAbility>>
        {
            { KeyCode.Q, ScopeData.Ability.Fireball },
            { KeyCode.W, ScopeData.Ability.Lightning },
            { KeyCode.E, ScopeData.Ability.Heal }
        };
    }
    
    public void OnKeyPressed(KeyCode key)
    {
        if (playerUnit == null) return;
        
        // 技能释放
        if (skillBindings.TryGetValue(key, out var abilityLink))
        {
            var target = GetMouseTarget();
            var result = playerUnit.CastAbility(abilityLink, target);
            
            if (!result.IsSuccess)
            {
                ShowErrorMessage($"技能释放失败: {result.Error}");
            }
        }
    }
    
    public void OnJoystickInput(Vector2 direction)
    {
        if (playerUnit == null) return;
        
        if (direction.Length() < 0.1f)
        {
            // 停止移动
            playerUnit.StopMovement();
        }
        else
        {
            // 向指定方向移动
            var angle = Math.Atan2(direction.Y, direction.X) * (180.0 / Math.PI);
            playerUnit.MoveInDirection((float)angle);
        }
    }
    
    public void OnRightClick(Vector3 worldPosition)
    {
        if (playerUnit == null) return;
        
        var scenePoint = new ScenePoint(worldPosition, playerUnit.Scene);
        var result = playerUnit.MoveTo(scenePoint);
        
        if (!result.IsSuccess)
        {
            Game.Logger.LogWarning("移动指令失败: {Error}", result.Error);
        }
    }
}
#endif
```

### AI控制器

```csharp
#if SERVER
/// <summary>
/// 服务端AI控制器，使用便利API实现智能行为
/// </summary>
public class SimpleAI
{
    private readonly Unit aiUnit;
    private readonly CommandContext aiContext;
    private readonly List<ScenePoint> patrolPoints;
    private int currentPatrolIndex = 0;
    
    // 缓存的技能实例（服务端优化）
    private readonly Ability attackAbility;
    private readonly Ability healAbility;
    
    public SimpleAI(Unit unit, Player aiPlayer, List<ScenePoint> patrol)
    {
        aiUnit = unit;
        aiContext = CommandContext.ForAI(aiPlayer);
        patrolPoints = patrol;
        
        // 服务端：直接获取Ability实例以获得更好性能
        var abilityManager = unit.GetComponent<AbilityManager>();
        attackAbility = abilityManager?.Get<AbilityExecute>(a => a.Link == ScopeData.Ability.Lightning).FirstOrDefault();
        healAbility = abilityManager?.Get<AbilityExecute>(a => a.Link == ScopeData.Ability.Heal).FirstOrDefault();
    }
    
    public async Task RunBehavior()
    {
        while (aiUnit.IsValid)
        {
            var enemy = FindNearestEnemy();
            
            if (enemy != null)
            {
                await CombatBehavior(enemy);
            }
            else
            {
                await PatrolBehavior();
            }
            
            await Game.Delay(TimeSpan.FromSeconds(1));
        }
    }
    
    private async Task CombatBehavior(Unit enemy)
    {
        var distanceToEnemy = Vector3.Distance(aiUnit.Position, enemy.Position);
        
        // 检查是否需要治疗
        if (aiUnit.Health.Ratio < 0.3f && healAbility != null)
        {
            // 使用缓存的Ability实例（服务端优化）
            var healResult = aiUnit.CastAbility(healAbility, aiUnit, asUser: false, aiContext.Player);
            Game.Logger.LogInformation("AI自我治疗: {Success}", healResult.IsSuccess);
        }
        
        // 攻击行为
        if (distanceToEnemy <= 10f && attackAbility != null)
        {
            // 直接使用Ability实例，避免Link查找
            var attackResult = aiUnit.CastAbility(attackAbility, enemy, asUser: false, aiContext.Player);
            
            if (!attackResult.IsSuccess)
            {
                Game.Logger.LogWarning("AI攻击失败: {Error}", attackResult.Error);
                
                // 攻击失败，尝试移动到敌人位置
                aiContext.MoveTo(enemy.Position).Execute(aiUnit);
            }
        }
        else
        {
            // 距离太远，移动靠近
            aiContext.MoveTo(enemy.Position).Execute(aiUnit);
        }
    }
    
    private async Task PatrolBehavior()
    {
        if (patrolPoints.Count == 0) return;
        
        var targetPoint = patrolPoints[currentPatrolIndex];
        var distance = Vector3.Distance(aiUnit.Position, targetPoint.Position);
        
        if (distance < 2f)
        {
            // 到达巡逻点，切换到下一个
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            targetPoint = patrolPoints[currentPatrolIndex];
        }
        
        // 移动到巡逻点
        var moveResult = aiContext.MoveTo(targetPoint).Execute(aiUnit);
        Game.Logger.LogDebug("AI巡逻移动: {Success}", moveResult.IsSuccess);
    }
    
    private Unit? FindNearestEnemy()
    {
        // 查找最近的敌人
        return Game.Instance.GetList<Unit>()
            .Where(u => u.IsValid && u.Player != aiUnit.Player)
            .OrderBy(u => Vector3.Distance(aiUnit.Position, u.Position))
            .FirstOrDefault();
    }
}
#endif
```

### 技能连击系统

```csharp
#if SERVER
/// <summary>
/// 技能连击系统，展示复杂的技能组合使用
/// </summary>
public class ComboSystem
{
    private readonly Unit caster;
    private readonly CommandContext context;
    
    // 缓存技能实例（服务端优化）
    private readonly Dictionary<string, Ability> cachedAbilities;
    
    public ComboSystem(Unit unit, Player player)
    {
        caster = unit;
        context = CommandContext.ForAI(player);
        cachedAbilities = new Dictionary<string, Ability>();
        
        // 预缓存常用技能
        var abilityManager = unit.GetComponent<AbilityManager>();
        if (abilityManager != null)
        {
            foreach (var ability in abilityManager.Get<AbilityExecute>())
            {
                if (ability.Link != null)
                {
                    cachedAbilities[ability.Link.ToString()] = ability;
                }
            }
        }
    }
    
    /// <summary>
    /// 执行火焰连击：火球术 → 闪电 → 治疗
    /// </summary>
    public async Task<bool> ExecuteFireCombo(Unit target)
    {
        try
        {
            // 第一步：火球术
            var fireball = GetCachedAbility("Fireball");
            if (fireball != null)
            {
                var result1 = caster.CastAbility(fireball, target, asUser: false, context.Player);
                if (!result1.IsSuccess)
                {
                    Game.Logger.LogWarning("连击第一步失败: {Error}", result1.Error);
                    return false;
                }
                
                await Game.Delay(TimeSpan.FromSeconds(1)); // 等待技能动画
            }
            
            // 第二步：闪电术
            var lightning = GetCachedAbility("Lightning");
            if (lightning != null)
            {
                var result2 = caster.CastAbility(lightning, target, asUser: false, context.Player);
                if (!result2.IsSuccess)
                {
                    Game.Logger.LogWarning("连击第二步失败: {Error}", result2.Error);
                    return false;
                }
                
                await Game.Delay(TimeSpan.FromSeconds(0.5f));
            }
            
            // 第三步：自我治疗
            var heal = GetCachedAbility("Heal");
            if (heal != null && caster.Health.Ratio < 0.8f)
            {
                var result3 = caster.CastAbility(heal, caster, asUser: false, context.Player);
                Game.Logger.LogInformation("连击治疗: {Success}", result3.IsSuccess);
            }
            
            Game.Logger.LogInformation("火焰连击执行完成");
            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "执行连击时发生异常");
            return false;
        }
    }
    
    /// <summary>
    /// 获取缓存的技能实例
    /// </summary>
    private Ability? GetCachedAbility(string name)
    {
        return cachedAbilities.TryGetValue(name, out var ability) ? ability : null;
    }
    
    /// <summary>
    /// 条件性技能释放
    /// </summary>
    public bool TryCastAbilityWithConditions(string abilityName, Unit target, Func<bool> condition)
    {
        if (!condition())
        {
            return false;
        }
        
        var ability = GetCachedAbility(abilityName);
        if (ability == null)
        {
            return false;
        }
        
        var result = caster.CastAbility(ability, target, asUser: false, context.Player);
        return result.IsSuccess;
    }
}
#endif
```

## ✅ 最佳实践

### 1. 选择合适的API层级

```csharp
// ✅ 简单场景 - 使用扩展方法
unit.CastAbility(abilityLink, target);

// ✅ 需要自定义配置 - 使用构建器
CommandBuilder.Create()
    .WithAbility(abilityLink)
    .WithTarget(target)
    .AsUser()
    .Execute(unit);

// ✅ 批量操作 - 使用上下文
var context = CommandContext.ForAI(aiPlayer);
foreach (var unit in units)
{
    context.CastAbility(abilityLink).Execute(unit);
}

// ✅ 频繁操作同一实体 - 使用绑定上下文
var bound = unit.AsUser();
bound.CastAbility(skill1);
bound.MoveInDirection(45f);
bound.AttackTarget(enemy);
```

### 2. 错误处理策略

```csharp
// ✅ 始终检查命令执行结果
var result = unit.CastAbility(abilityLink, target);
if (!result.IsSuccess)
{
    // 根据错误类型采取不同策略
    switch (result.Error)
    {
        case CmdError.AbilityNotFound:
            Game.Logger.LogError("技能不存在: {AbilityLink}", abilityLink);
            break;
        case CmdError.CannotFindCaster:
            Game.Logger.LogWarning("找不到施法者");
            break;
        case CmdError.OutOfRange:
            // 尝试移动到范围内
            unit.MoveTo(target.Position);
            break;
        default:
            Game.Logger.LogWarning("命令执行失败: {Error}", result.Error);
            break;
    }
}

// ✅ 使用测试模式预检查
if (unit.CastAbility(abilityLink, target, testOnly: true).IsSuccess)
{
    unit.CastAbility(abilityLink, target);
}
```

### 3. 上下文选择指南

```csharp
// ✅ 客户端用户操作
#if CLIENT
unit.CastAbility(abilityLink); // 自动使用IsUser标志
#endif

// ✅ 服务端AI操作
#if SERVER
var aiContext = CommandContext.ForAI(aiPlayer);
aiContext.CastAbility(abilityLink).Execute(unit);
#endif

// ✅ 服务端系统操作（绕过权限检查）
#if SERVER
var systemContext = CommandContext.ForSystem();
systemContext.MoveTo(position).Execute(unit);
#endif

// ❌ 避免在客户端使用AI/System上下文
#if CLIENT
// var aiContext = CommandContext.ForAI(player); // 这是错误的！
#endif
```

### 4. 性能优化建议

```csharp
// ✅ 复用上下文对象
var aiContext = CommandContext.ForAI(aiPlayer);
var boundContext = unit.AsAI(aiPlayer);

// ✅ 服务端使用Ability实例缓存
#if SERVER
private readonly Dictionary<string, Ability> abilityCache = new();

private Ability? GetCachedAbility(string name)
{
    if (!abilityCache.TryGetValue(name, out var ability))
    {
        ability = FindAbilityByName(name);
        if (ability != null)
        {
            abilityCache[name] = ability;
        }
    }
    return ability;
}

// 使用缓存的实例
var fireballAbility = GetCachedAbility("Fireball");
if (fireballAbility != null)
{
    unit.CastAbility(fireballAbility, target); // 比使用Link快20-30%
}
#endif

// ❌ 避免在循环中重复创建上下文
for (int i = 0; i < 1000; i++)
{
    // var context = CommandContext.ForAI(aiPlayer); // 性能浪费
}
```

### 5. 并发安全

```csharp
// ✅ CommandBuilder是不可变的，可以安全并发使用
var baseBuilder = CommandBuilder.Create()
    .WithIndex(CommandIndex.Execute)
    .WithType(ComponentTagEx.AbilityManager);

// 多个线程可以安全地基于baseBuilder创建不同的命令
Parallel.ForEach(targets, target =>
{
    var command = baseBuilder
        .WithAbility(GetAbilityForTarget(target))
        .ToUnit(target)
        .AsSystem()
        .Execute(caster);
});

// ✅ 上下文对象是线程安全的
var aiContext = CommandContext.ForAI(aiPlayer);
Parallel.ForEach(aiUnits, unit =>
{
    aiContext.MoveTo(GetPatrolPoint()).Execute(unit);
});
```

### 6. 调试和日志

```csharp
// ✅ 使用详细的日志记录
var result = unit.CastAbility(abilityLink, target);
Game.Logger.LogDebug("技能释放: {AbilityLink} -> {Target}, 结果: {Success}, 错误: {Error}", 
    abilityLink, target?.Id, result.IsSuccess, result.Error);

// ✅ 使用测试模式进行调试
#if DEBUG
// 在调试模式下，先测试命令是否可执行
var testResult = unit.CastAbility(abilityLink, target, testOnly: true);
if (!testResult.IsSuccess)
{
    Game.Logger.LogWarning("命令测试失败: {Error}", testResult.Error);
}
#endif

// ✅ 记录性能指标
#if SERVER
var stopwatch = Stopwatch.StartNew();
var result = unit.CastAbility(abilityInstance, target); // 使用优化路径
stopwatch.Stop();
Game.Logger.LogDebug("技能执行耗时: {Elapsed}ms", stopwatch.ElapsedMilliseconds);
#endif
```

## 🔗 相关系统

### 依赖关系

便利API构建在以下核心系统之上：

- 📦 **[指令系统](OrderSystem.md)** - 底层Command和Order机制
- ⚡ **[技能系统](AbilitySystem.md)** - 技能释放和管理
- 👤 **[玩家系统](PlayerSystem.md)** - 玩家权限和控制
- 🎭 **[单位系统](UnitSystem.md)** - 单位属性和行为

### 集成指南

```csharp
// 与技能系统集成
var abilityManager = unit.GetComponent<AbilityManager>();
var abilities = abilityManager.Get<AbilityExecute>();

// 与单位系统集成
var unit = Player.LocalPlayer.MainUnit;
unit?.CastAbility(abilityLink);

// 与消息系统集成
Game.TypedMessageBus.Subscribe<PlayerInputMessage>(msg =>
{
    if (msg.Type == InputType.Skill)
    {
        unit.CastAbility(msg.AbilityLink, msg.Target);
    }
});
```

### 扩展开发

如需为便利API添加新的命令类型：

1. **扩展CommandBuilder** - 添加新的With方法
2. **扩展CommandExtensions** - 添加新的扩展方法
3. **更新CommandContext** - 支持新的上下文方法
4. **添加单元测试** - 确保功能正确性

```csharp
// 示例：添加物品使用支持
public static class ItemCommandExtensions
{
    public static CmdResult UseItem(this Entity unit, IGameLink<GameDataItem> itemLink, ICommandTarget? target = null)
    {
        return CommandBuilder.Create()
            .WithIndex(CommandIndex.Execute)
            .WithType(ComponentTagEx.ItemManager)
            .WithItem(itemLink)
            .WithTarget(target)
            .AsUser()
            .Execute(unit);
    }
}
```

---

> 💡 **提示**: 便利API是对底层Command系统的高级封装，建议开发者优先使用便利API，仅在需要特殊定制时才直接使用底层Command结构。

> 🔧 **开发建议**: 在实际项目中，可以根据具体需求创建项目特定的扩展方法，进一步简化常用操作。