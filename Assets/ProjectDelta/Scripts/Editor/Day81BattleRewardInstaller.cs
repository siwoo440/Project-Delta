using System; // 예외 처리 참조
using ProjectDelta.Presentation; // 보상 UI 컨트롤러 참조
using UnityEditor; // Unity Editor 기능 참조
using UnityEditor.SceneManagement; // Editor 씬 제어 참조
using UnityEngine; // Unity 기본 기능 참조
using UnityEngine.SceneManagement; // 씬 열기 모드 참조
using UnityEngine.UI; // uGUI 참조

namespace ProjectDelta.EditorTools // 에디터 도구 계층
{
    public static class Day81BattleRewardInstaller // 81일차 보상 UI 자동 구성
    {
        private const string DungeonScenePath = // 던전 씬 경로
            "Assets/ProjectDelta/Scenes/DungeonScene.unity";

        private const string RewardPanelName = // 전체 보상 패널 이름
            "BattleRewardPanel";

        private const string ResultPanelName = // 상단 결과 패널 이름
            "BattleResultPanel";

        private const string BonusPanelName = // 하단 추가 보상 패널 이름
            "BonusRewardPanel";

        private const string SummaryName = // 전투 결과 글자 이름
            "Day81RewardSummary";

        private const string BonusGuideName = // 추가 보상 안내 이름
            "BonusRewardGuide";

        [MenuItem("Project Delta/81일차/81일차 정식 전투 보상 화면 적용")] // 상단 실행 메뉴
        private static void Install() // 보상 UI 자동 재구성
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) // 현재 씬 저장 여부 확인
            {
                return; // 저장 취소 시 작업 중단
            }

            string originalScenePath = // 기존 활성 씬 경로 저장
                SceneManager.GetActiveScene().path;

