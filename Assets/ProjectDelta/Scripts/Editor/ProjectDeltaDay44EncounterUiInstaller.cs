using System.Collections.Generic;
using System.IO;
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
    public static class ProjectDeltaDay44EncounterUiInstaller
    {
        // 던전 씬 경로
        private const string DungeonScenePath =
            "Assets/ProjectDelta/Scenes/DungeonScene.unity";

        // 캔버스 이름
        private const string EncounterCanvasName =
            "EncounterCanvas";

        // 패널 이름
        private const string EncounterPanelName =
            "EncounterPanel";

        [MenuItem("Project Delta/Day 44/Build Encounter UI")]
        public static void BuildEncounterUi()
        {
            // 던전 씬 존재 확인
            if (!File.Exists(DungeonScenePath))
            {
                // 누락 안내
                EditorUtility.DisplayDialog(
                    "Project Delta - Day 44",
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
                    "Project Delta - Day 44",
                    "DungeonScene에서 ExplorationMonsterEncounterController를 찾을 수 없습니다.",
                    "확인");

                return;
            }

            // 기존 Day44 캔버스 검색
            GameObject existingCanvas =
                FindRootObject(
                    dungeonScene,
                    EncounterCanvasName);

            // 기존 Day44 캔버스 제거
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

            // 캔버스 생성
            GameObject canvasObject =
                CreateEncounterCanvas();

            // 인카운터 패널 생성
            GameObject panelObject =
                CreateEncounterPanel(
                    canvasObject.transform);

            // 제목 텍스트 생성
            CreateText(
                "TitleText",
                panelObject.transform,
                "인카운터",
                new Vector2(0f, 205f),
                new Vector2(620f, 50f),
                30,
                TextAnchor.MiddleCenter,
                font);

            // 상태 텍스트 생성
            Text stateText =
                CreateText(
                    "StateText",
                    panelObject.transform,
                    "State : -",
                    new Vector2(0f, 145f),
                    new Vector2(620f, 42f),
                    22,
                    TextAnchor.MiddleLeft,
                    font);

            // 몬스터 정보 텍스트 생성
            Text monsterIdText =
                CreateText(
                    "MonsterIdText",
                    panelObject.transform,
                    "Monster : -",
                    new Vector2(0f, 95f),
                    new Vector2(620f, 42f),
                    22,
                    TextAnchor.MiddleLeft,
                    font);

            // 방 정보 텍스트 생성
            Text roomIdText =
                CreateText(
                    "RoomIdText",
                    panelObject.transform,
                    "Room : -",
                    new Vector2(0f, 45f),
                    new Vector2(620f, 42f),
                    22,
                    TextAnchor.MiddleLeft,
                    font);

            // 그리드 정보 텍스트 생성
            Text gridPositionText =
                CreateText(
                    "GridPositionText",
                    panelObject.transform,
                    "Grid : -",
                    new Vector2(0f, -5f),
                    new Vector2(620f, 42f),
                    22,
                    TextAnchor.MiddleLeft,
                    font);

            // 결과 텍스트 생성
            Text resultText =
                CreateText(
                    "ResultText",
                    panelObject.transform,
                    "행동 결과가 표시됩니다.",
                    new Vector2(0f, -75f),
                    new Vector2(620f, 70f),
                    20,
                    TextAnchor.MiddleCenter,
                    font);

            // 전투 버튼 생성
            Button battleButton =
                CreateButton(
                    "BattleButton",
                    panelObject.transform,
                    "전투",
                    new Vector2(-210f, -185f),
                    new Vector2(180f, 60f),
                    font);

            // 회피 버튼 생성
            Button escapeButton =
                CreateButton(
                    "EscapeButton",
                    panelObject.transform,
                    "회피",
                    new Vector2(0f, -185f),
                    new Vector2(180f, 60f),
                    font);

            // 테스트 종료 버튼 생성
            Button testEndButton =
                CreateButton(
                    "TestEndButton",
                    panelObject.transform,
                    "테스트 종료",
                    new Vector2(210f, -185f),
                    new Vector2(180f, 60f),
                    font);

            // 패널 컨트롤러 가져오기
            EncounterPanelController panelController =
                canvasObject.GetComponent<EncounterPanelController>();

            // 직렬화 참조 연결
            BindPanelController(
                panelController,
                encounterController,
                panelObject,
                stateText,
                monsterIdText,
                roomIdText,
                gridPositionText,
                resultText,
                battleButton,
                escapeButton,
                testEndButton);

            // 에디터에서 UI 확인 가능 상태 유지
            panelObject.SetActive(
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
                // 실패 안내
                EditorUtility.DisplayDialog(
                    "Project Delta - Day 44",
                    "DungeonScene 저장에 실패했습니다.",
                    "확인");

                return;
            }

            // 생성 결과 검증
            bool valid =
                ValidateEncounterUiInternal(
                    dungeonScene,
                    true);

            // 생성 오브젝트 선택
            Selection.activeGameObject =
                canvasObject;

            // 검증 실패 중단
            if (!valid)
            {
                return;
            }

            // 완료 안내
            EditorUtility.DisplayDialog(
                "Project Delta - Day 44",
                "DungeonScene에 EncounterCanvas와 EventSystem을 생성하고 Day44 UI 참조를 연결했습니다.",
                "확인");
        }

        [MenuItem("Project Delta/Day 44/Validate Encounter UI")]
        public static void ValidateEncounterUi()
        {
            // 던전 씬 존재 확인
            if (!File.Exists(DungeonScenePath))
            {
                // 누락 안내
                EditorUtility.DisplayDialog(
                    "Project Delta - Day 44",
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

            // 검증 실행
            bool valid =
                ValidateEncounterUiInternal(
                    dungeonScene,
                    true);

            // 성공 안내
            if (valid)
            {
                EditorUtility.DisplayDialog(
                    "Project Delta - Day 44",
                    "Day44 Encounter UI 연결 상태가 정상입니다.",
                    "확인");
            }
        }

        private static GameObject CreateEncounterCanvas()
        {
            // 캔버스 오브젝트 생성
            GameObject canvasObject =
                new GameObject(
                    EncounterCanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(EncounterPanelController));

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

            // 우선 렌더링 설정
            canvas.sortingOrder =
                100;

            // 스케일러 가져오기
            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            // 해상도 대응 방식 설정
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // 기준 해상도 설정
            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f);

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

        private static GameObject CreateEncounterPanel(
            Transform parent)
        {
            // 패널 오브젝트 생성
            GameObject panelObject =
                new GameObject(
                    EncounterPanelName,
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

            // 패널 위치 설정
            RectTransform rectTransform =
                panelObject.GetComponent<RectTransform>();

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

            // 패널 크기 설정
            rectTransform.sizeDelta =
                new Vector2(
                    720f,
                    520f);

            // 패널 위치 설정
            rectTransform.anchoredPosition =
                Vector2.zero;

            // 배경 이미지 가져오기
            Image image =
                panelObject.GetComponent<Image>();

            // 패널 배경색 설정
            image.color =
                new Color(
                    0.055f,
                    0.065f,
                    0.085f,
                    0.96f);

            return panelObject;
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

            // 텍스트 위치 설정
            rectTransform.anchoredPosition =
                anchoredPosition;

            // 텍스트 크기 설정
            rectTransform.sizeDelta =
                size;

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

            // 버튼 위치 설정
            rectTransform.anchoredPosition =
                anchoredPosition;

            // 버튼 크기 설정
            rectTransform.sizeDelta =
                size;

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
                    22,
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

        private static void BindPanelController(
            EncounterPanelController panelController,
            ExplorationMonsterEncounterController encounterController,
            GameObject panelObject,
            Text stateText,
            Text monsterIdText,
            Text roomIdText,
            Text gridPositionText,
            Text resultText,
            Button battleButton,
            Button escapeButton,
            Button testEndButton)
        {
            // 직렬화 객체 생성
            SerializedObject serializedObject =
                new SerializedObject(
                    panelController);

            // 인카운터 컨트롤러 연결
            serializedObject.FindProperty(
                "encounterController").objectReferenceValue =
                encounterController;

            // 패널 루트 연결
            serializedObject.FindProperty(
                "panelRoot").objectReferenceValue =
                panelObject;

            // 상태 텍스트 연결
            serializedObject.FindProperty(
                "stateText").objectReferenceValue =
                stateText;

            // 몬스터 텍스트 연결
            serializedObject.FindProperty(
                "monsterIdText").objectReferenceValue =
                monsterIdText;

            // 방 텍스트 연결
            serializedObject.FindProperty(
                "roomIdText").objectReferenceValue =
                roomIdText;

            // 그리드 텍스트 연결
            serializedObject.FindProperty(
                "gridPositionText").objectReferenceValue =
                gridPositionText;

            // 결과 텍스트 연결
            serializedObject.FindProperty(
                "resultText").objectReferenceValue =
                resultText;

            // 전투 버튼 연결
            serializedObject.FindProperty(
                "battleButton").objectReferenceValue =
                battleButton;

            // 회피 버튼 연결
            serializedObject.FindProperty(
                "escapeButton").objectReferenceValue =
                escapeButton;

            // 테스트 종료 버튼 연결
            serializedObject.FindProperty(
                "testEndButton").objectReferenceValue =
                testEndButton;

            // 직렬화 변경 적용
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            // 변경 상태 기록
            EditorUtility.SetDirty(
                panelController);
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

        private static bool ValidateEncounterUiInternal(
            Scene scene,
            bool logResult)
        {
            // 누락 목록 생성
            List<string> missingItems =
                new List<string>();

            // 캔버스 검색
            GameObject canvasObject =
                FindRootObject(
                    scene,
                    EncounterCanvasName);

            // 캔버스 확인
            if (canvasObject == null)
            {
                missingItems.Add(
                    EncounterCanvasName);
            }

            // 이벤트 시스템 검색
            EventSystem eventSystem =
                Object.FindFirstObjectByType<EventSystem>(
                    FindObjectsInactive.Include);

            // 이벤트 시스템 확인
            if (eventSystem == null)
            {
                missingItems.Add(
                    "EventSystem");
            }

            // 캔버스가 있는 경우
            if (canvasObject != null)
            {
                // Canvas 컴포넌트 확인
                if (canvasObject.GetComponent<Canvas>() == null)
                {
                    missingItems.Add(
                        "Canvas Component");
                }

                // 패널 컨트롤러 확인
                EncounterPanelController panelController =
                    canvasObject.GetComponent<EncounterPanelController>();

                // 패널 컨트롤러 누락 확인
                if (panelController == null)
                {
                    missingItems.Add(
                        "EncounterPanelController");
                }
                else
                {
                    // 직렬화 객체 생성
                    SerializedObject serializedObject =
                        new SerializedObject(
                            panelController);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "encounterController",
                        missingItems);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "panelRoot",
                        missingItems);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "stateText",
                        missingItems);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "monsterIdText",
                        missingItems);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "roomIdText",
                        missingItems);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "gridPositionText",
                        missingItems);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "resultText",
                        missingItems);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "battleButton",
                        missingItems);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "escapeButton",
                        missingItems);

                    // 필수 참조 확인
                    ValidateReference(
                        serializedObject,
                        "testEndButton",
                        missingItems);
                }
            }

            // 검증 성공 확인
            bool valid =
                missingItems.Count == 0;

            // 로그 생략 확인
            if (!logResult)
            {
                return valid;
            }

            // 성공 로그 출력
            if (valid)
            {
                Debug.Log(
                    "[Day44] Encounter UI validation passed.");

                return true;
            }

            // 실패 내용 결합
            string missingText =
                string.Join(
                    "\n- ",
                    missingItems);

            // 실패 로그 출력
            Debug.LogError(
                $"[Day44] Encounter UI validation failed.\n- {missingText}");

            // 실패 안내
            EditorUtility.DisplayDialog(
                "Project Delta - Day 44",
                $"다음 연결을 확인해야 합니다.\n- {missingText}",
                "확인");

            return false;
        }

        private static void ValidateReference(
            SerializedObject serializedObject,
            string propertyName,
            List<string> missingItems)
        {
            // 직렬화 필드 검색
            SerializedProperty property =
                serializedObject.FindProperty(
                    propertyName);

            // 필드 또는 참조 누락 확인
            if (property == null
                || property.objectReferenceValue == null)
            {
                missingItems.Add(
                    propertyName);
            }
        }
    }
}
