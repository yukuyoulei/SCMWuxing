# 模型动画系统

## 概述

WasiCore中的模型动画系统提供了一个全面的框架，用于管理和播放游戏Actor上的动画。它由三个主要层次组成：

1. **引擎层**：底层视图动画接口（`IViewAnimation`、`IViewAnimationSequence`、`IViewAsyncActor`）
2. **游戏层**：高级动画管理（`Animation`、`AnimationSequence`、`AnimationBracket`）
3. **Actor集成**：提供动画功能的Actor接口（`IActorAnimationPlayable`）

## 架构

### 核心组件

#### 1. 动画类型

系统支持三种主要的动画类型：

- **普通动画**：基础的单一动画播放，带有时间控制
- **动画序列**：复杂的多动画编排，包含事件和Actor生成
- **动画元组**：基于生命周期的动画（出生 → 待机 → 死亡）

#### 2. 关键类

```
GameCore.ModelAnimation/
├── Animation.cs                    # 核心动画包装器
├── AnimationSequence.cs           # 复杂序列控制器  
├── AnimationBracket.cs            # 元组动画控制器
├── Data/
│   ├── GameDataAnimationSimple.cs    # 普通动画配置
│   ├── GameDataAnimationSequence.cs  # 序列配置
│   └── GameDataAnimationBracket.cs   # 元组配置
├── Enum/
│   ├── BracketStage.cs               # 出生/待机/死亡阶段
│   ├── AnimationEventKey.cs          # 动画事件类型
│   └── SequenceEventKey.cs           # 序列事件类型
└── Struct/
    └── EventArgs.cs                  # 事件参数结构
```

#### 3. Actor集成

`IActorAnimationPlayable`接口为Actor提供动画功能：

```csharp
public interface IActorAnimationPlayable : IActor
{
    // 播放普通动画
    Animation? PlayAnimation(ResourceType.Animation animationFile, AnimationPlayParam? param = null);
    Animation? PlayAnimation(IGameLink<GameDataAnimationSimple> link);
    
    // 播放复杂序列
    AnimationSequence? PlayAnimationSequence(IGameLink<GameDataAnimationSequence> link);
    AnimationSequence? PlayAnimationSequence(IGameLink<GameDataAnimationSequence> link, bool triggerEvents);
    
    // 动画控制
    bool StopAnimationOfLayer(AnimationLogicLayer logicLayer);
    bool SetBirthStandDeathAnimation(UTF8String birth, UTF8String stand, UTF8String death);
    bool SetAnimationMapping(UTF8String alias, UTF8String animationRaw);
    bool ClearAnimationMapping(UTF8String alias);
    IEnumerable<Animation> GetAnimationInstances();
}
```

## 使用模式

### 1. 普通动画播放

```csharp
// 播放普通动画
var animation = actor.PlayAnimation(animationResource, new AnimationPlayParam {
    Priority = 100,
    Speed = 1.2f,
    IsLooping = true,
    BlendIn = TimeSpan.FromSeconds(0.3f)
});

// 控制播放
animation.Speed = 0.8f;
animation.Stop(TimeSpan.FromSeconds(0.5f));
```

### 2. 动画序列

```csharp
// 创建序列配置
var sequenceData = new GameDataAnimationSequence {
    Playbacks = [
        new AnimationPlayback { 
            AnimationRaw = "spell_cast_start", 
            PlaybackDuration = TimeSpan.FromSeconds(1.0) 
        },
        new AnimationPlayback { 
            AnimationRaw = "spell_cast_channel", 
            PlaybackDuration = TimeSpan.FromSeconds(3.0),
            IsLooping = true 
        }
    ],
    SequenceActors = [
        new SequenceActorSpawn {
            Actor = magicCircleActor,
            SpawnOffset = TimeSpan.FromSeconds(0.5),
            Duration = TimeSpan.FromSeconds(2.0)
        }
    ],
    SequenceEvents = [
        new SequenceEvent {
            EventKey = SequenceEventKey.ApplyPlayerState,
            Offset = TimeSpan.FromSeconds(1.5)
        }
    ]
};

// 播放序列
var sequence = actor.PlayAnimationSequence(sequenceDataLink, triggerEvents: true);

// 处理事件
sequence.EventTriggered += (args) => {
    switch (args.EventKey) {
        case SequenceEventKey.ApplyPlayerState:
            ApplySpellEffect(args.Elapsed);
            break;
    }
};

// 控制序列
sequence.Pause();
sequence.SwitchToPlayback(1);
sequence.Resume();
```

