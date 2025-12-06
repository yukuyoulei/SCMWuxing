# AI开发Canvas 2D游戏指南

## 📖 文档说明

本文档专为**使用AI工具开发WasiCore框架下的Canvas 2D游戏**而设计，是 [AI开发指导文档](AI_DEVELOPMENT_GUIDE.md) 的专项补充。

### 🎯 适用场景
- 2D平台游戏（如马里奥、塞尔达等）
- 2D射击游戏
- 益智游戏
- 跑酷游戏
- 任何需要使用Canvas绘图的2D游戏

### 📚 前置阅读
在阅读本文档前，请先阅读：
- [AI开发指导文档](AI_DEVELOPMENT_GUIDE.md) - 框架通用开发指南
- [框架概述](../../FRAMEWORK_OVERVIEW.md) - 理解框架核心概念

---

## 🔴 最重要的事：正确的编译配置

WasiCore框架使用**条件编译**区分客户端和服务端代码。对于2D游戏开发（使用Canvas），**必须**使用客户端编译配置：

```bash
# ✅ 正确：编译客户端代码（2D游戏开发）
dotnet build *.sln -c Client-Debug

# ❌ 错误！会导致数千个编译错误
dotnet build *.sln
dotnet build *.sln -c Debug
```

### ⚠️ 不使用正确配置的后果
- 所有 `#if CLIENT` 包裹的代码不会被编译
- 导致 **3000+ 编译错误**
- `GameUI`、`Canvas` 等类型全部显示"找不到"
- AI可能会误以为是API不存在而尝试错误的替代方案

### ✅ 正确的代码结构
```csharp
#if CLIENT
using GameUI.Control.Primitive;
using GameUI.Graphics;

namespace YourGame
{
    public class GameRenderer
    {
        private Canvas canvas;
        
        public void Draw()
        {
            // Canvas绘图代码
        }
    }
}
#endif
```

---

## 🖥️ 设计分辨率规范

WasiCore框架的UI系统使用**固定的设计分辨率**，这对于保持游戏元素比例至关重要。

### 标准设计分辨率

| 屏幕方向 | 设计分辨率 (宽×高) | 适用场景 |
|---------|------------------|---------|
| **横屏** | **1920 × 1080** | 2D平台游戏（推荐）、横版射击等 |
| **竖屏** | **1080 × 1920** | 益智游戏、跑酷游戏等 |

### 正确的分辨率设置

```csharp
// 获取视口信息并判断屏幕方向
var viewportSize = GameUI.Device.ScreenViewport.Primary.Size;
bool isLandscape = viewportSize.Width >= viewportSize.Height;

float gameWidth, gameHeight;
if (isLandscape)
{
    gameWidth = 1920f;  // 横屏
    gameHeight = 1080f;
}
else
{
    gameWidth = 1080f;  // 竖屏
    gameHeight = 1920f;
}

// 所有游戏元素的位置和尺寸都基于这个设计分辨率
const float GROUND_Y = 980f;  // 980/1080 = 90.7%（接近底部）
const float PLAYER_HEIGHT = 120f;  // 120/1080 = 11.1%
```

### 📱 安全区域 (Safe Zone) - 重要！

现代移动设备通常有**刘海屏**、**挖孔屏**、**圆角屏幕**或**系统手势区域**，这些区域可能遮挡或裁切游戏内容。WasiCore提供了 `SafeZonePadding` 属性来处理这些情况。

#### 什么是安全区域？

```csharp
// 获取安全区域内边距（设备独立像素）
var safeZone = GameUI.Device.ScreenViewport.Primary.SafeZonePadding;

// SafeZonePadding 包含四个方向的内边距：
// - Left: 左侧不安全区域宽度（刘海/挖孔/圆角）
// - Top: 顶部不安全区域高度（刘海/状态栏）
// - Right: 右侧不安全区域宽度（圆角）
// - Bottom: 底部不安全区域高度（Home Indicator/手势条/圆角）
```

