# WasiCore 编码规范

## 📝 命名规范

### 通用规则

- **语言**：所有代码、注释、文档使用中文
- **编码**：UTF-8 无 BOM
- **文件名**：PascalCase，与主要类名一致

### 命名约定

#### 接口 (Interface)

```csharp
// ✅ 正确：以 I 开头
public interface IEntity { }
public interface IExecutableObject { }

// ❌ 错误
public interface Entity { }
public interface ExecutableObject { }
```

#### 类 (Class) 和 结构体 (Struct)

```csharp
// ✅ 正确：PascalCase
public class GameManager { }
public struct Vector3 { }

// ❌ 错误
public class gameManager { }
public struct vector3 { }
```

#### 方法 (Method)

```csharp
// ✅ 正确：PascalCase，动词开头
public void Update() { }
public CmdResult Execute() { }
public bool CanCast() { }

// ❌ 错误
public void update() { }
public void DoUpdate() { }  // 避免 Do 前缀
```

#### 属性 (Property)

```csharp
// ✅ 正确：PascalCase，名词
public string Name { get; set; }
public bool IsAlive { get; }
public float Health { get; set; }

// ❌ 错误
public string name { get; set; }
public bool alive { get; }  // 应该用 IsAlive
```

#### 字段 (Field)

```csharp
// ✅ 正确：私有字段 camelCase
private int health;
private readonly string name;

// ✅ 正确：公共常量 PascalCase
public const int MaxHealth = 100;

// ✅ 正确：静态只读字段 PascalCase
public static readonly Vector3 Zero = new(0, 0, 0);

// ❌ 错误
private int Health;  // 私有字段应该 camelCase
private int _health;  // 不使用下划线前缀
```

#### 参数 (Parameter) 和 局部变量

```csharp
// ✅ 正确：camelCase
public void SetPosition(float x, float y, float z)
{
    var position = new Vector3(x, y, z);
    float distance = CalculateDistance(position);
}

// ❌ 错误
public void SetPosition(float X, float Y, float Z) { }
```

#### 枚举 (Enum)

```csharp
// ✅ 正确：类型名 PascalCase，值 PascalCase
public enum EntityState
{
    Idle,
    Moving,
    Attacking,
    Dead
}

// 标志枚举使用复数
[Flags]
public enum RoutedEvents
{
    None = 0,
    PointerClicked = 1,
    PointerPressed = 2,
    PointerReleased = 4
}
```

#### 命名空间 (Namespace)

```csharp
// ✅ 正确：PascalCase，与文件夹结构对应
namespace GameCore.AbilitySystem;
namespace GameUI.Control.Primitive;

// ❌ 错误
namespace gamecore.abilitysystem;
namespace GameCore.Ability_System;
```

### 特殊命名规范

#### 异步方法

```csharp
// ✅ 正确：以 Async 结尾
public async Task<bool> ConnectAsync() { }
public async Task LoadSceneAsync() { }

// ❌ 错误
public async Task<bool> Connect() { }
public async Task LoadScene() { }
```

#### 泛型类型参数

```csharp
// ✅ 正确：T 开头，描述性名称
public class Cache<TKey, TValue> { }
public interface IFactory<TProduct> where TProduct : IProduct { }

// ❌ 错误
public class Cache<K, V> { }  // 应该用 TKey, TValue
```

## 🎨 代码风格

### 缩进和空格

- 使用 **4 个空格**缩进，不使用 Tab
- 运算符两侧加空格
- 逗号后加空格
- 方法调用的括号前不加空格

```csharp
// ✅ 正确
int result = a + b * c;
CallMethod(param1, param2, param3);
if (condition)
{
    DoSomething();
}

// ❌ 错误
int result=a+b*c;
CallMethod (param1,param2,param3);
if(condition){
    DoSomething();
}
```

### 大括号

- 始终使用大括号，即使只有一行代码
- 大括号独占一行（Allman 风格）

```csharp
// ✅ 正确
if (condition)
{
    DoSomething();
}

foreach (var item in collection)
{
    ProcessItem(item);
}

// ❌ 错误
if (condition) DoSomething();

if (condition) {
    DoSomething();
}
```

