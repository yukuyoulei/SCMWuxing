# 🤖 AI系统

## 📖 概述

AI系统是WasiCore框架中负责管理非玩家角色(NPC)智能行为的核心系统。它提供了两种主要的AI架构：

1. **AIThinkTree** - 基于行为树的单体AI系统，适用于单个单位的复杂决策
2. **WaveAI** - 基于群体的AI系统，适用于多个单位的协同行为

AI系统通过GameData配置驱动，支持灵活的行为定制和参数调整。

## 🎯 AIThinkTree 行为树配置

### 基础配置

AIThinkTree通过`GameDataAIThinkTree`进行配置：

```csharp
[GameDataCategory]
public partial class GameDataAIThinkTree
{
    // 扫描过滤器 - 决定AI能发现哪些目标
    public TargetFilterComplex ScanFilters { get; set; }
    
    // 扫描排序 - 决定目标优先级
    public List<UnitFilter>? ScanSorts { get; set; }
    
    // 战斗行为树 - 定义AI的战斗逻辑
    public IGameLink<GameDataAINode>? CombatBehaviorTree { get; set; }
}
```

### 扫描过滤器配置

扫描过滤器决定AI能够发现和攻击哪些目标：

```csharp
// 基础敌人扫描配置
var scanFilters = new TargetFilterComplex
{
    Required = [
        UnitRelationship.Enemy,    // 必须是敌人
        UnitFilter.Unit,           // 必须是单位
        UnitRelationship.Visible    // 必须是可见的
    ],
    Excluded = [
        UnitFilter.Item,           // 排除物品
        UnitState.Invulnerable     // 排除无敌单位
    ]
};

// 高级扫描配置 - 只攻击特定类型的敌人
var advancedScanFilters = new TargetFilterComplex
{
    Required = [
        UnitRelationship.Enemy,
        UnitFilter.Unit,
        UnitRelationship.Visible,
        UnitFilter.Hero            // 只攻击英雄单位
    ],
    Excluded = [
        UnitFilter.Item,
        UnitState.Invulnerable,
        UnitState.Stealth          // 排除隐身单位
    ]
};
```

### 战斗行为树配置

战斗行为树定义了AI在战斗中的决策逻辑。框架提供了多种行为节点：

#### 1. 序列节点 (Sequence)
按顺序执行子节点，如果任何子节点失败则停止：

```csharp
var sequenceNode = new GameDataAINodeSequence
{
    Children = [
        validateScanLink,      // 1. 扫描敌人
        validateAttackLink,    // 2. 攻击目标
        validateMoveToLink     // 3. 移动到目标
    ]
};
```

#### 2. 选择节点 (Select)
尝试执行子节点，直到一个成功为止：

```csharp
var selectNode = new GameDataAINodeSelect
{
    Children = [
        validateAttackLink,    // 尝试攻击
        validateMoveToLink,    // 如果攻击失败，尝试移动
        validateRetreatLink    // 如果移动失败，尝试撤退
    ]
};
```

#### 3. 并行节点 (Parallel)
同时执行多个子节点：

```csharp
var parallelNode = new GameDataAINodeParallel
{
    Children = [
        validateScanLink,      // 持续扫描
        validateAttackLink,    // 同时攻击
        validateDefendLink     // 同时防御
    ]
};
```

#### 4. 验证节点 (Validate)
各种具体的AI行为验证：

```csharp
// 扫描验证
var scanNode = new GameDataAINodeValidateScan
{
    Range = 500,              // 扫描范围
    MaxTargets = 5            // 最大目标数
};

// 攻击验证
var attackNode = new GameDataAINodeValidateAttack
{
    Range = 200,              // 攻击范围
    RequireLineOfSight = true // 需要视线
};

// 移动验证
var moveNode = new GameDataAINodeValidateMoveTo
{
    Range = 50,               // 接近范围
    MaxDistance = 1000        // 最大移动距离
};

// 施法验证
var castNode = new GameDataAINodeValidateCast
{
    Ability = fireballAbilityLink,  // 技能链接
    Range = 300,                    // 施法范围
    RequireTarget = true            // 需要目标
};
```

### 完整行为树示例

