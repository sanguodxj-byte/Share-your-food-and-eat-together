# ? RimTalk True Social Dining - 编译成功！

## ?? 项目状态

**编译状态:** ? 成功  
**输出文件:** `Assemblies/RimTalkSocialDining.dll`  
**目标框架:** .NET Framework 4.7.2  
**RimWorld 版本:** 1.4/1.5

---

## ?? 已实现的功能特性

### ? 核心功能
1. **同步共餐系统** - 两个 Pawn 可以共享同一个食物实例
2. **SharedFoodTracker 组件** - 线程安全的食物追踪和引用计数
3. **幸存者逻辑** - 只有最后一个用餐者完成后才销毁食物
4. **野餐模式** - 找不到餐桌时在地面用餐
5. **存档兼容** - 完整的 `ExposeData` 实现，确保保存/加载安全

### ? AI 集成
- `JobGiver_SocialDine` - 智能任务生成器
- `ThinkNode_ConditionalCanSocialDine` - 条件检查节点
- 与原版 AI 系统无缝集成

### ? 游戏机制
- **心情加成** - 与他人共餐获得 +3 心情（0.5 天持续）
- **伙伴匹配算法** - 基于距离和饥饿程度选择最佳用餐伙伴
- **多语言支持** - 中文和英文完整翻译

---

## ?? 安装和使用

### 安装方法：
1. 将整个 `Share your food and eat together` 文件夹复制到 RimWorld 的 Mods 目录
   - Windows: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
2. 启动 RimWorld
3. 在 Mod 管理器中启用 "RimTalk True Social Dining"
4. 重启游戏

### 使用方法：
Mod 启用后会自动工作：
- 当殖民者饥饿时，系统会自动检测附近的其他饥饿殖民者
- 如果找到合适的伙伴，他们会自动安排共餐
- 两人会走到相同位置（餐桌或地面）
- 面对面同时食用同一个食物
- 完成后获得社交心情加成

---

## ?? 文件结构

```
Share your food and eat together/
├── About/
│   └── About.xml                                  # Mod 元数据
├── Assemblies/
│   └── RimTalkSocialDining.dll                   # ? 编译输出
├── Defs/
│   ├── JobDefs/
│   │   └── Jobs_SocialDining.xml                 # 任务定义
│   ├── ThinkTreeDefs/
│   │   └── ThinkTree_SocialDining.xml            # AI 思考树
│   └── ThoughtDefs/
│       └── Thoughts_SocialDining.xml             # 心情 Def
├── Patches/
│   └── ThinkTree_SocialDining_Patch.xml          # XML 补丁
├── Languages/
│   ├── ChineseSimplified/Keyed/
│   │   └── SocialDining_Keys.xml                 # 中文翻译
│   └── English/Keyed/
│       └── SocialDining_Keys.xml                 # 英文翻译
└── Source/
    └── RimTalkSocialDining/
        ├── SharedFoodTracker.cs                  # 食物追踪组件
        ├── JobDriver_SocialDine.cs               # 任务驱动器
        ├── JobGiver_SocialDine.cs                # 任务生成器
        ├── ThinkNode_ConditionalCanSocialDine.cs # AI 条件检查
        ├── HarmonyPatches.cs                     # Harmony 补丁
        ├── SocialDiningDefOf.cs                  # Def 引用类
        └── RimTalkSocialDining.csproj            # 项目文件
```

---

## ?? 技术实现细节

### 1. SharedFoodTracker 组件
```csharp
- 线程安全的 HashSet<Pawn> 管理用餐者
- 引用计数机制
- 幸存者逻辑：isLastEater 检查
- 完整的 ExposeData 序列化支持
```

### 2. JobDriver_SocialDine
```csharp
- 使用 Reserve() 预留食物和餐桌
- 7 个 Toil 序列：
  1. 前往食物
  2. 拾取食物
  3. 前往用餐地点
  4. 放置食物
  5. 注册到 Tracker
  6. 同步进食
  7. 清理和心情加成
- ExposeData 确保存档安全
```

### 3. JobGiver_SocialDine
```csharp
- 智能食物搜索（考虑营养、距离、可达性）
- 伙伴匹配算法（距离 + 饥饿程度评分）
- 餐桌查找（支持野餐fallback）
- 主线程执行保证
```

### 4. API 修复列表
已修复的 RimWorld API 兼容性问题：
- ? `Reserve()` 方法参数调整
- ? `FoodUtility.GetNutrition()` 签名匹配
- ? `RCellFinder.SpotToChewStandingNear()` 参数类型
- ? `ThingWithComps` vs `Thing` 类型转换
- ? 添加 `UnityEngine` using 用于 `Mathf`
- ? 创建自定义 `ThoughtDef` (AteWithColonist)

---

## ?? 配置参数

可在代码中调整的常量：
```csharp
// JobGiver_SocialDine.cs
MaxFoodSearchDistance = 50f;      // 食物搜索半径
MaxPartnerSearchDistance = 30f;   // 伙伴搜索半径
MaxTableSearchDistance = 40f;     // 餐桌搜索半径

// ThinkNode_ConditionalCanSocialDine.cs
MinHungerLevel = 0.25f;           // 触发饥饿阈值
```

---

## ?? 已知限制

1. **食物类型限制**
   - 仅支持 `ThingWithComps` 类型的食物
   - 不支持动物尸体或特殊食物源

2. **预留系统**
   - 当前实现不使用真正的 `maxClaimants=2`
   - 使用标准预留 + SharedFoodTracker 模拟多人共享

3. **AI 优先级**
   - 社交共餐优先级低于紧急任务
   - 可能被其他高优先级任务打断

---

## ?? 未来改进方向

- [ ] 支持 3+ 人群体共餐
- [ ] 添加更多社交互动动画
- [ ] 根据社交关系调整心情加成
- [ ] 支持宠物和主人共餐
- [ ] 添加特殊的共餐对话气泡

---

## ?? 许可证

本 Mod 为开源项目，欢迎修改和分发。

---

## ?? 致谢

- **RimWorld** by Ludeon Studios
- **Harmony** by pardeike
- 感谢 RimWorld modding 社区的支持

---

## ?? 支持

如遇到问题，请检查：
1. Mod 加载顺序（应在 Core 之后）
2. 游戏日志 (`Player.log`) 中的错误信息
3. 确保使用 RimWorld 1.4 或 1.5 版本

**祝您游戏愉快！** ??????