### 语句和表达式

#### var 使用

```csharp
// ✅ 正确：类型明显时使用 var
var list = new List<string>();
var name = GetName();
var count = list.Count;

// ✅ 正确：类型不明显时显式声明
IEntity entity = GetEntity();
float distance = CalculateDistance();

// ❌ 错误
List<string> list = new List<string>();  // 类型重复
var x = 42;  // 类型不明显
```

#### using 语句

```csharp
// ✅ 正确：文件作用域命名空间
namespace GameCore.AbilitySystem;

// ✅ 正确：using 声明
using var stream = File.OpenRead(path);

// ✅ 正确：global using（在项目文件中）
global using System;
global using GameCore.BaseInterface;
```

#### 模式匹配

```csharp
// ✅ 正确：使用模式匹配
if (obj is Entity entity)
{
    entity.Update();
}

var result = state switch
{
    EntityState.Idle => "空闲",
    EntityState.Moving => "移动中",
    EntityState.Dead => "死亡",
    _ => "未知"
};

// ❌ 错误
if (obj.GetType() == typeof(Entity))
{
    var entity = (Entity)obj;
    entity.Update();
}
```

### 日志系统规范

WasiCore 框架提供统一的日志系统，**禁止使用 `Console.WriteLine`**，必须使用框架提供的日志方法。

**重要**：必须使用**参数化消息**而不是字符串内插，这是现代日志系统的最佳实践。

```csharp
// ✅ 正确：使用参数化消息
Game.Logger.LogInformation("玩家 {PlayerName} 执行了 {Action}", playerName, action);
Game.Logger.LogWarning("资源不足: {ResourceType}, 当前: {Current}, 需要: {Required}", 
    resourceType, currentAmount, requiredAmount);
Game.Logger.LogError(exception, "操作失败: {Operation}", operationName);
Game.Logger.LogDebug("调试信息: {Value}", debugValue);

// ❌ 错误：使用字符串内插（性能和结构化问题）
Game.Logger.LogInformation($"玩家 {playerName} 执行了 {action}");

// ❌ 错误：禁止使用 Console.WriteLine
Console.WriteLine("这不会在引擎中正常工作");
```

#### 日志级别说明

- **LogInformation**：一般信息记录，如操作成功、状态变化
- **LogWarning**：警告信息，如非致命错误、性能问题
- **LogError**：错误信息，如异常、操作失败
- **LogDebug**：调试信息，仅在开发模式下输出

#### 日志最佳实践

```csharp
// 使用参数化消息提供详细信息
Game.Logger.LogInformation("玩家 {PlayerName} 已连接到服务器", playerName);

// 错误日志应包含足够的上下文信息
Game.Logger.LogError("属性设置失败: Property={PropertyName}, Value={Value}, Reason={ErrorReason}", 
    property.Name, value, errorReason);

// 在性能敏感的代码中，使用条件检查
#if DEBUG
Game.Logger.LogDebug("详细的调试信息: {ComplexData}", complexData);
#endif

// 避免在循环中频繁记录日志
foreach (var item in items)
{
    // ❌ 错误：可能产生大量日志
    // Game.Logger.LogInformation("处理项目: {ItemName}", item.Name);
}

// ✅ 正确：记录汇总信息
Game.Logger.LogInformation("批量处理完成，共处理 {ItemCount} 个项目", items.Count);
```

### 注释规范

#### XML 文档注释

```csharp
/// <summary>
/// 执行技能效果
/// </summary>
/// <param name="caster">施法者</param>
/// <param name="target">目标</param>
/// <returns>执行结果</returns>
/// <remarks>
/// 此方法会先验证执行条件，然后执行效果链。
/// 如果验证失败，将返回错误结果。
/// </remarks>
public CmdResult Execute(Entity caster, ITarget target)
{
    // 实现代码
}
```

#### 行内注释

