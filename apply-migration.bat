@echo off
echo RestaurantPOS - Database Migration Script
echo ========================================
echo.
echo This script will apply the OrderDetail table updates
echo.

echo Checking for dotnet-ef tool...
dotnet tool list -g | findstr "dotnet-ef" >nul
if %ERRORLEVEL% NEQ 0 (
    echo Installing dotnet-ef tool...
    dotnet tool install --global dotnet-ef
)

echo.
echo Running EF Core migration...
dotnet ef database update -p RestaurantPOS.Data -s RestaurantPOS.WPF

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Migration completed successfully!
) else (
    echo.
    echo Migration failed! Please check the error messages above.
    echo You can manually apply the migration using SQL Server Management Studio
    echo with the script: RestaurantPOS.Data\Migrations\AddNewOrderDetailProperties.sql
)

echo.
pause