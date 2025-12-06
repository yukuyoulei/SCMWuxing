# PropertyObject 系统

## 概述

`PropertyObject` 是 WasiCore 框架提供的无场景属性同步对象，用于在服务器和客户端之间同步自定义属性。

### 核心特性

- ✅ **无场景依赖** - 不需要 3D 场景、位置坐标或朝向
- ✅ **动态创建** - 可随时创建和销毁，无需在游戏初始化时固定
- ✅ **轻量级** - 底层使用 `CoreActor` 机制，无场景管理开销
- ✅ **无需数编** - 不需要配置 `GameDataUnit` 或提供 LinkHash
- ✅ **灵活同步** - 支持 `SyncType.Self`（自己）/`Ally`（队伍）/`All`（所有人）等同步方式
- ✅ **自动生成包装器** - PropertyObjectWrapper 源代码生成器，零样板代码

### 与 Entity 的对比

| 特性 | Entity | PropertyObject |
|------|--------|----------------|
| 需要场景 | ✅ 是 | ❌ 否 |
| 需要位置 | ✅ 是 | ❌ 否 |
| 需要 LinkHash | ✅ 是 | ❌ 否 |
| 动态创建 | ✅ 是 | ✅ 是 |
| 属性同步 | ✅ 是 | ✅ 是 |
| 场景表现 | ✅ 自动 | ❌ 无（客户端自行处理UI） |
| 物理碰撞 | ✅ 是 | ❌ 否 |
| 适用场景 | 3D 游戏 | **2D 联机游戏** ⭐ |

## 🎮 2D 联机游戏的理想选择

### 为什么 PropertyObject 适合 2D 联机游戏？

2D 联机游戏（卡牌对战、在线回合制、多人放置类等）通常具有以下特点：

- ❌ 不需要真实的 3D 场景空间
- ❌ 不需要物理碰撞和寻路
- ❌ 不需要战争迷雾（FOW）
- ✅ **需要在服务端和客户端之间同步游戏状态**
- ✅ **只需要属性同步和游戏逻辑**

PropertyObject 完美匹配这些需求：

```csharp
// 传统方式（过度设计）
var card = cardData.CreateUnit(player, new ScenePoint(scene, 0, 0, 0), Angle.Zero);
// ❌ 2D联机游戏不需要3D坐标、场景、朝向
// ❌ 需要复杂的 GameDataUnit 配置

// PropertyObject 方式（简洁高效）
var card = new PropertyObject(player, SyncType.All);
card.SetPropertyGeneric<PropertyCard, int>(PropertyCard.CardId, 101);
card.OrderIndex = 3;  // 手牌第3个位置
// ✅ 直接创建，无需配置
```

**注意：** 单机游戏不需要 PropertyObject，因为单机游戏无需同步机制，可以直接在客户端管理所有数据和逻辑。

### 适用的 2D 联机游戏类型

#### 1. 多人卡牌游戏 ⭐⭐⭐⭐⭐

```csharp
var card = new PropertyObject(player, SyncType.All);
card.SetPropertyGeneric<PropertyCard, int>(PropertyCard.CardId, 101);
card.SetPropertyGeneric<PropertyCard, int>(PropertyCard.Attack, 5);
card.OrderIndex = handSlot;  // 手牌槽位
card.GroupId = 1;            // 区域（1=手牌，2=战场，3=墓地）

```

**适用游戏**：炉石传说类、游戏王类、万智牌类

#### 2. 在线回合制策略游戏 ⭐⭐⭐⭐⭐

```csharp
var gridUnit = new PropertyObject(player, SyncType.All);
gridUnit.SetPropertyGeneric<PropertyGrid, int>(PropertyGrid.X, 5);
gridUnit.SetPropertyGeneric<PropertyGrid, int>(PropertyGrid.Y, 3);
gridUnit.SetPropertyGeneric<PropertyGrid, int>(PropertyGrid.Health, 100);

// 或使用 OrderIndex 编码坐标
gridUnit.OrderIndex = x * 100 + y;  // 编码二维坐标
```

**适用游戏**：火焰纹章类、高级战争类、棋类游戏

#### 3. 多人放置/挂机游戏 ⭐⭐⭐⭐⭐

