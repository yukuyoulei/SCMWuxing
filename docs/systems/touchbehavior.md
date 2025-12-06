# TouchBehavior 触摸行为系统

## 概述

TouchBehavior 是 WasiCore 框架中的 UI 交互行为系统，为控件提供丰富的触摸反馈效果，包括按压动画、长按检测等功能。它基于控件行为 (ControlBehavior) 框架构建，提供了统一且可配置的触摸交互体验。

## 🎯 设计目标

### 核心特性
- **按压动画**：支持缩放和透明度的动画反馈
- **长按检测**：可配置的长按时间和事件触发
- **事件管理**：完善的触摸事件生命周期管理
- **冲突处理**：长按与点击事件的智能冲突解决
- **高度可配置**：灵活的参数定制满足不同需求

### 设计原则
1. **非侵入性**：通过扩展方法轻松添加到任何控件
2. **性能优化**：使用高效的动画系统和事件管理
3. **灵活配置**：支持多种动画和时间参数的组合
4. **一致体验**：提供统一的触摸反馈标准

## 🏗️ 架构设计

### 核心组件架构

```
TouchBehavior 系统架构
┌─────────────────────────────────────┐
│            TouchBehavior            │
│  ┌─────────────────────────────────┐ │
│  │       Press Animation         │ │
│  │  • ScaleAndOpacity           │ │
│  │  • ArithmeticAnimation       │ │
│  │  • EasingMode Support        │ │
│  └─────────────────────────────────┘ │
│  ┌─────────────────────────────────┐ │
│  │      Long Press Detection      │ │
│  │  • 可配置时间阈值             │ │
│  │  • 事件生命周期管理           │ │
│  │  • 与点击事件冲突解决         │ │
│  └─────────────────────────────────┘ │
│  ┌─────────────────────────────────┐ │
│  │        Event Management        │ │
│  │  • LongPressStarted           │ │
│  │  • LongPressTriggered         │ │
│  │  • LongPressEnded             │ │
│  └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

### 继承关系

```csharp
ControlBehavior
    └── TouchBehavior
            ├── Press Animation (ArithmeticAnimation<ScaleAndOpacity>)
            ├── Long Press Timer (Game.Delay)
            └── Event Handlers (各种触摸事件)
```

## 🚀 使用方法

### 流式语法（推荐）⭐

**使用流式扩展方法，支持链式调用，代码更简洁流畅**：

```csharp
#if CLIENT
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;

// 方式1：基础流式语法
var button = Button("点击我")
    .Size(200, 80)
    .Background(Colors.Primary)
    .TouchBehavior()              // ✅ 默认配置
    .Click(() => DoSomething())
    .AddToRoot();

// 方式2：自定义参数
var customButton = Button("自定义")
    .Size(200, 80)
    .TouchBehavior(
        scaleFactor: 0.9f,        // 缩放到90%
        enablePressAnimation: true,
        enableLongPress: true
    )
    .AddToRoot();

// 方式3：精确控制时长
var preciseButton = Button("精确控制")
    .Size(200, 80)
    .TouchBehavior(
        scaleFactor: 0.97f,
        animationDurationMs: 80,   // 动画时长
        longPressDurationMs: 300   // 长按时长
    )
    .AddToRoot();

// 方式4：配置长按事件
var menuButton = Button("长按显示菜单")
    .Size(200, 80)
    .TouchBehavior(
        configure: tb => 
        {
            tb.LongPressTriggered += (s, e) => ShowContextMenu();
            tb.LongPressStarted += (s, e) => ShowHint();
        },
        scaleFactor: 0.95f
    )
    .Click(() => QuickAction())
    .AddToRoot();

// 方式5：预设快捷方法
var toolButton = Button("工具")
    .Size(60, 60)
    .QuickTouch()        // ✅ 快速触摸反馈
    .AddToRoot();

var primaryButton = Button("确认")
    .Size(300, 60)
    .EmphasizedTouch()   // ✅ 强调触摸反馈
    .AddToRoot();

var listItem = Panel()
    .Size(400, 60)
    .GentleTouch()       // ✅ 轻柔触摸反馈
    .AddToRoot();
#endif
```

### 传统方式

**如果需要直接访问 TouchBehavior 实例**：

```csharp
#if CLIENT
using GameUI.Control.Behavior;

