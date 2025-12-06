# WasiCore API 参考文档

## 📚 目录

- [GameData API](#gamedata-api)
  - [数据接口](#数据接口)
- [GameCore API](#gamecore-api)
  - [实体系统](#实体系统)
  - [技能系统](#技能系统)
  - [执行系统](#执行系统)
  - [组件系统](#组件系统)
- [GameUI API](#gameui-api)
  - [控件系统](#控件系统)
  - [事件系统](#事件系统)
  - [输入系统](#输入系统)
  - [Canvas 2D绘图系统](#canvas-2d绘图系统)
- [网络API](#网络api)

---

## GameData API

> 📖 **详细文档**：[GameData 数据驱动系统](systems/GameDataSystem.md)

WasiCore 引擎的数据驱动架构是其核心特性之一，通过 `IGameData`（数编表）和 `IGameLink`（数编Link）实现游戏数据与逻辑的完全解耦。

### 核心组件

- **IGameData（数编表）** - 游戏对象的数据模板和配置
- **IGameLink（数编Link）** - 类型安全的数据引用机制
- **GameDataCategory** - 数据分类管理和查找
- **可视化编辑器** - 数据的可视化编辑工具

### 主要优势

- 🔥 **热更新支持** - 运行时数据变更
- 🛡️ **类型安全** - 编译时类型检查
- ⚡ **高性能** - O(1)复杂度数据查找
- 🔄 **跨平台同步** - 服务端客户端数据一致

### 基本用法

#### 创建数编Link

```csharp
// 通过字符串ID创建（推荐使用UTF8字面量）
var heroLink = new GameLink<GameDataUnit, GameDataUnit>("HostTestHero"u8);

// 全局定义（通常由数据编辑器生成）
public static class ScopeData
{
    public static readonly GameLink<GameDataUnit, GameDataUnit> TestHero = new("TestHero"u8);
}
```

#### 使用数编表

```csharp
// 获取数编表数据
var heroData = ScopeData.TestHero.Data;
if (heroData != null)
{
    Game.Logger.LogInformation("英雄名称: {HeroName}", heroData.Name);
    
    // 创建游戏对象
    var unit = heroData.CreateUnit(player, position);
}
```

## GameCore API

WasiCore 的数据驱动架构基于以下核心理念：

- **数据与逻辑分离** - 游戏逻辑不直接依赖具体数据，通过数编Link进行间接引用
- **类型安全访问** - 泛型设计确保编译时类型检查，避免运行时类型错误  
- **高效数据查找** - 基于确定性哈希算法，实现O(1)复杂度的数据定位
- **热更新友好** - 数据变更不影响已有的引用关系，支持运行时动态切换

### 核心组件说明

#### GameData（数编表）

数编表是游戏数据的实际载体，定义了游戏对象的静态属性、配置和行为模板。

**特点：**
- 继承自 `GameDataCategory<T>` 基类
- 构造时自动注册到全局和类别目录
- 支持通过可视化编辑器编辑
- 服务端与客户端数据完全同步

**常见类型：**
- `GameDataUnit` - 单位数据（生命值、攻击力、技能等）
- `GameDataAbility` - 技能数据（冷却时间、效果、范围等）
- `GameDataItem` - 物品数据（属性加成、使用效果等）
- `GameDataScene` - 场景数据（地图、资源、限制等）

#### GameLink（数编Link）

数编Link是轻量级的值类型结构体，提供对数编表的类型安全引用。

**特点：**
- 值类型结构体，内存开销极小
- 支持字符串ID和HashCode两种构造方式
- 全局唯一性保证，避免重复引用
- 延迟加载，只在访问Data属性时进行查找

**哈希码机制：**
```csharp
// 全局HashCode = 本地HashCode ⊕ 类型HashCode
public int HashCode { get; } = HashCodeDeterministic.Combine(hashCodeLocal, TCategory.TypeHashCode);
```

### 构造方式

- **GameData** 通常通过数据编辑器或配置文件生成，包含静态属性和行为定义，也可以通过代码来生成。每个 GameData 实例对应一个具体的游戏对象类型，如单位、技能、物品等。

每个 **GameData** 的实例通常被称为一张"数编表"或者一个"数编节点"。而其类型定义则被称为"节点类型"。一个数编节点类型是一个 GameData 的具体实现类，如 `GameDataUnit`、`GameDataAbility` 等。

每个 GameData 实例都必须对应一个 GameLink 实例，GameLink 通过唯一的 HashCode 或字符串 Id 来标识和引用 GameData。

数编表通常会在服务器和客户端中生成两份，通过代码方式来构造数编表时，需要确保两边的数编表数据一致。

- **GameLink** 则是通过代码或数据编辑器创建的引用对象，为值类型结构体。

GameLink<TCategory, V> 是一个泛型结构体，定义了指向特定 GameData 类型的引用。它可以通过字符串 Id 或 HashCode 来唯一标识一个 GameData 实例。

#### GameLink 的构造示例

全局通用的 数编Link 实例通常在游戏初始化时创建，并在整个游戏生命周期内保持不变。用户可以通过它们来引用和访问 GameData 实例。由数据编辑器生成的数编Link 实例通常会定义在一个名为 [项目命名空间].ScopeData 的静态类中，便于全局访问。

```csharp
public static readonly GameLink<GameDataUnit, GameDataUnit> HostTestHero = new("HostTestHero"u8);
```

#### GameData 的构造示例

GameData 实例通常在游戏初始化时创建，并在整个游戏生命周期内保持不变。用户可以通过 GameLink 来引用和访问这些 GameData 实例。
在构造 GameData 实例时，必须指定一个 GameLink 实例作为其唯一标识符。GameData的构造函数包含将自身注册到 GameLink 的逻辑。

```csharp
        new GameDataUnit(Unit.HostTestHero)
        {
            Name = "测试英雄",
            AttackableRadius = 50,
            Properties = new() {
                { UnitProperty.LifeMax, 1000 },
                { UnitProperty.ManaMax, 1000 },
                { UnitProperty.Armor, 10 },
                { UnitProperty.MagicResistance, 10 },
                { UnitProperty.MoveSpeed, 300 },
                { UnitProperty.TurningSpeed, 1500 },
                { UnitProperty.AttackRange, 150 },
                { UnitProperty.InventoryPickUpRange, 200 },
            },
            UpdateFlags = new()
            {
                AllowMover = true,
                Turnable = true,
                Walkable = true,
            },
            VitalProperties = new()
            {
                { PropertyVital.Health, UnitProperty.LifeMax }
            },
            CollisionRadius = 32,
            DynamicCollisionMask = DynamicCollisionMask.Hero | DynamicCollisionMask.Building,
            Inventories = [Inventory.TestInventory6, Inventory.TestInventory6Equip],
            Filter = [UnitFilter.Hero, UnitFilter.Unit],
            DeathRemovalDelay = Timeout.InfiniteTimeSpan,
            ActorArray = [
                Actor.TestActorAdditionModel,
            ],
            Model = Model.HostTestHero,
        };
```

#### GameData 的获取示例

```csharp
// 获取 GameData 实例
var heroData = ScopeData.Unit.HostTestHero.Data;
```

每个 GameData 都必然对应一个GameLink，但 GameLink 不一定对应一个 GameData 实例。因此在取其 Data 属性时，需要注意可能为 null 的情况。

此外，GameLink 并不必须要记录在全局静态类中，用户可随时通过它们的唯一标识符（如字符串 Id 或 HashCode）来创建和引用 GameLink 实例。

```csharp
// 通过字符串 Id 获取 GameLink
var heroLink = new GameLink<GameDataUnit, GameDataUnit>("HostTestHero"u8);
// 通过 HashCode 获取 GameLink

上面的 heroLink 实例通过其 Data 属性所获得的 GameDataUnit 实例与全局静态类中的 HostTestHero 所获取的是同一个数编表。

下面的代码也是等价的：

```csharp
// 通过 HashCode 获取 GameLink
var heroLink = new GameLink<GameDataUnit, GameDataUnit>(1234567890); // 假设 1234567890 是 HostTestHero 的 HashCode
```

多数时候，数编表会对应着一些游戏对象的配置，如单位、技能、物品等。在构造这些对象时，通常会需要传入一个 GameLink 实例来关联到对应的 GameData 配置。这类数编表甚至多数拥有直接通过自身来构造对象的方法。

例如，`GameDataUnit` 类通常会提供一个 `CreateUnit` 方法，用于创建一个新的单位实例，并将其与 GameLink 关联。

以下是创建一个属于玩家3，坐标为 (10, 10)，场景为 TestScene 的 HostTestHero 单位的示例代码：

```csharp
 Unit unit = ScopeData.Unit.HostTestHero.Data!.CreateUnit(Player.GetById(3)!, new(10, 10, ScopeData.Scene.TestScene, 0);
```

而这些游戏对象通常会实现 IGameObject 接口，提供一些通用的属性和方法，如获取 GameLink 和 GameData 的方法。

```csharp
public interface IGameObject
{
    // 游戏对象的数编Link
    public IGameLink Link { get; }
    // 游戏对象的数编表数据
    public IGameData Cache { get; }
}
```

### 典型应用场景

- 单位、技能、物品等对象通过 GameLink 关联到对应的 GameData 配置，实现属性、行为、表现等的动态切换。
- 通过 GameLink 实现数据的唯一性、查找和复用，支持夸依赖库的数据补丁和。
- 组件、AI、UI 等系统通过 GameLink 访问和引用数据模板，实现高度的数据驱动和可扩展性。

---

## GameCore API

### 实体系统

#### Entity 类

实体基类，服务端同步游戏对象的基类。拥有场景、位置、所属玩家、同步属性等等特性。
所有以服务端为中心，拥有坐标的游戏对象都应该继承自此类。

```csharp
public class Entity
{
    /// <summary>
    /// 创建实体
    /// </summary>
    /// <param name="scene">所属场景</param>
    /// <param name="position">初始位置</param>
    public Entity(Scene scene, Vector3 position);
    
    /// <summary>
    /// 销毁实体
    /// </summary>
    public virtual void Destroy();
}
```

### 技能系统

#### Ability 类

技能基类实现。

```csharp
public class Ability
{
    /// <summary>
    /// 技能数据
    /// </summary>
    public GameDataAbility Cache { get; }
}
```

### 执行系统

#### IExecutableObject 接口

可执行对象接口，用于技能效果等。

```csharp
public interface IExecutableObject : IExecutionContext, IGameObject
{
    /// <summary>
    /// 父级执行对象
    /// </summary>
    IExecutableObject? Parent { get; }
    
    /// <summary>
    /// 共享执行参数
    /// </summary>
    ExecutionParamShared Shared { get; }
    
    /// <summary>
    /// 执行对象
    /// </summary>
    /// <returns>执行结果</returns>
    CmdResult Execute();
    
    /// <summary>
    /// 验证执行条件
    /// </summary>
    /// <returns>验证结果</returns>
    CmdResult Validate();
}
```

#### IExecutionContext 接口

执行上下文接口。

```csharp
public interface IExecutionContext
{
    /// <summary>
    /// 执行等级
    /// </summary>
    uint Level { get; }
    
    /// <summary>
    /// 施法者
    /// </summary>
    Entity Caster { get; }
    
    /// <summary>
    /// 关联的技能（可选）
    /// </summary>
    Ability? Ability { get; }
    
    /// <summary>
    /// 关联的物品（可选）
    /// </summary>
    Item? Item { get; }
    
    /// <summary>
    /// 主要目标
    /// </summary>
    ITarget MainTarget { get; }
}
```

### 组件系统

#### IComponent 接口

组件基础接口。

```csharp
public interface IComponent
{
    /// <summary>
    /// 所属实体
    /// </summary>
    Entity Owner { get; }
    
    /// <summary>
    /// 组件是否启用
    /// </summary>
    bool Enabled { get; set; }
    
    /// <summary>
    /// 附加到实体时调用
    /// </summary>
    void OnAttached();
    
    /// <summary>
    /// 从实体分离时调用
    /// </summary>
    void OnDetached();
    
    /// <summary>
    /// 更新组件
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    void Update(float deltaTime);
}
```

---

## GameUI API

### 控件系统

#### INode 接口

UI节点基础接口。

```csharp
public interface INode
{
    /// <summary>
    /// 数据上下文
    /// </summary>
    object? DataContext { get; set; }
    
    /// <summary>
    /// 子节点列表
    /// </summary>
    IReadOnlyList<INodeChild>? Children { get; }
    
    /// <summary>
    /// 添加子节点
    /// </summary>
    /// <param name="child">要添加的子节点</param>
    /// <returns>是否成功</returns>
    bool AddChild(INodeChild child);
    
    /// <summary>
    /// 移除子节点
    /// </summary>
    /// <param name="child">要移除的子节点</param>
    /// <returns>是否成功</returns>
    bool RemoveChild(INodeChild child);
    
    /// <summary>
    /// 查找子节点
    /// </summary>
    /// <param name="condition">查找条件</param>
    /// <returns>找到的子节点</returns>
    INodeChild? FindChild(Func<INodeChild, bool> condition);
}
```

#### Control 类

UI控件基类。

```csharp
public partial class Control : INode, INodeChild
{
    /// <summary>
    /// 控件名称
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// 是否可见
    /// </summary>
    public bool Visible { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Disabled { get; set; }
    
    /// <summary>
    /// 不透明度 (0-1)
    /// </summary>
    public float Opacity { get; set; }
    
    /// <summary>
    /// 背景画刷
    /// </summary>
    public ColorBrush Background { get; set; }
    
    /// <summary>
    /// 圆角半径
    /// </summary>
    public float CornerRadius { get; set; }
    
    /// <summary>
    /// 控件宽度
    /// </summary>
    public float Width { get; set; }
    
    /// <summary>
    /// 控件高度
    /// </summary>
    public float Height { get; set; }
    
    /// <summary>
    /// 父容器
    /// </summary>
    public INode? Parent { get; set; }
    
    /// <summary>
    /// 水平对齐方式
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; }
    
    /// <summary>
    /// 垂直对齐方式
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; }
    
    /// <summary>
    /// 边距
    /// </summary>
    public Thickness Margin { get; set; }
}
```

#### ⚠️ 重要概念澄清：Layout属性

**Layout属性仅在数编数据（GameData）中存在，用于归类布局相关属性。在实际的UI对象中，布局属性直接属于对象本身。**

```csharp
// ❌ 错误用法：在UI对象中不存在Layout属性
var button = new Button();
button.Layout = new() { Width = 100, Height = 50 }; // 编译错误！

// ✅ 正确用法：直接设置布局属性
var button = new Button()
{
    Width = 100,           // 直接属性
    Height = 50,           // 直接属性
    HorizontalAlignment = HorizontalAlignment.Center,  // 直接属性
    VerticalAlignment = VerticalAlignment.Center,      // 直接属性
    Margin = new Thickness(10, 10, 10, 10)           // 直接属性
};

// ✅ 数编数据中的Layout用法（仅用于数据定义）
var buttonData = new GameDataControlButton(link)
{
    Layout = new()  // 在数编数据中用于归类布局属性
    {
        Width = 100,
        Height = 50,
        HorizontalAlignment = HorizontalAlignment.Center
    }
};
```

#### Button 类

按钮控件，支持用户点击交互。

```csharp
[GameObject<GameDataControlButton>]
public partial class Button : Control
{
    /// <summary>
    /// 使用默认模板初始化按钮实例
    /// </summary>
    public Button();
    
    /// <summary>
    /// 使用指定模板数编Link初始化按钮实例
    /// </summary>
    /// <param name="link">按钮模板数编Link</param>
    public Button(IGameLink<GameDataControlButton> link);
    
    /// <summary>
    /// 鼠标悬停时显示的图像
    /// </summary>
    public UTF8String ImageHover { get; set; }
    
    /// <summary>
    /// 鼠标按下时显示的图像
    /// </summary>
    public UTF8String ImagePressed { get; set; }
}
```

#### Label 类

文本标签控件，用于显示文本内容。

```csharp
[GameObject<GameDataControlLabel>]
public partial class Label : Control
{
    /// <summary>
    /// 使用默认模板初始化标签实例
    /// </summary>
    public Label();
    
    /// <summary>
    /// 使用指定模板数编Link初始化标签实例
    /// </summary>
    /// <param name="link">标签模板数编Link</param>
    public Label(IGameLink<GameDataControlLabel> link);
    
    /// <summary>
    /// 显示的文本内容
    /// </summary>
    public string? Text { get; set; }
    
    /// <summary>
    /// 字体名称
    /// </summary>
    public UTF8String Font { get; set; }
    
    /// <summary>
    /// 字体大小
    /// </summary>
    public int FontSize { get; set; }
    
    /// <summary>
    /// 文本颜色
    /// </summary>
    public SolidColorBrush TextColor { get; set; }
    
    /// <summary>
    /// 行间距比例
    /// </summary>
    public float LineSpacingRatio { get; set; }
    
    /// <summary>
    /// 是否自动换行
    /// </summary>
    public bool TextWrap { get; set; }
    
    /// <summary>
    /// 是否粗体
    /// </summary>
    public bool Bold { get; set; }
    
    /// <summary>
    /// 是否斜体
    /// </summary>
    public bool Italic { get; set; }
    
    /// <summary>
    /// 描边大小
    /// </summary>
    public int StrokeSize { get; set; }
    
    /// <summary>
    /// 描边颜色
    /// </summary>
    public SolidColorBrush StrokeColor { get; set; }
    
    /// <summary>
    /// 阴影偏移
    /// </summary>
    public Vector2 ShadowOffset { get; set; }
    
    /// <summary>
    /// 阴影颜色
    /// </summary>
    public SolidColorBrush ShadowColor { get; set; }
    
    /// <summary>
    /// 文本裁剪方式
    /// </summary>
    public TextTrimming TextTrimming { get; set; }
}
```

#### Panel 类

面板容器控件，用于组织和布局子控件。

```csharp
public partial class Panel : Control
{
    /// <summary>
    /// 使用默认模板初始化面板实例
    /// </summary>
    public Panel();
}
```

### 事件系统

#### 事件参数

```csharp
public struct PointerEventArgs
{
    /// <summary>
    /// 指针位置
    /// </summary>
    public Vector2 Position { get; }
    
    /// <summary>
    /// 按键状态
    /// </summary>
    public PointerButtons Button { get; }
    
    /// <summary>
    /// 修饰键状态
    /// </summary>
    public ModifierKeys Modifiers { get; }
}
```

#### 事件处理

```csharp
// 鼠标点击事件
public event EventHandler<PointerEventArgs>? PointerClicked;

// 鼠标进入事件
public event EventHandler<PointerEventArgs>? PointerEntered;

// 鼠标离开事件
public event EventHandler<PointerEventArgs>? PointerExited;

// 拖拽事件
public event EventHandler<DragEventArgs>? Drag;

// 放置事件
public event EventHandler<DropEventArgs>? Drop;
```

### 输入系统

#### InputManager 类

输入管理器。

```csharp
public class InputManager : IGameClass
{
    /// <summary>
    /// 处理输入
    /// </summary>
    public void ProcessInput();
    
    /// <summary>
    /// 获取按键状态
    /// </summary>
    /// <param name="key">按键</param>
    /// <returns>是否按下</returns>
    public bool IsKeyDown(Keys key);
    
    /// <summary>
    /// 获取鼠标位置
    /// </summary>
    /// <returns>鼠标位置</returns>
    public Vector2 GetMousePosition();
    
    /// <summary>
    /// 射线检测
    /// </summary>
    /// <param name="position">屏幕位置</param>
    /// <param name="mode">检测模式</param>
    /// <returns>命中结果</returns>
    public RaycastHit? Raycast(Vector2 position, RaycastMode mode);
}

### Canvas 2D绘图系统

#### Canvas 类

Canvas类是一个功能强大的2D绘图控件，提供了完整的2D图形绘制API。继承自Control基类，可以嵌入到UI界面中进行自定义绘制。

Canvas类采用partial class设计，功能分布在多个文件中，提供了丰富的绘图功能包括基础图形绘制、图像处理、路径绘制和坐标变换等。

```csharp
[GameObject<GameDataControlCanvas>]
public partial class Canvas : Control
{
    /// <summary>
    /// 使用默认模板初始化Canvas实例
    /// </summary>
    public Canvas();
    
    /// <summary>
    /// 使用指定的游戏数编Link初始化Canvas实例
    /// </summary>
    /// <param name="link">指向GameDataControlCanvas数据的游戏数编Link</param>
    public Canvas(IGameLink<GameDataControlCanvas> link);
}
```

#### 绘制属性

Canvas提供了三个核心绘制属性，用于控制绘制操作的外观：

```csharp
/// <summary>
/// 填充颜色，用于填充图形内部
/// </summary>
/// <value>填充颜色，默认为黑色</value>
public System.Drawing.Color FillColor { get; set; }

/// <summary>
/// 描边颜色，用于绘制图形轮廓和线条
/// </summary>
/// <value>描边颜色，默认为黑色</value>
public System.Drawing.Color StrokeColor { get; set; }

/// <summary>
/// 线条宽度，用于控制轮廓和线条的粗细
/// </summary>
/// <value>线条宽度，默认为1.0像素</value>
public float StrokeSize { get; set; }
```

#### 基础图形绘制

Canvas提供了绘制基本几何图形的方法：

```csharp
/// <summary>
/// 绘制直线
/// </summary>
/// <param name="x1">起点X坐标</param>
/// <param name="y1">起点Y坐标</param>
/// <param name="x2">终点X坐标</param>
/// <param name="y2">终点Y坐标</param>
public void DrawLine(float x1, float y1, float x2, float y2);

/// <summary>
/// 绘制圆形轮廓
/// </summary>
/// <param name="centerX">圆心X坐标</param>
/// <param name="centerY">圆心Y坐标</param>
/// <param name="radius">半径</param>
public void DrawCircle(float centerX, float centerY, float radius);

/// <summary>
/// 绘制矩形轮廓
/// </summary>
/// <param name="x">左上角X坐标</param>
/// <param name="y">左上角Y坐标</param>
/// <param name="width">宽度</param>
/// <param name="height">高度</param>
public void DrawRectangle(float x, float y, float width, float height);

/// <summary>
/// 绘制三角形轮廓
/// </summary>
/// <param name="x1">第一个顶点X坐标</param>
/// <param name="y1">第一个顶点Y坐标</param>
/// <param name="x2">第二个顶点X坐标</param>
/// <param name="y2">第二个顶点Y坐标</param>
/// <param name="x3">第三个顶点X坐标</param>
/// <param name="y3">第三个顶点Y坐标</param>
public void DrawTriangle(float x1, float y1, float x2, float y2, float x3, float y3);
```

#### 填充图形绘制

Canvas支持绘制实心填充的几何图形：

```csharp
/// <summary>
/// 绘制填充圆形
/// </summary>
/// <param name="centerX">圆心X坐标</param>
/// <param name="centerY">圆心Y坐标</param>
/// <param name="radius">半径</param>
public void FillCircle(float centerX, float centerY, float radius);

/// <summary>
/// 绘制填充矩形
/// </summary>
/// <param name="x">左上角X坐标</param>
/// <param name="y">左上角Y坐标</param>
/// <param name="width">宽度</param>
/// <param name="height">高度</param>
public void FillRectangle(float x, float y, float width, float height);

/// <summary>
/// 绘制填充三角形
/// </summary>
/// <param name="x1">第一个顶点X坐标</param>
/// <param name="y1">第一个顶点Y坐标</param>
/// <param name="x2">第二个顶点X坐标</param>
/// <param name="y2">第二个顶点Y坐标</param>
/// <param name="x3">第三个顶点X坐标</param>
/// <param name="y3">第三个顶点Y坐标</param>
public void FillTriangle(float x1, float y1, float x2, float y2, float x3, float y3);
```

#### 图像绘制

Canvas支持绘制图像资源：

```csharp
/// <summary>
/// 在指定位置和尺寸绘制图像
/// </summary>
/// <param name="image">要绘制的图像资源</param>
/// <param name="x">图像左上角X坐标</param>
/// <param name="y">图像左上角Y坐标</param>
/// <param name="width">图像显示宽度</param>
/// <param name="height">图像显示高度</param>
public void DrawImage(Image image, float x, float y, float width, float height);
```

#### 路径绘制

Canvas支持复杂路径的绘制，包括贝塞尔曲线和圆弧：

```csharp
/// <summary>
/// 绘制完整路径的轮廓
/// </summary>
/// <param name="path">要绘制的路径对象</param>
/// <remarks>
/// 支持的路径操作包括：移动、直线、二次贝塞尔曲线、三次贝塞尔曲线、圆弧和路径闭合
/// </remarks>
public void DrawPath(PathF path);
```

#### 坐标变换

Canvas提供了完整的2D坐标变换功能：

```csharp
/// <summary>
/// 旋转画布坐标系
/// </summary>
/// <param name="degrees">旋转角度（以度为单位），正值为顺时针</param>
public void Rotate(float degrees);

/// <summary>
/// 缩放画布坐标系
/// </summary>
/// <param name="scaleX">X轴缩放因子</param>
/// <param name="scaleY">Y轴缩放因子</param>
public void Scale(float scaleX, float scaleY);

/// <summary>
/// 平移画布坐标系
/// </summary>
/// <param name="deltaX">X轴平移距离</param>
/// <param name="deltaY">Y轴平移距离</param>
public void Translate(float deltaX, float deltaY);
```

#### 画布管理

```csharp
/// <summary>
/// 清除画布内容
/// </summary>
/// <remarks>清除画布上的所有绘制内容，但不会重置坐标变换状态</remarks>
public void Clear();
```

#### 使用示例

```csharp
// 创建Canvas实例
var canvas = new Canvas();

// 设置绘制属性
canvas.StrokeColor = Color.Red;
canvas.FillColor = Color.Blue;
canvas.StrokeSize = 2.0f;

// 绘制基础图形
canvas.DrawRectangle(10, 10, 100, 50);     // 红色矩形轮廓
canvas.FillCircle(60, 35, 20);             // 蓝色填充圆形

// 使用坐标变换
canvas.Rotate(45);                          // 旋转45度
canvas.Scale(1.5f, 1.5f);                  // 放大1.5倍
canvas.Translate(50, 30);                   // 平移坐标系

// 绘制图像
var image = new Image("path/to/image.png");
canvas.DrawImage(image, 0, 0, 100, 80);

// 绘制复杂路径
var path = new PathF();
path.MoveTo(10, 10);
path.LineTo(100, 10);
path.QuadTo(new PointF(150, 50), new PointF(100, 100));
path.Close();
canvas.StrokeColor = Color.Green;
canvas.DrawPath(path);

// 清除画布
canvas.Clear();
```

#### 🔥 重要：角度单位说明

**Canvas API 统一使用角度制（度数），而不是弧度制！**

所有Canvas相关的角度参数都期望使用度数（0-360°），包括：
- `Canvas.Rotate(degrees)` - 旋转画布
- `Canvas.RotateDegrees(degrees)` - 明确的度数旋转
- `Canvas.DrawCircle(center, radius, startAngle, endAngle, clockwise)` - 圆弧绘制
- `PathF.AddCircleArc()` - 路径圆弧
- `PathF.AddEllipseArc()` - 路径椭圆弧

#### 已修复的角度单位问题
在v2.1版本中，我们修复了底层NanoVG API与上层Canvas API之间的角度单位不一致问题：
- ✅ `Canvas.Rotate()` - 现在正确处理度数到弧度的转换
- ✅ `Canvas.RotateDegrees()` - 语义明确的度数旋转方法
- ✅ `Canvas.RotateRadians()` - 语义明确的弧度旋转方法
- ✅ `PathAddCircleArc()` - 底层方法现在正确转换角度单位
- ✅ `PathAddEllipseArc()` - 底层方法现在正确转换角度单位

#### 最佳实践

1. **使用语义明确的方法**：
   ```csharp
   // 推荐：语义明确
   canvas.RotateDegrees(45f);    // 明确使用度数
   canvas.RotateRadians(MathF.PI / 4f);  // 明确使用弧度
   
   // 可用但不推荐：可能引起混淆
   canvas.Rotate(45f);  // 虽然正确，但不够明确
   ```

2. **使用预定义常量**：
   ```csharp
   using static GameUI.Graphics.GeometryUtil.CommonAnglesInRadians;
   
   // 对于常用角度，使用预定义常量
   canvas.RotateRadians(Degrees45);    // π/4弧度
   canvas.RotateRadians(Degrees90);    // π/2弧度
   canvas.RotateRadians(Degrees180);   // π弧度
   ```

3. **角度转换工具**：
   ```csharp
   // 使用GeometryUtil进行转换
   var degrees = 45f;
   var radians = GeometryUtil.DegreesToRadians(degrees);
   
   // 或使用转换常量
   var radians2 = degrees * GeometryUtil.DegreesToRadiansConstant;
   ```

#### 系统互操作注意事项

当与其他系统（如Unity、WPF、DirectX等）互操作时，请注意：
- **System.Numerics.Matrix3x2.CreateRotation()** 期望弧度制
- **WPF Transform.Rotation** 使用度数制  
- **Unity Transform.rotation** 使用弧度制（四元数）
- **DirectX数学库** 通常使用弧度制

#### 错误排查

如果发现旋转角度异常（如旋转过度、方向错误），请检查：
1. 是否使用了正确的角度单位（度数 vs 弧度）
2. 是否调用了正确的方法（RotateDegrees vs RotateRadians）
3. 是否在与其他库互操作时进行了正确的单位转换

---

## 网络API

### 消息定义

```csharp
[MessagePackObject]
public class NetworkMessage
{
    /// <summary>
    /// 消息类型
    /// </summary>
    [Key(0)]
    public MessageType Type { get; set; }
    
    /// <summary>
    /// 消息数据
    /// </summary>
    [Key(1)]
    public byte[] Data { get; set; }
    
    /// <summary>
    /// 时间戳
    /// </summary>
    [Key(2)]
    public long Timestamp { get; set; }
}
```

### 网络管理器

```csharp
public class NetworkManager
{
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="message">要发送的消息</param>
    /// <param name="reliable">是否可靠传输</param>
    public void SendMessage(NetworkMessage message, bool reliable = true);
    
    /// <summary>
    /// 接收消息
    /// </summary>
    /// <returns>接收到的消息</returns>
    public NetworkMessage? ReceiveMessage();
    
    /// <summary>
    /// 连接到服务器
    /// </summary>
    /// <param name="address">服务器地址</param>
    /// <param name="port">端口</param>
    /// <returns>是否成功</returns>
    public Task<bool> ConnectAsync(string address, int port);
    
    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect();
}
```

---

## 服务器频道系统 API

> 📖 **详细文档**：[服务器频道系统](systems/ServerChannelSystem.md)

服务器频道系统提供跨服务器消息传递能力，支持全局广播和用户专属消息。

### ServerChannelMessage 类

主要的静态API类，提供频道操作的简单接口。

#### 通用频道方法

```csharp
/// <summary>
/// 订阅指定频道
/// </summary>
/// <param name="channel">频道名称</param>
/// <returns>是否成功</returns>
public static bool Subscribe(string channel);

/// <summary>
/// 取消订阅指定频道
/// </summary>
/// <param name="channel">频道名称</param>
/// <returns>是否成功</returns>
public static bool Unsubscribe(string channel);

/// <summary>
/// 向频道发送JSON序列化的对象消息
/// </summary>
/// <typeparam name="T">消息对象类型</typeparam>
/// <param name="channel">频道名称</param>
/// <param name="data">要发送的数据对象</param>
/// <returns>是否成功</returns>
public static bool Publish<T>(string channel, T data);

/// <summary>
/// 向频道发送字节数组消息
/// </summary>
/// <param name="channel">频道名称</param>
/// <param name="message">消息内容</param>
/// <returns>是否成功</returns>
public static bool Publish(string channel, byte[] message);

/// <summary>
/// 尝试将JSON消息解析为指定类型
/// </summary>
/// <typeparam name="T">目标类型</typeparam>
/// <param name="message">消息字节数组</param>
/// <param name="result">解析结果</param>
/// <returns>是否解析成功</returns>
public static bool TryParseJson<T>(ReadOnlySpan<byte> message, out T? result);
```

#### 用户专属消息方法

```csharp
/// <summary>
/// 订阅指定用户的特定类型消息
/// </summary>
/// <param name="userId">用户ID</param>
/// <param name="messageType">消息类型（如 "chat"、"friend"、"team" 等）</param>
/// <returns>是否成功</returns>
public static bool SubscribeUser(long userId, string messageType);

/// <summary>
/// 取消订阅用户消息
/// </summary>
/// <param name="userId">用户ID</param>
/// <param name="messageType">消息类型</param>
/// <returns>是否成功</returns>
public static bool UnsubscribeUser(long userId, string messageType);

/// <summary>
/// 向指定用户发送JSON序列化的消息
/// </summary>
/// <typeparam name="T">消息对象类型</typeparam>
/// <param name="userId">目标用户ID</param>
/// <param name="messageType">消息类型</param>
/// <param name="data">要发送的数据对象</param>
/// <returns>是否成功</returns>
public static bool PublishToUser<T>(long userId, string messageType, T data);

/// <summary>
/// 向指定用户发送字节数组消息
/// </summary>
/// <param name="userId">目标用户ID</param>
/// <param name="messageType">消息类型</param>
/// <param name="message">消息内容</param>
/// <returns>是否成功</returns>
public static bool PublishToUser(long userId, string messageType, byte[] message);

/// <summary>
/// 从频道信息中提取用户ID和消息类型
/// </summary>
/// <param name="channel">频道标识</param>
/// <param name="userId">提取的用户ID</param>
/// <param name="messageType">提取的消息类型</param>
/// <returns>是否成功提取</returns>
public static bool TryGetUserIdFromChannel(string channel, out long userId, out string messageType);
```

### EventChannelMessage 事件

```csharp
/// <summary>
/// 频道消息事件
/// </summary>
public class EventChannelMessage : ITriggerEvent<EventChannelMessage>
{
    /// <summary>
    /// 消息来源的频道标识
    /// </summary>
    public string Channel { get; }
    
    /// <summary>
    /// 消息内容的字节数组
    /// </summary>
    public byte[] Message { get; }
}
```

### 使用示例

#### 订阅和发布通用频道

```csharp
// 订阅世界事件频道
ServerChannelMessage.Subscribe("world-events");

// 发送世界Boss击杀事件
var eventData = new
{
    EventType = "BossKilled",
    BossName = "Dragon King",
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
};
ServerChannelMessage.Publish("world-events", eventData);
```

#### 用户专属消息

```csharp
// 用户登录时订阅私聊频道
ServerChannelMessage.SubscribeUser(userId, "chat");

// 发送跨服私聊消息
var chatMessage = new
{
    FromUserId = fromUserId,
    FromUserName = "Player1",
    Message = "Hello!",
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
};
ServerChannelMessage.PublishToUser(toUserId, "chat", chatMessage);
```

#### 接收消息

```csharp
// 注册消息处理器
var trigger = new Trigger<EventChannelMessage>(OnChannelMessageReceived);
trigger.Register(Game.Instance);

private static async Task<bool> OnChannelMessageReceived(object sender, EventChannelMessage args)
{
    // 判断是否为用户消息
    if (ServerChannelMessage.TryGetUserIdFromChannel(args.Channel, out var userId, out var messageType))
    {
        // 处理用户消息
        if (messageType == "chat")
        {
            if (ServerChannelMessage.TryParseJson<ChatMessage>(args.Message, out var chatMsg))
            {
                await HandleChatMessage(userId, chatMsg);
            }
        }
    }
    return true;
}
```

---

## 🔧 工具类

### CmdResult 结构

命令执行结果。

```csharp
public struct CmdResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; }
    
    /// <summary>
    /// 错误代码
    /// </summary>
    public CmdError Error { get; }
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string? Message { get; }
    
    /// <summary>
    /// 成功结果
    /// </summary>
    public static CmdResult Ok => new(true, CmdError.None);
    
    /// <summary>
    /// 隐式转换为布尔值
    /// </summary>
    public static implicit operator bool(CmdResult result) => result.Success;
}
```

### Vector3 结构

三维向量。

```csharp
public struct Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    
    public static Vector3 Zero => new(0, 0, 0);
    public static Vector3 One => new(1, 1, 1);
    
    public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);
    public Vector3 Normalized() => this / Length();
}
```

### 扩展方法

```csharp
public static class EntityExtensions
{
    /// <summary>
    /// 获取实体的组件
    /// </summary>
    public static T? GetComponent<T>(this Entity entity) where T : IComponent;
    
    /// <summary>
    /// 添加组件到实体
    /// </summary>
    public static T AddComponent<T>(this Entity entity) where T : IComponent, new();
    
    /// <summary>
    /// 移除实体的组件
    /// </summary>
    public static bool RemoveComponent<T>(this Entity entity) where T : IComponent;
}
```

---

## 📝 使用示例

### 创建实体

```csharp
// 创建单位
var unit = new Unit(scene, position);
unit.Name = "Player";
unit.Health = 100;

// 添加组件
var movement = unit.AddComponent<MovementComponent>();
movement.Speed = 5.0f;
```

### 使用技能

```csharp
// 创建技能
var fireball = new Ability(fireballData);
fireball.Level = 1;

// 施放技能
var result = fireball.Cast(caster, target);
if (result.Success)
{
                Game.Logger.LogInformation("技能施放成功: {SkillName}", skill.Name);
}
else
{
                Game.Logger.LogWarning("技能施放失败: {Reason}", result.Message);
}
```

### UI操作

```csharp
// 创建按钮
var button = new Button();
button.Name = "StartButton";
button.ImageHover = "button_hover.png"u8;
button.ImagePressed = "button_pressed.png"u8;
button.PointerClicked += (sender, args) =>
{
    Game.Start();
};

// 创建文本标签
var label = new Label();
label.Text = "开始游戏";
label.Font = "Arial"u8;
label.FontSize = 16;
label.TextColor = Color.White;
label.Bold = true;

// 添加到UI根节点
uiRoot.AddChild(button);
uiRoot.AddChild(label);

// Canvas绘图示例
var canvas = new Canvas();
canvas.StrokeColor = Color.Red;
canvas.FillColor = Color.Blue;
canvas.StrokeSize = 2.0f;

// 绘制基础图形
canvas.DrawRectangle(10, 10, 100, 50);     // 红色矩形轮廓
canvas.FillCircle(60, 35, 20);             // 蓝色填充圆形

// 使用坐标变换
canvas.Rotate(45);                          // 旋转45度
canvas.Scale(1.5f, 1.5f);                  // 放大1.5倍
canvas.Translate(50, 30);                   // 平移坐标系

// 绘制复杂路径
var path = new PathF();
path.MoveTo(10, 10);
path.LineTo(100, 10);
path.QuadTo(new PointF(150, 50), new PointF(100, 100));
path.Close();
canvas.StrokeColor = Color.Green;
canvas.DrawPath(path);

uiRoot.AddChild(canvas);
```

---

## 🎯 基础形状系统 API

> 📖 **详细文档**：[基础形状系统总结](../../SHAPE_SYSTEM_SUMMARY.md)

基础形状系统是为**AI助手友好**设计的3D原型开发工具，让AI能够快速搭建游戏场景而无需复杂的美术资源。

### 核心组件

#### AIShapeFactory 类

AI友好的形状创建工厂，提供语义化的API来快速创建基本形状。

```csharp
public static class AIShapeFactory
{
    /// <summary>
    /// 默认的形状颜色模式
    /// </summary>
    public static ShapeColorMode DefaultColorMode { get; set; } = ShapeColorMode.SmartDefaults;

    /// <summary>
    /// 默认的颜色主题
    /// </summary>
    public static ShapeColorTheme DefaultColorTheme { get; set; } = ShapeColorTheme.Standard;

    /// <summary>
    /// 是否启用自动染色设置（推荐用于AI快速原型）
    /// </summary>
    public static bool EnableAutoTint { get; set; } = true;
}
```

### 基础形状创建

#### 创建基本形状

```csharp
// 创建基本形状Actor
var actor = AIShapeFactory.CreateShape(
    PrimitiveShape.Sphere,           // 形状类型
    new ScenePoint(x, y, 0),        // 位置
    scope: null                      // 作用域（可选）
);

// 创建带缩放的形状
var scaledActor = AIShapeFactory.CreateShape(
    PrimitiveShape.Cube,
    new ScenePoint(x, y, 0),
    new Vector3(2.0f, 1.5f, 1.0f), // 缩放
    scope: null
);

// 创建指定颜色模式的形状
var coloredActor = AIShapeFactory.CreateShape(
    PrimitiveShape.Capsule,
    new ScenePoint(x, y, 0),
    ShapeColorMode.SmartDefaults,    // 颜色模式
    ShapeColorTheme.Gaming,          // 颜色主题
    scope: null
);
```

#### 语义化形状创建

```csharp
// 创建玩家角色（蓝色胶囊）
var player = AIShapeFactory.CreatePlayer(
    new ScenePoint(x, y, 0),
    scope: null,
    scale: new Vector3(1.0f, 1.0f, 1.0f)
);

// 创建敌人（红色球体）
var enemy = AIShapeFactory.CreateEnemy(
    new ScenePoint(x, y, 0),
    scope: null,
    scale: new Vector3(0.8f, 0.8f, 0.8f)
);

// 创建平台（棕色平面）
var platform = AIShapeFactory.CreatePlatform(
    new ScenePoint(x, y, 0),
    scale: new Vector3(width / 100f, height / 100f, 1f)
);

// 创建收集品（黄色圆锥）
var collectible = AIShapeFactory.CreateCollectible(
    new ScenePoint(x, y, 0),
    scope: null,
    scale: new Vector3(0.4f, 0.4f, 0.4f)
);

// 创建障碍物（灰色立方体）
var obstacle = AIShapeFactory.CreateObstacle(
    new ScenePoint(x, y, 0),
    scale: new Vector3(1.0f, 1.0f, 1.0f)
);
```

### 批量形状创建

#### 批量创建相同形状

```csharp
// 批量创建相同形状的Actor
var positions = new List<ScenePoint>
{
    new ScenePoint(100, 100, 0),
    new ScenePoint(200, 100, 0),
    new ScenePoint(300, 100, 0)
};

var actors = AIShapeFactory.CreateShapes(
    PrimitiveShape.Sphere,
    positions,
    scope: null
);
```

#### 网格布局创建

```csharp
// 在网格位置创建形状阵列
var actors = AIShapeFactory.CreateShapeGrid(
    PrimitiveShape.Cube,
    centerPosition: new ScenePoint(500, 500, 0),
    gridSize: (5, 3),           // 5列3行
    spacing: 150f,              // 网格间距
    scope: null
);
```

### 形状组合系统

#### AIShapeComposer 类

智能形状组合工具，用于创建复杂的复合对象。

```csharp
// 创建机器人角色
var robot = AIShapeComposer.CreateRobotCharacter(
    new ScenePoint(x, y, 0),
    scope: null,
    scale: 1.5f
);

// 创建简单房屋
var house = AIShapeComposer.CreateSimpleHouse(
    new ScenePoint(x, y, 0),
    scope: null,
    scale: 2.0f
);

// 创建城堡塔楼
var tower = AIShapeComposer.CreateCastleTower(
    new ScenePoint(x, y, 0),
    scope: null,
    scale: 1.8f
);

// 创建太空船
var spaceship = AIShapeComposer.CreateSimpleSpaceship(
    new ScenePoint(x, y, 0),
    scope: null,
    scale: 1.2f
);
```

### 颜色主题系统

#### 支持的颜色主题

```csharp
// 标准主题 - 基于通用认知的标准颜色
AIShapeFactory.DefaultColorTheme = ShapeColorTheme.Standard;

// 游戏主题 - 基于游戏设计惯例的颜色
AIShapeFactory.DefaultColorTheme = ShapeColorTheme.Gaming;

// 教育主题 - 明亮且易于区分的教育色彩
AIShapeFactory.DefaultColorTheme = ShapeColorTheme.Educational;

// 自然主题 - 基于自然元素的颜色
AIShapeFactory.DefaultColorTheme = ShapeColorTheme.Natural;
```

#### 颜色模式

```csharp
// 智能默认颜色（推荐AI使用）
AIShapeFactory.DefaultColorMode = ShapeColorMode.SmartDefaults;

// 纯白色（适合自定义材质）
AIShapeFactory.DefaultColorMode = ShapeColorMode.PureWhite;

// 随机颜色（调试模式）
AIShapeFactory.DefaultColorMode = ShapeColorMode.RandomColors;
```

### 完整使用示例

#### 创建游戏场景

```csharp
public class GameScene
{
    public void CreateScene()
    {
        // 创建地面平台
        var ground = AIShapeFactory.CreatePlatform(
            new ScenePoint(0, 0, 0),
            new Vector3(10.0f, 1.0f, 10.0f)
        );

        // 创建玩家
        var player = AIShapeFactory.CreatePlayer(
            new ScenePoint(0, 2, 0),
            scope: null,
            scale: new Vector3(1.0f, 1.0f, 1.0f)
        );

        // 创建敌人阵列
        var enemyPositions = new List<ScenePoint>
        {
            new ScenePoint(5, 2, 5),
            new ScenePoint(-5, 2, 5),
            new ScenePoint(5, 2, -5),
            new ScenePoint(-5, 2, -5)
        };

        var enemies = AIShapeFactory.CreateShapes(
            PrimitiveShape.Sphere,
            enemyPositions,
            scope: null
        );

        // 创建收集品网格
        var collectibles = AIShapeFactory.CreateShapeGrid(
            PrimitiveShape.Cone,
            new ScenePoint(0, 1, 0),
            gridSize: (3, 3),
            spacing: 2.0f,
            scope: null
        );

        // 创建装饰建筑
        var tower = AIShapeComposer.CreateCastleTower(
            new ScenePoint(10, 0, 10),
            scope: null,
            scale: 1.5f
        );
    }
}
```

### 性能优化建议

#### 批量操作

```csharp
// 推荐：批量创建
var actors = AIShapeFactory.CreateShapes(shape, positions, scope);

// 不推荐：循环创建
foreach (var position in positions)
{
    var actor = AIShapeFactory.CreateShape(shape, position, scope);
}
```

#### 作用域管理

```csharp
// 使用作用域管理生命周期
var gameScope = new ActorScope();
var actor = AIShapeFactory.CreateShape(
    PrimitiveShape.Cube,
    position,
    scope: gameScope
);

// 作用域销毁时自动清理所有相关Actor
gameScope.Dispose();
```

### 常见问题解决

#### ScenePoint参数类型问题

```csharp
// ❌ 错误：缺少Scene参数
new ScenePoint(x, y, 0)

// ✅ 正确：提供Scene参数
new ScenePoint(x, y, 0, Game.CurrentScene)
// 或者使用默认场景
new ScenePoint(x, y, 0, null) // 如果API支持
```

#### 形状尺寸问题

```csharp
// 所有形状基于100单位的基准尺寸
// 缩放时使用相对比例
var smallCube = AIShapeFactory.CreateShape(
    PrimitiveShape.Cube,
    position,
    new Vector3(0.5f, 0.5f, 0.5f) // 50x50x50
);

var largeCube = AIShapeFactory.CreateShape(
    PrimitiveShape.Cube,
    position,
    new Vector3(2.0f, 2.0f, 2.0f) // 200x200x200
);
```

### 网络通信

```csharp
// 发送消息
var message = new NetworkMessage
{
    Type = MessageType.PlayerMove,
    Data = MessagePackSerializer.Serialize(moveData)
};
networkManager.SendMessage(message);

// 接收消息
var received = networkManager.ReceiveMessage();
if (received != null)
{
    switch (received.Type)
    {
        case MessageType.PlayerMove:
            var moveData = MessagePackSerializer.Deserialize<MoveData>(received.Data);
            HandlePlayerMove(moveData);
            break;
    }
}
```