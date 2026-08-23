using System.Collections.Generic; // 목록 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 25일차: 상자 하나가 실제로 담고 있는 아이템 목록과 개봉 상태를 보유한다.
    // RoomContentMarker(빈 자리 표시 전용, 19일차 원칙)와 별도 컴포넌트로 분리해서
    // "이 자리에 상자가 있다"는 표시와 "이 상자의 실제 내용물" 상태를 나눈다.
    // 같은 GameObject에 RoomContentMarker(ContentType = Chest)와 함께 붙인다.
    public sealed class ChestContentMarker : MonoBehaviour // 상자 내용물 보유
    {
        [SerializeField] private List<string> itemDisplayNames = new List<string>(); // 인스펙터에서 채우는 상자 내용물 (자리표시자 수준 아이템 이름)

        private readonly List<string> remainingItems = new List<string>(); // 아직 꺼내지 않은 아이템
        private bool initialized; // 최초 목록 복사 여부

        public IReadOnlyList<string> RemainingItems // 남은 아이템 목록 공개
        {
            get
            {
                EnsureInitialized(); // 최초 접근 시 목록 준비
                return remainingItems; // 남은 아이템 목록 반환
            }
        }

        private void EnsureInitialized() // 인스펙터 목록을 런타임 목록으로 최초 1회 복사
        {
            if (initialized) // 이미 초기화되었는지 확인
            {
                return; // 중복 초기화 중단
            }

            remainingItems.AddRange(itemDisplayNames); // 인스펙터 목록 복사
            initialized = true; // 초기화 완료 표시
        }

        // 지정한 순번의 아이템을 꺼낸다. 성공하면 true와 꺼낸 아이템 이름을 반환한다.
        public bool TryTake(int index, out string displayName) // 아이템 꺼내기 시도
        {
            EnsureInitialized(); // 목록 준비 확인

            if (index < 0 || index >= remainingItems.Count) // 순번 범위 확인
            {
                displayName = null; // 결과 이름 초기화
                return false; // 꺼내기 실패 반환
            }

            displayName = remainingItems[index]; // 꺼낼 아이템 이름 조회
            remainingItems.RemoveAt(index); // 목록에서 제거
            return true; // 꺼내기 성공 반환
        }
    }
}
