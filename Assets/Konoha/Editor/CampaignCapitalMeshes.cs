using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Konoha.Editor
{
    // Meshes are persisted as assets: scene serialization must not lose editor-created geometry.
    internal static class CampaignCapitalMeshes
    {
        internal const string Folder = SpikeProject.Generated + "/Capital";

        internal static Mesh Lathe(string name, Vector2[] profile, int sides = 32)
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            for (int row = 0; row < profile.Length; row++)
                for (int i = 0; i <= sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    vertices.Add(new Vector3(Mathf.Cos(a) * profile[row].x, profile[row].y,
                        Mathf.Sin(a) * profile[row].x));
                    uv.Add(new Vector2(i / (float)sides, profile[row].y));
                    if (row == 0 || i == sides) continue;
                    int b = row * (sides + 1) + i, p = b - sides - 1;
                    triangles.AddRange(new[] { p, b, p + 1, p + 1, b, b + 1 });
                }
            return Save(name, vertices, uv, triangles);
        }

        internal static Mesh Ring(string name, float inner, float outer, int sides = 96)
        {
            return Lathe(name, new[] { new Vector2(outer, 0), new Vector2(inner, 0) }, sides);
        }

        internal static Mesh Roof()
        {
            // A raised ridge, sloping hip faces and upturned eaves, not intersecting roof slabs.
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            Vector3 a = new Vector3(-1f, 0, -1), b = new Vector3(1f, 0, -1);
            Vector3 c = new Vector3(1f, 0, 1), d = new Vector3(-1f, 0, 1);
            Vector3 e = new Vector3(-0.42f, 0.65f, 0), f = new Vector3(0.42f, 0.65f, 0);
            Face(v, uv, t, a, e, f, b);
            Face(v, uv, t, d, c, f, e);
            Face(v, uv, t, a, d, e, e);
            Face(v, uv, t, b, f, c, c);
            Face(v, uv, t, a, b, c, d);
            return Save("HippedNusantaraRoof", v, uv, t);
        }

        internal static Mesh Feather(string name, bool palm)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            // Closed, curved, pointed leaf/feather. The central ridge catches sunlight.
            const int steps = 10;
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                float width = Mathf.Sin(Mathf.PI * u) * (palm ? 0.16f : 0.23f) + 0.002f;
                float curve = palm ? 0.50f * Mathf.Sin(u * Mathf.PI) - u * 0.28f : u * u * 0.19f;
                v.Add(new Vector3(-width, curve, u));
                v.Add(new Vector3(0, curve + width * 0.3f, u));
                v.Add(new Vector3(width, curve, u));
                uv.Add(new Vector2(0, u)); uv.Add(new Vector2(0.5f, u)); uv.Add(new Vector2(1, u));
                if (i == steps) continue;
                int n = i * 3;
                t.AddRange(new[] { n, n + 3, n + 1, n + 1, n + 3, n + 4,
                    n + 1, n + 4, n + 2, n + 2, n + 4, n + 5 });
            }
            // Both sides visible even from the low showcase camera.
            int count = v.Count, tc = t.Count;
            for (int i = 0; i < count; i++) { v.Add(v[i] - Vector3.up * 0.015f); uv.Add(uv[i]); }
            for (int i = 0; i < tc; i += 3)
                t.AddRange(new[] { t[i + 2] + count, t[i + 1] + count, t[i] + count });
            return Save(name, v, uv, t);
        }

        internal static Mesh Arch()
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            const int steps = 16;
            for (int i = 0; i < steps; i++)
            {
                float a = Mathf.PI * i / steps, b = Mathf.PI * (i + 1) / steps;
                Vector3 ai = new Vector3(Mathf.Cos(a), Mathf.Sin(a), -0.18f);
                Vector3 ao = new Vector3(Mathf.Cos(a) * 1.24f, Mathf.Sin(a) * 1.24f, -0.18f);
                Vector3 bi = new Vector3(Mathf.Cos(b), Mathf.Sin(b), -0.18f);
                Vector3 bo = new Vector3(Mathf.Cos(b) * 1.24f, Mathf.Sin(b) * 1.24f, -0.18f);
                Vector3 depth = Vector3.forward * 0.36f;
                Face(v, uv, t, ai, bi, bo, ao);
                Face(v, uv, t, ao + depth, bo + depth, bi + depth, ai + depth);
                Face(v, uv, t, ao, bo, bo + depth, ao + depth);
                Face(v, uv, t, bi, ai, ai + depth, bi + depth);
            }
            return Save("CarvedArch", v, uv, t);
        }

        private static void Face(List<Vector3> v, List<Vector2> uv, List<int> t,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int n = v.Count;
            v.AddRange(new[] { a, b, c, d });
            uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
            t.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
        }

        private static Mesh Save(string name, List<Vector3> v, List<Vector2> uv, List<int> t)
        {
            string path = Folder + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, path); }
            mesh.Clear(); mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
