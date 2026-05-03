$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptDir
Set-Location $repoRoot

Write-Host "CampusBooking setup (Windows / LocalDB)"
Write-Host "Repo root: $repoRoot"
Write-Host ""

Write-Host "Checking .NET SDK..."
$sdks = & dotnet --list-sdks 2>$null
if (-not $sdks) {
    Write-Host "dotnet was not found on PATH. Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0 and try again."
    exit 1
}
$has8 = $sdks | Where-Object { $_ -match '^8\.' }
if (-not $has8) {
    Write-Host "No .NET 8 SDK found. Installed SDKs:"
    $sdks | ForEach-Object { Write-Host "  $_" }
    Write-Host "Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0 and try again."
    exit 1
}
Write-Host "Found .NET 8 SDK."

Write-Host ""
Write-Host "Checking SqlLocalDB..."
$localDb = Get-Command sqllocaldb -ErrorAction SilentlyContinue
if (-not $localDb) {
    Write-Host "sqllocaldb was not found. Install SQL Server Express LocalDB (bundled with SSMS: https://aka.ms/ssmsfullsetup) and try again."
    exit 1
}

$instance = 'mssqllocaldb'
Write-Host "Starting LocalDB instance '$instance'..."
& sqllocaldb start $instance | Out-Host

Write-Host ""
Write-Host "Checking sqlcmd..."
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    Write-Host "sqlcmd was not found. Install the SQL Server command-line tools (https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility) and try again."
    exit 1
}

$server = "(localdb)\$instance"
$dbName = 'CampusBooking'

Write-Host "Ensuring database '$dbName' exists..."
& sqlcmd -S $server -b -Q "IF DB_ID('$dbName') IS NULL CREATE DATABASE [$dbName]" | Out-Host
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to create or verify database '$dbName'."
    exit 1
}

$schemaPath = Join-Path $repoRoot 'sql\CampusBooking-Schema.sql'
if (Test-Path $schemaPath) {
    Write-Host "Applying schema from $schemaPath..."
    & sqlcmd -S $server -d $dbName -b -i $schemaPath | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Schema script returned a non-zero exit code."
        exit 1
    }
} else {
    Write-Host "Schema file not found at $schemaPath. Skipping schema apply."
}

Write-Host ""
Write-Host "Building solution..."
& dotnet build (Join-Path $repoRoot 'src\CampusBooking.sln') --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed."
    exit 1
}

Write-Host ""
Write-Host "Seeding test data..."
& dotnet run --project (Join-Path $repoRoot 'src\CampusBooking.Api') -- seed --test
if ($LASTEXITCODE -ne 0) {
    Write-Host "Seeder returned a non-zero exit code."
    exit 1
}

Write-Host ""
Write-Host "Setup complete. Run 'dotnet run --project src/CampusBooking.Api' to start the API."
