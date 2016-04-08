copy Bin\Release.NET4\*.dll Lib\LeMP
@IF ERRORLEVEL 1 GOTO ERROR
copy Bin\Release.NET4\*.exe Lib\LeMP
@IF ERRORLEVEL 1 GOTO ERROR
copy Bin\Release.NET4\*.xml Lib\LeMP
copy Bin\Release.NET4\*.pdb Lib\LeMP
del Lib\LeMP\*.vshost.exe
"C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" /out vsbuild.log /build Debug "Visual Studio Integration\Visual Studio Integration 2010.sln"
@IF ERRORLEVEL 1 GOTO ERROR
copy "Visual Studio Integration\LoycFileGeneratorForVs\bin\Debug\LoycFileGeneratorForVs.exe" "Lib\LeMP"
@IF ERRORLEVEL 1 GOTO ERROR
"Lib\LeMP\LoycFileGeneratorForVs.exe"
@IF ERRORLEVEL 1 GOTO ERROR
copy "Visual Studio Integration\LoycExtensionForVs\bin\Debug\LoycSyntaxForVs.vsix" "Lib\LeMP\LoycSyntaxForVs.vsix"
@IF ERRORLEVEL 1 GOTO ERROR
"Lib\LeMP\LoycSyntaxForVs.vsix"
pause
GOTO STOP
:ERROR
@echo **********************
@echo *** ERROR OCCURRED ***
@echo **********************
@if exist vsbuild.log more vsbuild.log
pause
:STOP
if exist vsbuild.log del vsbuild.log
