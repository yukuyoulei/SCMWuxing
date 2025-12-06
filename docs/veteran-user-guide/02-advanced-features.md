# 高级功能

本文档介绍星火编辑器2.0中的高级功能，包括扩展枚举、效果节点、响应节点、通用公式等。

## 目录

- [扩展枚举](#扩展枚举)
- [效果节点](#效果节点)
- [响应节点](#响应节点)
- [通用公式](#通用公式)
- [技能指示器](#技能指示器)

---

## 扩展枚举

在2.0中，枚举扩展功能被扩展到了整个枚举类型级别。用户可以在代码或将来的触发器中定义全新的可扩展枚举，或是扩展已有的可扩展枚举。

### 基本用法

`[EnumExtension]` 代码生成特性可用于在编译时生成扩展枚举的代码。`Extends` 属性用于指定要扩展的枚举类型。

#### 扩展现有枚举

以下代码为 `PropertyUnit` 枚举添加了三个新的枚举值：

```csharp
[EnumExtension(Extends = typeof(PropertyUnit))]
internal enum EGameplayPropertyUnit
{
    MagicPower,
    CriticalRate,
    Inventory,
}
```

此后，所有需要输入 `PropertyUnit` 枚举值的地方，都可以输入 `GameplayPropertyUnit` 枚举值：

```csharp
unit.SetProperty(PropertyUnit.Health, 100.0); // PropertyUnit.Health是内置的枚举值
unit.SetProperty(GameplayPropertyUnit.MagicPower, 100.0); // GameplayPropertyUnit.MagicPower是扩展枚举值
unit.SetProperty(GameplayPropertyUnit.CriticalRate, 0.15f); 
unit.SetProperty(GameplayPropertyUnit.Inventory, inventoryData);
```

### 命名规则

⚠️ **重要**：在代码模式下，任何使用了 `[EnumExtension]` 特性的枚举名称都必须以 **E** 开头，比如 `EGameplayPropertyUnit`。生成的扩展枚举名称会自动去除E前缀，比如 `GameplayPropertyUnit`。

此外，`[EnumExtension]` 特性仅对**直属于命名空间**的枚举有效，不支持扩展在类或结构体中定义的枚举。

### 创建新的可扩展枚举

`[EnumExtension]` 特性还支持构造新的可扩展枚举。使用 `Extendable` 属性指定该枚举为可扩展枚举：

```csharp
[EnumExtension(Extendable = true)]
internal enum EMyExtendableEnum
{
    MyTest,
    MyTest2,
    MyTest3,
}

// 代码生成的可扩展枚举会去掉E前缀，使用时应当使用 MyExtendableEnum 类型
public void Test(MyExtendableEnum enumValue)
{
    // ...
}
```

### 扩展自定义可扩展枚举

依赖当前库的项目中，可以自行扩展已有的可扩展枚举：

```csharp
[EnumExtension(Extends = typeof(EMyExtendableEnum))]
internal enum EMyExtendableEnumExtension
{
    MyTest4,
    MyTest5,
    MyTest6,
}

// 可以在需要 MyExtendableEnum 的地方传入 MyExtendableEnumExtension 的实例
Test(MyExtendableEnumExtension.MyTest4);
Test(MyExtendableEnumExtension.MyTest5);
Test(MyExtendableEnumExtension.MyTest6);
```

### 可继承的扩展关系

`[EnumExtension]` 特性的 `Extendable` 属性和 `Extends` 属性可以同时使用，代表当前枚举在扩展另一个枚举的同时可以被其它枚举扩展。

扩展关系是可继承的，即孙辈的枚举值可以填充祖辈的枚举值参数。

### 深入学习

更多扩展枚举的详细信息，请参阅：
- [扩展枚举文档](../tools/EnumExtension.md) - 完整的扩展枚举文档

---

## 效果节点

星火编辑器2.0允许用户自定义新的效果节点。与1.0版本不同的是，2.0版本的效果节点的实现是可以被继承和复用的。

### 基本概念

数编效果节点表和普通的数编表没有本质的不同，`[GameDataNodeType<T,V>]` 特性同样适用于效果节点表。

### 内置效果节点示例

以下是内置杀死单位效果节点的实现示例，用户可以模仿它来实现自己的效果节点：

```csharp
namespace GameCore.Execution.Data;

[GameDataNodeType<GameDataEffect, GameDataEffectUnit>]
public partial class GameDataEffectUnitKill
{
    public DeathType DeathType { get; set; } = DeathType.Normal;
    
#if SERVER
    // 重载Execute方法来实现杀死单位效果节点的逻辑
    public override void Execute(Effect context)
    {
        // Execute方法的Effect参数就是效果节点实例
        // 其Target属性指向的是效果节点的目标
        // 效果节点的目标是ITarget类型的接口，可以是单位或点
        var entity = context.Target?.Entity!;
        entity.Kill(DeathType, context);
    }
    // Validate方法也可以被重载，但GameDataEffectUnitKill的GameDataEffectUnit基类已经实现了验证逻辑
    // 因此这里不需要重载
#endif
}
```

### 关键点说明

- **Execute方法**：效果节点的核心逻辑方法
  - `Effect context` 参数是效果节点实例
  - `context.Target` 指向效果节点的目标（可以是单位或点）
  - `context.Target.Entity` 获取目标实体

- **Validate方法**：可选的验证方法，用于检查效果节点执行前的条件

### CreateEffect方法

与单位、技能、物品、buff等数编表类似，效果节点表也有 `CreateEffect` 方法，用于创建效果节点实例。

效果节点实例的基类是 `Effect` 类型。用户可以酌情重载 `CreateEffect` 方法来创建自己的效果节点实例类型。

部分内置效果节点表也重载了 `CreateEffect` 方法，比如 `GameDataEffectDamage` 重载了 `CreateEffect` 方法来创建 `EffectDamage` 实例，相比起普通的 `Effect` 对象，额外保存了对伤害实例的引用。

### 设计思路

效果节点与响应节点的基本设计思路与用法承袭1.0的传统，但功能更广泛，可以获取的信息也更为复杂。

---

## 响应节点

响应节点是处理特定类型事件的特殊节点，可以拦截和修改游戏逻辑的执行流程。

### 内置响应节点示例

以下是内置伤害响应节点的实现示例，用户可以模仿它来实现自己的响应节点：

```csharp
namespace GameCore.Behavior;

[GameDataNodeType<GameDataResponse, GameDataResponse<GameDataResponseDamage, ResponseContextDamage>>]
public partial class GameDataResponseDamage
{
    public ResponseDamageFlags ResponseDamageFlags = new();
    public Dictionary<IGameLink<GameDataDamageType>, bool> DamageType = new()
    {
        [ScopeData.DamageType.Physical] = true,
        [ScopeData.DamageType.Magical] = true,
    };
    
    // 伤害响应节点的公式字段
    // 用户可以在这里编写公式来计算伤害响应节点的结果
    public Func<ResponseContextDamage, double>? Modification { get; set; }
    public Func<ResponseContextDamage, double>? Multiplier { get; set; }

#if SERVER
    protected internal override ResponseContext? CreateResponseContext(
        Response response, IExecutionContext incomingContext, ITarget? defenderOverride)
    {
        return new ResponseContextDamage(response, 
            incomingContext as EffectDamage ?? throw new InvalidCastException("IncomingContext is not EffectDamage"));
    }
    
    public override CmdResult Execute(ResponseContextDamage responseContext)
    {
        var damage = responseContext.Damage;
        
        if (ResponseDamageFlags.Fatal != damage.IsFatal)
        {
            return CmdError.NotSupported;
        }
        if (damage.Current <= 0 && !ResponseDamageFlags.HandleNullifiedDamage)
        {
            return CmdError.NotSupported;
        }
        if (damage.Origin <= 0 && !ResponseDamageFlags.HandleZeroDamage)
        {
            return CmdError.NotSupported;
        }
        if (!DamageType.TryGetValue(damage.Type, out var value) || !value)
        {
            return CmdError.NotSupported;
        }
        
        damage.Current += Modification?.Invoke(responseContext) ?? 0;
        if (Multiplier is not null)
        {
            damage.Current *= Multiplier(responseContext);
        }
        if (ResponseDamageFlags.SetAsCrit)
        {
            damage.IsCritical = true;
        }
        
        return CmdResult.Ok;
    }
#endif
}

// ResponseContextDamage类是伤害响应节点的上下文类
// 用于存储伤害响应节点的相关信息
public class ResponseContextDamage(Response response, EffectDamage effectDamage)
    : ResponseContext(response, effectDamage)
{
    /// <summary>
    /// 引发响应的伤害效果
    /// </summary>
    public EffectDamage EffectDamage => IncomingContext as EffectDamage 
        ?? throw new InvalidCastException("IncomingContext is not EffectDamage");
        
    /// <summary>
    /// 引发响应的伤害实例
    /// </summary>
    public Damage Damage => EffectDamage.Damage 
        ?? throw new InvalidOperationException("EffectDamage.Damage is null");
}
```

### 关键点说明

#### 响应上下文

注意到 `GameDataResponseDamage` 的 `[GameDataNodeType<T,V>]` 特性参数中，V为 `GameDataResponse<GameDataResponseDamage, ResponseContextDamage>`。

这额外的参数代表响应节点表的配置方式比普通数编表略为复杂，需要指定响应的上下文类型。这一机制使不同类型的响应节点公式字段可以获取不同的上下文。

比如单位死亡响应可以实现 `ResponseContextDeath`，为响应公式字段传递单位死亡的相关信息。

#### 公式字段

响应节点的公式字段可以使用响应上下文参数来获取比1.0版本更丰富的信息：

```csharp
public Func<ResponseContextDamage, double>? Modification { get; set; }
public Func<ResponseContextDamage, double>? Multiplier { get; set; }
```

在公式字段内，用户可以使用 `ResponseContextDamage` 来获取伤害效果、伤害实例等详细信息。

### 发送响应事件

自定义的响应与自定义触发器事件类似，需要主动发送事件来引发响应。响应通过 `Post()` 方法来引发：

```csharp
GameDataResponseDamage.Post(effectDamage);
```

---

## 通用公式

在2.0版本的框架中，许多数编公式字段现在都支持服务器和客户端共同定义。

### 服务器客户端共享的API

物品和Buff的属性加成公式字段现在可以使用 `#if SERVER` 和 `#if CLIENT` 条件编译指令来分别定义服务器和客户端的公式，也可以使用服务器客户端共享的API来构造通用公式。

### Buff属性加成示例

例如，可以将Buff的生命最大值加成公式定义为目标的力量属性值×2：

```csharp
new GameDataBuff(buffLink)
{
    Modifications = [
        new () {
            Property = ScopeData.UnitProperty.LifeMax,
            Value = (context) => context.Target.GetUnitPropertyFinal(ScopeData.UnitProperty.Strength) * 2,
        }
    ],
};
```

### 通用公式的优势

上面的代码中，`context.Target.GetUnitPropertyFinal(ScopeData.UnitProperty.Strength) * 2` 是一个通用公式，它在服务器和客户端都会被计算。

#### IExecutionContext接口

此类通用公式的参数不再使用效果节点，而是一个新的 `IExecutionContext` 接口类型。它可以获取施法者和目标，但无法获取整个效果节点树的信息。

#### 客户端显示

对于客户端来说，Buff所属单位的单位属性是客户端可以获取的，因此客户端可以计算出这个Buff的最大生命值加成。

Buff或者物品加成界面不会再像1.0版本那样，无法显示使用了公式的属性加成了。

---

## 技能指示器

技能指示器在星火编辑器2.0中被重构为使用表现Actor来实现。

现在用户可以像操作普通表现那样操作技能指示器。

⚠️ **注意**：详细的技能指示器API和使用方法请参考相关示例代码和API文档。

---

## 下一步

了解了高级功能后，建议继续阅读：
- [UI系统](./03-UI-SYSTEM.md) - 了解2.0版本强大的UI创建方式
- [当前版本限制](./04-LIMITATIONS.md) - 了解当前版本的限制和未来展望

