---
title: PhysicsGame Quick Start Guide
document_type: quick-start
priority: high
target_audience: beginner
topics:
  - physics
  - game-development
  - getting-started
version: 1.0
last_updated: 2025-01-27
related_docs:
  - PhysicsGame_API_Reference.md
  - PhysicsGame_Multiplayer_Guide.md
  - PhysicsGame_Best_Practices.md
---

# PhysicsGame 快速入门指南

## 简介

PhysicsGame 是一个基于物理引擎的游戏开发框架，支持创建物理对象、碰撞检测和脚本组件系统。本指南将帮助你在15-30分钟内创建第一个物理游戏。

## 框架基础概念

### PhysicsGame 架构

PhysicsGame 框架采用继承式架构，你需要创建一个继承自 `GameCorePhysics.Core.PhysicsGame` 的类：

```csharp
using System;
using System.Collections.Generic;
using GameCore.GameSystem.Data;
using GameUI.CameraSystem.Data;
using GameCorePhysics.Actor;
using EngineInterface.Urho3DInterface;

public class MyGameInstance : GameCorePhysics.Core.PhysicsGame
{
    // 必须重写的三个配置方法
    public override IGameLink<GameDataGameMode>? GetGameMode() { }
    public override IGameLink<GameDataScene>? GetGameScene() { }
    public override IGameLink<GameDataCamera>? GetCamera() { }

    // 游戏初始化逻辑
    public override void OnSetup() { }
}
```

### 核心概念

1. **PhysicsActor 继承自 Unit**
   - PhysicsActor 既是物理对象又是游戏单位
   - 这是框架的核心设计，简化了对象管理

2. **服务器权威**
   - 所有游戏逻辑和单位创建在服务器端执行
   - 客户端只做视觉表现

3. **事件驱动**
   - 客户端通过事件监听服务器同步过来的数据变化
   - 避免手动管理网络同步

4. **配置驱动**
   - 通过 GameLink 配置游戏模式、场景、相机等
   - 服务器和客户端共享配置

### 坐标系统

框架使用 **Unreal Engine 左手坐标系**：

- **X轴**：向前（Forward）- 正值向前
- **Y轴**：向右（Right）- 正值向右  
- **Z轴**：向上（Up）- 正值向上

```csharp
// 坐标示例
new Vector3(1000, 0, 0)    // 前方地面（X轴延伸）
new Vector3(1000, 0, 400)  // 前方400单位高的平台
new Vector3(1000, 200, 0)  // 右侧200单位的地面物体
```

> 注意：Vector3 使用 `System.Numerics.Vector3`

---

## 快速上手：制作你的第一个物理游戏

### 第一步：创建游戏实例

```csharp
public class MyGameInstance : GameCorePhysics.Core.PhysicsGame
{
    // 定义游戏配置
    private GameLink<GameDataGameMode, GameDataGameMode> MyGameMode = new("MyGameMode"u8);
    private GameLink<GameDataScene, GameDataScene> MyGameScene = new("MyGameScene"u8);

    public override IGameLink<GameDataGameMode>? GetGameMode()
    {
        return MyGameMode;
    }

    public override IGameLink<GameDataScene>? GetGameScene()
    {
        return MyGameScene;
    }

    public override IGameLink<GameDataCamera>? GetCamera()
    {
        return GameEntry.ScopeData.Camera.DefaultCamera;
    }

    private GameCore.SceneSystem.Scene scene;
    private List<PhysicsActor> gameObjects;

    public override void OnSetup()
    {
        // 使用 GameScene 获取场景实例
        scene = GameCore.SceneSystem.Scene.Get(MyGameScene);
        gameObjects = new List<PhysicsActor>();

        CreateGameWorld();
    }
}
```

**关键点**：
- `GetGameMode()`, `GetGameScene()`, `GetCamera()` 必须重写
- `OnSetup()` 是游戏初始化的入口
- 使用 `MyGameScene`（不是 MyGameMode）获取场景实例

### 第二步：创建游戏世界

```csharp
private void CreateGameWorld()
{
    // 创建地板
    CreateFloor();

    // 创建玩家主控单位
    CreatePlayerUnits();

    // 创建游戏物体
    CreateGameObjects();
}
```

#### 创建地板

