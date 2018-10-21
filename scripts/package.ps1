#!/usr/bin/env pwsh
[CmdletBinding(PositionalBinding = $false)]
param(
    [string]
    $Output = "$PSScriptRoot/../artifacts/",
    [ValidateSet("Debug", "Release")]
    $Configuration = "Debug"
)

Add-Type -assembly "System.IO.Compression.FileSystem"

$rids = @(
    "win-x64"
)

[xml]$manifest = Get-Content "$PSScriptRoot/../Version.props"
$version = "$($manifest.Project.PropertyGroup.VersionPrefix)"

$path = (Get-Location).Path

if ((Test-Path -Path "$Output") -ne $True) {
    Write-Host "Artifacts folder does not exist, creating it now ..." -ForegroundColor Cyan
    New-Item "$Output" -ItemType Directory | Out-Null
}

foreach ($id in $rids) {
    $zipFile = "$Output/docker-watch.$id.$version.zip"

    try {

        if (Test-Path -Path $zipFile) {
            Write-Host "Artifact '$zipfile' already exists, removing ..." -ForegroundColor Cyan
            Remove-Item -Path $zipFile
        }

        Write-Host "Creating $zipFile" -ForegroundColor Cyan

        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            "$path/.build/bin/DockerWatch/$Configuration/netcoreapp2.1/$id/publish",
            $zipFile
        )
    }
    catch {
        Write-Error "Something went wrong!"
        exit 1
    }
}

Write-Host 'Done' -ForegroundColor Magenta
