# 服务器频道系统 (Server Channel System)

## 概述

服务器频道系统提供了一个基于发布/订阅模式的跨服务器消息传递机制，使得运行相同游戏的多个服务器实例能够实时交换数据和事件。为开发者提供简洁易用的API。

### 核心特性

- **跨服务器通信**：多个游戏服务器实例可以实时交换消息
- **发布/订阅模式**：灵活的消息分发机制，支持一对多通信
- **分类频道**：支持创建多个命名频道，实现消息分类管理
- **JSON序列化**：内置JSON序列化支持，方便传递结构化数据
- **用户专属消息**：提供便捷API实现点对点的用户消息传递
- **事件驱动**：与框架事件系统无缝集成

### 典型应用场景

| 场景 | 说明 | 示例 |
|------|------|------|
| **跨服私聊** | 不同服务器的玩家互相发送私信 | Server-1的玩家给Server-2的玩家发消息 |
| **世界事件** | 广播全服事件通知 | 世界Boss击杀、服务器维护通知 |
| **跨服组队** | 跨服务器的队伍组建和协调 | 跨服副本组队系统 |
| **全局聊天** | 跨服务器的聊天频道 | 世界频道、公会频道 |
| **拍卖行** | 全服共享的交易系统 | 物品上架通知、交易完成通知 |
| **好友系统** | 跨服好友状态同步 | 好友上线/下线通知 |
| **排行榜** | 实时的跨服数据同步 | 竞技场排行榜更新 |

## 基础用法

### 1. 订阅和发布通用频道

通用频道适用于广播类消息，所有订阅该频道的服务器都会收到消息。

```csharp
using GameCore.ServerChannelSystem;

// 订阅频道
ServerChannelMessage.Subscribe("world-events");
ServerChannelMessage.Subscribe("global-chat");

// 发送消息到频道（JSON对象）
var eventData = new
{
    EventType = "BossKilled",
    BossName = "Dragon King",
    ServerName = "Server-1",
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
};

ServerChannelMessage.Publish("world-events", eventData);
```

### 2. 接收频道消息

通过注册事件处理器来接收消息：

```csharp
using GameCore.ServerChannelSystem.Event;
using Events;

public class ChannelMessageHandler : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameInstanceInitialization += Initialize;
    }

    private static void Initialize()
    {
        // 注册消息接收处理器
        var trigger = new Trigger<EventChannelMessage>(OnMessageReceived);
        trigger.Register(Game.Instance);
    }

    private static async Task<bool> OnMessageReceived(object sender, EventChannelMessage args)
    {
        Game.Logger.LogInformation("Received message from channel: {Channel}", args.Channel);
        
        // 解析JSON消息
        if (ServerChannelMessage.TryParseJson<WorldEventData>(args.Message, out var eventData))
        {
            await HandleWorldEvent(eventData);
        }
        
        return true;
    }
    
    private static async Task HandleWorldEvent(WorldEventData eventData)
    {
        // 处理世界事件...
        await Task.CompletedTask;
    }
}

public class WorldEventData
{
    public string? EventType { get; set; }
    public string? BossName { get; set; }
    public string? ServerName { get; set; }
    public long Timestamp { get; set; }
}
```

## 用户专属消息

系统提供了专门的API用于向特定用户发送消息，无需关心用户当前在哪个服务器。

### 用户消息类型

用户消息支持多种类型的子频道，用于区分不同类型的消息，用户可以自定义频道名称，以下是一些建议的频道名称：

- **`chat`** - 跨服私聊消息
- **`friend`** - 好友相关通知（上线、下线等）
- **`team`** - 跨服组队邀请和通知
- **`mail`** - 邮件到达通知
- **`trade`** - 交易和拍卖行通知
- **`system`** - 系统通知

### 订阅用户消息

当用户登录时，订阅该用户需要接收的消息类型：

```csharp
public static void OnUserLogin(long userId)
{
    // 订阅用户所需的消息类型
    ServerChannelMessage.SubscribeUser(userId, "chat");     // 私聊消息
    ServerChannelMessage.SubscribeUser(userId, "friend");   // 好友通知
    ServerChannelMessage.SubscribeUser(userId, "team");     // 组队邀请
    ServerChannelMessage.SubscribeUser(userId, "mail");     // 邮件通知
    
    Game.Logger.LogInformation("User {UserId} subscribed to personal channels", userId);
}

public static void OnUserLogout(long userId)
{
    // 登出时取消订阅
    ServerChannelMessage.UnsubscribeUser(userId, "chat");
    ServerChannelMessage.UnsubscribeUser(userId, "friend");
    ServerChannelMessage.UnsubscribeUser(userId, "team");
    ServerChannelMessage.UnsubscribeUser(userId, "mail");
}
```

