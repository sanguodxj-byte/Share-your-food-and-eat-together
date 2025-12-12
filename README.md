# Share your food and eat together - 完整说明

## ??? 项目概述

这是 RimWorld mod 实现了真正的社交用餐系统，允许多个 Pawn 同时共享并食用**同一个**食物实例。

### 核心特性：
- ? **多人预订系统** - 使用 `ReservationManager` 的 `maxClaimants=2` 机制
- ? **线程安全** - 所有 AI 逻辑在主线程执行（`LongEventHandler`）
- ? **存档安全** - `JobDriver` 实现 `ExposeData` 防止数据丢失
- ? **自毁逻辑** - 只有最后一个用餐者负责销毁空食物
- ? **野餐模式** - 找不到餐桌时会在野地用餐

## ?? 编译配置

### 方法 1: 修改项目文件中的引用路径

编辑 `Source/RimTalkSocialDining/RimTalkSocialDining.csproj`，将以下路径修改为您的 RimWorld 实际安装路径：

```xml
<Reference Include="Assembly-CSharp">
  <HintPath>你的RimWorld路径\RimWorldWin64_Data\Managed\Assembly-CSharp.dll</HintPath>
  <Private>False</Private>
</Reference>
<Reference Include="UnityEngine.CoreModule">
  <HintPath>你的RimWorld路径\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll</HintPath>
  <Private>False</Private>
</Reference>
<Reference Include="0Harmony">
  <HintPath>你的RimWorld路径\RimWorldWin64_Data\Managed\0Harmony.dll</HintPath>
  <Private>False</Private>
</Reference>
```

### 方法 2: 使用环境变量（推荐）

1. 设置环境变量 `RIMWORLD_DIR` 指向您的 RimWorld 安装目录
2. 项目文件会自动使用该路径

### 方法 3: 手动复制 DLL（最简单）

如果您只想测试，可以直接复制编译好的 DLL：

1. 从 RimWorld 的 `RimWorldWin64_Data\Managed\` 目录复制以下文件到项目的 `Assemblies` 文件夹：
   - `Assembly-CSharp.dll`
   - `UnityEngine.CoreModule.dll`
   - `0Harmony.dll`
   - `UnityEngine.dll`

2. 编译后将生成的 `RimTalkSocialDining.dll` 放入 mod 的 `Assemblies` 文件夹

## ?? 文件结构

```
Share your food and eat together/
├── About/
│   └── About.xml                          # Mod 元数据
├── Assemblies/                            # 编译输出目录
│   └── RimTalkSocialDining.dll
├── Defs/
│   ├── JobDefs/
│   │   └── Jobs_SocialDining.xml          # 任务定义
│   └── ThinkTreeDefs/
│       └── ThinkTree_SocialDining.xml     # AI 思考树定义
├── Patches/
│   └── ThinkTree_SocialDining_Patch.xml   # XML 补丁
├── Languages/
│   ├── ChineseSimplified/
│   │   └── Keyed/
│   │       └── SocialDining_Keys.xml      # 中文翻译
│   └── English/
│       └── Keyed/
│           └── SocialDining_Keys.xml      # 英文翻译
└── Source/
    └── RimTalkSocialDining/
        ├── SharedFoodTracker.cs           # 食物追踪组件
        ├── JobDriver_SocialDine.cs        # 社交共餐任务驱动
        ├── JobGiver_SocialDine.cs         # 任务生成器
        ├── ThinkNode_ConditionalCanSocialDine.cs  # AI 条件检查
        ├── HarmonyPatches.cs              # Harmony 补丁
        ├── SocialDiningDefOf.cs           # Def 静态引用
        └── RimTalkSocialDining.csproj     # 项目文件
```

## ?? 快速开始

### 编译项目：
```bash
cd "Source/RimTalkSocialDining"
dotnet build
```

### 安装 Mod：
1. 将整个文件夹复制到 RimWorld 的 `Mods` 目录
2. 在游戏中的 Mod 管理器启用此 mod
3. 重启游戏

## ?? 使用方法

Mod 启用后会自动工作：
1. 当两个或更多殖民者同时饥饿时
2. 系统会自动尝试安排他们共享同一份食物
3. 他们会走到同一位置（餐桌或地面）
4. 面对面同时食用同一个食物实例
5. 获得社交加成心情

## ?? 技术细节

### SharedFoodTracker 组件
- 附加到食物 Thing 上
- 线程安全的引用计数
- 防止食物被过早销毁

### JobDriver_SocialDine
- 实现 `ExposeData` 保证存档兼容性
- 使用 `maxClaimants=2` 实现多人预留
- 同步的进食 Toil 序列

### JobGiver_SocialDine
- 使用 `LongEventHandler.ExecuteWhenFinished` 确保主线程执行
- 智能匹配算法选择最佳用餐伙伴
- 支持餐桌和野餐两种模式

### Harmony 补丁
- 防止共享食物被其他系统干扰
- 集成到原版 AI 流程

## ?? 注意事项

1. **兼容性**：此 mod 修改了 Pawn 的 AI 思考树，可能与其他修改进食行为的 mod 冲突
2. **性能**：在大量殖民者同时饥饿时可能有轻微性能影响
3. **存档**：可以安全地在现有存档中添加或移除此 mod

## ?? 调试

启用开发模式后，mod 会在日志中输出详细信息：
- Harmony 补丁应用状态
- 社交共餐触发条件
- 伙伴匹配过程
- 食物追踪状态

## ?? 许可

此 mod 为开源项目，欢迎修改和分发。

## ?? 致谢

感谢 RimWorld 社区和 Ludeon Studios。
