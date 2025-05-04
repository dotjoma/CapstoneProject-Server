@echo off
cls
echo.
echo ====[ Git Push Assistant ]===============================================
echo.
echo This script will help you push your changes to a remote Git repository.
echo Make sure you have saved all your files before proceeding.
echo =========================================================================
echo.

:: Check if we're in a Git repository
git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo [ERROR] This doesn't appear to be a Git repository.
    echo.
    echo Possible solutions:
    echo 1. Run 'git init' first to create a new repository
    echo 2. Navigate to your project's root folder
    echo 3. Clone an existing repository first
    echo.
    pause
    exit /b 1
)

:: Stage changes
echo.
echo ===[ Staging Changes ]==================================================
echo.
echo Adding all changed files to Git staging area...
git add . >nul 2>&1
if errorlevel 1 (
    echo.
    echo [ERROR] Failed to stage changes. Possible issues:
    echo - File permission problems
    echo - Git repository corruption
    echo.
    pause
    exit /b %errorlevel%
)

:: Check if anything is staged
git diff --cached --quiet
if %errorlevel% EQU 0 (
    echo.
    echo [INFO] No changes were staged.
    echo Nothing to commit.
    echo.
    pause
    exit /b 0
)

:: Get commit message
echo.
echo ===[ Commit Message ]===================================================
echo.
:retry_commit_message
set /p "commit_message=Describe your changes (commit message): "
if "%commit_message%"=="" (
    echo [ERROR] Commit message cannot be empty!
    goto retry_commit_message
)

:: Create commit
echo.
echo Creating commit with your message...
git commit -m "%commit_message%" >nul 2>&1
if errorlevel 1 (
    echo.
    echo [ERROR] Commit failed. Common reasons:
    echo - Merge conflicts need resolution
    echo - Pre-commit hooks failing
    echo.
    pause
    exit /b %errorlevel%
)

:: Push changes
echo.
echo ===[ Pushing Changes ]==================================================
echo.
echo Connecting to remote repository...
git remote show origin >nul 2>&1 || (
    echo.
    echo [WARNING] No remote 'origin' configured
    set /p "remote_url=Enter your remote repository URL: "
    git remote add origin "%remote_url%"
)

echo.
echo Pushing changes to main branch...
git push -u origin main
if errorlevel 1 (
    echo.
    echo [ERROR] Push failed. Troubleshooting:
    echo 1. Check internet connection
    echo 2. Verify repository URL: git remote -v
    echo 3. Try pulling first: git pull origin main
    echo 4. Check permissions (SSH keys if using SSH)
    echo.
    pause
    exit /b %errorlevel%
)

:: Success
echo.
echo ======[ SUCCESS ]=======================================================
echo.
echo Your changes have been successfully pushed to the remote repository!
echo.
echo Next steps:
echo - Refresh your repository page online
echo - To make more changes: edit files, then run this script again
echo - View your commit history: git log --oneline
echo.
pause