```csharp
var idleHero = new PropertyObject(player, SyncType.Self);
idleHero.SetPropertyGeneric<PropertyIdle, long>(PropertyIdle.TotalDPS, dps);
idleHero.SetPropertyGeneric<PropertyIdle, int>(PropertyIdle.Level, level);

// 客户端根据数值自行显示UI，无需场景表现
```

**适用游戏**：放置奇兵类、剑与远征类

#### 4. 在线 2D 塔防 ⭐⭐⭐⭐

```csharp
var tower = new PropertyObject(player, SyncType.All);
tower.OrderIndex = pathNodeIndex;  // 路径节点索引

var enemy = new PropertyObject(Player.DefaultPlayer, SyncType.All);
enemy.SetPropertyGeneric<PropertyEnemy, float>(PropertyEnemy.PathProgress, 0.5f);
```

#### 5. 在线文字/养成游戏 ⭐⭐⭐⭐

```csharp
var pet = new PropertyObject(player, SyncType.Self);
pet.SetPropertyGeneric<PropertyPet, int>(PropertyPet.Happiness, 80);
pet.SetPropertyGeneric<PropertyPet, int>(PropertyPet.Hunger, 60);
```

## 基本使用

### 创建和销毁

```csharp
#if SERVER
// 创建 PropertyObject
var obj = new PropertyObject(player, SyncType.Self);

// 使用完毕后销毁
obj.Destroy();
#endif
```

### 属性操作

PropertyObject 支持三种属性使用方式：

#### 方式1：默认属性枚举（基础）

```csharp
// 扩展 PropertyPropertyObject 枚举（通过代码生成）
var obj = new PropertyObject(player, SyncType.Self);
obj.SetProperty(PropertyPropertyObject.OrderIndex, 3);
obj.SetProperty(PropertyPropertyObject.GroupId, 1);
```

#### 方式2：自定义属性枚举

```csharp
// 定义专门的属性枚举
[EnumExtension(Extendable = true)]
public enum EPropertyCard
{
    CardId,
    Attack,
    Defense,
}

// 使用泛型方法
var card = new PropertyObject(player, SyncType.All);
card.SetPropertyGeneric<PropertyCard, int>(PropertyCard.CardId, 101);
card.SetPropertyGeneric<PropertyCard, int>(PropertyCard.Attack, 5);

// 读取属性
var attack = card.GetPropertyGeneric<PropertyCard, int>(PropertyCard.Attack);
```

#### 方式3：PropertyObjectWrapper 自动生成包装器（强烈推荐）⭐

```csharp
// 添加 PropertyObjectWrapper 特性，自动生成包装器类
[PropertyObjectWrapper]  // 🔥 触发自动生成
[EnumExtension(Extendable = true)]
public enum EPropertyCard
{
    CardId,
    Attack,
    Defense,
}

// 使用生成的包装器（简洁且类型安全）
var card = new Card(propertyObject);
card.CardId = 101;
card.Attack = 5;
var attack = card.Attack;  // 简洁！
```

