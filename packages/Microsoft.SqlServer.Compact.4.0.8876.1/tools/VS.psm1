function Get-VsFileSystem {
    $componentModel = Get-VSComponentModel
    $fileSystemProvider = $componentModel.GetService([NuGet.VisualStudio.IFileSystemProvider])
    $solutionManager = $componentModel.GetService([NuGet.VisualStudio.ISolutionManager])
    
    $fileSystem = $fileSystemProvider.GetFileSystem($solutionManager.SolutionDirectory)
    
    return $fileSystem
}

function Add-PostBuildEvent ($project, $installPath) {
    $currentPostBuildCmd = $project.Properties.Item("PostBuildEvent").Value
    $sqlCEPostBuildCmd = Get-PostBuildCommand $installPath
    # Append our post build command if it's not already there
    if (!$currentPostBuildCmd.Contains($sqlCEPostBuildCmd)) {
        $project.Properties.Item("PostBuildEvent").Value += $SqlCEPostBuildCmd
    }
}

function Add-FilesToDirectory ($srcDirectory, $destDirectory) {
    ls $srcDirectory -Recurse -Filter *.dll  | %{
        $srcPath = $_.FullName

        $relativePath = $srcPath.Substring($srcDirectory.Length + 1)
        $destPath = Join-Path $destDirectory $relativePath
        
        $fileSystem = Get-VsFileSystem
        if (!(Test-Path $destPath)) {
            $fileStream = $null
            try {
                $fileStream = [System.IO.File]::OpenRead($_.FullName)
                $fileSystem.AddFile($destPath, $fileStream)
            } catch {
                # We don't want an exception to surface if we can't add the file for some reason
            } finally {
                if ($fileStream -ne $null) {
                    $fileStream.Dispose()
                }
            }
        }
    }
}

function Remove-FilesFromDirectory ($srcDirectory, $destDirectory) {
    $fileSystem = Get-VsFileSystem
    
    ls $srcDirectory -Recurse -Filter *.dll | %{
        $relativePath = $_.FullName.Substring($srcDirectory.Length + 1)
        $fileInBin = Join-Path $destDirectory $relativePath
        if ($fileSystem.FileExists($fileInBin) -and ((Get-Item $fileInBin).Length -eq $_.Length)) {
            # If a corresponding file exists in bin and has the exact file size as the one inside the package, it's most likely the same file.
            try {
                $fileSystem.DeleteFile($fileInBin)
            } catch {
                # We don't want an exception to surface if we can't delete the file
            }
        }
    }
}

function Remove-PostBuildEvent ($project, $installPath) {
    $sqlCEPostBuildCmd = Get-PostBuildCommand $installPath
    
    try {
        # Get the current Post Build Event cmd
        $currentPostBuildCmd = $project.Properties.Item("PostBuildEvent").Value

        # Remove our post build command from it (if it's there)
        $project.Properties.Item("PostBuildEvent").Value = $currentPostBuildCmd.Replace($SqlCEPostBuildCmd, '')
    } catch {
        # Accessing $project.Properties might throw
    }
}

function Get-PostBuildCommand ($installPath) {
    Write-Host $dte.Solution.FullName $installPath
    $solutionDir = [IO.Path]::GetDirectoryName($dte.Solution.FullName) + "\"
    $path = $installPath.Replace($solutionDir, "`$(SolutionDir)")

    $NativeAssembliesDir = Join-Path $path "NativeBinaries"
    $x86 = $(Join-Path $NativeAssembliesDir "x86\*.*")
    $x64 = $(Join-Path $NativeAssembliesDir "amd64\*.*")

    return "
    if not exist `"`$(TargetDir)x86`" md `"`$(TargetDir)x86`"
    xcopy /s /y `"$x86`" `"`$(TargetDir)x86`"
    if not exist `"`$(TargetDir)amd64`" md `"`$(TargetDir)amd64`"
    xcopy /s /y `"$x64`" `"`$(TargetDir)amd64`""
}

function Get-ProjectRoot($project) {
    try {
        $project.Properties.Item("FullPath").Value
    } catch {

    }
}

