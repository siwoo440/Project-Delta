using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 추가 작업: 3D 탐험 공간에서 몬스터 2D 일러스트가 플레이어 카메라를 향하도록 표시한다.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MonsterBillboardView : MonoBehaviour
    {
        private const string ResourceFolder =
            "MonsterSprites";

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float visualHeight = 2f;

        private Transform targetCamera;

        public bool HasSprite =>
            spriteRenderer != null
            && spriteRenderer.sprite != null;

        private void Awake()
        {
            ResolveSpriteRenderer();
            ResolveTargetCamera();
        }

        private void LateUpdate()
        {
            if (!HasSprite)
            {
                return;
            }

            ResolveTargetCamera();

            if (targetCamera == null)
            {
                return;
            }

            transform.rotation =
                CalculateYawRotation(
                    transform.position,
                    targetCamera.position,
                    transform.rotation);
        }

        public bool Configure(
            string monsterDefinitionId,
            float targetVisualHeight = 2f)
        {
            ResolveSpriteRenderer();

            visualHeight =
                Mathf.Max(
                    0.01f,
                    targetVisualHeight);

            Sprite sprite =
                string.IsNullOrEmpty(monsterDefinitionId)
                    ? null
                    : Resources.Load<Sprite>(
                        BuildResourcePath(
                            monsterDefinitionId));

            spriteRenderer.sprite =
                sprite;

            ApplyVisualScale();
            ResolveTargetCamera();

            return sprite != null;
        }

        public static string BuildResourcePath(
            string monsterDefinitionId)
        {
            if (string.IsNullOrEmpty(monsterDefinitionId))
            {
                return null;
            }

            return
                $"{ResourceFolder}/{monsterDefinitionId}";
        }

        public static Quaternion CalculateYawRotation(
            Vector3 billboardPosition,
            Vector3 targetPosition,
            Quaternion fallbackRotation)
        {
            Vector3 direction =
                targetPosition - billboardPosition;

            direction.y =
                0f;

            if (direction.sqrMagnitude <= 0.000001f)
            {
                return fallbackRotation;
            }

            return Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);
        }

        private void ResolveSpriteRenderer()
        {
            if (spriteRenderer != null)
            {
                return;
            }

            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        private void ResolveTargetCamera()
        {
            if (targetCamera != null)
            {
                return;
            }

            Camera mainCamera =
                Camera.main;

            targetCamera =
                mainCamera != null
                    ? mainCamera.transform
                    : null;
        }

        private void ApplyVisualScale()
        {
            if (!HasSprite)
            {
                transform.localScale =
                    Vector3.one;

                return;
            }

            float spriteHeight =
                spriteRenderer.sprite.bounds.size.y;

            if (spriteHeight <= 0.0001f)
            {
                transform.localScale =
                    Vector3.one;

                return;
            }

            float scale =
                visualHeight / spriteHeight;

            transform.localScale =
                new Vector3(
                    scale,
                    scale,
                    1f);
        }
    }
}
