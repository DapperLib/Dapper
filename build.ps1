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

# Build all
Write-Host "Building..." -ForegroundColor "Green"
msbuild "$PSScriptRoot\Dapper.sln" /m /v:m /nologo /t:Build /p:Configuration=Debug "/p:PackageVersionSuffix=$PreReleaseSuffix"

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
Write-Host "Packaging..." -ForegroundColor "Green"
msbuild "$PSScriptRoot\Dapper.sln" /m /v:m /nologo "/t:Build;PackWithFrameworkAssemblies" /p:Configuration=Release "/p:PackageOutputPath=$packageOutputFolder" "/p:PackageVersionSuffix=$PreReleaseSuffix"