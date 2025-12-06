---
title: PhysicsGame API Reference
document_type: api-reference
priority: high
target_audience: all
topics:
  - physics
  - api
  - collision
  - scripting
version: 1.0
last_updated: 2025-01-27
related_docs:
  - PhysicsGame_Quick_Start.md
  - PhysicsGame_Best_Practices.md
---

# PhysicsGame API 参考

本文档提供 PhysicsGame 框架的完整 API 参考。用于快速查询接口用法。

> **注意**：本文档是API速查手册。如需教程和示例，请参阅 [快速入门指南](./PhysicsGame_Quick_Start.md)。

---

## 目录

1. [坐标系统](#坐标系统)
2. [场景管理](#场景管理)
3. [玩家管理](#玩家管理)
4. [物理对象API](#物理对象api)
5. [Node操作](#node操作)
6. [主控单位](#主控单位)
7. [脚本组件系统](#脚本组件系统)
8. [刚体组件](#刚体组件)
9. [碰撞过滤器](#碰撞过滤器)
10. [物理平面裁剪](#物理平面裁剪)
11. [客户端特有API](#客户端特有api)
12. [服务器端特有API](#服务器端特有api)
13. [玩家控制](#玩家控制)

---

## 坐标系统

### Unreal Engine 左手坐标系

- **X轴**: 向前（Forward）- 正值向前
- **Y轴**: 向右（Right）- 正值向右  
- **Z轴**: 向上（Up）- 正值向上

### 示例坐标使用

```csharp
// 地面平台 - X轴延伸，Z=0为地面
new Vector3(1000, 0, 0)  // 前方地面

// 悬浮平台 - Z值表示高度
new Vector3(1000, 0, 400)  // 前方400单位高的平台

// 侧面物体 - Y值偏移
new Vector3(1000, 200, 0)  // 右侧200单位的地面物体
```

> **注意**：Vector3 使用 C# 系统自带的结构：`System.Numerics.Vector3`

---

## 场景管理

### 获取场景

```csharp
// 参数类型为：GameLink<GameDataScene, GameDataScene>
GameCore.SceneSystem.Scene scene = GameCore.SceneSystem.Scene.Get(
    GameEntry.PhysicsGameData.Scene.PhysicsScene
);
```

---

## 玩家管理

### 获取本地玩家（客户端）

```csharp
GameCore.PlayerAndUsers.Player localPlayer = Player.LocalPlayer;
```

### 获取指定玩家

```csharp
// 获取1~n的玩家（多人联机使用，客户端和服务器都可以使用）
// 传入为1~n，表示不同的玩家
GameCore.PlayerAndUsers.Player player = Player.GetById(1);

// 系统玩家（用于创建环境物体）
GameCore.PlayerAndUsers.Player systemPlayer = Player.GetById(0);
```

---

## 物理对象API

### 创建物理对象

```csharp
// 基础形状创建
var actor = new GameCorePhysics.Actor.PhysicsActor(
    GameCore.PlayerAndUsers.Player.LocalPlayer,           // 玩家引用
    PhysicsActor.GetPrimitiveLink(PrimitiveShape.Cube),   // 形状
    scene,                                                // 场景
    new Vector3(x, y, z),                                 // 位置
    Vector3.Zero                                          // 旋转
);
```

### 销毁物理对象

```csharp
GameCorePhysics.Actor.PhysicsActor.DestroyImmediately(actor);
```

### 获取物理对象材质

```csharp
Material[] materials = actor.GetModelMaterials();
```

> **注意**：`PhysicsActor` 继承自 `Unit`

### 施加力和动量

```csharp
// 施加力
actor.ApplyForce(new Vector3(0, 0, 100));

// 施加动量
actor.ApplyImpulse(new Vector3(0, 0, 100));

// 应用修改后的物理属性
// 通过以上接口修改的物理属性，都必须调用此接口让其生效！
actor.ApplyPhysicsAttribute();
```

### 支持的形状类型

- `PrimitiveShape.Cube` - 立方体/矩形（半径50）
- `PrimitiveShape.Sphere` - 球体（半径50）
- `PrimitiveShape.Cylinder` - 圆柱体（半径50，高度100）
- `PrimitiveShape.Cone` - 圆锥体（半径50，高度100）
- `PrimitiveShape.Capsule` - 胶囊体（半径50，高度200）

**碰撞层配置**：
- 基础形状默认碰撞 Layer = 2
- 基础形状默认 Mask = unsigned 最大值

---

## Node操作

### 获取节点

```csharp
Node node = actor.GetNode();
```

### 位置操作

```csharp
// 获取世界位置
Vector3 position = node.position;

// 修改世界位置
node.position = new Vector3(0, 0, 100);

// 获取本地位置
Vector3 localPosition = node.localPosition;

// 修改本地位置
node.localPosition = new Vector3(0, 0, 100);
```

### 缩放操作

```csharp
// 获取本地缩放
Vector3 localScale = node.localScale;

// 设置本地缩放
node.localScale = new Vector3(10, 10, 10);
```

---

## 主控单位

### 获取主控单位

```csharp
GameCore.PlayerAndUsers.Player player = GameCore.PlayerAndUsers.Player.GetById(0);
var mainUnit = player.MainUnit;

// 获取主控单位的 node
var node = mainUnit.GetNode();
```

### 设置主控单位

```csharp
GameCore.PlayerAndUsers.Player player = GameCore.PlayerAndUsers.Player.GetById(0);
player.MainUnit = unit;
```

> **注意**：`Player.MainUnit` 类型为 `Unit`

**碰撞配置**：
- 主控物理对象默认碰撞 Layer = 1
- 主控物理对象默认碰撞 Mask = unsigned 最大值

---

## 脚本组件系统

### 创建脚本组件

> **注意**：`ScriptComponent` 继承自 `Component`

```csharp
using EngineInterface.Urho3DInterface;

public class MyGameComponent : ScriptComponent
{
    public override void OnStart()
    {
        // 组件启动时调用
    }

    public override void OnDelayedStart()
    {
        // 延迟启动（用于获取依赖的系统引用）
    }

    public override void OnStop()
    {
        // 组件结束时调用
    }

    public override void OnUpdate(float timeStep)
    {
        // 每个渲染帧更新
    }

    public override void OnPostUpdate(float timeStep)
    {
        // 每个渲染帧更新之后
    }

    public override void OnFixedUpdate(float timeStep)
    {
        // 每个物理帧更新
    }

    public override void OnFixedPostUpdate(float timeStep)
    {
        // 每个物理帧更新之后
    }

    public override void OnTriggerEnter(Node node)
    {
        // 碰撞开始时调用
        Console.WriteLine("检测到碰撞!");
    }

    public override void OnTriggerStay(Node node)
    {
        // 碰撞持续时调用
    }

    public override void OnTriggerExit(Node node)
    {
        // 碰撞结束时调用
    }
}
```

### 添加组件到物理对象

```csharp
// 方式1: 直接创建
Node node = physicsActor.GetNode();
node.CreateComponent<MyGameComponent>();

// 方式2: 实例化后添加（推荐用于需要传参的组件）
Node node = physicsActor.GetNode();
MyGameComponent component = new MyGameComponent();
node.AddComponent<MyGameComponent>(component);
```

### 获取组件节点

```csharp
// 组件定义
Component component;

// 通过组件获取节点
Node node = component.node;
```

---

## 刚体组件

> **注意**：`RigidBody` 继承自 `Component`

### 获取刚体组件

```csharp
// 通过 node 获取刚体组件
RigidBody rigidBody = node.GetComponent<RigidBody>();
```

> **警告**：不能主动创建 RigidBody 组件！通过 PhysicsActor 获取的 Node 里面已经自动创建好 RigidBody 了。

### 力和动量

```csharp
// 设置力
rigidBody.ApplyForce(new Vector3(0, 0, 100));

// 设置动量
rigidBody.ApplyImpulse(new Vector3(0, 0, 100));
```

### 速度控制

```csharp
// 设置线性速度
rigidBody.SetLinearVelocity(new Vector3(0, 0, 100));

// 获取线性速度
Vector3 lv = rigidBody.GetLinearVelocity();

// 设置角速度
rigidBody.SetAngularVelocity(new Vector3(0, 0, 100));

// 获取角速度
Vector3 av = rigidBody.GetAngularVelocity();
```

### 质量和阻尼

```csharp
// 设置质量
rigidBody.SetMass(1.0f);
float mass = rigidBody.GetMass();

// 设置线性阻尼
rigidBody.SetLinearDamping(0.0f);
float ld = rigidBody.GetLinearDamping();

// 设置角度阻尼
rigidBody.SetAngularDamping(0.0f);
float ad = rigidBody.GetAngularDamping();
```

### 物理材质属性

```csharp
// 设置摩擦力
rigidBody.SetFriction(0.5f);
float friction = rigidBody.GetFriction();

// 设置滚动摩擦力
rigidBody.SetRollingFriction(0.5f);
float rollingFriction = rigidBody.GetRollingFriction();

// 设置恢复系数（弹性）
rigidBody.SetRestitution(0.0f);
float restitution = rigidBody.GetRestitution();
```

### 重力和模式

```csharp
// 设置重力开关
rigidBody.SetUseGravity(true);
bool flag = rigidBody.GetUseGravity();

// 设置动力学模式
rigidBody.SetKinematic(true);

// 设置触发模式
rigidBody.SetTrigger(true);
```

### 碰撞层和遮罩

```csharp
// 设置碰撞 Layer
rigidBody.SetCollisionLayer(1u);
uint layer = rigidBody.GetCollisionLayer();

// 设置碰撞遮罩
rigidBody.SetCollisionMask(1u);
uint mask = rigidBody.GetCollisionMask();
```

**碰撞 Layer 和 Mask 工作原理**：

当物理引擎检查两个物体（A 和 B）是否应该碰撞时，执行位运算：

```
if ((A.Layer & B.Mask) != 0 && (B.Layer & A.Mask) != 0)
```

只有同时满足这两个条件，碰撞才会发生：
- 物体A的层 必须在 物体B的遮罩 中
- 物体B的层 必须在 物体A的遮罩 中

**系统预制碰撞层**：
- 地板、地形的碰撞层 = 4

### 休眠控制

```csharp
// 唤醒物体进入活跃状态
rigidBody.WakeUp();

// 让物体进入休眠状态
rigidBody.Sleep();

// 获取物体是否在休眠状态
bool isSleep = rigidBody.IsSleep();
```

### 属性直接访问

也可以通过属性直接访问和修改：

```csharp
RigidBody rigidBody;

// 线性速度
rigidBody.linearVelcity

// 角速度
rigidBody.angularVelocity

// 质量
rigidBody.mass

// 线性阻尼
rigidBody.linearDamping

// 角度阻尼
rigidBody.angularDamping

// 摩擦力
rigidBody.friction

// 滚动摩擦力
rigidBody.rollingFriction

// 恢复系数
rigidBody.restitution

// 是否使用重力
rigidBody.useGravity

// 动力学模式
rigidBody.kinematic

// 触发模式
rigidBody.isTrigger

// 碰撞层级
rigidBody.collisionLayer

// 碰撞遮罩
rigidBody.collisionMask
```

---

## 物理世界查询

### 获取物理世界

```csharp
// 使用物理 actor 获取物理世界对象
PhysicsWorld world = actor.GetOwnerPhysicsWorld();

// 使用单位获取物理世界对象
PhysicsWorld world = unit.GetOwnerPhysicsWorld();
```

### 物理查询结构

```csharp
public struct PhysicsRaycastResult
{
    /// <summary>
    /// 位置
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Hit worldspace normal.
    /// </summary>
    public Vector3 Normal { get; set; }

    /// <summary>
    /// Hit distance from ray origin.
    /// </summary>
    public float Distance { get; set; }

    /// <summary>
    /// Hit fraction.
    /// </summary>
    public float HitFraction { get; set; }

    /// <summary>
    /// Rigid body that was hit.
    /// </summary>
    public RigidBody? Body => Context.GetObject<RigidBody>(BodyPtr);

    /// <summary>
    /// Rigid body native ptr that was hit.
    /// </summary>
    public NativePtr BodyPtr { get; set; }
}
```

### 射线检测

```csharp
/// <summary>
/// 射线检测
/// </summary>
/// <param name="position">射线原点</param>
/// <param name="direction">射线矢量方向</param>
/// <param name="maxDistance">射线检测最大距离</param>
/// <param name="collisionMask">碰撞遮罩</param>
/// <returns>返回所有满足条件的结果</returns>
PhysicsRaycastResult[] results = world.Raycast(
    new Vector3(0, 0, 0), 
    new Vector3(0, 0, 100), 
    10000
);

/// <summary>
/// 射线检测（单个结果）
/// </summary>
/// <returns>返回距离最近的结果</returns>
PhysicsRaycastResult? result = world.RaycastSingle(
    new Vector3(0, 0, 0), 
    new Vector3(0, 0, 100), 
    10000
);
```

### 球形检测

```csharp
/// <summary>
/// 球形检测
/// </summary>
/// <param name="origin">球中心点</param>
/// <param name="radius">球半径</param>
/// <param name="direction">方向</param>
/// <param name="maxDistance">检测最大距离</param>
/// <param name="collisionMask">碰撞遮罩</param>
/// <returns>返回距离最近的结果</returns>
PhysicsRaycastResult? result = world.SphereCast(
    new Vector3(0, 0, 0), 
    500, 
    new Vector3(0, 0, 100), 
    10000
);
```

### 范围查询

```csharp
/// <summary>
/// 查询球形范围内的刚体
/// </summary>
/// <param name="origin">球中心点</param>
/// <param name="radius">球半径</param>
/// <param name="collisionMask">碰撞 mask</param>
/// <returns>刚体数组</returns>
RigidBody[] rigidBodies = world.GetRigidBodies(
    new Vector3(0.0f, 0.0f, 0.0f), 
    300.0f
);

/// <summary>
/// 查询 Box 范围内的刚体
/// </summary>
/// <param name="aabbMin">Box 最小坐标</param>
/// <param name="aabbMax">Box 最大坐标</param>
/// <param name="collisionMask">碰撞遮罩</param>
/// <returns>刚体数组</returns>
RigidBody[] rigidBodies = world.GetRigidBodies(
    new Vector3(0.0f, 0.0f, 0.0f), 
    new Vector3(500.0f, 500.0f, 500.0f)
);
```

---

## 碰撞过滤器

### 设置碰撞过滤器

```csharp
RigidBody rigidBody = node.GetComponent<RigidBody>();

// 设置碰撞过滤器
rigidBody.SetCollisionFilter((RigidBody otherRigidBody, Vector3 contactPoint) =>
{
    // 返回 true 表示忽略 contactPoint 这个点的碰撞
    return true;
});
```

> **警告**：不允许在过滤函数里面修改物理属性！！！

---

## 物理平面裁剪

当物体个数特别多并且使用碰撞点过滤器时，引擎调用 C# 做碰撞点判断时效率不够快，会成为性能瓶颈。

因此引擎底层在碰撞点过滤器的基础上封装了一套物理平面裁剪的组件，用于大量物体下碰撞点过滤器的使用。

### 创建平面裁剪体

```csharp
Node node = physicsActor.GetNode();

// 创建平面裁剪体
// 通常组件挂载在需要被裁剪的平面上（比如：地板、地形等）
node.CreateComponent<PlaneClippingBody>();
```

### 创建平面裁剪形状

```csharp
Node node = physicsActor.GetNode();

// 创建平面裁剪形状
// 组件可以挂载在被裁剪的平面上
// 如果存在需要动态移动裁剪形状的情况，可以将组件挂载在需要移动的物体上
// （比如你需要在一个平面上动态移动一个圆形裁剪面）
var shape = node.CreateComponent<PlaneClippingShape>();

// 设置形状（圆形）
shape?.SetCircle(new Vector3(0, 0, 0), 50.0f);
```

---

## 客户端特有API

### 客户端单位创建通知

```csharp
Events.Trigger<GameCore.Event.EventUnitCreate> triggerUnitCreated = new(async (n, e) =>
{
    // 获取客户端单位
    GameCore.EntitySystem.Unit? unit = e.Unit;
    // 获取客户端单位 Node
    Node node = unit.GetNode();
    return true;
});
triggerUnitCreated.Register(Game.Instance);
```

### 玩家主控变化通知

```csharp
Events.Trigger<GameCore.Event.EventPlayerMainUnitChanged> playerMainUnitChanged = new(async (s, e) =>
{
    // 玩家
    GameCore.PlayerAndUsers.Player player = e.Player;
    // 变化后的主控单位
    GameCore.EntitySystem.Unit? unit = e.Unit;

    return true;
});
playerMainUnitChanged.Register(Game.Instance);
```

### 客户端获取玩家

```csharp
// 客户端获取自己的玩家
GameCore.PlayerAndUsers.Player myPlayer = Player.LocalPlayer;

// Unit unit;
// 获取单位所属玩家
GameCore.PlayerAndUsers.Player ownerPlayer = unit.Player;
```

---

## 服务器端特有API

### 键盘输入事件

```csharp
Events.Trigger<GameCore.Event.EventPlayerKeyUp> keyUpEvent = new(async (s, d) =>
{
    Console.WriteLine($"keyDownEvent => {d.Key}");
    return true;
});
keyUpEvent.Register(Game.Instance);
```

---

## 玩家控制

系统预制了玩家的移动、相机操作：

- **相机自动跟随**：相机自动跟随主控单位
  - 系统提供了第一人称视角、第三人称视角、俯视角选项
  
- **WASD 移动**：WASD 可以移动主控单位
  - 手机上自动适配为摇杆 UI

---

## 常见错误避免

### ❌ 错误做法

```csharp
Vector3 direction = (target - source).normalized; // normalized 不存在！
RigidBody rb = node.CreateComponent<RigidBody>(); // 不要手动创建！
Vector3 pos = GetNode().position; // 组件中用 node 属性！
```

### ✅ 正确做法

```csharp
Vector3 direction = Vector3.Normalize(target - source);
RigidBody rb = node.GetComponent<RigidBody>();
Vector3 pos = node.position;
```

---

## 渲染系统 API

### 渲染类型定义

所有类型定义位于命名空间：

```csharp
namespace EngineInterface.Urho3DInterface.Graphics;
```

#### PrimitiveType（图元类型）

```csharp
public enum PrimitiveType
{
    TriangleList = 0,   // 三角形列表
    LineList,           // 线段列表
    PointList,          // 点列表
    TriangleStrip,      // 三角形带
    LineStrip,          // 线段带
    TriangleFan,        // 三角形扇
};
```

#### BlendMode, CompareMode, CullMode, FillMode, StencilOp

```csharp
public enum BlendMode { Replace = 0, Add, Multiply, Alpha, AddAlpha, PremulAlpha, InvdestAlpha, Subtract, SubtractAlpha };
public enum CompareMode { Always = 0, Equal, NotEqual, Less, LessEqual, Greater, GreaterEqual, Max };
public enum CullMode { None = 0, CCW, CW, Max };
public enum FillMode { Solid = 0, Wireframe, Point };
public enum StencilOp { Keep = 0, Zero, Ref, Incr, Decr };
```

#### StencilState 结构

```csharp
public struct StencilState
{
    public bool StencilTest { get; set; }
    public CompareMode StencilCompare { get; set; }
    public int StencilRef { get; set; }
    public StencilOp PassOp { get; set; }
    public uint StencilReadMask { get; set; }
    public uint StencilWriteMask { get; set; }
    public static readonly StencilState Default = new();
}
```

> **详细教程**：参见 [材质系统指南](./Material_System_Guide.md)

### 自定义 Mesh

#### 创建自定义 Mesh

```csharp
Mesh mesh = Mesh.CreateCustomMesh(verts, indies, primitiveType);
```

**参数**：
- `vertexArray`：Vector3 数组，定义顶点位置
- `indexArray`：uint 数组，定义三角形索引
- `primitiveType`：渲染图元类型（通常用 `PrimitiveType.TriangleList`）

### StaticMeshComponent

```csharp
Node node = actor.GetNode();
StaticMeshComponent comp = node.GetComponent<StaticMeshComponent>();

// 设置 Mesh
comp.SetMesh(mesh);
comp.GetMesh();

// 设置材质
comp.SetMaterial(material);
comp.GetMaterial();

// 设置材质数组（多 Part 物体）
comp.SetMaterials(materials);
comp.GetMaterials();
```

---

**相关文档**：
- 🚀 [快速入门指南](./PhysicsGame_Quick_Start.md)
- 🎨 [材质系统指南](./Material_System_Guide.md)
- 🌐 [多人游戏开发](./PhysicsGame_Multiplayer_Guide.md)
- ✅ [最佳实践](./PhysicsGame_Best_Practices.md)