**详见**：[最佳实践 - 1.2 使用 PropertyObjectWrapper 自动生成包装器](#12-使用-propertyobjectwrapper-自动生成包装器推荐)

### 内置便捷属性

PropertyObject 提供了两个专为 2D 游戏设计的内置属性：

```csharp
// OrderIndex - 顺序索引（手牌位置、格子索引等）
card.OrderIndex = 3;

// GroupId - 分组ID（区域、阵营、分类等）
card.GroupId = 1;  // 1=手牌区, 2=战场区
```

### 查询和遍历

```csharp
// 根据 ID 查询
var obj = PropertyObject.GetById(objectId);

// 遍历所有 PropertyObject
foreach (var obj in PropertyObject.All)
{
    // 处理逻辑
}
```

## 高级特性


### 支持冷却系统

```csharp
#if SERVER
var card = new PropertyObject(player, SyncType.All);

// 设置冷却
card.SetCooldown(cooldownLink, TimeSpan.FromSeconds(5));
#endif
```

### 支持事件系统

```csharp
// PropertyObject 可以发布和订阅事件
card.GetPublisher<EventCustom>()?.Invoke(new EventCustom(card));
```

## 2D 联机游戏架构模式

### 推荐架构

```csharp
#if SERVER
// 服务端：游戏逻辑层
public class Card2DGameLogic
{
    // 使用 PropertyObject 作为游戏对象
    private Dictionary<int, PropertyObject> deck = new();
    private Dictionary<int, PropertyObject> hand = new();
    
    public PropertyObject DrawCard(Player player)
    {
        var card = new PropertyObject(player, SyncType.All);
        card.SetPropertyGeneric<PropertyCard, int>(PropertyCard.CardId, GetRandomCardId());
        card.OrderIndex = hand.Count;
        card.GroupId = 1;  // 手牌区
        
        hand[card.Id] = card;
        return card;
    }
    
    public void PlayCard(PropertyObject card, PropertyObject target)
    {
        // 移动到战场
        card.GroupId = 2;
        
        // 使用框架的技能系统
        var ability = card.GetAbility(someAbilityLink);
        ability?.Execute(target);
    }
}
#endif

#if CLIENT
// 客户端：UI 表现层
public class Card2DGameUI
{
    // 监听 PropertyObject 属性变化
    public void OnCardPropertyChanged(PropertyObject card)
    {
        var zone = card.GroupId;
        var slot = card.OrderIndex;
        
        // 根据属性更新UI（可播放动画）
        AnimateCardToPosition(card, zone, slot);
    }
}
#endif
```

### 关键设计原则

1. **逻辑与表现分离**
   - 服务端：PropertyObject 管理游戏逻辑和属性
   - 客户端：监听属性变化，自行处理 UI 表现

2. **使用 OrderIndex 和 GroupId 替代场景坐标**
   - OrderIndex：槽位、索引、一维位置
   - GroupId：区域、分类、阵营

3. **充分利用框架能力**
   - 冷却系统：技能 CD、回合限制
   - 事件系统：游戏逻辑通知

## 性能优势

### 相比 Entity 的性能对比

| 维度 | Entity | PropertyObject | 优势说明 |
|------|--------|----------------|---------|
| 内存占用 | 较大 | **较小** | 无场景、物理、导航数据 |
| 创建开销 | 较高 | **较低** | 无需场景初始化和物理组件 |
| 同步带宽 | 较大 | **较小** | 不同步位置、朝向、动画等 |
| 配置复杂度 | 需要 LinkHash | **无需配置** | 无需创建 GameDataUnit |
| 适用对象数量 | 中等 | **大量** | 可创建更多实例 |

**注意**：具体性能取决于使用场景和属性数量。PropertyObject 的核心优势是**简化开发流程**，而非单纯的性能提升。

### 适合大量对象的场景

PropertyObject 的轻量特性使其特别适合需要大量对象的 2D 联机游戏：

- **多人卡牌游戏**：每个玩家的手牌、牌库、战场单位（可能数百个对象）
- **在线放置游戏**：英雄池、装备池、技能池（需要同步大量数据）
- **联机背包系统**：数百个物品需要在玩家间同步
- **实时动作游戏**：子弹、特效、道具等短生命周期对象

## 示例代码

完整的使用示例请参考：

- **FlappyBird 多人版**：`Tests/Game/FlappyBirdMultiplayer/` - 完整的 2D 联机游戏实现
  - 展示了 PropertyObjectWrapper 自动生成包装器的完整用法
  - 包含服务端逻辑、客户端渲染、业务方法扩展等最佳实践
- **Simple2DMultiplayerGame 框架**：[Framework.md](./Framework.md) - 框架文档和更多示例

## API 参考

### 构造函数

```csharp
// 服务端创建
PropertyObject(Player owner, SyncType syncType = SyncType.Self)
```

### 主要方法

#### 属性操作

```csharp
// 方式1：默认属性枚举
void SetProperty<TValue>(PropertyPropertyObject property, TValue value)
TValue? GetProperty<TValue>(PropertyPropertyObject property)

// 方式2：自定义属性枚举（推荐）
void SetPropertyGeneric<TProperty, TValue>(TProperty property, TValue value)
TValue? GetPropertyGeneric<TProperty, TValue>(TProperty property)
```

#### 内置属性

```csharp
int? OrderIndex { get; set; }  // 顺序索引
int? GroupId { get; set; }     // 分组ID
```

#### 查询

```csharp
static PropertyObject? GetById(int id)
static IEnumerable<PropertyObject> All { get; }
```

#### 生命周期

```csharp
void Destroy()      // 服务端销毁
bool IsValid { get; } // 检查有效性
```

## 最佳实践

### 1. 为不同用途定义专门的属性枚举

```csharp
// ✅ 推荐：类型安全，语义清晰
[EnumExtension(Extendable = true)]
public enum EPropertyCard
{
    CardId,
    Attack,
    Defense,
}

// 使用时
card.SetPropertyGeneric<PropertyCard, int>(PropertyCard.Attack, 5);
var attack = card.GetPropertyGeneric<PropertyCard, int>(PropertyCard.Attack);
```

### 1.1 使用包装器模式简化访问

```csharp
// ✅ 更推荐：使用包装器类封装 PropertyObject
public class Card
{
    private readonly PropertyObject _obj;
    
    public Card(PropertyObject obj) { _obj = obj; }
    
    // 属性访问更简洁
    public int Attack
    {
        get => _obj.GetPropertyGeneric<PropertyCard, int>(PropertyCard.Attack) ?? 0;
        #if SERVER
        set => _obj.SetPropertyGeneric<PropertyCard, int>(PropertyCard.Attack, value);
        #endif
    }
}

// 使用
var card = new Card(propertyObject);
card.Attack = 5;  // 简洁！
```

### 1.2 使用 PropertyObjectWrapper 自动生成包装器（推荐）⭐

手动创建包装器仍然需要大量样板代码。WasiCore 提供了 **PropertyObjectWrapper 源代码生成器**，可以自动生成包装器类。

#### 使用方式

```csharp
// 步骤1：定义属性枚举并标记特性
[PropertyObjectWrapper]  // 🔥 触发自动生成！
[EnumExtension(Extendable = true)]
public enum EPropertyCard
{
    CardId,        // → int（自动推断）
    Attack,        // → int（默认）
    Defense,       // → int（默认）
    Health,        // → float（包含health，自动推断）
    IsActive,      // → bool（以Is开头，自动推断）
}

// 步骤2：框架自动生成 Card.g.cs 类
// 包含以下内容：
// - 所有属性的 getter/setter
// - 构造函数 Card(PropertyObject obj)
// - OrderIndex 和 GroupId 便捷属性
// - Destroy() 方法
// - 服务端字段缓存优化

// 步骤3：使用 partial class 扩展业务方法
public partial class Card
{
    // 添加业务逻辑（属性由生成器自动生成）
    public void ApplyDamage(int damage)
    {
        Health -= damage;  // 使用生成的属性
        if (Health <= 0)
        {
            IsActive = false;
        }
    }
    
    public void Attack(Card target)
    {
        target.ApplyDamage(Attack);
    }
}

// 步骤4：使用包装器
var card = new Card(propertyObject);
card.Attack = 5;           // 简洁的属性访问
card.Health = 100f;
card.ApplyDamage(20);      // 调用业务方法
```

#### 自动类型推断

生成器会根据属性名智能推断类型：

| 关键词 | 推断类型 | 示例 |
|--------|---------|------|
| `id`, `count`, `index` | `int` | CardId, PlayerCount |
| `x`, `y`, `velocity`, `speed` | `float` | PositionY, MoveSpeed |
| `duration`, `time`, `delay`, `cooldown` | `float` | GameDuration, CooldownRemaining |
| `health`, `damage`, `armor`, `energy` | `float` | MaxHealth, AttackDamage |
| `is...`, `has...`, `can...`, `alive` | `bool` | IsAlive, HasWeapon, CanAttack |

**显式指定类型**：
```csharp
[PropertyObjectWrapper]
public enum EPropertyCard
{
    CardId,                          // → int（自动推断）
    
    [PropertyType(typeof(string))]
    CardName,                        // → string（显式指定）
    
    [PropertyType(typeof(double))]
    PreciseValue,                    // → double（显式指定）
}
```

#### 生成的代码特性

自动生成的包装器类包含以下优化：

**服务端优化**：
```csharp
// 服务端使用字段缓存，避免重复调用 GetPropertyGeneric
#if SERVER
private float _health;

public float Health
{
    get => _health;  // 直接返回缓存
    set
    {
        _health = value;
        PropertyObject.SetPropertyGeneric<PropertyCard, float>(PropertyCard.Health, value);
    }
}
#endif
```

**客户端优化**：
```csharp
// 客户端直接读取 PropertyObject（无缓存开销）
#if CLIENT
public float Health
{
    get => PropertyObject.GetPropertyGeneric<PropertyCard, float>(PropertyCard.Health) ?? 0f;
}
#endif
```

#### 优势总结

| 项目 | 手动包装器 | PropertyObjectWrapper | 提升 |
|------|-----------|----------------------|------|
| **代码量** | 100行 | 1行特性标记 | **99%** |
| **维护成本** | 每次修改枚举需同步修改包装器 | 自动同步 | **零维护** |
| **类型安全** | ✅ | ✅ | 相同 |
| **性能优化** | 需手动实现缓存 | 自动生成缓存 | 自动 |
| **开发时间** | 10-15分钟 | 5秒 | **200倍** |

#### 详细文档

完整的类型推断规则请参考：
- [类型推断规则](./TypeInference.md) - 完整的类型推断规则和示例

### 2. 使用 OrderIndex 和 GroupId

```csharp
// ✅ 2D 位置管理
card.OrderIndex = 3;   // 第3个槽位
card.GroupId = 1;      // 手牌区
```

**推荐组合使用**：
```csharp
// 使用包装器 + OrderIndex/GroupId
var card = new Card(propertyObject);
card.OrderIndex = 3;   // 便捷属性（生成器自动包含）
card.GroupId = 1;
```

### 3. 客户端 UI/逻辑分离

```csharp
#if CLIENT
// ✅ 客户端监听属性，自行更新UI
void UpdateCardUI(PropertyObject card)
{
    var zone = card.GroupId;
    var slot = card.OrderIndex;
    AnimateCardMove(card, zone, slot);
}
#endif
```

### 4. 正确管理生命周期

```csharp
#if SERVER
// ✅ 使用完毕后销毁
card.Destroy();

// ✅ 避免使用已销毁的对象
if (card.IsValid)
{
    card.SetProperty(...);
}

// ⚠️ 注意：PropertyObject 不实现 IDisposable，不能使用 using
// 必须手动调用 Destroy()
#endif
```

### 5. Category 属性的妙用

```csharp
#if SERVER
// 使用 Category 区分对象类型
private const int CategoryCard = 1;
private const int CategoryHero = 2;

var card = new PropertyObject(player, SyncType.All);
card.Category = CategoryCard;
#endif

#if CLIENT
// 客户端根据 Category 分类处理
foreach (var obj in PropertyObject.All)
{
    switch (obj.Category)
    {
        case CategoryCard:
            DrawCard(obj);
            break;
        case CategoryHero:
            DrawHero(obj);
            break;
    }
}
#endif
```

## 架构优势总结

### 对 3D 游戏

PropertyObject 作为 Entity 的补充，用于：
- 跨场景的全局数据
- 玩家级别的数据同步
- 临时会话对象

### 对 2D 联机游戏 ⭐

PropertyObject 可作为**主要游戏对象**，提供：
- 轻量级的联机同步机制
- 完整的游戏系统支持（属性、冷却、事件）
- 更低的开发复杂度
- 更优的运行性能

**重要：** 单机 2D 游戏不需要 PropertyObject，应直接在客户端管理数据，无需服务端同步开销。

## 技术特性

### 同步机制

- 属性变化自动同步到客户端
- 支持增量同步，节省带宽
- 支持灵活的可见性控制（SyncType）
- 客户端会自动复制服务端创建的 PropertyObject

### 生命周期

- 服务端创建和销毁
- 客户端自动复制和清理
- 支持通过 ID 查询已创建的对象

## 常见问题

### Q: PropertyObject 和 Entity 应该如何选择？

**使用 PropertyObject**：
- ✅ 2D 联机游戏（卡牌、棋类、放置等）
- ✅ 不需要 3D 场景和物理
- ✅ 纯数据同步需求
- ✅ 需要大量动态创建的对象

**使用 Entity**：
- ✅ 3D 游戏
- ✅ 需要场景位置、朝向
- ✅ 需要物理碰撞、寻路
- ✅ 需要战争迷雾（FOW）

### Q: PropertyObject 会自动同步到客户端吗？

**是的**，但取决于 SyncType：

**2D 游戏常用的 SyncType**：
- `SyncType.All` - 同步到所有玩家（适用于公共对象）
- `SyncType.Self` - 只同步到对象所有者（适用于私密数据）
- `SyncType.Ally` - 同步到对象所有者和同队玩家（适用于队伍游戏）

**3D 游戏特有的 SyncType**（PropertyObject 不推荐使用）：
- `SyncType.Sight` - 基于视野同步（需要战争迷雾，2D游戏无此概念）
- `SyncType.SelfOrSight` - 自己或视野内
- `SyncType.AllyOrSight` - 盟友或视野内

**注意**: 对于 2D 游戏，建议只使用 `Self`、`Ally`、`All` 三种。

### Q: 什么时候使用 SyncType.Ally？

**使用场景**: 队伍/阵营游戏

```csharp
// 队伍标记（只让队友看到）
var teamMarker = CreateGameObject(player, SyncType.Ally);
teamMarker.SetProperty(PropertyTeam.IsLeader, true);

// 队伍资源（队友共享）
var teamResource = CreateGameObject(player, SyncType.Ally);
teamResource.SetProperty(PropertyTeam.SharedGold, 1000);
```

**适用游戏**:
- MOBA 类游戏（队友信息共享）
- 团队竞技游戏（队伍标记）
- 合作游戏（队伍资源）

### Q: 如何在客户端监听 PropertyObject 的创建？

```csharp
#if CLIENT
protected override void OnPropertyObjectCreated(PropertyObject obj)
{
    // 当 PropertyObject 被复制到客户端时调用
    if (obj.Category == CategoryCard)
    {
        CreateCardUI(obj);
    }
}
#endif
```

### Q: PropertyObject 的属性变化会自动通知客户端吗？

**会的**。当服务端修改 PropertyObject 的属性时，变化会自动同步到客户端。客户端在渲染循环中读取最新的属性值即可。

### Q: 可以在客户端修改 PropertyObject 吗？

**不可以**。PropertyObject 的创建和修改只能在服务端进行。客户端只能读取。这确保了服务端权威，防止作弊。

### Q: 如何简化 PropertyObject 的属性访问？

**推荐使用 PropertyObjectWrapper 自动生成包装器**：

```csharp
// 步骤1：添加特性标记
[PropertyObjectWrapper]
[EnumExtension(Extendable = true)]
public enum EPropertyCard
{
    CardId,
    Attack,
    Health,
}

// 步骤2：使用生成的包装器
var card = new Card(propertyObject);
card.Attack = 5;        // 简洁！
var health = card.Health;
```

相比手动调用 `GetPropertyGeneric`/`SetPropertyGeneric`，包装器提供：
- ✅ 简洁的属性语法
- ✅ 类型安全
- ✅ IntelliSense 支持
- ✅ 服务端自动缓存优化

详见下文 [最佳实践 - 1.2 PropertyObjectWrapper](#12-使用-propertyobjectwrapper-自动生成包装器推荐)

## 注意事项

### 1. 服务端权威
```csharp
#if SERVER
// ✅ 只在服务端创建和修改
var obj = new PropertyObject(player, SyncType.All);
obj.SetProperty(...);
#endif

#if CLIENT
// ❌ 客户端不能创建或修改
// 只能读取
var value = obj.GetProperty(...);
#endif
```

### 2. 属性类型限制
PropertyObject 支持的属性类型：
- ✅ 基本类型：`int`, `long`, `float`, `double`, `bool`, `string`
- ✅ 枚举类型
- ✅ 自定义结构体（需要可序列化）
- ❌ 引用类型（除了 `string`）

### 3. 性能考虑
- 避免创建过多 PropertyObject（虽然比 Entity 轻量，但仍有开销）
- 避免频繁修改属性（每次修改都会触发同步）
- 合理使用 SyncType 减少不必要的同步（使用 `Self`/`Ally` 而非全部用 `All`）

## 相关文档

### 🎓 推荐阅读顺序

**新手入门**:
1. [5分钟快速教程](./QUICKSTART.md) - 最简单的 Pong 游戏
2. 本文档 - PropertyObject 基础概念
3. [Simple2DMultiplayerGame 框架](./Framework.md) - 框架完整介绍

**遇到问题**:
- [常见错误速查表](./CommonMistakes.md) - 快速查找解决方案

**深入学习**:
- [FlappyBird 多人版示例](../../Tests/Game/FlappyBirdMultiplayer/) - 完整示例项目
- [类型推断规则](./TypeInference.md) - PropertyObjectWrapper 类型推断规则
- [SyncType 参考](./SyncType.md) - SyncType 选择指南

### 其他系统
- [Entity 系统](../systems/EntitySystem.md) - 场景实体对象（3D游戏）
- [属性系统](../systems/PropertySystem.md) - 属性同步机制

