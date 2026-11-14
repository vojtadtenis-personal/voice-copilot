[CmdletBinding()]
param(
    [ValidateSet('sdk-intercept', 'asr', 'ui-local', 'ui-canary')]
    [string[]] $Case = @('ui-local'),
    [string] $Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$assets = (New-Item -ItemType Directory -Force '.\.assets').FullName
$env:DOTNET_CLI_HOME = (New-Item -ItemType Directory -Force "$assets\dotnet-home").FullName
$env:NUGET_PACKAGES = (New-Item -ItemType Directory -Force "$assets\nuget").FullName
$env:TEMP = (New-Item -ItemType Directory -Force "$assets\runtime-temp").FullName
$env:TMP = $env:TEMP
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
if ([IO.Path]::IsPathFullyQualified($Dotnet)) { $env:DOTNET_ROOT = Split-Path $Dotnet }
& $Dotnet build '.\D02.Feasibility.csproj' --verbosity quiet -p:RestoreLockedMode=true
if ($LASTEXITCODE -ne 0) { throw 'Diagnostic build failed.' }
foreach ($name in $Case) {
    & $Dotnet '.\bin\Debug\net10.0-windows\D02.Feasibility.dll' $name |
        Tee-Object -FilePath "$assets\$name.log"
    if ($LASTEXITCODE -ne 0) { throw "Diagnostic $name failed; see sanitized log." }
}
