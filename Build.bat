@echo off
chcp 65001 >nul
echo ========================================
echo RimTalk Social Dining - 编译脚本
echo ========================================
echo.

REM 检测 RimWorld 路径
set "RIMWORLD_PATH="

REM 常见的 Steam 安装路径
if exist "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll" (
    set "RIMWORLD_PATH=C:\Program Files (x86)\Steam\steamapps\common\RimWorld"
)

REM 另一个常见的 Steam 路径
if exist "C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll" (
    set "RIMWORLD_PATH=C:\Steam\steamapps\common\RimWorld"
)

REM D 盘常见路径
if exist "D:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll" (
    set "RIMWORLD_PATH=D:\Steam\steamapps\common\RimWorld"
)

if "%RIMWORLD_PATH%"=="" (
    echo [!] 未找到 RimWorld 安装目录！
    echo.
    echo 请手动输入您的 RimWorld 安装路径
    echo 例如: C:\Program Files (x86)\Steam\steamapps\common\RimWorld
    echo.
    set /p RIMWORLD_PATH="RimWorld 路径: "
)

if not exist "%RIMWORLD_PATH%\RimWorldWin64_Data\Managed\Assembly-CSharp.dll" (
    echo [X] 错误: 指定的路径不包含 RimWorld 文件！
    echo 路径: %RIMWORLD_PATH%
    pause
    exit /b 1
)

echo [√] 找到 RimWorld 安装目录:
echo     %RIMWORLD_PATH%
echo.

REM 创建临时的项目文件
echo [*] 正在生成项目配置...
set "MANAGED_PATH=%RIMWORLD_PATH%\RimWorldWin64_Data\Managed"

REM 备份原始项目文件
if not exist "Source\RimTalkSocialDining\RimTalkSocialDining.csproj.backup" (
    copy "Source\RimTalkSocialDining\RimTalkSocialDining.csproj" "Source\RimTalkSocialDining\RimTalkSocialDining.csproj.backup" >nul
)

REM 使用 PowerShell 替换路径
powershell -Command "(Get-Content 'Source\RimTalkSocialDining\RimTalkSocialDining.csproj.backup') -replace 'Program Files \(x86\)\\Steam\\steamapps\\common\\RimWorld', '%MANAGED_PATH:\=\\%' -replace 'RimWorldWin64_Data\\Managed\\', '' | Set-Content 'Source\RimTalkSocialDining\RimTalkSocialDining.csproj'"

echo [√] 项目配置完成
echo.

REM 编译项目
echo [*] 开始编译...
echo.
cd Source\RimTalkSocialDining
dotnet build -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo [√] 编译成功！
    echo ========================================
    echo.
    echo DLL 已生成到: Assemblies\RimTalkSocialDining.dll
    echo.
    echo 下一步:
    echo 1. 将整个 mod 文件夹复制到 RimWorld 的 Mods 目录
    echo 2. 在游戏中启用 "RimTalk True Social Dining" mod
    echo 3. 重启游戏享受社交共餐！
    echo.
) else (
    echo.
    echo ========================================
    echo [X] 编译失败
    echo ========================================
    echo.
    echo 请检查错误信息并修复后重试
    echo.
)

cd ..\..
pause
