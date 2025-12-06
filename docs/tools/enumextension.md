# 枚举扩展机制 (EnumExtension)

## 概述

`EnumExtension` 是 WasiCore 框架提供的代码生成工具，允许开发者扩展现有的可扩展枚举类型，无需修改原有代码。

## 支持的可扩展枚举

框架中以下枚举支持扩展：

- `PropertyUnit` - 单位基础属性
- `PropertyEntity` - 实体属性  
- `PropertySubType` - 属性子类型
- `ComponentTag` - 组件标签
- `EventType` - 事件类型

## 基本用法

### 语法格式

```csharp
[EnumExtension(Extends = typeof(TargetEnum))]
public enum ECustomEnum
{
    CustomValue1,
    CustomValue2,
    // ...
}
```

### 命名约定

- **源枚举**：必须以 `E` 开头（如 `ECustomPropertyUnit`）
- **生成枚举**：自动去除 `E` 前缀（生成 `CustomPropertyUnit`）
- **值命名**：推荐使用 PascalCase 命名

## 实际示例

### 扩展单位属性

```csharp
[EnumExtension(Extends = typeof(PropertyUnit))]
public enum EGameplayPropertyUnit
{
    // 战斗属性
    AttackPower,      // 攻击力
    DefensePower,     // 防御力
    CriticalRate,     // 暴击率
    
    // 角色属性
    Level,            // 等级
    Experience,       // 经验值
    SkillPoints,      // 技能点数
}
```

### 扩展属性子类型

```csharp
[EnumExtension(Extends = typeof(PropertySubType))]
public enum ECustomPropertySubType
{
    Equipment,        // 装备加成
    Buff,            // Buff效果
    Penalty,         // 负面效果
}
```

## 代码生成结果

编译时，上述代码会生成：

```csharp
// 自动生成的代码（不需要手写）
public static class GameplayPropertyUnit
{
    public static readonly PropertyUnit AttackPower = new PropertyUnit(1000);
    public static readonly PropertyUnit DefensePower = new PropertyUnit(1001);
    public static readonly PropertyUnit CriticalRate = new PropertyUnit(1002);
    // ...
}
```

## 使用生成的扩展

```csharp
// 设置扩展属性
unit.SetProperty(GameplayPropertyUnit.AttackPower, 150.0);
unit.SetProperty(GameplayPropertyUnit.Level, 25);

// 获取扩展属性
var attackPower = unit.GetProperty<double>(GameplayPropertyUnit.AttackPower);
var level = unit.GetProperty<int>(GameplayPropertyUnit.Level);

// 在数值公式系统中使用
propertyComplex.SetFixed(GameplayPropertyUnit.AttackPower, CustomPropertySubType.Equipment, 50.0);
```

## 最佳实践

### 1. 合理分组

```csharp
// 推荐：按功能分组的扩展
[EnumExtension(Extends = typeof(PropertyUnit))]
public enum ECombatPropertyUnit
{
    AttackPower,
    DefensePower,
    CriticalRate,
}

[EnumExtension(Extends = typeof(PropertyUnit))]
public enum ECharacterPropertyUnit
{
    Level,
    Experience,
    SkillPoints,
}
```

### 2. 明确的命名

```csharp
// 推荐：清晰明确的命名
public enum EPlayerAttributeUnit
{
    MaxHealth,           // 最大生命值
    CurrentHealth,       // 当前生命值
    MovementSpeed,       // 移动速度
}

// 避免：模糊的命名
public enum EPlayerAttributeUnit
{
    Value1,    // 不清楚代表什么
    Data,      // 过于模糊
    Temp,      // 临时性命名
}
```

### 3. 文档注释

```csharp
[EnumExtension(Extends = typeof(PropertyUnit))]
public enum EAdvancedPropertyUnit
{
    /// <summary>
    /// 魔法强度，影响魔法技能的威力
    /// 数据类型: double, 范围: 0-1000
    /// </summary>
    MagicPower,
    
    /// <summary>
    /// 魔法抗性，减少受到的魔法伤害
    /// 数据类型: float, 范围: 0.0-1.0
    /// </summary>
    MagicResistance,
}
```

## 编译时行为

### 构建过程

1. **代码分析**：编译器分析所有带有 `EnumExtension` 特性的枚举
2. **ID分配**：自动分配唯一的内部ID（通常从1000开始）
3. **代码生成**：生成对应的静态类和属性
4. **类型注册**：将生成的属性注册到框架的类型系统中

### 冲突检测

框架会自动检测并报告以下冲突：

- **重复命名**：同一扩展目标中的重复属性名
- **ID冲突**：手动指定ID时的冲突
- **类型不匹配**：扩展目标类型错误

## 故障排除

### 常见错误

1. **枚举命名错误**
```
错误：枚举名不以 'E' 开头
[EnumExtension(Extends = typeof(PropertyUnit))]
public enum CustomPropertyUnit  // 错误：应该是 ECustomPropertyUnit
```

2. **目标类型不支持扩展**
```
错误：尝试扩展不可扩展的枚举
[EnumExtension(Extends = typeof(DayOfWeek))]  // 错误：DayOfWeek 不支持扩展
```

3. **循环依赖**
```
错误：扩展枚举之间存在循环引用
[EnumExtension(Extends = typeof(EnumA))]
public enum EEnumB { ... }

[EnumExtension(Extends = typeof(EnumB))]  // 错误：循环引用
public enum EEnumA { ... }
```

### 调试技巧

```csharp
// 检查生成的属性是否正确注册
#if DEBUG
public static void ValidateExtensions()
{
    var attackPower = GameplayPropertyUnit.AttackPower;
    Game.Logger.LogInformation("AttackPower ID: {PropertyId}", attackPower.HashCode);
    Game.Logger.LogInformation("AttackPower Name: {PropertyName}", attackPower.FriendlyName);
}
#endif
```

## 性能考虑

- **编译时开销**：代码生成只在编译时进行，不影响运行时性能
- **内存占用**：生成的静态属性会占用少量内存
- **查找性能**：使用哈希码进行快速查找，性能优秀

## 相关文档

- [单位属性系统](../systems/UnitPropertySystem.md) - 属性系统的详细使用方法
- [代码生成器](CodeGenerator.md) - 代码生成器的详细说明
- [编码规范](../CONVENTIONS.md) - 命名和设计规范 