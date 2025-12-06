# 调试作弊系统 (Debug Cheat System) 🎮

## 概述

调试作弊系统是WasiCore框架中专为开发者和测试人员设计的综合性调试工具套件。该系统仅在DEBUG模式下工作，提供了完整的图形界面和编程API，支持单位操作、状态控制、动画调试等多种功能。

## 核心功能

### 🎬 动画信息Overlay (Animation Info Overlay)

**最新添加的核心功能** - 实时动画调试信息显示系统

#### 功能特色
- **实时跟踪**: 鼠标悬停时自动检测并跟踪具有动画的单位/Actor
- **智能跟随**: 面板自动跟随目标移动，保持信息始终可见
- **死亡动画支持**: 特别支持显示死亡动画信息，不会在单位死亡时立即消失
- **性能优化**: 20FPS更新频率，对游戏性能影响最小
- **异常容错**: 多层异常保护，确保系统稳定性

#### 显示信息
- 动画资源名称
- 动画逻辑层
- 播放优先级
- 实时播放进度
- 循环状态
- 抑制状态
- 播放速度

#### 使用方法
```csharp
// 启用动画信息Overlay
AnimationInfoOverlay.EnableOverlay();

// 禁用动画信息Overlay  
AnimationInfoOverlay.DisableOverlay();

// 切换开关状态
AnimationInfoOverlay.ToggleOverlay();

// 检查是否启用
bool isEnabled = AnimationInfoOverlay.IsEnabled;
```

#### 技术实现
- **射线检测**: 使用 `RayCastMode.AABB` 模式确保兼容性
- **事件驱动**: 基于 `EventGamePointerButtonMove` 和 `EventGameTick`
- **坐标转换**: 世界坐标到屏幕坐标的实时转换
- **边界检测**: 自动处理屏幕边界和目标有效性

### 🛡️ 无敌系统

基于PropertyPlayer扩展的持续性无敌状态管理。

```csharp
// 设置无敌状态
DebugCheatSystem.SetPlayerInvincible(playerId, true);

// 检查无敌状态
bool isInvincible = DebugCheatSystem.IsPlayerInvincible(playerId);
```

### ✨ 单位创建系统

支持在指定位置创建任意类型的游戏单位。

```csharp
var unit = DebugCheatSystem.CreateUnit("UnitDataId", ownerId, position, facing);
```

### 📋 信息查询系统

提供精确的射线检测和单位信息获取功能。

```csharp
// 获取鼠标下的单位信息
string unitInfo = DebugCheatSystem.GetUnitInfoAtMousePosition();

// 获取单位详细属性
string properties = DebugCheatSystem.GetUnitProperties(unit);
```

## 使用指南

### 启用条件
- 必须在 `DEBUG` 编译模式下
- `Game.IsDebugTestMode` 必须为 `true`
- 使用 Client-Debug 或 Server-Debug 配置编译

### 界面操作
按下 **`Ctrl + F2`** 快捷键打开/关闭调试界面。

### 动画调试工作流程
1. 在调试界面中启用"Animation Info Overlay"开关
2. 将鼠标悬停在具有动画的单位上
3. 观察实时显示的动画信息面板
4. 面板会自动跟随目标移动
5. 支持观察死亡动画全过程

## API参考

### AnimationInfoOverlay类
```csharp
public static class AnimationInfoOverlay
{
    public static bool IsEnabled { get; }
    public static void EnableOverlay();
    public static void DisableOverlay(); 
    public static void ToggleOverlay();
}
```

### DebugCheatSystem类
```csharp
public static class DebugCheatSystem  
{
    // 初始化
    public static void Initialize();
    
    // 无敌功能
    public static void SetPlayerInvincible(int playerId, bool invincible);
    public static bool IsPlayerInvincible(int playerId);
    
    // 单位操作
    public static Unit? CreateUnit(string unitDataId, int ownerId, ScenePoint position, Angle facing = default);
    public static bool HealUnit(Unit unit);
    public static bool KillUnit(Unit unit);
    public static bool TeleportUnit(Unit unit, ScenePoint destination);
    
    // 信息获取
    public static string GetUnitInfoAtMousePosition();
    public static string GetUnitProperties(Unit unit);
    public static List<Player> GetAllPlayers();
    public static List<Unit> GetAllUnitsInScene(Scene scene);
}
```

## 注意事项

### 性能考虑
- 动画Overlay以20FPS频率更新，性能影响较小
- 建议仅在需要时启用动画Overlay功能
- 射线检测优化：使用AABB模式确保兼容性

### 兼容性
- 动画Overlay仅在客户端可用
- 单位操作功能仅在服务端有效
- 支持WebAssembly环境下的单线程运行

### 调试建议
- 使用日志系统跟踪操作结果
- 动画信息显示"Disposed"属于正常现象
- 建议在测试环境中充分验证功能

## 故障排除

### 动画Overlay常见问题

**Overlay不显示**
- 检查Client-Debug编译配置
- 确认已启用Animation Info Overlay开关
- 验证目标具有动画组件

**面板位置异常**
- 确认目标在屏幕可见范围
- 检查世界坐标转换是否正常

**显示"Disposed"信息**
- 正常现象，表示动画资源已释放
- 尝试切换到其他活跃目标

### 其他功能问题
详细的故障排除信息请参考 `TriggerEncapsulation/DEBUG_CHEAT_SYSTEM_README.md`。

## 扩展开发

要添加新的调试功能：

1. 在 `DebugCheatSystem` 中添加静态方法
2. 在 `DebugCheatUI` 中添加UI控件和事件处理
3. 确保新功能有适当的DEBUG检查保护
4. 更新相关文档和API参考

## 相关文档

- <see cref="TriggerEncapsulation.DebugCheatSystem"/>
- <see cref="TriggerEncapsulation.AnimationInfoOverlay"/>
- [模型动画系统](../systems/ModelAnimationSystem.md)
- [触发器系统](../systems/TriggerSystem.md) 