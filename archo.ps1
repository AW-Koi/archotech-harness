# Builds the CLI and the harness mod on first use (or with $env:ARCHO_REBUILD set), then runs archo.
$root = $PSScriptRoot
$exe = Join-Path $root '.bin/cli/archo.exe'
if (-not (Test-Path $exe) -or -not (Test-Path (Join-Path $root 'mod/1.7/Assemblies/ArchotechHarness.dll')) -or $env:ARCHO_REBUILD) {
    dotnet build (Join-Path $root 'Source/ArchotechHarness/ArchotechHarness.csproj') -c Release --nologo -v q | Out-Host
    dotnet build (Join-Path $root 'Source/ArchotechHarness.Cli/ArchotechHarness.Cli.csproj') -c Release -o (Join-Path $root '.bin/cli') --nologo -v q | Out-Host
}
& $exe @args
exit $LASTEXITCODE
