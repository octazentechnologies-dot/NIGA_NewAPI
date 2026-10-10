<#
.SYNOPSIS
  IIS watchdog for Homeocentrum. Emails when IIS, an app pool, a site or an API stops, and when it recovers.

.DESCRIPTION
  Runs from a scheduled task (see Register-HomeocentrumWatchdog.ps1) as SYSTEM every few minutes.
  The APIs cannot report IIS itself being stopped, so this script checks from outside:
    - Windows services W3SVC and WAS
    - IIS app pools and sites HomeocentrumUI, HomeocentrumOldAPI, HomeocentrumNewAPI
    - HTTP: UI on port 80, Old API on 5001, API on 5002
    - Free disk space: DOWN when a fixed drive has DiskSpaceAlert:MinFreePercent (default 10) or less free.
      DiskSpaceAlert:Drives in the same appsettings.json limits the drives; -MinFreePercent overrides the percentage.
    - New IIS / ASP.NET Core Module error events since the last run
  A mail is sent when a check goes DOWN, again every -ReminderMinutes while it stays down, and once when it RECOVERS.
  -Mode Startup (scheduled at boot) mails "server restarted", including whether the previous shutdown was unexpected
  (power loss or crash), since that cannot be mailed from this machine while it is off.

  SMTP settings and recipients are read at run time from the deployed API appsettings.json (smtp + ErrorAlert:Recipients).
  Secrets are never printed or logged.

.EXAMPLE
  .\Watch-Homeocentrum.ps1 -DryRun          # run all checks, print the result, send nothing, change no state
