<#
.SYNOPSIS
  Runs the OWASP ZAP API scan (zap-staging-plan.yaml) against the STAGING New API and Old API.

  Needs Docker. Uses the official ghcr.io/zaproxy/zaproxy:stable image.
  Refuses production hosts. Run only against a staging database that can be restored.

  Credentials come from environment variables, never from the command line:
    NIGA_ZAP_USER / NIGA_ZAP_PASSWORD        staging test doctor used for the bearer token
    NIGA_SWAGGER_USER / NIGA_SWAGGER_PASSWORD SwaggerAuth user, to download swagger.json
  Tokens are passed to the container by environment variable name only and are never printed.

  Reports: <OutDir>\reports\new-api.html|.sarif and old-api.html|.sarif. Exit code = number of
  scans that returned a non-zero ZAP status (ZAP exits 1 on errors, 2 on warnings).

.EXAMPLE
  $env:NIGA_ZAP_USER='Tufan_Doctor2'; $env:NIGA_ZAP_PASSWORD='...'
  $env:NIGA_SWAGGER_USER='...'; $env:NIGA_SWAGGER_PASSWORD='...'
  .\Run-ZapStaging.ps1 -NewApiBase https://stage-newapi.example -OldApiBase https://stage-oldapi.example
#>
param(
    [Parameter(Mandatory)] [string]$NewApiBase,
    [Parameter(Mandatory)] [string]$OldApiBase,
    [string]$NewOpenApiFile = '',
    [string]$OldOpenApiFile = '',
    [string]$OutDir = (Join-Path $PSScriptRoot ("run-" + (Get-Date -Format 'yyyyMMdd-HHmm'))),
    [string]$Image = 'ghcr.io/zaproxy/zaproxy:stable'
)

$ErrorActionPreference = 'Stop'
$productionHosts = @('nigaui.homeocentrum.com', 'homeocentrum.com', 'www.homeocentrum.com')
foreach ($u in $NewApiBase, $OldApiBase) {
    $h = ([Uri]$u).Host.ToLowerInvariant()
    if ($productionHosts -contains $h) { throw "Refusing to scan production host $h." }
}
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Docker is required (or install ZAP and run zap.bat -cmd -autorun with the same plan).' }
foreach ($v in 'NIGA_ZAP_USER', 'NIGA_ZAP_PASSWORD') {
    if (-not [Environment]::GetEnvironmentVariable($v)) { throw "Set $v first." }
}

New-Item -ItemType Directory -Force -Path (Join-Path $OutDir 'reports') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'zap-staging-plan.yaml') $OutDir -Force
Add-Type -AssemblyName System.Net.Http

function Get-OpenApi([string]$base, [string]$dest) {
    if (-not $env:NIGA_SWAGGER_USER -or -not $env:NIGA_SWAGGER_PASSWORD) { throw 'Set NIGA_SWAGGER_USER and NIGA_SWAGGER_PASSWORD, or pass -NewOpenApiFile/-OldOpenApiFile.' }
    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.CookieContainer = New-Object System.Net.CookieContainer
    $handler.AllowAutoRedirect = $false
    $client = New-Object System.Net.Http.HttpClient $handler
    $form = New-Object 'System.Collections.Generic.Dictionary[string,string]'
    $form['username'] = $env:NIGA_SWAGGER_USER
    $form['password'] = $env:NIGA_SWAGGER_PASSWORD
    $client.PostAsync("$base/swagger-login", (New-Object System.Net.Http.FormUrlEncodedContent $form)).GetAwaiter().GetResult() | Out-Null
    $res = $client.GetAsync("$base/swagger/v1/swagger.json").GetAwaiter().GetResult()
    if ([int]$res.StatusCode -ne 200) { throw "Could not download swagger.json from $base (HTTP $([int]$res.StatusCode))." }
    [IO.File]::WriteAllText($dest, $res.Content.ReadAsStringAsync().GetAwaiter().GetResult())
}

function Get-Token([string]$base) {
    $body = @{ userName = $env:NIGA_ZAP_USER; password = $env:NIGA_ZAP_PASSWORD } | ConvertTo-Json -Compress
    $r = Invoke-WebRequest "$base/api/Account/Login" -Method Post -Body $body -ContentType 'application/json' -UseBasicParsing
    $m = [regex]::Match($r.Content, '"(?:token|accessToken)"\s*:\s*"(eyJ[^"]+)"', 'IgnoreCase')
    if (-not $m.Success) { throw "Login failed on $base." }
    return $m.Groups[1].Value
}

$targets = @(
    @{ Name = 'new-api'; Base = $NewApiBase.TrimEnd('/'); Spec = $NewOpenApiFile },
    @{ Name = 'old-api'; Base = $OldApiBase.TrimEnd('/'); Spec = $OldOpenApiFile }
)
$failed = 0
foreach ($t in $targets) {
    $specName = "$($t.Name)-openapi.json"
    $specPath = Join-Path $OutDir $specName
    if ($t.Spec) { Copy-Item $t.Spec $specPath -Force } else { Get-OpenApi $t.Base $specPath }

    $env:ZAP_AUTH_HEADER_VALUE = 'Bearer ' + (Get-Token $t.Base)
    $env:ZAP_AUTH_HEADER_SITE = ([Uri]$t.Base).Host
    $env:TARGET_URL = $t.Base
    $env:OPENAPI_FILE = "/zap/wrk/$specName"
    $env:REPORT_NAME = $t.Name
    try {
        Write-Output "Scanning $($t.Name) at $($t.Base) ..."
        & docker run --rm -v "${OutDir}:/zap/wrk:rw" `
            -e ZAP_AUTH_HEADER_VALUE -e ZAP_AUTH_HEADER_SITE -e TARGET_URL -e OPENAPI_FILE -e REPORT_NAME `
            $Image zap.sh -cmd -autorun /zap/wrk/zap-staging-plan.yaml
        Write-Output "$($t.Name): ZAP exit $LASTEXITCODE"
        if ($LASTEXITCODE -ne 0) { $failed++ }
    }
    finally {
        Remove-Item Env:ZAP_AUTH_HEADER_VALUE, Env:ZAP_AUTH_HEADER_SITE -ErrorAction SilentlyContinue
    }
}
Write-Output "Reports: $(Join-Path $OutDir 'reports')"
exit $failed