#### 为什么需要考虑安全区域？

| 问题 | 后果 |
|------|------|
| **UI按钮放在不安全区域** | 用户无法点击或误触系统手势 |
| **重要信息被刘海遮挡** | 玩家看不到分数、生命值等 |
| **游戏内容被圆角裁切** | 视觉效果不佳，内容丢失 |

#### 正确使用安全区域

```csharp
// ✅ 推荐：将UI元素放置在安全区域内
public void CreateGameUI()
{
    var safeZone = GameUI.Device.ScreenViewport.Primary.SafeZonePadding;
    var viewportSize = GameUI.Device.ScreenViewport.Primary.Size;
    
    // 计算安全区域内的可用空间
    float safeLeft = safeZone.Left;
    float safeTop = safeZone.Top;
    float safeRight = viewportSize.Width - safeZone.Right;
    float safeBottom = viewportSize.Height - safeZone.Bottom;
    
    float safeWidth = safeRight - safeLeft;
    float safeHeight = safeBottom - safeTop;
    
    // 在安全区域内放置UI元素
    // 例如：左上角的分数显示
    var scoreLabel = new Label
    {
        Text = "Score: 0",
        Position = new Vector2(safeLeft + 20f, safeTop + 20f),  // 留出20px边距
        Parent = canvas
    };
    
    // 例如：右下角的按钮（考虑手势区域）
    var pauseButton = new Button
    {
        Width = 80f,
        Height = 80f,
        Position = new Vector2(
            safeRight - 100f,   // 距离右边界100px（避开圆角和边缘）
            safeBottom - 100f   // 距离底部100px（避开Home Indicator）
        ),
        Parent = canvas
    };
}
```

#### Canvas游戏内容的安全区域适配

```csharp
public class SafeGameRenderer
{
    private float gameContentLeft;
    private float gameContentTop;
    private float gameContentWidth;
    private float gameContentHeight;
    
    public SafeGameRenderer()
    {
        UpdateSafeArea();
        
        // 监听屏幕方向变化，重新计算安全区域
        GameUI.Device.ScreenViewport.Primary.OnOrientationChanged += _ => UpdateSafeArea();
    }
    
    private void UpdateSafeArea()
    {
        var safeZone = GameUI.Device.ScreenViewport.Primary.SafeZonePadding;
        var designResolution = GameUI.Device.ScreenViewport.Primary.DesignResolution;
        
        // 计算游戏内容区域（在安全区域内）
        gameContentLeft = safeZone.Left;
        gameContentTop = safeZone.Top;
        gameContentWidth = designResolution.Width - safeZone.Left - safeZone.Right;
        gameContentHeight = designResolution.Height - safeZone.Top - safeZone.Bottom;
        
        // 重新计算游戏元素位置
        RecalculateGameLayout();
    }
    
    private void RecalculateGameLayout()
    {
        // 例如：确保地面在安全区域内
        float safeGroundY = gameContentTop + gameContentHeight - 100f;
        
        // 例如：确保玩家初始位置在安全区域内
        float playerStartX = gameContentLeft + 100f;
        float playerStartY = safeGroundY - playerHeight;
    }
    
    public void DrawGame(Canvas canvas)
    {
        // 1. 绘制背景（全屏）
        DrawBackground(canvas);
        
        // 2. 在安全区域内绘制游戏内容
        canvas.Save();
        // 可选：裁剪到安全区域
        canvas.ClipRect(gameContentLeft, gameContentTop, gameContentWidth, gameContentHeight);
        
        DrawGameContent(canvas);
        
        canvas.Restore();
        
        // 3. UI元素已经在安全区域内放置（见上文）
    }
}
```

#### 调试：可视化安全区域

