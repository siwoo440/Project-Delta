using System;
using System.Collections.Generic; // 마커 목록 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 기획서 10.3절 "방 표현": RoomView는 테마·문·계단·상자·비밀 벽·NPC 지점·환경 소품만
    // 표시하고, 몬스터 조우 확률이나 이벤트 결과를 직접 결정하지 않는다.
    // 문 열기 판정은 그대로 Domain(GridPassage.TryOpenDoor)이 담당하고, RoomView는
    // RoomPassageController를 통해 그 결과를 읽어 보여주기만 한다.
    [RequireComponent(typeof(RoomPassageController))] // 문 상태 조회에 필요
    public sealed class RoomView : MonoBehaviour // 방 표시 진입점
    {
        [SerializeField] private string themeId; // 현재 방 테마 식별자 (TODO: 26~35일차 던전 생성에서 연결)

        private RoomPassageController passageController; // 문 상태를 담당하는 통로 컨트롤러
        private readonly Dictionary<RoomContentType, List<RoomContentMarker>> markersByType = new Dictionary<RoomContentType, List<RoomContentMarker>>(); // 종류별 콘텐츠 자리 목록

        public string ThemeId => themeId; // 현재 방 테마 공개
        public RoomPassageController PassageController => passageController; // 문 상태 조회용 통로 컨트롤러 공개

        private void Awake() // 콘텐츠 자리 수집
        {
            passageController = GetComponent<RoomPassageController>(); // 같은 오브젝트의 통로 컨트롤러 연결
            CollectMarkers(); // 자식의 콘텐츠 자리 전부 수집
        }

        private void CollectMarkers() // 자식 콘텐츠 자리를 종류별로 정리
        {
            markersByType.Clear(); // 기존 수집 결과 초기화

            foreach (RoomContentMarker marker in GetComponentsInChildren<RoomContentMarker>(true)) // 비활성 오브젝트 포함 전체 검색
            {
                if (!markersByType.TryGetValue(marker.ContentType, out List<RoomContentMarker> list)) // 종류별 목록 존재 확인
                {
                    list = new List<RoomContentMarker>(); // 새 목록 생성
                    markersByType[marker.ContentType] = list; // 종류별 목록 등록
                }

                list.Add(marker); // 자리를 해당 종류 목록에 추가
            }
        }

        // 특정 종류의 콘텐츠 자리를 조회한다. 아직 어떤 콘텐츠를 배치할지는 이후 생성 시스템이 결정한다.
        public IReadOnlyList<RoomContentMarker> GetMarkers(RoomContentType type) // 종류별 콘텐츠 자리 조회
        {
            return markersByType.TryGetValue(type, out List<RoomContentMarker> list) ? list : Array.Empty<RoomContentMarker>(); // 없으면 빈 목록 반환
        }
    }
}
