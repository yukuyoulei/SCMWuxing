# UI属性系统文档

## 概述

UI属性系统是WasiCore框架的扩展功能，专为处理客户端UI状态同步而设计。它在保持现有`PropertyPlayer`安全性的同时，提供了一种安全的方式让客户端设置和同步非关键的UI状态数据。

## 🎯 设计目标

### 核心问题
现有的属性系统(`PropertyPlayer`)只允许服务端设置属性，这确保了游戏逻辑的安全性。但对于UI状态（如面板折叠状态、用户偏好设置等），客户端需要能够设置这些属性，并享受属性系统的断线重连恢复功能。

### 设计原则
1. **安全第一**：严格区分游戏逻辑属性和UI属性
2. **向后兼容**：不影响现有`PropertyPlayer`系统
3. **可扩展性**：支持新的UI属性类型
4. **性能优化**：批量操作和合理的验证机制

## 🏗️ 架构设计

### 分层防护架构

```
┌─────────────────────────────────────────────────────────────┐
│                    客户端层 (CLIENT)                        │
├─────────────────────────────────────────────────────────────┤
│ UI组件 → SetUIPropertyAsync() → 消息系统 → 发送到服务端      │
└─────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────┐
│                    传输层 (Network)                         │
├─────────────────────────────────────────────────────────────┤
│        ProtoCustomMessage + TypedMessage包装                │
└─────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────┐
│                    服务端验证层 (SERVER)                    │
├─────────────────────────────────────────────────────────────┤
│ 1. 属性权限检查 (IsUIPropertyClientSettable)               │
│ 2. 数据格式验证 (ValidateUIPropertyValue)                  │
│ 3. 大小和频率限制                                           │
└─────────────────────────────────────────────────────────────┘
                                ↓
┌─────────────────────────────────────────────────────────────┐
│                 属性存储和同步层 (Storage)                  │
├─────────────────────────────────────────────────────────────┤
│ PropertyPlayerUI → IPropertyOwner → 底层引擎 → 自动同步     │
└─────────────────────────────────────────────────────────────┘
```

### 核心组件

| 组件 | 责任 | 位置 |
|------|------|------|
| `PropertyPlayerUI` | 定义UI属性枚举 | `GameCore/PlayerAndUsers/PropertyPlayerUI.cs` |
| `Player.IPropertyOwnerUI` | 核心属性操作和服务端验证 | `GameCore/PlayerAndUsers/Player.IPropertyOwnerUI.cs` |
| `PlayerUIPropertyExtensions` | 客户端扩展方法 | `TriggerEncapsulation/UIProperty/PlayerUIPropertyExtensions.cs` |
| `UIPropertyMessageHandler` | 消息处理和验证 | `TriggerEncapsulation/UIProperty/UIPropertyMessageHandler.cs` |
| `EventPlayerUIPropertyChange` | 属性变更事件 | `GameCore/Event/PlayerUIEvents.cs` |

## 🚀 使用方法

### 客户端使用

```csharp
#if CLIENT
// 引用扩展命名空间
using TriggerEncapsulation.UIProperty;

// 设置单个UI属性
await player.SetUIPropertyAsync(PropertyPlayerUI.UIPanelCollapsed, true);

// 批量设置多个UI属性
var uiProps = new Dictionary<PropertyPlayerUI, object>
{
    { PropertyPlayerUI.ChatChannelPreference, 2 },
    { PropertyPlayerUI.UILayoutConfig, "compact" }
};
await player.SetUIPropertiesAsync(uiProps);

// 设置复杂对象（自动JSON序列化）
var keyBindings = new Dictionary<string, string> { {"Attack", "Space"} };
await player.SetUIPropertyJsonAsync(PropertyPlayerUI.KeyBindingSettings, keyBindings);

// 读取UI属性
var isCollapsed = player.GetUIPropertyAs<bool>(PropertyPlayerUI.UIPanelCollapsed);
var keyBindingsRead = player.GetUIPropertyFromJson<Dictionary<string, string>>(
    PropertyPlayerUI.KeyBindingSettings, new Dictionary<string, string>());
#endif
```

### 初始化和服务端使用

```csharp
// 在游戏启动时初始化UI属性系统
using TriggerEncapsulation.UIProperty;

public void InitializeGame()
{
    // 初始化UI属性消息处理器
    UIPropertyMessageHandler.Initialize();
}

#if SERVER
// 监听UI属性变更
var trigger = new Trigger<EventPlayerUIPropertyChange>(OnUIPropertyChanged);
trigger.Register(Game.Instance);

// 为新玩家设置默认UI属性
player.SetUIPropertyInternal(PropertyPlayerUI.UIPanelCollapsed, false);

private static async Task<bool> OnUIPropertyChanged(object sender, EventPlayerUIPropertyChange e)
{
    Game.Logger.LogInformation("Player {Id} changed UI property {Property}", 
        e.Player.Id, e.Property);
    return true;
}
#endif
```

## 🔒 安全机制

### 1. 白名单权限控制

```csharp
private static bool IsUIPropertyClientSettable(PropertyPlayerUI property)
{
    return property switch
    {
        PropertyPlayerUI.UIPanelCollapsed => true,
        PropertyPlayerUI.ChatChannelPreference => true,
        PropertyPlayerUI.KeyBindingSettings => true,
        PropertyPlayerUI.UILayoutConfig => true,
        _ => false // 默认不允许客户端设置
    };
}
```

### 2. 数据验证机制

