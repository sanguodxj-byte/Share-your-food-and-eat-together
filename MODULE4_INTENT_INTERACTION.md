# Module 4: Intent & Interaction Handlers - 实现文档

## ?? 概述

**Module 4** 实现了 AI 意图处理器和原版互动工作器，统一了 AI 和玩家触发的共餐行为逻辑。

---

## ?? 核心组件

### 4.1 AI Intent Handler ?

**文件:** `AIIntentHandler.cs`  
**功能:** 处理来自 AI 系统的"分享食物"意图

#### 核心方法

##### 1. `HandleAIIntent(string intentName, Pawn initiator, Pawn recipient, Thing food = null)`

**功能：** AI 意图处理的主入口

**特性：**
- ? 在主线程执行（使用 `LongEventHandler`）
- ? 支持 "share_food" 意图
- ? 自动查找食物（如果未提供）
- ? 完整的错误处理
- ? 视觉反馈（拒绝气泡由工具类处理）

**参数：**
```csharp
string intentName  // 意图名称（例如："share_food"）
Pawn initiator     // 发起者
Pawn recipient     // 接收者
Thing food         // 食物（可选，留空则自动查找）
```

**返回值：**
```csharp
bool - 是否成功处理意图
```

**使用示例：**
```csharp
// 处理 share_food 意图
bool success = AIIntentHandler.HandleAIIntent(
    "share_food",
    alice,
    bob,
    meal  // 可选
);

if (success)
{
    Log.Message("AI 意图执行成功！");
}
```

##### 2. `FindFoodForSharing(Pawn pawn)`

**功能：** 自动为发起者查找可分享的食物

**搜索顺序：**
```
1. 手持物品 (carryTracker.CarriedThing)
2. 背包物品 (inventory.innerContainer)
3. 附近食物 (15 格内可达)
```

##### 3. `HandleBatchIntents(...)`

**功能：** 批量处理多个 AI 意图（用于特殊事件）

**使用示例：**
```csharp
var intents = new List<(Pawn, Pawn, Thing)>
{
    (alice, bob, meal1),
    (carol, dave, meal2),
    (eve, frank, meal3)
};

int successCount = AIIntentHandler.HandleBatchIntents("share_food", intents);
// 输出：批量处理 AI 意图: 2/3 成功
```

---

### 4.2 Vanilla Interaction Worker ?

**文件:** `InteractionWorker_OfferFood.cs`  
**功能:** 实现原版社交互动系统中的"提供食物"互动

#### 核心方法

##### 1. `RandomSelectionWeight(Pawn initiator, Pawn recipient)`

**功能：** 计算互动的随机选择权重

**权重计算公式：**
```
基础权重 = 0.5

IF 接收者不饥饿 (>= 50%):
    权重 = 0 (不触发)

IF 发起者无食物:
    权重 = 0 (不触发)

IF 满足条件:
    饥饿因子 = 1 - 饥饿度百分比 (0-1)
    权重 = 0.5 + (饥饿因子 * 0.5)  // 范围: 0.5 - 1.0
    
    IF 好感度 > 20:
        权重 += 0.2
    ELSE IF 好感度 < -20:
        权重 *= 0.5
```

**权重范围：**
| 条件 | 权重 | 触发概率 |
|------|------|---------|
| 不符合条件 | 0.0 | 0% |
| 基础条件 | 0.5 - 1.0 | 中等 |
| 高好感 | 0.7 - 1.2 | 高 |
| 敌对 | 0.25 - 0.5 | 低 |

##### 2. `Interacted(Pawn initiator, Pawn recipient, ...)`

**功能：** 执行互动

**执行流程：**
```
1. 查找食物 (FindFoodForSharing)
2. 生成上下文描述 (ContextBaitGenerator)
3. 调用统一触发逻辑 (FoodSharingUtility.TryTriggerShareFood)
4. 记录互动历史 (TaleRecorder)
5. 应用心情效果
```

**特性：**
- ? 统一概率检查（与 AI 意图相同）
- ? 视觉反馈（拒绝气泡）
- ? 不抛出错误
- ? 自动记录互动

---

## ?? InteractionDef 定义

**文件:** `Defs/InteractionDefs/Interaction_OfferFood.xml`

### OfferFood 互动

```xml
<InteractionDef>
  <defName>OfferFood</defName>
  <label>提供食物</label>
  <workerClass>RimTalkSocialDining.InteractionWorker_OfferFood</workerClass>
  <socialFightBaseChance>0.0</socialFightBaseChance>
  <initiatorThought>OfferedFood</initiatorThought>
  <recipientThought>ReceivedFoodOffer</recipientThought>
  <initiatorXpGainSkill>Social</initiatorXpGainSkill>
  <initiatorXpGainAmount>15</initiatorXpGainAmount>
</InteractionDef>
```

