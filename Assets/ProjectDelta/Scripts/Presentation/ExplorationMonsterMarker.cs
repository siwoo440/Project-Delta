using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 41일차: 탐험 화면에 배치된 정지형 테스트 몬스터의 논리 위치 정보.
    // 추가 작업: 몬스터 Root는 Grid/Encounter 판정용으로 유지하고 외형은 Billboard Sprite 자식으로 분리한다.
    public sealed class ExplorationMonsterMarker : MonoBehaviour
    {
        public const string BillboardObjectName =
            "MonsterBillboardVisual";

        [SerializeField] private string roomId;
        [SerializeField] private string monsterDefinitionId;
        [SerializeField] private int gridX;
        [SerializeField] private int gridZ;
        [SerializeField] private float billboardHeight = 2f;
        [SerializeField] private Vector3 billboardLocalOffset =
            new Vector3(
                0f,
                0.35f,
                0f);

        private MonsterBillboardView billboardView;

        public string RoomId =>
            roomId;

        public string MonsterDefinitionId =>
            monsterDefinitionId;

        public GridPosition GridPosition =>
            new GridPosition(
                gridX,
                gridZ);

        public bool HasBillboardSprite =>
            billboardView != null
            && billboardView.HasSprite;

        public void Configure(
            string targetRoomId,
            string targetMonsterDefinitionId,
            GridPosition position)
        {
            roomId =
                targetRoomId;

            monsterDefinitionId =
                targetMonsterDefinitionId;

            gridX =
                position.X;

            gridZ =
                position.Z;

            ConfigureBillboardVisual();
        }

        private void ConfigureBillboardVisual()
        {
            ResolveOrCreateBillboardView();

            bool hasSprite =
                billboardView != null
                && billboardView.Configure(
                    monsterDefinitionId,
                    billboardHeight);

            Renderer fallbackRenderer =
                GetComponent<Renderer>();

            if (fallbackRenderer != null)
            {
                fallbackRenderer.enabled =
                    !hasSprite;
            }
        }

        private void ResolveOrCreateBillboardView()
        {
            if (billboardView != null)
            {
                billboardView.transform.localPosition =
                    billboardLocalOffset;

                return;
            }

            Transform existingVisual =
                transform.Find(
                    BillboardObjectName);

            if (existingVisual != null)
            {
                billboardView =
                    existingVisual.GetComponent<MonsterBillboardView>();

                if (billboardView == null)
                {
                    billboardView =
                        existingVisual.gameObject.AddComponent<MonsterBillboardView>();
                }

                existingVisual.localPosition =
                    billboardLocalOffset;

                return;
            }

            GameObject visualObject =
                new GameObject(
                    BillboardObjectName);

            visualObject.transform.SetParent(
                transform,
                false);

            visualObject.transform.localPosition =
                billboardLocalOffset;

            visualObject.transform.localRotation =
                Quaternion.identity;

            visualObject.transform.localScale =
                Vector3.one;

            SpriteRenderer spriteRenderer =
                visualObject.AddComponent<SpriteRenderer>();

            spriteRenderer.drawMode =
                SpriteDrawMode.Simple;

            billboardView =
                visualObject.AddComponent<MonsterBillboardView>();
        }
    }
}
