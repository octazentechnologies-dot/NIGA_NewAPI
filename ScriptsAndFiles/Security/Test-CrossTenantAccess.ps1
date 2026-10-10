<#
.SYNOPSIS
  Cross-tenant (IDOR) regression tests for the API (5002) and Old API (5001).

  Proves that one doctor / patient / reception / pharmacy user cannot read or change another
  tenant's data, and that each user CAN still read their own (so a pass is not vacuous).

  Needs the test tenant from "S5_Week5\Database scripts\10_Security_Test_Tenant_LOCAL_ONLY.sql"
  (never run that on production). Ids are looked up from the database by user name.

  Tokens are never printed. Exit code = number of failed checks.

.EXAMPLE
  .\Test-CrossTenantAccess.ps1
  .\Test-CrossTenantAccess.ps1 -NewBase https://staging/new-api/api -OldBase https://staging/old-api/api -SqlServer stagingdb -Database HomeoCentrum
#>
param(
    [string]$NewBase = 'http://127.0.0.1:5002/api',
    [string]$OldBase = 'http://127.0.0.1:5001/api',
    [string]$SqlServer = 'localhost\MSSQLSERVER25',
    [string]$Database = 'HomeoCentrum_Dev',
    [string]$Password = $(if ($env:NIGA_TEST_PASSWORD) { $env:NIGA_TEST_PASSWORD } else { '123456' }),
    [string]$ReportPath = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
$http = New-Object System.Net.Http.HttpClient
$http.Timeout = [TimeSpan]::FromSeconds(60)

function Sql([string]$query) {
    $out = & sqlcmd -S $SqlServer -d $Database -E -C -h -1 -W -Q ("SET NOCOUNT ON; " + $query)
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $out" }
    return ($out | Where-Object { $_ -and $_.Trim() } | Select-Object -First 1).Trim()
}

$ids = @{
    DoctorAUser  = [long](Sql "SELECT UserId FROM UserMaster WHERE UserName='Tufan_Doctor'")
    DoctorBUser  = [long](Sql "SELECT UserId FROM UserMaster WHERE UserName='Tufan_Doctor2'")
    PatientAUser = [long](Sql "SELECT UserId FROM UserMaster WHERE UserName='Tufan_Patient'")
    PatientBUser = [long](Sql "SELECT UserId FROM UserMaster WHERE UserName='Tufan_Patient2'")
}
$ids.DoctorA  = [int](Sql "SELECT DoctorID FROM Doctor WHERE UserId=$($ids.DoctorAUser)")
$ids.DoctorB  = [int](Sql "SELECT DoctorID FROM Doctor WHERE UserId=$($ids.DoctorBUser)")
$ids.PatientA = [int](Sql "SELECT TOP 1 PatientId FROM PatientUserMap WHERE UserId=$($ids.PatientAUser) AND DeleteStatus=0 ORDER BY IsPrimary DESC")
$ids.PatientB = [int](Sql "SELECT TOP 1 PatientId FROM PatientUserMap WHERE UserId=$($ids.PatientBUser) AND DeleteStatus=0 ORDER BY IsPrimary DESC")
$ids.CaseA    = [int](Sql "SELECT TOP 1 CaseId FROM CaseEntryDetails WHERE PatientId=$($ids.PatientA) AND DoctorId=$($ids.DoctorA) AND DeleteStatus=0")
$ids.CaseB    = [int](Sql "SELECT TOP 1 CaseId FROM CaseEntryDetails WHERE PatientId=$($ids.PatientB) AND DoctorId=$($ids.DoctorB) AND DeleteStatus=0")
$ids.MobileB  = Sql "SELECT MobileNo FROM Patient WHERE PatientID=$($ids.PatientB)"

function Send([string]$method, [string]$url, [string]$token, $body) {
    $req = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::new($method)), $url
    if ($token) { $req.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue 'Bearer', $token }
    if ($null -ne $body) {
        $req.Content = New-Object System.Net.Http.StringContent (($body | ConvertTo-Json -Depth 6 -Compress)), ([Text.Encoding]::UTF8), 'application/json'
    }
    $res = $http.SendAsync($req).GetAwaiter().GetResult()
    return [pscustomobject]@{ Status = [int]$res.StatusCode; Body = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult() }
}

function Login([string]$base, [string]$user) {
    $r = Send 'POST' "$base/Account/Login" $null @{ userName = $user; password = $Password }
    $m = [regex]::Match($r.Body, '"(?:token|accessToken)"\s*:\s*"(eyJ[^"]+)"', 'IgnoreCase')
    if (-not $m.Success) { throw "Login failed for $user on $base (HTTP $($r.Status))" }
    return $m.Groups[1].Value
}

$tokens = @{}
foreach ($u in 'Tufan_Admin', 'Tufan_Doctor', 'Tufan_Doctor2', 'Tufan_Patient', 'Tufan_Patient2', 'Tufan_Reception', 'Tufan_Reception2', 'Tufan_Pharmacy') {
    $tokens["New:$u"] = Login $NewBase $u
}
foreach ($u in 'Tufan_Admin', 'Tufan_Doctor', 'Tufan_Doctor2', 'Tufan_Patient') {
    $tokens["Old:$u"] = Login $OldBase $u
}

