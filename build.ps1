param(
    [parameter(Position=0)][string] $PreReleaseSuffix = '',
    [switch] $SkipTests = $false
)

$packageOutputFolder = "$PSScriptRoot\.nupkgs"

# Restore packages and build product
Write-Host "Restoring..." -ForegroundColor "Green"
& dotnet msbuild /t:Restore /nologo /m /v:m "/p:Configuration=Release" "/p:PackageVersionSuffix=$PreReleaseSuffix" # Restore all packages
if ($LASTEXITCODE -ne 0)
{
    throw "restore failed with exit code $LASTEXITCODE"
}

if ($PreReleaseSuffix) {
    & dotnet build -c Release --version-suffix "$PreReleaseSuffix"
} else {
    & dotnet build -c Release
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
        & dotnet test "$_" -c Release --no-build
    }
}

# Package all
if ($PreReleaseSuffix) {
    & dotnet pack -c Release -o "$packageOutputFolder" --no-build --version-suffix "$PreReleaseSuffix"
} else {
    & dotnet pack -c Release -o "$packageOutputFolder" --no-build
}