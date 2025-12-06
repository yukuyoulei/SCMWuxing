# 指针捕获系统 (Pointer Capture System)

> **适用版本**: WasiCore v1.0+  
> **更新日期**: 2025-01-25  
> **难度级别**: 中级到高级

---

## 📚 目录

- [概述](#概述)
- [什么是指针捕获](#什么是指针捕获)
- [核心API](#核心api)
- [使用场景](#使用场景)
- [完整示例](#完整示例)
- [最佳实践](#最佳实践)
- [常见陷阱](#常见陷阱)
- [高级技巧](#高级技巧)

---

## 概述

**指针捕获（Pointer Capture）** 是一个强大的UI交互机制，允许控件"捕获"指针输入，使其能够持续接收指针移动事件，即使指针移动到控件边界之外。这是实现拖拽、绘图、手势识别等高级交互的基础。

### 核心特性

- 🎯 **持续跟踪**: 捕获后持续接收指针移动事件
- 🔒 **独占控制**: 其他控件不会收到被捕获指针的事件
- 🖱️ **多按钮支持**: 可以同时捕获多个指针按钮
- 📱 **多点触控**: 支持触摸屏的多指操作
- 🔄 **自动释放**: 指针抬起时自动释放捕获

### 典型应用场景

| 场景 | 说明 | 难度 |
|------|------|------|
| 控件拖拽 | 拖动UI元素移动位置 | ⭐⭐☆☆☆ |
| 窗口调整大小 | 拖动边框调整窗口尺寸 | ⭐⭐⭐☆☆ |
| 画板绘图 | 连续绘制线条和形状 | ⭐⭐⭐⭐☆ |
| 摇杆控制 | 虚拟摇杆的拖拽控制 | ⭐⭐⭐☆☆ |
| 手势识别 | 滑动、缩放等手势 | ⭐⭐⭐⭐⭐ |
| 滑块控制 | 进度条、音量控制 | ⭐⭐☆☆☆ |

---

## 什么是指针捕获

### 普通指针事件 vs 捕获指针事件

#### 场景1：没有捕获（普通事件）

```
用户在Button上按下鼠标
    ↓
Button收到 OnPointerPressed 事件
    ↓
用户移动鼠标（离开Button区域）
    ↓
❌ Button不再收到移动事件
    ↓
其他控件可能收到移动事件
```

**问题**: 拖拽时鼠标离开控件后，控件失去追踪。

#### 场景2：使用捕获

```
用户在Button上按下鼠标
    ↓
Button收到 OnPointerPressed 事件
    ↓
Button调用 CapturePointer(e.PointerButtons) 🔒
    ↓
用户移动鼠标（离开Button区域）
    ↓
✅ Button继续收到 OnPointerCapturedMove 事件
    ↓
用户释放鼠标
    ↓
Button调用 ReleasePointer(e.PointerButtons) 🔓
    ↓
捕获结束，恢复正常事件分发
```

**优势**: 无论鼠标在哪里，控件都能持续追踪。

### 可视化示例

```
┌─────────────────────────────────────────┐
│  窗口边界                                │
│                                          │
│  ┌─────────┐                            │
│  │ Button  │ ← 1. 用户在这里按下鼠标    │
│  │ [捕获]  │                            │
│  └─────────┘                            │
│                                          │
│              ●  ← 2. 鼠标移动到这里     │
│                    （已离开Button区域）  │
│                                          │
│  ✅ Button仍然收到 OnPointerCapturedMove │
│                                          │
│                        ●  ← 3. 继续移动 │
│                                          │
│  ✅ Button仍然收到移动事件               │
│                                          │
└─────────────────────────────────────────┘
```

---

## 核心API

### 1. CapturePointer - 捕获指针

```csharp
public void CapturePointer(PointerButtons pointerButtons)
```

**功能**: 捕获指定的指针按钮，使控件持续接收该按钮的移动事件。

**参数**:
- `pointerButtons`: 要捕获的指针按钮（可组合多个）

**常用按钮**:
```csharp
PointerButtons.Button1  // 左键（主按钮）
PointerButtons.Button2  // 中键（滚轮）
PointerButtons.Button3  // 右键
PointerButtons.All      // 所有按钮
```

**示例**:
```csharp
#if CLIENT
control.OnPointerPressed += (sender, e) =>
{
    control.CapturePointer(e.PointerButtons); // 捕获触发事件的按钮
    Game.Logger.LogInformation("指针已捕获");
};
#endif
```

### 2. ReleasePointer - 释放捕获

```csharp
public void ReleasePointer(PointerButtons pointerButtons)
```

**功能**: 释放之前捕获的指针按钮。

**参数**:
- `pointerButtons`: 要释放的指针按钮

**示例**:
```csharp
#if CLIENT
control.OnPointerReleased += (sender, e) =>
{
    control.ReleasePointer(e.PointerButtons); // 释放捕获
    Game.Logger.LogInformation("指针已释放");
};
#endif
```

⚠️ **重要**: 
- 必须与 `CapturePointer` 成对使用
- 不释放捕获会导致其他控件无法响应该按钮
- 指针抬起时会自动释放，但显式释放是最佳实践

### 3. OnPointerCapturedMove - 捕获移动事件

```csharp
public event EventHandler<PointerCapturedMoveEventArgs>? OnPointerCapturedMove
```

**功能**: 当捕获的指针移动时触发的事件。

**事件参数**:
```csharp
public class PointerCapturedMoveEventArgs : PointerEventArgs
{
    // 触发移动的按钮
    public PointerButtons PointerButtons { get; }
    
    // 当前在控件上按下的所有按钮
    public PointerButtons ButtonsOnControl { get; }
    
    // 指针的当前位置
    public UIPosition? PointerPosition { get; }
}
```

**示例**:
```csharp
#if CLIENT
control.OnPointerCapturedMove += (sender, e) =>
{
    var pos = e.PointerPosition;
    if (pos != null)
    {
        Game.Logger.LogInformation($"捕获移动: ({pos.X}, {pos.Y})");
        
        // 更新控件位置
        control.X = pos.X;
        control.Y = pos.Y;
    }
};
#endif
```

---

## 使用场景

### 场景1：基础控件拖拽 ⭐⭐☆☆☆

最简单的拖拽实现。

```csharp
#if CLIENT
public class DraggablePanel : Panel
{
    private Vector2 dragOffset;
    private bool isDragging;
    
    public DraggablePanel()
    {
        // 设置事件
        OnPointerPressed += StartDrag;
        OnPointerReleased += EndDrag;
        OnPointerCapturedMove += OnDrag;
    }
    
    private void StartDrag(object? sender, PointerEventArgs e)
    {
        // 记录拖拽起始偏移
        var pointerPos = e.PointerPosition;
        if (pointerPos != null)
        {
            dragOffset = new Vector2(pointerPos.X - X, pointerPos.Y - Y);
            isDragging = true;
            
            // 🔒 关键：捕获指针
            CapturePointer(e.PointerButtons);
        }
    }
    
    private void OnDrag(object? sender, PointerCapturedMoveEventArgs e)
    {
        if (!isDragging) return;
        
        var pointerPos = e.PointerPosition;
        if (pointerPos != null)
        {
            // 更新位置（考虑拖拽偏移）
            X = pointerPos.X - dragOffset.X;
            Y = pointerPos.Y - dragOffset.Y;
        }
    }
    
    private void EndDrag(object? sender, PointerEventArgs e)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // 🔓 关键：释放指针
        ReleasePointer(e.PointerButtons);
    }
}

// 使用
var draggable = new DraggablePanel()
    .Size(100, 100)
    .Background(Color.Blue)
    .Position(100, 100);
#endif
```

### 场景2：窗口大小调整 ⭐⭐⭐☆☆

拖动边框调整窗口尺寸。

```csharp
#if CLIENT
public class ResizableWindow : Panel
{
    private const float ResizeHandleSize = 10f;
    private bool isResizing;
    private Vector2 resizeStartPos;
    private Vector2 originalSize;
    private ResizeDirection direction;
    
    private enum ResizeDirection
    {
        None,
        Right,
        Bottom,
        BottomRight
    }
    
    public ResizableWindow()
    {
        OnPointerPressed += StartResize;
        OnPointerReleased += EndResize;
        OnPointerCapturedMove += OnResize;
    }
    
    private void StartResize(object? sender, PointerEventArgs e)
    {
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        // 检测点击位置，判断调整方向
        direction = GetResizeDirection(pos);
        if (direction == ResizeDirection.None) return;
        
        isResizing = true;
        resizeStartPos = new Vector2(pos.X, pos.Y);
        originalSize = new Vector2(Width, Height);
        
        // 🔒 捕获指针
        CapturePointer(e.PointerButtons);
    }
    
    private void OnResize(object? sender, PointerCapturedMoveEventArgs e)
    {
        if (!isResizing) return;
        
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        var currentPos = new Vector2(pos.X, pos.Y);
        var delta = currentPos - resizeStartPos;
        
        // 根据方向调整尺寸
        switch (direction)
        {
            case ResizeDirection.Right:
                Width = Math.Max(MinWidth, originalSize.X + delta.X);
                break;
                
            case ResizeDirection.Bottom:
                Height = Math.Max(MinHeight, originalSize.Y + delta.Y);
                break;
                
            case ResizeDirection.BottomRight:
                Width = Math.Max(MinWidth, originalSize.X + delta.X);
                Height = Math.Max(MinHeight, originalSize.Y + delta.Y);
                break;
        }
    }
    
    private void EndResize(object? sender, PointerEventArgs e)
    {
        if (!isResizing) return;
        
        isResizing = false;
        direction = ResizeDirection.None;
        
        // 🔓 释放指针
        ReleasePointer(e.PointerButtons);
    }
    
    private ResizeDirection GetResizeDirection(UIPosition pos)
    {
        // 判断点击位置是否在调整区域
        bool onRightEdge = pos.X >= Width - ResizeHandleSize;
        bool onBottomEdge = pos.Y >= Height - ResizeHandleSize;
        
        if (onRightEdge && onBottomEdge)
            return ResizeDirection.BottomRight;
        if (onRightEdge)
            return ResizeDirection.Right;
        if (onBottomEdge)
            return ResizeDirection.Bottom;
        
        return ResizeDirection.None;
    }
}
#endif
```

### 场景3：画板绘图 ⭐⭐⭐⭐☆

连续绘制线条和形状。

```csharp
#if CLIENT
public class DrawingCanvas : Canvas
{
    private List<Vector2> currentPath = new();
    private bool isDrawing;
    
    public DrawingCanvas()
    {
        Background = new SolidColorBrush(Color.White);
        
        OnPointerPressed += StartDrawing;
        OnPointerReleased += EndDrawing;
        OnPointerCapturedMove += OnDrawing;
    }
    
    private void StartDrawing(object? sender, PointerEventArgs e)
    {
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        isDrawing = true;
        currentPath.Clear();
        currentPath.Add(new Vector2(pos.X, pos.Y));
        
        // 🔒 捕获指针 - 确保即使移出Canvas也能继续绘制
        CapturePointer(e.PointerButtons);
    }
    
    private void OnDrawing(object? sender, PointerCapturedMoveEventArgs e)
    {
        if (!isDrawing) return;
        
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        // 添加路径点
        currentPath.Add(new Vector2(pos.X, pos.Y));
        
        // 绘制当前路径
        DrawCurrentPath();
    }
    
    private void EndDrawing(object? sender, PointerEventArgs e)
    {
        if (!isDrawing) return;
        
        isDrawing = false;
        
        // 保存最终路径
        SavePath(currentPath);
        currentPath.Clear();
        
        // 🔓 释放指针
        ReleasePointer(e.PointerButtons);
    }
    
    private void DrawCurrentPath()
    {
        if (currentPath.Count < 2) return;
        
        // 设置画笔
        StrokePaint = new SolidPaint(Color.Black);
        StrokeSize = 2f;
        
        // 绘制路径
        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            var p1 = currentPath[i];
            var p2 = currentPath[i + 1];
            DrawLine(p1.X, p1.Y, p2.X, p2.Y);
        }
    }
    
    private void SavePath(List<Vector2> path)
    {
        // 保存路径以便重绘
        // 实际应用中可以保存到持久化存储
    }
}
#endif
```

### 场景4：虚拟摇杆 ⭐⭐⭐☆☆

实现虚拟游戏摇杆控制。

```csharp
#if CLIENT
public class SimpleJoystick : Panel
{
    private Panel knob;
    private Vector2 centerPosition;
    private bool isDragging;
    private float radius = 50f;
    
    public Vector2 InputValue { get; private set; }
    public event EventHandler<Vector2>? ValueChanged;
    
    public SimpleJoystick()
    {
        Width = radius * 2;
        Height = radius * 2;
        Background = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128));
        CornerRadius = radius;
        
        // 创建摇杆头
        knob = new Panel
        {
            Width = 30,
            Height = 30,
            Background = new SolidColorBrush(Color.White),
            CornerRadius = 15,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        AddChild(knob);
        
        // 设置事件
        knob.OnPointerPressed += OnKnobPressed;
        knob.OnPointerReleased += OnKnobReleased;
        knob.OnPointerCapturedMove += OnKnobMoved;
        
        centerPosition = new Vector2(radius, radius);
    }
    
    private void OnKnobPressed(object? sender, PointerEventArgs e)
    {
        isDragging = true;
        
        // 🔒 捕获指针 - 允许拖拽到摇杆外部
        knob.CapturePointer(e.PointerButtons);
    }
    
    private void OnKnobMoved(object? sender, PointerCapturedMoveEventArgs e)
    {
        if (!isDragging) return;
        
        var mousePos = e.PointerPosition;
        if (mousePos == null) return;
        
        // 计算相对于摇杆中心的偏移
        var joystickScreenPos = ScreenPosition;
        var offset = new Vector2(
            mousePos.X - joystickScreenPos.X - centerPosition.X,
            mousePos.Y - joystickScreenPos.Y - centerPosition.Y
        );
        
        // 限制在圆形区域内
        var distance = offset.Length();
        if (distance > radius)
        {
            offset = Vector2.Normalize(offset) * radius;
        }
        
        // 更新摇杆头位置
        knob.X = centerPosition.X + offset.X - knob.Width / 2;
        knob.Y = centerPosition.Y + offset.Y - knob.Height / 2;
        
        // 计算输入值（归一化到-1到1）
        InputValue = offset / radius;
        ValueChanged?.Invoke(this, InputValue);
    }
    
    private void OnKnobReleased(object? sender, PointerEventArgs e)
    {
        isDragging = false;
        
        // 🔓 释放指针
        knob.ReleasePointer(e.PointerButtons);
        
        // 回归中心
        ResetToCenter();
    }
    
    private void ResetToCenter()
    {
        knob.X = centerPosition.X - knob.Width / 2;
        knob.Y = centerPosition.Y - knob.Height / 2;
        InputValue = Vector2.Zero;
        ValueChanged?.Invoke(this, InputValue);
    }
}

// 使用示例
var joystick = new SimpleJoystick()
    .Position(50, 50)
    .AddToVisualTree();

joystick.ValueChanged += (sender, value) =>
{
    Game.Logger.LogInformation($"摇杆输入: ({value.X:F2}, {value.Y:F2})");
    // 使用输入值控制游戏角色移动
};
#endif
```

### 场景5：滑块控制 ⭐⭐☆☆☆

音量、进度等滑块控制。

```csharp
#if CLIENT
public class Slider : Panel
{
    private Panel thumb;
    private Panel track;
    private bool isDragging;
    private float _value;
    
    public float Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0f, 1f);
            UpdateThumbPosition();
            ValueChanged?.Invoke(this, _value);
        }
    }
    
    public event EventHandler<float>? ValueChanged;
    
    public Slider()
    {
        Width = 200;
        Height = 20;
        
        // 轨道
        track = new Panel
        {
            Width = 200,
            Height = 4,
            Background = new SolidColorBrush(Color.Gray),
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = 2
        };
        AddChild(track);
        
        // 滑块
        thumb = new Panel
        {
            Width = 20,
            Height = 20,
            Background = new SolidColorBrush(Color.Blue),
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = 10
        };
        AddChild(thumb);
        
        // 事件
        thumb.OnPointerPressed += OnThumbPressed;
        thumb.OnPointerReleased += OnThumbReleased;
        thumb.OnPointerCapturedMove += OnThumbMoved;
        
        // 轨道点击
        track.OnPointerPressed += OnTrackClicked;
        
        // 初始化
        Value = 0.5f;
    }
    
    private void OnThumbPressed(object? sender, PointerEventArgs e)
    {
        isDragging = true;
        
        // 🔒 捕获指针 - 允许拖拽到滑块外部
        thumb.CapturePointer(e.PointerButtons);
    }
    
    private void OnThumbMoved(object? sender, PointerCapturedMoveEventArgs e)
    {
        if (!isDragging) return;
        
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        // 计算相对于轨道的位置
        var trackScreenPos = track.ScreenPosition;
        var relativeX = pos.X - trackScreenPos.X;
        
        // 计算值（0到1）
        Value = Math.Clamp(relativeX / track.Width, 0f, 1f);
    }
    
    private void OnThumbReleased(object? sender, PointerEventArgs e)
    {
        isDragging = false;
        
        // 🔓 释放指针
        thumb.ReleasePointer(e.PointerButtons);
    }
    
    private void OnTrackClicked(object? sender, PointerEventArgs e)
    {
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        // 点击轨道直接跳转
        var trackScreenPos = track.ScreenPosition;
        var relativeX = pos.X - trackScreenPos.X;
        Value = Math.Clamp(relativeX / track.Width, 0f, 1f);
    }
    
    private void UpdateThumbPosition()
    {
        thumb.X = track.X + (track.Width - thumb.Width) * _value;
    }
}

// 使用示例
var volumeSlider = new Slider()
    .Position(100, 100)
    .AddToVisualTree();

volumeSlider.ValueChanged += (sender, value) =>
{
    Game.Logger.LogInformation($"音量: {(int)(value * 100)}%");
    // 设置实际音量
};
#endif
```

---

## 完整示例

### 综合示例：可拖拽可调整大小的窗口

```csharp
#if CLIENT
using GameUI.Control.Primitive;
using GameUI.Control.Extensions;
using static GameUI.Control.Extensions.UI;
using System.Numerics;

public class DraggableResizableWindow : Panel
{
    private const float ResizeHandleSize = 10f;
    private const float MinWindowSize = 100f;
    
    // 标题栏
    private Panel titleBar;
    private Label titleLabel;
    
    // 状态
    private bool isDragging;
    private bool isResizing;
    private Vector2 dragOffset;
    private Vector2 resizeStartPos;
    private Vector2 originalSize;
    private ResizeDirection resizeDirection;
    
    private enum ResizeDirection
    {
        None,
        Right,
        Bottom,
        BottomRight
    }
    
    public string Title
    {
        get => titleLabel.Text;
        set => titleLabel.Text = value;
    }
    
    public DraggableResizableWindow()
    {
        // 窗口样式
        Width = 400;
        Height = 300;
        Background = new SolidColorBrush(Color.White);
        CornerRadius = 8;
        
        // 创建标题栏
        CreateTitleBar();
        
        // 设置调整大小事件
        OnPointerPressed += StartResize;
        OnPointerReleased += EndResize;
        OnPointerCapturedMove += OnResize;
    }
    
    private void CreateTitleBar()
    {
        titleBar = new Panel
        {
            Height = 40,
            Background = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            CornerRadius = 8
        };
        AddChild(titleBar);
        
        titleLabel = new Label
        {
            Text = "窗口标题",
            FontSize = 16,
            TextColor = Color.White,
            Bold = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleBar.AddChild(titleLabel);
        
        // 标题栏拖拽事件
        titleBar.OnPointerPressed += StartDrag;
        titleBar.OnPointerReleased += EndDrag;
        titleBar.OnPointerCapturedMove += OnDrag;
    }
    
    // ===== 拖拽相关 =====
    
    private void StartDrag(object? sender, PointerEventArgs e)
    {
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        dragOffset = new Vector2(pos.X - X, pos.Y - Y);
        isDragging = true;
        
        // 🔒 捕获指针 - 拖拽标题栏
        titleBar.CapturePointer(e.PointerButtons);
    }
    
    private void OnDrag(object? sender, PointerCapturedMoveEventArgs e)
    {
        if (!isDragging) return;
        
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        X = pos.X - dragOffset.X;
        Y = pos.Y - dragOffset.Y;
    }
    
    private void EndDrag(object? sender, PointerEventArgs e)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // 🔓 释放指针
        titleBar.ReleasePointer(e.PointerButtons);
    }
    
    // ===== 调整大小相关 =====
    
    private void StartResize(object? sender, PointerEventArgs e)
    {
        if (isDragging) return; // 拖拽时不调整大小
        
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        resizeDirection = GetResizeDirection(pos);
        if (resizeDirection == ResizeDirection.None) return;
        
        isResizing = true;
        resizeStartPos = new Vector2(pos.X, pos.Y);
        originalSize = new Vector2(Width, Height);
        
        // 🔒 捕获指针 - 调整大小
        CapturePointer(e.PointerButtons);
    }
    
    private void OnResize(object? sender, PointerCapturedMoveEventArgs e)
    {
        if (!isResizing) return;
        
        var pos = e.PointerPosition;
        if (pos == null) return;
        
        var currentPos = new Vector2(pos.X, pos.Y);
        var delta = currentPos - resizeStartPos;
        
        switch (resizeDirection)
        {
            case ResizeDirection.Right:
                Width = Math.Max(MinWindowSize, originalSize.X + delta.X);
                break;
                
            case ResizeDirection.Bottom:
                Height = Math.Max(MinWindowSize, originalSize.Y + delta.Y);
                break;
                
            case ResizeDirection.BottomRight:
                Width = Math.Max(MinWindowSize, originalSize.X + delta.X);
                Height = Math.Max(MinWindowSize, originalSize.Y + delta.Y);
                break;
        }
    }
    
    private void EndResize(object? sender, PointerEventArgs e)
    {
        if (!isResizing) return;
        
        isResizing = false;
        resizeDirection = ResizeDirection.None;
        
        // 🔓 释放指针
        ReleasePointer(e.PointerButtons);
    }
    
    private ResizeDirection GetResizeDirection(UIPosition pos)
    {
        // 转换为相对于窗口的坐标
        var screenPos = ScreenPosition;
        var relativeX = pos.X - screenPos.X;
        var relativeY = pos.Y - screenPos.Y;
        
        bool onRightEdge = relativeX >= Width - ResizeHandleSize && relativeX <= Width;
        bool onBottomEdge = relativeY >= Height - ResizeHandleSize && relativeY <= Height;
        
        if (onRightEdge && onBottomEdge)
            return ResizeDirection.BottomRight;
        if (onRightEdge)
            return ResizeDirection.Right;
        if (onBottomEdge)
            return ResizeDirection.Bottom;
        
        return ResizeDirection.None;
    }
    
    // 添加内容控件
    public void SetContent(Control content)
    {
        content.Margin = new Thickness(10, 50, 10, 10); // 为标题栏留空间
        AddChild(content);
    }
}

// ===== 使用示例 =====

public class WindowDemo
{
    public static void CreateDemo()
    {
        var window = new DraggableResizableWindow
        {
            Title = "可拖拽窗口",
            X = 100,
            Y = 100
        };
        
        // 添加窗口内容
        var content = VStack(20,
            Title("窗口内容", 20),
            Body("这是一个可拖拽、可调整大小的窗口示例。"),
            Body("• 拖拽标题栏移动窗口"),
            Body("• 拖拽右边缘或底边调整大小"),
            Body("• 拖拽右下角同时调整宽高"),
            
            Spacer(),
            
            HStack(10,
                Primary("确定"),
                Secondary("取消")
            ).Center()
        ).Padding(20);
        
        window.SetContent(content);
        window.AddToVisualTree();
    }
}
#endif
```

---

## 最佳实践

### ✅ 推荐做法

#### 1. 始终成对使用捕获和释放

```csharp
#if CLIENT
// ✅ 正确：配对使用
control.OnPointerPressed += (s, e) =>
{
    control.CapturePointer(e.PointerButtons); // 捕获
};

control.OnPointerReleased += (s, e) =>
{
    control.ReleasePointer(e.PointerButtons); // 释放
};
#endif
```

#### 2. 保存捕获时的状态

```csharp
#if CLIENT
// ✅ 正确：保存初始状态
private Vector2 dragStartPos;
private Vector2 controlStartPos;

control.OnPointerPressed += (s, e) =>
{
    var pos = e.PointerPosition;
    if (pos != null)
    {
        dragStartPos = new Vector2(pos.X, pos.Y);
        controlStartPos = new Vector2(control.X, control.Y);
        control.CapturePointer(e.PointerButtons);
    }
};
#endif
```

#### 3. 检查指针位置是否有效

```csharp
#if CLIENT
// ✅ 正确：检查null
control.OnPointerCapturedMove += (s, e) =>
{
    var pos = e.PointerPosition;
    if (pos == null) return; // 安全检查
    
    // 使用位置
    control.X = pos.X;
    control.Y = pos.Y;
};
#endif
```

#### 4. 使用状态标志控制行为

```csharp
#if CLIENT
// ✅ 正确：使用标志
private bool isDragging;

control.OnPointerPressed += (s, e) =>
{
    isDragging = true;
    control.CapturePointer(e.PointerButtons);
};

control.OnPointerCapturedMove += (s, e) =>
{
    if (!isDragging) return; // 检查状态
    
    // 拖拽逻辑
};

control.OnPointerReleased += (s, e) =>
{
    isDragging = false;
    control.ReleasePointer(e.PointerButtons);
};
#endif
```

#### 5. 合理使用PointerButtons

```csharp
#if CLIENT
// ✅ 正确：捕获特定按钮
control.OnPointerPressed += (s, e) =>
{
    // 只处理左键
    if (e.PointerButtons == PointerButtons.Button1)
    {
        control.CapturePointer(e.PointerButtons);
    }
};

// ✅ 正确：支持多按钮
control.OnPointerPressed += (s, e) =>
{
    // 捕获触发事件的按钮（自动支持多按钮）
    control.CapturePointer(e.PointerButtons);
};
#endif
```

---

## 常见陷阱

### ❌ 陷阱1：忘记释放捕获

**问题**：指针被永久捕获，其他控件无法响应。

```csharp
#if CLIENT
// ❌ 错误：忘记释放
control.OnPointerPressed += (s, e) =>
{
    control.CapturePointer(e.PointerButtons);
    // 忘记在某处调用 ReleasePointer
};

// 结果：鼠标被"卡住"，其他控件无法点击
#endif
```

**解决方案**：
```csharp
#if CLIENT
// ✅ 正确：确保释放
control.OnPointerPressed += (s, e) =>
{
    control.CapturePointer(e.PointerButtons);
};

control.OnPointerReleased += (s, e) =>
{
    control.ReleasePointer(e.PointerButtons); // 始终释放
};

// 可选：添加安全机制
control.OnPointerExited += (s, e) =>
{
    if (isCaptured)
    {
        control.ReleasePointer(capturedButtons);
    }
};
#endif
```

### ❌ 陷阱2：在虚拟化列表中捕获

**问题**：虚拟化列表项被回收后，捕获状态丢失。

```csharp
#if CLIENT
// ❌ 错误：在虚拟化列表项中直接捕获
var panel = new VirtualizingPanel
{
    ItemTemplate = (item) =>
    {
        var itemControl = CreateItem(item);
        
        itemControl.OnPointerPressed += (s, e) =>
        {
            itemControl.CapturePointer(e.PointerButtons);
            // 问题：拖拽到远处时，itemControl可能被回收
        };
        
        return itemControl;
    }
};
#endif
```

**解决方案**：转移到持久父级（参见虚拟化面板文档的限制章节）。

### ❌ 陷阱3：不检查null指针位置

**问题**：某些情况下`PointerPosition`可能为null。

```csharp
#if CLIENT
// ❌ 错误：不检查null
control.OnPointerCapturedMove += (s, e) =>
{
    control.X = e.PointerPosition.X; // NullReferenceException!
    control.Y = e.PointerPosition.Y;
};
#endif
```

**解决方案**：
```csharp
#if CLIENT
// ✅ 正确：检查null
control.OnPointerCapturedMove += (s, e) =>
{
    var pos = e.PointerPosition;
    if (pos != null)
    {
        control.X = pos.X;
        control.Y = pos.Y;
    }
};
#endif
```

### ❌ 陷阱4：混淆普通移动和捕获移动

**问题**：使用错误的事件类型。

```csharp
#if CLIENT
// ❌ 错误：期望在捕获后收到OnPointerMoved
control.OnPointerPressed += (s, e) =>
{
    control.CapturePointer(e.PointerButtons);
};

control.OnPointerMoved += (s, e) => // ❌ 捕获后不会触发此事件
{
    // 这个事件不会被触发！
};
#endif
```

**解决方案**：
```csharp
#if CLIENT
// ✅ 正确：使用OnPointerCapturedMove
control.OnPointerPressed += (s, e) =>
{
    control.CapturePointer(e.PointerButtons);
};

control.OnPointerCapturedMove += (s, e) => // ✅ 正确的事件
{
    // 这个事件会被触发
};
#endif
```

### ❌ 陷阱5：捕获错误的按钮

**问题**：捕获的按钮与释放的按钮不匹配。

```csharp
#if CLIENT
// ❌ 错误：按钮不匹配
private PointerButtons capturedButtons;

control.OnPointerPressed += (s, e) =>
{
    control.CapturePointer(PointerButtons.Button1); // 总是捕获Button1
    capturedButtons = PointerButtons.Button1;
};

control.OnPointerReleased += (s, e) =>
{
    control.ReleasePointer(e.PointerButtons); // 可能是Button3!
    // 如果用户按了Button3，Button1不会被释放
};
#endif
```

**解决方案**：
```csharp
#if CLIENT
// ✅ 正确：保持一致
private PointerButtons capturedButtons;

control.OnPointerPressed += (s, e) =>
{
    capturedButtons = e.PointerButtons;
    control.CapturePointer(capturedButtons);
};

control.OnPointerReleased += (s, e) =>
{
    control.ReleasePointer(capturedButtons); // 释放相同的按钮
};
#endif
```

---

## 高级技巧

### 技巧1：多按钮同时捕获

支持同时拖拽和其他操作。

```csharp
#if CLIENT
// 左键拖拽，右键旋转
private Dictionary<PointerButtons, Action<UIPosition>> capturedActions = new();

control.OnPointerPressed += (s, e) =>
{
    if (e.PointerButtons == PointerButtons.Button1)
    {
        capturedActions[e.PointerButtons] = DragAction;
        control.CapturePointer(e.PointerButtons);
    }
    else if (e.PointerButtons == PointerButtons.Button3)
    {
        capturedActions[e.PointerButtons] = RotateAction;
        control.CapturePointer(e.PointerButtons);
    }
};

control.OnPointerCapturedMove += (s, e) =>
{
    if (capturedActions.TryGetValue(e.PointerButtons, out var action))
    {
        var pos = e.PointerPosition;
        if (pos != null)
        {
            action(pos);
        }
    }
};

control.OnPointerReleased += (s, e) =>
{
    if (capturedActions.Remove(e.PointerButtons))
    {
        control.ReleasePointer(e.PointerButtons);
    }
};

void DragAction(UIPosition pos)
{
    // 拖拽逻辑
    control.X = pos.X;
    control.Y = pos.Y;
}

void RotateAction(UIPosition pos)
{
    // 旋转逻辑
    var angle = CalculateAngle(pos);
    control.Rotation = angle;
}
#endif
```

### 技巧2：限制拖拽范围

确保控件不会被拖出边界。

```csharp
#if CLIENT
private RectangleF dragBounds;

public void SetDragBounds(float x, float y, float width, float height)
{
    dragBounds = new RectangleF(x, y, width, height);
}

control.OnPointerCapturedMove += (s, e) =>
{
    var pos = e.PointerPosition;
    if (pos == null) return;
    
    // 限制在边界内
    control.X = Math.Clamp(pos.X, dragBounds.Left, dragBounds.Right - control.Width);
    control.Y = Math.Clamp(pos.Y, dragBounds.Top, dragBounds.Bottom - control.Height);
};
#endif
```

### 技巧3：捕获延迟（防止误触）

小范围移动不触发拖拽。

```csharp
#if CLIENT
private const float DragThreshold = 5f; // 5像素阈值
private Vector2 pressPosition;
private bool isDragStarted;

control.OnPointerPressed += (s, e) =>
{
    var pos = e.PointerPosition;
    if (pos != null)
    {
        pressPosition = new Vector2(pos.X, pos.Y);
        isDragStarted = false;
        control.CapturePointer(e.PointerButtons); // 先捕获
    }
};

control.OnPointerCapturedMove += (s, e) =>
{
    var pos = e.PointerPosition;
    if (pos == null) return;
    
    if (!isDragStarted)
    {
        // 检查是否超过阈值
        var currentPos = new Vector2(pos.X, pos.Y);
        var distance = Vector2.Distance(pressPosition, currentPos);
        
        if (distance < DragThreshold)
        {
            return; // 还没开始拖拽
        }
        
        isDragStarted = true; // 开始拖拽
    }
    
    // 正常拖拽逻辑
    control.X = pos.X;
    control.Y = pos.Y;
};
#endif
```

### 技巧4：捕获期间的视觉反馈

```csharp
#if CLIENT
private SolidColorBrush originalBackground;

control.OnPointerPressed += (s, e) =>
{
    // 保存原始背景
    originalBackground = control.Background as SolidColorBrush;
    
    // 改变外观表示捕获状态
    control.Background = new SolidColorBrush(Color.LightBlue);
    control.Opacity = 0.8f;
    control.Layer = 999; // 置于最前
    
    control.CapturePointer(e.PointerButtons);
};

control.OnPointerReleased += (s, e) =>
{
    // 恢复外观
    control.Background = originalBackground;
    control.Opacity = 1.0f;
    control.Layer = 0;
    
    control.ReleasePointer(e.PointerButtons);
};
#endif
```

---

## 相关文档

- [虚拟化列表系统](VirtualizingPanelSystem.md) - 虚拟化列表中的指针捕获注意事项
- [TouchBehavior系统](TouchBehavior.md) - 触摸行为与指针捕获的配合
- [Canvas绘图系统](CanvasDrawingSystem.md) - 在Canvas中使用指针捕获
- [UI设计规范](../guides/UIDesignStandards.md) - 交互设计标准

---

## 总结

**指针捕获系统**是实现高级UI交互的关键机制，掌握以下要点：

### 核心要点 ✅
1. **配对使用**: `CapturePointer` 和 `ReleasePointer` 必须成对
2. **正确事件**: 捕获后使用 `OnPointerCapturedMove` 而不是 `OnPointerMoved`
3. **null检查**: 始终检查 `PointerPosition` 是否为null
4. **状态管理**: 使用标志追踪捕获状态
5. **虚拟化注意**: 在虚拟化列表中使用需要特殊处理

### 典型应用 🎯
- ✅ 控件拖拽
- ✅ 窗口调整大小
- ✅ 画板绘图
- ✅ 虚拟摇杆
- ✅ 滑块控制
- ✅ 手势识别

### 最佳实践 💡
- 保存捕获时的初始状态
- 提供视觉反馈
- 限制拖拽范围
- 实现拖拽阈值
- 及时释放捕获

遵循这些原则，你就能构建流畅、可靠的UI交互体验！

---

*本文档持续更新中。如有问题或建议，欢迎反馈。*

