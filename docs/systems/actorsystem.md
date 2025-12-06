# 🎭 演员系统（Actor System）

演员系统是 WasiCore 游戏框架的视觉表现层核心组件，专门用于处理游戏世界中的**视觉效果、动画、模型和声音**等表现元素。

## 📋 目录

- [🏗️ 系统概述](#系统概述)
- [🔄 Actor vs Entity 核心区别](#actor-vs-entity-核心区别)
- [🎯 Actor 类型](#actor-类型)
- [🎮 基本用法](#基本用法)
- [⚙️ 作用域系统](#作用域系统)
- [🔗 附着机制](#附着机制)
- [🎨 视觉属性](#视觉属性)
- [🔄 生命周期管理](#生命周期管理)
- [🎯 使用场景](#使用场景)
- [🔧 API 参考](#api-参考)
- [💡 最佳实践](#最佳实践)

## 🏗️ 系统概述

### 设计理念

Actor系统基于**"表现与逻辑分离"**的设计理念：

- **Actor** 专注于**视觉表现**：模型、动画、特效、声音
- **Entity** 专注于**游戏逻辑**：状态、属性、行为、同步

```csharp
/// <summary>
/// Actors are objects that can be displayed on the client side within the game world, 
/// such as effects, models, animations, sounds, and other visual elements.
/// </summary>
public abstract partial class Actor : IActor
```

### 核心特性

- ✅ **轻量级创建** - 客户端可直接创建
- ✅ **灵活的生命周期** - 支持瞬态和持久化
- ✅ **强大的附着系统** - 支持复杂的层次结构
- ✅ **作用域管理** - 统一的上下文和生命周期
- ✅ **视觉属性** - 位置、旋转、缩放、偏移
- ✅ **高性能** - 最小化网络和内存开销

## 🔄 Actor vs Entity 核心区别

### 📊 对比表格

| 特性 | Actor | Entity/Unit |
|------|-------|-------------|
| **主要职责** | 视觉表现 | 游戏逻辑 |
| **创建位置** | 🟢 客户端/服务端 | 🔴 仅服务端 |
| **同步机制** | 🟢 默认不同步 | 🔴 自动同步 |
| **生命周期** | 🟢 瞬态/持久均可 | 🔴 持久化管理 |
| **属性系统** | 🟢 视觉属性 | 🔴 游戏属性 |
| **网络开销** | 🟢 极小 | 🔴 较大 |
| **继承关系** | 🟢 独立基类 | 🔴 Entity → Unit |
| **典型用途** | 特效、模型、动画 | 玩家、NPC、建筑 |

### 🏗️ 架构关系

```
游戏对象架构：

Entity 家族（游戏逻辑）          Actor 家族（视觉表现）
┌─────────────────────┐        ┌─────────────────────┐
│ Entity (逻辑基类)    │        │ Actor (表现基类)     │
├─────────────────────┤        ├─────────────────────┤
│ • Unit (玩家/NPC)   │        │ • ActorModel (模型)  │
│ • Building (建筑)   │        │ • ActorParticle (特效)│
│ • Item (道具)       │        │ • ActorSound (声音)  │
│ • Projectile (投射物)│        │ • ActorText (文本)   │
└─────────────────────┘        └─────────────────────┘
         │                              │
         └─────── 可以附着 ─────────────┘
```

### 🎯 职责分离示例

```csharp
// ✅ 正确的设计模式
public class CombatSystem
{
    #if SERVER
    public void ProcessAttack(Unit attacker, Unit target)
    {
        // 1. 游戏逻辑（Entity负责）
        var damage = CalculateDamage(attacker, target);
        target.TakeDamage(damage);
        
        // 2. 视觉表现（Actor负责）
        CreateHitEffect(attacker, target);
    }
    #endif
    
    private void CreateHitEffect(Unit attacker, Unit target)
    {
        // 在目标位置创建击中特效
        var hitEffect = new ActorParticle(hitEffectLink, false, target);
        hitEffect.Play();
    }
}
```

## 🎯 Actor 类型

### 内置Actor类型

```csharp
// 🎨 模型Actor - 显示3D模型
public class ActorModel : Actor
{
    // 用于显示角色模型、建筑模型等
}

// ✨ 粒子Actor - 特效系统
public class ActorParticle : Actor
{
    // 用于火焰、爆炸、魔法特效等
}

// 🔊 声音Actor - 音频播放
public class ActorSound : Actor
{
    // 用于播放音效、背景音乐等
}

// 📝 文本Actor - 3D文本显示
public class ActorText : Actor
{
    // 用于伤害数字、提示文本等
}

// 🎯 分段Actor - 复杂形状
public class ActorSegmented : Actor
{
    // 用于技能指示器、选择框等
}

// 📏 光束Actor - 连接效果
public class ActorBeam : Actor
{
    // 用于激光、闪电链等连接效果
}
```

### 🎪 特殊Actor类型

```csharp
// 🎭 高亮Actor - 选择和强调
public class ActorHighlight : Actor
{
    // 用于单位选择、建筑高亮等
}

// 🎪 动作Actor - 复杂动画序列
public class ActorAction : Actor
{
    // 用于技能动画、过场动画等
}

// 🎯 网格Actor - 辅助显示
public class ActorGrid : Actor
{
    // 用于建造网格、坐标显示等
}
```

## 🎮 基本用法

### 创建Actor

```csharp
// 🎨 创建模型Actor
var modelActor = new ActorModel(
    link: modelLink,           // 模型数据链接
    skipBirth: false,          // 是否跳过出生动画
    scope: parentScope,        // 作用域
    scene: currentScene,       // 场景
    forcePlayer: localPlayer   // 强制指定玩家
);

// ✨ 创建特效Actor
var effectActor = new ActorParticle(
    link: explosionEffectLink,
    skipBirth: false,
    scope: null,               // 无作用域（独立Actor）
    scene: battleScene
);

// 🔊 创建声音Actor
var soundActor = new ActorSound(
    link: soundEffectLink,
    skipBirth: true,           // 声音通常跳过出生动画
    scope: soundScope
);
```

### 基本操作

```csharp
// 📍 位置操作
actor.Position = new ScenePoint(100, 200, currentScene);
actor.SetOffset(new Vector3(0, 0, 50));  // 偏移

// 🔄 旋转操作
actor.Rotation = new Vector3(0, 45, 0);   // 欧拉角
actor.Facing = new Angle(90);             // 朝向

// 📏 缩放操作
actor.Scale = new Vector3(1.5f, 1.5f, 1.5f);

// 🎯 附着操作
actor.AttachTo(parentActor, "weapon_socket");
actor.Detach();

// 🎮 播放控制（如果支持）
if (actor is IActorPlayable playable)
{
    playable.Play();
    playable.Stop();
    playable.Pause();
}
```

### 🎬 动画调试

对于具有动画功能的Actor，框架提供了专门的调试工具：

```csharp
// 启用动画信息实时显示
AnimationInfoOverlay.EnableOverlay();
```

使用快捷键 `Ctrl + F2` 打开调试界面，启用"Animation Info Overlay"开关后，鼠标悬停在Actor上即可查看实时动画信息。详见 <see cref="调试作弊系统"/> 和 <see cref="模型动画系统"/>。

## ⚙️ 作用域系统

### 作用域概念

Actor的作用域（Scope）提供了**统一的上下文管理**和**生命周期控制**：

```csharp
public interface IActorScope
{
    IActorScopeContext Context { get; }  // 上下文信息
    bool IsTransient { get; }             // 是否瞬态
    bool Add(IActor actor);               // 添加Actor
    bool Remove(IActor actor);            // 移除Actor
}
```

### 作用域类型

#### 1. **瞬态作用域** (`ActorScopeTransient`)

```csharp
// 🔥 瞬态作用域 - 用于临时效果
public class ActorScopeTransient : ActorScope
{
    public override bool IsTransient => true;
    
    // 瞬态Actor会在完成后自动清理
}

// 使用示例
var transientScope = new ActorScopeTransient(context);
var explosionEffect = new ActorParticle(explosionLink, false, transientScope);
// 爆炸效果播放完成后会自动清理
```

#### 2. **持久作用域** (`ActorScopePersist`)

```csharp
// 🏗️ 持久作用域 - 用于长期存在的Actor
public class ActorScopePersist : ActorScope
{
    public override bool IsTransient => false;
    
    // 持久Actor需要手动管理生命周期
}

// 使用示例
var persistScope = new ActorScopePersist(context);
var characterModel = new ActorModel(modelLink, false, persistScope);
// 角色模型会一直存在，直到手动销毁
```

### 作用域上下文

```csharp
public interface IActorScopeContext
{
    ScenePoint Position { get; }          // 位置
    ITarget? Target { get; }              // 目标
    Player Player { get; }                // 玩家
    float? Scale { get; }                 // 缩放
    Angle? Facing { get; }                // 朝向
    Vector3? Volume { get; }              // 体积
    IActorSync? Host { get; }             // 宿主
    bool IsValid { get; }                 // 是否有效
}
```

### 作用域使用示例

```csharp
// 🎯 创建以单位为中心的作用域
public void CreateUnitEffects(Unit unit)
{
    // 单位本身就是作用域上下文
    var unitScope = new ActorScopePersist(unit);
    
    // 在单位身上创建光环效果
    var auraEffect = new ActorParticle(auraLink, false, unitScope);
    auraEffect.AttachTo(unit, "effect_socket");
    
    // 在单位脚下创建阴影
    var shadowEffect = new ActorModel(shadowLink, false, unitScope);
    shadowEffect.Position = unit.Position with { Z = 0 };
}

// 🎪 创建技能效果作用域
public void CreateSkillEffect(Unit caster, Unit target)
{
    var skillContext = new SkillEffectContext(caster, target);
    var skillScope = new ActorScopeTransient(skillContext);
    
    // 施法者特效
    var casterEffect = new ActorParticle(castEffectLink, false, skillScope);
    casterEffect.AttachTo(caster, "hand_socket");
    
    // 目标特效
    var targetEffect = new ActorParticle(hitEffectLink, false, skillScope);
    targetEffect.AttachTo(target, "body_socket");
    
    // 连接特效
    var beamEffect = new ActorBeam(beamLink, false, skillScope);
    beamEffect.SetEndPoints(caster, target);
}
```

## 🔗 附着机制

### 附着系统

Actor支持强大的父子附着机制，允许创建复杂的视觉层次结构：

```csharp
// 🔗 基本附着
bool AttachTo(IActor parent, UTF8String socket = null)
bool Detach(bool resetBearings = false)

// 🎯 插槽附着
actor.AttachTo(parentActor, "weapon_socket");    // 武器插槽
actor.AttachTo(parentActor, "effect_socket");    // 特效插槽
actor.AttachTo(parentActor, "shield_socket");    // 盾牌插槽
```

### 附着配置

```csharp
public class GameDataActor
{
    public UTF8String Socket { get; set; }           // 默认插槽
    public Vector3? Offset { get; set; }             // 偏移
    public Vector3? Rotation { get; set; }           // 旋转
    public bool AttachForwardOnce { get; set; }      // 一次性附着
}
```

### 附着使用示例

```csharp
// 🗡️ 武器系统
public void EquipWeapon(Unit unit, IGameLink<GameDataActor> weaponLink)
{
    var weaponActor = new ActorModel(weaponLink, false, unit);
    
    // 附着到武器插槽
    weaponActor.AttachTo(unit, "weapon_right");
    
    // 武器特效
    var weaponGlow = new ActorParticle(weaponGlowLink, false, unit);
    weaponGlow.AttachTo(weaponActor, "glow_socket");
}

// 🎭 复杂的视觉层次
public void CreateComplexEffect(Unit target)
{
    var rootScope = new ActorScopeTransient(target);
    
    // 主效果
    var mainEffect = new ActorParticle(mainEffectLink, false, rootScope);
    mainEffect.AttachTo(target, "effect_socket");
    
    // 子效果1 - 附着在主效果上
    var subEffect1 = new ActorParticle(subEffectLink1, false, rootScope);
    subEffect1.AttachTo(mainEffect, "sub_socket_1");
    
    // 子效果2 - 附着在主效果上
    var subEffect2 = new ActorParticle(subEffectLink2, false, rootScope);
    subEffect2.AttachTo(mainEffect, "sub_socket_2");
    
    // 声音效果 - 附着在目标上
    var soundEffect = new ActorSound(soundLink, true, rootScope);
    soundEffect.AttachTo(target);
}
```

## 🎨 视觉属性

### 变换属性

```csharp
// 📍 位置
ScenePoint Position { get; set; }        // 世界位置
ScenePoint WorldPosition { get; }        // 世界坐标（只读）
void SetOffset(Vector3 offset);          // 局部偏移

// 🔄 旋转
Angle Facing { get; set; }               // 朝向（2D）
Vector3 Rotation { get; set; }           // 旋转（3D欧拉角）

// 📏 缩放
Vector3 Scale { get; set; }              // 缩放系数
float GroundZ { get; set; }              // 地面高度偏移
```

### 属性使用示例

```csharp
// 🎯 动态调整Actor属性
public async Task AnimateActor(Actor actor)
{
    var originalScale = actor.Scale;
    var originalPosition = actor.Position;
    
    // 缩放动画
    for (float t = 0; t <= 1; t += 0.1f)
    {
        actor.Scale = Vector3.Lerp(originalScale, originalScale * 2, t);
        await Game.DelayFrames(1);
    }
    
    // 位置动画
    var targetPosition = originalPosition with { X = originalPosition.X + 100 };
    for (float t = 0; t <= 1; t += 0.1f)
    {
        actor.Position = ScenePoint.Lerp(originalPosition, targetPosition, t);
        await Game.DelayFrames(1);
    }
    
    // 旋转动画
    for (float angle = 0; angle <= 360; angle += 10)
    {
        actor.Facing = new Angle(angle);
        await Game.DelayFrames(1);
    }
}
```

## 🔄 生命周期管理

### 瞬态 vs 持久

```csharp
/// <summary>
/// 瞬态Actor - 自动清理
/// </summary>
public bool IsTransient => Cache.IsTransient || Scope?.IsTransient == true;

// 🔥 瞬态Actor示例
var explosionEffect = new ActorParticle(explosionLink, false, transientScope);
// 播放完成后自动销毁，无需手动清理

// 🏗️ 持久Actor示例
var characterModel = new ActorModel(characterLink, false, persistScope);
// 需要手动销毁
characterModel.Dispose();
```

### 生命周期事件

```csharp
public partial class Actor : IDisposable
{
    protected virtual void OnDispose() { }
    protected virtual void DelayedInitialization() { }
    
    // 延迟初始化
    protected bool HasDelayedInitialization => false;
}
```

### 生命周期管理示例

```csharp
// 🎭 自定义Actor生命周期
public class CustomEffectActor : ActorParticle
{
    private Timer? _lifetimeTimer;
    
    public CustomEffectActor(IGameLink<GameDataActor> link, bool skipBirth, IActorScope scope) 
        : base(link, skipBirth, scope)
    {
        // 设置生命周期
        SetLifetime(TimeSpan.FromSeconds(5));
    }
    
    private void SetLifetime(TimeSpan duration)
    {
        _lifetimeTimer = new Timer(OnLifetimeExpired, null, duration, TimeSpan.Zero);
    }
    
    private void OnLifetimeExpired(object? state)
    {
        Dispose();
    }
    
    protected override void OnDispose()
    {
        _lifetimeTimer?.Dispose();
        _lifetimeTimer = null;
        base.OnDispose();
    }
}
```

## 🎯 ActorArray 集成

Actor系统与Effect、Buff系统深度集成，通过 `ActorArray` 字段为游戏效果提供统一的视觉表现管理。

### 概述

`ActorArray` 是 `GameDataEffect` 和 `GameDataBuff` 中的字段，用于定义效果执行时自动创建的视觉表现：

```csharp
public class GameDataEffect
{
    /// <summary>
    /// 效果执行时创建的Actor表现数组
    /// </summary>
    public List<IGameLink<GameDataActor>>? ActorArray { get; set; }
}

public class GameDataBuff
{
    /// <summary>
    /// Buff激活时创建的Actor表现数组
    /// </summary>
    public List<IGameLink<GameDataActor>>? ActorArray { get; set; }
}
```

### 创建位置规则

#### 效果目标类型决定表现位置

| 目标类型 | Actor创建位置 | 附着方式 | 示例 |
|----------|-------------|----------|------|
| **单位目标** | 目标单位身上 | 自动附着到单位 | 治疗光环、Buff特效 |
| **点目标** | 世界坐标位置 | 独立存在 | 地面爆炸、陷阱效果 |
| **Buff** | 必然附着单位 | 始终跟随单位 | 护盾特效、状态指示 |

#### 单位ActorArray字段

单位本身也拥有 `ActorArray` 字段，用于定义单位的**附属视觉表现组件**：

```csharp
public class GameDataUnit
{
    /// <summary>
    /// 单位的附属视觉表现Actor数组（不包括单位本体模型）
    /// </summary>
    public List<IGameLink<GameDataActor>>? ActorArray { get; set; }
}
```

**重要说明：** 单位的 `ActorArray` 仅影响**附属表现**，单位本体的模型表现由单独的字段管理。

**单位ActorArray的特点：**

- **创建时机**：附属表现会在单位创建时自动附加到单位身上
- **附着关系**：所有附属Actor都直接附着到该单位
- **生命周期**：跟随单位的完整生命周期
- **用途范围**：装备武器、护甲显示、魔法光环、状态指示器、特殊效果等

```csharp
// 🏰 单位附属表现配置示例
var heroUnitLink = new GameLink<GameDataUnit, GameDataUnit>("vampire_hero");
var heroUnitData = new GameDataUnit(heroUnitLink)
{
    Name = "吸血鬼英雄",
    Model = heroMainModelLink,  // 单位本体模型（独立字段）
    
    // 单位的附属视觉表现配置
    ActorArray = new List<IGameLink<GameDataActor>>
    {
        CreateWeaponActor(),           // 手持武器显示
        CreateArmorGlowActor(),        // 护甲光效
        CreateStatusAuraActor(),       // 状态光环
        CreateLevelIndicatorActor()    // 等级指示器
    },
    // 其他单位属性...
};
```

**附属表现示例：**
- 🗡️ **武器装备**：手持的剑、法杖、盾牌等
- ✨ **魔法光环**：治疗光环、护盾特效、buff指示
- 🛡️ **装备效果**：护甲光芒、宝石闪烁等
- 📊 **状态指示**：生命条、魔法条、等级标识
- 🎯 **特殊标记**：选中指示器、目标标记等

#### 位置配置示例

```csharp
// 🎯 单位目标效果 - 表现附着到目标
var healEffectLink = new GameLink<GameDataEffect, GameDataEffectUnitModifyVital>("heal_effect");
var healEffectData = new GameDataEffectUnitModifyVital(healEffectLink)
{
    Name = "治疗效果",
    ActorArray = new List<IGameLink<GameDataActor>>
    {
        CreateHealingAura(),     // 附着到目标单位
        CreateHealingSparkle()   // 附着到目标单位
    }
};

// 🌍 点目标效果 - 表现创建在世界坐标
var earthquakeLink = new GameLink<GameDataEffect, GameDataEffectDamage>("earthquake");
var earthquakeData = new GameDataEffectDamage(earthquakeLink)
{
    Name = "地震",
    TargetType = TargetType.Point,  // 点目标
    ActorArray = new List<IGameLink<GameDataActor>>
    {
        CreateGroundCrack(),     // 在指定位置创建
        CreateDustCloud(),       // 在指定位置创建
        CreateRumbleSound()      // 在指定位置播放
    }
};
```

### 播放模式规则

#### 瞬时效果 vs 持续效果

```csharp
// 🔥 瞬时效果（IsTransient = true）
public class InstantExplosion : GameDataEffectDamage
{
    public override bool IsTransient => true;
    
    // 所有Actor强制一次性播放
    public List<IGameLink<GameDataActor>> ActorArray => new()
    {
        explosionFlash,    // ForceOneShot = true
        explosionSound,    // ForceOneShot = true
        shockwave         // ForceOneShot = true
    };
}

// 🔄 持续效果（IsTransient = false）
public class PersistentAura : GameDataEffectPersist
{
    public override bool IsTransient => false;
    
    // Actor根据自身配置决定播放模式
    public List<IGameLink<GameDataActor>> ActorArray => new()
    {
        auraLoop,         // 持续播放直到效果结束
        activationFlash,  // 一次性播放
        ambientSound      // 循环播放
    };
}
```

#### Actor配置的播放属性

```csharp
// ✨ 一次性表现 - 适用于瞬间效果
var flashActor = new GameDataActorParticle(flashLink)
{
    AutoPlay = true,
    KillOnFinish = true,     // 播放完成后自动销毁
    ForceOneShot = true,     // 强制一次性播放
    Duration = TimeSpan.FromSeconds(0.5)
};

// 🔄 循环表现 - 适用于持续效果
var loopActor = new GameDataActorParticle(loopLink)
{
    AutoPlay = true,
    KillOnFinish = false,    // 需要手动停止
    ForceOneShot = false,    // 允许循环播放
    Duration = null          // 无限循环
};
```

### 投射物命中表现

投射物效果提供专门的命中表现机制：

```csharp
public class ProjectileWithImpact : GameDataEffectLaunchMissile
{
    // 🚀 投射物飞行表现（在投射物单位上）
    public IGameLink<GameDataUnit> Missile { get; set; }  // 投射物单位自带ActorArray
    
    // 💥 命中表现（在命中位置创建）
    public List<IGameLink<GameDataActor>>? ImpactActors { get; set; }
    
    // 🎯 命中后效果（可能有自己的ActorArray）
    public IGameLink<GameDataEffect>? ImpactEffect { get; set; }
}
```

#### 投射物完整示例

```csharp
public class FireballProjectileSystem : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
    }
    
    private static void OnGameDataInitialization()
    {
        // 🚀 火球投射物单位
        var fireballMissileLink = new GameLink<GameDataUnit, GameDataUnit>("fireball_missile");
        var fireballMissileData = new GameDataUnit(fireballMissileLink)
        {
            Name = "火球",
            ActorArray = new List<IGameLink<GameDataActor>>
            {
                CreateFireballTrail(),   // 飞行轨迹特效
                CreateFireballCore(),    // 火球核心
                CreateWhooshSound()      // 飞行音效
            }
        };
        
        // 💥 投射物效果配置
        var projectileLink = new GameLink<GameDataEffect, GameDataEffectLaunchMissile>("fireball_launch");
        var projectileData = new GameDataEffectLaunchMissile(projectileLink)
        {
            Name = "火球发射",
            Missile = fireballMissileLink,
            Speed = (effect) => 800f,
            
            // ⚡ 命中位置表现（在投射物命中点创建）
            ImpactActors = new List<IGameLink<GameDataActor>>
            {
                CreateExplosionEffect(),    // 爆炸特效
                CreateScorchMark(),         // 焦痕标记
                CreateExplosionSound()      // 爆炸音效
            },
            
            // 🎯 命中后执行的伤害效果（有自己的表现）
            ImpactEffect = CreateFireballDamageEffect()
        };
        
        // 🔥 伤害效果（在目标单位上创建表现）
        var damageLink = new GameLink<GameDataEffect, GameDataEffectDamage>("fireball_damage");
        var damageData = new GameDataEffectDamage(damageLink)
        {
            Name = "火球伤害",
            Amount = (effect) => 200,
            ActorArray = new List<IGameLink<GameDataActor>>
            {
                CreateBurnEffect(),      // 燃烧效果（附着到目标）
                CreateDamageNumber(),    // 伤害数字
                CreateHitFlash()         // 受击闪光
            }
        };
    }
}
```

### 生命周期管理

#### 自动生命周期

Actor的生命周期由其所属的Effect或Buff自动管理：

```csharp
// 🎭 Effect生命周期
Effect.Execute()
    → 创建 ActorArray 中的所有Actor
    → 根据效果类型设置播放模式

Effect.Update()
    → 持续效果：Actor继续存在
    → 瞬时效果：Actor可能已完成

Effect.Complete()
    → 停止并清理所有相关Actor
    → 释放资源

// 🛡️ Buff生命周期
Buff.Apply()
    → 创建 ActorArray 中的所有Actor
    → Actor附着到目标单位

Buff.Update()
    → Actor持续播放表现
    → 根据Buff状态调整

Buff.Remove()
    → 清理所有相关Actor
    → 可选播放移除表现
```

#### 优雅消失流程

当Effect、Buff或Unit消失时，其关联的Actor不会立即移除，而是会执行优雅的消失流程：

```csharp
// 🎭 优雅消失流程概述

主体消失触发
    ↓
开始优雅消失流程
    ↓
各Actor按配置执行消失动画
    ↓
    ├─ 粒子Actor → 淡出效果
    ├─ 音效Actor → 音量渐减
    ├─ 模型Actor → 播放Death动画
    └─ 其他Actor → 自定义消失流程
    ↓
所有Actor完成消失后清理
```

#### 模型死亡动画配置

模型Actor的消失流程由模型数据的 `BirthStandDeathAnimation` 配置控制：

```csharp
public class GameDataModel
{
    /// <summary>
    /// 出生-待机-死亡动画配置
    /// </summary>
    public BirthStandDeathAnimation? BirthStandDeathAnimation { get; set; }
}

public class BirthStandDeathAnimation
{
    public Animation? BirthAnimation { get; set; }  // 出生动画
    public Animation? StandAnimation { get; set; }  // 待机动画
    public Animation? DeathAnimation { get; set; }  // 死亡动画
}

// 🎭 武器模型死亡流程示例（单位附属表现）
public class WeaponActorSystem : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
    }
    
    private static void OnGameDataInitialization()
    {
        // 武器模型配置（附属表现）
        var weaponModelLink = new GameLink<GameDataModel, GameDataModel>("enchanted_sword_model");
        var weaponModelData = new GameDataModel(weaponModelLink)
        {
            Asset = "weapons/sword/enchanted_sword.prefab"u8,
            BirthStandDeathAnimation = new BirthStandDeathAnimation
            {
                BirthAnimation = "sword_materialize"u8,   // 武器出现动画
                StandAnimation = "sword_glow_idle"u8,     // 武器待机发光
                DeathAnimation = "sword_dissolve"u8       // 武器消失动画
            }
        };
        
        // 武器附属Actor（在单位ActorArray中使用）
        var weaponActorLink = new GameLink<GameDataActor, GameDataActorModel>("enchanted_sword_actor");
        var weaponActorData = new GameDataActorModel(weaponActorLink)
        {
            Model = weaponModelLink,
            Socket = "weapon_right"u8,     // 附着到右手武器插槽
            PlayDeathAnimation = true,     // 启用死亡动画播放
            AutoPlay = true
        };
        
        // 在单位配置中使用武器附属表现
        var heroUnitData = new GameDataUnit(heroUnitLink)
        {
            Name = "战士",
            Model = heroMainModelLink,  // 单位本体模型
            ActorArray = new List<IGameLink<GameDataActor>>
            {
                weaponActorLink,        // 附属武器表现
                // 其他附属表现...
            }
        };
    }
}
```

### 性能优化

#### 1. 条件表现

```csharp
// 根据游戏设置调整表现复杂度
public static class EffectQualityManager
{
    public enum EffectQuality
    {
        Low,     // 基础表现
        Medium,  // 标准表现
        High,    // 完整表现
        Ultra    // 超级表现
    }
    
    public static EffectQuality CurrentQuality { get; set; } = EffectQuality.Medium;
    
    public static List<IGameLink<GameDataActor>> GetQualityAdjustedActors(
        IGameLink<GameDataActor> basic,
        IGameLink<GameDataActor>? enhanced = null,
        IGameLink<GameDataActor>? ultra = null)
    {
        var actors = new List<IGameLink<GameDataActor>> { basic };
        
        if (CurrentQuality >= EffectQuality.Medium && enhanced != null)
        {
            actors.Add(enhanced);
        }
        
        if (CurrentQuality >= EffectQuality.Ultra && ultra != null)
        {
            actors.Add(ultra);
        }
        
        return actors;
    }
}
```

#### 2. 距离优化

```csharp
// 根据距离决定表现精度
public static class DistanceBasedEffects
{
    public static List<IGameLink<GameDataActor>> GetDistanceOptimizedActors(
        Vector3 effectPosition,
        Player viewer,
        IGameLink<GameDataActor> nearActor,
        IGameLink<GameDataActor> farActor)
    {
        var viewerPosition = viewer.MainUnit?.Position.ToVector3() ?? Vector3.Zero;
        var distance = Vector3.Distance(effectPosition, viewerPosition);
        
        // 近距离使用高质量表现，远距离使用简化表现
        return distance < 500f 
            ? new List<IGameLink<GameDataActor>> { nearActor }
            : new List<IGameLink<GameDataActor>> { farActor };
    }
}
```

### 调试工具

#### 表现调试器

```csharp
#if DEBUG
public static class ActorArrayDebugger
{
    public static void LogActorArrayCreation(Effect effect)
    {
        var actorCount = effect.Cache.ActorArray?.Count ?? 0;
        Game.Logger.LogDebug("Effect {name} creating {count} actors", 
            effect.Cache.Name, actorCount);
        
        foreach (var actorLink in effect.Cache.ActorArray ?? Enumerable.Empty<IGameLink<GameDataActor>>())
        {
            var actorData = actorLink.Data;
            Game.Logger.LogDebug("  - Creating actor: {name} ({type})", 
                actorData?.Name ?? "Unknown", 
                actorData?.GetType().Name ?? "Unknown");
        }
    }
    
    public static void LogActorLifecycle(Actor actor, string operation)
    {
        Game.Logger.LogDebug("Actor {name} - {operation} at {position}", 
            actor.Cache?.Name ?? "Unknown",
            operation,
            actor.Position);
    }
}
#endif
```

### 最佳实践

#### ✅ 推荐做法

1. **正确的表现分类**
   - 瞬时效果使用一次性表现
   - 持续效果使用循环表现
   - 投射物分别配置飞行和命中表现

2. **性能优化**
   - 根据距离调整表现质量
   - 避免过多同时活跃的Actor

3. **生命周期管理**
   - 依赖框架自动管理Actor生命周期
   - 及时清理不需要的表现
   - 避免Actor泄漏

#### ❌ 避免的做法

1. **表现配置错误**
   ```csharp
   // ❌ 错误：瞬时效果配置持续表现
   var instantEffect = new GameDataEffectDamage(link)
   {
       IsTransient = true,
       ActorArray = new[] { loopingActor } // 应该是一次性表现
   };
   ```

2. **性能问题**
   ```csharp
   // ❌ 错误：为小效果创建过多表现
   ActorArray = new List<IGameLink<GameDataActor>>
   {
       // 对于简单伤害过于复杂
       explosionEffect, shockwaveEffect, debrisEffect,
       smokeEffect, fireEffect, sparkEffect, soundEffect
   }
   ```

3. **生命周期混乱**
   ```csharp
   // ❌ 错误：手动管理应该自动管理的Actor
   public void OnEffectEnd(Effect effect)
   {
       // 框架会自动处理，无需手动清理
       foreach (var actor in effect.CreatedActors)
       {
           actor.Dispose(); // 可能导致重复清理
       }
   }
   ```

## 🎯 使用场景

### ✅ 使用Actor的场景

#### 1. **视觉特效**
```csharp
// 🔥 爆炸效果
var explosion = new ActorParticle(explosionLink, false, transientScope);
explosion.Position = bombPosition;

// ✨ 技能特效
var skillEffect = new ActorParticle(skillEffectLink, false, casterScope);
skillEffect.AttachTo(caster, "hand_socket");

// 🌟 环境特效
var ambientEffect = new ActorParticle(ambientLink, false, sceneScope);
ambientEffect.Position = forestPosition;
```

#### 2. **模型显示**
```csharp
// 🏗️ 装饰模型
var decoration = new ActorModel(decorationLink, false, sceneScope);
decoration.Position = decorationSpot;

// 🎯 预览模型
var preview = new ActorModel(buildingLink, false, uiScope);
preview.Position = mousePosition;
preview.Scale = new Vector3(0.8f, 0.8f, 0.8f);
```

#### 3. **UI绑定的3D元素**
```csharp
// 🎪 角色预览
var characterPreview = new ActorModel(characterLink, false, uiScope);
characterPreview.Position = previewPosition;
characterPreview.Rotation = new Vector3(0, 45, 0);

// 🎨 武器展示
var weaponDisplay = new ActorModel(weaponLink, false, uiScope);
weaponDisplay.AttachTo(characterPreview, "weapon_socket");
```

#### 4. **声音效果**
```csharp
// 🔊 音效播放
var soundEffect = new ActorSound(soundLink, true, transientScope);
soundEffect.Position = soundPosition;

// 🎵 环境音效
var ambientSound = new ActorSound(ambientSoundLink, false, sceneScope);
ambientSound.Position = ambientPosition;
```

#### 5. **辅助显示**
```csharp
// 📏 建造网格
var buildGrid = new ActorGrid(gridLink, false, uiScope);
buildGrid.Position = buildArea;

// 🎯 技能指示器
var skillIndicator = new ActorSegmented(indicatorLink, false, uiScope);
skillIndicator.Position = targetArea;

// 💬 飘字效果
var damageText = new ActorText(damageTextLink, false, transientScope);
damageText.Position = hitPosition;
```

### ❌ 不应该使用Actor的场景

#### 1. **有状态的游戏对象**
```csharp
// ❌ 错误：敌人应该是Unit，不是Actor
var enemy = new EnemyActor();
enemy.Health = 100;  // Actor没有游戏逻辑属性！

// ✅ 正确：使用Unit
#if SERVER
var enemy = enemyData.CreateUnit(player, position, facing);
enemy.SetProperty(PropertyUnit.Health, 100f);
#endif
```

#### 2. **需要同步的对象**
```csharp
// ❌ 错误：需要同步的建筑应该是Entity
var building = new BuildingActor();
building.SyncToAllPlayers();  // Actor不支持同步！

// ✅ 正确：使用Entity
#if SERVER
var building = buildingData.CreateUnit(player, position, facing);
building.SetSyncType(SyncType.All);
#endif
```

#### 3. **复杂的游戏逻辑**
```csharp
// ❌ 错误：复杂逻辑应该在Entity中
var spell = new SpellActor();
spell.CalculateDamage();  // Actor不处理游戏逻辑！
spell.ApplyEffect();

// ✅ 正确：使用Ability系统
#if SERVER
var spell = spellData.CreateAbility(caster);
spell.Execute(target);
#endif
```

## 🔧 API 参考

### 核心类

#### Actor 基类
```csharp
public abstract partial class Actor : IActor
{
    // 属性
    public Player Player { get; set; }
    public IActorScope? Scope { get; internal set; }
    public IActorScopeContext? Context { get; }
    public IActor? Parent { get; protected set; }
    public bool IsTransient { get; }
    
    // 变换
    public ScenePoint Position { get; set; }
    public ScenePoint WorldPosition { get; }
    public Angle Facing { get; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; }
    public float GroundZ { get; set; }
    
    // 附着
    public bool AttachTo(IActor parent, UTF8String socket = null);
    public virtual bool Detach(bool resetBearings = false);
    public void SetOffset(Vector3 offset);
}
```

#### 具体Actor类型
```csharp
// 模型Actor
public class ActorModel : Actor
{
    public ActorModel(IGameLink<GameDataActor> link, bool skipBirth, IActorScope scope, Scene scene = null, Player forcePlayer = null);
}

// 粒子Actor
public class ActorParticle : Actor
{
    public ActorParticle(IGameLink<GameDataActor> link, bool skipBirth, IActorScope scope, Scene scene = null, Player forcePlayer = null);
}

// 声音Actor
public class ActorSound : Actor
{
    public ActorSound(IGameLink<GameDataActor> link, bool skipBirth, IActorScope scope, Scene scene = null, Player forcePlayer = null);
}

// 光束Actor
public class ActorBeam : Actor, IActorDualEndPoints
{
    public void SetEndPoints(ITarget start, ITarget end);
}
```

### 作用域系统

#### 作用域接口
```csharp
public interface IActorScope
{
    IActorScopeContext Context { get; }
    bool IsTransient { get; }
    bool Add(IActor actor);
    bool Remove(IActor actor);
}

public interface IActorScopeContext
{
    ScenePoint Position { get; }
    ITarget? Target { get; }
    Player Player { get; }
    float? Scale { get; }
    Angle? Facing { get; }
    bool IsValid { get; }
}
```

#### 作用域实现
```csharp
// 瞬态作用域
public class ActorScopeTransient : ActorScope
{
    public ActorScopeTransient(IActorScopeContext context);
    public override bool IsTransient => true;
}

// 持久作用域
public class ActorScopePersist : ActorScope
{
    public ActorScopePersist(IActorScopeContext context);
    public override bool IsTransient => false;
}
```

### 常用接口

```csharp
// 可播放接口
public interface IActorPlayable
{
    void Play();
    void Stop();
    void Pause();
}

// 可高亮接口
public interface IActorColorizable
{
    void SetHighlight(bool enabled);
    void SetHighlightColor(Color color);
}

// 双端点接口
public interface IActorDualEndPoints
{
    void SetEndPoints(ITarget start, ITarget end);
}
```

## 💡 最佳实践

### ✅ 推荐做法

1. **明确职责分离**
   - Actor负责视觉表现
   - Entity负责游戏逻辑
   - 避免混合使用

2. **合理选择作用域**
   - 临时效果使用瞬态作用域
   - 长期存在使用持久作用域
   - 利用作用域统一管理生命周期

3. **优化性能**
   - 使用Actor池避免频繁创建
   - 瞬态Actor自动清理
   - 及时释放不需要的持久Actor

4. **利用附着系统**
   - 创建复杂的视觉层次
   - 使用插槽系统组织结构
   - 避免过深的附着层次

### ❌ 避免的做法

1. **职责混乱**
   ```csharp
   // ❌ 错误：在Actor中处理游戏逻辑
   public class WeaponActor : Actor
   {
       public void Attack(Unit target)  // 应该在游戏逻辑中！
       {
           target.TakeDamage(10);
       }
   }
   ```

2. **内存泄漏**
   ```csharp
   // ❌ 错误：持久Actor忘记释放
   var effect = new ActorParticle(link, false, persistScope);
   // 忘记调用 effect.Dispose(); 
   ```

3. **过度创建**
   ```csharp
   // ❌ 错误：频繁创建销毁Actor
   for (int i = 0; i < 100; i++)
   {
       var temp = new ActorParticle(link, false, scope);
       temp.Dispose();  // 频繁创建销毁影响性能
   }
   ```

4. **忽略作用域**
   ```csharp
   // ❌ 错误：没有使用作用域管理
   var effect = new ActorParticle(link, false, null);  // 生命周期难以管理
   ```

## 🔗 相关文档

- [🎯 单位系统](UnitSystem.md) - Actor的逻辑对应物
- [🎮 实体系统](EntitySystem.md) - 游戏逻辑基础
- [🎨 UI系统](UISystem.md) - UI层的Actor使用
- [🎪 模型动画系统](ModelAnimationSystem.md) - Actor动画控制
- [🔊 音频系统](AudioSystem.md) - 声音Actor详解
- [🎯 事件系统](EventSystem.md) - Actor事件处理

---

> 💡 **提示**: Actor系统是游戏表现层的核心，正确理解其与Entity的区别对于构建高性能、可维护的游戏系统至关重要。记住：**Actor负责"看到的"，Entity负责"逻辑的"**。 