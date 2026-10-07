<#
.SYNOPSIS
  End-to-end checks for versioned consent notices, guardian consent for minors, and reception password hashing.

  - Consent records store the notice id, version and SHA-256 that was agreed to; stale versions are refused.
  - A patient under 18 cannot consent; a verified parent / guardian can (family account, in clinic, OTP).
  - Reception staff passwords are PBKDF2 like UserMaster; legacy encoded values are upgraded on login (both APIs).

  LOCAL / STAGING ONLY: creates a child family member under Tufan_Patient (removed at the end) and temporarily
  rewrites Tufan_Reception2's stored password into the legacy encoding. Needs the test tenant from
  "S5_Week5\Database scripts\10_Security_Test_Tenant_LOCAL_ONLY.sql". Tokens and OTP codes are never printed.
  Exit code = number of failed checks.
#>
param(
    [string]$NewBase = 'http://127.0.0.1:5002/api',
    [string]$OldBase = 'http://127.0.0.1:5001/api',
    [string]$SqlServer = 'localhost\MSSQLSERVER25',
    [string]$Database = 'HomeoCentrum_Dev',
    [string]$Password = $(if ($env:NIGA_TEST_PASSWORD) { $env:NIGA_TEST_PASSWORD } else { '123456' })
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
Add-Type -AssemblyName System.Web
$http = New-Object System.Net.Http.HttpClient
$http.Timeout = [TimeSpan]::FromSeconds(60)

function Sql([string]$query) {
    $out = & sqlcmd -S $SqlServer -d $Database -E -C -I -h -1 -W -b -Q ("SET NOCOUNT ON; " + $query)
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $out" }
    $line = $out | Where-Object { $_ -and $_.Trim() } | Select-Object -First 1
    if ($null -eq $line) { return '' }
    return "$line".Trim()
}

function SqlFails([string]$statement, [int]$expectedError) {
    $out = & sqlcmd -S $SqlServer -d $Database -E -C -I -h -1 -W -b -Q ("SET NOCOUNT ON; BEGIN TRAN; " + $statement + "; ROLLBACK;") 2>&1
    return $LASTEXITCODE -ne 0 -and ("$out" -match "Msg $expectedError,")
}

function Send([string]$method, [string]$url, [string]$token, $body) {
    $req = New-Object System.Net.Http.HttpRequestMessage ([System.Net.Http.HttpMethod]::new($method)), $url
    if ($token) { $req.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue 'Bearer', $token }
    if ($null -ne $body) {
        $req.Content = New-Object System.Net.Http.StringContent (($body | ConvertTo-Json -Depth 6 -Compress)), ([Text.Encoding]::UTF8), 'application/json'
    }
    $res = $http.SendAsync($req).GetAwaiter().GetResult()
    $text = $res.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $json = $null
    try { if ($text) { $json = $text | ConvertFrom-Json } } catch { }
    return [pscustomobject]@{ Status = [int]$res.StatusCode; Json = $json }
}

function Login([string]$base, [string]$user) {
    $r = Send 'POST' "$base/Account/Login" $null @{ userName = $user; password = $Password }
    $t = $r.Json.token, $r.Json.accessToken, $r.Json.data.token, $r.Json.data.accessToken | Where-Object { "$_" -like 'eyJ*' } | Select-Object -First 1
    return [pscustomobject]@{ Status = $r.Status; Token = $t }
}

$results = New-Object System.Collections.Generic.List[object]
function Record([string]$area, [string]$check, [bool]$ok, [string]$detail = '') {
    $results.Add([pscustomobject]@{ Result = if ($ok) { 'PASS' } else { 'FAIL' }; Area = $area; Check = $check; Detail = $detail })
}

$tok = @{}
foreach ($u in 'Tufan_Admin', 'Tufan_Patient', 'Tufan_Patient2', 'Tufan_Doctor') {
    $l = Login $NewBase $u
    if (-not $l.Token) { throw "Login failed for $u (HTTP $($l.Status))" }
    $tok[$u] = $l.Token
}
$parentUser = [long](Sql "SELECT UserId FROM UserMaster WHERE UserName='Tufan_Patient'")
$parentPatient = [int](Sql "SELECT TOP 1 PatientId FROM PatientUserMap WHERE UserId=$parentUser AND DeleteStatus=0 ORDER BY IsPrimary DESC")

# --- Notice ---------------------------------------------------------------------------------------------------
$n = Send 'GET' "$NewBase/Consent/Notice/Privacy" $tok['Tufan_Patient'] $null
$notice = $n.Json.data
Record 'Notice' 'Current privacy notice is served' ($n.Status -eq 200 -and $notice.version) "HTTP $($n.Status) version $($notice.version)"
$sha = [BitConverter]::ToString([Security.Cryptography.SHA256]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes([string]$notice.body))).Replace('-', '').ToLowerInvariant()
$dbSha = Sql "SELECT BodySha256 FROM ConsentNotice WHERE ConsentNoticeId=$($notice.consentNoticeId)"
Record 'Notice' 'SHA-256 of served text = stored hash = SQL hash' ($sha -eq $notice.bodySha256 -and $sha -eq $dbSha) ''
Record 'Notice' 'Published text cannot be edited (trigger)' (SqlFails "UPDATE ConsentNotice SET Body=Body+N'x' WHERE ConsentNoticeId=$($notice.consentNoticeId)" 51011) ''
Record 'Notice' 'Published notice cannot be deleted (trigger)' (SqlFails "DELETE FROM ConsentNotice WHERE ConsentNoticeId=$($notice.consentNoticeId)" 51010) ''
$r = Send 'POST' "$NewBase/Consent/Notice" $tok['Tufan_Patient'] @{ consentTypeCode = 'Privacy'; version = '9.9'; title = 't'; body = 'b' }
Record 'Notice' 'Patient cannot publish a notice' ($r.Status -eq 403) "HTTP $($r.Status)"
$r = Send 'POST' "$NewBase/Consent/Notice" $tok['Tufan_Admin'] @{ consentTypeCode = 'Privacy'; version = $notice.version; language = 'en'; title = 't'; body = 'b' }
Record 'Notice' 'Re-publishing an existing version is refused' ($r.Status -eq 409 -and $r.Json.code -eq 'NOTICE_VERSION_EXISTS') "HTTP $($r.Status)"

