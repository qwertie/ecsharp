copy Bin\LLLPG\Release.NET4\*.dll Lib\LLLPG
copy Bin\LLLPG\Release.NET4\*.exe Lib\LLLPG
copy Bin\LLLPG\Release.NET4\*.xml Lib\LLLPG
copy Bin\LLLPG\Release.NET4\*.pdb Lib\LLLPG
del Lib\LLLPG\*.vshost.exe
"C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" /out vsbuild.log /build Debug "Visual Studio Integration\Visual Studio Integration 2010.sln"
@IF ERRORLEVEL 1 GOTO ERROR
copy "Visual Studio Integration\LoycFileGeneratorForVs\bin\Debug\LllpgForVisualStudio.exe" "Lib\LLLPG"
@IF ERRORLEVEL 1 GOTO ERROR
"Lib\LLLPG\LllpgForVisualStudio.exe"
@IF ERRORLEVEL 1 GOTO ERROR
copy "Visual Studio Integration\LoycExtensionForVs\bin\Debug\LoycSyntaxForVs.vsix" "Visual Studio Integration\LoycExtensionForVs\LoycSyntaxForVs2010.vsix"
@IF ERRORLEVEL 1 GOTO ERROR
"Visual Studio Integration\LoycExtensionForVs\LoycSyntaxForVs2010.vsix"
pause
GOTO STOP
:ERROR
@echo *** ERROR OCCURRED ***
more vsbuild.log
del vsbuild.log
pause
:STOP

