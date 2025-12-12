# Module 3: Context Bait Generator - 实现文档

## ?? 概述

**ContextBaitGenerator** 是一个上下文感知模块，用于生成标准化的情境描述字符串，帮助 AI 系统识别和触发共餐机会。

---

## ?? 核心功能

### 主方法：`GetFoodContextDescription(Pawn initiator, Pawn recipient)`

**功能：** 生成包含关键词的上下文描述，用于 AI 触发共餐逻辑

#### ?? 判定逻辑

```
┌─────────────────────────────────────┐
│ 检查接收者是否饥饿 (< 30%)           │
└──────────────┬──────────────────────┘
               │ NO → 返回空字符串
               ↓ YES
┌─────────────────────────────────────┐
│ 检查发起者是否拥有食物               │
│ - 手持物品                          │
│ - 背包中的食物                      │
│ - 附近可达的食物 (10格内)           │
└──────────────┬──────────────────────┘
               │ NO → 返回空字符串
               ↓ YES
┌─────────────────────────────────────┐
│ 生成上下文描述                      │
│ 包含关键词：饥饿、食物、分享         │
└─────────────────────────────────────┘
```

#### ?? 返回值格式

**满足条件时：**
```
"[环境状态] 目标 {Name} 处于**饥饿**状态。发起者拥有**食物**，并且可以进行**分享**。"
```

**示例：**
```
"[环境状态] 目标 Alice 处于**饥饿**状态。发起者拥有**食物**，并且可以进行**分享**。"
```

**不满足条件时：**
```
string.Empty
```

---

## ?? 技术实现细节

### 1. 饥饿检查

```csharp
private const float HungerThreshold = 0.3f; // 30% 阈值

// 检查逻辑
float hungerLevel = recipient.needs.food.CurLevelPercentage;
bool isHungry = hungerLevel < HungerThreshold;
```

**饥饿状态对照表：**
| 饱食度 | 状态 | 是否触发 |
|--------|------|---------|
| < 10% | 极度饥饿 | ? 是 |
| 10-30% | 饥饿 | ? 是 |
| 30-50% | 有点饿 | ? 否 |
| 50-80% | 正常 | ? 否 |
| > 80% | 饱食 | ? 否 |

### 2. 食物检查（三层检查）

#### Layer 1: 手持物品
```csharp
if (initiator.carryTracker?.CarriedThing != null)
{
    Thing carried = initiator.carryTracker.CarriedThing;
    if (carried.def.IsIngestible && 
        carried.def.ingestible.preferability != FoodPreferability.Undefined)
    {
        return true; // 发起者手持食物
    }
}
```

#### Layer 2: 背包物品
```csharp
if (initiator.inventory?.innerContainer != null)
{
    foreach (Thing thing in initiator.inventory.innerContainer)
    {
        if (thing.def.IsIngestible && 
            thing.def.ingestible.preferability != FoodPreferability.Undefined)
        {
            return true; // 背包中有食物
        }
    }
}
```

#### Layer 3: 附近食物
```csharp
Thing nearbyFood = GenClosest.ClosestThingReachable(
    initiator.Position,
    initiator.Map,
    ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree),
    PathEndMode.ClosestTouch,
    TraverseParms.For(initiator, Danger.Deadly, TraverseMode.ByPawn, false),
    10f, // 10格搜索半径
    (Thing t) => t.def.IsIngestible && 
                 !t.IsForbidden(initiator) && 
                 ReservationUtility.CanReserve(initiator, t)
);
```

**搜索条件：**
- ? 10 格内可达
- ? 可食用
- ? 未被禁止
- ? 可以预留

---

## ?? 使用场景

### 场景 1: AI 自动检测
```csharp
public class JobGiver_SocialDine : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        // 遍历附近的饥饿殖民者
        foreach (Pawn colonist in nearbyColonists)
        {
            // 生成上下文描述
            string context = ContextBaitGenerator.GetFoodContextDescription(pawn, colonist);
            
            if (!string.IsNullOrEmpty(context))
            {
                // 上下文满足条件，尝试触发共餐
                Log.Message(context); // 输出上下文用于调试
                
                if (FoodSharingUtility.TryTriggerShareFood(pawn, colonist, food))
                {
                    return null; // 成功触发
                }
            }
        }
        
        return null;
    }
}
```

