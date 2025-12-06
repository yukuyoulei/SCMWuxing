# 消息传递系统文档

## 概述

WasiCore 框架提供了 `ProtoCustomMessage` 消息传递系统，用于在客户端和服务器之间传递自定义格式的数据。该系统支持双向通信，提供简单易用的 API，并通过事件机制实现消息的分发和处理。

## 核心特性

- **双向通信**：支持客户端到服务器以及服务器到客户端的消息传递
- **自定义格式**：支持任意数据格式，包括 JSON、MessagePack、二进制等
- **事件驱动**：基于事件系统进行消息分发，解耦发送和处理逻辑
- **灵活路由**：支持广播、定向发送和条件过滤
- **高性能**：轻量级实现，适合高频消息传递场景

## 基本用法

### 客户端发送消息到服务器

```csharp
// 创建消息内容
var playerAction = new { Action = "Move", X = 10.5f, Y = 20.3f };
var json = JsonSerializer.Serialize(playerAction);

// 创建自定义消息
var message = new ProtoCustomMessage 
{ 
    Message = Encoding.UTF8.GetBytes(json) 
};

// 发送到服务器
if (message.SendToServer())
{
    Game.Logger.LogInformation("消息发送成功");
}
else
{
    Game.Logger.LogWarning("消息发送失败");
}
```

### 服务器发送消息到客户端

```csharp
// 向所有玩家广播
var gameState = new { Wave = 5, TimeRemaining = 120 };
var message = new ProtoCustomMessage 
{ 
    Message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(gameState)) 
};
message.Broadcast();

// 向特定玩家发送
var notification = new { Type = "LevelUp", NewLevel = 15 };
var notifyMessage = new ProtoCustomMessage 
{ 
    Message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification)) 
};
notifyMessage.SendTo(targetPlayer);
```

## 消息处理流程

### 1. 消息发送流程

1. **创建消息**：实例化 `ProtoCustomMessage` 并设置 `Message` 属性
2. **调用发送方法**：使用相应的发送方法（`SendToServer`、`Broadcast`、`SendTo`）
3. **网络传输**：框架自动处理消息的序列化和网络传输
4. **接收处理**：接收方的 `Handle` 方法被自动调用

### 2. 消息接收流程

1. **网络接收**：框架接收到消息并识别为 `ProtoCustomMessage` 类型
2. **自动分发**：调用相应的 `Handle` 方法
3. **事件触发**：触发 `EventServerMessage` 或 `EventClientMessage` 事件
4. **用户处理**：注册的事件监听器处理具体的消息内容

## 事件系统集成

### 客户端消息处理

```csharp
// 注册来自服务器的消息监听器
var trigger = new Trigger<EventServerMessage>(OnServerMessageReceived);
trigger.Register(Game.Instance);

private static async Task<bool> OnServerMessageReceived(object sender, EventServerMessage eventArgs)
{
    var messageBytes = eventArgs.Message;
    
    try
    {
        var json = Encoding.UTF8.GetString(messageBytes);
        var data = JsonSerializer.Deserialize<GameStateUpdate>(json);
        
        // 处理游戏状态更新
        HandleGameStateUpdate(data);
        
        Game.Logger.LogInformation("处理服务器消息成功: {MessageType}", data.Type);
        return true;
    }
    catch (Exception ex)
    {
        Game.Logger.LogError(ex, "处理服务器消息失败");
        return false;
    }
}
```

### 服务器消息处理

```csharp
// 注册来自客户端的消息监听器
var trigger = new Trigger<EventClientMessage>(OnClientMessageReceived);
trigger.Register(Game.Instance);

private static async Task<bool> OnClientMessageReceived(object sender, EventClientMessage eventArgs)
{
    var player = eventArgs.Player;
    var messageBytes = eventArgs.Message;
    
    // 验证玩家状态
    if (!player.IsOnline)
    {
        Game.Logger.LogWarning("收到离线玩家消息: {PlayerId}", player.Id);
        return false;
    }
    
    try
    {
        var json = Encoding.UTF8.GetString(messageBytes);
        var playerAction = JsonSerializer.Deserialize<PlayerAction>(json);
        
        // 处理玩家操作
        var result = ProcessPlayerAction(player, playerAction);
        
        // 发送处理结果回客户端
        if (!result.Success)
        {
            var errorMessage = new ProtoCustomMessage 
            { 
                Message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Error = result.Message })) 
            };
            errorMessage.SendTo(player);
        }
        
        Game.Logger.LogInformation("处理玩家 {PlayerId} 操作: {Action}", player.Id, playerAction.Action);
        return true;
    }
    catch (Exception ex)
    {
        Game.Logger.LogError(ex, "处理客户端消息失败");
        return false;
    }
}
```

