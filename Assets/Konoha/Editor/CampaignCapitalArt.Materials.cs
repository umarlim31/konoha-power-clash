using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Konoha.Editor
{
    // 0.2.1 "Material nyata": surfaces get detail + normal maps so walls, roofs, stone and
    // paving stop looking like flat-coloured toys.
    //
    // Two sources per surface slot, checked in this order:
    //   1. Owner-uploaded photo textures (CC0, e.g. Poly Haven 1k JPG) in
    //      Assets/Konoha/Art/Textures/<Slot>/ : a file whose name contains "diff", "color",
    //      "albedo" or "basecolor" is the colour map, one containing "nor" or "normal" the
    //      normal map (OpenGL / "nor_gl"). See Docs/PANDUAN_TEKSTUR_NYATA.md.
    //   2. Otherwise a procedural fallback generated here (256 px colour + normal).
    // The generator never writes into Assets/Konoha/Art/; it only reads and sets import options.
    internal sealed partial class CampaignCapitalArt
    {
        internal const string TextureRoot = "Assets/Konoha/Art/Textures";

        // Surface slots and their procedural fallbacks.
        internal static readonly string[] SurfaceSlots = { "Tembok", "Genteng", "Batu", "Paving", "Aspal", "Rumput", "Kayu", "Beton" };

        private Material genteng;

        // Called after CreatePalette (and again for Nusantara materials once they exist).
        private void ApplyRealSurfaces()
        {
            genteng = Surface("GentengTanahLiat", new Color(.62f, .30f, .18f), .18f);

            Slot(ivory, "Tembok", new Vector2(3f, 3f), PlasterAlbedo, PlasterHeight, 1.2f, keepTint: true);
            Slot(genteng, "Genteng", new Vector2(5f, 5f), TileAlbedo, TileHeight, 3.5f, keepTint: false);
            Slot(stone, "Batu", new Vector2(2f, 2f), StoneAlbedo, StoneHeight, 3f, keepTint: true);
            Slot(paving, "Paving", new Vector2(44f, 44f), null, PavingHeight, 2.5f, keepTint: true);
            Slot(dark, "Kayu", new Vector2(2f, 2f), WoodAlbedo, WoodHeight, 1.5f, keepTint: true);
        }

        private void ApplyRealNusantaraSurfaces(Material grass)
        {
            Slot(asphalt, "Aspal", new Vector2(44f, 1f), null, AsphaltHeight, 1.5f, keepTint: true);
            Slot(grass, "Rumput", new Vector2(150f, 150f), GrassAlbedo, null, 0f, keepTint: false);
            Slot(concrete, "Beton", new Vector2(1f, 3f), null, PlasterHeight, 1.5f, keepTint: true);
            Slot(timber, "Kayu", new Vector2(1f, 1f), WoodAlbedo, WoodHeight, 1.5f, keepTint: true);
        }

        // --- Slot resolution -----------------------------------------------------------

        private void Slot(Material mat, string slot, Vector2 tiling, Func<int, int, Color> albedo,
            Func<int, int, float> height, float bumpStrength, bool keepTint)
        {
            if (mat == null)
                return;

            if (TryPhotoTextures(slot, out Texture2D photoColor, out Texture2D photoNormal))
            {
                if (photoColor != null)
                {
                    mat.SetTexture("_BaseMap", photoColor);
                    // Photos carry their own colour; a light tint keeps faction/area colours.
                    mat.SetColor("_BaseColor", keepTint ? Color.Lerp(Color.white, mat.GetColor("_BaseColor"), .25f) : Color.white);
                }
                if (photoNormal != null)
                    SetNormal(mat, photoNormal, 1f);
                mat.SetTextureScale("_BaseMap", tiling);
                EditorUtility.SetDirty(mat);
                return;
            }

            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", ColorTexture("Real" + slot, albedo));
                if (!keepTint)
                    mat.SetColor("_BaseColor", Color.white);
            }
            if (height != null && bumpStrength > 0f)
                SetNormal(mat, NormalTexture("Real" + slot + "Normal", height, bumpStrength), 1f);
            mat.SetTextureScale("_BaseMap", tiling);
            EditorUtility.SetDirty(mat);
        }

        private static void SetNormal(Material mat, Texture2D normal, float scale)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", scale);
            mat.EnableKeyword("_NORMALMAP");
        }

        private static bool TryPhotoTextures(string slot, out Texture2D color, out Texture2D normal)
        {
            color = null;
            normal = null;
            string folder = TextureRoot + "/" + slot;
            if (!AssetDatabase.IsValidFolder(folder))
                return false;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool isNormal = file.Contains("nor") || file.Contains("normal");
                bool isColor = !isNormal && (file.Contains("diff") || file.Contains("color") || file.Contains("albedo") ||
                    file.Contains("basecolor") || file.Contains("col"));
                if (!isNormal && !isColor)
                    continue;

                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    bool dirty = false;
                    var wanted = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    if (importer.textureType != wanted) { importer.textureType = wanted; dirty = true; }
                    if (importer.maxTextureSize > 1024) { importer.maxTextureSize = 1024; dirty = true; }
                    if (!isNormal && !importer.sRGBTexture) { importer.sRGBTexture = true; dirty = true; }
                    if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; dirty = true; }
                    if (dirty) importer.SaveAndReimport();
                }

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (isNormal && normal == null) normal = texture;
                else if (isColor && color == null) color = texture;
            }
            return color != null || normal != null;
        }

        // Tangent-space normal map from a height function (RGB = XYZ, works for both the
        // RGB and the AG unpack paths URP uses on Android and in the editor).
        private static Texture2D NormalTexture(string name, Func<int, int, float> height, float strength)
        {
            string path = CampaignCapitalMeshes.Folder + "/" + name + ".asset";
            const int size = 256;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
                AssetDatabase.CreateAsset(tex, path);
            }
            tex.name = name; tex.wrapMode = TextureWrapMode.Repeat; tex.filterMode = FilterMode.Trilinear; tex.anisoLevel = 4;
            var heights = new float[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    heights[y * size + x] = height(x, y);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float left = heights[y * size + (x + size - 1) % size], right = heights[y * size + (x + 1) % size];
                    float down = heights[((y + size - 1) % size) * size + x], up = heights[((y + 1) % size) * size + x];
                    var n = new Vector3((left - right) * strength, (down - up) * strength, 1f).normalized;
                    pixels[y * size + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1f);
                }
            tex.SetPixels(pixels); tex.Apply(true, false); EditorUtility.SetDirty(tex);
            return tex;
        }

        // --- Procedural surfaces (fallbacks) -----------------------------------------------

        private static float Noise(int x, int y, float scale, float seed = 0f) =>
            Mathf.PerlinNoise(x * scale + seed, y * scale + seed * 1.7f);

        // Painted plaster (tembok cat): soft trowel waves, fine sand grain, water stains.
        private static Color PlasterAlbedo(int x, int y)
        {
            float grain = Noise(x, y, .45f) * .06f;
            float stain = Mathf.Clamp01((Noise(x, y, .018f, 11f) - .58f) * 3f) * .16f;
            float streak = Mathf.Clamp01((Noise(x / 6, y, .05f, 3f) - .62f) * 4f) * .08f;
            float v = .95f + grain - stain - streak;
            return new Color(v, v * .985f, v * .96f);
        }

        private static float PlasterHeight(int x, int y) =>
            Noise(x, y, .5f) * .35f + Noise(x, y, .06f, 5f) * .5f;

        // Genteng tanah liat: staggered rows of curved clay tiles with per-tile colour.
        private const int TileW = 32, TileH = 40;

        private static Color TileAlbedo(int x, int y)
        {
            int row = y / TileH, shift = (row & 1) * (TileW / 2);
            int col = (x + shift) / TileW;
            int hash = (col * 73 + row * 151) & 255;
            float tone = (hash % 7) * .025f;
            float local = TileHeight(x, y);
            float soot = Mathf.Clamp01((Noise(x, y, .03f, 7f) - .55f) * 2f) * .15f;
            return new Color(.66f + tone - soot, .31f + tone * .6f - soot * .8f, .18f + tone * .3f - soot * .6f) *
                (.78f + local * .3f);
        }

        private static float TileHeight(int x, int y)
        {
            int row = y / TileH, shift = (row & 1) * (TileW / 2);
            float u = ((x + shift) % TileW) / (float)TileW;
            float v = (y % TileH) / (float)TileH;
            float curve = Mathf.Sin(u * Mathf.PI);                 // Rounded across the tile.
            float lip = v < .12f ? v / .12f : 1f;                     // Shadowed lower lip.
            return curve * .8f * lip + Noise(x, y, .4f) * .08f;
        }

        // Andesite blocks with mortar joints.
        private static Color StoneAlbedo(int x, int y)
        {
            int row = y / 32, shift = (row & 1) * 32;
            int col = (x + shift) / 64;
            bool joint = y % 32 < 2 || (x + shift) % 64 < 2;
            float tone = (((col * 37 + row * 91) & 255) % 5) * .035f;
            float v = joint ? .55f : .80f + tone + Noise(x, y, .35f) * .1f;
            return new Color(v, v, v * .98f);
        }

        private static float StoneHeight(int x, int y)
        {
            int row = y / 32, shift = (row & 1) * 32;
            bool joint = y % 32 < 2 || (x + shift) % 64 < 2;
            return joint ? 0f : .7f + Noise(x, y, .3f) * .3f;
        }

        // Same basketweave as the paving colour texture (CampaignCapitalArt.Texture "Paving").
        private static float PavingHeight(int x, int y)
        {
            int cx = x / 64, cy = y / 64, lx = x % 64, ly = y % 64;
            bool horizontal = ((cx + cy) & 1) == 0;
            int along = horizontal ? lx : ly, across = horizontal ? ly % 32 : lx % 32;
            bool joint = along < 2 || across < 2;
            return joint ? 0f : .8f + Noise(x, y, .5f) * .15f;
        }

        private static float AsphaltHeight(int x, int y) => Noise(x, y, .9f) * .6f + Noise(x, y, .2f, 4f) * .4f;

        // Tropical lawn: mixed greens, dry patches, blade speckle (no normal map needed).
        private static Color GrassAlbedo(int x, int y)
        {
            float patch = Noise(x, y, .03f, 21f);
            float blade = Noise(x, y, .9f, 2f);
            float dry = Mathf.Clamp01((Noise(x, y, .02f, 9f) - .62f) * 3f);
            var lush = new Color(.20f + blade * .08f, .36f + blade * .12f + patch * .05f, .12f + blade * .04f);
            var straw = new Color(.45f, .42f, .22f);
            return Color.Lerp(lush, straw, dry * .5f);
        }

        private static Color WoodAlbedo(int x, int y)
        {
            float grain = Mathf.Sin(x * .35f + Noise(x, y, .04f) * 6f) * .5f + .5f;
            float v = .78f + grain * .12f + Noise(x, y, .3f) * .06f;
            return new Color(v, v * .96f, v * .92f);
        }

        private static float WoodHeight(int x, int y) => Mathf.Sin(x * .35f + Noise(x, y, .04f) * 6f) * .3f;

        // --- Colour grading --------------------------------------------------------------

        // Mild, mobile-friendly grading: neutral tonemapping, a little contrast and warmth,
        // light vignette. Replaces the flat "toy" look without costly effects (no SSAO/bloom).
        internal static void ConfigurePostProcessing(Camera view)
        {
            string path = CampaignCapitalMeshes.Folder + "/NusantaraGrading.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null)
                AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(14f);
            color.saturation.Override(8f);
            color.postExposure.Override(.15f);
            color.colorFilter.Override(new Color(1f, .97f, .92f));
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.22f);
            vignette.smoothness.Override(.45f);
            foreach (var component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);

            var volume = new GameObject("NusantaraColourGrading").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;

            if (view != null)
                view.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }
    }
}