### 3. 动画元组

```csharp
// 配置元组动画
var bracketData = new GameDataAnimationBracket {
    BirthStandDeathAnimation = new BirthStandDeathAnimation {
        BirthAnimation = "enemy_spawn",
        StandAnimation = "enemy_idle", 
        DeathAnimation = "enemy_death"
    },
    ForceOneShot = false,  // 出生 → 待机（循环）→ 死亡
    KillOnFinish = true    // 死亡后销毁actor
};

// 播放元组
var bracket = actor.PlayAnimationBracket(bracketData.CachePlayParam);
```

## 动画优先级系统

系统使用基于优先级的方法来管理多个动画：

- **优先级值**：数值越高 = 优先级越高
- **压制**：当高优先级动画播放时，低优先级动画被压制（暂停）
- **混合**：基于`BlendIn`时间的动画间平滑过渡
- **逻辑层**：不同层可以同时播放（`AnimationLogicLayer`）

### 优先级示例

```csharp
// 基础待机动画（低优先级）
var idleAnimation = actor.PlayAnimation(idleAnimationLink, new AnimationPlayParam {
    Priority = 0,
    IsLooping = true
});

// 攻击动画（高优先级）- 将压制待机动画
var attackAnimation = actor.PlayAnimation(attackAnimationLink, new AnimationPlayParam {
    Priority = 100,
    BlendIn = TimeSpan.FromSeconds(0.1f)
});

// 攻击完成时，待机动画自动恢复
```

## 事件系统

### 动画事件

普通动画可以通过引擎的状态变化系统触发事件：

```csharp
animation.StateChanged += (stateEvent) => {
    switch (stateEvent) {
        case AnimationStateEvent.Started:
            OnAnimationStarted();
            break;
        case AnimationStateEvent.Finished:
            OnAnimationFinished();
            break;
        case AnimationStateEvent.Removed:
            OnAnimationRemoved();
            break;
    }
};
```

### 序列事件

动画序列提供丰富的事件功能：

```csharp
sequence.EventTriggered += (args) => {
    // 访问全面的事件上下文
    var sequenceInstance = args.Sequence;
    var eventType = args.EventKey;
    var timingSinceStart = args.Elapsed;
    var activePlaybackIndex = args.PlaybackIndex;
    var activeAnimation = args.Animation;
};

sequence.Removed += () => {
    // 序列完全结束
};

sequence.PlaybackIndexChanged += (newIndex) => {
    // 切换到不同的播放
};
```

## 高级功能

### 1. 动画映射

为动画创建别名以启用运行时交换：

```csharp
// 设置映射
actor.SetAnimationMapping("walk", "character_walk_normal");
actor.SetAnimationMapping("run", "character_run_normal");

// 稍后，改变角色的移动风格
actor.SetAnimationMapping("walk", "character_walk_sneaky");
actor.SetAnimationMapping("run", "character_run_sneaky");
```

### 2. 动态Actor生成

序列可以精确定时生成临时Actor：

```csharp
var sequenceActors = new List<SequenceActorSpawn> {
    new SequenceActorSpawn {
        Actor = sparkEffectActor,
        SpawnOffset = TimeSpan.FromSeconds(0.8),  // 在0.8秒时生成
        Duration = TimeSpan.FromSeconds(1.5)      // 存活1.5秒
    },
    new SequenceActorSpawn {
        Actor = smokeEffectActor,
        SpawnOffset = TimeSpan.FromSeconds(1.0),  // 在1.0秒时生成
        Duration = TimeSpan.FromSeconds(3.0)      // 存活3.0秒
    }
};
```

### 3. 播放控制

