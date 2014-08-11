@rem This is used to push changes in Core/ to the LoycCore repo on GitHub.
@rem If the push fails due to changes in LoycCore, do a fetch+merge first:
@rem     git subtree pull --prefix=Core core master
rem It is typical that the first command fails (it only works the first time)
git remote add core https://github.com/qwertie/LoycCore.git
git subtree push --prefix=Core core master
pause
