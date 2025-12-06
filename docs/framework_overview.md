# WasiCore 框架概述

## 📐 架构设计

WasiCore 采用分层架构设计，各层职责明确，通过接口进行通信。

### 系统架构图

```
┌─────────────────────────────────────────────────────────┐
│                     应用层 (Game Entry)                  │
├─────────────────────────────────────────────────────────┤
│                      UI层 (GameUI)                       │
├─────────────────────────────────────────────────────────┤
│                   游戏逻辑层 (GameCore)                  │
├─────────────────────────────────────────────────────────┤
│                   数据层 (GameData)                      │
├─────────────────────────────────────────────────────────┤
│               引擎层 (Engine Interface)                  │
├─────────────────────────────────────────────────────────┤
│                   平台层 (Platform)                      │
└─────────────────────────────────────────────────────────┘
```

### 网络架构

```
┌──────────────┐         ┌──────────────┐
│   客户端     │◄────────►│   服务器     │
│  (Client)    │         │  (Server)    │
├──────────────┤         ├──────────────┤
│ GameUI       │         │ Game Logic   │
│ Rendering    │         │ Simulation   │
│ Input        │         │ Authority    │
├──────────────┤         ├──────────────┤
│ Protocol     │         │ Protocol     │
│ Client       │         │ Server       │
└──────────────┘         └──────────────┘
      ▲                          ▲
      └──────── Network ─────────┘
           (MessagePack)
```

## 🎯 核心概念

### 1. 游戏类自动注册 (IGameClass Auto-Registration)

WasiCore 框架提供了自动游戏类注册机制，简化了系统初始化过程。

#### 🔧 IGameClass 接口

```csharp
/// <summary>
/// Register game classes that are used in the game setups.
/// </summary>
public interface IGameClass
{
    static abstract void OnRegisterGameClass();
}
```

#### ⚡ 自动注册机制

**实现方式**：
1. **代码生成器扫描**：编译时自动扫描所有实现 `IGameClass` 的类
2. **自动生成注册代码**：生成 `AssemblySetup.g.cs` 文件
3. **运行时自动调用**：游戏启动时自动调用所有 `OnRegisterGameClass()` 方法

**使用示例**：
```csharp
// 定义游戏类
public class MyGameSystem : IGameClass
{
    public static void OnRegisterGameClass()
    {
        // 系统初始化逻辑
        Game.OnGameDataInitialization += OnGameDataInitialization;
        Game.Logger.LogInformation("🎮 MyGameSystem initialized");
    }
    
    private static void OnGameDataInitialization()
    {
        // 游戏数据初始化
    }
}
```

**生成的注册代码**：
```csharp
// 自动生成的 AssemblySetup.g.cs
public static class AssemblySetup
{
    public static void Setup()
    {
        // 自动注册所有 IGameClass
        GameCore.GameSystem.Game.OnRegisterGameClasses += 
            GameCore.GameSystem.Game.RegisterGameClass<MyGameSystem>;
    }
}
```

#### 🌟 主要优势

- **零配置**：无需手动注册，编译时自动处理
- **类型安全**：编译时检查，避免运行时错误
- **依赖管理**：支持程序集间的依赖关系自动处理
- **统一初始化**：所有系统在统一的生命周期中初始化

#### ⚠️ 重要提示

- **无需手动调用**：`OnRegisterGameClass()` 方法会被框架自动调用
- **静态方法**：必须是静态方法，因为在类实例化之前调用
- **幂等性**：确保多次调用的安全性（避免重复注册）

### 2. 坐标系统 (Coordinate System)

WasiCore 框架采用特定的坐标系统约定，请务必区分2D场景和3D空间的不同约定。

#### 🎯 重要区分：2D场景 vs 3D空间

**2D场景坐标系统（UI、地图俯视图）**：
```
场景坐标系统（2D俯视视角）：

(0,0) ←─────────────────────► X轴正方向（右方）
  │
  │
  │
  │
  ▼
Y轴正方向（下方）
```

**3D空间坐标系统（实际游戏世界）**：
```
3D空间坐标系统：

       ▲ Z轴正方向（上方，高度轴）
       │
       │
       │
(0,0,0)├─────────────► Y轴正方向（前后，深度）
      ╱
     ╱
    ▼
X轴正方向（左右，水平）

XY平面 = 地面
```

#### 📐 关键约定说明

**2D场景约定**（用于UI、小地图等）：
- **原点位置**：场景 `(0,0)` 点位于地图的**左上方**
- **X轴正方向**：指向**右方**
- **Y轴正方向**：指向**下方**

