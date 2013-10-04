REM copy LesLexerGenerated.cs "LesLexerGenerated - previous.cs"
REM copy LesParserGenerated.cs "LesParserGenerated - previous.cs"
REM copy ..\..\..\Bin\LLLPG\Debug.NET4\LesLexerGenerated.cs .
REM if errorlevel 1 (pause)
copy LesParserGrammar.cs LesParserGenerated.cs
if errorlevel 1 (pause)