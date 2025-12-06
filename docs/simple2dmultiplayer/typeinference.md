# PropertyObjectWrapper 类型推断规则

## 📖 概述

PropertyObjectWrapper 源代码生成器会根据属性名自动推断属性类型，减少手动标注的工作量。

---

## 🎯 推断规则

### 1. **int 类型**

包含以下关键词的属性名会被推断为 `int`：

| 关键词 | 示例 | 说明 |
|--------|------|------|
| `id` | `PlayerId`, `UnitId` | 标识符 |
| `count` | `PlayerCount`, `ItemCount` | 计数 |
| `index` | `CurrentIndex`, `PageIndex` | 索引 |

**示例：**
```csharp
[PropertyObjectWrapper]
public enum EPropertyPlayer
{
    PlayerId,      // → int
    Score,         // → int (默认)
    Level,         // → int (默认)
    ItemCount,     // → int
}
```

---

### 2. **float 类型**

包含以下关键词的属性名会被推断为 `float`：

#### 2.1 坐标/方向
| 关键词 | 示例 |
|--------|------|
| `x`, `y`, `z` | `BirdX`, `PositionY`, `CameraZ` |
| `position` | `PositionX`, `WorldPosition` |
| `offset` | `OffsetY`, `ScrollOffset` |
| `rotation` | `Rotation`, `RotationAngle` |
| `angle` | `Angle`, `ViewAngle` |

#### 2.2 时间相关 ⭐（新增优化）
| 关键词 | 示例 |
|--------|------|
| `time` | `GameTime`, `LastJumpTime` |
| `timer` | `RespawnTimer`, `CooldownTimer` |
| `duration` | `GameDuration`, `AnimationDuration` |
| `delay` | `SpawnDelay`, `AttackDelay` |
| `interval` | `SpawnInterval`, `UpdateInterval` |
| `cooldown` | `SkillCooldown`, `JumpCooldown` |
| `elapsed` | `ElapsedTime`, `TimeElapsed` |
| `remaining` | `TimeRemaining`, `DurationRemaining` |

#### 2.3 距离/尺寸
| 关键词 | 示例 |
|--------|------|
| `distance` | `Distance`, `ViewDistance` |
| `radius` | `CollisionRadius`, `ExplosionRadius` |
| `height` | `Height`, `JumpHeight` |
| `width` | `Width`, `ScreenWidth` |
| `depth` | `Depth`, `WaterDepth` |
| `length` | `Length`, `PathLength` |
| `size` | `Size`, `FontSize` |

#### 2.4 物理/运动
| 关键词 | 示例 |
|--------|------|
| `velocity` | `Velocity`, `BirdVelocity` |
| `speed` | `Speed`, `MoveSpeed`, `PipeSpeed` |
| `force` | `Force`, `GravityForce` |
| `power` | `Power`, `EnginePower` |
| `weight` | `Weight`, `TotalWeight` |
| `mass` | `Mass`, `ObjectMass` |

#### 2.5 游戏属性
| 关键词 | 示例 |
|--------|------|
| `energy` | `Energy`, `MaxEnergy` |
| `health` | `Health`, `MaxHealth` |
| `damage` | `Damage`, `AttackDamage` |
| `armor` | `Armor`, `DefenseArmor` |

#### 2.6 进度/百分比
| 关键词 | 示例 |
|--------|------|
| `progress` | `Progress`, `LoadingProgress` |
| `percent` | `Percent`, `CompletePercent` |
| `ratio` | `Ratio`, `AspectRatio` |
| `rate` | `Rate`, `FrameRate` |

#### 2.7 视觉效果
| 关键词 | 示例 |
|--------|------|
| `alpha` | `Alpha`, `TransparencyAlpha` |
| `opacity` | `Opacity`, `LayerOpacity` |
| `volume` | `Volume`, `SoundVolume` |
| `area` | `Area`, `CollisionArea` |
| `scale` | `Scale`, `SizeScale` |

**示例：**
```csharp
[PropertyObjectWrapper]
public enum EPropertyFlappyGame
{
    GameDuration,     // → float ✅ (包含 duration)
    TimeRemaining,    // → float ✅ (包含 remaining)
    PipeSpeed,        // → float ✅ (包含 speed)
    GapY,             // → float ✅ (包含 y)
}
```

---

### 3. **bool 类型**

包含以下关键词的属性名会被推断为 `bool`：