**特性：**
- ?? 不会引发社交冲突
- ?? 社交技能经验 +15
- ?? 发起者心情 +2（帮助了他人）
- ?? 接收者心情 +3（有人关心我）

### 相关 ThoughtDef

#### 1. OfferedFood（发起者）
```xml
<ThoughtDef>
  <defName>OfferedFood</defName>
  <durationDays>0.25</durationDays>
  <baseMoodEffect>2</baseMoodEffect>
  <label>帮助了他人</label>
  <description>我分享了食物，感觉很有帮助。</description>
</ThoughtDef>
```

#### 2. ReceivedFoodOffer（接收者）
```xml
<ThoughtDef>
  <defName>ReceivedFoodOffer</defName>
  <durationDays>0.5</durationDays>
  <baseMoodEffect>3</baseMoodEffect>
  <label>有人关心我</label>
  <description>有人主动分享食物，让我感到温暖。</description>
</ThoughtDef>
```

---

## ?? 统一行为逻辑

### AI 意图 vs 原版互动

两种触发方式共享相同的核心逻辑：

```
┌─────────────────────────────────────┐
│ AI Intent Handler                   │
│ - HandleAIIntent("share_food", ...) │
└──────────────┬──────────────────────┘
               │
               ↓
┌─────────────────────────────────────┐
│ Vanilla Interaction Worker          │
│ - InteractionWorker_OfferFood       │
└──────────────┬──────────────────────┘
               │
               ↓ 统一调用
┌─────────────────────────────────────┐
│ FoodSharingUtility                  │
│ - TryTriggerShareFood(...)          │
│   ├─ IsSafeToDisturb                │
│   ├─ TryRollForAcceptance           │
│   ├─ DropFood                       │
│   ├─ MultiReserve Check             │
│   ├─ FindTable/PicnicSpot           │
│   └─ CreateJobs                     │
└─────────────────────────────────────┘
```

**好处：**
- ? 逻辑一致性
- ? 易于维护
- ? 统一的概率检查
- ? 统一的视觉反馈

---

## ?? 使用场景

### 场景 1: AI 自动触发

```csharp
// 在 JobGiver 或 ThinkNode 中调用
protected override Job TryGiveJob(Pawn pawn)
{
    Pawn partner = FindHungryPartner(pawn);
    
    if (partner != null)
    {
        // 使用 AI 意图处理器
        bool success = AIIntentHandler.HandleAIIntent(
            "share_food",
            pawn,
            partner
        );
        
        if (success)
        {
            return null; // 任务已创建
        }
    }
    
    return null;
}
```

### 场景 2: 原版社交互动

```csharp
// RimWorld 会自动调用
// 当两个 Pawn 进行社交时，如果满足条件：
// 1. RandomSelectionWeight > 0
// 2. 随机选择到该互动
// 则自动触发 Interacted 方法
```

### 场景 3: 特殊事件批量触发

```csharp
// 节日事件：所有人一起吃饭
public void OnFestivalMeal()
{
    List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned.ToList();
    var intents = new List<(Pawn, Pawn, Thing)>();
    
    for (int i = 0; i < colonists.Count - 1; i += 2)
    {
        Thing meal = FindMealForPair(colonists[i], colonists[i + 1]);
        if (meal != null)
        {
            intents.Add((colonists[i], colonists[i + 1], meal));
        }
    }
    
    int success = AIIntentHandler.HandleBatchIntents("share_food", intents);
    Messages.Message($"节日共餐：{success} 对殖民者开始用餐", MessageTypeDefOf.PositiveEvent);
}
```

---

## ?? 测试场景

### 测试 1: AI 意图处理

```csharp
[Test]
public void TestAIIntent_ShareFood_Success()
{
    Pawn alice = CreatePawnWithFood();
    Pawn bob = CreateHungryPawn(0.25f);
    Thing meal = alice.carryTracker.CarriedThing;
    
    bool result = AIIntentHandler.HandleAIIntent("share_food", alice, bob, meal);
    
    Assert.IsTrue(result);
    // 验证任务已创建
    Assert.IsNotNull(alice.CurJob);
    Assert.AreEqual(SocialDiningDefOf.SocialDine, alice.CurJob.def);
}
```

### 测试 2: 互动权重计算

```csharp
[Test]
public void TestInteractionWeight_Hungry_HighWeight()
{
    Pawn alice = CreatePawnWithFood();
    Pawn bob = CreateHungryPawn(0.20f); // 80% 饥饿
    
    var worker = new InteractionWorker_OfferFood();
    float weight = worker.RandomSelectionWeight(alice, bob);
    
    Assert.Greater(weight, 0.8f); // 应该很高
}

[Test]
public void TestInteractionWeight_NotHungry_ZeroWeight()
{
    Pawn alice = CreatePawnWithFood();
    Pawn bob = CreateHungryPawn(0.85f); // 不饥饿
    
    var worker = new InteractionWorker_OfferFood();
    float weight = worker.RandomSelectionWeight(alice, bob);
    
    Assert.AreEqual(0f, weight); // 应该为 0
}
```

