using System; // 씨드 재선택 범위
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class TrialStartSeedRules // 첫 층의 접근 가능한 휴식 보장
    { // 규칙 시작
        public static bool IsSuitable(int seed) // 첫 층의 실제 콘텐츠와 휴식 공간 검사
        { // 검사 시작
            try // 실제 생성 규칙 보호
            { // 생성 시작
                var layout = FloorLayoutRules.Generate(seed, 1, 3); // 새 회차의 첫 층 구조
                var floorSeed = FloorSeedRules.For(seed, 1); // 첫 층 배치 씨드
                var encounters = TrialRoomContentPlacement.Excluded(layout, 1, 3, true); // 첫 조우와 다수 조우 제외
                if (encounters == null || !FloorRoomContent.TryCreate(1, layout, floorSeed, encounters, out var content)) // 기존 상자와 함정 배치 검사
                { // 기존 배치 오류 시작
                    return false; // 잘못된 첫 층 차단
                } // 기존 배치 오류 끝
                var saved = content.Snapshot(); // 검증된 기존 콘텐츠 정보
                if (!TrialEventPlacement.TrySelect(layout, floorSeed, saved, encounters, out var eventRoom)) // 실제 이벤트 방 검사
                { // 이벤트 오류 시작
                    return false; // 기존 이벤트 없는 시작 차단
                } // 이벤트 오류 끝
                var accessible = TrialRestRules.ExcludeLockedRoom(layout, encounters); // 잠긴 말단 방 제외
                return accessible != null && TrialRestRules.TryCreate(1, layout, floorSeed, saved, accessible, eventRoom, null, out _); // 접근 가능한 휴식 방 확인
            } // 생성 끝
            catch (Exception) // 잘못된 던전 생성 검사
            { // 오류 시작
                return false; // 생성할 수 없는 시작 씨드 차단
            } // 오류 끝
        } // 검사 끝
        public static bool TrySelect(int initial, out int selected) // 유효한 시작 씨드의 결정적 선택
        { // 선택 시작
            selected = initial; // 기본 입력 씨드 보존
            for (var offset = 0; offset < 2048; offset++) // 제한된 후보 씨드 순회
            { // 후보 검사 시작
                var candidate = unchecked(initial + offset); // 정수 경계를 넘는 후보의 안전한 순환
                if (IsSuitable(candidate)) // 실제 첫 층의 휴식 가능 검사
                { // 선택 성공 시작
                    selected = candidate; // 검증된 씨드 확정
                    return true; // 접근 가능한 시작 선택 완료
                } // 선택 성공 끝
            } // 후보 검사 끝
            for (var candidate = 0; candidate < 1002; candidate++) // 검증 가능한 고정 범위의 보조 후보
            { // 보조 검사 시작
                if (IsSuitable(candidate)) // 실제 첫 층의 휴식 가능 검사
                { // 보조 선택 시작
                    selected = candidate; // 검증된 대체 씨드 확정
                    return true; // 접근 가능한 시작 선택 완료
                } // 보조 선택 끝
            } // 보조 검사 끝
            return false; // 유효한 씨드가 없는 경우 명시적 실패
        } // 선택 끝
    } // 규칙 끝
} // 이름 공간 끝