```csharp
// ✅ 正确：解释为什么，而不是做什么
// 使用弱引用避免循环引用
private WeakReference<Entity> parentRef;

// 延迟初始化以提高启动性能
private Lazy<List<Component>> components;

// ❌ 错误：显而易见的注释
// 增加计数器
count++;

// 设置名称
Name = name;
```

## 🎯 框架特定约定

### 序列化限制 ⚠️

WasiCore 框架使用 System.Text.Json 进行网络序列化，存在以下重要限制：

#### 不支持的数据类型

```csharp
// ❌ 错误：二维数组无法序列化
public class GameData
{
    public PieceType[,] Board { get; set; }  // 会导致序列化错误
}

// ✅ 正确：使用一维数组 + 辅助方法
public class GameData
{
    public PieceType[] Board { get; set; }   // 支持序列化
    public int BoardWidth { get; set; }
    public int BoardHeight { get; set; }
    
    // 提供辅助方法访问
    public PieceType GetPiece(int row, int col)
    {
        return Board[row * BoardWidth + col];
    }
    
    public void SetPiece(int row, int col, PieceType piece)
    {
        Board[row * BoardWidth + col] = piece;
    }
}
```

#### 其他序列化限制

```csharp
// ❌ 避免：复杂的嵌套泛型
public Dictionary<string, Dictionary<int, List<SomeClass>>> ComplexData { get; set; }

// ❌ 避免：循环引用
public class Parent
{
    public Child Child { get; set; }
}
public class Child
{
    public Parent Parent { get; set; }  // 循环引用
}

// ✅ 推荐：简单的数据结构
public class SimpleData
{
    public string Name { get; set; }
    public int Value { get; set; }
    public List<string> Items { get; set; }
}
```

#### 最佳实践

1. **优先使用基础类型**：string, int, float, bool
2. **使用一维数组**：而不是多维数组
3. **避免循环引用**：使用ID引用而不是对象引用
4. **测试序列化**：在开发过程中测试数据结构的序列化

### 玩家系统约定

WasiCore 框架提供了内置的玩家系统，开发者需要了解以下重要概念：

#### 玩家ID获取

```csharp
// ❌ 错误：硬编码玩家ID
private int _localPlayerId = 1;  // 所有客户端都会是1

// ✅ 正确：动态获取本地玩家ID
private int _localPlayerId => Player.LocalPlayer.Id;

// ✅ 正确：在需要时获取
public void ShowPlayerInfo()
{
    // 直接使用对象的 ToString()
    Game.Logger.LogInformation("玩家 {Player} 正在游戏，用户: {User}", 
        Player.LocalPlayer, Player.LocalPlayer.User);
}
```

#### 玩家事件监听

```csharp
public static void OnRegisterGameClass()
{
    Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
}

private static void OnGameTriggerInitialization()
{
    // ✅ 正确：监听玩家连接事件
    var connectTrigger = new Trigger<EventPlayerUserConnected>(OnPlayerConnected);
    connectTrigger.Register(Game.Instance);
    
    var disconnectTrigger = new Trigger<EventPlayerUserDisconnected>(OnPlayerDisconnected);
    disconnectTrigger.Register(Game.Instance);
}

private static void OnPlayerConnected(EventPlayerUserConnected e)
{
    Game.Logger.LogInformation("玩家连接: {PlayerId}", e.Player.Id);
}

private static void OnPlayerDisconnected(EventPlayerUserDisconnected e)
{
    Game.Logger.LogInformation("玩家断开: {PlayerId}", e.Player.Id);
}
```

#### 玩家状态管理

```csharp
// ✅ 正确：检查玩家状态
public bool IsLocalPlayer(Player player)
{
    return player.Id == Player.LocalPlayer.Id;
}

// ✅ 正确：获取所有在线玩家
public List<Player> GetOnlinePlayers()
{
    return Game.Instance.GetPlayers()
        .Where(p => p.IsConnected)
        .ToList();
}
```

### IGameClass 自动注册约定

WasiCore 框架使用代码生成器自动注册实现 `IGameClass` 接口的类，开发者需要遵循以下约定：

