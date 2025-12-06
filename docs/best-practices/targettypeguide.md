# 🎯 TargetType 最佳实践指南

## 📋 概述

`TargetType` 是 WasiCore 效果系统中的核心配置，它决定了效果节点的目标解析方式和视觉表现的播放行为。正确理解和使用 TargetType 对于创建符合预期的游戏效果至关重要。

## 🎭 核心概念

### TargetType 的双重作用

1. **目标解析**：决定如何处理传入的目标数据
2. **表现播放**：决定视觉表现（Actor）的位置绑定和跟随行为

这两个作用相互关联但又有所区别，是理解 TargetType 的关键。

## 📚 详细用法指南

### 🔥 TargetType.Unit - 单位跟随型

**何时使用：**
- 需要表现跟随单位移动的效果
- 单体目标技能（伤害、治疗、Buff）
- 持续性光环或护盾效果
- 需要"附身"在单位上的特效

**技术要点：**
```csharp
var buffEffect = new GameDataEffectDamage()
{
    Name = "毒素DOT",
    TargetType = TargetType.Unit,  // 关键设置
    Amount = (_) => 50,
    ActorArray = { poisonVisualEffect }  // 毒雾特效会跟随中毒单位移动
};
```

**美术指导：**
- 粒子效果使用相对坐标系（以单位为原点）
- 动画需要考虑单位移动时的平滑过渡
- 特效大小应适应不同单位的体型

**常见应用：**
- DOT 伤害的毒雾、燃烧特效
- 增益/减益 Buff 的光环特效
- 治疗技能的恢复光效
- 护盾技能的防护罩特效

### 🌍 TargetType.Point - 位置固定型

**何时使用：**
- 地面效果或环境交互
- AOE 范围技能的爆炸中心
- 陷阱、召唤物的生成位置
- 固定位置的持续特效

**技术要点：**
```csharp
var aoeEffect = new GameDataEffectSearch()
{
    Name = "火焰爆炸",
    TargetType = TargetType.Point,  // 关键设置
    Method = SearchMethod.Circle,
    Radius = (_) => 300,
    Effect = damageEffect,
    ActorArray = { explosionEffect }  // 爆炸特效固定在击中点
};
```

**美术指导：**
- 粒子效果使用绝对坐标系（世界坐标）
- 动画独立播放，无需考虑跟随逻辑
- 可以使用地形交互效果（如地面焦痕）

**常见应用：**
- 火球术、闪电术的爆炸效果
- 地面陷阱的激活特效
- 传送门、召唤阵的生成特效
- 环境破坏效果（爆坑、冰墙）

### ⚡ TargetType.Any - 灵活适应型

**何时使用：**
- 通用型效果，需要处理多种目标类型
- 不确定目标类型的动态效果
- 需要保持目标原始特性的场合

**技术要点：**
```csharp
var universalEffect = new GameDataEffectCustomAction()
{
    Name = "通用检测",
    TargetType = TargetType.Any,  // 保持目标天然类型
    // 根据实际目标类型进行不同处理
};
```

## 🎮 实战场景案例

### 案例1：火球术技能设计

```csharp
// 🔥 完整的火球术效果链
public static void CreateFireballSkill()
{
    // 1. 发射阶段 - 从施法者位置发射
    var fireballLaunch = new GameDataEffectLaunchMissile()
    {
        Name = "火球发射",
        TargetType = TargetType.Any,  // 接受任何目标类型
        LaunchLocation = new() { Value = TargetLocation.Caster },
        LaunchTargetType = TargetType.Point,  // 发射位置固定
        Missile = ScopeData.Unit.Fireball,
        ActorArray = { ScopeData.Actor.FireballTrail }  // 飞行轨迹特效
    };
    
    // 2. 击中阶段 - 爆炸效果
    var fireballExplosion = new GameDataEffectSearch()
    {
        Name = "火球爆炸",
        TargetType = TargetType.Point,  // 🎯 关键：固定在击中点爆炸
        Method = SearchMethod.Circle,
        Radius = (_) => 250,
        Effect = ScopeData.Effect.FireDamage,
        ActorArray = { ScopeData.Actor.ExplosionEffect }  // 爆炸特效固定位置
    };
    
    // 3. 伤害阶段 - 对每个受害者
    var fireDamage = new GameDataEffectDamage()
    {
        Name = "火焰伤害",
        // TargetType = TargetType.Unit,  // 伤害跟随受害单位，但伤害效果天然只对单位目标有效，无需设置
        Amount = (_) => 200,
        Type = ScopeData.DamageType.Fire,
        ActorArray = { ScopeData.Actor.BurnEffect }  // 燃烧特效跟随受害者
    };
}
```

