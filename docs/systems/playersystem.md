# 👥 玩家系统 (Player System)

WasiCore 框架内置了强大的玩家系统，用于管理游戏中的玩家连接、状态和数据。本文档详细介绍了玩家系统的使用方法和最佳实践。

## 📋 目录

- [🎯 核心概念](#核心概念)
  - [Player vs User：关键概念区分](#player-vs-user关键概念区分)
- [🔧 基本用法](#基本用法)
- [🌐 网络玩家管理](#网络玩家管理)
- [🎮 游戏中的玩家](#游戏中的玩家)
- [💡 最佳实践](#最佳实践)
- [🚨 常见陷阱](#常见陷阱)
- [🔧 调试技巧](#调试技巧)
  - [Player-User关系调试](#player-user关系调试)
- [📖 总结与重点提醒](#总结与重点提醒)

## 🎯 核心概念

### Player 对象

`Player` 是框架中表示玩家的核心类，包含了玩家的基本信息和状态。

```csharp
public class Player
{
    public int Id { get; }              // 唯一玩家ID
    public string Name { get; set; }    // 玩家名称
    public bool IsConnected { get; }    // 连接状态
    public bool IsLocal { get; }        // 是否为本地玩家
    // ... 其他属性
}
```

### 本地玩家 vs 远程玩家

- **本地玩家 (Local Player)**：当前客户端控制的玩家
- **远程玩家 (Remote Player)**：其他客户端的玩家

### ⚠️ Player vs User：关键概念区分

这是WasiCore框架中**最重要**的概念区分之一，理解不当会导致严重的数据错误。

#### 基本概念对比

| 概念 | 类型 | 生命周期 | 唯一性 | 用途 |
|------|------|---------|--------|------|
| **Player.Id** | `int` | 当局游戏会话 | 会话内唯一 | 游戏内逻辑标识 |
| **User.UserId** | `long` | 用户账户生命周期 | 全局唯一 | 持久数据标识 |

#### 详细说明

**Player（玩家槽位）:**
- Player.Id 是当前游戏会话中的**槽位编号**（0, 1, 2, 3...）
- 每次游戏开始时重新分配，同一用户在不同会话中可能有不同的 Player.Id
- 用于游戏内的即时逻辑：回合制、团队分配、本地玩家识别等
- 生命周期：游戏开始 → 游戏结束

**User（真实用户）:**
- User.UserId 是平台用户的**全局唯一标识**
- 跨所有游戏会话保持不变，是用户的永久身份证明
- 用于持久数据存储：经验值、等级、背包、好友列表等
- 生命周期：用户注册 → 账户删除

#### 关系图解

```
游戏会话1:  Player.Id=0 ←→ User.UserId=10001 (张三)
          Player.Id=1 ←→ User.UserId=10002 (李四)

游戏会话2:  Player.Id=0 ←→ User.UserId=10002 (李四)  ← 注意：同一用户不同Player.Id
          Player.Id=1 ←→ User.UserId=10003 (王五)
          Player.Id=2 ←→ User.UserId=10001 (张三)  ← 注意：同一用户不同Player.Id
```

#### 获取User信息的正确方式

```csharp
#if SERVER
// ✅ 正确：从Player获取对应的User信息
public static User GetUserFromPlayer(Player player)
{
    if (player.SlotController is PlayerController controller)
    {
        return controller.User;  // 包含 UserId 和 Name
    }
    return null;  // AI玩家没有对应的User
}

// ✅ 正确：获取UserId用于云数据操作
public static long? GetUserIdFromPlayer(Player player)
{
    if (player.SlotController is PlayerController controller)
    {
        return controller.User.UserId;
    }
    return null;  // AI玩家返回null
}

// ✅ 正确：检查是否为真人玩家
public static bool IsHumanPlayer(Player player)
{
    return player.SlotController is PlayerController;
}
#endif
```

#### 使用场景对比

| 场景 | 使用Player.Id | 使用User.UserId |
|------|--------------|----------------|
| 回合制游戏逻辑 | ✅ 判断当前是谁的回合 | ❌ |
| 团队分配 | ✅ 游戏内临时团队 | ❌ |
| 本地玩家识别 | ✅ `Player.LocalPlayer.Id` | ❌ |
| 经验值存储 | ❌ | ✅ 跨会话持久化 |
| 背包数据 | ❌ | ✅ 跨会话持久化 |
| 好友系统 | ❌ | ✅ 跨会话持久化 |
| 成就系统 | ❌ | ✅ 跨会话持久化 |
| 排行榜 | ❌ | ✅ 跨会话持久化 |

#### 常见错误示例

```csharp
// ❌ 严重错误：用Player.Id存储持久数据
public async Task SavePlayerProgress(Player player, int level)
{
    // 这是错误的！Player.Id不是持久标识
    await CloudData.ForUser(player.Id)  // ← 错误！
        .SetData("level", level)
        .ExecuteAsync();
}

// ✅ 正确：用User.UserId存储持久数据  
public async Task SavePlayerProgress(Player player, int level)
{
    // 方式1：使用便利方法（推荐）
    await CloudData.ForPlayer(player)
        .SetData("level", level)
        .ExecuteAsync();
        
    // 方式2：手动获取UserId
    if (player.SlotController is PlayerController controller)
    {
        await CloudData.ForUser(controller.User.UserId)
            .SetData("level", level)
            .ExecuteAsync();
    }
}
```

#### AI玩家注意事项

AI玩家只有Player.Id，没有对应的User：

```csharp
public void HandlePlayerAction(Player player, string action)
{
    if (IsHumanPlayer(player))
    {
        // 真人玩家：可以访问云数据
        var userId = GetUserIdFromPlayer(player);
        // 记录行为数据...
    }
    else
    {
        // AI玩家：只做游戏逻辑，不访问云数据
        Game.Logger.LogInformation("AI玩家 {PlayerId} 执行了 {Action}", 
            player.Id, action);
    }
}
```

#### 最佳实践总结

1. **游戏逻辑**：使用 `Player.Id` 和 `Player` 对象
2. **数据存储**：使用 `User.UserId` 或 `CloudData.ForPlayer()` 便利方法
3. **类型检查**：始终检查是否为真人玩家后再访问云数据
4. **文档说明**：在代码中明确注释使用Player还是User的原因

```csharp
// 示例：清晰的代码注释
public class GameManager
{
    // 使用Player.Id - 游戏内逻辑
    private int _currentTurnPlayerId;
    
    // 使用User.UserId - 持久数据
    private async Task SaveGameResult(Player winner)
    {
        // 只有真人玩家才记录胜利数据
        if (IsHumanPlayer(winner))
        {
            await CloudData.ForPlayer(winner)  // 自动转换为UserId
                .AddToData("wins", 1)
                .ExecuteAsync();
        }
    }
}
```

## 🔧 基本用法

### 💡 显示玩家信息的最佳实践

**重要提示**：框架中 `Player` 类没有 `Name` 属性，用户名称存储在 `User` 类中。

| 场景 | 推荐用法 | 输出示例 |
|------|---------|---------|
| **日志输出玩家信息** | `{Player}` | `Player 1 [Team: 0]` |
| **日志输出用户信息** | `{User}` | `UserName [User 12345]` |
| **需要用户名字符串** | `User?.Name` | `"UserName"` |
| **按名称查找/比较** | `User?.Name == "..."` | 字符串比较 |

```csharp
// ✅ 推荐：日志输出直接使用对象的 ToString()
Game.Logger.LogInformation("玩家: {Player}", player);
Game.Logger.LogInformation("用户: {User}", player.User);

// ✅ 正确：需要字符串类型时使用 User?.Name
var message = new ChatMessage
{
    SenderName = Player.LocalPlayer.User?.Name,  // 字段类型是 string
    Content = "Hello"
};

// ✅ 正确：字符串比较时使用 User?.Name
var targetPlayer = players.FirstOrDefault(p => p.User?.Name == "目标用户名");

// ❌ 错误：Player 没有 Name 属性
var name = player.Name;  // 编译错误！
```

### 获取本地玩家

```csharp
// ✅ 正确：获取本地玩家
var localPlayer = Player.LocalPlayer;

// 直接使用 Player 对象的 ToString()
Game.Logger.LogInformation("本地玩家: {Player}", localPlayer);

// 或者显示用户信息（包含用户名）
Game.Logger.LogInformation("本地用户: {User}", localPlayer.User);
```

### 获取所有玩家

```csharp
// 获取游戏中所有玩家
var allPlayers = Game.Instance.GetPlayers();

foreach (var player in allPlayers)
{
    // 直接使用对象的 ToString()
    Game.Logger.LogInformation("玩家: {Player}, 用户: {User}, 连接状态: {IsConnected}", 
        player, player.User, player.IsConnected);
}
```

### 按条件筛选玩家

```csharp
// 获取所有在线玩家
var onlinePlayers = Game.Instance.GetPlayers()
    .Where(p => p.IsConnected)
    .ToList();

// 获取远程玩家
var remotePlayers = Game.Instance.GetPlayers()
    .Where(p => !p.IsLocal)
    .ToList();

// 按名称查找玩家
var playerByName = Game.Instance.GetPlayers()
    .FirstOrDefault(p => p.User?.Name == "目标玩家名");
```

## 🌐 网络玩家管理

### 监听玩家连接事件

```csharp
public class PlayerManager : IGameClass
{
    private static readonly List<Player> _connectedPlayers = new();
    
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }
    
    private static void OnGameTriggerInitialization()
    {
        // 监听玩家连接事件
        var connectTrigger = new Trigger<EventPlayerUserConnected>(OnPlayerConnected);
        connectTrigger.Register(Game.Instance);
        
        // 监听玩家断开事件
        var disconnectTrigger = new Trigger<EventPlayerUserDisconnected>(OnPlayerDisconnected);
        disconnectTrigger.Register(Game.Instance);
    }
    
    private static void OnPlayerConnected(EventPlayerUserConnected e)
    {
        _connectedPlayers.Add(e.Player);
        Game.Logger.LogInformation("🎮 玩家 {User} 已连接", e.User);
            
        // 通知其他系统
        OnPlayerCountChanged();
    }
    
    private static void OnPlayerDisconnected(EventPlayerUserDisconnected e)
    {
        _connectedPlayers.Remove(e.Player);
        Game.Logger.LogInformation("🚪 玩家 {User} 已断开", e.User);
            
        // 通知其他系统
        OnPlayerCountChanged();
    }
    
    private static void OnPlayerCountChanged()
    {
        Game.Logger.LogInformation("当前在线玩家数: {PlayerCount}", _connectedPlayers.Count);
        
        // 可以在这里触发相关的游戏逻辑
        // 例如：自动开始游戏、暂停游戏等
    }
    
    public static List<Player> GetConnectedPlayers()
    {
        return _connectedPlayers.ToList();
    }
}
```

### 玩家状态同步

```csharp
public class PlayerStateManager
{
    public static void BroadcastPlayerState(Player player, object stateData)
    {
        var message = new PlayerStateMessage
        {
            PlayerId = player.Id,
            StateData = stateData,
            Timestamp = DateTime.UtcNow
        };
        
        // 广播给所有其他玩家
        NetworkManager.BroadcastMessage(message);
    }
    
    public static void SendToPlayer(Player targetPlayer, object data)
    {
        var message = new PlayerTargetedMessage
        {
            TargetPlayerId = targetPlayer.Id,
            Data = data
        };
        
        NetworkManager.SendMessage(message);
    }
}
```

## 🎮 游戏中的玩家

### 回合制游戏示例

```csharp
public class TurnBasedGameManager : IGameClass
{
    private static int _currentPlayerId = 1;
    private static readonly List<Player> _gamePlayers = new();
    
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }
    
    private static void OnGameTriggerInitialization()
    {
        if (Game.GameModeLink != ScopeData.GameMode.TurnBasedGame)
            return;
            
        // 监听游戏开始事件
        var gameStartTrigger = new Trigger<EventGameStart>(OnGameStart);
        gameStartTrigger.Register(Game.Instance);
    }
    
    private static void OnGameStart(EventGameStart e)
    {
        // 初始化游戏玩家列表
        _gamePlayers.Clear();
        _gamePlayers.AddRange(PlayerManager.GetConnectedPlayers());
        
        // 随机选择起始玩家
        _currentPlayerId = _gamePlayers[Random.Range(0, _gamePlayers.Count)].Id;
        
        Game.Logger.LogInformation("🎯 游戏开始！当前玩家: {PlayerId}", _currentPlayerId);
        
        // 通知所有客户端
        BroadcastCurrentPlayer();
    }
    
    public static void NextTurn()
    {
        var currentIndex = _gamePlayers.FindIndex(p => p.Id == _currentPlayerId);
        var nextIndex = (currentIndex + 1) % _gamePlayers.Count;
        _currentPlayerId = _gamePlayers[nextIndex].Id;
        
        Game.Logger.LogInformation("🔄 轮到玩家: {PlayerId}", _currentPlayerId);
        
        BroadcastCurrentPlayer();
    }
    
    private static void BroadcastCurrentPlayer()
    {
        var message = new CurrentPlayerMessage
        {
            CurrentPlayerId = _currentPlayerId
        };
        
        NetworkManager.BroadcastMessage(message);
    }
    
    public static bool IsMyTurn()
    {
        return _currentPlayerId == Player.LocalPlayer.Id;
    }
    
    public static bool IsPlayerTurn(Player player)
    {
        return _currentPlayerId == player.Id;
    }
}
```

### 团队游戏示例

```csharp
public class TeamGameManager : IGameClass
{
    private static readonly Dictionary<int, int> _playerTeams = new();
    
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }
    
    private static void OnGameTriggerInitialization()
    {
        if (Game.GameModeLink != ScopeData.GameMode.TeamGame)
            return;
            
        var trigger = new Trigger<EventPlayerUserConnected>(OnPlayerConnected);
        trigger.Register(Game.Instance);
    }
    
    private static void OnPlayerConnected(EventPlayerUserConnected e)
    {
        // 自动分配队伍
        var teamId = AssignTeam(e.Player);
        _playerTeams[e.Player.Id] = teamId;
        
        Game.Logger.LogInformation("🏆 玩家 {User} 加入队伍 {TeamId}", 
            e.User, teamId);
    }
    
    private static int AssignTeam(Player player)
    {
        // 简单的均衡分配算法
        var team1Count = _playerTeams.Values.Count(t => t == 1);
        var team2Count = _playerTeams.Values.Count(t => t == 2);
        
        return team1Count <= team2Count ? 1 : 2;
    }
    
    public static int GetPlayerTeam(Player player)
    {
        return _playerTeams.GetValueOrDefault(player.Id, 0);
    }
    
    public static List<Player> GetTeamPlayers(int teamId)
    {
        return Game.Instance.GetPlayers()
            .Where(p => GetPlayerTeam(p) == teamId)
            .ToList();
    }
    
    public static bool AreTeammates(Player player1, Player player2)
    {
        var team1 = GetPlayerTeam(player1);
        var team2 = GetPlayerTeam(player2);
        return team1 > 0 && team1 == team2;
    }
}
```

## 💡 最佳实践

### 1. 使用动态玩家ID

```csharp
// ✅ 正确：动态获取
private int LocalPlayerId => Player.LocalPlayer.Id;

// ❌ 错误：硬编码
private int _localPlayerId = 1;
```

### 2. 监听玩家状态变化

```csharp
// ✅ 正确：监听连接事件
var connectTrigger = new Trigger<EventPlayerUserConnected>(OnPlayerConnected);
connectTrigger.Register(Game.Instance);

// ❌ 错误：轮询检查
// 不要在 Update 中反复检查玩家状态
```

### 3. 处理玩家断开连接

```csharp
private static void OnPlayerDisconnected(EventPlayerUserDisconnected e)
{
    // 从游戏中移除玩家
    RemovePlayerFromGame(e.Player);
    
    // 检查是否需要暂停游戏
    if (GetConnectedPlayers().Count < MinPlayersRequired)
    {
        PauseGame("玩家数量不足");
    }
    
    // 重新平衡队伍
    RebalanceTeams();
}
```

### 4. 安全的玩家数据访问

```csharp
public static Player GetPlayerById(int playerId)
{
    return Game.Instance.GetPlayers()
        .FirstOrDefault(p => p.Id == playerId);
}

public static bool IsValidPlayer(int playerId)
{
    return GetPlayerById(playerId) != null;
}
```

### 5. 玩家权限管理

```csharp
public static bool CanPlayerPerformAction(Player player, string action)
{
    if (!player.IsConnected)
        return false;
        
    // 检查玩家权限
    switch (action)
    {
        case "StartGame":
            return player.IsHost;
        case "MakeMove":
            return TurnBasedGameManager.IsPlayerTurn(player);
        default:
            return true;
    }
}
```

## 🚨 常见陷阱

### ❌ 陷阱1：混淆Player.Id和User.UserId（最严重）

这是**最危险**的陷阱，会导致数据丢失和用户投诉：

```csharp
// ❌ 极其危险：用Player.Id作为云数据的用户标识
public async Task SavePlayerData(Player player, PlayerData data)
{
    // 这会导致严重的数据混乱！
    await CloudData.ForUser(player.Id)  // Player.Id是临时的！
        .SetData("level", data.Level)
        .SetData("experience", data.Experience)
        .ExecuteAsync();
}

// 问题：
// 1. 用户A在游戏1中是Player.Id=0，数据存储到UserId=0
// 2. 用户A在游戏2中是Player.Id=2，数据存储到UserId=2  
// 3. 用户A的数据被分散存储，永远无法正确读取！

// ✅ 正确：使用便利方法或正确获取UserId
public async Task SavePlayerData(Player player, PlayerData data)
{
    // 方式1：使用便利方法（推荐）
    await CloudData.ForPlayer(player)
        .SetData("level", data.Level)
        .SetData("experience", data.Experience)
        .ExecuteAsync();
        
    // 方式2：手动验证和获取
    if (player.SlotController is PlayerController controller)
    {
        await CloudData.ForUser(controller.User.UserId)
            .SetData("level", data.Level)
            .SetData("experience", data.Experience)
            .ExecuteAsync();
    }
}
```

### ❌ 陷阱2：硬编码玩家ID

```csharp
// 错误：所有客户端都会是玩家1
private int _myPlayerId = 1;
```

### ❌ 陷阱3：不监听玩家事件

```csharp
// 错误：无法知道玩家何时连接/断开
public void UpdatePlayerCount()
{
    // 怎么知道玩家数量变化了？
}
```

### ❌ 陷阱4：忽略玩家断开连接

```csharp
// 错误：玩家断开后仍然在游戏列表中
public void ProcessPlayerTurn(int playerId)
{
    var player = GetPlayerById(playerId);
    // 没有检查玩家是否仍然连接
    player.DoSomething();  // 可能导致空引用异常
}
```

### ❌ 陷阱5：在错误的时机获取玩家

```csharp
// 错误：在玩家连接之前获取
public static void OnRegisterGameClass()
{
    Game.OnGameDataInitialization += () =>
    {
        var localPlayer = Player.LocalPlayer;  // 此时可能还没有本地玩家
    };
}
```

### ❌ 陷阱6：对AI玩家进行云数据操作

```csharp
// 错误：AI玩家没有User信息
public async Task RecordPlayerAction(Player player, string action)
{
    // AI玩家会导致异常或错误数据
    await CloudData.ForPlayer(player)  // 如果是AI玩家会抛出异常
        .SetData("last_action", action)
        .ExecuteAsync();
}

// ✅ 正确：检查玩家类型
public async Task RecordPlayerAction(Player player, string action)
{
    // 只为真人玩家记录云数据
    if (IsHumanPlayer(player))
    {
        await CloudData.ForPlayer(player)
            .SetData("last_action", action)
            .ExecuteAsync();
    }
}
```

## 🔧 调试技巧

### 玩家状态日志

```csharp
public static void LogPlayerStates()
{
    var players = Game.Instance.GetPlayers();
    
    Game.Logger.LogInformation("=== 玩家状态报告 ===");
    Game.Logger.LogInformation("总玩家数: {TotalCount}", players.Count);
    Game.Logger.LogInformation("在线玩家数: {OnlineCount}", 
        players.Count(p => p.IsConnected));
    
    foreach (var player in players)
    {
        Game.Logger.LogInformation("玩家 {PlayerId}: {PlayerName} - {Status}", 
            player.Id, player.Name, player.IsConnected ? "在线" : "离线");
    }
}
```

### 玩家事件监控

```csharp
public static void EnablePlayerEventLogging()
{
    var connectTrigger = new Trigger<EventPlayerUserConnected>(e =>
    {
        Game.Logger.LogInformation("🔵 玩家连接: {User}", e.User);
    });
    connectTrigger.Register(Game.Instance);
    
    var disconnectTrigger = new Trigger<EventPlayerUserDisconnected>(e =>
    {
        Game.Logger.LogInformation("🔴 玩家断开: {User}", e.User);
    });
    disconnectTrigger.Register(Game.Instance);
}
```

### Player-User关系调试

```csharp
#if SERVER
/// <summary>
/// 调试Player和User的对应关系
/// </summary>
public static void LogPlayerUserMapping()
{
    Game.Logger.LogInformation("=== Player-User 映射关系 ===");
    
    var allPlayers = Player.AllPlayers;
    foreach (var player in allPlayers)
    {
        if (player.SlotController is PlayerController controller)
        {
            // 真人玩家
            Game.Logger.LogInformation(
                "🧑 Player.Id={PlayerId} ↔ User.UserId={UserId} ({UserName}) [真人玩家]",
                player.Id, 
                controller.User.UserId, 
                controller.User.Name ?? "Unknown"
            );
        }
        else if (player.SlotController is AIController)
        {
            // AI玩家
            Game.Logger.LogInformation(
                "🤖 Player.Id={PlayerId} ↔ 无User [AI玩家]",
                player.Id
            );
        }
        else
        {
            // 其他类型
            Game.Logger.LogInformation(
                "❓ Player.Id={PlayerId} ↔ 未知类型 [{ControllerType}]",
                player.Id,
                player.SlotController?.GetType().Name ?? "null"
            );
        }
    }
}

/// <summary>
/// 验证云数据操作的用户ID正确性
/// </summary>
public static void ValidateCloudDataUsage(Player player, string operation)
{
    if (player.SlotController is PlayerController controller)
    {
        Game.Logger.LogInformation(
            "✅ 云数据操作 '{Operation}': Player.Id={PlayerId} → User.UserId={UserId}",
            operation, player.Id, controller.User.UserId
        );
    }
    else
    {
        Game.Logger.LogWarning(
            "⚠️ 尝试对非真人玩家进行云数据操作 '{Operation}': Player.Id={PlayerId}",
            operation, player.Id
        );
    }
}

/// <summary>
/// 检查潜在的Player.Id误用
/// </summary>
public static void CheckForPlayerIdMisuse()
{
    var allPlayers = Player.AllPlayers;
    var playerIds = allPlayers.Select(p => p.Id).ToArray();
    var userIds = allPlayers
        .Where(p => p.SlotController is PlayerController)
        .Select(p => ((PlayerController)p.SlotController).User.UserId)
        .ToArray();
    
    Game.Logger.LogInformation("=== ID使用检查 ===");
    Game.Logger.LogInformation("Player.Id范围: [{MinPlayerId}-{MaxPlayerId}]", 
        playerIds.DefaultIfEmpty().Min(), playerIds.DefaultIfEmpty().Max());
    Game.Logger.LogInformation("User.UserId范围: [{MinUserId}-{MaxUserId}]", 
        userIds.DefaultIfEmpty().Min(), userIds.DefaultIfEmpty().Max());
    
    // 检查是否有重叠（可能表示误用）
    var overlap = playerIds.Intersect(userIds.Select(id => (int)id)).ToArray();
    if (overlap.Any())
    {
        Game.Logger.LogWarning(
            "⚠️ 发现Player.Id和User.UserId重叠: {OverlapIds} - 请检查是否有误用",
            string.Join(", ", overlap)
        );
    }
    else
    {
        Game.Logger.LogInformation("✅ Player.Id和User.UserId无重叠，使用正确");
    }
}
#endif
```

### 示例：完整的调试工具类

```csharp
#if SERVER
public static class PlayerUserDebugger
{
    public static void EnableComprehensiveLogging()
    {
        // 1. 定期打印映射关系
        Game.RegisterPeriodicAction(TimeSpan.FromMinutes(1), LogPlayerUserMapping);
        
        // 2. 监听玩家事件并记录详细信息
        var connectTrigger = new Trigger<EventPlayerUserConnected>(OnPlayerConnected);
        connectTrigger.Register(Game.Instance);
        
        var disconnectTrigger = new Trigger<EventPlayerUserDisconnected>(OnPlayerDisconnected);
        disconnectTrigger.Register(Game.Instance);
        
        Game.Logger.LogInformation("🔍 Player-User调试日志已启用");
    }
    
    private static void OnPlayerConnected(EventPlayerUserConnected e)
    {
        Game.Logger.LogInformation(
            "🔵 玩家连接详情: Player.Id={PlayerId}, User.UserId={UserId}, UserName='{UserName}'",
            e.Player.Id, e.User.UserId, e.User.Name ?? "Unknown"
        );
        
        // 检查新连接是否导致ID冲突
        CheckForPlayerIdMisuse();
    }
    
    private static void OnPlayerDisconnected(EventPlayerUserDisconnected e)
    {
        Game.Logger.LogInformation(
            "🔴 玩家断开详情: Player.Id={PlayerId}, User.UserId={UserId}, UserName='{UserName}'",
            e.Player.Id, e.User.UserId, e.User.Name ?? "Unknown"
        );
    }
    
    /// <summary>
    /// 模拟常见的错误用法以进行测试
    /// </summary>
    public static void SimulateCommonMistakes()
    {
        var players = Player.AllPlayers.Take(2).ToArray();
        if (players.Length < 2) return;
        
        var player1 = players[0];
        var player2 = players[1];
        
        Game.Logger.LogWarning("🧪 模拟常见错误（仅用于测试）:");
        
        // 错误1：用Player.Id作为用户标识
        Game.Logger.LogError(
            "❌ 错误示例: 将Player.Id={PlayerId}用作云数据的用户标识",
            player1.Id
        );
        
        // 正确方法
        if (player1.SlotController is PlayerController controller1)
        {
            Game.Logger.LogInformation(
                "✅ 正确示例: Player.Id={PlayerId} → User.UserId={UserId}",
                player1.Id, controller1.User.UserId
            );
        }
    }
}
#endif
```

---

## 📖 总结与重点提醒

### 🎯 核心要点

1. **Player.Id** = 临时游戏槽位 → 用于游戏逻辑
2. **User.UserId** = 持久用户标识 → 用于数据存储
3. **AI玩家** = 只有Player，没有User
4. **云数据** = 必须使用UserId或便利方法

### ⚡ 快速检查清单

在编写涉及玩家的代码时，问自己：

- [ ] 我在做游戏逻辑还是数据存储？
- [ ] 如果是数据存储，我用的是UserId吗？
- [ ] 我处理了AI玩家的情况吗？
- [ ] 我用了便利方法`CloudData.ForPlayer()`吗？

### 🚨 危险信号

如果你的代码中有以下模式，**立即检查**：

```csharp
// 🚨 危险信号
CloudData.ForUser(player.Id)           // Player.Id不是UserId！
SaveUserData(player.Id, data)          // 同上！
await database.Save(player.Id, ...)    // 同上！
```

### ✅ 安全模式

```csharp
// ✅ 安全模式
CloudData.ForPlayer(player)            // 自动转换
if (IsHumanPlayer(player)) { ... }     // 检查玩家类型
controller.User.UserId                 // 直接获取UserId
```

---

> 💡 **重要**: Player vs User的概念区分是WasiCore框架中最关键的知识点之一。理解错误会导致严重的数据丢失和用户投诉。在编写任何涉及数据持久化的代码之前，请仔细思考你需要的是临时的游戏标识(Player.Id)还是持久的用户标识(User.UserId)。

> 🔧 **最佳实践**: 始终使用事件驱动的方式监听玩家状态变化，使用CloudData便利方法进行数据操作，并在生产环境中启用Player-User关系的调试日志。 