// 方式1：使用默认配置
var button = new Button();
var touchBehavior = button.AddTouchBehavior();

// 方式2：使用自定义配置
var touchBehavior = button.AddTouchBehavior(
    scaleFactor: 0.9f,           // 按压时缩放到90%
    enablePressAnimation: true,   // 启用按压动画
    enableLongPress: true        // 启用长按检测
);

// 方式3：完全自定义配置
var touchBehavior = button.AddTouchBehaviorWithDuration(
    scaleFactor: 0.95f,          // 缩放因子
    animationDuration: 150,      // 动画持续时间(ms)
    longPressDuration: 500       // 长按触发时间(ms)
);
#endif
```

### 事件订阅

```csharp
#if CLIENT
// 订阅长按事件
touchBehavior.LongPressStarted += (sender, e) =>
{
    Game.Logger.LogInformation("长按开始");
};

touchBehavior.LongPressTriggered += (sender, e) =>
{
    Game.Logger.LogInformation("长按触发");
    // 执行长按逻辑
};

touchBehavior.LongPressEnded += (sender, e) =>
{
    Game.Logger.LogInformation("长按结束");
};

// 普通点击事件（注意：长按触发后此事件会被屏蔽）
button.OnPointerClicked += (sender, e) =>
{
    Game.Logger.LogInformation("按钮点击");
};
#endif
```

### 高级配置示例

```csharp
#if CLIENT
// 示例1：快速响应按钮
var quickButton = new Button();
quickButton.AddTouchBehaviorWithDuration(
    scaleFactor: 0.98f,     // 轻微缩放
    animationDuration: 50,  // 快速动画
    longPressDuration: 300  // 较短长按时间
);

// 示例2：强调按钮
var emphasisButton = new Button();
emphasisButton.AddTouchBehaviorWithDuration(
    scaleFactor: 0.85f,     // 明显缩放
    animationDuration: 200, // 适中动画
    longPressDuration: 800  // 较长长按时间
);

// 示例3：仅动画，无长按
var animOnlyButton = new Button();
animOnlyButton.AddTouchBehavior(
    scaleFactor: 0.9f,
    enablePressAnimation: true,
    enableLongPress: false  // 禁用长按
);

// 示例4：仅长按，无动画
var longPressOnlyButton = new Button();
longPressOnlyButton.AddTouchBehavior(
    enablePressAnimation: false, // 禁用动画
    enableLongPress: true
);
#endif
```

## 🔧 配置参数详解

### 流式扩展方法 API

#### `.TouchBehavior()` - 基础方法

```csharp
public static T TouchBehavior<T>(
    this T control,
    float scaleFactor = 0.95f,
    bool enablePressAnimation = true,
    bool enableLongPress = true) where T : Control
```

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `scaleFactor` | `float` | `0.95f` | 按压时的缩放因子 (0.0-1.0) |
| `enablePressAnimation` | `bool` | `true` | 是否启用按压动画 |
| `enableLongPress` | `bool` | `true` | 是否启用长按检测 |

#### `.TouchBehavior()` - 时长控制重载

```csharp
public static T TouchBehavior<T>(
    this T control,
    float scaleFactor,
    int animationDurationMs,
    int longPressDurationMs,
    bool enablePressAnimation = true,
    bool enableLongPress = true) where T : Control
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `scaleFactor` | `float` | 按压时的缩放因子 |
| `animationDurationMs` | `int` | 按压动画时长（毫秒） |
| `longPressDurationMs` | `int` | 长按触发时长（毫秒） |
| `enablePressAnimation` | `bool` | 是否启用按压动画（默认true） |
| `enableLongPress` | `bool` | 是否启用长按检测（默认true） |

#### `.TouchBehavior()` - 事件配置重载