```csharp
// 创建一个完整的AI行为树
var aiThinkTreeData = new GameDataAIThinkTree
{
    // 扫描配置
    ScanFilters = new TargetFilterComplex
    {
        Required = [UnitRelationship.Enemy, UnitFilter.Unit, UnitRelationship.Visible],
        Excluded = [UnitFilter.Item, UnitState.Invulnerable]
    },
    
    // 战斗行为树
    CombatBehaviorTree = CreateCombatBehaviorTree()
};

private IGameLink<GameDataAINode> CreateCombatBehaviorTree()
{
    // 1. 扫描节点
    var scanNode = new GameDataAINodeValidateScan
    {
        Range = 500,
        MaxTargets = 3
    };
    
    // 2. 攻击节点
    var attackNode = new GameDataAINodeValidateAttack
    {
        Range = 200,
        RequireLineOfSight = true
    };
    
    // 3. 移动节点
    var moveNode = new GameDataAINodeValidateMoveTo
    {
        Range = 50,
        MaxDistance = 800
    };
    
    // 4. 技能节点
    var skillNode = new GameDataAINodeValidateCast
    {
        Ability = fireballAbilityLink,
        Range = 300,
        RequireTarget = true
    };
    
    // 5. 选择逻辑：优先攻击，其次移动，最后使用技能
    var selectNode = new GameDataAINodeSelect
    {
        Children = [
            attackNode.Link,   // 尝试攻击
            moveNode.Link,     // 尝试移动
            skillNode.Link     // 尝试使用技能
        ]
    };
    
    // 6. 序列逻辑：先扫描，再选择行动
    var sequenceNode = new GameDataAINodeSequence
    {
        Children = [
            scanNode.Link,     // 扫描敌人
            selectNode.Link    // 选择行动
        ]
    };
    
    return sequenceNode.Link;
}
```

## 🌊 WaveAI 群体AI配置

### 基础配置

WaveAI通过`GameDataWaveAI`进行配置：

```csharp
[GameDataCategory]
public partial class GameDataWaveAI
{
    // 行为类型
    public WaveType Type { get; set; }
    
    // 距离控制
    public float CombatLeash { get; set; }        // 战斗牵引距离
    public float? WaveLeash { get; set; }         // 群体牵引距离
    public float CombatResetRange { get; set; }   // 战斗重置距离
    
    // 扫描范围
    public float MinimalScanRange { get; set; }   // 最小扫描范围
    public float MaximalScanRange { get; set; }   // 最大扫描范围
    public float MinimalApproachRange { get; set; } // 最小接近范围
    
    // 功能开关
    public bool EnableCombat { get; set; }        // 启用战斗
    public bool EnableWaveFormation { get; set; } // 启用阵型
    public bool EnableLinkedAggro { get; set; }   // 启用连锁仇恨
    
    // 时间控制
    public TimeSpan InCombatMinimalDuration { get; set; } // 最小战斗持续时间
    
    // 控制优化
    public float MoveHysteresisFactor { get; set; } = 0.7f; // 移动滞后因子
    public float MinControlDuration { get; set; } = 1.5f;   // 最小控制持续时间
}
```

### 行为类型配置

WaveAI支持三种主要行为类型：

#### 1. Guard (守卫模式)
单位在指定位置守卫，当距离目标过远时返回：

```csharp
var guardWaveAI = new GameDataWaveAI
{
    Type = WaveType.Guard,
    EnableCombat = true,
    CombatLeash = 1500,           // 战斗牵引距离
    CombatResetRange = 1800,      // 战斗重置距离
    MinimalScanRange = 500,       // 扫描范围
    MaximalScanRange = 1000,      // 最大扫描范围
    MinimalApproachRange = 200,   // 接近范围
    InCombatMinimalDuration = TimeSpan.FromSeconds(3), // 最小战斗时间
    EnableWaveFormation = false,  // 守卫模式通常不需要阵型
    WaveLeash = 500              // 守卫牵引距离（默认500）
};
```

**Guard模式的核心特性：**
- **位置守卫** - 单位守卫在WaveTarget指定的位置
- **牵引机制** - 当单位距离目标超过WaveLeash时，自动移动到80%的牵引距离
- **简单逻辑** - 不涉及复杂的巡逻路径，专注于位置守卫

#### 2. Patrol (巡逻模式)
单位在WaveTarget和OriginTarget之间巡逻：

```csharp
var patrolWaveAI = new GameDataWaveAI
{
    Type = WaveType.Patrol,
    EnableCombat = true,
    CombatLeash = 1200,           // 战斗牵引距离
    CombatResetRange = 1500,      // 战斗重置距离
    MinimalScanRange = 400,       // 扫描范围
    MaximalScanRange = 800,       // 最大扫描范围
    MinimalApproachRange = 150,   // 接近范围
    InCombatMinimalDuration = TimeSpan.FromSeconds(2), // 最小战斗时间
    EnableWaveFormation = true,   // 巡逻模式可以使用阵型
    WaveLeash = 1000             // 群体牵引距离
};
```

