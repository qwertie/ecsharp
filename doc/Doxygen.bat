@REM How to setting up doxygen:
@REM 0. Install Doxygen
@REM 1. Run doxygen -g once (doxygen.exe, not this batch file) to create a default Doxyfile
@REM 2. Spend some time to edit and configure the Doxyfile
@REM 3. Run doxygen and check out what it generates
@REM 4. Using C#? Work around Doxygen's C# bugs, e.g.
@REM    http://stackoverflow.com/questions/1862318/doxygen-with-c-sharp-internal-access-modifier
@REM 5. Run Doxygen -l to create a layout file
@REM 6. Rearrange the layout file to your liking
@REM 7. Use sed to convert the original filenames to hyperlinks
@REM 8. Run this batch file (Note: I assume the output folder HTML_OUTPUT = code) (/Q = no confirm prompt)
del /Q code\*.*
doxygen.exe
if errorlevel 1 pause
del doxygen*.tmp
@REM 8a. Can you reach sed on the command line? If not, add it to your PATH 
@REM     (e.g. Git typically comes with a copy)
@REM 8b. Change the regex and http link to properly recognize your files and point to your repo
sed --in-place -r "s_<li>.*(/Src/.*)</li>_<li><a href='https://github.com/qwertie/Loyc/blob/master\1'>\1</a></li>_;s_The documentation for this [a-z]* was generated from the following file:_Source code:_" ./code/*.html
@REM 8c. Getting "sed: preserving permissions for `./sed002836': Permission denied"?
@REM     Changing the output folder permissions to "Full Control" for the current user may help.
@REM 9. If using Visual Studio, go to Tools | External Tools... and add this batch 
@REM    file as a tool. Th "Use Output Window" check box will allow you to double-
@REM    click errors to go to the error location in source code.
@echo The End.