## 高级功能

### 条件广播

```csharp
// 只向高等级玩家广播特殊事件
var specialEvent = new { Type = "BossEvent", BossId = 123 };
var message = new ProtoCustomMessage 
{ 
    Message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(specialEvent)) 
};

// 使用过滤器限制接收范围
message.Broadcast(player => player.Level >= 10);

// 向指定区域的玩家广播
var eventPosition = new Vector3(100, 0, 200);
message.Broadcast(player => Vector3.Distance(player.Position, eventPosition) < 50f);
```

### 消息优先级和可靠性

```csharp
// 对于重要消息，可以添加确认机制
public class ReliableMessageSender
{
    private readonly Dictionary<string, PendingMessage> _pendingMessages = new();
    
    public async Task<bool> SendReliableMessage(Player player, object data, TimeSpan timeout)
    {
        var messageId = Guid.NewGuid().ToString();
        var envelope = new 
        { 
            Id = messageId, 
            Type = "ReliableMessage", 
            Data = data,
            RequireAck = true
        };
        
        var message = new ProtoCustomMessage 
        { 
            Message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope)) 
        };
        
        // 记录待确认消息
        var tcs = new TaskCompletionSource<bool>();
        _pendingMessages[messageId] = new PendingMessage(tcs, DateTime.UtcNow.Add(timeout));
        
        if (message.SendTo(player))
        {
            // 等待确认或超时
            return await tcs.Task;
        }
        
        _pendingMessages.Remove(messageId);
        return false;
    }
    
    public void HandleAcknowledgment(string messageId)
    {
        if (_pendingMessages.TryGetValue(messageId, out var pending))
        {
            pending.TaskCompletionSource.SetResult(true);
            _pendingMessages.Remove(messageId);
        }
    }
}
```

### 消息类型识别

```csharp
// 推荐的消息格式设计
public class MessageEnvelope
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public object Data { get; set; } = null!;
}

// 使用示例
public static void SendTypedMessage<T>(string messageType, T data) where T : class
{
    var envelope = new MessageEnvelope
    {
        Type = messageType,
        Data = data
    };
    
    var message = new ProtoCustomMessage 
    { 
        Message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope)) 
    };
    
    message.SendToServer();
}

// 接收时的类型识别
private static MessageEnvelope? ParseMessage(byte[] messageBytes)
{
    try
    {
        var json = Encoding.UTF8.GetString(messageBytes);
        return JsonSerializer.Deserialize<MessageEnvelope>(json);
    }
    catch (Exception ex)
    {
        Game.Logger.LogError(ex, "消息解析失败");
        return null;
    }
}
```

## 序列化格式选择

### JSON 格式（推荐用于调试和开发）

```csharp
// 优点：易于调试、跨平台兼容性好
// 缺点：体积较大、性能相对较低

var data = new { PlayerId = 123, Action = "Attack", Target = 456 };
var json = JsonSerializer.Serialize(data);
var message = new ProtoCustomMessage 
{ 
    Message = Encoding.UTF8.GetBytes(json) 
};
```

### MessagePack 格式（推荐用于生产环境）

```csharp
// 优点：高性能、体积小
// 缺点：需要额外依赖、调试相对困难

[MessagePackObject]
public class PlayerAction
{
    [Key(0)]
    public int PlayerId { get; set; }
    
    [Key(1)]
    public string Action { get; set; } = string.Empty;
    
    [Key(2)]
    public int Target { get; set; }
}

// 序列化
var action = new PlayerAction { PlayerId = 123, Action = "Attack", Target = 456 };
var bytes = MessagePackSerializer.Serialize(action);
var message = new ProtoCustomMessage { Message = bytes };

// 反序列化
var deserializedAction = MessagePackSerializer.Deserialize<PlayerAction>(messageBytes);
```

### 自定义二进制格式（用于性能关键场景）

