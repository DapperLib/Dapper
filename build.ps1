param(
    [parameter(Position=0)][string] $PreReleaseSuffix = ''
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
Get-ChildItem "Dapper*.csproj" -Recurse |
ForEach-Object {
    if ($PreReleaseSuffix) {
        & dotnet build "$_" --version-suffix "$PreReleaseSuffix"
    } else {
        & dotnet build "$_"
    }
}

# Run tests
Write-Host "Running Tests..." -ForegroundColor "Green"
Get-ChildItem "Dapper.Test*.csproj" -Recurse |
ForEach-Object {
    & dotnet test "$_"
}

# Package all
Write-Host "Packaging..." -ForegroundColor "Green"
Get-ChildItem "Dapper*.csproj" -Recurse | Where-Object { $_.Name -NotLike "*.Tests*" } |
ForEach-Object {
    if ($PreReleaseSuffix) {
        & dotnet pack "$_" -c Release -o "$packageOutputFolder" --version-suffix "$PreReleaseSuffix" /p:NuGetBuildTasksPackTargets="000"
    } else {
        & dotnet pack "$_" -c Release -o "$packageOutputFolder" /p:NuGetBuildTasksPackTargets="000" 
    }
}