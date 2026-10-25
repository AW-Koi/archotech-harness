# Builds the CLI and the harness mod on first use (or with $env:RWH_REBUILD set), then runs rwh.
$root = $PSScriptRoot
$exe = Join-Path $root '.bin/cli/rwh.exe'
if (-not (Test-Path $exe) -or -not (Test-Path (Join-Path $root 'mod/1.7/Assemblies/DevHarness.dll')) -or $env:RWH_REBUILD) {
    dotnet build (Join-Path $root 'Source/DevHarness/DevHarness.csproj') -c Release --nologo -v q | Out-Host
    dotnet build (Join-Path $root 'Source/DevHarness.Cli/DevHarness.Cli.csproj') -c Release -o (Join-Path $root '.bin/cli') --nologo -v q | Out-Host
}
& $exe @args
exit $LASTEXITCODE
