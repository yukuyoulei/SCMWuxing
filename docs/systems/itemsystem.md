# 📦 物品系统（Item System）

物品系统是 WasiCore 游戏框架中用于管理装备、消耗品、资源和其他游戏物品的核心系统。它提供了完整的物品创建、管理、存储、使用和交互机制，支持复杂的物品属性、槽位限制和动态效果。

## 📋 目录

- [🏗️ 系统概述](#系统概述)
- [📦 核心组件](#核心组件)
- [🎯 物品类型](#物品类型)
  - [📄 基础物品（Item）](#基础物品item)
  - [🎒 可拾取物品（ItemPickable）](#可拾取物品itempickable)
  - [⚔️ 装备物品（ItemMod）](#装备物品itemmod)
- [🏪 容器系统](#容器系统)
  - [🎒 物品栏（Inventory）](#物品栏inventory)
  - [🔲 物品槽（InventorySlot）](#物品槽inventoryslot)
  - [📋 物品栏管理器（InventoryManager）](#物品栏管理器inventorymanager)
- [🔄 物品交互](#物品交互)
- [⚡ 物品效果](#物品效果)
- [🎮 物品指令](#物品指令)
- [🏷️ 物品分类](#物品分类)
- [📊 叠加系统](#叠加系统)
- [🎨 客户端表现](#客户端表现)
- [🛠️ 实用示例](#实用示例)
- [🔧 API 参考](#api-参考)
- [💡 最佳实践](#最佳实践)

## 🏗️ 系统概述

### 架构设计

物品系统采用分层的组件化设计，核心架构如下：

```
GameDataItem → Item → Unit → 场景/容器
     ↓         ↓      ↓        ↓
  物品配置   物品实例  物理表示  存储/显示
```

### 继承层次

```
Item (基础物品)
  ↓
ItemPickable (可拾取物品)
  ↓
ItemMod (装备物品)
```

### 核心特性

- ✅ **多种物品类型** - 支持基础物品、可拾取物品、装备物品等
- ✅ **灵活的容器系统** - 支持多种物品栏和槽位限制
- ✅ **叠加机制** - 支持物品堆叠和数量管理
- ✅ **属性修改** - 装备可以动态修改单位属性
- ✅ **分类过滤** - 基于分类的物品筛选和限制
- ✅ **需求验证** - 支持装备需求和使用条件
- ✅ **物品交互** - 拾取、丢弃、使用、交换等操作
- ✅ **客户端同步** - 完整的状态同步机制

### ⚠️ 重要限制

- **静态数编表** - 物品数编表尽量在编译时预定义，不建议在运行时动态创建
- **服务端-客户端同步** - 所有数编表必须在服务端和客户端都存在，否则会导致同步失败
- **动态物品生成** - 框架的动态物品生成机制尚在开发中，当前只能基于预定义的数编表创建物品实例

## 📦 核心组件

### GameDataItem（物品数编表）

物品的配置数据，定义物品的静态属性：

```csharp
public abstract partial class GameDataItem
{
    /// <summary>显示信息</summary>
    public LocalizedString? DisplayName { get; set; }
    public LocalizedString? Description { get; set; }
    public Icon? Icon { get; set; }
    
    /// <summary>关联的单位数据（物理表示）</summary>
    public required IGameLink<GameDataUnit> Unit { get; set; }
    
    /// <summary>默认品质和等级</summary>
    public uint Quality { get; set; }
    public uint Level { get; set; }
    
    /// <summary>物品分类</summary>
    public List<ItemCategory> Categories { get; set; } = [];
    
    /// <summary>目标过滤器（谁可以拾取）</summary>
    public TargetFilterComplex Filter { get; set; } = [];
    
    /// <summary>创建物品实例</summary>
    public Item CreateItem(ScenePoint scene, Player? player = null);
    protected abstract Item CreateItem(Unit unit);
}
```

### Item（基础物品）

所有物品的基类，提供基础功能：

```csharp
public abstract partial class Item : TagComponent
{
    /// <summary>物品配置引用</summary>
    public IGameLink<GameDataItem> Link { get; }
    public GameDataItem Cache { get; }
    
    /// <summary>关联的单位</summary>
    public Unit Unit { get; }
    
    /// <summary>动态属性</summary>
    public uint Quality { get; set; }
    public uint Level { get; set; }
    
    /// <summary>物品分类</summary>
    public List<ItemCategory> Categories { get; }
    
    /// <summary>分类检查</summary>
    public bool HasCategory(ItemCategory category);
}
```

## 🎯 物品类型

### 📄 基础物品（Item）

最基本的物品类型，提供核心功能：

```csharp
// 基于预定义数编表创建基础物品
public class BasicItemExample
{
    public void CreateBasicItem()
    {
        // ⚠️ 重要：必须使用预定义的数编表，不能运行时动态创建
        var basicItemData = ScopeData.Item.BasicTool; // 预定义的数编表引用
        
        // 在场景中创建物品实例
        var scenePosition = new ScenePoint(Vector3.Zero, currentScene);
        var item = basicItemData.CreateItem(scenePosition, Player.DefaultPlayer);
        
        Game.Logger.LogInfo("创建基础物品: {Item}", item);
    }
    
    /// <summary>
    /// 错误示例：不要这样做！
    /// </summary>
    public void DONT_DO_THIS()
    {
        // ❌ 错误：运行时动态创建数编表会导致客户端同步失败
        var badExample = new GameDataItemBasic
        {
            DisplayName = "动态物品", // 客户端无法获取此数据
            // ... 其他属性
        };
        // 这会导致客户端无法正确显示和处理物品
    }
}
```

### 🎒 可拾取物品（ItemPickable）

可以被拾取和存储在物品栏中的物品：

```csharp
// ⚠️ 注意：以下代码展示的是数编表的配置方式，这些配置必须在编译时预定义
// 不能在运行时动态创建，此处仅为说明配置结构

// 预定义的可拾取物品配置（编译时定义）
public static class PreDefinedItems
{
    // 这样的配置应该在数编表中预先定义
    public static readonly GameDataItemPickable HealthPotion = new()
    {
        DisplayName = "生命药水",
        Description = "恢复100点生命值",
        Icon = ScopeData.Icon.HealthPotion,
        Unit = ScopeData.Unit.PotionBottle,
        
        // 可拾取物品特有属性
        StackStart = 1,      // 初始数量
        StackMax = 50,       // 最大堆叠数量
        CanStack = true,     // 可以堆叠
        CanAbsorb = true,    // 可以被相同物品吸收
        CanDrop = true,      // 可以丢弃
        CanSell = true,      // 可以出售
        KillOnDepleted = true, // 数量为0时销毁
        
        Categories = { ItemCategory.Consumable, ItemCategory.Healing },
        
        // 拾取限制
        Filter = new TargetFilterComplex
        {
            Relation = TargetRelation.Ally, // 只有友军可以拾取
            Categories = { UnitCategory.Player } // 只有玩家可以拾取
        }
    };
}

// 正确的使用方式：基于预定义数编表创建物品实例
public class CorrectItemUsage
{
    public void CreateHealthPotion(ScenePoint location)
    {
        // ✅ 正确：使用预定义的数编表创建物品实例
        var healthPotionData = ScopeData.Item.HealthPotion; // 预定义的数编表
        var potionItem = healthPotionData.CreateItem(location, Player.DefaultPlayer);
        
        Game.Logger.LogInfo("在场景中创建了生命药水: {Item}", potionItem);
    }
    
    public void SpawnMultipleItems(ScenePoint baseLocation, int count)
    {
        // 基于预定义数编表批量创建物品
        for (int i = 0; i < count; i++)
        {
            var offset = new Vector3(i * 2, 0, 0);
            var location = baseLocation + offset;
            
            // 所有物品都基于相同的预定义数编表
            var item = ScopeData.Item.HealthPotion.CreateItem(location);
            
            // 可以修改动态属性（如品质、等级）
            if (item is ItemPickable pickableItem)
            {
                pickableItem.Quality = (uint)(1 + i % 3); // 动态调整品质
            }
        }
    }
}

#if SERVER
// 物品拾取示例
public class ItemPickupSystem
{
    /// <summary>
    /// 尝试拾取物品
    /// </summary>
    public bool TryPickupItem(Unit unit, ItemPickable item)
    {
        var inventoryManager = unit.GetComponent<InventoryManager>();
        if (inventoryManager == null)
        {
            Game.Logger.LogWarning("单位没有物品栏管理器");
            return false;
        }
        
        // 检查是否可以拾取
        if (!item.CanPickUp(inventoryManager))
        {
            Game.Logger.LogInfo("不能拾取物品: {Item}", item.Cache.DisplayName);
            return false;
        }
        
        // 检查距离
        var distance = unit.Position.Distance2D(item.Position);
        if (distance > inventoryManager.PickUpRange)
        {
            Game.Logger.LogInfo("物品太远，无法拾取");
            return false;
        }
        
        // 执行拾取
        bool success = item.PickUp(inventoryManager);
        if (success)
        {
            Game.Logger.LogInfo("{Unit} 拾取了 {Item}", unit.Name, item.Cache.DisplayName);
        }
        
        return success;
    }
}
#endif
```

### ⚔️ 装备物品（ItemMod）

可以装备并提供属性修改和技能的高级物品：

```csharp
// ⚠️ 注意：以下展示的是装备物品的配置结构，必须在编译时预定义
// 不能在运行时动态创建，此处仅为说明配置方式

// 预定义的装备物品配置（编译时定义）
public static class PreDefinedEquipment
{
    // 这样的配置应该在数编表中预先定义
    public static readonly GameDataItemMod FlamingSword = new()
        {
            DisplayName = "炎魔之剑",
            Description = "一把充满火焰力量的魔法武器",
            Icon = ScopeData.Icon.FlamingSword,
            Unit = ScopeData.Unit.Sword,
            
            Quality = 3, // 稀有品质
            Level = 10,
            
            Categories = { ItemCategory.Weapon, ItemCategory.Melee, ItemCategory.Magical },
            
            // 装备需求
            Requirements = new TargetFilterComplex
            {
                MinLevel = 10,
                RequiredStats = new Dictionary<IGameLink<GameDataUnitProperty>, int>
                {
                    { ScopeData.UnitProperty.Strength, 25 },
                    { ScopeData.UnitProperty.Intelligence, 15 }
                }
            },
            
            // 槽位修改数据
            Modifications = new Dictionary<ItemSlotType, IUnitModificationData>
            {
                [ItemSlotType.Equip] = new UnitModificationData
                {
                    Modifications = new List<UnitPropertyModification>
                    {
                        new()
                        {
                            Property = ScopeData.UnitProperty.AttackDamage,
                            Value = static (context) => 45 + context.Item?.Level * 3 ?? 0,
                            Operation = ModificationOperation.Add,
                            SubType = PropertySubType.Equipment
                        },
                        new()
                        {
                            Property = ScopeData.UnitProperty.FireDamage,
                            Value = static (_) => 20,
                            Operation = ModificationOperation.Add
                        }
                    },
                    
                    AddStates = { UnitState.FlameWeapon },
                    
                    // 装备时获得技能
                    GrantedAbility = ScopeData.Ability.FlameStrike
                }
            },
            
            // 使用时释放技能
            ActiveAbility = ScopeData.Ability.FlameBlast,
            
            StackStart = 1,
            StackMax = 1, // 装备不能堆叠
            CanStack = false
        };
}

// 正确的装备物品使用方式
public class CorrectEquipmentUsage
{
    public void CreateAndEquipWeapon(Unit unit, ScenePoint location)
    {
        // ✅ 正确：使用预定义的数编表创建装备实例
        var weaponData = ScopeData.Item.FlamingSword; // 预定义的数编表
        var weapon = weaponData.CreateItem(location, unit.Player) as ItemMod;
        
        if (weapon != null)
        {
            // 可以修改动态属性
            weapon.Quality = 4; // 史诗品质
            weapon.Level = 15;  // 提升等级
            
            Game.Logger.LogInfo("创建了炎魔之剑: {Weapon}", weapon);
        }
    }
    
    public void SpawnRandomWeapons(ScenePoint baseLocation)
    {
        // 基于预定义数编表创建不同品质的武器
        var weaponTypes = new[]
        {
            ScopeData.Item.IronSword,    // 普通剑
            ScopeData.Item.SteelSword,   // 钢制剑
            ScopeData.Item.FlamingSword  // 炎魔之剑
        };
        
        for (int i = 0; i < weaponTypes.Length; i++)
        {
            var location = baseLocation + new Vector3(i * 3, 0, 0);
            var weapon = weaponTypes[i].CreateItem(location) as ItemMod;
            
            if (weapon != null)
            {
                // 随机化属性
                weapon.Quality = (uint)Random.Shared.Next(1, 6);
                weapon.Level = (uint)Random.Shared.Next(1, 21);
            }
        }
    }
}

#if SERVER
// 装备系统示例
public class EquipmentSystem
{
    /// <summary>
    /// 装备物品到指定槽位
    /// </summary>
    public bool EquipItem(Unit unit, ItemMod item, ItemSlotType slotType)
    {
        var inventoryManager = unit.GetComponent<InventoryManager>();
        if (inventoryManager == null) return false;
        
        // 检查装备需求
        if (!item.TestRequirement(unit))
        {
            Game.Logger.LogWarning("{Unit} 不满足装备需求: {Item}", 
                unit.Name, item.Cache.DisplayName);
            return false;
        }
        
        // 查找合适的槽位
        var slot = FindEquipmentSlot(inventoryManager, item, slotType);
        if (slot == null)
        {
            Game.Logger.LogWarning("没有可用的装备槽位");
            return false;
        }
        
        // 装备物品
        var success = slot.Assign(item, ReasonItemAssign.Take);
        if (success)
        {
            Game.Logger.LogInfo("{Unit} 装备了 {Item}", unit.Name, item.Cache.DisplayName);
            
            // 触发装备事件
            OnItemEquipped(unit, item, slot);
        }
        
        return success;
    }
    
    /// <summary>
    /// 卸下装备
    /// </summary>
    public bool UnequipItem(Unit unit, ItemMod item)
    {
        if (item.Slot == null) return false;
        
        var success = item.Slot.Drop(ReasonItemDrop.Swap);
        if (success)
        {
            Game.Logger.LogInfo("{Unit} 卸下了 {Item}", unit.Name, item.Cache.DisplayName);
            OnItemUnequipped(unit, item);
        }
        
        return success;
    }
    
    private InventorySlot? FindEquipmentSlot(InventoryManager manager, ItemMod item, ItemSlotType slotType)
    {
        foreach (var inventory in manager.Inventories)
        {
            var slot = inventory.Slots.FirstOrDefault(s => 
                s.Cache.Type == slotType && 
                s.CanAssign(item, ReasonItemAssign.Take));
            
            if (slot != null) return slot;
        }
        
        return null;
    }
    
    private void OnItemEquipped(Unit unit, ItemMod item, InventorySlot slot)
    {
        // 应用装备效果
        item.UpdateModifications();
        
        // 发布装备事件
        Game.Instance.Publish(new EventItemEquipped(unit, item, slot));
    }
    
    private void OnItemUnequipped(Unit unit, ItemMod item)
    {
        // 发布卸装事件
        Game.Instance.Publish(new EventItemUnequipped(unit, item));
    }
}
#endif
```

## 🏪 容器系统

### 🎒 物品栏（Inventory）

物品栏是存储物品的容器，由多个槽位组成：

```csharp
// 物品栏配置
public class InventoryConfiguration
{
    public GameDataInventory CreatePlayerBackpack()
    {
        return new GameDataInventory
        {
            DisplayName = "玩家背包",
            SyncType = SyncType.Self, // 只同步给拥有者
            
            // 物品栏标志
            InventoryFlags = new InventoryFlags
            {
                AllowDrop = true,           // 允许丢弃物品
                AllowUse = true,            // 允许使用物品
                HandlePickUpRequest = true   // 处理拾取请求
            },
            
            // 槽位配置
            Slots = new List<InventorySlotData>
            {
                // 通用槽位 (20个)
                ..Enumerable.Range(0, 20).Select(_ => new InventorySlotData
                {
                    Type = ItemSlotType.Carry,
                    Required = { }, // 无分类要求
                    Excluded = { ItemCategory.QuestItem }, // 排除任务物品
                    DisallowItemWithFailedRequirement = false
                }),
                
                // 装备槽位
                new InventorySlotData
                {
                    Type = ItemSlotType.Equip,
                    Required = { ItemCategory.Weapon }, // 只能放武器
                    DisallowItemWithFailedRequirement = true // 不满足需求的物品无法放入
                },
                new InventorySlotData
                {
                    Type = ItemSlotType.Equip,
                    Required = { ItemCategory.Armor },
                },
                new InventorySlotData
                {
                    Type = ItemSlotType.Equip,
                    Required = { ItemCategory.Accessory },
                }
            }
        };
    }
    
    public GameDataInventory CreateShop()
    {
        return new GameDataInventory
        {
            DisplayName = "商店货架",
            SyncType = SyncType.SelfOrSight, // 视野内可见
            
            InventoryFlags = new InventoryFlags
            {
                AllowDrop = false,          // 不允许丢弃
                AllowUse = false,           // 不允许直接使用
                HandlePickUpRequest = false // 不处理拾取（需要购买）
            },
            
            Slots = Enumerable.Range(0, 40).Select(_ => new InventorySlotData
            {
                Type = ItemSlotType.Carry,
                Required = { ItemCategory.ForSale }, // 只能放可出售物品
            }).ToList()
        };
    }
}
```

### 🔲 物品槽（InventorySlot）

物品栏中的单个槽位，可以存储一个物品或物品堆：

```csharp
#if SERVER
public class InventorySlotOperations
{
    /// <summary>
    /// 物品槽位操作示例
    /// </summary>
    public void DemonstrateSlotOperations(InventorySlot slot, ItemPickable item)
    {
        // 检查是否可以放入
        if (slot.CanAssign(item, ReasonItemAssign.Take))
        {
            Game.Logger.LogInfo("可以将 {Item} 放入槽位", item.Cache.DisplayName);
            
            // 放入物品
            if (slot.Assign(item, ReasonItemAssign.Take))
            {
                Game.Logger.LogInfo("物品已放入槽位");
                
                // 物品状态会自动改变：
                // - 添加 IgnoreSelector 状态（不可选中）
                // - 添加 SuppressActor 状态（不显示）
                // - 设置拥有者为物品栏持有者
            }
        }
        
        // 检查是否可以移除
        if (slot.Item != null && slot.CanDrop(ReasonItemDrop.DropToGround))
        {
            Game.Logger.LogInfo("可以丢弃槽位中的物品");
            
            // 丢弃到地面
            if (slot.Drop(ReasonItemDrop.DropToGround))
            {
                Game.Logger.LogInfo("物品已丢弃到地面");
                
                // 物品会被放置在载体单位的位置
                // 物品状态会恢复正常（可见、可选中等）
            }
        }
        
        // 交换物品
        var otherSlot = GetOtherSlot();
        if (slot.Item != null && otherSlot.CanAssign(slot.Item, ReasonItemAssign.Swap, slot))
        {
            // 交换两个槽位的物品
            var tempItem = slot.Item;
            slot.Drop(ReasonItemDrop.Swap, slot);
            otherSlot.Assign(tempItem, ReasonItemAssign.Swap, otherSlot);
        }
    }
    
    /// <summary>
    /// 智能物品放置
    /// </summary>
    public bool SmartPutItem(InventorySlot slot, ItemPickable item)
    {
        // 尝试吸收到现有堆叠
        if (slot.Item != null && slot.Item.CanAbsorb(item))
        {
            var absorbed = slot.Item.Absorb(item);
            Game.Logger.LogInfo("吸收了 {Amount} 个物品到现有堆叠", absorbed);
            return item.Stack == 0; // 如果完全被吸收则成功
        }
        
        // 尝试交换放置
        return slot.Put(item);
    }
}
#endif
```

### 📋 物品栏管理器（InventoryManager）

管理单位的所有物品栏：

```csharp
#if SERVER
public class InventoryManagerOperations
{
    /// <summary>
    /// 物品栏管理器操作示例
    /// </summary>
    public void DemonstrateInventoryManager(Unit unit)
    {
        var inventoryManager = unit.GetOrCreateComponent<InventoryManager>();
        
        // 添加多个物品栏
        var backpack = new GameDataInventory().CreateInventory(unit);
        var equipment = new GameDataInventoryEquipment().CreateInventory(unit);
        
        inventoryManager.AddInventory(backpack);
        inventoryManager.AddInventory(equipment);
        
        // 拾取物品（自动选择合适的物品栏）
        var item = CreateSampleItem();
        bool pickupSuccess = inventoryManager.Take(item);
        
        if (pickupSuccess)
        {
            Game.Logger.LogInfo("物品已自动放入合适的物品栏");
        }
        
        // 物品吸收（合并相同物品）
        var similarItem = CreateSimilarItem();
        uint absorbedAmount = inventoryManager.Absorb(similarItem);
        
        if (absorbedAmount > 0)
        {
            Game.Logger.LogInfo("吸收了 {Amount} 个物品", absorbedAmount);
        }
        
        // 查找可放置的槽位
        var newItem = CreateAnotherItem();
        var availableSlot = inventoryManager.CanAssign(newItem, ReasonItemAssign.Take);
        
        if (availableSlot != null)
        {
            Game.Logger.LogInfo("找到可用槽位: {Slot}", availableSlot);
            inventoryManager.Assign(newItem, ReasonItemAssign.Take);
        }
    }
    
    /// <summary>
    /// 自动整理物品栏
    /// </summary>
    public void AutoSortInventory(InventoryManager manager)
    {
        var allItems = new List<ItemPickable>();
        
        // 收集所有物品
        foreach (var inventory in manager.Inventories)
        {
            allItems.AddRange(inventory.Items.ToList());
            
            // 清空所有槽位
            foreach (var slot in inventory.Slots)
            {
                slot.Drop(ReasonItemDrop.Destroy);
            }
        }
        
        // 按分类和品质排序
        allItems.Sort((a, b) =>
        {
            var categoryCompare = a.Categories.First().CompareTo(b.Categories.First());
            if (categoryCompare != 0) return categoryCompare;
            
            return b.Quality.CompareTo(a.Quality); // 高品质在前
        });
        
        // 重新放置物品
        foreach (var item in allItems)
        {
            if (!manager.Take(item))
            {
                Game.Logger.LogWarning("整理时无法放置物品: {Item}", item.Cache.DisplayName);
                // 丢弃到地面
                item.Unit.SetPosition(manager.Host.Position);
            }
        }
        
        Game.Logger.LogInfo("物品栏整理完成");
    }
}
#endif
```

## 🔄 物品交互

### 拾取系统

```csharp
#if SERVER
public class ItemInteractionSystem
{
    /// <summary>
    /// 自动拾取系统
    /// </summary>
    public async Task AutoPickupSystem(Unit unit, float range)
    {
        var inventoryManager = unit.GetComponent<InventoryManager>();
        if (inventoryManager == null) return;
        
        while (unit.IsValid && unit.IsAlive)
        {
            // 查找范围内的可拾取物品
            var nearbyItems = FindItemsInRange(unit, range)
                .OfType<IPickUpItem>()
                .Where(item => item.IsValid && item.CanPickUp(inventoryManager));
            
            foreach (var item in nearbyItems)
            {
                // 尝试拾取
                if (item.PickUp(inventoryManager))
                {
                    Game.Logger.LogInfo("{Unit} 自动拾取了物品", unit.Name);
                    
                    // 播放拾取效果
                    PlayPickupEffect(unit, item as Item);
                }
            }
            
            // 每秒检查一次
            await Game.Delay(TimeSpan.FromSeconds(1));
        }
    }
    
    /// <summary>
    /// 范围拾取
    /// </summary>
    public int PickupItemsInRange(Unit unit, float range, Func<Item, bool>? filter = null)
    {
        var inventoryManager = unit.GetComponent<InventoryManager>();
        if (inventoryManager == null) return 0;
        
        var nearbyItems = FindItemsInRange(unit, range);
        if (filter != null)
        {
            nearbyItems = nearbyItems.Where(filter);
        }
        
        int pickedUpCount = 0;
        
        foreach (var item in nearbyItems.OfType<IPickUpItem>())
        {
            if (item.CanPickUp(inventoryManager) && item.PickUp(inventoryManager))
            {
                pickedUpCount++;
            }
        }
        
        Game.Logger.LogInfo("范围拾取了 {Count} 个物品", pickedUpCount);
        return pickedUpCount;
    }
}
#endif
```

### 丢弃和交换

```csharp
#if SERVER
public class ItemTransferSystem
{
    /// <summary>
    /// 物品丢弃系统
    /// </summary>
    public bool DropItem(ItemPickable item, Vector3? position = null)
    {
        if (item.Slot == null) return false;
        
        // 检查是否可以丢弃
        if (!item.Slot.CanDrop(ReasonItemDrop.DropToGround))
        {
            Game.Logger.LogWarning("物品无法丢弃");
            return false;
        }
        
        // 设置丢弃位置
        if (position.HasValue)
        {
            item.Unit.SetPosition(position.Value);
        }
        
        // 执行丢弃
        var success = item.Slot.Drop(ReasonItemDrop.DropToGround);
        
        if (success)
        {
            Game.Logger.LogInfo("物品已丢弃");
            
            // 播放丢弃特效
            PlayDropEffect(item);
        }
        
        return success;
    }
    
    /// <summary>
    /// 物品交换系统
    /// </summary>
    public bool SwapItems(InventorySlot slotA, InventorySlot slotB)
    {
        var itemA = slotA.Item;
        var itemB = slotB.Item;
        
        // 检查交换的可行性
        bool canSwapA = itemA == null || slotB.CanAssign(itemA, ReasonItemAssign.Swap, slotA);
        bool canSwapB = itemB == null || slotA.CanAssign(itemB, ReasonItemAssign.Swap, slotB);
        
        if (!canSwapA || !canSwapB)
        {
            Game.Logger.LogWarning("无法交换物品");
            return false;
        }
        
        // 执行交换
        if (itemA != null)
        {
            slotA.Drop(ReasonItemDrop.Swap, slotA);
        }
        
        if (itemB != null)
        {
            slotB.Drop(ReasonItemDrop.Swap, slotB);
        }
        
        if (itemA != null)
        {
            slotB.Assign(itemA, ReasonItemAssign.Swap, slotB);
        }
        
        if (itemB != null)
        {
            slotA.Assign(itemB, ReasonItemAssign.Swap, slotA);
        }
        
        Game.Logger.LogInfo("物品交换完成");
        return true;
    }
    
    /// <summary>
    /// 给予物品系统
    /// </summary>
    public bool GiveItem(ItemPickable item, Unit target)
    {
        var targetInventory = target.GetComponent<InventoryManager>();
        if (targetInventory == null)
        {
            Game.Logger.LogWarning("目标单位没有物品栏");
            return false;
        }
        
        // 从当前位置移除
        if (item.Slot != null)
        {
            if (!item.Slot.Drop(ReasonItemDrop.Give))
            {
                Game.Logger.LogWarning("无法从当前位置移除物品");
                return false;
            }
        }
        
        // 给予目标
        var success = targetInventory.Take(item);
        
        if (success)
        {
            Game.Logger.LogInfo("物品已给予 {Target}", target.Name);
            
            // 播放给予特效
            PlayGiveEffect(item, target);
        }
        else
        {
            Game.Logger.LogWarning("目标物品栏已满");
            
            // 丢回地面
            item.Unit.SetPosition(target.Position);
        }
        
        return success;
    }
}
#endif
```

## ⚡ 物品效果

### 消耗品使用

```csharp
#if SERVER
public class ConsumableItemSystem
{
    /// <summary>
    /// 使用消耗品
    /// </summary>
    public bool UseConsumableItem(Unit user, ItemPickable item)
    {
        // 检查是否可以使用
        if (!CanUseItem(user, item))
        {
            return false;
        }
        
        // 执行使用效果
        var effect = GetItemUseEffect(item);
        if (effect != null)
        {
            var context = new EffectContext(user, user, item);
            effect.Execute(context);
            
            Game.Logger.LogInfo("{User} 使用了 {Item}", user.Name, item.Cache.DisplayName);
        }
        
        // 消耗物品
        if (item.Stack > 1)
        {
            item.Stack -= 1;
        }
        else
        {
            item.Destroy(); // 自动销毁
        }
        
        return true;
    }
    
    /// <summary>
    /// 创建治疗药水效果
    /// </summary>
    public GameDataEffect CreateHealingPotionEffect()
    {
        return new GameDataEffectHeal
        {
            Amount = static (context) =>
            {
                var item = context.Item;
                var baseHeal = 100;
                var levelBonus = (item?.Level ?? 1) * 10;
                var qualityBonus = (item?.Quality ?? 1) * 20;
                
                return baseHeal + levelBonus + qualityBonus;
            },
            
            // 治疗延迟
            Delay = static (_) => TimeSpan.FromSeconds(0.5),
            
            // 治疗特效
            VisualEffect = ScopeData.VisualEffect.HealingSparkle
        };
    }
    
    /// <summary>
    /// 创建复杂的物品效果
    /// </summary>
    public GameDataEffect CreateComplexItemEffect()
    {
        return new GameDataEffectComplex
        {
            Effects = new List<IGameLink<GameDataEffect>>
            {
                // 立即治疗
                CreateHealingPotionEffect().Link,
                
                // 添加临时Buff
                new GameDataEffectBuffAdd
                {
                    BuffLink = ScopeData.Buff.PotionRegeneration,
                    Duration = static (_) => TimeSpan.FromSeconds(30)
                }.Link,
                
                // 播放音效
                new GameDataEffectSound
                {
                    SoundEffect = ScopeData.Sound.PotionDrink
                }.Link
            }
        };
    }
    
    private bool CanUseItem(Unit user, ItemPickable item)
    {
        // 检查物品是否在用户的物品栏中
        if (item.Carrier != user)
        {
            Game.Logger.LogWarning("物品不在用户物品栏中");
            return false;
        }
        
        // 检查用户状态
        if (user.HasState(UnitState.Stunned) || user.HasState(UnitState.Silenced))
        {
            Game.Logger.LogWarning("用户状态不允许使用物品");
            return false;
        }
        
        // 检查物品冷却
        if (item.IsOnCooldown())
        {
            Game.Logger.LogWarning("物品正在冷却中");
            return false;
        }
        
        return true;
    }
}
#endif
```

### 装备技能

```csharp
#if SERVER
public class ItemAbilitySystem
{
    /// <summary>
    /// 使用物品技能
    /// </summary>
    public bool UseItemAbility(Unit user, ItemMod item, ITarget target)
    {
        // 检查物品是否已装备
        if (!item.IsEnabled || item.Carrier != user)
        {
            Game.Logger.LogWarning("物品未装备或不属于用户");
            return false;
        }
        
        // 获取物品技能
        var ability = item.ActiveAbility;
        if (ability == null)
        {
            Game.Logger.LogWarning("物品没有主动技能");
            return false;
        }
        
        // 通过技能系统使用
        var command = new Command
        {
            AbilityLink = ability,
            Index = CommandIndex.Execute,
            Type = ComponentTagEx.AbilityManager,
            Target = target,
            Item = item, // 指定物品作为技能来源
            Flag = CommandFlag.IsUser
        };
        
        var result = command.IssueOrder(user);
        
        if (result.IsSuccess)
        {
            Game.Logger.LogInfo("{User} 使用了物品技能 {Ability}", 
                user.Name, ability.Data?.DisplayName);
        }
        
        return result.IsSuccess;
    }
    
    /// <summary>
    /// 创建物品技能数据
    /// </summary>
    public GameDataAbilityExecute CreateItemAbility()
    {
        return new GameDataAbilityExecute
        {
            DisplayName = "炎爆术",
            Description = "释放火焰爆炸攻击敌人",
            Icon = ScopeData.Icon.FireBlast,
            
            // 冷却时间
            Cost = new Cost
            {
                Cooldown = static (context) => TimeSpan.FromSeconds(10),
                
                // 基于物品等级的法力消耗
                ManaCost = static (context) =>
                {
                    var item = context.Item;
                    return 30 + (item?.Level ?? 1) * 2;
                }
            },
            
            // 技能效果
            Effect = new GameDataEffectAreaDamage
            {
                Radius = static (context) =>
                {
                    var item = context.Item;
                    return 150 + (item?.Quality ?? 1) * 25; // 品质影响范围
                },
                
                Amount = static (context) =>
                {
                    var item = context.Item;
                    var baseDamage = 100;
                    var levelBonus = (item?.Level ?? 1) * 15;
                    var qualityBonus = (item?.Quality ?? 1) * 30;
                    
                    return baseDamage + levelBonus + qualityBonus;
                },
                
                DamageType = DamageType.Fire
            }.Link,
            
            // 目标配置
            TargetType = AbilityTargetType.Point,
            Range = static (_) => 800
        };
    }
}
#endif
```

## 🎮 物品指令

物品系统与指令系统集成，提供完整的物品操作：

```csharp
#if SERVER
public class ItemCommandSystem
{
    /// <summary>
    /// 拾取物品指令
    /// </summary>
    public CmdResult PickupItemCommand(Unit unit, Item targetItem)
    {
        var command = new Command
        {
            Index = CommandIndexInventory.PickUp,
            Type = ComponentTagEx.InventoryManager,
            Target = targetItem,
            Player = unit.Player,
            Flag = CommandFlag.IsUser
        };
        
        return command.IssueOrder(unit);
    }
    
    /// <summary>
    /// 使用物品指令
    /// </summary>
    public CmdResult UseItemCommand(Unit unit, ItemMod item)
    {
        var command = new Command
        {
            Index = CommandIndexInventory.Use,
            Type = ComponentTagEx.InventoryManager,
            Item = item,
            Player = unit.Player,
            Flag = CommandFlag.IsUser
        };
        
        return command.IssueOrder(unit);
    }
    
    /// <summary>
    /// 交换物品指令
    /// </summary>
    public CmdResult SwapItemCommand(Unit unit, ItemPickable item, InventorySlot targetSlot)
    {
        var command = new Command
        {
            Index = CommandIndexInventory.Swap,
            Type = ComponentTagEx.InventoryManager,
            Item = item,
            Target = targetSlot,
            Player = unit.Player,
            Flag = CommandFlag.IsUser
        };
        
        return command.IssueOrder(unit);
    }
    
    /// <summary>
    /// 丢弃物品指令
    /// </summary>
    public CmdResult DropItemCommand(Unit unit, ItemPickable item)
    {
        var command = new Command
        {
            Index = CommandIndexInventory.Drop,
            Type = ComponentTagEx.InventoryManager,
            Item = item,
            Player = unit.Player,
            Flag = CommandFlag.IsUser
        };
        
        return command.IssueOrder(unit);
    }
    
    /// <summary>
    /// 给予物品指令
    /// </summary>
    public CmdResult GiveItemCommand(Unit unit, ItemPickable item, Unit target)
    {
        var command = new Command
        {
            Index = CommandIndexInventory.Give,
            Type = ComponentTagEx.InventoryManager,
            Item = item,
            Target = target,
            Player = unit.Player,
            Flag = CommandFlag.IsUser
        };
        
        return command.IssueOrder(unit);
    }
}
#endif
```

## 🏷️ 物品分类

物品分类系统用于组织和筛选物品：

```csharp
// 物品分类枚举
public enum ItemCategory
{
    // 基础分类
    Weapon,      // 武器
    Armor,       // 护甲
    Accessory,   // 饰品
    Consumable,  // 消耗品
    Material,    // 材料
    Miscellaneous, // 杂项
    
    // 武器细分
    Melee,       // 近战武器
    Ranged,      // 远程武器
    Magical,     // 法术武器
    
    // 特殊分类
    QuestItem,   // 任务物品
    KeyItem,     // 关键物品
    ForSale,     // 可出售物品
    
    // 稀有度
    Common,      // 普通
    Uncommon,    // 稀有
    Rare,        // 珍稀
    Epic,        // 史诗
    Legendary,   // 传说
    
    // 功能分类
    Healing,     // 治疗类
    Buff,        // 增益类
    Utility,     // 实用类
}

public class ItemCategorySystem
{
    /// <summary>
    /// 根据分类过滤物品
    /// </summary>
    public IEnumerable<ItemPickable> FilterItemsByCategory(
        IEnumerable<ItemPickable> items, 
        ItemCategory category)
    {
        return items.Where(item => item.HasCategory(category));
    }
    
    /// <summary>
    /// 获取物品分类颜色
    /// </summary>
    public Color GetCategoryColor(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Common => Color.White,
            ItemCategory.Uncommon => Color.Green,
            ItemCategory.Rare => Color.Blue,
            ItemCategory.Epic => Color.Purple,
            ItemCategory.Legendary => Color.Orange,
            ItemCategory.QuestItem => Color.Yellow,
            _ => Color.Gray
        };
    }
    
    /// <summary>
    /// 检查物品分类兼容性
    /// </summary>
    public bool AreCategoriesCompatible(
        List<ItemCategory> required, 
        List<ItemCategory> excluded, 
        List<ItemCategory> itemCategories)
    {
        // 检查必需分类
        if (required.Count > 0 && !required.Any(itemCategories.Contains))
        {
            return false;
        }
        
        // 检查排除分类
        if (excluded.Count > 0 && excluded.Any(itemCategories.Contains))
        {
            return false;
        }
        
        return true;
    }
}
```

## 📊 叠加系统

物品的叠加机制用于管理相同物品的数量：

```csharp
#if SERVER
public class ItemStackingSystem
{
    /// <summary>
    /// 物品叠加示例
    /// </summary>
    public void DemonstrateItemStacking()
    {
        var potionData = new GameDataItemPickable
        {
            DisplayName = "治疗药水",
            StackStart = 1,
            StackMax = 50,
            CanStack = true,
            CanAbsorb = true
        };
        
        // 创建两个相同的药水
        var potion1 = potionData.CreateItem(scene, player) as ItemPickable;
        var potion2 = potionData.CreateItem(scene, player) as ItemPickable;
        
        potion1.Stack = 30;
        potion2.Stack = 25;
        
        // 尝试合并
        if (potion1.CanAbsorb(potion2))
        {
            var absorbed = potion1.Absorb(potion2);
            
            Game.Logger.LogInfo("合并了 {Amount} 个物品，当前堆叠: {Stack}", 
                absorbed, potion1.Stack);
            
            // potion1.Stack 现在是 50 (达到上限)
            // potion2.Stack 现在是 5 (剩余)
        }
    }
    
    /// <summary>
    /// 智能物品分发
    /// </summary>
    public bool DistributeItems(ItemPickable sourceItem, List<InventorySlot> targetSlots)
    {
        var remainingStack = sourceItem.Stack;
        
        foreach (var slot in targetSlots)
        {
            if (remainingStack == 0) break;
            
            if (slot.Item == null)
            {
                // 空槽位，直接放置
                if (slot.CanAssign(sourceItem, ReasonItemAssign.Take))
                {
                    slot.Assign(sourceItem, ReasonItemAssign.Take);
                    remainingStack = 0;
                }
            }
            else if (slot.Item.CanAbsorb(sourceItem))
            {
                // 已有相同物品，尝试吸收
                var absorbed = slot.Item.Absorb(sourceItem);
                remainingStack -= absorbed;
                sourceItem.Stack = remainingStack;
            }
        }
        
        // 返回是否完全分发
        return remainingStack == 0;
    }
    
    /// <summary>
    /// 自动合并物品栏中的相同物品
    /// </summary>
    public void ConsolidateInventory(Inventory inventory)
    {
        var itemGroups = inventory.Items
            .GroupBy(item => new { item.Cache, item.Level, item.Quality })
            .Where(group => group.Count() > 1);
        
        foreach (var group in itemGroups)
        {
            var items = group.OrderByDescending(item => item.Stack).ToList();
            var targetItem = items.First();
            
            for (int i = 1; i < items.Count; i++)
            {
                var sourceItem = items[i];
                
                if (targetItem.CanAbsorb(sourceItem))
                {
                    targetItem.Absorb(sourceItem);
                    
                    // 如果源物品被完全吸收，它会自动销毁
                    if (sourceItem.Stack == 0)
                    {
                        Game.Logger.LogInfo("物品 {Item} 已被合并", 
                            sourceItem.Cache.DisplayName);
                    }
                }
            }
        }
    }
}
#endif
```

## 🎨 客户端表现

### 物品UI显示

```csharp
#if CLIENT
public class ItemUIController
{
    /// <summary>
    /// 更新物品图标显示
    /// </summary>
    public void UpdateItemIcon(ItemIcon icon, ItemPickable item)
    {
        // 设置图标
        if (item.Cache.Icon != null)
        {
            icon.SetIcon(item.Cache.Icon);
        }
        
        // 设置品质边框
        var qualityColor = GetQualityColor(item.Quality);
        icon.SetBorderColor(qualityColor);
        
        // 设置叠加数量
        if (item.CanStack && item.Stack > 1)
        {
            icon.SetStackText(item.Stack.ToString());
            icon.ShowStackText(true);
        }
        else
        {
            icon.ShowStackText(false);
        }
        
        // 设置等级显示
        if (item.Level > 1)
        {
            icon.SetLevelText($"Lv.{item.Level}");
            icon.ShowLevelText(true);
        }
        
        // 设置分类标识
        var categoryIcon = GetCategoryIcon(item.Categories);
        if (categoryIcon != null)
        {
            icon.SetCategoryIcon(categoryIcon);
        }
        
        // 设置状态
        icon.SetEnabled(!item.IsOnCooldown());
    }
    
    /// <summary>
    /// 显示物品详细信息
    /// </summary>
    public void ShowItemTooltip(ItemPickable item, Vector2 position)
    {
        var tooltip = GetTooltip();
        
        // 标题（品质颜色）
        var titleColor = GetQualityColor(item.Quality);
        tooltip.SetTitle(item.Cache.DisplayName?.ToString() ?? "未知物品", titleColor);
        
        // 基础信息
        tooltip.AddLine($"等级: {item.Level}");
        tooltip.AddLine($"品质: {GetQualityName(item.Quality)}");
        
        if (item.CanStack)
        {
            tooltip.AddLine($"数量: {item.Stack}");
            if (item.StackMax.HasValue)
            {
                tooltip.AddLine($"最大叠加: {item.StackMax.Value}");
            }
        }
        
        // 描述
        if (item.Cache.Description != null)
        {
            tooltip.AddSeparator();
            tooltip.AddDescription(item.Cache.Description.ToString());
        }
        
        // 装备属性（如果是装备）
        if (item is ItemMod equipment)
        {
            ShowEquipmentStats(tooltip, equipment);
        }
        
        // 分类信息
        if (item.Categories.Count > 0)
        {
            tooltip.AddSeparator();
            var categories = string.Join(", ", item.Categories.Select(GetCategoryName));
            tooltip.AddLine($"分类: {categories}");
        }
        
        // 价值信息
        var value = GetItemValue(item);
        if (value > 0)
        {
            tooltip.AddLine($"价值: {value} 金币");
        }
        
        tooltip.Show(position);
    }
    
    /// <summary>
    /// 显示装备属性
    /// </summary>
    private void ShowEquipmentStats(ItemTooltip tooltip, ItemMod equipment)
    {
        tooltip.AddSeparator();
        tooltip.AddHeader("装备属性");
        
        // 显示属性修改
        var modificationData = equipment.GetActiveModificationData();
        if (modificationData != null)
        {
            foreach (var mod in modificationData.Modifications)
            {
                var property = mod.Property.Data?.DisplayName ?? "未知属性";
                var value = mod.Value(equipment);
                var operation = mod.Operation == ModificationOperation.Add ? "+" : "×";
                
                var color = value >= 0 ? Color.Green : Color.Red;
                tooltip.AddLine($"{property}: {operation}{value}", color);
            }
        }
        
        // 显示装备需求
        if (!equipment.MeetRequirement)
        {
            tooltip.AddSeparator();
            tooltip.AddLine("需求不满足", Color.Red);
            
            var requirements = GetEquipmentRequirements(equipment);
            foreach (var req in requirements)
            {
                tooltip.AddLine($"需要: {req}", Color.Red);
            }
        }
        
        // 显示套装信息
        var setInfo = GetSetInfo(equipment);
        if (setInfo != null)
        {
            tooltip.AddSeparator();
            tooltip.AddLine($"套装: {setInfo.Name}");
            tooltip.AddLine($"已装备: {setInfo.EquippedCount}/{setInfo.TotalCount}");
        }
    }
    
    /// <summary>
    /// 物品拖拽系统
    /// </summary>
    public void HandleItemDrag(ItemIcon icon, ItemPickable item)
    {
        icon.OnDragStart += () =>
        {
            // 开始拖拽，显示拖拽图标
            ShowDragIcon(item);
            HighlightValidDropTargets(item);
        };
        
        icon.OnDragEnd += (dropTarget) =>
        {
            // 结束拖拽
            HideDragIcon();
            ClearHighlights();
            
            if (dropTarget is InventorySlotUI slotUI)
            {
                // 尝试放置到目标槽位
                RequestItemMove(item, slotUI.Slot);
            }
            else if (dropTarget == null)
            {
                // 拖拽到空白处，可能是丢弃
                ShowDropConfirmation(item);
            }
        };
    }
    
    private Color GetQualityColor(uint quality)
    {
        return quality switch
        {
            1 => Color.White,    // 普通
            2 => Color.Green,    // 稀有
            3 => Color.Blue,     // 精良
            4 => Color.Purple,   // 史诗
            5 => Color.Orange,   // 传说
            _ => Color.Gray
        };
    }
    
    private void RequestItemMove(ItemPickable item, InventorySlot targetSlot)
    {
        // 发送物品移动请求到服务器
        var command = new Command
        {
            Index = CommandIndexInventory.Swap,
            Type = ComponentTagEx.InventoryManager,
            Item = item,
            Target = targetSlot,
            Player = Player.LocalPlayer,
            Flag = CommandFlag.IsUser
        };
        
        command.IssueOrder(Player.LocalPlayer.MainUnit);
    }
}
#endif
```

### 视觉效果

```csharp
#if CLIENT
public class ItemVisualEffects
{
    /// <summary>
    /// 物品拾取特效
    /// </summary>
    public void PlayPickupEffect(Unit unit, Item item)
    {
        // 播放拾取音效
        var qualityTier = GetQualityTier(item.Quality);
        PlaySound($"ItemPickup_{qualityTier}");
        
        // 播放拾取粒子效果
        var effect = CreateParticleEffect("ItemPickup", item.Position);
        effect.SetColor(GetQualityColor(item.Quality));
        effect.Play();
        
        // 显示拾取文字
        ShowFloatingText($"拾取: {item.Cache.DisplayName}", item.Position, Color.White);
        
        // 单位发光效果
        unit.AddGlowEffect(Color.Gold, TimeSpan.FromSeconds(0.5));
    }
    
    /// <summary>
    /// 物品掉落特效
    /// </summary>
    public void PlayDropEffect(Item item)
    {
        // 掉落音效
        PlaySound("ItemDrop");
        
        // 掉落粒子效果
        var effect = CreateParticleEffect("ItemDrop", item.Position);
        effect.Play();
        
        // 物品弹跳动画
        AnimateItemBounce(item);
    }
    
    /// <summary>
    /// 装备特效
    /// </summary>
    public void PlayEquipEffect(Unit unit, ItemMod equipment)
    {
        // 装备音效
        var category = equipment.Categories.FirstOrDefault();
        PlaySound($"Equip_{category}");
        
        // 装备光效
        var color = GetQualityColor(equipment.Quality);
        unit.AddEquipmentGlow(color, TimeSpan.FromSeconds(1));
        
        // 属性提升特效
        if (equipment.Modifications.Any(mod => mod.Value > 0))
        {
            ShowFloatingText("属性提升!", unit.Position + Vector3.Up, Color.Green);
        }
    }
    
    /// <summary>
    /// 物品使用特效
    /// </summary>
    public void PlayUseEffect(Unit user, ItemPickable item)
    {
        if (item.HasCategory(ItemCategory.Healing))
        {
            // 治疗物品特效
            var healEffect = CreateParticleEffect("HealingSparkle", user.Position);
            healEffect.SetColor(Color.Green);
            healEffect.Play();
            
            PlaySound("PotionDrink");
            ShowFloatingText("+HP", user.Position + Vector3.Up, Color.Green);
        }
        else if (item.HasCategory(ItemCategory.Buff))
        {
            // 增益物品特效
            var buffEffect = CreateParticleEffect("BuffAura", user.Position);
            buffEffect.SetColor(Color.Blue);
            buffEffect.Play();
            
            PlaySound("BuffApply");
            ShowFloatingText("增益!", user.Position + Vector3.Up, Color.Blue);
        }
    }
    
    /// <summary>
    /// 稀有物品特殊效果
    /// </summary>
    public void CreateRareItemEffects(Item item)
    {
        if (item.Quality >= 4) // 史诗及以上
        {
            // 持续发光效果
            var glowColor = GetQualityColor(item.Quality);
            item.Unit.AddPersistentGlow(glowColor, 0.5f);
            
            // 粒子轨迹
            var trailEffect = CreateParticleEffect("RareItemTrail", item.Position);
            trailEffect.SetColor(glowColor);
            trailEffect.Follow(item.Unit);
        }
        
        if (item.Quality >= 5) // 传说
        {
            // 额外的环形特效
            var ringEffect = CreateParticleEffect("LegendaryRing", item.Position);
            ringEffect.SetScale(2.0f);
            ringEffect.Loop(true);
        }
    }
}
#endif
```

## 🛠️ 实用示例

### 完整的物品系统示例

```csharp
#if SERVER
public class ComprehensiveItemSystem
{
    /// <summary>
    /// 基于预定义数编表创建完整的武器系统
    /// </summary>
    public void CreateWeaponSystem()
    {
        // ⚠️ 注意：以下配置应该是预定义的数编表，不是运行时创建
        // 此处仅为展示配置结构，实际使用应该引用 ScopeData 中的预定义数编表
        
        // 1. 使用预定义的基础武器数编表
        var ironSwordData = ScopeData.Item.IronSword; // 预定义的数编表
        
        // 在场景中创建物品实例
        var ironSwordInstance = ironSwordData.CreateItem(startLocation) as ItemMod;
        if (ironSwordInstance != null)
        {
            // 可以修改动态属性
            ironSwordInstance.Quality = 2; // 提升品质
            ironSwordInstance.Level = 5;   // 提升等级
            
            Game.Logger.LogInfo("创建铁剑: {Sword}", ironSwordInstance);
        }
        
        // 2. 使用预定义的魔法武器数编表
        var flamingSwordData = ScopeData.Item.FlamingSword; // 预定义的数编表
        var flamingSwordInstance = flamingSwordData.CreateItem(startLocation + Vector3.Right * 2) as ItemMod;
        if (flamingSwordInstance != null)
        {
            // 可以修改动态属性
            flamingSwordInstance.Quality = 5; // 传奇品质
            flamingSwordInstance.Level = 30;  // 最高等级
            
            Game.Logger.LogInfo("创建炎魔之剑: {Sword}", flamingSwordInstance);
        }
    }
    
    /// <summary>
    /// 创建套装系统
    /// </summary>
    public void CreateArmorSet()
    {
        var setData = new ArmorSetData
        {
            Name = "龙鳞套装",
            SetBonuses = new Dictionary<int, IUnitModificationData>
            {
                // 2件套奖励
                [2] = new UnitModificationData
                {
                    Modifications = new List<UnitPropertyModification>
                    {
                        new()
                        {
                            Property = ScopeData.UnitProperty.FireResistance,
                            Value = static (_) => 25,
                            Operation = ModificationOperation.Add
                        }
                    }
                },
                
                // 4件套奖励
                [4] = new UnitModificationData
                {
                    Modifications = new List<UnitPropertyModification>
                    {
                        new()
                        {
                            Property = ScopeData.UnitProperty.Health,
                            Value = static (_) => 200,
                            Operation = ModificationOperation.Add
                        }
                    },
                    
                    GrantedAbility = ScopeData.Ability.DragonBreath
                }
            }
        };
        
        // 创建套装物品
        var helmet = CreateSetPiece(setData, "龙鳞头盔", ItemCategory.Helmet);
        var chestplate = CreateSetPiece(setData, "龙鳞胸甲", ItemCategory.Chestplate);
        var leggings = CreateSetPiece(setData, "龙鳞护腿", ItemCategory.Leggings);
        var boots = CreateSetPiece(setData, "龙鳞战靴", ItemCategory.Boots);
    }
    
    /// <summary>
    /// 创建商店系统
    /// </summary>
    public void CreateShopSystem(Unit shopkeeper)
    {
        // 创建商店物品栏
        var shopInventory = new GameDataInventory
        {
            DisplayName = "武器商店",
            SyncType = SyncType.SelfOrSight,
            
            InventoryFlags = new InventoryFlags
            {
                AllowDrop = false,
                AllowUse = false,
                HandlePickUpRequest = false // 需要特殊购买逻辑
            },
            
            Slots = Enumerable.Range(0, 20).Select(_ => new InventorySlotData
            {
                Type = ItemSlotType.Carry,
                Required = { ItemCategory.ForSale }
            }).ToList()
        }.CreateInventory(shopkeeper);
        
        // 添加商品
        var weapons = CreateShopWeapons();
        foreach (var weapon in weapons)
        {
            shopInventory.Take(weapon);
        }
        
        // 设置购买逻辑
        SetupShopPurchaseLogic(shopkeeper, shopInventory);
    }
    
    /// <summary>
    /// 创建制作系统
    /// </summary>
    public void CreateCraftingSystem()
    {
        var recipe = new CraftingRecipe
        {
            Name = "强化铁剑",
            Description = "将铁剑强化为+1版本",
            
            RequiredItems = new Dictionary<IGameLink<GameDataItem>, int>
            {
                { ScopeData.Item.IronSword, 1 },
                { ScopeData.Item.IronOre, 3 },
                { ScopeData.Item.EnhancementStone, 1 }
            },
            
            ResultItem = ScopeData.Item.EnhancedIronSword,
            
            RequiredSkillLevel = 10,
            ExperienceGained = 50,
            
            CraftingTime = TimeSpan.FromSeconds(5)
        };
        
        RegisterCraftingRecipe(recipe);
    }
    
    private GameDataItemMod CreateSetPiece(ArmorSetData setData, string name, ItemCategory category)
    {
        return new GameDataItemMod
        {
            DisplayName = name,
            Categories = { ItemCategory.Armor, category, ItemCategory.Rare },
            SetData = setData, // 自定义属性
            
            Modifications = new Dictionary<ItemSlotType, IUnitModificationData>
            {
                [ItemSlotType.Equip] = new UnitModificationData
                {
                    Modifications = new List<UnitPropertyModification>
                    {
                        new()
                        {
                            Property = ScopeData.UnitProperty.Armor,
                            Value = static (_) => 30,
                        }
                    }
                }
            }
        };
    }
}
#endif
```

### 动态物品生成

```csharp
#if SERVER
public class DynamicItemGeneration
{
    /// <summary>
    /// 随机生成物品
    /// </summary>
    public ItemMod GenerateRandomItem(int level, ItemCategory category)
    {
        var random = new Random();
        
        // 随机品质（基于等级）
        var quality = GenerateQuality(level, random);
        
        // 随机属性
        var stats = GenerateRandomStats(category, quality, random);
        
        // 创建物品数据
        var itemData = new GameDataItemMod
        {
            DisplayName = GenerateItemName(category, quality),
            Description = GenerateItemDescription(category, stats),
            Icon = GetCategoryIcon(category),
            Unit = GetCategoryUnit(category),
            
            Quality = quality,
            Level = (uint)level,
            Categories = { category, GetQualityCategory(quality) },
            
            Modifications = new Dictionary<ItemSlotType, IUnitModificationData>
            {
                [ItemSlotType.Equip] = new UnitModificationData
                {
                    Modifications = stats
                }
            }
        };
        
        // 创建实例
        var unit = itemData.CreateItemUnit(GetRandomPosition(), Player.DefaultPlayer);
        return new ItemMod(unit, itemData.Link);
    }
    
    /// <summary>
    /// 生成随机属性
    /// </summary>
    private List<UnitPropertyModification> GenerateRandomStats(
        ItemCategory category, 
        uint quality, 
        Random random)
    {
        var stats = new List<UnitPropertyModification>();
        var statPool = GetAvailableStats(category);
        var statCount = GetStatCount(quality);
        
        // 随机选择属性
        var selectedStats = statPool.OrderBy(_ => random.Next()).Take(statCount);
        
        foreach (var stat in selectedStats)
        {
            var baseValue = GetBaseStat(stat, category);
            var qualityMultiplier = GetQualityMultiplier(quality);
            var variation = random.NextDouble() * 0.4 + 0.8; // 80-120%变化
            
            var finalValue = (int)(baseValue * qualityMultiplier * variation);
            
            stats.Add(new UnitPropertyModification
            {
                Property = stat,
                Value = static (_) => finalValue,
                SubType = PropertySubType.Equipment
            });
        }
        
        return stats;
    }
    
    /// <summary>
    /// 升级物品
    /// </summary>
    public bool UpgradeItem(ItemMod item, List<ItemPickable> materials)
    {
        var currentLevel = item.Level;
        var targetLevel = currentLevel + 1;
        
        // 检查材料需求
        var requiredMaterials = GetUpgradeRequirements(item, targetLevel);
        if (!HasSufficientMaterials(materials, requiredMaterials))
        {
            Game.Logger.LogWarning("材料不足，无法升级");
            return false;
        }
        
        // 消耗材料
        ConsumeMaterials(materials, requiredMaterials);
        
        // 提升等级
        item.Level = targetLevel;
        
        // 重新计算属性
        item.UpdateModifications(forced: true);
        
        Game.Logger.LogInfo("物品 {Item} 升级到 Lv.{Level}", 
            item.Cache.DisplayName, targetLevel);
        
        // 播放升级特效
        PlayUpgradeEffect(item);
        
        return true;
    }
    
    /// <summary>
    /// 附魔系统
    /// </summary>
    public bool EnchantItem(ItemMod item, ItemPickable enchantmentStone)
    {
        var enchantment = GetEnchantmentFromStone(enchantmentStone);
        if (enchantment == null)
        {
            Game.Logger.LogWarning("无效的附魔石");
            return false;
        }
        
        // 检查物品是否可以附魔
        if (!CanEnchant(item, enchantment))
        {
            Game.Logger.LogWarning("物品无法附魔");
            return false;
        }
        
        // 应用附魔
        ApplyEnchantment(item, enchantment);
        
        // 消耗附魔石
        enchantmentStone.Stack -= 1;
        
        Game.Logger.LogInfo("物品 {Item} 获得附魔: {Enchantment}", 
            item.Cache.DisplayName, enchantment.Name);
        
        return true;
    }
}
#endif
```

## 🔧 API 参考

### GameDataItem 类

```csharp
public abstract partial class GameDataItem
{
    // 基础属性
    public LocalizedString? DisplayName { get; set; }
    public LocalizedString? Description { get; set; }
    public Icon? Icon { get; set; }
    public required IGameLink<GameDataUnit> Unit { get; set; }
    
    // 等级和品质
    public uint Quality { get; set; }
    public uint Level { get; set; }
    
    // 分类和过滤
    public List<ItemCategory> Categories { get; set; }
    public TargetFilterComplex Filter { get; set; }
    
    // 同步设置
    public SyncType SyncType { get; set; }
    
    // 创建方法
    public Item CreateItem(ScenePoint scene, Player? player = null);
    protected abstract Item CreateItem(Unit unit);
    protected Unit CreateItemUnit(ScenePoint scene, Player? player);
}
```

### Item 类

```csharp
public abstract partial class Item : TagComponent
{
    // 基础属性
    public IGameLink<GameDataItem> Link { get; }
    public GameDataItem Cache { get; }
    public Unit Unit { get; }
    
    // 动态属性
    public uint Quality { get; set; }
    public uint Level { get; set; }
    
    // 分类方法
    public List<ItemCategory> Categories { get; }
    public bool HasCategory(ItemCategory category);
    
    // 位置和目标
    public virtual ScenePoint Position { get; }
    public virtual bool CanBeSeen(Entity caster);
    public float InteractRadius { get; }
}
```

### ItemPickable 类

```csharp
public partial class ItemPickable : Item, IPickUpItem
{
    // 叠加属性
    public virtual uint Stack { get; set; }
    public virtual bool CanStack { get; }
    public virtual bool AbsorbEnabled { get; }
    
    // 槽位信息
    public InventorySlot? Slot { get; }
    
    // 需求验证
    public virtual bool MeetRequirement { get; }
    public virtual bool TestRequirement(Unit unit);
    
    // 叠加方法
    public uint AddStack(int stack);
    public virtual bool CanAbsorb(ItemPickable inItem);
    public uint Absorb(ItemPickable inItem);
    
    // 拾取接口
    public bool CanPickUp(InventoryManager manager);
    public bool PickUp(InventoryManager manager, bool isRequest = false);
}
```

### ItemMod 类

```csharp
public partial class ItemMod : ItemPickable
{
    // 状态属性
    public bool IsEnabled { get; }
    public override bool MeetRequirement { get; }
    
    // 槽位类型
    public ItemSlotType SlotType { get; }
    
    // 技能相关
    public Ability? ActiveAbility { get; }
    
    // 修改管理
    public void UpdateModifications(bool forced = false);
    public ModificationManager? GetModificationManager();
    
    // 承载者
    protected override Entity EffectCaster { get; }
}
```

### Inventory 类

```csharp
public partial class Inventory : IGameObject<GameDataInventory>
{
    // 基础属性
    public List<InventorySlot> Slots { get; }
    public InventoryManager Manager { get; }
    public Unit Carrier { get; }
    
    // 配置属性
    public bool AllowDrop { get; set; }
    public bool AllowUse { get; set; }
    public bool HandlePickUpRequest { get; set; }
    
    // 物品集合
    public IEnumerable<ItemPickable> Items { get; }
    
    // 操作方法
    public uint Absorb(ItemPickable inItem);
    public InventorySlot? CanAssign(ItemPickable inItem, ReasonItemAssign reason = ReasonItemAssign.Take, InventorySlot? swapSource = null);
    public InventorySlot? Assign(ItemPickable inItem, ReasonItemAssign reason = ReasonItemAssign.Take, InventorySlot? swapSource = null);
    public bool Take(ItemPickable inItem);
}
```

### InventorySlot 类

```csharp
public partial class InventorySlot
{
    // 基础属性
    public Inventory Inventory { get; }
    public ItemPickable? Item { get; }
    public IInventorySlotData Cache { get; }
    public Unit Carrier { get; }
    
    // 状态属性
    public bool MeetRequirement { get; }
    
    // 操作方法
    public bool CanDrop(ReasonItemDrop reason = ReasonItemDrop.DropToGround, InventorySlot? swapSource = null);
    public bool Drop(ReasonItemDrop reason = ReasonItemDrop.DropToGround, InventorySlot? swapSource = null);
    public bool CanAssign(ItemPickable inItem, ReasonItemAssign reason = ReasonItemAssign.Take, InventorySlot? swapSource = null);
    public bool Assign(ItemPickable inItem, ReasonItemAssign reason = ReasonItemAssign.Take, InventorySlot? swapSource = null);
    public bool Put(ItemPickable inItem);
}
```

### InventoryManager 类

```csharp
public partial class InventoryManager : TagComponent, IObjectManager<Inventory>
{
    // 物品栏集合
    public OrderedSet<Inventory> Inventories { get; }
    
    // 拾取范围
    public float PickUpRange { get; }
    
    // 管理方法
    public void AddInventory(Inventory inventory);
    
    // 物品操作
    public uint Absorb(ItemPickable inItem);
    public InventorySlot? CanAssign(ItemPickable inItem, ReasonItemAssign reason = ReasonItemAssign.Take, InventorySlot? swapSource = null, bool isRequest = false);
    public InventorySlot? Assign(ItemPickable inItem, ReasonItemAssign reason = ReasonItemAssign.Take, InventorySlot? swapSource = null, bool isRequest = false);
    public bool Take(ItemPickable inItem, bool isRequest = false);
    
    // 指令处理
    public virtual CmdResult<Order> CreateOrder(Command command);
}
```

## 💡 最佳实践

### ✅ 推荐做法

1. **⚠️ 数编表使用（重要）**
   - **必须使用预定义的数编表** - 所有 GameDataItem 必须在编译时预定义
   - **避免运行时创建** - 不要使用 `new GameDataItem()` 动态创建数编表
   - **服务端-客户端同步** - 确保所有数编表在服务端和客户端都存在
   - **使用 ScopeData 引用** - 通过 `ScopeData.Item.XXX` 引用预定义的数编表
   - **动态修改实例属性** - 可以修改 Item 实例的 Quality、Level 等动态属性

2. **物品分类设计**
   - 使用层次化的分类系统
   - 为每个物品设置合适的分类
   - 利用分类实现过滤和限制

2. **叠加机制**
   - 为可堆叠物品设置合理的上限
   - 实现智能的物品合并逻辑
   - 考虑物品品质对叠加的影响

3. **容器设计**
   - 根据游戏需求设计槽位限制
   - 实现多样化的物品栏类型
   - 提供灵活的物品移动机制

4. **装备系统**
   - 设计清晰的装备需求
   - 实现动态的属性修改
   - 考虑装备的视觉表现

5. **性能优化**
   - 合理设置同步范围
   - 避免频繁的物品创建和销毁
   - 使用对象池管理临时物品

### ❌ 避免的做法

1. **⚠️ 数编表错误使用（严重）**
   - **避免服务端专用数编表** - 不要创建只在服务端存在的数编表

2. **设计问题**
   - 避免过于复杂的分类系统
   - 不要忽略物品的边界情况
   - 避免不合理的叠加限制

2. **性能问题**
   - 避免在客户端进行复杂的物品计算
   - 不要频繁更新物品状态
   - 避免大量物品的同时同步

3. **用户体验**
   - 不要让物品操作过于复杂
   - 避免不清晰的物品信息显示
   - 不要忽略操作的视觉反馈

## 🔗 相关文档

- [🎮 指令系统](OrderSystem.md) - 物品相关的指令处理
- [🌟 Buff系统](BuffSystem.md) - 物品如何提供Buff效果
- [⚡ 技能系统](AbilitySystem.md) - 物品技能的实现
- [🎯 单位系统](UnitSystem.md) - 物品对单位属性的影响
- [🎨 UI系统](UISystem.md) - 物品界面的设计和实现

---

> 💡 **提示**: 物品系统是游戏的重要组成部分，良好的物品设计能够极大提升游戏的可玩性和深度。在设计物品时，要考虑游戏平衡、玩家体验和技术实现的平衡，确保系统既功能强大又易于使用。 