# GameData 数据驱动系统

## 概述

WasiCore 引擎提供了一套完整的**数据驱动架构**，核心由 `IGameData`（数编表）和 `IGameLink`（数编Link）组成，实现了游戏数据与逻辑的完全解耦。这套系统是引擎的核心特性之一，为游戏的可维护性、可扩展性和热更新能力奠定了基础。

### 架构概述

- **IGameData（数编表）** - 定义游戏对象的静态数据结构和配置模板
- **IGameLink（数编Link）** - 提供类型安全的数据引用和索引机制  
- **GameDataCategory** - 管理同类型数据的集合和查找
- **可视化编辑器** - 支持数据的可视化编辑和实时预览

这种架构使游戏对象能够通过数据配置实现属性、行为、表现的动态切换，极大提升了系统的可扩展性和开发效率。

### 核心优势

- **热更新支持** - 数据变更无需重启程序
- **类型安全** - 泛型设计确保编译时类型检查
- **高性能查找** - 基于哈希码的快速数据定位（O(1)复杂度）
- **跨平台一致** - 服务端与客户端数据完全同步
- **可视化编辑** - 数据编辑器提供直观的编辑体验
- **版本控制友好** - 数据与代码分离，便于团队协作

## 核心接口

### IGameData 接口

游戏数据基础接口，所有数编表都必须实现此接口。

```csharp
public interface IGameData
{
    /// <summary>
    /// 数编表对应的数编Link引用
    /// </summary>
    IGameLink Link { get; }
    
    /// <summary>
    /// 数编表名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 友好显示名称，优先使用Name，否则使用Id或HashCode
    /// </summary>
    string FriendlyName { get; }
    
    /// <summary>
    /// 通过HashCode获取全局数编表实例
    /// </summary>
    /// <param name="hashCode">数编表的HashCode</param>
    /// <returns>对应的数编表实例，若不存在则返回null</returns>
    static IGameData? Get(int hashCode);
}

/// <summary>
/// 泛型数编表接口，提供类型安全的数据访问
/// </summary>
/// <typeparam name="T">数编表类型</typeparam>
public interface IGameData<out T> : IGameData
    where T : IGameData<T>
{
    /// <summary>
    /// 类型安全的数编Link引用
    /// </summary>
    new IGameLink<T> Link { get; }
}
```

### IGameLink 接口

游戏数编Link接口，提供数编表的引用和查找机制。

```csharp
public interface IGameLink
{
    /// <summary>
    /// 可选的字符串标识符，便于人类阅读，在全局范围内唯一
    /// </summary>
    string? Id { get; }
    
    /// <summary>
    /// 全局唯一的哈希码标识符，由本地哈希码和类型哈希码组合而成
    /// </summary>
    int HashCode { get; }
    
    /// <summary>
    /// 在所属数据类别中的本地哈希码标识符
    /// </summary>
    int HashCodeLocal { get; }
    
    /// <summary>
    /// 当前数编Link对应的数编表数据
    /// </summary>
    IGameData? Data { get; }
    
    /// <summary>
    /// 友好显示名称，优先使用Data的FriendlyName，否则使用Id或HashCode
    /// </summary>
    string FriendlyName { get; }
}

/// <summary>
/// 泛型数编Link接口，提供类型安全的数据访问
/// </summary>
/// <typeparam name="T">数编表类型</typeparam>
public interface IGameLink<out T> : IGameLink
    where T : IGameData<T>
{
    /// <summary>
    /// 类型安全的数编表数据访问
    /// </summary>
    new T? Data { get; }
}
```

### GameDataCategory 基类

数编表类别管理基类，提供同类型数据的集合管理和查找功能。

```csharp
public abstract class GameDataCategory<TSelf> : IGameDataCategory<TSelf>
    where TSelf : GameDataCategory<TSelf>, IGameDataCategory<TSelf>
{
    /// <summary>
    /// 当前类型的全部数编表实例集合
    /// </summary>
    public static IEnumerable<TSelf> Catalog { get; }
    
    /// <summary>
    /// 通过数编Link获取对应的数编表实例
    /// </summary>
    /// <typeparam name="V">数编表具体类型</typeparam>
    /// <param name="gameLink">数编Link</param>
    /// <returns>对应的数编表实例</returns>
    public static V? Get<V>(IGameLink<V> gameLink) where V : IGameData<V>, TSelf;
    
    /// <summary>
    /// 通过HashCode获取数编表实例
    /// </summary>
    /// <param name="hashCode">全局HashCode</param>
    /// <returns>对应的数编表实例</returns>
    public static TSelf? GetData(int hashCode);
    
    /// <summary>
    /// 通过本地HashCode获取数编Link
    /// </summary>
    /// <param name="hashCodeLocal">本地HashCode</param>
    /// <returns>对应的数编Link</returns>
    public static IGameLink<TSelf>? GetLinkByHashLocal(int hashCodeLocal);
}
```

