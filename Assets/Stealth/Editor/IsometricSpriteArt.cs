using System.IO;
using UnityEditor;
using UnityEngine;

// Small, editable pixel-art authoring helper. Its output is ordinary imported PNG sprites.
// Projection matches the fixed camera; gameplay never calls this code.
public static class IsometricSpriteArt
{
    public static readonly Quaternion ViewRotation = Quaternion.Euler(35.264f, -30f, 0f);
    private const float PixelsPerUnit = 48f;
    private const string Folder = "Assets/Stealth/Art/Scenery";
    private static readonly Color32 Ink = new Color32(20, 28, 44, 255);

    public static Sprite Box(string assetName, Vector3 size, Color color, bool detailed, string surfaceStyle = "panel")
    {
        Directory.CreateDirectory(Folder);
        Vector3 half = size * 0.5f;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector2 point = Project(Vector3.Scale(half, new Vector3(x, y, z)));
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        min -= Vector2.one * 2f;
        max += Vector2.one * 2f;
        var texture = new Texture2D(Mathf.CeilToInt(max.x - min.x), Mathf.CeilToInt(max.y - min.y), TextureFormat.RGBA32, false);
        texture.SetPixels32(new Color32[texture.width * texture.height]);
        Vector2 origin = -min;
        Vector3 a = new Vector3(-half.x, -half.y, -half.z);
        Vector3 b = new Vector3(half.x, -half.y, -half.z);
        Vector3 c = new Vector3(half.x, half.y, -half.z);
        Vector3 d = new Vector3(-half.x, half.y, -half.z);
        Vector3 e = new Vector3(half.x, -half.y, half.z);
        Vector3 f = new Vector3(half.x, half.y, half.z);
        Vector3 g = new Vector3(-half.x, half.y, half.z);
        Face(texture, origin, new[] { a, b, c, d }, color);
        Face(texture, origin, new[] { b, e, f, c }, color * new Color(0.7f, 0.75f, 0.85f, 1f));
        Face(texture, origin, new[] { d, c, f, g }, Color.Lerp(color, new Color(0.8f, 0.88f, 0.82f), 0.27f));

        if (detailed && size.y > 0.5f)
        {
            // Inset panels and seams on the front, all in the same authored projection.
            float inset = Mathf.Min(0.12f, size.x * 0.15f);
            Vector3 lower = a + new Vector3(inset, 0.15f, 0f);
            Vector3 upper = c - new Vector3(inset, 0.15f, 0f);
            Face(texture, origin, new[] { lower, new Vector3(upper.x, lower.y, lower.z), upper,
                new Vector3(lower.x, upper.y, lower.z) }, Color.Lerp(color, Ink, 0.16f));
            for (float y = lower.y + 0.3f; y < upper.y; y += 0.4f)
            {
                Line(texture, Project(new Vector3(lower.x, y, lower.z)) + origin,
                    Project(new Vector3(upper.x, y, lower.z)) + origin, Ink);
                if (surfaceStyle == "brick")
                    for (float x = lower.x + (Mathf.RoundToInt(y / 0.4f) % 2) * 0.35f; x < upper.x; x += 0.7f)
                        Line(texture, Project(new Vector3(x, y, lower.z)) + origin,
                            Project(new Vector3(x, Mathf.Min(y + 0.4f, upper.y), lower.z)) + origin, Ink);
            }
            if (surfaceStyle == "crate")
            {
                Line(texture, Project(lower) + origin, Project(upper) + origin, Ink);
                Line(texture, Project(new Vector3(lower.x, upper.y, lower.z)) + origin,
                    Project(new Vector3(upper.x, lower.y, lower.z)) + origin, Ink);
            }
            if (surfaceStyle == "pillar")
                for (float x = lower.x + 0.16f; x < upper.x; x += 0.2f)
                    Line(texture, Project(new Vector3(x, lower.y, lower.z)) + origin,
                        Project(new Vector3(x, upper.y, lower.z)) + origin, Ink);
            Vector3 label = new Vector3(0f, half.y * 0.3f, -half.z);
            Face(texture, origin, new[] { label + new Vector3(-0.22f, -0.1f, 0),
                label + new Vector3(0.22f, -0.1f, 0), label + new Vector3(0.22f, 0.1f, 0),
                label + new Vector3(-0.22f, 0.1f, 0) }, new Color(0.86f, 0.72f, 0.42f));
        }
        if (size.x > 20f && size.z > 2f)
        {
            // Tile lines belong to the flat ground illustration, not extra geometry.
            Color32 grout = new Color32(57, 67, 84, 255);
            for (float x = -half.x; x <= half.x; x += 2f)
                Line(texture, Project(new Vector3(x, half.y, -half.z)) + origin,
                    Project(new Vector3(x, half.y, half.z)) + origin, grout);
            for (float z = -half.z; z <= half.z; z += 1f)
                Line(texture, Project(new Vector3(-half.x, half.y, z)) + origin,
                    Project(new Vector3(half.x, half.y, z)) + origin, grout);
        }
        string path = Folder + "/" + assetName + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Vector2 pivot = new Vector2(origin.x / texture.width, origin.y / texture.height);
        Object.DestroyImmediate(texture);
        return Import(path, pivot, PixelsPerUnit);
    }

