# 🎯 单位系统（Unit System）

单位系统是 WasiCore 游戏框架的核心组件之一，为游戏中的角色、NPC、生物和其他可交互实体提供完整的管理和同步机制。

## 📋 目录

- [🏗️ 系统概述](#系统概述)
- [🎮 基本用法](#基本用法)
  - [🛡️ 安全创建的重要性](#安全创建的重要性)
  - [🔄 异步操作示例](#异步操作示例)
- [👁️ 可见性与视野系统](#可见性与视野系统)
- [🔄 客户端同步机制](#客户端同步机制)
- [👥 队伍视野共享](#队伍视野共享)
- [🏃‍♂️ 移动与同步](#移动与同步)
  - [🎯 移动机制概述](#移动机制概述)
  - [🎮 指令系统移动](#指令系统移动)
  - [🔧 直接移动方法](#直接移动方法)
  - [🎯 实际应用场景](#实际应用场景)
  - [🔄 移动同步机制](#移动同步机制)
  - [🎮 移动控制方法](#移动控制方法)
  - [🚫 移动限制与状态](#移动限制与状态)
- [⚙️ 高级配置](#高级配置)
- [🔧 API 参考](#api-参考)

## 🏗️系统概述

### 核心架构

单位系统基于以下核心类构建：

- **`Unit`** - 单位实体的核心类，继承自 `Entity`
- **`GameDataUnit`** - 单位的数据定义和配置
- **`Player`** - 玩家对象，管理单位的所有权和视野
- **`Team`** - 队伍系统，管理玩家分组和视野共享

### 主要特性

- ✅ **属性系统** - 生命值、法力值、移动速度等复杂属性管理
- ✅ **状态管理** - 单位状态的添加、移除和查询
- ✅ **视野系统** - 基于视野范围的可见性控制
- ✅ **生命周期** - 创建、死亡、复活、销毁的完整生命周期
- ✅ **能力系统** - 技能和法术的集成
- ✅ **AI 集成** - NPC 行为和决策系统
- ✅ **网络同步** - 服务端权威的客户端同步

## 🎮 基本用法

### 创建单位

```csharp
// 通过游戏数编Link创建单位
var unitLink = new GameLink<GameDataUnit>("MyUnitType");
var player = Player.GetById(1);
var position = new ScenePoint(100, 200, scene);
var facing = new Angle(0); // 面向右方

// 推荐方式：通过GameDataUnit.CreateUnit()方法创建（安全）
#if SERVER
var unit = unitLink.Data?.CreateUnit(player, position, facing);
if (unit == null)
{
    Game.Logger.LogError("单位创建失败: {UnitType}", unitLink.FriendlyName);
    return;
}
#endif

// 不推荐：直接使用构造函数（可能抛出异常）
#if SERVER
// var unit = new Unit(unitLink, player, position, facing); // 可能抛出异常而使逻辑意外中断。
#endif
```

### 🏭 工厂模式与继承

#### 数编表作为工厂

在 WasiCore 框架中，`GameDataUnit` 不仅是数据配置表，更重要的是充当了 `Unit` 的**工厂类**：

```csharp
// GameDataUnit 作为 Unit 的工厂
public virtual Unit? CreateUnit(Player player, ScenePoint scenePoint, Angle facing, 
                               IExecutionContext? creationContext = null, bool useDefaultAI = false)
{
    // 内部直接构造 Unit 实例
    ScopeScript.LastCreatedUnit = new Unit(Link, player, scenePoint, facing) 
    { 
        CreationContext = creationContext 
    };
    
    return ScopeScript.LastCreatedUnit;
}
```

#### ⚠️ 继承的重要原则

**当需要继承 Unit 类时，必须同时继承 GameDataUnit 并重载其 CreateUnit 方法。**

这是因为 `GameDataUnit.CreateUnit` 内部直接构造的是 `Unit` 类：

```csharp
// 如果只继承 Unit 但不重载 GameDataUnit.CreateUnit，
// 创建出来的仍然是 Unit 而不是自定义的子类

// ❌ 错误的继承方式
public class Hero : Unit
{
    // 只继承了 Unit，但没有对应的 GameDataHero
}

// 使用时创建的仍然是 Unit，无法获得 Hero 的特性
var hero = unitLink.Data?.CreateUnit(player, position, facing); // 返回 Unit，不是 Hero
```

#### ✅ 正确的继承模式

**步骤1：继承 Unit 类**

```csharp
public class Hero : Unit
{
    public Hero(IGameLink<GameDataUnit> link, Player player, ScenePoint scenePoint, Angle facing) 
        : base(link, player, scenePoint, facing)
    {
        InitializeHeroFeatures();
    }
    
    // 英雄特有的属性
    public int HeroLevel { get; set; }
    public List<Skill> UltimateSkills { get; set; } = new();
    
    // 英雄特有的方法
    public void LevelUp()
    {
        HeroLevel++;
        // 升级逻辑
    }
    
    private void InitializeHeroFeatures()
    {
        // 英雄特有的初始化逻辑
    }
}
```

**步骤2：继承 GameDataUnit 并重载 CreateUnit**

```csharp
public class GameDataHero : GameDataUnit
{
    // 英雄特有的数据配置
    public int BaseLevel { get; set; } = 1;
    public List<IGameLink<GameDataAbility>> UltimateAbilities { get; set; } = new();
    
    // 重载 CreateUnit 方法，创建 Hero 而不是 Unit
    public override Unit? CreateUnit(Player player, ScenePoint scenePoint, Angle facing, 
                                   IExecutionContext? creationContext = null, bool useDefaultAI = false)
    {
        try
        {
            // 创建 Hero 实例
            var hero = new Hero(Link, player, scenePoint, facing) 
            { 
                CreationContext = creationContext,
                HeroLevel = BaseLevel
            };
            
            // 英雄特有的初始化
            InitializeHeroSpecificFeatures(hero);
            
            ScopeScript.LastCreatedUnit = hero;
            return hero;
        }
        catch (Exception e)
        {
            Game.Logger.LogError(e, "Failed to create hero at {scenePoint}", scenePoint);
            return null;
        }
    }
    
    private void InitializeHeroSpecificFeatures(Hero hero)
    {
        // 添加英雄专属技能
        foreach (var abilityLink in UltimateAbilities)
        {
            if (abilityLink?.Data != null)
            {
                var ability = abilityLink.Data.CreateAbility(hero);
                hero.UltimateSkills.Add(ability);
            }
        }
    }
}
```

**步骤3：正确使用**

```csharp
// 创建数编表时使用 GameDataHero
new GameDataHero(ScopeData.Unit.TestHero)
{
    Name = "测试英雄",
    BaseLevel = 1,
    Properties = new() 
    {
        { ScopeData.UnitProperty.LifeMax, 1500 },
        { ScopeData.UnitProperty.AttackDamage, 120 }
    },
    UltimateAbilities = 
    {
        ScopeData.Ability.HeroicStrike,
        ScopeData.Ability.BattleRoar
    }
};

// 使用时可以正确创建 Hero 实例
var heroData = ScopeData.Unit.TestHero.Data as GameDataHero;
var hero = heroData?.CreateUnit(player, position, facing) as Hero;

if (hero != null)
{
    hero.LevelUp(); // 可以使用 Hero 特有的方法
    Console.WriteLine($"创建了 {hero.HeroLevel} 级英雄");
}
```

#### 🔄 其他组件的工厂模式

框架中其他组件也遵循相同的工厂模式：

```csharp
// 技能工厂
public partial class GameDataAbility
{
    public virtual Ability CreateAbility(Unit owner, Item? item = null);
}

// 物品工厂
public abstract class GameDataItem
{
    protected abstract Item CreateItem(Unit unit);
}

// 物品栏工厂
public class GameDataInventory
{
    public virtual Inventory CreateInventory(Unit unit)
    {
        return new Inventory(unit, Link);
    }
}
```

> 📖 **深入了解**：关于实体-组件-数据模式的完整指南，请参阅 [实体-组件-数据模式指南](../guides/EntityComponentDataPattern.md)

### 🛡️ 安全创建的重要性

使用 `GameDataUnit.CreateUnit()` 方法而非直接构造函数的原因：

#### ⚠️ 可能的创建失败原因

1. **数编Link无效** - 数编Link没有对应的GameDataUnit数据
2. **坐标超出范围** - 单位坐标超出场景有效范围
3. **场景未加载** - 目标场景尚未完成加载
4. **资源不足** - 内存或其他资源不足
5. **数据配置错误** - GameDataUnit配置存在问题

#### 🔒 安全创建模式

```csharp
#if SERVER
// ✅ 推荐：安全创建模式
var unitData = unitLink.Data;
if (unitData == null)
{
    Game.Logger.LogError("数编Link无效: {LinkName}", unitLink.FriendlyName);
    return null;
}

var unit = unitData.CreateUnit(player, position, facing);
if (unit == null)
{
    Game.Logger.LogError("单位创建失败: 位置={Position}, 玩家={Player}", position, player.Id);
    return null;
}

Game.Logger.LogInfo("单位创建成功: {UnitName} at {Position}", unit.Cache.FriendlyName, position);
#endif
```

#### ❌ 危险的直接构造

```csharp
#if SERVER
// ❌ 不推荐：可能抛出异常，若未捕获则会导致逻辑意外中断。
try
{
    var unit = new Unit(unitLink, player, position, facing);
}
catch (ArgumentException ex)
{
    // 数编Link无效或数据问题
    Game.Logger.LogError("单位创建异常: {Exception}", ex.Message);
}
catch (InvalidOperationException ex)
{
    // 场景或坐标问题
    Game.Logger.LogError("单位创建异常: {Exception}", ex.Message);
}
#endif
```

### 基本属性访问

```csharp
// 获取单位基本信息
var health = unit.GetProperty<float>(PropertyUnit.Health);
var isAlive = unit.IsAlive;
var isDead = unit.IsDead;
var position = unit.Position;
var facing = unit.Facing;
var owner = unit.Player;

// 设置单位属性（仅服务端）
#if SERVER
unit.SetProperty(PropertyUnit.Health, 100f);
unit.SetProperty(PropertyUnit.Level, 5);
#endif
```

### 单位状态管理

```csharp
// 添加状态
#if SERVER
unit.AddState(UnitState.Invulnerable);
unit.AddState(UnitState.Stunned);
#endif

// 检查状态
if (unit.HasState(UnitState.Dead))
{
    // 单位已死亡
}

// 移除状态
#if SERVER
unit.RemoveState(UnitState.Stunned);
#endif
```

### 生命周期管理

```csharp
// 杀死单位
#if SERVER
unit.Kill(DeathType.Normal);
#endif

// 复活单位
#if SERVER
if (unit.IsDead)
{
    unit.Revive();
}
#endif

// 销毁单位
unit.Destroy();
```

### 异步操作示例

在 WebAssembly 环境中，必须使用框架提供的异步方法：

```csharp
#if SERVER
// ✅ 推荐：使用Game.Delay进行延迟操作
public async Task DelayedHeal(Unit unit, float healAmount)
{
    // 播放治疗动画
    unit.PlayAnimation("HealStart");
    
    // 等待施法时间（与游戏tick对齐）
    await Game.Delay(TimeSpan.FromSeconds(2.0f));
    
    // 检查单位是否仍然有效
    if (unit.IsAlive)
    {
        unit.Heal(healAmount);
        Game.Logger.LogInfo("单位治疗完成: {Unit}", unit.ToString());
    }
}

// ✅ 推荐：渐进式效果
public async Task ApplyBuffOverTime(Unit unit, UnitState buffState, TimeSpan duration)
{
    unit.AddState(buffState);
    
    // 等待buff持续时间
    await Game.Delay(duration);
    
    // 移除buff（如果单位仍然有效）
    if (unit.IsValid)
    {
        unit.RemoveState(buffState);
    }
}

// ❌ 避免：使用Task.Delay（在Wasm中可能异常）
public async Task WrongDelayedOperation(Unit unit)
{
    await Task.Delay(1000); // 可能导致异常或不同步
    unit.DoSomething();
}
#endif
```

## 👁️ 可见性与视野系统

### 🔍 视野机制

每个单位都拥有独立的视野系统，默认情况下：

- **单位视野范围** - 由 `Sight` 属性控制
- **默认可见性** - 单位对其所有者玩家始终可见
- **视野阻挡** - 地形和其他单位可能阻挡视野
- **草丛视野** - 草丛是一种特殊的视野阻挡。当单位在草丛内部时，这个草丛对它没有任何影响。当单位在草丛外部时，草丛视为正常的视野阻挡

```csharp
// 获取单位视野范围
var sightRange = unit.GetPropertyComplex(ScopeData.UnitProperty.Sight);

// 检查可见性
var canSee = unit.CanBeSeen(otherUnit);
var isVisible = unit.IsVisibleTo(somePlayer);
```

### 🎯 可见性规则

#### 服务端可见性判定

```csharp
#if SERVER
// 检查单位是否对特定玩家可见
public bool IsVisibleTo(Player player)
{
    return _viewActor.IsVisibleTo(player.Id);
}
#endif
```

#### 客户端可见性规则

```csharp
#if CLIENT
// 客户端可见性基于玩家关系
public bool CanBeSeen(Entity caster)
{
    return IsValid && 
        (caster.Player.GetRelationShip(Player) >= PlayerRelationShip.Ally || 
         caster.Player == Player.LocalPlayer);
}
#endif
```

### 📊 可见性等级

| 关系类型 | 可见性 | 说明 |
|----------|--------|------|
| `Player` | 完全可见 | 自己的单位 |
| `Ally` | 完全可见 | 盟友单位 |
| `Enemy` | 视野内可见 | 敌方单位需要在视野范围内 |
| `Neutral` | 视野内可见 | 中立单位需要在视野范围内 |

## 🔄 客户端同步机制

### 🌐 同步原理

WasiCore 采用服务端权威的同步机制：

1. **服务端控制** - 所有单位逻辑在服务端执行
2. **选择性同步** - 客户端只接收视野内的单位数据
3. **实时更新** - 单位进入/离开视野时动态同步
4. **属性同步** - 位置、状态、属性变化实时同步

### 📡 同步类型

```csharp
// 设置同步类型
#if SERVER
unit.SetSyncType(SyncType.Sight);  // 仅视野内同步
unit.SetSyncType(SyncType.All);    // 全局同步
unit.SetSyncType(SyncType.Ally);   // 盟友同步
#endif
```

#### 同步类型说明

| 同步类型 | 描述 | 使用场景 |
|----------|------|----------|
| `None` | 不同步 | 服务端专用单位 |
| `Self` | 仅所有者 | 个人信息 |
| `Ally` | 盟友可见 | 团队单位 |
| `All` | 全局可见 | 重要单位 |
| `Sight` | 视野内同步 | 普通单位（默认） |
| `SelfOrSight` | 所有者或视野内 | 特殊单位 |
| `AllyOrSight` | 盟友或视野内 | 团队特殊单位 |

### 🎭 客户端单位获取与操作

#### 📍 获取Unit的方法

**1. 通过ID直接获取**
```csharp
#if CLIENT
// 通过EntityId获取
Unit? unit = Entity.GetById(entityId) as Unit;
if (unit != null && unit.IsValid)
{
    var health = unit.GetProperty<float>(PropertyUnit.Health);
    var position = unit.Position;
}
#endif
```

**2. 通过Player获取**
```csharp
#if CLIENT
// 获取本地玩家的主单位
Player localPlayer = Player.LocalPlayer;
Unit? mainUnit = localPlayer.MainUnit;

// 获取其他玩家
Player? otherPlayer = Player.GetById(playerId);
Unit? otherMainUnit = otherPlayer?.MainUnit;

// 获取所有玩家
List<Player> allPlayers = Player.AllPlayers;
#endif
```

**3. 通过Scene查询**
```csharp
#if CLIENT
Scene currentScene = Player.LocalPlayer.Scene;

// 获取场景中的所有Unit
IEnumerable<Unit> allUnits = currentScene.GetMorphs(entity => entity as Unit);

// 过滤特定条件的Unit
IEnumerable<Unit> aliveUnits = currentScene.GetMorphs(entity => 
    entity is Unit unit && unit.IsAlive ? unit : null);

// 获取指定玩家的单位
IEnumerable<Unit> playerUnits = currentScene.GetMorphs(entity => 
    entity is Unit unit && unit.Player.Id == targetPlayerId ? unit : null);
#endif
```

**4. 空间查询**
```csharp
#if CLIENT
Scene scene = Player.LocalPlayer.Scene;
ScenePoint searchCenter = new ScenePoint(1000, 1000, scene);

// 圆形范围搜索
IEnumerable<Unit>? unitsInRange = scene.SearchCircle(searchCenter, 500f, 
    entity => entity as Unit);

// 矩形范围搜索  
IEnumerable<Unit>? unitsInRect = scene.SearchRectangle(
    searchCenter, new Angle(0), 200f, 100f,
    entity => entity is Unit unit && unit.IsAlive);

// 扇形范围搜索
IEnumerable<Unit>? unitsInCone = scene.SearchCone(
    searchCenter, new Angle(45), 300f, new Angle(90),
    entity => entity is Unit);
#endif
```

**5. 事件监听获取**
```csharp
#if CLIENT
public class UnitWatcher : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }

    private static void OnGameTriggerInitialization()
    {
        // 监听单位创建
        var unitCreatedTrigger = new Trigger<EventUnitCreated>(OnUnitCreated);
        unitCreatedTrigger.Register(Game.Instance);

        // 监听玩家主单位变化
        Player.LocalPlayer.OnMainUnitChanged += OnMainUnitChanged;
    }

    private static void OnUnitCreated(EventUnitCreated e)
    {
        Unit newUnit = e.Unit;
        ProcessNewUnit(newUnit);
    }
}
#endif
```

#### 🎮 客户端操作权限

**✅ 可以进行的操作：**

**1. 读取操作**
```csharp
#if CLIENT
// 读取属性（只读）
var health = unit.GetProperty<float>(PropertyUnit.Health);
var position = unit.Position;
var facing = unit.Facing;
var isAlive = unit.IsAlive;

// 读取状态
bool isMoving = unit.HasState(UnitState.IsMoving);
bool isStunned = unit.HasState(UnitState.Stunned);

// 读取关系
bool canSee = unit.CanBeSeen(Player.LocalPlayer.MainUnit);
bool isVisible = unit.IsVisibleTo(Player.LocalPlayer);
#endif
```

**2. 发送指令（仅限自己的单位）**
```csharp
#if CLIENT
public void GiveOrderToMyUnit(Unit myUnit, ScenePoint targetPosition)
{
    // 检查所有权
    if (myUnit.Player != Player.LocalPlayer)
    {
        Game.Logger.LogWarning("不能给其他玩家的单位下指令");
        return;
    }

    // 发送移动指令
    var moveCommand = new Command
    {
        Index = CommandIndex.Move,
        Type = ComponentTagEx.Walkable,
        Target = targetPosition,
        Player = Player.LocalPlayer,
        Flag = CommandFlag.IsUser
    };

    var result = moveCommand.IssueOrder(myUnit);
}
#endif
```

**3. 创建视觉效果**
```csharp
#if CLIENT
public void CreateUnitEffects(Unit unit)
{
    // 选择高亮
    if (IsSelected(unit))
    {
        var highlight = new ActorHighlight(highlightLink, false, unit);
        highlight.AttachTo(unit);
    }

    // 状态特效
    if (unit.HasState(UnitState.Poisoned))
    {
        var poisonEffect = new ActorParticle(poisonEffectLink, false, unit);
        poisonEffect.AttachTo(unit, "status_socket");
    }
}
#endif
```

**❌ 无法进行的操作：**
- 直接修改Unit属性（如血量、状态）
- 创建或销毁Unit
- 给其他玩家的Unit下指令
- 访问不可见Unit的详细信息

#### 🛠️ 实用工具方法

```csharp
#if CLIENT
public static class UnitHelpers
{
    /// <summary>
    /// 获取本地玩家的所有单位
    /// </summary>
    public static IEnumerable<Unit> GetMyUnits()
    {
        var scene = Player.LocalPlayer.Scene;
        return scene.GetMorphs(entity => 
            entity is Unit unit && unit.Player == Player.LocalPlayer ? unit : null);
    }

    /// <summary>
    /// 获取可见的敌方单位
    /// </summary>
    public static IEnumerable<Unit> GetVisibleEnemies()
    {
        var scene = Player.LocalPlayer.Scene;
        var localPlayer = Player.LocalPlayer;
        
        return scene.GetMorphs(entity => 
        {
            if (entity is Unit unit && 
                unit.Player.GetRelationShip(localPlayer) == PlayerRelationShip.Enemy &&
                unit.CanBeSeen(localPlayer.MainUnit))
            {
                return unit;
            }
            return null;
        });
    }

    /// <summary>
    /// 查找最近的敌方单位
    /// </summary>
    public static Unit? FindNearestEnemy(ScenePoint position, float maxRange = 1000f)
    {
        var scene = position.Scene;
        var enemies = scene.SearchCircle(position, maxRange, entity =>
            entity is Unit unit && 
            unit.Player.GetRelationShip(Player.LocalPlayer) == PlayerRelationShip.Enemy &&
            unit.IsAlive);

        Unit? nearest = null;
        float nearestDistance = float.MaxValue;

        if (enemies != null)
        {
            foreach (Unit enemy in enemies.Cast<Unit>())
            {
                float distance = Vector3.Distance(position.ToVector3(), enemy.Position.ToVector3());
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }
}
#endif
```

#### ⚠️ 客户端使用注意事项

1. **权限限制**
   - 只能读取Unit属性，不能直接修改
   - 只能给自己的Unit发送指令
   - 无法直接创建或销毁Unit

2. **可见性限制**
   - 只能获取视野范围内的Unit
   - 敌方Unit某些信息可能不可见
   - 使用`CanBeSeen()`检查可见性

3. **同步延迟**
   - Unit状态变化有网络延迟
   - 使用事件系统响应状态变化
   - 避免频繁轮询Unit状态

4. **性能考虑**
   - 缓存经常访问的Unit引用
   - 避免每帧查询大量Unit
   - 使用空间查询而不是遍历所有Unit

## 👥 队伍视野共享

### 🤝 默认视野共享

同一队伍的玩家默认共享视野：

```csharp
// 队伍视野共享检查
public bool IsShareSightTo(Player player)
{
    return player.Team == Team || 
           (shareSightToPlayers?.Contains(player) ?? false);
}
```

### ⚙️ 自定义视野共享

```csharp
#if SERVER
// 设置与特定玩家的视野共享
player1.SetShareSightTo(player2, true);  // 开启共享
player1.SetShareSightTo(player2, false); // 关闭共享

// 检查视野共享状态
bool isSharing = player1.IsShareSightTo(player2);
#endif
```

### 👁️ 视野共享规则

1. **队伍成员** - 同队伍玩家自动共享视野
2. **自定义共享** - 可以与非队伍成员共享视野
3. **单向共享** - 视野共享可以是单向的
4. **实时更新** - 视野共享状态变化实时生效

```csharp
// 视野共享示例
var team1 = Team.GetOrCreateById(1);
var team2 = Team.GetOrCreateById(2);

var player1 = Player.GetById(1);
var player2 = Player.GetById(2);
var player3 = Player.GetById(3);

team1.AddPlayer(player1);
team1.AddPlayer(player2);
team2.AddPlayer(player3);

// player1 和 player2 自动共享视野（同队伍）
// player1 可以单独与 player3 共享视野
#if SERVER
player1.SetShareSightTo(player3, true);
#endif
```

## 🏃‍♂️ 移动与同步

### 🎯 移动机制概述

单位移动系统提供了两种主要的移动方式，每种都有其特定的使用场景和限制：

> **⚠️ 重要区别**：不同的移动方式对指令队列的影响不同：
> 
> - **指令系统移动** - 受指令队列管理，遵循技能释放等游戏规则
> - **直接移动方法** - 绕过指令队列，可在任何时候执行

### 📋 移动方式对比

| 移动方式 | 指令队列影响 | 技能期间可用 | 适用场景 |
|---------|-------------|-------------|----------|
| 指令系统移动 | ✅ 受限制 | ❌ 不可用* | 标准玩家控制、AI决策 |
| 直接移动方法 | ❌ 不受限制 | ✅ 可用 | 技能效果、强制移动、特殊机制 |

> ***注意**：瞬发（Transient）技能期间可以正常使用指令系统移动

### 🎮 指令系统移动

通过指令系统进行的移动会进入单位的指令队列，遵循游戏规则：

```csharp
#if SERVER
// 通过指令系统移动（推荐用于玩家控制）
var moveCommand = new Command
{
    Index = CommandIndex.Move,
    Type = ComponentTagEx.Walkable,
    Target = targetPosition,
    Player = player,
    Flag = CommandFlag.IsUser
};

var result = moveCommand.IssueOrder(unit);
if (result.IsSuccess)
{
    Game.Logger.LogInfo("移动指令已下达");
}

// 摇杆移动指令
var joystickCommand = new Command
{
    Index = CommandIndex.VectorMove,
    Type = ComponentTagEx.Walkable,
    Target = new Angle(45),  // 移动方向
    Player = player,
    Flag = CommandFlag.IsUser
};

joystickCommand.IssueOrder(unit);
#endif
```

**指令系统移动特点：**
- ✅ 遵循游戏规则和指令队列
- ✅ 支持玩家权限验证
- ✅ 与技能系统正确交互
- ❌ 不能与技能释放同时进行
- ❌ 可能被其他指令打断

### 🔧 直接移动方法

直接调用单位的移动方法，绕过指令队列限制：

```csharp
#if SERVER
// 1. 直接设置位置（瞬移）
var newPosition = new ScenePoint(500, 300, scene);
unit.MoveTo(newPosition);

// 2. 平滑位置移动
unit.SetPosition(newPosition, sync: false, syncDistance: true);

// 3. 路径移动（寻路）- 对应指令系统的Move指令
unit.PathTo(targetPosition, maxDistance: 100f, minDistance: 10f);

// 4. 摇杆移动
unit.StartJoystickMove(deltaX, deltaY);
// 或者使用角度
unit.StartJoystickMove(new Angle(45));

// 5. 停止摇杆移动
unit.StopJoystickMove();
#endif
```

**直接移动方法特点：**
- ✅ 不受指令队列限制
- ✅ 可在技能释放期间使用
- ✅ 适用于技能效果和特殊机制
- ❌ 不进行权限验证
- ❌ 可能与游戏规则冲突

### 🎯 实际应用场景

#### 💫 技能期间移动

```csharp
#if SERVER
// 在技能释放期间移动单位
public async Task CastSpellWithMovement(Unit caster, IGameLink<GameDataAbility> spell, Unit target)
{
    // 开始施法
    var castCommand = new Command
    {
        AbilityLink = spell,
        Index = CommandIndex.Execute,
        Type = ComponentTagEx.AbilityManager,
        Target = target,
        Flag = CommandFlag.IsSystem
    };
    
    var result = castCommand.IssueOrder(caster);
    if (!result.IsSuccess) return;
    
    // 在施法期间移动（只能使用直接移动方法）
    if (spell.Data.CanMoveWhileCasting)
    {
        var moveDirection = CalculateOptimalDirection(caster, target);
        caster.StartJoystickMove(moveDirection);
        
        // 等待施法完成
        await WaitForSpellComplete(caster);
        
        // 停止移动
        caster.StopJoystickMove();
    }
}
#endif
```

#### 🌪️ 位移技能实现

```csharp
#if SERVER
// 闪现技能：瞬间移动到目标位置
public void CastBlink(Unit caster, ScenePoint targetPosition)
{
    // 验证距离
    var maxBlinkRange = 1200f;
    if (caster.Position.DistanceTo(targetPosition) > maxBlinkRange)
    {
        targetPosition = caster.Position.MoveTowards(targetPosition, maxBlinkRange);
    }
    
    // 直接移动到目标位置（绕过指令队列）
    caster.MoveTo(targetPosition);
    
    // 播放特效
    PlayBlinkEffect(caster.Position, targetPosition);
    
    Game.Logger.LogInfo("单位闪现到位置: {Position}", targetPosition);
}

// 冲锋技能：快速移动到目标
public async Task CastCharge(Unit caster, Unit target)
{
    var chargeSpeed = 800f;
    var originalSpeed = caster.GetProperty<float>(PropertyUnit.MoveSpeed);
    
    // 临时提升移动速度
    caster.SetProperty(PropertyUnit.MoveSpeed, chargeSpeed);
    
    // 使用路径移动冲向目标
    caster.PathTo(target, maxDistance: 100f);
    
    // 等待接近目标
    while (caster.Position.DistanceTo(target.Position) > 150f && caster.IsValid)
    {
        await Game.DelayFrames(1);
    }
    
    // 恢复移动速度
    caster.SetProperty(PropertyUnit.MoveSpeed, originalSpeed);
}
#endif
```

#### 🎮 AI 移动控制

```csharp
#if SERVER
// AI 使用指令系统移动（推荐）
public void AIMoveTo(Unit aiUnit, ScenePoint destination)
{
    var moveCommand = new Command
    {
        Index = CommandIndex.Move,
        Type = ComponentTagEx.Walkable,
        Target = destination,
        Flag = CommandFlag.IsAI  // AI 标志
    };
    
    moveCommand.IssueOrder(aiUnit);
}

// AI 紧急避险移动（直接方法）
public void AIEmergencyMove(Unit aiUnit, ScenePoint safePosition)
{
    // 紧急情况下直接移动，不受指令队列限制
    aiUnit.PathTo(safePosition);
    Game.Logger.LogInfo("AI 紧急移动到安全位置");
}
#endif
```

#### 🌊 环境效果移动

```csharp
#if SERVER
// 传送门效果
public void TeleportUnitThroughPortal(Unit unit, ScenePoint exitPosition)
{
    // 播放传送动画
    PlayTeleportAnimation(unit);
    
    // 瞬间移动（不能被打断）
    unit.MoveTo(exitPosition);
    
    Game.Logger.LogInfo("单位通过传送门移动到: {Position}", exitPosition);
}

// 水流推动效果
public async Task ApplyWaterCurrent(Unit unit, Angle direction, float duration)
{
    var endTime = Game.Time + TimeSpan.FromSeconds(duration);
    
    while (Game.Time < endTime && unit.IsValid)
    {
        // 持续推动单位
        unit.StartJoystickMove(direction);
        await Game.DelayFrames(1);
    }
    
    unit.StopJoystickMove();
}
#endif
```

### 🔄 移动同步机制

无论使用哪种移动方式，服务端的位置变化都会自动同步到客户端：

#### 📡 同步特性

1. **实时同步** - 位置变化立即同步到有视野的客户端
2. **平滑插值** - 客户端自动进行位置插值，避免瞬移感
3. **视野更新** - 移动可能改变单位的视野范围和可见性
4. **状态同步** - 移动状态（如 `IsChangingPosition`）实时同步

```csharp
// 监听位置变化事件
#if CLIENT
unit.OnPositionChangingStart += (entity) => {
    // 单位开始移动 - 可以播放移动动画
    PlayMoveAnimation();
};

unit.OnPositionChangingEnd += (entity) => {
    // 单位停止移动 - 可以播放停止动画
    PlayIdleAnimation();
};

// 监听位置更新
unit.OnPositionChanged += (entity, oldPos, newPos) => {
    // 位置发生变化 - 更新UI或特效
    UpdatePositionIndicator(newPos);
};
#endif
```

#### 🎯 同步优化

```csharp
#if SERVER
// 控制同步频率（性能优化）
unit.SetPositionSyncRate(30);  // 每秒30次位置同步

// 设置同步距离阈值
unit.SetSyncDistanceThreshold(5f);  // 移动超过5单位时才同步

// 批量位置更新
var units = GetNearbyUnits();
foreach (var unit in units)
{
    unit.SetPosition(newPosition, sync: false);  // 先不同步
}
// 批量同步
SyncPositionBatch(units);
#endif
```

### 🎮 移动控制方法

除了前面介绍的指令系统移动和直接移动方法，还有一些通用的移动控制方法：

```csharp
#if SERVER
// 停止所有移动
unit.Stop();  // 清空指令队列并停止当前移动

// 设置朝向
unit.SetFacing(angle, duration: 0.5f);  // 0.5秒内转向目标角度

// 瞬间设置朝向
unit.SetFacing(angle);

// 转向目标
unit.FaceToTarget(targetUnit);
unit.FaceToPoint(targetPosition);

// 移动状态查询
bool isMoving = unit.HasState(UnitState.IsMoving);
bool isChangingPosition = unit.HasState(UnitState.IsChangingPosition);

// 获取移动组件
var walkable = unit.GetComponent<Walkable>();
if (walkable != null)
{
    var moveSpeed = walkable.MoveSpeed;
    var isPathFinding = walkable.IsPathFinding;
}
#endif
```

### 🚫 移动限制与状态

某些单位状态会影响移动能力：

```csharp
#if SERVER
// 检查移动限制
if (unit.HasState(UnitState.Rooted))
{
    // 单位被束缚，无法移动
    Game.Logger.LogInfo("单位被束缚，移动被阻止");
    return;
}

if (unit.HasState(UnitState.Stunned))
{
    // 单位被眩晕，无法移动
    return;
}

// 添加移动限制状态
unit.AddState(UnitState.Rooted);     // 束缚：不能移动但可以攻击和施法
unit.AddState(UnitState.Stunned);    // 眩晕：不能执行任何操作
unit.AddState(UnitState.Slowed);     // 减速：移动速度降低

// 检查是否可以移动
public bool CanMove(Unit unit)
{
    return !unit.HasState(UnitState.Rooted) && 
           !unit.HasState(UnitState.Stunned) && 
           !unit.HasState(UnitState.Dead) &&
           unit.IsValid;
}
#endif
```

## ⚙️ 高级配置

### 🎛️ 单位数据配置

在 `GameDataUnit` 中配置单位属性：

```csharp
[GameDataCategory]
public partial class GameDataUnit : IGameDataActorScopeOwner
{
    // 基础属性
    public float AttackableRadius { get; set; }
    public float CollisionRadius { get; set; }
    
    // 视野属性
    public List<UnitFilter> Filter { get; set; } = [];
    public List<UnitState>? State { get; set; }
    
    // 碰撞掩码
    public DynamicCollisionMask DynamicCollisionMask { get; set; } = 
        DynamicCollisionMask.Unit | DynamicCollisionMask.Building;
}
```

### 🔧 属性系统集成

```csharp
// 复杂属性管理
var propertyComplex = unit.GetComponent<UnitPropertyComplex>();

// 设置基础属性值
#if SERVER
propertyComplex.SetFixed(ScopeData.UnitProperty.Sight, PropertySubType.Base, 800f);
propertyComplex.SetFixed(ScopeData.UnitProperty.MoveSpeed, PropertySubType.Base, 350f);

// 添加属性修正
propertyComplex.AddFixed(ScopeData.UnitProperty.Sight, PropertySubType.Bonus, 200f);
#endif

// 获取最终属性值
var finalSight = propertyComplex.GetFinalValue(ScopeData.UnitProperty.Sight);
```

### 🎯 目标系统集成

```csharp
// 检查目标有效性
bool canTarget = unit.IsValidTargetTo(caster, isRequest: true);

// 计算角度
var angleToTarget = unit.AngleTo(target);

// 计算距离
var distanceToTarget = unit.DistanceTo(target);
```

## 🔧 API 参考

### 核心方法

#### 创建与销毁

```csharp
// 推荐的创建方式（安全，失败时返回null）
Unit? CreateUnit(Player player, ScenePoint scenePoint, Angle facing)  // GameDataUnit方法

// 构造函数（仅服务端，不推荐直接使用）
Unit(IGameLink<GameDataUnit> link, Player player, ScenePoint scenePoint, Angle facing)

// 生命周期
void Kill(DeathType deathType = DeathType.Normal)
void Revive()
void Destroy(bool forceNoFade = false)
```

#### 属性管理

```csharp
// 属性访问
TValue? GetProperty<TValue>(PropertyUnit property)
void SetProperty<TValue>(PropertyUnit property, TValue value)  // 仅服务端
void AddProperty(PropertyUnit property, Fixed value)          // 仅服务端
```

#### 状态管理

```csharp
// 状态操作
void AddState(UnitState state)      // 仅服务端
void RemoveState(UnitState state)   // 仅服务端
bool HasState(UnitState state)
IEnumerable<UnitState> GetStates()
```

#### 可见性检查

```csharp
// 可见性方法
bool IsVisibleTo(Player player)           // 仅服务端
bool CanBeSeen(Entity caster)
bool IsValidTargetTo(Entity caster, bool isRequest)
```

#### 移动控制

```csharp
// 移动方法（仅服务端）
bool MoveTo(ScenePoint scenePoint)
bool SetPosition(ScenePoint scenePoint, bool sync = false, bool syncDistance = false)
bool PathTo(IApproachableTarget target, float maxDis, float minDis)
void Stop()
void SetFacing(float angle, TimeSpan time)
```

### 重要属性

```csharp
// 基本属性
bool IsAlive { get; }
bool IsDead { get; }
bool IsValid { get; }
Player Player { get; }
ScenePoint Position { get; }
Angle Facing { get; }
float InteractRadius { get; }

// 单位特有属性
int ModSeed { get; }
bool CanReceiveCommandRequest { get; }
bool ShouldIgnoreDamage { get; }
```

### 事件系统

```csharp
// 生命周期事件
event Action<DeathType>? OnDeath;
event Action? OnRevived;
event Action<Entity>? OnPositionChangingStart;
event Action<Entity>? OnPositionChangingEnd;
```

## 💡 最佳实践

### ✅ 推荐做法

1. **安全创建** - 使用 `GameDataUnit.CreateUnit()` 方法创建单位，避免异常
2. **正确异步** - 使用 `Game.Delay()` 而非 `Task.Delay()` 进行异步操作
3. **服务端权威** - 所有游戏逻辑在服务端执行
4. **属性系统** - 使用 `UnitPropertyComplex` 管理复杂属性
5. **事件驱动** - 监听单位事件响应变化
6. **资源清理** - 及时销毁不需要的单位
7. **性能优化** - 合理设置同步类型

### ❌ 避免的做法

1. **直接构造** - 不要直接使用 `new Unit()` 构造函数，可能导致异常
2. **错误异步** - 不要使用 `Task.Delay()` 或多线程API，在Wasm环境中可能异常
3. **客户端修改** - 不要在客户端直接修改单位属性
4. **频繁创建** - 避免频繁创建和销毁单位
5. **全局同步** - 不要对所有单位使用 `SyncType.All`
6. **忽略状态** - 不要忽略单位状态检查
7. **内存泄漏** - 不要忘记清理事件订阅

## 🔗 相关文档

- [🔄 异步编程](../best-practices/AsyncProgramming.md) - **重要** WebAssembly环境异步编程指南
- [实体系统](EntitySystem.md) - 单位的基础实体系统
- [属性系统](UnitPropertySystem.md) - 单位属性管理详解
- [事件系统](EventSystem.md) - 单位事件处理机制
- [AI系统](AISystem.md) - NPC 单位的 AI 行为
- [日志系统](LoggingSystem.md) - 单位相关的日志记录

---

> 💡 **提示**: 单位系统是游戏的核心，正确理解其可见性和同步机制对于开发稳定的多人游戏至关重要。建议在开发过程中多关注服务端和客户端的职责分离，并始终使用 `GameDataUnit.CreateUnit()` 方法安全地创建单位。 