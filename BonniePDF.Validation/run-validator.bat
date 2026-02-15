@echo off
echo ========================================
echo BonniePDF PDF/A Validation Tool
echo ========================================
echo.

REM Check if VeraPDF service is running
echo Checking VeraPDF API...
curl -s http://localhost:8080/ >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Cannot connect to VeraPDF API at http://localhost:8080/
    echo.
    echo Please start the VeraPDF service first:
    echo   docker run -d -p 8080:8080 verapdf/verapdf-rest:latest
    echo.
    pause
    exit /b 1
)

echo VeraPDF API is running!
echo.

REM Build and run the validator
dotnet run --project BonniePDF.Validation/BonniePDF.Validation.csproj %*

echo.
echo ========================================
echo Validation complete!
echo ========================================
pause
