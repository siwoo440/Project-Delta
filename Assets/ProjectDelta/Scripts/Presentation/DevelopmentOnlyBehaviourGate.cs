using UnityEngine;

namespace ProjectDelta.Presentation
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class DevelopmentOnlyBehaviourGate : MonoBehaviour
    {
        [SerializeField]
        private Behaviour[] targets =
            new Behaviour[0];

        private void Awake()
        {
#if !UNITY_EDITOR
            if (!Debug.isDebugBuild)
            {
                DisableTargets();
            }
#endif
        }

        private void DisableTargets()
        {
            if (targets == null)
            {
                return;
            }

            for (int index = 0; index < targets.Length; index++)
            {
                Behaviour target = targets[index];

                if (target != null)
                {
                    target.enabled = false;
                }
            }
        }
    }
}