```csharp
// 开发时可以绘制安全区域边界，帮助调试
public void DrawSafeZoneDebug(Canvas canvas)
{
    var safeZone = GameUI.Device.ScreenViewport.Primary.SafeZonePadding;
    var viewport = GameUI.Device.ScreenViewport.Primary.Size;
    
    // 绘制不安全区域（半透明红色）
    canvas.FillPaint = new SolidPaint(Color.FromArgb(128, 255, 0, 0));
    
    // 左侧不安全区域
    if (safeZone.Left > 0)
        canvas.FillRectangle(0, 0, safeZone.Left, viewport.Height);
    
    // 顶部不安全区域
    if (safeZone.Top > 0)
        canvas.FillRectangle(0, 0, viewport.Width, safeZone.Top);
    
    // 右侧不安全区域
    if (safeZone.Right > 0)
        canvas.FillRectangle(viewport.Width - safeZone.Right, 0, safeZone.Right, viewport.Height);
    
    // 底部不安全区域
    if (safeZone.Bottom > 0)
        canvas.FillRectangle(0, viewport.Height - safeZone.Bottom, viewport.Width, safeZone.Bottom);
    
    // 绘制安全区域边界（绿色虚线）
    canvas.StrokePaint = new SolidPaint(Color.FromArgb(255, 0, 255, 0));
    canvas.StrokeWidth = 2f;
    canvas.StrokeRectangle(
        safeZone.Left, 
        safeZone.Top, 
        viewport.Width - safeZone.Left - safeZone.Right,
        viewport.Height - safeZone.Top - safeZone.Bottom
    );
}
```

#### 关键点

- **⭐ UI元素必须在安全区域内**：按钮、文本、重要信息等
- **游戏背景可以全屏**：装饰性背景可以延伸到不安全区域
- **监听方向变化**：屏幕旋转时安全区域可能变化
- **预留额外边距**：安全区域边界上仍建议留出10-20px边距
- **测试多种设备**：不同设备的安全区域差异很大（特别是iPhone的刘海和底部手势条）

### ⚠️ 常见错误

```csharp
// ❌ 错误：使用 Math.Max 导致地面位置错误
var gameWidth = Math.Max(800f, viewportSize.Width);  // 可能变成1920
var gameHeight = Math.Max(600f, viewportSize.Height); // 可能变成1080
const float GROUND_Y = 550f;  // 固定值
// 结果：550/1080 = 50.9%（画面中部）而不是预期的底部

// ✅ 正确：使用设计分辨率
const float GAME_HEIGHT = 1080f;
const float GROUND_Y = 980f;  // 980/1080 = 90.7%（底部）
```

---

## 🖼️ Canvas API 详细说明

### ⚠️ 最重要的API差异

WasiCore的Canvas API与HTML5 Canvas有显著差异，AI工具必须注意：

| API | 参数模式 | 说明 |
|-----|---------|------|
| `FillRectangle` | **(x, y, width, height)** | 左上角坐标 + 尺寸 |
| `FillEllipse` | **(centerX, centerY, radiusX, radiusY)** | **中心坐标 + 半径** |
| `FillCircle` | **(centerX, centerY, radius)** | **中心坐标 + 半径** |

### 关键理解示例

```csharp
// ❌ 错误理解（常见AI错误）
canvas.FillEllipse(x, y, width, height);  
// AI可能误以为是左上角+尺寸，类似FillRectangle

// ✅ 正确理解
canvas.FillEllipse(centerX, centerY, radiusX, radiusY);  
// 中心坐标 + 半径

// 实例：绘制直径60×40的椭圆，中心在(100, 50)
canvas.FillEllipse(100, 50, 30, 20);  
// 注意：半径是30和20，而非宽60高40

// 如果已知左上角和尺寸，需要这样转换：
float x = 70f, y = 30f;      // 左上角
float width = 60f, height = 40f;
float centerX = x + width / 2;   // 100
float centerY = y + height / 2;  // 50
float radiusX = width / 2;       // 30
float radiusY = height / 2;      // 20
canvas.FillEllipse(centerX, centerY, radiusX, radiusY);
```

