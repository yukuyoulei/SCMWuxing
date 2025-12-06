# 🏗️ 实体-组件-数据模式指南

本文档详细介绍 WasiCore 框架中实体（Entity）、组件（Component）和数据表（GameData）之间的关系和使用模式，帮助开发者正确理解和使用这个核心设计模式。

## 📋 目录

- [🎯 核心概念](#核心概念)
- [🏭 工厂模式设计](#工厂模式设计)
- [🔧 继承与扩展](#继承与扩展)
- [💡 实践指南](#实践指南)
- [⚖️ 设计分析](#设计分析)
- [🚀 最佳实践](#最佳实践)

## 🎯 核心概念

### 三层架构模式

WasiCore 框架采用了**实体-组件-数据**三层架构模式：

```
GameData（数编表）-> Entity/Component（实体/组件）-> 游戏逻辑
     ↓工厂模式            ↓运行时实例                ↓业务逻辑
   配置与模板            具体对象                  实际功能
```

### 🎮 实体系统

#### Entity（实体）
- **Unit**（单位）：游戏中的角色、NPC、建筑等可交互对象
- **Item**（物品）：装备、道具、消耗品等物品对象
- **Ability**（技能）：法术、技能、被动效果等能力对象

#### GameData（数编表）
- **GameDataUnit**：单位的数据定义和配置模板
- **GameDataItem**：物品的数据定义和配置模板
- **GameDataAbility**：技能的数据定义和配置模板

#### 关系说明
每个实体类型都有对应的数编表，数编表充当**工厂类**的角色，负责创建和配置实体实例。

## 🏭 工厂模式设计

### 工厂方法模式

框架中的数编表实现了**工厂方法模式**，每个数编表都有对应的创建方法：

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
    
    if (useDefaultAI)
    {
        AIThinkTree.AddDefaultAI(ScopeScript.LastCreatedUnit);
    }
    
    return ScopeScript.LastCreatedUnit;
}
```

### 其他工厂示例

```csharp
// GameDataItem 作为 Item 的工厂
public abstract class GameDataItem
{
    protected abstract Item CreateItem(Unit unit);
    
    public Item CreateItem(ScenePoint scene, Player? player = null)
    {
        return CreateItem(CreateItemUnit(scene, player));
    }
}

// GameDataAbility 作为 Ability 的工厂
public partial class GameDataAbility
{
    public virtual Ability CreateAbility(Unit owner, Item? item = null);
}
```

## 🔧 继承与扩展

### ⚠️ 重要原则

> **当需要继承 Unit 类时，必须同时继承 GameDataUnit 并重载其 CreateUnit 方法**

这是因为 `GameDataUnit.CreateUnit` 内部直接构造的是 `Unit` 类：

```csharp
// GameDataUnit.CreateUnit 内部
ScopeScript.LastCreatedUnit = new Unit(Link, player, scenePoint, facing);
```

### 正确的继承模式

#### 1. 继承 Unit 类

```csharp
// 创建自定义单位类
public class Hero : Unit
{
    public Hero(IGameLink<GameDataUnit> link, Player player, ScenePoint scenePoint, Angle facing) 
        : base(link, player, scenePoint, facing)
    {
        // 英雄特有的初始化逻辑
        InitializeHeroFeatures();
    }
    
    // 英雄特有的属性
    public int HeroLevel { get; set; }
    public List<Skill> UltimateSkills { get; set; } = new();
    
    // 英雄特有的方法
    public void LevelUp()
    {
        HeroLevel++;
        UpdateHeroStats();
    }
    
    private void InitializeHeroFeatures()
    {
        // 初始化英雄特有功能
    }
}
```

#### 2. 继承 GameDataUnit 类

```csharp
// 创建对应的数编表类
public class GameDataHero : GameDataUnit
{
    // 英雄特有的数据配置
    public int BaseLevel { get; set; } = 1;
    public List<IGameLink<GameDataAbility>> UltimateAbilities { get; set; } = new();
    public HeroType HeroType { get; set; }
    
    // 重载 CreateUnit 方法
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
            
            if (useDefaultAI)
            {
                AIThinkTree.AddDefaultAI(hero);
            }
            
            ScopeScript.LastCreatedUnit = hero;
            return hero;
        }
        catch (Exception e)
        {
            Game.Logger.LogError(e, "Failed to create hero {hero} at {scenePoint}", this, scenePoint);
            return null;
        }
    }
    
    private void InitializeHeroSpecificFeatures(Hero hero)
    {
        // 添加英雄特有的技能
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

### 其他组件的继承模式

#### 自定义物品类

```csharp
// 继承 Item
public class Equipment : Item
{
    public Equipment(Unit host, IGameLink<GameDataItem> link) : base(host, link)
    {
        InitializeEquipmentFeatures();
    }
    
    public EquipmentSlot Slot { get; set; }
    public Dictionary<PropertyType, float> StatBonuses { get; set; } = new();
}

// 继承 GameDataItem
public class GameDataEquipment : GameDataItem
{
    public EquipmentSlot RequiredSlot { get; set; }
    public Dictionary<PropertyType, float> BaseStats { get; set; } = new();
    
    protected override Item CreateItem(Unit unit)
    {
        return new Equipment(unit, Link)
        {
            Slot = RequiredSlot,
            StatBonuses = BaseStats
        };
    }
}
```

#### 自定义技能类

```csharp
// 继承 Ability
public class PassiveAbility : Ability
{
    public PassiveAbility(Unit owner, IGameLink<GameDataAbility> link, Item? item = null) 
        : base(owner, link, item)
    {
        // 被动技能特有的初始化
        IsPassive = true;
        AutoActivate = true;
    }
    
    public bool IsActive { get; set; }
    public TimeSpan Duration { get; set; }
}

// 继承 GameDataAbility
public class GameDataPassiveAbility : GameDataAbility
{
    public TimeSpan PassiveDuration { get; set; }
    public bool AutoTrigger { get; set; } = true;
    
    public override Ability CreateAbility(Unit owner, Item? item = null)
    {
        return new PassiveAbility(owner, Link, item)
        {
            Duration = PassiveDuration
        };
    }
}
```

## 💡 实践指南

### 创建实体的标准流程

#### 1. 定义数编表数据

```csharp
// 在初始化代码中创建数编表
new GameDataHero(ScopeData.Unit.TestHero)
{
    Name = "测试英雄",
    BaseLevel = 1,
    HeroType = HeroType.Warrior,
    AttackableRadius = 50,
    Properties = new()
    {
        { ScopeData.UnitProperty.LifeMax, 1500 },
        { ScopeData.UnitProperty.ManaMax, 800 },
        { ScopeData.UnitProperty.AttackDamage, 120 },
        { ScopeData.UnitProperty.MoveSpeed, 350 }
    },
    UltimateAbilities = 
    {
        ScopeData.Ability.HeroicStrike,
        ScopeData.Ability.BattleRoar
    }
};
```

#### 2. 使用工厂方法创建实例

```csharp
// 通过数编表创建英雄实例
var heroData = ScopeData.Unit.TestHero.Data as GameDataHero;
var hero = heroData?.CreateUnit(player, spawnPoint, facing) as Hero;

if (hero != null)
{
    // 英雄创建成功，可以进行后续操作
    hero.LevelUp();
    Console.WriteLine($"创建了 {hero.HeroLevel} 级英雄：{hero.Cache.Name}");
}
```

### 批量创建和管理

```csharp
public class UnitFactory
{
    /// <summary>
    /// 通用的单位创建方法
    /// </summary>
    public static T? CreateUnit<T>(IGameLink<GameDataUnit> link, Player player, ScenePoint position) 
        where T : Unit
    {
        var unitData = link.Data;
        if (unitData == null)
        {
            Game.Logger.LogError("无法找到单位数据: {LinkName}", link.FriendlyName);
            return null;
        }
        
        var unit = unitData.CreateUnit(player, position, new Angle(0));
        return unit as T;
    }
    
    /// <summary>
    /// 创建英雄
    /// </summary>
    public static Hero? CreateHero(IGameLink<GameDataUnit> heroLink, Player player, ScenePoint position)
    {
        return CreateUnit<Hero>(heroLink, player, position);
    }
    
    /// <summary>
    /// 批量创建单位
    /// </summary>
    public static List<Unit> CreateUnits(IEnumerable<IGameLink<GameDataUnit>> unitLinks, 
                                        Player player, ScenePoint basePosition)
    {
        var units = new List<Unit>();
        var offset = Vector3.Zero;
        
        foreach (var link in unitLinks)
        {
            var unit = CreateUnit<Unit>(link, player, basePosition + offset);
            if (unit != null)
            {
                units.Add(unit);
                offset += new Vector3(100, 0, 0); // 间隔放置
            }
        }
        
        return units;
    }
}
```

## ⚖️ 设计分析

### 💪 设计优势

#### 1. **职责分离**
- **GameData**：负责数据配置和实例创建
- **Entity/Component**：负责运行时逻辑和状态管理
- **游戏逻辑**：负责业务流程和交互

#### 2. **扩展性强**
- 通过继承可以轻松扩展新的实体类型
- 数编表支持热更新，可以动态调整配置
- 工厂模式支持复杂的创建逻辑

#### 3. **类型安全**
- 泛型设计确保编译时类型检查
- 强类型的数编Link避免运行时错误
- 明确的继承关系保证类型一致性

#### 4. **数据驱动**
- 配置与代码分离，便于平衡性调整
- 支持数据编辑器可视化编辑
- 可以通过数据配置实现不同的行为

### 🚨 潜在问题

#### 1. **继承耦合**
- 必须同时继承实体类和数编表类
- 违反了"组合优于继承"的原则
- 增加了系统的复杂性

#### 2. **工厂方法限制**
- 每个数编表只能创建一种类型的实体
- 难以支持多态创建
- 扩展新类型时需要修改现有代码

#### 3. **类型转换风险**
- 需要进行强制类型转换
- 运行时类型检查的开销
- 可能出现类型不匹配的错误

### 🛠️ 改进建议

#### 1. **引入泛型工厂**

```csharp
// 改进的泛型工厂设计
public abstract class GameDataUnit<T> : GameDataUnit where T : Unit
{
    public abstract T CreateUnitTyped(Player player, ScenePoint scenePoint, Angle facing, 
                                     IExecutionContext? creationContext = null, bool useDefaultAI = false);
    
    public override Unit? CreateUnit(Player player, ScenePoint scenePoint, Angle facing, 
                                   IExecutionContext? creationContext = null, bool useDefaultAI = false)
    {
        return CreateUnitTyped(player, scenePoint, facing, creationContext, useDefaultAI);
    }
}

// 使用示例
public class GameDataHero : GameDataUnit<Hero>
{
    public override Hero CreateUnitTyped(Player player, ScenePoint scenePoint, Angle facing, 
                                        IExecutionContext? creationContext = null, bool useDefaultAI = false)
    {
        return new Hero(Link, player, scenePoint, facing) { CreationContext = creationContext };
    }
}
```

#### 2. **组合模式替代继承**

```csharp
// 使用组合而非继承
public class UnitTypeConfig
{
    public string UnitTypeName { get; set; }
    public Type UnitType { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class GameDataUnit
{
    public UnitTypeConfig TypeConfig { get; set; }
    
    public virtual Unit? CreateUnit(Player player, ScenePoint scenePoint, Angle facing)
    {
        if (TypeConfig?.UnitType == null)
            return null;
            
        // 使用反射或依赖注入创建实例
        var unit = Activator.CreateInstance(TypeConfig.UnitType, Link, player, scenePoint, facing) as Unit;
        
        // 应用配置属性
        ApplyConfiguration(unit, TypeConfig.Properties);
        
        return unit;
    }
}
```

#### 3. **依赖注入模式**

```csharp
// 使用依赖注入和工厂注册
public interface IUnitFactory
{
    Unit CreateUnit(IGameLink<GameDataUnit> link, Player player, ScenePoint position, Angle facing);
}

public class UnitFactoryRegistry
{
    private readonly Dictionary<Type, IUnitFactory> _factories = new();
    
    public void RegisterFactory<T>(IUnitFactory factory) where T : GameDataUnit
    {
        _factories[typeof(T)] = factory;
    }
    
    public Unit? CreateUnit(GameDataUnit data, Player player, ScenePoint position, Angle facing)
    {
        if (_factories.TryGetValue(data.GetType(), out var factory))
        {
            return factory.CreateUnit(data.Link, player, position, facing);
        }
        
        // 回退到默认创建方式
        return new Unit(data.Link, player, position, facing);
    }
}
```

## 🚀 最佳实践

### 1. **命名规范**
- 实体类：`Unit` → `Hero`, `Monster`, `Building`
- 数编表类：`GameDataUnit` → `GameDataHero`, `GameDataMonster`, `GameDataBuilding`
- 保持一致的命名对应关系

### 2. **继承层级控制**
- 限制继承深度，避免过深的继承链
- 优先使用组合模式扩展功能
- 考虑使用接口定义行为契约

### 3. **错误处理**
- 在工厂方法中添加完整的错误处理
- 提供有意义的错误信息和日志
- 确保创建失败时的优雅降级

### 4. **性能优化**
- 缓存常用的数编表实例
- 使用对象池管理频繁创建的实体
- 考虑延迟初始化非关键组件

### 5. **单元测试**
- 为每个自定义的工厂方法编写单元测试
- 测试继承关系的正确性
- 验证类型转换的安全性

---

## 总结

WasiCore 框架的**实体-组件-数据模式**是一个强大的设计模式，它通过数编表作为工厂提供了灵活的实体创建机制。虽然当前的设计存在一些耦合问题，但通过合理的使用和适当的改进，可以很好地支持复杂游戏系统的开发需求。

开发者在使用这个模式时，需要牢记**同时继承实体类和数编表类**的原则，并注意类型安全和错误处理。通过遵循最佳实践，可以构建出可维护、可扩展的游戏系统。 