function Get-ChildProjectItem($parent, $name) {
	try {
		return $parent.ProjectItems.Item($name);
	} catch {
	
	}
}

function Ensure-Folder($parent, $name) {
	$item = Get-ChildProjectItem $parent $name
	if(!$item) {
		$item = (Get-Interface $parent.ProjectItems "EnvDTE.ProjectItems").AddFolder($name)
	}
	return $item;
}

function Remove-Child($parent, $name) {
	$item = Get-ChildProjectItem $parent $name
	if($item) {
		(Get-Interface $item "EnvDTE.ProjectItem").Delete()
	}
}

function Remove-EmptyFolder($item) {
	if($item.ProjectItems.Count -eq 0) {
		(Get-Interface $item "EnvDTE.ProjectItem").Delete()
	}
}

function Add-ProjectItem($item, $src, $itemtype = "None") {
	$newitem = (Get-Interface $item.ProjectItems "EnvDTE.ProjectItems").AddFromFileCopy($src)
	$newitem.Properties.Item("ItemType").Value = $itemtype
}

Export-ModuleMember -function Add-PostBuildEvent, Add-FilesToDirectory, Remove-PostBuildEvent, Remove-FilesFromDirectory, Get-ProjectRoot, Get-ChildProjectItem, Ensure-Folder, Remove-Child, Remove-EmptyFolder, Add-ProjectItem
# SIG # Begin signature block
# MIIaYAYJKoZIhvcNAQcCoIIaUTCCGk0CAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQU1qR1FcWhpAaK0NBKT/GqMhu8
# rkWgghUtMIIEoDCCA4igAwIBAgIKYRnMkwABAAAAZjANBgkqhkiG9w0BAQUFADB5
# MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVk
# bW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSMwIQYDVQQDExpN
# aWNyb3NvZnQgQ29kZSBTaWduaW5nIFBDQTAeFw0xMTEwMTAyMDMyMjVaFw0xMzAx
# MTAyMDMyMjVaMIGDMQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQ
# MA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9u
# MQ0wCwYDVQQLEwRNT1BSMR4wHAYDVQQDExVNaWNyb3NvZnQgQ29ycG9yYXRpb24w
# ggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDuW759ESTjhgbgZv9ItRe9
# AuS0DDLwcj59LofXTqGxp0Mv92WeMeEyMUWu18EkhCHXLrWEfvo101Mc17ZRHk/O
# ZrnrtwwC/SlcraiH9soitNW/CHX1inCPY9fvih7pj0MkZFrTh32QbTusds1XNn3o
# vBBWrJjwiV0uZMavJgleHmMV8T2/Fo+ZiALDMLfBC2AfD3LM1reoNRKGm6ELCuaT
# W476VJzB8xlfQo0Snx0/kLcnE4MZMoId89mH1CGyPKK2B0/XJKrujfWz2fr5OU+n
# 6fKvWVL03EGbLxFwY93q3qrxbSEEEFMzu7JPxeFTskFlR2439rzpmxZBkWsuWzDD
# AgMBAAGjggEdMIIBGTATBgNVHSUEDDAKBggrBgEFBQcDAzAdBgNVHQ4EFgQUG1IO
# 8xEqt8CJwxGBPdSWWLmjU24wDgYDVR0PAQH/BAQDAgeAMB8GA1UdIwQYMBaAFMsR
# 6MrStBZYAck3LjMWFrlMmgofMFYGA1UdHwRPME0wS6BJoEeGRWh0dHA6Ly9jcmwu
# bWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY0NvZFNpZ1BDQV8wOC0z
# MS0yMDEwLmNybDBaBggrBgEFBQcBAQROMEwwSgYIKwYBBQUHMAKGPmh0dHA6Ly93
# d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWljQ29kU2lnUENBXzA4LTMxLTIw
# MTAuY3J0MA0GCSqGSIb3DQEBBQUAA4IBAQClWzZsrU6baRLjb4oCm2l3w2xkciiI
# 2T1FbSwYe9QoLxPiWWobwgs0t4r96rmU7Acx5mr0dQTTp9peOgaeEP2pDb2cUUNv
# /2eUnOHPfPAksDXMg13u2sBvNknAWgpX9nPhnvPjCEw7Pi/M0s3uTyJw9wQfAqZL
# m7iPXIgONpRsMwe4qa1RoNDC3I4iEr3D34LXVqH33fClIFcQEJ3urIZ0bHGbwfDy
# wnBep9ttTTdYmU15QNA0XVolrmfrG05GBrCMKR+jEI+lM58j1fi1Rn3g7mOYkEs+
# BagvsBizWaSvQVOOCAUQLSrJOgZMHC6pMVFWZKyazKyXmCmKl5CH6p22MIIEujCC
# A6KgAwIBAgIKYQUTNgAAAAAAGjANBgkqhkiG9w0BAQUFADB3MQswCQYDVQQGEwJV
# UzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UE
# ChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEwHwYDVQQDExhNaWNyb3NvZnQgVGlt
# ZS1TdGFtcCBQQ0EwHhcNMTEwNzI1MjA0MjE3WhcNMTIxMDI1MjA0MjE3WjCBszEL
# MAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1v
# bmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjENMAsGA1UECxMETU9Q
# UjEnMCUGA1UECxMebkNpcGhlciBEU0UgRVNOOjE1OUMtQTNGNy0yNTcwMSUwIwYD
# VQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBTZXJ2aWNlMIIBIjANBgkqhkiG9w0B
# AQEFAAOCAQ8AMIIBCgKCAQEAnDSYGckJKWOZAhZ1qIhXfaG7qUES/GSRpdYFeL93
# 3OzmrrhQTsDjGr3tt/34IIpxOapyknKfignlE++RQe1hJWtRre6oQ7VhQiyd8h2x
# 0vy39Xujc3YTsyuj25RhgFWhD23d2OwW/4V/lp6IfwAujnokumidj8bK9JB5euGb
# 7wZdfvguw2oVnDwUL+fVlMgiG1HLqVWGIbda80ESOZ/wValOqiUrY/uRcjwPfMCW
# ctzBo8EIyt7FybXACl+lnAuqcgpdCkB9LpjQq7KIj4aA6H3RvlVr4FgsyDY/+eYR
# w/BDBYV4AxflLKcpfNPilRcAbNvcrTwZOgLgfWLUzvYdPQIDAQABo4IBCTCCAQUw
# HQYDVR0OBBYEFPaDiyCHEe6Dy9vehaLSaIY3YXSQMB8GA1UdIwQYMBaAFCM0+NlS
# RnAK7UD7dvuzK7DDNbMPMFQGA1UdHwRNMEswSaBHoEWGQ2h0dHA6Ly9jcmwubWlj
# cm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY3Jvc29mdFRpbWVTdGFtcFBD
# QS5jcmwwWAYIKwYBBQUHAQEETDBKMEgGCCsGAQUFBzAChjxodHRwOi8vd3d3Lm1p
# Y3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY3Jvc29mdFRpbWVTdGFtcFBDQS5jcnQw
# EwYDVR0lBAwwCgYIKwYBBQUHAwgwDQYJKoZIhvcNAQEFBQADggEBAGL0BQ1P5xtr
# gudSDN95jKhVgTOX06TKyf6vSNt72m96KE/H0LeJ2NGmmcyRVgA7OOi3Mi/u+c9r
# 2Zje1gL1QlhSa47aQNwWoLPUvyYVy0hCzNP9tPrkRIlmD0IOXvcEnyNIW7SJQcTa
# bPg29D/CHhXfmEwAxLLs3l8BAUOcuELWIsiTmp7JpRhn/EeEHpFdm/J297GOch2A
# djw2EUbKfjpI86/jSfYXM427AGOCnFejVqfDbpCjPpW3/GTRXRjCCwFQY6f889GA
# noTjMjTdV5VAo21+2usuWgi0EAZeMskJ6TKCcRan+savZpiJ+dmetV8QI6N3gPJN
# 1igAclCFvOUwggW8MIIDpKADAgECAgphMyYaAAAAAAAxMA0GCSqGSIb3DQEBBQUA
# MF8xEzARBgoJkiaJk/IsZAEZFgNjb20xGTAXBgoJkiaJk/IsZAEZFgltaWNyb3Nv
# ZnQxLTArBgNVBAMTJE1pY3Jvc29mdCBSb290IENlcnRpZmljYXRlIEF1dGhvcml0
# eTAeFw0xMDA4MzEyMjE5MzJaFw0yMDA4MzEyMjI5MzJaMHkxCzAJBgNVBAYTAlVT
# MRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQK
# ExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xIzAhBgNVBAMTGk1pY3Jvc29mdCBDb2Rl
# IFNpZ25pbmcgUENBMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAsnJZ
# XBkwZL8dmmAgIEKZdlNsPhvWb8zL8epr/pcWEODfOnSDGrcvoDLs/97CQk4j1XIA
# 2zVXConKriBJ9PBorE1LjaW9eUtxm0cH2v0l3511iM+qc0R/14Hb873yNqTJXEXc
# r6094CholxqnpXJzVvEXlOT9NZRyoNZ2Xx53RYOFOBbQc1sFumdSjaWyaS/aGQv+
# knQp4nYvVN0UMFn40o1i/cvJX0YxULknE+RAMM9yKRAoIsc3Tj2gMj2QzaE4BoVc
# TlaCKCoFMrdL109j59ItYvFFPeesCAD2RqGe0VuMJlPoeqpK8kbPNzw4nrR3XKUX
# no3LEY9WPMGsCV8D0wIDAQABo4IBXjCCAVowDwYDVR0TAQH/BAUwAwEB/zAdBgNV
# HQ4EFgQUyxHoytK0FlgByTcuMxYWuUyaCh8wCwYDVR0PBAQDAgGGMBIGCSsGAQQB
# gjcVAQQFAgMBAAEwIwYJKwYBBAGCNxUCBBYEFP3RMU7TJoqV4ZhgO6gxb6Y8vNgt
# MBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMB8GA1UdIwQYMBaAFA6sgmBAVieX
# 5SUT/CrhClOVWeSkMFAGA1UdHwRJMEcwRaBDoEGGP2h0dHA6Ly9jcmwubWljcm9z
# b2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL21pY3Jvc29mdHJvb3RjZXJ0LmNybDBU
# BggrBgEFBQcBAQRIMEYwRAYIKwYBBQUHMAKGOGh0dHA6Ly93d3cubWljcm9zb2Z0
# LmNvbS9wa2kvY2VydHMvTWljcm9zb2Z0Um9vdENlcnQuY3J0MA0GCSqGSIb3DQEB
# BQUAA4ICAQBZOT5/Jkav629AsTK1ausOL26oSffrX3XtTDst10OtC/7L6S0xoyPM
# fFCYgCFdrD0vTLqiqFac43C7uLT4ebVJcvc+6kF/yuEMF2nLpZwgLfoLUMRWzS3j
# StK8cOeoDaIDpVbguIpLV/KVQpzx8+/u44YfNDy4VprwUyOFKqSCHJPilAcd8uJO
# +IyhyugTpZFOyBvSj3KVKnFtmxr4HPBT1mfMIv9cHc2ijL0nsnljVkSiUc356aNY
# Vt2bAkVEL1/02q7UgjJu/KSVE+Traeepoiy+yCsQDmWOmdv1ovoSJgllOJTxeh9K
# u9HhVujQeJYYXMk1Fl/dkx1Jji2+rTREHO4QFRoAXd01WyHOmMcJ7oUOjE9tDhNO
# PXwpSJxy0fNsysHscKNXkld9lI2gG0gDWvfPo2cKdKU27S0vF8jmcjcS9G+xPGeC
# +VKyjTMWZR4Oit0Q3mT0b85G1NMX6XnEBLTT+yzfH4qerAr7EydAreT54al/RrsH
# YEdlYEBOsELsTu2zdnnYCjQJbRyAMR/iDlTd5aH75UcQrWSY/1AWLny/BSF64pVB
# J2nDk4+VyY3YmyGuDVyc8KKuhmiDDGotu3ZrAB2WrfIWe/YWgyS5iM9qqEcxL5rc
# 43E91wB+YkfRzojJuBj6DnKNwaM9rwJAav9pm5biEKgQtDdQCNbDPTCCBgcwggPv
# oAMCAQICCmEWaDQAAAAAABwwDQYJKoZIhvcNAQEFBQAwXzETMBEGCgmSJomT8ixk
# ARkWA2NvbTEZMBcGCgmSJomT8ixkARkWCW1pY3Jvc29mdDEtMCsGA1UEAxMkTWlj
# cm9zb2Z0IFJvb3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5MB4XDTA3MDQwMzEyNTMw
# OVoXDTIxMDQwMzEzMDMwOVowdzELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjEhMB8GA1UEAxMYTWljcm9zb2Z0IFRpbWUtU3RhbXAgUENBMIIBIjAN
# BgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAn6Fssd/bSJIqfGsuGeG94uPFmVEj
# UK3O3RhOJA/u0afRTK10MCAR6wfVVJUVSZQbQpKumFwwJtoAa+h7veyJBw/3DgSY
# 8InMH8szJIed8vRnHCz8e+eIHernTqOhwSNTyo36Rc8J0F6v0LBCBKL5pmyTZ9co
# 3EZTsIbQ5ShGLieshk9VUgzkAyz7apCQMG6H81kwnfp+1pez6CGXfvjSE/MIt1Nt
# UrRFkJ9IAEpHZhEnKWaol+TTBoFKovmEpxFHFAmCn4TtVXj+AZodUAiFABAwRu23
# 3iNGu8QtVJ+vHnhBMXfMm987g5OhYQK1HQ2x/PebsgHOIktU//kFw8IgCwIDAQAB
# o4IBqzCCAacwDwYDVR0TAQH/BAUwAwEB/zAdBgNVHQ4EFgQUIzT42VJGcArtQPt2
# +7MrsMM1sw8wCwYDVR0PBAQDAgGGMBAGCSsGAQQBgjcVAQQDAgEAMIGYBgNVHSME
# gZAwgY2AFA6sgmBAVieX5SUT/CrhClOVWeSkoWOkYTBfMRMwEQYKCZImiZPyLGQB
# GRYDY29tMRkwFwYKCZImiZPyLGQBGRYJbWljcm9zb2Z0MS0wKwYDVQQDEyRNaWNy
# b3NvZnQgUm9vdCBDZXJ0aWZpY2F0ZSBBdXRob3JpdHmCEHmtFqFKoKWtTHNY9AcT
# LmUwUAYDVR0fBEkwRzBFoEOgQYY/aHR0cDovL2NybC5taWNyb3NvZnQuY29tL3Br
# aS9jcmwvcHJvZHVjdHMvbWljcm9zb2Z0cm9vdGNlcnQuY3JsMFQGCCsGAQUFBwEB
# BEgwRjBEBggrBgEFBQcwAoY4aHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraS9j
# ZXJ0cy9NaWNyb3NvZnRSb290Q2VydC5jcnQwEwYDVR0lBAwwCgYIKwYBBQUHAwgw
# DQYJKoZIhvcNAQEFBQADggIBABCXisNcA0Q23em0rXfbznlRTQGxLnRxW20ME6vO
# vnuPuC7UEqKMbWK4VwLLTiATUJndekDiV7uvWJoc4R0Bhqy7ePKL0Ow7Ae7ivo8K
# BciNSOLwUxXdT6uS5OeNatWAweaU8gYvhQPpkSokInD79vzkeJkuDfcH4nC8GE6d
# jmsKcpW4oTmcZy3FUQ7qYlw/FpiLID/iBxoy+cwxSnYxPStyC8jqcD3/hQoT38IK
# YY7w17gX606Lf8U1K16jv+u8fQtCe9RTciHuMMq7eGVcWwEXChQO0toUmPU8uWZY
# sy0v5/mFhsxRVuidcJRsrDlM1PZ5v6oYemIp76KbKTQGdxpiyT0ebR+C8AvHLLvP
# Q7Pl+ex9teOkqHQ1uE7FcSMSJnYLPFKMcVpGQxS8s7OwTWfIn0L/gHkhgJ4VMGbo
# QhJeGsieIiHQQ+kr6bv0SMws1NgygEwmKkgkX1rqVu+m3pmdyjpvvYEndAYR7nYh
# v5uCwSdUtrFqPYmhdmG0bqETpr+qR/ASb/2KMmyy/t9RyIwjyWa9nR2HEmQCPS2v
# WY+45CHltbDKY7R4VAXUQS5QrJSwpXirs6CWdRrZkocTdSIvMqgIbqBbjCW/oO+E
# yiHW6x5PyZruSeD3AWVviQt9yGnI5m7qp5fOMSn/DsVbXNhNG6HY+i+ePy5VFmvJ
# E6P9MYIEnTCCBJkCAQEwgYcweTELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjEjMCEGA1UEAxMaTWljcm9zb2Z0IENvZGUgU2lnbmluZyBQQ0ECCmEZ
# zJMAAQAAAGYwCQYFKw4DAhoFAKCByjAZBgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIB
# BDAcBgorBgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAjBgkqhkiG9w0BCQQxFgQU
# 3wFC+NxHvXnIrl0/2K+O4SXKXKQwagYKKwYBBAGCNwIBDDFcMFqgOIA2AE0AaQBj
# AHIAbwBzAG8AZgB0ACAAQQBTAFAALgBOAEUAVAAgAFcAZQBiACAAUABhAGcAZQBz
# oR6AHGh0dHA6Ly93d3cuYXNwLm5ldC93ZWJtYXRyaXgwDQYJKoZIhvcNAQEBBQAE
# ggEAz8PRGRssNn9wrGQuIlxPVC5+NIxBSDJTVuT8z+ZUh166s7D8qU0DEuByCw7S
# Z+TKZYAM8mnCG1RGPGo4WPD4R3PWBFTObdPpwP25pkqhLxBy6zTVU8q+iPSo/xNg
# KFMdY41yPnwSff3k+Os7Vl1GZVOcrORrw+iVpIB4b14PY5+e4esGFd8yoHf+bJ9T
# iG+Z4/6llYMLNClpA26HDpA0XJqPKZrZSmAgiRRdkSLIGRKg8UqD2Fi2QpOvYM+C
# pQCUoC5X2wOjIDnq5Q/Mf9P+tPFawDAbLlALL9xssamedRugt5Em34Py3bJeYXuW
# JqZX+rL+NoRBJEMXo7ppnJA5YKGCAh0wggIZBgkqhkiG9w0BCQYxggIKMIICBgIB
# ATCBhTB3MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UE
# BxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSEwHwYD
# VQQDExhNaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0ECCmEFEzYAAAAAABowBwYFKw4D
# AhqgXTAYBgkqhkiG9w0BCQMxCwYJKoZIhvcNAQcBMBwGCSqGSIb3DQEJBTEPFw0x
# MjA1MDIyMzMzMjFaMCMGCSqGSIb3DQEJBDEWBBT/f8GnHFPOgw+3JqpQkl902xGy
# gzANBgkqhkiG9w0BAQUFAASCAQBTzF+Oxk9EWWKUtrw/zfFhpB7N5E84ugrd1+Pi
# nP8Hn8TkmwHMBIZIys9ckO2fYkcY/CkMrSGoC4WpyplVm5siPH28c2Ty8OyI4VM7
# OTxxAGs0ll9jpAPfL/y/P+N+DgGxU3rcUAL20XA+XPz8uY2lm0LW0sqxDn8B00oA
# 8/6YAsIVx2QwCECDGnNw5E33gTvbHKZQSge2g7gN97PvO5gCq0cGbKn4YJYkinJj
# T3daIhuAW7ik2IRlHwaKd6P+B8Frq8wqT61OiUx4DWBQ0W964c6J5CKcBEbR5H/+
# UPSB6RPSVSmZa4M4+qK4ot/uasmsmsMFuzqSqUlhZtzfCosg
# SIG # End signature block