# --- Adult self-consent stores the version -------------------------------------------------------------------
$r = Send 'POST' "$NewBase/Consent/GrantPrivacy" $tok['Tufan_Patient'] @{ noticeVersion = '0.1' }
Record 'Version' 'Stale notice version is refused' ($r.Status -eq 409 -and $r.Json.code -eq 'NOTICE_OUTDATED' -and $r.Json.currentVersion -eq $notice.version) "HTTP $($r.Status)"
$r = Send 'POST' "$NewBase/Consent/GrantPrivacy" $tok['Tufan_Patient'] @{ noticeVersion = $notice.version }
Record 'Version' 'Adult grants privacy against current version' ($r.Status -eq 200) "HTTP $($r.Status)"
$s = (Send 'GET' "$NewBase/Consent/PrivacyStatus" $tok['Tufan_Patient'] $null).Json.data
Record 'Version' 'Status reports granted + notice version' ($s.granted -and $s.grantedNoticeVersion -eq $notice.version -and -not $s.isMinor) "status $($s.status)"

# --- Child under the parent's family account -----------------------------------------------------------------
$relId = Sql "SELECT TOP 1 RelationId FROM FamilyRelationMaster WHERE DeleteStatus=0 ORDER BY SortOrder"
$relName = Sql "SELECT RelationName FROM FamilyRelationMaster WHERE RelationId=$relId"
$dob = (Get-Date).AddYears(-10).ToString('yyyy-MM-dd')
$r = Send 'POST' "$NewBase/Family" $tok['Tufan_Patient'] @{ relationId = [int]$relId; relation = $relName; patientName = "SecTest Child $(Get-Date -Format HHmmss)"; dateOfBirth = $dob }
$familyId = $r.Json.data.familyMemberId
$child = [int]$r.Json.data.memberPatientId
Record 'Family' 'Parent adds a child with date of birth' ($r.Status -eq 200 -and $child -gt 0 -and $r.Json.data.isMinor -eq $true) "HTTP $($r.Status)"
try {
    $list = (Send 'GET' "$NewBase/Family" $tok['Tufan_Patient'] $null).Json.data | Where-Object { $_.memberPatientId -eq $child }
    Record 'Family' 'Family list shows the child as a minor' ($list.isMinor -eq $true) ''
    $r = Send 'POST' "$NewBase/Family" $tok['Tufan_Patient2'] @{ relationId = [int]$relId; relation = $relName; patientName = 'Link attempt'; existingMemberPatientId = $child }
    Record 'Family' "Patient cannot attach someone else's record as family" ($r.Status -eq 403) "HTTP $($r.Status)"

    $s = (Send 'GET' "$NewBase/Consent/PrivacyStatus?patientId=$child" $tok['Tufan_Patient'] $null).Json.data
    Record 'Guardian' 'Child status: minor, guardian required, not granted' ($s.isMinor -and $s.guardianRequired -and -not $s.granted) "status $($s.status)"
    $r = Send 'GET' "$NewBase/Consent/PrivacyStatus?patientId=$child" $tok['Tufan_Patient2'] $null
    Record 'Guardian' "Another patient cannot read the child's consent status" ($r.Status -eq 403) "HTTP $($r.Status)"

    $grant = @{ consentTypeCode = 'Privacy'; subjectType = 'Patient'; subjectId = $child; noticeVersion = $notice.version }
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Patient'] $grant
    Record 'Guardian' 'Consent for a minor without guardian details is refused' ($r.Status -eq 403 -and $r.Json.code -eq 'GUARDIAN_CONSENT_REQUIRED') "HTTP $($r.Status)"
    $grant.guardian = @{ method = 'FamilyAccount'; declaresLegalGuardian = $false }
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Patient'] $grant
    Record 'Guardian' 'Guardian must declare they are parent / legal guardian' ($r.Status -eq 400 -and $r.Json.code -eq 'GUARDIAN_DECLARATION_REQUIRED') "HTTP $($r.Status)"
    $grant.guardian = @{ method = 'FamilyAccount'; declaresLegalGuardian = $true }
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Patient2'] $grant
    Record 'Guardian' 'Unrelated adult cannot consent for the child' ($r.Status -eq 403) "HTTP $($r.Status)"
    $grant.noticeVersion = '0.1'
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Patient'] $grant
    Record 'Guardian' 'Guardian consent on a stale notice is refused' ($r.Status -eq 409) "HTTP $($r.Status)"
    $grant.noticeVersion = $notice.version
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Patient'] $grant
    $recId = [long]$r.Json.data.consentRecordId
    Record 'Guardian' 'Parent (family account) gives consent' ($r.Status -eq 200 -and $recId -gt 0 -and $r.Json.data.grantedForMinor) "HTTP $($r.Status)"
    $row = Sql "SELECT CONCAT(NoticeVersion,'|',NoticeSha256,'|',GrantedForMinor,'|',GuardianUserId,'|',GuardianVerificationMethod,'|',LEFT(GuardianVerificationRef,7),'|',ConsentNoticeId) FROM ConsentRecord WHERE ConsentRecordId=$recId"
    $expected = "$($notice.version)|$dbSha|1|$parentUser|FamilyAccount|family:|$($notice.consentNoticeId)"
    Record 'Guardian' 'Stored row: notice id/version/hash + guardian user, method, family link' ($row -eq $expected) ''
    $s = (Send 'GET' "$NewBase/Consent/PrivacyStatus?patientId=$child" $tok['Tufan_Patient'] $null).Json.data
    Record 'Guardian' 'Child status now granted (by guardian)' ($s.granted -and $s.grantedForMinor) "status $($s.status)"
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Patient'] $grant
    Record 'Guardian' 'Repeat grant is idempotent' ($r.Status -eq 200 -and $r.Json.alreadyGranted) ''
    $audit = Sql "SELECT COUNT(*) FROM SecurityAuditLog WHERE EventType='GUARDIAN_CONSENT' AND Subject='patient:$child'"
    Record 'Guardian' 'Guardian consent written to the security audit log' ([int]$audit -ge 1) ''

    # In clinic: staff record the guardian's name, relationship and the identity document type checked.
    $clinic = @{ consentTypeCode = 'Booking'; subjectType = 'Patient'; subjectId = $child; guardian = @{ method = 'InClinic'; declaresLegalGuardian = $true; guardianName = 'Test Parent'; relationship = 'Mother' } }
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Admin'] $clinic
    Record 'Guardian' 'In-clinic consent needs the ID document type' ($r.Status -eq 400) "HTTP $($r.Status)"
    $clinic.guardian.idProofType = 'Aadhaar'
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Admin'] $clinic
    $clinicId = [long]$r.Json.data.consentRecordId
    $ref = Sql "SELECT GuardianVerificationRef FROM ConsentRecord WHERE ConsentRecordId=$clinicId"
    Record 'Guardian' 'In-clinic consent records staff id + ID type (no ID number)' ($r.Status -eq 200 -and $ref -match '^staff:\d+;idProof:Aadhaar$') "HTTP $($r.Status)"
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Doctor'] (@{ consentTypeCode = 'Marketing'; subjectType = 'Patient'; subjectId = $child; guardian = $clinic.guardian })
    Record 'Guardian' "Doctor without access to the child cannot record consent" ($r.Status -eq 403) "HTTP $($r.Status)"

    # OTP: the guardian proves control of their mobile; one OTP backs one consent.
    $o = Send 'POST' "$NewBase/Otp/RequestOtp" $tok['Tufan_Patient'] @{ action = 'GuardianConsent'; entityType = 'Patient'; entityId = "$child"; destination = '9000000001' }
    $challenge = [long]$o.Json.data.otpChallengeId
    $otpGrant = @{ consentTypeCode = 'Marketing'; subjectType = 'Patient'; subjectId = $child; guardian = @{ method = 'Otp'; declaresLegalGuardian = $true; guardianName = 'Test Parent'; relationship = 'Father'; otpChallengeId = $challenge } }
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Patient'] $otpGrant
    Record 'Guardian' 'Unverified OTP is refused' ($r.Status -eq 400 -and $r.Json.code -eq 'GUARDIAN_OTP_INVALID') "HTTP $($r.Status)"
    $v = Send 'POST' "$NewBase/Otp/VerifyOtp" $null @{ otpChallengeId = $challenge; code = [string]$o.Json.data.devCode }
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Patient'] $otpGrant
    $otpId = [long]$r.Json.data.consentRecordId
    $masked = Sql "SELECT GuardianMobileMasked FROM ConsentRecord WHERE ConsentRecordId=$otpId"
    Record 'Guardian' 'Verified OTP backs a consent; only masked mobile stored' ($v.Status -eq 200 -and $r.Status -eq 200 -and $masked -and $masked -notmatch '9000000001') "HTTP $($r.Status)"
    $otpGrant.consentTypeCode = 'TeleRecording'
    $r = Send 'POST' "$NewBase/Consent/Grant" $tok['Tufan_Patient'] $otpGrant
    Record 'Guardian' 'The same OTP cannot be reused for another consent' ($r.Status -eq 409 -and $r.Json.code -eq 'GUARDIAN_OTP_USED') "HTTP $($r.Status)"

    # Withdraw: the guardian can withdraw; an unrelated patient cannot.
    $r = Send 'POST' "$NewBase/Consent/Withdraw" $tok['Tufan_Patient2'] @{ consentRecordId = $otpId }
    Record 'Withdraw' 'Unrelated patient cannot withdraw the child consent' ($r.Status -eq 403) "HTTP $($r.Status)"
    $r = Send 'POST' "$NewBase/Consent/Withdraw" $tok['Tufan_Patient'] @{ consentRecordId = $otpId }
    Record 'Withdraw' 'Guardian withdraws the child consent' ($r.Status -eq 200) "HTTP $($r.Status)"
    $r = Send 'GET' "$NewBase/Consent/Patient/$child" $tok['Tufan_Patient'] $null
    Record 'Withdraw' 'Child consent history lists guardian + version for each record' ($r.Status -eq 200 -and @($r.Json.data | Where-Object { $_.grantedForMinor -and $_.noticeVersion }).Count -ge 3) "HTTP $($r.Status)"
}
finally {
    if ($familyId) { Send 'DELETE' "$NewBase/Family/$familyId" $tok['Tufan_Patient'] $null | Out-Null }
}

