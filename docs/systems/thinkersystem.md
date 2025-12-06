# 🧠 思考者系统（Thinker System）

思考者系统是 WasiCore 游戏框架中的核心机制，用于管理需要定期更新的游戏对象。该系统提供了高效的每帧处理能力，支持AI逻辑、动画更新、计时器和其他基于时间的行为。

## 📋 目录

- [🏗️ 系统概述](#系统概述)
- [🔧 核心接口](#核心接口)
  - [IThinker 接口](#ithinker-接口)
  - [IThinkerStaggered 接口](#ithinkerstaggered-接口)
- [⚡ 性能优化](#性能优化)
- [🛠️ 使用示例](#使用示例)
- [📊 实际应用](#实际应用)
- [💡 最佳实践](#最佳实践)
- [🔗 相关文档](#相关文档)

## 🏗️ 系统概述

思考者系统采用观察者模式设计，将需要定期更新的对象注册到全局的思考者管理器中。系统提供两种主要的思考者接口：

### 架构设计

```
游戏对象 → 实现IThinker/IThinkerStaggered → 注册到系统 → 每帧调用Think()
```

### 帧率设置

WasiCore 框架采用不同的帧率配置来优化服务端和客户端的性能：

- **服务端帧率**: 约 30 FPS（每帧约 33.3ms）
- **客户端帧率**: 约 60 FPS（每帧约 16.7ms）

这种设计允许客户端提供更流畅的视觉体验，同时服务端专注于逻辑处理和数据同步。

### 核心特性

- ✅ **自动管理** - 对象可以动态启用/禁用思考状态
- ✅ **性能优化** - 只处理活跃对象，避免不必要的计算
- ✅ **错列处理** - IThinkerStaggered 提供分散式处理，避免单帧性能峰值
- ✅ **类型安全** - 泛型设计确保编译时类型检查
- ✅ **异步支持** - 与游戏tick系统完美集成

## 🔧 核心接口

### IThinker 接口

`IThinker` 是基础的思考者接口，为需要每帧更新的对象提供标准化的处理机制。

#### 接口定义

```csharp
/// <summary>
/// 定义需要每游戏帧进行常规更新处理的对象接口
/// </summary>
/// <remarks>
/// 思考者系统允许对象执行每帧逻辑，如AI处理、动画更新、计时器或其他基于时间的行为。
/// 对象可以动态启用或禁用其思考状态，通过只处理活跃对象来优化性能。
/// </remarks>
public interface IThinker
{
    /// <summary>
    /// 执行对象的每帧思考逻辑
    /// </summary>
    /// <param name="delta">自上次tick以来经过的时间，以毫秒为单位</param>
    /// <remarks>
    /// 当 <see cref="DoesThink"/> 为 true 时，此方法在每个游戏tick中被调用。
    /// 实现应在此处执行基于帧的逻辑，如AI决策、状态更新或计时器处理。
    /// </remarks>
    void Think(int delta);
    
    /// <summary>
    /// 获取或设置指示此对象是否应包含在思考者系统中的值
    /// </summary>
    /// <remarks>
    /// 当设置为 true 时，对象注册到游戏的思考者系统，每个tick调用 <see cref="Think"/>。
    /// 当设置为 false 时，对象取消注册并停止接收思考调用，为非活跃对象提高性能。
    /// </remarks>
    bool DoesThink { get; set; }
}
```

> ⚠️ **重要警告：DoesThink 属性实现注意事项**
> 
> `IThinker` 接口的 `DoesThink` 属性**已提供默认实现**，包含了思考器的注册/注销逻辑。
> 
> **❌ 错误做法**：不要手动实现 `DoesThink` 属性
> ```csharp
> public class WrongThinker : IThinker
> {
>     public void Think(int delta) { /* 逻辑 */ }
>     
>     // ❌ 错误！这样会丢失思考器功能
>     public bool DoesThink { get; set; }
> }
> ```
> 
> **✅ 正确做法**：使用接口转换访问默认实现
> ```csharp
> public class CorrectThinker : IThinker
> {
>     public void Think(int delta) { /* 逻辑 */ }
>     
>     // ✅ 正确！使用接口的默认实现
>     public void StartThinking()
>     {
>         (this as IThinker).DoesThink = true;
>     }
>     
>     public void StopThinking()
>     {
>         (this as IThinker).DoesThink = false;
>     }
> }
> ```
> 
> **后果说明**：如果错误地实现了 `DoesThink` 属性，对象将无法正确注册到思考者系统，`Think` 方法永远不会被调用。

#### 基本使用

```csharp
public class MyGameObject : IThinker
{
    private float timer = 0f;
    
    public void Think(int delta)
    {
        // 更新计时器
        timer += delta;
        
        // 执行每帧逻辑
        if (timer >= 1000) // 每秒执行一次
        {
            DoSomething();
            timer = 0f;
        }
    }
    
    public void StartThinking()
    {
        (this as IThinker).DoesThink = true;
    }
    
    public void StopThinking()
    {
        (this as IThinker).DoesThink = false;
    }
    
    private void DoSomething()
    {
        // 自定义逻辑
        Game.Logger.LogInfo("执行定时操作");
    }
}
```

### IThinkerStaggered 接口

`IThinkerStaggered` 是高级的错列思考者接口，通过将对象分散到不同的帧中处理，避免单帧性能峰值。

#### 接口定义

```csharp
/// <summary>
/// 错列思考者接口，提供分散式处理能力
/// </summary>
/// <remarks>
/// 错列思考者系统将对象分散到多个帧中处理，避免在单帧中处理过多对象导致的性能问题。
/// 适用于AI系统、大量单位管理等需要性能优化的场景。
/// </remarks>
public interface IThinkerStaggered
{
    /// <summary>
    /// 错列处理的帧数间隔
    /// </summary>
    /// <remarks>
    /// 定义对象在多少帧中分散处理。例如，StaggeredCount = 15 表示对象每15帧被调用一次。
    /// 较大的值可以提供更好的性能，但会降低响应性。
    /// </remarks>
    int StaggeredCount { get; }
    
    /// <summary>
    /// 执行错列思考逻辑
    /// </summary>
    /// <remarks>
    /// 此方法不会每帧调用，而是根据 StaggeredCount 的值分散调用。
    /// 适合执行不需要每帧更新的逻辑，如AI决策、路径规划等。
    /// </remarks>
    void Think();
    
    /// <summary>
    /// 获取或设置是否启用错列思考
    /// </summary>
    /// <remarks>
    /// 当设置为 true 时，对象被添加到错列思考者系统中。
    /// 当设置为 false 时，对象从系统中移除。
    /// </remarks>
    bool DoesThink { get; set; }
}
```

> ⚠️ **重要警告：DoesThink 属性实现注意事项**
> 
> `IThinkerStaggered` 接口的 `DoesThink` 属性**已提供默认实现**，包含了错列思考器的注册/注销逻辑。
> 
> **❌ 错误做法**：不要手动实现 `DoesThink` 属性
> ```csharp
> public class WrongStaggeredThinker : IThinkerStaggered
> {
>     public int StaggeredCount => 15;
>     public void Think() { /* 逻辑 */ }
>     
>     // ❌ 错误！这样会丢失错列思考器功能
>     public bool DoesThink { get; set; }
> }
> ```
> 
> **✅ 正确做法**：使用接口转换访问默认实现
> ```csharp
> public class CorrectStaggeredThinker : IThinkerStaggered
> {
>     public int StaggeredCount => 15;
>     public void Think() { /* 逻辑 */ }
>     
>     // ✅ 正确！使用接口的默认实现
>     public void StartThinking()
>     {
>         (this as IThinkerStaggered).DoesThink = true;
>     }
>     
>     public void StopThinking()
>     {
>         (this as IThinkerStaggered).DoesThink = false;
>     }
> }
> ```

#### 基本使用

 ```csharp
 public class MyAIController : IThinkerStaggered
 {
     public int StaggeredCount => 15; // 每15帧思考一次 (服务端约0.5秒)
    
    public void Think()
    {
        // 执行AI逻辑（不需要每帧更新）
        AnalyzeSituation();
        MakeDecision();
        ExecuteAction();
    }
    
    public void StartAI()
    {
        (this as IThinkerStaggered).DoesThink = true;
    }
    
    public void StopAI()
    {
        (this as IThinkerStaggered).DoesThink = false;
    }
    
    private void AnalyzeSituation()
    {
        // 分析当前情况
    }
    
    private void MakeDecision()
    {
        // 做出决策
    }
    
    private void ExecuteAction()
    {
        // 执行行动
    }
}
```

## ⚡ 性能优化

### 选择合适的接口

| 接口类型 | 调用频率 | 性能开销 | 适用场景 |
|---------|---------|---------|---------|
| `IThinker` | 每帧 (服务端30fps/客户端60fps) | 较高 | 动画、UI更新、精确计时器 |
| `IThinkerStaggered` | 每N帧 (可配置间隔) | 较低 | AI逻辑、路径规划、状态检查 |

> **💡 性能提示**: 由于服务端和客户端的帧率不同，同样的 `StaggeredCount` 值在不同环境下的实际时间间隔会有差异。在设计跨端逻辑时需要考虑这一点。

### 错列处理策略

```csharp
public class PerformanceOptimizedAI : IThinkerStaggered
{
    // 根据AI复杂度选择合适的错列间隔
    public int StaggeredCount => GetOptimalStaggerCount();
    
         private int GetOptimalStaggerCount()
     {
         // 简单AI: 每30帧思考一次 (服务端约1秒，客户端约0.5秒)
         if (AIComplexity == AIComplexity.Simple)
             return 30;
         
         // 中等AI: 每15帧思考一次 (服务端约0.5秒，客户端约0.25秒)
         if (AIComplexity == AIComplexity.Medium)
             return 15;
         
         // 复杂AI: 每10帧思考一次 (服务端约0.33秒，客户端约0.17秒)
         return 10;
     }
    
    public void Think()
    {
        // 根据AI复杂度执行不同的思考逻辑
        switch (AIComplexity)
        {
            case AIComplexity.Simple:
                SimpleAILogic();
                break;
            case AIComplexity.Medium:
                MediumAILogic();
                break;
            case AIComplexity.Complex:
                ComplexAILogic();
                break;
        }
    }
}
```

### 动态启用/禁用

```csharp
public class ConditionalThinker : IThinker
{
    private bool isActive = false;
    private float lastActivityTime = 0f;
    
    public void Think(int delta)
    {
        if (!isActive)
        {
            // 如果长时间未活跃，停止思考以节省性能
            if (Game.Time - lastActivityTime > 5000) // 5秒
            {
                (this as IThinker).DoesThink = false;
                return;
            }
        }
        
        // 执行常规逻辑
        UpdateLogic(delta);
    }
    
    public void OnActivityDetected()
    {
        isActive = true;
        lastActivityTime = Game.Time;
        
        // 重新启用思考
        if (!(this as IThinker).DoesThink)
        {
            (this as IThinker).DoesThink = true;
        }
    }
}
```

## 🛠️ 使用示例

### 动画控制器

```csharp
public class AnimationController : IThinker
{
    private float animationTime = 0f;
    private float frameDuration = 100f; // 每帧持续100ms
    private int currentFrame = 0;
    private int totalFrames = 8;
    
    public void Think(int delta)
    {
        animationTime += delta;
        
        if (animationTime >= frameDuration)
        {
            // 切换到下一帧
            currentFrame = (currentFrame + 1) % totalFrames;
            animationTime = 0f;
            
            // 更新显示
            UpdateAnimationFrame(currentFrame);
        }
    }
    
    public void StartAnimation()
    {
        (this as IThinker).DoesThink = true;
    }
    
    public void StopAnimation()
    {
        (this as IThinker).DoesThink = false;
    }
    
    private void UpdateAnimationFrame(int frame)
    {
        // 更新动画帧
        Game.Logger.LogDebug($"切换到动画帧: {frame}");
    }
}
```

### 计时器系统

```csharp
public class GameTimer : IThinker
{
    private float remainingTime;
    private readonly float initialTime;
    private readonly Action onComplete;
    
    public GameTimer(float duration, Action onComplete)
    {
        this.initialTime = duration;
        this.remainingTime = duration;
        this.onComplete = onComplete;
    }
    
    public void Think(int delta)
    {
        remainingTime -= delta;
        
        if (remainingTime <= 0)
        {
            // 计时器完成
            (this as IThinker).DoesThink = false;
            onComplete?.Invoke();
        }
    }
    
    public void Start()
    {
        (this as IThinker).DoesThink = true;
    }
    
    public void Stop()
    {
        (this as IThinker).DoesThink = false;
    }
    
    public void Reset()
    {
        remainingTime = initialTime;
    }
    
    public float Progress => 1f - (remainingTime / initialTime);
}
```

### 简单AI系统

```csharp
 public class SimpleAI : IThinkerStaggered
 {
     private readonly Entity owner;
     private Entity? target;
     private AIState currentState = AIState.Idle;
     
     public int StaggeredCount => 20; // 每20帧思考一次 (服务端约0.67秒)
    
    public SimpleAI(Entity owner)
    {
        this.owner = owner;
    }
    
    public void Think()
    {
        switch (currentState)
        {
            case AIState.Idle:
                IdleThink();
                break;
            case AIState.Seeking:
                SeekingThink();
                break;
            case AIState.Attacking:
                AttackingThink();
                break;
            case AIState.Retreating:
                RetreatingThink();
                break;
        }
    }
    
    private void IdleThink()
    {
        // 寻找目标
        target = FindNearestEnemy();
        if (target != null)
        {
            currentState = AIState.Seeking;
        }
    }
    
    private void SeekingThink()
    {
        if (target == null || !target.IsValid)
        {
            currentState = AIState.Idle;
            return;
        }
        
        float distance = Vector3.Distance(owner.Position, target.Position);
        if (distance < 5f) // 攻击范围
        {
            currentState = AIState.Attacking;
        }
        else
        {
            // 移动向目标
            MoveToTarget(target);
        }
    }
    
    private void AttackingThink()
    {
        if (target == null || !target.IsValid)
        {
            currentState = AIState.Idle;
            return;
        }
        
        // 攻击目标
        AttackTarget(target);
        
        // 检查是否需要撤退
        if (owner.Health < owner.MaxHealth * 0.3f)
        {
            currentState = AIState.Retreating;
        }
    }
    
    private void RetreatingThink()
    {
        // 撤退逻辑
        Retreat();
        
        // 检查是否安全
        if (owner.Health > owner.MaxHealth * 0.7f)
        {
            currentState = AIState.Idle;
        }
    }
    
    public void StartAI()
    {
        (this as IThinkerStaggered).DoesThink = true;
    }
    
    public void StopAI()
    {
        (this as IThinkerStaggered).DoesThink = false;
    }
}

public enum AIState
{
    Idle,
    Seeking,
    Attacking,
    Retreating
}
```

## 📊 实际应用

### 在AI系统中的应用

WasiCore 框架中的 `AIThinkTree` 类是 `IThinkerStaggered` 的完美实现示例：

 ```csharp
 public partial class AIThinkTree : IThinkerStaggered
 {
     public int StaggeredCount => 15; // 每15帧思考一次 (服务端约0.5秒)
    
    void IThinkerStaggered.Think()
    {
        switch (CombatState)
        {
            case CombatState.Retreating:
                RetreatingThink();
                IsRetreatDoneThink();
                break;
            case CombatState.Leashing:
                CombatScanThink();
                IsRetreatDoneThink();
                break;
            case CombatState.OutOfCombat:
                CombatScanThink();
                LeaderLeashThink();
                break;
            case CombatState.InCombat:
                if (!CombatToRetreatThink() && !IsCasting())
                {
                    CombatInitThisTick();
                    CombatBehaviorTreeThink();
                }
                break;
        }
    }
}
```

### 在生命值系统中的应用

`Vital` 类使用 `IThinker` 接口实现生命值恢复：

```csharp
public class Vital : TagComponent, IThinker
{
    public void Think(int delta)
    {
        // 生命值恢复逻辑
        if (Current < Max && Regen.RatePerTick > 0)
        {
            Current = Math.Min(Max, Current + Regen.RatePerTick * delta);
        }
    }
    
    // 根据恢复速率动态启用/禁用思考
    private void UpdateThinkState()
    {
        (this as IThinker).DoesThink = Regen.RatePerTick > 0;
    }
}
```

### 在计时器系统中的应用

框架的 `Delay` 和 `Awaitable` 类使用思考者系统实现精确的计时功能：

```csharp
public class Awaitable : IThinker
{
    private float remainingTime;
    
    public void Think(int delta)
    {
        if (!(this as IThinker).DoesThink)
            return;
            
        remainingTime -= delta;
        
        if (remainingTime <= 0)
        {
            (this as IThinker).DoesThink = false;
            CompleteTask();
        }
    }
}
```

## 💡 最佳实践

### ✅ 推荐做法

1. **⚠️ 正确使用 DoesThink 属性（重要）**
   ```csharp
   // ✅ 正确：不要手动实现 DoesThink 属性，使用接口默认实现
   public class MyThinker : IThinker
   {
       public void Think(int delta) { /* 逻辑 */ }
       
       public void Enable() => (this as IThinker).DoesThink = true;
       public void Disable() => (this as IThinker).DoesThink = false;
   }
   
   // ❌ 错误：手动实现会丢失思考器功能
   public class WrongThinker : IThinker
   {
       public void Think(int delta) { /* 逻辑 */ }
       public bool DoesThink { get; set; }  // ❌ 永远不会被调用！
   }
   ```

2. **合理选择接口类型**
   ```csharp
   // 需要精确每帧更新的场景
   public class PreciseTimer : IThinker { }
   
   // 不需要每帧更新的场景
   public class AIController : IThinkerStaggered { }
   ```

2. **动态控制思考状态**
   ```csharp
   public void OnObjectActivated()
   {
       (this as IThinker).DoesThink = true;
   }
   
   public void OnObjectDeactivated()
   {
       (this as IThinker).DoesThink = false;
   }
   ```

 3. **优化错列间隔**
    ```csharp
    // 根据对象重要性调整错列间隔
    public int StaggeredCount => isImportant ? 10 : 30;
    
    // 考虑服务端和客户端的帧率差异
    public int StaggeredCount => 
    #if SERVER
        15; // 服务端30fps，约0.5秒间隔
    #else
        30; // 客户端60fps，约0.5秒间隔
    #endif
    ```

4. **使用条件思考**
   ```csharp
   public void Think(int delta)
   {
       if (!ShouldThink())
           return;
           
       // 执行思考逻辑
   }
   ```

5. **正确的生命周期管理**
   ```csharp
   public void Dispose()
   {
       // 清理时停止思考
       (this as IThinker).DoesThink = false;
   }
   ```

### ❌ 避免的做法

1. **⚠️ 手动实现 DoesThink 属性（严重错误）**
   ```csharp
   // ❌ 严重错误：手动实现 DoesThink 属性会完全破坏思考器功能
   public class BrokenThinker : IThinker
   {
       public void Think(int delta) 
       { 
           // 这个方法永远不会被调用！
           Game.Logger.LogInfo("这条日志永远不会出现");
       }
       
       // ❌ 这样实现会丢失框架的注册/注销逻辑
       public bool DoesThink { get; set; }
   }
   
   // ✅ 正确：只实现 Think 方法，通过接口转换使用 DoesThink
   public class WorkingThinker : IThinker
   {
       public void Think(int delta) { /* 正常工作 */ }
       
       public void Start() => (this as IThinker).DoesThink = true;
   }
   ```

2. **忘记停止思考**
   ```csharp
   // ❌ 错误：对象销毁时忘记停止思考
   public void Destroy()
   {
       // 忘记设置 DoesThink = false
   }
   
   // ✅ 正确
   public void Destroy()
   {
       (this as IThinker).DoesThink = false;
   }
   ```

2. **在Think方法中执行耗时操作**
   ```csharp
   // ❌ 错误：在Think中执行耗时操作
   public void Think(int delta)
   {
       // 这会阻塞游戏帧
       Thread.Sleep(100);
   }
   
   // ✅ 正确：使用异步或分时处理
   public async void Think(int delta)
   {
       await Game.Delay(TimeSpan.FromMilliseconds(100));
   }
   ```

 3. **错误的错列间隔设置**
    ```csharp
    // ❌ 错误：错列间隔过小，失去性能优化意义
    public int StaggeredCount => 1; // 基本等同于每帧调用
    
    // ❌ 错误：错列间隔过大，响应性太差
    public int StaggeredCount => 1000; // 服务端约33秒，客户端约17秒
    
    // ✅ 正确：合理的错列间隔 (根据需求调整)
    public int StaggeredCount => 15; // 服务端约0.5秒，客户端约0.25秒
    ```

4. **在Think方法中修改DoesThink状态**
   ```csharp
   // ❌ 错误：可能导致状态混乱
   public void Think(int delta)
   {
       if (someCondition)
       {
           (this as IThinker).DoesThink = false; // 不推荐
       }
   }
   
   // ✅ 正确：在Think方法外部管理状态
   public void OnConditionChanged()
   {
       (this as IThinker).DoesThink = newCondition;
   }
   ```

### 🔧 性能优化建议

1. **批量处理**
   ```csharp
   public class BatchThinker : IThinkerStaggered
   {
       private readonly List<IProcessable> batch = new();
       
       public void Think()
       {
           // 批量处理多个对象
           foreach (var item in batch)
           {
               item.Process();
           }
       }
   }
   ```

2. **条件性思考**
   ```csharp
   public class ConditionalThinker : IThinker
   {
       private bool needsUpdate = true;
       
       public void Think(int delta)
       {
           if (!needsUpdate)
               return;
               
           // 执行更新逻辑
           DoUpdate();
           needsUpdate = false;
       }
       
       public void MarkDirty()
       {
           needsUpdate = true;
       }
   }
   ```

3. **分时处理**
   ```csharp
   public class TimeSplicedThinker : IThinker
   {
       private readonly Queue<Action> tasks = new();
       private const int MaxTasksPerFrame = 5;
       
       public void Think(int delta)
       {
           int processed = 0;
           while (tasks.Count > 0 && processed < MaxTasksPerFrame)
           {
               tasks.Dequeue()();
               processed++;
           }
       }
   }
   ```

## 🔧 故障排除

### 常见问题诊断

#### ❓ Think方法没有被调用

**症状**：实现了`IThinker.Think`方法，但是方法内的代码从未执行。

**可能原因**：
1. ❌ **最常见**：错误地手动实现了`DoesThink`属性
2. ❌ 忘记设置`DoesThink = true`
3. ❌ 对象在设置`DoesThink = true`后被垃圾回收

**诊断方法**：
```csharp
// 1. 检查是否手动实现了DoesThink
public class MyThinker : IThinker
{
    public void Think(int delta)
    {
        Game.Logger.LogInfo("Think被调用了"); // 如果看不到这条日志...
    }
    
    // ❌ 如果有这一行，立即删除！
    // public bool DoesThink { get; set; }
}

// 2. 确认正确启动
var thinker = new MyThinker();
(thinker as IThinker).DoesThink = true; // ✅ 正确方式

// 3. 验证注册状态（调试用）
Game.Logger.LogInfo($"DoesThink状态: {(thinker as IThinker).DoesThink}");
```

**解决方案**：
- 移除任何手动的`DoesThink`属性实现
- 使用`(this as IThinker).DoesThink = true`启动思考

#### ❓ 性能问题

**症状**：游戏帧率下降，特别是有大量思考者时。

**解决方案**：
- 考虑使用`IThinkerStaggered`代替`IThinker`
- 增加错列间隔`StaggeredCount`
- 在`Think`方法中添加条件判断，避免不必要的计算

#### ❓ 内存泄漏

**症状**：思考者对象无法被垃圾回收。

**原因**：忘记设置`DoesThink = false`导致系统持有引用。

**解决方案**：
```csharp
public void Dispose()
{
    (this as IThinker).DoesThink = false; // ✅ 必须！
}
```

### 自动检测工具

#### 运行时验证（调试模式）
```csharp
#if DEBUG
public static void ValidateThinker<T>(T thinker) where T : IThinker
{
    var type = typeof(T);
    var doesThinkProperty = type.GetProperty("DoesThink");
    
    if (doesThinkProperty?.DeclaringType == type)
    {
        throw new InvalidOperationException(
            $"错误：{type.Name} 手动实现了DoesThink属性！" +
            "这会导致思考器功能完全失效。请移除此属性实现。");
    }
}
#endif
```

## 🔗 相关文档

- [🤖 AI系统](AISystem.md) - AI系统中思考者的具体应用
- [⏰ 计时器系统](TimerSystem.md) - 计时器系统的实现细节
- [🎯 单位系统](UnitSystem.md) - 单位系统中的思考者使用
- [🔄 异步编程](../best-practices/AsyncProgramming.md) - 异步编程最佳实践

---

> 💡 **提示**: 思考者系统是框架的性能关键组件，正确使用可以显著提升游戏性能。在设计游戏逻辑时，始终考虑是否需要每帧更新，并选择合适的思考者接口。 