copy LesLexerGenerated.cs "LesLexerGenerated - previous.cs"
copy LesParserGenerated.cs "LesParserGenerated - previous.cs"
copy ..\..\..\Bin\LLLPG\Debug.NET4\LesLexerGenerated.cs .
if errorlevel 1 (pause)
copy ..\..\..\Bin\LLLPG\Debug.NET4\LesParserGenerated.cs .
if errorlevel 1 (pause)