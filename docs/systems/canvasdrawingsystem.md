# Canvas 绘图系统完整指南

> **适用版本**: WasiCore v1.0+  
> **更新日期**: 2025-01-25  
> **难度级别**: 中级到高级

---

## 📚 目录

- [概述](#概述)
- [Canvas基础](#canvas基础)
- [坐标系统](#坐标系统)
- [绘制属性](#绘制属性)
- [基础图形绘制](#基础图形绘制)
- [路径绘制](#路径绘制)
- [图像绘制](#图像绘制)
- [变换操作](#变换操作)
- [状态管理](#状态管理)
- [Paint系统](#paint系统)
- [高级特性](#高级特性)
- [实战示例](#实战示例)
- [性能优化](#性能优化)
- [最佳实践](#最佳实践)

---

## 概述

**Canvas** 是WasiCore框架中的强大2D绘图控件，提供了丰富的绘图API，可以绘制从简单图形到复杂路径的各种内容。它基于NanoVG渲染引擎，性能优异，适合实时游戏UI和数据可视化。

### 核心特性

- 🎨 **丰富图形**: 线条、圆形、矩形、椭圆、多边形、贝塞尔曲线
- 🖼️ **图像渲染**: 支持图像绘制、裁剪、缩放
- 🎭 **变换系统**: 平移、旋转、缩放、矩阵变换
- 🌈 **渐变填充**: 线性渐变、径向渐变、盒式渐变
- 📏 **路径系统**: 完整的PathF路径构建和渲染
- 🔄 **状态管理**: SaveState/RestoreState状态栈
- 🎯 **精确控制**: 线条样式、端点、连接点的完整控制
- 🎪 **混合模式**: 多种混合模式实现特效

### 典型应用场景

| 场景 | 复杂度 | 说明 |
|------|--------|------|
| UI装饰 | ⭐⭐☆☆☆ | 绘制边框、分隔线、装饰图案 |
| 图表绘制 | ⭐⭐⭐☆☆ | 折线图、柱状图、饼图 |
| 小地图 | ⭐⭐⭐☆☆ | 游戏中的迷你地图 |
| 绘图板 | ⭐⭐⭐⭐☆ | 手绘应用、签名板 |
| 数据可视化 | ⭐⭐⭐⭐☆ | 复杂图表、热力图 |
| 自定义控件 | ⭐⭐⭐⭐⭐ | 完全自定义的UI控件 |

---

## Canvas基础

### 创建Canvas

```csharp
#if CLIENT
using GameUI.Control.Primitive;
using GameUI.Graphics;
using System.Drawing;

// 方式1：简单创建
var canvas = new Canvas()
    .Size(400, 300)
    .Background(Color.White);

// 方式2：使用流式API
var canvas = new Canvas()
    .Size(400, 300)
    .Background(Color.FromArgb(255, 240, 240, 240))
    .Position(100, 100)
    .AddToVisualTree();
#endif
```

### Canvas生命周期

```csharp
#if CLIENT
public class MyCanvas : Canvas
{
    public MyCanvas()
    {
        Width = 400;
        Height = 300;
        
        // Canvas创建时初始化
        InitializeDrawing();
    }
    
    private void InitializeDrawing()
    {
        // 在这里进行初始绘制
        DrawContent();
    }
    
    private void DrawContent()
    {
        // 清除画布
        ResetState();
        
        // 设置绘制属性
        StrokePaint = new SolidPaint(Color.Black);
        StrokeWidth = 2f;
        
        // 绘制图形
        DrawCircle(200, 150, 50);
    }
    
    // 可选：实现动态更新
    public void UpdateDrawing()
    {
        DrawContent();
    }
}
#endif
```

---

## 坐标系统

### 两种坐标模式

Canvas支持两种坐标模式，通过`CoordinateMode`属性设置：

#### 1. DesignResolution模式（默认，推荐）⭐⭐⭐⭐⭐

```csharp
#if CLIENT
canvas.CoordinateMode = CanvasCoordinateMode.DesignResolution;

// 特点：
// - 坐标系统与UI其他控件一致
// - 自动处理设备像素比例（DPI）
// - AI友好，容易理解和使用
// - 在不同设备上保持一致的视觉效果

// 示例：
canvas.DrawCircle(100, 100, 50); // 在设计坐标(100, 100)绘制
// 在高DPI设备上自动缩放，保持视觉大小一致
#endif
```

**优势**：
- ✅ 与UI控件坐标系统一致
- ✅ 跨设备一致性好
- ✅ 不需要考虑像素比例
- ✅ 推荐用于UI装饰、图表等

#### 2. CanvasResolution模式（像素精确）

```csharp
#if CLIENT
canvas.CoordinateMode = CanvasCoordinateMode.CanvasResolution;

// 特点：
// - 1:1像素映射
// - 精确的像素级控制
// - 需要手动处理设备像素比
// - 适合像素艺术、精密绘制

// 示例：
canvas.Resolution = new SizeF(800, 600);
canvas.DrawCircle(400, 300, 100); // 在第400列第300行绘制
#endif
```

**适用场景**：
- 像素艺术
- 精确的像素级绘制
- 与Canvas分辨率直接对应的绘制

### 坐标系统原点

```
 (0, 0)
   ┌───────────────────► X轴
   │
   │
   │
   │
   │
   ▼
  Y轴

Canvas坐标系统：
- 原点在左上角
- X轴向右为正
- Y轴向下为正
```

### 分辨率设置

```csharp
#if CLIENT
// 自动分辨率（默认）
canvas.AutoUpdateResolutionOnResize = true;
// 分辨率会自动根据控件大小调整

// 手动分辨率
canvas.AutoUpdateResolutionOnResize = false;
canvas.Resolution = new SizeF(1920, 1080);
// 固定分辨率，适合精确绘制需求
#endif
```

---

## 绘制属性

### Paint系统概览

Canvas使用Paint对象定义绘制的颜色和样式：

```csharp
#if CLIENT
// FillPaint - 用于填充（实心图形）
canvas.FillPaint = new SolidPaint(Color.Blue);
canvas.FillRectangle(10, 10, 100, 50);

// StrokePaint - 用于描边（轮廓线条）
canvas.StrokePaint = new SolidPaint(Color.Red);
canvas.DrawRectangle(10, 10, 100, 50);
#endif
```

### 基础属性完整列表

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `FillPaint` | `Paint` | 白色 | 填充画笔 |
| `StrokePaint` | `Paint` | 黑色 | 描边画笔 |
| `StrokeWidth` | `float` | 1.0 | 线条宽度（像素） |
| `LineCap` | `LineCap` | Butt | 线条端点样式 |
| `LineJoin` | `LineJoin` | Miter | 线条连接样式 |
| `MiterLimit` | `float` | 10.0 | 斜接限制 |
| `Alpha` | `float` | 1.0 | 全局透明度 (0-1) |
| `BlendMode` | `BlendMode` | SourceOver | 混合模式 |

### 线条样式详解

#### StrokeWidth - 线条宽度

```csharp
#if CLIENT
canvas.StrokeWidth = 1.0f;  // 细线
canvas.DrawLine(10, 10, 100, 10);

canvas.StrokeWidth = 5.0f;  // 粗线
canvas.DrawLine(10, 30, 100, 30);

canvas.StrokeWidth = 10.0f; // 很粗的线
canvas.DrawLine(10, 60, 100, 60);
#endif
```

#### LineCap - 线条端点

```csharp
#if CLIENT
canvas.StrokeWidth = 10f;

// Butt - 平头端点（默认）
canvas.LineCap = LineCap.Butt;
canvas.DrawLine(10, 10, 100, 10);

// Round - 圆形端点
canvas.LineCap = LineCap.Round;
canvas.DrawLine(10, 30, 100, 30);

// Square - 方形端点（延伸）
canvas.LineCap = LineCap.Square;
canvas.DrawLine(10, 50, 100, 50);
#endif
```

可视化效果：
```
Butt:   |-----------|
Round:  ●-----------●
Square: □-----------□
```

#### LineJoin - 线条连接

```csharp
#if CLIENT
canvas.StrokeWidth = 8f;

// Miter - 斜接连接（默认）
canvas.LineJoin = LineJoin.Miter;
canvas.DrawTriangle(10, 10, 50, 50, 90, 10);

// Round - 圆形连接
canvas.LineJoin = LineJoin.Round;
canvas.DrawTriangle(110, 10, 150, 50, 190, 10);

// Bevel - 斜切连接
canvas.LineJoin = LineJoin.Bevel;
canvas.DrawTriangle(210, 10, 250, 50, 290, 10);
#endif
```

可视化效果：
```
Miter:  ∧    (尖角)
Round:  ⌒    (圆角)
Bevel:  ‾    (平角)
```

### Alpha - 全局透明度

```csharp
#if CLIENT
canvas.Alpha = 1.0f;  // 完全不透明
canvas.FillCircle(50, 50, 30);

canvas.Alpha = 0.5f;  // 半透明
canvas.FillCircle(100, 50, 30);

canvas.Alpha = 0.2f;  // 几乎透明
canvas.FillCircle(150, 50, 30);
#endif
```

---

## 基础图形绘制

### 线条

```csharp
#if CLIENT
// 直线
canvas.StrokePaint = new SolidPaint(Color.Black);
canvas.StrokeWidth = 2f;
canvas.DrawLine(10, 10, 200, 10);

// 多条线段（手动绘制折线）
canvas.DrawLine(10, 30, 50, 30);
canvas.DrawLine(50, 30, 50, 50);
canvas.DrawLine(50, 50, 90, 50);
#endif
```

### 矩形

```csharp
#if CLIENT
// 矩形轮廓
canvas.StrokePaint = new SolidPaint(Color.Blue);
canvas.StrokeWidth = 2f;
canvas.DrawRectangle(10, 10, 100, 50);

// 填充矩形
canvas.FillPaint = new SolidPaint(Color.LightBlue);
canvas.FillRectangle(130, 10, 100, 50);

// 正方形（便捷方法）
canvas.DrawSquare(250, 10, 50);    // 轮廓
canvas.FillSquare(320, 10, 50);    // 填充
#endif
```

### 圆形

```csharp
#if CLIENT
// 圆形轮廓
canvas.StrokePaint = new SolidPaint(Color.Red);
canvas.StrokeWidth = 2f;
canvas.DrawCircle(60, 60, 30);

// 填充圆形
canvas.FillPaint = new SolidPaint(Color.LightCoral);
canvas.FillCircle(150, 60, 30);

// 使用PointF
canvas.DrawCircle(new PointF(240, 60), 30);

// 圆弧
canvas.DrawCircle(new PointF(330, 60), 30, 0, 180, true);
// 参数：中心点, 半径, 起始角度, 结束角度, 顺时针
#endif
```

### 椭圆

```csharp
#if CLIENT
// 椭圆轮廓
canvas.StrokePaint = new SolidPaint(Color.Green);
canvas.StrokeWidth = 2f;
canvas.DrawEllipse(60, 60, 50, 30); // 中心X, 中心Y, 半径X, 半径Y

// 填充椭圆
canvas.FillPaint = new SolidPaint(Color.LightGreen);
canvas.FillEllipse(180, 60, 50, 30);

// 使用PointF
canvas.FillEllipse(new PointF(300, 60), 50, 30);
#endif
```

### 三角形

```csharp
#if CLIENT
// 三角形轮廓
canvas.StrokePaint = new SolidPaint(Color.Purple);
canvas.StrokeWidth = 2f;
canvas.DrawTriangle(
    50, 10,  // 顶点1
    10, 70,  // 顶点2
    90, 70   // 顶点3
);

// 填充三角形
canvas.FillPaint = new SolidPaint(Color.Plum);
canvas.FillTriangle(150, 10, 110, 70, 190, 70);
#endif
```

### 同时绘制轮廓和填充

```csharp
#if CLIENT
// 先填充后描边（推荐顺序）
canvas.FillPaint = new SolidPaint(Color.LightBlue);
canvas.StrokePaint = new SolidPaint(Color.DarkBlue);
canvas.StrokeWidth = 3f;

// 绘制带边框的填充矩形
canvas.SaveState();
canvas.FillRectangle(10, 10, 100, 50);
canvas.DrawRectangle(10, 10, 100, 50);
canvas.RestoreState();
#endif
```

---

## 路径绘制

### PathF简介

`PathF`是一个强大的路径构建类，支持复杂的图形绘制。

```csharp
#if CLIENT
using GameUI.Graphics;

// 创建路径
var path = new PathF();

// 构建路径
path.MoveTo(10, 10);      // 移动到起点
path.LineTo(100, 10);     // 直线到
path.LineTo(100, 100);    // 直线到
path.LineTo(10, 100);     // 直线到
path.Close();             // 闭合路径

// 绘制路径
canvas.StrokePaint = new SolidPaint(Color.Black);
canvas.StrokeWidth = 2f;
canvas.DrawPath(path);    // 绘制轮廓

// 填充路径
canvas.FillPaint = new SolidPaint(Color.LightBlue);
canvas.FillPath(path);    // 填充
#endif
```

### PathF基础操作

```csharp
#if CLIENT
var path = new PathF();

// MoveTo - 移动画笔（不绘制）
path.MoveTo(x, y);

// LineTo - 绘制直线
path.LineTo(x, y);

// QuadTo - 二次贝塞尔曲线
path.QuadTo(controlX, controlY, endX, endY);

// CurveTo - 三次贝塞尔曲线
path.CurveTo(control1X, control1Y, control2X, control2Y, endX, endY);

// Close - 闭合路径
path.Close();
#endif
```

### 绘制复杂路径

#### 五角星

```csharp
#if CLIENT
public PathF CreateStarPath(PointF center, float outerRadius, float innerRadius)
{
    var path = new PathF();
    var angleStep = 36f; // 360 / 10 = 36度
    
    for (int i = 0; i < 10; i++)
    {
        var angle = i * angleStep - 90; // -90度从顶部开始
        var radius = (i % 2 == 0) ? outerRadius : innerRadius;
        
        var x = center.X + radius * MathF.Cos(angle * MathF.PI / 180);
        var y = center.Y + radius * MathF.Sin(angle * MathF.PI / 180);
        
        if (i == 0)
            path.MoveTo(x, y);
        else
            path.LineTo(x, y);
    }
    
    path.Close();
    return path;
}

// 使用
var starPath = CreateStarPath(new PointF(100, 100), 50, 20);
canvas.FillPaint = new SolidPaint(Color.Gold);
canvas.FillPath(starPath);
canvas.StrokePaint = new SolidPaint(Color.DarkGoldenrod);
canvas.DrawPath(starPath);
#endif
```

#### 平滑曲线

```csharp
#if CLIENT
// 使用贝塞尔曲线绘制平滑曲线
var path = new PathF();
path.MoveTo(10, 100);

// 二次贝塞尔曲线
path.QuadTo(50, 50, 100, 100);  // 控制点, 终点

// 三次贝塞尔曲线
path.CurveTo(150, 150, 200, 50, 250, 100);
// 控制点1, 控制点2, 终点

canvas.StrokePaint = new SolidPaint(Color.Blue);
canvas.StrokeWidth = 3f;
canvas.DrawPath(path);
#endif
```

### 预定义形状路径

PathF提供了便捷的形状添加方法：

```csharp
#if CLIENT
var path = new PathF();

// 添加矩形
path.AppendRectangle(10, 10, 100, 50);

// 添加圆角矩形
path.AppendRoundedRectangle(10, 70, 100, 50, 10);

// 添加圆形
path.AppendCircle(200, 50, 30);

// 添加椭圆
path.AppendEllipse(200, 130, 40, 25);

canvas.FillPaint = new SolidPaint(Color.LightGray);
canvas.FillPath(path);
#endif
```

### 缠绕规则 (Winding Mode)

控制复杂路径的填充方式：

```csharp
#if CLIENT
// NonZero - 非零缠绕规则（默认）
canvas.DefaultPathWinding = WindingMode.NonZero;
canvas.FillPath(path);
// 适用于大多数普通形状

// EvenOdd - 奇偶规则
canvas.DefaultPathWinding = WindingMode.EvenOdd;
canvas.FillPath(path);
// 创建镂空效果，路径交叉处会"挖空"

// 示例：绘制带孔的形状
var pathWithHole = new PathF();
pathWithHole.AppendRectangle(10, 10, 200, 200); // 外部矩形
pathWithHole.AppendCircle(110, 110, 50);        // 内部圆形（将被挖空）

canvas.FillPaint = new SolidPaint(Color.Blue);
canvas.FillPath(pathWithHole, WindingMode.EvenOdd);
// 结果：蓝色矩形中间有圆形孔
#endif
```

---

## 图像绘制

### 基础图像绘制

```csharp
#if CLIENT
using GameCore.ResourceType;

// 加载图像
var image = new Image("path/to/image.png");

// 方式1：原始尺寸绘制
canvas.DrawImage(image, 10, 10);

// 方式2：指定尺寸绘制（缩放）
canvas.DrawImage(image, 10, 10, 200, 150);
// 参数：图像, X, Y, 宽度, 高度
#endif
```

### 图像裁剪

```csharp
#if CLIENT
// 从精灵表中裁剪
var spriteSheet = new Image("sprites.png");

// DrawImage with clipping
canvas.DrawImage(
    spriteSheet,
    32, 0, 32, 32,    // 源区域：X, Y, 宽, 高
    10, 10, 64, 64    // 目标区域：X, Y, 宽, 高
);
// 从精灵表(32,0)位置裁剪32x32区域，绘制到(10,10)并放大到64x64

// 使用RectangleF（AI友好）
var sourceRect = new RectangleF(32, 0, 32, 32);
var destRect = new RectangleF(10, 10, 64, 64);
canvas.DrawImage(spriteSheet, sourceRect, destRect);
#endif
```

### 图像应用示例

#### 精灵动画

```csharp
#if CLIENT
public class SpriteAnimator
{
    private Image spriteSheet;
    private int frameWidth = 32;
    private int frameHeight = 32;
    private int currentFrame = 0;
    private int totalFrames = 8;
    
    public SpriteAnimator(string spriteSheetPath)
    {
        spriteSheet = new Image(spriteSheetPath);
    }
    
    public void DrawFrame(Canvas canvas, float x, float y)
    {
        var sourceX = (currentFrame % 8) * frameWidth;
        var sourceY = (currentFrame / 8) * frameHeight;
        
        canvas.DrawImage(
            spriteSheet,
            sourceX, sourceY, frameWidth, frameHeight,
            x, y, 64, 64  // 放大显示
        );
    }
    
    public void NextFrame()
    {
        currentFrame = (currentFrame + 1) % totalFrames;
    }
}

// 使用
var animator = new SpriteAnimator("character_walk.png");
animator.DrawFrame(canvas, 100, 100);
animator.NextFrame(); // 下一帧
#endif
```

#### 平铺图像

```csharp
#if CLIENT
public void DrawTiledBackground(Canvas canvas, Image tile, float width, float height)
{
    // 假设tile是32x32
    var tileSize = 32f;
    
    for (float y = 0; y < height; y += tileSize)
    {
        for (float x = 0; x < width; x += tileSize)
        {
            canvas.DrawImage(tile, x, y, tileSize, tileSize);
        }
    }
}
#endif
```

---

## 变换操作

### 基础变换

#### Translate - 平移

```csharp
#if CLIENT
canvas.SaveState();

// 平移坐标系
canvas.Translate(100, 50);

// 在新坐标系中绘制
canvas.FillRectangle(0, 0, 50, 50);
// 实际绘制在(100, 50)

canvas.RestoreState();
#endif
```

#### Rotate - 旋转

```csharp
#if CLIENT
canvas.SaveState();

// 旋转坐标系（角度制）
canvas.Rotate(45);  // 顺时针旋转45度

// 或使用明确的方法
canvas.RotateDegrees(45f);   // 角度制
canvas.RotateRadians(MathF.PI / 4);  // 弧度制

// 绘制旋转后的矩形
canvas.FillRectangle(0, 0, 100, 50);

canvas.RestoreState();
#endif
```

⚠️ **注意**：旋转围绕原点(0,0)进行，通常需要配合平移使用。

#### Scale - 缩放

```csharp
#if CLIENT
canvas.SaveState();

// 缩放坐标系
canvas.Scale(2.0f, 1.5f);  // X轴放大2倍，Y轴放大1.5倍

// 绘制会被缩放
canvas.FillCircle(50, 50, 20);
// 实际绘制的椭圆：中心(100,75), 半径(40,30)

canvas.RestoreState();
#endif
```

### 组合变换

#### 围绕中心旋转

```csharp
#if CLIENT
public void DrawRotatedRectAroundCenter(Canvas canvas, float centerX, float centerY, 
    float width, float height, float angleDegrees)
{
    canvas.SaveState();
    
    // 1. 平移到旋转中心
    canvas.Translate(centerX, centerY);
    
    // 2. 旋转
    canvas.RotateDegrees(angleDegrees);
    
    // 3. 绘制（以中心为原点）
    canvas.FillRectangle(-width / 2, -height / 2, width, height);
    
    canvas.RestoreState();
}

// 使用
canvas.FillPaint = new SolidPaint(Color.Blue);
DrawRotatedRectAroundCenter(canvas, 200, 150, 100, 50, 30);
#endif
```

#### 复杂变换链

```csharp
#if CLIENT
canvas.SaveState();

// 变换链：平移 -> 旋转 -> 缩放
canvas.Translate(200, 150);      // 移到(200,150)
canvas.RotateDegrees(45);        // 旋转45度
canvas.Scale(1.5f, 1.5f);        // 放大1.5倍

// 绘制
canvas.FillCircle(0, 0, 30);     // 在变换后的坐标系中绘制

canvas.RestoreState();
#endif
```

### 变换矩阵

```csharp
#if CLIENT
using System.Numerics;

// 使用矩阵进行复杂变换
var matrix = Matrix3x2.CreateRotation(MathF.PI / 4);      // 旋转
matrix *= Matrix3x2.CreateScale(1.5f);                     // 缩放
matrix *= Matrix3x2.CreateTranslation(100, 50);            // 平移

canvas.SaveState();
canvas.ConcatenateTransform(matrix);

// 绘制
canvas.FillRectangle(0, 0, 50, 50);

canvas.RestoreState();

// 重置所有变换
canvas.ResetTransform();
#endif
```

---

## 状态管理

### SaveState / RestoreState

Canvas使用栈式状态管理，确保变换和属性的安全隔离：

```csharp
#if CLIENT
// 保存当前状态
canvas.SaveState();

// 修改状态
canvas.Translate(100, 100);
canvas.FillPaint = new SolidPaint(Color.Red);
canvas.FillCircle(0, 0, 30);

// 恢复到保存的状态
canvas.RestoreState();

// 状态已恢复：平移和颜色都恢复了
canvas.FillCircle(0, 0, 30);  // 绘制在原点，使用原来的颜色
#endif
```

### 状态包含的内容

SaveState/RestoreState会保存和恢复以下内容：

- ✅ 变换矩阵（Translate, Rotate, Scale）
- ✅ 裁剪区域
- ✅ 绘制属性（FillPaint, StrokePaint, StrokeWidth等）
- ✅ Alpha（全局透明度）
- ✅ BlendMode（混合模式）

### 最佳实践

```csharp
#if CLIENT
// ✅ 正确：嵌套状态
public void DrawComplexShape(Canvas canvas)
{
    canvas.SaveState();  // 外层状态
    
    canvas.Translate(100, 100);
    
    // 内层状态
    canvas.SaveState();
    canvas.RotateDegrees(45);
    canvas.FillCircle(0, 0, 30);
    canvas.RestoreState();  // 恢复旋转
    
    // 仍然保持平移
    canvas.FillRectangle(50, 50, 30, 30);
    
    canvas.RestoreState();  // 恢复平移
}

// ❌ 错误：不平衡的Save/Restore
public void BadExample(Canvas canvas)
{
    canvas.SaveState();
    canvas.Translate(100, 100);
    // 忘记调用RestoreState！
}
#endif
```

### ResetState - 清除画布

```csharp
#if CLIENT
// 清除画布并重置所有状态
canvas.ResetState();

// 相当于：
// - 清除所有绘制内容
// - 重置变换矩阵
// - 清空状态栈
// - 恢复默认属性
#endif
```

---

## Paint系统

### SolidPaint - 实心画笔

```csharp
#if CLIENT
using GameUI.Graphics;

// 创建实心颜色画笔
var solidPaint = new SolidPaint(Color.Blue);
canvas.FillPaint = solidPaint;

// 使用ARGB创建
var transparentRed = new SolidPaint(Color.FromArgb(128, 255, 0, 0));
canvas.FillPaint = transparentRed;

// 快捷设置
canvas.FillPaint = new SolidPaint(Color.Red);
canvas.StrokePaint = new SolidPaint(Color.Black);
#endif
```

### LinearGradientPaint - 线性渐变

```csharp
#if CLIENT
// 创建线性渐变：从起点到终点
var linearGradient = new LinearGradientPaint(
    new PointF(0, 0),      // 起点
    new PointF(200, 0),    // 终点
    Color.Blue,            // 起始颜色
    Color.Red              // 结束颜色
);

canvas.FillPaint = linearGradient;
canvas.FillRectangle(10, 10, 200, 100);

// 垂直渐变
var verticalGradient = new LinearGradientPaint(
    new PointF(0, 0),      // 顶部
    new PointF(0, 100),    // 底部
    Color.White,
    Color.Black
);

// 对角渐变
var diagonalGradient = new LinearGradientPaint(
    new PointF(0, 0),      // 左上
    new PointF(100, 100),  // 右下
    Color.Yellow,
    Color.Orange
);
#endif
```

### RadialGradientPaint - 径向渐变

```csharp
#if CLIENT
// 创建径向渐变：从中心向外辐射
var radialGradient = new RadialGradientPaint(
    new PointF(100, 100),  // 中心点
    0,                     // 内半径
    50,                    // 外半径
    Color.White,           // 中心颜色
    Color.Blue             // 外围颜色
);

canvas.FillPaint = radialGradient;
canvas.FillCircle(100, 100, 50);

// 偏心径向渐变（光照效果）
var spotlightGradient = new RadialGradientPaint(
    new PointF(80, 80),    // 光源偏移位置
    0,
    60,
    Color.White,
    Color.Black
);
canvas.FillPaint = spotlightGradient;
canvas.FillCircle(100, 100, 60);
#endif
```

### BoxGradientPaint - 盒式渐变

```csharp
#if CLIENT
// 创建盒式渐变：矩形区域的柔和渐变
var boxGradient = new BoxGradientPaint(
    new RectangleF(10, 10, 200, 100),  // 矩形区域
    10,                                 // 圆角半径
    20,                                 // 羽化范围
    Color.LightBlue,                    // 内部颜色
    Color.DarkBlue                      // 外部颜色
);

canvas.FillPaint = boxGradient;
canvas.FillRectangle(10, 10, 200, 100);

// 用于创建阴影效果
var shadowGradient = new BoxGradientPaint(
    new RectangleF(50, 50, 100, 50),
    5,
    10,
    Color.FromArgb(0, 0, 0, 0),        // 透明
    Color.FromArgb(128, 0, 0, 0)       // 半透明黑色
);
#endif
```

### Paint应用示例

#### 彩虹渐变按钮

```csharp
#if CLIENT
public void DrawRainbowButton(Canvas canvas, float x, float y, float width, float height)
{
    // 多色渐变需要通过多个矩形实现
    var colors = new[] {
        Color.Red, Color.Orange, Color.Yellow, 
        Color.Green, Color.Blue, Color.Indigo, Color.Violet
    };
    
    float segmentWidth = width / colors.Length;
    
    for (int i = 0; i < colors.Length; i++)
    {
        var gradient = new LinearGradientPaint(
            new PointF(x + i * segmentWidth, y),
            new PointF(x + (i + 1) * segmentWidth, y),
            colors[i],
            i < colors.Length - 1 ? colors[i + 1] : colors[i]
        );
        
        canvas.FillPaint = gradient;
        canvas.FillRectangle(x + i * segmentWidth, y, segmentWidth, height);
    }
}
#endif
```

#### 光照效果球体

```csharp
#if CLIENT
public void DrawLitSphere(Canvas canvas, PointF center, float radius)
{
    // 径向渐变模拟光照
    var lightPos = new PointF(center.X - radius * 0.3f, center.Y - radius * 0.3f);
    
    var gradient = new RadialGradientPaint(
        lightPos,
        0,
        radius * 1.2f,
        Color.FromArgb(255, 255, 255, 200),  // 高光
        Color.FromArgb(255, 50, 50, 150)     // 阴影
    );
    
    canvas.FillPaint = gradient;
    canvas.FillCircle(center.X, center.Y, radius);
}
#endif
```

---

## 高级特性

### 裁剪区域

```csharp
#if CLIENT
// 设置矩形裁剪区域
canvas.ClipRect(50, 50, 200, 100);

// 只有在裁剪区域内的部分会显示
canvas.FillCircle(100, 100, 60);  // 圆形被裁剪

// 从裁剪区域中减去矩形（镂空）
canvas.SubtractFromClip(100, 75, 50, 50);
// 创建"挖洞"效果

// 重置裁剪（使用ResetState）
canvas.ResetState();
#endif
```

### 混合模式 (BlendMode)

```csharp
#if CLIENT
// 标准混合（默认）
canvas.BlendMode = BlendMode.SourceOver;
canvas.FillRectangle(10, 10, 100, 100);

// 相乘混合（变暗）
canvas.BlendMode = BlendMode.Multiply;
canvas.FillPaint = new SolidPaint(Color.FromArgb(128, 255, 0, 0));
canvas.FillCircle(60, 60, 40);

// 屏幕混合（变亮）
canvas.BlendMode = BlendMode.Screen;
canvas.FillCircle(100, 60, 40);

// 覆盖混合
canvas.BlendMode = BlendMode.Overlay;
canvas.FillCircle(140, 60, 40);

// 恢复默认
canvas.BlendMode = BlendMode.SourceOver;
#endif
```

可用的混合模式：
- `SourceOver` - 标准混合（默认）
- `SourceIn` - 只在已有内容区域内绘制
- `SourceOut` - 只在已有内容区域外绘制
- `SourceAtop` - 替换已有内容的非透明部分
- `Multiply` - 相乘混合，产生更暗的颜色
- `Screen` - 屏幕混合，产生更亮的颜色
- `Overlay` - 覆盖混合

### 便捷方法

#### SetColors - 同时设置填充和描边

```csharp
#if CLIENT
// 同时设置两种颜色
canvas.SetColors(Color.Blue, Color.DarkBlue);
// 相当于：
// canvas.FillPaint = new SolidPaint(Color.Blue);
// canvas.StrokePaint = new SolidPaint(Color.DarkBlue);

// 绘制带边框的形状
canvas.FillRectangle(10, 10, 100, 50);
canvas.DrawRectangle(10, 10, 100, 50);
#endif
```

---

## 实战示例

### 示例1：绘制折线图

```csharp
#if CLIENT
public class LineChart
{
    private float[] dataPoints;
    private Canvas canvas;
    private float chartWidth = 400;
    private float chartHeight = 200;
    private float marginLeft = 50;
    private float marginBottom = 30;
    
    public LineChart(Canvas canvas, float[] data)
    {
        this.canvas = canvas;
        this.dataPoints = data;
    }
    
    public void Draw()
    {
        DrawAxes();
        DrawGrid();
        DrawData();
        DrawLabels();
    }
    
    private void DrawAxes()
    {
        canvas.StrokePaint = new SolidPaint(Color.Black);
        canvas.StrokeWidth = 2f;
        
        // Y轴
        canvas.DrawLine(marginLeft, 10, marginLeft, chartHeight + 10);
        
        // X轴
        canvas.DrawLine(marginLeft, chartHeight + 10, 
            marginLeft + chartWidth, chartHeight + 10);
    }
    
    private void DrawGrid()
    {
        canvas.StrokePaint = new SolidPaint(Color.LightGray);
        canvas.StrokeWidth = 1f;
        
        // 横向网格线
        for (int i = 0; i <= 5; i++)
        {
            float y = 10 + (chartHeight / 5) * i;
            canvas.DrawLine(marginLeft, y, marginLeft + chartWidth, y);
        }
        
        // 纵向网格线
        for (int i = 0; i <= dataPoints.Length; i++)
        {
            float x = marginLeft + (chartWidth / dataPoints.Length) * i;
            canvas.DrawLine(x, 10, x, chartHeight + 10);
        }
    }
    
    private void DrawData()
    {
        if (dataPoints.Length < 2) return;
        
        canvas.StrokePaint = new SolidPaint(Color.Blue);
        canvas.StrokeWidth = 3f;
        
        float maxValue = dataPoints.Max();
        float xStep = chartWidth / (dataPoints.Length - 1);
        
        var path = new PathF();
        
        for (int i = 0; i < dataPoints.Length; i++)
        {
            float x = marginLeft + i * xStep;
            float y = 10 + chartHeight - (dataPoints[i] / maxValue * chartHeight);
            
            if (i == 0)
                path.MoveTo(x, y);
            else
                path.LineTo(x, y);
            
            // 绘制数据点
            canvas.FillPaint = new SolidPaint(Color.Red);
            canvas.FillCircle(x, y, 4);
        }
        
        canvas.DrawPath(path);
    }
    
    private void DrawLabels()
    {
        // 标签绘制需要Text API（如果有的话）
        // 或使用Label控件叠加
    }
}

// 使用
var data = new float[] { 10, 25, 15, 40, 30, 50, 45 };
var chart = new LineChart(canvas, data);
chart.Draw();
#endif
```

### 示例2：绘制饼图

```csharp
#if CLIENT
public class PieChart
{
    private Canvas canvas;
    private float[] values;
    private Color[] colors;
    private PointF center;
    private float radius;
    
    public PieChart(Canvas canvas, float[] values, Color[] colors, PointF center, float radius)
    {
        this.canvas = canvas;
        this.values = values;
        this.colors = colors;
        this.center = center;
        this.radius = radius;
    }
    
    public void Draw()
    {
        float total = values.Sum();
        float startAngle = -90; // 从顶部开始
        
        for (int i = 0; i < values.Length; i++)
        {
            float sweepAngle = (values[i] / total) * 360;
            
            DrawPieSlice(center, radius, startAngle, startAngle + sweepAngle, colors[i]);
            
            startAngle += sweepAngle;
        }
    }
    
    private void DrawPieSlice(PointF center, float radius, float startAngle, float endAngle, Color color)
    {
        var path = new PathF();
        
        // 移动到中心
        path.MoveTo(center.X, center.Y);
        
        // 移动到圆弧起点
        float startRad = startAngle * MathF.PI / 180;
        path.LineTo(
            center.X + radius * MathF.Cos(startRad),
            center.Y + radius * MathF.Sin(startRad)
        );
        
        // 添加圆弧
        path.AddCircleArc(center, radius, startAngle, endAngle, true);
        
        // 回到中心
        path.Close();
        
        // 填充
        canvas.FillPaint = new SolidPaint(color);
        canvas.FillPath(path);
        
        // 描边
        canvas.StrokePaint = new SolidPaint(Color.White);
        canvas.StrokeWidth = 2f;
        canvas.DrawPath(path);
    }
}

// 使用
var values = new float[] { 30, 20, 25, 15, 10 };
var colors = new Color[] {
    Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Purple
};
var pieChart = new PieChart(canvas, values, colors, new PointF(200, 150), 100);
pieChart.Draw();
#endif
```

### 示例3：绘制小地图

```csharp
#if CLIENT
public class MiniMap
{
    private Canvas canvas;
    private RectangleF mapBounds;
    private List<Entity> entities;
    
    public class Entity
    {
        public Vector2 Position { get; set; }
        public EntityType Type { get; set; }
        public float Size { get; set; }
    }
    
    public enum EntityType
    {
        Player,
        Enemy,
        Item,
        Obstacle
    }
    
    public MiniMap(Canvas canvas, RectangleF bounds)
    {
        this.canvas = canvas;
        this.mapBounds = bounds;
        this.entities = new List<Entity>();
    }
    
    public void AddEntity(Entity entity)
    {
        entities.Add(entity);
    }
    
    public void Draw()
    {
        // 绘制地图背景
        canvas.FillPaint = new SolidPaint(Color.FromArgb(128, 50, 50, 50));
        canvas.FillRectangle(
            mapBounds.X, mapBounds.Y, 
            mapBounds.Width, mapBounds.Height
        );
        
        // 绘制边框
        canvas.StrokePaint = new SolidPaint(Color.White);
        canvas.StrokeWidth = 2f;
        canvas.DrawRectangle(
            mapBounds.X, mapBounds.Y, 
            mapBounds.Width, mapBounds.Height
        );
        
        // 绘制实体
        foreach (var entity in entities)
        {
            DrawEntity(entity);
        }
    }
    
    private void DrawEntity(Entity entity)
    {
        // 将世界坐标映射到小地图坐标
        var mapPos = WorldToMapPosition(entity.Position);
        
        // 根据类型选择颜色
        Color color = entity.Type switch
        {
            EntityType.Player => Color.Green,
            EntityType.Enemy => Color.Red,
            EntityType.Item => Color.Yellow,
            EntityType.Obstacle => Color.Gray,
            _ => Color.White
        };
        
        canvas.FillPaint = new SolidPaint(color);
        canvas.FillCircle(mapPos.X, mapPos.Y, entity.Size);
    }
    
    private Vector2 WorldToMapPosition(Vector2 worldPos)
    {
        // 简化的坐标映射（需要根据实际世界大小调整）
        var mapX = mapBounds.X + (worldPos.X / 1000) * mapBounds.Width;
        var mapY = mapBounds.Y + (worldPos.Y / 1000) * mapBounds.Height;
        return new Vector2(mapX, mapY);
    }
}

// 使用
var miniMap = new MiniMap(canvas, new RectangleF(500, 20, 150, 150));
miniMap.AddEntity(new MiniMap.Entity 
{ 
    Position = new Vector2(500, 500), 
    Type = MiniMap.EntityType.Player,
    Size = 5
});
miniMap.AddEntity(new MiniMap.Entity 
{ 
    Position = new Vector2(300, 700), 
    Type = MiniMap.EntityType.Enemy,
    Size = 4
});
miniMap.Draw();
#endif
```

---

## 性能优化

### 1. 减少状态切换

```csharp
#if CLIENT
// ❌ 低效：频繁切换Paint
for (int i = 0; i < 100; i++)
{
    canvas.FillPaint = new SolidPaint(Color.Red);
    canvas.FillCircle(i * 10, 50, 5);
    canvas.FillPaint = new SolidPaint(Color.Blue);
    canvas.FillCircle(i * 10, 100, 5);
}

// ✅ 高效：批量绘制相同属性的图形
canvas.FillPaint = new SolidPaint(Color.Red);
for (int i = 0; i < 100; i++)
{
    canvas.FillCircle(i * 10, 50, 5);
}

canvas.FillPaint = new SolidPaint(Color.Blue);
for (int i = 0; i < 100; i++)
{
    canvas.FillCircle(i * 10, 100, 5);
}
#endif
```

### 2. 复用路径对象

```csharp
#if CLIENT
// ✅ 推荐：缓存复杂路径
private PathF cachedStarPath;

public PathF GetStarPath()
{
    if (cachedStarPath == null)
    {
        cachedStarPath = CreateStarPath(new PointF(0, 0), 50, 20);
    }
    return cachedStarPath;
}

// 多次使用同一路径
var starPath = GetStarPath();
canvas.SaveState();
canvas.Translate(100, 100);
canvas.FillPath(starPath);
canvas.RestoreState();

canvas.SaveState();
canvas.Translate(200, 100);
canvas.FillPath(starPath);
canvas.RestoreState();
#endif
```

### 3. 避免不必要的SaveState

```csharp
#if CLIENT
// ❌ 不必要的状态保存
canvas.SaveState();
canvas.FillPaint = new SolidPaint(Color.Red);
canvas.FillCircle(50, 50, 30);
canvas.RestoreState();

// ✅ 直接设置（如果不需要恢复）
canvas.FillPaint = new SolidPaint(Color.Red);
canvas.FillCircle(50, 50, 30);
#endif
```

### 4. 使用合适的分辨率

```csharp
#if CLIENT
// 根据实际显示尺寸设置分辨率
canvas.AutoUpdateResolutionOnResize = true; // 推荐

// 或手动设置合理的分辨率
canvas.AutoUpdateResolutionOnResize = false;
canvas.Resolution = new SizeF(800, 600); // 不要过大
#endif
```

### 5. 减少复杂路径的复杂度

```csharp
#if CLIENT
// ❌ 过于复杂的路径
var path = new PathF();
for (int i = 0; i < 1000; i++)
{
    path.LineTo(i, Math.Sin(i * 0.1) * 50 + 100);
}

// ✅ 降低采样率
var path = new PathF();
for (int i = 0; i < 100; i+=10)  // 减少点数
{
    path.LineTo(i, Math.Sin(i * 0.1) * 50 + 100);
}
#endif
```

---

## 最佳实践

### ✅ 推荐做法

#### 1. 始终使用SaveState/RestoreState配对

```csharp
#if CLIENT
public void DrawSomething(Canvas canvas)
{
    canvas.SaveState();
    try
    {
        // 绘制操作
        canvas.Translate(100, 100);
        canvas.FillCircle(0, 0, 30);
    }
    finally
    {
        canvas.RestoreState(); // 确保总是恢复
    }
}
#endif
```

#### 2. 使用DesignResolution模式

```csharp
#if CLIENT
// ✅ 推荐：使用设计分辨率模式
canvas.CoordinateMode = CanvasCoordinateMode.DesignResolution;
// 坐标与UI其他部分一致，跨设备效果一致
#endif
```

#### 3. 组织绘制代码

```csharp
#if CLIENT
public class MyCanvasControl : Canvas
{
    public MyCanvasControl()
    {
        Width = 400;
        Height = 300;
    }
    
    // 将复杂绘制分解为方法
    public void DrawAll()
    {
        ResetState();
        DrawBackground();
        DrawContent();
        DrawBorder();
    }
    
    private void DrawBackground()
    {
        FillPaint = new SolidPaint(Color.White);
        FillRectangle(0, 0, Width, Height);
    }
    
    private void DrawContent()
    {
        // 内容绘制
    }
    
    private void DrawBorder()
    {
        StrokePaint = new SolidPaint(Color.Black);
        StrokeWidth = 2f;
        DrawRectangle(0, 0, Width, Height);
    }
}
#endif
```

#### 4. 使用辅助方法简化代码

```csharp
#if CLIENT
public static class CanvasExtensions
{
    public static void DrawCenteredText(this Canvas canvas, string text, 
        PointF center, float fontSize)
    {
        // 如果有文本API，在这里实现
        // 或使用Label控件叠加
    }
    
    public static void FillRoundedRect(this Canvas canvas, RectangleF rect, float radius)
    {
        var path = new PathF();
        path.AppendRoundedRectangle(rect.X, rect.Y, rect.Width, rect.Height, radius);
        canvas.FillPath(path);
    }
}

// 使用
canvas.FillRoundedRect(new RectangleF(10, 10, 100, 50), 8);
#endif
```

### ❌ 避免的做法

#### 1. 忘记ResetState或RestoreState

```csharp
#if CLIENT
// ❌ 错误：变换累积
public void DrawMultipleTimes()
{
    for (int i = 0; i < 5; i++)
    {
        canvas.Translate(50, 0);  // 累积平移！
        canvas.FillCircle(0, 0, 20);
        // 忘记RestoreState
    }
}

// ✅ 正确：每次恢复状态
public void DrawMultipleTimes()
{
    for (int i = 0; i < 5; i++)
    {
        canvas.SaveState();
        canvas.Translate(i * 50, 0);
        canvas.FillCircle(0, 0, 20);
        canvas.RestoreState();
    }
}
#endif
```

#### 2. 过度使用复杂路径

```csharp
#if CLIENT
// ❌ 避免：每帧创建复杂路径
public void OnUpdate()
{
    var complexPath = CreateVeryComplexPath(); // 创建1000+点的路径
    canvas.DrawPath(complexPath);
}

// ✅ 推荐：缓存路径
private PathF cachedPath;
public void OnUpdate()
{
    cachedPath ??= CreateVeryComplexPath();
    canvas.DrawPath(cachedPath);
}
#endif
```

#### 3. 不考虑坐标系统

```csharp
#if CLIENT
// ❌ 错误：混淆坐标系统
canvas.Rotate(45);
canvas.DrawCircle(100, 100, 30);  // 位置不符合预期

// ✅ 正确：理解旋转围绕原点
canvas.SaveState();
canvas.Translate(100, 100);  // 先平移到目标位置
canvas.Rotate(45);           // 然后旋转
canvas.DrawCircle(0, 0, 30); // 在局部原点绘制
canvas.RestoreState();
#endif
```

---

## 相关文档

- [虚拟化列表系统](VirtualizingPanelSystem.md) - Canvas在虚拟化列表项中的使用
- [指针捕获系统](PointerCaptureSystem.md) - 在Canvas上实现交互式绘图
- [UI设计规范](../guides/UIDesignStandards.md) - UI绘制的设计标准
- [DrawPath实现说明](../../GameUI/Graphics/DrawPath_README.md) - PathF的详细实现

---

## 总结

**Canvas绘图系统**是WasiCore框架中功能强大的2D绘图工具，掌握以下要点：

### 核心概念 ✅
1. **坐标系统**: 优先使用DesignResolution模式
2. **Paint系统**: 实心、线性渐变、径向渐变、盒式渐变
3. **状态管理**: SaveState/RestoreState必须配对
4. **路径绘制**: PathF提供强大的复杂图形绘制能力
5. **变换操作**: Translate -> Rotate -> Scale 的正确顺序

### 典型应用 🎯
- ✅ 图表绘制（折线图、柱状图、饼图）
- ✅ UI装饰（边框、分隔线、背景图案）
- ✅ 游戏UI（小地图、血条、技能图标）
- ✅ 数据可视化（热力图、网络图）
- ✅ 交互式绘图（画板、签名）
- ✅ 自定义控件

### 性能优化 💡
- 减少状态切换
- 复用路径对象
- 合理设置分辨率
- 批量绘制相同属性的图形
- 避免过于复杂的路径

### 最佳实践 🌟
- 使用SaveState/RestoreState确保状态隔离
- 将复杂绘制分解为小方法
- 缓存复杂的路径和计算结果
- 使用DesignResolution模式保持一致性
- 理解并正确使用变换顺序

遵循这些原则，你就能用Canvas创建出精美、高性能的2D图形！

---

*本文档持续更新中。如有问题或建议，欢迎反馈。*

