@echo off
echo.
echo === Preparing to push to remote repository ===

echo.
echo Adding all files to Git...
git add .
if errorlevel 1 (
    echo Error: Failed to add files to Git.
    exit /b %errorlevel%
)

set /p commit_message=Enter your commit message: 

echo.
echo Making initial commit...
git commit -m "%commit_message%"
if errorlevel 1 (
    echo Error: Failed to make initial commit.
    exit /b %errorlevel%
)

echo.
echo Pushing to remote repository...
git push -u origin main
if errorlevel 1 (
    echo Error: Failed to push to remote repository.
    exit /b %errorlevel%
)

echo.
echo Push to remote repository completed successfully!
pause