#### ✅ 正确实现

```csharp
using GameCore.BaseInterface;

public class MyGameSystem : IGameClass
{
    // ✅ 必须是静态方法
    public static void OnRegisterGameClass()
    {
        // ✅ 使用 Game 事件进行初始化
        Game.OnGameDataInitialization += OnGameDataInitialization;
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
        
        // ✅ 使用框架日志系统
        Game.Logger.LogInformation("🎮 {SystemName} registered", nameof(MyGameSystem));
    }
    
    private static void OnGameDataInitialization()
    {
        // 在这里初始化游戏数据
    }
    
    private static void OnGameTriggerInitialization()
    {
        // 在这里注册触发器和事件处理器
    }
}
```

#### ❌ 常见错误

```csharp
public class BadGameSystem : IGameClass
{
    // ❌ 错误：非静态方法
    public void OnRegisterGameClass() { }
    
    // ❌ 错误：私有或内部方法
    private static void OnRegisterGameClass() { }
    internal static void OnRegisterGameClass() { }
    
    // ❌ 错误：手动调用其他类的注册方法
    public static void OnRegisterGameClass()
    {
        OtherGameSystem.OnRegisterGameClass(); // 框架会自动处理
    }
}
```

#### 🔄 初始化顺序

遵循框架定义的初始化顺序：

```csharp
public static void OnRegisterGameClass()
{
    // 1. OnRegisterAssemblySetup (自动处理)
    // 2. OnGameDataInitialization - 数据初始化
    Game.OnGameDataInitialization += () =>
    {
        // 创建游戏数据对象
        // 设置默认配置
    };
    
    // 3. OnGameplaySettingsInitialization - 游戏玩法设置
    Game.OnGameplaySettingsInitialization += (gameMode) =>
    {
        // 根据游戏模式初始化
    };
    
    // 4. OnGameInstanceInitialization - 游戏实例初始化
    Game.OnGameInstanceInitialization += () =>
    {
        // 创建游戏实例
    };
    
    // 5. OnGameTriggerInitialization - 触发器初始化
    Game.OnGameTriggerInitialization += () =>
    {
        if (Game.GameModeLink != ScopeData.GameMode.MyGameMode)
        {
            return; // 只在特定游戏模式下初始化
        }
        
        // 注册事件处理器和触发器
    };
}
```

#### 🎮 游戏模式条件初始化

```csharp
public static void OnRegisterGameClass()
{
    Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
}

private static void OnGameTriggerInitialization()
{
    // ✅ 正确：检查游戏模式
    if (Game.GameModeLink != ScopeData.GameMode.MyGameMode)
    {
        return; // 只在特定模式下初始化
    }
    
    // 注册特定于游戏模式的功能
    var trigger = new Trigger<EventGameStart>(OnGameStart);
    trigger.Register(Game.Instance);
}
```

#### ⚠️ 重要提醒

- **自动调用**：`OnRegisterGameClass()` 由代码生成器自动调用，无需手动调用
- **编译时注册**：所有 `IGameClass` 实现在编译时被扫描并注册
- **幂等性**：确保方法可以安全地多次调用（虽然框架会避免重复调用）
- **静态性质**：所有相关方法必须是静态的，因为在类实例化之前调用
- **日志使用**：使用 `Game.Logger` 而不是 `Console.WriteLine`

## 🏗️ 设计模式

### 1. 组件模式 (Component Pattern)

用于实体系统，实现功能的模块化和复用。

```csharp
public abstract class Component : IComponent
{
    public Entity Owner { get; private set; }
    public bool Enabled { get; set; } = true;
    
    public virtual void OnAttached() { }
    public virtual void OnDetached() { }
    public virtual void Update(float deltaTime) { }
}

// 使用示例
public class HealthComponent : Component
{
    public float MaxHealth { get; set; } = 100;
    public float CurrentHealth { get; private set; }
    
    public void TakeDamage(float damage)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
        if (CurrentHealth == 0)
        {
            Owner.Destroy();
        }
    }
}
```

### 2. 命令模式 (Command Pattern)

