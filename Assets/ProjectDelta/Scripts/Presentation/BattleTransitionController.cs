using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 88일차: 탐험에서 전투로 넘어갈 때 화면 전체를 검게 덮었다가 다시 밝히는 전환을 담당한다.
    [DisallowMultipleComponent]
    public sealed class BattleTransitionController : MonoBehaviour
    {
        // 화면이 완전히 검게 되는 데 걸리는 기본 시간이다.
        private const float FadeOutSeconds = 0.20f;

        // 전투 HUD가 검은 화면 뒤에서 한 프레임 이상 준비될 수 있도록 유지하는 시간이다.
        private const float BlackHoldSeconds = 0.10f;

        // 전투 화면이 다시 보이기까지 걸리는 기본 시간이다.
        private const float FadeInSeconds = 0.20f;

        // 기존 F1 디버그 로그와 전투 속도 HUD보다 항상 앞에 보이도록 높은 정렬 순서를 사용한다.
        private const int CanvasSortingOrder = 20000;

        // 현재 런타임에서 하나만 유지되는 전환 컨트롤러를 외부에서 안전하게 조회한다.
        public static BattleTransitionController Current { get; private set; }

        // 현재 Fade Out 또는 Fade In이 진행 중인지 외부에서 확인한다.
        public bool IsTransitioning { get; private set; }

        // 런타임에 자동 생성하는 검은 화면의 투명도와 입력 차단을 제어한다.
        private CanvasGroup overlayGroup;

        // Scene에 수동 배치하지 않아도 Scene 로드 직후 전환 오브젝트를 준비한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            GetOrCreate();
        }

        // 이미 존재하면 재사용하고 없으면 DontDestroyOnLoad 런타임 오브젝트를 만든다.
        public static BattleTransitionController GetOrCreate()
        {
            if (Current != null)
            {
                return Current;
            }

            BattleTransitionController existing =
                FindFirstObjectByType<BattleTransitionController>();

            if (existing != null)
            {
                Current =
                    existing;

                return Current;
            }

            GameObject hostObject =
                new GameObject(
                    nameof(BattleTransitionController));

            DontDestroyOnLoad(
                hostObject);

            Current =
                hostObject.AddComponent<BattleTransitionController>();

            return Current;
        }

        // 테스트와 실제 Fade 루틴이 같은 보간 규칙을 사용하도록 알파 계산을 한 곳에 둔다.
        public static float EvaluateTransitionAlpha(
            float startAlpha,
            float targetAlpha,
            float elapsedSeconds,
            float durationSeconds)
        {
            float clampedStart =
                Mathf.Clamp01(
                    startAlpha);

            float clampedTarget =
                Mathf.Clamp01(
                    targetAlpha);

            if (durationSeconds <= 0f)
            {
                return clampedTarget;
            }

            float progress =
                Mathf.Clamp01(
                    Mathf.Max(
                        0f,
                        elapsedSeconds)
                    / durationSeconds);

            return Mathf.Lerp(
                clampedStart,
                clampedTarget,
                progress);
        }

        // 중복 인스턴스를 제거하고 최초 인스턴스에서 런타임 Canvas를 만든다.
        private void Awake()
        {
            if (Current != null
                && Current != this)
            {
                Destroy(
                    gameObject);

                return;
            }

            Current =
                this;

            DontDestroyOnLoad(
                gameObject);

            CreateRuntimeOverlay();
            ForceReveal();
        }

        // 현재 인스턴스가 제거될 때 static 참조가 죽은 오브젝트를 가리키지 않도록 정리한다.
        private void OnDestroy()
        {
            if (Current == this)
            {
                Current =
                    null;
            }
        }

        // 화면을 0 → 1 알파로 바꾸면서 전환 중 입력을 차단한다.
        public IEnumerator FadeToBlack()
        {
            while (IsTransitioning)
            {
                yield return null;
            }

            IsTransitioning =
                true;

            SetInputBlocked(
                true);

            yield return FadeRoutine(
                1f,
                FadeOutSeconds);

            IsTransitioning =
                false;
        }

        // 완전 검정 상태를 짧게 유지해 Battle HUD가 검은 화면 뒤에서 준비될 시간을 확보한다.
        public IEnumerator HoldBlack()
        {
            if (BlackHoldSeconds <= 0f)
            {
                yield break;
            }

            yield return new WaitForSecondsRealtime(
                BlackHoldSeconds);
        }

        // 화면을 1 → 0 알파로 되돌리고 전환이 끝난 뒤 UI 입력 차단을 해제한다.
        public IEnumerator FadeFromBlack()
        {
            while (IsTransitioning)
            {
                yield return null;
            }

            IsTransitioning =
                true;

            SetInputBlocked(
                true);

            yield return FadeRoutine(
                0f,
                FadeInSeconds);

            SetInputBlocked(
                false);

            IsTransitioning =
                false;
        }

        // Encounter가 비정상 종료되거나 Controller가 비활성화될 때 검은 화면이 남지 않게 즉시 복원한다.
        public void ForceReveal()
        {
            StopAllCoroutines();

            IsTransitioning =
                false;

            SetOverlayAlpha(
                0f);

            SetInputBlocked(
                false);
        }

        // 현재 알파에서 목표 알파까지 unscaled time으로 보간해 전투 배속과 독립적으로 전환한다.
        private IEnumerator FadeRoutine(
            float targetAlpha,
            float durationSeconds)
        {
            if (overlayGroup == null)
            {
                yield break;
            }

            float startAlpha =
                overlayGroup.alpha;

            if (durationSeconds <= 0f)
            {
                SetOverlayAlpha(
                    targetAlpha);

                yield break;
            }

            float elapsedSeconds =
                0f;

            while (elapsedSeconds < durationSeconds)
            {
                elapsedSeconds +=
                    Time.unscaledDeltaTime;

                float alpha =
                    EvaluateTransitionAlpha(
                        startAlpha,
                        targetAlpha,
                        elapsedSeconds,
                        durationSeconds);

                SetOverlayAlpha(
                    alpha);

                yield return null;
            }

            SetOverlayAlpha(
                targetAlpha);
        }

        // 별도의 Scene/Prefab 작업 없이 화면 전체를 덮는 Canvas와 검은 Image를 런타임에 생성한다.
        private void CreateRuntimeOverlay()
        {
            if (overlayGroup != null)
            {
                return;
            }

            GameObject canvasObject =
                new GameObject(
                    "BattleTransitionCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(
                transform,
                false);

            Canvas canvas =
                canvasObject.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder =
                CanvasSortingOrder;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f);

            scaler.matchWidthOrHeight =
                0.5f;

            GameObject overlayObject =
                new GameObject(
                    "BlackOverlay",
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(Image));

            overlayObject.transform.SetParent(
                canvasObject.transform,
                false);

            RectTransform rectTransform =
                overlayObject.GetComponent<RectTransform>();

            rectTransform.anchorMin =
                Vector2.zero;

            rectTransform.anchorMax =
                Vector2.one;

            rectTransform.offsetMin =
                Vector2.zero;

            rectTransform.offsetMax =
                Vector2.zero;

            Image image =
                overlayObject.GetComponent<Image>();

            image.color =
                Color.black;

            image.raycastTarget =
                true;

            overlayGroup =
                overlayObject.GetComponent<CanvasGroup>();

            overlayGroup.alpha =
                0f;

            overlayGroup.interactable =
                false;

            overlayGroup.blocksRaycasts =
                false;
        }

        // 검은 Image의 투명도는 항상 0~1 범위로 제한한다.
        private void SetOverlayAlpha(
            float alpha)
        {
            if (overlayGroup == null)
            {
                return;
            }

            overlayGroup.alpha =
                Mathf.Clamp01(
                    alpha);
        }

        // 전환 중에는 전체 화면 Image가 UI 입력을 막고 전환이 끝나면 다시 통과시킨다.
        private void SetInputBlocked(
            bool blocked)
        {
            if (overlayGroup == null)
            {
                return;
            }

            overlayGroup.blocksRaycasts =
                blocked;

            overlayGroup.interactable =
                blocked;
        }
    }
}