### 案例2：治疗光环技能

```csharp
// 💚 持续治疗光环
var healingAura = new GameDataEffectPersist()
{
    Name = "治疗光环",
    TargetType = TargetType.Unit,  // 🎯 光环跟随施法者移动
    Duration = (_) => TimeSpan.FromSeconds(30),
    TickInterval = (_) => TimeSpan.FromSeconds(1),
    TickEffect = healingTick,
    ActorArray = { 
        ScopeData.Actor.HealingAuraGlow,    // 光环特效跟随施法者
        ScopeData.Actor.HealingParticles    // 治疗粒子跟随移动
    }
};

var healingTick = new GameDataEffectSearch()
{
    Name = "治疗脉冲",
    TargetType = TargetType.Point,  // 🎯 搜索以当前位置为中心
    Method = SearchMethod.Circle,
    Radius = (_) => 400,
    Effect = instantHeal,
    ActorArray = { ScopeData.Actor.HealPulse }  // 脉冲特效固定在当时位置
};
```

### 案例3：传送技能组合

```csharp
// 🌟 复杂传送技能：起始固定，结束跟随
var teleportSkill = new GameDataEffectSet()
{
    Name = "闪现术",
    Effects = new()
    {
        // 起始特效：固定在原地
        { CreateTeleportStart(), 1.0f },
        // 传送逻辑
        { CreateTeleportAction(), 1.0f },
        // 结束特效：跟随单位
        { CreateTeleportEnd(), 1.0f }
    }
};

var teleportStart = new GameDataEffectCustomAction()
{
    Name = "传送起始",
    TargetType = TargetType.Point,  // 固定在施法位置
    ActorArray = { ScopeData.Actor.TeleportVanish }
};

var teleportEnd = new GameDataEffectCustomAction()
{
    Name = "传送结束", 
    TargetType = TargetType.Unit,   // 跟随传送后的单位
    ActorArray = { ScopeData.Actor.TeleportAppear }
};
```

## 🚫 语义限制约束

### 单位专用效果类型

**⚠️ 重要警告：**某些效果类型由于业务语义的天然限制，只能作用于单位实体，**不可**将 TargetType 设置为 Point。

#### 受限效果类型清单

| 效果类型 | 语义原因 | 默认TargetType | 设置Point的后果 |
|---------|---------|---------------|----------------|
| `GameDataEffectDamage` | 伤害只能施加给有血量的单位 | Unit | CmdError.MustTargetEntity |
| `GameDataEffectBuffAdd` | Buff只能附加到单位身上 | Unit | CmdError.MustTargetUnit |
| `GameDataEffectBuffRemove` | 只能从单位身上移除Buff | Unit | CmdError.MustTargetUnit |
| `GameDataEffectUnitMoverApply` | 移动器只能控制单位移动 | Unit | CmdError.MustTargetEntity |
| `GameDataEffectUnitMoverRemove` | 只能移除单位的移动器 | Unit | CmdError.MustTargetEntity |
| `GameDataEffectUnitModifyVital` | 生命值属性只有单位拥有 | Unit | CmdError.MustTargetEntity |
| `GameDataEffectUnitModifyFacing` | 朝向只有单位才具有 | Unit | CmdError.MustTargetEntity |
| `GameDataEffectUnitModifyOwner` | 归属关系只有单位才有 | Unit | CmdError.MustTargetEntity |
| `GameDataEffectUnitKill` | 只有单位才能被击杀 | Unit | CmdError.MustTargetEntity |
| `GameDataEffectUnitRevive` | 只有单位才能被复活 | Unit | CmdError.MustTargetEntity |
| `GameDataEffectAbilityModify系列` | 技能只有单位才拥有 | Unit | CmdError.MustTargetEntity |