用于技能和动作系统，支持撤销和重做。

```csharp
public interface ICommand
{
    CmdResult Execute();
    CmdResult Undo();
    bool CanExecute();
}

public class MoveCommand : ICommand
{
    private readonly Entity entity;
    private readonly Vector3 targetPosition;
    private Vector3 previousPosition;
    
    public CmdResult Execute()
    {
        if (!CanExecute()) return CmdError.InvalidTarget;
        
        previousPosition = entity.Position;
        entity.Position = targetPosition;
        return CmdResult.Ok;
    }
    
    public CmdResult Undo()
    {
        entity.Position = previousPosition;
        return CmdResult.Ok;
    }
}
```

### 3. 观察者模式 (Observer Pattern)

用于事件系统，实现解耦的事件通知。

```csharp
public interface IEventBus
{
    void Subscribe<T>(Action<T> handler) where T : IGameEvent;
    void Unsubscribe<T>(Action<T> handler) where T : IGameEvent;
    void Publish<T>(T gameEvent) where T : IGameEvent;
}

// 使用示例
eventBus.Subscribe<EntityDeathEvent>(OnEntityDeath);
eventBus.Publish(new EntityDeathEvent { Entity = entity });
```

### 4. 对象池模式 (Object Pool Pattern)

用于性能优化，减少内存分配。

```csharp
public class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> pool = new();
    private readonly Action<T>? resetAction;
    
    public T Get()
    {
        if (pool.Count > 0)
        {
            return pool.Pop();
        }
        return new T();
    }
    
    public void Return(T obj)
    {
        resetAction?.Invoke(obj);
        pool.Push(obj);
    }
}
```

### 5. 工厂模式 (Factory Pattern)

用于对象创建，特别是效果和实体创建。

```csharp
public interface IEffectFactory
{
    IEffect CreateEffect(string effectId, IExecutionContext context);
}

public class EffectFactory : IEffectFactory
{
    private readonly Dictionary<string, Func<IExecutionContext, IEffect>> creators = new();
    
    public void Register(string effectId, Func<IExecutionContext, IEffect> creator)
    {
        creators[effectId] = creator;
    }
    
    public IEffect CreateEffect(string effectId, IExecutionContext context)
    {
        if (creators.TryGetValue(effectId, out var creator))
        {
            return creator(context);
        }
        throw new InvalidOperationException($"未注册的效果: {effectId}");
    }
}
```

## 📋 最佳实践

### 1. 空值处理

```csharp
// ✅ 使用可空引用类型
public string? Name { get; set; }
public Entity? Target { get; set; }

// ✅ 使用空值传播
var length = Name?.Length ?? 0;
Target?.TakeDamage(10);

// ✅ 使用空合并运算符
var displayName = Name ?? "未命名";
```

### 2. 异常处理

```csharp
// ✅ 只捕获预期的异常
try
{
    var data = LoadGameData(path);
}
catch (FileNotFoundException ex)
{
    Logger.LogError("游戏数据文件不存在: {Path}", path);
    return null;
}
catch (JsonException ex)
{
    Logger.LogError("游戏数据格式错误: {Message}", ex.Message);
    return null;
}

// ❌ 避免捕获所有异常
catch (Exception ex)
{
    // 太宽泛
}
```

### 3. 资源管理

```csharp
// ✅ 实现 IDisposable
public class ResourceManager : IDisposable
{
    private bool disposed;
    private readonly List<IDisposable> resources = new();
    
    public void Dispose()
    {
        if (!disposed)
        {
            foreach (var resource in resources)
            {
                resource.Dispose();
            }
            resources.Clear();
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
```

### 4. 性能优化

```csharp
// ✅ 使用 StringBuilder 进行字符串拼接
var sb = new StringBuilder();
foreach (var item in items)
{
    sb.Append(item.Name).Append(", ");
}

// ✅ 使用 Span<T> 处理大量数据
Span<byte> buffer = stackalloc byte[256];

// ✅ 缓存计算结果
private float? cachedDistance;
public float GetDistance()
{
    cachedDistance ??= CalculateDistance();
    return cachedDistance.Value;
}
```