**3D空间约定**（用于游戏世界、物理、形状等）：
- **X轴正方向**：水平方向（左右移动）
- **Y轴正方向**：水平方向（前后移动，深度）
- **Z轴正方向**：高度方向（上下移动，**跳跃轴**）
- **XY平面**：地面水平面
- **Z轴**：高度轴，Z=0为地面，Z>0为空中

#### 🤚 坐标系手性说明

**WasiCore使用左手坐标系（Left-handed coordinate system）**，与Unreal Engine保持一致：

```
左手定则验证：
👍 拇指 → X轴正方向（右）
👆 食指 → Y轴正方向（前）  
🖕 中指 → Z轴正方向（上）
```

**默认镜头设置**：
- **Yaw**: -90° (水平旋转)
- **Pitch**: -70° (俯视角)
- **Roll**: 0° (无倾斜)
- **默认视角**：俯视角2.5D镜头
- **默认镜头下的原点位置**：左上角

这种设计的优势：
- **兼容性**：与Unreal Engine的坐标系完全一致
- **标准化**：遵循主流3D引擎的左手坐标系约定
- **简化开发**：减少从其他引擎迁移的坐标转换复杂度

#### ⚠️ 与其他引擎的差异

| 引擎/系统 | 原点位置 | Y轴正方向 | 坐标系手性 | 备注 |
|-----------|----------|-----------|-----------|------|
| **WasiCore** | 左上角 | 前方 | 左手坐标系 | 与UE一致，默认俯视角，默认镜头Yaw有-90°的旋转，因此默认镜头下，原点位置在左上角 |
| Unity | 中心或左下 | 上方 | 左手坐标系 | 3D世界坐标系 |
| Unreal Engine | 中心 | 前方 | 左手坐标系 | 与WasiCore一致 |
| DirectX | 左下 | 上方 | 左手坐标系 | 传统3D约定 |
| OpenGL | 左下 | 上方 | 右手坐标系 | 传统3D约定 |
| Web/UI | 左上 | 下方 | 2D坐标系 | 仅2D平面 |

#### 🎯 实际应用场景

这种坐标系统约定会影响以下开发场景：

**1. 2D场景使用（地图、UI）**：
```csharp
// 地图左上角（2D场景）
var topLeft = new ScenePoint(0, 0, scene);

// 向右移动100单位（2D场景）
var moveRight = new ScenePoint(100, 0, scene);

// 向下移动100单位（2D场景）
var moveDown = new ScenePoint(0, 100, scene);
```

**2. 3D空间使用（游戏世界）**：
```csharp
// 地面位置（3D空间）
var groundPosition = new ScenePoint(new Vector3(100, 200, 0), scene);

// 跳跃中的角色（3D空间）
var jumpingPlayer = new ScenePoint(new Vector3(100, 200, 50), scene);

// 飞行物体（3D空间）
var flyingObject = new ScenePoint(new Vector3(100, 200, 150), scene);
```

**3. 物理和移动（3D空间）**：
```csharp
// 水平移动：X或Y增加
unit.MoveTo(new ScenePoint(new Vector3(newX, newY, currentZ), scene));

// 垂直移动（跳跃）：Z增加
unit.MoveTo(new ScenePoint(new Vector3(currentX, currentY, newZ), scene));

// 重力下落：Z减少
velocity.Z -= gravity * deltaTime;
```

#### 💡 开发建议

**⚠️ 针对AI助手的关键提醒**：
1. **3D游戏开发**：始终使用Z轴作为高度轴（跳跃、重力、飞行）
2. **2D界面开发**：使用传统的XY坐标系（Y向下）
3. **物理系统**：重力作用在Z轴负方向，跳跃在Z轴正方向
4. **形状创建**：Vector3(x, y, z)中，XY为地面位置，Z为高度
5. **碰撞检测**：地面接触检查Z坐标，水平距离检查XY坐标

**2D方向常量（UI、地图）**：
```csharp
public static class Direction2D
{
    public static readonly Vector2 Up = new(0, -1);      // Y减少（2D向上）
    public static readonly Vector2 Down = new(0, 1);     // Y增加（2D向下）
    public static readonly Vector2 Left = new(-1, 0);    // X减少
    public static readonly Vector2 Right = new(1, 0);    // X增加
}
```

**3D方向常量（游戏世界）**：
```csharp
public static class Direction3D
{
    public static readonly Vector3 Forward = new(0, 1, 0);   // Y正方向（前）
    public static readonly Vector3 Backward = new(0, -1, 0); // Y负方向（后）
    public static readonly Vector3 Left = new(-1, 0, 0);     // X负方向（左）
    public static readonly Vector3 Right = new(1, 0, 0);     // X正方向（右）
    public static readonly Vector3 Up = new(0, 0, 1);        // Z正方向（上，跳跃方向）
    public static readonly Vector3 Down = new(0, 0, -1);     // Z负方向（下，重力方向）
}
```