```csharp
// 用于高频、小数据量的消息
public class BinaryMessageBuilder
{
    private readonly List<byte> _buffer = new();
    
    public BinaryMessageBuilder WriteInt(int value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
        return this;
    }
    
    public BinaryMessageBuilder WriteFloat(float value)
    {
        _buffer.AddRange(BitConverter.GetBytes(value));
        return this;
    }
    
    public BinaryMessageBuilder WriteString(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteInt(bytes.Length);
        _buffer.AddRange(bytes);
        return this;
    }
    
    public byte[] Build() => _buffer.ToArray();
}

// 使用示例
var messageBytes = new BinaryMessageBuilder()
    .WriteInt(1) // 消息类型
    .WriteInt(123) // 玩家ID
    .WriteFloat(10.5f) // X坐标
    .WriteFloat(20.3f) // Y坐标
    .Build();

var message = new ProtoCustomMessage { Message = messageBytes };
```

## 最佳实践

### 1. 消息大小控制

```csharp
// ✅ 推荐：控制消息大小，避免网络压力
private const int MAX_MESSAGE_SIZE = 64 * 1024; // 64KB

public static bool ValidateMessageSize(byte[] message)
{
    if (message.Length > MAX_MESSAGE_SIZE)
    {
        Game.Logger.LogWarning("消息过大: {Size} bytes，最大允许: {MaxSize} bytes", 
            message.Length, MAX_MESSAGE_SIZE);
        return false;
    }
    return true;
}

// 使用前验证
if (ValidateMessageSize(messageBytes))
{
    var message = new ProtoCustomMessage { Message = messageBytes };
    message.SendToServer();
}
```

### 2. 错误处理和重试机制

```csharp
// ✅ 推荐：实现重试机制
public static async Task<bool> SendWithRetry(ProtoCustomMessage message, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        if (message.SendToServer())
        {
            return true;
        }
        
        Game.Logger.LogWarning("消息发送失败，正在重试 {Retry}/{MaxRetries}", i + 1, maxRetries);
        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // 指数退避
    }
    
    Game.Logger.LogError("消息发送失败，已达到最大重试次数");
    return false;
}
```

### 3. 消息频率控制

```csharp
// ✅ 推荐：避免消息泛滥
public class MessageRateLimiter
{
    private readonly Dictionary<string, Queue<DateTime>> _messageTimes = new();
    private readonly TimeSpan _timeWindow = TimeSpan.FromSeconds(1);
    private readonly int _maxMessagesPerWindow = 10;
    
    public bool CanSendMessage(string messageType)
    {
        var now = DateTime.UtcNow;
        
        if (!_messageTimes.TryGetValue(messageType, out var times))
        {
            times = new Queue<DateTime>();
            _messageTimes[messageType] = times;
        }
        
        // 清理过期记录
        while (times.Count > 0 && now - times.Peek() > _timeWindow)
        {
            times.Dequeue();
        }
        
        if (times.Count >= _maxMessagesPerWindow)
        {
            Game.Logger.LogWarning("消息频率过高: {MessageType}", messageType);
            return false;
        }
        
        times.Enqueue(now);
        return true;
    }
}

// 全局实例
private static readonly MessageRateLimiter RateLimiter = new();

// 发送前检查
if (RateLimiter.CanSendMessage("PlayerMove"))
{
    message.SendToServer();
}
```

### 4. 消息版本兼容性

```csharp
// ✅ 推荐：考虑版本兼容性
public class VersionedMessage
{
    public int Version { get; set; } = 1;
    public string Type { get; set; } = string.Empty;
    public object Data { get; set; } = null!;
}

// 处理不同版本的消息
private static bool HandleVersionedMessage(byte[] messageBytes)
{
    try
    {
        var json = Encoding.UTF8.GetString(messageBytes);
        var versionedMessage = JsonSerializer.Deserialize<VersionedMessage>(json);
        
        return versionedMessage?.Version switch
        {
            1 => HandleV1Message(versionedMessage),
            2 => HandleV2Message(versionedMessage),
            _ => HandleUnknownVersion(versionedMessage?.Version ?? 0)
        };
    }
    catch (Exception ex)
    {
        Game.Logger.LogError(ex, "版本化消息处理失败");
        return false;
    }
}
```

### 5. 安全考虑

```csharp
// ✅ 推荐：验证消息来源和内容
private static bool ValidateClientMessage(Player player, byte[] messageBytes)
{
    // 1. 验证玩家状态
    if (!player.IsOnline || player.Status != PlayerStatus.Connected)
    {
        Game.Logger.LogWarning("收到无效状态玩家消息: {PlayerId}, 状态: {Status}", 
            player.Id, player.Status);
        return false;
    }
    
    // 2. 验证消息大小
    if (messageBytes.Length > MAX_MESSAGE_SIZE)
    {
        Game.Logger.LogWarning("玩家 {PlayerId} 发送过大消息: {Size} bytes", 
            player.Id, messageBytes.Length);
        return false;
    }
    
    // 3. 验证消息格式
    try
    {
        var json = Encoding.UTF8.GetString(messageBytes);
        JsonDocument.Parse(json); // 验证JSON格式
        return true;
    }
    catch (JsonException)
    {
        Game.Logger.LogWarning("玩家 {PlayerId} 发送无效JSON消息", player.Id);
        return false;
    }
}
```

