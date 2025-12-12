@echo off
echo ========================================
echo Share your food and eat together - 部署脚本
echo ========================================
echo.

REM 设置变量
set SOURCE_DIR=%~dp0
set TARGET_DIR=D:\steam\steamapps\common\RimWorld\Mods\Share your food and eat together

echo 源目录: %SOURCE_DIR%
echo 目标目录: %TARGET_DIR%
echo.

REM 步骤 1: 编译项目
echo [1/5] 正在编译项目...
dotnet build "%SOURCE_DIR%Source\RimTalkSocialDining\RimTalkSocialDining.csproj" -c Release
if errorlevel 1 (
    echo [错误] 编译失败！
    pause
    exit /b 1
)
echo [完成] 编译成功
echo.

REM 步骤 2: 清理目标目录（如果存在）
if exist "%TARGET_DIR%" (
    echo [2/5] 清理旧文件...
    rmdir /s /q "%TARGET_DIR%"
    echo [完成] 清理完成
) else (
    echo [2/5] 目标目录不存在，跳过清理
)
echo.

REM 步骤 3: 创建目录结构
echo [3/5] 创建目录结构...
mkdir "%TARGET_DIR%"
mkdir "%TARGET_DIR%\About"
mkdir "%TARGET_DIR%\Assemblies"
mkdir "%TARGET_DIR%\Defs\JobDefs"
mkdir "%TARGET_DIR%\Defs\ThinkTreeDefs"
mkdir "%TARGET_DIR%\Defs\ThoughtDefs"
mkdir "%TARGET_DIR%\Defs\InteractionDefs"
mkdir "%TARGET_DIR%\Patches"
mkdir "%TARGET_DIR%\Languages\ChineseSimplified\Keyed"
mkdir "%TARGET_DIR%\Languages\English\Keyed"
echo [完成] 目录创建完成
echo.

REM 步骤 4: 复制文件
echo [4/5] 复制文件...

REM 复制 About.xml
copy "%SOURCE_DIR%About\About.xml" "%TARGET_DIR%\About\" > nul
echo     复制: About\About.xml

REM 复制 DLL
copy "%SOURCE_DIR%Assemblies\RimTalkSocialDining.dll" "%TARGET_DIR%\Assemblies\" > nul
echo     复制: Assemblies\RimTalkSocialDining.dll

REM 复制 Defs
copy "%SOURCE_DIR%Defs\JobDefs\*.xml" "%TARGET_DIR%\Defs\JobDefs\" > nul
echo     复制: Defs\JobDefs\*.xml

copy "%SOURCE_DIR%Defs\ThinkTreeDefs\*.xml" "%TARGET_DIR%\Defs\ThinkTreeDefs\" > nul
echo     复制: Defs\ThinkTreeDefs\*.xml

copy "%SOURCE_DIR%Defs\ThoughtDefs\*.xml" "%TARGET_DIR%\Defs\ThoughtDefs\" > nul
echo     复制: Defs\ThoughtDefs\*.xml

copy "%SOURCE_DIR%Defs\InteractionDefs\*.xml" "%TARGET_DIR%\Defs\InteractionDefs\" > nul
echo     复制: Defs\InteractionDefs\*.xml

REM 复制 Patches
copy "%SOURCE_DIR%Patches\*.xml" "%TARGET_DIR%\Patches\" > nul
echo     复制: Patches\*.xml

REM 复制翻译文件
copy "%SOURCE_DIR%Languages\ChineseSimplified\Keyed\*.xml" "%TARGET_DIR%\Languages\ChineseSimplified\Keyed\" > nul
echo     复制: Languages\ChineseSimplified\Keyed\*.xml

copy "%SOURCE_DIR%Languages\English\Keyed\*.xml" "%TARGET_DIR%\Languages\English\Keyed\" > nul
echo     复制: Languages\English\Keyed\*.xml

REM 复制 README（可选）
if exist "%SOURCE_DIR%README.md" (
    copy "%SOURCE_DIR%README.md" "%TARGET_DIR%\" > nul
    echo     复制: README.md
)

echo [完成] 文件复制完成
echo.

REM 步骤 5: 验证部署
echo [5/5] 验证部署...
if exist "%TARGET_DIR%\Assemblies\RimTalkSocialDining.dll" (
    echo     [OK] DLL 文件存在
) else (
    echo     [错误] DLL 文件未找到！
    pause
    exit /b 1
)

if exist "%TARGET_DIR%\About\About.xml" (
    echo     [OK] About.xml 存在
) else (
    echo     [错误] About.xml 未找到！
    pause
    exit /b 1
)

if exist "%TARGET_DIR%\Defs\JobDefs\Jobs_SocialDining.xml" (
    echo     [OK] JobDefs 存在
) else (
    echo     [错误] JobDefs 未找到！
    pause
    exit /b 1
)

echo [完成] 验证通过
echo.

echo ========================================
echo 部署成功！
echo ========================================
echo.
echo Mod 已部署到: %TARGET_DIR%
echo.
echo 下一步：
echo 1. 启动 RimWorld
echo 2. 在 Mod 管理器中启用 "Share your food and eat together"
echo 3. 重启游戏
echo.
pause
