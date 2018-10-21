#!/usr/bin/env pwsh
[CmdletBinding(PositionalBinding = $false)]
param(
    [string]
    $Output = "$PSScriptRoot/../artifacts/",
    [ValidateSet("Debug", "Release")]
    $Configuration = "Debug"
)

Set-StrictMode -Version 1
$ErrorActionPreference = 'Stop'

function exec([string]$_cmd) {
    Write-Host ">>> $_cmd $args" -ForegroundColor Cyan
    $ErrorActionPreference = 'Continue'
    & $_cmd @args
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed with exit code $LASTEXITCODE"
        exit 1
    }
}

[string[]] $MSBuildArgs = @("-p:Configuration=$Configuration")

Remove-Item -Recurse $Output -ErrorAction Ignore

exec dotnet build @MSBuildArgs

exec dotnet pack `
    --no-build `
    -o $Output @MSBuildArgs `
    "./src/DockerWatch/DockerWatch.csproj"

exec dotnet publish `
    -r win-x64 @MSBuildArgs `
    "./src/DockerWatch/DockerWatch.csproj"

Write-Host 'Done' -ForegroundColor Magenta
