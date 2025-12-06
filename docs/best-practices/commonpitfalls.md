# 🚨 常见开发陷阱与解决方案

本文档总结了在使用 WasiCore 框架开发过程中容易遇到的问题及其解决方案，帮助开发者避免常见错误。

## 📋 目录

- [🎭 Entity vs Actor 混淆](#entity-vs-actor-混淆)
- [✨ ActorParticle 屏外优化陷阱](#actorparticle-屏外优化陷阱)
- [🔄 序列化问题](#序列化问题)
- [👥 玩家系统陷阱](#玩家系统陷阱)
- [⏰ 异步编程错误](#异步编程错误)
- [🎯 事件系统误用](#事件系统误用)
- [🔧 性能问题](#性能问题)
- [🌐 网络同步陷阱](#网络同步陷阱)

## 🎭 Entity vs Actor 混淆

### 陷阱1：混淆继承关系

**问题描述**：误以为Unit继承自Actor，或者Actor和Entity是同一类型

```csharp
// ❌ 错误理解：认为Unit继承自Actor
// 实际上：Unit : Entity，Actor是完全独立的类

// ❌ 错误：试图将Actor当作Entity使用
var enemy = new ActorModel(enemyLink, false, scope);
var health = enemy.GetProperty<float>(PropertyUnit.Health);  // 编译错误！

// ❌ 错误：试图将Entity当作Actor使用
var unit = CreateUnit(player, position, facing);
unit.AttachTo(parent, "socket");  // 编译错误！
```

**解决方案**：理解正确的架构关系

```csharp
// ✅ 正确理解：Entity和Actor是两个独立的系统
// Entity: 游戏逻辑、状态、同步
// Actor: 视觉表现、特效、模型

// ✅ 正确：为Unit创建对应的视觉Actor
#if SERVER
var unit = unitData.CreateUnit(player, position, facing);
#endif

// ✅ 正确：创建Unit的视觉表现
var unitModel = new ActorModel(unitModelLink, false, unit);
unitModel.AttachTo(unit, "body_socket");
```

### 陷阱2：在客户端创建Entity

**问题描述**：尝试在客户端创建Entity或Unit

```csharp
// ❌ 错误：客户端不能创建Entity
#if CLIENT
var unit = new Unit(unitLink, player, position, facing);  // 编译错误！
var building = buildingData.CreateUnit(player, pos, facing);  // 运行时错误！
#endif
```

**现象**：编译错误或运行时异常

**解决方案**：明确创建规则

```csharp
// ✅ 正确：Entity只能在服务端创建
#if SERVER
var unit = unitData.CreateUnit(player, position, facing);
var building = buildingData.CreateUnit(player, pos, facing);
#endif

// ✅ 正确：Actor可以在客户端创建
#if CLIENT
var effect = new ActorParticle(effectLink, false, transientScope);
var model = new ActorModel(modelLink, false, persistScope);
#endif
```

### 陷阱3：用Actor做游戏逻辑

**问题描述**：在Actor中处理游戏逻辑和状态

```csharp
// ❌ 错误：Actor处理游戏逻辑
public class EnemyActor : ActorModel
{
    public float Health { get; set; }  // Actor不应该有游戏属性！
    
    public void TakeDamage(float damage)  // Actor不应该处理游戏逻辑！
    {
        Health -= damage;
        if (Health <= 0)
        {
            Die();
        }
    }
    
    public void Attack(Unit target)  // Actor不应该执行游戏行为！
    {
        target.TakeDamage(10);
    }
}
```

**解决方案**：正确的职责分离

```csharp
// ✅ 正确：Entity处理游戏逻辑
#if SERVER
public class EnemyLogic : IGameClass
{
    public static void ProcessEnemyAttack(Unit enemy, Unit target)
    {
        // 游戏逻辑在服务端Entity中
        var damage = enemy.GetProperty<float>(PropertyUnit.AttackDamage);
        target.TakeDamage(damage);
        
        // 创建视觉效果
        CreateAttackEffect(enemy, target);
    }
}
#endif

// ✅ 正确：Actor只做视觉表现
public static void CreateAttackEffect(Unit attacker, Unit target)
{
    var attackEffect = new ActorParticle(attackEffectLink, false, attacker);
    attackEffect.AttachTo(attacker, "weapon_socket");
    
    var hitEffect = new ActorParticle(hitEffectLink, false, target);
    hitEffect.AttachTo(target, "hit_socket");
}
```

### 陷阱4：用Entity做纯视觉效果

**问题描述**：为纯视觉效果创建Entity

```csharp
// ❌ 错误：爆炸特效应该用Actor，不是Entity
#if SERVER
var explosionEntity = new EffectEntity(explosionLink, player, position, facing);
explosionEntity.SetSyncType(SyncType.All);  // 不必要的网络开销！
#endif
```

**问题影响**：
- 不必要的网络开销
- 服务端资源浪费
- 同步延迟影响视觉效果

**解决方案**：使用Actor处理纯视觉效果

```csharp
// ✅ 正确：视觉效果用Actor
var explosionEffect = new ActorParticle(explosionLink, false, transientScope);
explosionEffect.Position = explosionPosition;
// 无网络开销，即时显示，自动清理
```

### 陷阱5：过度使用Entity

**问题描述**：将所有对象都设计为Entity

```csharp
// ❌ 错误：所有对象都用Entity
#if SERVER
var particle = new ParticleEntity(particleLink, player, pos, facing);
var sound = new SoundEntity(soundLink, player, pos, facing);
var decoration = new DecorationEntity(decorationLink, player, pos, facing);
var ui3d = new UI3DEntity(uiLink, player, pos, facing);
#endif
```

**问题影响**：
- 大量不必要的网络流量
- 服务端性能问题
- 客户端延迟显示

**解决方案**：按需选择Entity vs Actor

```csharp
// ✅ 正确：根据需要选择
#if SERVER
// 需要游戏逻辑的用Entity
var tower = towerData.CreateUnit(player, pos, facing);
var hero = heroData.CreateUnit(player, pos, facing);
#endif

// 纯视觉效果用Actor
var particle = new ActorParticle(particleLink, false, transientScope);
var sound = new ActorSound(soundLink, false, transientScope);
var decoration = new ActorModel(decorationLink, false, sceneScope);
var ui3d = new ActorModel(uiLink, false, uiScope);
```

### 陷阱6：忽略作用域管理

**问题描述**：不使用作用域管理Actor生命周期

```csharp
// ❌ 错误：没有作用域管理
var effect1 = new ActorParticle(effectLink, false, null);
var effect2 = new ActorParticle(effectLink, false, null);
var effect3 = new ActorParticle(effectLink, false, null);
// 生命周期难以管理，容易造成内存泄漏
```

**解决方案**：使用作用域统一管理

```csharp
// ✅ 正确：使用作用域管理
var skillScope = new ActorScopeTransient(skillContext);

var effect1 = new ActorParticle(effectLink, false, skillScope);
var effect2 = new ActorParticle(effectLink, false, skillScope);
var effect3 = new ActorParticle(effectLink, false, skillScope);
// 技能结束时，所有相关Actor自动清理
```

### 陷阱7：同步不需要同步的Actor

**问题描述**：尝试手动同步Actor

```csharp
// ❌ 错误：Actor不需要也不支持同步
var actor = new ActorModel(modelLink, false, scope);
actor.SetSyncType(SyncType.All);  // 方法不存在！
NetworkManager.SendActor(actor);  // 错误的做法！
```

**解决方案**：理解Actor的客户端本地特性

```csharp
// ✅ 正确：Actor是客户端本地的
var actor = new ActorModel(modelLink, false, scope);
// Actor在各个客户端独立创建，不需要同步
// 如果需要同步，应该通过Entity的状态变化来触发Actor的创建
```

## ✨ ActorParticle 屏外优化陷阱

### 陷阱1：一次性粒子在屏外被立即销毁

**问题描述**：同时具有 `KillOnFinish` 和 `ForceOneShot` 标志的粒子表现在屏外创建或更新时被立即销毁

```csharp
// ❌ 容易遇到的问题：一次性粒子表现刚创建就失效
var explosionEffect = new GameDataActorParticle(explosionLink)
{
    AutoPlay = true,
    KillOnFinish = true,     // 播放完成后自动销毁
    ForceOneShot = true,     // 强制一次性播放
    CreationFilterLevel = 1  // 默认过滤等级
};

// 当在屏外一定距离尝试播放时，可能立即被销毁
var actor = explosionEffect.CreateActor(transientScope, false, scene);
if (actor == null)
{
    // 粒子因屏外优化被过滤，没有创建
    Game.Logger.LogWarning("爆炸特效在屏外被过滤，无法显示");
}
```

**产生原因**：
- 框架存在屏外优化机制，通过 `CreationFilterLevel` 进行距离检查
- 当 `CreationFilterLevel < Actor.CreationFilterLevel` 时，粒子Actor不会被创建
- 瞬态粒子（`IsTransient => ForceOneShot && KillOnFinish`）在创建后第二帧开始更新时，如果仍处于屏外一定距离也可能被立即销毁

**常见场景**：
1. 远距离爆炸特效
2. 屏幕边缘的技能特效
3. 玩家视野外的环境粒子
4. 小地图标记对应的3D特效

**解决方案1**：提高创建过滤等级

```csharp
// ✅ 方案1：提高CreationFilterLevel确保重要特效显示
var importantExplosion = new GameDataActorParticle(explosionLink)
{
    AutoPlay = true,
    KillOnFinish = true,
    ForceOneShot = true,
    CreationFilterLevel = 3  // 提高过滤等级，减少被屏外优化的可能性
};
```

**解决方案2**：使用持续性粒子配置

```csharp
// ✅ 方案2：对于重要特效，考虑使用非瞬态配置
var persistentExplosion = new GameDataActorParticle(explosionLink)
{
    AutoPlay = true,
    KillOnFinish = true,     // 播放完成后销毁
    ForceOneShot = false,    // 不强制一次性，避免瞬态标记
    CreationFilterLevel = 1
};
```

**解决方案3**：检查创建结果并提供反馈

```csharp
// ✅ 方案3：检查Actor创建结果，提供适当的反馈或替代方案
public static void CreateExplosionEffect(ScenePoint position, IActorScope scope)
{
    var explosionActor = explosionData.CreateActor(scope, false);
    
    if (explosionActor == null)
    {
        // 粒子被屏外优化过滤，考虑替代方案
        Game.Logger.LogDebug("爆炸特效在屏外被过滤，位置: {Position}", position);
        
        // 可选：播放音效作为替代反馈
        var soundActor = explosionSoundData.CreateActor(scope, false);
        soundActor?.SetPosition(position);
        
        // 可选：显示简化的UI提示
        ShowExplosionIndicator(position);
    }
    else
    {
        explosionActor.Position = position;
    }
}
```

### 陷阱2：误解屏外优化机制

**问题描述**：不理解屏外优化的工作原理，导致调试困难

```csharp
// ❌ 错误理解：认为所有粒子都会显示
public void DebugEffect(ScenePoint position)
{
    var testEffect = testParticleData.CreateActor(debugScope, false);
    if (testEffect == null)
    {
        Game.Logger.LogError("特效创建失败！数据有问题？");  // 错误的判断
    }
}
```

**正确理解**：
- 屏外优化是性能优化手段，防止创建过多不可见的粒子
- `Actor.CreationFilterLevel` 是全局设置，影响所有Actor的创建
- 粒子的 `CreationFilterLevel` 与全局设置比较决定是否创建

**解决方案**：正确的调试方法

```csharp
// ✅ 正确：理解屏外优化的调试方法
public void DebugEffect(ScenePoint position)
{
    Game.Logger.LogDebug("当前全局CreationFilterLevel: {Level}", Actor.CreationFilterLevel);
    Game.Logger.LogDebug("粒子CreationFilterLevel: {Level}", testParticleData.CreationFilterLevel);
    
    var testEffect = testParticleData.CreateActor(debugScope, false);
    if (testEffect == null)
    {
        Game.Logger.LogDebug("特效因屏外优化被过滤，位置: {Position}", position);
        
        // 临时提高过滤等级进行测试
        var originalLevel = Actor.CreationFilterLevel;
        Actor.CreationFilterLevel = 0;  // 禁用过滤
        
        var forceEffect = testParticleData.CreateActor(debugScope, false);
        if (forceEffect != null)
        {
            Game.Logger.LogDebug("禁用屏外优化后特效创建成功");
            forceEffect.Position = position;
        }
        
        Actor.CreationFilterLevel = originalLevel;  // 恢复设置
    }
    else
    {
        testEffect.Position = position;
        Game.Logger.LogDebug("特效创建成功");
    }
}
```

### 最佳实践

**✅ 推荐做法**：
1. **重要特效提高过滤等级** - 对于游戏性关键的特效，适当提高 `CreationFilterLevel`
2. **合理配置瞬态标志** - 只有真正的一次性短暂特效才同时使用 `KillOnFinish` 和 `ForceOneShot`
3. **提供替代反馈** - 当视觉特效被过滤时，考虑音效或UI反馈
4. **调试时禁用优化** - 开发调试时可以临时设置 `Actor.CreationFilterLevel = 0`

**❌ 避免的做法**：
1. **忽视屏外优化** - 不了解优化机制，遇到问题时误判原因
2. **过度提高过滤等级** - 所有特效都设置高过滤等级会影响性能优化效果
3. **依赖屏外特效** - 不应该依赖玩家看不到的特效来提供重要反馈

> 💡 **记住**：屏外优化是正常的性能优化机制，不是bug。理解并合理配置 `CreationFilterLevel` 可以在性能和视觉效果之间找到平衡。

## 🔄 序列化问题

### 陷阱1：使用二维数组

**问题描述**：使用多维数组作为网络消息的属性

```csharp
// ❌ 错误：会导致序列化异常
public class GameStateMessage
{
    public PieceType[,] Board { get; set; }  // SerializeTypeInstanceNotSupported
}
```

**错误信息**：
```
SerializeTypeInstanceNotSupported, GameEntry.MyGame.PieceType[,]
```

**解决方案**：使用一维数组 + 辅助方法

```csharp
// ✅ 正确：使用一维数组
public class GameStateMessage
{
    public PieceType[] Board { get; set; }
    public int BoardWidth { get; set; }
    public int BoardHeight { get; set; }
    
    // 提供便捷的访问方法
    public PieceType GetPiece(int row, int col)
    {
        return Board[row * BoardWidth + col];
    }
    
    public void SetPiece(int row, int col, PieceType piece)
    {
        Board[row * BoardWidth + col] = piece;
    }
}
```

### 陷阱2：复杂的嵌套数据结构

**问题描述**：使用过于复杂的嵌套泛型

```csharp
// ❌ 错误：序列化性能差且可能失败
public Dictionary<string, Dictionary<int, List<PlayerData>>> ComplexPlayerData { get; set; }
```

**解决方案**：简化数据结构

```csharp
// ✅ 正确：使用简单的扁平结构
public class PlayerDataCollection
{
    public List<PlayerEntry> Players { get; set; } = new();
}

public class PlayerEntry
{
    public string GroupName { get; set; }
    public int PlayerId { get; set; }
    public PlayerData Data { get; set; }
}
```

## 👥 玩家系统陷阱

### 陷阱3：硬编码玩家ID

**问题描述**：在客户端硬编码本地玩家ID

```csharp
// ❌ 错误：所有客户端都会显示为同一个玩家
public class GameClient
{
    private int _localPlayerId = 1;  // 硬编码
    
    public void ShowCurrentPlayer()
    {
        Game.Logger.LogInformation("当前玩家: {PlayerId}", _localPlayerId);
    }
}
```

**现象**：多个客户端都显示自己是玩家1

**解决方案**：动态获取本地玩家ID

```csharp
// ✅ 正确：动态获取真实的本地玩家ID
public class GameClient
{
    private int LocalPlayerId => Player.LocalPlayer.Id;
    
    public void ShowCurrentPlayer()
    {
        Game.Logger.LogInformation("当前玩家: {PlayerId}", LocalPlayerId);
    }
    
    public bool IsMyTurn(int activePlayerId)
    {
        return activePlayerId == LocalPlayerId;
    }
}
```

### 陷阱4：忽略玩家连接事件

**问题描述**：没有监听玩家连接/断开事件

```csharp
// ❌ 错误：无法检测到玩家状态变化
public class GameLobby
{
    private List<Player> _players = new();
    
    public void UpdatePlayerList()
    {
        // 怎么知道玩家何时连接？
    }
}
```

**解决方案**：正确监听玩家事件

```csharp
// ✅ 正确：监听玩家连接事件
public class GameLobby : IGameClass
{
    private static readonly List<Player> _connectedPlayers = new();
    
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }
    
    private static void OnGameTriggerInitialization()
    {
        // 监听玩家连接
        var connectTrigger = new Trigger<EventPlayerUserConnected>(OnPlayerConnected);
        connectTrigger.Register(Game.Instance);
        
        // 监听玩家断开
        var disconnectTrigger = new Trigger<EventPlayerUserDisconnected>(OnPlayerDisconnected);
        disconnectTrigger.Register(Game.Instance);
    }
    
    private static void OnPlayerConnected(EventPlayerUserConnected e)
    {
        _connectedPlayers.Add(e.Player);
        Game.Logger.LogInformation("玩家加入: {User}", e.User);
    }
    
    private static void OnPlayerDisconnected(EventPlayerUserDisconnected e)
    {
        _connectedPlayers.Remove(e.Player);
        Game.Logger.LogInformation("玩家离开: {User}", e.User);
    }
}
```

## ⏰ 异步编程错误

### 陷阱5：使用 Task.Delay 而不是 Game.Delay

**问题描述**：在 WebAssembly 环境中使用标准的 Task.Delay

```csharp
// ❌ 错误：在 Wasm 中可能导致时间不同步
public async Task DelayedAction()
{
    await Task.Delay(1000);  // 独立于游戏时间
    DoSomething();
}
```

**解决方案**：使用框架的 Game.Delay

```csharp
// ✅ 正确：与游戏时间同步的延迟
public async Task DelayedAction()
{
    await Game.Delay(TimeSpan.FromSeconds(1));  // 与游戏时间同步
    DoSomething();
}
```

### 陷阱6：长时间异步操作不检查游戏状态

**问题描述**：在长时间循环中不检查游戏状态

```csharp
// ❌ 错误：游戏结束后仍在运行
public async Task LongRunningTask()
{
    for (int i = 0; i < 1000; i++)
    {
        DoSomething();
        await Game.Delay(TimeSpan.FromSeconds(0.1));
    }
}
```

**解决方案**：定期检查游戏状态

```csharp
// ✅ 正确：检查游戏状态
public async Task LongRunningTask()
{
    for (int i = 0; i < 1000; i++)
    {
        if (!Game.IsRunning)
            break;
            
        DoSomething();
        await Game.Delay(TimeSpan.FromSeconds(0.1));
    }
}
```

## 🎯 事件系统误用

### 陷阱7：在错误的初始化阶段注册事件

**问题描述**：在 OnGameDataInitialization 中注册游戏事件

```csharp
// ❌ 错误：在数据初始化阶段注册事件
public static void OnRegisterGameClass()
{
    Game.OnGameDataInitialization += () =>
    {
        // 这里不应该注册游戏事件
        var trigger = new Trigger<EventGameStart>(OnGameStart);
        trigger.Register(Game.Instance);  // Game.Instance 可能还未创建
    };
}
```

**解决方案**：在正确的阶段注册事件

```csharp
// ✅ 正确：在触发器初始化阶段注册事件
public static void OnRegisterGameClass()
{
    Game.OnGameDataInitialization += OnGameDataInitialization;
    Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
}

private static void OnGameDataInitialization()
{
    // 在这里初始化数据
    CreateGameData();
}

private static void OnGameTriggerInitialization()
{
    // 在这里注册事件和触发器
    var trigger = new Trigger<EventGameStart>(OnGameStart);
    trigger.Register(Game.Instance);
}
```

### 陷阱8：忘记检查游戏模式

**问题描述**：在所有游戏模式下都注册事件

```csharp
// ❌ 错误：会在所有游戏模式下注册
private static void OnGameTriggerInitialization()
{
    var trigger = new Trigger<EventGameStart>(OnGameStart);
    trigger.Register(Game.Instance);
}
```

**解决方案**：检查游戏模式

```csharp
// ✅ 正确：只在特定游戏模式下注册
private static void OnGameTriggerInitialization()
{
    if (Game.GameModeLink != ScopeData.GameMode.MyGameMode)
    {
        return;  // 不是我们的游戏模式，不注册
    }
    
    var trigger = new Trigger<EventGameStart>(OnGameStart);
    trigger.Register(Game.Instance);
}
```

## 🔧 性能问题

### 陷阱9：在游戏循环中频繁分配对象

**问题描述**：在 Update 方法中创建临时对象

```csharp
// ❌ 错误：每帧都创建新对象
public void Update()
{
    var position = new Vector3(x, y, z);  // 每帧分配
    var direction = new Vector3(dx, dy, dz);  // 每帧分配
    
    DoSomething(position, direction);
}
```

**解决方案**：重用对象或使用值类型

```csharp
// ✅ 正确：重用对象
public class MyComponent
{
    private Vector3 _tempPosition = new();
    private Vector3 _tempDirection = new();
    
    public void Update()
    {
        _tempPosition.Set(x, y, z);
        _tempDirection.Set(dx, dy, dz);
        
        DoSomething(_tempPosition, _tempDirection);
    }
}
```

### 陷阱10：过度使用日志

**问题描述**：在性能敏感的代码中大量使用日志

```csharp
// ❌ 错误：循环中的大量日志
public void ProcessItems(List<Item> items)
{
    foreach (var item in items)
    {
        Game.Logger.LogDebug("处理项目: {ItemName}", item.Name);
        ProcessItem(item);
    }
}
```

**解决方案**：使用条件日志或汇总日志

```csharp
// ✅ 正确：汇总日志
public void ProcessItems(List<Item> items)
{
    var processedCount = 0;
    
    foreach (var item in items)
    {
        ProcessItem(item);
        processedCount++;
    }
    
    Game.Logger.LogInformation("批量处理完成，共处理 {Count} 个项目", processedCount);
}
```

## 🌐 网络同步陷阱

### 陷阱11：客户端直接修改游戏状态

**问题描述**：客户端直接修改游戏状态而不通过服务器

```csharp
// ❌ 错误：客户端直接修改状态
public void OnStartButtonClicked()
{
    gameState.IsGameStarted = true;  // 直接修改本地状态
    gameState.CurrentPlayer = 1;
}
```

**解决方案**：发送请求给服务器

```csharp
// ✅ 正确：发送请求给服务器
public void OnStartButtonClicked()
{
    var message = new StartGameMessage();
    NetworkManager.SendMessage(message);  // 让服务器处理
}
```

### 陷阱12：忽略网络延迟

**问题描述**：假设网络操作是即时的

```csharp
// ❌ 错误：假设立即收到响应
public void MakeMove(int row, int col)
{
    SendMoveMessage(row, col);
    // 立即更新UI，可能与服务器状态不一致
    UpdateUI();
}
```

**解决方案**：使用乐观更新或等待确认

```csharp
// ✅ 正确：乐观更新 + 回滚机制
public void MakeMove(int row, int col)
{
    // 乐观更新（立即反馈）
    var oldState = gameState.Clone();
    gameState.ApplyMove(row, col);
    UpdateUI();
    
    // 发送到服务器
    SendMoveMessage(row, col, (success) =>
    {
        if (!success)
        {
            // 回滚状态
            gameState = oldState;
            UpdateUI();
            ShowError("移动失败");
        }
    });
}
```

## 💡 检查清单

在开发过程中，使用以下检查清单避免常见陷阱：

### 🔍 数据结构检查
- [ ] 是否使用了多维数组？
- [ ] 数据结构是否过于复杂？
- [ ] 是否存在循环引用？

### 👥 玩家系统检查
- [ ] 是否硬编码了玩家ID？
- [ ] 是否监听了玩家连接事件？
- [ ] 是否检查了游戏模式？

### ⏰ 异步编程检查
- [ ] 是否使用了 `Game.Delay` 而不是 `Task.Delay`？
- [ ] 长时间异步操作是否检查游戏状态？
- [ ] 是否有适当的错误处理？

### 🎯 事件系统检查
- [ ] 事件是否在正确的初始化阶段注册？
- [ ] 是否检查了游戏模式？
- [ ] 是否有内存泄漏的风险？

### 🚀 性能检查
- [ ] 是否在游戏循环中频繁分配对象？
- [ ] 日志使用是否合理？
- [ ] 是否有不必要的重复计算？

### 🌐 网络同步检查
- [ ] 客户端是否直接修改游戏状态？
- [ ] 是否考虑了网络延迟？
- [ ] 是否有冲突解决机制？

---

> 💡 **提示**: 当遇到问题时，首先检查是否属于这些常见陷阱。大多数问题都可以通过遵循框架约定和最佳实践来避免。 