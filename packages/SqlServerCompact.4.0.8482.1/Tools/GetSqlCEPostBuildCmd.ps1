$solutionDir = [System.IO.Path]::GetDirectoryName($dte.Solution.FullName) + "\"
$path = $installPath.Replace($solutionDir, "`$(SolutionDir)")

$NativeAssembliesDir = Join-Path $path "NativeBinaries"
$x86 = $(Join-Path $NativeAssembliesDir "x86\*.*")
$x64 = $(Join-Path $NativeAssembliesDir "amd64\*.*")

$SqlCEPostBuildCmd = "
if not exist `"`$(TargetDir)x86`" md `"`$(TargetDir)x86`"
xcopy /s /y `"$x86`" `"`$(TargetDir)x86`"
if not exist `"`$(TargetDir)amd64`" md `"`$(TargetDir)amd64`"
xcopy /s /y `"$x64`" `"`$(TargetDir)amd64`""