**Patrol模式的核心特性：**
- **双点巡逻** - 在WaveTarget和OriginTarget之间移动
- **智能选择** - 根据当前距离选择更近的目标点
- **接近判断** - 只有当距离目标超过MinimalApproachRange时才移动

#### 3. Move (移动模式)
单位移动到指定目标位置，使用智能控制切换机制：

```csharp
var moveWaveAI = new GameDataWaveAI
{
    Type = WaveType.Move,
    EnableCombat = true,
    CombatLeash = 1000,           // 战斗牵引距离
    CombatResetRange = 1200,      // 战斗重置距离
    MinimalScanRange = 300,       // 扫描范围
    MaximalScanRange = 600,       // 最大扫描范围
    MinimalApproachRange = 100,   // 接近范围（控制切换阈值）
    InCombatMinimalDuration = TimeSpan.FromSeconds(1.5), // 最小战斗时间
    EnableWaveFormation = true,   // 移动模式通常使用阵型
    WaveLeash = 800,             // 群体牵引距离
    MoveHysteresisFactor = 0.8f, // 移动滞后因子（防止震荡）
    MinControlDuration = 2.0f    // 最小控制持续时间
};
```

**Move模式的核心特性：**

1. **智能控制切换** - 根据距离目标的位置自动切换控制权：
   - **远距离** (> MinimalApproachRange): WaveAI控制长距离移动
   - **缓冲区** (MinimalApproachRange * MoveHysteresisFactor ~ MinimalApproachRange): 保持当前控制器
   - **近距离** (< MinimalApproachRange * MoveHysteresisFactor): AIThinkTree控制精确定位和战斗

2. **防震荡机制** - 使用滞后因子和最小控制时间防止频繁切换

3. **精确定位** - 当切换到AIThinkTree控制时，会发出精确移动指令帮助单位到达目标位置

### 高级配置示例

#### 精英怪物群体
```csharp
var eliteGroupAI = new GameDataWaveAI
{
    Type = WaveType.Guard,
    EnableCombat = true,
    EnableWaveFormation = true,
    EnableLinkedAggro = true,     // 启用连锁仇恨
    CombatLeash = 2000,           // 较大的战斗范围
    CombatResetRange = 2500,      // 较大的重置范围
    WaveLeash = 1500,             // 群体牵引距离
    MinimalScanRange = 600,       // 较大的扫描范围
    MaximalScanRange = 1200,      // 最大扫描范围
    MinimalApproachRange = 250,   // 较大的接近范围
    InCombatMinimalDuration = TimeSpan.FromSeconds(5), // 较长的战斗时间
    MoveHysteresisFactor = 0.6f, // 较低的滞后因子
    MinControlDuration = 3.0f    // 较长的控制时间
};
```

#### 快速反应小队
```csharp
var quickResponseAI = new GameDataWaveAI
{
    Type = WaveType.Patrol,
    EnableCombat = true,
    EnableWaveFormation = true,
    EnableLinkedAggro = false,    // 不启用连锁仇恨
    CombatLeash = 800,            // 较小的战斗范围
    CombatResetRange = 1000,      // 较小的重置范围
    WaveLeash = 600,              // 较小的群体牵引距离
    MinimalScanRange = 200,       // 较小的扫描范围
    MaximalScanRange = 400,       // 最大扫描范围
    MinimalApproachRange = 80,    // 较小的接近范围
    InCombatMinimalDuration = TimeSpan.FromSeconds(1), // 较短的最小战斗时间
    MoveHysteresisFactor = 0.9f, // 较高的滞后因子
    MinControlDuration = 0.5f    // 较短的控制时间
};
```

## 🔧 使用方法

### 1. 创建和配置AI

#### 创建单体AI
```csharp
// 方法1：通过Unit数据自动创建
public static AIThinkTree? AddDefaultAI(Unit unit)
{
    return unit.Cache.TacticalAI?.Data?.CreateAI(unit);
}

// 方法2：手动创建AI
var aiThinkTree = new AIThinkTree(aiDataLink, unit);

// 方法3：通过GameData创建
var aiData = Game.Instance.GameData.Get<GameDataAIThinkTree>("EliteWarriorAI");
var ai = aiData.CreateAI(unit);
```

