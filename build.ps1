param(
    [parameter(Position=0)][string] $PreReleaseSuffix = '',
    [switch] $SkipTests = $false
)

$packageOutputFolder = "$PSScriptRoot\.nupkgs"

# Restore packages and build product
Write-Host "Restoring..." -ForegroundColor "Green"
& dotnet restore -v Minimal # Restore all packages
if ($LASTEXITCODE -ne 0)
{
    throw "dotnet restore failed with exit code $LASTEXITCODE"
}

if ($PreReleaseSuffix) {
    & dotnet build --version-suffix "$PreReleaseSuffix"
} else {
    & dotnet build
}

# Run tests
if ($SkipTests)
{
    Write-Host "Skipping Tests..." -ForegroundColor "Yellow"
}
else
{
    Write-Host "Running Tests..." -ForegroundColor "Green"
    Get-ChildItem "Dapper.Test*.csproj" -Recurse |
    ForEach-Object {
        & dotnet test "$_"
    }
}

# Package all
if ($PreReleaseSuffix) {
    & dotnet pack -c Release -o "$packageOutputFolder" --version-suffix "$PreReleaseSuffix"   
} else {
    & dotnet pack -c Release -o "$packageOutputFolder"
}