## 性能优化

### 1. 对象池使用

```csharp
// ✅ 推荐：重用消息对象减少GC压力
public static class MessagePool
{
    private static readonly ConcurrentQueue<byte[]> ByteArrayPool = new();
    
    public static byte[] RentByteArray(int minSize)
    {
        if (ByteArrayPool.TryDequeue(out var array) && array.Length >= minSize)
        {
            return array;
        }
        return new byte[Math.Max(minSize, 1024)];
    }
    
    public static void ReturnByteArray(byte[] array)
    {
        if (array.Length <= 64 * 1024) // 只回收较小的数组
        {
            ByteArrayPool.Enqueue(array);
        }
    }
}
```

### 2. 批量消息处理

```csharp
// 对于高频消息，考虑批量处理
public class BatchMessageProcessor
{
    private readonly List<ProtoCustomMessage> _batch = new();
    private readonly Timer _flushTimer;
    
    public BatchMessageProcessor()
    {
        _flushTimer = new Timer(FlushBatch, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
    }
    
    public void AddMessage(ProtoCustomMessage message)
    {
        lock (_batch)
        {
            _batch.Add(message);
            
            if (_batch.Count >= 10) // 达到批量大小时立即发送
            {
                FlushBatch(null);
            }
        }
    }
    
    private void FlushBatch(object? state)
    {
        List<ProtoCustomMessage> toProcess;
        
        lock (_batch)
        {
            if (_batch.Count == 0) return;
            
            toProcess = new List<ProtoCustomMessage>(_batch);
            _batch.Clear();
        }
        
        // 批量处理消息
        ProcessBatch(toProcess);
    }
}
```

## 故障排除

### 常见问题及解决方案

#### 1. 消息发送失败

**症状**：`SendToServer()` 返回 `false`
**可能原因**：
- 网络连接断开
- 消息过大
- 客户端未正确初始化

**解决方案**：
```csharp
// 检查网络状态
if (!Game.Instance.IsConnected)
{
    Game.Logger.LogWarning("网络未连接，无法发送消息");
    return false;
}

// 检查消息大小
if (messageBytes.Length > MAX_MESSAGE_SIZE)
{
    Game.Logger.LogError("消息过大: {Size} bytes", messageBytes.Length);
    return false;
}
```

#### 2. 消息处理器未触发

**症状**：发送消息后接收方的事件处理器未被调用
**可能原因**：
- 事件监听器未正确注册
- 消息格式错误
- 异常被静默吞没

**解决方案**：
```csharp
// 确保在正确的时机注册监听器
private static void RegisterMessageHandlers()
{
    var trigger = new Trigger<EventServerMessage>(OnServerMessage);
    trigger.Register(Game.Instance);
    
    Game.Logger.LogInformation("消息监听器注册成功");
}

// 添加异常处理
private static async Task<bool> OnServerMessage(object sender, EventServerMessage eventArgs)
{
    try
    {
        // 消息处理逻辑
        return true;
    }
    catch (Exception ex)
    {
        Game.Logger.LogError(ex, "消息处理异常");
        return false;
    }
}
```

#### 3. 内存泄漏

**症状**：长时间运行后内存使用持续增长
**可能原因**：
- 大量消息对象未释放
- 事件监听器未正确注销
- 循环引用

**解决方案**：
```csharp
// 正确注销事件监听器
public class MessageHandler : IDisposable
{
    private readonly Trigger<EventServerMessage> _trigger;
    
    public MessageHandler()
    {
        _trigger = new Trigger<EventServerMessage>(OnMessage);
        _trigger.Register(Game.Instance);
    }
    
    public void Dispose()
    {
        _trigger?.Unregister();
    }
}
```

## 完整示例

### 聊天系统实现

