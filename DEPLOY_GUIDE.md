# ?? 部署指南 - RimTalk Social Dining

## ?? 快速部署（推荐）

### 方法 1: 一键部署
```cmd
双击运行: QuickDeploy.bat
```

这将自动：
1. ? 编译项目（Release 配置）
2. ? 清理旧文件
3. ? 创建目录结构
4. ? 复制所有必需文件
5. ? 验证部署结果

---

## ??? 手动部署步骤

### 步骤 1: 编译项目
```cmd
Build.bat
```

### 步骤 2: 部署到 RimWorld
```cmd
Deploy.bat
```

### 步骤 3: 验证部署
检查目标目录：
```
D:\steam\steamapps\common\RimWorld\Mods\RimTalkSocialDining\
```

---

## ?? 部署后的文件结构

```
D:\steam\steamapps\common\RimWorld\Mods\RimTalkSocialDining\
├── About/
│   └── About.xml                     ? Mod 元数据
├── Assemblies/
│   └── RimTalkSocialDining.dll       ? 编译后的代码
├── Defs/
│   ├── JobDefs/
│   │   └── Jobs_SocialDining.xml     ? 任务定义
│   ├── ThinkTreeDefs/
│   │   └── ThinkTree_SocialDining.xml ? AI 思考树
│   ├── ThoughtDefs/
│   │   └── Thoughts_SocialDining.xml ? 心情定义
│   └── InteractionDefs/
│       └── Interaction_OfferFood.xml ? 互动定义
├── Patches/
│   └── ThinkTree_SocialDining_Patch.xml ? XML 补丁
├── Languages/
│   ├── ChineseSimplified/Keyed/
│   │   └── SocialDining_Keys.xml     ? 中文翻译
│   └── English/Keyed/
│       └── SocialDining_Keys.xml     ? 英文翻译
└── README.md                         ?? 可选
```

---

## ?? 在游戏中启用 Mod

### 步骤 1: 启动 RimWorld
```
1. 双击 RimWorld 启动器
2. 等待游戏加载到主菜单
```

### 步骤 2: 打开 Mod 管理器
```
主菜单 → 选项 (Options) → Mod 设置 (Mods)
```

### 步骤 3: 找到并启用 Mod
```
1. 在可用 Mod 列表中找到：
   "RimTalk True Social Dining"
   
2. 点击激活（勾选复选框）

3. 确保加载顺序正确：
   - Core（核心）
   - Harmony（如果有）
   - HugsLib（如果有）
   - RimTalk True Social Dining ← 您的 Mod
   - 其他 Mod...
   
4. 点击底部的"重启"按钮
```

### 步骤 4: 重启游戏
```
游戏会自动重启并加载 Mod
```

---

## ? 验证 Mod 是否工作

### 方法 1: 检查 Mod 列表
```
1. 游戏启动后，在主菜单查看左下角
2. 应该能看到 Mod 名称和版本
```

### 方法 2: 查看日志
```
日志文件位置：
C:\Users\您的用户名\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log

查找关键信息：
? "RimTalkSocialDining.dll loaded successfully"
? "Harmony patches applied"
? 如果有错误，会显示 [Error] 标记
```

### 方法 3: 游戏内测试
```
1. 开始新游戏或加载存档
2. 让 2 个殖民者处于饥饿状态（< 30%）
3. 确保有食物可用
4. 观察殖民者是否自动走向食物并寻找伙伴共餐

预期效果：
? 两个殖民者会走到同一个食物旁
? 面对面同步进食
? 完成后获得心情加成 "+3 与他人共餐"
```

---

## ?? 故障排除

### 问题 1: Mod 未出现在列表中

**可能原因：**
- About.xml 格式错误
- 文件夹名称或位置不正确

**解决方法：**
```cmd
# 验证 About.xml
type "D:\steam\steamapps\common\RimWorld\Mods\RimTalkSocialDining\About\About.xml"

# 检查格式是否正确
```

