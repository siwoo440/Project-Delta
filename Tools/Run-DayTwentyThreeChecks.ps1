param # 실행 옵션 시작
( # 옵션 목록 시작
    [ValidateSet('EditMode', 'PlayMode', 'Configure', 'Build', 'PlayerTests', 'Screenshot')] # 실행 종류 제한
    [string]$Check = 'EditMode', # 기본 상태 검증
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe' # 기준 편집기 경로
) # 옵션 목록 끝
$ErrorActionPreference = 'Stop' # 오류 발생 시 중단
$projectPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')) # 프로젝트 절대 경로
$artifactPath = Join-Path $projectPath 'Artifacts' # 검증 결과 폴더
New-Item -ItemType Directory -Path $artifactPath -Force | Out-Null # 결과 폴더 준비
$logPath = Join-Path $artifactPath ("day23-" + $Check + '.log') # 실행 기록 경로
$resultPath = Join-Path $artifactPath ("day23-" + $Check + '.xml') # 검증 결과 경로
if ($Check -in @('PlayerTests', 'Screenshot')) # 실제 플레이 화면 저장 검사
{ # 화면 저장 조건 시작
    $capturePath = Join-Path $artifactPath 'day23-play.png' # 최신 플레이 화면 경로
    if (Test-Path -LiteralPath $capturePath) # 이전 캡처 존재 검사
    { # 이전 캡처 조건 시작
        Remove-Item -LiteralPath $capturePath -Force # 오래된 검증 화면 제거
    } # 이전 캡처 조건 끝
    $env:PROJECT_DELTA_CAPTURE_PATH = $capturePath # 실행 파일에 화면 저장 경로 전달
} # 화면 저장 조건 끝
if ($Check -notin @('Configure', 'Build') -and (Test-Path -LiteralPath $resultPath)) # 이전 테스트 결과 검사
{ # 이전 결과 정리 시작
    Remove-Item -LiteralPath $resultPath -Force # 같은 실행 종류의 오래된 결과 제거
} # 이전 결과 정리 끝
$unityArguments = @('-batchmode', '-nographics', '-projectPath', $projectPath, '-logFile', $logPath) # 공통 실행 옵션
if ($Check -eq 'Screenshot') # 화면 없는 렌더링 검사
{ # 화면 렌더링 조건 시작
    $unityArguments = @('-batchmode', '-force-d3d11', '-projectPath', $projectPath, '-logFile', $logPath) # 그래픽 장치 사용
} # 화면 렌더링 조건 끝
if ($Check -eq 'Configure' -or $Check -eq 'Build') # 제작 도구 실행 검사
{ # 제작 도구 조건 시작
    $methodName = 'ProjectDelta.Editor.DayFourProjectSetup.Configure' # 시험 방과 지도 연결 작업
    if ($Check -eq 'Build') # 빌드 작업 검사
    { # 빌드 조건 시작
        $methodName = 'ProjectDelta.Editor.DayTwentyThreeProjectSetup.BuildWindows' # 23일차 Windows 빌드 작업
    } # 빌드 조건 끝
    $unityArguments += @('-quit', '-executeMethod', $methodName) # 제작 메서드 지정
} # 제작 도구 조건 끝
else # 테스트 실행 분기
{ # 테스트 분기 시작
    $platformName = $Check # 편집기 테스트 종류
    if ($Check -eq 'PlayerTests') # 실제 실행 파일 검증 검사
    { # 실행 파일 조건 시작
        $platformName = 'StandaloneWindows64' # Windows 테스트 대상
    } # 실행 파일 조건 끝
    if ($Check -eq 'Screenshot') # 화면 캡처 테스트 검사
    { # 캡처 테스트 조건 시작
        $platformName = 'PlayMode' # 편집기 플레이 테스트 지정
        $unityArguments += @('-testFilter', 'ProjectDelta.Tests.DungeonRuntimeGenerationTests.Inventory_FullOffer_ConfirmAndCancel') # 실제 이동 검증 지정
    } # 캡처 테스트 조건 끝
    $unityArguments += @('-runTests', '-testPlatform', $platformName, '-testResults', $resultPath) # 테스트 실행 옵션
} # 테스트 분기 끝
$quotedArguments = foreach ($unityArgument in $unityArguments) # 공백 경로 보호
{ # 인수 변환 시작
    '"' + $unityArgument + '"' # 공백을 포함한 인수 보호
} # 인수 변환 끝
$unityProcess = Start-Process -FilePath $UnityPath -ArgumentList ($quotedArguments -join ' ') -WindowStyle Hidden -PassThru # Unity 편집기 실행
$unityProcess.WaitForExit() # 편집기 본체 종료 대기
Get-Content -LiteralPath $logPath -Tail 18 | Select-String -NotMatch '^##utp:' | Select-Object -ExpandProperty Line # 마지막 실행 기록 출력
if ($Check -notin @('Configure', 'Build')) # 테스트 실행 결과 검사
{ # 결과 검사 시작
    if (!(Test-Path -LiteralPath $resultPath)) # 새 테스트 결과 누락 검사
    { # 누락 조건 시작
        throw '새 테스트 결과 XML 생성 실패' # 결과 없는 성공 종료 차단
    } # 누락 조건 끝
    [xml]$testResults = Get-Content -LiteralPath $resultPath # XML 결과 읽기
    $testResults.'test-run' | Select-Object result, total, passed, failed | Format-List # 통과 수 출력
    if ([int]$testResults.'test-run'.total -eq 0) # 빈 테스트 실행 검사
    { # 빈 실행 조건 시작
        throw '테스트가 한 개도 실행되지 않음' # 누락 검증 차단
    } # 빈 실행 조건 끝
    if ([int]$testResults.'test-run'.failed -gt 0 -or $testResults.'test-run'.result -ne 'Passed') # 실제 테스트 실패 검사
    { # 실패 조건 시작
        throw '테스트 실패 또는 미완료 결과' # 검증 실패 종료
    } # 실패 조건 끝
    if ($Check -in @('PlayerTests', 'Screenshot') -and !(Test-Path -LiteralPath $capturePath)) # 최신 실제 플레이 화면 검사
    { # 화면 누락 조건 시작
        throw '최신 플레이 화면 캡처 생성 실패' # 스크린샷 누락 방지
    } # 화면 누락 조건 끝
} # 결과 검사 끝
exit $unityProcess.ExitCode # Unity 종료 코드 전달