### 5. 线程安全

```csharp
// ✅ 使用 lock 保护共享资源
private readonly object lockObject = new();
private int count;

public void Increment()
{
    lock (lockObject)
    {
        count++;
    }
}

// ✅ 使用并发集合
private readonly ConcurrentDictionary<int, Entity> entities = new();
```

## 🚫 禁止事项

1. **不要使用 goto 语句**
2. **不要在循环中分配大量内存**
3. **不要忽略编译器警告**
4. **不要使用魔法数字**，使用常量或枚举
5. **不要在构造函数中执行复杂逻辑**
6. **不要创建过深的继承层次**（最多3层）
7. **不要在 Update 方法中进行 I/O 操作**

## 📂 项目组织

### 文件组织

- 每个文件只包含一个公共类型
- 文件名与主要类型名称一致
- 使用部分类分离不同关注点

```Text
Control.cs           // 主要逻辑
Control.Events.cs    // 事件相关
Control.Properties.cs // 属性定义
Control.Layout.cs    // 布局逻辑
```

### 命名空间组织

- 命名空间与文件夹结构对应
- 避免过深的命名空间嵌套（最多4层）
- 相关功能放在同一命名空间下

```
GameCore/
├── AbilitySystem/      -> namespace GameCore.AbilitySystem
├── EntitySystem/       -> namespace GameCore.EntitySystem
└── Components/         -> namespace GameCore.Components
```

## 🔍 代码审查清单

- [ ] 命名是否符合规范？
- [ ] 是否有适当的注释？
- [ ] 是否处理了所有可能的空值？
- [ ] 是否正确释放了资源？
- [ ] 是否有性能问题？
- [ ] 是否有合适的单元测试？
- [ ] 是否遵循了 SOLID 原则？

---

## 🎨 UI系统最佳实践

### UI控件创建规范

#### ✅ 正确的控件创建方式

```csharp
// 创建面板容器
var panel = new Panel()
{
    Width = 800,           // 直接设置宽度
    Height = 600,          // 直接设置高度
    Background = new SolidColorBrush(Color.FromArgb(255, 135, 206, 235)),
    HorizontalAlignment = HorizontalAlignment.Center,
    VerticalAlignment = VerticalAlignment.Center
};

// 创建标签
var label = new Label()
{
    Text = "游戏分数",
    FontSize = 24,
    TextColor = Color.White,        // 使用TextColor，不是Foreground
    HorizontalAlignment = HorizontalAlignment.Center,
    VerticalAlignment = VerticalAlignment.Top,
    Margin = new Thickness(0, 20, 0, 0),
    Parent = panel                  // 设置父容器
};

// 创建按钮
var button = new Button()
{
    Width = 200,
    Height = 50,
    HorizontalAlignment = HorizontalAlignment.Center,
    VerticalAlignment = VerticalAlignment.Bottom,
    Margin = new Thickness(0, 0, 0, 20),
    Parent = panel
};
```

#### ❌ 错误的控件创建方式

```csharp
// 错误1：使用不存在的Layout属性
var panel = new Panel();
panel.Layout = new() { Width = 800, Height = 600 }; // 编译错误！

// 错误2：使用错误的可见性属性
var label = new Label();
label.Visibility = Visibility.Collapsed; // 应该使用Visible = false

// 错误3：使用错误的颜色属性
var label = new Label();
label.Foreground = Color.White; // 应该使用TextColor

// 错误4：错误的父子关系设置
var parent = new Panel();
var child = new Label();
parent.Children.Add(child); // 应该使用child.Parent = parent
```

### UI层次结构管理

#### ✅ 正确的层次结构设置

```csharp
// 创建UI树结构
var rootPanel = new Panel() { Width = 1000, Height = 800 };
var gameCanvas = new Canvas() { Parent = rootPanel };
var scorePanel = new Panel() { Parent = rootPanel };

// 添加子控件
var scoreLabel = new Label() { Parent = scorePanel };
var restartButton = new Button() { Parent = scorePanel };

// 将根面板添加到视觉树
_ = rootPanel.AddToVisualTree();
```

