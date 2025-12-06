# 💬 对话系统（Conversation System）

对话系统是 WasiCore 游戏框架中用于管理游戏对话、剧情展示和玩家选择的核心系统。它提供了灵活而强大的机制来处理各种对话场景，支持角色台词、分支选择、语音播放、镜头控制等功能，并与框架的其他系统无缝集成。

## 📋 目录

- [🏗️ 系统概述](#系统概述)
- [📊 核心概念](#核心概念)
- [🎯 快速开始](#快速开始)
- [📦 GameData 定义](#gamedata-定义)
- [🎨 UI 实现](#ui-实现)
- [🔄 对话流程](#对话流程)
- [⚙️ 高级特性](#高级特性)
- [🎮 实用示例](#实用示例)
- [🔧 API 参考](#api-参考)
- [💡 最佳实践](#最佳实践)
- [⚠️ 常见问题](#常见问题)

## 🏗️ 系统概述

### 架构设计

对话系统采用数据驱动的设计模式，核心架构如下：

```
GameDataConversation → Conversation → IConversationUI → 实际UI实现
       ↓                    ↓              ↓                ↓
    数据定义            运行时逻辑      UI接口          具体展示
```

### 核心特性

- ✅ **灵活的对话结构** - 支持线性台词、分支选择、并行对话等多种结构
- ✅ **丰富的表现形式** - 支持角色立绘、语音、动画、镜头控制
- ✅ **客户端驱动** - 对话逻辑在客户端执行，服务器可选择性监听
- ✅ **UI 解耦** - 通过接口抽象，支持任意 UI 实现
- ✅ **条件验证** - 支持条件判断和动态内容
- ✅ **超时机制** - 选择支持超时和默认选项
- ✅ **参数化文本** - 支持动态文本替换
- ✅ **本地化支持** - 完整的多语言支持
- ✅ **事件系统** - 提供对话生命周期事件

### 系统位置

对话系统位于 `GameUI` 命名空间，仅在客户端可用：

```
GameUI.ConversationSystem/
├── Data/                    # 数据定义
│   ├── GameDataConversation.cs
│   ├── GameDataConversationLine.cs
│   ├── GameDataConversationChoiceGroup.cs
│   ├── GameDataConversationChoiceItem.cs
│   ├── GameDataConversationSet.cs
│   ├── GameDataConversationWait.cs
│   ├── GameDataConversationCustomAction.cs
│   └── GameDataCharacter.cs
├── IConversationUI.cs       # UI 接口
├── Conversation.cs          # 运行时基类
├── ConversationLine.cs      # 台词实现
├── ConversationChoiceGroup.cs # 选择实现
└── Event/                   # 事件定义
```

## 📊 核心概念

### 对话类型

框架提供了多种对话类型，每种类型有特定的用途：

| 类型 | 说明 | 用途 |
|-----|------|------|
| `ConversationLine` | 单条台词 | 角色说话、旁白、提示 |
| `ConversationChoiceGroup` | 选择组 | 玩家分支选择 |
| `ConversationSet` | 对话集合 | 组织多条台词 |
| `ConversationWait` | 等待 | 延迟或等待条件 |
| `ConversationCustomAction` | 自定义动作 | 执行游戏逻辑 |

### 数据层与运行时层

```
数据层（GameData*）        运行时层（Conversation*）
     ↓                           ↓
  定义内容                    执行逻辑
  可序列化                    生命周期管理
  可编辑                      UI交互
```

### UI 接口

对话系统通过 `IConversationUI` 接口与 UI 层解耦：

```csharp
public interface IConversationUI
{
    Task ShowLineAsync(ConversationLineInfo lineInfo, 
                      GameDataConversationLine? lineData);
    
    Task<int> ShowChoicesAsync(List<ConversationChoiceInfo> choices, 
                              GameDataConversationChoiceGroup? choiceGroupData, 
                              CancellationToken cancellationToken = default);
    
    void Hide();
    void Clear();
}
```

## 🎯 快速开始

### 1. 注册 UI 实现

首先需要实现 `IConversationUI` 接口并注册：

```csharp
// 实现 UI 接口
public class MyConversationUI : IConversationUI
{
    public async Task ShowLineAsync(ConversationLineInfo lineInfo, 
                                   GameDataConversationLine? lineData)
    {
        // 显示台词
        ShowDialogBox(lineInfo.Text, lineInfo.CharacterName);
        
        if (lineInfo.WaitForConfirmation)
        {
            await WaitForPlayerClick();
        }
        else if (lineInfo.Duration.HasValue)
        {
            await Task.Delay(lineInfo.Duration.Value);
        }
    }

    public async Task<int> ShowChoicesAsync(List<ConversationChoiceInfo> choices, 
                                           GameDataConversationChoiceGroup? choiceGroupData,
                                           CancellationToken cancellationToken)
    {
        // 显示选择列表
        return await ShowChoiceDialog(choices, cancellationToken);
    }

    public void Hide() => HideDialogBox();
    public void Clear() => ClearDialogContent();
}

// 在游戏初始化时注册
ConversationUI.Register(new MyConversationUI());
```

### 2. 创建简单对话

```csharp
// 创建角色
var hero = new GameDataCharacter
{
    DisplayName = new LocalizedString("英雄", "Hero"),
    Portrait = heroPortraitTexture
};

// 创建台词
var greeting = new GameDataConversationLine
{
    Character = hero.ToLink(),
    Text = new LocalizedString("你好，欢迎来到这个世界！", "Hello, welcome to this world!"),
    WaitForConfirmation = true,
    AllowSkip = true
};

// 播放对话
await greeting.StartConversation();
```

### 3. 创建分支选择

```csharp
// 创建选择项
var choice1 = new GameDataConversationChoiceItem
{
    Text = new LocalizedString("接受任务", "Accept Quest"),
    NextConversation = acceptQuestDialog.ToLink()
};

var choice2 = new GameDataConversationChoiceItem
{
    Text = new LocalizedString("拒绝任务", "Decline Quest"),
    NextConversation = declineQuestDialog.ToLink()
};

// 创建选择组
var choiceGroup = new GameDataConversationChoiceGroup
{
    Choices = [choice1.ToLink(), choice2.ToLink()],
    Timeout = TimeSpan.FromSeconds(30),  // 30秒超时
    DefaultChoiceOnTimeout = 1            // 超时默认选择第2项（拒绝）
};

// 播放对话
await choiceGroup.StartConversation();
```

## 📦 GameData 定义

### GameDataConversationLine（台词）

```csharp
var line = new GameDataConversationLine
{
    // === 基础内容 ===
    Character = characterLink,              // 说话的角色
    Text = new LocalizedString("台词内容"),  // 台词文本（支持本地化）
    TextParameters = new List<Func<IExecutionContext, object>>  // 文本参数
    {
        ctx => ctx.MainTarget.DisplayName   // 动态替换 {0}
    },
    
    // === 语音 ===
    VoiceLink = voiceActorLink,            // 语音资源
    
    // === 视觉效果 ===
    PortraitSide = PortraitSide.Left,      // 立绘位置（左/右）
    SpeakerAnimation = animationLink,      // 角色动画
    
    // === 镜头控制 ===
    Camera = cameraLink,                    // 指定镜头
    CameraFollowCharacter = true,          // 镜头跟随角色
    
    // === 时间控制 ===
    Duration = TimeSpan.FromSeconds(3),    // 持续时间（null则自动计算）
    WaitForConfirmation = true,            // 需要玩家确认
    AllowSkip = true,                      // 允许跳过
    
    // === 条件验证 ===
    Validators = ctx => QuestCompleted(ctx) // 条件判断
};
```

### GameDataConversationChoiceGroup（选择组）

```csharp
var choiceGroup = new GameDataConversationChoiceGroup
{
    // 选择项列表
    Choices = new List<IGameLink<GameDataConversationChoiceItem>?>
    {
        choice1Link,
        choice2Link,
        choice3Link
    },
    
    // 超时设置
    Timeout = TimeSpan.FromSeconds(30),     // 超时时间
    DefaultChoiceOnTimeout = 0              // 超时默认选择索引
};
```

### GameDataConversationChoiceItem（选择项）

```csharp
var choiceItem = new GameDataConversationChoiceItem
{
    Text = new LocalizedString("选项文本"),  // 选项文本
    NextConversation = nextDialogLink,      // 后续对话
    Validators = ctx => HasItem(ctx),       // 显示条件
    ServerEffect = rewardEffectLink,        // 服务器端执行的效果
    NotifyServer = true                     // 是否通知服务器
};
```

**ServerEffect 说明：**
- 当玩家选择此选项时，服务器会自动执行指定的效果
- 施法者和目标为本地玩家的主控单位
- 如果本地玩家没有主控单位，则不执行
- 设置了 `ServerEffect` 后会自动通知服务器（无论 `NotifyServer` 如何设置）
- 优先于 `EventPlayerConversationChoiceSelected` 事件执行

### GameDataConversationSet（对话集合）

```csharp
var conversationSet = new GameDataConversationSet
{
    Lines = new List<IGameLink<GameDataConversation>?>
    {
        line1Link,
        line2Link,
        line3Link
    },
    
    // 执行模式
    ExecutionMode = ConversationExecutionMode.Sequential,  // 顺序播放
    // 或
    // ExecutionMode = ConversationExecutionMode.RandomOne,  // 随机播放一条
    
    LineDisplayMode = ConversationDisplayMode.UI           // 显示模式
};
```

### GameDataCharacter（角色）

```csharp
var character = new GameDataCharacter
{
    DisplayName = new LocalizedString("角色名", "Character Name"),
    Portrait = portraitTexture,            // 立绘图片
    NameColorOverride = Color.Gold,        // 名字颜色
    Description = new LocalizedString("角色描述"),
    InGameUnit = unitInstance              // 关联的游戏单位（可选）
};
```

## 🎨 UI 实现

### 实现 IConversationUI

完整的 UI 实现示例：

```csharp
public class AdvancedConversationUI : IConversationUI
{
    private UIElement dialogBox;
    private TaskCompletionSource<int>? choiceTcs;
    
    public async Task ShowLineAsync(ConversationLineInfo lineInfo, 
                                   GameDataConversationLine? lineData)
    {
        // 显示对话框
        dialogBox.Visibility = Visibility.Visible;
        
        // 设置角色立绘
        if (lineInfo.Portrait is not null)
        {
            portraitImage.Source = lineInfo.Portrait;
            portraitImage.HorizontalAlignment = lineInfo.PortraitSide == PortraitSide.Left 
                ? HorizontalAlignment.Left 
                : HorizontalAlignment.Right;
        }
        
        // 设置角色名
        if (lineInfo.CharacterName is not null)
        {
            characterNameText.Text = lineInfo.CharacterName;
            characterNameText.Visibility = Visibility.Visible;
        }
        
        // 打字机效果显示文本
        await TypewriterEffect(dialogText, lineInfo.Text);
        
        // 等待玩家确认或自动结束
        if (lineInfo.WaitForConfirmation)
        {
            await WaitForConfirmation(lineInfo.AllowSkip);
        }
        else if (lineInfo.Duration.HasValue)
        {
            await Task.Delay(lineInfo.Duration.Value);
        }
    }
    
    public async Task<int> ShowChoicesAsync(
        List<ConversationChoiceInfo> choices, 
        GameDataConversationChoiceGroup? choiceGroupData,
        CancellationToken cancellationToken)
    {
        choiceTcs = new TaskCompletionSource<int>();
        
        // 注册取消令牌
        using var registration = cancellationToken.Register(() => 
        {
            choiceTcs?.TrySetCanceled(cancellationToken);
        });
        
        // 显示选择按钮
        choicePanel.Children.Clear();
        for (int i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            var button = new Button
            {
                Content = choice.Text,
                IsEnabled = choice.IsEnabled,
                Tag = i
            };
            button.Click += OnChoiceClick;
            choicePanel.Children.Add(button);
        }
        
        choicePanel.Visibility = Visibility.Visible;
        
        try
        {
            return await choiceTcs.Task;
        }
        finally
        {
            choicePanel.Visibility = Visibility.Collapsed;
        }
    }
    
    private void OnChoiceClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int index)
        {
            choiceTcs?.TrySetResult(index);
        }
    }
    
    public void Hide()
    {
        dialogBox.Visibility = Visibility.Collapsed;
    }
    
    public void Clear()
    {
        dialogText.Text = string.Empty;
        characterNameText.Text = string.Empty;
        portraitImage.Source = null;
        choicePanel.Children.Clear();
    }
    
    private async Task TypewriterEffect(TextBlock textBlock, string text)
    {
        textBlock.Text = string.Empty;
        foreach (char c in text)
        {
            textBlock.Text += c;
            await Task.Delay(30);  // 每个字符30ms
        }
    }
    
    private async Task WaitForConfirmation(bool allowSkip)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        void OnClick(object? sender, PointerEventArgs e)
        {
            tcs.TrySetResult(true);
        }
        
        if (allowSkip)
        {
            // 允许点击跳过
            dialogBox.PointerPressed += OnClick;
        }
        
        // 显示继续提示
        continueHint.Visibility = Visibility.Visible;
        
        try
        {
            await tcs.Task;
        }
        finally
        {
            continueHint.Visibility = Visibility.Collapsed;
            if (allowSkip)
            {
                dialogBox.PointerPressed -= OnClick;
            }
        }
    }
}
```

### UI 设计建议

1. **视觉层次**
   - 角色立绘应突出显示
   - 文本应易于阅读（对比度、字体大小）
   - 选择按钮应明显区分可用/不可用状态

2. **动画效果**
   - 打字机效果提升沉浸感
   - 立绘淡入淡出
   - 选择按钮悬停效果

3. **响应式设计**
   - 支持不同分辨率
   - 移动端适配
   - 键盘快捷键支持

4. **可访问性**
   - 支持屏幕阅读器
   - 可调整文字大小
   - 颜色盲友好

## 🔄 对话流程

### 基本流程

```
开始 → 验证条件 → 显示内容 → 等待交互 → 执行后续 → 结束
  ↓        ↓          ↓          ↓          ↓        ↓
创建    Validators   UI显示   玩家操作   下一个   清理
```

### 生命周期事件

```csharp
// 订阅对话开始事件
Game.Instance.Subscribe<EventConversationStarted>(e =>
{
    Game.Logger.LogInfo("对话开始: {0}", e.Conversation.Link.FriendlyName);
    // 可以在这里暂停游戏、禁用输入等
});

// 订阅对话完成事件
Game.Instance.Subscribe<EventConversationCompleted>(e =>
{
    Game.Logger.LogInfo("对话完成: {0}", e.Conversation.Link.FriendlyName);
    // 恢复游戏状态
});

// 订阅选择事件
Game.Instance.Subscribe<EventConversationChoiceSelected>(e =>
{
    Game.Logger.LogInfo("玩家选择: {0} (索引 {1})", 
        e.ChoiceLink.FriendlyName, 
        e.ChoiceIndex);
    // 记录玩家选择、影响剧情走向等
});
```

### 执行上下文

对话执行需要一个上下文，默认使用本地玩家的主控单位：

```csharp
// 使用默认上下文（本地玩家主控单位）
await conversation.StartConversation();

// 指定自定义上下文
await conversation.StartConversation(customUnit);

// 共享父级上下文
var childConversation = new ConversationLine(link, parentConversation);
```

## ⚙️ 高级特性

### 1. 动态文本参数

```csharp
var line = new GameDataConversationLine
{
    // 文本中使用 {0}, {1} 等占位符
    Text = new LocalizedString("你好，{0}！你有 {1} 金币。"),
    TextParameters = new List<Func<IExecutionContext, object>>
    {
        ctx => ctx.MainTarget.DisplayName,     // {0}: 玩家名字
        ctx => GetGoldAmount(ctx.MainTarget)   // {1}: 金币数量
    }
};
```

### 2. 条件验证

```csharp
var choiceItem = new GameDataConversationChoiceItem
{
    Text = new LocalizedString("购买装备（需要100金币）"),
    Validators = ctx =>
    {
        var gold = GetGoldAmount(ctx.MainTarget);
        if (gold < 100)
        {
            return CmdError.NotEnoughResources;
        }
        return CmdResult.Ok;
    },
    NextConversation = purchaseDialogLink
};

// 不满足条件的选项会显示为灰色（IsEnabled = false）
```

### 3. 服务器效果执行

对话系统提供两种方式在服务器端执行逻辑：

#### 方式 1：使用 ServerEffect（推荐）

```csharp
// 定义奖励效果
var questRewardEffect = new GameDataEffectSet
{
    Effects = new List<IGameLink<GameDataEffect>?>
    {
        giveGoldEffect.ToLink(),
        giveExpEffect.ToLink(),
        completeQuestEffect.ToLink()
    }
};

var choiceItem = new GameDataConversationChoiceItem
{
    Text = new LocalizedString("接受任务"),
    ServerEffect = questRewardEffect.ToLink(),  // 服务器自动执行效果
    NextConversation = questAcceptedDialog.ToLink()
};

// 优点：
// - 自动执行，无需额外代码
// - 使用效果系统，功能强大
// - 施法者和目标自动设置为玩家主控单位
// - 安全可靠，服务器端执行
```

#### 方式 2：监听选择事件

```csharp
var choiceItem = new GameDataConversationChoiceItem
{
    Text = new LocalizedString("接受任务"),
    NotifyServer = true,  // 启用服务器通知
    NextConversation = questAcceptedDialog.ToLink()
};

// 在服务器端监听
#if SERVER
Game.Instance.Subscribe<EventPlayerConversationChoiceSelected>(e =>
{
    var player = e.Player;
    var choice = e.ChoiceLink.Data;
    
    // 服务器端验证和处理
    if (VerifyChoice(player, choice))
    {
        GiveQuestReward(player);
    }
});
#endif

// 优点：
// - 灵活性高，可以执行任意逻辑
// - 适合复杂的条件判断
// - 可以访问玩家对象
```

**最佳实践：**
- 简单的奖励、状态变更等使用 `ServerEffect`
- 复杂的业务逻辑使用事件监听
- 两者可以同时使用（ServerEffect 先执行，然后触发事件）

### 4. 超时机制

```csharp
var choiceGroup = new GameDataConversationChoiceGroup
{
    Choices = choices,
    Timeout = TimeSpan.FromSeconds(15),  // 15秒超时
    DefaultChoiceOnTimeout = 0           // 超时选择第一项
};

// UI 实现需要响应 CancellationToken
public async Task<int> ShowChoicesAsync(
    List<ConversationChoiceInfo> choices,
    GameDataConversationChoiceGroup? choiceGroupData,
    CancellationToken cancellationToken)
{
    var tcs = new TaskCompletionSource<int>();
    
    // 注册取消回调
    using var registration = cancellationToken.Register(() => 
    {
        tcs.TrySetCanceled(cancellationToken);
    });
    
    // ... 显示UI并等待选择 ...
    
    return await tcs.Task;  // 超时时会抛出 OperationCanceledException
}
```

### 5. 自定义动作

```csharp
var customAction = new GameDataConversationCustomAction
{
    Func = async (conversation) =>
    {
        // 执行任意游戏逻辑
        await PlayCutscene();
        GiveItemToPlayer(conversation.MainTarget);
        
        // 返回 true 继续下一个对话，false 中断
        return true;
    },
    NextConversation = nextDialogLink
};
```

### 6. 镜头控制

```csharp
var line = new GameDataConversationLine
{
    Character = npcCharacter.ToLink(),
    Text = new LocalizedString("看那边！"),
    Camera = specialCameraLink,          // 使用特定镜头
    CameraFollowCharacter = false        // 镜头不跟随角色移动
};

// 对话结束后会自动恢复默认镜头
```

### 7. 持续时间自动计算

框架提供了智能的持续时间估算器：

```csharp
var line = new GameDataConversationLine
{
    Text = new LocalizedString("这是一段较长的台词，系统会根据文本长度自动计算显示时间。"),
    Duration = null  // 不指定时间，自动计算
};

// 计算规则：
// - 基础时间：1000ms
// - 每个字符：50ms
// - 标点符号额外停顿（。！？ 400ms，，； 200-300ms，… 500ms）
// - 最小时间：1500ms
// - 最大时间：15000ms
```

## 🎮 实用示例

### 示例 1：简单的 NPC 对话

```csharp
public static class NPCDialogs
{
    public static readonly GameDataCharacter Merchant = new()
    {
        DisplayName = new LocalizedString("商人", "Merchant"),
        Portrait = Resources.MerchantPortrait
    };
    
    public static readonly GameDataConversationLine Greeting = new()
    {
        Character = Merchant.ToLink(),
        Text = new LocalizedString("欢迎光临！看看我的商品吧。", "Welcome! Take a look at my wares."),
        WaitForConfirmation = true
    };
    
    public static async Task StartMerchantDialog(Unit player)
    {
        await Greeting.StartConversation(player);
    }
}
```

### 示例 2：任务接受对话

```csharp
public class QuestDialog
{
    public static async Task ShowQuestOffer(Quest quest, Unit player)
    {
        // 任务说明
        var questDescription = new GameDataConversationLine
        {
            Character = quest.QuestGiver.ToLink(),
            Text = quest.Description,
            WaitForConfirmation = true
        };
        
        // 创建任务接受效果（服务器端执行）
        var acceptQuestEffect = new GameDataEffectSet
        {
            Effects = new List<IGameLink<GameDataEffect>?>
            {
                quest.AcceptEffect?.ToLink(),      // 接受任务
                CreateGoldReward(50).ToLink(),     // 给予50金币
                CreateExpReward(100).ToLink()      // 给予100经验
            }
        };
        
        // 接受选项
        var acceptChoice = new GameDataConversationChoiceItem
        {
            Text = new LocalizedString("我接受这个任务！"),
            ServerEffect = acceptQuestEffect.ToLink(),  // 服务器自动执行奖励
            NextConversation = CreateAcceptDialog(quest).ToLink()
        };
        
        // 拒绝选项
        var declineChoice = new GameDataConversationChoiceItem
        {
            Text = new LocalizedString("我再考虑考虑。"),
            NextConversation = CreateDeclineDialog().ToLink()
        };
        
        // 选择组
        var choices = new GameDataConversationChoiceGroup
        {
            Choices = [acceptChoice.ToLink(), declineChoice.ToLink()]
        };
        
        // 创建对话集合
        var conversation = new GameDataConversationSet
        {
            Lines = [questDescription.ToLink(), choices.ToLink()],
            ExecutionMode = ConversationExecutionMode.Sequential
        };
        
        await conversation.StartConversation(player);
    }
    
    private static GameDataConversationLine CreateAcceptDialog(Quest quest)
    {
        return new GameDataConversationLine
        {
            Character = quest.QuestGiver.ToLink(),
            Text = new LocalizedString("太好了！祝你好运。"),
            WaitForConfirmation = true
        };
    }
    
    private static GameDataConversationLine CreateDeclineDialog()
    {
        return new GameDataConversationLine
        {
            Text = new LocalizedString("好的，有需要再来找我。"),
            WaitForConfirmation = true
        };
    }
    
    private static GameDataEffect CreateGoldReward(int amount)
    {
        // 示例：创建金币奖励效果
        return new GameDataEffectModifyProperty
        {
            Property = UnitProperty.Gold,
            Value = _ => amount
        };
    }
    
    private static GameDataEffect CreateExpReward(int amount)
    {
        // 示例：创建经验奖励效果
        return new GameDataEffectModifyProperty
        {
            Property = UnitProperty.Experience,
            Value = _ => amount
        };
    }
}
```

### 示例 3：带超时的战斗选择

```csharp
public static async Task<BattleChoice> ShowBattleChoice()
{
    var attackChoice = new GameDataConversationChoiceItem
    {
        Text = new LocalizedString("攻击"),
        NotifyServer = true
    };
    
    var defendChoice = new GameDataConversationChoiceItem
    {
        Text = new LocalizedString("防御"),
        NotifyServer = true
    };
    
    var fleeChoice = new GameDataConversationChoiceItem
    {
        Text = new LocalizedString("逃跑"),
        NotifyServer = true
    };
    
    var choices = new GameDataConversationChoiceGroup
    {
        Choices = [
            attackChoice.ToLink(), 
            defendChoice.ToLink(), 
            fleeChoice.ToLink()
        ],
        Timeout = TimeSpan.FromSeconds(10),  // 10秒超时
        DefaultChoiceOnTimeout = 1            // 超时默认防御
    };
    
    await choices.StartConversation();
    
    // 服务器端会收到选择事件
    return BattleChoice.Defend;  // 示例返回
}
```

### 示例 4：随机 NPC 闲聊

```csharp
public static readonly GameDataConversationSet RandomGreetings = new()
{
    Lines = new List<IGameLink<GameDataConversation>?>
    {
        CreateLine("今天天气真好！").ToLink(),
        CreateLine("你好陌生人。").ToLink(),
        CreateLine("城里最近不太平啊...").ToLink(),
        CreateLine("听说北方有宝藏。").ToLink()
    },
    ExecutionMode = ConversationExecutionMode.RandomOne  // 随机播放一条
};

// 每次交互都会随机说一句
await RandomGreetings.StartConversation(player);
```

### 示例 5：过场动画对话

```csharp
public static async Task PlayCutsceneDialog()
{
    var scene = new GameDataConversationSet
    {
        Lines = new List<IGameLink<GameDataConversation>?>
        {
            // 第一句：英雄
            new GameDataConversationLine
            {
                Character = Hero.ToLink(),
                Text = new LocalizedString("终于找到你了！"),
                Camera = heroCloseupCamera.ToLink(),
                VoiceLink = heroVoice1.ToLink(),
                Duration = null,  // 根据语音长度
                WaitForConfirmation = false
            }.ToLink(),
            
            // 等待1秒
            new GameDataConversationWait
            {
                Duration = TimeSpan.FromSeconds(1)
            }.ToLink(),
            
            // 第二句：反派
            new GameDataConversationLine
            {
                Character = Villain.ToLink(),
                Text = new LocalizedString("你来得正好..."),
                Camera = villainCamera.ToLink(),
                VoiceLink = villainVoice1.ToLink(),
                Duration = null,
                WaitForConfirmation = false
            }.ToLink(),
            
            // 自定义动作：播放战斗开始动画
            new GameDataConversationCustomAction
            {
                Func = async (ctx) =>
                {
                    await PlayBattleStartAnimation();
                    return true;
                }
            }.ToLink()
        },
        ExecutionMode = ConversationExecutionMode.Sequential
    };
    
    await scene.StartConversation();
}
```

## 🔧 API 参考

### 核心类

#### Conversation

```csharp
public abstract class Conversation : IExecutableObject, IGameObject<GameDataConversation>
{
    // 对话数据
    public IGameLink<GameDataConversation> Link { get; }
    public GameDataConversation Cache { get; }
    
    // 执行上下文
    public IExecutableObject? Parent { get; }
    public ExecutionParamShared Shared { get; }
    public ITarget Source { get; }
    
    // 启动对话
    public Task<CmdResult> StartAsync();
    
    // 子类实现
    public abstract Task Execute();
}
```

#### ConversationLineInfo

```csharp
public class ConversationLineInfo
{
    public required string Text { get; set; }              // 台词文本
    public string? CharacterName { get; set; }            // 角色名称
    public Texture? Portrait { get; set; }                // 角色立绘
    public PortraitSide PortraitSide { get; set; }       // 立绘位置
    public TimeSpan? Duration { get; set; }               // 持续时间
    public bool WaitForConfirmation { get; set; }        // 等待确认
    public bool AllowSkip { get; set; }                   // 允许跳过
}
```

#### ConversationChoiceInfo

```csharp
public class ConversationChoiceInfo
{
    public required string Text { get; set; }  // 选择文本
    public bool IsEnabled { get; set; }        // 是否可用
}
```

### 扩展方法

```csharp
public static class ConversationExtensions
{
    // 启动对话的便捷方法
    public static async Task<CmdResult> StartConversation(
        this GameDataConversation data, 
        IExecutionContext? context = null);
}
```

### 事件

```csharp
// 对话开始事件
public record struct EventConversationStarted(Conversation Conversation);

// 对话完成事件
public record struct EventConversationCompleted(Conversation Conversation);

// 选择事件（客户端）
public record struct EventConversationChoiceSelected(
    Conversation Conversation, 
    int ChoiceIndex, 
    IGameLink<GameDataConversationChoiceItem> ChoiceLink);

// 选择事件（服务器）
public record struct EventPlayerConversationChoiceSelected(
    Player Player, 
    IGameLink<GameDataConversationChoiceItem> ChoiceLink);
```

## 💡 最佳实践

### 1. 数据组织

```csharp
// ✅ 推荐：集中管理对话数据
public static class GameDialogs
{
    // 角色定义
    public static class Characters
    {
        public static readonly GameDataCharacter Hero = new() { /*...*/ };
        public static readonly GameDataCharacter Merchant = new() { /*...*/ };
    }
    
    // 按场景组织对话
    public static class Chapter1
    {
        public static readonly GameDataConversation Opening = /*...*/;
        public static readonly GameDataConversation MeetMerchant = /*...*/;
    }
}

// ❌ 避免：到处分散创建对话数据
```

### 2. UI 性能

```csharp
// ✅ 推荐：复用 UI 元素
public class ConversationUIPool
{
    private readonly Stack<DialogBox> pool = new();
    
    public DialogBox Get()
    {
        if (pool.Count > 0)
            return pool.Pop();
        return new DialogBox();
    }
    
    public void Return(DialogBox box)
    {
        box.Clear();
        pool.Push(box);
    }
}

// ❌ 避免：每次都创建新的 UI 元素
```

### 3. 错误处理

```csharp
// ✅ 推荐：优雅地处理错误
try
{
    await conversation.StartConversation();
}
catch (Exception ex)
{
    Game.Logger.LogError(ex, "对话执行失败");
    // 显示友好的错误提示给玩家
    ShowErrorToast("对话系统暂时不可用");
}

// ❌ 避免：让异常直接崩溃游戏
```

### 4. 内存管理

```csharp
// ✅ 推荐：及时清理引用
public class MyConversationUI : IConversationUI
{
    public void Clear()
    {
        dialogText.Text = string.Empty;
        portraitImage.Source = null;  // 释放纹理引用
        choiceButtons.Clear();
    }
}

// ❌ 避免：持有大量对话历史记录
```

### 5. 测试友好

```csharp
// ✅ 推荐：使用验证器而非硬编码条件
var choice = new GameDataConversationChoiceItem
{
    Text = new LocalizedString("高级选项"),
    Validators = ctx => PlayerLevel(ctx) >= 10  // 可测试
};

// ❌ 避免：在 UI 层做条件判断
```

### 6. 本地化

```csharp
// ✅ 推荐：使用 LocalizedString
Text = new LocalizedString("你好", "Hello", "Bonjour")

// ❌ 避免：硬编码文本
Text = "你好"  // 无法本地化
```

### 7. 可维护性

```csharp
// ✅ 推荐：使用命名常量
public static class ConversationConstants
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    public static readonly int MaxChoices = 6;
}

// ❌ 避免：魔法数字
Timeout = TimeSpan.FromSeconds(30)  // 这个30是什么意思？
```

## ⚠️ 常见问题

### Q: 对话系统是否支持语音？
A: 支持。通过 `GameDataConversationLine.VoiceLink` 设置语音资源。系统会自动播放语音，并根据语音长度自动调整持续时间（未来版本）。

### Q: 如何实现打字机效果？
A: 在 UI 实现的 `ShowLineAsync` 方法中自己实现。框架提供文本和时间信息，UI 层负责具体的展示效果。

### Q: 对话可以暂停吗？
A: 当前版本不直接支持暂停，但可以通过游戏暂停功能间接实现。未来版本会提供对话状态管理器。

### Q: 如何实现对话历史记录？
A: 框架不内置历史记录功能。建议在游戏层实现：

```csharp
public class DialogHistory
{
    public List<ConversationLineInfo> History { get; } = new();
    
    public void Record(ConversationLineInfo line)
    {
        History.Add(line);
        // 限制历史记录数量
        if (History.Count > 100)
            History.RemoveAt(0);
    }
}
```

### Q: 超时机制如何工作？
A: 框架使用 `Game.Delay` 计时系统配合 `CancellationToken` 实现。UI 实现需要正确处理取消令牌：

```csharp
public async Task<int> ShowChoicesAsync(..., CancellationToken cancellationToken)
{
    var tcs = new TaskCompletionSource<int>();
    using var registration = cancellationToken.Register(() => 
        tcs.TrySetCanceled(cancellationToken));
    // ... 等待玩家选择 ...
    return await tcs.Task;  // 超时时抛出 OperationCanceledException
}
```

### Q: 如何防止客户端作弊？
A: 对话系统提供了内置的服务器端保护机制：

#### 方式 1：使用 ServerEffect（最安全）
```csharp
var choiceItem = new GameDataConversationChoiceItem
{
    Text = new LocalizedString("领取奖励"),
    ServerEffect = rewardEffect.ToLink(),  // 服务器端执行，客户端无法伪造
    Validators = ctx => HasCompletedQuest(ctx)
};
```
**优点：**
- 效果在服务器端执行，客户端无法篡改
- 自动处理验证和执行
- 最安全的方式

#### 方式 2：服务器端事件验证
```csharp
#if SERVER
Game.Instance.Subscribe<EventPlayerConversationChoiceSelected>(e =>
{
    // 服务器端验证
    if (!IsValidChoice(e.Player, e.ChoiceLink))
    {
        Game.Logger.LogWarning("玩家 {0} 尝试非法选择", e.Player.DisplayName);
        // 处理作弊行为
        // KickPlayer(e.Player);
        return;
    }
    
    // 验证通过后执行奖励
    GiveReward(e.Player);
});
#endif
```
**优点：**
- 灵活的验证逻辑
- 可以记录作弊行为
- 适合复杂场景

### Q: 对话系统性能如何？
A: 对话系统是轻量级的：
- 数据层是简单的 POCO 对象
- 运行时层使用异步模式，不阻塞主线程
- UI 层由游戏自己实现，可自由优化
- 建议：避免在一个对话中创建过多节点（>100）

### Q: 可以动态创建对话吗？
A: 完全可以。所有 GameData 类都可以在运行时创建：

```csharp
var dynamicLine = new GameDataConversationLine
{
    Character = npc.ToLink(),
    Text = new LocalizedString($"你好，{playerName}！"),
    WaitForConfirmation = true
};

await dynamicLine.StartConversation();
```

### Q: 如何实现复杂的对话树？
A: 使用选择项的 `NextConversation` 链接不同分支：

```
Opening → ChoiceGroup → Choice1 → Branch1Dialog
                     → Choice2 → Branch2Dialog
                     → Choice3 → Branch3Dialog
```

建议使用图形化工具（如 Articy Draft）设计复杂对话树，然后导入为 GameData。

---

## 📚 相关文档

- [触发器系统](TriggerSystem.md) - 可用于触发对话
- [事件系统](MessagingSystem.md) - 监听对话事件
- [本地化指南](../guides/Localization.md) - 多语言支持
- [UI 设计标准](../guides/UIDesignStandards.md) - UI 实现指南

## 🔗 示例项目

完整的示例项目请参考：
- `Examples/ConversationSystemExample/` - 基础对话示例
- `Tests/ConversationSystemTests/` - 单元测试

---

**更新日期**: 2025年10月14日  
**版本**: v1.0  
**维护者**: WasiCore Team