### 发送用户消息

向指定用户发送消息，无需知道用户在哪个服务器：

```csharp
// 发送私聊消息
public static void SendPrivateMessage(long fromUserId, string fromUserName, 
                                      long toUserId, string message)
{
    var chatMessage = new
    {
        Type = "PrivateChat",
        FromUserId = fromUserId,
        FromUserName = fromUserName,
        Message = message,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    };

    // 发送到目标用户的私聊频道
    ServerChannelMessage.PublishToUser(toUserId, "chat", chatMessage);
}

// 发送好友上线通知
public static void NotifyFriendOnline(long userId, long friendId, string friendName)
{
    var notification = new
    {
        Type = "FriendOnline",
        FriendId = friendId,
        FriendName = friendName,
        ServerName = $"Server-{Game.Instance.SessionId}"
    };

    ServerChannelMessage.PublishToUser(userId, "friend", notification);
}

// 发送组队邀请
public static void SendTeamInvitation(long fromUserId, string fromUserName, 
                                      long toUserId, int teamId)
{
    var invitation = new
    {
        Type = "TeamInvitation",
        FromUserId = fromUserId,
        FromUserName = fromUserName,
        TeamId = teamId,
        ExpireTime = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
    };

    ServerChannelMessage.PublishToUser(toUserId, "team", invitation);
}
```

### 处理用户消息

接收并处理发送给特定用户的消息：

```csharp
private static async Task<bool> OnMessageReceived(object sender, EventChannelMessage args)
{
    // 判断是否为用户消息，并提取用户ID和消息类型
    if (ServerChannelMessage.TryGetUserIdFromChannel(args.Channel, out var userId, out var messageType))
    {
        // 根据消息类型分发处理
        switch (messageType)
        {
            case "chat":
                if (ServerChannelMessage.TryParseJson<ChatMessage>(args.Message, out var chatMsg))
                {
                    await DeliverChatToUser(userId, chatMsg);
                }
                break;
                
            case "friend":
                if (ServerChannelMessage.TryParseJson<FriendNotification>(args.Message, out var friendMsg))
                {
                    await NotifyUserAboutFriend(userId, friendMsg);
                }
                break;
                
            case "team":
                if (ServerChannelMessage.TryParseJson<TeamInvitation>(args.Message, out var teamMsg))
                {
                    await DeliverTeamInvitation(userId, teamMsg);
                }
                break;
                
            case "mail":
                if (ServerChannelMessage.TryParseJson<MailNotification>(args.Message, out var mailMsg))
                {
                    await NotifyUserAboutNewMail(userId, mailMsg);
                }
                break;
        }
    }
    
    return true;
}

private static async Task DeliverChatToUser(long userId, ChatMessage message)
{
    // 找到目标用户并发送消息到客户端
    var player = Player.AllPlayers.FirstOrDefault(p => p.User?.UserId == userId);
    if (player != null)
    {
        // 发送到客户端的逻辑...
        Game.Logger.LogInformation("Delivered chat to user {UserId}", userId);
    }
    await Task.CompletedTask;
}
```

## 完整示例

### 跨服私聊系统