```csharp
// 消息数据结构
public class ChatMessage
{
    public string Type { get; set; } = "Chat";
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ChatChannel Channel { get; set; } = ChatChannel.Global;
}

public enum ChatChannel
{
    Global,
    Team,
    Private,
    System
}

// 客户端聊天管理器
public class ClientChatManager
{
    private static ClientChatManager? _instance;
    public static ClientChatManager Instance => _instance ??= new ClientChatManager();
    
    private ClientChatManager()
    {
        RegisterMessageHandler();
    }
    
    private void RegisterMessageHandler()
    {
        var trigger = new Trigger<EventServerMessage>(OnChatMessageReceived);
        trigger.Register(Game.Instance);
    }
    
    public void SendMessage(string content, ChatChannel channel = ChatChannel.Global)
    {
        var chatMessage = new ChatMessage
        {
            PlayerId = Game.LocalPlayer.Id.ToString(),
            PlayerName = Game.LocalPlayer.User?.Name,
            Content = content,
            Channel = channel
        };
        
        var json = JsonSerializer.Serialize(chatMessage);
        var message = new ProtoCustomMessage 
        { 
            Message = Encoding.UTF8.GetBytes(json) 
        };
        
        if (message.SendToServer())
        {
            Game.Logger.LogInformation("聊天消息发送成功");
        }
        else
        {
            Game.Logger.LogWarning("聊天消息发送失败");
        }
    }
    
    private static async Task<bool> OnChatMessageReceived(object sender, EventServerMessage eventArgs)
    {
        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Message);
            var chatMessage = JsonSerializer.Deserialize<ChatMessage>(json);
            
            if (chatMessage?.Type != "Chat") return false;
            
            // 显示聊天消息
            DisplayChatMessage(chatMessage);
            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "处理聊天消息失败");
            return false;
        }
    }
    
    private static void DisplayChatMessage(ChatMessage message)
    {
        var channelPrefix = message.Channel switch
        {
            ChatChannel.Global => "[全局]",
            ChatChannel.Team => "[队伍]",
            ChatChannel.Private => "[私聊]",
            ChatChannel.System => "[系统]",
            _ => ""
        };
        
        Game.Logger.LogInformation("{Channel} {PlayerName}: {Content}", 
            channelPrefix, message.PlayerName, message.Content);
    }
}

// 服务器聊天管理器
public class ServerChatManager
{
    private static ServerChatManager? _instance;
    public static ServerChatManager Instance => _instance ??= new ServerChatManager();
    
    private ServerChatManager()
    {
        RegisterMessageHandler();
    }
    
    private void RegisterMessageHandler()
    {
        var trigger = new Trigger<EventClientMessage>(OnChatMessageReceived);
        trigger.Register(Game.Instance);
    }
    
    private static async Task<bool> OnChatMessageReceived(object sender, EventClientMessage eventArgs)
    {
        var player = eventArgs.Player;
        
        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Message);
            var chatMessage = JsonSerializer.Deserialize<ChatMessage>(json);
            
            if (chatMessage?.Type != "Chat") return false;
            
            // 验证消息内容
            if (string.IsNullOrWhiteSpace(chatMessage.Content) || 
                chatMessage.Content.Length > 200)
            {
                Game.Logger.LogWarning("玩家 {PlayerId} 发送无效聊天消息", player.Id);
                return false;
            }
            
            // 设置服务器信息
            chatMessage.PlayerId = player.Id.ToString();
            chatMessage.PlayerName = player.Name;
            chatMessage.Timestamp = DateTime.UtcNow;
            
            // 广播消息
            BroadcastChatMessage(chatMessage);
            
            Game.Logger.LogInformation("处理聊天消息: {PlayerName} -> {Content}", 
                player.Name, chatMessage.Content);
            return true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "处理客户端聊天消息失败");
            return false;
        }
    }
    
    private static void BroadcastChatMessage(ChatMessage chatMessage)
    {
        var json = JsonSerializer.Serialize(chatMessage);
        var message = new ProtoCustomMessage 
        { 
            Message = Encoding.UTF8.GetBytes(json) 
        };
        
        // 根据频道类型选择广播范围
        switch (chatMessage.Channel)
        {
            case ChatChannel.Global:
                message.Broadcast();
                break;
                
            case ChatChannel.Team:
                // 只向同队玩家广播
                var senderId = int.Parse(chatMessage.PlayerId);
                var sender = Player.GetById(senderId);
                if (sender != null)
                {
                    message.Broadcast(p => p.TeamId == sender.TeamId);
                }
                break;
                
            // 其他频道类型的处理...
        }
    }
}
```

## 框架通信机制对比

WasiCore 框架提供了两种主要的客户端-服务器通信机制，每种都有其特定的用途和优势：

