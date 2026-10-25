# Builds the CLI and the harness mod when their sources changed since the last build (or with $env:ARCHO_REBUILD set), then runs archo.
$root = $PSScriptRoot
$exe = Join-Path $root '.bin/cli/archo.exe'
$mod = Join-Path $root 'mod/1.7/Assemblies/ArchotechHarness.dll'

# True when the stamp is missing or any file under the paths, outside bin/ and obj/, is newer.
function Test-Stale([string]$Stamp, [string[]]$Paths) {
    if (-not (Test-Path $Stamp)) { return $true }
    $since = [IO.File]::GetLastWriteTimeUtc($Stamp)
    $pending = [Collections.Generic.Stack[string]]::new()
    foreach ($path in $Paths) {
        if ([IO.File]::Exists($path)) {
            if ([IO.File]::GetLastWriteTimeUtc($path) -gt $since) { return $true }
        }
        elseif ([IO.Directory]::Exists($path)) { $pending.Push($path) }
    }
    while ($pending.Count -gt 0) {
        $directory = $pending.Pop()
        foreach ($file in [IO.Directory]::EnumerateFiles($directory)) {
            if ([IO.File]::GetLastWriteTimeUtc($file) -gt $since) { return $true }
        }
        foreach ($child in [IO.Directory]::EnumerateDirectories($directory)) {
            if ([IO.Path]::GetFileName($child) -notin 'bin', 'obj') { $pending.Push($child) }
        }
    }
    return $false
}

# Builds a project, then records the time the build started in its stamp.
function Invoke-Build([string]$Stamp, [string[]]$Arguments) {
    New-Item -ItemType Directory -Force (Join-Path $root '.bin') | Out-Null
    Set-Content -Path "$Stamp.new" -Value '' -Encoding utf8
    dotnet build @Arguments -c Release --nologo -v q | Out-Host
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Move-Item -Force "$Stamp.new" $Stamp
}

$modStamp = Join-Path $root '.bin/mod.stamp'
$modSources = 'Source/ArchotechHarness', 'Source/Shared', 'build/FindRimWorld.props', 'Directory.Build.props' | ForEach-Object { Join-Path $root $_ }
if ($env:ARCHO_REBUILD -or -not (Test-Path $mod) -or (Test-Stale $modStamp $modSources)) {
    Invoke-Build $modStamp @((Join-Path $root 'Source/ArchotechHarness/ArchotechHarness.csproj'))
}

$cliStamp = Join-Path $root '.bin/cli.stamp'
$cliSources = 'Source/ArchotechHarness.Cli', 'Source/Shared', 'Directory.Build.props' | ForEach-Object { Join-Path $root $_ }
if ($env:ARCHO_REBUILD -or -not (Test-Path $exe) -or (Test-Stale $cliStamp $cliSources)) {
    Invoke-Build $cliStamp @((Join-Path $root 'Source/ArchotechHarness.Cli/ArchotechHarness.Cli.csproj'), '-o', (Join-Path $root '.bin/cli'))
}

& $exe @args
exit $LASTEXITCODE
