---
title: PhysicsGame Best Practices and Common Pitfalls
document_type: best-practices
priority: high
target_audience: all
topics:
  - best-practices
  - common-mistakes
  - debugging
  - performance
version: 1.0
last_updated: 2025-01-27
related_docs:
  - PhysicsGame_Quick_Start.md
  - PhysicsGame_API_Reference.md
  - PhysicsGame_Multiplayer_Guide.md
---

# PhysicsGame 最佳实践和常见陷阱

本文档总结了 PhysicsGame 框架开发中的常见陷阱、最佳实践和调试技巧。**强烈建议开发前阅读**。

---

## 目录

1. [常见陷阱与解决方案](#常见陷阱与解决方案)
2. [最佳实践总结](#最佳实践总结)

---

## 常见陷阱与解决方案

### 1. 单位创建陷阱

❌ **错误做法**：
```csharp
// 在客户端创建单位
#if CLIENT
public override void OnSetup()
{
    // 错误！客户端不应该创建单位
    CreatePlayerUnits();
}
#endif
```

✅ **正确做法**：
```csharp
#if SERVER
public override void OnSetup()
{
    // 正确！只在服务器创建单位
    CreatePlayerUnits();
}
#endif

#if CLIENT
public override void OnSetup()
{
    // 正确！客户端只做初始化
    InitializeClientExtensions();
}
#endif
```

**原因**：框架采用服务器权威架构，单位由服务器创建后自动同步到客户端。

### 2. 场景获取陷阱

❌ **错误做法**：
```csharp
// 使用错误的参数类型
scene = GameCore.SceneSystem.Scene.Get(MyGameMode); // 错误！
```

✅ **正确做法**：
```csharp
// 使用正确的 Scene 参数
scene = GameCore.SceneSystem.Scene.Get(MyGameScene); // 正确！
```

**原因**：`Scene.Get()` 的参数类型是 `GameLink<GameDataScene, GameDataScene>`，必须使用场景配置。

### 3. 主控单位设置陷阱

❌ **错误做法**：
```csharp
player.SetMainUnit(unit); // 错误！不存在这个方法
```

✅ **正确做法**：
```csharp
player.MainUnit = unit; // 正确！直接赋值属性
```

**原因**：`MainUnit` 是属性，不是方法。

### 4. 物理查询性能陷阱

❌ **错误做法**：
```csharp
// 低效的距离计算 - O(N) 复杂度
foreach (var obj in allObjects)
{
    float distance = Vector3.Distance(playerPos, obj.position);
    if (distance < radius) { /* 处理 */ }
}
```

✅ **正确做法**：
```csharp
// 高效的空间查询 - O(log N) 复杂度
RigidBody[] nearbyObjects = world.GetRigidBodies(playerPos, radius);
foreach (var rigidBody in nearbyObjects)
{
    // 直接处理查询结果
}
```

**原因**：物理引擎使用空间分区（八叉树/网格），查询效率远高于手动遍历。

### 5. 组件生命周期陷阱

❌ **错误做法**：
```csharp
public override void OnStart()
{
    // 错误！在 OnStart 中获取可能还未初始化的系统引用
    world = player.MainUnit.GetOwnerPhysicsWorld(); // 可能为 null
}
```

✅ **正确做法**：
```csharp
public override void OnDelayedStart()
{
    // 正确！在 DelayedStart 中获取系统引用
    world = player.MainUnit.GetOwnerPhysicsWorld();
}
```

**原因**：`OnStart` 阶段某些系统可能未完全初始化，`OnDelayedStart` 确保所有依赖系统已就绪。

### 6. 事件监听陷阱

❌ **错误做法**：
```csharp
// 在组件中直接访问可能不存在的玩家
var player = Player.GetById(1);
var unit = player.MainUnit; // 可能抛出 NullReferenceException
```

✅ **正确做法**：
```csharp
// 使用事件监听处理动态变化
Trigger<EventPlayerMainUnitChanged> trigger = new(async (s, e) =>
{
    Player player = e.Player;
    Unit? unit = e.Unit;

    if (player != null && unit != null)
    {
        // 安全地处理主控单位变化
    }
    return true;
});
trigger.Register(Game.Instance);
```

**原因**：网络游戏中，玩家和单位的存在是动态的，事件驱动确保在正确时机处理。

### 7. 碰撞过滤器陷阱

❌ **错误做法**：
```csharp
rigidBody.SetCollisionFilter((other, point) =>
{
    // 错误！在过滤函数中修改物理属性
    other.ApplyForce(new Vector3(0, 0, 100));
    return true;
});
```

✅ **正确做法**：
```csharp
rigidBody.SetCollisionFilter((other, point) =>
{
    // 正确！只做判断，不修改物理属性
    if (other.GetCollisionLayer() == 4u)
    {
        return IsPointInSpecialArea(point);
    }
    return false;
});
```

**原因**：过滤函数在物理引擎计算过程中被调用，修改物理属性会导致不可预测的行为。

### 8. PhysicsActor 陷阱

❌ **错误做法**：
```csharp
// PhysicsActor 并不是 node 的组件，这是一个单向引用关系
// 只能通过 actor.GetNode() 获取 node
// 没有提供任何 node 的接口可以获取他所属的 PhysicsActor!!!!
var actor = node.GetComponent<PhysicsActor>(); // 错误！
```

**原因**：`PhysicsActor` 不是组件，无法通过 `GetComponent` 获取。

### 9. Vector3 方法陷阱

❌ **错误做法**：
```csharp
Vector3 direction = (target - source).normalized; // normalized 不存在！
```

✅ **正确做法**：
```csharp
Vector3 direction = Vector3.Normalize(target - source);
```

**原因**：C# 的 `System.Numerics.Vector3` 不支持 `normalized` 属性，需要使用静态方法 `Vector3.Normalize()`。

### 10. RigidBody 创建陷阱

❌ **错误做法**：
```csharp
RigidBody rb = node.CreateComponent<RigidBody>(); // 不要手动创建！
```

✅ **正确做法**：
```csharp
RigidBody rb = node.GetComponent<RigidBody>(); // 使用已存在的组件
```

**原因**：通过 `PhysicsActor` 创建的 Node 已经自动创建好 `RigidBody`，手动创建会导致冲突。

### 11. 组件中访问 Node 陷阱

❌ **错误做法**：
```csharp
public class MyComponent : ScriptComponent
{
    public override void OnUpdate(float timeStep)
    {
        Vector3 pos = GetNode().position; // 错误！组件中用 node 属性
    }
}
```

✅ **正确做法**：
```csharp
public class MyComponent : ScriptComponent
{
    public override void OnUpdate(float timeStep)
    {
        Vector3 pos = node.position; // 正确！
    }
}
```

**原因**：`ScriptComponent` 提供了 `node` 属性，直接使用即可。

---

## 最佳实践总结

### 1. 架构设计原则

#### 服务器权威

```csharp
// ✅ 正确：服务器负责游戏逻辑
#if SERVER
public override void OnSetup()
{
    CreateAllGameUnits();
    InitializeGameLogic();
}
#endif
```

#### 客户端表现

```csharp
// ✅ 正确：客户端只负责视觉效果和用户体验
#if CLIENT
public override void OnSetup()
{
    InitializeClientExtensions();  // 注册事件监听
}
#endif
```

#### 事件驱动

```csharp
// ✅ 正确：使用事件监听处理网络同步的数据变化
Trigger<EventUnitCreate> trigger = new(async (n, e) =>
{
    // 处理服务器同步过来的单位
    return true;
});
trigger.Register(Game.Instance);
```

#### 配置共享

```csharp
// ✅ 正确：使用共享文件管理 GameLink 配置
public static class MyGameShared
{
    public static readonly GameLink<GameDataGameMode, GameDataGameMode> GameMode = 
        new("MyGameMode"u8);
}
```

### 2. 性能优化建议

#### 空间查询优先

```csharp
// ✅ 高效：使用物理引擎空间查询
RigidBody[] nearbyObjects = world.GetRigidBodies(playerPos, 100f);
```

```csharp
// ❌ 低效：手动遍历所有对象
foreach (var obj in allObjects)
{
    if (Vector3.Distance(playerPos, obj.position) < 100f) { /* ... */ }
}
```

#### 组件复用

```csharp
// ✅ 高效：为每个玩家创建独立组件，避免为每个对象创建
for (int playerId = 1; playerId <= 8; playerId++)
{
    var component = new MyGameComponent(playerId);
    player.MainUnit.GetNode().AddComponent<MyGameComponent>(component);
}
```

#### 早期退出

```csharp
// ✅ 高效：在组件更新中及时检查 null 值
public override void OnFixedUpdate(float timeStep)
{
    if (world == null) return;  // 早期退出
    if (player?.MainUnit == null) return;
    
    // 执行逻辑...
}
```

#### 固定更新分离

```csharp
// ✅ 正确：物理逻辑用 OnFixedUpdate
public override void OnFixedUpdate(float timeStep)
{
    // 物理计算（固定时间步长）
    ApplyForces();
}

// ✅ 正确：渲染逻辑用 OnUpdate
public override void OnUpdate(float timeStep)
{
    // 渲染更新（可变时间步长）
    UpdateVisualEffects();
}
```

### 3. 代码组织建议

#### 推荐的文件结构

```
MyGame/
├── Shared/
│   ├── MyGameShared.cs          // 共享配置和逻辑
│   └── MyGameComponents.cs      // 通用组件
├── Server/
│   ├── MyServerGameInstance.cs  // 服务器游戏实例
│   └── MyServerComponents.cs    // 服务器专用组件
└── Client/
    ├── MyClientGameInstance.cs  // 客户端游戏实例
    └── MyClientComponents.cs    // 客户端专用组件
```

#### 清晰的命名空间

```csharp
using System;
using System.Collections.Generic;
using GameCore.GameSystem.Data;
using GameUI.CameraSystem.Data;
using GameCorePhysics.Actor;
using EngineInterface.Urho3DInterface;
```

### 4. 调试技巧

#### 使用详细的日志输出

```csharp
public override void OnStart()
{
    Console.WriteLine($"组件启动: {this.GetType().Name}");
}

#if SERVER
    Console.WriteLine("[服务器] 创建单位");
#endif

#if CLIENT
    Console.WriteLine("[客户端] 接收单位");
#endif
```

#### 在关键位置添加 null 检查

```csharp
public override void OnFixedUpdate(float timeStep)
{
    if (world == null)
    {
        Console.WriteLine("警告：PhysicsWorld 为 null");
        return;
    }

    var player = Player.GetById(playerId);
    if (player?.MainUnit == null)
    {
        Console.WriteLine($"警告：玩家 {playerId} 主控单位为 null");
        return;
    }

    // 执行逻辑...
}
```

### 5. 多人游戏稳定性

#### 错误隔离

```csharp
// ✅ 正确：为每个玩家创建独立组件，避免互相影响
for (int playerId = 1; playerId <= maxPlayers; playerId++)
{
    try
    {
        var player = Player.GetById(playerId);
        if (player?.MainUnit != null)
        {
            var component = new MyGameComponent(playerId);
            player.MainUnit.GetNode().AddComponent<MyGameComponent>(component);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"为玩家 {playerId} 创建组件失败: {ex.Message}");
        // 单个玩家失败不影响其他玩家
    }
}
```

#### 优雅降级

```csharp
// ✅ 正确：功能失败时不崩溃
for (int playerId = 1; playerId <= 8; playerId++)
{
    var player = Player.GetById(playerId);
    if (player?.MainUnit == null) continue;  // 优雅降级
    
    // 处理逻辑
}
```

### 6. 命名空间最佳实践

```csharp
// ✅ 正确：使用完整命名空间避免类名冲突
EngineInterface.Urho3DInterface.Material material = 
    new EngineInterface.Urho3DInterface.Material();
```

```csharp
// ❌ 错误：可能与内部类名冲突
Material material = new Material();
```

---

## 核心原则

记住这四大核心原则，你就能避免大部分问题：

1. **服务器权威**：所有游戏逻辑和单位创建在服务器端执行
2. **客户端表现**：客户端只负责视觉效果和用户体验
3. **事件驱动**：使用事件监听处理网络同步的数据变化
4. **配置共享**：使用共享文件管理 GameLink 配置，避免重复定义

这将帮助你构建稳定、高性能的多人物理游戏。

---

## 快速检查清单

开发前检查：
- [ ] 确认单位只在服务器创建
- [ ] 确认客户端使用事件监听
- [ ] 确认使用正确的场景参数（`MyGameScene` 而非 `MyGameMode`）
- [ ] 确认使用 `player.MainUnit = unit` 而非 `player.SetMainUnit(unit)`

性能检查：
- [ ] 使用 `world.GetRigidBodies()` 而非手动遍历
- [ ] 物理逻辑放在 `OnFixedUpdate` 中
- [ ] 渲染逻辑放在 `OnUpdate` 中
- [ ] 避免在更新循环中频繁创建对象

代码质量检查：
- [ ] 添加了适当的 null 检查
- [ ] 使用了 try-catch 进行错误隔离
- [ ] 添加了日志输出便于调试
- [ ] 使用完整命名空间避免类名冲突

---

**相关文档**：
- 🚀 [快速入门指南](./PhysicsGame_Quick_Start.md)
- 📖 [物理游戏 API 参考](./PhysicsGame_API_Reference.md)
- 🌐 [多人游戏开发](./PhysicsGame_Multiplayer_Guide.md)
- 🏗️ [架构深度解析](./PhysicsGame_Architecture_Deep_Dive.md)
- 🎨 [材质系统指南](./Material_System_Guide.md)