### 机制对比表

| 特性 | ProtoCustomMessage (即时消息) | 属性同步 (Property Sync) |
|------|---------------------------|------------------------|
| **消息类型** | 即时、瞬态消息 | 状态同步、持久化数据 |
| **传输时机** | 立即发送 | 属性变化时自动同步 |
| **可靠性** | 必达，但不保证重连后重发 | 支持断线重连后状态恢复 |
| **连接要求** | 客户端必须在线 | 支持离线状态恢复 |
| **数据格式** | 自定义字节数组 | 强类型属性 |
| **同步范围** | 广播/定向发送 | 基于可见性和SyncType |
| **性能开销** | 较低（仅传输） | 中等（自动管理） |
| **使用复杂度** | 需要手动序列化/反序列化 | 框架自动处理 |

### 即时消息机制 (ProtoCustomMessage)

**适用场景：**
- 聊天消息、通知
- 临时事件触发
- 实时交互反馈
- 自定义通信协议

**特点：**
- ✅ 即时传递，延迟最低
- ✅ 支持任意数据格式
- ✅ 灵活的路由策略
- ❌ 断线重连后消息丢失
- ❌ 需要手动处理序列化

**代码示例：**
```csharp
// 服务器发送即时通知
var notification = new { Type = "Achievement", Name = "第一滴血", Reward = 100 };
var message = new ProtoCustomMessage 
{ 
    Message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification)) 
};
message.Broadcast(player => player.Level >= 5); // 仅向5级以上玩家广播
```

### 属性同步机制 (Property Sync)

**适用场景：**
- 单位属性（生命值、等级、位置）
- 游戏状态（波数、时间、分数）
- 技能属性、Buff状态
- 组件数据同步

**特点：**
- ✅ 自动状态恢复
- ✅ 强类型安全
- ✅ 基于可见性的智能同步
- ✅ 支持复杂的数值公式
- ❌ 仅支持预定义属性类型
- ❌ 需要框架级别的配置

**代码示例：**
```csharp
// 服务器设置单位属性 - 自动同步到可见的客户端
unit.SetProperty(PropertyUnit.Health, 80.0f);
unit.SetProperty(PropertyUnit.Level, 15);

// 设置游戏属性 - 同步给所有玩家
Game.Instance.SetProperty(PropertyGame.CurrentWave, 10);

// 配置同步范围
unit.SetPropertySyncType(PropertyUnit.Experience, SyncType.Self); // 仅同步给所有者
unit.SetPropertySyncType(PropertyUnit.Name, SyncType.Sight);     // 视野内可见
```

### SyncType 同步类型详解

```csharp
public enum SyncType
{
    None = 0,           // 不同步
    Self = 1 << 0,      // 仅所有者可见
    Ally = 1 << 1,      // 盟友可见
    All = 1 << 2,       // 全局可见
    Sight = 1 << 3,     // 视野内可见
    SelfOrSight = 1 << 4,   // 所有者或视野内
    AllyOrSight = 1 << 5,   // 盟友或视野内
}
```

**同步类型使用指南：**

| 同步类型 | 使用场景 | 示例 |
|---------|---------|------|
| `None` | 服务器专用数据 | 内部计算状态 |
| `Self` | 个人隐私信息 | 经验值、金币 |
| `Ally` | 团队共享信息 | 队友状态 |
| `All` | 全局信息 | 排行榜、公告 |
| `Sight` | 常规游戏对象 | 单位生命值、位置 |
| `SelfOrSight` | 重要对象 | 主角技能冷却 |
| `AllyOrSight` | 团队作战信息 | 队友技能状态 |

### 重连恢复机制

**属性同步的自动恢复：**
```csharp
// 客户端重连时自动触发的恢复流程
private void OnPlayerReconnected(Player player)
{
    // 1. 同步当前场景中所有可见单位的属性
    foreach (var entity in player.GetVisibleEntities())
    {
        SyncAllVisibleProperties(entity, player);
    }
    
    // 2. 同步游戏全局状态
    SyncGameProperties(player);
    
    // 3. 同步玩家个人状态
    SyncPlayerProperties(player);
}
```

**即时消息的重连处理：**
```csharp
// 即时消息需要主动处理重连情况
private void OnPlayerReconnected(Player player)
{
    // 发送当前状态总结消息
    var stateMessage = new ProtoCustomMessage
    {
        Message = CreateStateSummary(player)
    };
    stateMessage.SendTo(player);
    
    // 发送重要的历史消息（如果需要）
    foreach (var importantMessage in GetImportantMessages(player))
    {
        importantMessage.SendTo(player);
    }
}
```