#### ❌ 错误的层次结构设置

```csharp
// 错误：使用不存在的Children集合
var parent = new Panel();
var child = new Label();
parent.Children.Add(child); // 编译错误！

// 错误：忘记添加到视觉树
var panel = new Panel();
// 缺少 panel.AddToVisualTree() 调用
```

### 事件处理规范

#### ✅ 正确的事件注册

```csharp
// 鼠标点击事件
button.OnPointerPressed += OnButtonClicked;

// 键盘事件（使用Trigger系统）
Trigger<EventGameKeyDown> keyDownTrigger = new(async (s, d) =>
{
    if (d.Key == GameCore.Platform.SDL.VirtualKey.Space)
    {
        HandleSpaceKey();
        return true;
    }
    return false;
});
keyDownTrigger.Register(Game.Instance);
```

#### ❌ 错误的事件注册

```csharp
// 错误：使用不存在的Game事件
Game.OnGameStart += OnGameStart;        // 编译错误！
Game.OnGameUpdate += OnGameUpdate;      // 编译错误！
Game.OnGameEnd += OnGameEnd;            // 编译错误！

// 错误：使用不存在的Click事件
button.Click += OnButtonClick;          // 应该使用OnPointerPressed
```

### 资源管理规范

#### ✅ 正确的资源清理

```csharp
public class GameUI : IDisposable
{
    private List<Control> controls = new();
    private List<Trigger<EventGameKeyDown>> triggers = new();

    public void Dispose()
    {
        // 清理UI控件
        foreach (var control in controls)
        {
            control.Dispose();
        }
        controls.Clear();

        // 清理事件触发器
        foreach (var trigger in triggers)
        {
            trigger.Unregister(Game.Instance);
        }
        triggers.Clear();
    }
}
```

#### ❌ 错误的资源清理

```csharp
// 错误：使用不存在的RemoveActor方法
Game.RemoveActor(actor); // 编译错误！

// 错误：忘记清理事件触发器
// 这会导致内存泄漏
```

### 性能优化建议

#### ✅ 批量UI操作

```csharp
// 推荐：批量创建UI元素
var labels = new List<Label>();
for (int i = 0; i < 100; i++)
{
    var label = new Label()
    {
        Text = $"Label {i}",
        Parent = parentPanel
    };
    labels.Add(label);
}

// 推荐：使用对象池复用UI元素
private readonly Queue<Label> labelPool = new();
```

#### ❌ 性能问题

```csharp
// 不推荐：频繁创建和销毁UI元素
public void UpdateScore(int score)
{
    // 每次都创建新标签，性能差
    var scoreLabel = new Label() { Text = score.ToString() };
    parentPanel.AddChild(scoreLabel);
}
```

### 常见陷阱和解决方案

#### 陷阱1：Layout属性混淆

**问题**：误以为UI对象有Layout属性
**解决**：记住Layout属性只在数编数据中存在，UI对象直接设置布局属性

#### 陷阱2：可见性属性错误

**问题**：使用Visibility枚举而不是Visible布尔值
**解决**：使用`Visible = true/false`而不是`Visibility = Visibility.Visible`

#### 陷阱3：事件系统混淆

**问题**：使用不存在的Game事件
**解决**：使用Trigger系统注册游戏事件，使用OnPointerPressed等UI事件

#### 陷阱4：父子关系设置错误

**问题**：使用Children.Add()方法
**解决**：设置子控件的Parent属性

### UI测试规范

```csharp
[Test]
public void TestButtonClick()
{
    // 创建测试UI
    var button = new Button();
    bool clicked = false;
    button.OnPointerPressed += (s, e) => clicked = true;

    // 模拟点击
    var args = new PointerEventArgs(new Vector2(0, 0), PointerButtons.Left, ModifierKeys.None);
    button.OnPointerPressed?.Invoke(button, args);

    // 验证结果
    Assert.IsTrue(clicked);
}
```