### 测试 3: 批量意图处理

```csharp
[Test]
public void TestBatchIntents_MultipleSuccess()
{
    var intents = new List<(Pawn, Pawn, Thing)>
    {
        (CreatePawnWithFood(), CreateHungryPawn(0.2f), CreateMeal()),
        (CreatePawnWithFood(), CreateHungryPawn(0.3f), CreateMeal())
    };
    
    int success = AIIntentHandler.HandleBatchIntents("share_food", intents);
    
    Assert.AreEqual(2, success);
}
```

---

## ?? 性能分析

### AI Intent Handler

**时间复杂度：**
- 食物查找：O(n) - n = 附近物品数
- 意图处理：O(1)
- 批量处理：O(m) - m = 意图数量

**空间复杂度：** O(1)

### Interaction Worker

**时间复杂度：**
- 权重计算：O(1)
- 互动执行：O(n) - n 取决于食物搜索

**空间复杂度：** O(1)

---

## ?? 调试技巧

### 启用详细日志

```csharp
// 在 HandleAIIntent 中
Log.Message($"[RimTalkSocialDining] AI 意图触发: {context}");
Log.Message($"[RimTalkSocialDining] AI 意图执行{'成功' : '失败'}");

// 在 Interacted 中
Log.Message($"[RimTalkSocialDining] 原版互动触发: {context}");
Log.Message($"[RimTalkSocialDining] 原版互动被拒绝: {initiator} → {recipient}");
```

### Dev Mode 测试

```csharp
// 在控制台执行
Pawn p1 = Find.Selector.SingleSelectedThing as Pawn;
Pawn p2 = GetTargetPawn();
AIIntentHandler.HandleAIIntent("share_food", p1, p2);
```

---

## ?? 配置参数

### AIIntentHandler

```csharp
// 附近食物搜索半径
float nearbySearchRadius = 15f;
```

### InteractionWorker_OfferFood

```csharp
// 饥饿阈值
private const float HungerThreshold = 0.5f; // 50%

// 基础权重
float baseWeight = 0.5f;

// 好感加成
float opinionBonus = 0.2f;

// 敌对惩罚
float hostilePenalty = 0.5f; // 乘法
```

---

## ?? 技术亮点

### 1. 主线程执行保证 ?

```csharp
LongEventHandler.ExecuteWhenFinished(delegate
{
    result = HandleAIIntentInternal(...);
});
```

### 2. 统一行为逻辑 ?

```csharp
// AI 和原版都调用相同的触发方法
FoodSharingUtility.TryTriggerShareFood(initiator, recipient, food);
```

### 3. 无错误抛出 ?

```csharp
// 失败时只记录日志，不抛出异常
if (!success)
{
    Log.Message("执行失败");
    // 视觉反馈已由工具类处理
}
```

### 4. 权重动态计算 ?

```csharp
// 根据饥饿度、好感度动态调整互动概率
float weight = baseWeight + hungerFactor * 0.5f + opinionBonus;
```

---

## ?? 与其他模块集成

### Module 2: FoodSharingUtility

```csharp
// Module 4 依赖 Module 2 的核心逻辑
FoodSharingUtility.TryTriggerShareFood(...)
FoodSharingUtility.IsSafeToDisturb(...)
```

### Module 3: ContextBaitGenerator

```csharp
// 使用上下文生成器记录日志
string context = ContextBaitGenerator.GetFoodContextDescription(initiator, recipient);
Log.Message($"触发: {context}");
```

### JobDriver & JobGiver

```csharp
// AI 意图可以在任何地方调用
// 原版互动由 RimWorld 自动调用
```

---

## ?? 未来扩展

### 短期
- [ ] 添加更多意图类型（例如："request_food"）
- [ ] 实现互动对话文本
- [ ] 添加互动动画

### 中期
- [ ] 支持群体互动（3+ 人）
- [ ] 根据特性调整互动权重
- [ ] 添加互动历史追踪

### 长期
- [ ] AI 学习的互动偏好
- [ ] 动态意图生成
- [ ] 跨 Mod 互动集成

---

## ?? 总结

**Module 4: Intent & Interaction Handlers** 成功实现了：

- ? AI 意图处理器（主线程执行）
- ? 原版互动工作器（权重动态计算）
- ? 统一的行为逻辑
- ? 完整的视觉反馈
- ? 无错误抛出设计
- ? InteractionDef 和 ThoughtDef 定义
- ? 批量处理支持

这个模块完美统一了 AI 和玩家触发的共餐行为，确保两种方式使用相同的概率检查和触发逻辑！

---

**编译状态:** ? 成功  
**文件:** `AIIntentHandler.cs`, `InteractionWorker_OfferFood.cs`  
**定义:** `Interaction_OfferFood.xml`