## 通信机制最佳实践

### 1. 选择合适的通信机制

```csharp
// ✅ 推荐：根据数据性质选择机制

// 持久状态 → 使用属性同步
unit.SetProperty(PropertyUnit.Health, newHealth);
unit.SetProperty(PropertyUnit.Position, newPosition);

// 临时事件 → 使用即时消息
var chatMessage = new ProtoCustomMessage 
{ 
    Message = Encoding.UTF8.GetBytes($"玩家 {player.Name} 升级了！") 
};
chatMessage.Broadcast();

// 混合使用：重要状态用属性，通知用消息
unit.SetProperty(PropertyUnit.Level, newLevel);  // 状态同步
var levelUpNotification = new ProtoCustomMessage 
{ 
    Message = CreateLevelUpNotification(unit) 
};
levelUpNotification.SendTo(unit.Owner);  // 即时通知
```

### 2. 属性同步配置最佳实践

```csharp
// ✅ 推荐：合理配置同步范围
public class PlayerDataConfiguration
{
    public static void ConfigureUnitProperties()
    {
        // 个人信息：仅所有者
        SetPropertySyncType(PropertyUnit.Experience, SyncType.Self);
        SetPropertySyncType(PropertyUnit.Gold, SyncType.Self);
        
        // 公开信息：视野内可见
        SetPropertySyncType(PropertyUnit.Health, SyncType.Sight);
        SetPropertySyncType(PropertyUnit.Level, SyncType.Sight);
        
        // 团队信息：盟友可见
        SetPropertySyncType(PropertyUnit.TeamRole, SyncType.Ally);
        
        // 全局信息：所有人可见
        SetPropertySyncType(PropertyGame.CurrentWave, SyncType.All);
    }
}
```

### 3. 处理网络中断和重连

```csharp
// ✅ 推荐：设计容错的状态同步
public class RobustStateManager
{
    // 为重要状态提供冗余机制
    public void UpdatePlayerLevel(Player player, int newLevel)
    {
        // 1. 属性同步（主要机制）
        player.SetProperty(PropertyPlayer.Level, newLevel);
        
        // 2. 即时消息（冗余通知）
        var notification = new ProtoCustomMessage
        {
            Message = CreateLevelUpdateMessage(player.Id, newLevel)
        };
        notification.SendTo(player);
        
        // 3. 记录重要变更（用于重连恢复）
        RecordImportantStateChange(player, "LevelUp", newLevel);
    }
    
    private void RecordImportantStateChange(Player player, string changeType, object newValue)
    {
        // 在重连时可以发送这些重要变更的通知
        player.ImportantChanges.Add(new StateChange 
        { 
            Type = changeType, 
            Value = newValue, 
            Timestamp = DateTime.UtcNow 
        });
    }
}
```

### 4. 性能优化策略

```csharp
// ✅ 推荐：批量属性更新
public class BatchPropertyUpdater
{
    public void UpdateMultipleProperties(Unit unit, Dictionary<PropertyUnit, object> updates)
    {
        // 暂停同步以避免多次网络传输
        unit.BeginPropertyUpdate();
        
        try
        {
            foreach (var (property, value) in updates)
            {
                unit.SetPropertyInternal(property, value);
            }
        }
        finally
        {
            // 恢复同步，一次性发送所有变更
            unit.EndPropertyUpdate();
        }
    }
}

// ✅ 推荐：基于优先级的消息发送
public class PriorityMessageSender
{
    private readonly Queue<ProtoCustomMessage> _highPriorityQueue = new();
    private readonly Queue<ProtoCustomMessage> _normalPriorityQueue = new();
    
    public void ProcessMessageQueue()
    {
        // 优先处理高优先级消息
        while (_highPriorityQueue.Count > 0 && CanSendMessage())
        {
            var message = _highPriorityQueue.Dequeue();
            message.Broadcast();
        }
        
        // 然后处理普通消息
        while (_normalPriorityQueue.Count > 0 && CanSendMessage())
        {
            var message = _normalPriorityQueue.Dequeue();
            message.Broadcast();
        }
    }
}
```

### 5. 调试和监控

