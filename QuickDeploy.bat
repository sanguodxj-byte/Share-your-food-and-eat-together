@echo off
chcp 65001 > nul
echo ========================================
echo Share your food and eat together - Quick Deploy
echo ========================================
echo.

set "SOURCE=%~dp0"
set "TARGET=D:\steam\steamapps\common\RimWorld\Mods\ShareYourFoodAndEatTogether"

echo Source: %SOURCE%
echo Target: %TARGET%
echo.

REM Build
echo [1/4] Building...
dotnet build "%SOURCE%Source\RimTalkSocialDining\RimTalkSocialDining.csproj" -c Release
if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)
echo [OK] Build successful
echo.

REM Clean and create
echo [2/4] Cleaning target...
if exist "%TARGET%" rmdir /s /q "%TARGET%"
mkdir "%TARGET%"
mkdir "%TARGET%\About"
mkdir "%TARGET%\Assemblies"
mkdir "%TARGET%\Defs\JobDefs"
mkdir "%TARGET%\Defs\ThinkTreeDefs"
mkdir "%TARGET%\Defs\ThoughtDefs"
mkdir "%TARGET%\Defs\InteractionDefs"
mkdir "%TARGET%\Patches"
mkdir "%TARGET%\Languages\ChineseSimplified\Keyed"
mkdir "%TARGET%\Languages\English\Keyed"
echo [OK] Cleaned
echo.

REM Copy files
echo [3/4] Copying files...
xcopy "%SOURCE%About\About.xml" "%TARGET%\About\" /Y /Q > nul
xcopy "%SOURCE%Assemblies\*.dll" "%TARGET%\Assemblies\" /Y /Q > nul
xcopy "%SOURCE%Defs\JobDefs\*.xml" "%TARGET%\Defs\JobDefs\" /Y /Q > nul
xcopy "%SOURCE%Defs\ThinkTreeDefs\*.xml" "%TARGET%\Defs\ThinkTreeDefs\" /Y /Q > nul
xcopy "%SOURCE%Defs\ThoughtDefs\*.xml" "%TARGET%\Defs\ThoughtDefs\" /Y /Q > nul
xcopy "%SOURCE%Defs\InteractionDefs\*.xml" "%TARGET%\Defs\InteractionDefs\" /Y /Q > nul
xcopy "%SOURCE%Patches\*.xml" "%TARGET%\Patches\" /Y /Q > nul
xcopy "%SOURCE%Languages\ChineseSimplified\Keyed\*.xml" "%TARGET%\Languages\ChineseSimplified\Keyed\" /Y /Q > nul
xcopy "%SOURCE%Languages\English\Keyed\*.xml" "%TARGET%\Languages\English\Keyed\" /Y /Q > nul
if exist "%SOURCE%README.md" xcopy "%SOURCE%README.md" "%TARGET%\" /Y /Q > nul
echo [OK] Files copied
echo.

REM Verify
echo [4/4] Verifying...
if exist "%TARGET%\Assemblies\RimTalkSocialDining.dll" (
    echo [OK] DLL exists
) else (
    echo [ERROR] DLL not found
    pause
    exit /b 1
)
if exist "%TARGET%\About\About.xml" (
    echo [OK] About.xml exists
) else (
    echo [ERROR] About.xml not found
    pause
    exit /b 1
)
echo [OK] Verification passed
echo.

echo ========================================
echo Deployment Successful!
echo ========================================
echo.
echo Mod location: %TARGET%
echo.
echo Next: Launch RimWorld and enable the mod
echo.
pause
