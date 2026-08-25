using System; // 예외 기능 사용
using System.IO; // 파일 수정 기능 사용
using System.Text; // UTF-8 저장 기능 사용
using System.Text.RegularExpressions; // 코드 패턴 수정 기능 사용
using ProjectDelta.Presentation; // 전투 화면 컴포넌트 사용
using UnityEditor; // Unity Editor 기능 사용
using UnityEditor.SceneManagement; // 씬 변경 표시 기능 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.SceneManagement; // 현재 씬 정보 사용
using UnityEngine.UI; // Canvas UI 기능 사용

namespace ProjectDelta.Editor // 프로젝트 전용 Editor 네임스페이스
{
    public static class Day70SurrenderInstaller // 70일차 항복 시스템 설치 도구
    {
        private const string EncounterControllerPath =
            "Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs"; // 수정 대상 경로

        private const string CanvasName =
            "BattleSurrenderCanvas"; // 생성 Canvas 이름

        [MenuItem("Project Delta/70일차/항복 시스템 적용 + Canvas 생성")] // 실행 메뉴 등록
        public static void Install() // 코드 수정과 Canvas 생성을 한 번에 실행
        {
            CreateOrSelectCanvas(); // 항복 Canvas 생성
            PatchEncounterController(); // 기존 전투 컨트롤러 코드 수정
            AssetDatabase.Refresh(); // 수정된 스크립트 다시 불러오기
        }

        [MenuItem("Project Delta/70일차/항복 Canvas만 다시 생성")] // UI 재생성 메뉴 등록
        public static void RecreateCanvas() // Canvas만 다시 생성
        {
            GameObject existing =
                GameObject.Find(
                    CanvasName); // 기존 Canvas 조회

            if (existing != null) // 기존 Canvas 존재 확인
            {
                Undo.DestroyObjectImmediate(
                    existing); // 기존 Canvas 제거
            }

            CreateOrSelectCanvas(); // 새 Canvas 생성
        }

        private static void PatchEncounterController() // 전투 컨트롤러 자동 수정
        {
            if (!File.Exists(
                    EncounterControllerPath)) // 수정 대상 파일 확인
            {
                throw new FileNotFoundException(
                    "ExplorationMonsterEncounterController.cs를 찾을 수 없습니다.",
                    EncounterControllerPath); // 잘못된 프로젝트 구조 알림
            }

            string source =
                File.ReadAllText(
                    EncounterControllerPath); // 현재 소스 읽기

            source =
                source.Replace(
                    "\r\n",
                    "\n"); // 줄바꿈 형식 통일

            string updated =
                source; // 수정본 작업 시작

            updated =
                PatchBattleBegin(
                    updated); // 전투 시작 추적 초기화 추가

            updated =
                PatchDamageTracking(
                    updated); // 일반 공격과 스킬 피해 추적 추가

            updated =
                PatchDefeatExit(
                    updated); // 즉시 타이틀 복귀를 패배 처리 경유로 변경

            ValidatePatch(
                updated); // 필수 수정 결과 검증

            if (updated == source) // 이미 적용된 상태 확인
            {
                Debug.Log(
                    "[Project Delta] 70일차 전투 컨트롤러 수정이 이미 적용되어 있습니다."); // 중복 적용 방지 안내

                return;
            }

            File.WriteAllText(
                EncounterControllerPath,
                updated,
                new UTF8Encoding(
                    false)); // BOM 없는 UTF-8로 수정본 저장

            Debug.Log(
                "[Project Delta] 70일차 ExplorationMonsterEncounterController.cs 수정 완료"); // 수정 완료 로그
        }

        private static string PatchBattleBegin( // 전투 시작 추적 코드 추가
            string source) // 현재 소스 입력
        {
            if (source.Contains(
                    "BattleDefeatService.BeginBattle();")) // 기존 적용 여부 확인
            {
                return source;
            }

            string marker =
                "            Debug.Log(\n"
                + "                $\"[Project Delta] 54일차 Battle Starting /"; // 전투 시작 로그 위치

            if (!source.Contains(
                    marker)) // 기준 코드 존재 확인
            {
                throw new InvalidOperationException(
                    "전투 시작 수정 위치를 찾지 못했습니다. GitHub main 코드가 변경되었는지 확인하세요."); // 소스 불일치 차단
            }

            string replacement =
                "            BattleDefeatService.BeginBattle(); // 70일차 패배 추적 정보 초기화\n\n"
                + marker; // 초기화 코드와 기존 로그 결합

            return source.Replace(
                marker,
                replacement); // 전투 시작 코드 삽입
        }