```csharp
using GameCore.ServerChannelSystem;
using GameCore.ServerChannelSystem.Event;
using Events;

namespace MyGame.CrossServerSystems;

#if SERVER

/// <summary>
/// 跨服私聊系统
/// </summary>
public class CrossServerChatSystem : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameInstanceInitialization += Initialize;
    }

    private static void Initialize()
    {
        // 注册消息处理器
        var trigger = new Trigger<EventChannelMessage>(OnChannelMessageReceived);
        trigger.Register(Game.Instance);
        
        Game.Logger.LogInformation("CrossServerChat system initialized");
    }

    /// <summary>
    /// 用户登录时调用
    /// </summary>
    public static void OnPlayerLogin(long userId)
    {
        // 订阅私聊频道
        ServerChannelMessage.SubscribeUser(userId, "chat");
        Game.Logger.LogInformation("User {UserId} subscribed to chat channel", userId);
    }

    /// <summary>
    /// 用户登出时调用
    /// </summary>
    public static void OnPlayerLogout(long userId)
    {
        // 取消订阅
        ServerChannelMessage.UnsubscribeUser(userId, "chat");
        Game.Logger.LogInformation("User {UserId} unsubscribed from chat channel", userId);
    }

    /// <summary>
    /// 发送跨服私聊消息
    /// </summary>
    public static void SendChatMessage(long fromUserId, string fromUserName, 
                                      long toUserId, string message)
    {
        var chatMessage = new ChatMessage
        {
            Type = "PrivateChat",
            FromUserId = fromUserId,
            FromUserName = fromUserName,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        if (ServerChannelMessage.PublishToUser(toUserId, "chat", chatMessage))
        {
            Game.Logger.LogInformation("Chat sent from {From} to {To}", fromUserId, toUserId);
        }
        else
        {
            Game.Logger.LogWarning("Failed to send chat to user {UserId}", toUserId);
        }
    }

    /// <summary>
    /// 处理接收到的频道消息
    /// </summary>
    private static async Task<bool> OnChannelMessageReceived(object sender, EventChannelMessage args)
    {
        if (ServerChannelMessage.TryGetUserIdFromChannel(args.Channel, out var userId, out var channelType))
        {
            if (channelType == "chat")
            {
                if (ServerChannelMessage.TryParseJson<ChatMessage>(args.Message, out var chatMessage))
                {
                    await DeliverMessageToUser(userId, chatMessage);
                }
            }
        }
        return true;
    }

    /// <summary>
    /// 将消息投递给用户
    /// </summary>
    private static async Task DeliverMessageToUser(long userId, ChatMessage message)
    {
        var player = Player.AllPlayers.FirstOrDefault(p => p.User?.UserId == userId);
        
        if (player != null)
        {
            Game.Logger.LogInformation(
                "User {UserId} received chat from {FromUser}: {Message}",
                userId, message.FromUserName, message.Message);
            
            // TODO: 实现向客户端发送消息的逻辑
            // 例如：await player.SendNotification("PrivateChat", message);
        }
        else
        {
            Game.Logger.LogDebug("User {UserId} not found on this server", userId);
        }
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// 私聊消息数据结构
/// </summary>
public class ChatMessage
{
    public string Type { get; set; } = "PrivateChat";
    public long FromUserId { get; set; }
    public string FromUserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}

#endif
```

### 跨服好友系统

```csharp
/// <summary>
/// 跨服好友通知系统
/// </summary>
public class CrossServerFriendSystem : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameInstanceInitialization += Initialize;
    }

    private static void Initialize()
    {
        var trigger = new Trigger<EventChannelMessage>(OnChannelMessageReceived);
        trigger.Register(Game.Instance);
    }

    /// <summary>
    /// 用户登录时通知所有在线好友
    /// </summary>
    public static void NotifyFriendsAboutLogin(long userId, string userName, List<long> friendIds)
    {
        var notification = new FriendStatusNotification
        {
            UserId = userId,
            UserName = userName,
            Status = "Online",
            ServerName = $"Server-{Game.Instance.SessionId}",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        foreach (var friendId in friendIds)
        {
            ServerChannelMessage.PublishToUser(friendId, "friend", notification);
        }
        
        Game.Logger.LogInformation(
            "User {UserId} online notification sent to {Count} friends", 
            userId, friendIds.Count);
    }

    /// <summary>
    /// 用户登出时通知所有在线好友
    /// </summary>
    public static void NotifyFriendsAboutLogout(long userId, string userName, List<long> friendIds)
    {
        var notification = new FriendStatusNotification
        {
            UserId = userId,
            UserName = userName,
            Status = "Offline",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        foreach (var friendId in friendIds)
        {
            ServerChannelMessage.PublishToUser(friendId, "friend", notification);
        }
    }

    private static async Task<bool> OnChannelMessageReceived(object sender, EventChannelMessage args)
    {
        if (ServerChannelMessage.TryGetUserIdFromChannel(args.Channel, out var userId, out var channelType))
        {
            if (channelType == "friend")
            {
                if (ServerChannelMessage.TryParseJson<FriendStatusNotification>(
                    args.Message, out var notification))
                {
                    await HandleFriendNotification(userId, notification);
                }
            }
        }
        return true;
    }

    private static async Task HandleFriendNotification(long userId, FriendStatusNotification notification)
    {
        var player = Player.AllPlayers.FirstOrDefault(p => p.User?.UserId == userId);
        
        if (player != null)
        {
            Game.Logger.LogInformation(
                "Notifying user {UserId}: Friend {FriendName} is now {Status}",
                userId, notification.UserName, notification.Status);
            
            // TODO: 向客户端发送好友状态变化通知
        }
        
        await Task.CompletedTask;
    }
}

public class FriendStatusNotification
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}
```

## API 参考

### ServerChannelMessage

主要静态方法类，提供频道操作的简单API。

#### 通用频道方法

