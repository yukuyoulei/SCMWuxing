# IGameLink 比较指南

## 问题背景

C# 接口无法重载运算符（`==`, `!=`），这意味着当比较 `IGameLink` 实例时，使用 `==` 运算符会进行引用比较而不是逻辑相等比较，导致意外的行为。

## 正确的比较方式

### ✅ 正确做法

```csharp
// 使用 .Equals() 方法
if (buff.Link.Equals(BuffLink))
{
    // 正确的逻辑相等比较
}

// 处理可能为 null 的情况
if (mover.Cache?.Link?.Equals(Mover) ?? false)
{
    // 安全的比较
}
```

### ❌ 错误做法

```csharp
// 不要在 IGameLink 级别使用 == 运算符
if (buff.Link == BuffLink)  // 这是引用比较，不是逻辑比较！
{
    // 可能不会按预期工作
}
```

## 为什么会有这个问题

1. **接口限制**：C# 接口不能定义运算符重载
2. **具体类有重载**：`GameLink<T,V>` 具体类重载了 `==` 和 `!=` 运算符
3. **类型擦除**：当向上转型为 `IGameLink` 时，编译器使用默认的引用比较

## GameLink<T, V> 具体类型的情况

### ✅ GameLink<T, V> 之间的比较没有问题

当直接使用 `GameLink<T, V>` 具体类型时，`==` 和 `!=` 运算符工作正常：

```csharp
// 这些比较都是安全的，因为使用的是具体类型
GameLink<GameDataBuff, GameDataBuff> buffLink1 = new("TestBuff");
GameLink<GameDataBuff, GameDataBuff> buffLink2 = new("TestBuff");
IGameLink<GameDataBuff> buffLinkInterface = buffLink1;

// ✅ 正确：具体类型之间的比较
if (buffLink1 == buffLink2)  // 使用重载的运算符，逻辑相等比较
{
    // 正常工作
}

// ✅ 正确：具体类型与接口的比较  
if (buffLink1 == buffLinkInterface)  // 使用重载的运算符，逻辑相等比较
{
    // 正常工作
}

// ❌ 问题：接口与接口的比较
IGameLink buffLinkInterface2 = buffLink2;
if (buffLinkInterface == buffLinkInterface2)  // 引用比较！
{
    // 可能不会按预期工作
}
```

### 运算符重载的范围

`GameLink<T, V>` 类提供了以下运算符重载：

- `GameLink<T, V> == IGameLink` ✅
- `IGameLink == GameLink<T, V>` ✅  
- `GameLink<T, V> == IGameLink<T>` ✅
- `IGameLink<T> == GameLink<T, V>` ✅

但 **没有** 提供：
- `IGameLink == IGameLink` ❌ (无法在接口中定义)

## 检查清单

在代码审查中，请检查以下模式：

- [ ] `*.Link == *` 
- [ ] `*.Link != *`
- [ ] `* == *.Link`
- [ ] `* != *.Link`

## 自动化检测

### 🔍 Roslyn 分析器

项目中已集成了专用的 Roslyn 分析器来自动检测 IGameLink 比较问题：

| 规则ID | 严重性 | 描述 |
|--------|--------|------|
| IGAMELINK001 | Warning | 检测 `IGameLink == IGameLink` 比较，建议使用 `.Equals()` |
| IGAMELINK002 | Warning | 检测 `IGameLink != IGameLink` 比较，建议使用 `!.Equals()` |

**分析器特性：**
- ✅ 只检测真正有问题的比较（两边都是接口类型）
- ✅ 忽略安全的比较（涉及 `GameLink<T,V>` 具体类型）
- ✅ 在编译时提供即时反馈
- ✅ 集成在 CodeGenerator 项目中，自动应用到所有项目

## 最佳实践

1. **接口比较使用 `.Equals()`**：当比较 `IGameLink` 接口类型时，总是使用 `.Equals()` 方法
2. **具体类型可以使用 `==`**：`GameLink<T, V>` 具体类型之间或与接口的比较可以安全使用 `==` 运算符
3. **空值检查**：使用 `?.` 操作符处理可能为空的链接
4. **代码注释**：在关键位置添加注释说明比较策略的选择原因
5. **单元测试**：为比较逻辑编写测试确保正确性
6. **类型明确性**：在可能的情况下，优先使用具体的 `GameLink<T, V>` 类型而不是 `IGameLink` 接口

## 总结

### 问题的核心
- ❌ **`IGameLink == IGameLink`**：引用比较，通常不是我们想要的
- ✅ **`GameLink<T,V> == IGameLink`**：逻辑相等比较，正常工作
- ✅ **`IGameLink.Equals(IGameLink)`**：逻辑相等比较，总是安全的

### 记忆要点
> 当你看到涉及 `.Link` 属性的比较时，问自己：
> 1. 比较的两边都是接口类型吗？如果是，使用 `.Equals()`
> 2. 至少有一边是具体的 `GameLink<T,V>` 类型吗？如果是，`==` 是安全的

## 分析器实现

**位置：** `CodeGenerator/IGameLinkComparisonAnalyzer.cs`

该分析器会在编译时自动检测所有 IGameLink 比较问题，无需手动搜索。
