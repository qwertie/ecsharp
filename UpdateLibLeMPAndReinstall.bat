REM This script builds the extension (vsix file) for Visual Studio 2017,
REM copies it to Lib/LeMP and then starts the vsix file.
REM   Note: Any previously installed vsix must first be uninstalled manually 
REM from within VS.
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe" /out vsbuild.log /build Release.NET45 "Loyc.netfx.sln"
@IF ERRORLEVEL 1 GOTO ERROR
copy Bin\Release.NET45\*.dll Lib\LeMP
@IF ERRORLEVEL 1 GOTO ERROR
copy Bin\Release.NET45\*.exe Lib\LeMP
@IF ERRORLEVEL 1 GOTO ERROR
copy Bin\Release.NET45\*.xml Lib\LeMP
copy Bin\Release.NET45\*.pdb Lib\LeMP

"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe" /out vsbuild.log /build Debug "Visual Studio Integration\Visual Studio Integration 2017.sln"
@echo off
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
