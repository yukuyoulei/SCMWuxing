# VirtualizingPanel 虚拟化列表系统

> **适用版本**: WasiCore v1.0+  
> **更新日期**: 2025-10-12  
> **难度级别**: 中级

---

## 📚 目录

- [概述](#概述)
- [什么是虚拟化](#什么是虚拟化)
- [快速开始](#快速开始)
- [核心概念](#核心概念)
- [模板系统详解](#模板系统详解)
- [配置详解](#配置详解)
- [流式API扩展](#流式api扩展)
- [使用指南](#使用指南)
- [性能优化](#性能优化)
- [最佳实践](#最佳实践)
- [限制和注意事项](#限制和注意事项)
- [常见问题](#常见问题)

---

## 概述

**VirtualizingPanel** 是WasiCore框架中的高性能列表控件，专为显示大量数据而设计。通过只渲染可见区域的项目，它能够轻松处理数万甚至数十万条数据，而不会影响UI性能。

### 核心特性

- 🚀 **极高性能**: 只渲染可见项，内存占用极低
- ♻️ **控件回收**: 支持Recycling模式，重用控件实例
- 📏 **灵活滚动**: 支持像素和项目两种滚动单位
- 🔄 **动态更新**: 支持数据集的动态增删改
- 📐 **自动布局**: 自动测量和排列子项
- 🎯 **数据驱动**: 基于GameData模板系统，支持虚拟化阶段回调

### 使用场景

| 场景 | 数据量 | 推荐控件 | 原因 |
|------|--------|----------|------|
| 小型列表 | < 50项 | `Panel` | 简单，无需虚拟化开销 |
| 中型列表 | 50-500项 | `VirtualizingPanel` | 平衡性能和功能 |
| 大型列表 | 500-10000项 | `VirtualizingPanel (Recycling)` | 高性能，控件回收 |
| 超大列表 | > 10000项 | `VirtualizingPanel (Recycling)` + 分页 | 极致性能，配合数据分页 |

---

## 什么是虚拟化

### 传统Panel的问题

```csharp
// ❌ 传统Panel - 创建所有控件
var panel = new Panel();
panel.FlowOrientation = Orientation.Vertical;

foreach (var item in hugeDataset) // 假设10000项
{
    panel.AddChild(new Label(item.Name));
    // 创建了10000个Label控件！
}
// 问题：
// 1. 内存占用：10000个控件常驻内存
// 2. 初始化慢：创建10000个控件需要时间
// 3. 布局开销：布局系统需要处理所有控件
// 4. 渲染负担：即使不可见也占用资源
```

### 虚拟化的解决方案

```csharp
// ✅ VirtualizingPanel - 只创建可见控件
var template = CreateItemTemplate(); // 创建数据模板
var panel = new VirtualizingPanel();
panel.ItemsSource = hugeDataset; // 10000项
panel.ItemTemplate = template;  // ⭐ 使用IGameLink<GameDataControl>模板
panel.ItemSize = new SizeF(400, 40);  // ⭐ 显式指定

// 实际只创建约20-30个Label控件（取决于可见区域）
// 优势：
// 1. 内存占用：只有可见项的控件在内存中
// 2. 快速初始化：只创建可见部分
// 3. 布局高效：只布局可见项
// 4. 滚动流畅：动态创建/销毁控件
```

### 虚拟化原理

```
┌─────────────────────────────────┐
│   Viewport (可见区域)            │
│                                  │
│  ┌──────────────────────┐       │
│  │ Item 0 (Label)       │ ← 创建并显示
│  ├──────────────────────┤       │
│  │ Item 1 (Label)       │ ← 创建并显示
│  ├──────────────────────┤       │
│  │ Item 2 (Label)       │ ← 创建并显示
│  ├──────────────────────┤       │
│  │ Item 3 (Label)       │ ← 创建并显示
│  └──────────────────────┘       │
└─────────────────────────────────┘
│ Item 4 (数据存在)       │ ← 不可见，不创建控件
│ Item 5 (数据存在)       │ ← 不可见，不创建控件
│ ... 9995 other items    │ ← 不可见，不创建控件
```

---

## 快速开始

### 最简单的示例

```csharp
#if CLIENT
using GameData;
using GameData.Extension;
using GameUI.Control.Primitive;
using GameUI.Control.Data;

// 1. 准备数据
var items = Enumerable.Range(1, 1000)
    .Select(i => new { Id = i, Name = $"Item {i}" })
    .ToList();

// 2. 创建项目模板（GameDataControl）
var templateLink = new GameLink<GameDataControl, GameDataControlLabel>("SimpleItemTemplate");
var template = new GameDataControlLabel(templateLink)
{
    FontSize = 14,
    Layout = new()
    {
        Width = 300,
        Height = 40,
        Padding = new Thickness(10, 5)
    },
#if CLIENT
    OnVirtualizationPhase = 
    [
        static (c) =>
        {
            if (c is Label label && label.DataContext != null)
            {
                label.Text = label.DataContext.ToString();
            }
        }
    ]
#endif
};

// 3. 创建虚拟化面板
var panel = new VirtualizingPanel
{
    ItemsSource = items,
    ItemTemplate = template.Link,  // ⭐ 使用模板链接
    ItemSize = new SizeF(300, 40)  // ⭐ 显式指定尺寸
};

// 4. 设置布局
panel.Size(300, 400);
panel.GenerateChildren();  // 生成子控件
panel.AddToVisualTree();
#endif
```

### OnVirtualizationPhase 回调

`OnVirtualizationPhase`是虚拟化系统的核心机制。当控件被虚拟化（创建或回收重用）时，系统会调用这些回调来更新控件内容。

```csharp
#if CLIENT
// OnVirtualizationPhase是一个Action<Control>数组
OnVirtualizationPhase = 
[
    static (c) =>  // 使用static lambda避免闭包捕获
    {
        // c是当前控件实例
        // c.DataContext是当前数据项
        
        if (c is Label label && label.DataContext is MyData data)
        {
            // 根据数据更新控件
            label.Text = data.Name;
            label.TextColor = data.IsActive ? Color.Green : Color.Gray;
        }
    }
]
#endif
```

**重要**：
- ⭐ 通过`c.DataContext`获取当前项的数据
- ⭐ 在回调中更新控件的可变属性（如Text、Color等）

---

## 核心概念

### ItemsSource - 数据源

**类型**: `IEnumerable?`  
**必须**: 是（虚拟化必需）

数据源是虚拟化的基础，可以是任何实现了`IEnumerable`的集合。

```csharp
// 支持各种集合类型
panel.ItemsSource = new List<string>();           // List
panel.ItemsSource = new ObservableCollection<>();  // Observable
panel.ItemsSource = myArray;                       // Array
panel.ItemsSource = Enumerable.Range(1, 1000);    // LINQ
```

⚠️ **重要**: 修改数据源后需要调用 `GenerateChildren()` 来刷新列表。

### ItemTemplate - 项模板

**类型**: `IGameLink<GameDataControl>?`  
**必须**: 是（虚拟化必需）

⚠️ **注意**：`ItemTemplate`不是一个函数，而是一个**数据模板链接**。

```csharp
#if CLIENT
// ✅ 正确：创建GameDataControl模板
var templateLink = new GameLink<GameDataControl, GameDataControlPanel>("MyTemplate");
var template = new GameDataControlPanel(templateLink)
{
    Layout = new()
    {
        Width = 400,
        Height = 60
    },
    OnVirtualizationPhase = 
    [
        static (c) =>
        {
            if (c is Panel panel && c.DataContext is MyData data)
            {
                // 更新控件内容
                if (panel.Children?[0] is Label label)
                {
                    label.Text = data.Name;
                }
            }
        }
    ],
    Children = 
    [
        new GameDataControlLabel(new GameLink<GameDataControl, GameDataControlLabel>("ChildLabel"))
        {
            FontSize = 14
        }.Link
    ]
};

panel.ItemTemplate = template.Link;  // ⭐ 赋值链接

// ❌ 错误：不要使用函数
panel.ItemTemplate = (item) => new Label(item.ToString());  // 编译错误！
#endif
```

### ItemSize - 项目尺寸

**类型**: `SizeF?`  
**默认值**: `null`（自动推测）  
**推荐**: ⭐ **强烈建议手动指定**

```csharp
// ✅ 显式指定（推荐）
panel.ItemSize = new SizeF(400, 80);

// ⚠️ 自动推测（不推荐）
// 系统会尝试从第一个子元素的ActualSize推测尺寸
// 这可能导致性能问题和不准确的计算
```

**为什么要手动指定？**

| 方式 | 优点 | 缺点 | 性能影响 |
|------|------|------|----------|
| **显式指定** | ✅ 计算准确<br>✅ 性能最优<br>✅ 可预测行为 | 需要知道项目尺寸 | ⚡ 无额外开销 |
| **自动推测** | 无需计算 | ❌ 首次渲染延迟<br>❌ 可能不准确<br>❌ 依赖第一个元素 | ⚠️ 需要创建第一个子元素 |

### VirtualizationMode - 虚拟化模式

**类型**: `VirtualizationMode`  
**默认值**: `Standard`

| 模式 | 行为 | 性能 | 适用场景 |
|------|------|------|----------|
| **Standard** | 离开视口时销毁控件 | 中等 | < 1000项 |
| **Recycling** | 离开视口时回收并重用控件 | 极高 | > 1000项 |

```csharp
// Standard模式（默认）
panel.VirtualizationMode = VirtualizationMode.Standard;

// Recycling模式（推荐大数据）
panel.VirtualizationMode = VirtualizationMode.Recycling;
```

### ScrollUnit - 滚动单位

**类型**: `ScrollUnit`  
**默认值**: `Pixel`

| 单位 | 行为 | 适用场景 |
|------|------|----------|
| **Pixel** | 按像素平滑滚动 | 通用，流畅的用户体验 |
| **Item** | 按项目步进滚动 | 固定高度项，精确对齐 |

---

## 模板系统详解

### 什么是GameDataControl模板？

`GameDataControl`是WasiCore框架的数据驱动UI系统的核心。在虚拟化列表中，模板是一个**可重用的控件定义**，而不是控件实例本身。

### 模板的生命周期

```
1. 创建模板定义 (GameDataControl)
       ↓
2. 设置ItemTemplate = template.Link
       ↓
3. 当项进入视口 → 从模板创建控件实例
       ↓
4. 设置控件的DataContext = 数据项
       ↓
5. 调用OnVirtualizationPhase回调
       ↓
6. 控件显示在视口中
       ↓
7. 当项离开视口:
   回收控件，返回步骤4重用
```

### 创建复杂模板

```csharp
#if CLIENT
public class UserItemTemplate
{
    public static IGameLink<GameDataControl> Create()
    {
        var link = new GameLink<GameDataControl, GameDataControlPanel>("UserItemTemplate");
        
        var template = new GameDataControlPanel(link)
        {
            Layout = new()
            {
                Width = 400,
                Height = 72,
                Padding = new Thickness(12),
                FlowOrientation = Orientation.Horizontal
            },
            Background = new SolidColorBrush(Color.White),
            OnVirtualizationPhase = 
            [
                static (c) =>
                {
                    if (c is Panel panel && c.DataContext is User user)
                    {
                        // 更新头像
                        if (panel.Children?[0] is Panel avatar)
                        {
                            // 设置头像背景
                        }
                        
                        // 更新名称
                        if (panel.Children?[1] is Panel info &&
                            info.Children?[0] is Label nameLabel)
                        {
                            nameLabel.Text = user.Name;
                        }
                        
                        // 更新邮箱
                        if (panel.Children?[1] is Panel info2 &&
                            info2.Children?[1] is Label emailLabel)
                        {
                            emailLabel.Text = user.Email;
                        }
                        
                        // 更新状态
                        if (panel.Children?[2] is Label statusLabel)
                        {
                            statusLabel.Text = user.IsActive ? "活跃" : "离线";
                            statusLabel.TextColor = user.IsActive 
                                ? DesignColors.Success 
                                : DesignColors.Warning;
                        }
                    }
                }
            ],
            Children = 
            [
                // 头像
                new GameDataControlPanel(new GameLink<GameDataControl, GameDataControlPanel>("Avatar"))
                {
                    Layout = new()
                    {
                        Width = 48,
                        Height = 48,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    Background = new SolidColorBrush(DesignColors.Primary),
                    CornerRadius = 24
                }.Link,
                
                // 用户信息
                new GameDataControlPanel(new GameLink<GameDataControl, GameDataControlPanel>("UserInfo"))
                {
                    Layout = new()
                    {
                        FlowOrientation = Orientation.Vertical,
                        FlexGrow = 1,
                        Gap = 4
                    },
                    Children = 
                    [
                        // 名称
                        new GameDataControlLabel(new GameLink<GameDataControl, GameDataControlLabel>("NameLabel"))
                        {
                            FontSize = 16,
                            Bold = true
                        }.Link,
                        
                        // 邮箱
                        new GameDataControlLabel(new GameLink<GameDataControl, GameDataControlLabel>("EmailLabel"))
                        {
                            FontSize = 12,
                            TextColor = DesignColors.Secondary
                        }.Link
                    ]
                }.Link,
                
                // 状态标签
                new GameDataControlLabel(new GameLink<GameDataControl, GameDataControlLabel>("StatusLabel"))
                {
                    FontSize = 12,
                    Layout = new()
                    {
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }.Link
            ]
        };
        
        return template.Link;
    }
}

// 使用模板
var panel = new VirtualizingPanel
{
    ItemsSource = users,
    ItemTemplate = UserItemTemplate.Create(),
    ItemSize = new SizeF(400, 72)
};
#endif
```

### ItemTemplateSelector - 模板选择器

当需要根据数据类型动态选择不同模板时，使用`ItemTemplateSelector`。

```csharp
#if CLIENT
// 定义模板选择器委托
public delegate IGameLink<GameDataControl>? ItemTemplateSelector(object item);

// 创建多个模板
var textTemplate = CreateTextItemTemplate();
var imageTemplate = CreateImageItemTemplate();
var videoTemplate = CreateVideoItemTemplate();

// 创建选择器
ItemTemplateSelector selector = (item) =>
{
    return item switch
    {
        TextMessage _ => textTemplate,
        ImageMessage _ => imageTemplate,
        VideoMessage _ => videoTemplate,
        _ => textTemplate
    };
};

// 使用选择器
var panel = new VirtualizingPanel
{
    ItemsSource = messages,
    ItemTemplateSelector = selector,
    ItemSize = new SizeF(400, 80)
};
#endif
```

---

## 配置详解

### 完整配置示例

```csharp
#if CLIENT
var template = CreateItemTemplate();

var panel = new VirtualizingPanel
{
    // ===== 必需配置 =====
    ItemsSource = myDataCollection,
    ItemTemplate = template,  // IGameLink<GameDataControl>
    ItemSize = new SizeF(400, 80),  // ⭐ 强烈推荐显式指定
    
    // ===== 尺寸配置 =====
    Width = 400,
    Height = 600,
    
    // ===== 虚拟化配置 =====
    VirtualizationMode = VirtualizationMode.Recycling,
    IsVirtualizing = true,
    
    // ===== 缓存配置 =====
    CacheLength = new VirtualizationCacheLength(1.0, 1.0),  // 前后各缓存1页
    CacheLengthUnit = VirtualizationCacheLengthUnit.Page,
    
    // ===== 滚动配置 =====
    ScrollUnit = ScrollUnit.Pixel,
    ArrangeOnScroll = true,
    
    // ===== 布局配置 =====
    FlowOrientation = Orientation.Vertical,
    HorizontalAlignment = HorizontalAlignment.Stretch,
    VerticalAlignment = VerticalAlignment.Stretch,
    
    // ===== 样式配置 =====
    Background = new SolidColorBrush(Color.White),
    Padding = new Thickness(8),
};

panel.GenerateChildren();  // 生成初始子控件
#endif
```

---

## 流式API扩展

WasiCore框架为`VirtualizingPanel`提供了专用的流式扩展API（`VirtualizingExtensions`），让虚拟化列表的创建更加简洁和直观。

### 为什么需要专用扩展？

虚拟化面板有一些特殊的配置需求（如`ItemTemplate`、`ItemSize`、缓存配置等），流式扩展提供了类型安全的链式API来简化这些配置。

### 创建方法

#### VirtualList - 创建垂直虚拟化列表

```csharp
#if CLIENT
using GameUI.Control.Extensions;

var template = CreateItemTemplate();

// 方式1：通过VirtualUI工厂类
var panel = VirtualUI.VirtualList(
    template,                    // 项模板
    new SizeF(400, 60),         // 项大小（可选）
    cachePages: 1.0f            // 缓存页数（可选）
);

// 方式2：通过扩展方法
var panel = VirtualizingExtensions.VirtualList(template, new SizeF(400, 60));
#endif
```

**参数说明**：
- `itemTemplate`: `IGameLink<GameDataControl>` - 项模板（必需）
- `itemSize`: `SizeF?` - 项大小，推荐显式指定
- `cachePages`: `float` - 缓存页数，默认1.0（前后各缓存1页）

**自动配置**：
- `FlowOrientation = Orientation.Vertical` - 垂直滚动
- `ScrollEnabled = true` - 启用滚动
- `ArrangeOnScroll = true` - 滚动时自动重新布局
- `CacheLengthUnit = VirtualizationCacheLengthUnit.Page` - 按页缓存

#### VirtualHorizontalList - 创建水平虚拟化列表

```csharp
#if CLIENT
var template = CreateItemTemplate();

var panel = VirtualUI.VirtualHorizontalList(
    template,
    new SizeF(120, 300),  // 水平滚动时，宽度是关键
    cachePages: 1.0f
);
#endif
```

**与VirtualList的区别**：
- `FlowOrientation = Orientation.Horizontal` - 水平滚动

#### VirtualListWithSelector - 使用模板选择器

```csharp
#if CLIENT
// 定义选择器
ItemTemplateSelector selector = (item) =>
{
    return item switch
    {
        TextMessage _ => textTemplate,
        ImageMessage _ => imageTemplate,
        _ => textTemplate
    };
};

var panel = VirtualUI.VirtualListWithSelector(
    selector,
    Orientation.Vertical,    // 方向
    new SizeF(400, 80)       // 项大小
);
#endif
```

### 配置扩展方法

#### Cache() - 设置缓存配置

```csharp
#if CLIENT
panel.Cache(
    beforeViewport: 1.5f,    // 视口前缓存1.5页
    afterViewport: 1.5f,     // 视口后缓存1.5页
    VirtualizationCacheLengthUnit.Page  // 单位：页
);

// 也可以按项数缓存
panel.Cache(
    beforeViewport: 10,      // 视口前缓存10项
    afterViewport: 10,       // 视口后缓存10项
    VirtualizationCacheLengthUnit.Item  // 单位：项
);
#endif
```

#### ItemSize() - 设置项大小

```csharp
#if CLIENT
// 方式1：指定宽度和高度
panel.ItemSize(400, 60);

// 方式2：正方形项
panel.ItemSize(100);  // 100x100

// 方式3：使用SizeF
panel.ItemSize = new SizeF(400, 60);
#endif
```

#### Template() - 设置项模板

```csharp
#if CLIENT
var template = CreateItemTemplate();
panel.Template(template);  // 链式调用
#endif
```

#### TemplateSelector() - 设置模板选择器

```csharp
#if CLIENT
ItemTemplateSelector selector = (item) => { /* ... */ };
panel.TemplateSelector(selector);
#endif
```

#### Items() - 设置数据源

```csharp
#if CLIENT
var data = new List<MyData>();

// autoVirtualization = true（默认）- 自动调用GenerateChildren()
panel.Items(data);

// autoVirtualization = false - 需要手动调用GenerateChildren()
panel.Items(data, autoVirtualization: false);
// ... 稍后
panel.GenerateChildren();
#endif
```

#### ArrangeOnScroll() - 控制滚动时布局

```csharp
#if CLIENT
// 启用（默认）
panel.ArrangeOnScroll(true);

// 禁用
panel.ArrangeOnScroll(false);
#endif
```

### 完整示例：流式API

```csharp
#if CLIENT
using GameUI.Control.Extensions;
using GameData;
using GameData.Extension;

public class UserListPanel
{
    public VirtualizingPanel CreateUserList(List<User> users)
    {
        // 1. 创建模板
        var template = CreateUserTemplate();
        
        // 2. 使用流式API创建虚拟化列表
        var panel = VirtualUI.VirtualList(template, new SizeF(400, 72))
            .Items(users)                              // 设置数据源
            .Cache(1.5f, 1.5f)                        // 增加缓存
            .Size(400, 600)                           // 设置面板尺寸
            .Background(DesignColors.Background)      // 背景色
            .Padding(8)                               // 内边距
            .Rounded(8);                              // 圆角
        
        return panel;
    }
    
    private IGameLink<GameDataControl> CreateUserTemplate()
    {
        var link = new GameLink<GameDataControl, GameDataControlPanel>("UserItemTemplate");
        
        var template = new GameDataControlPanel(link)
        {
            Layout = new()
            {
                Width = 400,
                Height = 72,
                Padding = new Thickness(12),
                FlowOrientation = Orientation.Horizontal
            },
            Background = new SolidColorBrush(Color.White),
            OnVirtualizationPhase = 
            [
                static (c) =>
                {
                    if (c is Panel panel && c.DataContext is User user)
                    {
                        // 更新用户信息
                        if (panel.Children?[1] is Panel info &&
                            info.Children?[0] is Label nameLabel)
                        {
                            nameLabel.Text = user.Name;
                        }
                    }
                }
            ],
            // ... 子控件定义
        };
        
        return template.Link;
    }
}
#endif
```

### 流式API vs 基础API对比

#### 基础API（对象初始化器）

```csharp
#if CLIENT
var template = CreateItemTemplate();

var panel = new VirtualizingPanel
{
    ItemsSource = myDataCollection,
    ItemTemplate = template,
    ItemSize = new SizeF(400, 60),
    FlowOrientation = Orientation.Vertical,
    ScrollEnabled = true,
    ArrangeOnScroll = true,
    CacheLength = new VirtualizationCacheLength(1.5, 1.5),
    CacheLengthUnit = VirtualizationCacheLengthUnit.Page,
    Width = 400,
    Height = 600,
    Background = new SolidColorBrush(Color.White),
    Padding = new Thickness(8)
};

panel.GenerateChildren();
#endif
```

#### 流式API（链式调用）

```csharp
#if CLIENT
var template = CreateItemTemplate();

var panel = VirtualUI.VirtualList(template, new SizeF(400, 60))
    .Items(myDataCollection)  // 自动调用GenerateChildren()
    .Cache(1.5f, 1.5f)
    .Size(400, 600)
    .Background(Color.White)
    .Padding(8);
#endif
```

**对比总结**：

| 特性 | 基础API | 流式API |
|------|---------|---------|
| **代码行数** | 更多 | 更少 |
| **可读性** | 中等 | 更好（链式结构清晰） |
| **类型安全** | 高 | 高 |
| **配置默认值** | 需要手动设置 | 自动设置常用默认值 |
| **GenerateChildren** | 需要手动调用 | `Items()`自动调用 |
| **学习曲线** | 稍陡 | 平缓（符合流式布局习惯） |
| **AI友好性** | 好 | 更好（一致的链式模式） |

### 选择建议

**使用流式API**：
- ✅ 创建新的虚拟化列表
- ✅ 代码简洁性优先
- ✅ 与其他流式布局代码风格一致

**使用基础API**：
- ✅ 需要细粒度控制每个属性
- ✅ 在已有代码中修改
- ✅ 配置非常特殊的虚拟化场景

**混合使用**：
```csharp
#if CLIENT
// 可以先用流式API创建，再用基础API修改
var panel = VirtualUI.VirtualList(template, new SizeF(400, 60))
    .Items(data);

// 然后修改特殊属性
panel.VirtualizationMode = VirtualizationMode.Standard;  // 基础API
panel.ScrollUnit = ScrollUnit.Item;                      // 基础API
#endif
```

### 常见模式

#### 模式1：快速原型

```csharp
#if CLIENT
// 最简洁的虚拟化列表
var panel = VirtualUI.VirtualList(template)
    .Items(data);
#endif
```

#### 模式2：生产环境

```csharp
#if CLIENT
// 完整配置的虚拟化列表
var panel = VirtualUI.VirtualList(template, new SizeF(400, 60))
    .Items(data)
    .Cache(2.0f, 2.0f)  // 增加缓存减少闪烁
    .Size(400, 600)
    .Background(DesignColors.Background)
    .Padding(16)
    .Rounded(8);

// 设置Recycling模式（大数据）
panel.VirtualizationMode = VirtualizationMode.Recycling;
#endif
```

#### 模式3：响应式列表

```csharp
#if CLIENT
var template = CreateItemTemplate();
var data = new ObservableCollection<MyData>();

var panel = VirtualUI.VirtualList(template, new SizeF(400, 60))
    .Items(data)  // 使用ObservableCollection自动更新
    .Size(400, 600);

// 后续添加数据会自动刷新UI
data.Add(newItem);
#endif
```

---

## 使用指南

### 场景1：简单文本列表

```csharp
#if CLIENT
// 数据
var tasks = new List<string>
{
    "完成文档编写",
    "代码审查",
    "测试功能"
    // ... 1000+ 任务
};

// 创建模板
var templateLink = new GameLink<GameDataControl, GameDataControlLabel>("TaskItemTemplate");
var template = new GameDataControlLabel(templateLink)
{
    FontSize = 14,
    Layout = new()
    {
        Height = 48,
        Padding = new Thickness(12, 8),
        HorizontalAlignment = HorizontalAlignment.Stretch
    },
    Background = new SolidColorBrush(DesignColors.Surface),
    OnVirtualizationPhase = 
    [
        static (c) =>
        {
            if (c is Label label && label.DataContext != null)
            {
                label.Text = label.DataContext.ToString();
            }
        }
    ]
};

// 创建虚拟化面板
var taskList = new VirtualizingPanel
{
    Width = 300,
    Height = 400,
    ItemsSource = tasks,
    ItemTemplate = template.Link,
    ItemSize = new SizeF(300, 48)
};

taskList.GenerateChildren();
#endif
```

### 场景2：用户列表

```csharp
#if CLIENT
public class User
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Status { get; set; }
}

var users = LoadUsers(); // 5000+ users

// 创建用户项模板（见"模板系统详解"章节的UserItemTemplate示例）
var template = UserItemTemplate.Create();

var userList = new VirtualizingPanel
{
    Width = 400,
    Height = 600,
    VirtualizationMode = VirtualizationMode.Recycling,
    ItemsSource = users,
    ItemTemplate = template,
    ItemSize = new SizeF(400, 72)
};

userList.GenerateChildren();
#endif
```

---

## 性能优化

### 1. 显式指定ItemSize

```csharp
// ⚡ 避免首帧延迟
panel.ItemSize = new SizeF(400, 60);
```

### 2. 优化OnVirtualizationPhase回调

```csharp
#if CLIENT
OnVirtualizationPhase = 
[
    static (c) =>  // ⭐ 使用static lambda
    {
        // ✅ 只更新必要的属性
        if (c is Label label && label.DataContext is MyData data)
        {
            label.Text = data.Name;  // 简单赋值
        }
        
        // ❌ 避免复杂计算
        // var complexResult = HeavyComputation(data);  // 不要！
    }
]
#endif
```

### 4. 调整缓存大小

```csharp
// 增加缓存可以减少滚动时的闪烁，但会占用更多内存
panel.CacheLength = new VirtualizationCacheLength(2.0, 2.0);  // 前后各2页
```

### 5. 性能对比

```csharp
// 测试场景：10000项的列表

// ✅ 最优配置：
// - ItemSize: 显式指定
// - VirtualizationMode: Recycling
// - OnVirtualizationPhase: static lambda
// 性能：首次渲染 ~5ms，滚动 60 FPS，内存占用 ~2MB

// ⚠️ 次优配置：
// - ItemSize: 自动推测
// - VirtualizationMode: Standard
// 性能：首次渲染 ~15ms，滚动 30-60 FPS，内存占用 ~3MB
```

---

## 最佳实践

### ✅ 推荐做法

```csharp
#if CLIENT
// 1. 显式指定ItemSize
panel.ItemSize = new SizeF(400, 60);

// 2. 使用static lambda避免闭包
OnVirtualizationPhase = [static (c) => { /* ... */ }]

// 3. 大数据使用Recycling模式
panel.VirtualizationMode = VirtualizationMode.Recycling;

// 4. 使用ObservableCollection实现自动更新
var data = new ObservableCollection<Item>();
panel.ItemsSource = data;

// 5. 将模板定义提取为独立方法
public static IGameLink<GameDataControl> CreateItemTemplate() { /* ... */ }
#endif
```

### ❌ 避免的做法

```csharp
#if CLIENT
// 1. ❌ 不要直接AddChild
var panel = new VirtualizingPanel();
panel.AddChild(new Label("Item"));  // 错误！失去虚拟化


// 4. ❌ 不要忘记调用GenerateChildren
panel.ItemsSource = data;
panel.ItemTemplate = template;
// 缺少：panel.GenerateChildren();

// 5. ❌ 不要尝试将函数赋值给ItemTemplate
panel.ItemTemplate = (item) => CreateControl(item);  // ❌ 类型错误！
#endif
```

---

## 限制和注意事项

### 1. 拖拽限制

**问题**：虚拟化列表中的控件是临时的，拖拽过程中如果原父级被回收，会导致异常。

```csharp
#if CLIENT
// ✅ 解决方案：转移到持久的拖拽层
private Control draggedControl;
private Panel persistentDragLayer;

public void SetupVirtualizingPanelWithDrag()
{
    // 1. 创建持久的拖拽层
    persistentDragLayer = new Panel()
    {
        Name = "DragLayer",
        IsHitTestVisible = false
    };
    persistentDragLayer.AddToRoot();
    
    // 2. 设置虚拟化面板（见完整示例）
}

// 在OnVirtualizationPhase中设置拖拽：
OnVirtualizationPhase = 
[
    (c) =>
    {
        if (c is Panel itemControl)
        {
            Panel originalParent = null;
            
            itemControl.OnPointerPressed += (s, e) =>
            {
                originalParent = itemControl.Parent as Panel;
                if (originalParent != null)
                {
                    originalParent.RemoveChild(itemControl);
                    persistentDragLayer.AddChild(itemControl);  // 转移到持久层
                }
                itemControl.CapturePointer(e.PointerButtons);
            };
            
            itemControl.OnPointerReleased += (s, e) =>
            {
                itemControl.ReleasePointer(e.PointerButtons);
                persistentDragLayer.RemoveChild(itemControl);
                // 触发数据层重新排序，让虚拟化系统重新创建
            };
        }
    }
]
#endif
```

### 2. 控件引用的暂时性

**问题**：虚拟化列表中的控件是临时的，不应保存引用。

```csharp
#if CLIENT
// ❌ 错误：保存控件引用
var panel = new VirtualizingPanel { /* ... */ };
var firstItemControl = panel.Children[0];  // 危险！

// ✅ 正确：通过数据引用
// 在OnVirtualizationPhase中使用c.DataContext
// 或者通过数据ID查找
#endif
```

### 3. 不能直接访问子元素

```csharp
#if CLIENT
// ❌ 错误：遍历虚拟化面板的Children
var panel = new VirtualizingPanel { /* ... */ };
foreach (var child in panel.Children)
{
    // 只能访问到当前可见的项！
}

// ✅ 正确：遍历数据源
foreach (var item in panel.ItemsSource)
{
    // 操作数据
}
#endif
```

### 4. 所有项必须固定尺寸

```csharp
#if CLIENT
// ❌ 不适合：高度差异很大的项
var panel = new VirtualizingPanel
{
    ItemTemplate = CreateVariableHeightTemplate()  // 高度不固定
};

// ✅ 适合：固定或接近固定高度的项
var panel = new VirtualizingPanel
{
    ItemSize = new SizeF(400, 60),  // 所有项使用相同尺寸
    ItemTemplate = CreateFixedHeightTemplate()
};
#endif
```

---

## 常见问题

### Q1: 列表没有显示任何内容？

**可能原因**：
1. 未设置`ItemsSource`或`ItemTemplate`
2. 未调用`GenerateChildren()`
3. 数据源为空
4. 模板创建失败

**解决方案**:
```csharp
// 检查清单
var template = CreateItemTemplate();  // ✅ 创建模板
var panel = new VirtualizingPanel
{
    ItemsSource = data,          // ✅ 已设置
    ItemTemplate = template,     // ✅ 已设置
    ItemSize = new SizeF(400, 60), // ✅ 建议设置
    Width = 400,                 // ✅ 已设置
    Height = 600                 // ✅ 已设置
};

panel.GenerateChildren();  // ✅ 必须调用

// 验证数据
if (data == null || !data.Any())
{
    Game.Logger.LogWarning("数据源为空");
}
```

### Q2: ItemTemplate类型错误？

```csharp
// ❌ 错误：使用函数
panel.ItemTemplate = (item) => new Label(item.ToString());

// ✅ 正确：使用IGameLink<GameDataControl>
var template = CreateItemTemplate();  // 返回IGameLink<GameDataControl>
panel.ItemTemplate = template;
```

### Q3: 如何更新列表数据？

```csharp
#if CLIENT
// 方式1：替换整个数据源
panel.ItemsSource = newDataCollection;
panel.GenerateChildren();

// 方式2：使用ObservableCollection（推荐）
var observableData = new ObservableCollection<MyData>();
panel.ItemsSource = observableData;

observableData.Add(newItem);      // 自动刷新
observableData.RemoveAt(0);       // 自动刷新
#endif
```

### Q4: 滚动性能差？

```csharp
// 优化checklist：
panel.VirtualizationMode = VirtualizationMode.Recycling;  // ✅ 使用Recycling
panel.ItemSize = new SizeF(400, 60);  // ✅ 显式指定尺寸
// OnVirtualizationPhase使用static lambda  // ✅ 避免闭包
// OnVirtualizationPhase中避免复杂计算  // ✅ 保持简单
```

### Q5: 如何实现点击事件？

```csharp
#if CLIENT
OnVirtualizationPhase = 
[
    static (c) =>
    {
        if (c is Panel panel && panel.DataContext is MyData data)
        {
            // 更新内容
            UpdatePanelContent(panel, data);
            
            // 添加点击事件
            panel.OnPointerPressed -= OnItemClicked;  // 先移除旧的
            panel.OnPointerPressed += OnItemClicked;  // 添加新的
        }
    }
]

static void OnItemClicked(object sender, PointerPressedEventArgs e)
{
    if (sender is Control control && control.DataContext is MyData data)
    {
        Game.Logger.LogInformation($"Clicked: {data.Name}");
    }
}
#endif
```

---

## 附录：完整示例

### 聊天消息列表

```csharp
#if CLIENT
using GameData;
using GameData.Extension;
using GameUI.Control.Data;
using GameUI.Control.Primitive;
using System.Collections.ObjectModel;

public class ChatMessage
{
    public string Sender { get; set; }
    public string Content { get; set; }
    public DateTime Time { get; set; }
    public bool IsMine { get; set; }
}

public class ChatPanel
{
    private ObservableCollection<ChatMessage> messages;
    private VirtualizingPanel chatList;
    
    public void Initialize()
    {
        messages = new ObservableCollection<ChatMessage>();
        
        // 创建消息模板
        var template = CreateMessageTemplate();
        
        // 创建虚拟化面板
        chatList = new VirtualizingPanel
        {
            Width = 360,
            Height = 600,
            VirtualizationMode = VirtualizationMode.Recycling,
            ScrollUnit = ScrollUnit.Pixel,
            ItemsSource = messages,
            ItemTemplate = template,
            ItemSize = new SizeF(360, 80)
        };
        
        chatList.GenerateChildren();
    }
    
    private IGameLink<GameDataControl> CreateMessageTemplate()
    {
        var link = new GameLink<GameDataControl, GameDataControlPanel>("MessageTemplate");
        
        var template = new GameDataControlPanel(link)
        {
            Layout = new()
            {
                Width = 360,
                Height = 80,
                Padding = new Thickness(12, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            },
            OnVirtualizationPhase = 
            [
                static (c) =>
                {
                    if (c is Panel panel && c.DataContext is ChatMessage msg)
                    {
                        // 更新发送者
                        if (panel.Children?[0] is Panel bubble &&
                            bubble.Children?[0] is Label senderLabel)
                        {
                            senderLabel.Text = msg.Sender;
                        }
                        
                        // 更新内容
                        if (panel.Children?[0] is Panel bubble2 &&
                            bubble2.Children?[1] is Label contentLabel)
                        {
                            contentLabel.Text = msg.Content;
                        }
                        
                        // 更新时间
                        if (panel.Children?[0] is Panel bubble3 &&
                            bubble3.Children?[2] is Label timeLabel)
                        {
                            timeLabel.Text = msg.Time.ToString("HH:mm");
                        }
                        
                        // 根据是否是自己的消息调整对齐
                        if (panel.Children?[0] is Panel bubble4)
                        {
                            bubble4.Background = new SolidColorBrush(
                                msg.IsMine ? DesignColors.Primary : DesignColors.Surface
                            );
                            panel.HorizontalAlignment = msg.IsMine 
                                ? HorizontalAlignment.Right 
                                : HorizontalAlignment.Left;
                        }
                    }
                }
            ],
            Children = 
            [
                new GameDataControlPanel(new GameLink<GameDataControl, GameDataControlPanel>("MessageBubble"))
                {
                    Layout = new()
                    {
                        FlowOrientation = Orientation.Vertical,
                        Gap = 4,
                        Padding = new Thickness(12, 8),
                        MaxWidth = 250
                    },
                    CornerRadius = 8,
                    Children = 
                    [
                        new GameDataControlLabel(new GameLink<GameDataControl, GameDataControlLabel>("SenderLabel"))
                        {
                            FontSize = 10,
                            Bold = true
                        }.Link,
                        
                        new GameDataControlLabel(new GameLink<GameDataControl, GameDataControlLabel>("ContentLabel"))
                        {
                            FontSize = 14
                        }.Link,
                        
                        new GameDataControlLabel(new GameLink<GameDataControl, GameDataControlLabel>("TimeLabel"))
                        {
                            FontSize = 9,
                            TextColor = Color.Gray
                        }.Link
                    ]
                }.Link
            ]
        };
        
        return template.Link;
    }
    
    public void AddMessage(ChatMessage message)
    {
        messages.Add(message);  // ObservableCollection自动刷新UI
    }
}
#endif
```

---

**相关文档**：
- [流式布局指南](FluentUILayoutGuide.md)
- [指针捕获系统](PointerCaptureSystem.md)
- [GameData系统]()
