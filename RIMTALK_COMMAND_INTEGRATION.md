# RimTalk 命令集成指南

## 问题：纯自然语言常识无法直接驱动系统命令

### ?? 核心问题

RimTalk 的常识库只能提供**描述性知识**，例如：

```
"当你饥饿且手里有食物时，应该主动邀请对方一起分享食物"
```

AI 读取这条常识后：
- ? **能理解**："我应该做什么"
- ? **无法执行**：具体的游戏命令调用

---

## 解决方案：三层架构

### **Layer 1: 常识库 - 告诉 AI 如何调用命令**

在常识库中添加可执行的命令格式（已实现）：

```
标签：社交,共餐,命令
内容：要邀请某人共餐，使用意图命令：share_food(发起者, 接收者, 食物)。
      例如：当Alice想和Bob共餐时，执行 share_food(Alice, Bob, meal)。
      这会触发游戏内的共餐系统。
重要性：1.0
```

### **Layer 2: 意图监听器 - 解析 AI 输出**

`RimTalkIntentListener.cs` 负责：
- 监听 AI 的输出文本
- 使用正则表达式提取命令：`share_food\(([^,]+),([^,]+)(?:,([^)]+))?\)`
- 解析参数（发起者、接收者、食物）
- 调用 `AIIntentHandler.HandleAIIntent()`

```csharp
// 示例：AI 输出
"好的，让我帮你！share_food(Alice, Bob, meal)"

// 解析结果
initiator = "Alice"
recipient = "Bob"
food = "meal"

// 执行命令
AIIntentHandler.HandleAIIntent("share_food", alicePawn, bobPawn, mealThing)
```

### **Layer 3: Harmony 补丁 - 挂载到 RimTalk**

`Patch_RimTalk_ProcessResponse` 补丁：
- 拦截 RimTalk 的 AI 响应处理流程
- 自动调用 `RimTalkIntentListener.TryParseAndExecute()`
- 无需手动集成

---

## ?? 完整工作流程

```
1. 玩家/AI：Alice 很饿，看到Bob也很饿
   ↓
2. RimTalk AI 读取常识库
   ├─ 读取常识#1："饥饿时应该邀请同伴"
   ├─ 读取常识#0："使用命令 share_food(发起者, 接收者, 食物)"
   └─ AI 决策："Alice应该邀请Bob共餐"
   ↓
3. AI 输出（包含命令）
   "Alice注意到Bob也很饿，她想和Bob一起吃饭。share_food(Alice, Bob, meal)"
   ↓
4. Harmony 补丁拦截输出
   Patch_RimTalk_ProcessResponse.Postfix(__result)
   ↓
5. 意图监听器解析命令
   RimTalkIntentListener.TryParseAndExecute(__result, alicePawn, bobPawn)
   ├─ 提取: initiator="Alice", recipient="Bob", food="meal"
   ├─ 解析Pawn: alicePawn, bobPawn
   └─ 查找食物: mealThing
   ↓
6. 执行意图
   AIIntentHandler.HandleAIIntent("share_food", alicePawn, bobPawn, mealThing)
   ↓
7. 触发共餐系统
   FoodSharingUtility.TryTriggerShareFood(alicePawn, bobPawn, mealThing)
   ↓
8. 游戏内执行
   ├─ alicePawn.jobs.TryTakeOrderedJob(SocialDineJob)
   ├─ bobPawn.jobs.TryTakeOrderedJob(SocialDineJob)
   └─ 双方开始共餐动画
```

---

## ?? 关键代码位置

| 文件 | 功能 |
|------|------|
| `KnowledgeBaseGenerator.cs` | 生成包含命令格式的常识 |
| `RimTalkIntentListener.cs` | 解析AI输出，提取命令 |
| `HarmonyPatches.cs` | 挂载到RimTalk响应流程 |
| `AIIntentHandler.cs` | 执行意图，调用游戏系统 |
| `FoodSharingUtility.cs` | 最终的共餐逻辑 |

---

## ?? 配置说明

### 在游戏中启用

1. **安装依赖**：
   - RimTalk-main (核心)
   - RimTalk-ExpandMemory (记忆扩展)
   - Share your food and eat together (本Mod)

