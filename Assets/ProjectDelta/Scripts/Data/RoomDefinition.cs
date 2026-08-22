using System.Collections.Generic; // 통로 목록 기능 사용
using ProjectDelta.Domain; // 통로 항목 데이터 형식 사용
using UnityEngine; // ScriptableObject 기능 사용

namespace ProjectDelta.Data // 데이터 네임스페이스
{
    // 방 하나의 정적 설계도 (기획서 10.2절 정적 정의 데이터 "RoomDefinition | 방 프리팹과 연결 규칙").
    // 어떤 문/벽이 어디에 있는지만 담고, 문이 열렸는지 같은 회차 중 상태는 담지 않는다.
    // 그 런타임 상태는 ProjectDelta.Domain.RoomInstance가 담당한다.
    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "ProjectDelta/Data/Room Definition")]
    public sealed class RoomDefinition : DefinitionBase // 방 정의 데이터
    {
        [SerializeField] private int width = 5; // 방 가로 칸 수
        [SerializeField] private int height = 5; // 방 세로 칸 수
        [SerializeField] private List<PassageEntry> passages = new List<PassageEntry>(); // 방향별 통로 배치 목록

        public int Width => width; // 방 가로 칸 수 공개
        public int Height => height; // 방 세로 칸 수 공개
        public IReadOnlyList<PassageEntry> Passages => passages; // 통로 배치 목록 공개
    }
}
