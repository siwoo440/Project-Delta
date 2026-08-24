using ProjectDelta.Application;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerGridMovementController))]
    public sealed class ExplorationMonsterEncounterController : MonoBehaviour
    {
        [SerializeField] private PlayerGridMovementController movementController;
        [SerializeField] private PlayerLookController lookController;
        [SerializeField] private DungeonFloorController floorController;

        private readonly ExplorationEncounterSession session =
            new ExplorationEncounterSession();

        private ExplorationMonsterMarker activeMonster;
        private bool wasMoving;

        public bool IsEncounterActive => session.IsActive;
        public string ActiveMonsterDefinitionId => session.MonsterDefinitionId;

        private void Awake()
        {
            if (movementController == null)
            {
                movementController =
                    GetComponent<PlayerGridMovementController>();
            }

            if (lookController == null)
            {
                lookController =
                    GetComponent<PlayerLookController>();
            }

            if (floorController == null)
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>();
            }
        }

        private void OnEnable()
        {
            wasMoving =
                movementController != null
                && movementController.IsMoving;
        }

        private void OnDisable()
        {
            if (movementController != null)
            {
                movementController.IsInputLocked = false;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(false);
            }

            session.Complete();
            activeMonster = null;
            wasMoving = false;
        }

        private void Update()
        {
            if (movementController == null)
            {
                return;
            }

            bool isMovingNow =
                movementController.IsMoving;

            if (wasMoving && !isMovingNow)
            {
                TryBeginEncounterAtCurrentPosition();
            }

            wasMoving =
                isMovingNow;
        }

        public bool TryBeginEncounterAtCurrentPosition()
        {
            if (session.IsActive
                || movementController == null
                || movementController.PlayerState == null)
            {
                return false;
            }

            if (floorController == null)
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>();
            }

            if (floorController == null)
            {
                return false;
            }

            PlayerRunState playerState =
                movementController.PlayerState;

            if (!floorController.SpawnedMonsters.TryGetValue(
                    playerState.CurrentRoomId,
                    out ExplorationMonsterMarker monster)
                || monster == null
                || !monster.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!session.TryBegin(
                    playerState.CurrentRoomId,
                    playerState.CurrentGridPosition,
                    monster.RoomId,
                    monster.GridPosition,
                    monster.MonsterDefinitionId))
            {
                return false;
            }

            activeMonster =
                monster;

            movementController.IsInputLocked =
                true;

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(true);
            }

            Debug.Log(
                $"[Project Delta] 42일차 Encounter 접촉 / Room {monster.RoomId} / Grid {monster.GridPosition} / Monster {monster.MonsterDefinitionId}",
                this);

            return true;
        }

        public void CompleteTestEncounter()
        {
            if (!session.IsActive)
            {
                return;
            }

            if (activeMonster != null)
            {
                activeMonster.gameObject.SetActive(false);
            }

            activeMonster =
                null;

            session.Complete();

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    false;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(false);
            }

            Debug.Log(
                "[Project Delta] 42일차 테스트 Encounter 종료 / 탐험 복귀",
                this);
        }

        private void OnGUI()
        {
            if (!session.IsActive)
            {
                return;
            }

            float width =
                420f;

            float height =
                190f;

            Rect panelRect =
                new Rect(
                    (Screen.width - width) * 0.5f,
                    (Screen.height - height) * 0.5f,
                    width,
                    height);

            GUI.Box(
                panelRect,
                "ENCOUNTER");

            GUI.Label(
                new Rect(
                    panelRect.x + 24f,
                    panelRect.y + 50f,
                    panelRect.width - 48f,
                    28f),
                $"Monster : {session.MonsterDefinitionId}");

            GUI.Label(
                new Rect(
                    panelRect.x + 24f,
                    panelRect.y + 82f,
                    panelRect.width - 48f,
                    28f),
                "전투 인카운터가 시작되었습니다.");

            Rect closeButtonRect =
                new Rect(
                    panelRect.x + (panelRect.width - 140f) * 0.5f,
                    panelRect.y + 128f,
                    140f,
                    36f);

            if (GUI.Button(
                    closeButtonRect,
                    "테스트 종료"))
            {
                CompleteTestEncounter();
            }
        }
    }
}