        private static string PatchDamageTracking( // 직접 피해 공격자 추적 추가
            string source) // 현재 소스 입력
        {
            string pattern =
                "(?<indent>[ \\t]+)appliedDamage =\\n"
                + "[ \\t]+target\\.ApplyDamage\\(\\n"
                + "[ \\t]+damageResult\\.Damage\\);"
                + "(?!\\n\\n[ \\t]+BattleDefeatService\\.RecordAppliedDamage\\()"; // 들여쓰기와 무관하게 미적용 피해 블록 선택

            return Regex.Replace(
                source,
                pattern,
                match =>
                {
                    string indent =
                        match.Groups["indent"].Value; // 현재 블록 들여쓰기 보존

                    return match.Value
                        + "\n\n"
                        + indent
                        + "BattleDefeatService.RecordAppliedDamage(\n"
                        + indent
                        + "    actor,\n"
                        + indent
                        + "    target,\n"
                        + indent
                        + "    appliedDamage); // 70일차 마지막 실제 공격자 기록"; // 일반 공격과 스킬에 같은 추적 코드 추가
                }); // 일반 공격과 공격형 스킬 모두 수정
        }

        private static string PatchDefeatExit( // 패배 종료 흐름 교체
            string source) // 현재 소스 입력
        {
            if (source.Contains(
                    "BattleDefeatService.ReturnToTitleAfterDefeat(")) // 기존 적용 여부 확인
            {
                return source;
            }

            string oldCall =
                "            ApplicationFlow.Current?.ReturnToTitle();"; // 기존 즉시 복귀 호출

            if (!source.Contains(
                    oldCall)) // 기존 호출 존재 확인
            {
                throw new InvalidOperationException(
                    "패배 종료 수정 위치를 찾지 못했습니다. GitHub main 코드가 변경되었는지 확인하세요."); // 소스 불일치 차단
            }

            string newCall =
                "            BattleDefeatService.ReturnToTitleAfterDefeat(\n"
                + "                battleSession.Context,\n"
                + "                battleSession.RoundNumber); // 70일차 패배 기록 후 임시 타이틀 복귀"; // 새 패배 종료 호출

            return source.Replace(
                oldCall,
                newCall); // 패배 처리 경유 호출로 교체
        }

        private static void ValidatePatch( // 자동 수정 결과 검증
            string source) // 수정된 소스 입력
        {
            if (!source.Contains(
                    "BattleDefeatService.BeginBattle();")) // 시작 초기화 적용 확인
            {
                throw new InvalidOperationException(
                    "BattleDefeatService.BeginBattle 적용에 실패했습니다."); // 누락 차단
            }

            int damageTrackingCount =
                Regex.Matches(
                    source,
                    "BattleDefeatService\\.RecordAppliedDamage\\(").Count; // 직접 피해 추적 개수 계산

            if (damageTrackingCount < 2) // 공격과 스킬 두 위치 확인
            {
                throw new InvalidOperationException(
                    "일반 공격과 스킬의 피해 추적 코드가 모두 적용되지 않았습니다."); // 부분 적용 차단
            }

            if (!source.Contains(
                    "BattleDefeatService.ReturnToTitleAfterDefeat(")) // 패배 종료 적용 확인
            {
                throw new InvalidOperationException(
                    "패배 종료 처리 교체에 실패했습니다."); // 누락 차단
            }
        }

        private static void CreateOrSelectCanvas() // 항복 Canvas 생성 또는 선택
        {
            GameObject existing =
                GameObject.Find(
                    CanvasName); // 기존 Canvas 조회

            if (existing != null) // 기존 Canvas 존재 확인
            {
                Selection.activeGameObject =
                    existing; // 기존 Canvas 선택

                Debug.Log(
                    "[Project Delta] BattleSurrenderCanvas가 이미 존재해 새로 만들지 않았습니다."); // 중복 생성 방지 안내

                return;
            }

            GameObject canvasObject =
                new GameObject(
                    CanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster),
                    typeof(BattleSurrenderController)); // 항복 전용 Canvas 생성