    public static Sprite Guard()
    {
        Directory.CreateDirectory(Folder);
        var texture = new Texture2D(32, 48, TextureFormat.RGBA32, false);
        texture.SetPixels32(new Color32[32 * 48]);
        // An intentionally flat lookout silhouette; no model or external character package.
        Fill(texture, 7, 1, 14, 4, Ink);
        Fill(texture, 19, 1, 26, 4, Ink);
        Fill(texture, 10, 4, 14, 14, new Color32(45, 50, 67, 255));
        Fill(texture, 19, 4, 23, 14, new Color32(45, 50, 67, 255));
        Fill(texture, 7, 14, 26, 30, Ink);
        Fill(texture, 9, 16, 24, 29, new Color32(155, 68, 71, 255));
        Fill(texture, 12, 30, 22, 39, Ink);
        Fill(texture, 14, 31, 22, 37, new Color32(214, 176, 136, 255));
        Fill(texture, 10, 39, 24, 42, Ink);
        Fill(texture, 8, 37, 27, 39, Ink);
        Fill(texture, 20, 33, 22, 34, new Color32(255, 233, 165, 255));
        Fill(texture, 20, 23, 23, 25, new Color32(255, 203, 108, 255));
        string path = Folder + "/Lookout.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        return Import(path, new Vector2(0.5f, 0f), 28f);
    }

    private static Sprite Import(string path, Vector2 pivot, float pixelsPerUnit)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Vector2 Project(Vector3 point)
    {
        Vector3 view = Quaternion.Inverse(ViewRotation) * point;
        return new Vector2(view.x, view.y) * PixelsPerUnit;
    }

    private static void Face(Texture2D texture, Vector2 origin, Vector3[] points, Color color)
    {
        var polygon = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++) polygon[i] = Project(points[i]) + origin;
        float minY = polygon[0].y, maxY = polygon[0].y;
        foreach (Vector2 point in polygon) { minY = Mathf.Min(minY, point.y); maxY = Mathf.Max(maxY, point.y); }
        for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
        {
            float left = float.MaxValue, right = float.MinValue;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i], b = polygon[(i + 1) % polygon.Length];
                if ((a.y <= y && b.y > y) || (b.y <= y && a.y > y))
                {
                    float x = a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x);
                    left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                }
            }
            if (left <= right)
                for (int x = Mathf.CeilToInt(left); x <= Mathf.FloorToInt(right); x++) Pixel(texture, x, y, color);
        }
        for (int i = 0; i < polygon.Length; i++) Line(texture, polygon[i], polygon[(i + 1) % polygon.Length], Ink);
    }

    private static void Fill(Texture2D texture, int x1, int y1, int x2, int y2, Color32 color)
    { for (int y = y1; y <= y2; y++) for (int x = x1; x <= x2; x++) Pixel(texture, x, y, color); }

    private static void Line(Texture2D texture, Vector2 from, Vector2 to, Color32 color)
    {
        int steps = Mathf.CeilToInt(Vector2.Distance(from, to));
        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, steps == 0 ? 0f : (float)i / steps);
            Pixel(texture, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), color);
        }
    }

    private static void Pixel(Texture2D texture, int x, int y, Color32 color)
    { if (x >= 0 && x < texture.width && y >= 0 && y < texture.height) texture.SetPixel(x, y, color); }
}
