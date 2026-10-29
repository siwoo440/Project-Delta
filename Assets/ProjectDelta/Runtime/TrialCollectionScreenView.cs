using System; // 닫기 동작 연결
using System.Collections.Generic; // 기록 목록 구성
using ProjectDelta.Core; // 결말과 대상 규칙
using UnityEngine; // 화면 객체 구성
using UnityEngine.EventSystems; // 취소 입력 처리
using UnityEngine.UI; // 화면 표시 구성
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class TrialCollectionScreenView : MonoBehaviour, ICancelHandler // 읽기 전용 시험 기록 화면
    { // 화면 구성 시작
        private const int PageSize = 6; // 페이지당 기록 수
        private Action close; // 타이틀 복귀 동작
        private bool closing; // 중복 닫기 방지
        private bool observationsUnavailable; // 관찰 기록 확인 불가 상태
        private bool defeatsUnavailable; // 패배 기록 확인 불가 상태
        private string[] observations; // 실제 관찰 기록
        private IReadOnlyList<DefeatHistoryEntry> defeats; // 실제 패배 기록
        private int observationPage; // 관찰 페이지 위치
        private int defeatPage; // 패배 페이지 위치
        private Text observationText; // 관찰 기록 표시
        private Text defeatText; // 패배 기록 표시
        private Button observationPrevious; // 관찰 이전 버튼
        private Button observationNext; // 관찰 다음 버튼
        private Button defeatPrevious; // 패배 이전 버튼
        private Button defeatNext; // 패배 다음 버튼
        public static GameObject Create(Transform parent, AppRoot owner, Action close) // 타이틀 기록 화면 생성
        { // 생성 시작
            if (parent == null || owner == null || close == null || EventSystem.current == null) // 화면과 입력 준비 검사
            { // 준비 실패 시작
                throw new InvalidOperationException("Collection screen requires a title and active input."); // 화면 생성 차단
            } // 준비 실패 끝
            var panel = new GameObject("TrialCollectionPanel", typeof(RectTransform), typeof(Image), typeof(TrialCollectionScreenView)); // 독립 기록 화면 생성
            try // 화면 구성 시도
            { // 화면 구성 시작
                panel.transform.SetParent(parent, false); // 타이틀 위 배치
                var rect = panel.GetComponent<RectTransform>(); // 화면 영역 조회
                rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래 여백 제거
                rect.offsetMax = Vector2.zero; // 위 여백 제거
                panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 화면 배경색
                var view = panel.GetComponent<TrialCollectionScreenView>(); // 화면 동작 조회
                view.close = close; // 닫기 동작 보관
                view.Build(owner); // 기록 조회와 화면 구성
                return panel; // 완성된 화면 전달
            } // 화면 구성 끝
            catch // 화면 구성 실패 정리
            { // 실패 정리 시작
                panel.SetActive(false); // 불완전한 화면 숨김
                UnityEngine.Object.Destroy(panel); // 임시 화면 제거
                throw; // 원래 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void Build(AppRoot owner) // 기록 조회와 표시 구성
        { // 구성 시작
            var endings = new EndingHistoryStorage(RunStorage.ResolveDirectory()); // 독립 결말 조회 저장소
            endings.Load(out string endingWarning); // 검증된 결말 기록 조회
            var endingsUnavailable = endings.IsBlocked || owner.HasVerifiedEndingFile && !endings.HasVerifiedCurrentFile; // 손상 또는 이전에 확인한 파일 누락
            var defeatStorage = new DefeatHistoryStorage(RunStorage.ResolveDirectory(), id => id == "trial-elite" || MonsterCatalog.Find(id) != null); // 독립 패배 조회 저장소
            defeats = defeatStorage.Load(out string defeatWarning); // 검증된 패배 기록 조회
            defeatsUnavailable = defeatStorage.IsBlocked || owner.HasVerifiedDefeatFile && !defeatStorage.HasVerifiedCurrentFile; // 손상 또는 이전에 확인한 파일 누락
            var observationStorage = new ObservationStorage(ObservationStorage.ResolveDirectory()); // 독립 관찰 조회 저장소
            var journal = observationStorage.Load(out string observationWarning); // 현재 관찰 파일 재검증
            var observationMissing = owner.HasVerifiedObservationFile && !observationStorage.HasVerifiedCurrentFile; // 이전에 확인한 관찰 파일 누락 검사
            observationsUnavailable = owner.ObservationSaveBlocked || observationStorage.IsBlocked || observationMissing; // 기존 보호와 현재 기록 오류 검사
            observations = observationsUnavailable ? Array.Empty<string>() : journal.Snapshot(); // 검증된 실제 관찰 기록 복사
            UiFactory.CreateText(transform, "TrialCollectionHeader", "시험 기록", 52, new Vector2(0f, 460f), new Vector2(1400f, 80f), Color.white); // 화면 제목
            UiFactory.CreateText(transform, "TrialCollectionScope", "내부 시험 기록 · 정식 엔딩 및 Steam 업적 해금과 무관", 24, new Vector2(0f, 400f), new Vector2(1700f, 55f), new Color(0.62f, 0.87f, 0.79f)); // 시험 범위 안내
            UiFactory.CreateText(transform, "TrialCollectionEndingHeader", "결말", 32, new Vector2(-610f, 335f), new Vector2(500f, 60f), Color.white); // 결말 제목
            UiFactory.CreateText(transform, "TrialCollectionObservationHeader", "관찰", 32, new Vector2(0f, 335f), new Vector2(500f, 60f), Color.white); // 관찰 제목
            UiFactory.CreateText(transform, "TrialCollectionDefeatHeader", "패배 기록", 32, new Vector2(610f, 335f), new Vector2(500f, 60f), Color.white); // 패배 제목
            var ids = new[] // 실제 지원 결말 목록
            { // 결말 목록 시작
                EndingRules.ReturnBasic, // 기본 귀환 결말
                EndingRules.ReturnHeroic, // 영웅 귀환 결말
                EndingRules.ReturnEmptyHanded, // 빈손 귀환 결말
                EndingRules.ReturnWounded, // 부상 귀환 결말
                EndingRules.StayBasic, // 기본 잔류 결말
                EndingRules.StayConquest, // 정복 잔류 결말
                EndingRules.DemonServant // 마왕의 종 결말
            }; // 결말 목록 끝
            var endingLines = new List<string>(); // 결말 표시 줄
            if (endingsUnavailable) // 결말 기록 확인 가능 검사
            { // 결말 확인 불가 시작
                endingLines.Add("결말 기록 확인 불가"); // 확인 불가 안내
            } // 결말 확인 불가 끝
            else // 검증된 결말 기록
            { // 결말 목록 구성 시작
                foreach (var id in ids) // 지원 결말 순회
                { // 결말 한 건 시작
                    endingLines.Add(endings.IsUnlocked(id) ? EndingRules.DisplayName(id) : "??? · 미해금"); // 해금 이름 또는 숨긴 항목
                } // 결말 한 건 끝
            } // 결말 목록 구성 끝
            var endingText = UiFactory.CreateText(transform, "TrialCollectionEndings", string.Join("\n", endingLines), 25, new Vector2(-610f, 140f), new Vector2(530f, 300f), Color.white); // 결말 목록 표시
            endingText.alignment = TextAnchor.UpperLeft; // 결말 왼쪽 정렬
            observationText = UiFactory.CreateText(transform, "TrialCollectionObservations", "", 25, new Vector2(0f, 140f), new Vector2(530f, 300f), Color.white); // 관찰 목록 표시
            observationText.alignment = TextAnchor.UpperLeft; // 관찰 왼쪽 정렬
            defeatText = UiFactory.CreateText(transform, "TrialCollectionDefeats", "", 25, new Vector2(610f, 140f), new Vector2(530f, 300f), Color.white); // 패배 목록 표시
            defeatText.alignment = TextAnchor.UpperLeft; // 패배 왼쪽 정렬
            observationPrevious = PageButton("TrialObservationPrevious", "이전", new Vector2(-120f, -70f), PreviousObservation); // 이전 관찰 기록
            observationNext = PageButton("TrialObservationNext", "다음", new Vector2(120f, -70f), NextObservation); // 다음 관찰 기록
            defeatPrevious = PageButton("TrialDefeatPrevious", "이전", new Vector2(490f, -70f), PreviousDefeat); // 이전 패배 기록
            defeatNext = PageButton("TrialDefeatNext", "다음", new Vector2(730f, -70f), NextDefeat); // 다음 패배 기록
            var observationStatus = observationsUnavailable ? "확인 불가" : observations.Length > 0 ? "달성" : "잠금"; // 실제 관찰 진행 상태
            var defeatStatus = defeatsUnavailable ? "확인 불가" : defeats.Count > 0 ? "달성" : "잠금"; // 실제 패배 진행 상태
            UiFactory.CreateText(transform, "TrialCollectionProgress", "첫 관찰 기록: " + observationStatus + "    첫 패배 기록: " + defeatStatus, 26, new Vector2(0f, -190f), new Vector2(1600f, 70f), Color.white); // 확인된 시험 진행 항목
            var warnings = new List<string>(); // 기록 확인 안내 목록
            if (endingsUnavailable) // 결말 저장 보호 검사
            { // 결말 보호 시작
                warnings.Add("결말 기록 확인 불가 · " + endingWarning); // 결말 복구 안내
            } // 결말 보호 끝
            if (defeatsUnavailable) // 패배 저장 보호 검사
            { // 패배 보호 시작
                warnings.Add("패배 기록 확인 불가 · " + defeatWarning); // 패배 복구 안내
            } // 패배 보호 끝
            if (observationsUnavailable) // 관찰 저장 보호 검사
            { // 관찰 보호 시작
                warnings.Add("관찰 기록 확인 불가 · " + (string.IsNullOrEmpty(observationWarning) ? owner.ObservationSaveMessage : observationWarning)); // 현재 또는 기존 관찰 복구 안내
            } // 관찰 보호 끝
            UiFactory.CreateText(transform, "TrialCollectionWarning", string.Join("\n", warnings), 19, new Vector2(0f, -285f), new Vector2(1700f, 110f), new Color(1f, 0.72f, 0.55f)); // 기록 확인 경고
            var button = UiFactory.CreateButton(transform, "CloseTrialCollectionButton", "닫기", new Vector2(0f, -445f)); // 기록 화면 닫기 버튼
            button.onClick.AddListener(Close); // 닫기 동작 연결
            AddCancel(button); // 선택 버튼 취소 연결
            RefreshLists(); // 기록 페이지 표시
            EventSystem.current.SetSelectedGameObject(button.gameObject); // 기본 입력 선택
        } // 구성 끝
        private void PreviousObservation() // 이전 관찰 페이지 선택
        { // 이동 시작
            observationPage--; // 이전 페이지 이동
            RefreshLists(); // 실제 목록 갱신
        } // 이동 끝
        private void NextObservation() // 다음 관찰 페이지 선택
        { // 이동 시작
            observationPage++; // 다음 페이지 이동
            RefreshLists(); // 실제 목록 갱신
        } // 이동 끝
        private void PreviousDefeat() // 이전 패배 페이지 선택
        { // 이동 시작
            defeatPage--; // 이전 페이지 이동
            RefreshLists(); // 실제 목록 갱신
        } // 이동 끝
        private void NextDefeat() // 다음 패배 페이지 선택
        { // 이동 시작
            defeatPage++; // 다음 페이지 이동
            RefreshLists(); // 실제 목록 갱신
        } // 이동 끝
        private Button PageButton(string name, string label, Vector2 position, Action action) // 페이지 이동 버튼 구성
        { // 버튼 구성 시작
            var button = UiFactory.CreateButton(transform, name, label, position); // 공통 버튼 생성
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 60f); // 페이지 버튼 크기
            var labelText = button.GetComponentInChildren<Text>(); // 버튼 글자 조회
            labelText.rectTransform.sizeDelta = new Vector2(180f, 60f); // 글자 영역 크기
            labelText.fontSize = 24; // 페이지 글자 크기
            button.onClick.AddListener(() => action()); // 페이지 이동 연결
            AddCancel(button); // 취소 입력 연결
            return button; // 완성 버튼 전달
        } // 버튼 구성 끝
        private void AddCancel(Button button) // 선택 버튼 취소 입력 연결
        { // 연결 시작
            button.gameObject.AddComponent<TrialCollectionCancelRelay>().Owner = this; // 선택 객체 취소 전달
        } // 연결 끝
        private void RefreshLists() // 기록 페이지 새로 표시
        { // 새로 표시 시작
            var observationLines = new List<string>(); // 관찰 표시 줄
            if (observationsUnavailable) // 관찰 기록 확인 불가 검사
            { // 확인 불가 시작
                observationLines.Add("관찰 기록 확인 불가"); // 확인 불가 안내
            } // 확인 불가 끝
            else if (observations.Length == 0) // 실제 기록 부재 검사
            { // 빈 기록 시작
                observationLines.Add("관찰 기록 없음"); // 빈 기록 안내
            } // 빈 기록 끝
            else // 관찰 기록 표시
            { // 관찰 기록 시작
                for (var index = observationPage * PageSize; index < observations.Length && index < (observationPage + 1) * PageSize; index++) // 현재 페이지 순회
                { // 관찰 한 건 시작
                    observationLines.Add(TargetName(observations[index])); // 실제 관찰 대상 이름
                } // 관찰 한 건 끝
            } // 관찰 기록 끝
            observationText.text = string.Join("\n", observationLines); // 관찰 목록 갱신
            var defeatLines = new List<string>(); // 패배 표시 줄
            if (defeatsUnavailable) // 패배 기록 확인 불가 검사
            { // 확인 불가 시작
                defeatLines.Add("패배 기록 확인 불가"); // 확인 불가 안내
            } // 확인 불가 끝
            else if (defeats.Count == 0) // 실제 기록 부재 검사
            { // 빈 기록 시작
                defeatLines.Add("패배 기록 없음"); // 빈 기록 안내
            } // 빈 기록 끝
            else // 패배 기록 표시
            { // 패배 기록 시작
                for (var index = defeatPage * PageSize; index < defeats.Count && index < (defeatPage + 1) * PageSize; index++) // 현재 페이지 순회
                { // 패배 한 건 시작
                    var entry = defeats[index]; // 실제 패배 기록
                    var reason = entry.Reason == BattleDefeatReason.HealthZero ? "체력 소진" : entry.Reason == BattleDefeatReason.Surrender ? "항복" : "정력 소진"; // 검증된 패배 원인
                    defeatLines.Add(TargetName(entry.TargetId) + " · " + reason); // 대상과 원인 표시
                } // 패배 한 건 끝
            } // 패배 기록 끝
            defeatText.text = string.Join("\n", defeatLines); // 패배 목록 갱신
            observationPrevious.interactable = !observationsUnavailable && observationPage > 0; // 관찰 이전 가능 여부
            observationNext.interactable = !observationsUnavailable && (observationPage + 1) * PageSize < observations.Length; // 관찰 다음 가능 여부
            defeatPrevious.interactable = !defeatsUnavailable && defeatPage > 0; // 패배 이전 가능 여부
            defeatNext.interactable = !defeatsUnavailable && (defeatPage + 1) * PageSize < defeats.Count; // 패배 다음 가능 여부
        } // 새로 표시 끝
        private static string TargetName(string id) // 기록 대상의 확인된 표시 이름
        { // 이름 조회 시작
            if (id == "trial-elite") // 시험 상위 대상 검사
            { // 시험 대상 시작
                return "시험 상위 개체"; // 시험 대상 이름
            } // 시험 대상 끝
            var monster = MonsterCatalog.Find(id); // 등록된 대상 조회
            return monster != null ? monster.Name : "기록된 대상"; // 실제 이름 또는 확인된 대상 안내
        } // 이름 조회 끝
        public void OnCancel(BaseEventData eventData) // 키보드와 게임패드 취소 처리
        { // 취소 처리 시작
            eventData.Use(); // 중복 취소 전달 방지
            Close(); // 타이틀 복귀
        } // 취소 처리 끝
        private void Close() // 기록 화면 종료
        { // 종료 시작
            if (closing) // 중복 종료 검사
            { // 중복 종료 시작
                return; // 중복 실행 차단
            } // 중복 종료 끝
            closing = true; // 종료 진행 표시
            close(); // 타이틀 복귀 요청
        } // 종료 끝
    } // 화면 구성 끝
    public sealed class TrialCollectionCancelRelay : MonoBehaviour, ICancelHandler // 선택 버튼의 취소 전달
    { // 전달 구성 시작
        public TrialCollectionScreenView Owner { get; set; } // 취소 대상 화면
        public void OnCancel(BaseEventData eventData) // 선택 버튼 취소 처리
        { // 취소 처리 시작
            Owner.OnCancel(eventData); // 화면 종료 전달
        } // 취소 처리 끝
    } // 전달 구성 끝
} // 이름 공간 끝