#### 3.1 状态前缀
| 关键词 | 示例 |
|--------|------|
| `is` | `IsAlive`, `IsActive` |
| `has` | `HasWeapon`, `HasKey` |
| `can` | `CanJump`, `CanAttack` |
| `should` | `ShouldRespawn`, `ShouldUpdate` |
| `will` | `WillExpire`, `WillDestroy` |
| `was` | `WasHit`, `WasCompleted` |

#### 3.2 生命/激活
| 关键词 | 示例 |
|--------|------|
| `alive` | `IsAlive`, `StillAlive` |
| `dead` | `IsDead`, `AlreadyDead` |
| `active` | `Active`, `IsActive` |
| `inactive` | `Inactive` |
| `enabled` | `Enabled`, `IsEnabled` |
| `disabled` | `Disabled` |

#### 3.3 可见性
| 关键词 | 示例 |
|--------|------|
| `visible` | `Visible`, `IsVisible` |
| `hidden` | `Hidden`, `IsHidden` |
| `shown` | `Shown`, `IsShown` |
| `collapsed` | `Collapsed` |

#### 3.4 选中/聚焦
| 关键词 | 示例 |
|--------|------|
| `selected` | `Selected`, `IsSelected` |
| `checked` | `Checked`, `IsChecked` |
| `focused` | `Focused`, `IsFocused` |
| `hovered` | `Hovered`, `IsHovered` |

#### 3.5 完成/成功
| 关键词 | 示例 |
|--------|------|
| `completed` | `Completed`, `IsCompleted` |
| `finished` | `Finished`, `IsFinished` |
| `success` | `Success`, `IsSuccess` |
| `failed` | `Failed`, `HasFailed` |
| `scored` | `Scored`, `HasScored` |
| `passed` | `Passed`, `HasPassed` |

#### 3.6 锁定/冻结
| 关键词 | 示例 |
|--------|------|
| `locked` | `Locked`, `IsLocked` |
| `frozen` | `Frozen`, `IsFrozen` |
| `paused` | `Paused`, `IsPaused` |
| `stopped` | `Stopped`, `IsStopped` |

#### 3.7 就绪/等待
| 关键词 | 示例 |
|--------|------|
| `ready` | `Ready`, `IsReady` |
| `waiting` | `Waiting`, `IsWaiting` |
| `loading` | `Loading`, `IsLoading` |
| `loaded` | `Loaded`, `IsLoaded` |

#### 3.8 有效/过期
| 关键词 | 示例 |
|--------|------|
| `valid` | `Valid`, `IsValid` |
| `invalid` | `Invalid`, `IsInvalid` |
| `expired` | `Expired`, `IsExpired` |
| `available` | `Available`, `IsAvailable` |

**示例：**
```csharp
[PropertyObjectWrapper]
public enum EPropertyBird
{
    IsAlive,          // → bool ✅ (包含 is + alive)
    Scored,           // → bool ✅ (包含 scored)
    IsGameActive,     // → bool ✅ (包含 is + active)
}
```

---

### 4. **long 类型**

包含以下关键词的属性名会被推断为 `long`：

| 关键词 | 示例 | 说明 |
|--------|------|------|
| `timestamp` | `Timestamp`, `CreateTimestamp` | Unix时间戳 |
| `tick` | `Tick`, `GameTick` | 游戏帧数 |

**示例：**
```csharp
[PropertyObjectWrapper]
public enum EPropertySession
{
    CreateTimestamp,  // → long ✅
    LastUpdateTick,   // → long ✅
}
```

---

### 5. **默认类型**

如果属性名不匹配任何规则，默认推断为 `int`。

**示例：**
```csharp
[PropertyObjectWrapper]
public enum EPropertyPlayer
{
    Score,        // → int (默认)
    Level,        // → int (默认)
    Experience,   // → int (默认)
}
```

---

## 🔧 显式指定类型

如果自动推断不符合需求，可以使用 `[PropertyType]` 特性显式指定：

```csharp
[PropertyObjectWrapper]
public enum EPropertyPlayer
{
    PlayerId,                          // → int (自动推断)
    
    [PropertyType(typeof(double))]
    PrecisePosition,                   // → double (显式指定)
    
    [PropertyType(typeof(string))]
    PlayerName,                        // → string (显式指定)
}
```

---

## 📊 推断优先级

1. **[PropertyType]** 显式特性 - 最高优先级
2. **int** 规则 - id/count/index
3. **float** 规则 - 坐标/时间/物理等
4. **bool** 规则 - 状态/标志
5. **long** 规则 - timestamp/tick
6. **默认 int** - 最低优先级

---