精细的动画播放控制：

```csharp
// 速度操作
animation.Speed = 0.5f;  // 半速
animation.Speed = 2.0f;  // 双倍速

// 偏移控制
animation.PlayingOffset = TimeSpan.FromSeconds(1.5f);  // 跳转到动画的1.5秒处

// 循环控制
animation.IsLooping = true;

// 混合权重淡化
animation.FadeBlendWeight(0.5f, TimeSpan.FromSeconds(1.0f));
```

## 性能考虑

### 1. 动画缓存

系统维护高效的缓存：

- **动画缓存**：`ConditionalWeakTable<IViewAnimation, Animation>`用于自动清理
- **参数缓存**：`CachePlayParam`属性避免重复的参数构造
- **引擎集成**：直接的视图层集成最小化开销

### 2. 内存管理

- **可释放模式**：所有动画对象实现适当的释放
- **自动清理**：当Actor被销毁时动画会清理
- **弱引用**：引擎动画使用弱引用防止内存泄漏

### 3. 最佳实践

1. **重用GameData**：缓存动画配置而不是创建新的
2. **适当释放**：不再需要时始终释放动画
3. **事件取消订阅**：移除事件处理程序以防止内存泄漏
4. **优先级管理**：使用适当的优先级值避免冲突

## 调试和故障排除

### 常见问题

1. **动画不播放**：检查优先级冲突和压制
2. **内存泄漏**：确保适当的事件处理程序清理
3. **时间问题**：验证混入时间和序列偏移
4. **性能问题**：监控动画实例数量

### 调试功能

#### 编程方式调试

```csharp
// 检查活跃动画
var activeAnimations = actor.GetAnimationInstances();
foreach (var anim in activeAnimations) {
    Console.WriteLine($"Animation: {anim.AnimationResource}, Priority: {anim.Priority}, Playing: {anim.IsActivePlaying}");
}

// 监控序列状态
sequence.PlaybackIndexChanged += (index) => {
    Console.WriteLine($"Sequence switched to playback {index}");
};
```

#### 🎬 动画信息Overlay调试工具

框架提供了专门的动画信息可视化调试工具，可以实时显示动画播放状态：

```csharp
// 启用动画信息Overlay
AnimationInfoOverlay.EnableOverlay();
```

**功能特色**：
- **实时显示**: 鼠标悬停在Actor/Unit上时显示动画详细信息
- **信息丰富**: 显示动画资源、优先级、播放进度、循环状态等
- **智能跟随**: 面板自动跟随目标移动，保持信息可见
- **死亡动画支持**: 支持观察单位死亡动画的完整过程
- **性能友好**: 20FPS更新频率，对游戏性能影响最小

**使用方法**：
1. 按 `Ctrl + F2` 打开调试界面
2. 启用"Animation Info Overlay"开关
3. 将鼠标悬停在具有动画的单位上查看信息

更多详细信息请参考 <see cref="调试作弊系统"/>。

## 与其他系统的集成

### 1. Actor系统集成

所有支持动画的Actor都实现`IActorAnimationPlayable`：

- `ActorModel`：具有完整动画支持的3D模型Actor

### 2. 实体系统集成

实体可以通过Actor接口使用动画：

```csharp
public partial class Entity : IActorAnimationPlayable
{
    // 继承完整的动画功能
    public Animation? PlayAnimation(ResourceType.Animation animationFile, AnimationPlayParam? param = null)
    {
        return ((IActorAnimationPlayable)this).PlayAnimation(animationFile, param);
    }
}
```

### 3. 游戏数据集成

动画配置存储为游戏数据：

- **GameDataAnimationSimple**：基础动画参数
- **GameDataAnimationSequence**：复杂序列配置
- **GameDataAnimationBracket**：生命周期动画定义

这允许数据驱动的动画系统，其中动画行为可以在不更改代码的情况下修改。

### 4. 技能系统集成

技能系统通过 `GameDataAbilityActive` 的 `Animation` 字段与模型动画系统深度集成，提供技能释放时的动画播放功能。

#### Animation字段配置

