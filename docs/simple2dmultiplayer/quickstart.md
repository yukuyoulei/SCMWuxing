# Simple2DMultiplayerGame - 5分钟快速开始

## 🎯 目标

5分钟内创建一个可运行的多人 Pong 游戏，理解框架的核心概念。

**最终效果**:
- ✅ 2个玩家，各自控制一个挡板
- ✅ 球在屏幕中移动并反弹
- ✅ 碰到挡板改变方向
- ✅ 实时同步所有玩家的状态

**代码量**: 约 80 行

---

## 📝 步骤 1：创建文件结构（30秒）

```
MyPongGame/
├── MyPongGameData.cs      # GameMode 定义
├── MyPongGame.cs          # 主类和枚举
├── MyPongGame.Server.cs   # 服务端逻辑
└── MyPongGame.Client.cs   # 客户端渲染
```

---

## 📝 步骤 2：定义 GameMode（1分钟）

**文件**: `MyPongGameData.cs`

```csharp
using GameCore.GameSystem.Data;
using GameData;

namespace MyGame;

public class MyPongGameData : IGameClass
{
    public static class GameMode
    {
        public static readonly GameLink<GameDataGameMode, GameDataGameMode> 
            MyPong = new("MyPong"u8);
    }
    
    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += () =>
        {
            _ = new GameDataGameMode(GameMode.MyPong)
            {
                Name = "My Pong Game",
                Gameplay = GameCore.ScopeData.Gameplay.Default,
                SceneList = [],  // 2D游戏不需要场景
                PlayerSettings = GameEntry.ScopeData.GameDataPlayerSettings.PlayerSettings,
            };
        };
    }
}
```

---

## 📝 步骤 3：创建主类（1分钟）

**文件**: `MyPongGame.cs`

```csharp
using EngineInterface.BaseType;
using TriggerEncapsulation.GameTemplates;

namespace MyGame;

// 定义属性枚举
[PropertyObjectWrapper]  // 🔥 自动生成包装器
[EnumExtension(Extendable = true)]
public enum EPropertyPaddle
{
    PlayerId,    // → int
    PositionY,   // → float
}

[PropertyObjectWrapper]
[EnumExtension(Extendable = true)]
public enum EPropertyBall
{
    PositionX,   // → float
    PositionY,   // → float
    VelocityX,   // → float
    VelocityY,   // → float
}

// 主游戏类
public partial class MyPongGame : RealtimeActionGameTemplate<MyPongGame>
{
    // Category 定义
    private const int CategoryPaddle = 1;
    private const int CategoryBall = 2;
    
    // 消息类型
    private enum MessageType : byte
    {
        MovePaddle = 1,
    }
    
    // GameMode 检查
    protected override bool ShouldInitialize()
    {
        return Game.GameModeLink == MyPongGameData.GameMode.MyPong;
    }
}
```

---

## 📝 步骤 4：实现服务端（1.5分钟）

**文件**: `MyPongGame.Server.cs`