#### 识别语义受限效果的方法

```csharp
// 🔍 方法1：查看继承关系
[GameDataNodeType<GameDataEffect, GameDataEffectUnit>]  // 👈 继承自GameDataEffectUnit
public partial class GameDataEffectDamage

// 🔍 方法2：检查验证逻辑
public override CmdResult Validate(Effect context)
{
    return context.Target?.Entity?.IsValid == true
        ? CmdResult.Ok
        : CmdError.MustTargetEntity;  // 👈 要求实体目标
}

// 🔍 方法3：查看执行逻辑
public override void Execute(Effect context)
{
    var entity = context.Target?.Entity!;  // 👈 直接访问Entity属性
    entity.Kill(DeathType, context);
}
```

#### 最佳实践建议

```csharp
// ✅ 推荐：语义受限效果不显式设置TargetType
var healEffect = new GameDataEffectDamage()
{
    Name = "治疗术",
    Amount = (_) => -100,  // 负伤害=治疗
    Type = ScopeData.DamageType.Healing,
    // TargetType 使用默认值 Unit，确保语义正确
};

// ❌ 错误：强制设置Point导致失败
var brokenHealEffect = new GameDataEffectDamage()
{
    Name = "错误的治疗效果",
    TargetType = TargetType.Point,  // ❌ 语义错误！点没有血量概念
    Amount = (_) => -100,
};
```

## ⚠️ 常见错误和解决方案

### 错误1：违反语义限制导致效果失效

**问题描述：**
为语义受限的效果类型错误设置 TargetType.Point，导致整个效果链执行失败。

**错误配置：**
```csharp
// ❌ 严重错误：伤害效果设置为Point类型
var brokenFireball = new GameDataEffectSet()
{
    Effects = new()
    {
        // AOE搜索（正确）
        { CreateFireballSearch(TargetType.Point), 1.0f },
        // 伤害效果（错误！）
        { CreateFireballDamage(TargetType.Point), 1.0f }  // ❌ 伤害不能作用于点
    }
};

var damageEffect = new GameDataEffectDamage()
{
    TargetType = TargetType.Point,  // ❌ 语义冲突！
    Amount = (_) => 200,
};
```

**运行时错误：**
```
CmdError.MustTargetEntity: 必须以实体为目标
Effect execution failed: GameDataEffectDamage requires unit target
```

**正确配置：**
```csharp
// ✅ 正确：区分搜索逻辑和伤害逻辑的TargetType
var correctFireball = new GameDataEffectSet()
{
    Effects = new()
    {
        // AOE搜索：固定在爆炸点
        { CreateFireballSearch(TargetType.Point), 1.0f },
        // 伤害效果：作用于搜索到的每个单位（自动传递Unit目标）
        { CreateFireballDamage(), 1.0f }  // 使用默认Unit类型
    }
};

var damageEffect = new GameDataEffectDamage()
{
    // TargetType 保持默认 Unit，符合伤害效果的语义
    Amount = (_) => 200,
};
```

### 错误2：AOE效果表现位置不当

**问题描述：**
AOE伤害技能的爆炸特效应该固定在击中点，但却跟随着被击中的单位移动。

**错误配置：**
```csharp
var aoeEffect = new GameDataEffectSearch()
{
    TargetType = TargetType.Unit,  // ❌ 错误：会导致特效跟随首个被击中的单位
    Method = SearchMethod.Circle,
    ActorArray = { explosionEffect }
};
```

**正确配置：**
```csharp
var aoeEffect = new GameDataEffectSearch()
{
    TargetType = TargetType.Point,  // ✅ 正确：特效固定在爆炸中心
    Method = SearchMethod.Circle,
    ActorArray = { explosionEffect }
};
```

### 错误2：单体技能目标丢失

**问题描述：**
单体治疗技能在目标移动时治疗特效消失或位置错误。

