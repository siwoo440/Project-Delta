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

        public bool IsEncounterActive =>
            session.State != EncounterState.Idle;

        public EncounterState CurrentState =>
            session.State;

        public EncounterContext CurrentContext =>
            session.Context;

        public string ActiveMonsterDefinitionId =>
            session.MonsterDefinitionId;

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
            RestoreExplorationControl();

            session.ForceReset();
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
            if (session.State != EncounterState.Idle
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

            LockExplorationControl();

            Debug.Log(
                $"[Project Delta] 43일차 Encounter Starting / Room {monster.RoomId} / Grid {monster.GridPosition} / Monster {monster.MonsterDefinitionId}",
                this);

            if (!session.TryActivate())
            {
                Debug.LogError(
                    "[Project Delta] Encounter Starting → Active 전환에 실패했습니다.",
                    this);

                AbortEncounter();
                return false;
            }

            Debug.Log(
                $"[Project Delta] 43일차 Encounter Active / Monster {monster.MonsterDefinitionId}",
                this);

            return true;
        }

        public void CompleteTestEncounter()
        {
            if (session.State != EncounterState.Active)
            {
                return;
            }

            if (!session.TryBeginResolve())
            {
                return;
            }

            Debug.Log(
                "[Project Delta] 43일차 Encounter Resolving",
                this);

            if (activeMonster != null)
            {
                activeMonster.gameObject.SetActive(false);
            }

            if (!session.TryFinish())
            {
                Debug.LogError(
                    "[Project Delta] Encounter Resolving → Finished 전환에 실패했습니다.",
                    this);

                AbortEncounter();
                return;
            }

            Debug.Log(
                "[Project Delta] 43일차 Encounter Finished",
                this);

            activeMonster =
                null;

            RestoreExplorationControl();

            if (!session.TryReset())
            {
                Debug.LogError(
                    "[Project Delta] Encounter Finished → Idle 전환에 실패했습니다.",
                    this);

                session.ForceReset();
            }

            Debug.Log(
                "[Project Delta] 43일차 Encounter Idle 복귀 / 탐험 재개",
                this);
        }

        private void AbortEncounter()
        {
            activeMonster =
                null;

            RestoreExplorationControl();
            session.ForceReset();
        }

        private void LockExplorationControl()
        {
            if (movementController != null)
            {
                movementController.IsInputLocked =
                    true;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(true);
            }
        }

        private void RestoreExplorationControl()
        {
            if (movementController != null)
            {
                movementController.IsInputLocked =
                    false;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(false);
            }
        }

        private void OnGUI()
        {
            if (session.State != EncounterState.Active)
            {
                return;
            }

            float width =
                420f;

            float height =
                220f;

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
                    panelRect.y + 48f,
                    panelRect.width - 48f,
                    28f),
                $"State : {session.State}");

            GUI.Label(
                new Rect(
                    panelRect.x + 24f,
                    panelRect.y + 80f,
                    panelRect.width - 48f,
                    28f),
                $"Monster : {session.MonsterDefinitionId}");

            GUI.Label(
                new Rect(
                    panelRect.x + 24f,
                    panelRect.y + 112f,
                    panelRect.width - 48f,
                    28f),
                "전투 인카운터가 진행 중입니다.");

            Rect closeButtonRect =
                new Rect(
                    panelRect.x + (panelRect.width - 140f) * 0.5f,
                    panelRect.y + 158f,
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
