# 核心差异

本文档详细介绍星火编辑器2.0与1.0版本在核心系统上的主要差异。

## 目录

- [场景坐标系统](#场景坐标系统)
- [触发器系统](#触发器系统)
- [数编表系统](#数编表系统)
- [数编Link系统](#数编link系统)
- [服务器与客户端代码](#服务器与客户端代码)
- [日志系统](#日志系统)
- [组件与实体](#组件与实体)

---

## 场景坐标系统

星火编辑器2.0在新建项目时，默认的场景镜头相比1.0版本**旋转了90度**。这是一个重要的变化，会影响到所有涉及坐标的操作。

### 坐标系差异对比

#### 2.0版本（新版本）

在默认俯视角情况下：
- **X轴正向**：指向场景地形的**右方**
- **Y轴正向**：指向场景地形的**下方**
- **原点(0,0)**：位于地图**左上角**
- **Z轴**：依然表示空间高度（垂直向上）

#### 1.0版本（旧版本）

在默认俯视角情况下：
- **X轴正向**：指向场景地形的**上方**
- **Y轴正向**：指向场景地形的**右方**
- **原点(0,0)**：位于地图**左下角**
- **Z轴**：表示空间高度

### 设计理念

#### 镜头手性保持一致

星火编辑器的镜头手性依然与Unreal等工具保持一致：
- **XY平面**对应地面
- **Z轴**表示空间高度

#### 改动原因

这个改动的主要目的是**让俯视角下地形的XY轴与UI的XY轴方向保持一致**。

在2.0版本中，由于UI系统和场景系统使用统一的坐标系：
- UI的X轴向右，Y轴向下
- 场景的X轴向右，Y轴向下
- 这样在开发2D/2.5D游戏时，场景坐标和UI坐标的方向保持一致，减少混淆

### 坐标示例

假设地图大小为6000x6000：

#### 2.0版本坐标系

```csharp
// 地图四个角的坐标
var topLeft = new ScenePoint(0, 0, scene);        // 左上角
var topRight = new ScenePoint(6000, 0, scene);    // 右上角
var bottomLeft = new ScenePoint(0, 6000, scene);  // 左下角
var bottomRight = new ScenePoint(6000, 6000, scene); // 右下角

// 地图中心
var center = new ScenePoint(3000, 3000, scene);
```

#### 1.0版本坐标系（对比）

```csharp
// 在1.0版本中，相同位置的坐标表示不同
// 左上角: (0, 0)      -> 2.0中为 (0, 0)
// 右上角: (6000, 0)   -> 2.0中为 (6000, 0)  
// 左下角: (0, 6000)   -> 2.0中为 (0, 6000)
// 右下角: (6000, 6000)-> 2.0中为 (6000, 6000)
```

### 自定义镜头角度

如果您不习惯新的坐标系，可以通过**修改场景相机**的方式改回1.0版本的镜头角度：

```csharp
// 示例：调整相机角度（具体方法请参考相机系统文档）
// 用户可以在项目初始化时旋转相机来恢复1.0的视角
```

---

## 触发器系统

2.0版本的触发器可以用非常直观的C#代码来构造。

### 基本用法

以下代码构造了一个触发器，当 `MyEvent` 事件发生时，触发器会调用 `OnMyEvent` 方法：

```csharp
bool OnMyEvent(object sender, MyEvent e)
{
    Game.Logger.LogInformation("Hello, World!");
    return true;
}

var trigger = new Trigger<MyEvent>(OnMyEvent);
trigger.Register(Game.Instance);
```

### 便捷的Subscribe方法（推荐）

为了简化触发器的创建和注册流程，2.0版本提供了 `Subscribe` 扩展方法，可以一行代码完成触发器的创建和注册：

```csharp
// 使用 Subscribe 方法，简洁明了
var trigger = Game.Instance.Subscribe<EventEntityDeath>(async (sender, e) =>
{
    Game.Logger.LogInformation("单位 {Name} 死亡", e.Entity.Name);
    return true;
});

// 等价于标准方式：
// var trigger = new Trigger<EventEntityDeath>(async (sender, e) => { ... });
// trigger.Register(Game.Instance);
```

#### 更多示例

```csharp
// 订阅玩家按键事件
var keyTrigger = Game.Instance.Subscribe<EventPlayerKeyDown>(async (sender, e) =>
{
    if (e.Key == VirtualKey.Escape)
    {
        ShowPauseMenu();
    }
    return true;
});

// 订阅特定单位的属性变化
var heroPropertyTrigger = heroUnit.Subscribe<EventUnitPropertyChange>(async (sender, e) =>
{
    Game.Logger.LogInformation("英雄属性变化: {Property}", e.Property);
    UpdateHeroUI();
    return true;
});

// 需要时可以销毁触发器
trigger.Destroy();
```

**Subscribe方法的优势**：
- ✅ **简洁**：一行代码完成创建和注册
- ✅ **不易出错**：避免忘记调用 `Register` 方法
- ✅ **直观**：API设计符合事件订阅的直觉

**何时使用标准方式**：
- 需要在创建后延迟注册的触发器
- 需要将同一触发器注册到多个事件发送者

### 注册到事件发送者

`Register` 方法用于将触发器注册到事件发送者。事件发送者可以是：
- `Game.Instance` - 全局事件发送者
- 其它实体对象 - 局部事件发送者
- 数编表对象 - 数据类型级别的事件发送者

### 多级注册示例

以单位死亡事件 `EventEntityDeath` 为例，触发器可以注册到不同的级别：

```csharp
var entityDeathTrigger = new Trigger<EventEntityDeath>(OnEntityDeath);

// 全局注册：任何单位死亡时都会触发
entityDeathTrigger.Register(Game.Instance);

// 单位实例注册：特定单位死亡时会触发
// entityDeathTrigger.Register(unit);

// 数编表注册：任何该数编表的单位死亡时会触发
// entityDeathTrigger.Register(unitData);
```

### 触发器生命周期

#### 摧毁触发器

使用 `Destroy()` 方法摧毁触发器：

```csharp
trigger.Destroy();
```

#### 引用管理

触发器默认是**长期引用**，直到手动摧毁。如果不需要长期引用，可以设置 `keepReference` 参数为 `false`：

```csharp
var trigger = new Trigger<MyEvent>(OnMyEvent, keepReference: false);
```

当 `trigger` 被垃圾回收时，触发器会自动被摧毁。

### 响应键盘事件示例

以下是一个响应玩家按键的触发器示例：

```csharp
Trigger<EventPlayerKeyDown> keyTrigger = new(static async (sender, e) =>
{
    Game.Logger.LogInformation("玩家 {Player} 按下了键盘按键 {key}", e.Player, e.Key);
    return true;
});
```

### 触发器委托签名

`Trigger<T>` 的构造函数接受一个委托 `Func<object, T, Task<bool>>`：

- **参数1**：`object sender` - 事件发送者对象
- **参数2**：`T` - 事件参数类型
- **返回值**：`Task<bool>` - 异步返回值，表示触发器是否通过了条件判断

目前返回值本身未起到任何作用，但将来可能会用于触发器执行次数统计。

### 异步支持

触发器天生接受异步函数委托，可以直接使用 `async/await`：

```csharp
var trigger = new Trigger<EventEntityDeath>(async (sender, e) =>
{
    await Game.Delay(1000); // 延迟1秒
    Game.Logger.LogInformation("单位死亡1秒后触发");
    return true;
});
```

### 深入学习

更多触发器系统的详细信息，请参阅：
- [触发器系统文档](../systems/TriggerSystem.md) - 完整的触发器系统文档

---

## 数编表系统

数编表现在可以非常方便地和其它类一样定义，只需要使用特性对这些类进行标记即可，代码生成器会自动生成必要的样板代码。

### 定义数编分类

`[GameDataCategory]` 可用于定义数编分类。

以下代码定义了一个名为 `GameDataUnit` 的数编表，该数编表类是一个独立的数编分类，拥有一个名为 `Name` 的属性。可为以该数编表类型为根，增加继承它的子类型数编表。

```csharp
[GameDataCategory]
public partial class GameDataUnit
{
    public string Name { get; set; }
}
```

⚠️ **注意**：`GameDataUnit` 在 WasiCore 框架中已经是一个官方数编表类型，因此不需要再定义。此处只是为了演示数编表的定义方式。您可以尝试定义其它数编表类型，比如 `GameDataMyClass`。

### 定义数编表子类型

`[GameDataNodeType<T,V>]` 可用于定义指定数编分类下的继承某个父类的数编表。

- **T** - 指定的数编分类
- **V** - 当前数编类型的直接父类（V需要继承T或者为T）

以下代码定义了一个名为 `GameDataUnitHero` 的数编表，该数编表类是 `GameDataUnit` 的子类：

```csharp
[GameDataNodeType<GameDataUnit, GameDataUnit>]
public partial class GameDataUnitHero
{
    public HeroType HeroType { get; set; }
}
```

### 使用 partial 修饰符

这两个特性是代码生成所使用的特性，使用时务必记得在类定义前添加 `partial` 修饰符。

使用支持C#语言的IDE可以在编辑器中查看自动生成的代码。

### 通过数编表构造对象

在2.0中，如果某个数编表拥有对应的游戏对象类型，则该数编表通常提供一个 `Create` 方法来构造数编表对应的实体/组件对象。

#### 内置的Create方法

- `GameDataUnit` - 拥有 `CreateUnit` 方法来构造 `Unit` 对象
- `GameDataAbility` - 拥有 `CreateAbility` 方法来构造 `Ability` 对象
- `GameDataItem` - 拥有 `CreateItem` 方法来构造 `Item` 对象
- `GameDataBuff` - 拥有 `CreateBuff` 方法来构造 `Buff` 对象

#### 创建单位示例

```csharp
public partial class GameDataUnit
{
    public virtual Unit CreateUnit(Player player, ScenePoint scenePoint, Angle facing, 
                                  IExecutionContext? creationContext = null, bool useDefaultAI = false);
}
```

用户在需要创建单位时，只需要调用 `CreateUnit` 方法即可：

```csharp
GameDataUnit myUnitData = ...;
Player player = Player.GetById(1);
ScenePoint scenePoint = new(100, 200, scene);
Angle facing = 90.0f;

var unit = myUnitData.CreateUnit(player, scenePoint, facing);
if (unit == null)
{
    Game.Logger.LogError("Failed to create unit");
    return;
}
```

#### 自定义Create方法

用户在构造自己的数编表类型时，如果数编表拥有对应的游戏对象，应当尽量添加对应的 `Create` 方法。

### 深入学习

更多数编表系统的详细信息，请参阅：
- [GameData系统文档](../systems/GameDataSystem.md) - 完整的数编表系统文档

---

## 数编Link系统

数编Link（或者叫数编Id）不再像1.0那样只能通过字符串来表示。

### 强类型数编Link

现在数编Link是**强类型的泛型结构体**，定义了指向特定数编表的引用。数编表的类型能通过泛型参数直接反映到数编Link的类型上，避免了1.0中因为字符串引用而导致的类型安全问题。

以下代码定义了一个名为 `HostTestHero` 的数编Link，该数编Link指向了类型为 `GameDataUnitHero` 的数编表：

```csharp
public static readonly GameLink<GameDataUnit, GameDataUnitHero> HostTestHero = new("HostTestHero"u8);
```

### 泛型参数说明

`GameLink<T,V>` 的泛型参数：
- **T** - 指定的数编分类
- **V** - 数编表的类型

这与 `[GameDataNodeType<T,V>]` 的泛型参数类似，但有所不同：在 `GameDataNodeType` 中，V是当前数编类型的父类，而在 `GameLink` 中，V是数编表的具体类型。

### IGameLink<T> 接口

除了 `GameLink<T,V>` 外，数编类型还能以泛型接口 `IGameLink<T>` 的形式存在。

- `GameLink<T,V>` - 通常只用于定义一个数编Link
- `IGameLink<T>` - 通常用在属性类型或方法参数类型上

`GameLink<T,V>` 所对应的数编表类型必定为V，但 `IGameLink<T>` 所对应的数编表类型可以为V的子类。

#### 语义说明

`IGameLink<T>` 的语义是：我需要一个能转换成数编表类型V的数编Link，它对应的数编表不需要是V类型，可以是V的子类，我也不关心它是哪个分类的。

#### 使用示例

```csharp
// 参数类型为 IGameLink<GameDataUnit> 时
void ProcessUnit(IGameLink<GameDataUnit> unitLink)
{
    // 可以传入 GameLink<GameDataUnit, GameDataUnitHero>
    // 也可以传入 GameLink<GameDataUnit, GameDataUnit>
}
```

### 比较操作

⚠️ **重要**：由于 `IGameLink<T>` 是一个泛型接口，不应当使用 `==` 运算符来比较两个 `IGameLink<T>` 类型的实例。应当使用 `Equals` 方法。

但 `GameLink<T,V>` 类型重载了 `==` 运算符，可以使用 `==` 运算符来比较两个 `GameLink<T,V>` 类型的实例。

### 注册数编表

在2.0中，数编Link的注册可以通过代码来完成（新版本的数据编辑器同样也是通过生成代码来完成数编Link的注册）。

以下代码代表构造一个数编表，并将其注册到数编 `HostTestHero` 这个数编Link：

```csharp
GameLink<GameDataUnit, GameDataUnitHero> HostTestHero = new("HostTestHero"u8);
new GameDataUnitHero(HostTestHero)
{
    Name = "HostTestHero"
};
```

### 数编Link和数编表的互相获取

在2.0中，由于数编表和数编Link融入到了C#的类型系统中，它们可以通过面向对象属性来互相获取，不再需要繁琐的"通过数编Id获取数编表"这种操作了。

#### Link → Data

数编Link可以通过 `Data` 属性获取到对应的数编表。如果一个数编Link尚未被注册，则其 `Data` 属性为 `null`。

```csharp
var heroData = HostTestHero.Data;
```

#### Data → Link

数编表可以通过 `Link` 属性获取到对应的数编Link。数编表 `Link` 属性必定不为空。

```csharp
var heroLink = heroData.Link;
```

### 类型推断

`Data` 和 `Link` 两个属性都是强类型泛型属性，它们会根据数编Link的泛型参数类型自动推断出对应的数编表类型或反之。

在上面的例子中：
- `heroData` 的类型会是 `GameDataUnitHero`
- `heroLink` 的类型为 `GameLink<GameDataUnit, GameDataUnitHero>`

使用IDE进行开发的用户可以通过IDE的 `var` 关键字类型提示功能来查看具体的类型，或将鼠标悬停在 `var` 关键字上查看具体的类型。

### 类型安全的优势

在Link和数编表互相转换的过程中，无需再手动进行不安全的强制类型转换了。这与1.0版本形成鲜明对比，因为1.0所使用的TypeScript2lua并非真正的运行时强类型，也无法保证编译时类型安全。

---

## 服务器与客户端代码

星火编辑器2.0版本在设计时考虑了服务器与客户端代码的分离和共享。

### 条件编译指令

我们将项目的服务器代码和客户端代码放到同一个项目中，并使用 `#if SERVER` 和 `#if CLIENT` 条件编译指令来分离服务器与客户端代码。

```csharp
// 公共代码 - 可以在服务器和客户端共享
public class MyClass
{
    public void MyMethod()
    {
        Game.Logger.LogInformation("Hello, World!");
    }
}

#if SERVER
// 服务器代码
public class MyServerClass : MyClass
{
    public void MyServerMethod()
    {
        Game.Logger.LogInformation("Hello, Server!");
    }
}
#endif

#if CLIENT
// 客户端代码
public class MyClientClass : MyClass
{
    public void MyClientMethod()
    {
        Game.Logger.LogInformation("Hello, Client!");
    }
}
#endif
```

### 云数据访问

云数据（Cloud Data）访问必须只在服务器端执行，代码应被包裹在 `#if SERVER` 预处理指令中：

```csharp
#if SERVER
// 云数据操作必须在服务器端
var cloudData = await CloudDataManager.GetAsync(key);
#endif
```

### 异步调用注意事项

在本框架中，不要使用 `Task.Run()` 来调用异步函数。`Task.Run()` 在本框架中无法正常使用。

正确的做法：
```csharp
// ✅ 正确：直接调用异步函数
await MyAsyncMethod();

// ✅ 正确：不使用await也可以
MyAsyncMethod();

// ❌ 错误：不要使用 Task.Run()
// Task.Run(() => MyAsyncMethod());
```

同样，使用 `Game.Delay()` 替代 `Task.Delay()`：

```csharp
// ✅ 正确
await Game.Delay(1000);

// ❌ 错误
// await Task.Delay(1000);
```

---

## 日志系统

WasiCore使用 `Game.Logger` 作为基本的日志输出方式。

### 基本用法

```csharp
Game.Logger.LogInformation("🎮 开始运行AI形状系统示例...");
Game.Logger.LogWarning("注意事项: {Message}", warningMessage);
Game.Logger.LogError("发生错误: {ErrorCode}", errorCode);
```

### 禁止使用 Console.WriteLine

⚠️ **重要**：WasiCore 框架使用** `Console.WriteLine`**是无效的。

```csharp
// ✅ 正确：使用参数化消息（结构化日志）
Game.Logger.LogInformation("操作成功: {OperationType}", operationType);

// 可以使用字符串内插，但不推荐。
Game.Logger.LogInformation($"操作成功: {operationType}");

// ❌ 错误：不应当使用 Console.WriteLine
Console.WriteLine("这不会显示到日志里");
```

### 深入学习

更多日志系统的详细信息，请参阅：
- [日志系统文档](../systems/LoggingSystem.md) - 完整的日志系统文档

---

## 组件与实体

星火编辑器2.0版本的对象关系被重新设计，引入了组件与实体的概念。

### 基本概念

**实体（Entity）**：
- 用于实体级别的属性同步和网络复制
- 服务端权威，所有实体必须在服务端创建
- 1.0的单位在2.0属于实体

**组件（Component）**：
- 可以依附于实体
- 利用实体来实现组件的数据同步和网络复制
- 1.0的Buff和技能在2.0属于组件

### 自定义实体和组件

用户可以构造自己的实体和组件，并利用实体和组件来实现自己的游戏逻辑。

组件和实体在服务端和客户端拥有对应的关系，它们在服务器和客户端共享许多API。

### 客户端组件信息

在2.0版本中，组件拥有自己的客户端版本，拥有许多和服务器版本一致的API。用户可以以非常直观方便的方法获取它们的信息。

#### 遍历物品加成属性示例

以下遍历物品动态加成属性的代码在服务端和客户端都有效：

```csharp
ItemMod itemMod = ...; // ItemMod是有加成效果的物品类型

// 遍历物品的动态属性修改
foreach (var dynamicMod in itemMod.EnumerateDynamicModifications(slotType))
{
    Game.Logger.LogInformation($"动态属性加成: {dynamicMod.SlotType}, {dynamicMod.PropertyLink}, {dynamicMod.SubType}, {dynamicMod.Value}");
}
```

"物品动态加成属性"即1.0中的"物品额外属性"概念，现在它被重新设计，命名为"物品动态属性"。

### 内部UI源码参考

我们还将公开我们内部实现物品栏和技能摇杆UI控件的C#源码。这些界面是和用户一样的刚刚开始学习星火编辑器2.0的初学者编写的，用户可以参考它们来实现自己的UI界面。

---

## 下一步

了解了核心差异后，建议继续阅读：
- [高级功能](./02-ADVANCED-FEATURES.md) - 学习扩展枚举、效果节点等高级功能
- [UI系统](./03-UI-SYSTEM.md) - 了解2.0版本的UI创建方式