# --- Other consent paths are stamped with the notice too (patient consent centre) ----------------------------
$list = (Send 'GET' "$NewBase/Patient/Consents" $tok['Tufan_Patient'] $null).Json.data
$booking = $list | Where-Object { $_.code -eq 'Booking' } | Select-Object -First 1
if ($booking.consentRecordId -and $booking.granted) { Send 'POST' "$NewBase/Patient/Consents/$($booking.consentRecordId)/Withdraw" $tok['Tufan_Patient'] $null | Out-Null }
$r = Send 'POST' "$NewBase/Patient/Consents/Types/$($booking.consentTypeId)/Grant" $tok['Tufan_Patient'] $null
$newId = [long]$r.Json.data.consentRecordId
$stamped = Sql "SELECT CONCAT(NoticeVersion,'|',CASE WHEN ConsentNoticeId IS NULL THEN 'null' ELSE 'id' END,'|',LEN(NoticeSha256)) FROM ConsentRecord WHERE ConsentRecordId=$newId"
Record 'Version' 'Consent-centre grant is stamped with the current notice' ($r.Status -eq 200 -and $stamped -match '^[^|]+\|id\|64$') "HTTP $($r.Status)"
$unstamped = Sql "SELECT COUNT(*) FROM ConsentRecord WHERE ConsentNoticeId IS NULL"
Record 'Version' 'No consent record is missing its notice' ([int]$unstamped -eq 0) "$unstamped without notice"

