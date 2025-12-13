@echo off
chcp 65001 > nul
echo ========================================
echo Share your food and eat together - Deploy Script
echo ========================================
echo.

REM Set variables
set SOURCE_DIR=%~dp0
set TARGET_DIR=D:\steam\steamapps\common\RimWorld\Mods\ShareYourFoodAndEatTogether

echo Source: %SOURCE_DIR%
echo Target: %TARGET_DIR%
echo.

REM Step 1: Build project
echo [1/5] Building project...
dotnet build "%SOURCE_DIR%Source\RimTalkSocialDining\RimTalkSocialDining.csproj" -c Release
if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)
echo [OK] Build successful
echo.

REM Step 2: Clean target directory if exists
if exist "%TARGET_DIR%" (
    echo [2/5] Cleaning old files...
    rmdir /s /q "%TARGET_DIR%"
    echo [OK] Cleaned
) else (
    echo [2/5] Target directory does not exist, skipping cleanup
)
echo.

REM Step 3: Create directory structure
echo [3/5] Creating directory structure...
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

REM Step 4: Copy files
echo [4/5] Copying files...

REM Copy About.xml
copy "%SOURCE_DIR%About\About.xml" "%TARGET_DIR%\About\" > nul
echo     Copied: About\About.xml

REM Copy DLL
copy "%SOURCE_DIR%Assemblies\RimTalkSocialDining.dll" "%TARGET_DIR%\Assemblies\" > nul
echo     Copied: Assemblies\RimTalkSocialDining.dll

REM Copy Defs
copy "%SOURCE_DIR%Defs\JobDefs\*.xml" "%TARGET_DIR%\Defs\JobDefs\" > nul
echo     Copied: Defs\JobDefs\*.xml

copy "%SOURCE_DIR%Defs\ThinkTreeDefs\*.xml" "%TARGET_DIR%\Defs\ThinkTreeDefs\" > nul
echo     Copied: Defs\ThinkTreeDefs\*.xml

copy "%SOURCE_DIR%Defs\ThoughtDefs\*.xml" "%TARGET_DIR%\Defs\ThoughtDefs\" > nul
echo     Copied: Defs\ThoughtDefs\*.xml

copy "%SOURCE_DIR%Defs\InteractionDefs\*.xml" "%TARGET_DIR%\Defs\InteractionDefs\" > nul
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

REM Step 5: Verify deployment
echo [5/5] Verifying deployment...
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

echo [OK] Verification passed
echo.

echo ========================================
echo Deployment Successful!
echo ========================================
echo.
echo Mod deployed to: %TARGET_DIR%
echo.
echo Next steps:
echo 1. Launch RimWorld
echo 2. Enable "Share your food and eat together" in Mod Manager
echo 3. Restart the game
echo.
pause