#### 创建群体AI
```csharp
// 创建WaveAI实例
var waveAIData = Game.Instance.GameData.Get<GameDataWaveAI>("GuardPatrolAI");
var waveAI = waveAIData.CreateWaveAI();

// 添加单位到群体
waveAI.Add(unit1);
waveAI.Add(unit2);
waveAI.Add(unit3);

// 设置群体目标
waveAI.WaveTarget = targetEntity;      // 群体目标
waveAI.OriginTarget = originPosition;  // 起始位置

// 开始AI思考
waveAI.StartThinking();
```

### 2. 动态控制AI

#### 启用/禁用AI
```csharp
// 启用AI
aiThinkTree.Enable();

// 禁用AI
aiThinkTree.Disable();

// 检查AI状态
if (aiThinkTree.IsEnabled)
{
    // AI正常工作
}

// 临时禁用AI（用于移动等操作）
aiThinkTree.DisableForMove();
aiThinkTree.EnableFromMove();
```

#### 控制群体AI
```csharp
// 添加单位到群体
waveAI.Add(newUnit);

// 从群体移除单位
waveAI.Remove(unit);

// 停止群体AI
waveAI.StopThinking();

// 重新开始群体AI
waveAI.StartThinking();
```

### 3. 运行时配置调整

#### 动态调整AI参数
```csharp
// 获取AI数据
var aiData = aiThinkTree.Cache;

// 动态调整扫描范围
aiData.ScanFilters = new TargetFilterComplex
{
    Required = [UnitRelationship.Enemy, UnitFilter.Unit],
    Excluded = [UnitFilter.Item, UnitState.Invulnerable, UnitState.Stealth]
};

// 动态调整群体AI参数
var waveData = waveAI.Cache;
waveData.CombatLeash = 2000;  // 增加战斗牵引距离
waveData.EnableCombat = false; // 临时禁用战斗
```

## 🎮 最佳实践

### 1. AI配置原则

#### 扫描配置
- **扫描范围**：根据单位类型和游戏平衡调整
- **目标过滤**：确保AI不会攻击不应该攻击的目标
- **优先级排序**：通过ScanSorts定义目标选择优先级

#### 战斗配置
- **牵引距离**：防止AI追击过远
- **重置距离**：确保AI能返回原位
- **最小战斗时间**：避免AI频繁切换状态

#### 群体配置
- **群体牵引**：控制群体成员与领导者的距离
- **阵型启用**：根据行为类型决定是否使用阵型
- **连锁仇恨**：控制群体成员是否共享仇恨

### 2. 性能优化

#### 思考频率
```csharp
// WaveAI使用错开思考避免性能瓶颈
public int StaggeredCount => 60; // 每60帧思考一次
```

#### 缓存优化
```csharp
// AI会缓存频繁访问的数据
private float CombatLeashSquaredCache;
private float AttackRangeSquaredCache;

public void EnterCombatInit()
{
    // 缓存平方值避免重复计算
    CombatLeashSquaredCache = Leash * Leash;
    AttackRangeSquaredCache = Attack?.Range * Attack?.Range ?? 0;
}
```

### 3. 调试技巧

#### 日志记录
```csharp
// 在AI关键行为中添加日志
public void EnterCombatInit()
{
    Game.Logger.LogInformation("AI {AI} entering combat", Host.Name);
    // ... 其他逻辑
}
```

#### 状态监控
```csharp
// 监控AI状态
if (aiThinkTree.CombatState == CombatState.InCombat)
{
    Game.Logger.LogInformation("AI {AI} is in combat with {Target}", 
        aiThinkTree.Host.Name, aiThinkTree.FocusTarget?.Name);
}
```

## 🔗 相关文档

- [🎯 OrderSystem](OrderSystem.md) - AI指令执行系统
- [🧠 ThinkerSystem](ThinkerSystem.md) - AI思考调度系统
- [👤 ActorSystem](ActorSystem.md) - AI单位实体管理
- [⚔️ AbilitySystem](AbilitySystem.md) - AI技能和攻击系统
- [🎮 UnitSystem](UnitSystem.md) - AI单位属性系统

## 📋 总结

AI系统提供了灵活的配置驱动方案，通过GameData可以轻松定制各种AI行为：

**AIThinkTree** 适用于：
- 复杂的单体AI决策
- 基于行为树的逻辑控制
- 精细的战斗状态管理

**WaveAI** 适用于：
- 群体单位的协同行为
- 巡逻、守卫、移动等模式
- 阵型和距离控制

通过合理配置和使用AI系统，可以创建丰富多样的游戏体验，同时保持良好的性能和可维护性。