### Canvas API与HTML5 Canvas的主要差异

**重要**：WasiCore的Canvas API **没有**直接在Canvas上绘制文字的方法。

如果需要显示文字：
1. **推荐方式**：使用UI系统的 `Label` 控件
2. **替代方式**：使用 `Canvas.DrawPath()` 或 `FillPath()` 模仿文字（复杂）

```csharp
// ❌ 不存在的API
canvas.DrawText("Hello", x, y);  // WasiCore中没有这个方法

// ✅ 正确方式：使用Label控件
var label = new Label
{
    Text = "Hello",
    Position = new Vector2(x, y),
    Parent = canvas
};
```

### 常用Canvas API速查

```csharp
// 矩形绘制（左上角+尺寸）
canvas.FillRectangle(x, y, width, height);
canvas.StrokeRectangle(x, y, width, height);

// 圆形绘制（中心+半径）
canvas.FillCircle(centerX, centerY, radius);
canvas.StrokeCircle(centerX, centerY, radius);

// 椭圆绘制（中心+半径）
canvas.FillEllipse(centerX, centerY, radiusX, radiusY);
canvas.StrokeEllipse(centerX, centerY, radiusX, radiusY);

// 线条绘制
canvas.DrawLine(x1, y1, x2, y2);

// 设置绘制样式
canvas.FillPaint = new SolidPaint(Color.FromArgb(255, r, g, b));
canvas.StrokePaint = new SolidPaint(Color.FromArgb(255, r, g, b));
canvas.StrokeWidth = 2f;
```

---

## 🎮 2D游戏物理系统

### 跳跃系统设计

#### 跳跃高度计算公式

```csharp
// 物理公式：h = v² / (2g)
// 如果希望跳跃高度为 120 像素：
// v = sqrt(2 * g * h)

const float GRAVITY = 2000f;           // 重力加速度（像素/秒²）
const float DESIRED_JUMP_HEIGHT = 120f; // 期望跳跃高度

// 计算所需的跳跃速度
// v = sqrt(2 * 2000 * 120) = 693
// 留点余量：
const float JUMP_VELOCITY = -750f;  // 负值表示向上（Y轴向下为正）

// 在Update中应用
public void Update(float deltaTime)
{
    if (!IsOnGround)
    {
        velocityY += GRAVITY * deltaTime;  // 应用重力
    }
    
    if (InputJump && IsOnGround)
    {
        velocityY = JUMP_VELOCITY;  // 跳跃
        IsOnGround = false;
    }
    
    positionY += velocityY * deltaTime;
    
    // 地面检测
    if (positionY + height >= groundY)
    {
        positionY = groundY - height;
        velocityY = 0;
        IsOnGround = true;
    }
}
```

### 碰撞检测实战

#### 平台碰撞检测（带容差）

```csharp
// ✅ 正确：使用位置范围检测 + 容差
float playerBottom = player.Position.Y + player.Height;
float platformTop = platform.Position.Y;

// 1. 检查水平重叠
bool horizontalOverlap = 
    player.Position.X + player.Width > platform.Position.X &&
    player.Position.X < platform.Position.X + platform.Width;

// 2. 从上方着陆（带容差）
if (player.Velocity.Y >= 0 &&                    // 正在下落
    playerBottom >= platformTop &&               // 底部已经到达平台
    player.Position.Y < platformTop &&           // 顶部还在平台上方
    playerBottom <= platformTop + 20f &&         // 20像素容差
    horizontalOverlap)
{
    // 着陆成功
    player.Position = new Vector2(
        player.Position.X, 
        platformTop - player.Height
    );
    player.Velocity = new Vector2(player.Velocity.X, 0);
    player.IsOnGround = true;
}

// ❌ 错误：假设固定的deltaTime
if (player.Velocity.Y > 0 &&
    player.Position.Y + player.Height - player.Velocity.Y * 0.016f <= platform.Position.Y)
{
    // 这个假设deltaTime=0.016是不可靠的
}
```