```csharp
public static T TouchBehavior<T>(
    this T control,
    Action<TouchBehavior> configure,
    float scaleFactor = 0.95f,
    bool enablePressAnimation = true,
    bool enableLongPress = true) where T : Control
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `configure` | `Action<TouchBehavior>` | 配置回调，用于订阅事件 |
| 其他参数 | - | 与基础方法相同 |

#### 预设快捷方法

| 方法 | 配置 | 适用场景 |
|------|------|----------|
| `.QuickTouch()` | scale=0.97, anim=80ms, long=400ms | 工具栏按钮、快速响应控件 |
| `.EmphasizedTouch()` | scale=0.90, anim=150ms, long=500ms | 主按钮、需要明显反馈的控件 |
| `.GentleTouch()` | scale=0.98, anim=120ms, long=600ms | 列表项、需要轻微反馈的控件 |

### 传统方法参数

#### AddTouchBehavior

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `scaleFactor` | `float` | `0.95f` | 按压时的缩放因子 (0.0-1.0) |
| `enablePressAnimation` | `bool` | `true` | 是否启用按压动画 |
| `enableLongPress` | `bool` | `true` | 是否启用长按检测 |

#### AddTouchBehaviorWithDuration

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `animationDuration` | `int` | `150` | 动画持续时间 (毫秒) |
| `longPressDuration` | `int` | `500` | 长按触发时间 (毫秒) |

### 动画配置说明

```csharp
// ScaleAndOpacity 结构说明
public record struct ScaleAndOpacity(float Scale, float Opacity) 
    : IArithmeticAnimatable<ScaleAndOpacity>
{
    // Scale: 缩放值，1.0 = 原始大小，0.95 = 缩放到95%
    // Opacity: 透明度，1.0 = 完全不透明，0.8 = 80%透明度
}
```

## 🆚 流式扩展 vs 传统方式对比

### 语法对比

```csharp
// ❌ 传统方式：需要分两步，无法链式调用
var button = new TextButton("点击我");
button.Width = 200;
button.Height = 80;
button.Background = new SolidColorBrush(Colors.Primary);
button.AddTouchBehavior(scaleFactor: 0.9f);  // 返回TouchBehavior，链式中断
button.OnPointerClicked += (s, e) => DoSomething();

// ✅ 流式扩展：一气呵成，链式流畅
var button = UI.Button("点击我")
    .Size(200, 80)
    .Background(Colors.Primary)
    .TouchBehavior(scaleFactor: 0.9f)  // 返回控件本身，继续链式
    .Click(() => DoSomething())
    .AddToRoot();
```

### 优势总结

| 对比项 | 传统方式 | 流式扩展 ⭐ |
|--------|----------|------------|
| **代码行数** | 需要多行分别设置 | 一条链式语句搞定 |
| **可读性** | 分散，不直观 | 集中，语义清晰 |
| **链式调用** | ❌ 中断链式 | ✅ 支持链式 |
| **AI友好** | 较难生成 | 易于AI生成 |
| **预设配置** | ❌ 无 | ✅ 提供QuickTouch等快捷方法 |
| **向后兼容** | ✅ 完全兼容 | ✅ 完全兼容 |

### 何时使用哪种方式？

**推荐使用流式扩展**：
- ✅ 大多数UI构建场景
- ✅ 需要链式调用的代码
- ✅ 追求简洁易读的代码
- ✅ AI辅助开发

**使用传统方式**：
- 需要在创建后动态访问 `TouchBehavior` 实例
- 需要后续修改 `TouchBehavior` 的属性
- 在现有代码中局部修改

## 📱 实际应用场景

### 场景1：游戏按钮（流式语法）

```csharp
#if CLIENT
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;

// 主菜单按钮 - 需要强烈的视觉反馈
var playButton = Button("开始游戏")
    .Size(300, 80)
    .FontSize(32)
    .Background(Colors.Green)
    .TouchBehavior(
        scaleFactor: 0.9f,
        animationDurationMs: 100,
        longPressDurationMs: 1000
    )
    .Click(() => StartGame())
    .AddToRoot();
    
// 或使用预设
var quickStartButton = Button("快速开始")
    .Size(300, 80)
    .EmphasizedTouch()  // ✅ 预设：明显反馈
    .Click(() => QuickStart())
    .AddToRoot();
#endif
```

### 场景1：游戏按钮（传统方式）

```csharp
#if CLIENT
using GameUI.Control.Behavior;

// 主菜单按钮 - 需要强烈的视觉反馈
var playButton = new TextButton("开始游戏");
playButton.AddTouchBehaviorWithDuration(
    scaleFactor: 0.9f,      // 明显缩放
    animationDuration: 100, // 快速响应
    longPressDuration: 1000 // 避免误触长按
);
#endif
```

### 场景2：工具栏按钮（流式语法）

```csharp
#if CLIENT
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;