## 设计理念

WasiCore 的数据驱动架构基于以下核心理念：

### 数据与逻辑分离

游戏逻辑不直接依赖具体数据，通过数编Link进行间接引用，实现：

- **配置的灵活性** - 修改数据不需要重新编译代码
- **版本管理友好** - 数据变更不影响代码版本控制
- **团队协作** - 策划和程序员可以并行工作

### 类型安全访问

泛型设计确保编译时类型检查，避免运行时类型错误：

```csharp
// 编译时类型安全
GameLink<GameDataUnit, GameDataUnit> unitLink;
GameDataUnit? unitData = unitLink.Data; // 自动类型推断

// 避免类型转换错误
// GameDataItem? itemData = unitLink.Data; // 编译错误！
```

### 高效数据查找

基于确定性哈希算法，实现O(1)复杂度的数据定位：

```csharp
// 哈希码组合算法
public int HashCode { get; } = HashCodeDeterministic.Combine(hashCodeLocal, TCategory.TypeHashCode);
```

这种设计确保：
- 不同类型的数据不会产生哈希冲突
- 相同ID的不同类型数据可以共存
- 高效的O(1)查找性能

### 热更新友好

数据变更不影响已有的引用关系，支持运行时动态切换：

- GameLink保持不变，指向新的数据实例
- 无需重启程序即可应用数据变更
- 支持A/B测试和实时调试

## 代码生成特性（Attributes）

WasiCore 框架提供了两个重要的特性（Attribute），用于简化数编表类型的开发和自动生成必要的样板代码。

### GameDataCategory 特性

`[GameDataCategory]` 特性用于标记一个类作为游戏数据类别的根类型。

#### 作用和用途

- **标识数编类别**：标记一个类作为游戏数据类别，表示它是游戏引擎使用的配置数据类型
- **自动代码生成**：通过 `GameDataCategorySourceGenerator` 自动生成继承 `GameDataCategory<T>` 的 partial 类
- **自动发现和管理**：允许引擎自动发现、加载和管理这些数据类型
- **配置数据支持**：用于定义游戏规则、单位属性、技能、物品等可配置的游戏元素，这些元素可以在不重新编译代码的情况下进行修改

#### 使用示例

```csharp
namespace GameCore.ActorSystem.Data;

/// <summary>
/// Actor 数据的基础类别
/// </summary>
[GameDataCategory]
public abstract partial class GameDataActor
{
    public bool IsTransient { get; set; }
    public Vector3 Position { get; set; }
    // ... 其他属性
}
```

#### 自动生成的代码

当使用 `[GameDataCategory]` 特性时，代码生成器会自动创建以下代码：

```csharp
namespace GameCore.ActorSystem.Data
{
    public partial class GameDataActor 
        : GameDataCategory<GameDataActor>,
        IGameDataCategory<GameDataActor>
    {
        static int IGameDataCategory<GameDataActor>.TypeHashCode => [类型哈希码];
        
        public GameDataActor(GameLink<GameDataActor, GameDataActor> link) : base(link)
        {
        }
        
        public GameDataActor(IGameLink<GameDataActor> link) : base(link)
        {
        }
    }
}
```

### GameDataNodeType 特性

`[GameDataNodeType<T,V>]` 特性用于标记一个类作为特定数据类别的节点类型，建立类型继承关系。

#### 泛型参数

- **T**：数据类别类型，必须继承自 `GameDataCategory<T>`
- **V**：父类型，必须是 T 的子类

#### 作用和用途

- **节点类型标识**：标记一个类作为特定数据类别的具体节点实现
- **自动代码生成**：通过 `GameDataNodeTypeSourceGenerator` 自动生成继承关系和接口实现
- **类型约束保证**：通过泛型约束确保类型安全和正确的继承关系
- **继承链建立**：自动生成类，继承指定的父类型并实现 `IGameData<T>` 接口

#### 使用示例

