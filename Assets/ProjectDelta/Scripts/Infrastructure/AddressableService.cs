using ProjectDelta.Application;
using System.Collections;
using UnityEngine.AddressableAssets;

namespace ProjectDelta.Infrastructure
{
    public sealed class AddressableService : IAddressableService
    {
        public IEnumerator InitializeRoutine()
        {
            yield return Addressables.InitializeAsync();
        }
    }
}
