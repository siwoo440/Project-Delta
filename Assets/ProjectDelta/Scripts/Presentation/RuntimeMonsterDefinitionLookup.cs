using System.Collections.Generic;
using ProjectDelta.Data;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 122일차: RuntimeItemDefinitionLookup(114일차)과 같은 패턴 - 런타임에 이미 로드된
    // MonsterDefinition을 ID/에셋명/표시명으로 조회한다. 전투 화면이 지금까지 참가자 이름을
    // DefinitionId(영문 ID)로 그대로 보여주던 걸 실제 한글 DisplayName으로 바꾸는 데 쓴다.
    public static class RuntimeMonsterDefinitionLookup
    {
        private static readonly Dictionary<string, MonsterDefinition> lookup =
            new Dictionary<string, MonsterDefinition>();

        private static bool cacheInitialized;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            lookup.Clear();

            cacheInitialized =
                false;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InvalidateAfterSceneLoad()
        {
            cacheInitialized =
                false;
        }

        public static bool TryFind(
            string monsterKey,
            out MonsterDefinition definition)
        {
            definition =
                null;

            if (string.IsNullOrEmpty(
                    monsterKey))
            {
                return false;
            }

            EnsureCache();

            if (!lookup.TryGetValue(
                    monsterKey,
                    out definition)
                || definition == null)
            {
                definition =
                    null;

                return false;
            }

            return true;
        }

        // 인스턴스 ID("MON_SLIME#1")나 슬롯 접미사가 붙은 값도 받아 대표 정의 ID로 풀어본다.
        public static string ResolveDisplayName(
            string monsterKey)
        {
            string canonicalKey =
                StripInstanceSuffix(
                    monsterKey);

            return TryFind(
                    canonicalKey,
                    out MonsterDefinition definition)
                && !string.IsNullOrEmpty(
                    definition.DisplayName)
                    ? definition.DisplayName
                    : monsterKey;
        }

        private static string StripInstanceSuffix(
            string monsterKey)
        {
            if (string.IsNullOrEmpty(
                    monsterKey))
            {
                return monsterKey;
            }

            int hashIndex =
                monsterKey.IndexOf(
                    '#');

            return hashIndex >= 0
                ? monsterKey.Substring(
                    0,
                    hashIndex)
                : monsterKey;
        }

        private static void EnsureCache()
        {
            if (cacheInitialized)
            {
                return;
            }

            RebuildCache();
        }

        private static void RebuildCache()
        {
            lookup.Clear();

            MonsterDefinition[] definitions =
                Resources.FindObjectsOfTypeAll<MonsterDefinition>();

            for (int index = 0;
                 index < definitions.Length;
                 index++)
            {
                MonsterDefinition definition =
                    definitions[index];

                if (definition == null)
                {
                    continue;
                }

                AddLookupKey(
                    definition.Id,
                    definition);

                AddLookupKey(
                    definition.name,
                    definition);

                AddLookupKey(
                    definition.DisplayName,
                    definition);
            }

            cacheInitialized =
                true;
        }

        private static void AddLookupKey(
            string key,
            MonsterDefinition definition)
        {
            if (string.IsNullOrEmpty(
                    key)
                || definition == null)
            {
                return;
            }

            lookup[key] =
                definition;
        }
    }
}