            try // 안전한 씬 수정 시작
            {
                IntegrateRewardPanel(); // 두 구역 보상 패널 재구성
                RestoreOriginalScene( // 원래 작업 씬 복귀
                    originalScenePath);

                AssetDatabase.SaveAssets(); // 변경 에셋 저장
                AssetDatabase.Refresh(); // 에셋 데이터 갱신

                Debug.Log( // 완료 로그 출력
                    "[Project Delta] 81일차 전투 결과·추가 보상 분리 UI 적용 완료");
            }
            catch (Exception exception) // 자동 구성 오류 처리
            {
                Debug.LogException( // 오류 내용 출력
                    exception);

                RestoreOriginalScene( // 오류 발생 시 원래 씬 복귀
                    originalScenePath);

                throw; // 오류 재전달
            }
        }

        private static void IntegrateRewardPanel() // 던전 씬 보상 패널 통합
        {
            Scene scene = // 던전 씬 단독 열기
                EditorSceneManager.OpenScene(
                    DungeonScenePath,
                    OpenSceneMode.Single);

            BattleRewardPanelController controller = // 보상 패널 컨트롤러 탐색
                UnityEngine.Object.FindFirstObjectByType<BattleRewardPanelController>();

            if (controller == null) // 컨트롤러 존재 확인
            {
                throw new InvalidOperationException( // 구성 누락 오류
                    "DungeonScene에서 BattleRewardPanelController를 찾지 못했습니다.");
            }

            SerializedObject serializedController = // 컨트롤러 직렬화 접근
                new SerializedObject(
                    controller);

            GameObject panelRoot = // 기존 전체 패널 루트 가져오기
                serializedController.FindProperty(
                    "panelRoot").objectReferenceValue
                as GameObject;

            if (panelRoot == null) // 패널 연결 확인
            {
                throw new InvalidOperationException( // 패널 누락 오류
                    "BattleRewardPanelController의 Panel Root가 연결되어 있지 않습니다.");
            }

            ConfigurePanelRoot( // 전체 패널 크기와 배경 정리
                panelRoot);

            RemovePanelChildren( // 이전 구성 전체 제거
                panelRoot.transform);

            GameObject resultPanel = // 상단 전투 결과 패널 생성
                CreateSectionPanel(
                    panelRoot.transform,
                    ResultPanelName,
                    new Vector2(
                        0.05f,
                        0.38f),
                    new Vector2(
                        0.95f,
                        0.94f),
                    new Color(
                        0.10f,
                        0.12f,
                        0.17f,
                        0.98f));

            Text summaryText = // 상단 전투 결과 글자 생성
                CreateSummaryText(
                    resultPanel.transform);

            GameObject bonusPanel = // 하단 추가 보상 패널 생성
                CreateSectionPanel(
                    panelRoot.transform,
                    BonusPanelName,
                    new Vector2(
                        0.05f,
                        0.06f),
                    new Vector2(
                        0.95f,
                        0.33f),
                    new Color(
                        0.13f,
                        0.15f,
                        0.21f,
                        0.98f));

            CreateBonusGuide( // 하단 추가 보상 안내 생성
                bonusPanel.transform);

            Button[] rewardButtons = // 추가 보상 버튼 배열 생성
                new Button[3];

            Text[] rewardTexts = // 추가 보상 버튼 글자 배열 생성
                new Text[3];

            rewardButtons[0] = // 첫 번째 골드 보상 버튼 생성
                CreateRewardButton(
                    bonusPanel.transform,
                    "RewardButton_Gold",
                    "골드 +100",
                    0.04f,
                    0.33f,
                    out rewardTexts[0]);

            rewardButtons[1] = // 두 번째 체력 보상 버튼 생성
                CreateRewardButton(
                    bonusPanel.transform,
                    "RewardButton_Health",
                    "HP +10",
                    0.355f,
                    0.645f,
                    out rewardTexts[1]);

            rewardButtons[2] = // 세 번째 마나 보상 버튼 생성
                CreateRewardButton(
                    bonusPanel.transform,
                    "RewardButton_Mana",
                    "MP +5",
                    0.67f,
                    0.96f,
                    out rewardTexts[2]);

            serializedController.FindProperty( // 상단 결과 글자 연결
                    "summaryText").objectReferenceValue =
                summaryText;

            SerializedProperty buttonArray = // 버튼 배열 프로퍼티 가져오기
                serializedController.FindProperty(
                    "rewardButtons");

            buttonArray.arraySize = // 버튼 배열 크기 설정
                rewardButtons.Length;

            for (int index = 0; // 버튼 배열 순회
                 index < rewardButtons.Length; // 배열 범위 확인
                 index++) // 다음 버튼 이동
            {
                buttonArray.GetArrayElementAtIndex( // 버튼 참조 저장
                    index).objectReferenceValue =
                    rewardButtons[index];
            }

            SerializedProperty textArray = // 버튼 글자 배열 프로퍼티 가져오기
                serializedController.FindProperty(
                    "rewardTexts");

            textArray.arraySize = // 글자 배열 크기 설정
                rewardTexts.Length;

            for (int index = 0; // 글자 배열 순회
                 index < rewardTexts.Length; // 배열 범위 확인
                 index++) // 다음 글자 이동
            {
                textArray.GetArrayElementAtIndex( // 글자 참조 저장
                    index).objectReferenceValue =
                    rewardTexts[index];
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo(); // 컨트롤러 연결 반영
            EditorUtility.SetDirty( // 컨트롤러 변경 상태 표시
                controller);

            panelRoot.transform.SetAsLastSibling(); // HUD 최상단 표시 순서
            panelRoot.SetActive( // 플레이 전 기본 숨김
                false);

            EditorSceneManager.MarkSceneDirty( // 씬 변경 상태 표시
                scene);

            EditorSceneManager.SaveScene( // 수정된 던전 씬 저장
                scene);
        }

        private static void ConfigurePanelRoot( // 전체 보상 패널 외형 설정
            GameObject panelRoot) // 기존 보상 패널
        {
            panelRoot.name = // 패널 이름 통일
                RewardPanelName;

            RectTransform rect = // 패널 RectTransform 가져오기
                panelRoot.GetComponent<RectTransform>();

            if (rect == null) // RectTransform 확인
            {
                throw new InvalidOperationException( // UI 구조 오류
                    "BattleRewardPanel에 RectTransform이 없습니다.");
            }

            rect.anchorMin = // 화면 중앙 고정 앵커
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchorMax = // 화면 중앙 고정 앵커
                rect.anchorMin;

            rect.pivot = // 중앙 피벗
                new Vector2(
                    0.5f,
                    0.5f);

            rect.anchoredPosition = // 화면 중앙 위치
                Vector2.zero;

            rect.sizeDelta = // 두 하위 패널을 담는 전체 크기
                new Vector2(
                    960f,
                    760f);

            Image image = // 전체 패널 배경 이미지 가져오기
                panelRoot.GetComponent<Image>();

            if (image == null) // 배경 이미지 확인
            {
                image = // 누락 시 배경 이미지 추가
                    panelRoot.AddComponent<Image>();
            }

            image.color = // 가장 바깥쪽 어두운 배경
                new Color(
                    0.045f,
                    0.052f,
                    0.075f,
                    0.98f);

            image.raycastTarget = // 배경 클릭 차단
                true;

            Outline outline = // 전체 패널 외곽선 탐색
                panelRoot.GetComponent<Outline>();

            if (outline == null) // 외곽선 확인
            {
                outline = // 누락 시 외곽선 추가
                    panelRoot.AddComponent<Outline>();
            }

            outline.effectColor = // 전체 패널 테두리 색상
                new Color(
                    0.40f,
                    0.48f,
                    0.66f,
                    0.82f);

            outline.effectDistance = // 전체 테두리 두께
                new Vector2(
                    2f,
                    -2f);

            outline.useGraphicAlpha = // 배경 알파 연동
                true;
        }

        private static void RemovePanelChildren( // 기존 패널 내부 UI 제거
            Transform panelTransform) // 전체 패널 Transform
        {
            for (int index = // 마지막 자식부터 순회
                     panelTransform.childCount - 1;
                 index >= 0; // 첫 자식까지 확인
                 index--) // 이전 자식 이동
            {
                UnityEngine.Object.DestroyImmediate( // 기존 자식 즉시 제거
                    panelTransform.GetChild(
                        index).gameObject);
            }
        }

        private static GameObject CreateSectionPanel( // 하위 구역 패널 생성
            Transform parent, // 전체 패널 부모
            string objectName, // 하위 패널 이름
            Vector2 anchorMin, // 하위 패널 왼쪽 아래
            Vector2 anchorMax, // 하위 패널 오른쪽 위
            Color backgroundColor) // 하위 패널 배경색
        {
            GameObject panelObject = // 하위 패널 오브젝트 생성
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Outline));

            panelObject.layer = // 부모 UI 레이어 사용
                parent.gameObject.layer;

            panelObject.transform.SetParent( // 전체 패널 내부 연결
                parent,
                false);

            RectTransform rect = // 하위 패널 RectTransform 가져오기
                panelObject.GetComponent<RectTransform>();

            rect.anchorMin = // 하위 패널 시작 앵커
                anchorMin;

            rect.anchorMax = // 하위 패널 끝 앵커
                anchorMax;

            rect.offsetMin = // 추가 여백 제거
                Vector2.zero;

            rect.offsetMax = // 추가 여백 제거
                Vector2.zero;

            Image image = // 하위 패널 배경 이미지 가져오기
                panelObject.GetComponent<Image>();

            image.color = // 하위 패널 배경색 적용
                backgroundColor;

            image.raycastTarget = // 하위 패널 클릭 차단
                true;

            Outline outline = // 하위 패널 외곽선 가져오기
                panelObject.GetComponent<Outline>();

            outline.effectColor = // 하위 패널 구분 테두리
                new Color(
                    0.34f,
                    0.41f,
                    0.57f,
                    0.78f);

            outline.effectDistance = // 하위 패널 테두리 두께
                new Vector2(
                    1.5f,
                    -1.5f);

            outline.useGraphicAlpha = // 배경 알파 연동
                true;

            return panelObject; // 생성된 하위 패널 반환
        }

        private static Text CreateSummaryText( // 상단 전투 결과 글자 생성
            Transform parent) // 결과 패널 부모
        {
            GameObject textObject = // 결과 글자 오브젝트 생성
                new GameObject(
                    SummaryName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            textObject.layer = // 부모 UI 레이어 사용
                parent.gameObject.layer;

            textObject.transform.SetParent( // 결과 패널 내부 연결
                parent,
                false);

            RectTransform rect = // 결과 글자 RectTransform 가져오기
                textObject.GetComponent<RectTransform>();

            rect.anchorMin = // 결과 패널 내부 왼쪽 아래
                new Vector2(
                    0.05f,
                    0.05f);

            rect.anchorMax = // 결과 패널 내부 오른쪽 위
                new Vector2(
                    0.95f,
                    0.95f);

            rect.offsetMin = // 추가 여백 제거
                Vector2.zero;

            rect.offsetMax = // 추가 여백 제거
                Vector2.zero;

            Text text = // 결과 Text 컴포넌트 가져오기
                textObject.GetComponent<Text>();

            text.font = // Unity 기본 폰트 사용
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            text.fontSize = // 결과 글자 크기
                24;

            text.fontStyle = // 결과 기본 글자 스타일
                FontStyle.Normal;

            text.alignment = // 전체 결과 중앙 정렬
                TextAnchor.MiddleCenter;

            text.horizontalOverflow = // 긴 내용 자동 줄바꿈
                HorizontalWrapMode.Wrap;

            text.verticalOverflow = // 아이템 목록 세로 출력
                VerticalWrapMode.Overflow;

            text.lineSpacing = // 결과 줄 간격
                1.05f;

            text.color = // 밝은 결과 글자
                new Color(
                    0.96f,
                    0.97f,
                    1f,
                    1f);

            text.raycastTarget = // 버튼 클릭 방해 방지
                false;

            text.supportRichText = // 단순 텍스트 사용
                false;

            text.text = // 편집기 확인용 결과 미리보기
                "전투 승리\n\n"
                + "획득 경험치 +0 EXP\n"
                + "레벨 Lv.1 / 변화 없음\n\n"
                + "획득 골드 0 Gold\n"
                + "획득 아이템 없음";

            return text; // 생성된 결과 글자 반환
        }

        private static Text CreateBonusGuide( // 추가 보상 안내 글자 생성
            Transform parent) // 추가 보상 패널 부모
        {
            GameObject textObject = // 안내 글자 오브젝트 생성
                new GameObject(
                    BonusGuideName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            textObject.layer = // 부모 UI 레이어 사용
                parent.gameObject.layer;

            textObject.transform.SetParent( // 추가 보상 패널 내부 연결
                parent,
                false);

            RectTransform rect = // 안내 글자 RectTransform 가져오기
                textObject.GetComponent<RectTransform>();

            rect.anchorMin = // 안내 영역 왼쪽 아래
                new Vector2(
                    0.05f,
                    0.58f);

            rect.anchorMax = // 안내 영역 오른쪽 위
                new Vector2(
                    0.95f,
                    0.94f);

            rect.offsetMin = // 추가 여백 제거
                Vector2.zero;

            rect.offsetMax = // 추가 여백 제거
                Vector2.zero;

            Text text = // 안내 Text 컴포넌트 가져오기
                textObject.GetComponent<Text>();

            text.font = // Unity 기본 폰트 사용
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            text.fontSize = // 추가 보상 안내 글자 크기
                36;

            text.fontStyle = // 추가 보상 안내 굵게 표시
                FontStyle.Bold;

            text.alignment = // 안내 문구 중앙 정렬
                TextAnchor.MiddleCenter;

            text.horizontalOverflow = // 한 줄 우선 표시
                HorizontalWrapMode.Wrap;

            text.verticalOverflow = // 안내 영역 안에서 표시
                VerticalWrapMode.Truncate;

            text.color = // 강조 안내 글자 색상
                Color.white;

            text.raycastTarget = // 버튼 클릭 방해 방지
                false;

            text.text = // 추가 보상 안내 문구
                "추가 보상 하나를 선택하세요.";

            return text; // 생성된 안내 글자 반환
        }

        private static Button CreateRewardButton( // 사각형 보상 버튼 생성
            Transform parent, // 추가 보상 패널 부모
            string objectName, // 버튼 오브젝트 이름
            string label, // 버튼 기본 글자
            float left, // 버튼 왼쪽 앵커
            float right, // 버튼 오른쪽 앵커
            out Text labelText) // 생성된 버튼 글자
        {
            GameObject buttonObject = // 버튼 오브젝트 생성
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(Outline));

            buttonObject.layer = // 부모 UI 레이어 사용
                parent.gameObject.layer;

            buttonObject.transform.SetParent( // 추가 보상 패널 내부 연결
                parent,
                false);

            RectTransform rect = // 버튼 RectTransform 가져오기
                buttonObject.GetComponent<RectTransform>();

            rect.anchorMin = // 버튼 왼쪽 아래 위치
                new Vector2(
                    left,
                    0.08f);

            rect.anchorMax = // 버튼 오른쪽 위 위치
                new Vector2(
                    right,
                    0.50f);

            rect.offsetMin = // 추가 여백 제거
                Vector2.zero;

            rect.offsetMax = // 추가 여백 제거
                Vector2.zero;

            Image image = // 버튼 배경 이미지 가져오기
                buttonObject.GetComponent<Image>();

            image.color = // 기본 버튼 배경
                new Color(
                    0.17f,
                    0.20f,
                    0.28f,
                    1f);

            Button button = // Button 컴포넌트 가져오기
                buttonObject.GetComponent<Button>();

            button.targetGraphic = // 버튼 전환 대상 설정
                image;

            button.transition = // 색상 전환 방식 사용
                Selectable.Transition.ColorTint;

            ColorBlock colors = // 기본 색상 블록 복사
                button.colors;

            colors.normalColor = // 평상시 버튼 색상
                new Color(
                    0.17f,
                    0.20f,
                    0.28f,
                    1f);

            colors.highlightedColor = // 마우스 오버 색상
                new Color(
                    0.27f,
                    0.33f,
                    0.46f,
                    1f);

            colors.pressedColor = // 클릭 중 색상
                new Color(
                    0.10f,
                    0.13f,
                    0.19f,
                    1f);

            colors.selectedColor = // 선택 상태 색상
                colors.highlightedColor;

            colors.disabledColor = // 비활성 상태 색상
                new Color(
                    0.10f,
                    0.11f,
                    0.14f,
                    0.55f);

            colors.colorMultiplier = // 색상 배율
                1f;

            colors.fadeDuration = // 색상 전환 시간
                0.08f;

            button.colors = // 버튼 색상 블록 반영
                colors;

            Outline outline = // 버튼 외곽선 가져오기
                buttonObject.GetComponent<Outline>();

            outline.effectColor = // 버튼 테두리 색상
                new Color(
                    0.50f,
                    0.61f,
                    0.82f,
                    0.92f);

            outline.effectDistance = // 버튼 테두리 두께
                new Vector2(
                    1.5f,
                    -1.5f);

            outline.useGraphicAlpha = // 버튼 알파 연동
                true;

            labelText = // 버튼 중앙 글자 생성
                CreateButtonLabel(
                    buttonObject.transform,
                    label);

            return button; // 생성된 버튼 반환
        }

        private static Text CreateButtonLabel( // 버튼 글자 생성
            Transform parent, // 버튼 부모
            string label) // 표시 문구
        {
            GameObject textObject = // 버튼 글자 오브젝트 생성
                new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            textObject.layer = // 부모 UI 레이어 사용
                parent.gameObject.layer;

            textObject.transform.SetParent( // 버튼 내부 연결
                parent,
                false);

            RectTransform rect = // 글자 RectTransform 가져오기
                textObject.GetComponent<RectTransform>();

            rect.anchorMin = // 버튼 전체 영역 사용
                Vector2.zero;

            rect.anchorMax = // 버튼 전체 영역 사용
                Vector2.one;

            rect.offsetMin = // 내부 여백 설정
                new Vector2(
                    12f,
                    8f);

            rect.offsetMax = // 내부 여백 설정
                new Vector2(
                    -12f,
                    -8f);

            Text text = // Text 컴포넌트 가져오기
                textObject.GetComponent<Text>();

            text.font = // Unity 기본 폰트 사용
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");

            text.fontSize = // 버튼 글자 크기
                24;

            text.fontStyle = // 버튼 강조 글자
                FontStyle.Bold;

            text.alignment = // 버튼 중앙 정렬
                TextAnchor.MiddleCenter;

            text.horizontalOverflow = // 버튼 안에서 줄바꿈
                HorizontalWrapMode.Wrap;

            text.verticalOverflow = // 버튼 높이 안에서 표시
                VerticalWrapMode.Truncate;

            text.color = // 버튼 글자 색상
                Color.white;

            text.raycastTarget = // 클릭을 부모 버튼으로 전달
                false;

            text.text = // 기본 보상 이름 출력
                label;

            return text; // 생성된 버튼 글자 반환
        }

        private static void RestoreOriginalScene( // 기존 작업 씬 복귀
            string originalScenePath) // 복귀할 씬 경로
        {
            if (string.IsNullOrEmpty( // 기존 씬 경로 확인
                    originalScenePath)
                || originalScenePath == DungeonScenePath)
            {
                EditorSceneManager.OpenScene( // 던전 씬 유지
                    DungeonScenePath,
                    OpenSceneMode.Single);

                return; // 복귀 처리 종료
            }

            EditorSceneManager.OpenScene( // 기존 작업 씬 다시 열기
                originalScenePath,
                OpenSceneMode.Single);
        }
    }
}