```csharp
using EngineInterface.BaseType;

namespace MyGame;

public partial class MyPongGame
{
#if SERVER
    private Ball? ball;
    private readonly List<Paddle> paddles = new();
    
    protected override void OnServerInitialize()
    {
        base.OnServerInitialize();
        
        // 创建球（随机方向）
        var ballObj = CreateGameObject(Player.DefaultPlayer, SyncType.All);
        ballObj.Category = CategoryBall;
        ball = new Ball(ballObj);
        ball.PositionX = 400f;
        ball.PositionY = 300f;
        
        // 随机左右方向
        var directionX = Random.Shared.Next(2) == 0 ? -1 : 1;
        var directionY = Random.Shared.Next(2) == 0 ? -1 : 1;
        ball.VelocityX = 200f * directionX;
        ball.VelocityY = 150f * directionY;
        
        // 注册消息
        RegisterMessageHandler((byte)MessageType.MovePaddle, OnMovePaddle);
    }
    
    protected override void OnPlayerJoined(Player player)
    {
        base.OnPlayerJoined(player);
        
        // 为玩家创建挡板
        var paddleObj = CreateGameObject(player, SyncType.All);
        paddleObj.Category = CategoryPaddle;
        var paddle = new Paddle(paddleObj);
        paddle.PlayerId = player.Id;
        paddle.PositionY = 300f;
        paddles.Add(paddle);
    }
    
    protected override void OnRealtimeServerTick(float deltaTime)
    {
        if (ball == null) return;
        
        // 更新球的位置
        ball.PositionX += ball.VelocityX * deltaTime;
        ball.PositionY += ball.VelocityY * deltaTime;
        
        // 上下边界反弹
        if (ball.PositionY < 0 || ball.PositionY > 600)
        {
            ball.VelocityY = -ball.VelocityY;
            ball.PositionY = Math.Clamp(ball.PositionY, 0f, 600f);
        }
        
        // 检测挡板碰撞
        CheckPaddleCollision();
        
        // 左右出界重置（随机方向）
        if (ball.PositionX < 0 || ball.PositionX > 800)
        {
            ball.PositionX = 400f;
            ball.PositionY = 300f;
            
            // 随机新方向
            var directionX = Random.Shared.Next(2) == 0 ? -1 : 1;
            var directionY = Random.Shared.Next(2) == 0 ? -1 : 1;
            ball.VelocityX = 200f * directionX;
            ball.VelocityY = 150f * directionY;
        }
    }
    
    private void CheckPaddleCollision()
    {
        if (ball == null) return;
        
        const float ballRadius = 10f;
        const float paddleWidth = 10f;
        const float paddleHeight = 100f;
        
        foreach (var paddle in paddles)
        {
            var paddleX = paddle.PlayerId == 1 ? 50f : 750f;
            var paddleY = paddle.PositionY;
            
            // AABB 碰撞检测
            var paddleLeft = paddleX;
            var paddleRight = paddleX + paddleWidth;
            var paddleTop = paddleY - paddleHeight / 2;
            var paddleBottom = paddleY + paddleHeight / 2;
            
            var ballLeft = ball.PositionX - ballRadius;
            var ballRight = ball.PositionX + ballRadius;
            var ballTop = ball.PositionY - ballRadius;
            var ballBottom = ball.PositionY + ballRadius;
            
            if (ballRight >= paddleLeft && ballLeft <= paddleRight &&
                ballBottom >= paddleTop && ballTop <= paddleBottom)
            {
                // 碰撞！反弹
                ball.VelocityX = -ball.VelocityX;
                
                // 防止球卡在挡板里
                if (ball.VelocityX > 0)
                    ball.PositionX = paddleRight + ballRadius;
                else
                    ball.PositionX = paddleLeft - ballRadius;
                
                // 根据击球位置调整Y速度
                var hitOffset = (ball.PositionY - paddleY) / (paddleHeight / 2);
                ball.VelocityY += hitOffset * 50f;
                
                break;
            }
        }
    }
    
    private void OnMovePaddle(Player player, byte[] payload)
    {
        if (payload.Length < 4) return;
        
        var targetY = BitConverter.ToSingle(payload, 0);
        var paddle = paddles.FirstOrDefault(p => p.PlayerId == player.Id);
        if (paddle != null)
        {
            paddle.PositionY = Math.Clamp(targetY, 50f, 550f);
        }
    }
#endif
}
```

---

## 📝 步骤 5：实现客户端（1分钟）

**文件**: `MyPongGame.Client.cs`

