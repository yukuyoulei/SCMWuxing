# 🔄 异步编程最佳实践

WasiCore 框架在 WebAssembly 环境中运行，对异步编程有特殊的要求和限制。本文档详细介绍了在框架中进行异步编程的最佳实践。

## 📋 目录

- [🌐 Wasm 环境限制](#wasm-环境限制)
- [⏰ 游戏时间对齐](#游戏时间对齐)
- [🎯 正确的异步模式](#正确的异步模式)
- [❌ 常见错误](#常见错误)
- [🛠️ 实用示例](#实用示例)
- [🔧 调试技巧](#调试技巧)

## 🌐 Wasm 环境限制

### 单线程约束

WebAssembly 环境下，WasiCore 框架运行在单线程中：

```csharp
// ❌ 这些多线程操作都不被支持
Task.Run(() => { /* 后台任务 */ });
ThreadPool.QueueUserWorkItem(workItem);
new Thread(threadStart).Start();
Parallel.ForEach(collection, action);
```

### 支持的异步操作

只有以下异步操作是安全的：

```csharp
// ✅ 支持的异步操作
await Game.Delay(TimeSpan.FromSeconds(1));          // 游戏延迟
await someAsyncGameMethod();                         // 游戏异步方法
await networkClient.SendAsync(data);                // 网络操作
await resourceLoader.LoadAsync(path);               // 资源加载
```

## ⏰ 游戏时间对齐

### Game.Delay vs Task.Delay

| 特性 | `Game.Delay()` | `Task.Delay()` |
|------|----------------|----------------|
| **Wasm 兼容性** | ✅ 完全支持 | ❌ 可能异常 |
| **游戏 Tick 对齐** | ✅ 自动对齐 | ❌ 独立计时 |
| **暂停/恢复** | ✅ 遵循游戏状态 | ❌ 忽略游戏状态 |
| **性能** | ✅ 优化 | ❌ 额外开销 |

### 时间对齐示例

```csharp
// ✅ 推荐：与游戏tick对齐的延迟
await Game.Delay(TimeSpan.FromSeconds(2.5f));

// ✅ 推荐：等待指定帧数
await Game.DelayFrames(60); // 等待60帧

// ❌ 避免：独立于游戏时间的延迟
await Task.Delay(2500); // 可能导致不同步
```

## 🎯 正确的异步模式

### 1. 技能释放延迟

```csharp
public async Task<bool> CastSpellWithDelay(Unit caster, ITarget target)
{
    // 开始施法动画
    caster.PlayAnimation("CastStart");
    
    // 等待施法时间（与游戏时间同步）
    await Game.Delay(TimeSpan.FromSeconds(1.5f));
    
    // 检查施法是否被打断
    if (caster.HasState(UnitState.Stunned))
    {
        return false;
    }
    
    // 执行技能效果
    ApplySpellEffect(caster, target);
    return true;
}
```

### 2. 渐进式效果

```csharp
public async Task ApplyDamageOverTime(Unit target, float totalDamage, TimeSpan duration)
{
    var interval = TimeSpan.FromSeconds(0.5f);
    var ticks = (int)(duration.TotalSeconds / interval.TotalSeconds);
    var damagePerTick = totalDamage / ticks;
    
    for (int i = 0; i < ticks; i++)
    {
        // 检查目标是否仍然有效
        if (!target.IsAlive)
            break;
            
        // 造成伤害
        target.TakeDamage(damagePerTick);
        
        // 等待下一次tick（与游戏时间同步）
        await Game.Delay(interval);
    }
}
```

### 3. 资源加载

```csharp
public async Task<Scene> LoadSceneAsync(string sceneName)
{
    try
    {
        Game.Logger.LogInfo("开始加载场景: {SceneName}", sceneName);
        
        // 使用框架的异步加载
        var scene = await SceneLoader.LoadAsync(sceneName);
        
        Game.Logger.LogInfo("场景加载完成: {SceneName}", sceneName);
        return scene;
    }
    catch (Exception ex)
    {
        Game.Logger.LogError("场景加载失败: {SceneName}, 错误: {Error}", 
            sceneName, ex.Message);
        throw;
    }
}
```

### 4. 网络通信

```csharp
public async Task<bool> SendPlayerActionAsync(PlayerAction action)
{
    try
    {
        // 发送动作到服务器
        await NetworkManager.SendAsync(action);
        
        // 等待服务器确认（带超时）
        var confirmation = await NetworkManager.WaitForConfirmationAsync(
            action.Id, 
            timeout: TimeSpan.FromSeconds(5)
        );
        
        return confirmation != null;
    }
    catch (TimeoutException)
    {
        Game.Logger.LogWarning("网络操作超时: {ActionType}", action.Type);
        return false;
    }
}
```

## ❌ 常见错误

### 1. 使用标准异步方法

```csharp
// ❌ 错误：使用Task.Delay
public async Task WrongDelay()
{
    await Task.Delay(1000); // 在Wasm中可能异常
}

// ✅ 正确：使用Game.Delay
public async Task CorrectDelay()
{
    await Game.Delay(TimeSpan.FromSeconds(1));
}
```

### 2. 创建后台任务

```csharp
// ❌ 错误：尝试创建后台线程
public void WrongBackgroundTask()
{
    Task.Run(async () => {
        while (true)
        {
            await DoSomething();
            await Task.Delay(100);
        }
    });
}

// ✅ 正确：使用游戏循环
public class BackgroundProcessor : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnTick += ProcessBackground;
    }
    
    private static void ProcessBackground(int deltaTime)
    {
        // 在游戏主循环中处理
    }
}
```

### 3. 不检查游戏状态

```csharp
// ❌ 错误：忽略游戏状态
public async Task WrongLongOperation()
{
    for (int i = 0; i < 100; i++)
    {
        DoSomething();
        await Game.Delay(TimeSpan.FromSeconds(0.1f));
    }
}

// ✅ 正确：检查游戏状态
public async Task CorrectLongOperation()
{
    for (int i = 0; i < 100; i++)
    {
        // 检查游戏是否还在运行
        if (!Game.IsRunning)
            break;
            
        DoSomething();
        await Game.Delay(TimeSpan.FromSeconds(0.1f));
    }
}
```

## 🛠️ 实用示例

### 单位移动序列

```csharp
public async Task<bool> MoveUnitInSequence(Unit unit, ScenePoint[] waypoints)
{
    foreach (var point in waypoints)
    {
        // 检查单位是否仍然有效
        if (!unit.IsValid || !unit.IsAlive)
            return false;
            
        // 开始移动到目标点
        unit.MoveTo(point);
        
        // 等待移动完成
        while (unit.IsMoving && unit.IsValid)
        {
            await Game.DelayFrames(1); // 等待一帧
        }
        
        // 在路点停留片刻
        await Game.Delay(TimeSpan.FromSeconds(0.5f));
    }
    
    return true;
}
```

### 技能连击系统

```csharp
public async Task<bool> ExecuteComboAttack(Unit attacker, Unit target)
{
    var comboSkills = new[]
    {
        "FirstStrike",
        "FollowUp", 
        "Finisher"
    };
    
    foreach (var skillName in comboSkills)
    {
        // 检查目标是否仍然有效
        if (!target.IsAlive || !attacker.IsAlive)
            return false;
            
        // 释放技能
        var skill = attacker.GetAbility(skillName);
        if (skill == null)
            return false;
            
        await skill.CastAsync(target);
        
        // 连击间隔
        await Game.Delay(TimeSpan.FromSeconds(0.8f));
    }
    
    return true;
}
```

### 渐变效果

```csharp
public async Task FadeOutUnit(Unit unit, TimeSpan duration)
{
    var startAlpha = 1.0f;
    var endAlpha = 0.0f;
    var frameTime = TimeSpan.FromSeconds(1.0f / 60.0f); // 60 FPS
    var totalFrames = (int)(duration.TotalSeconds * 60);
    
    for (int frame = 0; frame < totalFrames; frame++)
    {
        if (!unit.IsValid)
            break;
            
        var progress = (float)frame / totalFrames;
        var currentAlpha = Mathf.Lerp(startAlpha, endAlpha, progress);
        
        unit.SetAlpha(currentAlpha);
        
        await Game.DelayFrames(1);
    }
    
    // 确保最终透明度
    if (unit.IsValid)
        unit.SetAlpha(endAlpha);
}
```

## 🔧 调试技巧

### 1. 异步操作日志

```csharp
public async Task<T> LoggedAsyncOperation<T>(
    string operationName, 
    Func<Task<T>> operation)
{
    var startTime = Game.CurrentTime;
    Game.Logger.LogDebug("开始异步操作: {OperationName}", operationName);
    
    try
    {
        var result = await operation();
        var duration = Game.CurrentTime - startTime;
        Game.Logger.LogDebug("异步操作完成: {OperationName}, 耗时: {Duration}ms", 
            operationName, duration.TotalMilliseconds);
        return result;
    }
    catch (Exception ex)
    {
        Game.Logger.LogError("异步操作失败: {OperationName}, 错误: {Error}", 
            operationName, ex.Message);
        throw;
    }
}
```

### 2. 超时保护

```csharp
public async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout)
{
    var timeoutTask = Game.Delay(timeout);
    var completedTask = await Task.WhenAny(task, timeoutTask);
    
    if (completedTask == timeoutTask)
    {
        throw new TimeoutException($"操作超时: {timeout.TotalSeconds}秒");
    }
    
    return await task;
}
```

### 3. 异步状态监控

```csharp
public class AsyncOperationMonitor
{
    private static readonly Dictionary<string, int> _activeOperations = new();
    
    public static async Task<T> MonitorAsync<T>(string category, Task<T> task)
    {
        _activeOperations[category] = _activeOperations.GetValueOrDefault(category) + 1;
        
        try
        {
            return await task;
        }
        finally
        {
            _activeOperations[category]--;
            if (_activeOperations[category] <= 0)
                _activeOperations.Remove(category);
        }
    }
    
    public static void LogActiveOperations()
    {
        foreach (var kvp in _activeOperations)
        {
            Game.Logger.LogDebug("活跃异步操作: {Category} = {Count}", 
                kvp.Key, kvp.Value);
        }
    }
}
```

## 💡 最佳实践总结

### ✅ 推荐做法

1. **使用框架异步方法** - 始终使用 `Game.Delay()` 而非 `Task.Delay()`
2. **检查对象有效性** - 在长时间异步操作中定期检查对象状态
3. **适当的错误处理** - 使用 try-catch 处理异步异常
4. **合理的超时设置** - 为网络操作设置合理的超时时间
5. **日志记录** - 记录重要的异步操作开始和结束

### ❌ 避免的做法

1. **多线程操作** - 避免使用任何多线程API
2. **忽略游戏状态** - 不要忽略游戏暂停、停止等状态
3. **阻塞操作** - 避免在异步方法中使用 `.Result` 或 `.Wait()`
4. **资源泄漏** - 确保异步操作正确清理资源
5. **无限循环** - 避免没有退出条件的异步循环

---

> 💡 **提示**: 在 WebAssembly 环境中，正确的异步编程对于游戏性能和稳定性至关重要。始终使用框架提供的异步方法，并确保与游戏时间系统保持同步。 