using UnityEditor;

namespace ProjectDelta.Editor
{
    // 추가 작업: MonsterSprites 폴더에 넣은 일러스트를 자동으로 Sprite 타입으로 가져온다.
    public sealed class MonsterSpriteImporter : AssetPostprocessor
    {
        private const string MonsterSpriteFolder =
            "Assets/ProjectDelta/Resources/MonsterSprites/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(
                    MonsterSpriteFolder,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TextureImporter importer =
                assetImporter as TextureImporter;

            if (importer == null)
            {
                return;
            }

            importer.textureType =
                TextureImporterType.Sprite;

            importer.spriteImportMode =
                SpriteImportMode.Single;

            importer.alphaIsTransparency =
                true;

            importer.mipmapEnabled =
                false;
        }
    }
}
