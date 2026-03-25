@echo off
setlocal

REM Keep this wrapper manual-on-demand so ordinary Visual Studio runs do not rebuild theme assets automatically.
pushd "%~dp0"

if not exist node_modules\nul (
    REM Bootstrap the upstream/reference SASS workspace the first time the wrapper is used on a clone.
    call npm install
    if errorlevel 1 goto :failure
)

REM Rebuild the upstream/reference CSS output before copying the validated baseline themes into the Studio asset tree.
call npm run build
if errorlevel 1 goto :failure

REM Deploy the current light and dark baseline CSS files into the Studio-consumed generated asset location.
call npm run deploy:studio
if errorlevel 1 goto :failure

REM Verify the generated Studio theme files exist so the workflow fails loudly when deployment did not complete.
call npm run verify:studio
if errorlevel 1 goto :failure

popd
exit /b 0

:failure
set "EXIT_CODE=%ERRORLEVEL%"
popd
exit /b %EXIT_CODE%
