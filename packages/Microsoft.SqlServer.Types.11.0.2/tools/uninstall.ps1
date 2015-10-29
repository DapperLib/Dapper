param($installPath, $toolsPath, $package, $project)

$sqlServerTypes = $project.ProjectItems | where Name -eq "SqlServerTypes"
if($sqlServerTypes)
{
    $folderx86 = $sqlServerTypes.ProjectItems | where Name -eq "x86"
    if ($folderx86)
    {
		$cppFilex86 = $folderx86.ProjectItems | where Name -eq "msvcr100.dll"
        if($cppFilex86)
        {
            $cppFilex86.Delete();
        }

        $sqlFilex86 = $folderx86.ProjectItems | where Name -eq "SqlServerSpatial110.dll"
        if($sqlFilex86)
        {
            $sqlFilex86.Delete();
        }

        if($folderx86.ProjectItems.Count -eq 0)
        {
            $folderx86.Delete()
        }
    }

    $folderx64 = $sqlServerTypes.ProjectItems | where Name -eq "x64"
    if ($folderx64)
    {
		$cppFilex64 = $folderx64.ProjectItems | where Name -eq "msvcr100.dll"
        if($cppFilex64)
        {
            $cppFilex64.Delete();
        }

        $sqlFilex64 = $folderx64.ProjectItems | where Name -eq "SqlServerSpatial110.dll"
        if($sqlFilex64)
        {
            $sqlFilex64.Delete();
        }

        if($folderx64.ProjectItems.Count -eq 0)
        {
            $folderx64.Delete()
        }
    }

    if($sqlServerTypes.ProjectItems.Count -eq 0)
    {
        $sqlServerTypes.Delete()
    }
}