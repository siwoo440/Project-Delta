using System; // 예외 기능 사용
using System.Collections.Generic; // 목록·사전 기능 사용
using System.Reflection; // private 직렬화 필드 설정
using ProjectDelta.Data; // RoomDefinition 사용
using ProjectDelta.Domain; // RoomExit·GridPosition 사용
using ProjectDelta.Presentation; // RoomView·RoomExitMarker 사용
using UnityEditor; // Editor 자산 생성 기능 사용
using UnityEngine; // GameObject·Primitive 기능 사용

namespace ProjectDelta.Editor // 에디터 네임스페이스
{
    public static class Day31MultiExitRoomGenerator // 31일차 다중 출구 테스트 자산 생성기
    {
        private const string DataFolder = "Assets/ProjectDelta/Data/Rooms/Day31"; // 테스트 RoomDefinition 저장 위치
        private const string PrefabFolder = "Assets/ProjectDelta/Prefabs/Dungeon/Day31"; // 테스트 프리팹 저장 위치
        private const int RoomSize = 5; // 테스트 방 한 변 칸 수
        private const float CellSize = 2f; // 기존 프로젝트 한 칸 월드 크기
        private const float WallHeight = 2.5f; // 테스트 벽 높이
        private const float WallThickness = 0.2f; // 테스트 벽 두께
        private const float DoorWidth = 1.8f; // 중앙 문 폭
        private const float DoorHeight = 2.2f; // 중앙 문 높이

        private static readonly RoomSpec[] Specs =
        {
            new RoomSpec(
                "NS",
                "ROOM_TEST_NS",
                new RoomExit(new GridPosition(0, 2), CardinalDirection.North),
                new RoomExit(new GridPosition(0, -2), CardinalDirection.South)),
            new RoomSpec(
                "NE",
                "ROOM_TEST_NE",
                new RoomExit(new GridPosition(0, 2), CardinalDirection.North),
                new RoomExit(new GridPosition(2, 0), CardinalDirection.East)),
            new RoomSpec(
                "T",
                "ROOM_TEST_T",
                new RoomExit(new GridPosition(0, 2), CardinalDirection.North),
                new RoomExit(new GridPosition(2, 0), CardinalDirection.East),
                new RoomExit(new GridPosition(-2, 0), CardinalDirection.West)),
            new RoomSpec(
                "CROSS",
                "ROOM_TEST_CROSS",
                new RoomExit(new GridPosition(0, 2), CardinalDirection.North),
                new RoomExit(new GridPosition(2, 0), CardinalDirection.East),
                new RoomExit(new GridPosition(0, -2), CardinalDirection.South),
                new RoomExit(new GridPosition(-2, 0), CardinalDirection.West))
        };

        [MenuItem("Project Delta/Day31/Generate Multi-Exit Test Rooms")] // 31일차 테스트 자산 생성 메뉴
        public static void GenerateMultiExitTestRooms()
        {
            EnsureFolder(DataFolder); // 데이터 폴더 보장
            EnsureFolder(PrefabFolder); // 프리팹 폴더 보장

            for (int i = 0; i < Specs.Length; i++) // 모든 테스트 방 규격 생성
            {
                RoomSpec spec = Specs[i]; // 현재 규격 조회
                RoomDefinition definition = CreateOrUpdateDefinition(spec); // RoomDefinition 생성·갱신
                CreateOrUpdatePrefab(spec, definition); // 대응 RoomView 프리팹 생성·갱신
            }

            AssetDatabase.SaveAssets(); // 생성 자산 저장
            AssetDatabase.Refresh(); // Project 창 갱신

            bool valid = ValidateAll(false); // 생성 결과 검증

            if (valid) // 전체 검증 성공 확인
            {
                EditorUtility.DisplayDialog("Project Delta - Day31", "2·3·4출구 테스트 방 생성과 정렬 검증이 완료되었습니다.", "확인"); // 완료 안내
            }
            else
            {
                EditorUtility.DisplayDialog("Project Delta - Day31", "생성은 완료됐지만 검증 오류가 있습니다. Console을 확인하세요.", "확인"); // 오류 안내
            }
        }

