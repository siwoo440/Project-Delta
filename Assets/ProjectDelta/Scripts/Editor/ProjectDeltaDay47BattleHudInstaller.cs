using System.IO;
using ProjectDelta.Application;
using ProjectDelta.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectDelta.Editor
{
    // 47일차: 전투 화면 Canvas를 생성한다.
    // 오른쪽 = 플레이어 상태 일러스트, 위쪽 = 적 슬롯 1~4(맨 왼쪽이 1번),
    // 왼쪽 가운데 아래 = 행동 버튼 자리, 그 위 = 캐릭터 체력바.
    public static class ProjectDeltaDay47BattleHudInstaller
    {
        // 던전 씬 경로
        private const string DungeonScenePath =
            "Assets/ProjectDelta/Scenes/DungeonScene.unity";

        // 전투 캔버스 이름
        private const string BattleCanvasName =
            "BattleCanvas";

        // 전투 화면 루트 이름
        private const string BattleHudRootName =
            "BattleHudRoot";

        // 44일차 인카운터 캔버스 이름
        private const string EncounterCanvasName =
            "EncounterCanvas";

        // 44일차 인카운터 패널 이름
        private const string EncounterPanelName =
            "EncounterPanel";

        // 기준 해상도
        private static readonly Vector2 ReferenceResolution =
            new Vector2(1920f, 1080f);

        // 적 슬롯 크기
        private static readonly Vector2 EnemySlotSize =
            new Vector2(300f, 420f);

        // 적 슬롯 가로 간격
        private const float EnemySlotSpacing =
            330f;

        // 적 슬롯 묶음 중심 X
        private const float EnemySlotGroupCenterX =
            -240f;

        // 적 슬롯 Y
        private const float EnemySlotY =
            230f;

        [MenuItem("Project Delta/Day 47/Build Battle HUD")]
        public static void BuildBattleHud()
        {
            // 던전 씬 존재 확인
            if (!File.Exists(DungeonScenePath))
            {
                // 누락 안내
                EditorUtility.DisplayDialog(
                    "Project Delta - Day 47",
                    $"DungeonScene을 찾을 수 없습니다.\n{DungeonScenePath}",
                    "확인");

                return;
            }

            // 현재 씬 변경사항 저장 확인
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // 던전 씬 열기
            Scene dungeonScene =
                EditorSceneManager.OpenScene(
                    DungeonScenePath,
                    OpenSceneMode.Single);

            // 인카운터 컨트롤러 검색
            ExplorationMonsterEncounterController encounterController =
                Object.FindFirstObjectByType<ExplorationMonsterEncounterController>(
                    FindObjectsInactive.Include);

            // 필수 컨트롤러 확인
            if (encounterController == null)
            {
                // 누락 안내
                EditorUtility.DisplayDialog(
                    "Project Delta - Day 47",
                    "DungeonScene에서 ExplorationMonsterEncounterController를 찾을 수 없습니다.",
                    "확인");

                return;
            }

            // 이전 버전 인카운터 패널 테스트 UI 정리
            CleanUpLegacyEncounterPanelTestUi(
                dungeonScene);

            // 기존 전투 캔버스 제거
            GameObject existingCanvas =
                FindRootObject(
                    dungeonScene,
                    BattleCanvasName);

            // 기존 전투 캔버스 정리
            if (existingCanvas != null)
            {
                Object.DestroyImmediate(
                    existingCanvas);
            }

            // 이벤트 시스템 보장
            EnsureEventSystem();

            // 기본 폰트 로드
            Font font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            // 전투 캔버스 생성
            GameObject canvasObject =
                CreateBattleCanvas();

            // 전투 화면 루트 생성
            GameObject hudRoot =
                CreateFullScreenRoot(
                    BattleHudRootName,
                    canvasObject.transform);

            // 전투 상태 텍스트 생성 (좌측 상단)
            Text battleStateText =
                CreateText(
                    "BattleStateText",
                    hudRoot.transform,
                    "Battle : -",
                    new Vector2(-660f, 480f),
                    new Vector2(560f, 50f),
                    24,
                    TextAnchor.MiddleLeft,
                    font);

            // 적 슬롯 4개 생성 (맨 왼쪽이 1번)
            BattleParticipantSlotView[] enemySlots =
                new BattleParticipantSlotView[BattleContext.MaxEnemySlots];

            for (int slotIndex = 0; slotIndex < enemySlots.Length; slotIndex++)
            {
                // 슬롯 X 위치 계산
                float slotOffset =
                    slotIndex - (enemySlots.Length - 1) * 0.5f;

                // 적 슬롯 생성
                enemySlots[slotIndex] =
                    CreateEnemySlot(
                        slotIndex,
                        hudRoot.transform,
                        new Vector2(
                            EnemySlotGroupCenterX + slotOffset * EnemySlotSpacing,
                            EnemySlotY),
                        font);
            }

            // 플레이어 상태 일러스트 생성 (오른쪽)
            BattleParticipantSlotView playerSlot =
                CreatePlayerStatusPanel(
                    hudRoot.transform,
                    font);

            // 캐릭터 체력바 묶음 생성 (행동 버튼 위)
            PlayerVitalsWidgets vitals =
                CreatePlayerVitalsPanel(
                    hudRoot.transform,
                    font);

            // 행동 버튼 자리 생성 (왼쪽 가운데 아래), 49일차: 공격 버튼만 따로 반환받는다
            Button[] actionButtons =
                CreateActionButtonPanel(
                    hudRoot.transform,
                    font,
                    out Button attackButton);

            // 47~48일차 테스트 버튼 생성 (우측 상단)
            // 48일차부터 한 번 클릭에 참가자 한 명씩(Speed 순서대로) 진행한다.
            Button testNextTurnButton =
                CreateButton(
                    "TestNextTurnButton",
                    hudRoot.transform,
                    "Test Advance",
                    new Vector2(120f, 480f),
                    new Vector2(190f, 56f),
                    font);

            Button testWinButton =
                CreateButton(
                    "TestWinButton",
                    hudRoot.transform,
                    "Test Win",
                    new Vector2(330f, 480f),
                    new Vector2(190f, 56f),
                    font);

            Button testLoseButton =
                CreateButton(
                    "TestLoseButton",
                    hudRoot.transform,
                    "Test Lose",
                    new Vector2(540f, 480f),
                    new Vector2(190f, 56f),
                    font);

            // HUD 컨트롤러 가져오기
            BattleHudController hudController =
                canvasObject.GetComponent<BattleHudController>();

            // 직렬화 참조 연결
            BindHudController(
                hudController,
                encounterController,
                hudRoot,
                battleStateText,
                enemySlots,
                playerSlot,
                vitals,
                attackButton,
                actionButtons,
                testNextTurnButton,
                testWinButton,
                testLoseButton);

            // 에디터에서 레이아웃 확인 가능 상태 유지
            hudRoot.SetActive(
                true);

            // 씬 변경 표시
            EditorSceneManager.MarkSceneDirty(
                dungeonScene);

            // 씬 저장
            bool saved =
                EditorSceneManager.SaveScene(
                    dungeonScene);

            // 저장 실패 확인
            if (!saved)
            {
                EditorUtility.DisplayDialog(
                    "Project Delta - Day 47",
                    "DungeonScene 저장에 실패했습니다.",
                    "확인");

                return;
            }

            // 생성 오브젝트 선택
            Selection.activeGameObject =
                canvasObject;

            // 완료 안내
            EditorUtility.DisplayDialog(
                "Project Delta - Day 47",
                "DungeonScene에 BattleCanvas를 생성하고 Day47 전투 화면 참조를 연결했습니다.\n\n"
                + "- 오른쪽 : 플레이어 상태 일러스트\n"
                + "- 위쪽 : 적 슬롯 1~4 (맨 왼쪽이 1번)\n"
                + "- 왼쪽 가운데 아래 : 행동 버튼 자리\n"
                + "- 그 위 : 캐릭터 체력바",
                "확인");
        }

        // 이전 반복에서 EncounterPanel에 만들었던 Day47 테스트 UI를 제거한다.
        private static void CleanUpLegacyEncounterPanelTestUi(
            Scene scene)
        {
            // 인카운터 캔버스 검색
            GameObject encounterCanvas =
                FindRootObject(
                    scene,
                    EncounterCanvasName);

            // 인카운터 캔버스 확인
            if (encounterCanvas == null)
            {
                return;
            }

            // 인카운터 패널 검색
            Transform encounterPanel =
                encounterCanvas.transform.Find(
                    EncounterPanelName);

            // 인카운터 패널 확인
            if (encounterPanel == null)
            {
                return;
            }

            // 제거 대상 이름 목록
            string[] legacyNames =
            {
                "BattleStateText",
                "BattleNextTurnButton",
                "BattleWinButton",
                "BattleLoseButton"
            };

            // 제거 대상 순회
            foreach (string legacyName in legacyNames)
            {
                // 기존 오브젝트 검색
                Transform legacyObject =
                    encounterPanel.Find(
                        legacyName);

                // 기존 오브젝트 제거
                if (legacyObject != null)
                {
                    Object.DestroyImmediate(
                        legacyObject.gameObject);
                }
            }

            // 패널 원래 크기 복구
            RectTransform panelRect =
                encounterPanel.GetComponent<RectTransform>();

            // 패널 크기 확인
            if (panelRect != null
                && panelRect.sizeDelta.y > 520f)
            {
                panelRect.sizeDelta =
                    new Vector2(
                        panelRect.sizeDelta.x,
                        520f);
            }
        }

        private static GameObject CreateBattleCanvas()
        {
            // 캔버스 오브젝트 생성
            GameObject canvasObject =
                new GameObject(
                    BattleCanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(BattleHudController));

            // UI 레이어 설정
            canvasObject.layer =
                LayerMask.NameToLayer(
                    "UI");

            // 캔버스 설정
            Canvas canvas =
                canvasObject.GetComponent<Canvas>();

            // 화면 오버레이 모드 설정
            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            // 인카운터 패널보다 위에 그리도록 설정
            canvas.sortingOrder =
                110;

            // 스케일러 가져오기
            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            // 해상도 대응 방식 설정
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // 기준 해상도 설정
            scaler.referenceResolution =
                ReferenceResolution;

            // 가로세로 대응 방식 설정
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            // 가로세로 중간값 설정
            scaler.matchWidthOrHeight =
                0.5f;

            // 기준 픽셀 설정
            scaler.referencePixelsPerUnit =
                100f;

            return canvasObject;
        }

        private static GameObject CreateFullScreenRoot(
            string objectName,
            Transform parent)
        {
            // 루트 오브젝트 생성
            GameObject rootObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image));

            // 부모 연결
            rootObject.transform.SetParent(
                parent,
                false);

            // UI 레이어 설정
            rootObject.layer =
                LayerMask.NameToLayer(
                    "UI");

            // 위치 컴포넌트 가져오기
            RectTransform rectTransform =
                rootObject.GetComponent<RectTransform>();

            // 화면 전체 채움 설정
            rectTransform.anchorMin =
                Vector2.zero;

            // 화면 전체 채움 설정
            rectTransform.anchorMax =
                Vector2.one;

            // 여백 초기화
            rectTransform.offsetMin =
                Vector2.zero;

            // 여백 초기화
            rectTransform.offsetMax =
                Vector2.zero;

            // 전투 배경 이미지 가져오기
            Image image =
                rootObject.GetComponent<Image>();

            // 전투 배경색 설정
            image.color =
                new Color(
                    0.035f,
                    0.04f,
                    0.06f,
                    0.97f);

            return rootObject;
        }

        private static BattleParticipantSlotView CreateEnemySlot(
            int slotIndex,
            Transform parent,
            Vector2 anchoredPosition,
            Font font)
        {
            // 슬롯 루트 생성
            GameObject slotObject =
                CreatePanel(
                    $"EnemySlot{slotIndex + 1}",
                    parent,
                    anchoredPosition,
                    EnemySlotSize,
                    new Color(
                        0.09f,
                        0.10f,
                        0.14f,
                        0.92f));

            // 슬롯 뷰 컴포넌트 추가
            BattleParticipantSlotView slotView =
                slotObject.AddComponent<BattleParticipantSlotView>();

            // 49일차: 슬롯 클릭으로 대상을 선택할 수 있도록 Button 추가
            Image slotBackgroundImage =
                slotObject.GetComponent<Image>();

            Button slotButton =
                slotObject.AddComponent<Button>();

            slotButton.targetGraphic =
                slotBackgroundImage;

            // 색상은 BattleParticipantSlotView가 직접 관리하므로 Button 자체 트랜지션은 끈다
            slotButton.transition =
                Selectable.Transition.None;

            slotButton.interactable =
                false;

            // 슬롯 번호 텍스트 생성
            Text slotIndexText =
                CreateText(
                    "SlotIndexText",
                    slotObject.transform,
                    $"{slotIndex + 1}",
                    new Vector2(0f, 185f),
                    new Vector2(280f, 36f),
                    22,
                    TextAnchor.MiddleCenter,
                    font);

            // 일러스트 이미지 생성
            Image portraitImage =
                CreateImage(
                    "PortraitImage",
                    slotObject.transform,
                    new Vector2(0f, 40f),
                    new Vector2(250f, 220f),
                    new Color(
                        0.18f,
                        0.20f,
                        0.26f,
                        1f));

            // 이름 텍스트 생성
            Text nameText =
                CreateText(
                    "NameText",
                    slotObject.transform,
                    "-",
                    new Vector2(0f, -95f),
                    new Vector2(280f, 38f),
                    20,
                    TextAnchor.MiddleCenter,
                    font);

            // 체력바 생성
            Image healthFillImage =
                CreateHealthBar(
                    slotObject.transform,
                    new Vector2(0f, -142f),
                    new Vector2(250f, 24f),
                    new Color(
                        0.78f,
                        0.26f,
                        0.28f,
                        1f));

            // 체력 텍스트 생성
            Text healthText =
                CreateText(
                    "HealthText",
                    slotObject.transform,
                    "HP 0 / 0",
                    new Vector2(0f, -180f),
                    new Vector2(280f, 32f),
                    18,
                    TextAnchor.MiddleCenter,
                    font);

            // 슬롯 뷰 참조 연결
            BindSlotView(
                slotView,
                slotObject,
                slotIndexText,
                portraitImage,
                nameText,
                healthFillImage,
                healthText,
                slotButton,
                slotBackgroundImage);

            return slotView;
        }

        private static BattleParticipantSlotView CreatePlayerStatusPanel(
            Transform parent,
            Font font)
        {
            // 플레이어 패널 생성
            GameObject panelObject =
                CreatePanel(
                    "PlayerStatusPanel",
                    parent,
                    new Vector2(700f, 30f),
                    new Vector2(440f, 720f),
                    new Color(
                        0.09f,
                        0.10f,
                        0.14f,
                        0.92f));

            // 슬롯 뷰 컴포넌트 추가
            BattleParticipantSlotView slotView =
                panelObject.AddComponent<BattleParticipantSlotView>();

            // 패널 제목 텍스트 생성
            Text slotIndexText =
                CreateText(
                    "SlotIndexText",
                    panelObject.transform,
                    "플레이어",
                    new Vector2(0f, 320f),
                    new Vector2(400f, 46f),
                    26,
                    TextAnchor.MiddleCenter,
                    font);

            // 플레이어 일러스트 생성
            Image portraitImage =
                CreateImage(
                    "PortraitImage",
                    panelObject.transform,
                    new Vector2(0f, 70f),
                    new Vector2(380f, 440f),
                    new Color(
                        0.18f,
                        0.20f,
                        0.26f,
                        1f));

            // 이름 텍스트 생성
            Text nameText =
                CreateText(
                    "NameText",
                    panelObject.transform,
                    "-",
                    new Vector2(0f, -190f),
                    new Vector2(400f, 44f),
                    24,
                    TextAnchor.MiddleCenter,
                    font);

            // 체력바 생성
            Image healthFillImage =
                CreateHealthBar(
                    panelObject.transform,
                    new Vector2(0f, -245f),
                    new Vector2(380f, 30f),
                    new Color(
                        0.30f,
                        0.72f,
                        0.38f,
                        1f));

            // 체력 텍스트 생성
            Text healthText =
                CreateText(
                    "HealthText",
                    panelObject.transform,
                    "HP 0 / 0",
                    new Vector2(0f, -290f),
                    new Vector2(400f, 36f),
                    20,
                    TextAnchor.MiddleCenter,
                    font);

            // 슬롯 뷰 참조 연결 (플레이어 패널은 클릭 대상이 아니므로 Button 없이 연결)
            BindSlotView(
                slotView,
                panelObject,
                slotIndexText,
                portraitImage,
                nameText,
                healthFillImage,
                healthText,
                null,
                panelObject.GetComponent<Image>());

            return slotView;
        }

        private static PlayerVitalsWidgets CreatePlayerVitalsPanel(
            Transform parent,
            Font font)
        {
            // 뒤 배경 없이 위치만 잡아주는 빈 컨테이너 (HP·MP·SP를 한 줄로 나란히 배치)
            GameObject containerObject =
                CreateContainer(
                    "PlayerVitalsPanel",
                    parent,
                    new Vector2(-500f, -260f),
                    new Vector2(700f, 40f));

            // 결과 묶음 생성
            PlayerVitalsWidgets widgets =
                new PlayerVitalsWidgets();

            // HP 열 생성 (왼쪽, 글자가 바 왼쪽에 위치)
            widgets.HealthFillImage =
                CreateVitalColumn(
                    "Health",
                    containerObject.transform,
                    -233f,
                    new Color(
                        0.30f,
                        0.72f,
                        0.38f,
                        1f),
                    "HP  0 / 0",
                    font,
                    out Text healthText);

            widgets.HealthText =
                healthText;

            // MP 열 생성 (가운데, 글자가 바 왼쪽에 위치)
            widgets.ManaFillImage =
                CreateVitalColumn(
                    "Mana",
                    containerObject.transform,
                    0f,
                    new Color(
                        0.30f,
                        0.52f,
                        0.86f,
                        1f),
                    "MP  0 / 0",
                    font,
                    out Text manaText);

            widgets.ManaText =
                manaText;

            // SP 열 생성 (오른쪽, 글자가 바 왼쪽에 위치)
            widgets.StaminaFillImage =
                CreateVitalColumn(
                    "Stamina",
                    containerObject.transform,
                    233f,
                    new Color(
                        0.86f,
                        0.72f,
                        0.30f,
                        1f),
                    "SP  0 / 0",
                    font,
                    out Text staminaText);

            widgets.StaminaText =
                staminaText;

            return widgets;
        }

        private static Image CreateVitalColumn(
            string columnName,
            Transform parent,
            float columnCenterX,
            Color fillColor,
            string initialText,
            Font font,
            out Text valueText)
        {
            // 글자(왼쪽) + 체력바(오른쪽)를 한 줄로 묶어 열 중심에 배치
            const float textWidth = 90f;
            const float barWidth = 110f;
            const float gap = 8f;

            float blockLeftEdge =
                columnCenterX - (textWidth + gap + barWidth) * 0.5f;

            // 수치 텍스트 생성 (바 왼쪽)
            valueText =
                CreateText(
                    $"{columnName}Text",
                    parent,
                    initialText,
                    new Vector2(
                        blockLeftEdge + textWidth * 0.5f,
                        0f),
                    new Vector2(textWidth, 30f),
                    17,
                    TextAnchor.MiddleLeft,
                    font);

            // 체력바 생성 (글자 오른쪽)
            Image fillImage =
                CreateHealthBar(
                    parent,
                    new Vector2(
                        blockLeftEdge + textWidth + gap + barWidth * 0.5f,
                        0f),
                    new Vector2(barWidth, 22f),
                    fillColor,
                    columnName);

            return fillImage;
        }

        // 49일차: 1번(공격) 버튼은 실제로 연결되므로 attackButton으로 따로 반환하고,
        // 나머지 4개(행동·방어·아이템·도주)는 50~54일차에 연결할 자리로 반환한다.
        private static Button[] CreateActionButtonPanel(
            Transform parent,
            Font font,
            out Button attackButton)
        {
            // 뒤 배경 없이 위치만 잡아주는 빈 컨테이너 (5개를 납작하게 한 줄로 배치)
            GameObject containerObject =
                CreateContainer(
                    "ActionButtonPanel",
                    parent,
                    new Vector2(-500f, -386f),
                    new Vector2(700f, 56f));

            // 안내 제목 텍스트 생성 (버튼 위, 배경 없이 얇게)
            CreateText(
                "TitleText",
                parent,
                "행동 버튼 자리 (50~54일차 연결 예정)",
                new Vector2(-500f, -322f),
                new Vector2(660f, 24f),
                15,
                TextAnchor.MiddleCenter,
                font);

            // 행동 버튼 라벨 (공격만 49일차에 연결, 나머지는 50~54일차에 연결)
            string[] labels =
            {
                "공격",
                "행동",
                "방어",
                "아이템",
                "도주"
            };

            // 버튼 하나당 너비·간격 계산 (5칸을 700폭 안에 납작하게 균등 배치)
            const float buttonWidth = 128f;
            const float buttonHeight = 56f;
            const float buttonSpacing = buttonWidth + 10f;

            float groupStartX =
                -(labels.Length - 1) * buttonSpacing * 0.5f;

            // 전체 버튼 생성
            Button[] buttons =
                new Button[labels.Length];

            // 행동 버튼 순회 생성
            for (int index = 0; index < labels.Length; index++)
            {
                buttons[index] =
                    CreateButton(
                        $"ActionButton{index + 1}",
                        containerObject.transform,
                        labels[index],
                        new Vector2(
                            groupStartX + index * buttonSpacing,
                            0f),
                        new Vector2(buttonWidth, buttonHeight),
                        font);
            }

            // 1번(공격)을 분리해 반환
            attackButton =
                buttons[0];

            // 나머지 4개(행동·방어·아이템·도주)만 남긴다
            Button[] remainingButtons =
                new Button[buttons.Length - 1];

            for (int index = 1; index < buttons.Length; index++)
            {
                remainingButtons[index - 1] =
                    buttons[index];
            }

            return remainingButtons;
        }

        // 배경 이미지 없이 자식 요소의 기준 위치만 잡아주는 빈 컨테이너.
        private static GameObject CreateContainer(
            string objectName,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            // 컨테이너 오브젝트 생성 (Image 없음 → 뒤 패널이 보이지 않는다)
            GameObject containerObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform));

            // 부모 연결
            containerObject.transform.SetParent(
                parent,
                false);

            // UI 레이어 설정
            containerObject.layer =
                LayerMask.NameToLayer(
                    "UI");

            // 위치 컴포넌트 가져오기
            RectTransform rectTransform =
                containerObject.GetComponent<RectTransform>();

            // 중앙 기준 설정
            ApplyCenterAnchors(
                rectTransform,
                anchoredPosition,
                size);

            return containerObject;
        }

        private static GameObject CreatePanel(
            string objectName,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Color backgroundColor)
        {
            // 패널 오브젝트 생성
            GameObject panelObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image));

            // 부모 연결
            panelObject.transform.SetParent(
                parent,
                false);

            // UI 레이어 설정
            panelObject.layer =
                LayerMask.NameToLayer(
                    "UI");

            // 위치 컴포넌트 가져오기
            RectTransform rectTransform =
                panelObject.GetComponent<RectTransform>();

            // 중앙 기준 설정
            ApplyCenterAnchors(
                rectTransform,
                anchoredPosition,
                size);

            // 배경 이미지 가져오기
            Image image =
                panelObject.GetComponent<Image>();

            // 배경색 설정
            image.color =
                backgroundColor;

            return panelObject;
        }

        private static Image CreateImage(
            string objectName,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            // 이미지 오브젝트 생성
            GameObject imageObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image));

            // 부모 연결
            imageObject.transform.SetParent(
                parent,
                false);

            // UI 레이어 설정
            imageObject.layer =
                LayerMask.NameToLayer(
                    "UI");

            // 위치 컴포넌트 가져오기
            RectTransform rectTransform =
                imageObject.GetComponent<RectTransform>();

            // 중앙 기준 설정
            ApplyCenterAnchors(
                rectTransform,
                anchoredPosition,
                size);

            // 이미지 컴포넌트 가져오기
            Image image =
                imageObject.GetComponent<Image>();

            // 기본 색상 설정
            image.color =
                color;

            // 일러스트 비율 유지 설정
            image.preserveAspect =
                true;

            // 레이캐스트 제외
            image.raycastTarget =
                false;

            return image;
        }

        private static Image CreateHealthBar(
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Color fillColor,
            string namePrefix = "Health")
        {
            // 체력바 배경 생성
            GameObject backgroundObject =
                new GameObject(
                    $"{namePrefix}BarBackground",
                    typeof(RectTransform),
                    typeof(Image));

            // 부모 연결
            backgroundObject.transform.SetParent(
                parent,
                false);

            // UI 레이어 설정
            backgroundObject.layer =
                LayerMask.NameToLayer(
                    "UI");

            // 위치 컴포넌트 가져오기
            RectTransform backgroundRect =
                backgroundObject.GetComponent<RectTransform>();

            // 중앙 기준 설정
            ApplyCenterAnchors(
                backgroundRect,
                anchoredPosition,
                size);

            // 배경 이미지 가져오기
            Image backgroundImage =
                backgroundObject.GetComponent<Image>();

            // 배경색 설정
            backgroundImage.color =
                new Color(
                    0.04f,
                    0.05f,
                    0.07f,
                    1f);

            // 레이캐스트 제외
            backgroundImage.raycastTarget =
                false;

            // 체력바 채움 생성
            GameObject fillObject =
                new GameObject(
                    $"{namePrefix}BarFill",
                    typeof(RectTransform),
                    typeof(Image));

            // 부모 연결
            fillObject.transform.SetParent(
                backgroundObject.transform,
                false);

            // UI 레이어 설정
            fillObject.layer =
                LayerMask.NameToLayer(
                    "UI");

            // 위치 컴포넌트 가져오기
            RectTransform fillRect =
                fillObject.GetComponent<RectTransform>();

            // 배경 전체 채움 설정
            fillRect.anchorMin =
                Vector2.zero;

            // 배경 전체 채움 설정
            fillRect.anchorMax =
                Vector2.one;

            // 여백 설정
            fillRect.offsetMin =
                new Vector2(3f, 3f);

            // 여백 설정
            fillRect.offsetMax =
                new Vector2(-3f, -3f);

            // 채움 이미지 가져오기
            Image fillImage =
                fillObject.GetComponent<Image>();

            // 채움 기본 스프라이트 설정 (Filled 모드는 스프라이트가 필요하다)
            // 내장 리소스 경로는 Unity 버전마다 달라질 수 있어, 직접 만든 흰색 스프라이트를 사용한다.
            fillImage.sprite =
                GetSolidWhiteSprite();

            // 채움 방식 설정
            fillImage.type =
                Image.Type.Filled;

            // 가로 채움 설정
            fillImage.fillMethod =
                Image.FillMethod.Horizontal;

            // 왼쪽 기준 채움 설정
            fillImage.fillOrigin =
                (int)Image.OriginHorizontal.Left;

            // 초기 채움량 설정
            fillImage.fillAmount =
                1f;

            // 채움 색상 설정
            fillImage.color =
                fillColor;

            // 레이캐스트 제외
            fillImage.raycastTarget =
                false;

            return fillImage;
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            string content,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            TextAnchor alignment,
            Font font)
        {
            // 텍스트 오브젝트 생성
            GameObject textObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Text));

            // 부모 연결
            textObject.transform.SetParent(
                parent,
                false);

            // UI 레이어 설정
            textObject.layer =
                LayerMask.NameToLayer(
                    "UI");

            // 위치 컴포넌트 가져오기
            RectTransform rectTransform =
                textObject.GetComponent<RectTransform>();

            // 중앙 기준 설정
            ApplyCenterAnchors(
                rectTransform,
                anchoredPosition,
                size);

            // 텍스트 컴포넌트 가져오기
            Text text =
                textObject.GetComponent<Text>();

            // 기본 폰트 설정
            text.font =
                font;

            // 기본 문구 설정
            text.text =
                content;

            // 글자 크기 설정
            text.fontSize =
                fontSize;

            // 글자 색상 설정
            text.color =
                Color.white;

            // 정렬 설정
            text.alignment =
                alignment;

            // 가로 줄바꿈 설정
            text.horizontalOverflow =
                HorizontalWrapMode.Wrap;

            // 세로 넘침 허용
            text.verticalOverflow =
                VerticalWrapMode.Overflow;

            // 레이캐스트 제외
            text.raycastTarget =
                false;

            return text;
        }

        private static Button CreateButton(
            string objectName,
            Transform parent,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            Font font)
        {
            // 버튼 오브젝트 생성
            GameObject buttonObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            // 부모 연결
            buttonObject.transform.SetParent(
                parent,
                false);

            // UI 레이어 설정
            buttonObject.layer =
                LayerMask.NameToLayer(
                    "UI");

            // 위치 컴포넌트 가져오기
            RectTransform rectTransform =
                buttonObject.GetComponent<RectTransform>();

            // 중앙 기준 설정
            ApplyCenterAnchors(
                rectTransform,
                anchoredPosition,
                size);

            // 버튼 이미지 가져오기
            Image image =
                buttonObject.GetComponent<Image>();

            // 버튼 기본색 설정
            image.color =
                new Color(
                    0.22f,
                    0.25f,
                    0.32f,
                    1f);

            // 버튼 컴포넌트 가져오기
            Button button =
                buttonObject.GetComponent<Button>();

            // 버튼 이미지 연결
            button.targetGraphic =
                image;

            // 버튼 색상 정보 가져오기
            ColorBlock colors =
                button.colors;

            // 기본 색상 설정
            colors.normalColor =
                new Color(
                    0.22f,
                    0.25f,
                    0.32f,
                    1f);

            // 강조 색상 설정
            colors.highlightedColor =
                new Color(
                    0.34f,
                    0.39f,
                    0.50f,
                    1f);

            // 눌림 색상 설정
            colors.pressedColor =
                new Color(
                    0.16f,
                    0.18f,
                    0.24f,
                    1f);

            // 비활성 색상 설정
            colors.disabledColor =
                new Color(
                    0.12f,
                    0.13f,
                    0.16f,
                    0.55f);

            // 색상 적용
            button.colors =
                colors;

            // 버튼 라벨 생성
            Text labelText =
                CreateText(
                    "Label",
                    buttonObject.transform,
                    label,
                    Vector2.zero,
                    size,
                    20,
                    TextAnchor.MiddleCenter,
                    font);

            // 라벨 영역 가져오기
            RectTransform labelRect =
                labelText.rectTransform;

            // 부모 전체 채움 설정
            labelRect.anchorMin =
                Vector2.zero;

            // 부모 전체 채움 설정
            labelRect.anchorMax =
                Vector2.one;

            // 여백 초기화
            labelRect.offsetMin =
                Vector2.zero;

            // 여백 초기화
            labelRect.offsetMax =
                Vector2.zero;

            return button;
        }

        private static void ApplyCenterAnchors(
            RectTransform rectTransform,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            // 중앙 기준 설정
            rectTransform.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f);

            // 중앙 기준 설정
            rectTransform.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f);

            // 중앙 피벗 설정
            rectTransform.pivot =
                new Vector2(
                    0.5f,
                    0.5f);

            // 위치 설정
            rectTransform.anchoredPosition =
                anchoredPosition;

            // 크기 설정
            rectTransform.sizeDelta =
                size;
        }

        private static void BindSlotView(
            BattleParticipantSlotView slotView,
            GameObject slotRoot,
            Text slotIndexText,
            Image portraitImage,
            Text nameText,
            Image healthFillImage,
            Text healthText,
            Button clickButton,
            Image backgroundImage)
        {
            // 직렬화 객체 생성
            SerializedObject serializedObject =
                new SerializedObject(
                    slotView);

            // 슬롯 루트 연결
            serializedObject.FindProperty(
                "slotRoot").objectReferenceValue =
                slotRoot;

            // 슬롯 번호 텍스트 연결
            serializedObject.FindProperty(
                "slotIndexText").objectReferenceValue =
                slotIndexText;

            // 일러스트 연결
            serializedObject.FindProperty(
                "portraitImage").objectReferenceValue =
                portraitImage;

            // 이름 텍스트 연결
            serializedObject.FindProperty(
                "nameText").objectReferenceValue =
                nameText;

            // 체력바 연결
            serializedObject.FindProperty(
                "healthFillImage").objectReferenceValue =
                healthFillImage;

            // 체력 텍스트 연결
            serializedObject.FindProperty(
                "healthText").objectReferenceValue =
                healthText;

            // 49일차: 클릭 버튼 연결 (플레이어 패널은 null)
            serializedObject.FindProperty(
                "clickButton").objectReferenceValue =
                clickButton;

            // 49일차: 선택 상태 표시용 배경 이미지 연결
            serializedObject.FindProperty(
                "backgroundImage").objectReferenceValue =
                backgroundImage;

            // 직렬화 변경 적용
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            // 변경 상태 기록
            EditorUtility.SetDirty(
                slotView);
        }

        private static void BindHudController(
            BattleHudController hudController,
            ExplorationMonsterEncounterController encounterController,
            GameObject hudRoot,
            Text battleStateText,
            BattleParticipantSlotView[] enemySlots,
            BattleParticipantSlotView playerSlot,
            PlayerVitalsWidgets vitals,
            Button attackButton,
            Button[] actionButtons,
            Button testNextTurnButton,
            Button testWinButton,
            Button testLoseButton)
        {
            // 직렬화 객체 생성
            SerializedObject serializedObject =
                new SerializedObject(
                    hudController);

            // 인카운터 컨트롤러 연결
            serializedObject.FindProperty(
                "encounterController").objectReferenceValue =
                encounterController;

            // 전투 화면 루트 연결
            serializedObject.FindProperty(
                "hudRoot").objectReferenceValue =
                hudRoot;

            // 전투 상태 텍스트 연결
            serializedObject.FindProperty(
                "battleStateText").objectReferenceValue =
                battleStateText;

            // 적 슬롯 배열 연결
            SerializedProperty enemySlotsProperty =
                serializedObject.FindProperty(
                    "enemySlots");

            // 배열 크기 설정
            enemySlotsProperty.arraySize =
                enemySlots.Length;

            // 적 슬롯 순회 연결
            for (int index = 0; index < enemySlots.Length; index++)
            {
                enemySlotsProperty
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue =
                    enemySlots[index];
            }

            // 플레이어 슬롯 연결
            serializedObject.FindProperty(
                "playerSlot").objectReferenceValue =
                playerSlot;

            // HP 바 연결
            serializedObject.FindProperty(
                "healthFillImage").objectReferenceValue =
                vitals.HealthFillImage;

            // HP 텍스트 연결
            serializedObject.FindProperty(
                "healthText").objectReferenceValue =
                vitals.HealthText;

            // MP 바 연결
            serializedObject.FindProperty(
                "manaFillImage").objectReferenceValue =
                vitals.ManaFillImage;

            // MP 텍스트 연결
            serializedObject.FindProperty(
                "manaText").objectReferenceValue =
                vitals.ManaText;

            // SP 바 연결
            serializedObject.FindProperty(
                "staminaFillImage").objectReferenceValue =
                vitals.StaminaFillImage;

            // SP 텍스트 연결
            serializedObject.FindProperty(
                "staminaText").objectReferenceValue =
                vitals.StaminaText;

            // 49일차: 공격 버튼 연결
            serializedObject.FindProperty(
                "attackButton").objectReferenceValue =
                attackButton;

            // 행동 버튼 배열 연결
            SerializedProperty actionButtonsProperty =
                serializedObject.FindProperty(
                    "actionButtons");

            // 배열 크기 설정
            actionButtonsProperty.arraySize =
                actionButtons.Length;

            // 행동 버튼 순회 연결
            for (int index = 0; index < actionButtons.Length; index++)
            {
                actionButtonsProperty
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue =
                    actionButtons[index];
            }

            // 테스트 턴 버튼 연결
            serializedObject.FindProperty(
                "testNextTurnButton").objectReferenceValue =
                testNextTurnButton;

            // 테스트 승리 버튼 연결
            serializedObject.FindProperty(
                "testWinButton").objectReferenceValue =
                testWinButton;

            // 테스트 패배 버튼 연결
            serializedObject.FindProperty(
                "testLoseButton").objectReferenceValue =
                testLoseButton;

            // 직렬화 변경 적용
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            // 변경 상태 기록
            EditorUtility.SetDirty(
                hudController);
        }

        private static void EnsureEventSystem()
        {
            // 기존 이벤트 시스템 검색
            EventSystem eventSystem =
                Object.FindFirstObjectByType<EventSystem>(
                    FindObjectsInactive.Include);

            // 기존 이벤트 시스템이 없는 경우
            if (eventSystem == null)
            {
                // 새 이벤트 시스템 생성
                GameObject eventSystemObject =
                    new GameObject(
                        "EventSystem",
                        typeof(EventSystem),
                        typeof(InputSystemUIInputModule));

                // 루트 위치 초기화
                eventSystemObject.transform.position =
                    Vector3.zero;

                return;
            }

            // 입력 모듈 검색
            BaseInputModule inputModule =
                eventSystem.GetComponent<BaseInputModule>();

            // 입력 모듈이 없는 경우
            if (inputModule == null)
            {
                // 새 입력 시스템 모듈 추가
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        // Filled 모드 체력바에 쓸 흰색 1x1 스프라이트 에셋 경로.
        // 내장 리소스 경로는 Unity 버전마다 달라지고, 런타임에 Sprite.Create()로 만들면
        // 에셋 파일이 아니라서 씬 저장 후 참조가 끊어지므로 실제 파일로 만들어 둔다.
        private const string SolidWhiteSpritePath =
            "Assets/ProjectDelta/Art/UI/BattleHudSolidWhite.png";

        private static Sprite GetSolidWhiteSprite()
        {
            // 이미 만들어진 에셋이 있으면 그대로 사용
            Sprite existingSprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    SolidWhiteSpritePath);

            if (existingSprite != null)
            {
                return existingSprite;
            }

            // 흰색 4x4 텍스처 생성
            Texture2D texture =
                new Texture2D(
                    4,
                    4,
                    TextureFormat.RGBA32,
                    false);

            Color32[] pixels =
                new Color32[texture.width * texture.height];

            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] =
                    Color.white;
            }

            texture.SetPixels32(
                pixels);

            texture.Apply();

            // PNG 파일로 저장
            byte[] pngBytes =
                texture.EncodeToPNG();

            Object.DestroyImmediate(
                texture);

            string directoryPath =
                Path.GetDirectoryName(
                    SolidWhiteSpritePath);

            if (!string.IsNullOrEmpty(directoryPath)
                && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(
                    directoryPath);
            }

            File.WriteAllBytes(
                SolidWhiteSpritePath,
                pngBytes);

            AssetDatabase.ImportAsset(
                SolidWhiteSpritePath);

            // Sprite(2D and UI)로 임포트 설정
            TextureImporter importer =
                AssetImporter.GetAtPath(
                    SolidWhiteSpritePath) as TextureImporter;

            if (importer != null)
            {
                importer.textureType =
                    TextureImporterType.Sprite;

                importer.spriteImportMode =
                    SpriteImportMode.Single;

                importer.filterMode =
                    FilterMode.Bilinear;

                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(
                SolidWhiteSpritePath);
        }

        private static GameObject FindRootObject(
            Scene scene,
            string objectName)
        {
            // 루트 오브젝트 가져오기
            GameObject[] rootObjects =
                scene.GetRootGameObjects();

            // 루트 오브젝트 순회
            foreach (GameObject rootObject in rootObjects)
            {
                // 이름 일치 확인
                if (rootObject.name == objectName)
                {
                    return rootObject;
                }
            }

            return null;
        }

        // 체력바 묶음 생성 결과를 한 번에 전달하기 위한 보조 구조.
        private sealed class PlayerVitalsWidgets
        {
            public Image HealthFillImage;
            public Text HealthText;
            public Image ManaFillImage;
            public Text ManaText;
            public Image StaminaFillImage;
            public Text StaminaText;
        }
    }
}
