@echo off
chcp 65001 > nul
echo ========================================
echo Share your food and eat together - Deploy Script
echo ========================================
echo.

REM Set variables
set SOURCE_DIR=%~dp0
set TARGET_DIR=D:\steam\steamapps\common\RimWorld\Mods\ShareYourFoodAndEatTogether
set OLD_DIR=D:\steam\steamapps\common\RimWorld\Mods\Share your food and eat together

echo Source: %SOURCE_DIR%
echo Target: %TARGET_DIR%
echo.

REM Step 1: Build project
echo [1/6] Building project...
dotnet build "%SOURCE_DIR%Source\RimTalkSocialDining\RimTalkSocialDining.csproj" -c Release
if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)
echo [OK] Build successful
echo.

REM Step 2: Clean old version with spaces in name
if exist "%OLD_DIR%" (
    echo [2/6] Cleaning old version (with spaces)...
    rmdir /s /q "%OLD_DIR%"
    echo [OK] Old version removed
) else (
    echo [2/6] Old version does not exist, skipping
)
echo.

REM Step 3: Clean target directory if exists
if exist "%TARGET_DIR%" (
    echo [3/6] Cleaning current version...
    rmdir /s /q "%TARGET_DIR%"
    echo [OK] Cleaned
) else (
    echo [3/6] Target directory does not exist, skipping cleanup
)
echo.

REM Step 4: Create directory structure
echo [4/6] Creating directory structure...
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
echo [OK] Directories created
echo.

REM Step 5: Copy files
echo [5/6] Copying files...

REM Copy About.xml
copy "%SOURCE_DIR%About\About.xml" "%TARGET_DIR%\About\" > nul
echo     Copied: About\About.xml

REM Copy DLL
copy "%SOURCE_DIR%Assemblies\RimTalkSocialDining.dll" "%TARGET_DIR%\Assemblies\" > nul
echo     Copied: Assemblies\RimTalkSocialDining.dll

REM Copy Defs
copy "%SOURCE_DIR%Defs\JobDefs\*.xml" "%TARGET_DIR%\Defs\JobDefs\" > nul 2>&1
echo     Copied: Defs\JobDefs\*.xml

copy "%SOURCE_DIR%Defs\ThinkTreeDefs\*.xml" "%TARGET_DIR%\Defs\ThinkTreeDefs\" > nul 2>&1
echo     Copied: Defs\ThinkTreeDefs\*.xml

copy "%SOURCE_DIR%Defs\ThoughtDefs\*.xml" "%TARGET_DIR%\Defs\ThoughtDefs\" > nul 2>&1
echo     Copied: Defs\ThoughtDefs\*.xml

copy "%SOURCE_DIR%Defs\InteractionDefs\*.xml" "%TARGET_DIR%\Defs\InteractionDefs\" > nul 2>&1
echo     Copied: Defs\InteractionDefs\*.xml

REM Copy Patches
copy "%SOURCE_DIR%Patches\*.xml" "%TARGET_DIR%\Patches\" > nul
echo     Copied: Patches\*.xml

REM Copy language files
copy "%SOURCE_DIR%Languages\ChineseSimplified\Keyed\*.xml" "%TARGET_DIR%\Languages\ChineseSimplified\Keyed\" > nul
echo     Copied: Languages\ChineseSimplified\Keyed\*.xml

copy "%SOURCE_DIR%Languages\English\Keyed\*.xml" "%TARGET_DIR%\Languages\English\Keyed\" > nul
echo     Copied: Languages\English\Keyed\*.xml

REM Copy README (optional)
if exist "%SOURCE_DIR%README.md" (
    copy "%SOURCE_DIR%README.md" "%TARGET_DIR%\" > nul
    echo     Copied: README.md
)

echo [OK] Files copied
echo.

REM Step 6: Verify deployment
echo [6/6] Verifying deployment...
if exist "%TARGET_DIR%\Assemblies\RimTalkSocialDining.dll" (
    echo     [OK] DLL file exists
) else (
    echo     [ERROR] DLL file not found
    pause
    exit /b 1
)

if exist "%TARGET_DIR%\About\About.xml" (
    echo     [OK] About.xml exists
) else (
    echo     [ERROR] About.xml not found
    pause
    exit /b 1
)

if exist "%TARGET_DIR%\Defs\JobDefs\Jobs_SocialDining.xml" (
    echo     [OK] JobDefs exists
) else (
    echo     [WARNING] JobDefs not found
)

REM Check if old version still exists
if exist "%OLD_DIR%" (
    echo     [WARNING] Old version still exists, please delete manually
) else (
    echo     [OK] No duplicate versions found
)

echo [OK] Verification passed
echo.

echo ========================================
echo Deployment Successful!
echo ========================================
echo.
echo Mod deployed to: %TARGET_DIR%
echo.
echo IMPORTANT: Close RimWorld if running!
echo.
echo Next steps:
echo 1. Launch RimWorld
echo 2. Enable "Share your food and eat together" in Mod Manager
echo 3. Restart the game
echo.
pause
