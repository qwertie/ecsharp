@echo off
REM This script builds LeMP + the Loyc libraries, copies them into Lib\LeMP,
REM then builds the VS extension (vsix), copies it to Lib\LeMP, and starts it.
REM   Note: Any previously installed vsix must first be uninstalled manually
REM from within VS.
REM
REM Works with VS 2022 and VS 2026: devenv.exe is located via vswhere instead of
REM a hard-coded path. The Loyc references in the extension are resolved from the
REM DLLs copied into Lib\LeMP below, so the "Loyc.netfx.sln" build MUST run first.

setlocal
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" (
    @echo Could not find vswhere.exe at "%VSWHERE%".
    @echo Install Visual Studio 2022 or 2026, or set DEVENV manually below.
    pause
    goto STOP
)

REM Pick the newest installed VS (2026 sorts after 2022) that has the IDE.
for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -prerelease -products * -property productPath`) do set "DEVENV=%%i"
if not defined DEVENV (
    @echo vswhere did not report a Visual Studio installation with devenv.exe.
    pause
    goto STOP
)
@echo Using: "%DEVENV%"

"%DEVENV%" /out vsbuild.log /build Release "Loyc.netfx.sln"
@IF ERRORLEVEL 1 GOTO ERROR
if not exist Lib\LeMP mkdir Lib\LeMP
copy Bin\Release\*.dll Lib\LeMP
@IF ERRORLEVEL 1 GOTO ERROR
copy Bin\Release\*.exe Lib\LeMP
@IF ERRORLEVEL 1 GOTO ERROR
copy Bin\Release\*.xml Lib\LeMP
copy Bin\Release\*.pdb Lib\LeMP
copy Bin\Release\*.exe.config Lib\LeMP

"%DEVENV%" /out vsbuild.log /build Debug "Visual Studio Integration\Visual Studio Integration 2017.sln"
@IF ERRORLEVEL 1 GOTO ERROR

copy "Visual Studio Integration\LoycForVS2017\bin\Debug\LeMP_VisualStudio.vsix" "Lib\LeMP"
@IF ERRORLEVEL 1 GOTO ERROR

"Lib\LeMP\LeMP_VisualStudio.vsix"
pause
GOTO STOP

:ERROR
@echo **********************
@echo *** ERROR OCCURRED ***
@echo **********************
@if exist vsbuild.log type vsbuild.log
pause

:STOP
if exist vsbuild.log del vsbuild.log
endlocal