```csharp
// ✅ 推荐：添加详细的调试信息
public class CommunicationDebugger
{
    public static void LogPropertyChange(string entityType, string propertyName, object oldValue, object newValue, SyncType syncType)
    {
        Game.Logger.LogDebug("属性同步: {EntityType}.{PropertyName} {OldValue} → {NewValue} (SyncType: {SyncType})", 
            entityType, propertyName, oldValue, newValue, syncType);
    }
    
    public static void LogMessageSent(string messageType, int recipientCount, int messageSize)
    {
        Game.Logger.LogDebug("即时消息: {MessageType} → {RecipientCount} 个接收者 ({MessageSize} bytes)", 
            messageType, recipientCount, messageSize);
    }
    
    public static void LogReconnectionRecovery(Player player, int propertiesSynced, int messagesSent)
    {
        Game.Logger.LogInformation("重连恢复: 玩家 {PlayerId} 同步了 {PropertiesSynced} 个属性，发送了 {MessagesSent} 条消息", 
            player.Id, propertiesSynced, messagesSent);
    }
}
```

### 6. 安全性考虑

```csharp
// ✅ 推荐：验证属性访问权限
public class PropertyAccessValidator
{
    public static bool CanPlayerAccessProperty(Player player, Unit unit, PropertyUnit property)
    {
        var syncType = GetPropertySyncType(property);
        
        return syncType switch
        {
            SyncType.None => false,
            SyncType.Self => unit.Owner == player,
            SyncType.Ally => unit.Owner.Team == player.Team,
            SyncType.All => true,
            SyncType.Sight => player.CanSee(unit),
            SyncType.SelfOrSight => unit.Owner == player || player.CanSee(unit),
            SyncType.AllyOrSight => unit.Owner.Team == player.Team || player.CanSee(unit),
            _ => false
        };
    }
}

// ✅ 推荐：验证即时消息内容
public class MessageContentValidator
{
    public static bool ValidateMessage(Player sender, byte[] messageContent)
    {
        // 1. 检查消息大小
        if (messageContent.Length > MAX_MESSAGE_SIZE)
        {
            Game.Logger.LogWarning("玩家 {PlayerId} 发送过大消息", sender.Id);
            return false;
        }
        
        // 2. 检查发送频率
        if (!RateLimiter.CanSendMessage(sender, "CustomMessage"))
        {
            Game.Logger.LogWarning("玩家 {PlayerId} 消息发送过于频繁", sender.Id);
            return false;
        }
        
        // 3. 验证消息格式
        if (!IsValidMessageFormat(messageContent))
        {
            Game.Logger.LogWarning("玩家 {PlayerId} 发送无效格式消息", sender.Id);
            return false;
        }
        
        return true;
    }
}
```

## 计时器使用规范

在TypedMessage系统和其他消息处理组件中，如果需要使用计时器功能，请使用框架内置的计时器而不是System.Threading.Timer：

### 使用GameCore.Timers.Timer

```csharp
using GameCore.Timers;

// 创建计时器
var timer = new Timer(1000); // 1秒间隔
timer.Elapsed += (sender, e) => {
    // 计时器处理逻辑
    ProcessTimerEvent();
};
timer.AutoReset = true; // 自动重复
timer.Start();

// 停止计时器
timer.Stop();

// 销毁计时器
timer.Dispose();
```

### 为什么使用框架计时器

- **游戏时间同步**：与游戏主循环保持同步
- **性能优化**：避免与System.Threading.Timer的冲突
- **调试友好**：集成游戏日志系统
- **资源管理**：自动处理生命周期

### 在消息系统中的应用

```csharp
public class MessageRetryManager
{
    private readonly Timer _retryTimer;
    
    public MessageRetryManager()
    {
        _retryTimer = new Timer(5000); // 5秒重试间隔
        _retryTimer.Elapsed += OnRetryTimerElapsed;
        _retryTimer.AutoReset = true;
        _retryTimer.Start();
    }
    
    private void OnRetryTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        // 处理消息重试逻辑
        ProcessPendingRetries();
    }
    
    public void Dispose()
    {
        _retryTimer?.Stop();
        _retryTimer?.Dispose();
    }
}
```

## 相关文档

- [单位属性系统文档](../systems/UnitPropertySystem.md) - 详细了解属性同步机制
- [事件系统文档](../systems/EventSystem.md) - 了解底层事件机制
- [网络协议文档](../systems/NetworkProtocol.md) - 深入了解网络传输层
- [日志系统文档](../systems/LoggingSystem.md) - 消息调试和日志记录
- [最佳实践](../best-practices/AsyncProgramming.md) - 异步编程最佳实践
- [性能优化指南](../best-practices/PerformanceOptimization.md) - 性能优化建议 