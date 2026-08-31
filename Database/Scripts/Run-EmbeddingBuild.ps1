<#
.SYNOPSIS
    Builds enterprise rubric + concept embeddings without Cloudflare 524 timeouts.

.DESCRIPTION
    Option A (recommended): Run ON THE SAME PC WHERE cloudflared + API run.
    api1.homeocentrum.com tunnel -> http://localhost:5001 — call that port directly.
    Use -UseLocalhost (bypasses Cloudflare tunnel and 524 timeouts).

    Option B: Resume loop via public URL. Each call may 524, but incremental mode
    skips already-embedded rows; re-run until SQL counts stop growing.

.PARAMETER BaseUrl
    Public API URL, e.g. https://api1.homeocentrum.com

.PARAMETER Token
    JWT from POST /api/Account/Login (Bearer token only, no "Bearer " prefix)

.PARAMETER UseLocalhost
    Bypass Cloudflare: call 127.0.0.1 with Host header matching your site.

.PARAMETER LocalHostHeader
    IIS site hostname when using -UseLocalhost (default: api1.homeocentrum.com)

.PARAMETER LocalPort
    Local Kestrel port from cloudflared ingress (api1 = 5001, api = 5000).

.PARAMETER ResumeLoop
    For public URL only: repeat build calls until no new rows (handles 524).

.EXAMPLE
    # Best: run on server PC, RDP or local PowerShell
    .\Run-EmbeddingBuild.ps1 -UseLocalhost -Token "eyJ..."

.EXAMPLE
    # From any PC through public URL (slow, many retries)
    .\Run-EmbeddingBuild.ps1 -BaseUrl "https://api1.homeocentrum.com" -Token "eyJ..." -ResumeLoop
#>

param(
    [string]$BaseUrl = "https://api1.homeocentrum.com",
    [Parameter(Mandatory = $true)]
    [string]$Token,
    [switch]$UseLocalhost,
    [string]$LocalHostHeader = "api1.homeocentrum.com",
    [int]$LocalPort = 5001,
    [switch]$ResumeLoop,
    [switch]$RubricsOnly,
    [switch]$ConceptsOnly,
    [int]$MaxResumeAttempts = 200
)

$ErrorActionPreference = "Continue"

function Get-ApiRoot {
    if ($UseLocalhost) {
        $scheme = if ($LocalPort -eq 443) { "https" } else { "http" }
        return "$scheme://127.0.0.1:$LocalPort"
    }
    return $BaseUrl.TrimEnd("/")
}

function Get-RequestHeaders {
    $h = @{
        Authorization  = "Bearer $Token"
        "Content-Type" = "application/json"
    }
    if ($UseLocalhost) {
        $h["Host"] = $LocalHostHeader
    }
    return $h
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Body = $null,
        [int]$TimeoutSec = 7200
    )

    $uri = "$(Get-ApiRoot)$Path"
    $headers = Get-RequestHeaders

    try {
        if ($Body) {
            return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -Body $Body -TimeoutSec $TimeoutSec
        }
        return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -TimeoutSec $TimeoutSec
    }
    catch {
        $msg = $_.Exception.Message
        if ($_.ErrorDetails.Message) { $msg += " | $($_.ErrorDetails.Message)" }
        return [pscustomobject]@{ _error = $msg; _statusCode = $_.Exception.Response.StatusCode.value__ }
    }
}

function Show-Status {
    param([string]$Label)
    Write-Host ""
    Write-Host "=== $Label ===" -ForegroundColor Cyan
    $s = Invoke-Api -Method Get -Path "/api/AiEmbeddingInfrastructure/status" -TimeoutSec 60
    if ($s._error) {
        Write-Host "Status failed: $($s._error)" -ForegroundColor Red
        return $null
    }
    $r = $s.resultObject
    Write-Host "runningJobs=$($r.runningJobs) failed24h=$($r.failedJobsLast24Hours) pending=$($r.pendingQueueItems)"
    if ($r.currentVersion) {
        Write-Host "version=$($r.currentVersion.versionCode) id=$($r.currentVersion.embeddingVersionId)"
    }
    return $s
}

function Invoke-RubricBuild {
    param([bool]$FullReindex)

    $body = @{
        fullReindex   = $FullReindex
        triggerSource = "Run-EmbeddingBuild.ps1"
    } | ConvertTo-Json

    Write-Host ""
    Write-Host "POST rubrics/build (fullReindex=$FullReindex) ..." -ForegroundColor Yellow
    $resp = Invoke-Api -Method Post -Path "/api/AiEmbeddingInfrastructure/rubrics/build" -Body $body
    if ($resp._error) {
        Write-Host "Rubrics: $($resp._error)" -ForegroundColor DarkYellow
        return $false
    }
    if ($resp.success -eq $false) {
        Write-Host "Rubrics failed: $($resp.message)" -ForegroundColor Red
        return $false
    }
    $d = $resp.resultObject
    Write-Host "Rubrics OK: processed=$($d.processed) created=$($d.created) updated=$($d.updated) skipped=$($d.skipped) failed=$($d.failed)" -ForegroundColor Green
    return $true
}

