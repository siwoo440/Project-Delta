param(
    [string]$ProjectRoot = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

$target = Join-Path `
    $ProjectRoot `
    "Assets\ProjectDelta\Scripts\Presentation\ExplorationMonsterEncounterController.cs"

if (-not (Test-Path -LiteralPath $target))
{
    Write-Host ""
    Write-Host "[Project Delta][93일차] 대상 파일을 찾지 못했습니다." -ForegroundColor Red
    Write-Host $target -ForegroundColor Red
    Write-Host ""
    Read-Host "Enter를 누르면 종료합니다"
    exit 1
}

$source = Get-Content `
    -LiteralPath $target `
    -Raw `
    -Encoding UTF8

$normalized = $source.Replace(
    "`r`n",
    "`n")

$badStatusCall = @"
            ApplyRoundStartStatusEffectsIfNeeded(
                actor);

"@

$oldPendingName =
    "battleSession.HasPendingActorsInCurrentRound"

$newPendingName =
    "battleSession.HasPendingActorsThisRound"

$statusCallCount =
    ([regex]::Matches(
        $normalized,
        [regex]::Escape($badStatusCall))).Count

$oldPendingCount =
    ([regex]::Matches(
        $normalized,
        [regex]::Escape($oldPendingName))).Count

$newPendingCountBefore =
    ([regex]::Matches(
        $normalized,
        [regex]::Escape($newPendingName))).Count

if ($statusCallCount -eq 0 `
    -and $oldPendingCount -eq 0)
{
    Write-Host ""
    Write-Host "[Project Delta][93일차] 이미 수정된 상태입니다." -ForegroundColor Green
    Write-Host "HasPendingActorsThisRound 발견 수: $newPendingCountBefore"
    Write-Host ""
    Read-Host "Enter를 누르면 종료합니다"
    exit 0
}

if ($statusCallCount -ne 1)
{
    Write-Host ""
    Write-Host "[Project Delta][93일차] 예상한 잘못된 상태이상 호출이 정확히 1개가 아닙니다." -ForegroundColor Red
    Write-Host "발견 수: $statusCallCount"
    Write-Host "파일을 수정하지 않았습니다."
    Write-Host ""
    Read-Host "Enter를 누르면 종료합니다"
    exit 2
}

if ($oldPendingCount -ne 1)
{
    Write-Host ""
    Write-Host "[Project Delta][93일차] 예상한 잘못된 BattleSession 속성이 정확히 1개가 아닙니다." -ForegroundColor Red
    Write-Host "발견 수: $oldPendingCount"
    Write-Host "파일을 수정하지 않았습니다."
    Write-Host ""
    Read-Host "Enter를 누르면 종료합니다"
    exit 3
}

# BattleSession.TryStartRound()가 이미 라운드 시작 상태이상을 적용하므로
# 아이템 사용 직전에 중복 호출하려던 잘못된 메서드 호출은 제거한다.
$normalized =
    $normalized.Replace(
        $badStatusCall,
        "")

# 실제 BattleSession API 이름으로 수정한다.
$normalized =
    $normalized.Replace(
        $oldPendingName,
        $newPendingName)

if ($normalized.Contains(
        "ApplyRoundStartStatusEffectsIfNeeded("))
{
    Write-Host ""
    Write-Host "[Project Delta][93일차] 잘못된 메서드 호출이 아직 남아 있어 저장을 중단합니다." -ForegroundColor Red
    Write-Host ""
    Read-Host "Enter를 누르면 종료합니다"
    exit 4
}

if ($normalized.Contains(
        $oldPendingName))
{
    Write-Host ""
    Write-Host "[Project Delta][93일차] 잘못된 BattleSession 속성이 아직 남아 있어 저장을 중단합니다." -ForegroundColor Red
    Write-Host ""
    Read-Host "Enter를 누르면 종료합니다"
    exit 5
}

$utf8WithoutBom =
    New-Object `
        System.Text.UTF8Encoding(
            $false)

[System.IO.File]::WriteAllText(
    $target,
    $normalized,
    $utf8WithoutBom)

Write-Host ""
Write-Host "[Project Delta][93일차] 전투 아이템 사용 컴파일 오류 수정 완료." -ForegroundColor Green
Write-Host "수정 1: 존재하지 않는 ApplyRoundStartStatusEffectsIfNeeded 호출 제거"
Write-Host "수정 2: HasPendingActorsInCurrentRound → HasPendingActorsThisRound"
Write-Host ""
Write-Host "Unity로 돌아가 Script Compilation이 끝날 때까지 기다린 뒤 Console을 확인하세요."
Write-Host ""
Read-Host "Enter를 누르면 종료합니다"
