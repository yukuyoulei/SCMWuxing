# SyncType 快速参考（2D 游戏专用）

## 📖 概述

`SyncType` 控制 PropertyObject 同步到哪些客户端。对于 2D 游戏，只需使用 3 种：`Self`、`Ally`、`All`。

---

## ✅ 2D 游戏可用的 SyncType

### SyncType.All - 所有玩家

**同步范围**: 所有玩家的客户端

**适用场景**:
- 公共游戏对象（所有人看到相同的内容）
- 不包含私密信息的数据
- 需要全局可见的状态

**示例**:
```csharp
// FlappyBird 的管道（所有玩家看到相同的管道）
var pipe = CreateGameObject(Player.DefaultPlayer, SyncType.All);

// 公共敌人
var enemy = CreateGameObject(Player.DefaultPlayer, SyncType.All);

// 已打出的公共卡牌
var publicCard = CreateGameObject(player, SyncType.All);
```

---

### SyncType.Self - 仅所有者

**同步范围**: 仅对象所有者的客户端

**适用场景**:
- 玩家的私密数据
- 不应该让其他玩家看到的信息
- 个人状态

**示例**:
```csharp
// 玩家手牌（只有玩家自己能看到）
var handCard = CreateGameObject(player, SyncType.Self);

// 个人金币
var playerGold = CreateGameObject(player, SyncType.Self);

// 个人任务进度
var quest = CreateGameObject(player, SyncType.Self);
```

---

### SyncType.Ally - 所有者和队友

**同步范围**: 对象所有者和同队玩家的客户端

**适用场景**:
- 队伍共享信息
- 只让队友看到的数据
- 团队协作游戏

**示例**:
```csharp
// 队伍标记（队友可见）
var teamMarker = CreateGameObject(player, SyncType.Ally);

// 队伍共享资源
var teamResource = CreateGameObject(player, SyncType.Ally);

// MOBA 类游戏的队友位置标记
var allyPing = CreateGameObject(player, SyncType.Ally);
```

**注意**: 需要游戏支持队伍系统（玩家有队伍/阵营属性）。

---

## ❌ 2D 游戏不应使用的 SyncType

以下 SyncType 是为 3D 游戏的战争迷雾（FOW）设计的，2D 游戏**不应使用**：

### SyncType.Sight - 基于视野

**问题**: 2D 游戏没有"视野"概念，无法计算哪些对象在视野内

```csharp
// ❌ 错误：2D 游戏不要使用
var obj = CreateGameObject(player, SyncType.Sight);  // 需要战争迷雾系统
```

### SyncType.SelfOrSight - 自己或视野内

```csharp
// ❌ 错误：2D 游戏不要使用
var obj = CreateGameObject(player, SyncType.SelfOrSight);
```

### SyncType.AllyOrSight - 盟友或视野内

```csharp
// ❌ 错误：2D 游戏不要使用
var obj = CreateGameObject(player, SyncType.AllyOrSight);
```

**如果你需要类似效果**:
- 想要"自己能看到" → 使用 `SyncType.Self`
- 想要"队友能看到" → 使用 `SyncType.Ally`
- 想要"所有人能看到" → 使用 `SyncType.All`

---

## 📋 SyncType 选择决策树

```
需要同步给谁？
├─ 所有玩家 → SyncType.All
├─ 仅自己 → SyncType.Self
└─ 自己和队友 → SyncType.Ally
```

---

## 💡 常见场景示例

### 场景1：FlappyBird 类游戏

```csharp
// 管道（所有人看到相同的管道）
var pipe = CreateGameObject(Player.DefaultPlayer, SyncType.All);

// 每个玩家的小鸟（所有人都能看到）
var bird = CreateGameObject(player, SyncType.All);

// 游戏状态（所有人看到相同的状态）
var gameState = CreateGameObject(Player.DefaultPlayer, SyncType.All);
```

**结论**: FlappyBird 类游戏全部用 `SyncType.All`

---

### 场景2：卡牌对战游戏

```csharp
// 玩家手牌（只有自己能看到）
var handCard = CreateGameObject(player, SyncType.Self);

// 已打出的卡牌（所有人都能看到）
var playedCard = CreateGameObject(player, SyncType.All);

// 牌库顶（只有自己能看到）
var deckTop = CreateGameObject(player, SyncType.Self);

// 对手的手牌数量（所有人都能看到数量，但看不到具体牌）
// 方式1：用 All 同步数量
var opponentHandCount = CreateGameObject(player, SyncType.All);
opponentHandCount.SetProperty(PropertyHand.CardCount, count);  // 只同步数量

// 方式2：手牌用 Self，服务端单独发送数量给其他人
var handCard = CreateGameObject(player, SyncType.Self);  // 手牌内容
```

**结论**: 卡牌游戏混合使用 `Self` 和 `All`

---

### 场景3：团队 MOBA 游戏

```csharp
// 英雄（所有人都能看到）
var hero = CreateGameObject(player, SyncType.All);

// 队伍标记（队友可见，敌人不可见）
var teamPing = CreateGameObject(player, SyncType.Ally);

// 个人金币（只有自己能看到）
var gold = CreateGameObject(player, SyncType.Self);

// 队伍共享金币（队友可见）
var teamGold = CreateGameObject(player, SyncType.Ally);
```

**结论**: MOBA 游戏三种都会用到

---

### 场景4：合作 PvE 游戏

```csharp
// 怪物（所有人都能看到）
var monster = CreateGameObject(Player.DefaultPlayer, SyncType.All);

// 个人伤害统计（只有自己能看到）
var damageStats = CreateGameObject(player, SyncType.Self);

// 队伍总伤害（队友可见）
var teamDamage = CreateGameObject(player, SyncType.Ally);
```

---

## ⚠️ 常见错误

### 错误1：全部使用 All

```csharp
// ❌ 错误：手牌也同步给所有人
var handCard = CreateGameObject(player, SyncType.All);  // 其他玩家能看到你的牌！
```

**问题**: 泄露私密信息，浪费带宽

**解决**: 使用 `SyncType.Self`

---

### 错误2：使用 Sight 系列

```csharp
// ❌ 错误：2D 游戏使用视野同步
var obj = CreateGameObject(player, SyncType.Sight);  // 无效！
```

**问题**: 2D 游戏没有战争迷雾，视野系统无效

**解决**: 使用 `Self`/`Ally`/`All`

---

### 错误3：混淆 Self 和 Ally

```csharp
// ❌ 错误：想让队友看到，却用了 Self
var teamInfo = CreateGameObject(player, SyncType.Self);  // 队友看不到！
```

**解决**: 使用 `SyncType.Ally`

---

## 📚 参考文档

- [PropertyObject 文档](./PropertyObject.md) - PropertyObject 基础
- [Simple2DMultiplayerGame 框架](./Framework.md) - 框架使用
- [常见错误速查表](./CommonMistakes.md) - 错误13

---

**记住：2D 游戏只用 Self、Ally、All！** ✅

