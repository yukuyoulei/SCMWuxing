# 测试指南

本文档提供 WasiCore 框架的测试策略、最佳实践和常用命令。

## 🚨 重要须知

### 解决方案级别测试要求

⚠️ **关键要求**：WasiCore 框架的单元测试**必须**使用解决方案级别构建，不能直接对单个测试项目进行测试。

```bash
# ✅ 正确做法
dotnet test *.sln -c Server-Debug
dotnet test *.sln -c Client-Debug

# ❌ 错误做法 - 会导致构建失败
dotnet test GameCoreTests/GameCoreHostTests.csproj
dotnet test GameCoreClientTests/GameCoreClientTests.csproj
```

### 为什么需要解决方案级别测试？

1. **复杂的项目依赖关系**：GameCore 依赖于多个其他项目
2. **代码生成器依赖**：CodeGenerator 项目需要在测试前运行
3. **共享资源和配置**：某些资源和配置在解决方案级别共享
4. **构建顺序要求**：项目间有特定的构建顺序要求

## 📋 测试项目结构

```text
Tests/
├── GameCoreTests/           # 服务器端核心测试
│   ├── Timer/              # 计时器系统测试
│   ├── BaseType/           # 基础类型测试
│   ├── Behavior/           # 行为系统测试
│   └── ...
├── GameCoreClientTests/     # 客户端核心测试
├── EventsTests/            # 事件系统测试
├── TriggerEncapsulationTests/ # 触发器封装测试
└── ...
```

## 🔧 常用测试命令

### 基本测试命令

```bash
# 运行所有服务器端测试
dotnet test *.sln -c Server-Debug

# 运行所有客户端测试
dotnet test *.sln -c Client-Debug

# 显示详细输出
dotnet test *.sln -c Server-Debug --verbosity normal

# 显示测试结果详情
dotnet test *.sln -c Server-Debug --logger trx
```

### 筛选测试

```bash
# 运行特定类的测试
dotnet test *.sln -c Server-Debug --filter "FullyQualifiedName~TimerTest"

# 运行特定方法的测试
dotnet test *.sln -c Server-Debug --filter "Timer_WithAutoResetFalse_FiresOnlyOnce"

# 运行包含特定关键字的测试
dotnet test *.sln -c Server-Debug --filter "Constructor"

# 组合筛选器
dotnet test *.sln -c Server-Debug --filter "FullyQualifiedName~TimerTest&TestCategory!=Async"
```

### 并行测试

```bash
# 控制并行度（避免并发问题）
dotnet test *.sln -c Server-Debug --parallel

# 禁用并行测试（推荐用于调试）
dotnet test *.sln -c Server-Debug -- MSTest.Parallelize.Workers=1
```

## 🎯 测试最佳实践

### 1. 测试命名规范

```csharp
[TestMethod]
public void MethodName_Scenario_ExpectedBehavior()
{
    // 测试实现
}

// 示例
[TestMethod]
public void Timer_WithAutoResetFalse_FiresOnlyOnce()
{
    // 测试 AutoReset=false 时只触发一次
}
```

### 2. 测试结构（AAA 模式）

```csharp
[TestMethod]
public void ExampleTest()
{
    // Arrange - 准备测试数据
    using var timer = new GameCore.Timers.Timer(100);
    var eventCount = 0;
    timer.Elapsed += (sender, e) => eventCount++;

    // Act - 执行被测试的操作
    timer.Start();
    // 等待或触发

    // Assert - 验证结果
    Assert.AreEqual(1, eventCount);
}
```

### 3. 资源管理

```csharp
[TestMethod]
public void TestWithResources()
{
    // 使用 using 语句确保资源释放
    using var timer = new GameCore.Timers.Timer();
    using var resetEvent = new ManualResetEventSlim();
    
    // 测试逻辑...
}
```

### 4. 异步测试

```csharp
[TestMethod]
public async Task AsyncTestMethod()
{
    // 对于涉及 Delay 类的测试，使用异步模式
    using var timer = new GameCore.Timers.Timer(100);
    var resetEvent = new ManualResetEventSlim();
    
    timer.Elapsed += (sender, e) => resetEvent.Set();
    timer.Start();
    
    var completed = resetEvent.Wait(TimeSpan.FromSeconds(2));
    Assert.IsTrue(completed);
}
```

## 🐛 常见问题和解决方案

### 1. 构建失败

**问题**：直接测试单个项目时出现依赖关系错误
```
错误: 无法找到程序集 'GameCore'
```

**解决方案**：使用解决方案级别测试
```bash
dotnet test *.sln -c Server-Debug
```

### 2. 并发异常

**问题**：测试运行时出现并发集合访问错误
```
System.InvalidOperationException: Operations that change non-concurrent collections must have exclusive access
```

**解决方案**：
1. 使用测试筛选器避免运行所有异步测试
2. 禁用测试并行化
3. 分别运行不同类型的测试

```bash
# 避免某些并发敏感的测试
dotnet test *.sln -c Server-Debug --filter "TestCategory!=Concurrent"
```

### 3. 测试超时

**问题**：涉及计时器的测试偶尔超时

**解决方案**：
1. 使用适当的超时值
2. 使用 ManualResetEventSlim 而非 Thread.Sleep
3. 在测试中加入适当的等待逻辑

```csharp
// ✅ 好的做法
var completed = resetEvent.Wait(TimeSpan.FromSeconds(2));
Assert.IsTrue(completed, "Timer should have fired within timeout");

// ❌ 不好的做法
Thread.Sleep(1000); // 不可靠
```

## 📊 测试报告

### 生成测试报告

```bash
# 生成 TRX 报告
dotnet test *.sln -c Server-Debug --logger trx

# 生成代码覆盖率报告（如果配置了）
dotnet test *.sln -c Server-Debug --collect:"XPlat Code Coverage"
```

### 查看测试结果

测试报告将生成在 `TestResults/` 目录中，可以使用 Visual Studio 或其他工具查看。

## 🔗 相关文档

- [快速开始](QuickStart.md) - 项目设置和基本构建
- [项目结构](ProjectStructure.md) - 了解项目依赖关系
- [编码规范](../CONVENTIONS.md) - 测试代码编写规范
- [框架概述](../FRAMEWORK_OVERVIEW.md) - 架构设计理解

## 📝 贡献测试

### 添加新测试

1. 在相应的测试项目中创建测试类
2. 遵循命名规范和最佳实践
3. 确保使用解决方案级别命令验证测试
4. 更新相关文档

### 测试覆盖率目标

- **核心系统**：> 80%
- **工具类**：> 90%
- **关键业务逻辑**：> 95%

---

> ⚠️ **重要提醒**：始终使用 `dotnet test *.sln -c Server-Debug` 或 `dotnet test *.sln -c Client-Debug` 进行测试，避免直接测试单个项目导致的构建失败。 