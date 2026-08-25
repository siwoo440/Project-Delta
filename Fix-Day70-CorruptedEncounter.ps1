$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$targetPath = Join-Path $projectRoot "Assets\ProjectDelta\Scripts\Presentation\ExplorationMonsterEncounterController.cs"

if (-not (Test-Path $targetPath))
{
    Write-Host "[실패] ExplorationMonsterEncounterController.cs를 찾지 못했습니다."
    Write-Host "이 파일을 Project Delta 프로젝트 루트에 둔 상태로 실행하세요."
    exit 1
}

$source = [System.IO.File]::ReadAllText($targetPath)

$newLine = "`n"
if ($source.Contains("`r`n"))
{
    $newLine = "`r`n"
}

$pattern = '(damageResult\.Damage\);)\\n\\n([ \t]+BattleDefeatService\.RecordAppliedDamage\()\\n([ \t]+actor,)\\n([ \t]+target,)\\n([ \t]+appliedDamage\); // 70일차 마지막 실제 공격자 기록)'
$regex = [System.Text.RegularExpressions.Regex]::new($pattern)
$matches = $regex.Matches($source)

if ($matches.Count -eq 0)
{
    if ($source.Contains('\n\n') -or $source.Contains('RecordAppliedDamage(\n'))
    {
        Write-Host "[실패] 잘못된 \n 문자가 남아 있지만 예상한 형태와 다릅니다."
        Write-Host "파일을 임의로 수정하지 않고 종료합니다."
        exit 1
    }

    Write-Host "[완료] 이미 복구된 상태입니다. 변경할 내용이 없습니다."
    exit 0
}

if ($matches.Count -ne 2)
{
    Write-Host "[실패] 손상된 공격 코드 블록이 예상한 2개가 아닙니다. 발견 개수: $($matches.Count)"
    Write-Host "안전을 위해 파일을 수정하지 않았습니다."
    exit 1
}

$backupPath = "$targetPath.day70-backup"
if (-not (Test-Path $backupPath))
{
    Copy-Item $targetPath $backupPath
    Write-Host "[백업] $backupPath"
}

$fixed = $regex.Replace(
    $source,
    {
        param($match)

        return $match.Groups[1].Value `
            + $newLine + $newLine `
            + $match.Groups[2].Value + $newLine `
            + $match.Groups[3].Value + $newLine `
            + $match.Groups[4].Value + $newLine `
            + $match.Groups[5].Value
    }
)

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($targetPath, $fixed, $utf8NoBom)

Write-Host "[완료] ExplorationMonsterEncounterController.cs의 잘못된 \n 문자 2곳을 복구했습니다."
Write-Host "[다음] Unity로 돌아가 컴파일을 확인하세요."