| 方法 | 说明 |
|------|------|
| `Subscribe(string channel)` | 订阅指定频道 |
| `Unsubscribe(string channel)` | 取消订阅指定频道 |
| `Publish<T>(string channel, T data)` | 发送JSON序列化的对象到频道 |
| `Publish(string channel, byte[] message)` | 发送字节数组到频道 |
| `TryParseJson<T>(ReadOnlySpan<byte> message, out T? result)` | 尝试解析JSON消息 |

#### 用户专属消息方法

| 方法 | 说明 |
|------|------|
| `SubscribeUser(long userId, string messageType)` | 订阅指定用户的特定类型消息 |
| `UnsubscribeUser(long userId, string messageType)` | 取消订阅用户消息 |
| `PublishToUser<T>(long userId, string messageType, T data)` | 向指定用户发送特定类型的消息 |
| `PublishToUser(long userId, string messageType, byte[] message)` | 向指定用户发送字节数组消息 |
| `TryGetUserIdFromChannel(string channel, out long userId, out string messageType)` | 从频道信息中提取用户ID和消息类型 |

### EventChannelMessage

频道消息事件，当接收到订阅频道的消息时触发。

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Channel` | `string` | 消息来源的频道标识 |
| `Message` | `byte[]` | 消息内容的字节数组 |

## 最佳实践

### 1. 定义清晰的消息结构

使用强类型的数据结构定义消息格式：

```csharp
// 使用record定义消息结构，简洁且不可变
public record ServerEventMessage(
    string EventType,
    string SourceServer,
    long Timestamp,
    Dictionary<string, object>? Data = null
);
```

### 2. 合理规划频道命名

使用有意义的频道名称，便于管理和调试：

- `world-events` - 世界事件
- `global-chat` - 全局聊天
- `server-sync` - 服务器同步
- `auction-house` - 拍卖行通知

### 3. 错误处理

始终添加适当的错误处理：

```csharp
private static async Task<bool> OnMessageReceived(object sender, EventChannelMessage args)
{
    try
    {
        // 处理消息...
        return true;
    }
    catch (Exception ex)
    {
        Game.Logger.LogError(ex, "Error processing message from {Channel}", args.Channel);
        return false;
    }
}
```

### 4. 控制消息大小

避免发送过大的消息，必要时进行分片或压缩：

```csharp
// 好的做法：只发送必要的数据
var notification = new { UserId = userId, Status = "Online" };

// 避免：发送大量冗余数据
// var notification = new { UserObject = entireUserObject };
```

### 5. 资源管理

及时取消不再需要的订阅：

```csharp
public static void OnPlayerLogout(long userId)
{
    // 登出时取消所有订阅
    ServerChannelMessage.UnsubscribeUser(userId, "chat");
    ServerChannelMessage.UnsubscribeUser(userId, "friend");
    ServerChannelMessage.UnsubscribeUser(userId, "team");
}
```

### 6. 消息幂等性

设计消息处理逻辑时考虑幂等性，避免重复处理导致的问题：

```csharp
// 使用消息ID和时间戳来去重
public record IdempotentMessage(
    string MessageId,  // 唯一消息ID
    long Timestamp,
    string Content
);
```

## 性能考虑

1. **订阅管理**：只订阅真正需要的频道，避免无用的消息处理开销
2. **消息频率**：控制消息发送频率，避免频繁的小消息
3. **批量操作**：如果需要向多个用户发送相同消息，考虑使用通用频道
4. **异步处理**：消息处理器应该快速返回，耗时操作使用异步方式

## 限制和注意事项

1. **消息可靠性**：系统不保证消息的持久化存储，离线用户不会收到历史消息
2. **消息顺序**：不保证严格的消息顺序，需要顺序保证时应在消息中包含时间戳
3. **消息大小**：建议单条消息不超过1MB
4. **频道隔离**：系统会自动处理不同游戏实例间的频道隔离
5. **仅服务端**：此系统仅在服务端可用，客户端无法直接访问

## 故障排查

### 消息没有收到

1. 检查是否正确订阅了频道
2. 确认消息发送和接收在同一游戏实例
3. 检查事件处理器是否正确注册
4. 查看日志中的错误信息

### 消息解析失败

1. 确认发送和接收使用相同的数据结构
2. 检查JSON序列化/反序列化是否正确
3. 验证消息格式是否符合预期

### 性能问题

1. 减少不必要的频道订阅
2. 控制消息发送频率
3. 优化消息处理逻辑
4. 考虑使用消息批处理

## 相关系统

- [TypedMessage系统](TypedMessageSystem.md) - 客户端-服务器消息传递
- [CloudData系统](CloudDataSystem.md) - 持久化数据存储
- [Trigger系统](TriggerSystem.md) - 事件处理机制