## ✅ 最佳实践

### 1. 使用清晰的命名

```csharp
// ✅ 推荐：清晰的命名，自动推断正确
GameDuration,      // → float
IsGameActive,      // → bool
PlayerScore,       // → int

// ❌ 避免：模糊的命名，可能需要显式指定
Duration1,         // → float (推断正确)
Flag1,             // → int (默认，可能不符合预期)
Value,             // → int (默认，可能不符合预期)
```

### 2. 遵循命名约定

```csharp
// ✅ 推荐：使用框架约定
IsAlive,           // bool - 以 Is 开头
HasWeapon,         // bool - 以 Has 开头
CanJump,           // bool - 以 Can 开头
BirdY,             // float - 包含 Y
MoveSpeed,         // float - 包含 Speed
PlayerId,          // int - 包含 Id
```

### 3. 必要时显式指定

```csharp
// 对于不常见的类型，使用 [PropertyType]
[PropertyType(typeof(string))]
PlayerName,

[PropertyType(typeof(Vector3))]
Position,

[PropertyType(typeof(Color))]
TintColor,
```

---

## 🎯 常见场景示例

### 场景1：2D游戏对象
```csharp
[PropertyObjectWrapper]
public enum EPropertyGameObject
{
    ObjectId,         // → int (id)
    PositionX,        // → float (x)
    PositionY,        // → float (y)
    Rotation,         // → float (rotation)
    Velocity,         // → float (velocity)
    IsActive,         // → bool (is + active)
}
```

### 场景2：游戏状态
```csharp
[PropertyObjectWrapper]
public enum EPropertyGameState
{
    GameDuration,     // → float ✅ (duration)
    TimeRemaining,    // → float ✅ (remaining)
    RoundNumber,      // → int (默认)
    IsGameActive,     // → bool (is + active)
    PlayerCount,      // → int (count)
}
```

### 场景3：玩家数据
```csharp
[PropertyObjectWrapper]
public enum EPropertyPlayer
{
    PlayerId,         // → int (id)
    Health,           // → float ✅ (health)
    MaxHealth,        // → float ✅ (health)
    Armor,            // → float ✅ (armor)
    MoveSpeed,        // → float (speed)
    IsAlive,          // → bool (is + alive)
    Score,            // → int (默认)
    Level,            // → int (默认)
}
```

### 场景4：技能/道具
```csharp
[PropertyObjectWrapper]
public enum EPropertySkill
{
    SkillId,          // → int (id)
    Cooldown,         // → float ✅ (cooldown)
    Duration,         // → float ✅ (duration)
    Damage,           // → float ✅ (damage)
    Range,            // → float (distance)
    IsAvailable,      // → bool (is + available)
    CastDelay,        // → float ✅ (delay)
}
```

---

## 🚀 优化历史

### v1.0 - 初始版本
- 基础推断规则：id/count → int, x/y/velocity → float, is/has → bool

### v1.1 - 时间系统优化 ✅
**新增时间相关关键词：**
- `duration` - 持续时间（如 `GameDuration`）
- `delay` - 延迟（如 `SpawnDelay`）
- `interval` - 间隔（如 `UpdateInterval`）
- `cooldown` - 冷却（如 `SkillCooldown`）
- `elapsed` - 已用时间（如 `ElapsedTime`）
- `remaining` - 剩余时间（如 `TimeRemaining`）

**新增物理/游戏属性：**
- `force`, `power`, `energy`, `health`, `damage`, `armor`
- `weight`, `mass`, `volume`, `area`
- `alpha`, `opacity`

**新增bool类型关键词：**
- 状态：`should`, `will`, `was`, `dead`, `inactive`, `disabled`
- 可见性：`shown`, `collapsed`
- 选中：`selected`, `checked`, `focused`, `hovered`
- 完成：`completed`, `finished`, `success`, `failed`, `passed`
- 锁定：`locked`, `frozen`, `paused`, `stopped`
- 就绪：`ready`, `waiting`, `loading`, `loaded`
- 有效：`valid`, `invalid`, `expired`, `available`

---

## 💡 贡献改进

如果发现某个常见属性名未被正确推断，可以：

1. 检查是否符合现有规则
2. 如果不符合，在 `PropertyObjectWrapperSourceGenerator.cs` 的 `InferPropertyType` 方法中添加规则
3. 更新本文档

**提交改进时请包含：**
- 新增的关键词
- 典型的使用场景
- 推断的目标类型

---

**让类型推断更智能，让开发更高效！** 🎉

