using UnityEditor;
using UnityEngine;

namespace LostDungeon.EditorTools
{
    /// <summary>
    /// Applies the right import settings to the Kenney isometric sprites automatically,
    /// so 450+ images never have to be set up by hand.
    /// Values were measured from the art: a floor tile is a 256 x 128 diamond whose
    /// centre sits 84 px above the bottom of the 256 x 512 image; character feet sit about 60 px up.
    /// To apply: right-click the Isometric and Characters folders > Reimport.
    /// </summary>
    public class KenneyIsoImportSettings : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Art/kenney_isometric-miniature-dungeon/";
        private const float PixelsPerUnit = 256f; // one tile = 1 unit wide, 0.5 units tall

        private static readonly Vector2 TilePivot = new Vector2(0.5f, 84f / 512f);
        private static readonly Vector2 CharacterPivot = new Vector2(0.5f, 60f / 512f);

        private void OnPreprocessTexture()
        {
            bool isTile = assetPath.StartsWith(Root + "Isometric/");
            bool isCharacter = assetPath.StartsWith(Root + "Characters/");
            if (!isTile && !isCharacter)
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = isTile ? TilePivot : CharacterPivot;
            importer.SetTextureSettings(settings);
        }
    }
}
