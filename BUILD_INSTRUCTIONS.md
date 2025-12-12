# 编译配置指南

## ?? 找到您的 RimWorld 安装路径

### Steam 版本（常见路径）：
- **Windows**: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld`
- **Linux**: `~/.steam/steam/steamapps/common/RimWorld`
- **Mac**: `~/Library/Application Support/Steam/steamapps/common/RimWorld`

### GOG 版本：
- **Windows**: `C:\GOG Games\RimWorld`

### 自定义路径查找方法：
1. 在 Steam 中右键点击 RimWorld
2. 选择"管理" > "浏览本地文件"
3. 复制路径栏的地址

## ?? 配置项目引用

### 方法 1: 直接修改 .csproj 文件

打开 `Source/RimTalkSocialDining/RimTalkSocialDining.csproj`，找到 `<ItemGroup>` 部分，修改所有 `<HintPath>` 标签：

```xml
<Reference Include="Assembly-CSharp">
  <HintPath>[你的RimWorld路径]\RimWorldWin64_Data\Managed\Assembly-CSharp.dll</HintPath>
  <Private>False</Private>
</Reference>
```

将 `[你的RimWorld路径]` 替换为实际路径，例如：
```xml
<HintPath>C:\Games\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll</HintPath>
```

### 方法 2: 使用相对路径（如果 mod 在 RimWorld Mods 文件夹内）

如果您将此 mod 直接放在 RimWorld 的 Mods 文件夹中，可以使用相对路径：

```xml
<Reference Include="Assembly-CSharp">
  <HintPath>..\..\..\..\RimWorldWin64_Data\Managed\Assembly-CSharp.dll</HintPath>
  <Private>False</Private>
</Reference>
```

## ?? 编译步骤

### 使用命令行：
```bash
cd "Source/RimTalkSocialDining"
dotnet build
```

### 使用 Visual Studio：
1. 打开 `.csproj` 文件
2. 按 `Ctrl+Shift+B` 或点击"生成" > "生成解决方案"

### 编译成功后：
- DLL 会输出到 `Assemblies/RimTalkSocialDining.dll`
- 将整个 mod 文件夹复制到 RimWorld 的 Mods 目录

## ? 常见问题

### Q: 编译时提示找不到 Assembly-CSharp.dll
**A**: 检查 HintPath 路径是否正确，确保文件存在。

### Q: Mac/Linux 上路径分隔符问题
**A**: 将 Windows 的反斜杠 `\` 改为正斜杠 `/`

### Q: 仍然无法编译
**A**: 确保安装了 .NET Framework 4.7.2 或更高版本的 SDK

## ?? 不想编译？直接使用预编译版本

如果您只想使用 mod 而不想自己编译，可以：
1. 从 Release 页面下载预编译版本
2. 解压到 RimWorld Mods 文件夹
3. 在游戏中启用

## ?? 提示

为了方便多个 mod 项目共享引用，建议创建一个统一的引用文件夹，并在所有项目中使用相同的路径配置。
