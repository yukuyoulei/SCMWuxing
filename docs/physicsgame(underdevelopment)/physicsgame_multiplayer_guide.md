---
title: PhysicsGame Multiplayer Development Guide
document_type: tutorial
priority: high
target_audience: intermediate
topics:
  - multiplayer
  - networking
  - client-server
  - events
version: 1.0
last_updated: 2025-01-27
related_docs:
  - PhysicsGame_Quick_Start.md
  - PhysicsGame_API_Reference.md
  - PhysicsGame_Architecture_Deep_Dive.md
---

# 多人联网游戏开发指南

本文档介绍如何使用 PhysicsGame 框架开发多人联网游戏。框架采用客户端-服务器分离架构，确保游戏逻辑的权威性和网络同步的可靠性。

---

## 目录

1. [核心架构](#核心架构)
2. [客户端-服务器分离](#客户端-服务器分离)
3. [客户端事件监听](#客户端事件监听)
4. [多人游戏组件设计](#多人游戏组件设计)
5. [最佳实践](#最佳实践)

---

## 核心架构

### 服务器权威架构

PhysicsGame 框架采用**服务器权威**架构：

- **服务器端**：
  - 创建所有游戏单位（地板、玩家、物体）
  - 执行所有游戏逻辑
  - 处理物理计算
  - 管理游戏状态

- **客户端**：
  - 只做初始化工作
  - 接收服务器同步的单位
  - 添加视觉效果和 UI
  - 不参与游戏逻辑

### 网络同步机制

1. **服务器创建单位** → 自动同步 → **客户端接收**
2. 客户端通过事件监听处理同步过来的单位
3. 框架自动管理网络同步，无需手动处理

---

## 客户端-服务器分离

### 共享配置文件

创建一个共享文件，定义服务器和客户端都使用的配置：

```csharp
// MyGameShared.cs
public static class MyGameShared
{
    // 共享的游戏配置
    public static readonly GameLink<GameDataGameMode, GameDataGameMode> GameMode = 
        new("MyGameMode"u8);
    
    public static readonly GameLink<GameDataScene, GameDataScene> GameScene = 
        new("MyGameScene"u8);
    
    // 共享的单位定义
    public static readonly IGameLink<GameDataUnit> PlayerUnit = 
        GameEntry.PhysicsGameData.Unit.PhysicsHole;
    
    public static readonly IGameLink<GameDataUnit> FloorUnit = 
        GameEntry.PhysicsGameData.Unit.PhysicsFloor;
}
```

**优势**：
- 避免重复定义
- 确保客户端和服务器配置一致
- 便于维护

### 服务器端实现

```csharp
#if SERVER
using System;
using System.Collections.Generic;
using GameCorePhysics.Actor;
using EngineInterface.Urho3DInterface;

// 服务器端 - 负责游戏逻辑和单位创建
public class MyServerGameInstance : GameCorePhysics.Core.PhysicsGame
{
    public override IGameLink<GameDataGameMode>? GetGameMode() 
        => MyGameShared.GameMode;
    
    public override IGameLink<GameDataScene>? GetGameScene() 
        => MyGameShared.GameScene;
    
    public override IGameLink<GameDataCamera>? GetCamera() 
        => GameEntry.ScopeData.Camera.DefaultCamera;

    private GameCore.SceneSystem.Scene scene;
    private List<PhysicsActor> gameObjects;

    public override void OnSetup()
    {
        scene = GameCore.SceneSystem.Scene.Get(MyGameShared.GameScene);
        gameObjects = new List<PhysicsActor>();

        // 服务器负责创建所有游戏单位
        CreateFloor();
        CreatePlayerUnits();
        CreateGameObjects();
        CreateGameLogicComponents();
    }

    private void CreateFloor()
    {
        var floor = new PhysicsActor(
            Player.GetById(0),
            MyGameShared.FloorUnit,
            scene,
            Vector3.Zero,
            Vector3.Zero
        );
        floor.GetNode().localScale = new Vector3(10, 10, 1);
    }

    private void CreatePlayerUnits()
    {
        // 为 1-8 号玩家创建主控单位
        for (int playerId = 1; playerId <= 8; playerId++)
        {
            var player = Player.GetById(playerId);
            if (player != null)
            {
                var unit = new PhysicsActor(
                    player,
                    MyGameShared.PlayerUnit,
                    scene,
                    new Vector3(playerId * 100, 0, 50),
                    Vector3.Zero
                );
                
                // 设置为主控单位
                player.MainUnit = unit;
                
                // 配置物理属性
                var rigidBody = unit.GetNode().GetComponent<RigidBody>();
                if (rigidBody != null)
                {
                    rigidBody.SetUseGravity(false);
                }
            }
        }
    }

    private void CreateGameObjects()
    {
        for (int i = 0; i < 30; i++)
        {
            var obj = new PhysicsActor(
                Player.GetById(0),
                PhysicsActor.GetPrimitiveLink(PrimitiveShape.Cube),
                scene,
                new Vector3(
                    (float)(new Random().NextDouble() * 500),
                    (float)(new Random().NextDouble() * 500),
                    100
                ),
                Vector3.Zero
            );
            gameObjects.Add(obj);
        }
    }

    private void CreateGameLogicComponents()
    {
        // 为每个玩家创建游戏逻辑组件
        for (int playerId = 1; playerId <= 8; playerId++)
        {
            var player = Player.GetById(playerId);
            if (player?.MainUnit != null)
            {
                var component = new MyGameComponent(playerId);
                player.MainUnit.GetNode().AddComponent<MyGameComponent>(component);
            }
        }
    }
}

// 服务器游戏注册
public class MyServerGameClass : IGameClass
{
    public static void OnRegisterGameClass()
    {
        GameCorePhysics.Core.PhysicsGameManager.RegisterGame(new MyServerGameInstance());
    }
}
#endif
```

### 客户端实现

```csharp
#if CLIENT
using System;
using GameCorePhysics.Actor;
using EngineInterface.Urho3DInterface;

// 客户端 - 负责视觉效果和用户体验
public class MyClientGameInstance : GameCorePhysics.Core.PhysicsGame
{
    public override IGameLink<GameDataGameMode>? GetGameMode() 
        => MyGameShared.GameMode;
    
    public override IGameLink<GameDataScene>? GetGameScene() 
        => MyGameShared.GameScene;
    
    public override IGameLink<GameDataCamera>? GetCamera() 
        => GameEntry.ScopeData.Camera.DefaultCamera;

    public override void OnSetup()
    {
        // 客户端只做初始化工作，不创建单位
        MyClientExtension.InitializeClientExtensions();
    }
}

// 客户端游戏注册
public class MyClientGameClass : IGameClass
{
    public static void OnRegisterGameClass()
    {
        GameCorePhysics.Core.PhysicsGameManager.RegisterGame(new MyClientGameInstance());
    }
}
#endif
```

**关键点**：
- 服务器端在 `OnSetup()` 中创建所有单位
- 客户端在 `OnSetup()` 中只做初始化
- 单位由服务器创建后自动同步到客户端

---

## 客户端事件监听

客户端通过事件监听器处理服务器同步过来的单位：

### 客户端扩展管理器

```csharp
#if CLIENT
public static class MyClientExtension
{
    public static void InitializeClientExtensions()
    {
        RegisterUnitCreateListener();
        RegisterMainUnitChangeListener();
    }

    // 单位创建事件监听
    private static void RegisterUnitCreateListener()
    {
        Trigger<EventUnitCreate> triggerUnitCreated = new(async (n, e) =>
        {
            Unit unit = e.Unit;
            Player ownerPlayer = unit.Player;

            if (ownerPlayer?.Id == 0) // 系统创建的物理对象
            {
                // 为物理对象添加客户端特效
                AddClientEffectsToPhysicsObject(unit);
            }
            else if (ownerPlayer?.Id >= 1 && ownerPlayer?.Id <= 8) // 玩家单位
            {
                // 为玩家单位添加客户端 UI 或特效
                AddClientEffectsToPlayerUnit(unit);
            }

            return true;
        });
        triggerUnitCreated.Register(Game.Instance);
    }

    // 主控单位变化事件监听
    private static void RegisterMainUnitChangeListener()
    {
        Trigger<EventPlayerMainUnitChanged> triggerMainUnitChanged = new(async (s, e) =>
        {
            Player player = e.Player;
            Unit? unit = e.Unit;

            if (player?.Id >= 1 && player?.Id <= 8 && unit != null)
            {
                // 为新的主控单位添加客户端组件
                var node = unit.GetNode();
                node.AddComponent<MyClientComponent>(new MyClientComponent());
            }

            return true;
        });
        triggerMainUnitChanged.Register(Game.Instance);
    }

    private static void AddClientEffectsToPhysicsObject(Unit unit)
    {
        // 为物理对象添加视觉效果
        var node = unit.GetNode();
        // 例如：添加粒子效果、发光效果等
    }

    private static void AddClientEffectsToPlayerUnit(Unit unit)
    {
        // 为玩家单位添加 UI 或特效
        var node = unit.GetNode();
        // 例如：添加玩家名称 UI、血条等
    }
}
#endif
```

### 事件工作流程

1. **服务器创建单位** → `PhysicsActor` 创建
2. **网络同步** → 单位数据传输到客户端
3. **触发 EventUnitCreate** → 客户端接收单位
4. **客户端处理** → 添加视觉效果、UI 等

---

## 多人游戏组件设计

### 服务器端组件 - 负责游戏逻辑

```csharp
#if SERVER
public class MultiplayerGameComponent : ScriptComponent
{
    private int playerId;
    private PhysicsWorld world;

    public MultiplayerGameComponent(int playerIdParam)
    {
        playerId = playerIdParam;
    }

    public override void OnDelayedStart()
    {
        // 在 DelayedStart 中获取依赖的系统引用
        var player = Player.GetById(playerId);
        if (player?.MainUnit != null)
        {
            world = player.MainUnit.GetOwnerPhysicsWorld();
        }
    }

    public override void OnFixedUpdate(float timeStep)
    {
        if (world == null) return;

        var player = Player.GetById(playerId);
        if (player?.MainUnit == null) return;

        // 执行该玩家的游戏逻辑
        ProcessPlayerLogic(player);
    }

    private void ProcessPlayerLogic(Player player)
    {
        // 使用高效的空间查询
        Vector3 playerPos = player.MainUnit.GetNode().position;
        RigidBody[] nearbyObjects = world.GetRigidBodies(playerPos, 100f);

        foreach (var rigidBody in nearbyObjects)
        {
            // 处理玩家与周围物体的交互
            ProcessPlayerObjectInteraction(player, rigidBody);
        }
    }

    private void ProcessPlayerObjectInteraction(Player player, RigidBody rigidBody)
    {
        // 实现具体的交互逻辑
        // 例如：施加力、拾取物品、造成伤害等
    }
}
#endif
```

### 客户端组件 - 负责视觉效果

```csharp
#if CLIENT
public class MyClientComponent : ScriptComponent
{
    private EngineInterface.Urho3DInterface.Material? material;

    public override void OnStart()
    {
        // 获取材质引用
        var materials = GetComponent<PhysicsActor>()?.GetModelMaterials();
        if (materials != null && materials.Length > 0)
        {
            material = materials[0];
        }
    }

    public override void OnUpdate(float timeStep)
    {
        // 客户端视觉效果更新
        UpdateVisualEffects();
    }

    private void UpdateVisualEffects()
    {
        // 更新材质、粒子效果、UI 等
        if (material != null)
        {
            // 例如：根据玩家状态改变颜色
            // material.SetColor("TintColor", currentColor);
        }
    }
}
#endif
```

### 组件设计原则

1. **服务器端**：
   - 只包含游戏逻辑
   - 使用 `OnFixedUpdate` 处理物理相关逻辑
   - 每个玩家独立的组件实例

2. **客户端**：
   - 只包含视觉效果和 UI
   - 使用 `OnUpdate` 处理渲染相关逻辑
   - 不修改游戏状态

---

## 最佳实践

### 1. 职责分离

```csharp
// ✅ 正确：服务器权威
#if SERVER
public override void OnSetup()
{
    CreatePlayerUnits();  // 服务器创建
}
#endif

#if CLIENT
public override void OnSetup()
{
    InitializeClientExtensions();  // 客户端初始化
}
#endif
```

```csharp
// ❌ 错误：客户端创建单位
#if CLIENT
public override void OnSetup()
{
    CreatePlayerUnits();  // 错误！客户端不应该创建单位
}
#endif
```

### 2. 错误处理和容错

```csharp
// 为每个玩家独立创建组件，隔离故障
for (int playerId = 1; playerId <= 8; playerId++)
{
    try
    {
        var player = Player.GetById(playerId);
        if (player?.MainUnit == null) continue;  // 优雅降级
        
        var component = new MyGameComponent(playerId);
        player.MainUnit.GetNode().AddComponent<MyGameComponent>(component);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"为玩家 {playerId} 创建组件失败: {ex.Message}");
        // 单个玩家失败不影响其他玩家
    }
}
```

### 3. 性能优化

#### 避免重复计算

```csharp
// ✅ 正确：只让一个玩家执行清理
if (playerId == 1)  // 只有玩家1执行
{
    CleanupDestroyedObjects();
}
```

```csharp
// ❌ 错误：所有玩家都执行
CleanupDestroyedObjects();  // 重复计算 8 次！
```

#### 使用空间查询

```csharp
// ✅ 正确：使用物理引擎空间查询
RigidBody[] nearbyObjects = world.GetRigidBodies(playerPos, radius);
```

```csharp
// ❌ 错误：手动遍历所有对象
foreach (var obj in allObjects)
{
    float distance = Vector3.Distance(playerPos, obj.position);
    if (distance < radius) { /* 处理 */ }
}
```

### 4. 网络友好设计

- **最小化同步数据**：只同步必要的状态
- **依赖事件而非轮询**：使用事件监听处理网络同步
- **客户端预测**：在客户端添加视觉反馈，但不影响游戏逻辑

### 5. 调试技巧

```csharp
public override void OnStart()
{
#if SERVER
    Console.WriteLine($"[服务器] 组件启动: {this.GetType().Name}");
#endif

#if CLIENT
    Console.WriteLine($"[客户端] 组件启动: {this.GetType().Name}");
#endif
}
```

---

## 总结

多人游戏开发的核心要点：

1. **服务器权威**：所有游戏逻辑在服务器执行
2. **客户端表现**：客户端只负责视觉效果
3. **事件驱动**：使用事件监听处理网络同步
4. **配置共享**：使用共享文件管理配置
5. **错误隔离**：单个玩家故障不影响其他玩家
6. **性能优化**：避免重复计算，使用空间查询

---

**相关文档**：
- 🚀 [快速入门指南](./PhysicsGame_Quick_Start.md)
- 📖 [物理游戏 API 参考](./PhysicsGame_API_Reference.md)
- 🏗️ [架构深度解析](./PhysicsGame_Architecture_Deep_Dive.md)
- ✅ [最佳实践](./PhysicsGame_Best_Practices.md)
- 🎮 [黑洞游戏案例](./BlackHole_Game_Case_Study.md)

