# Runs the Unity Editor without a window (-batchmode) on the self-hosted Windows runner (2026-11-27).
# Unity Personal is already activated on this computer, so this costs nothing. The runner uses its own copy of the repo,
# so Yaroslav's open Editor (another folder) is not affected.
#
# Examples (from the repo root):
#   powershell -File tools/unity/unity.ps1 -Method AgentTools.Ping
#   powershell -File tools/unity/unity.ps1 -Method AgentTools.CapturePlay -Extra "-agentOut","build/shots/play.png","-agentWait","8"
#   powershell -File tools/unity/unity.ps1 -Tests EditMode
# The first run in a new copy imports the project (5-20 minutes). Later runs reuse unity/Library.
param(
    [string]$Method = "",
    [string[]]$Extra = @(),
    [string]$Tests = "",
    [int]$TimeoutMinutes = 40
)
$ErrorActionPreference = "Stop"
$unity = $env:UNITY_EXE
if (-not $unity) { $unity = "C:\Program Files\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe" }
$root = (git rev-parse --show-toplevel).Trim()
$project = Join-Path $root "unity"
$logDir = Join-Path $root "build\unity"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$log = Join-Path $logDir "unity-$stamp.log"

$uargs = @("-batchmode", "-projectPath", "`"$project`"", "-logFile", "`"$log`"")
if ($Tests) {
    $results = Join-Path $logDir "tests-$stamp.xml"
    $uargs += @("-runTests", "-testPlatform", $Tests, "-testResults", "`"$results`"")
} elseif ($Method) {
    $uargs += @("-executeMethod", $Method)
} else {
    Write-Error "Give -Method or -Tests."
}
$uargs += $Extra

Write-Host "Unity: $unity"
Write-Host "Args:  $($uargs -join ' ')"
$p = Start-Process -FilePath $unity -ArgumentList $uargs -PassThru -NoNewWindow
$null = $p.Handle
if (-not $p.WaitForExit($TimeoutMinutes * 60 * 1000)) {
    $p.Kill()
    Write-Host "Unity did not finish in $TimeoutMinutes minutes."
    exit 2
}
$code = $p.ExitCode
if ($null -eq $code) { Write-Error "Unity did not report an exit code."; exit 1 }
Write-Host "Exit code: $code. Log: $log"
if (Test-Path $log) {
    Get-Content $log | Select-String -Pattern "\[AgentTools\]|error CS|Exception|Test run|Passed|Failed" | Select-Object -Last 40 | ForEach-Object { $_.Line }
}
if ($Tests -and (Test-Path $results)) { Write-Host "Test results: $results" }
exit $code