**错误配置：**
```csharp
var healSpell = new GameDataEffectDamage()
{
    TargetType = TargetType.Point,  // ❌ 错误：治疗特效不会跟随目标
    Amount = (_) => -100,  // 负伤害=治疗
    ActorArray = { healingGlow }
};
```

**正确配置：**
```csharp
var healSpell = new GameDataEffectDamage()
{
    TargetType = TargetType.Unit,   // ✅ 正确：治疗特效跟随被治疗单位
    Amount = (_) => -100,
    ActorArray = { healingGlow }
};
```

### 错误3：持续效果表现异常

**问题描述：**
DOT效果的视觉标记应该跟随中毒单位，但固定在施法位置。

**解决方案：**
```csharp
// DOT效果的正确配置
var poisonDot = new GameDataEffectPersist()
{
    Name = "持续中毒",
    TargetType = TargetType.Unit,  // 🎯 确保中毒特效跟随目标
    Duration = (_) => TimeSpan.FromSeconds(10),
    TickInterval = (_) => TimeSpan.FromSeconds(1),
    TickEffect = poisonDamage,
    ActorArray = { ScopeData.Actor.PoisonAura }  // 中毒光环跟随单位
};
```

## 🎨 设计决策指导

### 快速决策流程图

```
开始设计效果
    ↓
效果类型是否有语义限制？
(继承自GameDataEffectUnit？)
    ↓                    ↓
   是                   否
    ↓                    ↓
保持默认Unit         是否需要表现跟随单位？
(不可修改)              ↓                    ↓
                       是                   否
                        ↓                    ↓
                   使用 Unit 或 Any        是否为地面/环境效果？
                                          ↓                ↓
                                         是               否
                                          ↓                ↓
                                     使用 Point        使用 Any
```

**⚠️ 重要提醒：**
- 第一步检查至关重要：语义受限的效果**不可**修改TargetType
- 语义受限效果包括：伤害、Buff、移动器、单位属性修改等
- 违反语义限制会导致运行时`CmdError.MustTargetEntity`错误

### 技能类型映射表

| 技能类型 | 推荐 TargetType | 理由 | 语义限制 |
|---------|----------------|------|---------|
| 单体伤害 | Unit | 伤害数字、击中特效需要跟随目标 | 🚫 不能设Point |
| 单体治疗 | Unit | 治疗光效需要附着在被治疗者身上 | 🚫 不能设Point |
| AOE爆炸搜索 | Point | 爆炸中心固定，不应跟随任何单位 | ✅ 可任意设置 |
| AOE伤害子效果 | Unit | 对每个受害者的伤害 | 🚫 不能设Point |
| DOT/HOT | Unit | 持续效果标记必须跟随受影响单位 | 🚫 不能设Point |
| 地面陷阱 | Point | 陷阱固定在触发位置 | ✅ 可任意设置 |
| 召唤技能 | Point | 召唤位置确定后不再改变 | ✅ 可任意设置 |
| Buff/Debuff | Unit | 状态图标和特效需要跟随单位 | 🚫 不能设Point |
| 移动控制 | Unit | 移动器只能控制单位 | 🚫 不能设Point |
| 传送起始 | Point | 固定记录传送前的位置 | ✅ 可任意设置 |
| 传送结束 | Unit | 特效需要出现在传送后的单位位置 | 🚫 不能设Point |

## 🛠️ 开发工具和调试

### IntelliSense 提示优化

通过丰富的 XML 注释，开发者在配置 TargetType 时会获得详细的智能提示：

```csharp
public TargetType TargetType { get; set; }  
// IntelliSense 会显示：
// "指定效果节点的目标类型，决定效果创建后的位置类型和视觉表现的播放行为"
// "Unit: 表现会跟随目标单位移动，适用于需要附着在单位身上的效果"
// "Point: 表现固定在指定坐标播放，适用于地面效果或固定位置特效"
```

### 运行时调试技巧

```csharp
// 🔍 调试 TargetType 解析结果
public override void Execute(Effect context)
{
    Game.Logger.LogDebug("Effect {name} with TargetType {type} resolved target: {target} (Type: {targetType})", 
        Name, TargetType, context.Target, context.Target?.GetType().Name);
    
    // 验证表现是否正确绑定
    if (TargetType == TargetType.Unit && context.Target?.Unit == null)
    {
        Game.Logger.LogWarning("Unit TargetType but no unit target resolved!");
    }
}
```