$results = New-Object System.Collections.Generic.List[object]
function Check([string]$api, [string]$user, [string]$method, [string]$path, [string]$expect, $body = $null, [string]$note = '') {
    $base = if ($api -eq 'Old') { $OldBase } else { $NewBase }
    $token = if ($user -eq 'anonymous') { $null } else { $tokens["${api}:$user"] }
    $r = Send $method "$base$path" $token $body
    $ok = switch ($expect) {
        'allow' { $r.Status -ge 200 -and $r.Status -lt 300 }
        'deny'  { $r.Status -in 401, 403, 404 }
        'notoken' { $r.Body -notmatch '"eyJ' }
    }
    $results.Add([pscustomobject]@{
        Result = if ($ok) { 'PASS' } else { 'FAIL' }; Api = $api; User = $user.Replace('Tufan_', ''); Expect = $expect
        Status = $r.Status; Request = "$method $path"; Note = $note
    })
}

$A = $ids; 
# --- API: patient records ---
Check New Tufan_Doctor  GET "/patient/GetPatientDetailsById/$($A.PatientA)" allow
Check New Tufan_Doctor  GET "/patient/GetPatientDetailsById/$($A.PatientB)" deny
Check New Tufan_Doctor2 GET "/patient/GetPatientDetailsById/$($A.PatientB)" allow
Check New Tufan_Doctor2 GET "/patient/GetPatientDetailsById/$($A.PatientA)" deny
Check New Tufan_Doctor  GET "/patient/GetCaseDetails/$($A.CaseA)" allow
Check New Tufan_Doctor  GET "/patient/GetCaseDetails/$($A.CaseB)" deny
Check New Tufan_Doctor  GET "/patient/GetComplaints/$($A.PatientB)" deny
Check New Tufan_Doctor  GET "/patient/GetPatientDetails/$($A.PatientB)/$($A.CaseB)" deny
Check New Tufan_Doctor  GET "/patient/ExportCaseToPdf/$($A.PatientB)/$($A.CaseB)" deny
Check New Tufan_Doctor  GET "/patient/$($A.DoctorAUser)" allow
Check New Tufan_Doctor  GET "/patient/$($A.DoctorBUser)" deny
Check New Tufan_Doctor  GET "/patient/getAllCases?UserId=$($A.DoctorBUser)" deny
Check New Tufan_Doctor  GET "/patient/ExportCasesToExcel?UserId=$($A.DoctorBUser)" deny
Check New Tufan_Doctor  POST "/patient/Deletepatient?patientId=$($A.PatientB)" deny
Check New Tufan_Doctor  POST "/patient/SaveComplaints" deny @{ PatientID = $A.PatientB; DoctorID = $A.DoctorA }
Check New Tufan_Doctor  GET "/PatientLab/GetPatientLabOrder/$($A.PatientB)" deny
Check New Tufan_Doctor  GET "/PatientLab/GetPatientLabEntry/$($A.PatientB)" deny
Check New Tufan_Doctor  POST "/PatientLab/SavePatientLabOrder" deny @{ PatientId = $A.PatientB; PatientLabTestId = 1; PatientLabTestName = 'idor-test'; LabName = 'idor-test'; OrderDate = (Get-Date).ToString('s') }
Check New Tufan_Doctor  GET "/clipboardRubrics/GetClipboardRubricsPatientId/$($A.PatientB)" deny
# --- API: users / doctors ---
Check New Tufan_Doctor  GET "/users/$($A.DoctorAUser)" allow
Check New Tufan_Doctor  GET "/users/$($A.DoctorBUser)" deny
Check New Tufan_Doctor  GET "/mastersAPI/GetDoctorDetails/$($A.DoctorAUser)" allow
Check New Tufan_Doctor  GET "/mastersAPI/GetDoctorDetails/$($A.DoctorBUser)" deny
Check New Tufan_Doctor  GET "/subsection/GetSubSectionsByDate/$($A.DoctorBUser)" deny
Check New Tufan_Doctor2 GET "/users" deny $null 'admin only (list all users)'
Check New Tufan_Doctor2 POST "/users/DeleteUser" deny @{ UserId = $A.DoctorAUser; UserName = 'x'; UserPassword = 'x' } 'admin only'
# --- API: patients ---
Check New Tufan_Patient  GET "/patient/GetPatientDetailsById/$($A.PatientA)" allow
Check New Tufan_Patient  GET "/patient/GetPatientDetailsById/$($A.PatientB)" deny
Check New Tufan_Patient2 GET "/patient/GetPatientDetailsById/$($A.PatientA)" deny
Check New Tufan_Patient  GET "/users/$($A.PatientBUser)" deny
Check New Tufan_Patient  GET "/patient/GetCaseDetails/$($A.CaseA)" deny $null 'doctor-only endpoint'
# --- API: reception (tied to its doctor) ---
Check New Tufan_Reception  GET "/patient/GetPatientDetailsById/$($A.PatientA)" allow $null 'own doctor patient'
Check New Tufan_Reception  GET "/patient/GetPatientDetailsById/$($A.PatientB)" deny
Check New Tufan_Reception2 GET "/patient/GetPatientDetailsById/$($A.PatientA)" deny
Check New Tufan_Reception  GET "/PatientLab/GetPatientLabEntry/$($A.PatientB)" deny
# --- API: pharmacy partner ---
Check New Tufan_Pharmacy GET "/patient/GetPatientDetailsById/$($A.PatientA)" deny
Check New Tufan_Pharmacy GET "/users/$($A.DoctorAUser)" deny
# --- API: admin-only security endpoints ---
Check New Tufan_Admin  GET "/Admin/SecurityAudit/Verify" allow
Check New Tufan_Doctor2 GET "/Admin/SecurityAudit/Verify" deny
Check New Tufan_Reception GET "/Admin/SecurityAudit/Verify" deny
Check New Tufan_Patient GET "/Admin/SecurityAudit" deny
# --- API: anonymous patient create must never hand out someone else's token ---
Check New anonymous POST "/patient" notoken @{ PatientName = 'IDOR Probe'; MobileNo = $A.MobileB; Gender = 1; DoctorID = $A.DoctorB; EntityType = 'PatientMobile' } 'account takeover via existing mobile'
Check New anonymous POST "/patient" deny @{ PatientID = $A.PatientB; PatientName = 'IDOR Probe'; MobileNo = $A.MobileB; EntityType = 'PatientMobile' } 'anonymous update by id'