```csharp
private static bool ValidateUIPropertyValue<TValue>(PropertyPlayerUI property, TValue value)
{
    return property switch
    {
        PropertyPlayerUI.UIPanelCollapsed => value is bool,
        PropertyPlayerUI.ChatChannelPreference => value is int && (int)(object)value >= 0 && (int)(object)value < 10,
        PropertyPlayerUI.KeyBindingSettings => ValidateKeyBindingSettings(value),
        PropertyPlayerUI.UILayoutConfig => ValidateUILayoutConfig(value),
        _ => false
    };
}
```

### 3. 大小和频率限制

- JSON字符串属性限制在10KB以内
- 布局配置限制在5KB以内
- 可以扩展添加频率限制机制

### 4. 严格的数据类型检查

- 使用强类型枚举定义属性
- 运行时类型验证
- JSON序列化/反序列化安全检查

## ⚡ 性能优化

### 批量操作
```csharp
// ✅ 推荐：批量设置减少网络开销
await player.SetUIPropertiesAsync(multipleProperties);

// ❌ 避免：频繁的单个属性设置
foreach (var prop in properties)
{
    await player.SetUIPropertyAsync(prop.Key, prop.Value); // 不推荐
}
```

### 异步操作
所有客户端属性设置操作都是异步的，避免阻塞UI线程。

### 智能同步
利用现有属性系统的同步机制，只同步给需要的客户端（基于SyncType配置）。

## 🛡️ 安全最佳实践

### ✅ 安全使用

```csharp
// 存储UI相关的非敏感数据
await player.SetUIPropertyAsync(PropertyPlayerUI.UIPanelCollapsed, true);
await player.SetUIPropertyAsync(PropertyPlayerUI.ChatChannelPreference, 2);

// 存储用户偏好设置
var keyBindings = new Dictionary<string, string> { /* 快捷键配置 */ };
await player.SetUIPropertyAsync(PropertyPlayerUI.KeyBindingSettings, 
    JsonSerializer.Serialize(keyBindings));
```

### ❌ 危险使用

```csharp
// 永远不要在UI属性中存储游戏逻辑数据
await player.SetUIPropertyAsync(PropertyPlayerUI.UILayoutConfig, 
    JsonSerializer.Serialize(new { Gold = 1000, Level = 10 })); // 错误！

// 不要基于UI属性做游戏逻辑判断
#if SERVER
var isCollapsed = player.GetUIProperty<bool>(PropertyPlayerUI.UIPanelCollapsed);
if (isCollapsed)
{
    player.SetProperty(PropertyPlayer.Gold, 1000); // 错误！
}
#endif
```

### 🎯 推荐场景

| ✅ 推荐使用 | ❌ 不推荐使用 |
|------------|--------------|
| UI面板状态 | 游戏货币 |
| 用户偏好设置 | 角色等级 |
| 界面布局配置 | 装备数据 |
| 快捷键绑定 | 技能冷却 |
| 聊天频道选择 | 位置坐标 |
| 音效设置 | 生命值 |

## 🔧 扩展新的UI属性

### 1. 添加枚举值

```csharp
// 在 PropertyPlayerUI.cs 中添加
public enum EPropertyPlayerUI
{
    // 现有属性...
    
    /// <summary>
    /// 新的UI属性 - 音效设置
    /// </summary>
    AudioSettings,
}
```

### 2. 更新权限控制

```csharp
private static bool IsUIPropertyClientSettable(PropertyPlayerUI property)
{
    return property switch
    {
        // 现有权限...
        PropertyPlayerUI.AudioSettings => true, // 新增
        _ => false
    };
}
```

### 3. 添加验证规则

```csharp
private static bool ValidateUIPropertyValue<TValue>(PropertyPlayerUI property, TValue value)
{
    return property switch
    {
        // 现有验证...
        PropertyPlayerUI.AudioSettings => ValidateAudioSettings(value), // 新增
        _ => false
    };
}

private static bool ValidateAudioSettings<TValue>(TValue value)
{
    // 实现具体的验证逻辑
    return value is string jsonString && jsonString.Length <= 1000;
}
```

## 🐛 故障排除

### 常见问题

1. **属性设置失败**
   - 检查属性是否在白名单中
   - 验证数据格式是否正确
   - 确认网络连接正常

2. **属性未同步**
   - 确认服务端已处理消息
   - 检查属性的SyncType配置
   - 验证客户端重连后的恢复逻辑

3. **性能问题**
   - 使用批量操作替代频繁的单个操作
   - 检查属性值大小是否超限
   - 监控网络消息频率

### 调试信息

```csharp
// 启用详细日志
Game.Logger.LogLevel = LogLevel.Debug;

// 监控UI属性变更
var trigger = new Trigger<EventPlayerUIPropertyChange>(async (sender, e) =>
{
    Game.Logger.LogInformation("UI Property Changed: Player={PlayerId}, Property={Property}", 
        e.Player.Id, e.Property);
    return true;
});
```

## 🔮 未来扩展

### 计划功能
1. **属性版本控制**：支持属性模式升级
2. **批量同步优化**：更智能的批量传输机制
3. **客户端缓存**：本地缓存机制减少网络请求
4. **压缩传输**：大属性值的压缩传输

### 兼容性保证
- UI属性系统完全独立于现有`PropertyPlayer`系统
- 可以逐步迁移现有UI状态管理代码
- 支持新老系统并存的过渡期

## 📚 相关文档

- [属性系统概述](PropertySystem.md)
- [消息系统文档](MessagingSystem.md)
- [安全最佳实践](../best-practices/Security.md)
- [性能优化指南](../best-practices/Performance.md) 