# UI系统

星火编辑器2.0版本中，UI控件能以三种不同的形式来创建。本文档详细介绍这三种创建方式及其使用场景。

## 目录

- [方式一：通过数编定义UI模板](#方式一通过数编定义ui模板)
- [方式二：运行时动态创建UI](#方式二运行时动态创建ui)
- [方式三：流式扩展语法](#方式三流式扩展语法)
- [Canvas与路径绘制](#canvas与路径绘制)
- [客户端组件信息](#客户端组件信息)

---

## 方式一：通过数编定义UI模板

类似于单位、技能和Buff，UI控件也拥有对应的数编表类型。用户可以在数编中定义UI模板，并在运行时创建UI控件实例。

### 定义UI模板

```csharp
_ = new GameDataControlButton(Control.MyButton)
{
    Layout = new()  // 布局属性在 Layout 对象中
    {
        Width = 200,
        Height = 50,
        HorizontalAlignment = HorizontalAlignment.Center,
    },
    Background = new SolidColorBrush(Color.Blue),
    Children = [
        new GameDataControlLabel(new("MyButtonLabel"))
        {
            Text = "点击我",
            TextColor = Color.White,
        }.Link
    ]
};
```

### 从模板创建实例

用户可以使用 `GameDataControlButton.CreateButton()` 方法来创建UI控件实例，并进行进一步的修改：

```csharp
var button = GameDataControlButton.CreateButton();
button.Width = 200;
button.Height = 50;
button.HorizontalAlignment = HorizontalAlignment.Center;
button.Background = new SolidColorBrush(Color.Blue);
button.AddToVisualTree(); // 将按钮添加到可视化树中
```

### 适用场景

- **静态UI** - 界面结构固定，不需要频繁改变
- **复杂布局** - 嵌套层次多，通过数编定义更清晰
- **可重用组件** - 需要在多处使用相同的UI模板

---

## 方式二：运行时动态创建UI

有时候你可能只想创建一个按钮，但不希望事先为它定义数编，那么你可以直接使用 `Button` 类来创建UI控件实例。

### 基本创建

```csharp
#if CLIENT
// 在客户端代码中动态创建
var button = new Button();
button.Width = 200;        // 直接属性
button.Height = 50;        // 直接属性
button.HorizontalAlignment = HorizontalAlignment.Center;  // 直接属性
button.Background = new SolidColorBrush(Color.Blue);
button.AddToVisualTree(); // 将按钮添加到可视化树中
#endif
```

### 添加子控件

```csharp
#if CLIENT
var button = new Button
{
    Width = 200,
    Height = 50,
    HorizontalAlignment = HorizontalAlignment.Center,
    Background = new SolidColorBrush(Color.Blue)
};

var label = new Label
{
    Text = "点击我",
    TextColor = Color.White,
    HorizontalAlignment = HorizontalAlignment.Center,
    VerticalAlignment = VerticalAlignment.Center
};

button.AddChild(label);

// 直接订阅事件
button.OnPointerClicked += (sender, e) =>
{
    Game.Logger.LogInformation("按钮被点击了！");
};

// 添加到视觉树
button.AddToVisualTree();
#endif
```

### 模板方式 vs 运行时方式的关键差异

| 特性 | 模板方式（数编） | 运行时方式 |
|------|-----------------|-----------|
| 布局属性 | `Layout.Width` | `Width` |
| 事件订阅 | `Event.OnPointerClicked` | `OnPointerClicked` |
| 数据结构 | 预定义结构化数据 | 动态创建 |
| 适用场景 | 静态UI、复杂布局、可重用组件 | 动态UI、游戏内UI、响应式界面 |

### 适用场景

- **动态UI** - 需要根据游戏状态动态创建或修改
- **游戏内UI** - 临时显示的提示、通知等
- **响应式界面** - 需要对用户操作做出即时反应

---

## 方式三：流式扩展语法

流式扩展语法是星火编辑器2.0版本中引入的一种新的UI创建方式。它使用链式调用的方式来创建UI控件，使代码更加简洁直观。

### 基本用法

```csharp
var button = Button()
    .Size(200, 50)
    .Center()
    .Background(Color.Blue);
button.AddToVisualTree(); // 将按钮添加到可视化树中
```

### 链式调用

流式扩展语法支持链式调用多个方法，每个方法都返回控件本身，支持连续调用多个方法：

```csharp
var button = Button()
    .Size(200, 50)
    .Center()
    .Background(Color.Blue)
    .Padding(10)
    .Margin(5);
```

### 优势

- **简洁** - 代码更短，更易读
- **直观** - 方法名清晰表达意图
- **灵活** - 可以随意组合各种设置方法

### 深入学习

更多流式UI语法的详细信息，请参阅：
- [流式UI布局指南](../guides/FluentUILayoutGuide.md) - 完整的流式UI语法文档

---

## Canvas与路径绘制

星火编辑器2.0版本还增加了全新的Canvas渲染控件，使星火编辑器正式获得了**2D游戏开发**的能力。

### Canvas控件

Canvas控件提供了：
- **2D绘图API** - 绘制线条、矩形、圆形等基本图形
- **路径绘制** - 复杂的矢量图形绘制
- **性能优化** - 针对2D游戏的渲染优化

### 适用场景

- **2D游戏** - 完整的2D游戏开发
- **自定义图形** - 需要绘制特殊图形的场景
- **数据可视化** - 图表、统计信息等

### 深入学习

更多Canvas绘图的详细信息，请参阅：
- [Canvas绘图系统文档](../systems/CanvasDrawingSystem.md) - 完整的Canvas API文档

---

## 客户端组件信息

在1.0版本中，物品、物品栏、Buff、技能的客户端实现以及UI界面的设计相当糟糕。用户在客户端触发器中无法获取许多重要的信息。

### 2.0版本的改进

在2.0版本中，这些对象的客户端版本遵循组件的设计理念，组件拥有自己的客户端版本，拥有许多和服务器版本一致的API。

用户可以以非常直观方便的方法获取它们的信息，比如：
- 物品的属性加成
- Buff的属性加成
- 技能的冷却时间
- 技能的公式值

### 遍历物品属性加成示例

以下遍历物品动态加成属性的代码在服务端和客户端都有效：

```csharp
ItemMod itemMod = ...; // ItemMod是有加成效果的物品类型

// 遍历物品的动态属性修改
foreach (var dynamicMod in itemMod.EnumerateDynamicModifications(slotType))
{
    Game.Logger.LogInformation($"动态属性加成: {dynamicMod.SlotType}, {dynamicMod.PropertyLink}, {dynamicMod.SubType}, {dynamicMod.Value}");
}
```

### 物品动态属性

"物品动态加成属性"即1.0中的"物品额外属性"概念。现在它被重新设计，命名为"物品动态属性"。

### 参考实现

我们还将公开我们内部实现物品栏和技能摇杆UI控件的C#源码。这些界面是和用户一样的刚刚开始学习星火编辑器2.0的初学者编写的，用户可以参考它们来实现自己的UI界面。

---

## UI系统最佳实践

### 选择合适的创建方式

根据不同的使用场景选择合适的UI创建方式：

1. **数编模板方式** - 适合静态、复杂、可重用的UI
2. **运行时创建方式** - 适合动态、临时、响应式的UI
3. **流式语法方式** - 适合简单、快速创建的UI

### 客户端代码标记

UI代码通常运行在客户端，记得使用 `#if CLIENT` 标记：

```csharp
#if CLIENT
var button = new Button();
// ... UI 创建代码
#endif
```

### 事件订阅与清理

记得在适当的时候取消事件订阅，避免内存泄漏：

```csharp
#if CLIENT
button.OnPointerClicked += OnButtonClick;

// 清理时
button.OnPointerClicked -= OnButtonClick;
#endif
```

---

## 下一步

了解了UI系统后，建议继续阅读：
- [当前版本限制](./04-LIMITATIONS.md) - 了解当前版本的限制和未来展望
- [UI设计标准](../guides/UIDesignStandards.md) - UI设计最佳实践
- [流式UI布局指南](../guides/FluentUILayoutGuide.md) - 深入学习流式UI语法

