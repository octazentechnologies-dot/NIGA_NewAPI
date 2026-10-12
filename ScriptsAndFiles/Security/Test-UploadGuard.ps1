<#
.SYNOPSIS
  Live check of upload screening (content signature, executable/script block, antivirus) on the API.
  Uses POST /api/SecureDocument/Upload as the signed-in doctor. The EICAR string is the industry-standard,
  harmless antivirus test file; it is assembled in memory and never written to disk.
  Exit code = number of failed checks.
#>
param(
    [string]$NewBase = 'http://127.0.0.1:5002/api',
    [string]$UserName = 'Tufan_Doctor',
    [string]$Password = $(if ($env:NIGA_TEST_PASSWORD) { $env:NIGA_TEST_PASSWORD } else { '123456' })
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$http = New-Object System.Net.Http.HttpClient
$http.Timeout = [TimeSpan]::FromSeconds(120)

$login = $http.PostAsync("$NewBase/Account/Login", (New-Object System.Net.Http.StringContent ((@{ userName = $UserName; password = $Password } | ConvertTo-Json)), ([Text.Encoding]::UTF8), 'application/json')).GetAwaiter().GetResult()
$body = $login.Content.ReadAsStringAsync().GetAwaiter().GetResult()
$token = [regex]::Match($body, '"(?:token|accessToken)"\s*:\s*"(eyJ[^"]+)"').Groups[1].Value
$userId = [regex]::Match($body, '"userId"\s*:\s*(\d+)', 'IgnoreCase').Groups[1].Value
if (-not $token) { throw "Login failed (HTTP $([int]$login.StatusCode))" }
$http.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue 'Bearer', $token

function Upload([string]$name, [byte[]]$bytes, [string]$contentType) {
    $form = New-Object System.Net.Http.MultipartFormDataContent
    $file = New-Object System.Net.Http.ByteArrayContent (, $bytes)
    $file.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse($contentType)
    $form.Add($file, 'file', $name)
    $form.Add((New-Object System.Net.Http.StringContent 'User'), 'ownerType')
    $form.Add((New-Object System.Net.Http.StringContent $userId), 'ownerId')
    $res = $http.PostAsync("$NewBase/SecureDocument/Upload", $form).GetAwaiter().GetResult()
    return [pscustomobject]@{ Status = [int]$res.StatusCode; Body = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult() }
}

$png = [byte[]](0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0,0,0,0x0D,0x49,0x48,0x44,0x52,0,0,0,1,0,0,0,1,8,6,0,0,0,0x1F,0x15,0xC4,0x89,0,0,0,0x0A,0x49,0x44,0x41,0x54,0x78,0x9C,0x63,0,1,0,0,5,0,1,0x0D,0x0A,0x2D,0xB4,0,0,0,0,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82)
$exe = [byte[]](@(0x4D,0x5A,0x90,0,3,0,0,0,4,0,0,0,0xFF,0xFF,0,0) + (,0 * 112))
$eicar = [Text.Encoding]::ASCII.GetBytes(('X5O!P%@AP[4\PZX54(P^)7CC)7}' + '$EICAR-STANDARD-ANTIVIRUS' + '-TEST-FILE!$H+H*'))

$checks = @(
    @{ Name = 'real PNG is accepted';                 File = 'scan.png';    Bytes = $png;   Type = 'image/png';  Ok = { param($s) $s -ge 200 -and $s -lt 300 } }
    @{ Name = 'Windows .exe renamed to .jpg';         File = 'invoice.jpg'; Bytes = $exe;   Type = 'image/jpeg'; Ok = { param($s) $s -in 415, 422 } }
    @{ Name = 'PNG content with .pdf extension';      File = 'report.pdf';  Bytes = $png;   Type = 'application/pdf'; Ok = { param($s) $s -in 415, 422 } }
    @{ Name = '.php file';                            File = 'shell.php';   Bytes = [Text.Encoding]::ASCII.GetBytes('<?php echo 1; ?>'); Type = 'text/plain'; Ok = { param($s) $s -in 415, 422 } }
    @{ Name = 'HTML script disguised as .txt';        File = 'notes.txt';   Bytes = [Text.Encoding]::ASCII.GetBytes('<html><script>alert(1)</script>'); Type = 'text/plain'; Ok = { param($s) $s -in 415, 422 } }
    @{ Name = 'EICAR antivirus test file as .txt';    File = 'notes2.txt';  Bytes = $eicar; Type = 'text/plain'; Ok = { param($s) $s -in 422, 503 } }
)

$failed = 0
foreach ($c in $checks) {
    $r = Upload $c.File $c.Bytes $c.Type
    $pass = & $c.Ok $r.Status
    if (-not $pass) { $failed++ }
    $code = [regex]::Match($r.Body, '"code"\s*:\s*"([^"]+)"').Groups[1].Value
    "{0}  {1,-36} HTTP {2} {3}" -f $(if ($pass) { 'PASS' } else { 'FAIL' }), $c.Name, $r.Status, $code
}
"Failed: $failed"
exit $failed
