Param(
    $Version = "2024.3"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
Set-Location $PSScriptRoot

. ".\settings.ps1"

Invoke-Exe $MSBuildPath "/t:Restore;Rebuild;Pack" "$SolutionPath" "/v:minimal" "/p:PackageVersion=$Version" "/p:PackageOutputPath=`"$OutputDirectory`""
