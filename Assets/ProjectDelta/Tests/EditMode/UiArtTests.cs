using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Runtime; // 공통 화면 구성
using UnityEngine; // 화면 객체 도구
using UnityEngine.UI; // 화면 이미지 형식
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class UiArtTests // 화면 그림 적용 검증
    { // 검증 클래스 시작
        [Test] // 공통 버튼 그림 검증
        public void CommonButton_HasNonBlockingArtwork() // 버튼 그림과 입력 확인
        { // 검증 시작
            var root = new GameObject("TestRoot", typeof(RectTransform)); // 독립 상위 화면
            try // 시험 객체 정리 보장
            { // 검증 본문 시작
                var button = UiFactory.CreateButton(root.transform, "TestButton", "선택", Vector2.zero); // 공통 버튼 생성
                var artwork = button.transform.Find("UiArtwork"); // 장식 이미지 조회
                Assert.That(artwork, Is.Not.Null); // 버튼 장식 존재 확인
                Assert.That(artwork.GetComponent<Image>().sprite, Is.Not.Null); // 실제 그림 연결 확인
                Assert.That(artwork.GetComponent<Image>().raycastTarget, Is.False); // 버튼 입력 통과 확인
                Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("선택")); // 버튼 글자 보존 확인
            } // 검증 본문 끝
            finally // 시험 객체 정리
            { // 정리 시작
                Object.DestroyImmediate(root); // 시험 화면 제거
            } // 정리 끝
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
