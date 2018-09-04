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
$projectsToBuild =
    'Dapper',
    'Dapper.StrongName',
    'Dapper.Contrib',
    'Dapper.EntityFramework',
    'Dapper.EntityFramework.StrongName',
    'Dapper.Rainbow',
    'Dapper.SqlBuilder'

$testsToRun =
    'Dapper.Tests',
    'Dapper.Tests.Contrib'

if ($PullRequestNumber) {
    Write-Host "Building for a pull request (#$PullRequestNumber), skipping packaging." -ForegroundColor Yellow
    $CreatePackages = $false
}

Write-Host "Building projects..." -ForegroundColor "Magenta"
foreach ($project in $projectsToBuild + $testsToRun) {
    Write-Host "Building $project (dotnet restore/build)..." -ForegroundColor "Magenta"
    dotnet restore ".\$project\$project.csproj" /p:CI=true
    dotnet build ".\$project\$project.csproj" -c Release /p:CI=true
    Write-Host ""
}
Write-Host "Done building." -ForegroundColor "Green"

if ($RunTests) {
    dotnet restore /ConsoleLoggerParameters:Verbosity=Quiet
    foreach ($project in $testsToRun) {
        Write-Host "Running tests: $project (all frameworks)" -ForegroundColor "Magenta"
        Push-Location ".\$project"

        dotnet test -c Release
        if ($LastExitCode -ne 0) {
            Write-Host "Error with tests, aborting build." -Foreground "Red"
            Pop-Location
            Exit 1
        }

        Write-Host "Tests passed!" -ForegroundColor "Green"
	    Pop-Location
    }
}

if ($CreatePackages) {
    mkdir -Force $packageOutputFolder | Out-Null
    Write-Host "Clearing existing $packageOutputFolder..." -NoNewline
    Get-ChildItem $packageOutputFolder | Remove-Item
    Write-Host "done." -ForegroundColor "Green"

    Write-Host "Building all packages" -ForegroundColor "Green"

    foreach ($project in $projectsToBuild) {
        Write-Host "Packing $project (dotnet pack)..." -ForegroundColor "Magenta"
        dotnet pack ".\$project\$project.csproj" --no-build -c Release /p:PackageOutputPath=$packageOutputFolder /p:NoPackageAnalysis=true /p:CI=true
        Write-Host ""
    }
}
Write-Host "Build Complete." -ForegroundColor "Green"