        [MenuItem("Project Delta/Day31/Validate Multi-Exit Test Rooms")] // 수정 후 재검증 메뉴
        public static void ValidateMultiExitTestRooms()
        {
            bool valid = ValidateAll(true); // 현재 자산 검증

            if (valid) // 전체 검증 성공 확인
            {
                EditorUtility.DisplayDialog("Project Delta - Day31", "다중 출구 RoomDefinition과 프리팹 정렬이 정상입니다.", "확인"); // 성공 안내
            }
            else
            {
                EditorUtility.DisplayDialog("Project Delta - Day31", "검증 오류가 있습니다. Console을 확인하세요.", "확인"); // 오류 안내
            }
        }

        private static RoomDefinition CreateOrUpdateDefinition(RoomSpec spec) // 테스트 RoomDefinition 생성·갱신
        {
            string assetPath = GetDefinitionPath(spec); // 데이터 자산 경로 계산
            RoomDefinition definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>(assetPath); // 기존 자산 조회

            if (definition == null) // 아직 자산이 없는지 확인
            {
                UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(assetPath); // 같은 경로 다른 자산 확인

                if (existing != null) // 경로가 이미 사용 중인지 확인
                {
                    AssetDatabase.DeleteAsset(assetPath); // 잘못된 기존 자산 제거
                }

                definition = ScriptableObject.CreateInstance<RoomDefinition>(); // 새 방 정의 생성
                AssetDatabase.CreateAsset(definition, assetPath); // 프로젝트 자산으로 저장
            }

            SetPrivateField(typeof(DefinitionBase), definition, "id", spec.Id); // 방 영구 ID 설정
            SetPrivateField(typeof(RoomDefinition), definition, "width", RoomSize); // 가로 5칸 설정
            SetPrivateField(typeof(RoomDefinition), definition, "height", RoomSize); // 세로 5칸 설정

            List<PassageEntry> passages = new List<PassageEntry>(); // 경계 문 목록 준비

            for (int i = 0; i < spec.Exits.Length; i++) // 규격 출구 전체 변환
            {
                RoomExit exit = spec.Exits[i]; // 현재 출구 조회
                passages.Add(new PassageEntry
                {
                    X = exit.LocalPosition.X,
                    Z = exit.LocalPosition.Z,
                    Direction = exit.Direction,
                    Type = PassageType.Door,
                    IsLocked = false
                });
            }

            SetPrivateField(typeof(RoomDefinition), definition, "passages", passages); // 경계 문 데이터 적용
            EditorUtility.SetDirty(definition); // 변경 저장 대상으로 표시
            return definition; // 완성된 정의 반환
        }

