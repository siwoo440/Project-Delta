using System.Collections.Generic; // 목록 기능 사용
using System.Reflection; // 테스트용 private 필드 설정
using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Data; // RoomDefinition 사용
using ProjectDelta.Domain; // RoomExit·RoomTemplate 사용
using UnityEngine; // ScriptableObject 정리 기능 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class RoomExitTests // 30일차 출구 좌표 보존 테스트
    {
        [Test]
        public void RoomExit_PreservesLocalPositionAndDirection() // RoomExit이 좌표와 방향을 그대로 보관하는지 확인
        {
            RoomExit exit = new RoomExit(new GridPosition(1, 2), CardinalDirection.North); // 북쪽 출구 생성

            Assert.AreEqual(new GridPosition(1, 2), exit.LocalPosition); // 좌표 보존 확인
            Assert.AreEqual(CardinalDirection.North, exit.Direction); // 방향 보존 확인
        }

        [Test]
        public void RoomExit_CanConnectTo_UsesOppositeDirectionAndAlignmentAxis() // 반대 방향과 축 위치로 연결 가능 여부를 판정하는지 확인
        {
            RoomExit north = new RoomExit(new GridPosition(1, 2), CardinalDirection.North); // 북쪽 X=1 출구
            RoomExit alignedSouth = new RoomExit(new GridPosition(1, -2), CardinalDirection.South); // 남쪽 X=1 출구
            RoomExit shiftedSouth = new RoomExit(new GridPosition(-1, -2), CardinalDirection.South); // 남쪽 X=-1 출구

            Assert.IsTrue(north.CanConnectTo(alignedSouth)); // 같은 X의 반대 방향은 연결 가능
            Assert.IsFalse(north.CanConnectTo(shiftedSouth)); // X가 다르면 연결 불가
        }

        [Test]
        public void RoomDefinition_ToRoomTemplate_PreservesBoundaryExitCoordinates() // RoomDefinition 변환 시 경계 출구 좌표가 유지되는지 확인
        {
            RoomDefinition definition = ScriptableObject.CreateInstance<RoomDefinition>(); // 테스트용 방 정의 생성

            try
            {
                SetPrivateField(definition, "width", 5); // 5칸 너비 설정
                SetPrivateField(definition, "height", 5); // 5칸 높이 설정
                SetPrivateField(definition, "passages", new List<PassageEntry> // 경계 문과 내부 문 구성
                {
                    new PassageEntry
                    {
                        X = 1,
                        Z = 2,
                        Direction = CardinalDirection.North,
                        Type = PassageType.Door,
                        IsLocked = false
                    },
                    new PassageEntry
                    {
                        X = 0,
                        Z = 0,
                        Direction = CardinalDirection.North,
                        Type = PassageType.Door,
                        IsLocked = false
                    }
                });

                RoomTemplate template = definition.ToRoomTemplate(); // 던전 생성용 템플릿으로 변환

                Assert.AreEqual(1, template.Exits.Count); // 내부 문은 제외되고 경계 문만 남는지 확인
                Assert.AreEqual(new GridPosition(1, 2), template.Exits[0].LocalPosition); // 경계 문 좌표 보존 확인
                Assert.AreEqual(CardinalDirection.North, template.Exits[0].Direction); // 경계 문 방향 보존 확인
            }
            finally
            {
                Object.DestroyImmediate(definition); // 테스트용 ScriptableObject 정리
            }
        }

        private static void SetPrivateField<T>(RoomDefinition target, string fieldName, T value) // 테스트용 private 필드 값 설정
        {
            FieldInfo field = typeof(RoomDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 대상 필드 조회
            Assert.IsNotNull(field, $"RoomDefinition.{fieldName} 필드를 찾지 못했습니다."); // 필드 존재 확인
            field.SetValue(target, value); // 테스트 값 적용
        }
    }
}
