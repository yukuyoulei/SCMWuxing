# 快速开始指南

本指南将帮助您快速上手 WasiCore 游戏框架，创建您的第一个游戏项目。

## 🎯 学习目标

完成本指南后，您将能够：

- 设置 WasiCore 开发环境
- 创建一个简单的游戏实体
- 使用属性系统管理游戏数据
- 运行和测试您的游戏逻辑

## 📋 前提条件

- .NET 9.0 SDK
- Visual Studio 2022 (17.14+) 或 VS Code
- 基础的 C# 编程知识

## 🔍 重要提示

### 日志系统使用

WasiCore 框架**禁止使用 `Console.WriteLine`**，必须使用框架提供的日志系统：

```csharp
// ✅ 正确：使用参数化消息（结构化日志）
Game.Logger.LogInformation("操作成功: {OperationType}", operationType);
Game.Logger.LogWarning("注意事项: {Message}", warningMessage);
Game.Logger.LogError("发生错误: {ErrorCode}", errorCode);

// ❌ 错误：禁止使用字符串内插
Game.Logger.LogInformation($"操作成功: {operationType}");

// ❌ 错误：禁止使用 Console.WriteLine
Console.WriteLine("这不会正常工作");
```

#### 为什么使用参数化消息？

- **性能优化**：只有在日志级别启用时才会执行参数格式化
- **结构化日志**：支持日志聚合和搜索分析
- **内存效率**：避免不必要的字符串分配

## 🚀 环境设置

### 1. 克隆项目

```bash
git clone <repository-url>
cd WasiCore
```

### 2. 构建解决方案

```bash
# 构建服务器调试版本
dotnet build *.sln -c Server-Debug

# 构建客户端调试版本  
dotnet build *.sln -c Client-Debug
```

### 3. 运行测试

```bash
dotnet test *.sln
```

如果所有测试通过，说明环境设置成功！

## 🛠️ 理解自动注册机制

在创建游戏功能之前，了解 WasiCore 的自动注册机制非常重要。

### IGameClass 自动注册

⚡ **核心概念**：实现 `IGameClass` 接口的类会被框架**自动注册**，无需手动调用。

```csharp
using GameCore.BaseInterface;

public class MyGameFeature : IGameClass
{
    // ✅ 正确：这个方法会被框架自动调用
    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
        Game.Logger.LogInformation("🎮 MyGameFeature registered automatically");
    }
    
    private static void OnGameDataInitialization()
    {
        // 在这里初始化游戏数据
        Game.Logger.LogInformation("📊 Game data initialized");
    }
}
```

**重要提醒**：
- **自动调用**：`OnRegisterGameClass()` 由框架自动调用，无需手动调用
- **编译时处理**：代码生成器在编译时自动扫描和注册
- **静态方法**：必须是静态方法，在类实例化之前调用

### ❌ 常见错误

```csharp
// ❌ 错误：手动调用（框架已自动处理）
public static void OnRegisterGameClass()
{
    MyGameFeature.OnRegisterGameClass(); // 不要这样做！
}

// ❌ 错误：非静态方法
public void OnRegisterGameClass() // 必须是静态的
{
    // 这不会被调用
}
```

## 🎮 创建第一个游戏实体

### 1. 定义游戏单位

让我们创建一个简单的战士单位：

```csharp
// 在 GameCore 项目中创建 Units/Warrior.cs
using GameCore.Components;
using GameCore.Entity;

namespace GameCore.Units
{
    public class Warrior : Unit
    {
        public Warrior() : base()
        {
            // 初始化战士的基础属性
            InitializeWarrior();
        }
        
        private void InitializeWarrior()
        {
            // 使用基础属性系统
            this.SetProperty(PropertyUnit.Name, "勇敢的战士");
            this.SetProperty(PropertyUnit.Level, 1);
            
            // 使用数值公式属性系统
            var propertyComplex = this.GetOrCreateComponent<UnitPropertyComplex>();
            propertyComplex.SetFixed(PropertyUnitNumeric.Health, PropertySubType.Base, 100.0);
            propertyComplex.SetFixed(PropertyUnitNumeric.AttackPower, PropertySubType.Base, 15.0);
        }
    }
}
```

## 🎨 创建简单UI

WasiCore 提供了强大的UI系统。让我们创建一个简单的按钮：

### UI模板定义 vs 运行时控件

⚠️ **重要概念**：UI属性的访问方式在模板定义和运行时控件中不同。

**方式1：在GameData中定义UI模板**
```csharp
// 在 ScopeData.cs 中定义UI模板
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

**方式2：运行时动态创建控件**
```csharp
#if CLIENT
// 在客户端代码中动态创建
var button = new Button
{
    Width = 200,        // 直接属性
    Height = 50,        // 直接属性
    HorizontalAlignment = HorizontalAlignment.Center,  // 直接属性
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

**关键差异总结**：
- **模板方式**：`Layout.Width`、预定义结构化数据
- **运行时方式**：`Width`（直接属性）、动态创建和事件订阅

选择哪种方式取决于您的具体需求：
- **模板方式**：适用于静态UI、复杂布局、可重用组件
- **运行时方式**：适用于动态UI、游戏内UI、响应式界面

## 📚 下一步

现在您已经掌握了基础知识，可以继续学习：

- [指令便利API](../systems/CommandAPI.md) - 学习简洁易用的指令API
- [指令系统详解](../systems/OrderSystem.md) - 深入了解底层指令机制
- [单位属性系统详解](../systems/UnitPropertySystem.md) - 深入了解属性系统
- [UI属性系统详解](../systems/UIPropertySystem.md) - 学习UI状态管理
- [异步编程最佳实践](../best-practices/AsyncProgramming.md) - 学习异步编程模式
- [常见陷阱](../best-practices/CommonPitfalls.md) - 避免开发中的常见问题 