// 工具栏按钮 - 需要快速响应
var toolButton = Button()
    .Size(60, 60)
    .Background(Colors.Gray)
    .QuickTouch()  // ✅ 预设：快速反馈
    .LongPress(() => ShowToolTip(), durationMs: 400)
    .Click(() => ExecuteTool())
    .AddToRoot();

// 或完整配置
var advancedToolButton = Button("高级工具")
    .Size(80, 60)
    .TouchBehavior(
        configure: tb => tb.LongPressTriggered += (s, e) => ShowToolTip(),
        scaleFactor: 0.97f
    )
    .AddToRoot();
#endif
```

### 场景3：列表项（流式语法）

```csharp
#if CLIENT
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;

// 列表项 - 长按显示菜单
var listItem = Panel()
    .Size(400, 60)
    .Background(Colors.White)
    .GentleTouch()  // ✅ 预设：轻柔反馈
    .LongPress(() => ShowContextMenu(), durationMs: 600)
    .Click(() => SelectItem())
    .AddToRoot();

// 或使用configure配置更多事件
var advancedListItem = Panel()
    .Size(400, 60)
    .TouchBehavior(
        configure: tb => 
        {
            tb.LongPressStarted += (s, e) => ShowHint("长按显示菜单");
            tb.LongPressTriggered += (s, e) => ShowContextMenu();
        },
        scaleFactor: 0.98f
    )
    .Click(() => 
    {
        SelectItem();
        // 注意：如果用户先触发了长按，这个点击事件不会执行
        Game.Logger.LogInformation("列表项被点击选择");
    })
    .AddToRoot();
#endif
```

### 场景4：事件冲突处理演示（流式语法）

```csharp
#if CLIENT
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;

// 演示长按与点击的冲突处理
var demoButton = Button("长按/点击测试")
    .Size(200, 80)
    .TouchBehavior(
        configure: tb => 
        {
            tb.LongPressTriggered += (s, e) =>
            {
                Game.Logger.LogInformation("✅ 长按触发 - 显示上下文菜单");
                ShowContextMenu();
            };
        },
        scaleFactor: 0.95f
    )
    .Click(() => 
    {
        // ⚠️ 重要：如果之前触发了长按，这个事件不会执行
        Game.Logger.LogInformation("✅ 点击触发 - 执行主要操作");
        ExecuteMainAction();
    })
    .AddToRoot();

/* 
 * 用户交互行为分析：
 * 1. 快速点击松开 → 只触发 Click
 * 2. 长按后松开 → 只触发 LongPressTriggered，不会触发 Click
 * 3. 长按期间松开 → 取消长按，不触发任何事件
 */
 
// 传统方式（对比参考）
var traditionalButton = new TextButton("长按/点击测试");
var touchBehavior = traditionalButton.AddTouchBehaviorWithDuration(
    scaleFactor: 0.95f,
    animationDuration: 150,
    longPressDuration: 500
);
touchBehavior.LongPressTriggered += (s, e) =>
{
    Game.Logger.LogInformation("✅ 长按触发 - 显示上下文菜单");
    ShowContextMenu();
};
traditionalButton.OnPointerClicked += (s, e) =>
{
    Game.Logger.LogInformation("✅ 点击触发 - 执行主要操作");
    ExecuteMainAction();
};
#endif
```

## 🔒 冲突处理机制

### ClickLockedPointerButtons 属性

TouchBehavior 提供了智能的事件冲突处理机制，通过 `ClickLockedPointerButtons` 属性防止长按过程中触发点击事件：

```csharp
public PointerButtons ClickLockedPointerButtons
{
    get
    {
        // 当满足以下条件时锁定点击事件：
        // 1. 当前处于长按状态 (isLongPressed = true)
        // 2. 有长按事件参数记录 (lastLongPressedEventArgs != null)  
        // 3. 至少订阅了一个长按事件
        return isLongPressed && lastLongPressedEventArgs != null &&
               (LongPressStarted is not null || 
                LongPressEnded is not null || 
                LongPressTriggered is not null)
            ? lastLongPressedEventArgs.PointerButtons
            : PointerButtons.None;
    }
}
```

### 冲突处理逻辑

1. **长按开始**：设置 `isLongPressed = true`，记录事件参数
2. **长按过程**：点击事件被 `ClickLockedPointerButtons` 屏蔽
3. **长按触发**：如果长按成功触发且有注册长按事件，后续的鼠标抬起不会触发Click事件
4. **长按结束**：重置状态，恢复点击事件响应
5. **取消长按**：如果在长按期间松开，也会重置状态

⚠️ **重要机制**：**如果触发了长按，且有注册长按事件，则鼠标抬起时普通的Click事件不会再触发**。这样设计是为了避免长按操作意外触发点击行为，提供更精确的用户交互体验。

## ⚡ 性能优化

### 动画优化
- 使用 `ArithmeticAnimation<ScaleAndOpacity>` 高效动画系统
- 支持硬件加速的缩放和透明度动画
- 自动管理动画生命周期，避免内存泄漏

### 事件优化
- 使用 `Game.Delay` 代替 `Task.Delay` 提高性能
- 智能的事件订阅检查，避免不必要的计算
- 延迟清理机制，避免频繁的对象创建

### 内存管理
```csharp
// TouchBehavior 自动处理资源清理
public override void Dispose()
{
    // 清理动画资源
    pressAnimation?.Dispose();
    
    // 取消延迟任务
    longPressTask?.Cancel();
    
    // 调用基类清理
    base.Dispose();
}
```

## 🛡️ 最佳实践

### ✅ 推荐做法（流式语法）

```csharp
#if CLIENT
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;