### 场景 2: UI 显示
```csharp
// 在 Gizmo 或 FloatMenu 中显示上下文信息
FloatMenuOption option = new FloatMenuOption(
    "邀请共餐",
    delegate {
        string context = ContextBaitGenerator.GetFoodContextDescription(pawn, target);
        if (!string.IsNullOrEmpty(context))
        {
            Messages.Message(context, MessageTypeDefOf.NeutralEvent);
            FoodSharingUtility.TryTriggerShareFood(pawn, target, food);
        }
    },
    MenuOptionPriority.Default
);
```

### 场景 3: 批量检测
```csharp
// 获取所有符合条件的潜在接收者的上下文描述
List<Pawn> allColonists = map.mapPawns.FreeColonistsSpawned;
string batchContext = ContextBaitGenerator.GetBatchContextDescription(initiator, allColonists);

if (!string.IsNullOrEmpty(batchContext))
{
    Log.Message($"共餐机会检测:\n{batchContext}");
    // 输出示例：
    // [环境状态] 目标 Alice 处于**饥饿**状态。发起者拥有**食物**，并且可以进行**分享**。
    // [环境状态] 目标 Bob 处于**饥饿**状态。发起者拥有**食物**，并且可以进行**分享**。
}
```

---

## ?? 扩展方法

### `GetBatchContextDescription()`
**功能：** 批量检查多个潜在接收者

**使用示例：**
```csharp
IEnumerable<Pawn> hungryColonists = map.mapPawns.FreeColonistsSpawned
    .Where(p => p.needs?.food?.CurLevelPercentage < 0.3f);

string contexts = ContextBaitGenerator.GetBatchContextDescription(initiator, hungryColonists);
```

### `GetDetailedHungerStatus()`
**功能：** 获取详细的饥饿状态描述（用于调试或 UI）

**返回值对照：**
| 饱食度 | 返回文本 |
|--------|---------|
| < 10% | "极度饥饿" |
| 10-30% | "饥饿" |
| 30-50% | "有点饿" |
| 50-80% | "正常" |
| > 80% | "饱食" |

**使用示例：**
```csharp
string status = ContextBaitGenerator.GetDetailedHungerStatus(pawn);
Log.Message($"{pawn.LabelShort} 的饥饿状态: {status}");
// 输出："Alice 的饥饿状态: 饥饿"
```

---

## ?? 测试用例

### 测试 1: 基本功能
```csharp
[Test]
public void TestContextGeneration_HungryWithFood_ReturnsContext()
{
    Pawn alice = CreateTestPawn("Alice");
    Pawn bob = CreateTestPawn("Bob");
    
    // 设置 Bob 饥饿
    bob.needs.food.CurLevel = 0.2f; // 20% 饥饿
    
    // 给 Alice 食物
    Thing meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
    alice.carryTracker.TryStartCarry(meal);
    
    // 测试
    string context = ContextBaitGenerator.GetFoodContextDescription(alice, bob);
    
    Assert.IsNotEmpty(context);
    Assert.IsTrue(context.Contains("饥饿"));
    Assert.IsTrue(context.Contains("食物"));
    Assert.IsTrue(context.Contains("分享"));
}
```

### 测试 2: 不满足条件
```csharp
[Test]
public void TestContextGeneration_NotHungry_ReturnsEmpty()
{
    Pawn alice = CreateTestPawn("Alice");
    Pawn bob = CreateTestPawn("Bob");
    
    // Bob 不饥饿
    bob.needs.food.CurLevel = 0.9f; // 90% 饱食
    
    // Alice 有食物
    Thing meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
    alice.carryTracker.TryStartCarry(meal);
    
    // 测试
    string context = ContextBaitGenerator.GetFoodContextDescription(alice, bob);
    
    Assert.IsEmpty(context); // 应该返回空字符串
}
```

### 测试 3: 附近食物检测
```csharp
[Test]
public void TestContextGeneration_NearbyFood_ReturnsContext()
{
    Pawn alice = CreateTestPawn("Alice");
    Pawn bob = CreateTestPawn("Bob");
    Map map = GetTestMap();
    
    // Bob 饥饿
    bob.needs.food.CurLevel = 0.25f;
    
    // 在 Alice 附近放置食物
    Thing meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
    GenPlace.TryPlaceThing(meal, alice.Position.RandomAdjacentCell8Way(), map, ThingPlaceMode.Near);
    
    // 测试
    string context = ContextBaitGenerator.GetFoodContextDescription(alice, bob);
    
    Assert.IsNotEmpty(context);
}
```

---

## ?? 关键词提取

生成的上下文描述包含以下**关键词**：

