using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectDelta.Tests.PlayMode
{
    // 기획서 10.6절 PlayMode 테스트 항목 "Bootstrap에서 Title 진입".
    public class AppRootBootTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_ReachesTitleScene()
        {
            SceneManager.LoadScene("BootstrapScene");
            yield return null;

            // AppRoot 초기화(로컬라이징·Addressables 비동기 대기 포함)가 끝날 때까지 대기.
            // TODO: 초기화 완료 이벤트가 생기면 고정 대기 대신 그 이벤트를 기다리도록 교체한다.
            yield return new UnityEngine.WaitForSeconds(2f);

            Assert.AreEqual("TitleScene", SceneManager.GetActiveScene().name);
        }
    }
}
