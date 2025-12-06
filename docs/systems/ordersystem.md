# 🎮 指令系统（Order System）

指令系统是 WasiCore 游戏框架中处理玩家操作和AI行为的核心机制，它管理从简单的移动指令到复杂的技能释放的所有游戏操作。

## 📋 目录

- [🏗️ 系统概述](#系统概述)
- [🔐 权限控制](#权限控制)
- [📦 核心组件](#核心组件)
- [🎯 指令类型](#指令类型)
  - [🔥 技能指令](#技能指令)
  - [🏃‍♂️ 移动指令](#移动指令)
  - [🕹️ 摇杆移动指令](#摇杆移动指令)
  - [📦 物品指令](#物品指令)
  - [⚔️ 攻击指令](#攻击指令)
    - [🎯 攻击技能标识机制](#攻击技能标识机制)
    - [🔄 攻击指令 vs 技能指令](#攻击指令-vs-技能指令)
    - [❌ 常见问题与解决方案](#常见问题与解决方案)
  - [⏹️ 停止指令](#停止指令)
- [🔄 指令生命周期](#指令生命周期)
  - [⏱️ 施法时间控制](#施法时间控制)
- [📊 指令队列管理](#指令队列管理)
- [🎮 摇杆系统](#摇杆系统)
- [🛠️ 实用示例](#实用示例)
- [🔧 API 参考](#api-参考)

## 🏗️ 系统概述

### 架构设计

指令系统采用命令模式（Command Pattern）设计，将游戏操作封装为独立的指令对象：

```
客户端/AI输入 → Command（指令） → Order（命令） → OrderQueue（队列） → 执行
```

### 核心特性

- ✅ **服务端权威** - 所有指令在服务端验证和执行
- ✅ **权限控制** - 严格的单位控制权限验证机制
- ✅ **队列管理** - 支持指令排队和优先级处理
- ✅ **状态跟踪** - 完整的指令生命周期管理
- ✅ **可打断性** - 支持指令的取消和覆盖
- ✅ **异步执行** - 与游戏tick系统完美集成
- ✅ **错误处理** - 优雅的失败处理和回滚

## 🔐 权限控制

指令系统实现了严格的权限控制机制，确保只有有权限的实体才能对单位下达指令：

### 服务端权限
- **完全控制权** - 服务端逻辑可以对任何单位下达指令
- **不受限制** - 服务端不受客户端权限规则约束
- **系统级操作** - AI、自动化系统等可直接操作任何单位
- **权限模拟** - 可通过设置Player字段和IsRequest标志模拟玩家请求

### 客户端权限限制
- **所有权验证** - 客户端只能对属于当前玩家的单位下达指令
- **共享控制** - 可以对被其他玩家共享了控制权的单位下达指令
- **状态检查** - 无法对拥有 `Uncommandable` 状态的单位下达指令
- **目标可见性** - 客户端发布的指令的目标不能是不可见的单位
- **隐藏技能限制** - 客户端发布的指令如果是技能指令，且尝试使用的技能是隐藏技能，指令也无法执行

### 权限验证流程
```csharp
#if SERVER
// 权限验证示例
public bool CanCommandUnit(Player player, Unit unit)
{
    // 检查单位是否可指挥
    if (unit.HasState(UnitState.Uncommandable))
        return false;
    
    // 检查所有权或共享控制权
    return unit.Owner == player || unit.IsSharedControlWith(player);
}
#endif
```

### 权限模拟

服务端可以通过设置指令的玩家字段和IsRequest标志来模拟玩家请求，使指令受到相同的权限限制：

```csharp
#if SERVER
// 服务端直接下达指令（无权限限制）
var serverCommand = new Command
{
    AbilityLink = ScopeData.Ability.Fireball,
    Index = CommandIndex.Execute,
    Type = ComponentTagEx.AbilityManager,
    Target = enemyUnit,
    Flag = CommandFlag.IsSystem  // 系统指令，无权限限制
};

// 模拟玩家请求（受权限限制）
var playerSimulatedCommand = new Command
{
    AbilityLink = ScopeData.Ability.Fireball,
    Index = CommandIndex.Execute,
    Type = ComponentTagEx.AbilityManager,
    Target = enemyUnit,
    Player = specificPlayer,           // 设置所属玩家
    Flag = CommandFlag.IsRequest       // 设置为请求标志
};

// AI系统确保遵循玩家权限
public CmdResult AIActionWithPlayerRights(Unit aiUnit, Unit target, Player owner)
{
    var command = new Command
    {
        Index = CommandIndex.Attack,
        Type = ComponentTagEx.AbilityManager,
        Target = target,
        Player = owner,                // AI代表玩家行动
        Flag = CommandFlag.IsRequest   // 受玩家权限限制
    };
    
    return command.IssueOrder(aiUnit);
}
#endif
```

### 权限异常处理
- **返回错误代码** - 返回错误代码 CmdResult ，客户端可以显示错误信息

## 📦 核心组件

### Command（指令）

指令是用户意图的抽象表示，包含执行所需的所有信息：

```csharp
public record struct Command
{
    public IGameLink<GameDataAbility>? AbilityLink { get; set; }  // 技能链接
    public CommandIndex Index { get; set; }                       // 指令类型
    public ComponentTag Type { get; set; }                        // 处理组件
    public CommandFlag Flag { get; set; }                         // 指令标志
    public ICommandTarget? Target { get; set; }                   // 目标
    public Item? Item { get; set; }                              // 物品
    public Player? Player { get; set; }                          // 发起玩家
}
```

### Order（命令）

命令是指令的执行实例，管理指令的具体执行过程：

```csharp
public abstract class Order : IDisposable
{
    public Command Command { get; }                    // 源指令
    public Entity Owner { get; }                       // 执行者
    public OrderStage Stage { get; }                   // 执行阶段
    public OrderState State { get; }                   // 执行状态
    public bool IsUnInterruptible { get; }             // 是否可打断
}
```

### OrderQueue（指令队列）

队列管理单位的所有指令，确保按序执行：

```csharp
public class OrderQueue : TagComponent
{
    public bool IsPaused { get; }                      // 是否暂停
    public bool IsEmpty { get; }                       // 是否为空
    public bool HasSingleOrder { get; }                // 是否只有一个指令
    
    public void Add(Order order, CommandFlag flag);    // 添加指令
    public Order? Peek();                              // 查看队首指令
    public void Pause();                               // 暂停队列
    public void Resume();                              // 恢复队列
}
```

## 🎯 指令类型

### 🔥 技能指令

技能指令用于释放各种能力和法术：

#### 基本技能释放

```csharp
#if SERVER
// 创建技能释放指令
var command = new Command
{
    AbilityLink = ScopeData.Ability.Fireball,
    Index = CommandIndex.Execute,
    Type = ComponentTagEx.AbilityManager,
    Target = enemyUnit,
    Player = player,
    Flag = CommandFlag.IsUser
};

// 发出指令
var result = command.IssueOrder(casterUnit);
if (result.IsSuccess)
{
    Game.Logger.LogInfo("技能指令已排队: {Ability}", command.AbilityLink.FriendlyName);
}
#endif
```

#### 切换型技能

```csharp
#if SERVER
// 开启切换技能
var toggleOnCommand = new Command
{
    AbilityLink = ScopeData.Ability.Shield,
    Index = CommandIndex.TurnOn,
    Type = ComponentTagEx.AbilityManager,
    Player = player,
    Flag = CommandFlag.IsUser
};

// 关闭切换技能
var toggleOffCommand = new Command
{
    AbilityLink = ScopeData.Ability.Shield,
    Index = CommandIndex.TurnOff,
    Type = ComponentTagEx.AbilityManager,
    Player = player,
    Flag = CommandFlag.IsUser
};

// 智能切换（开启→关闭，关闭→开启）
var smartToggleCommand = new Command
{
    AbilityLink = ScopeData.Ability.Shield,
    Index = CommandIndex.Toggle,
    Type = ComponentTagEx.AbilityManager,
    Player = player,
    Flag = CommandFlag.IsUser
};
#endif
```

#### AI技能释放

```csharp
#if SERVER
// AI使用技能
public CmdResult AIUsableAbility(Unit aiUnit, Unit target, IGameLink<GameDataAbilityActive> abilityLink)
{
    var command = new Command
    {
        AbilityLink = abilityLink,
        Index = abilityLink.Data is GameDataAbilityToggle ? CommandIndex.TurnOn : CommandIndex.Execute,
        Type = ComponentTagEx.AbilityManager,
        Target = target,
        Flag = CommandFlag.IsAI  // AI标志
    };
    
    return command.IssueOrder(aiUnit);
}
#endif
```

### 🏃‍♂️ 移动指令

移动指令控制单位在场景中的位置变化。

> **⚠️ 重要区别**：通过指令系统下达的移动指令受到指令队列的限制，而直接调用移动方法则不受限制：
> 
> - **指令系统移动**（`Move`、`VectorMove` 指令）- 遵循指令队列规则，不能与技能释放同时进行（瞬发 Transient 技能除外）
> - **直接移动方法**（`Unit.StartJoystickMove()`、`Unit.PathTo()`）- 不受指令队列限制，可在施法时移动

#### 点到点移动

```csharp
#if SERVER
// 移动到指定位置
var moveCommand = new Command
{
    Index = CommandIndex.Move,
    Type = ComponentTagEx.Walkable,
    Target = targetPosition,  // ScenePoint 或 Entity
    Player = player,
    Flag = CommandFlag.IsUser
};

var result = moveCommand.IssueOrder(unit);
if (result.IsSuccess)
{
    Game.Logger.LogInfo("单位开始移动到: {Target}", targetPosition);
}
#endif
```

#### 路径寻找移动

```csharp
#if SERVER
// 使用路径寻找的移动
public async Task<bool> MoveUnitToTarget(Unit unit, ITarget target, float maxDistance = 0)
{
    var walkable = unit.GetComponent<Walkable>();
    if (walkable == null)
    {
        return false;
    }

    // 直接调用路径寻找
    bool pathFound = walkable.PathTo(target, maxDistance);
    if (!pathFound)
    {
        Game.Logger.LogWarning("无法找到到达目标的路径: {Target}", target);
        return false;
    }

    // 等待移动完成
    while (unit.HasState(UnitState.IsMoving) && unit.IsValid)
    {
        await Game.DelayFrames(1);
    }

    return true;
}
#endif
```

### 🕹️ 摇杆移动指令

摇杆移动指令提供平滑的方向性移动控制。

> **⚠️ 指令队列限制**：摇杆移动指令（`VectorMove`）受指令队列管理，不能与技能释放同时进行。如需在施法时移动，请使用直接移动方法。

#### 开始摇杆移动

```csharp
#if SERVER
// 开始摇杆移动
var vectorMoveCommand = new Command
{
    Index = CommandIndex.VectorMove,
    Type = ComponentTagEx.Walkable,
    Target = new Angle(45),  // 移动方向（度数）
    Player = player,
    Flag = CommandFlag.IsUser
};

var result = vectorMoveCommand.IssueOrder(unit);
if (result.IsSuccess)
{
    Game.Logger.LogInfo("单位开始向量移动，方向: {Direction}度", 45);
}
#endif
```

#### 停止摇杆移动

```csharp
#if SERVER
// 停止摇杆移动
var stopVectorMoveCommand = new Command
{
    Index = CommandIndex.VectorMoveStop,
    Type = ComponentTagEx.Walkable,
    Player = player,
    Flag = CommandFlag.IsUser
};

var result = stopVectorMoveCommand.IssueOrder(unit);
#endif
```

#### 摇杆输入处理

```csharp
#if CLIENT
// 处理摇杆输入（客户端）
public class JoystickInputHandler
{
    private Unit? controlledUnit;
    private bool isMoving = false;

    public void HandleJoystickInput(Vector2 inputValue)
    {
        if (controlledUnit == null) return;

        // 死区处理
        if (inputValue.Length() < 0.1f)
        {
            if (isMoving)
            {
                StopVectorMove();
            }
            return;
        }

        // 计算移动角度
        var angle = Math.Atan2(inputValue.Y, inputValue.X) * (180.0 / Math.PI);
        
        // 发送移动指令到服务器
        var command = new Command
        {
            Index = CommandIndex.VectorMove,
            Type = ComponentTagEx.Walkable,
            Target = new Angle((float)angle),
            Player = Player.LocalPlayer,
            Flag = CommandFlag.IsUser
        };

        var result = command.IssueOrder(controlledUnit);
        if (result.IsSuccess)
        {
            isMoving = true;
        }
    }

    private void StopVectorMove()
    {
        if (controlledUnit == null) return;

        var command = new Command
        {
            Index = CommandIndex.VectorMoveStop,
            Type = ComponentTagEx.Walkable,
            Player = Player.LocalPlayer,
            Flag = CommandFlag.IsUser
        };

        command.IssueOrder(controlledUnit);
        isMoving = false;
    }
}
#endif
```

#### 直接移动方法（绕过指令队列）

当需要在技能释放期间移动单位时，可以使用直接移动方法绕过指令队列限制：

```csharp
#if SERVER
// 直接启动摇杆移动（不受指令队列限制）
public void StartDirectJoystickMove(Unit unit, Angle direction)
{
    // 直接调用单位的移动方法，绕过指令系统
    unit.StartJoystickMove(direction);
    
    Game.Logger.LogInfo("单位开始直接摇杆移动，方向: {Direction}度", direction.Degrees);
}

// 停止直接摇杆移动
public void StopDirectJoystickMove(Unit unit)
{
    unit.StopJoystickMove();
    Game.Logger.LogInfo("停止直接摇杆移动");
}

// 直接寻路移动（绕过指令队列）
public void DirectPathTo(Unit unit, ScenePoint targetPosition)
{
    // 直接调用寻路，不受指令队列影响
    unit.PathTo(targetPosition);
    
    Game.Logger.LogInfo("单位开始直接寻路到: {Position}", targetPosition);
}

// 施法期间移动的实用示例
public async Task CastWithMovement(Unit caster, IGameLink<GameDataAbility> ability, Unit target)
{
    // 开始施法
    var castCommand = new Command
    {
        AbilityLink = ability,
        Index = CommandIndex.Execute,
        Type = ComponentTagEx.AbilityManager,
        Target = target,
        Flag = CommandFlag.IsSystem
    };
    
    var result = castCommand.IssueOrder(caster);
    if (!result.IsSuccess)
        return;
    
    // 在施法期间允许移动（仅适用于允许移动施法的技能）
    if (ability.Data.CanMoveWhileCasting)
    {
        // 使用直接移动方法，不会被指令队列阻止
        var moveDirection = CalculateOptimalMoveDirection(caster, target);
        caster.StartJoystickMove(moveDirection);
        
        // 等待技能完成
        await WaitForAbilityComplete(caster);
        
        // 停止移动
        caster.StopJoystickMove();
    }
}
#endif
```

#### 移动方法对比

| 方法类型 | 指令队列影响 | 技能期间可用 | 适用场景 |
|---------|-------------|-------------|----------|
| `Move` 指令 | ✅ 受限制 | ❌ 不可用* | 标准单位移动 |
| `VectorMove` 指令 | ✅ 受限制 | ❌ 不可用* | 玩家控制的摇杆移动 |
| `Unit.StartJoystickMove()` | ❌ 不受限制 | ✅ 可用 | 技能期间移动、AI灵活控制 |
| `Unit.PathTo()` | ❌ 不受限制 | ✅ 可用 | 直接寻路移动、AI控制 |

> ***注意**：瞬发（Transient）技能期间可以正常使用指令系统移动

### 📦 物品指令

物品指令处理物品栏的各种操作：

#### 拾取物品

```csharp
#if SERVER
// 拾取物品指令
var pickupCommand = new Command
{
    Index = CommandIndexInventory.PickUp,
    Type = ComponentTagEx.InventoryManager,
    Target = droppedItem,
    Player = player,
    Flag = CommandFlag.IsUser
};

var result = pickupCommand.IssueOrder(unit);
#endif
```

#### 使用物品

```csharp
#if SERVER
// 使用物品指令
var useItemCommand = new Command
{
    Index = CommandIndexInventory.Use,
    Type = ComponentTagEx.InventoryManager,
    Item = potionItem,
    Player = player,
    Flag = CommandFlag.IsUser
};

var result = useItemCommand.IssueOrder(unit);
#endif
```

#### 物品栏操作

```csharp
#if SERVER
// 交换物品位置
var swapCommand = new Command
{
    Index = CommandIndexInventory.Swap,
    Type = ComponentTagEx.InventoryManager,
    Target = targetSlot,
    Item = sourceItem,
    Player = player,
    Flag = CommandFlag.IsUser
};

// 丢弃物品
var dropCommand = new Command
{
    Index = CommandIndexInventory.Drop,
    Type = ComponentTagEx.InventoryManager,
    Item = unwantedItem,
    Player = player,
    Flag = CommandFlag.IsUser
};

// 给予物品
var giveCommand = new Command
{
    Index = CommandIndexInventory.Give,
    Type = ComponentTagEx.InventoryManager,
    Target = otherPlayer.MainUnit,
    Item = giftItem,
    Player = player,
    Flag = CommandFlag.IsUser
};
#endif
```

### ⚔️ 攻击指令

攻击指令是特殊的技能指令，它与普通技能指令的关键区别在于**自动查找攻击技能**的机制。

#### 🎯 攻击技能标识机制

**重要**：技能要被攻击指令使用，必须在数编表中设置 `IsAttack` 标志：

```csharp
// GameDataAbilityExecute 数编表配置
var swordAttackData = new GameDataAbilityExecute
{
    // ✅ 关键：必须设置 IsAttack = true
    AbilityExecuteFlags = new AbilityExecuteFlags 
    { 
        IsAttack = true  // 标记为攻击技能
    },
    // 其他技能配置...
};
```

#### 🔄 攻击指令 vs 技能指令

| 指令类型 | AbilityLink字段 | 技能查找方式 | 适用场景 |
|---------|----------------|-------------|----------|
| **Attack指令** | 🔹 可选，通常不填 | 自动查找 `IsAttack=true` 的技能 | 通用攻击、AI攻击 |
| **Execute指令** | ✅ 必填 | 使用指定的技能 | 特定技能释放 |

#### 基本使用示例

```csharp
#if SERVER
// ✅ 攻击指令 - 自动查找攻击技能
var attackCommand = new Command
{
    Index = CommandIndex.Attack,
    Type = ComponentTagEx.AbilityManager,
    Target = enemyUnit,
    Player = player,
    Flag = CommandFlag.IsUser
    // 注意：不需要设置 AbilityLink，系统自动查找
};

var result = attackCommand.IssueOrder(attackerUnit);

// ✅ 指定特定攻击技能（可选）
var specificAttackCommand = new Command
{
    Index = CommandIndex.Attack,
    Type = ComponentTagEx.AbilityManager,
    Target = enemyUnit,
    AbilityLink = ScopeData.Ability.PowerfulStrike, // 指定特定攻击技能
    Player = player,
    Flag = CommandFlag.IsUser
};

// AI攻击逻辑
public CmdResult AIAttackTarget(Unit aiUnit, Unit target)
{
    var command = new Command
    {
        Index = CommandIndex.Attack,
        Type = ComponentTagEx.AbilityManager,
        Target = target,
        Flag = CommandFlag.IsAI
    };
    
    return command.IssueOrder(aiUnit);
}
#endif
```

#### 🔍 自动查找机制

当使用攻击指令时，系统按以下优先级查找攻击技能：

1. **指定AbilityLink**：如果指令中指定了 `AbilityLink`，检查该技能是否设置了 `IsAttack=true`
2. **自动查找**：如果未指定，系统遍历单位所有技能，寻找第一个满足条件的攻击技能：
   - `IsAttack = true`
   - `IsValid = true` 
   - `IsEnabled = true`

```csharp
#if SERVER
// 系统内部的查找逻辑示例
public AbilityExecute? GetValidAttack()
{
    return GetOne<AbilityExecute>(ability => 
        ability.IsAttack &&      // 必须标记为攻击技能
        ability.IsValid &&       // 技能有效
        ability.IsEnabled);      // 技能启用
}
#endif
```

#### ❌ 常见问题与解决方案

**问题**：给单位添加技能后，Attack指令无法使用该技能攻击，单位毫无反应

**原因**：技能的 `IsAttack` 标志未设置为 `true`

**解决方案**：
```csharp
// ❌ 错误：缺少 IsAttack 标志
var weaponSkillData = new GameDataAbilityExecute
{
    // 缺少 AbilityExecuteFlags 配置
};

// ✅ 正确：设置 IsAttack 标志
var weaponSkillData = new GameDataAbilityExecute
{
    AbilityExecuteFlags = new AbilityExecuteFlags 
    { 
        IsAttack = true  // 标记为攻击技能
    },
    // 其他配置...
};
```

**替代方案**：如果不想修改技能配置，可以使用技能指令：
```csharp
// 使用技能指令直接指定技能
var skillCommand = new Command
{
    Index = CommandIndex.Execute,  // 使用Execute而不是Attack
    Type = ComponentTagEx.AbilityManager,
    AbilityLink = ScopeData.Ability.YourWeaponSkill, // 必须指定技能
    Target = enemyUnit,
};
```

#### 🎮 多攻击技能管理

单位可以拥有多个攻击技能，系统会按技能添加顺序选择第一个有效的：

```csharp
#if SERVER
// 为单位添加多个攻击技能
unit.GetComponent<AbilityManager>()?.Add(ScopeData.Ability.MeleeAttack);
unit.GetComponent<AbilityManager>()?.Add(ScopeData.Ability.RangedAttack);
unit.GetComponent<AbilityManager>()?.Add(ScopeData.Ability.MagicAttack);

// 攻击指令会自动选择第一个有效的攻击技能
var attackCommand = new Command
{
    Index = CommandIndex.Attack,
    Target = enemyUnit,
    Flag = CommandFlag.IsAI
};

// 系统会按添加顺序检查：MeleeAttack -> RangedAttack -> MagicAttack
var result = attackCommand.IssueOrder(unit);
#endif
```

### ⏹️ 停止指令

停止指令可以中断单位当前的所有操作：

```csharp
#if SERVER
// 停止当前所有操作
var stopCommand = new Command
{
    Index = CommandIndex.Stop,
    Player = player,
    Flag = CommandFlag.IsUser
};

var result = stopCommand.IssueOrder(unit);
if (result.IsSuccess)
{
    Game.Logger.LogInfo("单位停止所有操作");
}
#endif
```

## 🔄 指令生命周期

### 指令阶段（OrderStage）

每个指令都经历以下阶段：

1. **Approach** - 接近阶段（移动到目标范围内）
2. **Preswing** - 前摇阶段（准备动作）
3. **Channel** - 引导阶段（持续施法）
4. **Backswing** - 后摇阶段（收尾动作）

```csharp
#if SERVER
// 监听指令阶段变化
public class OrderStageMonitor
{
    public void MonitorOrderStages(Unit unit)
    {
        var queue = unit.GetComponent<OrderQueue>();
        if (queue == null) return;

        var currentOrder = queue.Peek();
        if (currentOrder != null)
        {
            Game.Logger.LogInfo("当前指令阶段: {Stage}, 状态: {State}", 
                currentOrder.Stage, currentOrder.State);
        }
    }
}
#endif
```

### 指令状态（OrderState）

- **Normal** - 正常状态
- **Cancelled** - 已取消
- **Completed** - 已完成
- **Failed** - 执行失败

### 可打断性

```csharp
#if SERVER
// 检查指令是否可打断
public bool CanInterruptCurrentOrder(Unit unit, Command newCommand)
{
    var queue = unit.GetComponent<OrderQueue>();
    var currentOrder = queue?.Peek();
    
    if (currentOrder == null) return true;
    
    // 检查当前指令是否可被新指令打断
    return currentOrder.CanBeInterruptedBy(newOrder);
}
#endif
```



## 📊 指令队列管理

### 队列操作模式

#### 立即执行（Preempt）

```csharp
#if SERVER
// 立即执行，打断当前指令
var urgentCommand = new Command
{
    AbilityLink = ScopeData.Ability.EmergencyHeal,
    Index = CommandIndex.Execute,
    Flag = CommandFlag.IsUser | CommandFlag.Preempt,  // 抢占式
    Player = player
};
#endif
```

#### 排队执行（Queued）

```csharp
#if SERVER
// 排队执行，等待当前指令完成
var queuedCommand = new Command
{
    AbilityLink = ScopeData.Ability.Fireball,
    Index = CommandIndex.Execute,
    Flag = CommandFlag.IsUser | CommandFlag.Queued,   // 排队
    Player = player
};
#endif
```

#### 队列管理

```csharp
#if SERVER
// 暂停指令队列
public void PauseUnitOrders(Unit unit)
{
    var queue = unit.GetComponent<OrderQueue>();
    queue?.Pause();
}

// 恢复指令队列
public void ResumeUnitOrders(Unit unit)
{
    var queue = unit.GetComponent<OrderQueue>();
    queue?.Resume();
}

// 清空队列
public void ClearAllOrders(Unit unit)
{
    var queue = unit.GetComponent<OrderQueue>();
    if (queue != null)
    {
        while (!queue.IsEmpty)
        {
            queue.Peek()?.Cancel();
        }
    }
}
#endif
```

## 🎮 摇杆系统

WasiCore 提供三种类型的摇杆控件，用于不同的游戏场景：

### 摇杆类型

#### 1. JoystickNormal - 普通摇杆

固定位置的传统摇杆：

```csharp
#if CLIENT
// 创建普通摇杆
var joystick = new JoystickNormal()
{
    Radius = 80f,
    KnobSize = 30f,
    IsEnabled = true
};

// 处理摇杆输入
joystick.ValueChanged += (sender, e) =>
{
    var inputValue = e.NewValue;
    HandleJoystickMovement(inputValue);
};
#endif
```

#### 2. JoystickFloat - 浮动摇杆

在触摸位置动态出现：

```csharp
#if CLIENT
// 创建浮动摇杆
var floatJoystick = new JoystickFloat()
{
    Radius = 60f,
    KnobSize = 25f
};

floatJoystick.Activated += (sender, e) =>
{
    Game.Logger.LogInfo("摇杆在位置激活: {Position}", e.Position);
};

floatJoystick.Deactivated += (sender, e) =>
{
    // 停止移动
    StopVectorMovement();
};
#endif
```

#### 3. JoystickDynamic - 动态摇杆

中心点跟随手指移动：

```csharp
#if CLIENT
// 创建动态摇杆
var dynamicJoystick = new JoystickDynamic()
{
    Radius = 100f,
    FollowSensitivity = 0.3f
};

dynamicJoystick.CenterChanged += (sender, e) =>
{
    Game.Logger.LogInfo("摇杆中心从 {OldCenter} 移动到 {NewCenter}", 
        e.OldCenter, e.NewCenter);
};
#endif
```

### 摇杆与移动指令集成

```csharp
#if CLIENT
public class PlayerMovementController
{
    private Unit? controlledUnit;
    private JoystickNormal movementJoystick;
    private bool isCurrentlyMoving = false;

    public PlayerMovementController(Unit unit)
    {
        controlledUnit = unit;
        SetupJoystick();
    }

    private void SetupJoystick()
    {
        movementJoystick = new JoystickNormal();
        movementJoystick.ValueChanged += OnJoystickValueChanged;
    }

    private void OnJoystickValueChanged(object? sender, JoystickValueChangedEventArgs e)
    {
        var inputValue = e.NewValue;
        
        // 死区处理
        if (inputValue.Length() < 0.1f)
        {
            if (isCurrentlyMoving)
            {
                SendStopMovementCommand();
            }
            return;
        }

        // 计算移动方向
        var angle = Math.Atan2(inputValue.Y, inputValue.X) * (180.0 / Math.PI);
        SendMovementCommand((float)angle);
    }

    private void SendMovementCommand(float angle)
    {
        if (controlledUnit == null) return;

        var command = new Command
        {
            Index = CommandIndex.VectorMove,
            Type = ComponentTagEx.Walkable,
            Target = new Angle(angle),
            Player = Player.LocalPlayer,
            Flag = CommandFlag.IsUser
        };

        var result = command.IssueOrder(controlledUnit);
        if (result.IsSuccess)
        {
            isCurrentlyMoving = true;
        }
    }

    private void SendStopMovementCommand()
    {
        if (controlledUnit == null) return;

        var command = new Command
        {
            Index = CommandIndex.VectorMoveStop,
            Type = ComponentTagEx.Walkable,
            Player = Player.LocalPlayer,
            Flag = CommandFlag.IsUser
        };

        command.IssueOrder(controlledUnit);
        isCurrentlyMoving = false;
    }
}
#endif
```

## 🛠️ 实用示例

### 完整的玩家控制器

```csharp
#if CLIENT
public class GamePlayerController
{
    private Unit? playerUnit;
    private JoystickNormal movementJoystick;
    private Dictionary<string, Button> skillButtons = new();

    public void Initialize(Unit unit)
    {
        playerUnit = unit;
        SetupControls();
    }

    private void SetupControls()
    {
        // 设置移动摇杆
        movementJoystick = new JoystickNormal();
        movementJoystick.ValueChanged += OnMovementChanged;

        // 设置技能按钮
        CreateSkillButton("Q", ScopeData.Ability.Fireball);
        CreateSkillButton("W", ScopeData.Ability.Shield);
        CreateSkillButton("E", ScopeData.Ability.Teleport);
    }

    private void CreateSkillButton(string key, IGameLink<GameDataAbility> abilityLink)
    {
        var button = new Button();
        button.Clicked += () => CastSkill(abilityLink);
        skillButtons[key] = button;
    }

    private void OnMovementChanged(object? sender, JoystickValueChangedEventArgs e)
    {
        if (playerUnit == null) return;

        var inputValue = e.NewValue;
        
        if (inputValue.Length() < 0.1f)
        {
            // 停止移动
            var stopCommand = new Command
            {
                Index = CommandIndex.VectorMoveStop,
                Type = ComponentTagEx.Walkable,
                Player = Player.LocalPlayer,
                Flag = CommandFlag.IsUser
            };
            stopCommand.IssueOrder(playerUnit);
        }
        else
        {
            // 开始移动
            var angle = Math.Atan2(inputValue.Y, inputValue.X) * (180.0 / Math.PI);
            var moveCommand = new Command
            {
                Index = CommandIndex.VectorMove,
                Type = ComponentTagEx.Walkable,
                Target = new Angle((float)angle),
                Player = Player.LocalPlayer,
                Flag = CommandFlag.IsUser
            };
            moveCommand.IssueOrder(playerUnit);
        }
    }

    private void CastSkill(IGameLink<GameDataAbility> abilityLink)
    {
        if (playerUnit == null) return;

        var command = new Command
        {
            AbilityLink = abilityLink,
            Index = CommandIndex.Execute,
            Type = ComponentTagEx.AbilityManager,
            Player = Player.LocalPlayer,
            Flag = CommandFlag.IsUser
        };

        var result = command.IssueOrder(playerUnit);
        if (!result.IsSuccess)
        {
            Game.Logger.LogWarning("技能释放失败: {Error}", result.Error);
        }
    }
}
#endif
```

### AI行为控制器

```csharp
#if SERVER
public class SimpleAIController
{
    private Unit aiUnit;
    private Unit? currentTarget;

    public SimpleAIController(Unit unit)
    {
        aiUnit = unit;
    }

    public async Task RunAI()
    {
        while (aiUnit.IsValid && aiUnit.IsAlive)
        {
            // 寻找目标
            currentTarget = FindNearestEnemy();
            
            if (currentTarget != null && currentTarget.IsAlive)
            {
                // 攻击目标
                var attackResult = AttackTarget(currentTarget);
                if (!attackResult.IsSuccess)
                {
                    // 如果攻击失败，尝试移动到目标
                    MoveTo(currentTarget);
                }
            }
            else
            {
                // 没有目标时巡逻
                Patrol();
            }

            // 等待一段时间再次思考
            await Game.Delay(TimeSpan.FromSeconds(1));
        }
    }

    private CmdResult AttackTarget(Unit target)
    {
        var command = new Command
        {
            Index = CommandIndex.Attack,
            Type = ComponentTagEx.AbilityManager,
            Target = target,
            Flag = CommandFlag.IsAI
        };

        return command.IssueOrder(aiUnit);
    }

    private CmdResult MoveTo(Unit target)
    {
        var command = new Command
        {
            Index = CommandIndex.Move,
            Type = ComponentTagEx.Walkable,
            Target = target,
            Flag = CommandFlag.IsAI
        };

        return command.IssueOrder(aiUnit);
    }

    private void Patrol()
    {
        // 简单的巡逻逻辑
        var randomDirection = new Angle(Random.Shared.Next(0, 360));
        var patrolDistance = 100f;
        var patrolTarget = aiUnit.Position + randomDirection.ToVector2() * patrolDistance;

        var command = new Command
        {
            Index = CommandIndex.Move,
            Type = ComponentTagEx.Walkable,
            Target = new ScenePoint(patrolTarget, aiUnit.Scene),
            Flag = CommandFlag.IsAI
        };

        command.IssueOrder(aiUnit);
    }

    private Unit? FindNearestEnemy()
    {
        // 在这里实现寻找最近敌人的逻辑
        // 返回最近的敌方单位
        return null;
    }
}
#endif
```

## 🔧 API 参考

### Command 结构体

```csharp
public record struct Command
{
    public IGameLink<GameDataAbility>? AbilityLink { get; set; }
    public CommandIndex Index { get; set; }
    public ComponentTag Type { get; set; }
    public CommandFlag Flag { get; set; }
    public ICommandTarget? Target { get; set; }
    public Item? Item { get; set; }
    public Player? Player { get; set; }
    
    // 属性
    public bool IsRequest { get; }
    public bool IsAcquired { get; }
    
    // 方法
    public CmdResult<Order> IssueOrder(Entity entity, bool testOnly = false);  // 服务端
    public CmdResult IssueOrder(Entity entity, bool testOnly = false);        // 客户端
}
```

### CommandIndex 枚举

```csharp
public enum CommandIndex
{
    Execute,            // 执行技能
    TurnOn,            // 开启切换技能
    TurnOff,           // 关闭切换技能
    Toggle,            // 智能切换
    Attack,            // 攻击
    Stop,              // 停止
    VectorMove,        // 向量移动
    VectorMoveStop,    // 停止向量移动
    Move,              // 移动到目标
    
    // 物品指令
    PickUp,            // 拾取物品
    Use,               // 使用物品
    Swap,              // 交换物品
    Give,              // 给予物品
    Drop,              // 丢弃物品
}
```

### CommandFlag 标志

```csharp
[Flags]
public enum CommandFlag
{
    None = 0,
    Queued = 1,            // 排队执行
    Preempt = 1 << 1,      // 抢占执行
    IsAcquired = 1 << 2,   // 自动获得
    IsRequest = 1 << 4,    // 请求指令
    IsUser = 1 << 5,       // 用户指令
    IsAI = 1 << 6,         // AI指令
}
```

### Order 抽象类

```csharp
public abstract class Order : IDisposable
{
    // 属性
    public Command Command { get; }
    public Entity Owner { get; }
    public OrderStage Stage { get; }
    public OrderState State { get; }
    public bool IsUnInterruptible { get; }
    public bool IsAutoPreemptible { get; }
    
    // 方法
    public void Cancel();
    public void Destroy();
    protected abstract CmdResult ExecuteEffect();
}
```

### OrderQueue 类

```csharp
public class OrderQueue : TagComponent
{
    // 属性
    public bool IsPaused { get; }
    public bool IsEmpty { get; }
    public bool HasSingleOrder { get; }
    
    // 方法
    public void Add(Order order, CommandFlag flag);
    public Order? Peek();
    public void Pause();
    public void Resume();
    public IEnumerable<Order> GetOrders(Func<Order, bool> filter);
}
```

### AbilityExecuteFlags 类

```csharp
public class AbilityExecuteFlags
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
    
    /// <summary>
    /// 技能是否可以无限施法（开发中）。
    /// </summary>
    public bool InfiniteCasting { get; set; }
    
    /// <summary>
    /// 技能是否可以重新接近目标（开发中）。
    /// </summary>
    public bool Reapproachable { get; set; }
}
```

### AbilityExecute 类

```csharp
public partial class AbilityExecute : AbilityActive
{
    /// <summary>
    /// 技能是否为攻击技能，由 Cache.AbilityExecuteFlags.IsAttack 决定
    /// </summary>
    public bool IsAttack { get; }
    
    /// <summary>
    /// 技能的有效范围
    /// </summary>
    public float Range { get; }
    
    /// <summary>
    /// 无目标和向量目标技能是否总是获取目标
    /// </summary>
    public bool AlwaysAcquireTarget { get; }
}
```

### 摇杆控件 API

```csharp
// JoystickNormal
public class JoystickNormal : Panel
{
    public Vector2 InputValue { get; }
    public float Radius { get; set; }
    public float KnobSize { get; set; }
    public bool IsEnabled { get; set; }
    
    public event EventHandler<JoystickValueChangedEventArgs> ValueChanged;
    public event EventHandler DragStarted;
    public event EventHandler DragEnded;
}

// JoystickFloat
public class JoystickFloat : Control
{
    public Vector2 InputValue { get; }
    public bool IsActive { get; }
    
    public event EventHandler<JoystickActivatedEventArgs> Activated;
    public event EventHandler Deactivated;
}

// JoystickDynamic
public class JoystickDynamic : Control
{
    public Vector2 CurrentCenterPosition { get; }
    public float FollowSensitivity { get; set; }
    
    public event EventHandler<JoystickCenterChangedEventArgs> CenterChanged;
}
```

## 💡 最佳实践

### ✅ 推荐做法

1. **统一指令接口** - 所有操作都通过指令系统处理
2. **正确配置攻击技能** - 在 `GameDataAbilityExecute` 中设置 `IsAttack = true`，确保技能能被攻击指令使用
3. **异步操作** - 使用 `Game.Delay()` 等待指令完成
4. **错误处理** - 检查指令结果并适当处理失败情况
5. **队列管理** - 合理使用指令标志控制执行顺序
6. **AI优化** - AI使用 `IsAI` 标志避免不必要的权限检查
7. **摇杆调优** - 设置合适的死区和敏感度
8. **资源清理** - 及时清理不需要的指令和事件监听

### ❌ 避免的做法

1. **直接操作** - 不要绕过指令系统直接操作单位
2. **忽略结果** - 不要忽略指令执行的返回结果
3. **阻塞等待** - 不要使用 `.Result` 等阻塞方法等待指令
4. **频繁指令** - 避免过于频繁地发送相同指令
5. **权限忽略** - 不要忽略玩家权限检查
6. **内存泄漏** - 不要忘记清理摇杆事件监听器

## 🔗 相关文档

- [🔄 异步编程](../best-practices/AsyncProgramming.md) - 异步指令处理最佳实践
- [🎯 单位系统](UnitSystem.md) - 单位的基础功能和管理
- [⚡ 技能系统](AbilitySystem.md) - 技能配置、施法时间控制和攻击技能详解
- [📦 物品系统](ItemSystem.md) - 物品和背包管理
- [🤖 AI系统](AISystem.md) - AI使用指令系统的最佳实践

---

> 💡 **提示**: 指令系统是连接玩家输入和游戏逻辑的桥梁，正确使用指令系统可以确保游戏操作的一致性和可预测性。在设计游戏交互时，始终考虑使用适当的指令类型和标志。