```csharp
namespace GameCore.Execution.Data;

/// <summary>
/// 移除Buff的效果节点
/// </summary>
[GameDataNodeType<GameDataEffect, GameDataEffect>]
public partial class GameDataEffectBuffRemove
{
    public FuncUInt? Stack { get; set; }
    public IGameLink<GameDataBuff>? BuffLink { get; set; }
    public HashSetFilter<BuffCategory>? CategoryFilter { get; set; }
    
    // 效果执行逻辑
    public override void Execute(Effect context)
    {
        // 具体的Buff移除逻辑
    }
}
```

#### 自动生成的代码

当使用 `[GameDataNodeType<T,V>]` 特性时，代码生成器会自动创建以下代码：

```csharp
namespace GameCore.Execution.Data
{
    public partial class GameDataEffectBuffRemove : GameDataEffect, IGameData<GameDataEffectBuffRemove>
    {
        public GameDataEffectBuffRemove(GameLink<GameDataEffect, GameDataEffectBuffRemove> link) : base(link)
        {
        }
        
        public GameDataEffectBuffRemove(IGameLink<GameDataEffectBuffRemove> link) : base(link)
        {
        }
        
        public new IGameLink<GameDataEffectBuffRemove> Link => (IGameLink<GameDataEffectBuffRemove>)base.Link;
    }
}
```

### 代码生成的优势

1. **减少样板代码**：自动生成必要的继承关系和接口实现
2. **类型安全保证**：编译时确保正确的泛型参数和类型约束
3. **一致性维护**：确保所有数编表类型都遵循相同的模式
4. **开发效率提升**：开发者只需关注业务逻辑，无需手写基础架构代码
5. **错误预防**：避免手工编写继承关系时可能出现的错误

### 使用指南

#### 何时使用 GameDataCategory

- 创建新的数编分类根类型时（如 `GameDataUnit`、`GameDataAbility`、`GameDataEffect` 等）
- 需要管理一组相关数据类型的集合时
- 希望为数据类型提供统一的基础功能时

#### 何时使用 GameDataNodeType

- 需要继承现有的数编分类或节点类型时
- 实现特定功能的数编表类时

#### 最佳实践

1. **保持类型层次清晰**：确保 `GameDataNodeType` 的泛型参数正确反映继承关系
2. **使用有意义的命名**：类名应该清楚地表达其功能和用途
3. **分离关注点**：将业务逻辑和数据配置分开，特性仅用于标记类型
4. **文档化类型关系**：为复杂的类型层次提供清晰的文档说明

## 核心组件详解

### GameData（数编表）

数编表是游戏数据的实际载体，定义了游戏对象的静态属性、配置和行为模板。

#### 特点

- **继承结构** - 继承自 `GameDataCategory<T>` 基类
- **自动注册** - 构造时自动注册到全局和类别目录
- **可视化编辑** - 支持通过可视化编辑器编辑
- **数据同步** - 服务端与客户端数据完全同步
- **重复检查** - 防止同一Link被多次注册

#### 常见类型

| 数编表类型 | 用途 | 典型属性 |
|-----------|------|----------|
| `GameDataUnit` | 单位数据 | 生命值、攻击力、移动速度、技能列表 |
| `GameDataAbility` | 技能数据 | 冷却时间、魔法消耗、伤害值、效果范围 |
| `GameDataItem` | 物品数据 | 属性加成、使用效果、稀有度、图标 |
| `GameDataScene` | 场景数据 | 地图大小、资源点、传送门、限制条件 |
| `GameDataAI` | AI数据 | 行为树、决策条件、优先级权重 |

### GameLink（数编Link）

数编Link是轻量级的值类型结构体，提供对数编表的类型安全引用。

#### 特点

- **值类型** - 结构体，内存开销极小
- **多种构造** - 支持字符串ID和HashCode两种构造方式
- **全局唯一** - 全局唯一性保证，避免重复引用
- **延迟加载** - 只在访问Data属性时进行查找
- **序列化友好** - 可以安全地进行序列化和网络传输

#### 哈希码机制

```csharp
// 全局HashCode = 本地HashCode ⊕ 类型HashCode
// 确保不同类型的相同本地HashCode不会冲突
public int HashCode { get; } = HashCodeDeterministic.Combine(hashCodeLocal, TCategory.TypeHashCode);
```

这种设计确保：
- 不同类型的数据不会产生哈希冲突
- 相同ID的不同类型数据可以共存
- 高效的O(1)查找性能

