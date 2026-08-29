param( # 프로젝트 경로 입력
    [string]$ProjectRoot = "." # 기본 프로젝트 루트
)

$TargetPath = Join-Path $ProjectRoot "Assets\ProjectDelta\Scripts\Presentation\DungeonMinimapController.cs" # 대상 파일 경로
$AliasLine = "using DungeonRunState = ProjectDelta.Domain.DungeonRunState;" # 타입 별칭 선언
$DomainUsing = "using ProjectDelta.Domain;" # 기준 using 선언

if (-not (Test-Path $TargetPath)) # 대상 파일 존재 확인
{
    Write-Error "파일을 찾을 수 없습니다: $TargetPath" # 오류 메시지 출력
    exit 1 # 실패 종료
}

$Content = [System.IO.File]::ReadAllText($TargetPath, [System.Text.Encoding]::UTF8) # 원본 코드 읽기

if ($Content.Contains($AliasLine)) # 기존 별칭 확인
{
    Write-Host "이미 수정되어 있습니다: $TargetPath" # 중복 수정 방지 안내
    exit 0 # 정상 종료
}

$BackupPath = "$TargetPath.bak" # 백업 파일 경로
[System.IO.File]::Copy($TargetPath, $BackupPath, $true) # 원본 파일 백업

if ($Content.Contains($DomainUsing)) # Domain using 존재 확인
{
    $Replacement = $DomainUsing + [Environment]::NewLine + $AliasLine # 별칭 삽입 문자열
    $Content = $Content.Replace($DomainUsing, $Replacement) # Domain using 바로 아래 삽입
}
else # Domain using 부재 처리
{
    $Content = $AliasLine + [Environment]::NewLine + $Content # 파일 상단에 별칭 삽입
}

[System.IO.File]::WriteAllText($TargetPath, $Content, New-Object System.Text.UTF8Encoding($false)) # 수정 코드 저장
Write-Host "수정 완료: $TargetPath" # 완료 메시지 출력
Write-Host "백업 생성: $BackupPath" # 백업 경로 출력