> 💡 **重要提醒**：在3D游戏开发中，请始终确认您使用Z轴作为高度轴。这将避免跳跃错误、重力方向错误等常见问题。详细信息请参考 [3D坐标系统指南](../COORDINATE_SYSTEM_GUIDE.md)。

### 3. 实体系统 (Entity System)

实体系统是游戏对象的基础，专门处理游戏逻辑、状态管理和网络同步。

```csharp
// 实体层次结构（游戏逻辑）
Entity
├── Unit       // 单位（玩家、NPC）
├── Building   // 建筑
├── Item       // 物品
├── Effect     // 逻辑效果
└── Projectile // 投射物
```

**关键特性**：
- **组件化设计**：实体通过组件组合功能
- **生命周期管理**：自动处理创建、更新、销毁
- **网络同步**：内置网络同步机制
- **服务端权威**：所有实体必须在服务端创建

### 4. 演员系统 (Actor System)

演员系统专注于游戏世界的视觉表现，与实体系统形成明确的职责分离。

```csharp
// 演员层次结构（视觉表现）
Actor
├── ActorModel     // 模型演员
├── ActorParticle  // 粒子演员
├── ActorSound     // 声音演员
├── ActorText      // 文本演员
├── ActorBeam      // 光束演员
└── ActorSegmented // 分段演员
```

**关键特性**：
- **轻量级创建**：客户端可直接创建
- **视觉专注**：专门处理模型、特效、声音
- **灵活生命周期**：支持瞬态和持久化
- **附着系统**：支持复杂的视觉层次结构

### 5. 架构分离原则

```
游戏对象架构 - 职责分离：

Entity 家族（游戏逻辑）          Actor 家族（视觉表现）
┌─────────────────────┐        ┌─────────────────────┐
│ • 服务端权威        │        │ • 客户端创建        │
│ • 状态同步          │        │ • 视觉效果          │
│ • 游戏逻辑          │        │ • 轻量级            │
│ • 属性管理          │        │ • 瞬态/持久         │
└─────────────────────┘        └─────────────────────┘
         │                              │
         └─────── 可以协作 ─────────────┘
```

**设计原则**：
- **Entity** 负责 "逻辑的" - 状态、属性、行为、同步
- **Actor** 负责 "看到的" - 模型、特效、动画、声音
- 两者可以协作但职责明确分离

### 6. 技能系统 (Ability System)

技能系统提供了灵活的技能定义和执行框架。

**核心组件**：
- **Ability**：技能定义
- **Effect**：技能效果
- **Modifier**：属性修改器
- **Cooldown**：冷却管理
- **Cost**：消耗管理

**执行流程**：
1. 检查施法条件（冷却、消耗、目标）
2. 支付消耗
3. 执行效果链
4. 应用修改器
5. 触发事件

### 7. UI系统 (UI System)

基于控件树的UI系统，支持数据绑定和事件路由。

**控件层次**：
```
UIRoot
├── Control
│   ├── Panel
│   ├── Button
│   ├── Label
│   └── ...
└── Layout
    ├── StackPanel
    ├── Grid
    └── Canvas
```

**特性**：
- **属性系统**：丰富的控件属性
- **事件路由**：冒泡和隧道事件
- **数据绑定**：MVVM模式支持
- **布局系统**：灵活的布局管理

#### UI模板数据结构 vs 运行时属性

⚠️ **重要区别**：UI系统中的属性访问方式在模板定义和运行时控件中有所不同。

**在UI模板 (GameData) 中**：
```csharp
// 布局属性位于 Layout 对象下
_ = new GameDataControlButton(Control.TestButton)
{
    Layout = new()  // 布局属性在 Layout 对象中
    {
        Width = 500,
        Height = 90,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
    },
    Event = new()   // 事件位于 Event 对象下
    {
        OnPointerClicked = (s, e) => { /* 处理点击 */ }
    }
};
```

**在运行时控件类中**：
```csharp
// 属性直接作为控件的属性
var button = new Button
{
    Width = 500,                    // 直接属性
    Height = 90,                    // 直接属性
    HorizontalAlignment = HorizontalAlignment.Center,  // 直接属性
    VerticalAlignment = VerticalAlignment.Center       // 直接属性
};

// 事件直接订阅
button.OnPointerClicked += (s, e) => { /* 处理点击 */ };  // 直接事件
```