```csharp
private void CreateFloor()
{
    var floorActor = new PhysicsActor(
        Player.GetById(0),                                 // 系统玩家（ID=0）
        GameEntry.PhysicsGameData.Unit.PhysicsFloor,       // 地板单位类型
        scene,                                             // 场景
        new Vector3(0, 0, 0),                             // 位置
        Vector3.Zero                                      // 旋转
    );

    // 可选：设置地板缩放
    var floorNode = floorActor.GetNode();
    floorNode.localScale = new Vector3(10, 10, 1);  // 放大地板
}
```

#### 创建玩家主控单位

```csharp
private void CreatePlayerUnits()
{
    for (int playerId = 1; playerId <= 4; playerId++)
    {
        var player = Player.GetById(playerId);
        if (player != null)
        {
            // 为玩家创建主控单位
            var playerUnit = new PhysicsActor(
                player,
                GameEntry.PhysicsGameData.Unit.PhysicsHole,  // 使用黑洞单位作示例
                scene,
                new Vector3(playerId * 100, 0, 50),         // 分散放置
                Vector3.Zero
            );

            // 设置为玩家主控单位
            player.MainUnit = playerUnit;

            // 配置物理属性
            var rigidBody = playerUnit.GetNode().GetComponent<RigidBody>();
            if (rigidBody != null)
            {
                rigidBody.SetUseGravity(false);  // 主控单位不受重力影响
            }
        }
    }
}
```

**关键点**：
- 玩家 ID 从 1 开始（0 是系统玩家）
- 使用 `player.MainUnit = unit` 设置主控单位
- 可以配置刚体属性（重力、质量等）

#### 创建游戏物体

```csharp
private void CreateGameObjects()
{
    for (int i = 0; i < 10; i++)
    {
        // 创建随机形状的物理对象
        var shapes = new[] {
            PrimitiveShape.Cube, 
            PrimitiveShape.Sphere,
            PrimitiveShape.Cylinder, 
            PrimitiveShape.Capsule
        };

        var randomShape = shapes[new Random().Next(shapes.Length)];

        var gameObject = new PhysicsActor(
            Player.GetById(0),                              // 归属系统玩家
            PhysicsActor.GetPrimitiveLink(randomShape),    // 使用基础形状
            scene,
            new Vector3(
                (float)(new Random().NextDouble() * 500),
                (float)(new Random().NextDouble() * 500),
                100
            ),
            Vector3.Zero
        );

        gameObjects.Add(gameObject);
    }
}
```

**支持的基础形状**：
- `PrimitiveShape.Cube` - 立方体/矩形（半径50）
- `PrimitiveShape.Sphere` - 球体（半径50）
- `PrimitiveShape.Cylinder` - 圆柱体（半径50，高度100）
- `PrimitiveShape.Cone` - 圆锥体（半径50，高度100）
- `PrimitiveShape.Capsule` - 胶囊体（半径50，高度200）

### 第三步：添加游戏逻辑组件

```csharp
// 创建游戏逻辑组件
public class MyGameComponent : ScriptComponent
{
    public override void OnStart()
    {
        Console.WriteLine("游戏组件启动");
    }

    public override void OnDelayedStart()
    {
        // 在这里获取依赖的系统引用
    }

    public override void OnFixedUpdate(float timeStep)
    {
        // 物理相关逻辑放在 FixedUpdate 中
        // timeStep 是固定的物理时间步长
    }

    public override void OnUpdate(float timeStep)
    {
        // 渲染相关逻辑放在 Update 中
        // timeStep 是可变的渲染时间步长
    }

    public override void OnTriggerEnter(Node node)
    {
        // 碰撞开始时调用
        Console.WriteLine("检测到碰撞!");
    }
}
```

**组件生命周期**：
- `OnStart()` - 组件启动时调用（基础初始化）
- `OnDelayedStart()` - 延迟启动（获取系统引用）
- `OnFixedUpdate()` - 每个物理帧更新（固定时间步长）
- `OnUpdate()` - 每个渲染帧更新（可变时间步长）
- `OnTriggerEnter/Stay/Exit()` - 碰撞事件

#### 添加组件到物理对象

