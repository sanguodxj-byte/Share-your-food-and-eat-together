@echo off
REM 快速部署脚本 - 编译并部署到 RimWorld

echo 开始快速部署...
echo.

REM 编译
dotnet build "Source\RimTalkSocialDining\RimTalkSocialDining.csproj" -c Release
if errorlevel 1 (
    echo 编译失败！
    pause
    exit /b 1
)

REM 部署
call Deploy.bat

echo.
echo 快速部署完成！