# --- Old API ---
Check Old Tufan_Doctor  GET "/patient/GetPatientDetailsById/$($A.PatientA)" allow
Check Old Tufan_Doctor  GET "/patient/GetPatientDetailsById/$($A.PatientB)" deny
Check Old Tufan_Doctor2 GET "/patient/GetPatientDetailsById/$($A.PatientA)" deny
Check Old Tufan_Doctor  GET "/patient/GetCaseDetails/$($A.CaseB)" deny
Check Old Tufan_Doctor  GET "/patient/GetComplaints/$($A.PatientB)" deny
Check Old Tufan_Doctor  GET "/patient/$($A.DoctorBUser)" deny
Check Old Tufan_Doctor  POST "/patient/Deletepatient?patientId=$($A.PatientB)" deny
Check Old Tufan_Doctor  GET "/PatientLab/GetPatientLabOrder/$($A.PatientB)" deny
Check Old Tufan_Doctor  GET "/PatientLab/GetPatientLabEntry/$($A.PatientB)" deny
Check Old Tufan_Doctor  POST "/PatientLab/SavePatientLabEntry" deny @{ PatientId = $A.PatientB; PatientLabTestId = 1; ParameterName = 'idor'; ParameterValue = 'x' }
Check Old Tufan_Doctor  GET "/clipboardRubrics/GetClipboardRubricsPatientId/$($A.PatientB)" deny
Check Old Tufan_Doctor  GET "/users/$($A.DoctorAUser)" allow
Check Old Tufan_Doctor  GET "/users/$($A.DoctorBUser)" deny
Check Old Tufan_Doctor  GET "/mastersAPI/GetDoctorDetails/$($A.DoctorBUser)" deny
Check Old Tufan_Doctor  GET "/menuMaster/GetMenuByUserId/$($A.DoctorBUser)" deny
Check Old Tufan_Doctor  GET "/subsection/GetSubSectionsByDate/$($A.DoctorBUser)" deny
Check Old Tufan_Doctor  POST "/users/DeleteUser?userId=$($A.DoctorBUser)" deny $null 'admin only'
Check Old anonymous     POST "/BlogDetail/DeleteBlogDetail?blogDetailId=1" deny $null 'was anonymous'
Check Old anonymous     POST "/NewsDetail/DeleteNewsDetails?newsDetailId=1" deny $null 'was anonymous'
Check Old anonymous     POST "/Subscription/SaveUpdateSubscription" deny @{ DoctorId = $A.DoctorA } 'was anonymous'
Check Old Tufan_Patient GET "/patient/GetPatientDetailsById/$($A.PatientA)" allow
Check Old Tufan_Patient GET "/patient/GetPatientDetailsById/$($A.PatientB)" deny
Check Old Tufan_Admin   GET "/patient/GetPatientDetailsById/$($A.PatientB)" allow $null 'admin portal'

Sql "UPDATE Patient SET DeleteStatus = 1 WHERE PatientName = 'IDOR Probe' AND DeleteStatus = 0; SELECT @@ROWCOUNT" | Out-Null

$results | Format-Table -AutoSize Result, Api, User, Expect, Status, Request, Note | Out-String -Width 220
$failed = @($results | Where-Object Result -eq 'FAIL').Count
"Checks: $($results.Count)  Passed: $($results.Count - $failed)  Failed: $failed"
if ($ReportPath) { $results | Export-Csv -NoTypeInformation -Path $ReportPath }
exit $failed