**关键点**：
- 先检查水平方向是否重叠
- 使用位置范围而不是速度预测
- 添加容差（20像素）提高鲁棒性
- 不要假设固定的deltaTime

---

## 🎨 渲染系统最佳实践

### 坐标系统设计原则

#### 1. 建立清晰的相对坐标

```csharp
// ✅ 推荐：使用清晰的相对坐标和有意义的变量名
public void DrawPlayer(Canvas canvas, float screenX, float screenY, float height)
{
    // 从上到下定义各部分
    float headCenterY = screenY + height * 0.15f;  // 头部中心：15%
    float bodyTop = screenY + height * 0.35f;      // 身体顶部：35%
    float bodyHeight = height * 0.35f;             // 身体高度：35%
    float legTop = bodyTop + bodyHeight;           // 腿部顶部：70%
    float legHeight = height * 0.3f;               // 腿部高度：30%
    
    // 绘制各部分
    DrawHead(canvas, screenX, headCenterY, height * 0.2f);
    DrawBody(canvas, screenX, bodyTop, height * 0.35f);
    DrawLegs(canvas, screenX, legTop, height * 0.3f);
}

// ❌ 避免：混淆的名称
float bodyBottom = screenY - 5f;  // 实际上是腿部顶部，名称误导
```

#### 2. 严格的绘制顺序（从后到前）

```csharp
public void DrawScene(Canvas canvas)
{
    // 1. 最底层：背景
    DrawBackground(canvas);
    
    // 2. 远景元素
    DrawClouds(canvas);
    
    // 3. 游戏对象（从后到前）
    DrawPlatforms(canvas);
    DrawEnemies(canvas);
    DrawPlayer(canvas);
    
    // 4. 特效层
    DrawParticles(canvas);
    
    // 5. 最顶层：UI
    DrawScore(canvas);
    DrawHealth(canvas);
}

public void DrawCharacter(Canvas canvas, float x, float y, float height)
{
    // 角色内部也要分层
    DrawBody(canvas, x, y);      // 1. 身体
    DrawArms(canvas, x, y);      // 2. 手臂
    DrawHead(canvas, x, y);      // 3. 头部
    DrawFacialFeatures(canvas);  // 4. 面部特征
    DrawHat(canvas, x, y);       // 5. 帽子（最上层）
}
```

### ⚠️ 常见渲染陷阱

#### 陷阱1：形状之间有空隙

**症状**：身体和腿脱节，背景色透出

```csharp
// ❌ 错误：坐标计算不连续
float bodyTop = screenY - 50f;
float bodyBottom = screenY - 5f;  // 这实际是腿部顶部

// 身体：从 screenY-50 到 screenY-25
canvas.FillRectangle(x - 12f, bodyTop, 24f, 25f);

// 腿部：从 screenY-5 到 screenY
canvas.FillRectangle(x - 12f, bodyBottom, 8f, 5f);

// 结果：screenY-25 到 screenY-5 之间有 20 像素空隙！
```

**解决方案**：
```csharp
// ✅ 正确：确保每个部分的底部 = 下一个部分的顶部
float bodyTop = screenY + height * 0.35f;
float bodyHeight = height * 0.35f;
float bodyBottom = bodyTop + bodyHeight;

float pantsHeight = height * 0.3f;
float pantsBottom = bodyBottom + pantsHeight;

float legTop = pantsBottom;  // 腿部从裤子底部开始
float legHeight = height * 0.3f;

// 填充裤子，连接身体和腿部
canvas.FillPaint = new SolidPaint(Color.Blue);
canvas.FillRectangle(x - 12f, bodyBottom, 24f, pantsHeight);
```

#### 陷阱2：角色尺寸变化时脚深入地面