## 创建和管理

### GameLink 的创建

数编Link作为值类型结构体，可以通过多种方式创建：

```csharp
// 1. 通过字符串ID创建
var heroLink = new GameLink<GameDataUnit, GameDataUnit>("HostTestHero");

// 2. 通过UTF8字节数组创建
var heroLink2 = new GameLink<GameDataUnit, GameDataUnit>("HostTestHero"u8);

// 3. 通过本地HashCode创建
var heroLink3 = new GameLink<GameDataUnit, GameDataUnit>(12345);

// 4. 相等性比较
bool isEqual = heroLink == heroLink2; // true，指向同一数据
```

#### 全局数编Link定义

由数据编辑器生成的数编Link通常定义在静态类中，便于全局访问：

```csharp
namespace ScopeData
{
    public static class Unit
    {
        // 英雄单位
        public static readonly GameLink<GameDataUnit, GameDataUnit> HostTestHero = new("HostTestHero"u8);
        public static readonly GameLink<GameDataUnit, GameDataUnit> TestWarrior = new("TestWarrior"u8);
        
        // NPC单位
        public static readonly GameLink<GameDataUnit, GameDataUnit> TestNPC = new("TestNPC"u8);
        public static readonly GameLink<GameDataUnit, GameDataUnit> Merchant = new("Merchant"u8);
    }
    
    public static class Ability
    {
        // 战士技能
        public static readonly GameLink<GameDataAbility, GameDataAbility> Slash = new("Slash"u8);
        public static readonly GameLink<GameDataAbility, GameDataAbility> Shield = new("Shield"u8);
        
        // 法师技能
        public static readonly GameLink<GameDataAbility, GameDataAbility> Fireball = new("Fireball"u8);
        public static readonly GameLink<GameDataAbility, GameDataAbility> Teleport = new("Teleport"u8);
    }
}
```

### GameData 的创建

数编表通过继承 `GameDataCategory<T>` 并在构造函数中传入对应的数编Link来创建：

```csharp
// 创建英雄单位数编表
new GameDataUnit(ScopeData.Unit.HostTestHero)
{
    Name = "测试英雄",
    AttackableRadius = 50,
    Properties = new() 
    {
        { UnitProperty.LifeMax, 1000 },
        { UnitProperty.ManaMax, 1000 },
        { UnitProperty.Armor, 10 },
        { UnitProperty.MagicResistance, 10 },
        { UnitProperty.MoveSpeed, 300 },
        { UnitProperty.AttackRange, 150 },
    },
    UpdateFlags = new()
    {
        AllowMover = true,
        Turnable = true,
        Walkable = true,
    },
    VitalProperties = new()
    {
        { PropertyVital.Health, UnitProperty.LifeMax }
    },
    CollisionRadius = 32,
    DynamicCollisionMask = DynamicCollisionMask.Hero | DynamicCollisionMask.Building,
    Inventories = [Inventory.TestInventory6, Inventory.TestInventory6Equip],
    Filter = [UnitFilter.Hero, UnitFilter.Unit],
    Model = Model.HostTestHero,
};

// 创建技能数编表
new GameDataAbility(ScopeData.Ability.Fireball)
{
    Name = "火球术",
    CooldownTime = 3.0f,
    ManaCost = 50,
    CastRange = 600,
    EffectRadius = 150,
    Effects = new()
    {
        // 技能效果配置
    }
};
```

#### 重要特性

- **重复性检查** - 构造时自动检查，防止同一Link被多次注册
- **自动注册** - 自动注册到全局目录和类别目录
- **对象初始化器** - 支持对象初始化器语法进行属性设置
- **数据一致性** - 服务端与客户端需要创建相同的数编表

### 数据访问模式

#### 直接访问

```csharp
// 获取数编表实例
var heroData = ScopeData.Unit.HostTestHero.Data;
if (heroData != null)
{
    Game.Logger.LogInformation("英雄名称: {HeroName}", heroData.Name);
}
```

#### 动态查找

```csharp
// 通过字符串ID动态查找
var heroLink = new GameLink<GameDataUnit, GameDataUnit>("HostTestHero"u8);
var heroData = heroLink.Data;

// 通过HashCode查找
var foundData = IGameData.Get(heroLink.HashCode) as GameDataUnit;
```

#### 类别遍历