**关键差异总结**：
- **模板定义**：`Layout.Width`、`Event.OnPointerClicked`
- **运行时控件**：`Width`、`OnPointerClicked`（直接属性/事件）

这种设计允许模板系统更好地组织和管理复杂的UI数据结构，同时为运行时提供简洁直观的API。

### 8. 事件系统 (Event System)

基于发布-订阅模式的事件系统，支持同步和异步事件。

```csharp
// 事件流程
EventSource → EventBus → EventHandler
     ↓            ↓            ↓
  发布者      事件总线      订阅者
```

**事件类型**：
- **游戏事件**：游戏逻辑相关
- **UI事件**：用户交互
- **网络事件**：网络状态变化
- **系统事件**：系统级别事件

### 9. 数据系统 (Data System)

统一的游戏数据管理系统，支持热重载和版本控制。

**数据类型**：
- **静态数据**：配置表、常量
- **动态数据**：运行时数据
- **持久化数据**：存档数据

## 🔄 工作流程

### 1. 游戏初始化流程

```
1. 平台初始化
   ├── 加载配置
   ├── 初始化日志
   └── 设置环境
   
2. 引擎初始化
   ├── 创建渲染器
   ├── 初始化输入
   └── 启动网络
   
3. 游戏系统初始化
   ├── 加载游戏数据
   ├── 创建游戏世界
   └── 初始化UI
   
4. 进入游戏循环
```

### 2. 游戏循环

```csharp
while (running)
{
    // 输入处理
    InputManager.ProcessInput();
    
    // 逻辑更新
    GameWorld.Update(deltaTime);
    EntityManager.Update(deltaTime);
    AbilitySystem.Update(deltaTime);
    
    // 网络同步
    NetworkManager.SendUpdates();
    NetworkManager.ReceiveUpdates();
    
    // UI更新
    UIManager.Update(deltaTime);
    
    // 渲染
    Renderer.Render();
}
```

### 3. 网络同步流程

**客户端预测**：
1. 客户端执行操作
2. 发送操作到服务器
3. 继续本地模拟
4. 接收服务器确认
5. 校正本地状态

**服务器权威**：
1. 接收客户端输入
2. 验证合法性
3. 执行游戏逻辑
4. 广播状态更新
5. 处理冲突解决

### 4. 资源加载流程

```
资源请求 → 缓存检查 → 加载器选择 → 异步加载 → 缓存更新 → 回调通知
```

## 🔧 扩展机制

### 1. 插件系统

支持通过插件扩展功能：
- **游戏插件**：新增游戏玩法
- **UI插件**：自定义UI组件
- **工具插件**：开发工具扩展

### 2. 自定义组件

通过继承基类创建自定义组件：
```csharp
public class MyComponent : Component
{
    public override void OnAttached() { }
    public override void Update(float deltaTime) { }
    public override void OnDetached() { }
}
```

### 3. 数据扩展

通过数据模板扩展游戏内容：
- 技能模板
- 物品模板
- AI行为模板

## 🛡️ 安全机制

### 1. 客户端验证
- 输入合法性检查
- 范围限制
- 频率限制

### 2. 服务器验证
- 权限验证
- 数据完整性检查
- 防作弊机制

### 3. 网络安全
- 加密传输
- 防重放攻击
- DDoS防护

## 🌐 平台兼容性

### WebAssembly (Wasm) 运行环境

WasiCore 框架针对 WebAssembly 环境进行了特殊优化，但也带来了一些技术限制：

#### ⚠️ 重要限制

1. **多线程限制**
   - 当前框架暂不支持多线程操作
   - 所有逻辑运行在单一主线程中
   - 避免使用 `Thread`、`Task.Run` 等多线程API

2. **异步编程约束**
   - 必须使用框架内置的异步方法
   - 使用 `Game.Delay()` 替代 `Task.Delay()`
   - 确保异步操作与游戏tick对齐

#### 🎯 Wasm 优化特性

- **快速启动**：针对 Wasi 环境优化的启动流程
- **内存效率**：减少内存占用，适应浏览器限制
- **网络优化**：适配 Web 网络环境的同步机制

## 📊 性能优化

### 1. 内存管理
- 对象池
- 内存复用
- 垃圾回收优化

### 2. 渲染优化
- 批处理
- 视锥剔除
- LOD系统

### 3. 网络优化
- 增量更新
- 压缩算法
- 优先级队列

## 🔍 调试支持

### 1. 日志系统
- 分级日志
- 日志过滤
- 远程日志

### 2. 性能分析
- 帧率监控
- 内存分析
- 网络流量分析

### 3. 调试工具
- 实体查看器
- 属性编辑器
- 控制台命令 