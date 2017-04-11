param(
    [parameter(Position=0)][string] $PreReleaseSuffix = '',
    [switch] $SkipTests = $false
)

$packageOutputFolder = "$PSScriptRoot\.nupkgs"

# Restore packages and build product
Write-Host "Building..." -ForegroundColor "Green"
dotnet msbuild "$PSScriptRoot\Dapper.sln" /m /v:m /nologo "/t:Restore;Build" /p:Configuration=Debug "/p:PackageVersionSuffix=$PreReleaseSuffix"

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
dotnet msbuild "$PSScriptRoot\Dapper.sln" /m /v:m /nologo "/t:Restore;Build;Pack" /p:Configuration=Release "/p:PackageOutputPath=$packageOutputFolder" "/p:PackageVersionSuffix=$PreReleaseSuffix"