**症状**：角色尺寸变化后，底部穿过地面或悬空

```csharp
// ❌ 错误：修改高度时没有调整位置
public void ChangeSize(float newHeight)
{
    Height = newHeight;  // 直接修改高度
    // 由于位置是顶部坐标，底部位置改变了！
}

// ✅ 正确：保持底部位置不变
public void ChangeSize(float newHeight)
{
    // 1. 记录底部位置
    float bottomY = Position.Y + Height;
    
    // 2. 修改高度
    Height = newHeight;
    
    // 3. 调整位置，保持底部位置不变
    Position = new Vector2(Position.X, bottomY - Height);
}
```

**关键原则**：角色尺寸变化时，应保持脚部（底部）位置不变，通过调整顶部位置来实现尺寸变化。

#### 陷阱3：渲染超出碰撞边界

**症状**：角色一出现脚就在地面以下，视觉与物理不一致

```csharp
// ❌ 错误：比例超过100%
public void DrawPlayer(Canvas canvas, Player player, float height)
{
    float headHeight = height * 0.35f;    // 35%
    float bodyHeight = height * 0.35f;    // 35%
    float pantsHeight = height * 0.3f;    // 30%
    float legHeight = height * 0.3f;      // 30%
    float shoeHeight = height * 0.1f;     // 10%
    // 总计：35% + 35% + 30% + 30% + 10% = 140% ❌
    
    // 绘制各部分...
    // 结果：鞋子底部在 player.Position.Y + height * 1.4，超出碰撞框！
}

// ✅ 正确：确保所有部分在角色高度范围内
public void DrawPlayer(Canvas canvas, Player player, float height)
{
    // 方案1：所有部分总和 = 100%
    float headHeight = height * 0.35f;    // 35%
    float bodyHeight = height * 0.35f;    // 35%
    float legsHeight = height * 0.3f;     // 30%
    // 总计：100% ✓
    
    // 方案2：部分重叠绘制（腿和鞋在下半身内）
    float bodyTop = player.Position.Y + height * 0.35f;
    float bodyHeight = height * 0.35f;
    float pantsTop = bodyTop + bodyHeight;
    float pantsHeight = height * 0.3f;  // 到此 = 100%
    
    // 腿和鞋子在下半身区域内重叠绘制
    DrawLegsInsidePants(canvas, pantsTop, pantsHeight);
    DrawShoesInsidePants(canvas, pantsTop + pantsHeight - height * 0.05f);
}
```

**验证方法**：
```csharp
// 所有渲染部分的最底部必须满足：
float renderBottom = /* 计算所有部分的最低点 */;
Debug.Assert(renderBottom <= player.Position.Y + player.Height, 
    "渲染超出碰撞边界！");
```

---

## 🎮 游戏设计最佳实践

### 游戏平衡性考虑

#### 问题：敌人游荡到玩家初始位置导致开局死亡

**症状**：游戏加载后玩家还未操作就被敌人撞到死亡

**解决方案1 - 设置敌人移动边界**：
```csharp
public class Enemy
{
    // 添加移动边界属性
    public float MinX { get; set; } = 400f;  // 左边界：保护玩家起始区域
    public float MaxX { get; set; } = float.MaxValue;  // 右边界
    
    public void Update(float deltaTime)
    {
        // 更新位置
        Position += Velocity * deltaTime;
        
        // 检查移动边界
        if (Position.X < MinX)
        {
            Position = new Vector2(MinX, Position.Y);
            Velocity = new Vector2(-Velocity.X, Velocity.Y);
            MovingRight = true;
        }
        else if (Position.X + Width > MaxX)
        {
            Position = new Vector2(MaxX - Width, Position.Y);
            Velocity = new Vector2(-Velocity.X, Velocity.Y);
            MovingRight = false;
        }
    }
}
```

