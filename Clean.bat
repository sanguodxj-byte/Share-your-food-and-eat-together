@echo off
echo ========================================
echo RimTalk Social Dining - 清理脚本
echo ========================================
echo.

set TARGET_DIR=D:\steam\steamapps\common\RimWorld\Mods\Share your food and eat together

if exist "%TARGET_DIR%" (
    echo 正在清理目标目录...
    echo %TARGET_DIR%
    echo.
    
    choice /C YN /M "确定要删除该目录吗"
    if errorlevel 2 (
        echo 已取消
        pause
        exit /b 0
    )
    
    rmdir /s /q "%TARGET_DIR%"
    echo.
    echo 清理完成！
) else (
    echo 目标目录不存在，无需清理
)

echo.
pause
