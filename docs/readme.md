# WasiCore 文档

欢迎使用 WasiCore 游戏框架！

---

## 🎯 快速导航

### 我想开发...

| 游戏类型 | 查看文档 | 说明 |
|---------|---------|------|
| **2D 联机游戏** | [Simple2DMultiplayer/](./Simple2DMultiplayer/) ⭐ | 卡牌、棋类、FlappyBird类 |
| **3D 游戏** | [systems/](./systems/) | 使用 Entity 系统的 3D 游戏 |

---

## 📚 2D 联机游戏框架（推荐从这里开始）

### 什么是 Simple2DMultiplayerGame 框架？

一个专为 2D 联机游戏设计的高级框架，极大简化开发流程：

- ✅ **代码量减少 ** - 只需编写游戏逻辑
- ✅ **开发时间减少%** - 2-3小时完成原型
- ✅ **AI 友好** - 95%+ 的 AI 辅助成功率
- ✅ **自动代码生成** - PropertyObjectWrapper 零样板代码

### 适用游戏类型

- 🃏 卡牌对战（炉石传说类）
- ♟️ 棋类游戏（五子棋、象棋）
- 🐦 动作游戏（多人 FlappyBird、跑酷）
- 🏰 2D 塔防
- 🎮 休闲竞技（贪吃蛇、Pong）

### 快速开始

**第一步**：查看 [Simple2DMultiplayer 文档](./Simple2DMultiplayer/)

**最快体验**：完成 [5分钟快速教程](./Simple2DMultiplayer/QUICKSTART.md)（Pong 游戏）

**完整示例**：运行 [FlappyBird 多人版](../Tests/Game/FlappyBirdMultiplayer/)

---

## 🏗️ 3D 游戏系统

如果你要开发 3D 游戏，查看 [systems/](./systems/) 目录下的文档：

### 核心系统

- **Entity 系统** - 3D 游戏对象
- **Unit 系统** - 游戏单位
- **Scene 系统** - 场景管理
- **物理系统** - 碰撞和物理
- **AI 系统** - 游戏 AI

---

## 📖 文档结构

```
docs/
├── Simple2DMultiplayer/    ← 2D 联机游戏框架（推荐）⭐
│   ├── README.md           # 文档导航
│   ├── QUICKSTART.md       # 5分钟快速教程
│   ├── Framework.md        # 框架完整文档
│   ├── CommonMistakes.md   # 常见错误速查表
│   ├── PropertyObject.md   # PropertyObject 基础
│   ├── TypeInference.md    # 类型推断规则
│   └── SyncType.md         # SyncType 参考
│
└── systems/                ← 3D 游戏系统文档
    ├── EntitySystem.md
    ├── UnitSystem.md
    ├── SceneSystem.md
    └── ...
```

---

## 🎓 学习建议

### 新手（2D 联机游戏）

1. 阅读 [Simple2DMultiplayer 文档导航](./Simple2DMultiplayer/)
2. 完成 [5分钟快速教程](./Simple2DMultiplayer/QUICKSTART.md)
3. 运行 FlappyBird 示例
4. 创建自己的游戏

**学习时间**: 3-4 小时

### 新手（3D 游戏）

1. 查看 [systems/](./systems/) 目录
2. 阅读 Entity 系统文档
3. 运行示例项目
4. 学习单位和场景系统

**学习时间**: 1-2 天

---

## 💡 选择建议

### 我应该使用哪个系统？

| 问题 | 答案 | 推荐 |
|------|------|------|
| 需要 3D 场景？ | 是 | Entity 系统 |
| 需要 3D 场景？ | 否 | Simple2DMultiplayer ⭐ |
| 需要物理碰撞？ | 是 | Entity 系统 |
| 需要物理碰撞？ | 否 | Simple2DMultiplayer ⭐ |
| 是联机游戏？ | 是（2D） | Simple2DMultiplayer ⭐ |
| 是联机游戏？ | 是（3D） | Entity 系统 |
| 是单机游戏？ | 是 | 不需要 PropertyObject |

**简单判断**:
- 🎮 **2D 联机游戏** → [Simple2DMultiplayer](./Simple2DMultiplayer/) ⭐
- 🎮 **3D 游戏** → [systems/](./systems/)
- 🎮 **单机游戏** → 纯客户端逻辑即可

---

## 🔗 其他资源

### 示例项目

在 `Tests/Game/` 目录下查看各种示例：
- **FlappyBirdMultiplayer** - 2D 联机游戏完整示例 ⭐
- **VampireSurvivors3D** - 3D 游戏示例
- **其他示例** - 各种系统的使用示例

---

**开始你的游戏开发之旅！** 🚀