#>
[CmdletBinding()]
param(
    [ValidateSet('Check', 'Startup')]
    [string]$Mode = 'Check',
    [string]$AppSettingsPath = 'C:\inetpub\homeocentrum\newapi\appsettings.json',
    [string]$DataDir = 'C:\ProgramData\Homeocentrum\Watchdog',
    [int]$ReminderMinutes = 60,
    [double]$MinFreePercent = 0,
    [int]$HttpTimeoutSec = 15,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$Sites = @(
    @{ Name = 'HomeocentrumUI';     Url = 'http://localhost/' },
    @{ Name = 'HomeocentrumOldAPI'; Url = 'http://localhost:5001/health' },
    @{ Name = 'HomeocentrumNewAPI'; Url = 'http://localhost:5002/health' }
)

if (-not (Test-Path $DataDir)) { New-Item -ItemType Directory -Path $DataDir -Force | Out-Null }
$StatePath = Join-Path $DataDir 'state.json'
$LogPath = Join-Path $DataDir ('watchdog-' + (Get-Date -Format 'yyyyMMdd') + '.log')

function Write-Log([string]$level, [string]$message) {
    $line = '{0} [{1}] {2}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $level, $message
    if ($DryRun) { Write-Output $line } else { Add-Content -Path $LogPath -Value $line -Encoding UTF8 }
}

function Get-MailSettings {
    if (-not (Test-Path $AppSettingsPath)) { throw "appsettings not found: $AppSettingsPath" }
    # appsettings allows // and /* */ comments and trailing commas; strip them outside strings before parsing.
    $raw = Get-Content -Raw -Path $AppSettingsPath
    $raw = [regex]::Replace($raw, '("(?:\\.|[^"\\])*")|//[^\r\n]*|/\*[\s\S]*?\*/', '$1')
    $raw = [regex]::Replace($raw, '("(?:\\.|[^"\\])*")|,(\s*[}\]])', '$1$2')
    # Windows PowerShell's parse error echoes the whole input (secrets), so the message is never surfaced.
    try { $cfg = $raw | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "appsettings is not valid JSON: $AppSettingsPath" }
    finally { $raw = $null }
    $recipients = @()
    if ($cfg.ErrorAlert -and $cfg.ErrorAlert.Recipients) {
        $recipients = @($cfg.ErrorAlert.Recipients -split '[;,]' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    }
    $diskPercent = 10.0
    $diskDrives = @()
    if ($cfg.DiskSpaceAlert) {
        $p = 0.0
        if ([double]::TryParse([string]$cfg.DiskSpaceAlert.MinFreePercent, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$p) -and $p -gt 0 -and $p -lt 100) { $diskPercent = $p }
        $diskDrives = @($cfg.DiskSpaceAlert.Drives | Where-Object { $_ } | ForEach-Object { ([string]$_).Trim().TrimEnd('\') })
    }
    if ($MinFreePercent -gt 0) { $diskPercent = $MinFreePercent }
    [pscustomobject]@{
        Smtp           = $cfg.smtp
        Recipients     = $recipients
        DiskMinPercent = $diskPercent
        DiskDrives     = $diskDrives
    }
}

function Send-AlertMail($mail, [string]$subject, [string]$html) {
    if ($DryRun) { Write-Log 'DRYRUN' "Would mail $($mail.Recipients.Count) recipient(s): $subject"; return }
    if (-not $mail.Smtp -or -not $mail.Smtp.host -or $mail.Recipients.Count -eq 0) { Write-Log 'WARN' 'Mail not configured; alert not sent.'; return }
    $client = New-Object System.Net.Mail.SmtpClient($mail.Smtp.host, [int]$mail.Smtp.port)
    $client.EnableSsl = [bool]$mail.Smtp.enableSsl
    $client.Timeout = 30000
    if ($mail.Smtp.defaultCredentials) {
        $client.UseDefaultCredentials = $true
    } else {
        $client.Credentials = New-Object System.Net.NetworkCredential($mail.Smtp.userName, $mail.Smtp.password)
    }
    try {
        foreach ($to in $mail.Recipients) {
            $msg = New-Object System.Net.Mail.MailMessage($mail.Smtp.from, $to, $subject, $html)
            $msg.IsBodyHtml = $true
            try { $client.Send($msg); Write-Log 'INFO' "Mailed: $subject" }
            catch { Write-Log 'ERROR' ("Mail failed: " + $_.Exception.GetType().Name) }
            finally { $msg.Dispose() }
        }
    } finally { $client.Dispose() }
}

function ConvertTo-AlertHtml([string]$title, [string]$color, $rows) {
    $enc = { param($s) [System.Net.WebUtility]::HtmlEncode([string]$s) }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append('<div style="font-family:Segoe UI,Arial,sans-serif;font-size:14px;">')
    [void]$sb.Append("<h2 style=`"color:$color;margin:0 0 12px;`">" + (& $enc $title) + '</h2>')
    [void]$sb.Append('<table cellpadding="6" cellspacing="0" border="1" style="border-collapse:collapse;border-color:#cbd5e1;">')
    [void]$sb.Append('<tr style="background:#f1f5f9;"><th align="left">Check</th><th align="left">Status</th><th align="left">Detail</th></tr>')
    foreach ($r in $rows) {
        [void]$sb.Append('<tr><td>' + (& $enc $r.Check) + '</td><td>' + (& $enc $r.Status) + '</td><td>' + (& $enc $r.Detail) + '</td></tr>')
    }
    [void]$sb.Append('</table><p style="color:#475569;">Machine: ' + (& $enc $env:COMPUTERNAME) + ' | ' + (Get-Date -Format 'dd-MMM-yyyy HH:mm:ss') +
        '<br/>Runbook: docs/security/INCIDENT_RESPONSE_AND_KEY_ROTATION_RUNBOOK.md</p></div>')
    $sb.ToString()
}

function Test-Http([string]$url) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec $HttpTimeoutSec -MaximumRedirection 0 -ErrorAction Stop
        return @{ Ok = ($r.StatusCode -lt 500); Detail = "HTTP $($r.StatusCode)" }
    } catch {
        $resp = $_.Exception.Response
        if ($resp) {
            $code = [int]$resp.StatusCode
            return @{ Ok = ($code -lt 500); Detail = "HTTP $code" }
        }
        return @{ Ok = $false; Detail = $_.Exception.GetType().Name + ': no response' }
    }
}

function Get-Checks {
    $checks = New-Object System.Collections.Generic.List[object]
    foreach ($svc in 'W3SVC', 'WAS') {
        $s = Get-Service -Name $svc -ErrorAction SilentlyContinue
        $ok = $s -and $s.Status -eq 'Running'
        $checks.Add([pscustomobject]@{ Check = "Service $svc"; Ok = [bool]$ok; Detail = $(if ($s) { [string]$s.Status } else { 'not installed' }) })
    }

    $iisModule = $false
    try { Import-Module WebAdministration -ErrorAction Stop; $iisModule = $true } catch { }
    foreach ($site in $Sites) {
        if ($iisModule) {
            try {
                $pool = (Get-Item ("IIS:\Sites\" + $site.Name) -ErrorAction Stop).applicationPool
                $poolState = (Get-WebAppPoolState -Name $pool -ErrorAction Stop).Value
                $checks.Add([pscustomobject]@{ Check = "App pool $pool"; Ok = ($poolState -eq 'Started'); Detail = $poolState })
                $siteState = (Get-WebsiteState -Name $site.Name -ErrorAction Stop).Value
                $checks.Add([pscustomobject]@{ Check = "Site $($site.Name)"; Ok = ($siteState -eq 'Started'); Detail = $siteState })
            } catch {
                $checks.Add([pscustomobject]@{ Check = "Site $($site.Name)"; Ok = $false; Detail = 'not found in IIS' })
            }
        }
        $h = Test-Http $site.Url
        $checks.Add([pscustomobject]@{ Check = "HTTP $($site.Name)"; Ok = $h.Ok; Detail = "$($site.Url) -> $($h.Detail)" })
    }
    if (-not $iisModule) {
        $checks.Add([pscustomobject]@{ Check = 'IIS management module'; Ok = $false; Detail = 'WebAdministration unavailable (run as administrator / SYSTEM)' })
    }

    $disks = @(Get-CimInstance Win32_LogicalDisk -Filter 'DriveType=3' -ErrorAction SilentlyContinue)
    if ($mail.DiskDrives.Count -gt 0) { $disks = @($disks | Where-Object { $mail.DiskDrives -contains $_.DeviceID }) }
    foreach ($d in $disks) {
        if (-not $d.Size) { continue }
        $freePct = [math]::Round($d.FreeSpace * 100.0 / $d.Size, 1)
        $detail = '{0} GB free of {1} GB ({2}%); alert at {3}% free or less' -f [math]::Round($d.FreeSpace / 1GB, 1), [math]::Round($d.Size / 1GB, 1), $freePct, $mail.DiskMinPercent
        $checks.Add([pscustomobject]@{ Check = "Disk $($d.DeviceID)"; Ok = ($freePct -gt $mail.DiskMinPercent); Detail = $detail })
    }
    $checks
}

function Get-NewIisErrorEvents([datetime]$since) {
    $events = @()
    $filters = @(
        @{ LogName = 'System';      ProviderName = 'Microsoft-Windows-WAS', 'Microsoft-Windows-IIS-W3SVC'; Level = 1, 2; StartTime = $since },
        @{ LogName = 'Application'; ProviderName = 'IIS AspNetCore Module V2', 'IIS AspNetCore Module', '.NET Runtime', 'Application Error'; Level = 1, 2; StartTime = $since }
    )
    foreach ($f in $filters) {
        try { $events += Get-WinEvent -FilterHashtable $f -MaxEvents 20 -ErrorAction Stop } catch { }
    }
    $events | Where-Object {
        $_.ProviderName -notin '.NET Runtime', 'Application Error' -or $_.Message -match 'Homeocentrum|w3wp'
    } | Sort-Object TimeCreated
}

function Read-State {
    if (Test-Path $StatePath) {
        try { return Get-Content -Raw $StatePath | ConvertFrom-Json } catch { }
    }
    [pscustomobject]@{ LastRunUtc = (Get-Date).ToUniversalTime().AddMinutes(-10).ToString('o'); Down = [pscustomobject]@{} }
}

try {
    $mail = Get-MailSettings
    Write-Log 'INFO' ("Mode=$Mode. Mail recipients configured: " + $mail.Recipients.Count)

    if ($Mode -eq 'Startup') {
        $os = Get-CimInstance Win32_OperatingSystem
        $boot = $os.LastBootUpTime
        $unexpected = $null
        try { $unexpected = Get-WinEvent -FilterHashtable @{ LogName = 'System'; Id = 6008; StartTime = $boot.AddMinutes(-5) } -MaxEvents 1 -ErrorAction Stop } catch { }
        Start-Sleep -Seconds 90
        $rows = @(
            [pscustomobject]@{ Check = 'Boot time'; Status = 'INFO'; Detail = $boot.ToString('dd-MMM-yyyy HH:mm:ss') },
            [pscustomobject]@{ Check = 'Previous shutdown'; Status = $(if ($unexpected) { 'UNEXPECTED' } else { 'normal' }); Detail = $(if ($unexpected) { 'Power loss, crash or hard reset before this boot' } else { 'Clean shutdown or restart' }) }
        )
        foreach ($c in Get-Checks) { $rows += [pscustomobject]@{ Check = $c.Check; Status = $(if ($c.Ok) { 'OK' } else { 'DOWN' }); Detail = $c.Detail } }
        $title = 'Homeocentrum server restarted' + $(if ($unexpected) { ' after an unexpected shutdown' } else { '' })
        Send-AlertMail $mail ("Homeocentrum ALERT - " + $title) (ConvertTo-AlertHtml $title '#b45309' $rows)
        return
    }

    $state = Read-State
    $nowUtc = (Get-Date).ToUniversalTime()
    $down = @{}
    if ($state.Down) { $state.Down.PSObject.Properties | ForEach-Object { $down[$_.Name] = $_.Value } }

    $checks = @(Get-Checks)
    $newlyDown = @(); $stillDown = @(); $recovered = @()
    foreach ($c in $checks) {
        if (-not $c.Ok) {
            if ($down.ContainsKey($c.Check)) {
                $stillDown += $c
            } else {
                $newlyDown += $c
                $down[$c.Check] = [pscustomobject]@{ SinceUtc = $nowUtc.ToString('o'); LastMailUtc = $null }
            }
        } elseif ($down.ContainsKey($c.Check)) {
            $recovered += [pscustomobject]@{ Check = $c.Check; Ok = $true; Detail = $c.Detail; SinceUtc = $down[$c.Check].SinceUtc }
            $down.Remove($c.Check)
        }
    }
    foreach ($c in $checks) { Write-Log $(if ($c.Ok) { 'OK' } else { 'DOWN' }) ("{0}: {1}" -f $c.Check, $c.Detail) }

    $reminderDue = $stillDown | Where-Object {
        $last = $down[$_.Check].LastMailUtc
        -not $last -or ($nowUtc - [datetime]::Parse($last).ToUniversalTime()).TotalMinutes -ge $ReminderMinutes
    }

    if ($newlyDown.Count -gt 0 -or @($reminderDue).Count -gt 0) {
        $rows = @()
        foreach ($c in $newlyDown) { $rows += [pscustomobject]@{ Check = $c.Check; Status = 'DOWN (new)'; Detail = $c.Detail } }
        foreach ($c in $stillDown) {
            $mins = [int]($nowUtc - [datetime]::Parse($down[$c.Check].SinceUtc).ToUniversalTime()).TotalMinutes
            $rows += [pscustomobject]@{ Check = $c.Check; Status = "DOWN for $mins min"; Detail = $c.Detail }
        }
        $first = if ($newlyDown.Count -gt 0) { $newlyDown[0].Check } else { @($reminderDue)[0].Check }
        $title = if ($rows.Count -eq 1) { "$first is down" } else { "$($rows.Count) checks down ($first ...)" }
        if ($newlyDown.Count -eq 0) { $title = "Still down: $title" }
        Send-AlertMail $mail ("Homeocentrum ALERT - " + $title) (ConvertTo-AlertHtml $title '#b91c1c' $rows)
        foreach ($c in @($newlyDown) + @($stillDown)) { $down[$c.Check].LastMailUtc = $nowUtc.ToString('o') }
    }

    if ($recovered.Count -gt 0) {
        $rows = foreach ($c in $recovered) {
            $mins = [int]($nowUtc - [datetime]::Parse($c.SinceUtc).ToUniversalTime()).TotalMinutes
            [pscustomobject]@{ Check = $c.Check; Status = "RECOVERED after $mins min"; Detail = $c.Detail }
        }
        $title = if ($recovered.Count -eq 1) { "$($recovered[0].Check) recovered" } else { "$($recovered.Count) checks recovered" }
        Send-AlertMail $mail ("Homeocentrum RECOVERED - " + $title) (ConvertTo-AlertHtml $title '#15803d' $rows)
    }

    $since = [datetime]::Parse($state.LastRunUtc).ToUniversalTime().ToLocalTime()
    $events = @(Get-NewIisErrorEvents $since)
    if ($events.Count -gt 0) {
        $rows = foreach ($e in $events | Select-Object -Last 10) {
            $text = ($e.Message -replace '\s+', ' ')
            if ($text.Length -gt 300) { $text = $text.Substring(0, 300) + '...' }
            [pscustomobject]@{ Check = "$($e.ProviderName) #$($e.Id)"; Status = $e.TimeCreated.ToString('dd-MMM HH:mm:ss'); Detail = $text }
        }
        $title = "$($events.Count) IIS / ASP.NET Core error event(s) in the Windows event log"
        Send-AlertMail $mail ("Homeocentrum ALERT - " + $title) (ConvertTo-AlertHtml $title '#b45309' $rows)
    }

    if (-not $DryRun) {
        $out = [pscustomobject]@{ LastRunUtc = $nowUtc.ToString('o'); Down = [pscustomobject]$down }
        $out | ConvertTo-Json -Depth 5 | Set-Content -Path $StatePath -Encoding UTF8
    }
} catch {
    $msg = $_.Exception.Message
    if ($msg.Length -gt 200) { $msg = $msg.Substring(0, 200) }
    Write-Log 'ERROR' ("Watchdog failed: " + $_.Exception.GetType().Name + ': ' + ($msg -replace '\s+', ' '))
    exit 1
}
