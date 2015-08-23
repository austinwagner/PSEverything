$ErrorActionPreference = "Stop"

$releaseDir = "$PSScriptRoot\Release\PSEverything"

Task Configure {
    $moduleInfo = [IO.File]::ReadAllText("$PSScriptRoot\TemplateParameters.ps1") | Invoke-Expression
    Function Replace-Template($inFile, $outFile) {
        Write-Host "Generating from template"
        Write-Host "  Source: $inFile"
        Write-Host "  Destination: $outFile"
        Write-Host
        New-Item -Force -Path $outFile -Type File | Out-Null
        Get-Content -Path $inFile | 
            % { $_ -replace '__NAME__',$moduleInfo["Name"] } |
            % { $_ -replace '__DESCRIPTION__',$moduleInfo["Description"] } |
            % { $_ -replace '__COMPANY__',$moduleInfo["Company"] } |
            % { $_ -replace '__COPYRIGHT__',$moduleInfo["Copyright"] } |
            % { $_ -replace '__VERSION__',$moduleInfo["Version"] } |
            Set-Content -Force -Path $outFile 
    }

    $templates = "$PSScriptRoot\Templates"
    Replace-Template "$templates\AssemblyInfo.cs" "$PSScriptRoot\PSEverything\Properties\AssemblyInfo.cs"
    Replace-Template "$templates\PSEverything.psd1" "$PSScriptRoot\Module\PSEverything.psd1" 
}

Task Build -Depends Configure {
    $msbuildPath = Get-ChildItem -Path 'HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions' | 
        Select-Object -Property @{Name='Version';Expression={ [int]$_.PSChildName.Split('.')[0] }},PSPath,PSProvider | 
        Sort-Object -Descending -Property Version | 
        Select-Object -First 1 | 
        Get-ItemProperty -Name 'MSBuildToolsPath' |
        Select-Object -ExpandProperty 'MSBuildToolsPath'

    $msbuild = Join-Path $msbuildPath 'msbuild.exe'

    &$msbuild "$PSScriptRoot\PSEverything.sln" '/p:Configuration=Release'

    #Invoke-MsBuild -Path "$PSScriptRoot\PSEverything.sln" -MsBuildParameters '/p:Configuration=Release' | Out-Null

    New-Item -Path $releaseDir -Type Directory -ErrorAction SilentlyContinue | Out-Null

    Copy-Item -Force -Path "$PSScriptRoot\Module\*" -Destination $releaseDir | Out-Null
    Copy-Item -Force -Path "$PSScriptRoot\PSEverything\bin\Release\PSEverything.dll" -Destination $releaseDir | Out-Null
}

Task Install -Depends Build {
    Copy-Item -Force -Recurse -Path $releaseDir -Destination "$env:CommonProgramFiles\Modules"
}