2. **生成常识库**：
   - 游戏内：选项 → Mod设置 → Share your food and eat together
   - 点击："生成常识库条目并添加到 RimTalk"

3. **启用调试日志**（可选）：
   - 勾选 "启用调试日志"
   - 查看控制台输出，验证命令解析

---

## ?? 测试方法

### 方法 1: 手动测试AI输出

在 RimTalk 对话中，AI 应该输出类似这样的内容：

```
"Alice：嘿Bob，我有多余的食物，要不要一起吃？
 share_food(Alice, Bob, simple_meal)"
```

### 方法 2: 查看日志

启用调试日志后，应该看到：

```
[RimTalkSocialDining] 检测到 AI 输出中的共餐意图：share_food(Alice, Bob, meal)
[RimTalkSocialDining] 成功执行共餐意图：Alice -> Bob
[RimTalkSocialDining] AI 意图执行成功: Alice 和 Bob 开始用餐
```

### 方法 3: 观察游戏内行为

- Alice 和 Bob 应该走向同一个餐桌
- 双方同时对同一份食物进行"吃"动作
- 完成后获得 "+3 共同进餐" 心情加成

---

## ?? 常识库示例

生成的常识应该包括：

| 序号 | 标签 | 内容概要 |
|------|------|----------|
| 0 | 社交,共餐,命令 | 如何使用 share_food() 命令 |
| 1 | 社交,共餐,行为 | 饥饿时应该邀请同伴 |
| 2 | 社交,共餐,对象选择 | 优先邀请喜欢的人 |
| 3 | 社交,共餐,接受邀请 | 何时接受邀请 |
| 4 | 社交,共餐,拒绝邀请 | 何时拒绝邀请 |
| 5 | 社交,共餐,地点 | 优先餐桌，可野餐 |
| 6 | 社交,共餐,时机 | 最佳时机选择 |
| 7 | 社交,共餐,好处 | 增进关系，改善心情 |

---

## ?? 注意事项

### 当前限制

1. **需要RimTalk输出命令**
   - AI 必须在输出中包含 `share_food()` 格式
   - 依赖 AI 模型理解并输出命令

2. **参数解析简化**
   - Pawn 名字匹配可能不准确
   - 食物匹配只取第一个可用食物

3. **Harmony 补丁可能失效**
   - RimTalk 更新后可能需要调整补丁
   - 目标方法名可能变化

### 改进方向

1. **更智能的Pawn解析**
   - 支持昵称、代词（you/me/I）
   - 上下文感知（对话双方）

2. **更精确的食物匹配**
   - 根据名称匹配具体食物类型
   - 支持食物偏好过滤

3. **多种命令格式支持**
   - `share_food(Alice, Bob)`
   - `invite_to_dine(Alice, Bob)`
   - 自然语言解析（NLP）

---

## ?? 故障排查

### 问题 1: AI 不输出命令

**原因**：常识库未生效或 AI 未理解
**解决**：
- 确认已点击"生成常识库条目"按钮
- 在 RimTalk 设置中查看常识是否存在
- 提高命令常识的重要性（importance=1.0）

### 问题 2: 命令无法执行

**原因**：Harmony 补丁未生效
**解决**：
- 检查日志：`[RimTalkSocialDining] 找到 RimTalk 响应处理方法`
- 确认 RimTalk 已正确加载
- 重启游戏

### 问题 3: Pawn 解析失败

**原因**：名字不匹配
**解决**：
- 使用完整的 Pawn 名字
- 检查日志中的 Pawn 解析信息
- 手动指定 Pawn（通过原版互动模式）

---

## ?? 总结

**回答原问题**：

> "我没看到 command 相关参数，只靠自然语言就能驱动系统执行命令吗"

**答案**：不能。

- ? **纯自然语言**（描述性）→ AI 理解但无法执行
- ? **自然语言 + 命令格式**（`share_food(A,B)`）→ AI 输出 → 系统解析 → 执行

本实现通过**三层架构**实现了从自然语言到系统命令的转换：
1. 常识库教 AI "如何输出命令"
2. 监听器解析 AI 输出中的命令
3. 意图处理器执行游戏逻辑

这样既保留了自然语言的可读性，又实现了命令的可执行性。
