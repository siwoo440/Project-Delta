using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectDelta.Presentation
{
    // 138일차: 기획서 8.1절 "울트라와이드 대응 세이프존 유지"·"중앙 플레이 영역 보존" -
    // 씬마다 카메라를 직접 편집하는 대신, 씬이 로드될 때마다 자동으로 Camera.main에
    // 이 컴포넌트를 붙여 기준 종횡비(16:9) 밖으로 벗어난 화면 영역은 필러박스/레터박스
    // 처리한다. 오버레이 UI(Canvas ScreenSpaceOverlay)는 카메라 rect와 무관하게 전체
    // 화면을 그대로 쓰므로 영향을 받지 않는다 - 3D 게임 화면(던전·전투)만 대상이다.
    public sealed class AspectRatioSafeAreaController : MonoBehaviour
    {
        private const float TargetAspect = 16f / 9f;

        private Camera targetCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded +=
                (scene, mode) => AttachToMainCamera();

            AttachToMainCamera();
        }

        private static void AttachToMainCamera()
        {
            Camera mainCamera =
                Camera.main;

            if (mainCamera == null)
            {
                return;
            }

            if (mainCamera.GetComponent<AspectRatioSafeAreaController>() == null)
            {
                mainCamera.gameObject.AddComponent<AspectRatioSafeAreaController>();
            }
        }

        private void Awake()
        {
            targetCamera =
                GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            ApplyLetterbox();
        }

        private void ApplyLetterbox()
        {
            if (targetCamera == null
                || Screen.height <= 0)
            {
                return;
            }

            float windowAspect =
                Screen.width / (float)Screen.height;

            float scaleHeight =
                windowAspect / TargetAspect;

            Rect rect =
                targetCamera.rect;

            if (scaleHeight < 1f)
            {
                // 화면이 기준(16:9)보다 좁다(세로로 김) - 위아래를 레터박스 처리한다.
                rect.width =
                    1f;

                rect.height =
                    scaleHeight;

                rect.x =
                    0f;

                rect.y =
                    (1f - scaleHeight) / 2f;
            }
            else
            {
                // 화면이 기준(16:9)보다 넓다(울트라와이드) - 좌우를 필러박스 처리해
                // 중앙 16:9 영역만 플레이 화면으로 유지한다.
                float scaleWidth =
                    1f / scaleHeight;

                rect.width =
                    scaleWidth;

                rect.height =
                    1f;

                rect.x =
                    (1f - scaleWidth) / 2f;

                rect.y =
                    0f;
            }

            targetCamera.rect =
                rect;
        }
    }
}
