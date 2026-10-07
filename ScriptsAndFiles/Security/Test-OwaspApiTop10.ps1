<#
.SYNOPSIS
  Scripted OWASP API Security Top 10 (2023) probes against the New API (5002) and Old API (5001).

  This is a targeted, repeatable check of the controls built for each risk. It does not replace a
  full dynamic scan (OWASP ZAP / Burp) of staging; see ZAP\zap-staging-plan.yaml for that.

  API1 (object level authorization) is covered in depth by Test-CrossTenantAccess.ps1, which this
  script runs first unless -SkipIdorSuite is given.

  Forged-token checks need the New API signing key. It is read from -NewAppSettings by regex and is
  never printed. Tokens are never printed. Exit code = number of failed checks.

.EXAMPLE
  .\Test-OwaspApiTop10.ps1
  .\Test-OwaspApiTop10.ps1 -ReportPath .\owasp-report.md
#>
param(
    [string]$NewBase = 'http://127.0.0.1:5002',
    [string]$OldBase = 'http://127.0.0.1:5001',
    [string]$NewAppSettings = (Join-Path $PSScriptRoot '..\..\Homeocentrum.Niga.NewAPI\appsettings.json'),
    [string]$SqlServer = 'localhost\MSSQLSERVER25',
    [string]$Database = 'HomeoCentrum_Dev',
    [string]$Password = $(if ($env:NIGA_TEST_PASSWORD) { $env:NIGA_TEST_PASSWORD } else { '123456' }),
    [switch]$SkipIdorSuite,
    [string]$ReportPath = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $false
$http = New-Object System.Net.Http.HttpClient $handler
$http.Timeout = [TimeSpan]::FromSeconds(60)

$results = New-Object System.Collections.Generic.List[object]
function Record([string]$risk, [string]$check, [bool]$ok, [string]$detail, [switch]$Info) {
    $results.Add([pscustomobject]@{
        Result = if ($Info) { 'INFO' } elseif ($ok) { 'PASS' } else { 'FAIL' }
        Risk = $risk; Check = $check; Detail = $detail
    })
}

function Send([string]$method, [string]$url, [string]$token, [string]$json, [hashtable]$headers) {
    $req = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::new($method)), $url
    if ($token) { $req.Headers.TryAddWithoutValidation('Authorization', "Bearer $token") | Out-Null }
    if ($headers) { foreach ($k in $headers.Keys) { $req.Headers.TryAddWithoutValidation($k, [string]$headers[$k]) | Out-Null } }
    if ($json) { $req.Content = New-Object System.Net.Http.StringContent $json, ([Text.Encoding]::UTF8), 'application/json' }
    $res = $http.SendAsync($req).GetAwaiter().GetResult()
    $all = @{}
    foreach ($h in $res.Headers) { $all[$h.Key] = ($h.Value -join ',') }
    if ($res.Content) { foreach ($h in $res.Content.Headers) { $all[$h.Key] = ($h.Value -join ',') } }
    return [pscustomobject]@{ Status = [int]$res.StatusCode; Body = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult(); Headers = $all }
}

function ToJson($o) { $o | ConvertTo-Json -Depth 8 -Compress }

function Login([string]$base, [string]$user, [string]$pwd = $Password) {
    return Send 'POST' "$base/api/Account/Login" $null (ToJson @{ userName = $user; password = $pwd })
}

function TokenOf($response) {
    $m = [regex]::Match($response.Body, '"(?:token|accessToken)"\s*:\s*"(eyJ[^"]+)"', 'IgnoreCase')
    if (-not $m.Success) { throw "Login did not return a token (HTTP $($response.Status))" }
    return $m.Groups[1].Value
}