```csharp
public abstract partial class GameDataAbilityActive
{
    /// <summary>
    /// If set, the animation will be played when the ability is casting. 
    /// When multiple animations are provided, a random one will be selected for each cast.
    /// </summary>
    public List<IGameLink<GameDataAnimation>?>? Animation { get; set; }
}
```

#### 基本用法

```csharp
// 1. 首先创建动画数据
var attackAnimationLink = new GameLink<GameDataAnimation, GameDataAnimationSimple>("SwordAttack");
var attackAnimation = new GameDataAnimationSimple(attackAnimationLink)
{
    File = "sword_attack"u8,
    Priority = 100,
    LogicLayer = AnimationLogicLayer.Normal,
    Speed = 1.2f,
    BodyPart = AnimationBodyPart.FullBody
};

// 2. 在技能中配置动画
var swordAttackAbility = new GameDataAbilityExecute(abilityLink)
{
    Time = new()
    {
        Preswing = static (_) => TimeSpan.FromSeconds(0.5),
        Backswing = static (_) => TimeSpan.FromSeconds(0.3),
    },
    Effect = damageEffect,
    Animation = [attackAnimationLink], // 🎯 关键：配置技能动画
    // ... 其他配置
};
```

#### 多动画随机选择

技能系统支持为单个技能配置多个动画，每次释放时会随机选择：

```csharp
// 创建多个攻击动画变体
var attack1 = new GameLink<GameDataAnimation, GameDataAnimationSimple>("SwordAttack1");
var attack2 = new GameLink<GameDataAnimation, GameDataAnimationSimple>("SwordAttack2");
var attack3 = new GameLink<GameDataAnimation, GameDataAnimationSimple>("SwordAttack3");

// 在技能中配置多个动画
var combatAbility = new GameDataAbilityExecute(abilityLink)
{
    Animation = [attack1, attack2, attack3], // 随机选择播放
    // ... 其他配置
};
```

#### 动画与技能阶段同步

技能动画会自动与技能的执行阶段同步：

- **Preswing阶段**：开始播放配置的动画
- **Cast阶段**：动画进入主要播放阶段
- **Backswing阶段**：动画收尾阶段

```csharp
// 框架自动处理的动画生命周期
protected override void OnAnimationState(OrderStage stage)
{
    switch (stage.InnerValue)
    {
        case EOrderStage.Preswing:
            // 创建并开始播放动画
            break;
        case EOrderStage.Cast:
            // 动画进入主要阶段
            break;
        case EOrderStage.Backswing:
            // 动画收尾
            break;
    }
}
```

#### 实际应用示例

以下是在 Vampire3D 项目中的实际配置示例：

```csharp
// 怪物攻击动画配置
var monsterAttackAnimation = new GameDataAnimationSimple(Animation.MonsterAttack)
{
    File = "attack"u8,
    Priority = 100,
    LogicLayer = AnimationLogicLayer.Normal
};

// 怪物攻击技能配置
var monsterAttackAbility = new GameDataAbilityExecute(Ability.MonsterAttack)
{
    Time = new()
    {
        Preswing = static (_) => TimeSpan.FromSeconds(0.5),
        Backswing = static (_) => TimeSpan.FromSeconds(0.3),
    },
    Effect = damageEffect,
    Animation = [Animation.MonsterAttack], // 技能释放时播放攻击动画
    AbilityExecuteFlags = new() { IsAttack = true },
    // ... 其他配置
};
```

#### 最佳实践

1. **动画时长与技能时间匹配**
   - 确保动画总时长与技能的 Preswing + Backswing 时间大致匹配
   - 避免动画过长或过短导致的不自然表现

2. **使用合适的动画优先级**
   - 技能动画通常应有较高优先级（100+）
   - 确保技能动画能正确覆盖移动等低优先级动画

3. **考虑动画变体**
   - 为重复使用的技能提供多个动画变体
   - 增加游戏的视觉丰富性和随机性

4. **动画与效果同步**
   - 确保视觉效果的时机与动画关键帧匹配
   - 使用动画事件系统进行精确的时机控制 