```csharp
#if CLIENT
using GameUI.Control.Primitive;
using GameUI.Graphics;
using GameUI.Brush;
using GameUI.Control.Extensions;  // AddToRoot 等流式扩展
using System.Drawing;

namespace MyGame;

public partial class MyPongGame
{
    private Canvas? canvas;
    
    protected override void OnClientInitialize()
    {
        canvas = new Canvas { Width = 800, Height = 600 };
        canvas.AddToRoot();  // 🚨 必须调用（需要 GameUI.Control.Extensions）
        
        // 按下时捕获指针
        canvas.OnPointerPressed += (s, e) =>
        {
            canvas.CapturePointer(e.PointerButtons);  // 传入按键 mask
        };
        
        // 拖拽控制挡板（需要先捕获）
        canvas.OnPointerCapturedMove += (s, e) =>
        {
            var y = e.PointerPosition?.Top ?? 300f;  // UIPosition 使用 Top
            var bytes = BitConverter.GetBytes(y);
            SendMessageToServer((byte)MessageType.MovePaddle, bytes);
        };
        
        // 释放时取消捕获
        canvas.OnPointerReleased += (s, e) =>
        {
            canvas.ReleasePointer(e.PointerButtons);  // 传入按键 mask
        };
    }
    
    protected override void OnClientRender(float deltaTime)  // ✅ 框架传入 deltaTime
    {
        if (canvas == null) return;
        
        canvas.ResetState();
        
        // 背景
        canvas.FillPaint = new SolidPaint(Color.Black);
        canvas.FillRectangle(0, 0, 800, 600);
        
        // 绘制所有对象
        foreach (var obj in AllPropertyObjects)
        {
            if (!obj.IsValid) continue;
            
            if (obj.Category == CategoryPaddle)
            {
                var paddle = new Paddle(obj);
                var x = paddle.PlayerId == 1 ? 50f : 750f;
                canvas.FillPaint = new SolidPaint(Color.White);
                canvas.FillRectangle(x, paddle.PositionY - 50, 10, 100);
            }
            else if (obj.Category == CategoryBall)
            {
                var ball = new Ball(obj);
                canvas.FillPaint = new SolidPaint(Color.Yellow);
                canvas.FillCircle(ball.PositionX, ball.PositionY, 10);
            }
        }
    }
}
#endif
```

---

## 📝 步骤 6：注册游戏模式（30秒）

**文件**: `Tests/Game/GlobalConfig.cs`

```csharp
public class GlobalConfig : IGameClass
{
    public static void OnRegisterGameClass()
    {
        GameDataGlobalConfig.AvailableGameModes = new()
        {
            // ... 现有游戏模式
            {"MyPong", MyGame.MyPongGameData.GameMode.MyPong},  // ← 添加这行
        };
        
        // 可选：设置为测试模式
        GameDataGlobalConfig.TestGameMode = MyGame.MyPongGameData.GameMode.MyPong;
    }
}
```

---

## 🚀 步骤 7：运行游戏（10秒）

```bash
# 编译
dotnet build *.sln -c Server-Debug

# 运行
# 启动游戏并选择 "MyPong" 模式
```

**完成！** 🎉

你已经创建了一个完整的多人联机游戏！

---

## 📊 代码量统计

| 文件 | 行数 | 说明 |
|------|------|------|
| MyPongGameData.cs | 28 | GameMode 定义 |
| MyPongGame.cs | 42 | 枚举和常量 |
| MyPongGame.Server.cs | 144 | 服务端逻辑（含碰撞检测）|
| MyPongGame.Client.cs | 84 | 客户端渲染 |
| **总计** | **~298** | 包含空行和注释 |
| **纯代码** | **~200** | 实际逻辑代码 |

**如果不使用框架**: 估计需要 300+ 行

---

## 🎯 下一步

### 添加功能

1. **计分系统** - 球出界时给对方加分
2. **游戏结束** - 先到5分获胜
3. **重新开始** - 添加重启按钮
4. **多个球** - 增加游戏难度

### 参考示例

查看 `FlappyBirdMultiplayer` 了解如何实现：
- ✅ 计分和排行榜
- ✅ 游戏结束和重启
- ✅ 配置系统
- ✅ UI 优化

### 学习路径

1. **5分钟** - 完成本教程（Pong 游戏）
2. **15分钟** - 添加计分和游戏结束
3. **1小时** - 阅读 FlappyBird 示例代码
4. **2-3小时** - 创建自己的游戏

---

## ✅ 检查清单

完成本教程后，检查是否理解：

- [ ] 如何选择模板基类（Realtime vs TurnBased）
- [ ] 如何定义 PropertyObjectWrapper
- [ ] Category 的作用
- [ ] 服务端和客户端的职责分离
- [ ] 如何发送和接收消息
- [ ] PropertyObject 的同步机制

---

**恭喜！你已经掌握了 Simple2DMultiplayerGame 框架的基础！** 🎉

下一步：阅读 [Framework.md](./Framework.md) 了解更多高级特性。