function Invoke-ConceptBuild {
    $body = @{ triggerSource = "Run-EmbeddingBuild.ps1" } | ConvertTo-Json

    Write-Host ""
    Write-Host "POST concepts/build ..." -ForegroundColor Yellow
    $resp = Invoke-Api -Method Post -Path "/api/AiEmbeddingInfrastructure/concepts/build" -Body $body
    if ($resp._error) {
        Write-Host "Concepts: $($resp._error)" -ForegroundColor DarkYellow
        return $false
    }
    if ($resp.success -eq $false) {
        Write-Host "Concepts failed: $($resp.message)" -ForegroundColor Red
        return $false
    }
    $d = $resp.resultObject
    Write-Host "Concepts OK: processed=$($d.processed) created=$($d.created) updated=$($d.updated) skipped=$($d.skipped) failed=$($d.failed)" -ForegroundColor Green
    return $true
}

function Wait-RunningJobs {
    param([int]$MaxMinutes = 180)

    $deadline = (Get-Date).AddMinutes($MaxMinutes)
    while ((Get-Date) -lt $deadline) {
        $s = Invoke-Api -Method Get -Path "/api/AiEmbeddingInfrastructure/status" -TimeoutSec 60
        if ($s._error) { Start-Sleep -Seconds 15; continue }
        $n = [int]$s.resultObject.runningJobs
        if ($n -le 0) { return $true }
        Write-Host "  waiting... runningJobs=$n ($(Get-Date -Format 'HH:mm:ss'))"
        Start-Sleep -Seconds 30
    }
    Write-Host "Timed out waiting for runningJobs=0" -ForegroundColor Red
    return $false
}

# --- main ---
Write-Host "API root: $(Get-ApiRoot)" -ForegroundColor Cyan
if ($UseLocalhost) {
    Write-Host "Mode: LOCALHOST bypass (Host: $LocalHostHeader, port: $LocalPort)" -ForegroundColor Green
}
elseif ($ResumeLoop) {
    Write-Host "Mode: PUBLIC URL resume loop (524 errors expected)" -ForegroundColor DarkYellow
}
else {
    Write-Host "Mode: single long request (may 524 via Cloudflare)" -ForegroundColor DarkYellow
}

Show-Status "Before"

if (-not $ConceptsOnly) {
    if ($ResumeLoop -and -not $UseLocalhost) {
        # First pass: full reindex once if nothing embedded yet; then incremental resume.
        $first = $true
        for ($i = 1; $i -le $MaxResumeAttempts; $i++) {
            Write-Host ""
            Write-Host "--- Rubric attempt $i / $MaxResumeAttempts ---" -ForegroundColor Cyan
            [void](Invoke-RubricBuild -FullReindex:$first)
            $first = $false
            [void](Wait-RunningJobs -MaxMinutes 30)
            $st = Show-Status "After rubric attempt $i"
            if ($st -and [int]$st.resultObject.runningJobs -eq 0) {
                # If last attempt succeeded with no 524, we are likely done.
                # User should confirm AIRubricEmbedding count in SQL.
                break
            }
            Start-Sleep -Seconds 10
        }
    }
    else {
        [void](Invoke-RubricBuild -FullReindex:$true)
        if (-not $UseLocalhost) { [void](Wait-RunningJobs) }
    }
}

if (-not $RubricsOnly) {
    if ($ResumeLoop -and -not $UseLocalhost) {
        for ($i = 1; $i -le $MaxResumeAttempts; $i++) {
            Write-Host ""
            Write-Host "--- Concept attempt $i / $MaxResumeAttempts ---" -ForegroundColor Cyan
            $ok = Invoke-ConceptBuild
            [void](Wait-RunningJobs -MaxMinutes 30)
            Show-Status "After concept attempt $i"
            if ($ok) { break }
            Start-Sleep -Seconds 10
        }
    }
    else {
        [void](Invoke-ConceptBuild)
        if (-not $UseLocalhost) { [void](Wait-RunningJobs) }
    }
}

Show-Status "Done"

Write-Host ""
Write-Host "Verify in SQL:" -ForegroundColor Cyan
Write-Host @"
SELECT 'AIRubricEmbedding' AS T, COUNT(*) AS C FROM AIRubricEmbedding
UNION ALL SELECT 'AIConceptEmbedding', COUNT(*) FROM AIConceptEmbedding;
"@
