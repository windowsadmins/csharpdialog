# csharpDialog Launcher Script
# This script provides a convenient way to run csharpDialog while bypassing executable restrictions

[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments=$true, ValueFromPipeline=$false)]
    [string[]]$Arguments = @()
)

$dllPath = Join-Path $PSScriptRoot "src\CsharpDialog.CLI\bin\Debug\net9.0\CsharpDialog.CLI.dll"

if (Test-Path $dllPath) {
    if ($Arguments.Count -gt 0) {
        & dotnet $dllPath @Arguments
    } else {
        & dotnet $dllPath
    }
} else {
    Write-Error "csharpDialog CLI not found. Please run 'dotnet build' first."
    exit 1
}