**解决方案2 - 给玩家初始无敌时间**：
```csharp
public class Player
{
    public bool IsInvincible { get; private set; }
    private float invincibleTimer;
    
    public void MakeInvincible(float duration)
    {
        IsInvincible = true;
        invincibleTimer = duration;
    }
    
    public void Update(float deltaTime)
    {
        if (IsInvincible)
        {
            invincibleTimer -= deltaTime;
            if (invincibleTimer <= 0)
            {
                IsInvincible = false;
            }
        }
        
        // 其他更新逻辑...
    }
}

public class GameState
{
    public GameState(float gameWidth, float gameHeight)
    {
        // 初始化玩家
        Player = new Player(100, groundY - playerHeight);
        
        // 给玩家2秒的初始无敌保护时间
        Player.MakeInvincible(2f);
    }
    
    public void LoseLife()
    {
        Lives--;
        if (Lives > 0)
        {
            Player = new Player(100, groundY - playerHeight);
            // 重生时也给予无敌保护时间
            Player.MakeInvincible(2f);
        }
    }
}
```

**关键点**：
- 敌人不应该能到达玩家的初始安全区域
- 初始无敌时间应该足够长（2-3秒），让玩家有时间反应
- 重生时也需要无敌保护时间
- 两种方案可以同时使用，提供双重保护

---

## ✅ 2D游戏开发检查清单

开发Canvas 2D游戏前，确保AI工具已理解和遵循：

### 编译和环境
- [ ] ⭐ **使用正确的编译配置**：`dotnet build *.sln -c Client-Debug`
- [ ] ⭐ **所有客户端代码包裹在** `#if CLIENT` 中
- [ ] **已确认GameUI命名空间可用**

### 设计规范
- [ ] ⭐ **使用正确的设计分辨率**：横屏 1920×1080，竖屏 1080×1920
- [ ] **所有游戏元素尺寸基于设计分辨率计算**
- [ ] ⭐ **UI元素考虑了SafeZonePadding**（避免被刘海/圆角/手势区域遮挡）
- [ ] **监听了屏幕方向变化事件**（如果需要适配旋转）

### Canvas API使用
- [ ] **理解椭圆/圆形使用中心+半径**，而非左上角+尺寸
- [ ] **理解矩形使用左上角+尺寸**
- [ ] **不尝试使用不存在的Canvas文字绘制API**
- [ ] **使用Label控件显示文字**

### 渲染系统
- [ ] **设计了清晰的相对坐标系统**
- [ ] **使用有意义的变量名**（如 `bodyTop`, `headCenterY`）
- [ ] **规划了正确的绘制层次**（从后到前）
- [ ] **检查了形状之间是否有空隙**
- [ ] **检查了角色渲染比例之和是否≤100%**（避免脚深入地面）

### 物理系统
- [ ] **如果涉及跳跃，已正确计算跳跃速度**（使用公式 v = sqrt(2gh)）
- [ ] **碰撞检测使用位置范围而非速度预测**
- [ ] **碰撞检测添加了容差**（如20像素）
- [ ] **角色尺寸变化时保持底部位置不变**

### 游戏平衡性
- [ ] **敌人不会游荡到玩家初始位置**（设置移动边界或初始无敌时间）
- [ ] **初始无敌时间足够长**（2-3秒）
- [ ] **重生时有无敌保护时间**

### 项目结构
- [ ] **游戏类文件放在正确的项目目录下**
- [ ] **在 `ScopeData.GameMode.cs` 中注册了游戏模式**
- [ ] **在 `GlobalConfig.cs` 中添加了游戏模式**

---

## 📋 常见错误速查表

