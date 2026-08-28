param(
    [ValidatePattern('^(?:\d{1,3}\.){3}\d{1,3}$')]
    [string]$TargetIp = '127.0.0.1'
)

$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'PowerShell을 관리자 권한으로 실행하세요.'
}

$octets = $TargetIp.Split('.') | ForEach-Object { [int]$_ }
if ($octets.Where({ $_ -lt 0 -or $_ -gt 255 }).Count -gt 0) {
    throw '올바른 IPv4 주소가 아닙니다.'
}

$hostsPath = Join-Path $env:SystemRoot 'System32\drivers\etc\hosts'
$domain = 'error-dungeon.local'
$lines = Get-Content -LiteralPath $hostsPath
$filtered = $lines | Where-Object { $_ -notmatch "(^|\s)$([regex]::Escape($domain))(\s|$)" }
$updated = @($filtered) + "$TargetIp`t$domain"
Set-Content -LiteralPath $hostsPath -Value $updated -Encoding ascii
ipconfig /flushdns | Out-Null
Get-Content -LiteralPath $hostsPath | Select-String -SimpleMatch $domain