            Undo.RegisterCreatedObjectUndo(
                canvasObject,
                "Create Battle Surrender Canvas"); // Undo 등록

            Canvas canvas =
                canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 조회

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay; // 화면 오버레이 Canvas 설정

            canvas.sortingOrder =
                120; // 기존 Battle HUD 위 표시

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>(); // Canvas Scaler 조회

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 대응 설정

            scaler.referenceResolution =
                new Vector2(
                    1920f,
                    1080f); // 기준 해상도 설정

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정

            scaler.matchWidthOrHeight =
                0.5f; // 가로 세로 균형 설정

            Button surrenderButton =
                CreateButton(
                    canvasObject.transform,
                    "SurrenderButton",
                    "항복",
                    new Vector2(
                        24f,
                        24f),
                    new Vector2(
                        220f,
                        64f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f),
                    new Vector2(
                        0f,
                        0f)); // 화면 왼쪽 아래 항복 버튼 생성

            GameObject confirmationRoot =
                CreatePanel(
                    canvasObject.transform,
                    "SurrenderConfirmation",
                    new Vector2(
                        520f,
                        260f)); // 화면 중앙 확인 패널 생성

            CreateText(
                confirmationRoot.transform,
                "MessageText",
                "정말 항복하시겠습니까?",
                new Vector2(
                    0f,
                    58f),
                new Vector2(
                    440f,
                    70f),
                30); // 확인 문구 생성

            Button confirmButton =
                CreateButton(
                    confirmationRoot.transform,
                    "ConfirmButton",
                    "확인",
                    new Vector2(
                        -120f,
                        -70f),
                    new Vector2(
                        180f,
                        58f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f)); // 확인 버튼 생성

            Button cancelButton =
                CreateButton(
                    confirmationRoot.transform,
                    "CancelButton",
                    "취소",
                    new Vector2(
                        120f,
                        -70f),
                    new Vector2(
                        180f,
                        58f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f),
                    new Vector2(
                        0.5f,
                        0.5f)); // 취소 버튼 생성

            BattleSurrenderController surrenderController =
                canvasObject.GetComponent<BattleSurrenderController>(); // 항복 UI 컨트롤러 조회

            SerializedObject serializedController =
                new SerializedObject(
                    surrenderController); // 비공개 Inspector 필드 연결 준비

            serializedController.FindProperty(
                    "encounterController").objectReferenceValue =
                UnityEngine.Object.FindFirstObjectByType<ExplorationMonsterEncounterController>(); // 현재 씬 전투 컨트롤러 연결

            serializedController.FindProperty(
                    "surrenderButton").objectReferenceValue =
                surrenderButton; // 항복 버튼 연결

            serializedController.FindProperty(
                    "confirmationRoot").objectReferenceValue =
                confirmationRoot; // 확인 패널 연결

            serializedController.FindProperty(
                    "confirmButton").objectReferenceValue =
                confirmButton; // 확인 버튼 연결

            serializedController.FindProperty(
                    "cancelButton").objectReferenceValue =
                cancelButton; // 취소 버튼 연결

            serializedController.ApplyModifiedPropertiesWithoutUndo(); // Inspector 연결 적용

            confirmationRoot.SetActive(
                false); // 확인 패널 기본 숨김

            EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene()); // 현재 씬 변경 상태 표시

            Selection.activeGameObject =
                canvasObject; // 생성 Canvas 선택

