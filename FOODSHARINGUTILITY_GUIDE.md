# FoodSharingUtility - 社交共餐核心工具类

## ?? 概述

`FoodSharingUtility` 是 RimTalk Social Dining Mod 的"大脑"，包含所有智能决策逻辑。这是一个静态工具类，提供了从安全检查到共餐触发的完整功能链。

---

## ?? 核心功能模块

### 1. 安全检查 (`IsSafeToDisturb`)

**功能：** 检查 Pawn 是否处于可被打扰的状态

**返回 `false` 的情况：**
- ? Pawn 已死亡或倒地
- ? 处于征召（Drafted）状态
- ? 处于精神崩溃状态
- ? 正在灭火 (`BeatFire`, `ExtinguishSelf`)
- ? 正在进行手术 (`TendPatient`, `DoBill`)
- ? 正在护理伤员
- ? 执行不可打断的任务

```csharp
if (!FoodSharingUtility.IsSafeToDisturb(pawn))
{
    // 此时不应打扰该 Pawn
}
```

---

### 2. 餐桌查找 (`TryFindTableForTwo`)

**功能：** 找到适合两人用餐的最佳餐桌

**评分标准：**
- ? 餐桌必须有有效的交互单元格
- ? 两个 Pawn 都能到达
- ? 两个 Pawn 都能预留餐桌
- ? 选择距离两人总和最短的餐桌

**参数：**
```csharp
Building table = FoodSharingUtility.TryFindTableForTwo(
    map,              // 地图
    pawn1,            // 第一个 Pawn
    pawn2,            // 第二个 Pawn
    maxDistance: 40f  // 最大搜索距离（默认 40）
);
```

**返回值：** `Building` 或 `null`（如果没找到）

---

### 3. 野餐地点查找 (`TryFindStandingSpotNear`)

**功能：** 找到两个相邻的有效站立格子用于野餐模式

**搜索逻辑：**
- 从中心点开始，逐渐扩大搜索半径（1-5 格）
- 查找两个相邻的可站立格子
- 优先选择离中心点最近的位置

```csharp
if (FoodSharingUtility.TryFindStandingSpotNear(center, map, out IntVec3 spot1, out IntVec3 spot2))
{
    // 找到了两个野餐地点
}
```

---

### 4. 拒绝文本生成 (`GetRandomRefusalText`)

**功能：** 根据拒绝原因生成口语化的中文拒绝文本

**拒绝类型和对应文本：**

#### ?? `hostile` - 敌对关系
- "离我远点"
- "不想跟你吃"
- "别烦我"
- "我们不熟"
- "滚开"

#### ?? `full` - 吃太饱
- "还不饿呢"
- "吃不下了"
- "刚吃过"
- "不饿"
- "待会儿再说"

#### ?? `busy` - 忙碌中
- "我很忙"
- "没空"
- "正忙着呢"
- "等我忙完"

#### ?? `foodhate` - 不喜欢食物
- "我不吃这个"
- "这什么玩意"
- "换个别的吧"
- "不喜欢这个"

#### ? `generic` - 通用拒绝
- "下次吧"
- "没心情"
- "不想吃"
- "改天吧"
- "算了"
- "不了"

```csharp
string refusalText = FoodSharingUtility.GetRandomRefusalText(initiator, recipient, "hostile");
// 返回例如："离我远点"
```

---

### 5. 接受度计算 (`TryRollForAcceptance`)

**功能：** 计算并判定接收者是否接受共餐邀请

#### ?? 基础概率
- **基准接受率：** 40%

#### ? 正面修正因素

| 因素 | 条件 | 加成 |
|------|------|------|
| ??? **高饥饿度** | 饥饿度 > 50% | +40% |
| ?? **高好感度** | 好感度 ≥ 20 | +30% × (好感度/100) |
| ?? **社交技能** | 发起者社交等级 ≥ 8 | +15% × (技能/20) |
| ?? **善良特性** | 拥有 Kind 特性 | +15% |

#### ? 负面修正因素

| 因素 | 条件 | 惩罚 |
|------|------|------|
| ?? **敌对关系** | 好感度 < -20 | **直接拒绝** |
| ?? **粗鲁特性** | 拥有 Abrasive 特性 | -20% |
| ?? **禁欲特性** | 拥有 Ascetic 特性 | -10% |
| ?? **食物偏好** | 不吃该类食物 | **直接拒绝** |
| ?? **太饱了** | 饱食度 > 80% | **直接拒绝** |
| ?? **忙碌中** | 执行重要任务 | **直接拒绝** |

#### 使用示例：
```csharp
if (FoodSharingUtility.TryRollForAcceptance(initiator, recipient, food, out string refusalReason))
{
    // 接受了！
}
else
{
    // 拒绝了，refusalReason 包含原因
    string text = FoodSharingUtility.GetRandomRefusalText(initiator, recipient, refusalReason);
    // 显示拒绝气泡
}
```

