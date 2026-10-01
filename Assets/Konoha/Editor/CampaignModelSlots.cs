using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Konoha.Editor
{
    // 0.3.1: owner-supplied 3D models for buildings, trees and street props of Jalur Takhta.
    //
    // Upload Assets/Konoha/Art/Models/<Slot>/<Slot>.fbx (or .obj, or the only model file in
    // that folder) with its textures. Every place where the generator builds that slot then
    // shows the model instead of the code-built shape: the model is scaled to fit the slot's
    // box, stood on the ground and centred on the spot. Colliders of the code-built shape stay
    // exactly where they were (route and collision never change); only its visible parts go.
    // An optional atur.txt in the folder tunes the fit: skala=1.2, putar=90, naik=0.1,
    // ukuran=asli. Anything wrong with a model (missing import, too many triangles, an
    // exception) is logged and the code-built shape stays: generation never fails because of art.
    public static class CampaignModelSlots
    {
        public const string ModelsRoot = "Assets/Konoha/Art/Models";
        public const string MaterialFolder = SpikeProject.Generated + "/Models";
        public const string SettingsFile = "atur.txt";

        public sealed class Slot
        {
            public string name;
            public string label;
            // Fit box in the slot's frame (x across, y up, z along the facing).
            public Vector3 box;
            // Rotation that turns a model whose front faces +Z (Unity convention) to the slot's front.
            public float frontYaw;
            // Keep the model's own origin on the spot (pole-and-arm shapes) instead of its centre.
            public bool useOrigin;
            public bool castShadows = true;
            // Occluder name prefix, so the camera fades the model like the code-built shape.
            public string occluder;
            // Height (fraction of the fitted model) where the code-built name sign (shop name,
            // gang name, warung name) is kept on the model's front; 0 drops the signs.
            public float signHeight;
            public int maxTriangles;
            public int instances;
        }

        public struct Settings
        {
            public float scale, yaw, lift;
            public bool originalSize;
            public static Settings Default => new Settings { scale = 1f };
        }

        public static readonly Slot[] Slots =
        {
            new Slot { name = "Lentera", label = "lentera taman", box = new Vector3(.5f, 2.3f, .5f), castShadows = false, maxTriangles = 2000, instances = 22 },
            new Slot { name = "PohonPalem", label = "pohon kelapa/palem", box = new Vector3(4.5f, 6.5f, 4.5f), useOrigin = true, occluder = "Palm crown model", maxTriangles = 3000, instances = 40 },
            new Slot { name = "PohonKetapang", label = "pohon ketapang", box = new Vector3(6f, 6.4f, 6f), occluder = "NusantaraTall model", maxTriangles = 12000, instances = 6 },
            new Slot { name = "PohonFlamboyan", label = "pohon flamboyan", box = new Vector3(5.5f, 5.2f, 5.5f), occluder = "MegahTall model", maxTriangles = 12000, instances = 6 },
            new Slot { name = "PohonTrembesi", label = "pohon trembesi", box = new Vector3(11f, 7.6f, 11f), occluder = "NusantaraTall model", maxTriangles = 20000, instances = 4 },
            new Slot { name = "PohonPisang", label = "pohon pisang", box = new Vector3(3f, 3.4f, 3f), castShadows = false, maxTriangles = 4000, instances = 10 },
            new Slot { name = "Becak", label = "becak", box = new Vector3(1.2f, 1.9f, 2.3f), maxTriangles = 10000, instances = 2 },
            new Slot { name = "MotorBebek", label = "motor bebek parkir", box = new Vector3(.7f, 1.2f, 1.4f), castShadows = false, maxTriangles = 8000, instances = 3 },
            new Slot { name = "GerobakBakso", label = "gerobak bakso", box = new Vector3(2.2f, 3.1f, 2.2f), frontYaw = 180f, occluder = "NusantaraTall model", maxTriangles = 10000, instances = 1 },
            new Slot { name = "WarungKopi", label = "warung kopi", box = new Vector3(4.6f, 3.3f, 4f), frontYaw = 180f, occluder = "NusantaraTall model", signHeight = .73f, maxTriangles = 20000, instances = 1 },
            new Slot { name = "LampuPJU", label = "lampu jalan PJU", box = new Vector3(.6f, 7.6f, 2.2f), frontYaw = 180f, useOrigin = true, castShadows = false, maxTriangles = 2000, instances = 22 },
            new Slot { name = "GapuraKampung", label = "gapura kampung", box = new Vector3(4.5f, 4.5f, .8f), frontYaw = 180f, occluder = "NusantaraTall model", signHeight = .77f, maxTriangles = 10000, instances = 2 },
            new Slot { name = "RumahKampung", label = "rumah kampung", box = new Vector3(7f, 5.3f, 6f), frontYaw = 180f, occluder = "KotaTall model", maxTriangles = 5000, instances = 22 },
            new Slot { name = "Ruko", label = "ruko dua lantai", box = new Vector3(7.8f, 7.7f, 8.4f), frontYaw = 180f, occluder = "KotaTall model", signHeight = .56f, maxTriangles = 6000, instances = 16 },
            new Slot { name = "GedungLama", label = "gedung lama", box = new Vector3(4.4f, 40f, 4.4f), frontYaw = 180f, occluder = "NusantaraTall model", maxTriangles = 15000, instances = 6 },
            new Slot { name = "Pendopo", label = "pendopo taman", box = new Vector3(5.4f, 5.3f, 5.4f), occluder = "NusantaraTall model", maxTriangles = 20000, instances = 2 },
        };

        private sealed class Entry
        {
            public GameObject source;
            public string path;
            public Settings settings;
            public string problem;
            public int triangles, placed;
            public Texture2D colorFallback;
            // One combined mesh per slot (a submesh per material) and its URP materials:
            // one renderer per placed model instead of one per part of the file.
            public Mesh mesh;
            public Material[] materials;
            public readonly Dictionary<string, Color> mtlColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();

        // Called at the start of every capital generation.
        public static void Begin() => Entries.Clear();

        public static Slot Find(string slotName)
        {
            foreach (var slot in Slots)
                if (slot.name == slotName) return slot;
            return null;
        }

        public static bool IsSlotAsset(string assetPath) =>
            assetPath.StartsWith(ModelsRoot + "/", StringComparison.Ordinal);

        public static bool IsModelFile(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".fbx" || extension == ".obj";
        }

        // <Slot>.fbx, then <Slot>.obj, then the only model file in the folder; otherwise none.
        public static string FindModelPath(string slotName)
        {
            string folder = ModelsRoot + "/" + slotName;
            if (!Directory.Exists(folder)) return null;
            foreach (string extension in new[] { ".fbx", ".FBX", ".obj", ".OBJ" })
            {
                string named = folder + "/" + slotName + extension;
                if (File.Exists(named)) return named;
            }
            string only = null;
            foreach (string file in Directory.GetFiles(folder))
            {
                if (!IsModelFile(file)) continue;
                if (only != null) return null; // ambiguous: the owner has to name one <Slot>.fbx
                only = file.Replace('\\', '/');
            }
            return only;
        }

        // "skala=1.2", "putar: 90", "naik=0,1", "ukuran=asli"; unknown lines are ignored.
        public static Settings ParseSettings(string text)
        {
            var settings = Settings.Default;
            if (string.IsNullOrEmpty(text)) return settings;
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                int split = line.IndexOfAny(new[] { '=', ':' });
                if (split <= 0) continue;
                string key = line.Substring(0, split).Trim().ToLowerInvariant();
                string value = line.Substring(split + 1).Trim().ToLowerInvariant();
                if (key == "ukuran")
                {
                    settings.originalSize = value == "asli";
                    continue;
                }
                if (!float.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
                    continue;
                if (key == "skala" && number > 0.01f && number < 100f) settings.scale = number;
                else if (key == "putar") settings.yaw = number;
                else if (key == "naik") settings.lift = Mathf.Clamp(number, -5f, 5f);
            }
            return settings;
        }

        // Uniform scale that fits a model of this size inside the box (axes under 1 cm ignored).
        public static float FitScale(Vector3 size, Vector3 box)
        {
            float scale = float.MaxValue;
            if (size.x > .01f) scale = Mathf.Min(scale, box.x / size.x);
            if (size.y > .01f) scale = Mathf.Min(scale, box.y / size.y);
            if (size.z > .01f) scale = Mathf.Min(scale, box.z / size.z);
            return scale == float.MaxValue ? 1f : scale;
        }

        // Replaces the visible parts of root's children [firstChild..] with the slot model.
        // Returns false (and changes nothing) when the slot has no usable model.
        public static bool Apply(string slotName, Transform root, int firstChild, Vector3 anchor, float yaw, Vector3? boxOverride = null,
            bool? castShadows = null)
        {
            var slot = Find(slotName);
            if (slot == null || root == null) return false;
            var entry = Load(slot);
            if (entry == null || entry.problem != null) return false;

            GameObject holder = null;
            try
            {
                var fallback = new List<Transform>();
                for (int i = firstChild; i < root.childCount; i++) fallback.Add(root.GetChild(i));

                holder = new GameObject("Model " + slot.name);
                holder.transform.SetParent(root, false);
                // Fit in the holder's own frame; the holder moves to the spot afterwards.
                holder.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var model = new GameObject(slot.occluder != null ? slot.occluder + " " + slot.name : slot.name,
                    typeof(MeshFilter), typeof(MeshRenderer));
                model.transform.SetParent(holder.transform, false);
                model.GetComponent<MeshFilter>().sharedMesh = entry.mesh;
                var renderer = model.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = entry.materials;
                renderer.shadowCastingMode = (castShadows ?? slot.castShadows) ? ShadowCastingMode.On : ShadowCastingMode.Off;

                Quaternion turn = Quaternion.Euler(0f, slot.frontYaw + entry.settings.yaw, 0f);
                Bounds bounds = TurnedBounds(entry.mesh.bounds, turn);
                if (bounds.size.sqrMagnitude < 1e-6f)
                    throw new InvalidOperationException("model tanpa bagian yang terlihat");
                float scale = (entry.settings.originalSize ? 1f : FitScale(bounds.size, boxOverride ?? slot.box)) * entry.settings.scale;
                model.transform.localRotation = turn;
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = new Vector3(slot.useOrigin ? 0f : -bounds.center.x * scale,
                    -bounds.min.y * scale + entry.settings.lift, slot.useOrigin ? 0f : -bounds.center.z * scale);
                holder.transform.SetPositionAndRotation(anchor, Quaternion.Euler(0f, yaw, 0f));
                GameObjectUtility.SetStaticEditorFlags(model, StaticEditorFlags.BatchingStatic);

                // Keep the Indonesian name signs (TOKO KELONTONG, GANG MERDEKA, WARKOP RAKYAT) on
                // the front of the model at the slot's sign height.
                if (slot.signHeight > 0f)
                {
                    float front = (bounds.min.z - (slot.useOrigin ? 0f : bounds.center.z)) * scale - .04f;
                    float height = bounds.size.y * scale * slot.signHeight + entry.settings.lift;
                    foreach (var part in fallback)
                        foreach (var sign in part.GetComponentsInChildren<TextMesh>(true))
                        {
                            sign.transform.SetParent(holder.transform, true);
                            sign.transform.localPosition = new Vector3(0f, height, front);
                        }
                }
                foreach (var part in fallback) Strip(part, true);
                entry.placed++;
                return true;
            }
            catch (Exception exception)
            {
                if (holder != null) Object.DestroyImmediate(holder);
                entry.problem = "gagal dipasang (" + exception.Message + ")";
                Debug.LogWarning("Model slot " + slot.name + " (" + entry.path + "): " + exception.Message + "; bentuk kode dipakai.");
                return false;
            }
        }

        // One line for the HUD: empty when the owner has not uploaded any model.
        public static string Summary()
        {
            var installed = new List<string>();
            var problems = new List<string>();
            int placedTotal = 0;
            foreach (var slot in Slots)
            {
                if (!Entries.TryGetValue(slot.name, out var entry) || entry == null) continue;
                if (entry.problem != null) problems.Add(slot.name + " " + entry.problem);
                else if (entry.placed > 0) { installed.Add(slot.name + " x" + entry.placed); placedTotal += entry.placed; }
            }
            if (installed.Count == 0 && problems.Count == 0) return "";
            // 0.3.2: short when everything is in (the full list ran off the tablet screen);
            // only problems are spelled out.
            if (problems.Count == 0)
                return "MODEL 3D: " + installed.Count + "/" + Slots.Length + " slot terpasang (" + placedTotal + " objek)";
            return "MODEL 3D: " + installed.Count + "/" + Slots.Length + " terpasang  •  TIDAK DIPAKAI: " + string.Join(", ", problems);
        }

        private static Entry Load(Slot slot)
        {
            if (Entries.TryGetValue(slot.name, out var cached)) return cached;
            string path = FindModelPath(slot.name);
            if (path == null)
            {
                Entries[slot.name] = null;
                return null;
            }
            var entry = new Entry { path = path };
            Entries[slot.name] = entry;
            try
            {
                string settingsPath = ModelsRoot + "/" + slot.name + "/" + SettingsFile;
                entry.settings = File.Exists(settingsPath) ? ParseSettings(File.ReadAllText(settingsPath)) : Settings.Default;
                entry.source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (entry.source == null)
                {
                    entry.problem = "belum ter-import";
                    return entry;
                }
                entry.triangles = Triangles(entry.source);
                if (entry.triangles > slot.maxTriangles)
                {
                    entry.problem = "terlalu berat (" + entry.triangles + " > " + slot.maxTriangles + " segitiga)";
                    return entry;
                }
                string folder = Path.GetDirectoryName(path).Replace('\\', '/');
                entry.colorFallback = OnlyColorTexture(folder);
                ReadMtlColors(path, entry.mtlColors);
                Combine(slot, entry);
            }
            catch (Exception exception)
            {
                entry.problem = "gagal dibaca (" + exception.Message + ")";
            }
            if (entry.problem != null)
                Debug.LogWarning("Model slot " + slot.name + " (" + path + "): " + entry.problem + "; bentuk kode dipakai.");
            return entry;
        }

        private static int Triangles(GameObject source)
        {
            long total = 0;
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
                total += Triangles(filter.sharedMesh);
            foreach (var skinned in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                total += Triangles(skinned.sharedMesh);
            return (int)Math.Min(total, int.MaxValue);
        }

        private static long Triangles(Mesh mesh)
        {
            if (mesh == null) return 0;
            long count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                if (mesh.GetTopology(i) == MeshTopology.Triangles) count += mesh.GetIndexCount(i) / 3;
            return count;
        }

        // Removes what is drawn, keeps what collides. A part (and its subtree) without any
        // collider is deleted, except group roots at the top, which other code may look up by name.
        private static void Strip(Transform part, bool top)
        {
            if (part == null) return;
            bool collides = part.GetComponentInChildren<Collider>(true) != null;
            bool isGroup = part.childCount > 0 && part.GetComponent<Renderer>() == null;
            if (!collides && !(top && isGroup))
            {
                Object.DestroyImmediate(part.gameObject);
                return;
            }
            var text = part.GetComponent<TextMesh>();
            if (text != null) Object.DestroyImmediate(text);
            var renderer = part.GetComponent<Renderer>();
            if (renderer != null) Object.DestroyImmediate(renderer);
            var filter = part.GetComponent<MeshFilter>();
            if (filter != null) Object.DestroyImmediate(filter);
            var children = new List<Transform>();
            foreach (Transform child in part) children.Add(child);
            foreach (var child in children) Strip(child, false);
        }

        // Merges every mesh of the file (cameras, lights, colliders and animation are simply not
        // taken) into one mesh with a submesh per material, in the file's own root space.
        private static void Combine(Slot slot, Entry entry)
        {
            var groups = new List<KeyValuePair<Material, List<CombineInstance>>>();
            Matrix4x4 toRoot = entry.source.transform.worldToLocalMatrix;
            Matrix4x4 rootTurn = Matrix4x4.Rotate(entry.source.transform.localRotation) * Matrix4x4.Scale(entry.source.transform.localScale);
            foreach (var renderer in entry.source.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh = null;
                if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
                else if (renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
                if (mesh == null) continue;
                // Keep the root's own rotation and scale (e.g. a Z-up correction), drop its position.
                Matrix4x4 matrix = rootTurn * toRoot * renderer.transform.localToWorldMatrix;
                Material[] materials = renderer.sharedMaterials;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    if (mesh.GetTopology(sub) != MeshTopology.Triangles) continue;
                    Material material = materials.Length == 0 ? null : materials[Mathf.Min(sub, materials.Length - 1)];
                    var group = groups.Find(g => g.Key == material);
                    if (group.Value == null)
                    {
                        group = new KeyValuePair<Material, List<CombineInstance>>(material, new List<CombineInstance>());
                        groups.Add(group);
                    }
                    group.Value.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = matrix });
                }
            }
            if (groups.Count == 0) throw new InvalidOperationException("tidak ada mesh");

            var parts = new CombineInstance[groups.Count];
            entry.materials = new Material[groups.Count];
            for (int i = 0; i < groups.Count; i++)
            {
                var part = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                part.CombineMeshes(groups[i].Value.ToArray(), true, true);
                parts[i] = new CombineInstance { mesh = part, transform = Matrix4x4.identity };
                entry.materials[i] = CreateLit(slot, groups[i].Key, entry.colorFallback, entry.mtlColors, i);
            }
            var combined = new Mesh { name = slot.name + "Gabungan", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            combined.CombineMeshes(parts, false, false);
            if (combined.vertexCount < 65000) combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            combined.RecalculateBounds();
            foreach (var part in parts) Object.DestroyImmediate(part.mesh);

            EnsureFolder();
            string path = MaterialFolder + "/" + slot.name + "Gabungan.asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(combined, path); saved = combined; }
            else
            {
                EditorUtility.CopySerialized(combined, saved);
                saved.name = slot.name + "Gabungan";
                Object.DestroyImmediate(combined);
            }
            EditorUtility.SetDirty(saved);
            entry.mesh = saved;
        }

        // Axis-aligned bounds of a box after a turn about Y.
        private static Bounds TurnedBounds(Bounds bounds, Quaternion turn)
        {
            var result = new Bounds(turn * bounds.center, Vector3.zero);
            Vector3 e = bounds.extents;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                result.Encapsulate(turn * (bounds.center + corner));
            }
            return result;
        }

        // Kd colours from the .mtl next to an .obj, by material name. Unity's OBJ import does not
        // always carry them over, so they are applied to the converted materials by name.
        private static void ReadMtlColors(string modelPath, Dictionary<string, Color> colors)
        {
            if (Path.GetExtension(modelPath).ToLowerInvariant() != ".obj") return;
            string folder = Path.GetDirectoryName(modelPath);
            var files = new List<string>();
            foreach (string line in File.ReadAllLines(modelPath))
                if (line.StartsWith("mtllib ", StringComparison.Ordinal))
                    files.Add(Path.Combine(folder, line.Substring(7).Trim()));
            files.Add(Path.ChangeExtension(modelPath, ".mtl"));
            foreach (string file in files)
            {
                if (!File.Exists(file)) continue;
                ParseMtl(File.ReadAllText(file), colors);
            }
        }

        public static void ParseMtl(string text, Dictionary<string, Color> colors)
        {
            string current = null;
            foreach (string raw in text.Split('\n'))
            {
                string[] parts = raw.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[0] == "newmtl") current = parts[1];
                else if (parts.Length >= 4 && parts[0] == "Kd" && current != null &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                    float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                    float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float b))
                    colors[current] = new Color(r, g, b, 1f);
            }
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder(SpikeProject.Generated, "Models");
        }

        // Every model material becomes a saved URP Lit copy: no pink materials from OBJ/MTL or
        // non-URP FBX shaders; MTL Kd colours by name; cut-out and two-sided for leaf textures.
        private static Material CreateLit(Slot slot, Material original, Texture2D colorFallback,
            Dictionary<string, Color> mtlColors, int index)
        {
            EnsureFolder();
            string baseName = original != null ? original.name : "Bahan";
            foreach (char bad in Path.GetInvalidFileNameChars()) baseName = baseName.Replace(bad, '_');
            string path = MaterialFolder + "/" + slot.name + "_" + index + "_" + baseName + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            else mat.shader = shader;

            Color color = Color.white;
            Texture texture = null, normal = null;
            if (original != null)
            {
                if (original.HasProperty("_BaseColor")) color = original.GetColor("_BaseColor");
                else if (original.HasProperty("_Color")) color = original.GetColor("_Color");
                if (original.HasProperty("_BaseMap")) texture = original.GetTexture("_BaseMap");
                if (texture == null && original.HasProperty("_MainTex")) texture = original.GetTexture("_MainTex");
                if (original.HasProperty("_BumpMap")) normal = original.GetTexture("_BumpMap");
            }
            if (texture == null && colorFallback != null) texture = colorFallback;
            string key = original != null ? original.name.Replace(" (Instance)", "").Trim() : "";
            if (mtlColors.TryGetValue(key, out Color mtl)) color = mtl;
            color.a = 1f;
            string lower = key.ToLowerInvariant();
            bool glass = lower.Contains("glass") || lower.Contains("kaca");
            bool metal = lower.Contains("metal") || lower.Contains("silver") || lower.Contains("gold") ||
                lower.Contains("bronze") || lower.Contains("chrome") || lower.Contains("steel");
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", texture);
            mat.SetFloat("_Smoothness", glass ? .85f : metal ? .55f : .15f);
            mat.SetFloat("_Metallic", metal ? .35f : 0f);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            else mat.DisableKeyword("_NORMALMAP");

            bool cutout = (original != null && original.renderQueue >= (int)RenderQueue.AlphaTest) || HasAlpha(texture);
            mat.SetFloat("_AlphaClip", cutout ? 1f : 0f);
            mat.SetFloat("_Cutoff", .5f);
            mat.SetFloat("_Cull", cutout ? (float)CullMode.Off : (float)CullMode.Back);
            if (cutout) mat.EnableKeyword("_ALPHATEST_ON"); else mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = cutout ? (int)RenderQueue.AlphaTest : -1;
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static bool HasAlpha(Texture texture)
        {
            if (texture == null) return false;
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) is TextureImporter importer &&
                importer.DoesSourceTextureHaveAlpha() && importer.alphaSource != TextureImporterAlphaSource.None;
        }

        // The colour texture to use when a model's materials lost their texture link (common
        // with OBJ and with downloads that keep textures next to the model): the single
        // non-normal texture in the folder, or the one whose name says colour/diffuse/albedo.
        private static Texture2D OnlyColorTexture(string folder)
        {
            Texture2D only = null, named = null;
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (file.Contains("nor") || file.Contains("rough") || file.Contains("metal") || file.Contains("ao") ||
                    file.Contains("height") || file.Contains("disp") || file.Contains("spec") || file.Contains("opacity"))
                    continue;
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;
                count++;
                only = texture;
                if (file.Contains("diff") || file.Contains("color") || file.Contains("albedo") || file.Contains("basecolor") || file.Contains("col"))
                    named = texture;
            }
            return named != null ? named : count == 1 ? only : null;
        }
    }

    // Import rules for Assets/Konoha/Art/Models/**: static props, no cameras/lights/colliders,
    // URP materials from the material description, textures capped at 1024 px for Android.
    internal sealed class CampaignModelSlotImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!CampaignModelSlots.IsSlotAsset(assetPath) || !(assetImporter is ModelImporter importer))
                return;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            // Readable in the editor so the parts can be merged; the source file itself is not in the build.
            importer.isReadable = true;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        private void OnPreprocessTexture()
        {
            if (!CampaignModelSlots.IsSlotAsset(assetPath) || !(assetImporter is TextureImporter importer))
                return;
            string file = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
            if (file.Contains("nor") || file.Contains("normal"))
                importer.textureType = TextureImporterType.NormalMap;
            if (importer.maxTextureSize > 1024) importer.maxTextureSize = 1024;
        }
    }
}
