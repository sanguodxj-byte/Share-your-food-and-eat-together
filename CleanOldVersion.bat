@echo off
chcp 65001 > nul
echo ========================================
echo Cleaning Old Mod Versions
echo ========================================
echo.

set OLD_DIR=D:\steam\steamapps\common\RimWorld\Mods\Share your food and eat together
set NEW_DIR=D:\steam\steamapps\common\RimWorld\Mods\ShareYourFoodAndEatTogether

echo Checking for old version...
if exist "%OLD_DIR%" (
    echo Found old version: %OLD_DIR%
    echo Deleting...
    rmdir /s /q "%OLD_DIR%"
    if exist "%OLD_DIR%" (
        echo [ERROR] Failed to delete old version
        echo Please close RimWorld and try again
    ) else (
        echo [OK] Old version deleted
    )
) else (
    echo [OK] No old version found
)

echo.
echo Checking for new version...
if exist "%NEW_DIR%" (
    echo [OK] New version exists: %NEW_DIR%
) else (
    echo [WARNING] New version not found
)

echo.
pause