---

### 6. 触发共餐 (`TryTriggerShareFood`)

**功能：** 完整的共餐触发流程 - 这是最主要的入口方法

#### ?? 执行流程

```
┌─────────────────────────────────────┐
│ Step 1: 安全检查                      │
│ - IsSafeToDisturb(initiator)       │
│ - IsSafeToDisturb(recipient)       │
└──────────────┬──────────────────────┘
               │
               ↓
┌─────────────────────────────────────┐
│ Step 2: 接受度判定                    │
│ - TryRollForAcceptance()           │
│ - 失败则显示拒绝气泡                   │
└──────────────┬──────────────────────┘
               │
               ↓
┌─────────────────────────────────────┐
│ Step 3: 掉落食物                      │
│ - 如果发起者持有食物，放到地上          │
└──────────────┬──────────────────────┘
               │
               ↓
┌─────────────────────────────────────┐
│ Step 4: 多人预留检查 (The Hack)       │
│ - CanReserve(food) for both        │
│ - 使用 SharedFoodTracker 追踪        │
└──────────────┬──────────────────────┘
               │
               ↓
┌─────────────────────────────────────┐
│ Step 5: 查找用餐地点                  │
│ - TryFindTableForTwo()             │
│ - 或野餐模式                          │
└──────────────┬──────────────────────┘
               │
               ↓
┌─────────────────────────────────────┐
│ Step 6: 创建并启动任务                 │
│ - JobMaker.MakeJob() × 2           │
│ - StartJob() for both pawns        │
└─────────────────────────────────────┘
```

#### 使用示例：
```csharp
Pawn alice = ...; // 发起者
Pawn bob = ...;   // 接收者
Thing meal = ...; // 食物

if (FoodSharingUtility.TryTriggerShareFood(alice, bob, meal))
{
    // 成功触发共餐！
    // 系统会自动显示消息："Alice 和 Bob 开始共餐"
}
else
{
    // 失败（可能被拒绝、无法预留等）
}
```

---

## ?? 实际应用场景

### 场景 1: AI 自动触发
在 `JobGiver_SocialDine` 中调用：
```csharp
protected override Job TryGiveJob(Pawn pawn)
{
    Thing food = FindBestFood(pawn);
    Pawn partner = FindBestDiningPartner(pawn, food);
    
    // 使用工具类触发
    if (FoodSharingUtility.TryTriggerShareFood(pawn, partner, food))
    {
        return null; // 任务已由工具类创建
    }
    
    return null;
}
```

### 场景 2: 玩家手动触发
创建一个 FloatMenu 选项：
```csharp
FloatMenuOption option = new FloatMenuOption(
    "邀请共餐",
    delegate
    {
        Thing food = pawn.carryTracker.CarriedThing;
        Pawn target = GetSelectedPawn();
        FoodSharingUtility.TryTriggerShareFood(pawn, target, food);
    }
);
```

---

## ?? 配置参数

可在代码中调整的常量：

```csharp
// 接受度基础概率
private const float BaseAcceptanceChance = 0.4f;         // 40%

// 修正系数
private const float HighHungerBonus = 0.4f;              // 高饥饿 +40%
private const float HighOpinionBonus = 0.3f;             // 高好感 +30%
private const float SocialSkillBonus = 0.15f;            // 社交技能 +15%
private const float AbrasiveTraitPenalty = 0.2f;         // 粗鲁特性 -20%
```

---

## ?? 调试技巧

### 启用详细日志
在每个关键方法中添加：
```csharp
Log.Message($"[FoodSharingUtility] IsSafeToDisturb({pawn.LabelShort}): {result}");
```

### 测试接受度计算
```csharp
// 在 Dev Mode 控制台执行
FoodSharingUtility.TryRollForAcceptance(initiator, recipient, food, out string reason);
Log.Message($"Result: {reason}");
```

---

## ?? 注意事项

1. **线程安全：** 所有方法都应在主线程调用
2. **空值检查：** 所有公共方法都包含空值保护
3. **性能优化：** 餐桌查找有距离限制，避免全地图搜索
4. **兼容性：** 使用 `WillEat` 确保食物偏好兼容

---

## ?? 未来扩展

- [ ] 支持 3+ 人群体共餐
- [ ] 添加更多特性的影响（例如：贪食者增加接受率）
- [ ] 根据时间（早中晚）调整接受率
- [ ] 添加"常一起吃饭"的记忆，提高接受率
- [ ] 支持不同食物类型的偏好加成

---

## ?? 总结

`FoodSharingUtility` 是整个社交共餐系统的核心，提供了：
- ? 全面的安全检查
- ? 智能的地点选择
- ? 真实的接受度模拟
- ? 生动的拒绝反馈
- ? 完整的触发流程

通过这个工具类，您可以轻松地在任何地方触发和管理社交共餐功能！
