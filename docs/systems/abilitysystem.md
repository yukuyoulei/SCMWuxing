# ⚡ 技能系统（Ability System）

技能系统是 WasiCore 游戏框架的核心组件之一，负责管理所有单位能力，包括主动技能、被动技能、攻击技能和切换技能。它与指令系统、效果系统、动画系统等深度集成，提供完整的技能释放体验。

## 📋 目录

- [🏗️ 系统概述](#系统概述)
- [📦 核心组件](#核心组件)
- [🎯 技能类型](#技能类型)
  - [⚡ 执行技能（AbilityExecute）](#执行技能abilityexecute)
  - [🔄 切换技能（AbilityToggle）](#切换技能abilitytoggle)
  - [📡 被动技能（Passive Abilities）](#被动技能passive-abilities)
- [🕐 施法时间控制](#施法时间控制)
  - [⏱️ SpellCastTime 结构](#spellcasttime-结构)
  - [🎯 NormalizedDuration - 攻击间隔精确控制](#normalizedduration---攻击间隔精确控制)
  - [🎭 动画系统集成](#动画系统集成)
- [🎮 目标系统](#目标系统)
- [🔧 技能配置](#技能配置)
- [⚔️ 攻击技能特殊机制](#攻击技能特殊机制)
- [🔄 技能管理](#技能管理)
- [�� 客户端表现](#客户端表现)
- [🚀 高级用法：动态数编表创建](#高级用法动态数编表创建)
- [🛠️ 实用示例](#实用示例)
- [🔧 API 参考](#api-参考)
- [💡 最佳实践](#最佳实践)

## 🏗️ 系统概述

### 架构设计

技能系统采用数据驱动的设计模式，核心架构如下：

```
GameDataAbility (数编表) → Ability (运行时实例) → Order (执行指令) → Effect (实际效果)
                ↓                  ↓                 ↓                ↓
         配置技能属性        管理技能状态        控制施法流程      产生游戏效果
```

### 核心特性

- ✅ **数据驱动** - 所有技能通过数编表配置，支持热更新
- ✅ **类型安全** - 强类型的技能分类和属性验证
- ✅ **生命周期管理** - 完整的技能创建、激活、禁用、销毁流程
- ✅ **多阶段执行** - 支持前摇、施法、引导、后摇的精确控制
- ✅ **动画集成** - 与动画系统无缝结合，支持多种动画模式
- ✅ **目标系统** - 灵活的目标选择和过滤机制
- ✅ **冷却管理** - 内置冷却和充能系统
- ✅ **权限控制** - 与指令系统集成的权限验证
- ✅ **客户端同步** - 自动的客户端状态同步

## 📦 核心组件

### GameDataAbility（技能数编表）

技能的基础配置数据，定义技能的所有静态属性：

```csharp
public partial class GameDataAbility
{
    /// <summary>技能显示名称</summary>
    public LocalizedString? DisplayName { get; set; }
    
    /// <summary>技能描述</summary>
    public LocalizedString? Description { get; set; }
    
    /// <summary>目标类型（单体、范围、自身等）</summary>
    public AbilityTargetType TargetType { get; set; }
    
    /// <summary>技能分类列表</summary>
    public List<AbilityCategory> Categories { get; set; } = [];
    
    /// <summary>技能标志位</summary>
    public AbilityFlags Flags { get; set; } = new();
    
    /// <summary>被动技能周期</summary>
    public FuncTime? PassivePeriod { get; set; }
    
    /// <summary>被动技能效果</summary>
    public IGameLink<GameDataEffect>? PassivePeriodicEffect { get; set; }
    
    /// <summary>技能最大等级</summary>
    public uint? LevelMax { get; set; }
    
    /// <summary>单位属性修改</summary>
    public List<UnitPropertyModification> Modifications { get; set; } = [];
    
    /// <summary>添加的单位状态</summary>
    public List<UnitState> AddStates { get; set; } = [];
    
    /// <summary>移除的单位状态</summary>
    public List<UnitState> RemoveStates { get; set; } = [];
    
    /// <summary>免疫的单位状态</summary>
    public List<UnitState> ImmuneStates { get; set; } = [];
}
```

### GameDataAbilityExecute（执行技能数编表）

执行技能的特化配置，继承自 GameDataAbility：

```csharp
public partial class GameDataAbilityExecute : GameDataAbility
{
    /// <summary>执行标志位（攻击标记、目标获取等）</summary>
    public AbilityExecuteFlags AbilityExecuteFlags { get; set; } = new();
    
    /// <summary>施法时间配置</summary>
    public SpellCastTime? CastTime { get; set; }
    
    /// <summary>技能范围</summary>
    public FuncFloat? Range { get; set; }
    
    /// <summary>冷却时间</summary>
    public FuncTime? Cooldown { get; set; }
    
    /// <summary>法力消耗</summary>
    public FuncFloat? ManaCost { get; set; }
    
    /// <summary>技能效果</summary>
    public IGameLink<GameDataEffect>? Effect { get; set; }
    
    /// <summary>验证器列表</summary>
    public List<IGameLink<GameDataValidator>> Validators { get; set; } = [];
}
```

### Ability（运行时技能实例）

单位上的技能实例，管理技能的运行时状态：

```csharp
public abstract class Ability : TagComponent
{
    public IGameLink<GameDataAbility> Link { get; }           // 数编表链接
    public GameDataAbility Data { get; }                      // 数编表数据
    public uint Level { get; set; }                          // 技能等级
    public bool IsValid { get; }                             // 是否有效
    public bool IsEnabled { get; set; }                      // 是否启用
    public bool IsVisible { get; set; }                      // 是否可见
    public bool IsHidden { get; }                            // 是否隐藏
    public AbilityTargetType TargetType { get; }             // 目标类型
    public List<AbilityCategory> Categories { get; }         // 技能分类
}
```

## 🎯 技能类型

### ⚡ 执行技能（AbilityExecute）

执行技能是最常见的技能类型，包括伤害技能、治疗技能、辅助技能等。

#### 预定义技能配置示例

```csharp
// 火球术技能数编表定义
public static readonly GameDataAbilityExecute FireballData = new(ScopeData.Ability.Fireball)
{
    DisplayName = "火球术",
    Description = "发射一颗火球，对敌人造成火焰伤害",
    TargetType = AbilityTargetType.Unit,
    
    // 施法时间配置
    Time = new SpellCastTime
    {
        Preswing = static (_) => TimeSpan.FromSeconds(0.3f),   // 0.3秒前摇
        Cast = static (_) => TimeSpan.FromSeconds(1.5f),       // 1.5秒施法时间
        Backswing = static (_) => TimeSpan.FromSeconds(0.3f)   // 0.3秒后摇
    },
    
    Range = new FuncFloat(600f),        // 600单位射程
    Cooldown = new FuncTime(8f),        // 8秒冷却
    ManaCost = new FuncFloat(75f),      // 75法力消耗
    
    Effect = ScopeData.Effect.FireballDamage,  // 伤害效果
    
    Validators = [
        ScopeData.Validator.HasMana,           // 法力检查
        ScopeData.Validator.TargetIsEnemy      // 敌方目标检查
    ]
};
```

#### 使用预定义技能

```csharp
#if SERVER
// 为单位添加火球术技能
public void AddFireballToUnit(Unit unit)
{
    var abilityManager = unit.GetComponent<AbilityManager>();
    if (abilityManager != null)
    {
        // 使用预定义的技能数编表
        abilityManager.Add(ScopeData.Ability.Fireball);
        Game.Logger.LogInfo("已为单位添加火球术技能");
    }
}

// 通过指令系统释放技能
public CmdResult CastFireball(Unit caster, Unit target)
{
    var command = new Command
    {
        AbilityLink = ScopeData.Ability.Fireball,  // 引用预定义技能
        Index = CommandIndex.Execute,
        Type = ComponentTagEx.AbilityManager,
        Target = target,
        Flag = CommandFlag.IsSystem
    };
    
    return command.IssueOrder(caster);
}
#endif
```

#### 攻击技能配置

攻击技能需要特别设置 `IsAttack` 标志，以便攻击指令能够识别：

```csharp
// 基础攻击技能数编表定义
public static readonly GameDataAbilityExecute BasicAttackData = new(ScopeData.Ability.BasicAttack)
{
    DisplayName = "基础攻击",
    Description = "使用武器进行普通攻击",
    TargetType = AbilityTargetType.Unit,
    
    // ✅ 关键：标记为攻击技能
    AbilityExecuteFlags = new AbilityExecuteFlags 
    { 
        IsAttack = true  // 攻击指令将自动找到此技能
    },
    
    Time = new SpellCastTime
    {
        Preswing = static (_) => TimeSpan.FromSeconds(0.1f),   // 0.1秒前摇
        Backswing = static (_) => TimeSpan.FromSeconds(0.4f)   // 0.4秒后摇
    },
    
    Range = new FuncFloat(120f),         // 近战范围
    Cooldown = new FuncTime(1.0f),       // 1秒攻击间隔
    
    Effect = ScopeData.Effect.WeaponDamage,
    
    Validators = [
        ScopeData.Validator.TargetIsEnemy,
        ScopeData.Validator.InRange
    ]
};
```

### 🔄 切换技能（AbilityToggle）

切换技能可以开启和关闭，通常用于光环、护盾、变身等持续效果。

#### 预定义切换技能配置

```csharp
// 魔法护盾技能数编表定义
public static readonly GameDataAbilityToggle ManaShieldData = new(ScopeData.Ability.ManaShield)
{
    DisplayName = "魔法护盾",
    Description = "开启后消耗法力值抵挡伤害",
    TargetType = AbilityTargetType.None,
    
    Cooldown = new FuncTime(2f),         // 2秒冷却
    ManaCost = new FuncFloat(50f),       // 开启消耗50法力
    ManaPerSecond = new FuncFloat(10f),  // 每秒消耗10法力
    
    TurnOnEffect = ScopeData.Effect.ActivateManaShield,   // 开启效果
    TurnOffEffect = ScopeData.Effect.DeactivateManaShield, // 关闭效果
    
    Validators = [
        ScopeData.Validator.HasMana
    ]
};
```

#### 切换技能使用

```csharp
#if SERVER
// 智能切换技能状态
public CmdResult ToggleManaShield(Unit unit)
{
    var command = new Command
    {
        AbilityLink = ScopeData.Ability.ManaShield,
        Index = CommandIndex.Toggle,  // 智能切换
        Type = ComponentTagEx.AbilityManager,
        Flag = CommandFlag.IsSystem
    };
    
    return command.IssueOrder(unit);
}

// 显式开启/关闭
public void ExplicitShieldControl(Unit unit, bool enable)
{
    var command = new Command
    {
        AbilityLink = ScopeData.Ability.ManaShield,
        Index = enable ? CommandIndex.TurnOn : CommandIndex.TurnOff,
        Type = ComponentTagEx.AbilityManager,
        Flag = CommandFlag.IsSystem
    };
    
    command.IssueOrder(unit);
}
#endif
```

### 📡 被动技能（Passive Abilities）

被动技能在添加到单位后自动生效，通常用于属性加成、周期性效果等。

#### 预定义被动技能配置

```csharp
// 法力再生光环数编表定义
public static readonly GameDataAbility ManaRegenAuraData = new(ScopeData.Ability.ManaRegenAura)
{
    DisplayName = "法力再生光环",
    Description = "为周围友军提供法力恢复",
    TargetType = AbilityTargetType.None,
    
    // 周期性效果配置
    PassivePeriod = new FuncTime(1f),  // 每秒触发
    PassivePeriodicEffect = ScopeData.Effect.ManaRegenAura,  // 光环效果
    
    // 属性修改（对技能拥有者）
    Modifications = [
        new UnitPropertyModification
        {
            Property = UnitProperty.ManaRegeneration,
            Value = (_) => 2  // +2法力/秒
        }
    ]
};
```

#### 被动技能管理

```csharp
#if SERVER
// 添加被动技能
public void AddPassiveAbility(Unit unit)
{
    var abilityManager = unit.GetComponent<AbilityManager>();
    if (abilityManager != null)
    {
        // 被动技能添加后自动生效
        abilityManager.Add(ScopeData.Ability.ManaRegenAura);
        Game.Logger.LogInfo("已添加法力再生光环，技能立即生效");
    }
}

// 临时禁用被动技能
public void TemporaryDisablePassive(Unit unit, float duration)
{
    var ability = unit.GetComponent<AbilityManager>()?.Get(ScopeData.Ability.ManaRegenAura);
    if (ability != null)
    {
        ability.IsEnabled = false;
        
        // 定时重新启用
        Game.Delay(TimeSpan.FromSeconds(duration)).ContinueWith(_ =>
        {
            if (ability.IsValid)
            {
                ability.IsEnabled = true;
            }
        });
    }
}
#endif
```

## 🕐 施法时间控制

### ⏱️ SpellCastTime 类

`SpellCastTime` 提供精确的施法时机控制，支持多阶段的施法过程：

```csharp
public class SpellCastTime
{
    /// <summary>前摇时间 - 施法准备阶段</summary>
    public FuncTime Preswing { get; set; } = static (_) => TimeSpan.Zero;
    
    /// <summary>施法时间 - 主要施法阶段</summary>
    public FuncTime Cast { get; set; } = static (_) => TimeSpan.Zero;
    
    /// <summary>引导时间 - 持续施法阶段</summary>
    public FuncTime Channel { get; set; } = static (_) => TimeSpan.Zero;
    
    /// <summary>后摇时间 - 施法后恢复阶段</summary>
    public FuncTime Backswing { get; set; } = static (_) => TimeSpan.Zero;
    
    /// <summary>标准化持续时间 - 攻击间隔精确控制</summary>
    public FuncTime? NormalizedDuration { get; set; }
}
```

#### 不同技能的时间配置示例

```csharp
// 快速技能：瞬发伤害
public static readonly SpellCastTime QuickStrikeTime = new()
{
    Preswing = static (_) => TimeSpan.FromSeconds(0.1f),    // 极短前摇
    Backswing = static (_) => TimeSpan.FromSeconds(0.2f)    // 短后摇
};

// 标准技能：火球术
public static readonly SpellCastTime FireballTime = new()
{
    Preswing = static (_) => TimeSpan.FromSeconds(0.3f),    // 0.3秒前摇
    Cast = static (_) => TimeSpan.FromSeconds(1.5f),        // 1.5秒施法
    Backswing = static (_) => TimeSpan.FromSeconds(0.5f)    // 0.5秒后摇
};

// 引导技能：暴风雪
public static readonly SpellCastTime BlizzardTime = new()
{
    Preswing = static (_) => TimeSpan.FromSeconds(0.5f),    // 0.5秒前摇
    Channel = static (_) => TimeSpan.FromSeconds(5.0f),     // 5秒引导
    Backswing = static (_) => TimeSpan.FromSeconds(0.3f)    // 0.3秒后摇
};

// 攻击技能：武器攻击（使用标准化持续时间）
public static readonly SpellCastTime WeaponAttackTime = new()
{
    Preswing = static (_) => TimeSpan.FromSeconds(0.2f),
    Backswing = static (_) => TimeSpan.FromSeconds(0.4f),
    NormalizedDuration = static (_) => TimeSpan.FromSeconds(1.0f)  // 1秒攻击间隔
};

// 瞬发技能：闪现
public static readonly SpellCastTime BlinkTime = new()
{
    // 完全瞬发，使用默认值（全部为0）
};
```

### 🎯 NormalizedDuration - 攻击间隔精确控制

`NormalizedDuration` 是一个特殊的时间类型，专门用于控制攻击技能的间隔时机，确保攻击节奏的一致性：

```csharp
// 标准化持续时间的攻击配置
public static readonly GameDataAbilityExecute WeaponAttackData = new(ScopeData.Ability.WeaponAttack)
{
    Time = new SpellCastTime
    {
        Preswing = static (_) => TimeSpan.FromSeconds(0.1f),
        Backswing = static (_) => TimeSpan.FromSeconds(0.4f),
        NormalizedDuration = static (_) => TimeSpan.FromSeconds(1.0f)  // 使用标准化持续时间
    },
    
    Cooldown = new FuncTime(0f),  // 无额外冷却，完全由Backswing控制
    
    AbilityExecuteFlags = new AbilityExecuteFlags { IsAttack = true }
};
```

#### NormalizedDuration 的优势

1. **攻击节奏一致性** - 确保不同动画长度的武器有统一的攻击间隔
2. **动画同步** - 自动与攻击动画的节拍点同步
3. **性能优化** - 减少不必要的时间计算开销
4. **平衡性保证** - 避免因动画差异导致的DPS不平衡

### 🎭 动画系统集成

技能系统与动画系统深度集成，支持多种动画触发模式：

```csharp
// 技能配置中的动画控制
public static readonly GameDataAbilityExecute SwordSlashData = new(ScopeData.Ability.SwordSlash)
{
    Time = new SpellCastTime
    {
        Preswing = static (_) => TimeSpan.FromSeconds(0.2f),   // 动画预备时间
        Backswing = static (_) => TimeSpan.FromSeconds(0.6f)   // 动画恢复时间
    },
    
    // 动画配置
    AnimationLink = ScopeData.Animation.SwordSlash,
    AnimationTrigger = AnimationTrigger.OnCast,  // 在Cast阶段开始播放
    
    Effect = ScopeData.Effect.SlashDamage
};
```

#### 动画触发时机

- **OnCast** - 在前摇阶段开始时触发
- **OnChannel** - 在引导阶段开始时触发  
- **OnEffect** - 在效果生效时触发
- **OnComplete** - 在技能完成时触发

## 🎮 目标系统

技能的目标系统决定了技能可以作用于哪些对象：

### 目标类型（AbilityTargetType）

```csharp
public enum AbilityTargetType
{
    None,          // 无目标（自身技能、光环等）
    Unit,          // 单位目标
    Point,         // 地面点目标
    Vector,        // 方向目标
    Instant,       // 瞬发目标（自动选择）
}
```

### 目标验证器示例

```csharp
// 敌方单位验证器配置
public static readonly GameDataValidator EnemyTargetValidator = new(ScopeData.Validator.EnemyTarget)
{
    ValidatorType = ValidatorType.TargetIsEnemy,
    FailureMessage = "目标必须是敌方单位"
};

// 距离验证器配置
public static readonly GameDataValidator RangeValidator = new(ScopeData.Validator.InRange)
{
    ValidatorType = ValidatorType.Range,
    Range = new FuncFloat(500f),
    FailureMessage = "目标超出技能范围"
};

// 法力值验证器配置
public static readonly GameDataValidator ManaValidator = new(ScopeData.Validator.HasMana)
{
    ValidatorType = ValidatorType.HasMana,
    FailureMessage = "法力值不足"
};
```

## 🔧 技能配置

### 技能分类系统

技能可以通过分类进行组织和管理：

```csharp
public enum AbilityCategory
{
    Attack,        // 攻击技能
    Defense,       // 防御技能
    Healing,       // 治疗技能
    Buff,          // 增益技能
    Debuff,        // 减益技能
    Movement,      // 移动技能
    Utility,       // 实用技能
    Ultimate,      // 大招技能
}
```

### 技能标志系统

```csharp
public partial class AbilityFlags
{
    public bool IsHidden { get; set; }              // 是否隐藏
    public bool IgnoreGlobalCooldown { get; set; }  // 忽略全局冷却
    public bool CancelOnMoving { get; set; }        // 移动时取消
    public bool CanCastWhileMoving { get; set; }    // 移动中可施法
    public bool RequiresLineOfSight { get; set; }   // 需要视线
}
```

## ⚔️ 攻击技能特殊机制

### 攻击技能标识

攻击技能必须在 `AbilityExecuteFlags` 中设置 `IsAttack = true`：

```csharp
public partial class AbilityExecuteFlags
{
    /// <summary>
    /// 标记技能是否为攻击技能。
    /// 只有设置为 true 的技能才能被 Attack 指令使用。
    /// </summary>
    public bool IsAttack { get; set; }
    
    /// <summary>
    /// 无目标和向量目标技能是否总是获取目标。
    /// </summary>
    public bool AlwaysAcquireTarget { get; set; }
}
```

### 攻击指令 vs 技能指令

| 指令类型 | AbilityLink字段 | 技能查找方式 | 适用场景 |
|---------|----------------|-------------|----------|
| **Attack指令** | 可选 | 自动查找 `IsAttack=true` 的技能 | 通用攻击、AI攻击 |
| **Execute指令** | 必填 | 使用指定的技能 | 特定技能释放 |

### 自动攻击技能查找

系统按以下优先级查找攻击技能：

1. 如果Attack指令指定了 `AbilityLink`，检查该技能的 `IsAttack` 标志
2. 如果未指定，遍历单位所有技能，寻找第一个满足条件的攻击技能：
   - `IsAttack = true`
   - `IsValid = true`
   - `IsEnabled = true`

## 🔄 技能管理

### AbilityManager 组件

`AbilityManager` 是管理单位技能的核心组件：

```csharp
public class AbilityManager : TagComponent
{
    public void Add(IGameLink<GameDataAbility> abilityLink);
    public void Remove(IGameLink<GameDataAbility> abilityLink);
    public Ability? Get(IGameLink<GameDataAbility> abilityLink);
    public T? Get<T>(IGameLink<GameDataAbility> abilityLink) where T : Ability;
    public IEnumerable<Ability> GetAll();
    public bool Has(IGameLink<GameDataAbility> abilityLink);
    public int Count { get; }
    
    // 事件
    public event EventHandler<AbilityAddedEventArgs> AbilityAdded;
    public event EventHandler<AbilityRemovedEventArgs> AbilityRemoved;
    public event EventHandler<AbilityStateChangedEventArgs> AbilityStateChanged;
}
```

### 技能生命周期事件

```csharp
#if SERVER
// 监听技能事件
public class AbilityEventMonitor
{
    public void MonitorAbilityEvents(Unit unit)
    {
        var abilityManager = unit.GetComponent<AbilityManager>();
        if (abilityManager == null) return;

        // 技能添加事件
        abilityManager.AbilityAdded += (sender, e) =>
        {
            Game.Logger.LogInfo("技能已添加: {Ability}", e.Ability.Data.DisplayName);
        };

        // 技能移除事件
        abilityManager.AbilityRemoved += (sender, e) =>
        {
            Game.Logger.LogInfo("技能已移除: {Ability}", e.Ability.Data.DisplayName);
        };

        // 技能状态变化事件
        abilityManager.AbilityStateChanged += (sender, e) =>
        {
            Game.Logger.LogInfo("技能状态变化: {Ability}, 新状态: {State}", 
                e.Ability.Data.DisplayName, e.NewState);
        };
    }
}
#endif
```

## 🎨 客户端表现

### 技能UI集成

```csharp
#if CLIENT
// 技能按钮控件
public class AbilityButton : Button
{
    private Unit? targetUnit;
    private IGameLink<GameDataAbility>? abilityLink;
    private AbilityExecute? ability;

    public void Initialize(Unit unit, IGameLink<GameDataAbility> link)
    {
        targetUnit = unit;
        abilityLink = link;
        
        // 获取技能实例
        ability = unit.GetComponent<AbilityManager>()?.Get<AbilityExecute>(link);
        
        // 设置按钮外观
        UpdateButtonState();
        
        // 绑定点击事件
        this.Clicked += OnAbilityButtonClicked;
    }

    private void OnAbilityButtonClicked()
    {
        if (targetUnit == null || abilityLink == null) return;

        // 发送技能指令到服务器
        var command = new Command
        {
            AbilityLink = abilityLink,
            Index = CommandIndex.Execute,
            Type = ComponentTagEx.AbilityManager,
            Player = Player.LocalPlayer,
            Flag = CommandFlag.IsUser
        };

        var result = command.IssueOrder(targetUnit);
        if (!result.IsSuccess)
        {
            ShowErrorMessage(result.Error);
        }
    }

    private void UpdateButtonState()
    {
        if (ability == null) return;

        // 更新按钮可用状态
        this.IsEnabled = ability.IsEnabled && ability.CanActivate();
        
        // 更新冷却显示
        if (ability.IsOnCooldown)
        {
            this.CooldownProgress = ability.CooldownProgress;
        }
        
        // 更新法力消耗显示
        this.ManaCost = ability.GetManaCost();
    }
}
#endif
```

### 技能效果可视化

```csharp
#if CLIENT
// 技能释放特效管理
public class AbilityVisualEffects
{
    public void PlayCastEffect(Unit caster, IGameLink<GameDataAbility> abilityLink)
    {
        var abilityData = abilityLink.Data;
        
        // 播放施法特效
        if (abilityData.CastEffect != null)
        {
            EffectManager.Play(abilityData.CastEffect, caster.Position);
        }
        
        // 播放施法音效
        if (abilityData.CastSound != null)
        {
            AudioManager.Play(abilityData.CastSound, caster.Position);
        }
        
        // 显示施法条
        if (abilityData.CastTime?.Cast?.Value > 0)
        {
            ShowCastBar(caster, abilityData.CastTime.Cast.Value);
        }
    }

    private void ShowCastBar(Unit caster, float duration)
    {
        var castBar = new CastBar()
        {
            Duration = duration,
            AbilityName = caster.CurrentAbility?.Data.DisplayName ?? "未知技能"
        };
        
        // 显示在单位头顶
        UIManager.ShowWorldUI(castBar, caster.Position + Vector3.Up * 2f);
    }
}
#endif
```

## 🚀 高级用法：动态数编表创建

> **⚠️ 重要警告**：动态创建数编表是高级功能，需要确保服务端和客户端创建完全一致的数编表，否则会导致同步失败。建议仅在深入理解框架同步机制的情况下使用。

### 动态创建的适用场景

- **程序化生成内容** - 随机生成的技能效果
- **模组系统** - 运行时加载的自定义技能
- **数据驱动配置** - 从外部文件读取的技能数据

### 动态创建示例

```csharp
#if SERVER
// 动态创建程序化技能（高级用法）
public IGameLink<GameDataAbilityExecute> CreateDynamicAbility(string abilityId, float damage, float range)
{
    // ⚠️ 注意：必须确保客户端也创建相同的数编表
    var dynamicLink = new GameLink<GameDataAbilityExecute>(abilityId);
    
    var abilityData = new GameDataAbilityExecute(dynamicLink)
    {
        DisplayName = $"动态技能_{abilityId}",
        TargetType = AbilityTargetType.Unit,
        Range = new FuncFloat(range),
        Time = new SpellCastTime
        {
            Preswing = static (_) => TimeSpan.FromSeconds(0.2f),
            Cast = static (_) => TimeSpan.FromSeconds(1.0f),
            Backswing = static (_) => TimeSpan.FromSeconds(0.5f)
        },
        Effect = CreateDynamicDamageEffect(damage)  // 同样需要动态创建效果
    };
    
    // ⚠️ 关键：客户端必须执行相同的创建逻辑
    Game.DataManager.RegisterDynamicData(dynamicLink, abilityData);
    
    return dynamicLink;
}

// 同步动态数编表到客户端（示例机制）
public void SyncDynamicAbilityToClient(Player player, IGameLink<GameDataAbilityExecute> abilityLink)
{
    var abilityData = abilityLink.Data;
    
    // 发送数编表数据到客户端
    var syncMessage = new AbilityDataSyncMessage
    {
        AbilityId = abilityLink.LinkID,
        DisplayName = abilityData.DisplayName,
        Range = abilityData.Range?.Value ?? 0f,
        CastTime = abilityData.CastTime,
        // ... 其他属性
    };
    
    player.SendMessage(syncMessage);
}
#endif
```

### 动态创建的最佳实践

1. **确保同步一致性** - 服务端和客户端必须创建相同的数编表
2. **使用统一的ID生成** - 确保动态ID在服务端和客户端一致
3. **提供回滚机制** - 当同步失败时能够安全回滚
4. **充分测试** - 动态创建的内容需要更多的测试覆盖

## 🛠️ 实用示例

### 完整的技能系统集成示例

```csharp
#if SERVER
public class CompleteAbilityExample
{
    public async Task SetupUnitWithAbilities(Unit unit)
    {
        var abilityManager = unit.GetComponent<AbilityManager>();
        if (abilityManager == null) return;

        // 添加各种类型的技能
        abilityManager.Add(ScopeData.Ability.BasicAttack);    // 攻击技能
        abilityManager.Add(ScopeData.Ability.Fireball);       // 主动技能
        abilityManager.Add(ScopeData.Ability.ManaShield);     // 切换技能
        abilityManager.Add(ScopeData.Ability.ManaRegenAura);  // 被动技能
        
        Game.Logger.LogInfo("已为单位配置完整技能集合");
    }

    public async Task DemonstrateAbilityUsage(Unit unit, Unit target)
    {
        // 1. 释放火球术
        var fireballResult = await CastAbilityAndWait(unit, ScopeData.Ability.Fireball, target);
        if (fireballResult.IsSuccess)
        {
            Game.Logger.LogInfo("火球术释放成功");
        }

        // 2. 开启魔法护盾
        var shieldResult = ToggleAbility(unit, ScopeData.Ability.ManaShield, true);
        if (shieldResult.IsSuccess)
        {
            Game.Logger.LogInfo("魔法护盾已开启");
        }

        // 3. 等待一段时间后关闭护盾
        await Game.Delay(TimeSpan.FromSeconds(10));
        ToggleAbility(unit, ScopeData.Ability.ManaShield, false);

        // 4. 使用攻击指令（自动选择攻击技能）
        var attackResult = AttackTarget(unit, target);
        if (attackResult.IsSuccess)
        {
            Game.Logger.LogInfo("开始攻击目标");
        }
    }

    private async Task<CmdResult> CastAbilityAndWait(Unit caster, IGameLink<GameDataAbility> abilityLink, Unit target)
    {
        var command = new Command
        {
            AbilityLink = abilityLink,
            Index = CommandIndex.Execute,
            Type = ComponentTagEx.AbilityManager,
            Target = target,
            Flag = CommandFlag.IsSystem
        };

        var result = command.IssueOrder(caster);
        if (!result.IsSuccess)
            return result;

        // 等待技能释放完成
        await WaitForAbilityComplete(caster);
        return CmdResult.Success;
    }

    private CmdResult ToggleAbility(Unit unit, IGameLink<GameDataAbility> abilityLink, bool enable)
    {
        var command = new Command
        {
            AbilityLink = abilityLink,
            Index = enable ? CommandIndex.TurnOn : CommandIndex.TurnOff,
            Type = ComponentTagEx.AbilityManager,
            Flag = CommandFlag.IsSystem
        };

        return command.IssueOrder(unit);
    }

    private CmdResult AttackTarget(Unit attacker, Unit target)
    {
        var command = new Command
        {
            Index = CommandIndex.Attack,  // 不指定AbilityLink，自动查找攻击技能
            Type = ComponentTagEx.AbilityManager,
            Target = target,
            Flag = CommandFlag.IsSystem
        };

        return command.IssueOrder(attacker);
    }

    private async Task WaitForAbilityComplete(Unit unit)
    {
        var orderQueue = unit.GetComponent<OrderQueue>();
        if (orderQueue == null) return;

        // 等待当前指令完成
        while (!orderQueue.IsEmpty)
        {
            var currentOrder = orderQueue.Peek();
            if (currentOrder?.Stage == OrderStage.Completed || 
                currentOrder?.State != OrderState.Normal)
            {
                break;
            }
            
            await Game.DelayFrames(1);
        }
    }
}
#endif
```

## 🔧 API 参考

### GameDataAbilityExecute 类

```csharp
public partial class GameDataAbilityExecute : GameDataAbility
{
    public AbilityExecuteFlags AbilityExecuteFlags { get; set; }
    public SpellCastTime? CastTime { get; set; }
    public FuncFloat? Range { get; set; }
    public FuncTime? Cooldown { get; set; }
    public FuncFloat? ManaCost { get; set; }
    public IGameLink<GameDataEffect>? Effect { get; set; }
    public List<IGameLink<GameDataValidator>> Validators { get; set; }
}
```

### AbilityExecute 类

```csharp
public partial class AbilityExecute : AbilityActive
{
    public bool IsAttack { get; }                    // 是否为攻击技能
    public float Range { get; }                      // 技能范围
    public bool AlwaysAcquireTarget { get; }         // 是否总是获取目标
    public bool CanActivate();                       // 是否可以激活
    public float GetManaCost(uint level = 0);        // 获取法力消耗
    public float GetCooldown(uint level = 0);        // 获取冷却时间
}
```

### AbilityManager 类

```csharp
public class AbilityManager : TagComponent
{
    public void Add(IGameLink<GameDataAbility> abilityLink);
    public void Remove(IGameLink<GameDataAbility> abilityLink);
    public Ability? Get(IGameLink<GameDataAbility> abilityLink);
    public T? Get<T>(IGameLink<GameDataAbility> abilityLink) where T : Ability;
    public IEnumerable<Ability> GetAll();
    public bool Has(IGameLink<GameDataAbility> abilityLink);
    public int Count { get; }
    
    // 事件
    public event EventHandler<AbilityAddedEventArgs> AbilityAdded;
    public event EventHandler<AbilityRemovedEventArgs> AbilityRemoved;
    public event EventHandler<AbilityStateChangedEventArgs> AbilityStateChanged;
}
```

### SpellCastTime 类

```csharp
public class SpellCastTime
{
    public FuncTime Preswing { get; set; }        // 前摇时间
    public FuncTime Cast { get; set; }            // 施法时间
    public FuncTime Channel { get; set; }         // 引导时间
    public FuncTime Backswing { get; set; }       // 后摇时间
    public FuncTime? NormalizedDuration { get; set; }  // 标准化持续时间（攻击间隔控制）
}
```

## 💡 最佳实践

### ✅ 推荐做法

1. **优先使用预定义数编表** - 通过 `ScopeData.Ability.*` 引用编译时定义的技能
2. **合理配置攻击技能** - 确保攻击技能设置 `IsAttack = true`
3. **精确控制施法时间** - 根据技能类型合理配置 `SpellCastTime`
4. **使用标准化时间** - 攻击技能使用 `NormalizedDuration` 确保平衡性
5. **添加验证器** - 为技能添加适当的验证器确保使用条件
6. **监听技能事件** - 通过事件系统响应技能状态变化
7. **客户端同步优化** - 合理配置技能的可见性和隐藏属性
8. **性能优化** - 避免频繁查询技能状态，适当缓存结果

### ❌ 避免的做法

1. **滥用动态创建** - 非必要情况下避免动态创建数编表
2. **忽略同步问题** - 动态创建时忽略服务端客户端同步
3. **配置不一致** - 攻击技能未设置 `IsAttack` 标志
4. **时间配置不当** - 施法时间配置过长或过短影响游戏体验
5. **缺少验证** - 技能缺少必要的使用条件验证
6. **内存泄漏** - 不及时清理技能事件监听器
7. **性能浪费** - 过度复杂的技能配置影响运行性能
8. **调试困难** - 过多动态内容导致问题难以追踪

### 🔧 调试技巧

1. **使用日志系统** - 记录技能添加、移除、状态变化
2. **验证器调试** - 通过验证器的错误信息定位问题
3. **时间轴分析** - 分析技能各阶段的时间消耗
4. **客户端同步检查** - 比较服务端客户端的技能状态
5. **性能监控** - 监控技能系统的CPU和内存使用

## 🔗 相关文档

- [🎮 指令系统](OrderSystem.md) - 技能释放的指令控制机制
- [💫 Buff系统](BuffSystem.md) - 技能效果和状态管理
- [⚡ 效果系统](EffectSystem.md) - 技能产生的各种游戏效果
- [🎭 动画系统](AnimationSystem.md) - 技能动画播放和控制
- [🎯 目标系统](TargetSystem.md) - 技能目标选择和验证
- [🔄 异步编程](../best-practices/AsyncProgramming.md) - 异步技能处理最佳实践

---

> 💡 **提示**: 技能系统是游戏玩法的核心，合理的技能配置能够创造丰富多样的游戏体验。在设计技能时，要平衡施法时间、冷却时间、伤害数值等参数，确保游戏的可玩性和平衡性。 