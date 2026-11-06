@echo off
setlocal
REM Builds CS2MultiplayerMod from master into the local CS2 Mods folder.
REM Run from anywhere. Code lives in %USERPROFILE%\CS2MultiplayerMod.

set REPO=%USERPROFILE%\CS2MultiplayerMod

echo == Checking tools ==
where git >nul 2>&1 || (echo Installing Git... & winget install --id Git.Git -e --accept-package-agreements --accept-source-agreements)
where node >nul 2>&1 || (echo Installing Node.js... & winget install --id OpenJS.NodeJS.LTS -e --accept-package-agreements --accept-source-agreements)
where dotnet >nul 2>&1 || (echo Installing .NET SDK... & winget install --id Microsoft.DotNet.SDK.8 -e --accept-package-agreements --accept-source-agreements)
dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.NETCore.App 6." >nul || (echo Installing .NET 6 runtime... & winget install --id Microsoft.DotNet.Runtime.6 -e --accept-package-agreements --accept-source-agreements)
where git >nul 2>&1 && where node >nul 2>&1 && where dotnet >nul 2>&1 && set TOOLS_OK=1
if not defined TOOLS_OK (
  echo.
  echo Tools were just installed. Close this window and run the script again.
  pause & exit /b 1
)

if "%CSII_TOOLPATH%"=="" (
  echo.
  echo ERROR: CS2 modding toolchain not found.
  echo Open Cities: Skylines II, Options ^> Modding, install the toolchain,
  echo quit the game, sign out of Windows and back in, then rerun this.
  pause & exit /b 1
)

echo == Getting latest code ==
if exist "%REPO%\.git" (
  git -C "%REPO%" checkout master || goto :fail
  git -C "%REPO%" pull --ff-only || goto :fail
) else (
  git clone https://github.com/bobdyksen/CS2MultiplayerMod.git "%REPO%" || goto :fail
)

echo == Installing UI packages ==
pushd "%REPO%\CS2MultiplayerMod\UI"
call npm.cmd install || (popd & goto :fail)
popd

echo == Building mod ==
dotnet build "%REPO%\CS2MultiplayerMod\CS2MultiplayerMod.csproj" -c Release || goto :fail

echo.
echo BUILD OK. Mod installed to:
echo   %CSII_LOCALMODSPATH%\CS2MultiplayerMod
echo Disable the Paradox Mods copy in your playset, then restart the game.
echo Zip that folder for the other player. Do NOT publish.
explorer "%CSII_LOCALMODSPATH%"
pause
exit /b 0

:fail
echo.
echo BUILD FAILED - copy the error above and send it over.
pause
exit /b 1