# --- Reception staff passwords --------------------------------------------------------------------------------
$legacy = [System.Web.HttpUtility]::UrlEncode([Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($Password)))
foreach ($api in @(@{ Name = 'Old'; Base = $OldBase }, @{ Name = 'New'; Base = $NewBase })) {
    Sql "UPDATE DoctorReceptionStaff SET Password=N'$legacy' WHERE UserID='Tufan_Reception2'" | Out-Null
    $l = Login $api.Base 'Tufan_Reception2'
    $stored = Sql "SELECT CONCAT(LEFT(Password,10),'|',LEN(Password)) FROM DoctorReceptionStaff WHERE UserID='Tufan_Reception2'"
    Record 'Reception' "$($api.Name) API: legacy password logs in and is re-hashed to PBKDF2" ($l.Token -and $stored -match '^PBKDF2\$v1\$\|\d{2,3}$' -and [int]$stored.Split('|')[1] -ge 80) "HTTP $($l.Status)"
    $l = Login $api.Base 'Tufan_Reception2'
    Record 'Reception' "$($api.Name) API: hashed password logs in" ([bool]$l.Token) "HTTP $($l.Status)"
}
$bad = Send 'POST' "$NewBase/Account/Login" $null @{ userName = 'Tufan_Reception2'; password = "$Password-wrong" }
Record 'Reception' 'Wrong password is rejected on hashed row' ($bad.Status -in 400, 401) "HTTP $($bad.Status)"
$plain = Sql "SELECT COUNT(*) FROM DoctorReceptionStaff WHERE Password IS NOT NULL AND Password NOT LIKE 'PBKDF2`$v1`$%'"
Record 'Reception' 'No reception password is stored reversibly' ([int]$plain -eq 0) "$plain legacy rows"

$results | Format-Table -AutoSize | Out-String -Width 220 | Write-Host
$failed = @($results | Where-Object Result -eq 'FAIL').Count
Write-Host "Checks: $($results.Count)  Passed: $($results.Count - $failed)  Failed: $failed"
exit $failed
