# Simple2DMultiplayerGame 常见错误速查表

## 🎯 快速导航

- [编译错误](#-编译错误)
- [运行时错误](#-运行时错误)
- [逻辑错误](#-逻辑错误)
- [性能问题](#-性能问题)

---

## 🔴 编译错误

### ❌ 错误1：找不到生成的包装器类型

**错误信息**:
```
error CS0246: The type or namespace name 'Bird' could not be found
```

**原因**:
1. 忘记添加 `[PropertyObjectWrapper]` 特性
2. 枚举命名不符合规范（应该以 EProperty 开头）
3. 未重新编译项目

**解决方案**:
```csharp
// ✅ 正确：添加特性并重新编译
[PropertyObjectWrapper]  // ← 必须有这个特性
[EnumExtension(Extendable = true)]
public enum EPropertyBird  // ← 以 EProperty 开头
{
    PlayerId,
    BirdY,
}

// 然后重新编译项目
// dotnet build
```

---

### ❌ 错误2：类型转换错误

**错误信息**:
```
error CS0266: Cannot implicitly convert type 'float' to 'int'
```

**原因**: 类型推断不符合预期

**示例**:
```csharp
// ❌ 错误：GameDuration 被推断为 int，但赋值 float
public enum EPropertyGame
{
    Score,         // → int
    GameDuration,  // → ??? (期望 float，但可能推断为 int)
}

gameState.GameDuration = 300f;  // 编译错误！
```

**解决方案1**: 使用类型转换
```csharp
gameState.GameDuration = (int)300f;  // 强制转换
```

**解决方案2**: 显式指定类型（推荐）
```csharp
public enum EPropertyGame
{
    Score,
    
    [PropertyType(typeof(float))]  // ← 显式指定
    GameDuration,
}
```

**解决方案3**: 使用符合推断规则的命名（推荐）
```csharp
public enum EPropertyGame
{
    Score,
    DurationRemaining,  // → float (包含 duration 和 remaining)
}
```

**参考**: [类型推断规则文档](./TypeInference.md)

---

### ❌ 错误3：泛型约束错误

**错误信息**:
```
error CS0311: The type 'MyGame' cannot be used as type parameter 'TSelf'
```

**原因**: 泛型约束不满足

**错误代码**:
```csharp
// ❌ 错误：没有传入自身类型
public class MyGame : RealtimeActionGameTemplate<FlappyBirdMultiplayer>
{
    // 应该传入 MyGame 而不是 FlappyBirdMultiplayer
}
```

**正确代码**:
```csharp
// ✅ 正确：传入自身类型
public class MyGame : RealtimeActionGameTemplate<MyGame>
{
    // 泛型参数是自身类型
}
```

---

## 🟡 运行时错误

### ❌ 错误4：对象不同步到客户端

**现象**: 服务端创建了 PropertyObject，但客户端看不到

**原因1**: 忘记设置 Category
```csharp
// ❌ 错误：没有设置 Category
var obj = CreateGameObject(player, SyncType.All);
var bird = new Bird(obj);  // 客户端可能无法正确识别
```

**解决方案**:
```csharp
// ✅ 正确：设置 Category
var obj = CreateGameObject(player, SyncType.All);
obj.Category = CategoryBird;  // ← 必须设置
var bird = new Bird(obj);
```

**原因2**: SyncType 设置错误
```csharp
// ❌ 错误：想让所有玩家看到，却用了 SyncType.Self
var obj = CreateGameObject(player, SyncType.Self);  // 只有所有者能看到
```

**解决方案**: 根据需求选择正确的 SyncType
```csharp
// ✅ 公共对象：使用 SyncType.All
var publicObj = CreateGameObject(player, SyncType.All);  // 所有玩家都能看到

// ✅ 私密数据：使用 SyncType.Self
var privateObj = CreateGameObject(player, SyncType.Self);  // 只有所有者能看到

// ✅ 队伍数据：使用 SyncType.Ally
var teamObj = CreateGameObject(player, SyncType.Ally);  // 所有者和队友能看到
```

**SyncType 选择指南**:
- `All` - 所有玩家（管道、敌人、公共区域）
- `Self` - 仅所有者（手牌、个人状态）
- `Ally` - 所有者和队友（队伍信息）
- ⚠️ 避免使用 `Sight`/`SelfOrSight`/`AllyOrSight`（3D游戏的视野系统，2D游戏用不到）

---

### ❌ 错误5：UI 不显示

**现象**: UI 元素创建了，但屏幕上看不到

**原因**: 忘记添加到可视化树

**错误代码**:
```csharp
#if CLIENT
protected override void OnClientInitialize()
{
    var panel = new Panel { Width = 800, Height = 600 };
    // ❌ 错误：没有添加到可视化树
}
#endif
```

**正确代码**:
```csharp
#if CLIENT
protected override void OnClientInitialize()
{
    var panel = new Panel { Width = 800, Height = 600 };
    panel.AddToRoot();  // ← 必须调用
}
#endif
```

---

### ❌ 错误6：消息处理器不触发

**现象**: 客户端发送消息，服务端没有响应

**原因1**: 忘记注册消息处理器
```csharp
// ❌ 错误：定义了处理器但没注册
#if SERVER
protected override void OnServerInitialize()
{
    // 忘记注册
}

private void OnJumpMessage(Player player, byte[] payload)
{
    // 永远不会被调用
}
#endif
```

**解决方案**:
```csharp
// ✅ 正确：注册消息处理器
#if SERVER
protected override void OnServerInitialize()
{
    RegisterMessageHandler((byte)MessageType.Jump, OnJumpMessage);  // ← 必须注册
}
#endif
```

**原因2**: 消息类型不匹配
```csharp
// ❌ 错误：发送和接收的消息类型不一致
// 客户端
SendMessageToServer((byte)MessageType.Jump);  // 发送 1

// 服务端
RegisterMessageHandler((byte)MessageType.Attack, ...);  // 注册 2 (不匹配！)
```

---

### ❌ 错误7：忘记调用 base 方法

**现象**: 框架功能失效

**错误代码**:
```csharp
// ❌ 错误：重写方法但没调用 base
protected override void OnPlayerJoined(Player player)
{
    // 直接实现，忘记调用 base
    CreatePlayerBird(player);
}
```

**正确代码**:
```csharp
// ✅ 正确：先调用 base
protected override void OnPlayerJoined(Player player)
{
    base.OnPlayerJoined(player);  // ← 必须调用
    CreatePlayerBird(player);
}
```

**受影响的方法**:
- `OnPlayerJoined(player)` - 必须调用 base
- `OnPlayerLeft(player)` - 必须调用 base
- `OnServerInitialize()` - 如果需要框架的初始化逻辑，必须调用 base

---

## 🟠 逻辑错误

### ❌ 错误8：客户端尝试修改游戏状态

**现象**: 客户端修改无效，或导致不同步

**错误代码**:
```csharp
#if CLIENT
private void OnJumpButtonClick()
{
    // ❌ 错误：客户端直接修改（无效）
    bird.BirdY += 10;  // setter 在客户端不存在或无效
}
#endif
```

**正确代码**:
```csharp
#if CLIENT
private void OnJumpButtonClick()
{
    // ✅ 正确：发送消息到服务端
    SendMessageToServer((byte)MessageType.Jump);
}
#endif

#if SERVER
private void OnJumpMessage(Player player, byte[] payload)
{
    // ✅ 服务端处理逻辑
    var bird = GetPlayerBird(player);
    bird.Jump();
}
#endif
```

---

### ❌ 错误9：使用错误的 Player 对象

**现象**: 操作影响了错误的玩家

**错误代码**:
```csharp
// ❌ 错误：使用 LocalPlayer 创建对象
#if SERVER
protected override void OnPlayerJoined(Player player)
{
    var obj = CreateGameObject(Player.LocalPlayer, SyncType.All);  // 错误！
}
#endif
```

**正确代码**:
```csharp
// ✅ 正确：使用参数传入的 player
#if SERVER
protected override void OnPlayerJoined(Player player)
{
    var obj = CreateGameObject(player, SyncType.All);  // ← 使用正确的 player
}
#endif
```

---

### ❌ 错误10：防作弊检查缺失

**现象**: 玩家可以作弊

**错误代码**:
```csharp
// ❌ 错误：没有验证玩家状态
private void OnJumpMessage(Player player, byte[] payload)
{
    var bird = GetPlayerBird(player);
    bird.Jump();  // 直接执行，没有任何检查
}
```

**正确代码**:
```csharp
// ✅ 正确：添加防作弊检查
private void OnJumpMessage(Player player, byte[] payload)
{
    var bird = GetPlayerBird(player);
    
    // 检查1：对象是否存在
    if (bird == null) return;
    
    // 检查2：玩家是否存活
    if (!bird.IsAlive) return;
    
    // 检查3：防止频繁操作
    if (GameTime - bird.LastJumpTime < 0.1f)
    {
        Game.Logger.LogWarning("Player {id} jump too frequent", player.Id);
        return;
    }
    
    // 执行操作
    bird.Jump();
    bird.LastJumpTime = GameTime;
}
```

---

## 🔵 性能问题

### ❌ 错误11：每帧遍历所有对象查找

**现象**: 客户端渲染卡顿

**错误代码**:
```csharp
#if CLIENT
protected override void OnClientRender(float deltaTime)
{
    // ❌ 错误：每次都遍历查找 GameState
    foreach (var obj in AllPropertyObjects)
    {
        if (obj.Category == CategoryGameState)
        {
            var gameState = new GameState(obj);
            UpdateTimer(gameState.TimeRemaining);  // 每帧查找
        }
    }
}
#endif
```

**正确代码**:
```csharp
#if CLIENT
// ✅ 正确：缓存唯一对象
private GameState? cachedGameState;

protected override void OnPropertyObjectCreated(PropertyObject obj)
{
    if (obj.Category == CategoryGameState)
    {
        cachedGameState = new GameState(obj);  // 缓存
    }
}

protected override void OnClientRender(float deltaTime)
{
    // 直接使用缓存
    if (cachedGameState != null)
    {
        UpdateTimer(cachedGameState.TimeRemaining);
    }
}
#endif
```

---

### ❌ 错误12：所有对象都用 SyncType.All

**现象**: 网络带宽浪费，可能泄露私密信息

**错误代码**:
```csharp
// ❌ 错误：玩家的私密数据也同步给所有人
var playerHand = CreateGameObject(player, SyncType.All);  // 手牌不应该让所有人看到
var playerGold = CreateGameObject(player, SyncType.All);  // 个人金币也同步给所有人
```

**正确代码**: 根据数据性质选择 SyncType
```csharp
// ✅ 公共对象：SyncType.All
var enemy = CreateGameObject(Player.DefaultPlayer, SyncType.All);  // 所有人看到相同的敌人

// ✅ 私密数据：SyncType.Self
var playerHand = CreateGameObject(player, SyncType.Self);  // 只有玩家自己能看到手牌
var playerGold = CreateGameObject(player, SyncType.Self);  // 只有玩家自己能看到金币

// ✅ 队伍数据：SyncType.Ally
var teamStrategy = CreateGameObject(player, SyncType.Ally);  // 队友可见的战术信息
```

**SyncType 选择原则**:
| 数据类型 | 推荐 SyncType | 示例 |
|---------|--------------|------|
| 公共游戏对象 | `All` | 管道、敌人、道具 |
| 玩家私密数据 | `Self` | 手牌、背包、个人状态 |
| 队伍共享数据 | `Ally` | 队伍标记、队友位置 |

---

### ❌ 错误13：使用 3D 游戏特有的 SyncType

**现象**: PropertyObject 同步行为异常或不符合预期

**错误代码**:
```csharp
// ❌ 错误：2D 游戏使用视野相关的 SyncType
var obj = CreateGameObject(player, SyncType.Sight);        // 需要战争迷雾（2D游戏无此概念）
var obj2 = CreateGameObject(player, SyncType.SelfOrSight); // 无意义
var obj3 = CreateGameObject(player, SyncType.AllyOrSight); // 无意义
```

**解决方案**: 使用 2D 游戏适用的 SyncType
```csharp
// ✅ 正确：只使用 Self、Ally、All
var publicObj = CreateGameObject(player, SyncType.All);   // 所有玩家
var privateObj = CreateGameObject(player, SyncType.Self); // 仅自己
var teamObj = CreateGameObject(player, SyncType.Ally);    // 自己和队友
```

**说明**:
- `Sight`/`SelfOrSight`/`AllyOrSight` 是为 3D 游戏的战争迷雾设计的
- PropertyObject 不推荐使用这些选项
- 2D 游戏没有"视野"概念，应该用 `Self`/`Ally`/`All` 控制可见性

---

## 📋 开发检查清单

### 创建新游戏时

- [ ] ✅ 定义独立的 GameMode（避免与其他游戏冲突）
- [ ] ✅ 继承正确的模板基类（Realtime 或 TurnBased）
- [ ] ✅ 传入自身类型作为泛型参数 `<MyGame>`
- [ ] ✅ 实现 `ShouldInitialize()` 方法
- [ ] ✅ 定义 Category 常量

### 定义属性枚举时

- [ ] ✅ 添加 `[PropertyObjectWrapper]` 特性
- [ ] ✅ 添加 `[EnumExtension(Extendable = true)]`
- [ ] ✅ 枚举名以 `EProperty` 开头
- [ ] ✅ 检查类型推断是否正确（查看 TYPE_INFERENCE_RULES.md）
- [ ] ✅ 对于特殊类型，使用 `[PropertyType]` 显式指定

### 服务端逻辑

- [ ] 🚨 **必须**调用 `base.OnPlayerJoined(player)`
- [ ] 🚨 **必须**调用 `base.OnPlayerLeft(player)`
- [ ] 🚨 **必须**设置 `obj.Category`
- [ ] ✅ 注册所有消息处理器
- [ ] ✅ 添加防作弊检查（状态验证、频率限制）
- [ ] ✅ 使用正确的 Player 对象（参数传入的，而非 LocalPlayer）
- [ ] ✅ 选择正确的 SyncType

### 客户端渲染

- [ ] 🚨 **必须**调用 `panel.AddToRoot()`
- [ ] ✅ 缓存全局唯一的对象（如 GameState）
- [ ] ✅ 检查 `obj.IsValid` 再使用
- [ ] ✅ 添加异常处理（try-catch）
- [ ] ✅ 避免在渲染循环中创建大量临时对象

### 消息系统

- [ ] 🚨 **必须**注册消息处理器
- [ ] ✅ 客户端和服务端的消息类型匹配
- [ ] ✅ 消息类型使用枚举（避免魔法数字）
- [ ] ✅ 服务端验证消息来源（防作弊）

---

## 🚨 关键步骤（必须执行）

### 1. **必须**设置 Category

```csharp
// 🚨 每个 PropertyObject 都应该设置 Category
var obj = CreateGameObject(player, SyncType.All);
obj.Category = CategoryBird;  // ← 必须！
```

**为什么**: 客户端通过 Category 区分对象类型

### 2. **必须**调用 base 方法

```csharp
// 🚨 重写框架方法时必须调用 base
protected override void OnPlayerJoined(Player player)
{
    base.OnPlayerJoined(player);  // ← 必须！
    // 你的逻辑
}
```

**为什么**: 框架需要执行初始化逻辑

### 3. **必须**添加 UI 到可视化树

```csharp
// 🚨 UI 必须添加到可视化树才能显示
var panel = new Panel { ... };
panel.AddToRoot();  // ← 必须！
```

**为什么**: 不在可视化树中的元素不会渲染

### 4. **必须**在服务端创建对象

```csharp
// 🚨 只能在服务端创建和修改 PropertyObject
#if SERVER
var obj = new PropertyObject(...);  // ✅ 正确
obj.SetProperty(...);               // ✅ 正确
#endif

#if CLIENT
// var obj = new PropertyObject(...);  // ❌ 编译错误
// obj.SetProperty(...);               // ❌ 客户端只读
var value = obj.GetProperty(...);      // ✅ 客户端只能读取
#endif
```

---

## 💡 调试技巧

### 问题：PropertyObject 未同步

**排查步骤**:
1. 检查服务端是否创建成功（添加日志）
2. 检查 SyncType 是否正确
3. 检查客户端是否有 `OnPropertyObjectCreated` 日志
4. 检查 Category 是否设置

```csharp
// 服务端调试
var obj = CreateGameObject(player, SyncType.All);
obj.Category = CategoryBird;
Game.Logger.LogInformation("Created Bird: Id={Id}, Category={Cat}", obj.Id, obj.Category);

// 客户端调试
protected override void OnPropertyObjectCreated(PropertyObject obj)
{
    Game.Logger.LogInformation("Received Object: Id={Id}, Category={Cat}", obj.Id, obj.Category);
}
```

### 问题：类型推断错误

**排查步骤**:
1. 检查属性名是否符合推断规则
2. 参考 [类型推断规则文档](./TypeInference.md)
3. 使用 `[PropertyType]` 显式指定类型

**常见推断规则**:
- `Duration`, `Time`, `Delay` → `float`
- `Id`, `Count`, `Index` → `int`
- `IsXxx`, `HasXxx`, `Alive` → `bool`

### 问题：消息不触发

**排查步骤**:
1. 检查消息类型是否匹配
2. 检查是否注册了处理器
3. 添加日志确认消息发送和接收

```csharp
// 客户端
SendMessageToServer((byte)MessageType.Jump);
Game.Logger.LogDebug("Sent Jump message");

// 服务端
private void OnJumpMessage(Player player, byte[] payload)
{
    Game.Logger.LogDebug("Received Jump from Player {id}", player.Id);
}
```

---

## ⚡ 快速修复参考

| 现象 | 可能原因 | 快速检查 |
|------|---------|---------|
| 编译找不到类型 | 缺少 PropertyObjectWrapper | 检查特性，重新编译 |
| 类型转换错误 | 推断类型错误 | 添加 [PropertyType] |
| 对象不同步 | 未设置 Category 或 SyncType 错误 | 检查这两个属性 |
| UI 不显示 | 未 AddToRoot | 添加到可视化树 |
| 消息不触发 | 未注册处理器 | 检查 RegisterMessageHandler |
| 操作无效 | 客户端修改 | 移到服务端 |
| 性能差 | 每帧遍历 | 缓存唯一对象 |

---

## 🎓 学习建议

### 新手建议

1. **从示例开始** - 先运行 FlappyBirdMultiplayer，理解整体流程
2. **小步迭代** - 先实现最基本功能，再逐步添加
3. **使用检查清单** - 创建对象、定义枚举时对照清单检查
4. **频繁编译** - 每完成一小步就编译，及早发现错误
5. **查看生成代码** - 了解 PropertyObjectWrapper 生成了什么

### AI 辅助开发建议

**提示 AI 时包含**:
- ✅ 明确游戏类型（实时/回合制）
- ✅ 明确需要的功能
- ✅ 参考 FlappyBirdMultiplayer 的代码组织
- ✅ 要求 AI 使用检查清单验证代码

**示例提示词**:
```
使用 WasiCore 的 Simple2DMultiplayerGame 框架（RealtimeActionGameTemplate）
创建一个多人贪吃蛇游戏。

要求：
1. 使用 PropertyObjectWrapper 自动生成包装器
2. 参考 FlappyBirdMultiplayer 的文件组织
3. 实现基础的移动和碰撞检测
4. 添加防作弊检查
5. 使用开发检查清单验证代码
```

---

## 📚 相关文档

- [Framework.md](./Framework.md) - 框架主文档
- [PropertyObject.md](./PropertyObject.md) - PropertyObject 基础
- [TypeInference.md](./TypeInference.md) - 类型推断规则
- [SyncType.md](./SyncType.md) - SyncType 选择指南
- [FlappyBird 多人版示例](../../Tests/Game/FlappyBirdMultiplayer/README.md) - 完整示例

---

**遇到问题？先查这份速查表！** 🔍