            Debug.Log(
                "[Project Delta] 70일차 BattleSurrenderCanvas 생성 및 참조 연결 완료"); // Canvas 생성 로그
        }

        private static GameObject CreatePanel( // 확인 패널 생성
            Transform parent, // 부모 Transform 입력
            string objectName, // 오브젝트 이름 입력
            Vector2 size) // 패널 크기 입력
        {
            GameObject panelObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image)); // Panel 오브젝트 생성

            panelObject.transform.SetParent(
                parent,
                false); // Canvas 아래 배치

            RectTransform rectTransform =
                panelObject.GetComponent<RectTransform>(); // RectTransform 조회

            rectTransform.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f); // 중앙 최소 Anchor

            rectTransform.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f); // 중앙 최대 Anchor

            rectTransform.pivot =
                new Vector2(
                    0.5f,
                    0.5f); // 중앙 Pivot

            rectTransform.anchoredPosition =
                Vector2.zero; // 화면 중앙 배치

            rectTransform.sizeDelta =
                size; // 패널 크기 적용

            Image image =
                panelObject.GetComponent<Image>(); // 배경 Image 조회

            image.color =
                new Color(
                    0.08f,
                    0.08f,
                    0.08f,
                    0.94f); // 어두운 패널 배경 적용

            return panelObject;
        }

        private static Button CreateButton( // 공통 버튼 생성
            Transform parent, // 부모 Transform 입력
            string objectName, // 오브젝트 이름 입력
            string label, // 버튼 문구 입력
            Vector2 anchoredPosition, // 위치 입력
            Vector2 size, // 크기 입력
            Vector2 anchorMin, // 최소 Anchor 입력
            Vector2 anchorMax, // 최대 Anchor 입력
            Vector2 pivot) // Pivot 입력
        {
            GameObject buttonObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button)); // Button 오브젝트 생성

            buttonObject.transform.SetParent(
                parent,
                false); // 부모 아래 배치

            RectTransform rectTransform =
                buttonObject.GetComponent<RectTransform>(); // 버튼 RectTransform 조회

            rectTransform.anchorMin =
                anchorMin; // 최소 Anchor 적용

            rectTransform.anchorMax =
                anchorMax; // 최대 Anchor 적용

            rectTransform.pivot =
                pivot; // Pivot 적용

            rectTransform.anchoredPosition =
                anchoredPosition; // 버튼 위치 적용

            rectTransform.sizeDelta =
                size; // 버튼 크기 적용

            Image image =
                buttonObject.GetComponent<Image>(); // 버튼 배경 조회

            image.color =
                new Color(
                    0.92f,
                    0.92f,
                    0.92f,
                    1f); // 밝은 버튼 배경 적용

            CreateText(
                buttonObject.transform,
                "Text",
                label,
                Vector2.zero,
                size,
                26); // 버튼 문구 생성

            return buttonObject.GetComponent<Button>(); // Button 컴포넌트 반환
        }

        private static Text CreateText( // 공통 UI Text 생성
            Transform parent, // 부모 Transform 입력
            string objectName, // 오브젝트 이름 입력
            string value, // 표시 문구 입력
            Vector2 anchoredPosition, // 위치 입력
            Vector2 size, // 크기 입력
            int fontSize) // 글자 크기 입력
        {
            GameObject textObject =
                new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(Text)); // Text 오브젝트 생성

            textObject.transform.SetParent(
                parent,
                false); // 부모 아래 배치

            RectTransform rectTransform =
                textObject.GetComponent<RectTransform>(); // Text RectTransform 조회

            rectTransform.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f); // 중앙 Anchor 최소값

            rectTransform.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f); // 중앙 Anchor 최대값

            rectTransform.pivot =
                new Vector2(
                    0.5f,
                    0.5f); // 중앙 Pivot

            rectTransform.anchoredPosition =
                anchoredPosition; // Text 위치 적용

            rectTransform.sizeDelta =
                size; // Text 크기 적용

            Text text =
                textObject.GetComponent<Text>(); // Text 컴포넌트 조회

            text.text =
                value; // 표시 문구 적용

            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf"); // Unity 기본 폰트 적용

            text.fontSize =
                fontSize; // 글자 크기 적용

            text.alignment =
                TextAnchor.MiddleCenter; // 중앙 정렬 적용

            text.color =
                Color.black; // 기본 글자색 적용

            if (parent.name == "SurrenderConfirmation") // 확인 패널 문구 여부 확인
            {
                text.color =
                    Color.white; // 확인 문구 흰색 적용
            }

            return text;
        }
    }
}