## 📖 AI助手使用指南

### 提示AI助手的关键信息

当与AI助手协作开发效果时，请明确以下信息：

1. **效果预期行为**：说明特效应该跟随单位还是固定位置
2. **持续时间**：瞬间效果 vs 持续效果的不同考虑
3. **视觉设计意图**：美术希望达到的视觉效果

**良好的需求描述示例：**
```
"创建一个火球AOE技能，爆炸特效应该固定在击中的地面位置，
但对每个受伤单位添加短暂的燃烧标记特效跟随他们移动"
```

**对应的AI助手理解：**
- 主AOE效果：TargetType.Point（爆炸固定）
- 燃烧子效果：TargetType.Unit（标记跟随）

## 🎯 最佳实践总结

### DO（推荐做法）

✅ **明确设计意图**
```csharp
// 清晰的命名和注释
var spellEffect = new GameDataEffectDamage()
{
    Name = "火球单体伤害_跟随目标",  // 命名体现TargetType意图
    TargetType = TargetType.Unit,
    // ... 其他配置
};
```

✅ **合理的效果分层**
```csharp
// 复杂技能分解为多个子效果，各自使用合适的TargetType
var complexSpell = new GameDataEffectSet()
{
    Effects = new()
    {
        { groundEffect(TargetType.Point), 1.0f },    // 地面部分
        { unitEffect(TargetType.Unit), 1.0f },       // 单位部分
        { searchEffect(TargetType.Point), 1.0f }     // 搜索部分
    }
};
```

✅ **充分的测试验证**
```csharp
// 在不同场景下测试效果表现
[Test]
public void TestFireballVisualBehavior()
{
    // 测试目标移动时的表现行为
    var target = CreateMovingUnit();
    var effect = CreateFireballEffect(TargetType.Unit);
    
    var initialPos = target.Position;
    effect.Execute();
    MoveUnit(target, new Vector3(100, 0, 0));
    
    // 验证特效是否正确跟随
    Assert.AreNotEqual(initialPos, GetEffectVisualPosition(effect));
}
```

### DON'T（避免做法）

❌ **违反语义限制**
```csharp
// 🚫 不要：为语义受限效果设置Point类型
var brokenEffect = new GameDataEffectDamage()
{
    TargetType = TargetType.Point,  // ❌ 严重错误！伤害不能作用于点
    Amount = (_) => 100,
};

var brokenBuff = new GameDataEffectBuffAdd()
{
    TargetType = TargetType.Point,  // ❌ 严重错误！Buff不能附加到点
    BuffLink = someBuffLink,
};
```

❌ **盲目使用默认值**
```csharp
// 不要不考虑就使用默认的 TargetType.Any（对于非语义受限效果）
var effect = new GameDataEffectSearch()  // 非语义受限效果
{
    // TargetType = TargetType.Any,  // 缺乏明确意图，应该根据需求明确设置
};
```

❌ **忽视表现影响**
```csharp
// 不要只考虑功能逻辑，忽视视觉表现
var aoeEffect = new GameDataEffectSearch()
{
    TargetType = TargetType.Unit,  // ❌ AOE爆炸不应跟随单位
    ActorArray = { explosionEffect }
};
```

❌ **混淆技能目标类型**
```csharp
// 不要将 AbilityTargetType 与 EffectTargetType 混淆
var ability = new GameDataAbilityExecute()
{
    TargetType = AbilityTargetType.Unit,  // 技能层面的目标类型
    Effect = effectLink  // 效果内部还有自己的TargetType
};
```

## 🔗 相关资源

- [效果系统完整文档](./EffectSystem.md)
- [Actor系统文档](./ActorSystem.md)
- [目标系统接口文档](../API_REFERENCE.md#itarget)
- [坐标系统指南](../COORDINATE_SYSTEM_GUIDE.md)

---

> 💡 **记住**：TargetType 是连接游戏逻辑和视觉表现的重要桥梁，正确使用它能让你的技能效果既功能正确又视觉出色！