```csharp
// 遍历所有单位数编表
foreach (var unitData in GameDataCategory<GameDataUnit>.Catalog)
{
    Game.Logger.LogInformation("单位: {UnitName}, 生命值: {Health}", 
        unitData.Name, unitData.Properties.GetValueOrDefault(UnitProperty.LifeMax, 0));
}
```

## 工厂模式设计

### 数编表作为工厂

WasiCore 框架中的数编表不仅仅是数据存储，某些数编表也充当了**工厂类**的角色。与实体/组件对应的数编表都提供了创建对应实体/组件实例的方法。

#### 工厂方法模式

```csharp
// GameDataUnit 作为 Unit 的工厂
public virtual Unit? CreateUnit(Player player, ScenePoint scenePoint, Angle facing, 
                               IExecutionContext? creationContext = null, bool useDefaultAI = false)
{
    // 创建 Unit 实例
    ScopeScript.LastCreatedUnit = new Unit(Link, player, scenePoint, facing) 
    { 
        CreationContext = creationContext 
    };
    
    return ScopeScript.LastCreatedUnit;
}

// GameDataAbility 作为 Ability 的工厂  
public partial class GameDataAbility
{
    public virtual Ability CreateAbility(Unit owner, Item? item = null);
}

// GameDataItem 作为 Item 的工厂
public abstract class GameDataItem
{
    protected abstract Item CreateItem(Unit unit);
}
```

#### 继承与工厂模式

⚠️ **重要原则**：当需要继承实体类时，必须同时继承对应的数编表类并重载其工厂方法。

这是因为数编表的工厂方法内部直接构造的是基础类型：

```csharp
// GameDataUnit.CreateUnit 内部直接创建 Unit
ScopeScript.LastCreatedUnit = new Unit(Link, player, scenePoint, facing);
```

#### 正确的继承模式

**步骤1：继承实体类**

```csharp
public class Hero : Unit
{
    public Hero(IGameLink<GameDataUnit> link, Player player, ScenePoint scenePoint, Angle facing) 
        : base(link, player, scenePoint, facing)
    {
        InitializeHeroFeatures();
    }
    
    public int HeroLevel { get; set; }
    public List<Skill> UltimateSkills { get; set; } = new();
    
    private void InitializeHeroFeatures()
    {
        // 英雄特有的初始化逻辑
    }
}
```

**步骤2：继承数编表类并重载工厂方法**

```csharp
public class GameDataHero : GameDataUnit
{
    public int BaseLevel { get; set; } = 1;
    public List<IGameLink<GameDataAbility>> UltimateAbilities { get; set; } = new();
    
    // 必须重载 CreateUnit 方法
    public override Unit? CreateUnit(Player player, ScenePoint scenePoint, Angle facing, 
                                   IExecutionContext? creationContext = null, bool useDefaultAI = false)
    {
        try
        {
            // 创建 Hero 实例而不是 Unit
            var hero = new Hero(Link, player, scenePoint, facing) 
            { 
                CreationContext = creationContext,
                HeroLevel = BaseLevel
            };
            
            // 添加英雄特有的初始化逻辑
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
        // 为英雄添加专属技能
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

#### 工厂模式的优势

1. **统一创建入口**：所有实体的创建都通过数编表进行，确保配置的一致性
2. **配置与实例分离**：数据配置在数编表中，运行时逻辑在实体中
3. **支持多态创建**：通过继承可以创建不同类型的实体
4. **便于扩展**：新增实体类型只需继承对应的类并重载工厂方法

#### 设计考量

虽然这种设计要求同时继承实体类和数编表类，增加了一定的复杂性，但它带来了以下好处：

- **类型安全**：编译时确保工厂方法创建正确的实体类型
- **配置驱动**：通过数编表配置控制实体的创建和初始化
- **扩展性强**：支持复杂的继承关系和自定义创建逻辑
- **维护性好**：数据与逻辑分离，便于团队协作

> 📖 **深入了解**：关于实体-组件-数据模式的详细说明，请参阅 [实体-组件-数据模式指南](../guides/EntityComponentDataPattern.md)

## 实际应用场景

### 单位创建系统

```csharp
public class UnitFactory
{
    /// <summary>
    /// 通过数编Link创建单位实例
    /// </summary>
    public Unit CreateUnit(IGameLink<GameDataUnit> unitLink, Player owner, Vector3 position)
    {
        var unitData = unitLink.Data;
        if (unitData == null)
        {
            Game.Logger.LogError("无法找到单位数据: {LinkName}", unitLink.FriendlyName);
            return null;
        }
        
        // 使用数编表的CreateUnit方法
        return unitData.CreateUnit(owner, position);
    }
    
