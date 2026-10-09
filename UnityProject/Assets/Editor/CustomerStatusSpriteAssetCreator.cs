using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.TextCore;

internal static class CustomerStatusSpriteAssetCreator
{
    private const string Folder = "Assets/Resources/Sprite Assets";
    private const string TexturePath = Folder + "/CustomerStatusIcons.png";
    private const string AssetPath = Folder + "/CustomerStatusIcons.asset";

    [MenuItem("AI Business Tycoon/Create Customer Status Sprite Asset")]
    private static void CreateAsset()
    {
        EnsureFolders();
        const int size = 64;
        const int count = 4;
        var texture = new Texture2D(size * count, size, TextureFormat.RGBA32, false);
        var pixels = new Color[texture.width * texture.height];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        DrawCircle(pixels, texture.width, 32, 32, 24, new Color(1f, .75f, .1f));
        DrawDot(pixels, texture.width, 20, 28, 5, Color.black);
        DrawDot(pixels, texture.width, 44, 28, 5, Color.black);
        DrawDot(pixels, texture.width, 32, 42, 5, Color.black);
        DrawCircle(pixels, texture.width, 96, 32, 24, new Color(.95f, .15f, .15f));
        DrawLine(pixels, texture.width, 84, 20, 108, 44, Color.white, 6);
        DrawLine(pixels, texture.width, 108, 20, 84, 44, Color.white, 6);
        DrawCircle(pixels, texture.width, 160, 32, 24, new Color(.2f, .65f, 1f));
        DrawLine(pixels, texture.width, 150, 32, 158, 40, Color.white, 6);
        DrawLine(pixels, texture.width, 158, 40, 174, 22, Color.white, 6);
        DrawCircle(pixels, texture.width, 224, 32, 24, new Color(.2f, .8f, .3f));
        DrawLine(pixels, texture.width, 212, 32, 220, 40, Color.white, 6);
        DrawLine(pixels, texture.width, 220, 40, 238, 20, Color.white, 6);

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null)
        {
            Debug.LogError("Could not create a Sprite Editor data provider for the customer status icon sheet.");
            return;
        }
        provider.InitSpriteEditorDataProvider();
        var names = new[] { "waiting", "error", "shopping", "success" };
        var rects = new SpriteRect[count];
        for (var i = 0; i < count; i++)
        {
            rects[i] = new SpriteRect
            {
                name = names[i],
                rect = new Rect(i * size, 0, size, size),
                pivot = new Vector2(.5f, .5f),
                alignment = SpriteAlignment.Center,
                spriteID = GUID.Generate()
            };
        }
        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();

        var sprites = new List<Sprite>();
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(TexturePath))
            if (obj is Sprite sprite) sprites.Add(sprite);
        if (sprites.Count != count)
        {
            Debug.LogError("Customer status sprites were not imported correctly.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetPath) != null)
            AssetDatabase.DeleteAsset(AssetPath);
        var spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        spriteAsset.name = "CustomerStatusIcons";
        spriteAsset.spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var glyphs = new List<TMP_SpriteGlyph>();
        var characters = new List<TMP_SpriteCharacter>();
        for (var i = 0; i < count; i++)
        {
            var glyph = new TMP_SpriteGlyph((uint)i, new GlyphMetrics(size, size, 0, size, size), new GlyphRect(sprites[i].rect), 1f, 0, sprites[i]);
            glyphs.Add(glyph);
            var character = new TMP_SpriteCharacter((uint)i, spriteAsset, glyph) { name = names[i] };
            characters.Add(character);
        }
        spriteAsset.spriteGlyphTable.Clear();
        spriteAsset.spriteGlyphTable.AddRange(glyphs);
        spriteAsset.spriteCharacterTable.Clear();
        spriteAsset.spriteCharacterTable.AddRange(characters);
        AssetDatabase.CreateAsset(spriteAsset, AssetPath);

        var spriteShader = Shader.Find("TextMeshPro/Sprite");
        if (spriteShader == null)
        {
            Debug.LogError("Could not find the TextMeshPro/Sprite shader.");
            return;
        }

        var spriteMaterial = new Material(spriteShader)
        {
            name = "CustomerStatusIcons Material",
            mainTexture = spriteAsset.spriteSheet
        };
        AssetDatabase.AddObjectToAsset(spriteMaterial, spriteAsset);
        spriteAsset.material = spriteMaterial;
        spriteAsset.UpdateLookupTables();
        AssetDatabase.SaveAssets();
        Debug.Log("Created CustomerStatusIcons TMP Sprite Asset.");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "Sprite Assets");
    }

    private static void DrawDot(Color[] p, int w, int cx, int cy, int r, Color c)
    {
        for (var y = -r; y <= r; y++) for (var x = -r; x <= r; x++)
            if (x * x + y * y <= r * r) p[(cy + y) * w + cx + x] = c;
    }

    private static void DrawCircle(Color[] p, int w, int x, int y, int r, Color c) => DrawDot(p, w, x, y, r, c);

    private static void DrawLine(Color[] p, int w, int x1, int y1, int x2, int y2, Color c, int thickness)
    {
        var steps = Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1));
        for (var i = 0; i <= steps; i++)
        {
            var t = steps == 0 ? 0 : (float)i / steps;
            DrawDot(p, w, Mathf.RoundToInt(Mathf.Lerp(x1, x2, t)), Mathf.RoundToInt(Mathf.Lerp(y1, y2, t)), thickness / 2, c);
        }
    }
}