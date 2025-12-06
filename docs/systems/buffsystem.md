# 🌟 Buff系统（Buff System）

Buff系统是 WasiCore 游戏框架中用于管理临时效果和状态变更的核心系统。它提供了强大而灵活的机制来处理各种增益、减益、光环、诅咒等效果，支持复杂的叠加规则、生命周期管理和动态效果。

## 📋 目录

- [🏗️ 系统概述](#系统概述)
- [📊 GameData表定义](#gamedata表定义)
- [📦 核心组件](#核心组件)
- [🔄 Buff生命周期](#buff生命周期)
- [⏰ 时间管理](#时间管理)
- [📊 叠加系统](#叠加系统)
- [🎯 效果系统](#效果系统)
- [⚡ 属性修改](#属性修改)
- [🛡️ 状态管理](#状态管理)
- [🎮 引导Buff](#引导buff)
- [🤔 验证器系统](#验证器系统)
- [🎨 客户端表现](#客户端表现)
- [🛠️ 实用示例](#实用示例)
- [🚀 高级用法：动态GameData创建](#高级用法动态gamedata创建)
- [🔧 API 参考](#api-参考)
- [💡 最佳实践](#最佳实践)

## 🏗️ 系统概述

### 架构设计

Buff系统采用效果驱动的设计模式，核心架构如下：

```
Effect → EffectBuffAdd → Buff → BuffManager → 属性修改 + 状态变更
  ↓            ↓         ↓         ↓             ↓           ↓
创建效果    添加Buff    Buff实例  Buff管理    动态修改     游戏状态
```

### 核心特性

- ✅ **丰富的效果类型** - 支持即时、持续、周期性、引导等多种效果
- ✅ **灵活的叠加规则** - 支持按施法者叠加、数量限制、时间刷新等机制
- ✅ **完整的生命周期** - 从创建到销毁的完整管理流程
- ✅ **动态属性修改** - 实时的属性增益和减益效果
- ✅ **状态管理** - 单位状态的添加、移除和免疫机制
- ✅ **时间控制** - 精确的持续时间、暂停和恢复功能
- ✅ **验证器系统** - 条件验证和自动启用/禁用
- ✅ **引导机制** - 需要持续施法维持的Buff效果
- ✅ **客户端同步** - 完整的客户端状态同步

## 📊 GameData表定义

### 预定义Buff数据表

在实际项目中，建议在编译时预定义Buff和Effect数据表，以确保服务端客户端的完全一致性：

```csharp
// 文件: GameData/BuffConfigurations.cs
public static class BuffConfigurations  
{
    /// <summary>
    /// 力量增益Buff - 增加攻击力的基础增益效果
    /// </summary>
    public static readonly GameDataBuff StrengthBuff = new()
    {
        DisplayName = new LocalizedString("力量增益", "Strength Boost"),
        Description = new LocalizedString("增加单位的攻击力", "Increases unit's attack power"),
        Icon = Icons.StrengthBoost,
        
        BuffFlags = new BuffFlags
        {
            IsBeneficial = true,
            IsVisible = true,
            CanBeDispelled = true
        },
        
        StackStart = 1,
        StackMax = new FuncUIntConstant(5),
        Duration = new FuncTimeConstant(TimeSpan.FromSeconds(30)),
        
        Modifications = new List<UnitPropertyModification>
        {
            new()
            {
                Property = UnitProperty.AttackDamage,
                Value = (_) => 10.0f
            }
        }
    };

    /// <summary>
    /// 生命恢复Buff - 周期性恢复生命值
    /// </summary>
    public static readonly GameDataBuff RegenerationBuff = new()
    {
        DisplayName = new LocalizedString("生命恢复", "Regeneration"),
        Description = new LocalizedString("持续恢复生命值", "Continuously restores health"),
        Icon = Icons.Regeneration,
        
        BuffFlags = new BuffFlags
        {
            IsBeneficial = true,
            IsVisible = true,
            IsPeriodicBuff = true
        },
        
        Duration = new FuncTimeConstant(TimeSpan.FromSeconds(20)),
        Period = new FuncTimeConstant(TimeSpan.FromSeconds(2)),
        
        PeriodicEffect = EffectConfigurations.HealOverTime
    };

    /// <summary>
    /// 魔法护盾Buff - 吸收伤害的防护效果
    /// </summary>
    public static readonly GameDataBuff MagicalShieldBuff = new()
    {
        DisplayName = new LocalizedString("魔法护盾", "Magical Shield"),
        Description = new LocalizedString("吸收一定数量的伤害", "Absorbs a certain amount of damage"),
        Icon = Icons.MagicalShield,
        
        BuffFlags = new BuffFlags
        {
            IsBeneficial = true,
            IsVisible = true,
            HasCharges = true
        },
        
        StackStart = 1,
        StackMax = new FuncUIntConstant(1),
        Duration = new FuncTimeConstant(TimeSpan.FromSeconds(60)),
        
        // 设置护盾值
        Modifications = new List<UnitPropertyModification>
        {
            new()
            {
                Property = UnitProperty.Shield,
                Value = new FuncFloatConstant(100.0f)
            }
        },
        
        // 护盾被破坏时的特效
        FinalEffect = EffectConfigurations.ShieldBreakEffect
    };

    /// <summary>
    /// 中毒Buff - 造成持续伤害的负面效果
    /// </summary>
    public static readonly GameDataBuff PoisonBuff = new()
    {
        DisplayName = new LocalizedString("中毒", "Poison"),
        Description = new LocalizedString("持续造成毒性伤害", "Deals poison damage over time"),
        Icon = Icons.Poison,
        
        BuffFlags = new BuffFlags
        {
            IsBeneficial = false,
            IsVisible = true,
            IsDebuff = true,
            IsPeriodicBuff = true
        },
        
        StackStart = 1,
        StackMax = new FuncUIntConstant(3),
        Duration = new FuncTimeConstant(TimeSpan.FromSeconds(15)),
        Period = new FuncTimeConstant(TimeSpan.FromSeconds(3)),
        
        PeriodicEffect = EffectConfigurations.PoisonDamage
    };

    /// <summary>
    /// 眩晕Buff - 使目标无法行动
    /// </summary>
    public static readonly GameDataBuff StunBuff = new()
    {
        DisplayName = new LocalizedString("眩晕", "Stun"),
        Description = new LocalizedString("无法移动或施法", "Cannot move or cast spells"),
        Icon = Icons.Stun,
        
        BuffFlags = new BuffFlags
        {
            IsBeneficial = false,
            IsVisible = true,
            IsDebuff = true,
            IsControlEffect = true
        },
        
        Duration = new FuncTimeConstant(TimeSpan.FromSeconds(3)),
        
        AddStates = new List<UnitState>
        {
            UnitState.Stunned,
            UnitState.Uncommandable,
            UnitState.Silenced
        }
    };
}

/// <summary>
/// 预定义Effect数据表配置
/// </summary>
public static class EffectConfigurations
{
    /// <summary>
    /// 持续治疗效果
    /// </summary>
    public static readonly GameDataEffectHeal HealOverTime = new()
    {
        Amount = new FuncFloatConstant(25.0f),
        Type = HealType.Health,
        TargetSelf = true
    };

    /// <summary>
    /// 中毒伤害效果
    /// </summary>
    public static readonly GameDataEffectDamage PoisonDamage = new()
    {
        Amount = new FuncFloatConstant(15.0f),
        DamageType = DamageType.Poison,
        TargetSelf = true
    };

    /// <summary>
    /// 护盾破碎特效
    /// </summary>
    public static readonly GameDataEffectVisual ShieldBreakEffect = new()
    {
        Animation = "ShieldBreak",
        Sound = "shield_break.wav",
        ParticleEffect = "ShieldShatter"
    };

    /// <summary>
    /// 力量Buff添加效果
    /// </summary>
    public static readonly GameDataEffectBuffAdd AddStrengthBuff = new()
    {
        BuffLink = BuffConfigurations.StrengthBuff,
        Target = EffectTarget.Self,
        Duration = new FuncTimeConstant(TimeSpan.FromSeconds(30))
    };

    /// <summary>
    /// 区域治疗效果
    /// </summary>
    public static readonly GameDataEffectAreaHeal AreaHeal = new()
    {
        Amount = new FuncFloatConstant(50.0f),
        Radius = 200.0f,
        MaxTargets = 5,
        TargetFilter = TargetFilter.Allies
    };
}

### 使用预定义数据表

使用预定义的数据表可以确保服务端和客户端的完全一致性：

```csharp
#if SERVER
// 添加力量增益Buff
var effect = new EffectBuffAdd
{
    BuffLink = BuffConfigurations.StrengthBuff,
    Target = EffectTarget.Self,
    Duration = TimeSpan.FromSeconds(30)
};

effect.Apply(caster, target);

// 添加中毒效果
var poisonEffect = new EffectBuffAdd
{
    BuffLink = BuffConfigurations.PoisonBuff,
    Target = EffectTarget.DirectTarget
};

poisonEffect.Apply(caster, enemy);
#endif
```

### 在ScopeData中注册

预定义的数据表需要在ScopeData中注册，以便在代码中引用：

```csharp
// 文件: ScopeData.cs  
public static class ScopeData
{
    public static class Buff
    {
        public static readonly IGameLink<GameDataBuff> Strength = 
            new GameLink<GameDataBuff>(BuffConfigurations.StrengthBuff);
            
        public static readonly IGameLink<GameDataBuff> Regeneration = 
            new GameLink<GameDataBuff>(BuffConfigurations.RegenerationBuff);
            
        public static readonly IGameLink<GameDataBuff> MagicalShield = 
            new GameLink<GameDataBuff>(BuffConfigurations.MagicalShieldBuff);
            
        public static readonly IGameLink<GameDataBuff> Poison = 
            new GameLink<GameDataBuff>(BuffConfigurations.PoisonBuff);
            
        public static readonly IGameLink<GameDataBuff> Stun = 
            new GameLink<GameDataBuff>(BuffConfigurations.StunBuff);
    }
    
    public static class Effect
    {
        public static readonly IGameLink<GameDataEffect> HealOverTime = 
            new GameLink<GameDataEffect>(EffectConfigurations.HealOverTime);
            
        public static readonly IGameLink<GameDataEffect> AddStrengthBuff = 
            new GameLink<GameDataEffect>(EffectConfigurations.AddStrengthBuff);
            
        public static readonly IGameLink<GameDataEffect> AreaHeal = 
            new GameLink<GameDataEffect>(EffectConfigurations.AreaHeal);
    }
}
```

## 📦 核心组件

### GameDataBuff（Buff数编表）

Buff的配置数据，定义Buff的所有静态属性：

```csharp
public partial class GameDataBuff
{
    /// <summary>显示名称和描述</summary>
    public LocalizedString? DisplayName { get; set; }
    public LocalizedString? Description { get; set; }
    public Icon? Icon { get; set; }
    
    /// <summary>Buff行为标志</summary>
    public BuffFlags BuffFlags { get; set; } = new();
    
    /// <summary>叠加配置</summary>
    public uint StackStart { get; set; } = 1;
    public uint? InstanceMax { get; set; }
    public FuncUIntEffect? StackMax { get; set; }
    
    /// <summary>时间配置</summary>
    public FuncTimeEffect? Duration { get; set; }
    public FuncTimeEffect? Period { get; set; }
    
    /// <summary>效果配置</summary>
    public IGameLink<GameDataEffect>? InitialEffect { get; set; }
    public IGameLink<GameDataEffect>? PeriodicEffect { get; set; }
    public IGameLink<GameDataEffect>? ExpireEffect { get; set; }
    public IGameLink<GameDataEffect>? RefreshEffect { get; set; }
    public IGameLink<GameDataEffect>? FinalEffect { get; set; }
    
    /// <summary>属性修改</summary>
    public List<UnitPropertyModification> Modifications { get; set; } = [];
    public List<UnitState> AddStates { get; set; } = [];
    public List<UnitState> RemoveStates { get; set; } = [];
    public List<UnitState> ImmuneStates { get; set; } = [];
    
    /// <summary>验证器</summary>
    public ValidatorEffect? EnableValidator { get; set; }
    public ValidatorEffect? PersistValidator { get; set; }
    
    /// <summary>启用/禁用控制</summary>
    public List<IGameLink<GameDataBuff>> BuffsEnable { get; set; } = [];
    public List<IGameLink<GameDataBuff>> BuffsDisable { get; set; } = [];
}
```

### Buff类（运行时实例）

Buff的运行时实例，管理Buff的动态状态：

```csharp
public partial class Buff : TagComponent
{
    /// <summary>Buff数据链接</summary>
    public IGameLink<GameDataBuff> BuffLink { get; }
    
    /// <summary>生命周期状态</summary>
    public BuffStage Stage { get; }
    public BuffState State { get; }
    public bool IsExpired { get; }
    public bool IsEnabled { get; }
    
    /// <summary>叠加信息</summary>
    public uint Stack { get; }
    public uint InstanceCount { get; }
    
    /// <summary>时间信息</summary>
    public TimeSpan? TimeRemaining { get; }
    public TimeSpan? TimeTotal { get; }
    public DateTime CreationTime { get; }
    
    /// <summary>来源信息</summary>
    public Entity? Caster { get; }
    public Effect? SourceEffect { get; }
    
    /// <summary>核心方法</summary>
    public void Refresh(TimeSpan? newDuration = null);
    public uint AddStack(int stack);
    public uint SetStack(uint stack);
    public void RemoveStack(uint count = 1);
    public void Disable();
    public void Enable();
    public void Destroy();
}
```

### BuffManager类（管理器）

管理单位上所有Buff的组件：

```csharp
public class BuffManager : ObjectManager<Buff, GameDataBuff>, IContextTypeService<Entity, BuffManager>
{
    /// <summary>继承自ObjectManager的基础方法</summary>
    public Buff? Get(IGameLink<GameDataBuff> link);                    // 获取指定链接的第一个Buff
    public HashSet<Buff> GetAll();                                     // 获取所有Buff
    public IEnumerable<T> Get<T>(Func<T, bool> predicate) where T : Buff;  // 按条件获取Buff
    public T? GetOrCreate<T>(IGameLink<GameDataBuff> link) where T : Buff; // 获取或创建Buff
    
    /// <summary>新增的便利方法</summary>
    public bool HasBuff(IGameLink<GameDataBuff> buffLink);             // 检查是否有指定Buff
    public bool HasAnyBuff(params IGameLink<GameDataBuff>[] buffLinks); // 检查是否有任意一个指定Buff
    public bool HasAllBuffs(params IGameLink<GameDataBuff>[] buffLinks); // 检查是否有所有指定Buff
    public int RemoveByPredicate(Func<Buff, bool> predicate);          // 按条件移除Buff
    public int DispelBuffs(BuffPolarity polarity, bool onlyDispellable = true); // 驱散指定极性的Buff
    public int DispelPositiveBuffs(bool onlyDispellable = true);       // 驱散有益Buff
    public int DispelNegativeBuffs(bool onlyDispellable = true);       // 驱散有害Buff
    public void ClearAllDispellableBuffs();                           // 清除所有可驱散的Buff
    public bool HasState(UnitState state);                            // 检查是否有指定状态
    public IEnumerable<Buff> GetCurrentStates();                      // 获取当前所有状态Buff
    
    /// <summary>内部方法（不对外暴露）</summary>
    internal Buff Add(Buff buff);        // 添加Buff实例（仅内部使用）
    
    /// <summary>注意：ObjectManager没有公开的Remove方法</summary>
    /// 要移除Buff，应该调用 buff.Destroy() 方法
    
    /// <summary>事件</summary>
    public event Action<Buff> OnLinkAttached;   // Buff添加事件
    public event Action<Buff> OnLinkDetached;   // Buff移除事件
}
```

### ⚠️ 重要说明：如何添加Buff

**BuffManager本身不提供公开的添加Buff方法**，因为Buff需要完整的执行上下文。正确的添加Buff方式有：

### 📋 最佳实践指南

#### 🎯 添加Buff操作
**推荐方式**：优先使用`Unit.AddBuff()`扩展方法
```csharp
// ✅ 最佳实践：直接使用Unit扩展方法（内部会自动处理BuffManager的创建）
var buff = unit.AddBuff(ScopeData.Buff.Strength, caster);
```

**备选方式**：需要BuffManager特定功能时，使用`GetOrCreateComponent<BuffManager>()`
```csharp
// ✅ 可接受：当需要BuffManager的其他功能时
var buffManager = unit.GetOrCreateComponent<BuffManager>();
var buff = buffManager.AddBuff(ScopeData.Buff.Strength, caster);
```

#### 🔍 查询Buff操作
**查询操作**：使用`GetComponent<BuffManager>()`，失败时返回合适结果
```csharp
// ✅ 查询操作：获取失败可以直接返回
var buffManager = unit.GetComponent<BuffManager>();
if (buffManager == null) return false; // 或 return null 等
```

#### 🗑️ 删除Buff操作
**删除操作**：同样使用`GetComponent<BuffManager>()`
```csharp
// ✅ 删除操作：没有BuffManager说明没有Buff可删除
var buffManager = unit.GetComponent<BuffManager>();
if (buffManager == null) return false;
```

#### 1. 通过效果树添加（推荐）
```csharp
// 在效果树中使用 EffectBuffAdd
var effectBuffAdd = new GameDataEffectBuffAdd
{
    BuffLink = ScopeData.Buff.Strength,
    Duration = TimeSpan.FromSeconds(30),
    Stacks = 1
};
```

#### 2. 通过TriggerEncapsulation便利方法
```csharp
// 直接在Unit上添加
unit.AddBuff(ScopeData.Buff.Strength, caster);

// 或者在BuffManager上添加（扩展方法）
var buffManager = unit.GetOrCreateComponent<BuffManager>();
buffManager.AddBuff(ScopeData.Buff.Strength, caster);

// 批量添加
buffManager.AddBuffs(new[] { 
    ScopeData.Buff.Strength, 
    ScopeData.Buff.Speed 
}, caster);

// 智能添加/刷新
buffManager.AddOrRefreshBuff(ScopeData.Buff.Strength, caster, duration: TimeSpan.FromSeconds(30));
```

## 🔄 Buff生命周期

### 生命周期阶段

每个Buff都会经历以下生命周期阶段，每个阶段可以触发对应的效果：

1. **初始化（Initial）** - Buff首次添加到单位时
2. **周期触发（Periodic）** - 按设定周期重复执行
3. **刷新（Refresh）** - Buff被重新添加或刷新时
4. **过期（Expire）** - Buff自然到期时
5. **最终清理（Final）** - Buff被移除时（包括驱散、覆盖等）

### 基本Buff使用

```csharp
#if SERVER
// 添加预定义的力量增益Buff
var buffManager = unit.GetComponent<BuffManager>();
var buff = buffManager?.AddBuff(ScopeData.Buff.Strength, caster);

if (buff != null)
{
    Game.Logger.LogInfo("力量Buff已添加，层数: {Stack}, 剩余时间: {TimeRemaining}", 
        buff.Stack, buff.TimeRemaining);
}

// 移除特定Buff
if (buffManager?.HasBuff(ScopeData.Buff.Poison) == true)
{
    var poisonBuff = buffManager.Get(ScopeData.Buff.Poison);
    if (poisonBuff != null)
    {
        poisonBuff.Destroy();
        Game.Logger.LogInfo("中毒Buff已移除");
    }
}

// 清除所有负面效果
var dispelledCount = buffManager?.RemoveByPredicate(buff => 
    buff.BuffLink.Data.BuffFlags.IsDebuff) ?? 0;
Game.Logger.LogInfo("驱散了 {Count} 个负面效果", dispelledCount);
#endif
```

### Buff状态监控

```csharp
#if SERVER
// 监听Buff事件
var buffManager = unit.GetComponent<BuffManager>();
if (buffManager != null)
{
    buffManager.BuffAdded += (buff) =>
    {
        Game.Logger.LogInfo("Buff添加: {BuffName} (层数: {Stacks})", 
            buff.BuffLink.FriendlyName, buff.Stack);
    };

    buffManager.BuffRemoved += (buff) =>
    {
        Game.Logger.LogInfo("Buff移除: {BuffName}", buff.BuffLink.FriendlyName);
    };

    buffManager.BuffRefreshed += (buff) =>
    {
        Game.Logger.LogInfo("Buff刷新: {BuffName} (新时间: {TimeRemaining})", 
            buff.BuffLink.FriendlyName, buff.TimeRemaining);
    };
}

// 查询Buff状态
public class BuffStatusChecker
{
    public void CheckBuffStatus(Unit unit)
    {
        var buffManager = unit.GetComponent<BuffManager>();
        if (buffManager == null) return;

        // 检查是否有特定Buff
        bool hasStrength = buffManager.HasBuff(ScopeData.Buff.Strength);
        bool isPoisoned = buffManager.HasBuff(ScopeData.Buff.Poison);
        
        // 获取所有增益效果
        var beneficialBuffs = buffManager.GetBuffs(buff => 
            buff.BuffLink.Data.BuffFlags.IsBeneficial);
            
        // 获取所有可驱散的负面效果
        var dispellableDebuffs = buffManager.GetBuffs(buff => 
            buff.BuffLink.Data.BuffFlags.IsDebuff && 
            buff.BuffLink.Data.BuffFlags.CanBeDispelled);

        Game.Logger.LogInfo("单位状态 - 力量增益: {HasStrength}, 中毒: {IsPoisoned}, " +
            "增益数: {BeneficialCount}, 可驱散负面: {DebuffCount}",
            hasStrength, isPoisoned, beneficialBuffs.Count(), dispellableDebuffs.Count());
    }
}
#endif
// 添加预定义的力量Buff
public CmdResult<Buff> AddStrengthBuff(Unit target, Entity caster)
{
    // 直接使用Unit扩展方法，无需检查BuffManager是否存在
    var buff = target.AddBuff(ScopeData.Buff.Strength, caster);
    if (buff != null)
    {
        Game.Logger.LogInfo("成功为 {Target} 添加力量Buff", target.FriendlyName);
        return buff;  // 隐式转换为CmdResult<Buff>
    }
    
    return CmdError.FailedToAddBuff;  // 隐式转换为CmdResult<Buff>
}

// 移除特定Buff
public bool RemoveStrengthBuff(Unit target)
{
    var buffManager = target.GetComponent<BuffManager>();
    if (buffManager == null) return false;
    
    var strengthBuffs = buffManager.GetBuffs(ScopeData.Buff.Strength);
    var buff = strengthBuffs.FirstOrDefault();
    
    if (buff != null)
    {
        buffManager.RemoveBuff(buff);
        Game.Logger.LogInfo("移除了 {Target} 的力量Buff", target.FriendlyName);
        return true;
    }
    
    return false;
}

// 检查Buff状态
public void CheckBuffStatus(Unit unit)
{
    var buffManager = unit.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    foreach (var buff in buffManager.GetAll())
    {
        Game.Logger.LogInfo("Buff: {Name}, 层数: {Stacks}, 剩余时间: {Duration}",
            buff.BuffLink.FriendlyName,
            buff.Stacks,
            buff.RemainingDuration);
    }
}
#endif
```

## ⏰ 时间管理

### 持续时间控制

```csharp
#if SERVER
// 延长Buff持续时间
public void ExtendBuffDuration(Unit target, IGameLink<GameDataBuff> buffLink, TimeSpan additionalTime)
{
    var buffManager = target.GetComponent<BuffManager>();
    var buffs = buffManager?.GetBuffs(buffLink);
    
    foreach (var buff in buffs ?? Enumerable.Empty<Buff>())
    {
        buff.ExtendDuration(additionalTime);
        Game.Logger.LogInfo("延长Buff {BuffName} 持续时间 {Time} 秒",
            buff.BuffLink.FriendlyName, additionalTime.TotalSeconds);
    }
}

// 暂停和恢复Buff
public void PauseAllBuffs(Unit target)
{
    var buffManager = target.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    foreach (var buff in buffManager.GetAll())
    {
        buff.Pause();
    }
    
    Game.Logger.LogInfo("暂停了 {Target} 的所有Buff", target.FriendlyName);
}

public void ResumeAllBuffs(Unit target)
{
    var buffManager = target.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    foreach (var buff in buffManager.GetAll())
    {
        buff.Resume();
    }
    
    Game.Logger.LogInfo("恢复了 {Target} 的所有Buff", target.FriendlyName);
}
#endif
```

## 📊 叠加系统

### 叠加规则配置

在GameDataBuff中配置叠加行为：

```csharp
// 基础叠加配置示例（在预定义配置中）
public static readonly GameDataBuff StackableBuff = new()
{
    DisplayName = new LocalizedString("可叠加效果", "Stackable Effect"),
    
    // 叠加配置
    StackStart = 1,                                    // 初始层数
    StackMax = new FuncUIntConstant(5),               // 最大层数
    InstanceMax = 3,                                  // 最大实例数（不同施法者）
    
    BuffFlags = new BuffFlags
    {
        StacksByCaster = true,                        // 按施法者分别叠加
        RefreshOnReapply = true,                      // 重新应用时刷新时间
        IndependentStacks = false                     // 层数是否独立计算
    },
    
    // 每层效果
    Modifications = new List<UnitPropertyModification>
    {
        new()
        {
            Property = UnitProperty.AttackDamage,
            Value = new FuncFloatPerStack(2.0f)        // 每层增加2点攻击力
        }
    }
};
```

### 叠加操作

```csharp
#if SERVER
// 添加可叠加的Buff
public void ApplyStackingBuff(Unit target, Entity caster, uint stacksToAdd = 1)
{
    var buffManager = target.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    // 检查是否已有相同施法者的Buff
    var existingBuff = buffManager.Get(ScopeData.Buff.Strength);
    // 如果有多个实例，可能需要进一步筛选
    if (existingBuff?.Caster == caster)
    {
        // 增加层数
        existingBuff.AddStack((int)stacksToAdd);
        Game.Logger.LogInfo("为现有Buff增加 {Stacks} 层，当前层数: {Current}",
            stacksToAdd, existingBuff.Stack);
    }
    else
    {
        // 添加新的Buff实例
        var buff = target.AddBuff(ScopeData.Buff.Strength, caster);
        if (buff != null && stacksToAdd > 1)
        {
            buff.AddStack((int)(stacksToAdd - 1)); // 减1因为已有初始层数
        }
    }
}

// 移除指定层数
public void RemoveStacksFromBuff(Unit target, IGameLink<GameDataBuff> buffLink, uint stacksToRemove)
{
    var buffManager = target.GetComponent<BuffManager>();
    var buffs = buffManager?.GetBuffs(buffLink);
    
    foreach (var buff in buffs ?? Enumerable.Empty<Buff>())
    {
        buff.RemoveStacks(stacksToRemove);
        
        if (buff.Stacks == 0)
        {
            Game.Logger.LogInfo("Buff {Name} 层数归零，自动移除", buff.BuffLink.FriendlyName);
        }
        else
        {
            Game.Logger.LogInfo("从Buff {Name} 移除 {Removed} 层，剩余 {Remaining} 层",
                buff.BuffLink.FriendlyName, stacksToRemove, buff.Stacks);
        }
    }
}
#endif
```

## 🎯 效果系统

### 效果触发时机

Buff可以在不同时机触发效果：

- **InitialEffect** - Buff添加时立即执行
- **PeriodicEffect** - 按周期重复执行
- **RefreshEffect** - Buff刷新时执行
- **ExpireEffect** - Buff自然到期时执行
- **FinalEffect** - Buff被移除时执行（任何原因）

### 使用预定义效果

```csharp
#if SERVER
// 使用技能添加带效果的Buff
public CmdResult CastHealingBlessing(Unit caster, Unit target)
{
    // 添加治疗和Buff的复合效果
    var effect = ScopeData.Effect.HealAndBuff;
    
    var castContext = new EffectCastContext
    {
        Caster = caster,
        Target = target,
        CastPosition = target.Position,
        Level = 1
    };
    
    return effect.Data.Execute(castContext);
}

// 批量处理周期效果
public async Task ProcessPeriodicEffects(Unit unit)
{
    var buffManager = unit.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    var periodicBuffs = buffManager.GetAll()
        .Where(b => b.BuffLink.Data.PeriodicEffect != null);
    
    foreach (var buff in periodicBuffs)
    {
        if (buff.ShouldTriggerPeriodic())
        {
            var effect = buff.BuffLink.Data.PeriodicEffect;
            var context = new EffectCastContext
            {
                Caster = buff.Caster,
                Target = unit,
                Buff = buff
            };
            
            await effect.Data.Execute(context);
        }
    }
}
#endif
```

## ⚡ 属性修改

### 属性修改配置

在GameDataBuff中定义属性修改：

```csharp
// 在预定义配置中设置属性修改
public static readonly GameDataBuff ComplexAttributeBuff = new()
{
    DisplayName = new LocalizedString("复合属性强化", "Complex Attribute Enhancement"),
    
    Modifications = new List<UnitPropertyModification>
    {
        // 增加固定攻击力
        new()
        {
            Property = UnitProperty.AttackDamage,
            Value = (_) => 25
        },
        
        // 增加百分比攻击速度
        new()
        {
            Property = UnitProperty.AttackSpeed,
            SubType = PropertySubType.Percentage // 自定义的子属性，需要在属性公式中自行实现逻辑
            Value = (_) => 20 // 20%
        },
    }
};
```

### 动态属性查询

```csharp
#if SERVER
// 查询单位的有效属性值
public void CheckUnitProperties(Unit unit)
{
    var properties = unit.GetComponent<UnitPropertyManager>();
    if (properties == null) return;
    
    Game.Logger.LogInfo("单位 {Name} 的当前属性:", unit.FriendlyName);
    Game.Logger.LogInfo("  攻击力: {Attack} (基础: {Base})", 
        properties.GetProperty(UnitProperty.AttackDamage),
        properties.GetBaseProperty(UnitProperty.AttackDamage));
    Game.Logger.LogInfo("  生命值: {Health}/{MaxHealth}",
        unit.CurrentHealth,
        properties.GetProperty(UnitProperty.MaxHealth));
    Game.Logger.LogInfo("  移动速度: {Speed}",
        properties.GetProperty(UnitProperty.MovementSpeed));
}

// 获取属性修改的来源分析
public void AnalyzePropertyModifications(Unit unit, UnitProperty property)
{
    var properties = unit.GetComponent<UnitPropertyManager>();
    if (properties == null) return;
    
    var modifications = properties.GetModifications(property);
    
    Game.Logger.LogInfo("属性 {Property} 的修改来源:", property);
    foreach (var mod in modifications)
    {
        Game.Logger.LogInfo("  来源: {Source}, 操作: {Op}, 值: {Value}",
            mod.Source, mod.Operation, mod.Value);
    }
}
#endif
```

## 🛡️ 状态管理

### 单位状态配置

Buff可以添加、移除或提供对单位状态的免疫：

```csharp
// 在预定义配置中设置状态操作
public static readonly GameDataBuff ControlImmunityBuff = new()
{
    DisplayName = new LocalizedString("控制免疫", "Control Immunity"),
    
    // 移除现有的控制状态
    RemoveStates = new List<UnitState>
    {
        UnitState.Stunned,
        UnitState.Silenced,
        UnitState.Rooted,
        UnitState.Slowed
    },
    
    // 提供控制免疫
    ImmuneStates = new List<UnitState>
    {
        UnitState.Stunned,
        UnitState.Silenced,
        UnitState.Rooted,
        UnitState.Feared
    },
    
    Duration = new FuncTimeConstant(TimeSpan.FromSeconds(5))
};

public static readonly GameDataBuff FearBuff = new()
{
    DisplayName = new LocalizedString("恐惧", "Fear"),
    
    // 添加恐惧状态
    AddStates = new List<UnitState>
    {
        UnitState.Feared,
        UnitState.Uncommandable
    },
    
    Duration = new FuncTimeConstant(TimeSpan.FromSeconds(3)),
    
    BuffFlags = new BuffFlags
    {
        IsDebuff = true,
        IsControlEffect = true,
        CanBeDispelled = true
    }
};
```

### 状态查询和管理

```csharp
#if SERVER
// 检查单位状态
public void CheckUnitStates(Unit unit)
{
    var buffManager = unit.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    // 检查特定状态
    bool isStunned = buffManager.HasState(UnitState.Stunned);
    bool isSilenced = buffManager.HasState(UnitState.Silenced);
    bool isImmobilized = buffManager.HasState(UnitState.Immobilized);
    
    Game.Logger.LogInfo("单位 {Name} 状态 - 眩晕: {Stunned}, 沉默: {Silenced}, 禁锢: {Immobilized}",
        unit.FriendlyName, isStunned, isSilenced, isImmobilized);
    
    // 获取所有当前状态
    var currentStates = buffManager.GetCurrentStates();
    Game.Logger.LogInfo("当前所有状态: {States}", string.Join(", ", currentStates));
}

// 清除特定类型的状态
public void ClearControlEffects(Unit target)
{
    var buffManager = target.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    // 移除所有控制效果Buff
    var controlBuffs = buffManager.GetBuffs(buff => 
        buff.BuffLink.Data.BuffFlags.IsControlEffect);
    
    int removedCount = 0;
    foreach (var buff in controlBuffs)
    {
        if (buffManager.Remove(buff))
        {
            removedCount++;
        }
    }
    
    Game.Logger.LogInfo("清除了 {Count} 个控制效果", removedCount);
}

// 应用状态免疫
public void GrantTemporaryImmunity(Unit target, Entity caster, List<UnitState> immuneStates, TimeSpan duration)
{
    // 使用预定义或动态创建免疫Buff
    var buff = buffManager.AddBuff(ScopeData.Buff.ControlImmunity, caster);
    if (buff != null)
    {
        Game.Logger.LogInfo("为 {Target} 提供了 {Duration} 秒的状态免疫", 
            target.FriendlyName, duration.TotalSeconds);
    }
}
#endif
```

## 🎮 引导Buff

引导Buff是需要施法者持续维持的特殊Buff类型：

### 引导Buff配置

```csharp
// 预定义引导Buff
public static readonly GameDataBuff ChanneledHealBuff = new()
{
    DisplayName = new LocalizedString("引导治疗", "Channeled Heal"),
    
    BuffFlags = new BuffFlags
    {
        IsChanneled = true,        // 标记为引导Buff
        IsBeneficial = true,
        RequiresCaster = true      // 需要施法者维持
    },
    
    Duration = new FuncTimeConstant(TimeSpan.FromSeconds(8)),
    Period = new FuncTimeConstant(TimeSpan.FromSeconds(1)),
    
    // 每秒治疗
    PeriodicEffect = ScopeData.Effect.HealOverTime,
    
    // 引导中断时的效果
    FinalEffect = ScopeData.Effect.ChannelInterruptedEffect
};
```

### 引导Buff使用

```csharp
#if SERVER
// 开始引导治疗
public async Task<bool> StartChanneledHeal(Unit caster, Unit target)
{
    // 直接使用Unit扩展方法添加引导Buff
    var channeledBuff = target.AddBuff(ScopeData.Buff.ChanneledHeal, caster);
    if (channeledBuff == null) return false;
    
    // 施法者需要维持引导状态
    caster.AddState(UnitState.Channeling);
    
    try
    {
        // 监控引导状态
        while (channeledBuff.IsValid && !channeledBuff.IsExpired)
        {
            // 检查施法者是否仍能维持引导
            if (!CanMaintainChannel(caster))
            {
                channeledBuff.Destroy(); // 中断引导
                Game.Logger.LogInfo("引导被中断：施法者无法维持");
                break;
            }
            
            await Game.DelayFrames(1);
        }
        
        return channeledBuff.State == BuffState.Completed;
    }
    finally
    {
        caster.RemoveState(UnitState.Channeling);
    }
}

// 检查是否能维持引导
private bool CanMaintainChannel(Unit caster)
{
    // 检查施法者状态
    if (caster.HasState(UnitState.Stunned) || 
        caster.HasState(UnitState.Silenced) ||
        !caster.IsAlive)
    {
        return false;
    }
    
    // 检查距离（如果需要）
    // 检查法力值（如果需要）
    
    return true;
}

// 强制中断所有引导Buff
public void InterruptAllChannels(Unit target)
{
    var buffManager = target.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    var channeledBuffs = buffManager.GetBuffs(buff => 
        buff.BuffLink.Data.BuffFlags.IsChanneled);
    
    foreach (var buff in channeledBuffs)
    {
        buff.Destroy();
        Game.Logger.LogInfo("中断引导Buff: {Name}", buff.BuffLink.FriendlyName);
    }
}
#endif
```

## 🤔 验证器系统

验证器系统允许Buff根据条件自动启用或禁用：

### 验证器配置

```csharp
// 带条件验证的Buff配置
public static readonly GameDataBuff ConditionalBuff = new()
{
    DisplayName = new LocalizedString("条件强化", "Conditional Enhancement"),
    
    // 启用条件：生命值低于50%时启用
    EnableValidator = new ValidatorAnd
    {
        Validators = new List<ValidatorEffect>
        {
            new ValidatorHealthPercent
            {
                ComparisonType = ComparisonType.LessEqual,
                Value = new FuncFloatConstant(0.5f)
            }
        }
    },
    
    // 持续条件：生命值高于20%时保持
    PersistValidator = new ValidatorHealthPercent
    {
        ComparisonType = ComparisonType.Greater,
        Value = new FuncFloatConstant(0.2f)
    },
    
    Modifications = new List<UnitPropertyModification>
    {
        new()
        {
            Property = UnitProperty.AttackDamage,
            SubType = PropertySubType.Percentage // 自定义的子属性，需要在属性公式中自行实现逻辑
            Value = (_) => 50 // 50%攻击力加成
        }
    }
};
```

### 验证器使用

```csharp
#if SERVER
// 添加带验证器的Buff
public void ApplyConditionalBuff(Unit target, Entity caster)
{
    // 直接使用Unit扩展方法添加条件Buff
    var buff = target.AddBuff(ScopeData.Buff.ConditionalBuff, caster);
    if (buff != null)
    {
        Game.Logger.LogInfo("条件Buff已添加，当前启用状态: {Enabled}", buff.IsEnabled);
        
        // 验证器会自动根据条件启用/禁用Buff
    }
}

// 手动检查Buff条件
public void CheckBuffConditions(Unit unit)
{
    var buffManager = unit.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    var conditionalBuffs = buffManager.GetBuffs(buff => 
        buff.BuffLink.Data.EnableValidator != null ||
        buff.BuffLink.Data.PersistValidator != null);
    
    foreach (var buff in conditionalBuffs)
    {
        Game.Logger.LogInfo("Buff {Name} - 启用: {Enabled}, 满足条件: {Valid}",
            buff.BuffLink.FriendlyName, buff.IsEnabled, buff.IsConditionMet);
    }
}
#endif
```

## 🎨 客户端表现

### Buff UI显示

```csharp
#if CLIENT
// Buff图标显示组件
public class BuffIconDisplay : Panel
{
    private readonly Dictionary<Buff, BuffIcon> buffIcons = new();
    
    public void UpdateBuffDisplay(Unit unit)
    {
        var buffManager = unit.GetComponent<BuffManager>();
        if (buffManager == null) return;
        
        // 获取可见的Buff
        var visibleBuffs = buffManager.GetBuffs(buff => 
            buff.BuffLink.Data.BuffFlags.IsVisible);
        
        // 更新显示
        foreach (var buff in visibleBuffs)
        {
            if (!buffIcons.ContainsKey(buff))
            {
                CreateBuffIcon(buff);
            }
            
            UpdateBuffIcon(buff);
        }
        
        // 移除已失效的图标
        var expiredBuffs = buffIcons.Keys.Where(b => !b.IsValid).ToList();
        foreach (var expiredBuff in expiredBuffs)
        {
            RemoveBuffIcon(expiredBuff);
        }
    }
    
    private void CreateBuffIcon(Buff buff)
    {
        var icon = new BuffIcon
        {
            Buff = buff,
            Texture = buff.BuffLink.Data.Icon?.Texture,
            Size = new Size(32, 32)
        };
        
        // 设置工具提示
        icon.ToolTip = CreateBuffTooltip(buff);
        
        // 右键菜单（用于驱散等操作）
        if (buff.BuffLink.Data.BuffFlags.CanBeDispelled)
        {
            icon.ContextMenu = CreateBuffContextMenu(buff);
        }
        
        buffIcons[buff] = icon;
        Children.Add(icon);
    }
    
    private void UpdateBuffIcon(Buff buff)
    {
        if (!buffIcons.TryGetValue(buff, out var icon)) return;
        
        // 更新层数显示
        icon.StackText = buff.Stack > 1 ? buff.Stack.ToString() : null;
        
        // 更新时间显示
        if (buff.TimeRemaining.HasValue)
        {
            icon.TimeText = FormatTimeRemaining(buff.TimeRemaining.Value);
        }
        
        // 更新状态（启用/禁用）
        icon.IsEnabled = buff.IsEnabled;
        icon.Opacity = buff.IsEnabled ? 1.0f : 0.5f;
    }
    
    private string FormatTimeRemaining(TimeSpan time)
    {
        return time.TotalMinutes >= 1 
            ? $"{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.TotalSeconds:F0}";
    }
}

// Buff工具提示
public ToolTip CreateBuffTooltip(Buff buff)
{
    var tooltip = new ToolTip();
    
    var panel = new StackPanel { Orientation = Orientation.Vertical };
    
    // 标题
    panel.Children.Add(new TextBlock
    {
        Text = buff.BuffLink.Data.DisplayName?.ToString(),
        FontWeight = FontWeight.Bold,
        Foreground = buff.BuffLink.Data.BuffFlags.IsBeneficial ? Brushes.Green : Brushes.Red
    });
    
    // 描述
    if (!string.IsNullOrEmpty(buff.BuffLink.Data.Description?.ToString()))
    {
        panel.Children.Add(new TextBlock
        {
            Text = buff.BuffLink.Data.Description.ToString(),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 250
        });
    }
    
    // 层数和时间信息
    var infoText = new List<string>();
    
    if (buff.Stack > 1)
        infoText.Add($"层数: {buff.Stack}");
    
    if (buff.TimeRemaining.HasValue)
        infoText.Add($"剩余: {FormatTimeRemaining(buff.TimeRemaining.Value)}");
    
    if (buff.Caster != null)
        infoText.Add($"施法者: {buff.Caster.FriendlyName}");
    
    if (infoText.Any())
    {
        panel.Children.Add(new TextBlock
        {
            Text = string.Join(" | ", infoText),
            FontStyle = FontStyle.Italic,
            Foreground = Brushes.Gray
        });
    }
    
    tooltip.Content = panel;
    return tooltip;
}
#endif
```

## 🛠️ 实用示例

### AI系统集成

```csharp
#if SERVER
// AI决策中的Buff考量
public class AIBuffAwareness
{
    public bool ShouldUseDispel(Unit aiUnit, Unit target)
    {
        var buffManager = target.GetComponent<BuffManager>();
        if (buffManager == null) return false;
        
        // 检查目标是否有可驱散的负面效果（如果是友军）
        if (target.IsAllyWith(aiUnit))
        {
            var dispellableDebuffs = buffManager.GetBuffs(buff =>
                buff.BuffLink.Data.BuffFlags.IsDebuff &&
                buff.BuffLink.Data.BuffFlags.CanBeDispelled);
            
            return dispellableDebuffs.Any();
        }
        
        // 检查目标是否有可驱散的正面效果（如果是敌军）
        if (target.IsEnemyWith(aiUnit))
        {
            var dispellableBuffs = buffManager.GetBuffs(buff =>
                buff.BuffLink.Data.BuffFlags.IsBeneficial &&
                buff.BuffLink.Data.BuffFlags.CanBeDispelled);
            
            return dispellableBuffs.Any();
        }
        
        return false;
    }
    
    public float CalculateThreatLevel(Unit enemy)
    {
        var buffManager = enemy.GetComponent<BuffManager>();
        if (buffManager == null) return 1.0f;
        
        float threatMultiplier = 1.0f;
        
        // 检查增益效果
        var beneficialBuffs = buffManager.GetBuffs(buff => 
            buff.BuffLink.Data.BuffFlags.IsBeneficial);
        
        foreach (var buff in beneficialBuffs)
        {
            // 根据Buff类型调整威胁等级
            if (buff.BuffLink.Data.Modifications.Any(m => 
                m.Property == UnitProperty.AttackDamage))
            {
                threatMultiplier += 0.2f; // 攻击力增益提高威胁
            }
            
            if (buff.BuffLink.Data.ImmuneStates.Contains(UnitState.Stunned))
            {
                threatMultiplier += 0.3f; // 控制免疫大幅提高威胁
            }
        }
        
        return threatMultiplier;
    }
}
#endif
```

## 🚀 高级用法：动态GameData创建

> **⚠️ 重要警告**：动态创建数编表是高级功能，需要确保服务端和客户端创建完全一致的数编表，否则会导致同步失败。建议仅在深入理解框架同步机制的情况下使用。

### 动态创建的考虑因素

在考虑使用动态创建之前，请评估以下因素：

1. **同步复杂性** - 服务端和客户端必须生成完全相同的数编表
2. **调试难度** - 动态创建的数编表更难追踪和调试
3. **性能影响** - 运行时创建比预定义配置消耗更多资源
4. **维护成本** - 需要额外的同步逻辑和错误处理

### 简单动态Buff示例

```csharp
#if SERVER
// 动态创建简单的属性修改Buff
public GameDataBuff CreateDynamicAttributeBuff(UnitProperty property, float value, TimeSpan duration)
{
    ⚠️ // 警告：这是动态创建示例，需要确保客户端同步
    
    return new GameDataBuff
    {
        DisplayName = new LocalizedString($"动态{property}增益", $"Dynamic {property} Boost"),
        
        Duration = new FuncTimeConstant(duration),
        
        BuffFlags = new BuffFlags
        {
            IsBeneficial = value > 0,
            IsVisible = true,
            CanBeDispelled = true
        },
        
        Modifications = new List<UnitPropertyModification>
        {
            new()
            {
                Property = property,
                Value = (_) => value,
            }
        }
    };
}

// 使用动态创建的Buff
public CmdResult ApplyDynamicBuff(Unit target, Entity caster, UnitProperty property, float value)
{
    var dynamicBuffData = CreateDynamicAttributeBuff(property, value, TimeSpan.FromSeconds(30));
    
    // 注意：需要确保客户端也能访问到相同的数编表
    var buffLink = new GameLink<GameDataBuff>(dynamicBuffData);
    
    // 对于动态Buff，使用GetOrCreateComponent确保BuffManager存在
    var buffManager = target.GetOrCreateComponent<BuffManager>();
    var buff = buffManager.AddBuff(buffLink, caster);
    return buff != null ? CmdResult.Ok : CmdError.FailedToAddBuff;
}
#endif
```

### 动态效果创建

```csharp
#if SERVER
// 动态创建复合效果
public GameDataEffectComplex CreateDynamicHealAndBuffEffect(float healAmount, IGameLink<GameDataBuff> buffToAdd)
{
    ⚠️ // 警告：确保客户端能够创建相同的效果
    
    return new GameDataEffectComplex
    {
        Effects = new List<IGameLink<GameDataEffect>>
        {
            new GameLink<GameDataEffect>(new GameDataEffectHeal
            {
                Amount = new FuncFloatConstant(healAmount),
                Type = HealType.Health
            }),
            new GameLink<GameDataEffect>(new GameDataEffectBuffAdd
            {
                BuffLink = buffToAdd,
                Duration = new FuncTimeConstant(TimeSpan.FromSeconds(20))
            })
        }
    };
}
#endif
```

### 推荐的替代方案

相比动态创建，推荐使用以下替代方案：

```csharp
#if SERVER
// 方案1：使用参数化的预定义配置
public static readonly GameDataBuff ParameterizedBuff = new()
{
    DisplayName = new LocalizedString("参数化增益", "Parameterized Boost"),
    Duration = new FuncTimeCaster(), // 使用施法者的参数
    
    Modifications = new List<UnitPropertyModification>
    {
        new()
        {
            Property = UnitProperty.AttackDamage,
            Value = (e) => e.Level * ... // 基于施法者等级计算
        }
    }
};

// 方案2：创建多个预定义变体
public static class AttributeBuffVariants
{
    public static readonly GameDataBuff StrengthMinor = CreateAttributeBuff(UnitProperty.AttackDamage, 10);
    public static readonly GameDataBuff StrengthMajor = CreateAttributeBuff(UnitProperty.AttackDamage, 25);
    public static readonly GameDataBuff SpeedMinor = CreateAttributeBuff(UnitProperty.MovementSpeed, 50);
    public static readonly GameDataBuff SpeedMajor = CreateAttributeBuff(UnitProperty.MovementSpeed, 100);
    
    private static GameDataBuff CreateAttributeBuff(UnitProperty property, float value)
    {
        return new GameDataBuff
        {
            // ... 标准配置
        };
    }
}

// 方案3：使用组合效果
public CmdResult ApplyFlexibleBuff(Unit target, Entity caster, BuffConfiguration config)
{
    // 根据配置选择合适的预定义Buff
    var buffLink = config.Type switch
    {
        BuffType.StrengthMinor => ScopeData.Buff.StrengthMinor,
        BuffType.StrengthMajor => ScopeData.Buff.StrengthMajor,
        BuffType.SpeedBoost => ScopeData.Buff.SpeedBoost,
        _ => throw new ArgumentException("未知Buff类型")
    };
    
    // 直接使用Unit扩展方法添加Buff
    var buff = target.AddBuff(buffLink, caster);
    return buff != null ? CmdResult.Ok : CmdError.FailedToAddBuff;
}
#endif
```

## 🛠️ TriggerEncapsulation便利方法

TriggerEncapsulation模块提供了便利的扩展方法，让用户可以在不需要复杂效果树的场合直接添加Buff：

### Unit扩展方法

```csharp
public static class UnitBuffExtensions
{
    /// <summary>
    /// 直接为单位添加Buff（仅服务端）
    /// </summary>
    /// <param name="target">目标单位</param>
    /// <param name="buffLink">Buff数据链接</param>
    /// <param name="caster">施法者，可选</param>
    /// <param name="stack">堆叠数量，可选</param>
    /// <param name="duration">持续时间，可选</param>
    /// <returns>创建的Buff实例，失败返回null</returns>
    public static Buff? AddBuff(this Unit target, IGameLink<GameDataBuff> buffLink, 
        Unit? caster = null, uint? stack = null, TimeSpan? duration = null);
}
```

### BuffManager扩展方法

```csharp
public static class BuffManagerExtensions
{
    /// <summary>基础添加方法</summary>
    public static Buff? AddBuff(this BuffManager buffManager, IGameLink<GameDataBuff> buffLink, 
        Unit? caster = null, uint? stack = null, TimeSpan? duration = null);
    
    /// <summary>批量添加方法</summary>
    public static List<Buff> AddBuffs(this BuffManager buffManager, 
        IEnumerable<IGameLink<GameDataBuff>> buffLinks, Unit? caster = null);
    
    /// <summary>智能添加/刷新方法</summary>
    public static Buff? AddOrRefreshBuff(this BuffManager buffManager, IGameLink<GameDataBuff> buffLink, 
        Unit? caster = null, uint? stack = null, TimeSpan? duration = null);
}
```

### 使用示例

```csharp
#if SERVER
// 方式1：直接在Unit上添加
unit.AddBuff(ScopeData.Buff.Strength, caster);

// 方式2：通过BuffManager添加
var buffManager = unit.GetOrCreateComponent<BuffManager>();
var buff = buffManager.AddBuff(ScopeData.Buff.Strength, caster, stack: 3, duration: TimeSpan.FromMinutes(5));

// 方式3：批量添加
var buffsToAdd = new[] {
    ScopeData.Buff.Strength,
    ScopeData.Buff.Speed,
    ScopeData.Buff.Protection
};
var addedBuffs = buffManager.AddBuffs(buffsToAdd, caster);

// 方式4：智能添加/刷新（如果已存在则刷新时间）
buffManager.AddOrRefreshBuff(ScopeData.Buff.Strength, caster, duration: TimeSpan.FromMinutes(10));
#endif
```

### ⚠️ 注意事项

1. **仅服务端可用**：这些方法只能在服务端使用（#if SERVER 条件编译）
2. **效果树上下文缺失**：使用便利方法添加的Buff会丢失完整的效果树信息
3. **适用场景**：仅用于简单的Buff添加，复杂效果应使用EffectBuffAdd
4. **虚拟效果**：内部会创建虚拟的EffectBuffAdd效果来记录施法者

## 🔧 API 参考

### BuffFlags 类

```csharp
public class BuffFlags
{
    public bool IsBeneficial { get; set; }           // 是否为增益效果
    public bool IsDebuff { get; set; }               // 是否为减益效果
    public bool IsVisible { get; set; }              // 是否在UI中显示
    public bool CanBeDispelled { get; set; }         // 是否可被驱散
    public bool IsChanneled { get; set; }            // 是否为引导效果
    public bool IsAura { get; set; }                 // 是否为光环效果
    public bool IsControlEffect { get; set; }        // 是否为控制效果
    public bool IsPeriodicBuff { get; set; }         // 是否有周期效果
    public bool RequiresCaster { get; set; }         // 是否需要施法者维持
    public bool StacksByCaster { get; set; }         // 是否按施法者分别叠加
    public bool RefreshOnReapply { get; set; }       // 重新应用时是否刷新
    public bool HasCharges { get; set; }             // 是否有充能机制
}
```

### BuffState 枚举

```csharp
public enum BuffState
{
    Inactive,       // 未激活
    Active,         // 激活中
    Paused,         // 已暂停
    Expired,        // 已过期
    Completed,      // 已完成
    Cancelled       // 已取消
}
```

### BuffStage 枚举

```csharp
public enum BuffStage
{
    Initial,        // 初始化阶段
    Periodic,       // 周期阶段
    Refresh,        // 刷新阶段
    Expire,         // 过期阶段
    Final          // 最终阶段
}
```

## 💡 最佳实践

### ✅ 推荐做法

1. **优先使用预定义配置** - 在编译时定义Buff和Effect数编表
2. **合理设置叠加规则** - 根据游戏设计选择合适的叠加行为
3. **使用验证器** - 利用验证器系统实现动态的Buff启用条件
4. **正确处理生命周期** - 监听Buff事件并适当响应
5. **优化客户端显示** - 只显示重要的Buff，避免UI过载
6. **使用状态免疫** - 通过状态系统实现控制免疫机制
7. **合理设计引导效果** - 确保引导Buff有适当的中断条件

### ❌ 避免的做法

1. **过度使用动态创建** - 避免不必要的运行时数编表创建
2. **忽略叠加规则** - 不设置适当的叠加限制可能导致性能问题
3. **循环依赖** - 避免Buff之间的相互依赖造成死循环
4. **忽略客户端性能** - 避免创建过多的视觉效果或频繁更新
5. **不处理异常情况** - 确保在施法者死亡等情况下正确清理Buff
6. **属性修改冲突** - 避免不同Buff对同一属性进行冲突的修改

### 🔗 相关文档

- [⚡ 技能系统](AbilitySystem.md) - Buff与技能系统的集成
- [🎯 效果系统](EffectSystem.md) - Effect的详细配置和使用
- [👤 单位系统](UnitSystem.md) - 单位属性和状态管理
- [🤖 AI系统](AISystem.md) - AI如何利用Buff信息进行决策

---

> 💡 **提示**: Buff系统是游戏中实现临时效果的核心机制，正确使用Buff系统可以创造丰富的游戏体验。在设计Buff时，始终考虑游戏平衡性和玩家体验。

在GameDataBuff中配置状态操作：

```csharp
// 控制型Buff配置示例
public static readonly GameDataBuff SilenceBuff = new()
{
    DisplayName = new LocalizedString("沉默", "Silence"),
    Description = new LocalizedString("无法使用技能", "Cannot cast abilities"),
    
    BuffFlags = new BuffFlags
    {
        IsBeneficial = false,
        IsDebuff = true,
        CanBeDispelled = true
    },
    
    Duration = new FuncTimeConstant(TimeSpan.FromSeconds(4)),
    
    // 添加沉默状态
    AddStates = new List<UnitState>
    {
        UnitState.Silenced
    }
};

// 免疫型Buff配置示例  
public static readonly GameDataBuff MagicImmunityBuff = new()
{
    DisplayName = new LocalizedString("魔法免疫", "Magic Immunity"),
    
    BuffFlags = new BuffFlags
    {
        IsBeneficial = true,
        IsVisible = true
    },
    
    Duration = new FuncTimeConstant(TimeSpan.FromSeconds(8)),
    
    // 免疫多种状态
    ImmuneStates = new List<UnitState>
    {
        UnitState.Silenced,
        UnitState.Stunned,
        UnitState.Slowed,
        UnitState.Charmed
    },
    
    // 移除现有的负面状态
    RemoveStates = new List<UnitState>
    {
        UnitState.Silenced,
        UnitState.Stunned
    }
};
```

### 状态检查和操作

```csharp
#if SERVER
// 检查单位状态
public bool CanUnitCastSpells(Unit unit)
{
    return !unit.HasState(UnitState.Silenced) && 
           !unit.HasState(UnitState.Stunned) &&
           unit.IsAlive;
}

// 应用状态控制Buff
public void ApplyControlEffect(Unit caster, Unit target, ControlType controlType)
{
    IGameLink<GameDataBuff> buffToApply = controlType switch
    {
        ControlType.Stun => ScopeData.Buff.Stun,
        ControlType.Silence => ScopeData.Buff.Silence,
        ControlType.Slow => ScopeData.Buff.Slow,
        _ => throw new ArgumentException($"未知的控制类型: {controlType}")
    };
    
    // 直接使用Unit扩展方法添加控制Buff
    var buff = target.AddBuff(buffToApply, caster);
    
    if (buff != null)
    {
        Game.Logger.LogInfo("{Caster} 对 {Target} 施加了 {Control} 效果",
            caster.FriendlyName, target.FriendlyName, controlType);
    }
}

// 清除特定类型的状态
public void DispelNegativeEffects(Unit target)
{
    var buffManager = target.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    // 移除所有负面可驱散的Buff
    int removed = buffManager.DispelBuffs(beneficial: false);
    
    if (removed > 0)
    {
        Game.Logger.LogInfo("从 {Target} 身上移除了 {Count} 个负面效果",
            target.FriendlyName, removed);
    }
}
#endif
```

## 🎮 引导Buff

引导Buff是需要施法者持续维持的特殊Buff类型，当施法者停止引导时Buff会自动移除。

### 引导Buff配置

```csharp
// 引导治疗Buff配置示例
public static readonly GameDataBuff ChanneledHealBuff = new()
{
    DisplayName = new LocalizedString("引导治疗", "Channeled Heal"),
    
    BuffFlags = new BuffFlags
    {
        IsBeneficial = true,
        IsChanneled = true,               // 标记为引导Buff
        RequiresCaster = true,            // 需要施法者维持
        IsVisible = true
    },
    
    // 引导Buff通常有较长的持续时间
    Duration = new FuncTimeConstant(TimeSpan.FromSeconds(10)),
    Period = new FuncTimeConstant(TimeSpan.FromSeconds(0.5f)),
    
    // 周期性治疗效果
    PeriodicEffect = ScopeData.Effect.HealOverTime,
    
    // 引导被打断时的效果
    FinalEffect = ScopeData.Effect.ChannelInterruptedEffect
};
```

### 引导管理

```csharp
#if SERVER
// 开始引导Buff
public async Task<bool> StartChanneledBuff(Unit caster, Unit target, IGameLink<GameDataBuff> channeledBuffLink)
{
    // 检查施法者是否可以引导
    if (!CanStartChanneling(caster))
    {
        return false;
    }
    
    // 添加引导Buff
    var channeledBuff = target.AddBuff(channeledBuffLink, caster);
    
    if (channeledBuff == null)
    {
        return false;
    }
    
    // 施法者开始引导状态
    caster.AddState(UnitState.Channeling);
    
    try
    {
        // 监控引导过程
        while (channeledBuff.IsValid && caster.HasState(UnitState.Channeling))
        {
            // 检查引导是否被打断
            if (ShouldInterruptChannel(caster))
            {
                Game.Logger.LogInfo("{Caster} 的引导被打断", caster.FriendlyName);
                break;
            }
            
            await Game.DelayFrames(1);
        }
    }
    finally
    {
        // 清理引导状态
        caster.RemoveState(UnitState.Channeling);
        
        // 移除引导Buff（如果还存在）
        if (channeledBuff.IsValid)
        {
            buffManager?.RemoveBuff(channeledBuff);
        }
    }
    
    return true;
}

// 检查是否可以开始引导
private bool CanStartChanneling(Unit caster)
{
    return !caster.HasState(UnitState.Silenced) &&
           !caster.HasState(UnitState.Stunned) &&
           !caster.HasState(UnitState.Channeling) &&
           caster.IsAlive;
}

// 检查引导是否应该被打断
private bool ShouldInterruptChannel(Unit caster)
{
    return caster.HasState(UnitState.Silenced) ||
           caster.HasState(UnitState.Stunned) ||
           !caster.IsAlive ||
           caster.IsCastingOtherAbility();
}
#endif
```

## 🤔 验证器系统

验证器系统用于控制Buff的启用/禁用和持续性检查。

### 验证器配置

```csharp
// 条件性Buff配置示例
public static readonly GameDataBuff ConditionalBuff = new()
{
    DisplayName = new LocalizedString("条件强化", "Conditional Enhancement"),
    
    // 启用验证器 - 只有在生命值低于50%时才启用
    EnableValidator = new ValidatorEffect
    {
        ValidatorType = ValidatorType.UnitHealthPercent,
        ComparisonType = ComparisonType.LessOrEqual,
        Value = new FuncFloatConstant(0.5f)
    },
    
    // 持续验证器 - 生命值高于80%时移除Buff
    PersistValidator = new ValidatorEffect
    {
        ValidatorType = ValidatorType.UnitHealthPercent,
        ComparisonType = ComparisonType.LessOrEqual,
        Value = new FuncFloatConstant(0.8f)
    },
    
    Modifications = new List<UnitPropertyModification>
    {
        new()
        {
            Property = UnitProperty.AttackDamage,
            SubType = PropertySubType.Percentage // 自定义的子属性，需要在属性公式中自行实现逻辑
            Value = (_) => 50 // 50%攻击加成
        }
    }
};
```

### 验证器处理

```csharp
#if SERVER
// 手动检查Buff验证器
public void ValidateBuffs(Unit unit)
{
    var buffManager = unit.GetComponent<BuffManager>();
    if (buffManager == null) return;
    
    var buffsToRemove = new List<Buff>();
    
    foreach (var buff in buffManager.GetAll())
    {
        // 检查持续验证器
        if (buff.BuffLink.Data.PersistValidator != null)
        {
            var isValid = EvaluateValidator(buff.BuffLink.Data.PersistValidator, unit, buff);
            if (!isValid)
            {
                buffsToRemove.Add(buff);
                Game.Logger.LogInfo("Buff {Name} 验证失败，将被移除",
                    buff.BuffLink.FriendlyName);
            }
        }
        
        // 检查启用验证器（控制启用/禁用状态）
        if (buff.BuffLink.Data.EnableValidator != null)
        {
            var shouldEnable = EvaluateValidator(buff.BuffLink.Data.EnableValidator, unit, buff);
            if (shouldEnable != buff.IsEnabled)
            {
                if (shouldEnable)
                {
                    buff.Enable();
                }
                else
                {
                    buff.Disable();
                }
            }
        }
    }
    
    // 移除验证失败的Buff
    foreach (var buff in buffsToRemove)
    {
        buffManager.RemoveBuff(buff);
    }
}

// 评估验证器
private bool EvaluateValidator(ValidatorEffect validator, Unit unit, Buff buff)
{
    return validator.ValidatorType switch
    {
        ValidatorType.UnitHealthPercent => 
            CompareValues(unit.HealthPercent, validator.Value.GetValue(unit, buff), validator.ComparisonType),
        ValidatorType.UnitManaPercent => 
            CompareValues(unit.ManaPercent, validator.Value.GetValue(unit, buff), validator.ComparisonType),
        ValidatorType.BuffStacks => 
            CompareValues(buff.Stacks, validator.Value.GetValue(unit, buff), validator.ComparisonType),
        ValidatorType.HasState => 
            unit.HasState((UnitState)validator.Value.GetValue(unit, buff)),
        _ => true
    };
}

// 比较数值
private bool CompareValues(float actual, float expected, ComparisonType comparison)
{
    return comparison switch
    {
        ComparisonType.Equal => Math.Abs(actual - expected) < 0.001f,
        ComparisonType.NotEqual => Math.Abs(actual - expected) >= 0.001f,
        ComparisonType.Greater => actual > expected,
        ComparisonType.GreaterOrEqual => actual >= expected,
        ComparisonType.Less => actual < expected,
        ComparisonType.LessOrEqual => actual <= expected,
        _ => true
    };
}
#endif
```

## 🎨 客户端表现

### Buff图标显示

```csharp
#if CLIENT
// Buff UI显示组件
public class BuffDisplayPanel : Panel
{
    private Unit? displayTarget;
    private Dictionary<Buff, BuffIcon> buffIcons = new();

    public void SetTarget(Unit target)
    {
        displayTarget = target;
        RefreshBuffDisplay();
    }

    private void RefreshBuffDisplay()
    {
        if (displayTarget == null) return;

        var buffManager = displayTarget.GetComponent<BuffManager>();
        if (buffManager == null) return;

        // 清除现有图标
        Children.Clear();
        buffIcons.Clear();

        // 为每个可见Buff创建图标
        foreach (var buff in buffManager.GetAll().Where(b => b.BuffLink.Data.BuffFlags.IsVisible))
        {
            var icon = CreateBuffIcon(buff);
            buffIcons[buff] = icon;
            Children.Add(icon);
        }
    }

    private BuffIcon CreateBuffIcon(Buff buff)
    {
        var icon = new BuffIcon
        {
            BuffData = buff,
            Width = 32,
            Height = 32,
            Margin = new Thickness(2)
        };

        // 设置图标
        if (buff.BuffLink.Data.Icon != null)
        {
            icon.IconSource = buff.BuffLink.Data.Icon;
        }

        // 设置边框颜色（增益/减益）
        icon.BorderBrush = buff.BuffLink.Data.BuffFlags.IsBeneficial 
            ? Brushes.Green 
            : Brushes.Red;

        // 添加工具提示
        icon.ToolTip = CreateBuffTooltip(buff);

        return icon;
    }

    private BuffTooltip CreateBuffTooltip(Buff buff)
    {
        return new BuffTooltip
        {
            DisplayName = buff.BuffLink.Data.DisplayName?.GetLocalizedString() ?? "未知Buff",
            Description = buff.BuffLink.Data.Description?.GetLocalizedString() ?? "",
            Stacks = buff.Stacks,
            RemainingDuration = buff.RemainingDuration
        };
    }
}

// Buff图标控件
public class BuffIcon : Border
{
    public Buff BuffData { get; set; }
    
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        
        // 绘制层数
        if (BuffData.Stacks > 1)
        {
            var stackText = BuffData.Stacks.ToString();
            var formattedText = new FormattedText(stackText, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface("Arial"), 10, Brushes.White);
            
            drawingContext.DrawText(formattedText, new Point(Width - 12, Height - 12));
        }
        
        // 绘制持续时间条
        if (BuffData.RemainingDuration.HasValue)
        {
            var progress = CalculateDurationProgress();
            var progressRect = new Rect(0, Height - 3, Width * progress, 3);
            drawingContext.DrawRectangle(Brushes.Yellow, null, progressRect);
        }
    }
    
    private double CalculateDurationProgress()
    {
        if (!BuffData.RemainingDuration.HasValue) return 1.0;
        
        var total = BuffData.BuffLink.Data.Duration?.GetValue(BuffData.Owner, BuffData) ?? TimeSpan.Zero;
        if (total == TimeSpan.Zero) return 1.0;
        
        return BuffData.RemainingDuration.Value.TotalSeconds / total.TotalSeconds;
    }
}
#endif
```

### 视觉效果同步

```csharp
#if CLIENT
// Buff视觉效果管理器
public class BuffVisualManager : TagComponent
{
    private Dictionary<Buff, List<VisualEffect>> activeEffects = new();

    protected override void OnAdd()
    {
        base.OnAdd();
        
        // 监听Buff变化事件
        var buffManager = Entity.GetComponent<BuffManager>();
        if (buffManager != null)
        {
            buffManager.BuffAdded += OnBuffAdded;
            buffManager.BuffRemoved += OnBuffRemoved;
            buffManager.BuffStacksChanged += OnBuffStacksChanged;
        }
    }

    private void OnBuffAdded(Buff buff)
    {
        // 播放Buff添加的视觉效果
        var visualEffects = new List<VisualEffect>();
        
        if (buff.BuffLink.Data.AddVisualEffect != null)
        {
            var effect = PlayVisualEffect(buff.BuffLink.Data.AddVisualEffect, Entity.Position);
            visualEffects.Add(effect);
        }
        
        // 添加持续性视觉效果
        if (buff.BuffLink.Data.LoopVisualEffect != null)
        {
            var loopEffect = PlayLoopingVisualEffect(buff.BuffLink.Data.LoopVisualEffect, Entity);
            visualEffects.Add(loopEffect);
        }
        
        activeEffects[buff] = visualEffects;
    }

    private void OnBuffRemoved(Buff buff)
    {
        // 停止所有相关的视觉效果
        if (activeEffects.TryGetValue(buff, out var effects))
        {
            foreach (var effect in effects)
            {
                effect.Stop();
            }
            activeEffects.Remove(buff);
        }
        
        // 播放Buff移除的视觉效果
        if (buff.BuffLink.Data.RemoveVisualEffect != null)
        {
            PlayVisualEffect(buff.BuffLink.Data.RemoveVisualEffect, Entity.Position);
        }
    }

    private void OnBuffStacksChanged(Buff buff, uint oldStacks, uint newStacks)
    {
        // 更新基于层数的视觉