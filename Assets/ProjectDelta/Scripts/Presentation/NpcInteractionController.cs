using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectDelta.Presentation
{
    // 113일차: 정면 한 칸의 NPC를 F로 선택하고 대화/서비스/떠나기 공통 흐름을 제공한다.
    public sealed class NpcInteractionController : MonoBehaviour
    {
        private PlayerGridMovementController movementController;
        private PlayerLookController lookController;
        private Transform viewTransform;
        private NpcContentMarker openNpc;
        private readonly NpcInteractionService interactionService =
            new NpcInteractionService();

        private bool isPanelOpen;
        private string promptText;
        private string statusText;

        private void Awake()
        {
            movementController =
                GetComponent<PlayerGridMovementController>();

            lookController =
                GetComponent<PlayerLookController>();

            Camera mainCamera =
                Camera.main;

            viewTransform =
                mainCamera != null
                    ? mainCamera.transform
                    : transform;
        }

        private void Update()
        {
            if (isPanelOpen)
            {
                if (Keyboard.current != null
                    && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CloseInteraction();
                }

                return;
            }

            if (movementController == null
                || movementController.PlayerState == null
                || movementController.IsMoving
                || movementController.IsInputLocked)
            {
                promptText =
                    string.Empty;

                return;
            }

            NpcContentMarker npc =
                FindNpcInFront();

            promptText =
                npc != null
                    ? $"{npc.Definition?.DisplayName ?? "NPC"} 대화 [F]"
                    : string.Empty;

            if (npc != null
                && Keyboard.current != null
                && Keyboard.current.fKey.wasPressedThisFrame)
            {
                OpenInteraction(
                    npc);
            }
        }

        private void OpenInteraction(
            NpcContentMarker npc)
        {
            if (npc == null
                || npc.Definition == null
                || npc.RelationshipState == null)
            {
                return;
            }

            openNpc =
                npc;

            isPanelOpen =
                true;

            promptText =
                string.Empty;

            statusText =
                "무엇을 할지 선택하세요.";

            openNpc.RelationshipState.RegisterEncounter();

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    true;
            }

            lookController?.SetCursorFreeForUi(
                true);
        }

        private void CloseInteraction()
        {
            isPanelOpen =
                false;

            openNpc =
                null;

            statusText =
                string.Empty;

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    false;
            }

            lookController?.SetCursorFreeForUi(
                false);
        }

        private NpcContentMarker FindNpcInFront()
        {
            RoomView roomView =
                movementController != null
                    ? movementController.CurrentRoomView
                    : null;

            PlayerRunState playerState =
                movementController != null
                    ? movementController.PlayerState
                    : null;

            if (roomView == null
                || playerState == null)
            {
                return null;
            }

            float yaw =
                viewTransform != null
                    ? viewTransform.eulerAngles.y
                    : transform.eulerAngles.y;

            CardinalDirection facing =
                GridMovement.GetFacingFromYaw(
                    yaw);

            GridPosition delta =
                GridMovement.GetDirectionDelta(
                    facing);

            GridPosition frontPosition =
                new GridPosition(
                    playerState.CurrentGridPosition.X + delta.X,
                    playerState.CurrentGridPosition.Z + delta.Z);

            foreach (RoomContentMarker marker
                     in roomView.GetMarkers(
                         RoomContentType.NpcPoint))
            {
                if (marker == null
                    || !marker.gameObject.activeInHierarchy
                    || marker.GridPosition != frontPosition)
                {
                    continue;
                }

                NpcContentMarker npc =
                    marker.GetComponent<NpcContentMarker>();

                if (npc != null)
                {
                    return npc;
                }
            }

            return null;
        }

        private void OnGUI()
        {
            if (!isPanelOpen)
            {
                DrawPrompt();
                return;
            }

            DrawInteractionPanel();
        }

        private void DrawPrompt()
        {
            if (string.IsNullOrEmpty(promptText))
            {
                return;
            }

            GUIStyle promptStyle =
                new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold
                };

            promptStyle.normal.textColor =
                Color.white;

            Rect promptRect =
                new Rect(
                    (Screen.width - 360f) * 0.5f,
                    Screen.height - 92f,
                    360f,
                    34f);

            GUI.Label(
                promptRect,
                promptText,
                promptStyle);
        }

        private void DrawInteractionPanel()
        {
            if (openNpc == null
                || openNpc.Definition == null
                || openNpc.RelationshipState == null)
            {
                CloseInteraction();
                return;
            }

            const float panelWidth = 480f;
            const float panelHeight = 330f;

            Rect panelRect =
                new Rect(
                    (Screen.width - panelWidth) * 0.5f,
                    (Screen.height - panelHeight) * 0.5f,
                    panelWidth,
                    panelHeight);

            GUI.Box(
                panelRect,
                string.Empty);

            GUILayout.BeginArea(
                new Rect(
                    panelRect.x + 22f,
                    panelRect.y + 18f,
                    panelRect.width - 44f,
                    panelRect.height - 36f));

            GUIStyle titleStyle =
                new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };

            titleStyle.normal.textColor =
                Color.white;

            GUILayout.Label(
                openNpc.Definition.DisplayName,
                titleStyle,
                GUILayout.Height(34f));

            GUILayout.Label(
                $"ID : {openNpc.Definition.Id}");

            GUILayout.Label(
                $"호감도 : {openNpc.RelationshipState.Affinity} / 100   |   관계 : {openNpc.RelationshipState.Stage}");

            GUILayout.Label(
                $"서비스 : {openNpc.Definition.ServiceTypes}   |   전투 가능 : {(openNpc.Definition.CanBattle ? "가능" : "불가")}");

            GUILayout.Space(
                10f);

            GUILayout.Label(
                string.IsNullOrEmpty(statusText)
                    ? "무엇을 할지 선택하세요."
                    : statusText,
                GUILayout.Height(48f));

            GUILayout.Space(
                8f);

            if (GUILayout.Button(
                    "대화",
                    GUILayout.Height(36f)))
            {
                ApplyResult(
                    interactionService.Resolve(
                        openNpc.Definition,
                        openNpc.RelationshipState,
                        NpcInteractionCommand.Talk));
            }

            GUI.enabled =
                openNpc.Definition.ServiceTypes != NpcServiceType.None;

            if (GUILayout.Button(
                    "서비스",
                    GUILayout.Height(36f)))
            {
                ApplyResult(
                    interactionService.Resolve(
                        openNpc.Definition,
                        openNpc.RelationshipState,
                        NpcInteractionCommand.Service));
            }

            GUI.enabled =
                true;

            if (GUILayout.Button(
                    "떠나기",
                    GUILayout.Height(36f)))
            {
                ApplyResult(
                    interactionService.Resolve(
                        openNpc.Definition,
                        openNpc.RelationshipState,
                        NpcInteractionCommand.Leave));
            }

            GUILayout.EndArea();
        }

        private void ApplyResult(
            NpcInteractionResult result)
        {
            if (result == null)
            {
                return;
            }

            statusText =
                result.Message;

            if (result.ResultType == NpcInteractionResultType.ReturnToExploration)
            {
                CloseInteraction();
            }
        }

        private void OnDisable()
        {
            if (isPanelOpen)
            {
                CloseInteraction();
            }
        }
    }
}
