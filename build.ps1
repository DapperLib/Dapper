[CmdletBinding(PositionalBinding=$false)]
param(
    [bool] $CreatePackages,
    [bool] $RunTests = $true,
    [string] $PullRequestNumber
)

Write-Host "Run Parameters:" -ForegroundColor Cyan
Write-Host "  CreatePackages: $CreatePackages"
Write-Host "  RunTests: $RunTests"
Write-Host "  dotnet --version:" (dotnet --version)

$packageOutputFolder = "$PSScriptRoot\.nupkgs"

if ($PullRequestNumber) {
    Write-Host "Building for a pull request (#$PullRequestNumber), skipping packaging." -ForegroundColor Yellow
    $CreatePackages = $false
}

Write-Host "Building all projects (Build.csproj traversal)..." -ForegroundColor "Magenta"
dotnet build ".\Build.csproj" -c Release /p:CI=true
Write-Host "Done building." -ForegroundColor "Green"

if ($RunTests) {
    Write-Host "Running tests: Build.csproj traversal (all frameworks)" -ForegroundColor "Magenta"
    dotnet test ".\Build.csproj" -c Release --no-build --logger trx
    if ($LastExitCode -ne 0) {
        Write-Host "Error with tests, aborting build." -Foreground "Red"
        Exit 1
    }
    Write-Host "Tests passed!" -ForegroundColor "Green"
}

if ($CreatePackages) {
    New-Item -ItemType Directory -Path $packageOutputFolder -Force | Out-Null
    Write-Host "Clearing existing $packageOutputFolder..." -NoNewline
    Get-ChildItem $packageOutputFolder | Remove-Item
    Write-Host "done." -ForegroundColor "Green"

    Write-Host "Building all packages" -ForegroundColor "Green"
    dotnet pack ".\Build.csproj" --no-build -c Release /p:PackageOutputPath=$packageOutputFolder /p:CI=true
}
Write-Host "Build Complete." -ForegroundColor "Green"
