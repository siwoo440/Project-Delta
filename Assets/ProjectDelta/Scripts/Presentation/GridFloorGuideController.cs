using System.Collections.Generic; // 생성 선 목록 기능 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public sealed class GridFloorGuideController : MonoBehaviour // 이동 가능한 그리드 칸 바닥 선 표시
    {
        [SerializeField] private int minX = -2; // 표시 최소 X 칸
        [SerializeField] private int maxX = 2; // 표시 최대 X 칸
        [SerializeField] private int minZ = -2; // 표시 최소 Z 칸
        [SerializeField] private int maxZ = 2; // 표시 최대 Z 칸
        [SerializeField] private float cellSize = 2f; // 한 칸 월드 크기
        [SerializeField] private float lineHeight = 0.015f; // 바닥 위 선 높이
        [SerializeField] private float lineWidth = 0.025f; // 바닥 선 굵기

        private readonly List<GameObject> lineObjects = new List<GameObject>(); // 생성된 칸 선 오브젝트 목록
        private Material lineMaterial; // 바닥 선 전용 재질
        private bool guideVisible = true; // 현재 가이드 표시 여부

        private void Awake() // 이동 가능 칸 선 생성
        {
            CreateLineMaterial(); // 바닥 선 재질 생성
            CreateGridLines(); // 방 전체 이동 가능 칸 외곽선 생성
            ApplyVisibility(); // 초기 표시 상태 적용
        }

        private void OnDestroy() // 런타임 재질 정리
        {
            if (lineMaterial != null) // 생성 재질 존재 확인
            {
                Destroy(lineMaterial); // 런타임 재질 제거
            }
        }

        public void SetGuideVisible(bool visible) // 현재 방 이동 가능 칸 선 표시 전환
        {
            guideVisible = visible; // 표시 상태 저장
            ApplyVisibility(); // 생성된 선 오브젝트 표시 갱신
        }

        private void CreateLineMaterial() // 선 전용 런타임 재질 생성
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit"); // URP Unlit 셰이더 검색

            if (shader == null) // URP 셰이더 검색 실패 확인
            {
                shader = Shader.Find("Sprites/Default"); // 기본 스프라이트 셰이더 대체 검색
            }

            if (shader == null) // 사용 가능한 셰이더 확인
            {
                Debug.LogWarning("[Project Delta] 이동 가능 칸 가이드용 셰이더를 찾지 못했습니다.", this); // 셰이더 누락 경고 출력
                return; // 재질 생성 중단
            }

            lineMaterial = new Material(shader); // 선 전용 런타임 재질 생성
            lineMaterial.name = "GridFloorGuide_Runtime"; // 런타임 재질 이름 지정
            lineMaterial.color = new Color(1f, 1f, 1f, 0.55f); // 반투명 흰색 선 적용

            if (lineMaterial.HasProperty("_BaseColor")) // URP 기본 색상 속성 확인
            {
                lineMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.55f)); // URP 기본 색상 적용
            }
        }

        private void CreateGridLines() // 이동 가능한 모든 칸 외곽선 생성
        {
            for (int x = minX; x <= maxX; x++) // X 칸 범위 순회
            {
                for (int z = minZ; z <= maxZ; z++) // Z 칸 범위 순회
                {
                    CreateCellLine(x, z); // 현재 이동 가능 칸 선 생성
                }
            }
        }

        private void CreateCellLine(int gridX, int gridZ) // 한 칸 사각 외곽선 생성
        {
            GameObject lineObject = new GameObject($"GridGuide_{gridX}_{gridZ}"); // 칸 선 오브젝트 생성
            lineObject.transform.SetParent(transform, false); // 현재 방 자식으로 연결
            lineObject.transform.localPosition = Vector3.zero; // 방 원점 기준 위치 설정
            lineObject.transform.localRotation = Quaternion.identity; // 방 기준 회전 초기화
            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>(); // 선 렌더러 추가
            lineRenderer.useWorldSpace = false; // 방 로컬 좌표 기준 사용
            lineRenderer.loop = true; // 사각형 마지막 선 자동 연결
            lineRenderer.positionCount = 4; // 사각형 꼭짓점 네 개 설정
            lineRenderer.startWidth = lineWidth; // 시작 선 굵기 적용
            lineRenderer.endWidth = lineWidth; // 끝 선 굵기 적용
            lineRenderer.numCornerVertices = 0; // 모서리 추가 정점 비활성화
            lineRenderer.numCapVertices = 0; // 선 끝 추가 정점 비활성화
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 선 그림자 비활성화
            lineRenderer.receiveShadows = false; // 선 그림자 수신 비활성화

            if (lineMaterial != null) // 선 재질 생성 여부 확인
            {
                lineRenderer.sharedMaterial = lineMaterial; // 모든 칸 선 재질 공유
            }

            float centerX = gridX * cellSize; // 현재 칸 중심 X 계산
            float centerZ = gridZ * cellSize; // 현재 칸 중심 Z 계산
            float half = cellSize * 0.5f; // 칸 절반 크기 계산
            lineRenderer.SetPosition(0, new Vector3(centerX - half, lineHeight, centerZ - half)); // 좌하단 꼭짓점 설정
            lineRenderer.SetPosition(1, new Vector3(centerX - half, lineHeight, centerZ + half)); // 좌상단 꼭짓점 설정
            lineRenderer.SetPosition(2, new Vector3(centerX + half, lineHeight, centerZ + half)); // 우상단 꼭짓점 설정
            lineRenderer.SetPosition(3, new Vector3(centerX + half, lineHeight, centerZ - half)); // 우하단 꼭짓점 설정
            lineObjects.Add(lineObject); // 생성 선 목록 등록
        }

        private void ApplyVisibility() // 생성 선 표시 상태 적용
        {
            foreach (GameObject lineObject in lineObjects) // 모든 생성 선 순회
            {
                if (lineObject != null) // 선 오브젝트 존재 확인
                {
                    lineObject.SetActive(guideVisible); // 현재 방 가이드 표시 상태 적용
                }
            }
        }
    }
}