function B64U([byte[]]$bytes) { [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') }
function FromB64U([string]$s) {
    $s = $s.Replace('-', '+').Replace('_', '/')
    switch ($s.Length % 4) { 2 { $s += '==' } 3 { $s += '=' } }
    return [Convert]::FromBase64String($s)
}
function JwtParts([string]$jwt) {
    $p = $jwt.Split('.')
    return [pscustomobject]@{
        Header  = [Text.Encoding]::UTF8.GetString((FromB64U $p[0])) | ConvertFrom-Json
        Payload = [Text.Encoding]::UTF8.GetString((FromB64U $p[1])) | ConvertFrom-Json
        Sig     = $p[2]
    }
}
function SignJwt($header, $payload, [byte[]]$key) {
    $h = B64U ([Text.Encoding]::UTF8.GetBytes((ToJson $header)))
    $b = B64U ([Text.Encoding]::UTF8.GetBytes((ToJson $payload)))
    $mac = switch ($header.alg) {
        'HS512' { New-Object System.Security.Cryptography.HMACSHA512 (, $key) }
        'HS384' { New-Object System.Security.Cryptography.HMACSHA384 (, $key) }
        default { New-Object System.Security.Cryptography.HMACSHA256 (, $key) }
    }
    $sig = B64U ($mac.ComputeHash([Text.Encoding]::ASCII.GetBytes("$h.$b")))
    return "$h.$b.$sig"
}
function CopyPayload($payload) { $payload | ConvertTo-Json -Depth 8 | ConvertFrom-Json }
function UnixNow { [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() }

function Sql([string]$query) {
    $out = & sqlcmd -S $SqlServer -d $Database -E -C -h -1 -W -Q ("SET NOCOUNT ON; " + $query)
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed" }
    return ($out | Where-Object { $_ -and $_.Trim() } | Select-Object -First 1).Trim()
}

# ---------------------------------------------------------------- API1 Broken Object Level Authorization
if (-not $SkipIdorSuite) {
    $idor = Join-Path $PSScriptRoot 'Test-CrossTenantAccess.ps1'
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File $idor -NewBase "$NewBase/api" -OldBase "$OldBase/api" -SqlServer $SqlServer -Database $Database 2>&1
    $code = $LASTEXITCODE
    $summary = ($out | Select-String 'Checks:' | Select-Object -Last 1)
    Record 'API1' 'Cross-tenant suite (doctor/patient/reception/pharmacy, New + Old)' ($code -eq 0) ("$summary".Trim())
}

$doctorBUser = [long](Sql "SELECT UserId FROM UserMaster WHERE UserName='Tufan_Doctor2'")
$tok = @{}
foreach ($u in 'Tufan_Admin', 'Tufan_Doctor2', 'Tufan_Patient2', 'Tufan_Reception2') { $tok["New:$u"] = TokenOf (Login $NewBase $u) }
foreach ($u in 'Tufan_Admin', 'Tufan_Doctor2') { $tok["Old:$u"] = TokenOf (Login $OldBase $u) }

# ---------------------------------------------------------------- API2 Broken Authentication
foreach ($api in @(@{ Name = 'New'; Base = $NewBase }, @{ Name = 'Old'; Base = $OldBase })) {
    $n = $api.Name; $base = $api.Base
    $target = "$base/api/users/$doctorBUser"
    $real = $tok["${n}:Tufan_Doctor2"]

    $r = Send 'GET' $target $real
    Record 'API2' "$n control: valid token reads own user" ($r.Status -eq 200) "HTTP $($r.Status)"

    $r = Send 'GET' $target $null
    Record 'API2' "$n no token" ($r.Status -eq 401) "HTTP $($r.Status)"

    $r = Send 'GET' $target 'not.a.jwt'
    Record 'API2' "$n garbage token" ($r.Status -eq 401) "HTTP $($r.Status)"

    $parts = JwtParts $real
    $p = CopyPayload $parts.Payload
    $roleClaim = @($p.PSObject.Properties.Name | Where-Object { $_ -match 'role$' }) | Select-Object -First 1
    if (-not $roleClaim) { $roleClaim = 'role' }
    $p | Add-Member -NotePropertyName $roleClaim -NotePropertyValue 'Admin' -Force
    $tampered = (B64U ([Text.Encoding]::UTF8.GetBytes((ToJson $parts.Header)))) + '.' + (B64U ([Text.Encoding]::UTF8.GetBytes((ToJson $p)))) + '.' + $parts.Sig
    $r = Send 'GET' $target $tampered
    Record 'API2' "$n payload tampered to Admin, original signature" ($r.Status -eq 401) "HTTP $($r.Status)"

    $none = (B64U ([Text.Encoding]::UTF8.GetBytes('{"alg":"none","typ":"JWT"}'))) + '.' + (B64U ([Text.Encoding]::UTF8.GetBytes((ToJson $parts.Payload)))) + '.'
    $r = Send 'GET' $target $none
    Record 'API2' "$n alg=none token" ($r.Status -eq 401) "HTTP $($r.Status)"
}

$signingKey = $null
if (Test-Path $NewAppSettings) {
    $m = [regex]::Match([IO.File]::ReadAllText($NewAppSettings), '"TokenKey"\s*:\s*"([^"]+)"')
    if ($m.Success) { $signingKey = [Text.Encoding]::UTF8.GetBytes($m.Groups[1].Value) }
}
if ($signingKey) {
    $target = "$NewBase/api/users/$doctorBUser"
    $parts = JwtParts $tok['New:Tufan_Doctor2']
    $forge = {
        param([scriptblock]$mutate)
        $p = CopyPayload $parts.Payload
        & $mutate $p
        SignJwt $parts.Header $p $signingKey
    }
    $r = Send 'GET' $target (& $forge { param($p) })
    $controlOk = $r.Status -eq 200
    Record 'API2' 'New control: re-signed copy of a real token is accepted' $controlOk "HTTP $($r.Status) (proves the forged checks below are meaningful)"
    if ($controlOk) {
        $r = Send 'GET' $target (& $forge { param($p) $p.iss = 'https://attacker.example' })
        Record 'API2' 'New forged token with wrong issuer' ($r.Status -eq 401) "HTTP $($r.Status)"
        $r = Send 'GET' $target (& $forge { param($p) $p.aud = 'https://attacker.example' })
        Record 'API2' 'New forged token with wrong audience' ($r.Status -eq 401) "HTTP $($r.Status)"
        $r = Send 'GET' $target (& $forge { param($p) $p.PSObject.Properties.Remove('iss'); $p.PSObject.Properties.Remove('aud') })
        Record 'API2' 'New forged token without issuer/audience' ($r.Status -eq 401) "HTTP $($r.Status)"
        $r = Send 'GET' $target (& $forge { param($p) $now = UnixNow; $p.exp = $now - 900; if ($p.PSObject.Properties['nbf']) { $p.nbf = $now - 3600 }; if ($p.PSObject.Properties['iat']) { $p.iat = $now - 3600 } })
        Record 'API2' 'New expired token (15 min past expiry)' ($r.Status -eq 401) "HTTP $($r.Status)"
    }
    $r = Send 'GET' $target (SignJwt $parts.Header $parts.Payload ([Text.Encoding]::UTF8.GetBytes('wrong-key-wrong-key-wrong-key-wrong-key-wrong-key-wrong-key-1234')))
    Record 'API2' 'New token signed with a different key' ($r.Status -eq 401) "HTTP $($r.Status)"
}
else {
    Record 'API2' 'New forged issuer/audience/expiry tokens' $true 'Skipped: TokenKey not readable from appsettings' -Info
}

foreach ($api in @(@{ Name = 'New'; Base = $NewBase }, @{ Name = 'Old'; Base = $OldBase })) {
    $probeUser = 'owasp_probe_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
    $statuses = @()
    for ($i = 1; $i -le 6; $i++) { $statuses += (Login $api.Base $probeUser 'wrong-password').Status }
    $retry = (Login $api.Base $probeUser 'wrong-password').Headers['Retry-After']
    Record 'API2' "$($api.Name) brute force: 6 failed logins for one user name" ($statuses[5] -eq 429 -and $statuses[0] -ne 429) ("statuses " + ($statuses -join ',') + "; Retry-After $retry")

    $unknown = Login $api.Base ('owasp_nouser_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)) 'wrong-password'
    $known = Login $api.Base 'Tufan_Doctor2' 'wrong-password'
    $msg = { param($b) ([regex]::Match($b, '"message"\s*:\s*"([^"]*)"', 'IgnoreCase')).Groups[1].Value }
    $same = $unknown.Status -eq $known.Status -and (& $msg $unknown.Body) -eq (& $msg $known.Body)
    Record 'API2' "$($api.Name) no user enumeration: unknown user vs wrong password look the same" $same "HTTP $($unknown.Status) vs $($known.Status)"
    Login $api.Base 'Tufan_Doctor2' | Out-Null
}

# ---------------------------------------------------------------- API3 Broken Object Property Level Authorization
foreach ($n in 'New', 'Old') {
    $base = if ($n -eq 'New') { $NewBase } else { $OldBase }
    $r = Send 'GET' "$base/api/mastersAPI/GetFirmDetails" $tok["${n}:Tufan_Admin"]
    $leak = [regex]::IsMatch($r.Body, '"(mailPassword|firmConnectionPath|databaseBackupPath)"\s*:\s*"[^"]+"', 'IgnoreCase')
    Record 'API3' "$n firm details hide mail password, connection path, backup path" ($r.Status -eq 200 -and -not $leak) "HTTP $($r.Status)"

    $r = Send 'GET' "$base/api/users/$doctorBUser" $tok["${n}:Tufan_Doctor2"]
    $leak = [regex]::IsMatch($r.Body, '"(userPassword|password|passwordHash)"\s*:\s*"[^"]+"', 'IgnoreCase')
    Record 'API3' "$n user record has no password or hash" ($r.Status -eq 200 -and -not $leak) "HTTP $($r.Status)"

    $r = Login $base 'Tufan_Doctor2'
    $leak = [regex]::IsMatch($r.Body, '"(userPassword|password|passwordHash)"\s*:\s*"[^"]+"', 'IgnoreCase')
    Record 'API3' "$n login response has no password or hash" (-not $leak) "HTTP $($r.Status)"
}

# ---------------------------------------------------------------- API4 Unrestricted Resource Consumption
$deep = ('[' * 200) + (']' * 200)
foreach ($n in 'New', 'Old') {
    $base = if ($n -eq 'New') { $NewBase } else { $OldBase }
    $r = Send 'POST' "$base/api/Account/Login" $null ('{"userName":"x","password":"y","extra":' + $deep + '}')
    $ok = $r.Status -lt 500 -and $r.Body -notmatch '\bat [A-Z][\w\.]+\('
    Record 'API4' "$n JSON nested 200 deep is rejected safely" $ok "HTTP $($r.Status)" -Info:($n -eq 'Old' -and $ok -and $r.Status -lt 400)
}
Record 'API4' 'Login throttling (per user name and per IP)' $true 'See API2 brute force rows' -Info
Record 'API4' 'Upload size, type and antivirus limits' $true 'Covered by Test-UploadGuard.ps1 (413/415/422)' -Info

# ---------------------------------------------------------------- API5 Broken Function Level Authorization
$bfla = @(
    @('New', 'Tufan_Doctor2', 'GET', '/api/Admin/SecurityAudit/Verify', $null),
    @('New', 'Tufan_Reception2', 'GET', '/api/Admin/SecurityAudit/Verify', $null),
    @('New', 'Tufan_Patient2', 'GET', '/api/users', $null),
    @('New', 'Tufan_Doctor2', 'GET', '/api/users', $null),
    @('Old', 'Tufan_Doctor2', 'POST', '/api/NewsDetail/DeleteNewsDetails?newsDetailId=1', $null),
    @('Old', 'Tufan_Doctor2', 'POST', '/api/BlogDetail/DeleteBlogDetail?blogDetailId=1', $null)
)
foreach ($b in $bfla) {
    $base = if ($b[0] -eq 'New') { $NewBase } else { $OldBase }
    $r = Send $b[2] "$base$($b[3])" $tok["$($b[0]):$($b[1])"] $b[4]
    Record 'API5' "$($b[0]) $($b[1].Replace('Tufan_','')) cannot call admin $($b[2]) $($b[3])" ($r.Status -in 401, 403) "HTTP $($r.Status)"
}
$r = Send 'GET' "$NewBase/api/Admin/SecurityAudit/Verify" $tok['New:Tufan_Admin']
Record 'API5' 'New admin control: audit chain verify' ($r.Status -eq 200 -and $r.Body -match 'intact') "HTTP $($r.Status)"

# ---------------------------------------------------------------- API6 Unrestricted Access to Sensitive Business Flows
$fake = ToJson @{ PackageId = 1; OrderId = 'order_owaspprobe'; PaymentId = 'pay_owaspprobe'; TransactionId = ('0' * 64) }
$r = Send 'POST' "$NewBase/api/Subscription/SaveUpdateSubscription" $tok['New:Tufan_Doctor2'] $fake
Record 'API6' 'New subscription without a verified Razorpay payment is refused' ($r.Status -in 400, 402, 403, 409, 503) "HTTP $($r.Status)"
$r = Send 'POST' "$OldBase/api/Subscription/SaveUpdateSubscription" $tok['Old:Tufan_Doctor2'] $fake
Record 'API6' 'Old subscription save is admin only' ($r.Status -in 401, 403) "HTTP $($r.Status)"

# ---------------------------------------------------------------- API7 Server Side Request Forgery
Record 'API7' 'No endpoint fetches a caller-supplied URL' $true 'Code review: outbound calls go to fixed Razorpay, Meta and SMS/mail hosts from configuration' -Info

# ---------------------------------------------------------------- API8 Security Misconfiguration
foreach ($n in 'New', 'Old') {
    $base = if ($n -eq 'New') { $NewBase } else { $OldBase }
    $r = Send 'POST' "$base/api/Account/Login" $null '{"userName": "x", "password": '
    $h = $r.Headers
    Record 'API8' "$n security headers (nosniff, frame DENY)" ($h['X-Content-Type-Options'] -eq 'nosniff' -and $h['X-Frame-Options'] -eq 'DENY') "nosniff=$($h['X-Content-Type-Options']) frame=$($h['X-Frame-Options'])"
    Record 'API8' "$n no Server / X-Powered-By banner" (-not $h.ContainsKey('Server') -and -not $h.ContainsKey('X-Powered-By')) ("Server=" + $h['Server'] + " X-Powered-By=" + $h['X-Powered-By'])
    $leak = $r.Body -match '(\bat [A-Z][\w\.]+\(|Exception|StackTrace|SqlClient|\\Homeocentrum)'
    Record 'API8' "$n malformed JSON gives a clean error without internals" ($r.Status -lt 500 -and -not $leak) "HTTP $($r.Status)"

    # CORS is open to any origin by design (Bearer tokens, no cookies); the control is that credentials are never allowed.
    $pre = @{ Origin = 'https://any-client.example'; 'Access-Control-Request-Method' = 'GET'; 'Access-Control-Request-Headers' = 'authorization' }
    $r = Send 'OPTIONS' "$base/api/users/$doctorBUser" $null $null $pre
    Record 'API8' "$n CORS answers any origin (open by design)" ($r.Headers['Access-Control-Allow-Origin'] -eq 'https://any-client.example') ("ACAO=" + $r.Headers['Access-Control-Allow-Origin'])
    Record 'API8' "$n CORS never allows credentials" ($r.Headers['Access-Control-Allow-Credentials'] -ne 'true') ("ACAC=" + $r.Headers['Access-Control-Allow-Credentials'])
    $r = Send 'GET' "$base/api/users/$doctorBUser" $null $null @{ Origin = 'https://any-client.example' }
    Record 'API8' "$n cross-origin call without a token is still refused" ($r.Status -eq 401) "HTTP $($r.Status)"

    $r = Send 'TRACE' "$base/api/users/$doctorBUser" $null
    Record 'API8' "$n TRACE is not served" ($r.Status -ne 200) "HTTP $($r.Status)"

    $r = Send 'GET' "$base/swagger/v1/swagger.json" $null
    Record 'API8' "$n Swagger document needs sign-in" ($r.Status -ne 200) "HTTP $($r.Status)"
}

# ---------------------------------------------------------------- API9 Improper Inventory Management
$r = Send 'POST' "$OldBase/api/login/authenticate" $null (ToJson @{ userName = 'Tufan_Doctor2'; password = $Password })
Record 'API9' 'Old retired /api/login/authenticate is gone' ($r.Status -eq 410 -and $r.Body -notmatch 'eyJ') "HTTP $($r.Status)"
Record 'API9' 'Old API runs on ASP.NET Core 2.2 (end of life)' $false 'Migrate the Old API to a supported runtime; see the impact report' -Info

# ---------------------------------------------------------------- API10 Unsafe Consumption of APIs
$r = Send 'POST' "$NewBase/api/WhatsApp/Receipts" $null '{"entry":[]}'
Record 'API10' 'New WhatsApp webhook without Meta signature is refused' ($r.Status -in 401, 403, 503) "HTTP $($r.Status)"
$r = Send 'POST' "$NewBase/api/WhatsApp/Receipts" $null '{"entry":[]}' @{ 'X-Hub-Signature-256' = 'sha256=' + ('0' * 64) }
Record 'API10' 'New WhatsApp webhook with a wrong signature is refused' ($r.Status -in 401, 403, 503) "HTTP $($r.Status)"

# ---------------------------------------------------------------- report
$results | Format-Table -AutoSize -Wrap Result, Risk, Check, Detail | Out-String -Width 220 | Write-Output
$failed = @($results | Where-Object Result -eq 'FAIL').Count
$passed = @($results | Where-Object Result -eq 'PASS').Count
Write-Output ("Checks: {0}  Passed: {1}  Failed: {2}  Info: {3}" -f $results.Count, $passed, $failed, @($results | Where-Object Result -eq 'INFO').Count)

if ($ReportPath) {
    $md = New-Object System.Text.StringBuilder
    [void]$md.AppendLine("# OWASP API Security Top 10 probe - $(Get-Date -Format 'yyyy-MM-dd HH:mm')")
    [void]$md.AppendLine("")
    [void]$md.AppendLine("New API: $NewBase  |  Old API: $OldBase  |  Passed $passed, Failed $failed")
    [void]$md.AppendLine("")
    [void]$md.AppendLine("| Result | Risk | Check | Detail |")
    [void]$md.AppendLine("|---|---|---|---|")
    foreach ($x in $results) { [void]$md.AppendLine("| $($x.Result) | $($x.Risk) | $($x.Check) | $($x.Detail.Replace('|','/')) |") }
    [IO.File]::WriteAllText($ReportPath, $md.ToString(), (New-Object Text.UTF8Encoding $false))
}

exit $failed