// 1. 根据控件重要性选择合适的动画强度
var importantButton = Button("重要")
    .Size(200, 80)
    .EmphasizedTouch()  // ✅ 使用预设：明显反馈
    .AddToRoot();

var subtleButton = Button("次要")
    .Size(150, 60)
    .QuickTouch()  // ✅ 使用预设：轻微反馈
    .AddToRoot();

// 2. 为不同场景配置合适的长按时间
var quickAction = Button("快速操作")
    .Size(120, 60)
    .TouchBehavior(
        scaleFactor: 0.97f,
        animationDurationMs: 80,
        longPressDurationMs: 300  // ✅ 快速长按
    )
    .AddToRoot();

var cautiousAction = Button("重要操作")
    .Size(180, 60)
    .TouchBehavior(
        scaleFactor: 0.92f,
        animationDurationMs: 150,
        longPressDurationMs: 800  // ✅ 谨慎长按，避免误触
    )
    .AddToRoot();

// 3. 合理使用事件订阅和方法引用
var menuButton = Button("菜单")
    .Size(100, 60)
    .TouchBehavior(
        configure: tb => tb.LongPressTriggered += OnLongPress  // ✅ 使用方法引用
    )
    .Click(OnQuickAction)  // ✅ 简洁的方法引用
    .AddToRoot();

// 4. 理解事件互斥机制
var dualActionButton = Button("双重操作")
    .Size(200, 80)
    .TouchBehavior(
        configure: tb => 
        {
            // 长按：显示详细信息或上下文菜单
            tb.LongPressTriggered += (s, e) => ShowDetailedInfo();
        }
    )
    .Click(() => 
    {
        // 点击：执行主要操作
        // ✅ 注意：长按触发后，这个事件不会执行
        ExecuteMainAction();
    })
    .AddToRoot();

// 5. 使用预设快捷方法提高代码可读性
var toolbar = Panel()
    .HStack()
    .Add(
        Button("工具1").QuickTouch().Click(() => Tool1()),
        Button("工具2").QuickTouch().Click(() => Tool2()),
        Button("工具3").QuickTouch().Click(() => Tool3())
    )
    .AddToRoot();
#endif
```

### ❌ 避免的做法

```csharp
#if CLIENT
// 1. 避免过度的动画效果
button.TouchBehavior(scaleFactor: 0.5f);  // ❌ 过度缩放，影响体验
// ✅ 推荐：使用 0.85-0.98 之间的值

// 2. 避免过短的长按时间
button.TouchBehavior(
    scaleFactor: 0.95f,
    animationDurationMs: 150,
    longPressDurationMs: 100  // ❌ 太短，容易误触
);
// ✅ 推荐：至少 300ms，通常 500-800ms

// 3. 避免在循环中不必要地创建行为
foreach (var item in items)
{
    item.Panel()
        .TouchBehavior()  // ⚠️ 大量创建可能影响性能
        .AddToRoot();
}
// ✅ 推荐：仅在需要交互的控件上添加

// 4. 避免忘记处理长按事件
var button = Button("测试")
    .TouchBehavior(enableLongPress: true)  // ❌ 启用了长按但未处理事件
    .AddToRoot();