| 错误症状 | 可能原因 | 解决方案 |
|---------|---------|---------|
| 3000+编译错误，GameUI找不到 | 未使用Client-Debug配置 | `dotnet build -c Client-Debug` |
| 椭圆/圆形位置不对 | 混淆了中心坐标和左上角 | 使用 `centerX, centerY, radius` |
| 角色各部分脱节有空隙 | 坐标计算不连续 | 确保每部分底部=下部分顶部 |
| 角色脚深入地面 | 渲染比例>100% | 所有部分比例总和≤100% |
| 跳不上平台 | 跳跃速度不足 | 用公式计算：v=sqrt(2gh) |
| 玩家穿透平台 | 碰撞检测逻辑错误 | 使用位置范围+容差检测 |
| 角色变大时脚深入地面 | 尺寸变化时位置未调整 | 保持底部位置不变 |
| 地面在画面中部而非底部 | 使用了动态分辨率计算 | 使用固定设计分辨率 |
| 开局就死亡 | 敌人到达初始位置 | 设置敌人边界或初始无敌 |
| UI按钮被刘海/圆角遮挡 | 未考虑SafeZonePadding | 将UI放在安全区域内 |
| 底部按钮误触系统手势 | 按钮太靠近屏幕边缘 | 使用SafeZonePadding留出边距 |

---

## 🎓 最佳实践总结

### 编译和环境
1. **⭐ 编译配置**：始终使用 `-c Client-Debug` 编译
2. **⭐ 条件编译**：所有客户端代码包裹在 `#if CLIENT` 中

### 设计规范
3. **⭐ 设计分辨率**：横屏 1920×1080，竖屏 1080×1920（这一设计分辨率是通用的，不需要动态计算）
4. **相对尺寸**：所有元素尺寸使用设计分辨率的百分比

### API使用
5. **API使用**：永远先查文档，不要假设
6. **文字显示**：使用Label控件，不要尝试Canvas.DrawText

### 渲染系统
7. **坐标系统**：建立清晰的相对坐标，使用有意义的变量名
8. **绘制顺序**：严格从后到前，分层绘制
9. **视觉检查**：代码正确≠视觉正确，需要实际运行测试

### 物理系统
10. **测试驱动**：先计算物理参数，确保游戏可玩性
11. **容差设计**：碰撞检测使用容差提高鲁棒性

### 游戏设计
12. **游戏平衡性**：保护玩家初始体验，给予足够的反应时间

---

## 📚 相关文档

### 必读文档
- [AI开发指导文档](AI_DEVELOPMENT_GUIDE.md) - 框架通用开发指南
- [AI友好的UI API](AI_FRIENDLY_UI_API.md) - UI系统使用指南

### 参考文档
- [框架概述](../../FRAMEWORK_OVERVIEW.md) - 框架核心概念
- [坐标系统指南](../../COORDINATE_SYSTEM_GUIDE.md) - 3D坐标系统说明
- API文档：`api/client/GameUI.Control.xml` - Canvas API详细文档

---

## 💡 AI工具使用提示

**当AI工具遇到Canvas 2D游戏开发问题时**：

### 编译问题
1. 首先检查是否使用了 `-c Client-Debug` 编译配置
2. 确认所有客户端代码包裹在 `#if CLIENT` 中

### 视觉问题
1. 检查API参数是否正确（中心 vs 左上角，半径 vs 尺寸）
2. 检查坐标计算是否连续（是否有空隙）
3. 检查绘制顺序是否正确（从后到前）
4. 检查渲染比例是否≤100%

### 物理问题
1. 使用公式计算跳跃速度：v = sqrt(2 * g * h)
2. 碰撞检测使用位置范围 + 容差
3. 角色尺寸变化时保持底部位置不变

### 游戏平衡性问题
1. 设置敌人移动边界保护初始区域
2. 给予玩家初始和重生无敌时间（2-3秒）

---

> 💡 **记住**：将本文档的相关部分提供给AI工具，可以帮助AI更好地理解Canvas 2D游戏开发的特殊要求，避免常见错误。

> 📖 **注意**：本文档是 [AI开发指导文档](AI_DEVELOPMENT_GUIDE.md) 的专项补充，两者应配合使用。

