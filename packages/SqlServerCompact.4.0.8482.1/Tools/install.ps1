param($installPath, $toolsPath, $package, $project)

. (Join-Path $toolsPath "GetSqlCEPostBuildCmd.ps1")

# Get the current Post Build Event cmd
$currentPostBuildCmd = $project.Properties.Item("PostBuildEvent").Value

# Append our post build command if it's not already there
if (!$currentPostBuildCmd.Contains($SqlCEPostBuildCmd)) {
    $project.Properties.Item("PostBuildEvent").Value += $SqlCEPostBuildCmd
}
