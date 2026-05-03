#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(dirname "$script_dir")"
cd "$repo_root"

echo "CampusBooking setup (macOS / Linux)"
echo "Repo root: $repo_root"
echo ""

echo "Checking .NET SDK..."
if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet was not found on PATH. Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0 and try again."
    exit 1
fi
if ! dotnet --list-sdks | grep -q '^8\.'; then
    echo "No .NET 8 SDK found. Installed SDKs:"
    dotnet --list-sdks | sed 's/^/  /'
    echo "Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0 and try again."
    exit 1
fi
echo "Found .NET 8 SDK."

platform="$(uname -s)"
echo ""
echo "Detected platform: $platform"
echo "LocalDB is not available outside Windows. Point this script at a SQL Server reachable from your machine"
echo "(SQL Server in Docker, Azure SQL, or any remote instance)."

default_conn="Server=localhost,1433;Database=CampusBooking;User Id=sa;Password=YourStrong!Pass;TrustServerCertificate=True"
conn="${CFB_API_CONNECTIONSTRING:-$default_conn}"

if [ -z "${CFB_API_CONNECTIONSTRING:-}" ]; then
    echo "CFB_API_CONNECTIONSTRING not set. Using default: $default_conn"
else
    echo "Using CFB_API_CONNECTIONSTRING from environment."
fi

echo ""
echo "Checking sqlcmd..."
if ! command -v sqlcmd >/dev/null 2>&1; then
    echo "sqlcmd was not found. Install Microsoft mssql-tools:"
    echo "  macOS:  brew install mssql-tools"
    echo "  Linux:  https://learn.microsoft.com/sql/linux/sql-server-linux-setup-tools"
    exit 1
fi

parse_field() {
    local key="$1"
    echo "$conn" | tr ';' '\n' | awk -F'=' -v k="$key" 'tolower($1)==tolower(k){sub(/^[^=]*=/,""); print; exit}'
}

server="$(parse_field 'Server')"
[ -z "$server" ] && server="$(parse_field 'Data Source')"
db="$(parse_field 'Database')"
[ -z "$db" ] && db="$(parse_field 'Initial Catalog')"
user="$(parse_field 'User Id')"
[ -z "$user" ] && user="$(parse_field 'Uid')"
password="$(parse_field 'Password')"
[ -z "$password" ] && password="$(parse_field 'Pwd')"

if [ -z "$server" ] || [ -z "$db" ] || [ -z "$user" ] || [ -z "$password" ]; then
    echo "Could not parse Server / Database / User Id / Password from CFB_API_CONNECTIONSTRING."
    exit 1
fi

echo "Ensuring database '$db' exists on '$server'..."
sqlcmd -S "$server" -U "$user" -P "$password" -C -b -Q "IF DB_ID('$db') IS NULL CREATE DATABASE [$db]"

schema_path="$repo_root/sql/CampusBooking-Schema.sql"
if [ -f "$schema_path" ]; then
    echo "Applying schema from $schema_path..."
    sqlcmd -S "$server" -U "$user" -P "$password" -C -d "$db" -b -i "$schema_path"
else
    echo "Schema file not found at $schema_path. Skipping schema apply."
fi

echo ""
echo "Building solution..."
dotnet build "$repo_root/src/CampusBooking.sln" --nologo

echo ""
echo "Seeding test data..."
CFB_ConnectionStrings__Default="$conn" dotnet run --project "$repo_root/src/CampusBooking.Api" -- seed --test

echo ""
echo "Setup complete. Run 'CFB_ConnectionStrings__Default=\"\$CFB_API_CONNECTIONSTRING\" dotnet run --project src/CampusBooking.Api' to start the API."