    /// <summary>
    /// 批量创建单位
    /// </summary>
    public List<Unit> CreateUnits(IEnumerable<IGameLink<GameDataUnit>> unitLinks, Player owner, Vector3 basePosition)
    {
        var units = new List<Unit>();
        var offset = Vector3.Zero;
        
        foreach (var link in unitLinks)
        {
            var unit = CreateUnit(link, owner, basePosition + offset);
            if (unit != null)
            {
                units.Add(unit);
                offset += new Vector3(50, 0, 0); // 间隔放置
            }
        }
        
        return units;
    }
}
```

### 技能系统集成

```csharp
public class AbilitySystem
{
    /// <summary>
    /// 为单位添加技能
    /// </summary>
    public void LearnAbility(Unit unit, IGameLink<GameDataAbility> abilityLink)
    {
        var abilityData = abilityLink.Data;
        if (abilityData == null)
        {
            Game.Logger.LogWarning("技能数据不存在: {AbilityLink}", abilityLink.FriendlyName);
            return;
        }
        
        var ability = new Ability(unit, abilityLink);
        unit.Abilities.Add(ability);
        
        Game.Logger.LogInformation("单位 {UnitName} 学会了技能 {AbilityName}", 
            unit.Cache.Name, abilityData.Name);
    }
    
    /// <summary>
    /// 检查技能是否可用
    /// </summary>
    public bool CanCastAbility(Unit caster, IGameLink<GameDataAbility> abilityLink)
    {
        var abilityData = abilityLink.Data;
        if (abilityData == null) return false;
        
        // 检查魔法值
        var currentMana = caster.GetProperty<float>(PropertyUnit.Mana);
        if (currentMana < abilityData.ManaCost)
        {
            return false;
        }
        
        // 检查冷却时间
        var ability = caster.Abilities.FirstOrDefault(a => a.Link == abilityLink);
        return ability?.IsReady ?? false;
    }
}
```

### 物品模板系统

```csharp
public class ItemTemplateSystem
{
    /// <summary>
    /// 根据模板创建物品实例
    /// </summary>
    public Item CreateItem(IGameLink<GameDataItem> itemTemplate, int quantity = 1)
    {
        var templateData = itemTemplate.Data;
        if (templateData == null)
        {
            Game.Logger.LogError("物品模板不存在: {ItemTemplate}", itemTemplate.FriendlyName);
            return null;
        }
        
        return new Item(itemTemplate)
        {
            Quantity = quantity,
            // 其他属性从模板继承
        };
    }
    
    /// <summary>
    /// 获取物品的属性加成
    /// </summary>
    public Dictionary<PropertyUnit, float> GetItemBonuses(IGameLink<GameDataItem> itemTemplate)
    {
        var templateData = itemTemplate.Data;
        return templateData?.PropertyBonuses ?? new Dictionary<PropertyUnit, float>();
    }
}
```

### 场景加载系统

```csharp
public class SceneManager
{
    /// <summary>
    /// 加载场景
    /// </summary>
    public async Task<Scene> LoadScene(IGameLink<GameDataScene> sceneLink)
    {
        var sceneData = sceneLink.Data;
        if (sceneData == null)
        {
            Game.Logger.LogError("场景数据不存在: {SceneLink}", sceneLink.FriendlyName);
            return null;
        }
        
        Game.Logger.LogInformation("开始加载场景: {SceneName}", sceneData.Name);
        
        var scene = new Scene(sceneLink);
        
        // 加载场景资源
        await LoadSceneResources(sceneData);
        
        // 初始化场景对象
        InitializeSceneObjects(scene, sceneData);
        
        Game.Logger.LogInformation("场景加载完成: {SceneName}", sceneData.Name);
        return scene;
    }
    
    private async Task LoadSceneResources(GameDataScene sceneData)
    {
        // 加载地形、贴图、模型等资源
        foreach (var resource in sceneData.Resources)
        {
            await ResourceManager.LoadAsync(resource);
        }
    }
    
