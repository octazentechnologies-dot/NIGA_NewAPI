<#
.SYNOPSIS
  Registers (or removes) the Homeocentrum IIS watchdog scheduled tasks. Run as administrator on the IIS server, after deploy.

.DESCRIPTION
  Copies Watch-Homeocentrum.ps1 to C:\ProgramData\Homeocentrum\Watchdog and creates two SYSTEM tasks:
    Homeocentrum Watchdog          every -IntervalMinutes (down / still down / recovered mails, IIS error events)
    Homeocentrum Watchdog Startup  at boot ("server restarted", flags an unexpected shutdown such as power loss)

.EXAMPLE
  .\Register-HomeocentrumWatchdog.ps1
  .\Register-HomeocentrumWatchdog.ps1 -Remove
#>
[CmdletBinding()]
param(
    [int]$IntervalMinutes = 5,
    [string]$AppSettingsPath = 'C:\inetpub\homeocentrum\newapi\appsettings.json',
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'
$taskNames = 'Homeocentrum Watchdog', 'Homeocentrum Watchdog Startup'

if ($Remove) {
    foreach ($t in $taskNames) { Unregister-ScheduledTask -TaskName $t -Confirm:$false -ErrorAction SilentlyContinue }
    Write-Output 'Watchdog tasks removed.'
    return
}

$identity = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $identity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run this script as administrator.' }

$dir = 'C:\ProgramData\Homeocentrum\Watchdog'
New-Item -ItemType Directory -Path $dir -Force | Out-Null
$script = Join-Path $dir 'Watch-Homeocentrum.ps1'
Copy-Item -Path (Join-Path $PSScriptRoot 'Watch-Homeocentrum.ps1') -Destination $script -Force

# State and logs hold no secrets, but only SYSTEM and administrators need them.
icacls $dir /inheritance:r /grant:r 'SYSTEM:(OI)(CI)F' 'Administrators:(OI)(CI)F' | Out-Null

$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 10)
$baseArgs = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$script`" -AppSettingsPath `"$AppSettingsPath`""

$check = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "$baseArgs -Mode Check"
$every = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes $IntervalMinutes)
Register-ScheduledTask -TaskName $taskNames[0] -Action $check -Trigger $every -Principal $principal -Settings $settings -Force | Out-Null

$startup = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "$baseArgs -Mode Startup"
Register-ScheduledTask -TaskName $taskNames[1] -Action $startup -Trigger (New-ScheduledTaskTrigger -AtStartup) -Principal $principal -Settings $settings -Force | Out-Null

Write-Output "Registered '$($taskNames[0])' (every $IntervalMinutes min) and '$($taskNames[1])' (at boot). Logs: $dir"