// ✅ 推荐：如果启用长按，务必配置事件或使用 LongPress() 方法

// 5. 避免不必要的复杂配置
var simpleButton = Button("简单")
    .TouchBehavior(
        configure: tb => { /* 空配置 */ },  // ❌ 不必要的配置
        scaleFactor: 0.95f
    )
    .AddToRoot();
// ✅ 推荐：使用默认配置或预设方法
var betterButton = Button("简单")
    .TouchBehavior()  // 或 .QuickTouch() 或 .EmphasizedTouch()
    .AddToRoot();
#endif
```

## 🔧 扩展开发

### 自定义动画类型

TouchBehavior 使用了 `IArithmeticAnimatable<T>` 接口，可以扩展支持其他动画类型：

```csharp
// 自定义颜色动画类型
public record struct ColorAndScale(Color Color, float Scale) 
    : IArithmeticAnimatable<ColorAndScale>
{
    public static ColorAndScale operator +(ColorAndScale left, ColorAndScale right)
        => new(BlendColors(left.Color, right.Color), left.Scale + right.Scale);

    public static ColorAndScale operator -(ColorAndScale left, ColorAndScale right)
        => new(BlendColors(left.Color, right.Color, -1), left.Scale - right.Scale);

    public static ColorAndScale operator *(ColorAndScale left, float right)
        => new(MultiplyColor(left.Color, right), left.Scale * right);

    // 实现颜色混合逻辑...
}
```

### 自定义 TouchBehavior 子类

```csharp
public class CustomTouchBehavior : TouchBehavior
{
    public CustomTouchBehavior(Control control) : base(control)
    {
        // 自定义初始化逻辑
    }

    protected override void OnPressed(PointerEventArgs e)
    {
        // 自定义按压逻辑
        base.OnPressed(e);
        
        // 添加额外效果，如震动、声音等
        TriggerHapticFeedback();
    }

    private void TriggerHapticFeedback()
    {
        // 实现震动反馈
    }
}
```

## 🐛 故障排除

### 常见问题

1. **动画不显示**
   - 检查是否设置了 `enablePressAnimation = true`
   - 确认 `scaleFactor` 不等于 1.0
   - 验证控件是否正确添加到视觉树

2. **长按不触发**
   - 检查是否设置了 `enableLongPress = true`
   - 确认订阅了 `LongPressTriggered` 事件
   - 验证 `longPressDuration` 设置是否合理

3. **点击和长按冲突 / 点击事件不触发**
   - **这是设计行为**：长按触发后，Click事件会被自动屏蔽
   - **触发条件**：有注册长按事件 + 长按成功触发 = 屏蔽后续点击
   - **解决方案**：
     - 如果需要两种行为都支持，将逻辑放在不同的事件中
     - 使用更短的 `longPressDuration` 来减少误触
     - 检查 `ClickLockedPointerButtons` 属性了解当前状态

4. **性能问题**
   - 避免在大量控件上同时使用复杂动画
   - 考虑禁用不需要的功能（动画或长按）
   - 检查是否正确释放了 TouchBehavior 资源

### 调试信息

```csharp
#if CLIENT && DEBUG
// 启用 TouchBehavior 调试日志
touchBehavior.LongPressStarted += (s, e) =>
{
    Game.Logger.LogDebug("TouchBehavior: Long press started");
};

touchBehavior.LongPressTriggered += (s, e) =>
{
    Game.Logger.LogDebug("TouchBehavior: Long press triggered");
};

touchBehavior.LongPressEnded += (s, e) =>
{
    Game.Logger.LogDebug("TouchBehavior: Long press ended");
};
#endif
```

## 📚 相关文档

- [控件行为系统](ControlBehavior.md)
- [UI设计标准](../guides/UIDesignStandards.md)
- [事件系统](../systems/TriggerSystem.md)
- [动画系统](../systems/AnimationSystem.md)
- [UI最佳实践](../best-practices/UIBestPractices.md)

## 🔮 未来扩展

### 计划功能
1. **手势识别**：支持滑动、双击等复杂手势
2. **音效集成**：内置音效反馈支持
3. **震动反馈**：移动设备震动反馈
4. **主题系统**：预定义的动画主题和样式

### 兼容性
- TouchBehavior 完全兼容现有的控件系统
- 可以与其他 ControlBehavior 组合使用
- 支持运行时动态添加和移除
