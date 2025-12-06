# 🤔 Entity vs Actor 选择指南

本文档帮助开发者正确选择使用Entity还是Actor来实现游戏功能，避免常见的设计错误。

## 📋 目录

- [🎯 快速决策](#快速决策)
- [📊 详细对比](#详细对比)
- [🎮 实际场景分析](#实际场景分析)
- [💡 设计模式](#设计模式)
- [🔄 协作模式](#协作模式)
- [⚠️ 常见误区](#常见误区)
- [✅ 最佳实践](#最佳实践)

## 🎯 快速决策

### 🌟 一分钟决策树

```
你的对象需要什么？
├── 需要同步到其他玩家吗？
│   ├── 是 → 🎯 使用 Entity/Unit
│   └── 否 → 有游戏逻辑吗？
│       ├── 是 → 🎯 使用 Entity
│       └── 否 → 🎭 使用 Actor
└── 只是视觉效果吗？
    └── 是 → 🎭 使用 Actor
```

### ⚡ 三秒决策

| 问题 | Entity | Actor |
|------|--------|-------|
| 有血量/属性？ | ✅ | ❌ |
| 需要同步？ | ✅ | ❌ |
| 纯视觉效果？ | ❌ | ✅ |
| 客户端创建？ | ❌ | ✅ |

## 📊 详细对比

### 🏗️ 技术特性对比

| 特性 | Entity | Actor | 说明 |
|------|--------|-------|------|
| **创建权限** | 🔴 仅服务端 | 🟢 任意端 | Entity需要服务端权威 |
| **同步机制** | 🔴 自动同步 | 🟢 无同步 | Entity状态自动同步到客户端 |
| **生命周期** | 🔴 持久化 | 🟢 灵活 | Actor支持瞬态和持久化 |
| **内存开销** | 🔴 较大 | 🟢 较小 | Entity包含完整的组件系统 |
| **网络开销** | 🔴 持续 | 🟢 无 | Entity需要同步状态变化 |
| **创建速度** | 🔴 较慢 | 🟢 快速 | Actor创建无需网络通信 |
| **功能复杂度** | 🔴 复杂 | 🟢 简单 | Entity支持完整的游戏逻辑 |

### 🎯 使用场景对比

| 场景 | 推荐选择 | 原因 |
|------|---------|------|
| 玩家角色 | Entity | 需要属性、状态、同步 |
| NPC敌人 | Entity | 需要AI、血量、战斗逻辑 |
| 建筑 | Entity | 需要血量、状态、同步 |
| 道具 | Entity | 需要属性、拾取逻辑 |
| 投射物 | Entity | 需要碰撞、伤害逻辑 |
| 爆炸特效 | Actor | 纯视觉，无游戏逻辑 |
| 背景音乐 | Actor | 纯音频播放 |
| UI 3D模型 | Actor | 界面展示，无游戏逻辑 |
| 装饰模型 | Actor | 纯装饰，不影响游戏 |
| 粒子效果 | Actor | 纯视觉反馈 |

## 🎮 实际场景分析

### 🗡️ 战斗系统

```csharp
// ✅ 正确的战斗系统设计
public class CombatSystem : IGameClass
{
    #if SERVER
    public static void ProcessAttack(Unit attacker, Unit target)
    {
        // 1. 游戏逻辑（Entity负责）
        var damage = CalculateDamage(attacker, target);
        var actualDamage = target.TakeDamage(damage);
        
        // 2. 同步到客户端（自动处理）
        // target的血量变化会自动同步
        
        // 3. 视觉效果（Actor负责）
        CreateCombatEffects(attacker, target, actualDamage);
    }
    #endif
    
    private static void CreateCombatEffects(Unit attacker, Unit target, float damage)
    {
        // 攻击特效
        var attackEffect = new ActorParticle(attackEffectLink, false, attacker);
        attackEffect.AttachTo(attacker, "weapon_socket");
        
        // 击中特效
        var hitEffect = new ActorParticle(hitEffectLink, false, target);
        hitEffect.AttachTo(target, "hit_socket");
        
        // 伤害数字
        var damageText = new ActorText(damageTextLink, false, target);
        damageText.Position = target.Position with { Z = target.Position.Z + 100 };
        damageText.SetText(damage.ToString("F0"));
        
        // 击中音效
        var hitSound = new ActorSound(hitSoundLink, true, target);
        hitSound.Position = target.Position;
    }
}
```

### 🏗️ 建造系统

```csharp
public class BuildingSystem : IGameClass
{
    // ✅ 正确：预览用Actor，实际建筑用Entity
    #if CLIENT
    private static ActorModel? _buildingPreview;
    
    public static void ShowBuildingPreview(IGameLink<GameDataUnit> buildingLink, ScenePoint position)
    {
        // 预览模型（纯视觉，客户端本地）
        _buildingPreview?.Dispose();
        _buildingPreview = new ActorModel(buildingLink.ActorLink, false, null);
        _buildingPreview.Position = position;
        _buildingPreview.Scale = new Vector3(0.8f, 0.8f, 0.8f);  // 半透明预览
    }
    
    public static void HideBuildingPreview()
    {
        _buildingPreview?.Dispose();
        _buildingPreview = null;
    }
    #endif
    
    #if SERVER
    public static void BuildStructure(Player player, IGameLink<GameDataUnit> buildingLink, ScenePoint position)
    {
        // 实际建筑（游戏逻辑，服务端权威）
        var building = buildingLink.Data?.CreateUnit(player, position, new Angle(0));
        if (building != null)
        {
            building.SetProperty(PropertyUnit.Health, building.GetProperty<float>(PropertyUnit.MaxHealth));
            
            // 建造完成效果
            CreateBuildCompleteEffect(building);
        }
    }
    #endif
    
    private static void CreateBuildCompleteEffect(Unit building)
    {
        var buildEffect = new ActorParticle(buildCompleteEffectLink, false, building);
        buildEffect.AttachTo(building, "center_socket");
        
        var buildSound = new ActorSound(buildCompleteSoundLink, true, building);
        buildSound.Position = building.Position;
    }
}
```

### 🎪 技能系统

```csharp
public class SkillSystem : IGameClass
{
    #if SERVER
    public static async Task CastFireball(Unit caster, ScenePoint targetPosition)
    {
        // 1. 游戏逻辑检查（Entity）
        if (!CanCastSpell(caster, fireballSpell))
            return;
            
        // 2. 扣除法力值（Entity）
        var manaCost = fireballSpell.Data.ManaCost;
        caster.AddProperty(PropertyUnit.Mana, -manaCost);
        
        // 3. 创建投射物（Entity - 需要碰撞检测）
        var projectile = CreateFireballProjectile(caster, targetPosition);
        
        // 4. 施法特效（Actor）
        CreateCastingEffects(caster);
        
        // 5. 等待投射物到达
        await WaitForProjectileHit(projectile);
        
        // 6. 目标区域伤害（Entity）
        DealAreaDamage(targetPosition, fireballSpell.Data.Damage);
        
        // 7. 爆炸特效（Actor）
        CreateExplosionEffects(targetPosition);
    }
    #endif
    
    private static void CreateCastingEffects(Unit caster)
    {
        // 施法光环
        var castingAura = new ActorParticle(castingAuraLink, false, caster);
        castingAura.AttachTo(caster, "body_socket");
        
        // 手部特效
        var handEffect = new ActorParticle(handGlowLink, false, caster);
        handEffect.AttachTo(caster, "hand_right");
        
        // 施法音效
        var castSound = new ActorSound(castSoundLink, true, caster);
        castSound.Position = caster.Position;
    }
    
    private static void CreateExplosionEffects(ScenePoint position)
    {
        var transientScope = new ActorScopeTransient(new PositionContext(position));
        
        // 爆炸特效
        var explosion = new ActorParticle(explosionEffectLink, false, transientScope);
        explosion.Position = position;
        
        // 火焰残留
        var flames = new ActorParticle(flameEffectLink, false, transientScope);
        flames.Position = position;
        
        // 爆炸音效
        var explosionSound = new ActorSound(explosionSoundLink, true, transientScope);
        explosionSound.Position = position;
        
        // 屏幕震动（如果在附近）
        if (Vector3.Distance(position, Player.LocalPlayer.Position) < 500)
        {
            CameraShake.Start(0.5f, 0.2f);
        }
    }
}
```

## 💡 设计模式

### 🎯 模式1：Entity + Actor 协作

**适用场景**：有游戏逻辑的对象需要视觉表现

```csharp
// Entity负责逻辑，Actor负责表现
public class UnitVisualSystem : IGameClass
{
    #if SERVER
    public static void OnUnitCreated(Unit unit)
    {
        // 创建对应的视觉Actor
        CreateUnitVisuals(unit);
    }
    #endif
    
    private static void CreateUnitVisuals(Unit unit)
    {
        // 主体模型
        var bodyModel = new ActorModel(unit.Cache.BodyModelLink, false, unit);
        bodyModel.AttachTo(unit, "body_socket");
        
        // 武器模型
        if (unit.Cache.WeaponModelLink != null)
        {
            var weaponModel = new ActorModel(unit.Cache.WeaponModelLink, false, unit);
            weaponModel.AttachTo(unit, "weapon_socket");
        }
        
        // 状态特效
        UpdateUnitStateEffects(unit);
    }
    
    private static void UpdateUnitStateEffects(Unit unit)
    {
        // 根据Unit状态创建对应的Actor效果
        if (unit.HasState(UnitState.Poisoned))
        {
            var poisonEffect = new ActorParticle(poisonEffectLink, false, unit);
            poisonEffect.AttachTo(unit, "status_socket");
        }
        
        if (unit.HasState(UnitState.Shielded))
        {
            var shieldEffect = new ActorParticle(shieldEffectLink, false, unit);
            shieldEffect.AttachTo(unit, "shield_socket");
        }
    }
}
```

### 🎨 模式2：纯Actor表现

**适用场景**：纯视觉效果，无游戏逻辑

```csharp
// 纯Actor实现的环境系统
public class EnvironmentEffects : IGameClass
{
    public static void CreateAmbientEffects(Scene scene)
    {
        var sceneScope = new ActorScopePersist(scene);
        
        // 环境粒子
        CreateEnvironmentParticles(sceneScope);
        
        // 环境音效
        CreateAmbientSounds(sceneScope);
        
        // 装饰模型
        CreateDecorations(sceneScope);
    }
    
    private static void CreateEnvironmentParticles(IActorScope scope)
    {
        // 飘落的叶子
        var leaves = new ActorParticle(leavesEffectLink, false, scope);
        leaves.Position = new ScenePoint(500, 300, scope.Context.Position.Scene);
        
        // 飞舞的萤火虫
        var fireflies = new ActorParticle(firefliesLink, false, scope);
        fireflies.Position = new ScenePoint(800, 600, scope.Context.Position.Scene);
    }
    
    private static void CreateAmbientSounds(IActorScope scope)
    {
        // 森林环境音
        var forestAmbient = new ActorSound(forestAmbientLink, false, scope);
        forestAmbient.Position = scope.Context.Position;
        
        // 鸟叫声
        var birdSounds = new ActorSound(birdSoundsLink, false, scope);
        birdSounds.Position = scope.Context.Position;
    }
}
```

### 🔄 模式3：动态切换

**适用场景**：同一对象在不同状态下需要不同的表现方式

```csharp
public class DynamicObjectSystem : IGameClass
{
    #if SERVER
    public static void TransformObject(Unit unit, IGameLink<GameDataUnit> newForm)
    {
        // 1. 更新Entity数据（游戏逻辑）
        unit.ChangeDataLink(newForm);
        unit.SetProperty(PropertyUnit.Health, newForm.Data.MaxHealth);
        
        // 2. 更新视觉表现（Actor）
        UpdateObjectVisuals(unit, newForm);
    }
    #endif
    
    private static void UpdateObjectVisuals(Unit unit, IGameLink<GameDataUnit> newForm)
    {
        // 变身特效
        var transformEffect = new ActorParticle(transformEffectLink, false, unit);
        transformEffect.AttachTo(unit, "center_socket");
        
        // 延迟更新模型
        _ = DelayedUpdateModel(unit, newForm);
    }
    
    private static async Task DelayedUpdateModel(Unit unit, IGameLink<GameDataUnit> newForm)
    {
        await Game.Delay(TimeSpan.FromSeconds(1));  // 等待变身特效
        
        if (unit.IsValid)
        {
            // 移除旧模型（如果有）
            var oldModel = Actor.GetByViewActorId(unit.EntityId);
            oldModel?.Dispose();
            
            // 创建新模型
            var newModel = new ActorModel(newForm.ActorLink, false, unit);
            newModel.AttachTo(unit, "body_socket");
        }
    }
}
```

## 🔄 协作模式

### 📡 事件驱动协作

```csharp
public class EntityActorSync : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }
    
    private static void OnGameTriggerInitialization()
    {
        // 监听Entity事件，创建对应的Actor效果
        var healthChangeTrigger = new Trigger<EventUnitHealthChanged>(OnUnitHealthChanged);
        healthChangeTrigger.Register(Game.Instance);
        
        var stateChangeTrigger = new Trigger<EventUnitStateChanged>(OnUnitStateChanged);
        stateChangeTrigger.Register(Game.Instance);
    }
    
    private static void OnUnitHealthChanged(EventUnitHealthChanged e)
    {
        var unit = e.Unit;
        var healthPercent = unit.GetProperty<float>(PropertyUnit.Health) / 
                           unit.GetProperty<float>(PropertyUnit.MaxHealth);
        
        // 根据血量显示不同的视觉效果
        if (healthPercent < 0.3f)
        {
            // 低血量特效
            var lowHealthEffect = new ActorParticle(lowHealthEffectLink, false, unit);
            lowHealthEffect.AttachTo(unit, "status_socket");
        }
        
        // 血量变化时的伤害/治疗数字
        var changeAmount = e.NewHealth - e.OldHealth;
        ShowHealthChange(unit, changeAmount);
    }
    
    private static void OnUnitStateChanged(EventUnitStateChanged e)
    {
        var unit = e.Unit;
        
        if (e.StateAdded)
        {
            // 状态添加时的视觉效果
            CreateStateEffect(unit, e.State);
        }
        else
        {
            // 状态移除时清理效果
            RemoveStateEffect(unit, e.State);
        }
    }
}
```

### 🎯 位置同步协作

```csharp
public class PositionSync : IGameClass
{
    private static readonly Dictionary<int, ActorModel> _unitModels = new();
    
    public static void OnRegisterGameClass()
    {
        Game.OnGameTriggerInitialization += OnGameTriggerInitialization;
    }
    
    private static void OnGameTriggerInitialization()
    {
        var positionTrigger = new Trigger<EventUnitPositionChanged>(OnUnitPositionChanged);
        positionTrigger.Register(Game.Instance);
    }
    
    private static void OnUnitPositionChanged(EventUnitPositionChanged e)
    {
        // Entity位置变化时，同步Actor位置
        if (_unitModels.TryGetValue(e.Unit.EntityId, out var model))
        {
            model.Position = e.NewPosition;
            
            // 添加移动轨迹特效
            if (e.Unit.HasState(UnitState.IsMoving))
            {
                CreateMovementTrail(model);
            }
        }
    }
    
    private static void CreateMovementTrail(ActorModel unitModel)
    {
        var trailEffect = new ActorParticle(movementTrailLink, false, unitModel.Scope);
        trailEffect.AttachTo(unitModel, "feet_socket");
    }
}
```

## ⚠️ 常见误区

### ❌ 误区1：万物皆Entity

```csharp
// ❌ 错误：所有对象都用Entity
#if SERVER
var explosionEntity = new ExplosionEntity(explosionLink, player, pos, facing);
var soundEntity = new SoundEntity(soundLink, player, pos, facing);
var decorationEntity = new DecorationEntity(decorationLink, player, pos, facing);
#endif

// 问题：
// 1. 大量不必要的网络流量
// 2. 服务端资源浪费
// 3. 客户端延迟显示
```

### ❌ 误区2：万物皆Actor

```csharp
// ❌ 错误：所有对象都用Actor
var enemyActor = new EnemyActor(enemyLink, false, scope);
enemyActor.Health = 100;  // Actor没有游戏属性！
enemyActor.Attack(player);  // Actor不处理游戏逻辑！

// 问题：
// 1. 无法同步游戏状态
// 2. 无法处理游戏逻辑
// 3. 客户端状态不一致
```

### ❌ 误区3：混合职责

```csharp
// ❌ 错误：在Entity中处理视觉细节
#if SERVER
public class Unit : Entity
{
    public void PlayAttackAnimation()  // 应该在Actor中！
    {
        AnimationController.Play("attack");
    }
    
    public void UpdateHealthBar()  // 应该在UI/Actor中！
    {
        HealthBarUI.SetValue(Health / MaxHealth);
    }
}
#endif

// ❌ 错误：在Actor中处理游戏逻辑
public class WeaponActor : ActorModel
{
    public void DealDamage(Unit target)  // 应该在Entity中！
    {
        target.TakeDamage(attackDamage);
    }
}
```

## ✅ 最佳实践

### 🎯 选择原则

1. **职责分离**
   - Entity：游戏逻辑、状态、属性、同步
   - Actor：视觉表现、特效、音效、动画

2. **同步需求**
   - 需要同步 → Entity
   - 不需要同步 → Actor

3. **创建权限**
   - 服务端权威 → Entity
   - 客户端本地 → Actor

4. **性能考虑**
   - 重要对象 → Entity
   - 装饰效果 → Actor

### 📋 检查清单

在设计游戏对象时，问自己以下问题：

- [ ] 这个对象有游戏逻辑吗？（血量、状态、行为）
- [ ] 这个对象需要同步给其他玩家吗？
- [ ] 这个对象会影响游戏结果吗？
- [ ] 这个对象需要持久化吗？
- [ ] 这个对象只是视觉效果吗？

**如果前4个问题有任何一个回答"是"，使用Entity**  
**如果只有最后一个问题回答"是"，使用Actor**

### 🎨 组合使用

```csharp
// ✅ 最佳实践：Entity + Actor 完美组合
public class GameObjectFactory : IGameClass
{
    #if SERVER
    public static Unit CreateUnit(IGameLink<GameDataUnit> unitLink, Player player, ScenePoint position)
    {
        // 1. 创建Entity（游戏逻辑）
        var unit = unitLink.Data?.CreateUnit(player, position, new Angle(0));
        if (unit == null) return null;
        
        // 2. 创建配套的Actor（视觉表现）
        CreateUnitVisuals(unit);
        
        return unit;
    }
    #endif
    
    private static void CreateUnitVisuals(Unit unit)
    {
        // 主体模型
        var bodyActor = new ActorModel(unit.Cache.ModelLink, false, unit);
        
        // 状态指示器
        var statusIndicator = new ActorParticle(statusIndicatorLink, false, unit);
        statusIndicator.AttachTo(bodyActor, "status_socket");
        
        // 阴影
        var shadow = new ActorModel(shadowLink, false, unit);
        shadow.Position = unit.Position with { Z = 0 };
    }
}
```

## 🔗 相关文档

- [🎭 Actor系统](../systems/ActorSystem.md) - Actor系统详细文档
- [🎯 单位系统](../systems/UnitSystem.md) - Entity/Unit系统详细文档
- [🚨 常见陷阱](../best-practices/CommonPitfalls.md) - 避免常见错误
- [🏗️ 框架概述](../FRAMEWORK_OVERVIEW.md) - 整体架构理解

---

> 💡 **核心原则**: Entity负责"逻辑的"，Actor负责"看到的"。两者各司其职，协作配合，构建完整的游戏体验。 