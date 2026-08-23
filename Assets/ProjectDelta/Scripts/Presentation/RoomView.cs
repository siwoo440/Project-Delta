using System;
using System.Collections.Generic; // 마커 목록 기능 사용
using ProjectDelta.Domain; // RoomExit 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation
{
    [RequireComponent(typeof(RoomPassageController))]
    public sealed class RoomView : MonoBehaviour
    {
        [SerializeField] private string themeId;

        private RoomPassageController passageController;
        private readonly Dictionary<RoomContentType, List<RoomContentMarker>> markersByType =
            new Dictionary<RoomContentType, List<RoomContentMarker>>();

        public string ThemeId => themeId;
        public RoomPassageController PassageController => passageController;

        private void Awake()
        {
            passageController = GetComponent<RoomPassageController>();
            RefreshMarkers();
        }

        // 36일차: 계단 등 런타임 마커를 추가한 뒤 다시 수집할 수 있게 공개한다.
        public void RefreshMarkers()
        {
            markersByType.Clear();

            foreach (RoomContentMarker marker in GetComponentsInChildren<RoomContentMarker>(true))
            {
                if (!markersByType.TryGetValue(marker.ContentType, out List<RoomContentMarker> list))
                {
                    list = new List<RoomContentMarker>();
                    markersByType[marker.ContentType] = list;
                }

                list.Add(marker);
            }
        }

        public IReadOnlyList<RoomContentMarker> GetMarkers(RoomContentType type)
        {
            return markersByType.TryGetValue(type, out List<RoomContentMarker> list)
                ? list
                : Array.Empty<RoomContentMarker>();
        }

        // 36일차: 그래프에 저장된 정확한 출구와 프리팹의 RoomExitMarker를 연결한다.
        public RoomExitMarker FindExitMarker(RoomExit target)
        {
            foreach (RoomExitMarker marker in GetComponentsInChildren<RoomExitMarker>(true))
            {
                if (marker.Exit == target)
                {
                    return marker;
                }
            }

            return null;
        }
    }
}