        private static void CreateOrUpdatePrefab(RoomSpec spec, RoomDefinition definition) // 테스트 RoomView 프리팹 생성·갱신
        {
            GameObject root = new GameObject($"Room_Test_{spec.Suffix}"); // 테스트 방 루트 생성

            try
            {
                RoomView roomView = root.AddComponent<RoomView>(); // RoomView 추가하며 RoomPassageController도 자동 추가
                RoomPassageController passageController = roomView.GetComponent<RoomPassageController>(); // 자동 추가된 통로 컨트롤러 조회
                SetPrivateField(typeof(RoomPassageController), passageController, "roomId", spec.Id); // 테스트 방 ID 연결
                SetPrivateField(typeof(RoomPassageController), passageController, "roomDefinition", definition); // 방 정의 연결

                CreateFloor(root.transform); // 바닥 생성
                CreateWalls(root.transform, spec); // 출구 규격에 맞는 외곽 벽 생성

                for (int i = 0; i < spec.Exits.Length; i++) // 출구 마커·문 시각 생성
                {
                    CreateExitMarker(root.transform, spec.Exits[i]); // 현재 출구 생성
                }

                string prefabPath = GetPrefabPath(spec); // 프리팹 저장 경로 계산
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath); // 생성 프리팹 저장 또는 덮어쓰기
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root); // 임시 씬 오브젝트 정리
            }
        }

        private static void CreateFloor(Transform root) // 10x10 테스트 바닥 생성
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube); // 기본 큐브 바닥 생성
            floor.name = "Floor"; // 오브젝트 이름 설정
            floor.transform.SetParent(root, false); // 방 자식으로 연결
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f); // 바닥 높이 설정
            floor.transform.localScale = new Vector3(RoomSize * CellSize, 0.2f, RoomSize * CellSize); // 5x5 칸 크기 적용
            RemoveCollider(floor); // 테스트 표시 전용으로 Collider 제거
        }

        private static void CreateWalls(Transform root, RoomSpec spec) // 네 방향 외곽 벽 생성
        {
            CreateWallSide(root, spec, CardinalDirection.North); // 북쪽 벽 생성
            CreateWallSide(root, spec, CardinalDirection.East); // 동쪽 벽 생성
            CreateWallSide(root, spec, CardinalDirection.South); // 남쪽 벽 생성
            CreateWallSide(root, spec, CardinalDirection.West); // 서쪽 벽 생성
        }

        private static void CreateWallSide(Transform root, RoomSpec spec, CardinalDirection direction) // 한 방향 벽 생성
        {
            bool hasExit = HasExit(spec, direction); // 해당 방향 중앙 출구 존재 확인
            float fullLength = RoomSize * CellSize; // 벽 전체 길이
            float halfRoom = fullLength * 0.5f; // 방 절반 크기

            if (!hasExit) // 출구 없는 완전한 벽인지 확인
            {
                CreateWallCube(root, $"Wall_{direction}", direction, 0f, fullLength, halfRoom); // 전체 벽 하나 생성
                return; // 현재 방향 완료
            }

            float segmentLength = (fullLength - DoorWidth) * 0.5f; // 문 양옆 벽 길이
            float segmentOffset = (DoorWidth * 0.5f) + (segmentLength * 0.5f); // 벽 조각 중심 오프셋
            CreateWallCube(root, $"Wall_{direction}_A", direction, -segmentOffset, segmentLength, halfRoom); // 첫 벽 조각 생성
            CreateWallCube(root, $"Wall_{direction}_B", direction, segmentOffset, segmentLength, halfRoom); // 두 번째 벽 조각 생성
        }

        private static void CreateWallCube(Transform root, string name, CardinalDirection direction, float alongOffset, float length, float halfRoom) // 벽 큐브 생성
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube); // 기본 큐브 생성
            wall.name = name; // 벽 이름 설정
            wall.transform.SetParent(root, false); // 방 자식으로 연결

            if (direction == CardinalDirection.North || direction == CardinalDirection.South) // 북·남 벽 확인
            {
                float z = direction == CardinalDirection.North ? halfRoom : -halfRoom; // Z 경계 위치 계산
                wall.transform.localPosition = new Vector3(alongOffset, WallHeight * 0.5f, z); // 벽 위치 설정
                wall.transform.localScale = new Vector3(length, WallHeight, WallThickness); // X 방향 벽 크기 설정
            }
            else
            {
                float x = direction == CardinalDirection.East ? halfRoom : -halfRoom; // X 경계 위치 계산
                wall.transform.localPosition = new Vector3(x, WallHeight * 0.5f, alongOffset); // 벽 위치 설정
                wall.transform.localScale = new Vector3(WallThickness, WallHeight, length); // Z 방향 벽 크기 설정
            }

            RemoveCollider(wall); // 정렬 확인용 벽 Collider 제거
        }

        private static void CreateExitMarker(Transform root, RoomExit exit) // 출구 마커와 문 시각 생성
        {
            GameObject markerObject = new GameObject($"Exit_{exit.Direction}_{exit.LocalPosition.X}_{exit.LocalPosition.Z}"); // 출구 마커 생성
            markerObject.transform.SetParent(root, false); // 방 자식으로 연결
            markerObject.transform.localPosition = GetDoorWorldPosition(exit); // 실제 방 경계 문 위치로 이동

            RoomExitMarker marker = markerObject.AddComponent<RoomExitMarker>(); // 출구 데이터 마커 추가
            marker.Configure(exit.LocalPosition, exit.Direction); // 좌표·방향 적용

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube); // 문 위치 확인용 큐브 생성
            visual.name = "DoorVisual"; // 문 시각 이름 설정
            visual.transform.SetParent(markerObject.transform, false); // 마커 자식으로 연결
            visual.transform.localPosition = Vector3.zero; // 마커 중심 사용

            if (exit.Direction == CardinalDirection.North || exit.Direction == CardinalDirection.South) // 북·남 문 확인
            {
                visual.transform.localScale = new Vector3(DoorWidth, DoorHeight, 0.08f); // X 방향 문 크기
            }
            else
            {
                visual.transform.localScale = new Vector3(0.08f, DoorHeight, DoorWidth); // Z 방향 문 크기
            }

            RemoveCollider(visual); // 시각 확인용 Collider 제거
        }

        private static Vector3 GetDoorWorldPosition(RoomExit exit) // RoomExit을 프리팹 로컬 월드 위치로 변환
        {
            GridPosition delta = GridMovement.GetDirectionDelta(exit.Direction); // 방 밖 방향 벡터 조회
            float centerX = exit.LocalPosition.X * CellSize; // 경계 칸 중심 X 계산
            float centerZ = exit.LocalPosition.Z * CellSize; // 경계 칸 중심 Z 계산
            float offsetX = delta.X * CellSize * 0.5f; // 칸 경계까지 X 절반 칸 이동
            float offsetZ = delta.Z * CellSize * 0.5f; // 칸 경계까지 Z 절반 칸 이동
            return new Vector3(centerX + offsetX, DoorHeight * 0.5f, centerZ + offsetZ); // 실제 문 중심 위치 반환
        }

        private static bool ValidateAll(bool logSuccess) // 생성된 데이터·프리팹 전체 검증
        {
            bool valid = true; // 전체 검증 결과

            for (int i = 0; i < Specs.Length; i++) // 각 테스트 방 검증
            {
                RoomSpec spec = Specs[i]; // 현재 규격 조회
                RoomDefinition definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>(GetDefinitionPath(spec)); // 정의 조회
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(spec)); // 프리팹 조회

                if (definition == null || prefab == null) // 필수 자산 존재 확인
                {
                    Debug.LogError($"[Day31] {spec.Id} RoomDefinition 또는 프리팹이 없습니다."); // 누락 오류
                    valid = false; // 전체 실패 표시
                    continue; // 다음 규격 검사
                }

                RoomTemplate template = definition.ToRoomTemplate(); // 실제 변환 결과 조회
                RoomExitMarker[] markers = prefab.GetComponentsInChildren<RoomExitMarker>(true); // 프리팹 출구 마커 조회

                if (template.Exits.Count != spec.Exits.Length) // 정의 출구 개수 확인
                {
                    Debug.LogError($"[Day31] {spec.Id} RoomDefinition 출구 수가 예상과 다릅니다. Expected={spec.Exits.Length}, Actual={template.Exits.Count}", definition); // 출구 수 오류
                    valid = false; // 전체 실패 표시
                }

                if (markers.Length != spec.Exits.Length) // 프리팹 마커 개수 확인
                {
                    Debug.LogError($"[Day31] {spec.Id} 프리팹 출구 마커 수가 예상과 다릅니다. Expected={spec.Exits.Length}, Actual={markers.Length}", prefab); // 마커 수 오류
                    valid = false; // 전체 실패 표시
                }

                for (int exitIndex = 0; exitIndex < spec.Exits.Length; exitIndex++) // 규격 출구 하나씩 확인
                {
                    RoomExit expected = spec.Exits[exitIndex]; // 예상 출구
                    bool definitionHasExit = ContainsExit(template.Exits, expected); // 데이터 보유 확인
                    RoomExitMarker marker = FindMarker(markers, expected); // 대응 프리팹 마커 검색

                    if (!definitionHasExit) // RoomDefinition 변환 결과 누락 확인
                    {
                        Debug.LogError($"[Day31] {spec.Id} RoomDefinition에 {expected} 출구가 없습니다.", definition); // 데이터 누락 오류
                        valid = false; // 전체 실패 표시
                    }

                    if (marker == null) // 프리팹 출구 마커 누락 확인
                    {
                        Debug.LogError($"[Day31] {spec.Id} 프리팹에 {expected} 마커가 없습니다.", prefab); // 마커 누락 오류
                        valid = false; // 전체 실패 표시
                        continue; // 위치 검증 생략
                    }

                    Vector3 expectedPosition = GetDoorWorldPosition(expected); // 규격상 문 위치 계산

                    if (Vector3.Distance(marker.transform.localPosition, expectedPosition) > 0.001f) // 마커 실제 위치 일치 확인
                    {
                        Debug.LogError($"[Day31] {spec.Id} {expected} 문 위치가 규격과 다릅니다. Expected={expectedPosition}, Actual={marker.transform.localPosition}", prefab); // 문 위치 오류
                        valid = false; // 전체 실패 표시
                    }
                }
            }

            valid &= ValidateNeighborAlignment(); // 인접 방 문 정렬 규칙 추가 검증

            if (valid && logSuccess) // 성공 로그 요청 확인
            {
                Debug.Log("[Day31] 2·3·4출구 RoomDefinition, RoomExitMarker, 문 정렬 규격 검증 성공."); // 성공 로그 출력
            }

            return valid; // 전체 결과 반환
        }

        private static bool ValidateNeighborAlignment() // 방 한 칸 간격 배치 시 반대편 문 월드 위치 일치 검증
        {
            bool valid = true; // 정렬 결과
            float roomWorldSize = RoomSize * CellSize; // 방 하나 월드 크기

            RoomExit north = new RoomExit(new GridPosition(0, 2), CardinalDirection.North); // 북쪽 중앙 문
            RoomExit south = new RoomExit(new GridPosition(0, -2), CardinalDirection.South); // 남쪽 중앙 문
            RoomExit east = new RoomExit(new GridPosition(2, 0), CardinalDirection.East); // 동쪽 중앙 문
            RoomExit west = new RoomExit(new GridPosition(-2, 0), CardinalDirection.West); // 서쪽 중앙 문

            Vector3 northWorld = GetDoorWorldPosition(north); // 기준 방 북쪽 문 위치
            Vector3 southNeighborWorld = GetDoorWorldPosition(south) + new Vector3(0f, 0f, roomWorldSize); // 북쪽 인접 방 남쪽 문 위치

            if (Vector3.Distance(northWorld, southNeighborWorld) > 0.001f || !north.CanConnectTo(south)) // 북-남 정렬 확인
            {
                Debug.LogError("[Day31] North-South 문 정렬 규격이 일치하지 않습니다."); // 북남 오류
                valid = false; // 실패 표시
            }

            Vector3 eastWorld = GetDoorWorldPosition(east); // 기준 방 동쪽 문 위치
            Vector3 westNeighborWorld = GetDoorWorldPosition(west) + new Vector3(roomWorldSize, 0f, 0f); // 동쪽 인접 방 서쪽 문 위치

            if (Vector3.Distance(eastWorld, westNeighborWorld) > 0.001f || !east.CanConnectTo(west)) // 동-서 정렬 확인
            {
                Debug.LogError("[Day31] East-West 문 정렬 규격이 일치하지 않습니다."); // 동서 오류
                valid = false; // 실패 표시
            }

            return valid; // 정렬 결과 반환
        }

        private static bool HasExit(RoomSpec spec, CardinalDirection direction) // 특정 방향 출구 존재 여부
        {
            for (int i = 0; i < spec.Exits.Length; i++) // 출구 전체 순회
            {
                if (spec.Exits[i].Direction == direction) // 같은 방향 확인
                {
                    return true; // 출구 존재 반환
                }
            }

            return false; // 출구 없음 반환
        }

        private static bool ContainsExit(IReadOnlyList<RoomExit> exits, RoomExit target) // RoomExit 목록 포함 여부
        {
            for (int i = 0; i < exits.Count; i++) // 목록 전체 순회
            {
                if (exits[i] == target) // 동일 출구 확인
                {
                    return true; // 포함 확인 반환
                }
            }

            return false; // 미포함 반환
        }

        private static RoomExitMarker FindMarker(RoomExitMarker[] markers, RoomExit target) // 대응 프리팹 출구 마커 검색
        {
            for (int i = 0; i < markers.Length; i++) // 마커 전체 순회
            {
                if (markers[i].Exit == target) // 동일 출구 마커 확인
                {
                    return markers[i]; // 대응 마커 반환
                }
            }

            return null; // 대응 마커 없음
        }

        private static string GetDefinitionPath(RoomSpec spec) // 테스트 데이터 자산 경로
        {
            return $"{DataFolder}/RoomDefinition_Test_{spec.Suffix}.asset"; // 규격별 데이터 경로 반환
        }

        private static string GetPrefabPath(RoomSpec spec) // 테스트 프리팹 경로
        {
            return $"{PrefabFolder}/Room_Test_{spec.Suffix}.prefab"; // 규격별 프리팹 경로 반환
        }

        private static void EnsureFolder(string folderPath) // 중첩 폴더 생성
        {
            string[] parts = folderPath.Split('/'); // 경로 조각 분리
            string current = parts[0]; // Assets부터 시작

            for (int i = 1; i < parts.Length; i++) // 하위 폴더 순서대로 확인
            {
                string next = $"{current}/{parts[i]}"; // 다음 전체 경로 계산

                if (!AssetDatabase.IsValidFolder(next)) // 폴더 미존재 확인
                {
                    AssetDatabase.CreateFolder(current, parts[i]); // 현재 단계 폴더 생성
                }

                current = next; // 다음 단계로 이동
            }
        }

        private static void RemoveCollider(GameObject target) // Primitive 자동 Collider 제거
        {
            Collider collider = target.GetComponent<Collider>(); // 자동 생성 Collider 조회

            if (collider != null) // Collider 존재 확인
            {
                UnityEngine.Object.DestroyImmediate(collider); // 테스트 표시 전용이므로 제거
            }
        }

        private static void SetPrivateField(Type ownerType, object target, string fieldName, object value) // private 직렬화 필드 설정
        {
            FieldInfo field = ownerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 대상 필드 조회

            if (field == null) // 필드 누락 확인
            {
                throw new InvalidOperationException($"{ownerType.Name}.{fieldName} 필드를 찾지 못했습니다."); // 프로젝트 구조 변경 오류
            }

            field.SetValue(target, value); // 값 적용
        }

        private readonly struct RoomSpec // 테스트 방 한 종류 규격
        {
            public string Suffix { get; } // 자산 이름 접미사
            public string Id { get; } // RoomDefinition ID
            public RoomExit[] Exits { get; } // 경계 출구 목록

            public RoomSpec(string suffix, string id, params RoomExit[] exits) // 규격 생성자
            {
                Suffix = suffix; // 접미사 저장
                Id = id; // ID 저장
                Exits = exits; // 출구 목록 저장
            }
        }
    }
}