```csharp
// 在 CreatePlayerUnits 中添加组件
private void CreatePlayerUnits()
{
    // ... 创建主控单位后
    var playerNode = playerUnit.GetNode();
    playerNode.AddComponent<MyGameComponent>(new MyGameComponent());
}
```

### 第四步：注册游戏

```csharp
public class MyGameClass : IGameClass
{
    public static void OnRegisterGameClass()
    {
        GameCorePhysics.Core.PhysicsGameManager.RegisterGame(new MyGameInstance());
    }
}
```

---

## 完整示例代码

```csharp
using System;
using System.Collections.Generic;
using GameCore.GameSystem.Data;
using GameUI.CameraSystem.Data;
using GameCorePhysics.Actor;
using EngineInterface.Urho3DInterface;

// 游戏实例
public class MyGameInstance : GameCorePhysics.Core.PhysicsGame
{
    private GameLink<GameDataGameMode, GameDataGameMode> MyGameMode = new("MyGameMode"u8);
    private GameLink<GameDataScene, GameDataScene> MyGameScene = new("MyGameScene"u8);
    private GameCore.SceneSystem.Scene scene;
    private List<PhysicsActor> gameObjects;

    public override IGameLink<GameDataGameMode>? GetGameMode() => MyGameMode;
    public override IGameLink<GameDataScene>? GetGameScene() => MyGameScene;
    public override IGameLink<GameDataCamera>? GetCamera() 
        => GameEntry.ScopeData.Camera.DefaultCamera;

    public override void OnSetup()
    {
        scene = GameCore.SceneSystem.Scene.Get(MyGameScene);
        gameObjects = new List<PhysicsActor>();
        
        CreateFloor();
        CreatePlayerUnits();
        CreateGameObjects();
    }

    private void CreateFloor()
    {
        var floor = new PhysicsActor(
            Player.GetById(0),
            GameEntry.PhysicsGameData.Unit.PhysicsFloor,
            scene,
            Vector3.Zero,
            Vector3.Zero
        );
        floor.GetNode().localScale = new Vector3(10, 10, 1);
    }

    private void CreatePlayerUnits()
    {
        for (int i = 1; i <= 4; i++)
        {
            var player = Player.GetById(i);
            if (player != null)
            {
                var unit = new PhysicsActor(
                    player,
                    GameEntry.PhysicsGameData.Unit.PhysicsHole,
                    scene,
                    new Vector3(i * 100, 0, 50),
                    Vector3.Zero
                );
                player.MainUnit = unit;
            }
        }
    }

    private void CreateGameObjects()
    {
        for (int i = 0; i < 10; i++)
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
}

// 游戏注册
public class MyGameClass : IGameClass
{
    public static void OnRegisterGameClass()
    {
        GameCorePhysics.Core.PhysicsGameManager.RegisterGame(new MyGameInstance());
    }
}
```

---

## 下一步

恭喜！你已经创建了第一个物理游戏。接下来可以：

1. **学习 API**：查阅 [物理游戏 API 参考](./PhysicsGame_API_Reference.md) 了解更多功能
2. **添加视觉效果**：阅读 [材质系统指南](./Material_System_Guide.md) 实现炫酷效果
3. **开发多人游戏**：学习 [多人游戏开发指南](./PhysicsGame_Multiplayer_Guide.md)
4. **避免常见错误**：浏览 [最佳实践](./PhysicsGame_Best_Practices.md)

---

## 常见问题

### Q: 为什么场景获取要用 MyGameScene 而不是 MyGameMode？

A: `Scene.Get()` 的参数类型是 `GameLink<GameDataScene, GameDataScene>`，必须使用场景配置。

### Q: Player.GetById(0) 是什么？

A: ID=0 是系统玩家，用于创建不归属任何玩家的物体（如地板、环境物体）。

### Q: 如何销毁物理对象？

A: 使用 `PhysicsActor.DestroyImmediately(actor)`

### Q: 如何修改物体位置？

A: 通过 Node 修改：`actor.GetNode().position = new Vector3(x, y, z)`

---

**相关文档**：
- 📖 [物理游戏 API 参考](./PhysicsGame_API_Reference.md)
- 🌐 [多人游戏开发指南](./PhysicsGame_Multiplayer_Guide.md)
- ✅ [最佳实践和常见陷阱](./PhysicsGame_Best_Practices.md)