1. **饥饿** - 标识接收者状态
2. **食物** - 标识发起者拥有资源
3. **分享** - 标识可执行的动作

这些关键词可用于：
- AI 模式识别
- 自然语言处理
- 事件触发逻辑
- 上下文匹配算法

---

## ?? 配置参数

### 可调整的常量

```csharp
// 饥饿阈值 (默认 30%)
private const float HungerThreshold = 0.3f;

// 附近食物搜索半径 (默认 10 格)
float nearbySearchRadius = 10f;
```

### 调整建议

**更严格的触发条件（推荐用于平衡性）：**
```csharp
private const float HungerThreshold = 0.2f; // 20% - 更饿才触发
```

**更宽松的触发条件（推荐用于测试）：**
```csharp
private const float HungerThreshold = 0.5f; // 50% - 更容易触发
```

---

## ?? 调试技巧

### 启用上下文日志
```csharp
string context = ContextBaitGenerator.GetFoodContextDescription(initiator, recipient);
if (!string.IsNullOrEmpty(context))
{
    Log.Message($"[ContextBait] {context}");
}
else
{
    Log.Message($"[ContextBait] 不满足条件 - " +
                $"饥饿度:{recipient.needs.food.CurLevelPercentage:P}, " +
                $"有食物:{ContextBaitGenerator.InitiatorHasFood(initiator)}");
}
```

### Dev Mode 控制台测试
```csharp
// 在 Dev Mode 控制台执行
Pawn pawn1 = Find.Selector.SingleSelectedThing as Pawn;
Pawn pawn2 = GetSecondPawn();
string result = ContextBaitGenerator.GetFoodContextDescription(pawn1, pawn2);
Log.Message(result);
```

---

## ?? 与其他模块集成

### 与 FoodSharingUtility 集成
```csharp
// 先检查上下文，再触发共餐
string context = ContextBaitGenerator.GetFoodContextDescription(initiator, recipient);

if (!string.IsNullOrEmpty(context))
{
    // 上下文满足，进行共餐触发
    if (FoodSharingUtility.TryTriggerShareFood(initiator, recipient, food))
    {
        Messages.Message(context, MessageTypeDefOf.PositiveEvent);
    }
}
```

### 与 ThinkNode 集成
```csharp
public class ThinkNode_ConditionalCanSocialDine : ThinkNode_Conditional
{
    protected override bool Satisfied(Pawn pawn)
    {
        // 使用上下文生成器检查是否有共餐机会
        foreach (Pawn colonist in pawn.Map.mapPawns.FreeColonistsSpawned)
        {
            string context = ContextBaitGenerator.GetFoodContextDescription(pawn, colonist);
            if (!string.IsNullOrEmpty(context))
            {
                return true; // 有机会
            }
        }
        
        return false;
    }
}
```

---

## ?? 性能优化

### 1. 早期退出
```csharp
// 饥饿检查在最前面，快速排除不符合条件的情况
if (!isHungry)
    return string.Empty; // 立即返回
```

### 2. 分层检查
```csharp
// 按照可能性从高到低检查
// 1. 手持物品（最可能）
// 2. 背包物品（较可能）
// 3. 附近食物（最不可能，且开销最大）
```

### 3. 有限搜索范围
```csharp
// 附近食物搜索限制在 10 格内
// 避免全图搜索带来的性能问题
10f, // 搜索半径
```

---

## ?? 注意事项

1. **线程安全：** 所有方法应在主线程调用
2. **空值检查：** 所有公共方法都包含空值保护
3. **性能考虑：** 附近食物检查有距离限制
4. **关键词一致性：** 生成的文本格式应保持一致

---

## ?? 总结

**ContextBaitGenerator** 成功实现了：

- ? 严格的条件判定（饥饿 < 30% + 有食物）
- ? 三层食物检查（手持 → 背包 → 附近）
- ? 标准化上下文描述格式
- ? 关键词嵌入（饥饿、食物、分享）
- ? 批量检测支持
- ? 详细状态查询
- ? 性能优化（早期退出、有限搜索）

这个模块为 AI 系统提供了清晰的上下文信息，使得共餐触发逻辑更加智能和可控！

---

## ?? 版本信息

- **模块名称:** ContextBaitGenerator (Module 3)
- **实现日期:** 2025/12/7
- **文件:** `Source/RimTalkSocialDining/ContextBaitGenerator.cs`
- **行数:** 150+ 行
- **编译状态:** ? 成功
