# 🚀 强类型消息系统 (TypedMessage System)

WasiCore 框架提供了先进的强类型消息系统，它在传统的 `ProtoCustomMessage` 基础上提供了类型安全、自动序列化和智能路由功能。本文档详细介绍了 TypedMessage 系统的使用方法和最佳实践。

## 📋 目录

- [🎯 系统概述](#系统概述)
- [🔧 基本用法](#基本用法)
- [📨 消息发送](#消息发送)
- [📬 消息接收](#消息接收)
- [🎛️ 消息处理器](#消息处理器)
- [🏗️ 流畅构建器API](#流畅构建器api)
- [⚙️ 高级功能](#高级功能)
- [🆚 与ProtoCustomMessage对比](#与protocustommessage对比)
- [💡 最佳实践](#最佳实践)
- [🚨 常见陷阱](#常见陷阱)

## 🎯 系统概述

### 核心特性

- **🔒 类型安全**：编译时类型检查，避免运行时错误
- **🤖 自动序列化**：内置JSON序列化，支持复杂数据结构
- **📡 智能路由**：基于消息类型的自动分发机制
- **⚡ 高性能**：优化的序列化和网络传输
- **🎯 优先级控制**：支持消息优先级和超时管理
- **🔄 自动注册**：基于特性的消息处理器自动发现

### 架构组件

```
┌─────────────────────────────────────────────────────────┐
│                TypedMessage<T> 系统架构                  │
├─────────────────────────────────────────────────────────┤
│  应用层: MessageBuilder<T> + TypedMessage<T>            │
├─────────────────────────────────────────────────────────┤
│  处理层: TypedMessageHandler + MessageHandlerAttribute  │
├─────────────────────────────────────────────────────────┤
│  传输层: ProtoCustomMessage + MessageEnvelope           │
├─────────────────────────────────────────────────────────┤
│  网络层: ProtocolClientTransient + ProtocolServerTransient │
└─────────────────────────────────────────────────────────┘
```

## 🔧 基本用法

### 1. 定义消息类型

```csharp
// 简单消息类型
public class PlayerMoveMessage
{
    public int PlayerId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// 复杂消息类型
public class GameStateUpdateMessage
{
    public int CurrentWave { get; set; }
    public int PlayersAlive { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public List<PlayerStatus> PlayerStates { get; set; } = new();
    public Dictionary<string, object> CustomData { get; set; } = new();
}

public class PlayerStatus
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public float Health { get; set; }
    public bool IsAlive { get; set; }
}
```

### 2. 创建和发送消息

```csharp
// 创建消息实例
var moveMessage = new PlayerMoveMessage
{
    PlayerId = Player.LocalPlayer.Id,
    X = 10.5f,
    Y = 0.0f,
    Z = 20.3f
};

// 创建强类型消息
var typedMessage = new TypedMessage<PlayerMoveMessage>(moveMessage);

#if CLIENT
// 客户端发送到服务器
bool success = typedMessage.SendToServer();
if (success)
{
    Game.Logger.LogInformation("移动消息发送成功");
}

// 异步发送
bool asyncSuccess = await typedMessage.SendToServerAsync();
#endif

#if SERVER
// 服务器广播给所有玩家
typedMessage.Broadcast();

// 发送给特定玩家
typedMessage.SendTo(targetPlayer);

// 条件广播
typedMessage.Broadcast(player => player.Level >= 10);
#endif
```

## 📨 消息发送

### 客户端发送示例

```csharp
#if CLIENT
public class ClientMessageSender
{
    /// <summary>
    /// 发送玩家操作到服务器
    /// </summary>
    public static async Task<bool> SendPlayerAction(string action, object parameters)
    {
        var actionMessage = new PlayerActionMessage
        {
            PlayerId = Player.LocalPlayer.Id,
            Action = action,
            Parameters = JsonSerializer.Serialize(parameters),
            Timestamp = DateTime.UtcNow
        };

        var message = new TypedMessage<PlayerActionMessage>(actionMessage)
        {
            Priority = MessagePriority.High,
            Timeout = TimeSpan.FromSeconds(5)
        };

        return await message.SendToServerAsync();
    }

    /// <summary>
    /// 发送聊天消息
    /// </summary>
    public static void SendChatMessage(string content, ChatChannel channel = ChatChannel.Global)
    {
        var chatMessage = new ChatMessage
        {
            SenderId = Player.LocalPlayer.Id,
            SenderName = Player.LocalPlayer.User?.Name,
            Content = content,
            Channel = channel
        };

        var message = new TypedMessage<ChatMessage>(chatMessage);
        message.SendToServer();
    }
}
#endif
```

### 服务器发送示例

```csharp
#if SERVER
public class ServerMessageSender
{
    /// <summary>
    /// 广播游戏状态更新
    /// </summary>
    public static void BroadcastGameState(GameState gameState)
    {
        var stateMessage = new GameStateUpdateMessage
        {
            CurrentWave = gameState.CurrentWave,
            PlayersAlive = gameState.GetAlivePlayers().Count,
            TimeRemaining = gameState.TimeRemaining,
            PlayerStates = gameState.GetPlayerStates()
        };

        var message = new TypedMessage<GameStateUpdateMessage>(stateMessage)
        {
            Priority = MessagePriority.High
        };

        message.Broadcast();
    }

    /// <summary>
    /// 发送个人通知
    /// </summary>
    public static async Task SendNotificationToPlayer(Player player, string title, string content, NotificationType type = NotificationType.Info)
    {
        var notification = new NotificationMessage
        {
            Title = title,
            Content = content,
            Type = type,
            Timestamp = DateTime.UtcNow
        };

        var message = new TypedMessage<NotificationMessage>(notification)
        {
            Priority = MessagePriority.Normal,
            Timeout = TimeSpan.FromSeconds(10)
        };

        await message.SendToAsync(player);
    }

    /// <summary>
    /// 向团队广播消息
    /// </summary>
    public static void BroadcastToTeam(int teamId, object messageData)
    {
        var teamMessage = new TeamMessage
        {
            TeamId = teamId,
            Data = JsonSerializer.Serialize(messageData),
            Timestamp = DateTime.UtcNow
        };

        var message = new TypedMessage<TeamMessage>(teamMessage);
        
        // 只向指定团队的玩家广播
        message.Broadcast(player => player.TeamId == teamId);
    }
}
#endif
```

## 📬 消息接收

### 手动注册处理器

```csharp
public class MessageHandlers : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }

    private static void OnGameTriggerInitialization()
    {
        // 确保TypedMessageHandler已初始化
        TypedMessageHandler.Initialize();

        // 手动注册消息处理器
        RegisterMessageHandlers();
    }

    private static void RegisterMessageHandlers()
    {
#if CLIENT
        // 客户端处理器
        TypedMessageHandler.Register<GameStateUpdateMessage>(OnGameStateUpdate, 
            MessagePriority.High, "GameStateHandler");
        
        TypedMessageHandler.Register<NotificationMessage>(OnNotificationReceived,
            MessagePriority.Normal, "NotificationHandler");
        
        TypedMessageHandler.Register<ChatMessage>(OnChatMessageReceived,
            MessagePriority.Normal, "ChatHandler");
#endif

#if SERVER
        // 服务器处理器
        TypedMessageHandler.Register<PlayerActionMessage>(OnPlayerActionReceived,
            MessagePriority.High, "PlayerActionHandler");
        
        TypedMessageHandler.Register<ChatMessage>(OnChatMessageReceived,
            MessagePriority.Normal, "ChatHandler");
#endif

        Game.Logger.LogInformation("💬 TypedMessage handlers registered");
    }

#if CLIENT
    private static async Task<bool> OnGameStateUpdate(Player? sender, GameStateUpdateMessage message)
    {
        try
        {
            // 更新客户端游戏状态
            UpdateGameUI(message);
            
            Game.Logger.LogInformation("🎮 Game state updated: Wave {Wave}, Players {Players}", 
                message.CurrentWave, message.PlayersAlive);
            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "Failed to handle game state update");
            return false;
        }
    }

    private static async Task<bool> OnNotificationReceived(Player? sender, NotificationMessage message)
    {
        // 显示通知给用户
        ShowNotification(message.Title, message.Content, message.Type);
        
        Game.Logger.LogInformation("📢 Notification: {Title}", message.Title);
        return true;
    }
#endif

#if SERVER
    private static async Task<bool> OnPlayerActionReceived(Player? sender, PlayerActionMessage message)
    {
        if (sender == null) return false;

        try
        {
            // 验证玩家操作
            if (!ValidatePlayerAction(sender, message))
            {
                return false;
            }

            // 处理玩家操作
            await ProcessPlayerAction(sender, message);
            
            Game.Logger.LogInformation("⚡ Player {PlayerId} action: {Action}", 
                sender.Id, message.Action);
            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "Failed to handle player action from {PlayerId}", sender.Id);
            return false;
        }
    }
#endif

    private static async Task<bool> OnChatMessageReceived(Player? sender, ChatMessage message)
    {
#if SERVER
        // 服务器：验证并转发聊天消息
        if (sender != null && ValidateChatMessage(sender, message))
        {
            // 设置服务器信息
            message.SenderId = sender.Id;
            message.SenderName = sender.Name;
            message.Timestamp = DateTime.UtcNow;

            // 转发给其他玩家
            var forwardMessage = new TypedMessage<ChatMessage>(message);
            forwardMessage.Broadcast(p => p.Id != sender.Id);
        }
#endif

#if CLIENT
        // 客户端：显示聊天消息
        DisplayChatMessage(message);
#endif

        return true;
    }
}
```

## 🎛️ 消息处理器

### 基于特性的自动注册

TypedMessage系统支持基于 `MessageHandlerAttribute` 的自动消息处理器注册：

```csharp
public class AutoRegisteredHandlers
{
    /// <summary>
    /// 高优先级的游戏状态处理器
    /// </summary>
    [MessageHandler(Priority = MessagePriority.High, Name = "AutoGameStateHandler")]
    public static async Task<bool> HandleGameStateUpdate(Player? sender, GameStateUpdateMessage message)
    {
        // 处理游戏状态更新
        Game.Logger.LogInformation("🎯 Auto-handled game state update");
        return true;
    }

    /// <summary>
    /// 异步聊天消息处理器
    /// </summary>
    [MessageHandler(Priority = MessagePriority.Normal, IsAsync = true)]
    public static async Task<bool> HandleChatMessage(Player? sender, ChatMessage message)
    {
        // 异步处理聊天消息
        await ProcessChatAsync(message);
        return true;
    }

    /// <summary>
    /// 同步的玩家操作处理器
    /// </summary>
    [MessageHandler(Priority = MessagePriority.Critical, IsAsync = false)]
    public static bool HandlePlayerAction(Player? sender, PlayerActionMessage message)
    {
        // 同步处理（会被包装为异步）
        return ProcessPlayerActionSync(sender, message);
    }

    /// <summary>
    /// 错误处理演示
    /// </summary>
    [MessageHandler(Priority = MessagePriority.Low)]
    public static async Task<bool> HandleErrorTest(Player? sender, ErrorTestMessage message)
    {
        try
        {
            if (message.ShouldFail)
            {
                throw new InvalidOperationException("Intentional test error");
            }

            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "Error in message handler");
            return false; // 返回false表示处理失败
        }
    }
}
```

### 处理器方法签名要求

```csharp
// ✅ 正确的处理器签名
[MessageHandler]
public static async Task<bool> ValidHandler1(Player? sender, MyMessage message) { return true; }

[MessageHandler]
public static Task<bool> ValidHandler2(Player? sender, MyMessage message) { return Task.FromResult(true); }

[MessageHandler]
public static bool ValidHandler3(Player? sender, MyMessage message) { return true; }

// ❌ 错误的处理器签名
public static void InvalidHandler1(Player? sender, MyMessage message) { } // 错误：返回类型

public static async Task<bool> InvalidHandler2(MyMessage message) { return true; } // 错误：参数数量

public static async Task<bool> InvalidHandler3(string sender, MyMessage message) { return true; } // 错误：参数类型

private static async Task<bool> InvalidHandler4(Player? sender, MyMessage message) { return true; } // 错误：非public
```

## 🏗️ 流畅构建器API

`MessageBuilder<T>` 提供了链式API来创建和发送消息：

### 基本用法

```csharp
// 简单的消息构建和发送
await MessageBuilder<PlayerMoveMessage>
    .Create(new PlayerMoveMessage { X = 10, Y = 20, Z = 30 })
    .SendToServerAsync();

// 带优先级和超时的消息
await MessageBuilder<ImportantMessage>
    .Create(new ImportantMessage { Content = "Critical update" })
    .WithPriority(MessagePriority.Critical)
    .WithTimeout(TimeSpan.FromSeconds(30))
    .SendToServerAsync();
```

### 服务器端高级用法

```csharp
#if SERVER
// 条件广播
MessageBuilder<EventMessage>
    .Create(new EventMessage { Type = "BossSpawn", Data = bossData })
    .WithPriority(MessagePriority.High)
    .Broadcast(player => player.Level >= 10 && player.IsInArea(bossArea));

// 向特定玩家列表发送
var vipPlayers = GetVipPlayers();
await MessageBuilder<VipMessage>
    .Create(new VipMessage { Content = "VIP专属活动开始！" })
    .WithPriority(MessagePriority.Normal)
    .SendToPlayersAsync(vipPlayers);

// 队列发送（批量优化）
var messages = GeneratePlayerRewards();
await MessageBuilder<RewardMessage>
    .CreateBatch(messages)
    .WithPriority(MessagePriority.Low)
    .SendToAllAsync();
#endif
```

### 复杂的构建器示例

```csharp
public class AdvancedMessageBuilder
{
    /// <summary>
    /// 发送带重试机制的重要消息
    /// </summary>
    public static async Task<bool> SendReliableMessage<T>(T data, Player target, int maxRetries = 3) where T : class
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var success = await MessageBuilder<T>
                    .Create(data)
                    .WithPriority(MessagePriority.High)
                    .WithTimeout(TimeSpan.FromSeconds(10))
                    .SendToAsync(target);

                if (success)
                {
                    Game.Logger.LogInformation("✅ Reliable message sent successfully on attempt {Attempt}", attempt);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Game.Logger.LogWarning(ex, "⚠️ Message send attempt {Attempt} failed", attempt);
            }

            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 指数退避
                await Game.Delay(delay);
            }
        }

        Game.Logger.LogError("❌ Failed to send reliable message after {MaxRetries} attempts", maxRetries);
        return false;
    }

    /// <summary>
    /// 智能广播：根据消息内容决定接收者
    /// </summary>
    public static void SmartBroadcast<T>(T data) where T : class
    {
        var builder = MessageBuilder<T>.Create(data);

        // 根据消息类型设置不同的广播策略
        switch (data)
        {
            case ChatMessage chat when chat.Channel == ChatChannel.Global:
                builder.Broadcast(); // 全局聊天
                break;

            case ChatMessage chat when chat.Channel == ChatChannel.Team:
                builder.Broadcast(p => p.TeamId == chat.TeamId); // 团队聊天
                break;

            case GameEventMessage gameEvent when gameEvent.Severity == EventSeverity.Critical:
                builder.WithPriority(MessagePriority.Critical).Broadcast(); // 紧急事件
                break;

            case PlayerSpecificMessage playerMsg:
                var targetPlayer = Player.GetById(playerMsg.TargetPlayerId);
                if (targetPlayer != null)
                {
                    builder.SendTo(targetPlayer); // 定向消息
                }
                break;

            default:
                builder.WithPriority(MessagePriority.Normal).Broadcast(); // 默认广播
                break;
        }
    }
}
```

## ⚙️ 高级功能

### 消息统计和监控

```csharp
/// <summary>
/// 消息统计信息
/// </summary>
public static class MessageStatistics
{
    public static void LogMessageStats()
    {
        var stats = TypedMessageMetrics.GetCurrentStats();
        
        Game.Logger.LogInformation("📊 Message Statistics:");
        Game.Logger.LogInformation("   Total Sent: {SentCount}", stats.TotalSent);
        Game.Logger.LogInformation("   Total Received: {ReceivedCount}", stats.TotalReceived);
        Game.Logger.LogInformation("   Bytes Sent: {SentBytes:N0}", stats.BytesSent);
        Game.Logger.LogInformation("   Bytes Received: {ReceivedBytes:N0}", stats.BytesReceived);

        foreach (var (messageType, count) in stats.MessageTypeCounts)
        {
            Game.Logger.LogInformation("   {MessageType}: {Count}", messageType, count);
        }
    }

    public static void EnablePerformanceMonitoring()
    {
        // 定期记录性能统计
        var timer = new Timer(1000); // 每秒记录一次
        timer.OnTick += _ => LogMessageStats();
        timer.Start();
    }
}
```

### 消息中间件

```csharp
/// <summary>
/// 消息中间件：在消息处理前后执行自定义逻辑
/// </summary>
public static class MessageMiddleware
{
    private static readonly List<Func<object, Player?, Task<bool>>> _preprocessors = new();
    private static readonly List<Func<object, Player?, bool, Task>> _postprocessors = new();

    /// <summary>
    /// 添加消息预处理器
    /// </summary>
    public static void AddPreprocessor(Func<object, Player?, Task<bool>> processor)
    {
        _preprocessors.Add(processor);
    }

    /// <summary>
    /// 添加消息后处理器
    /// </summary>
    public static void AddPostprocessor(Func<object, Player?, bool, Task> processor)
    {
        _postprocessors.Add(processor);
    }

    /// <summary>
    /// 执行预处理
    /// </summary>
    public static async Task<bool> ExecutePreprocessors(object message, Player? sender)
    {
        foreach (var processor in _preprocessors)
        {
            if (!await processor(message, sender))
            {
                return false; // 如果任一预处理器返回false，停止处理
            }
        }
        return true;
    }

    /// <summary>
    /// 执行后处理
    /// </summary>
    public static async Task ExecutePostprocessors(object message, Player? sender, bool handlerResult)
    {
        foreach (var processor in _postprocessors)
        {
            try
            {
                await processor(message, sender, handlerResult);
            }
            catch (Exception ex)
            {
                Game.Logger.LogError(ex, "Error in message postprocessor");
            }
        }
    }
}

// 中间件使用示例
public class MessageMiddlewareExample : IGameClass
{
    public static void OnRegisterGameClass()
    {
        // 注册消息预处理器
        MessageMiddleware.AddPreprocessor(LogIncomingMessage);
        MessageMiddleware.AddPreprocessor(ValidateMessageSize);
        MessageMiddleware.AddPreprocessor(CheckPlayerPermissions);

        // 注册消息后处理器
        MessageMiddleware.AddPostprocessor(LogProcessingResult);
        MessageMiddleware.AddPostprocessor(UpdatePlayerStats);
    }

    private static async Task<bool> LogIncomingMessage(object message, Player? sender)
    {
        Game.Logger.LogDebug("📨 Incoming message: {MessageType} from {Sender}", 
            message.GetType().Name, sender?.Name ?? "Server");
        return true;
    }

    private static async Task<bool> ValidateMessageSize(object message, Player? sender)
    {
        var serialized = JsonSerializer.Serialize(message);
        if (serialized.Length > 64 * 1024) // 64KB limit
        {
            Game.Logger.LogWarning("⚠️ Message too large: {Size} bytes from {Sender}", 
                serialized.Length, sender?.Name ?? "Unknown");
            return false;
        }
        return true;
    }

    private static async Task<bool> CheckPlayerPermissions(object message, Player? sender)
    {
        // 只在服务器端检查权限
#if SERVER
        if (sender != null && message is AdminCommandMessage)
        {
            if (!sender.IsAdmin)
            {
                Game.Logger.LogWarning("🚫 Non-admin player {PlayerId} attempted admin command", sender.Id);
                return false;
            }
        }
#endif
        return true;
    }

    private static async Task LogProcessingResult(object message, Player? sender, bool result)
    {
        if (!result)
        {
            Game.Logger.LogWarning("❌ Message processing failed: {MessageType} from {Sender}", 
                message.GetType().Name, sender?.Name ?? "Server");
        }
    }

    private static async Task UpdatePlayerStats(object message, Player? sender, bool result)
    {
#if SERVER
        if (sender != null && result)
        {
            // 更新玩家消息统计
            sender.IncrementMessageCount(message.GetType().Name);
        }
#endif
    }
}
```

### 消息序列化自定义

```csharp
/// <summary>
/// 自定义消息序列化器
/// </summary>
public static class CustomMessageSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new Vector3JsonConverter(), new TimeSpanJsonConverter() }
    };

    public static byte[] Serialize<T>(T data) where T : class
    {
        return JsonSerializer.SerializeToUtf8Bytes(data, _options);
    }

    public static T? Deserialize<T>(byte[] data) where T : class
    {
        return JsonSerializer.Deserialize<T>(data, _options);
    }
}

// 自定义转换器示例
public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // 从JSON读取Vector3
        var obj = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
        return new Vector3(
            obj.GetProperty("x").GetSingle(),
            obj.GetProperty("y").GetSingle(),
            obj.GetProperty("z").GetSingle()
        );
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        // 将Vector3写入JSON
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}
```

## 🆚 与ProtoCustomMessage对比

| 特性 | TypedMessage<T> | ProtoCustomMessage |
|------|----------------|-------------------|
| **类型安全** | ✅ 编译时检查 | ❌ 运行时检查 |
| **序列化** | ✅ 自动JSON序列化 | ❌ 手动处理 |
| **消息路由** | ✅ 基于类型自动分发 | ❌ 手动解析分发 |
| **开发效率** | ✅ 高（声明式） | ❌ 低（命令式） |
| **调试友好** | ✅ 强类型，易调试 | ❌ 字节数组，难调试 |
| **性能开销** | ⚠️ 中等（JSON序列化） | ✅ 低（直接字节） |
| **灵活性** | ⚠️ 受JSON限制 | ✅ 支持任意格式 |
| **学习曲线** | ✅ 简单易学 | ❌ 需要理解底层 |

### 迁移指南：从ProtoCustomMessage到TypedMessage

```csharp
// 原始的ProtoCustomMessage代码
public class OldMessageHandler
{
    public static void SendPlayerUpdate(int playerId, float x, float y)
    {
        var data = new { PlayerId = playerId, X = x, Y = y };
        var json = JsonSerializer.Serialize(data);
        var message = new ProtoCustomMessage 
        { 
            Message = Encoding.UTF8.GetBytes(json) 
        };
        message.SendToServer();
    }

    private static async Task<bool> OnServerMessageReceived(object sender, EventServerMessage eventArgs)
    {
        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Message);
            var envelope = JsonSerializer.Deserialize<MessageEnvelope>(json);
            
            if (envelope?.Type == "PlayerUpdate")
            {
                var data = JsonSerializer.Deserialize<PlayerUpdateData>(envelope.Data.ToString()!);
                // 处理数据...
                return true;
            }
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "消息处理失败");
        }
        return false;
    }
}

// 新的TypedMessage代码
public class NewMessageHandler
{
    public static void SendPlayerUpdate(int playerId, float x, float y)
    {
        var message = new PlayerUpdateMessage
        {
            PlayerId = playerId,
            X = x,
            Y = y
        };

        var typedMessage = new TypedMessage<PlayerUpdateMessage>(message);
        typedMessage.SendToServer();
    }

    [MessageHandler]
    public static async Task<bool> OnPlayerUpdateReceived(Player? sender, PlayerUpdateMessage message)
    {
        // 直接处理强类型消息，无需手动序列化
        ProcessPlayerUpdate(message);
        return true;
    }
}
```

### 性能对比测试

```csharp
public static class PerformanceComparison
{
    public static async Task RunComparison()
    {
        const int messageCount = 1000;
        var testData = new PlayerMoveMessage { PlayerId = 1, X = 10, Y = 20, Z = 30 };

        // 测试ProtoCustomMessage
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < messageCount; i++)
        {
            var json = JsonSerializer.Serialize(testData);
            var message = new ProtoCustomMessage { Message = Encoding.UTF8.GetBytes(json) };
            // message.SendToServer(); // 模拟发送
        }
        sw1.Stop();

        // 测试TypedMessage
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < messageCount; i++)
        {
            var message = new TypedMessage<PlayerMoveMessage>(testData);
            // message.SendToServer(); // 模拟发送
        }
        sw2.Stop();

        Game.Logger.LogInformation("Performance Comparison ({MessageCount} messages):", messageCount);
        Game.Logger.LogInformation("ProtoCustomMessage: {Time1}ms", sw1.ElapsedMilliseconds);
        Game.Logger.LogInformation("TypedMessage<T>: {Time2}ms", sw2.ElapsedMilliseconds);
        Game.Logger.LogInformation("Ratio: {Ratio:F2}x", (double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds);
    }
}
```

## 💡 最佳实践

### 1. 消息类型设计

```csharp
// ✅ 推荐：清晰的消息类型命名
public class PlayerJoinGameMessage { /* ... */ }
public class GameStateUpdateMessage { /* ... */ }
public class ChatBroadcastMessage { /* ... */ }

// ❌ 避免：模糊的命名
public class MessageData { /* ... */ }
public class Info { /* ... */ }
public class Update { /* ... */ }

// ✅ 推荐：包含版本信息
public class PlayerActionMessage
{
    public int Version { get; set; } = 1;
    public string Action { get; set; } = string.Empty;
    public object? Parameters { get; set; }
}

// ✅ 推荐：使用枚举而不是字符串
public enum ChatChannel { Global, Team, Private, System }
public enum NotificationType { Info, Warning, Error, Success }
```

### 2. 错误处理模式

```csharp
// ✅ 推荐：完整的错误处理
[MessageHandler]
public static async Task<bool> HandlePlayerAction(Player? sender, PlayerActionMessage message)
{
    try
    {
        // 1. 验证发送者
        if (sender == null || !sender.IsOnline)
        {
            Game.Logger.LogWarning("Received message from invalid sender");
            return false;
        }

        // 2. 验证消息内容
        if (string.IsNullOrEmpty(message.Action))
        {
            Game.Logger.LogWarning("Received empty action from player {PlayerId}", sender.Id);
            return false;
        }

        // 3. 执行操作
        var result = await ExecutePlayerAction(sender, message);
        
        // 4. 发送结果反馈
        if (!result.Success)
        {
            await SendErrorToPlayer(sender, result.ErrorMessage);
        }

        return result.Success;
    }
    catch (Exception ex)
    {
        Game.Logger.LogError(ex, "Error handling player action from {PlayerId}", sender?.Id ?? -1);
        
        // 发送通用错误消息给客户端
        if (sender != null)
        {
            await SendErrorToPlayer(sender, "操作执行失败，请稍后重试");
        }
        
        return false;
    }
}
```

### 3. 消息优先级使用

```csharp
// ✅ 推荐：合理使用优先级
public static class MessagePriorityGuide
{
    public static void SendCriticalMessage<T>(T data) where T : class
    {
        // Critical: 影响游戏核心功能的消息
        MessageBuilder<T>.Create(data)
            .WithPriority(MessagePriority.Critical)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .SendToServer();
    }

    public static void SendHighPriorityMessage<T>(T data) where T : class
    {
        // High: 游戏状态更新、玩家操作
        MessageBuilder<T>.Create(data)
            .WithPriority(MessagePriority.High)
            .WithTimeout(TimeSpan.FromSeconds(10))
            .SendToServer();
    }

    public static void SendNormalMessage<T>(T data) where T : class
    {
        // Normal: 聊天消息、通知
        MessageBuilder<T>.Create(data)
            .WithPriority(MessagePriority.Normal)
            .WithTimeout(TimeSpan.FromSeconds(5))
            .SendToServer();
    }

    public static void SendLowPriorityMessage<T>(T data) where T : class
    {
        // Low: 统计数据、日志信息
        MessageBuilder<T>.Create(data)
            .WithPriority(MessagePriority.Low)
            .WithTimeout(TimeSpan.FromSeconds(60))
            .SendToServer();
    }
}
```

### 4. 批量消息优化

```csharp
// ✅ 推荐：批量处理提高效率
public class BatchMessageProcessor<T> where T : class
{
    private readonly List<T> _batch = new();
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    private Timer? _flushTimer;

    public BatchMessageProcessor(int batchSize = 10, TimeSpan? flushInterval = null)
    {
        _batchSize = batchSize;
        _flushInterval = flushInterval ?? TimeSpan.FromMilliseconds(100);
        
        _flushTimer = new Timer((int)_flushInterval.TotalMilliseconds);
        _flushTimer.OnTick += _ => FlushBatch();
        _flushTimer.Start();
    }

    public void AddMessage(T message)
    {
        lock (_batch)
        {
            _batch.Add(message);
            
            if (_batch.Count >= _batchSize)
            {
                FlushBatch();
            }
        }
    }

    private void FlushBatch()
    {
        List<T> toProcess;
        
        lock (_batch)
        {
            if (_batch.Count == 0) return;
            
            toProcess = new List<T>(_batch);
            _batch.Clear();
        }

        // 创建批量消息
        var batchMessage = new BatchMessage<T>
        {
            Items = toProcess,
            BatchId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow
        };

        var typedMessage = new TypedMessage<BatchMessage<T>>(batchMessage);
        typedMessage.SendToServer();
    }

    public void Dispose()
    {
        _flushTimer?.Stop();
        _flushTimer?.Dispose();
        FlushBatch(); // 发送剩余消息
    }
}

// 批量消息数据结构
public class BatchMessage<T> where T : class
{
    public List<T> Items { get; set; } = new();
    public Guid BatchId { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## 🚨 常见陷阱

### 1. 序列化陷阱

```csharp
// ❌ 错误：使用不支持的数据类型
public class BadMessage
{
    public PieceType[,] Board { get; set; } = new PieceType[15, 15]; // 二维数组不支持
    public Dictionary<Player, int> PlayerScores { get; set; } = new(); // Player对象不能序列化
}

// ✅ 正确：使用支持的数据结构
public class GoodMessage
{
    public PieceType[] Board { get; set; } = new PieceType[225]; // 使用一维数组
    public int BoardWidth { get; set; } = 15;
    public int BoardHeight { get; set; } = 15;
    public Dictionary<int, int> PlayerScores { get; set; } = new(); // 使用玩家ID
    
    // 辅助方法
    public PieceType GetPiece(int row, int col) => Board[row * BoardWidth + col];
    public void SetPiece(int row, int col, PieceType piece) => Board[row * BoardWidth + col] = piece;
}
```

### 2. 内存泄漏陷阱

```csharp
// ❌ 错误：未正确注销事件处理器
public class LeakyMessageHandler
{
    public void StartListening()
    {
        TypedMessageHandler.Register<MyMessage>(HandleMessage);
        // 忘记保存引用以便后续注销
    }
}

// ✅ 正确：实现IDisposable模式
public class ProperMessageHandler : IDisposable
{
    private bool _disposed = false;

    public ProperMessageHandler()
    {
        TypedMessageHandler.Register<MyMessage>(HandleMessage, name: "ProperHandler");
    }

    [MessageHandler]
    private static async Task<bool> HandleMessage(Player? sender, MyMessage message)
    {
        // 处理消息
        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            TypedMessageHandler.RemoveHandlers<MyMessage>();
            _disposed = true;
        }
    }
}
```

### 3. 死循环陷阱

```csharp
// ❌ 错误：可能导致消息循环
[MessageHandler]
public static async Task<bool> BadHandler(Player? sender, ChatMessage message)
{
    // 在处理聊天消息时又发送聊天消息！
    var response = new ChatMessage { Content = "Received: " + message.Content };
    var typedMessage = new TypedMessage<ChatMessage>(response);
    typedMessage.SendToServer(); // 可能导致无限循环
    
    return true;
}

// ✅ 正确：避免循环引用
[MessageHandler]
public static async Task<bool> GoodHandler(Player? sender, ChatMessage message)
{
    // 使用不同的消息类型或添加防循环检查
    if (message.Source == "AutoReply") return true; // 防止自动回复循环
    
    var response = new SystemNotificationMessage 
    { 
        Content = "Received: " + message.Content,
        Source = "AutoReply"
    };
    
    var typedMessage = new TypedMessage<SystemNotificationMessage>(response);
    typedMessage.SendToServer();
    
    return true;
}
```

### 4. 性能陷阱

```csharp
// ❌ 错误：高频消息发送
public class PerformanceProblem
{
    private void Update() // 在游戏循环中被调用
    {
        // 每帧都发送位置更新！
        var position = new PlayerPositionMessage
        {
            PlayerId = Player.LocalPlayer.Id,
            X = transform.Position.X,
            Y = transform.Position.Y,
            Z = transform.Position.Z
        };
        
        var message = new TypedMessage<PlayerPositionMessage>(position);
        message.SendToServer(); // 非常低效
    }
}

// ✅ 正确：使用节流和批量处理
public class PerformanceOptimized
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(50); // 20Hz
    private DateTime _lastUpdate = DateTime.MinValue;
    private Vector3 _lastPosition;
    private const float MinMovement = 0.1f; // 最小移动距离

    private void Update()
    {
        var now = DateTime.UtcNow;
        var currentPosition = transform.Position;
        
        // 检查时间间隔和移动距离
        if (now - _lastUpdate < UpdateInterval ||
            Vector3.Distance(currentPosition, _lastPosition) < MinMovement)
        {
            return;
        }

        var position = new PlayerPositionMessage
        {
            PlayerId = Player.LocalPlayer.Id,
            X = currentPosition.X,
            Y = currentPosition.Y,
            Z = currentPosition.Z,
            Timestamp = now
        };

        var message = new TypedMessage<PlayerPositionMessage>(position);
        message.SendToServer();

        _lastUpdate = now;
        _lastPosition = currentPosition;
    }
}
```

---

## 📚 相关文档

- [消息传递系统 (ProtoCustomMessage)](MessagingSystem.md)
- [常见开发陷阱与解决方案](../best-practices/CommonPitfalls.md)
- [框架编码规范](../CONVENTIONS.md)
- [异步编程最佳实践](../best-practices/AsyncProgramming.md)

---

> 💡 **提示**: TypedMessage系统是WasiCore框架的高级特性，它在保持高性能的同时提供了类型安全和开发便利性。建议在新项目中优先使用TypedMessage，在需要极致性能的场景中考虑ProtoCustomMessage。 