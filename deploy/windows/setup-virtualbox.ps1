param(
    [Parameter(Mandatory = $true)]
    [string]$VmName
)

$ErrorActionPreference = 'Stop'
$vboxManage = Get-Command VBoxManage.exe -ErrorAction Stop
$state = & $vboxManage.Source showvminfo $VmName --machinereadable | Select-String '^VMState='
if ($state -notmatch 'poweroff|aborted|saved') {
    throw '포트포워딩을 설정하기 전에 VM을 종료하세요.'
}

& $vboxManage.Source modifyvm $VmName --nic1 nat
& $vboxManage.Source modifyvm $VmName --natpf1 delete 'error-dungeon-http' 2>$null
& $vboxManage.Source modifyvm $VmName --natpf1 delete 'error-dungeon-ssh' 2>$null
& $vboxManage.Source modifyvm $VmName --natpf1 'error-dungeon-http,tcp,127.0.0.1,8080,,80'
& $vboxManage.Source modifyvm $VmName --natpf1 'error-dungeon-ssh,tcp,127.0.0.1,2222,,22'
& $vboxManage.Source showvminfo $VmName | Select-String 'NIC 1 Rule'