    private void InitializeSceneObjects(Scene scene, GameDataScene sceneData)
    {
        // 创建场景中的静态对象
        foreach (var objectInfo in sceneData.StaticObjects)
        {
            var obj = ObjectFactory.Create(objectInfo.Template, objectInfo.Position);
            scene.AddObject(obj);
        }
    }
}
```

## 性能优化

### 哈希码优化

```csharp
// 使用确定性哈希算法确保一致性
public static class HashCodeDeterministic
{
    public static int Combine(int hash1, int hash2)
    {
        // 使用XOR和位移操作，确保分布均匀
        return hash1 ^ (hash2 << 1);
    }
}
```

### 内存优化

```csharp
// GameLink是值类型，避免额外的堆分配
public readonly struct GameLink<TCategory, V> : IGameLink<V>
{
    // 仅包含必要的字段，最小化内存占用
    public readonly required int HashCode { get; init; }
    public readonly required int HashCodeLocal { get; init; }
    public string? Id { get; private init; }
}
```

### 查找优化

```csharp
// 使用Dictionary进行O(1)查找
private static readonly Dictionary<int, TSelf> catalog = [];

public static V? Get<V>(IGameLink<V> gameLink) where V : IGameData<V>, TSelf
{
    // 直接通过HashCode查找，避免遍历
    return catalog.TryGetValue(gameLink.HashCode, out TSelf? data) ? data as V : default;
}
```

## 最佳实践

### 命名规范

```csharp
// 数编Link命名：描述性名称，使用PascalCase
public static readonly GameLink<GameDataUnit, GameDataUnit> FireElemental = new("FireElemental"u8);
public static readonly GameLink<GameDataAbility, GameDataAbility> LightningStrike = new("LightningStrike"u8);

// 数编表命名：与Link保持一致
new GameDataUnit(ScopeData.Unit.FireElemental)
{
    Name = "火元素", // 用户友好的显示名称
    // ...
};
```

### 错误处理

```csharp
public void ProcessUnit(IGameLink<GameDataUnit> unitLink)
{
    var unitData = unitLink.Data;
    if (unitData == null)
    {
        Game.Logger.LogWarning("单位数据不存在: {UnitLink}", unitLink.FriendlyName);
        return;
    }
    
    // 安全地使用unitData
}
```

### 数据验证

```csharp
public class GameDataValidator
{
    public static bool ValidateUnit(GameDataUnit unitData)
    {
        if (unitData.Properties.GetValueOrDefault(UnitProperty.LifeMax, 0) <= 0)
        {
            Game.Logger.LogError("单位 {UnitName} 的最大生命值无效", unitData.Name);
            return false;
        }
        
        return true;
    }
}
```

## 故障排除

### 常见问题

#### 1. 重复注册错误

```
InvalidOperationException: Duplicate registered GameData: HostTestHero
```

**原因**：同一个GameLink被用于创建多个GameData实例
**解决方案**：确保每个GameLink只对应一个GameData实例

#### 2. 数据不一致

**原因**：服务端与客户端的GameData创建顺序或内容不同
**解决方案**：使用相同的数据加载逻辑，确保创建顺序一致

#### 3. 性能问题

**原因**：频繁调用Data属性进行查找
**解决方案**：缓存查找结果，避免重复查找

```csharp
// 不推荐：频繁查找
for (int i = 0; i < 1000; i++)
{
    var data = someLink.Data; // 每次都查找
}

// 推荐：缓存结果
var data = someLink.Data;
if (data != null)
{
    for (int i = 0; i < 1000; i++)
    {
        // 使用缓存的data
    }
}
```

### 调试技巧

```csharp
// 打印所有已注册的数编表
public static void DebugPrintAllGameData()
{
    foreach (var data in IGameData.catalogGlobal.Values)
    {
        Game.Logger.LogDebug("数编表: {DataType} - {DataName} ({HashCode})", 
            data.GetType().Name, data.FriendlyName, data.Link.HashCode);
    }
}

// 验证GameLink的有效性
public static bool ValidateGameLink(IGameLink link)
{
    var hasData = link.Data != null;
    Game.Logger.LogDebug("GameLink验证: {LinkName} - 有效: {IsValid}", 
        link.FriendlyName, hasData);
    return hasData;
}
```

## 相关文档

- [单位属性系统](UnitPropertySystem.md) - 了解如何在数编表中定义单位属性
- [事件系统](EventSystem.md) - 学习如何监听数据变更事件
- [快速开始指南](../guides/QuickStart.md) - 新手入门教程
- [编码规范](../CONVENTIONS.md) - 命名和设计规范
- [性能优化](../best-practices/PerformanceOptimization.md) - 性能优化建议 