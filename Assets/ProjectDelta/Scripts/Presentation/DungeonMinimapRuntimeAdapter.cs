using System; // 문자열 비교 기능
using System.Reflection; // 기존 컨트롤러 빌더 호출
using System.Text; // 상태 지문 생성
using UnityEngine; // Unity 런타임 기능

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public sealed class DungeonMinimapRuntimeAdapter : MonoBehaviour // 기존 미니맵과 UGUI 연결
    {
        private const string ControllerTypeName =
            "DungeonMinimapController"; // 대상 컨트롤러 이름

        private const string RuntimeMethodName =
            "BuildDungeonMinimapRuntimeUi142"; // 자동 변환 빌더 이름

        private static DungeonMinimapRuntimeAdapter instance; // 전역 연결기 인스턴스

        private MonoBehaviour controller; // 실제 미니맵 컨트롤러
        private MethodInfo runtimeUiBuilder; // 변환된 UI 빌더
        private DungeonMinimapRuntimeView view; // 런타임 UGUI 화면
        private string lastFingerprint = string.Empty; // 마지막 화면 지문
        private string lastRuntimeError = string.Empty; // 마지막 런타임 오류
        private float nextControllerSearchTime; // 다음 검색 시간
        private bool isBuilding; // 중복 빌드 방지

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 장면 로드 후 자동 시작
        private static void Bootstrap() // 자동 연결기 생성
        {
            if (instance != null) // 기존 연결기 확인
            {
                return; // 중복 생성 방지
            }

            GameObject host =
                new GameObject(
                    "DungeonMinimapRuntimeAdapter"); // 연결기 호스트 생성

            DontDestroyOnLoad(
                host); // 장면 전환 유지

            instance =
                host.AddComponent<DungeonMinimapRuntimeAdapter>(); // 연결기 컴포넌트 추가
        }

        private void Awake() // 연결기 초기 준비
        {
            if (instance != null
                && instance != this) // 중복 연결기 확인
            {
                Destroy(
                    gameObject); // 중복 호스트 제거
                return; // 중복 초기화 종료
            }

            instance =
                this; // 현재 인스턴스 등록

            view =
                gameObject.AddComponent<DungeonMinimapRuntimeView>(); // 미니맵 View 추가

            view.Initialize(); // UGUI 화면 초기화
        }

        private void Update() // 미니맵 상태를 UGUI에 반영
        {
            EnsureController(); // 현재 미니맵 컨트롤러 연결

            if (controller == null
                || runtimeUiBuilder == null) // 연결 가능 상태 확인
            {
                if (view != null) // View 존재 확인
                {
                    view.Hide(); // 대상 없으면 화면 숨김
                }

                lastFingerprint =
                    string.Empty; // 화면 지문 초기화
                return; // 갱신 종료
            }

            DungeonMinimapRuntimeFrame currentFrame =
                BuildFrame(); // 현재 지도 프레임 생성

            if (currentFrame == null
                || currentFrame.Root == null
                || currentFrame.Root.Children.Count == 0) // 표시할 지도 확인
            {
                view.Hide(); // 빈 지도 숨김
                lastFingerprint = string.Empty; // 화면 지문 초기화
                return; // 갱신 종료
            }

            string fingerprint =
                BuildFingerprint(
                    currentFrame); // 현재 표시 상태 지문 생성

            if (!string.Equals(
                    fingerprint,
                    lastFingerprint,
                    StringComparison.Ordinal)
                || !view.IsVisible) // 실제 표시 상태 변경 확인
            {
                lastFingerprint =
                    fingerprint; // 새 지문 저장

                view.Show(
                    currentFrame); // 상태 변경 시점 UGUI 재생성
            }
        }

        private void EnsureController() // 장면의 미니맵 컨트롤러 검색
        {
            if (controller != null
                && runtimeUiBuilder != null
                && controller.gameObject.scene.IsValid()
                && controller.gameObject.activeInHierarchy
                && controller.enabled) // 기존 연결 상태 확인
            {
                return; // 현재 연결 유지
            }

            if (Time.unscaledTime < nextControllerSearchTime) // 검색 주기 확인
            {
                return; // 다음 검색 시점까지 대기
            }

            nextControllerSearchTime =
                Time.unscaledTime + 0.5f; // 다음 검색 시간 예약

            MonoBehaviour[] behaviours =
                Resources.FindObjectsOfTypeAll<MonoBehaviour>(); // 로드된 컴포넌트 조회

            for (int index = 0;
                 index < behaviours.Length;
                 index++) // 전체 컴포넌트 순회
            {
                MonoBehaviour behaviour =
                    behaviours[index]; // 현재 컴포넌트 조회

                if (behaviour == null
                    || !behaviour.gameObject.scene.IsValid()) // 실제 장면 오브젝트 확인
                {
                    continue; // 프리팹과 빈 객체 제외
                }

                if (!string.Equals(
                        behaviour.GetType().Name,
                        ControllerTypeName,
                        StringComparison.Ordinal)) // 타입 이름 확인
                {
                    continue; // 다른 컴포넌트 제외
                }

                controller =
                    behaviour; // 대상 컨트롤러 저장

                runtimeUiBuilder =
                    behaviour.GetType().GetMethod(
                        RuntimeMethodName,
                        BindingFlags.Instance
                        | BindingFlags.Public
                        | BindingFlags.NonPublic); // 변환 빌더 연결

                lastFingerprint =
                    string.Empty; // 컨트롤러 교체 시 지문 초기화
                return; // 검색 종료
            }

            controller =
                null; // 대상 컨트롤러 해제

            runtimeUiBuilder =
                null; // 빌더 참조 해제
        }

        private DungeonMinimapRuntimeFrame BuildFrame() // 기존 미니맵 표시 코드를 프레임으로 실행
        {
            if (isBuilding) // 중복 빌드 확인
            {
                return null; // 재진입 차단
            }

            isBuilding =
                true; // 빌드 상태 설정

            DungeonMinimapRuntimeGuiProxy.BeginFrame(); // 프록시 기록 시작

            try
            {
                runtimeUiBuilder.Invoke(
                    controller,
                    null); // 기존 OnGUI 본문 실행
            }
            catch (TargetInvocationException exception)
            {
                ReportRuntimeError(
                    exception.InnerException ?? exception); // 내부 오류 보고
            }
            catch (Exception exception)
            {
                ReportRuntimeError(
                    exception); // 기타 오류 보고
            }
            finally
            {
                isBuilding =
                    false; // 빌드 상태 해제
            }

            return DungeonMinimapRuntimeGuiProxy.EndFrame(); // 완성 프레임 반환
        }

        private void ReportRuntimeError(
            Exception exception) // 런타임 변환 오류 보고
        {
            string message =
                exception != null
                    ? exception.ToString()
                    : "알 수 없는 오류"; // 오류 문자열 생성

            if (string.Equals(
                    message,
                    lastRuntimeError,
                    StringComparison.Ordinal)) // 동일 오류 확인
            {
                return; // 반복 로그 차단
            }

            lastRuntimeError =
                message; // 새 오류 저장

            Debug.LogError(
                "[Day142] DungeonMinimap UGUI 변환 오류\n"
                + message,
                this); // 오류 로그 출력
        }

        private static string BuildFingerprint(
            DungeonMinimapRuntimeFrame currentFrame) // 표시 상태 지문 생성
        {
            StringBuilder builder =
                new StringBuilder(4096); // 지문 버퍼 생성

            AppendNodeFingerprint(
                builder,
                currentFrame.Root); // 전체 노드 지문 기록

            return builder.ToString(); // 최종 지문 반환
        }

        private static void AppendNodeFingerprint(
            StringBuilder builder,
            DungeonMinimapRuntimeNode node) // 노드 지문 재귀 기록
        {
            if (node == null) // 빈 노드 확인
            {
                builder.Append("null;"); // 빈 상태 기록
                return; // 빈 노드 종료
            }

            builder.Append((int)node.Kind).Append('|'); // 종류 기록
            builder.Append(node.Rect.x.ToString("0.###")).Append(','); // X 기록
            builder.Append(node.Rect.y.ToString("0.###")).Append(','); // Y 기록
            builder.Append(node.Rect.width.ToString("0.###")).Append(','); // 너비 기록
            builder.Append(node.Rect.height.ToString("0.###")).Append('|'); // 높이 기록
            builder.Append(node.Color.r.ToString("0.###")).Append(','); // 빨강 기록
            builder.Append(node.Color.g.ToString("0.###")).Append(','); // 초록 기록
            builder.Append(node.Color.b.ToString("0.###")).Append(','); // 파랑 기록
            builder.Append(node.Color.a.ToString("0.###")).Append('|'); // 알파 기록
            builder.Append(node.FontSize).Append('|'); // 글자 크기 기록
            builder.Append((int)node.FontStyle).Append('|'); // 글자 스타일 기록
            builder.Append((int)node.Alignment).Append('|'); // 글자 정렬 기록
            builder.Append(node.RotationAngle.ToString("0.###")).Append('|'); // 회전 기록
            builder.Append(node.Text ?? string.Empty).Append('|'); // 문자열 기록
            builder.Append(
                node.Texture != null
                    ? node.Texture.GetInstanceID()
                    : 0).Append(';'); // 텍스처 식별자 기록

            for (int index = 0;
                 index < node.Children.Count;
                 index++) // 자식 노드 순회
            {
                AppendNodeFingerprint(
                    builder,
                    node.Children[index]); // 자식 지문 기록
            }
        }
    }
}