### 问题 2: 游戏启动时报错

**查看日志文件：**
```
C:\Users\您的用户名\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
```

**常见错误：**
```
1. "Could not load RimTalkSocialDining.dll"
   → 重新编译并部署
   
2. "Missing dependency: Harmony"
   → 从 Steam 创意工坊安装 Harmony Mod
   
3. "XML parse error"
   → 检查 XML 文件格式
```

### 问题 3: 功能不工作

**检查清单：**
- [ ] Mod 是否在 Mod 列表中启用
- [ ] 游戏是否重启
- [ ] 殖民者是否足够饥饿（< 30%）
- [ ] 是否有可用的食物
- [ ] 是否有附近的伙伴

**启用 Dev Mode 调试：**
```
1. 游戏中按 F12 启用开发者模式
2. 按 Ctrl + F12 打开日志窗口
3. 触发共餐场景，观察日志输出
```

---

## ?? 更新部署

### 修改代码后重新部署：
```cmd
# 方法 1: 快速部署（推荐）
QuickDeploy.bat

# 方法 2: 分步部署
Build.bat
Deploy.bat
```

### 仅更新 XML 定义：
```cmd
# 直接复制 XML 文件
xcopy "Defs\*" "D:\steam\steamapps\common\RimWorld\Mods\RimTalkSocialDining\Defs\" /E /Y
xcopy "Patches\*" "D:\steam\steamapps\common\RimWorld\Mods\RimTalkSocialDining\Patches\" /E /Y

# 重启游戏生效
```

---

## ?? 清理部署

### 完全删除 Mod：
```cmd
Clean.bat
```

### 手动删除：
```cmd
rmdir /s /q "D:\steam\steamapps\common\RimWorld\Mods\RimTalkSocialDining"
```

---

## ?? 部署检查清单

### 编译检查 ?
- [ ] `dotnet build` 成功（0 警告，0 错误）
- [ ] `Assemblies\RimTalkSocialDining.dll` 已生成
- [ ] DLL 大小约 30 KB

### 文件检查 ?
- [ ] `About\About.xml` 存在
- [ ] `Assemblies\RimTalkSocialDining.dll` 存在
- [ ] `Defs\JobDefs\Jobs_SocialDining.xml` 存在
- [ ] `Defs\ThinkTreeDefs\ThinkTree_SocialDining.xml` 存在
- [ ] `Defs\ThoughtDefs\Thoughts_SocialDining.xml` 存在
- [ ] `Defs\InteractionDefs\Interaction_OfferFood.xml` 存在
- [ ] `Patches\ThinkTree_SocialDining_Patch.xml` 存在
- [ ] `Languages\ChineseSimplified\Keyed\SocialDining_Keys.xml` 存在
- [ ] `Languages\English\Keyed\SocialDining_Keys.xml` 存在

### 游戏检查 ?
- [ ] Mod 出现在 Mod 列表中
- [ ] 没有加载错误
- [ ] 游戏可以正常启动
- [ ] 功能可以正常触发

---

## ?? 快速参考

### 常用命令
```cmd
# 编译
Build.bat

# 部署
Deploy.bat

# 一键部署
QuickDeploy.bat

# 清理
Clean.bat
```

### 重要路径
```
源代码目录:
C:\Users\Administrator\Desktop\rim mod\Share your food and eat together\

目标部署目录:
D:\steam\steamapps\common\RimWorld\Mods\RimTalkSocialDining\

日志文件:
C:\Users\您的用户名\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
```

---

## ?? 需要帮助？

如果遇到问题：

1. **查看日志** - 检查 Player.log 文件
2. **重新部署** - 运行 Clean.bat 然后 QuickDeploy.bat
3. **验证文件** - 确保所有必需文件都已复制
4. **检查版本** - 确认 RimWorld 版本与 Mod 支持的版本匹配

---

**祝部署顺利！享受社交共餐功能！** ??????
