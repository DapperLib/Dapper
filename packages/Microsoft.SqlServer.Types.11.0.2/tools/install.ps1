param($installPath, $toolsPath, $package, $project)

$packagePath = (New-Object system.IO.DirectoryInfo $toolsPath).Parent.FullName
$cppBinaryPathx86 = Join-Path $packagePath "nativeBinaries\x86\msvcr100.dll"
$cppBinaryPathx64 = Join-Path $packagePath "nativeBinaries\x64\msvcr100.dll"
$sqlBinaryPathx86 = Join-Path $packagePath "nativeBinaries\x86\SqlServerSpatial110.dll"
$sqlBinaryPathx64 = Join-Path $packagePath "nativeBinaries\x64\SqlServerSpatial110.dll"

$sqlServerTypes = $project.ProjectItems.Item("SqlServerTypes")

$folderx86 = $sqlServerTypes.ProjectItems | where Name -eq "x86"
if (!$folderx86)
{
    $folderx86 = $sqlServerTypes.ProjectItems.AddFolder("x86")
}

$folderx64 = $sqlServerTypes.ProjectItems | where Name -eq "x64"
if (!$folderx64)
{
    $folderx64 = $sqlServerTypes.ProjectItems.AddFolder("x64")
}

$cppLinkx86 = $folderx86.ProjectItems | where Name -eq "msvcr100.dll"
if (!$cppLinkx86)
{
    $cppLinkx86 = $folderx86.ProjectItems.AddFromFile($cppBinaryPathx86)
    $cppLinkx86.Properties.Item("CopyToOutputDirectory").Value = 2
}

$sqlLinkx86 = $folderx86.ProjectItems | where Name -eq "SqlServerSpatial110.dll"
if (!$sqlLinkx86)
{
    $sqlLinkx86 = $folderx86.ProjectItems.AddFromFile($sqlBinaryPathx86)
    $sqlLinkx86.Properties.Item("CopyToOutputDirectory").Value = 2
}

$cppLinkx64 = $folderx64.ProjectItems | where Name -eq "msvcr100.dll"
if (!$cppLinkx64)
{
    $cppLinkx64 = $folderx64.ProjectItems.AddFromFile($cppBinaryPathx64)
    $cppLinkx64.Properties.Item("CopyToOutputDirectory").Value = 2
}

$sqlLinkx64 = $folderx64.ProjectItems | where Name -eq "SqlServerSpatial110.dll"
if (!$sqlLinkx64)
{
    $sqlLinkx64 = $folderx64.ProjectItems.AddFromFile($sqlBinaryPathx64)
    $sqlLinkx64.Properties.Item("CopyToOutputDirectory").Value = 2
}

$readmefile = Join-Path (Split-Path $project.FileName) "SqlServerTypes\readme.htm"
$dte.ItemOperations.Navigate($readmefile)