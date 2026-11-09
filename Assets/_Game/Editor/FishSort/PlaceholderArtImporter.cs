using UnityEditor;
using UnityEngine;

// Import settings for the generated placeholder art only (Assets/_Game/Art/Placeholder).
// Real art should be configured by hand in its own folder.
public sealed class PlaceholderArtImporter : AssetPostprocessor
{
    private const string PLACEHOLDER_FOLDER = "Assets/_Game/Resources/Art/Placeholder/";
    private const float DEFAULT_PPU = 100f;

    private struct Rule
    {
        public string prefix;
        public float pixelsPerUnit;
        public float border;
        public Rule(string prefix, float pixelsPerUnit, float border)
        {
            this.prefix = prefix; this.pixelsPerUnit = pixelsPerUnit; this.border = border;
        }
    }

    // Fish / bubble / ice are authored so 1 world unit = sprite width (fish) or diameter (bubble).
    private static readonly Rule[] Rules =
    {
        new Rule("fish_", 256f, 0f),
        new Rule("bubble", 256f, 0f),
        new Rule("ice", 256f, 0f),
        new Rule("glow", 128f, 0f),
        new Rule("tank_", 200f, 0f),
        new Rule("shelf", 200f, 0f),
        new Rule("tray_", 200f, 0f),
        new Rule("counter_bg", 200f, 0f),
        new Rule("hud_", 200f, 0f),
        new Rule("coin", 100f, 0f),
        new Rule("icon_", 100f, 0f),
        new Rule("badge", 200f, 0f),
        new Rule("square", 4f, 0f),
        new Rule("background", 100f, 0f),
        new Rule("ui_vignette", 100f, 60f),
        new Rule("ui_round", 100f, 44f),
        new Rule("ui_", 100f, 0f)
    };

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PLACEHOLDER_FOLDER))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = DEFAULT_PPU;

        string fileName = System.IO.Path.GetFileName(assetPath);
        for (int i = 0; i < Rules.Length; i++)
        {
            if (fileName.StartsWith(Rules[i].prefix))
            {
                importer.spritePixelsPerUnit = Rules[i].pixelsPerUnit;
                float border = Rules[i].border;
                importer.spriteBorder = new Vector4(border, border, border, border);
                break;
            }
        }
    }
}
