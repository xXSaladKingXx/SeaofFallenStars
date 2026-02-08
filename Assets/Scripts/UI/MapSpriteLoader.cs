using System.IO;
using UnityEngine;

public static class MapSpriteLoader
{
    /// <summary>
    /// Prototype-friendly synchronous loader.
    /// Supports:
    /// - Absolute/relative disk file paths (png/jpg)
    /// - Resources paths (Sprite) if disk path not found
    /// Does NOT do http/https (by design for now).
    /// </summary>
    public static bool TryLoadSprite(string urlOrPath, out Sprite sprite, out Texture2D texture, out string error)
    {
        sprite = null;
        texture = null;
        error = null;

        if (string.IsNullOrWhiteSpace(urlOrPath))
        {
            error = "Empty path.";
            return false;
        }

        // Disk path (preferred for your current prototype)
        if (File.Exists(urlOrPath))
        {
            byte[] bytes = File.ReadAllBytes(urlOrPath);

            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Object.Destroy(texture);
                texture = null;
                error = "Texture2D.LoadImage failed.";
                return false;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            return true;
        }

        // Resources fallback (if you later move maps into Resources)
        // Accepts "Maps/Seawatch" (no extension)
        string resourcesKey = StripExtension(urlOrPath);
        Sprite resSprite = Resources.Load<Sprite>(resourcesKey);
        if (resSprite != null)
        {
            sprite = resSprite;
            texture = resSprite.texture;
            return true;
        }

        error = $"File not found on disk and not found in Resources: '{urlOrPath}'";
        return false;
    }

    private static string StripExtension(string p)
    {
        if (string.IsNullOrWhiteSpace(p))
            return p;

        // If it looks like a path with an extension, remove extension.
        // For Windows paths, keep directories.
        string ext = Path.GetExtension(p);
        if (string.IsNullOrWhiteSpace(ext))
            return p;

        return p.Substring(0, p.Length - ext.Length);
    }
}
