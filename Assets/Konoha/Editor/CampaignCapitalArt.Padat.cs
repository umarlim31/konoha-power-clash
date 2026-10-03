using System.Collections.Generic;
using Konoha.Campaign;
using UnityEngine;

namespace Konoha.Editor
{
    // 0.6.3 KARIER "Padat" (owner: "aku nggak bisa menembus apapun; rumah harus masuk lewat
    // pintu, kalau nggak ada pintu ya nggak bisa masuk"). Most decor of the capital is built
    // without colliders on purpose (the tested MODE PRESIDEN route). This pass adds a solid
    // box for every prop a person could bump into (houses, ruko, carts, trees, poles, fences,
    // benches, parked motors) under one root that only KARIER switches on; MODE PRESIDEN and
    // its route tests never see them. The boxes:
    //  - are separate objects (decor itself still has no collider, as the scene tests demand),
    //  - cover only the body band (ground to 2.4 m), fitted to the vertices in that band,
    //    turned with the prop (trunks and poles stay thin, crowns and roofs are not included),
    //  - sit on layer 2 (Ignore Raycast) like the other solid walls, so the orbit camera keeps
    //    looking past them exactly as before,
    //  - are never placed where KARIER needs the hero (job zones, stalls, spawn, POLSEK door
    //    and cell, sapa and preman points): a box overlapping such a point is dropped.
    // Model slot instances (owner FBX) get a recipe per slot: trees and poles a thin trunk,
    // the gapura two pillars, buildings and carts their body; the pendopo keeps its own
    // landing, columns and benches (they never lost their colliders).
    internal sealed partial class CampaignCapitalArt
    {
        private const float BodyBand = 2.4f;
        private const float StepHeight = 0.4f;     // Hero step offset is 0.25 m.
        private const float ReachMargin = 3f;      // Outside the invisible oval nobody can bump into it.

        private static readonly string[] PadatSkip =
        {
            "frond", "crown", "tajuk", "daun", "leaf", "foliage", "blossom", "bunga", "rumput", "grass",
            " air", "water", "kolam", "teluk", "bayang", "shadow", "glow", "cahaya", "garis", "marka", "zebra",
            "bendera", "flag", "banner", "umbul", "kabel", "teks", "label", "sign", "papan nama", "atap", "roof",
            "genteng", "kanopi", "awning", "payung", "tenda", "jendela", "window", "pintu", "door", "lampu", "lamp head",
            "ground", "jalan", "trotoar", "lantai", "floor", "karpet", "jembatan", "bridge", "stair", "tangga",
            "ramp", "landing", "plint", "jeruji", "zona", "zone", "pandu", "layar", "sail", "perahu", "boat",
            "finial", "mustaka", "ridge", "bubungan", "lisplang", "talang", "ac", "balkon", "kusen", "ambang",
            "galian", "piring", "jajanan", "kaca"
        };

        private sealed class SlotRecipe
        {
            public float bandFrom, bandTo, maxWidth;
            public bool pillars, skip;
        }

        private static SlotRecipe Recipe(string slot)
        {
            switch (slot)
            {
                case "PohonPalem":
                case "PohonKetapang":
                case "PohonFlamboyan":
                case "PohonPisang":
                    return new SlotRecipe { bandFrom = .2f, bandTo = 1.1f, maxWidth = .6f };
                case "PohonTrembesi":
                    return new SlotRecipe { bandFrom = .2f, bandTo = 1.1f, maxWidth = .65f };
                case "Lentera":
                case "LampuPJU":
                    return new SlotRecipe { bandFrom = .3f, bandTo = 1.5f, maxWidth = .35f };
                case "GapuraKampung":
                    return new SlotRecipe { bandFrom = .1f, bandTo = BodyBand, maxWidth = 1.4f, pillars = true };
                case "Pendopo":
                    return new SlotRecipe { skip = true };
                default:
                    return new SlotRecipe { bandFrom = .05f, bandTo = BodyBand, maxWidth = 40f };
            }
        }

        // Builds the boxes (switched off) and returns their root. keepFree: ground points KARIER needs.
        internal GameObject BuildSolidProps(IList<Vector3> keepFree)
        {
            var padat = new GameObject("KarierPadat");
            var props = kotaRoot != null ? kotaRoot.Find("Karier properti") : null;
            // Fixed kampung scenes that live next to the moving life (0.5.0): solid as well.
            var scenes = new List<Transform>();
            if (props != null)
                scenes.Add(props);
            if (kotaRoot != null)
                foreach (Transform child in kotaRoot)
                    if (child.name == "Kota pos ronda" || child.name == "Kota gerobak sayur" || child.name == "Kota rumah duka" ||
                        child.name == "Kota motor jatuh")
                        scenes.Add(child);
            Physics.SyncTransforms();
            int made = 0;
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                Transform t = renderer.transform;
                if (t.GetComponent<TextMesh>() != null || t.GetComponent<Collider>() != null)
                    continue;
                // Moving life (walkers, traffic, the crowd, ridden props): only the fixed KARIER props.
                if (kotaRoot != null && t.IsChildOf(kotaRoot) && !UnderAny(t, scenes))
                    continue;
                if (t.GetComponentInParent<Animator>() != null || IsPerson(t))
                    continue;
                Transform holder = t.parent;
                if (holder != null && holder.name.StartsWith("Model ", System.StringComparison.Ordinal))
                {
                    if (t.name.EndsWith(" atas", System.StringComparison.Ordinal))
                        continue;
                    SlotRecipe recipe = Recipe(holder.name.Substring(6));
                    if (recipe.skip)
                        continue;
                    made += SolidFor(renderer, padat.transform, recipe.bandFrom, recipe.bandTo, recipe.maxWidth, recipe.pillars);
                    continue;
                }
                if (Skipped(t.name))
                    continue;
                string lower = t.name.ToLowerInvariant();
                bool trunk = lower.Contains("trunk") || lower.Contains("batang") || lower.Contains("pokok");
                made += SolidFor(renderer, padat.transform, .05f, BodyBand, trunk ? .6f : 40f, false);
            }

            // Nothing solid where KARIER needs the hero to stand.
            Physics.SyncTransforms();
            var hits = new Collider[64];
            int dropped = 0;
            foreach (Vector3 point in keepFree)
            {
                Vector3 ground = new Vector3(point.x, point.y, point.z);
                int count = Physics.OverlapCapsuleNonAlloc(ground + Vector3.up * .45f, ground + Vector3.up * 1.6f, .65f, hits, ~0,
                    QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    if (hits[i] == null || !hits[i].transform.IsChildOf(padat.transform))
                        continue;
                    Object.DestroyImmediate(hits[i].gameObject);
                    dropped++;
                }
            }
            Physics.SyncTransforms();
            Debug.Log("KARIER padat: " + (made - dropped) + " kotak (" + dropped + " dibuang di titik KARIER).");
            return padat;
        }

        private static bool UnderAny(Transform t, List<Transform> roots)
        {
            foreach (Transform r in roots)
                if (t.IsChildOf(r))
                    return true;
            return false;
        }

        private static bool IsPerson(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                string n = p.name;
                if (n.StartsWith("Kota warga", System.StringComparison.Ordinal) || n.StartsWith("Kota anak", System.StringComparison.Ordinal) ||
                    n.StartsWith("Karier penjual", System.StringComparison.Ordinal) || n.StartsWith("Karier warga", System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool Skipped(string name)
        {
            string lower = " " + name.ToLowerInvariant() + " ";
            foreach (string word in PadatSkip)
            {
                if (word == "ac")
                {
                    if (lower.Contains(" ac ")) return true;
                    continue;
                }
                if (lower.Contains(word))
                    return true;
            }
            return false;
        }

        // One box (two for pillars) around the vertices of this renderer in the body band.
        private int SolidFor(MeshRenderer renderer, Transform parent, float bandFrom, float bandTo, float maxWidth, bool pillars)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
                return 0;
            Bounds world = renderer.bounds;
            if (!Reachable(world))
                return 0;
            float ground = Ground(world);
            float bottom = Mathf.Max(world.min.y, ground + bandFrom);
            float top = Mathf.Min(world.max.y, ground + bandTo);
            if (world.max.y - ground < StepHeight || world.min.y - ground > 1.5f || top - bottom < .15f)
                return 0;

            // The prop's own yaw, so walls and carts get a turned box instead of a fat one.
            Vector3 forward = renderer.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < .01f)
            {
                forward = renderer.transform.up;
                forward.y = 0f;
            }
            Quaternion yaw = forward.sqrMagnitude < .01f ? Quaternion.identity : Quaternion.LookRotation(forward.normalized);
            Quaternion unyaw = Quaternion.Inverse(yaw);
            Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;

            Vector3[] vertices;
            try { vertices = mesh.vertices; }
            catch { return 0; }
            var left = new Bounds();
            var right = new Bounds();
            var all = new Bounds();
            bool anyLeft = false, anyRight = false, any = false;
            float pivot = (unyaw * world.center).x;
            foreach (Vector3 v in vertices)
            {
                Vector3 w = toWorld.MultiplyPoint3x4(v);
                if (w.y < bottom - .01f || w.y > top + .01f)
                    continue;
                Vector3 local = unyaw * w;
                local.y = 0f;
                Add(ref all, ref any, local);
                if (pillars)
                {
                    if (local.x < pivot) Add(ref left, ref anyLeft, local);
                    else Add(ref right, ref anyRight, local);
                }
            }
            if (!any)
            {
                // Long faces with no vertex inside the band (a cube 0..3 m): use its footprint.
                foreach (Vector3 v in new[] { mesh.bounds.min, mesh.bounds.max,
                    new Vector3(mesh.bounds.min.x, mesh.bounds.min.y, mesh.bounds.max.z), new Vector3(mesh.bounds.max.x, mesh.bounds.min.y, mesh.bounds.min.z) })
                {
                    Vector3 local = unyaw * toWorld.MultiplyPoint3x4(v);
                    local.y = 0f;
                    Add(ref all, ref any, local);
                }
            }
            if (pillars && anyLeft && anyRight)
            {
                // Gate: one post on each side, the passage in between stays open.
                return Box(parent, renderer.name, Shrink(left, maxWidth, pivot), yaw, bottom, top) +
                    Box(parent, renderer.name, Shrink(right, maxWidth, pivot), yaw, bottom, top);
            }
            return Box(parent, renderer.name, Cap(all, maxWidth), yaw, bottom, top);
        }

        private static void Add(ref Bounds bounds, ref bool any, Vector3 point)
        {
            if (!any) { bounds = new Bounds(point, Vector3.zero); any = true; }
            else bounds.Encapsulate(point);
        }

        // Trunks and poles: at most maxWidth around the middle of what is in the band.
        private static Bounds Cap(Bounds b, float maxWidth)
        {
            Vector3 size = b.size;
            size.x = Mathf.Min(size.x, maxWidth);
            size.z = Mathf.Min(size.z, maxWidth);
            return new Bounds(b.center, size);
        }

        // Gate pillar: the outer maxWidth of one side (the inner side faces the passage).
        private static Bounds Shrink(Bounds b, float maxWidth, float pivot)
        {
            if (b.size.x <= maxWidth)
                return b;
            bool leftSide = b.center.x < pivot;
            float outer = leftSide ? b.min.x : b.max.x;
            float inner = leftSide ? outer + maxWidth : outer - maxWidth;
            var result = new Bounds(new Vector3((outer + inner) * .5f, 0f, b.center.z), new Vector3(maxWidth, 0f, b.size.z));
            return result;
        }

        private static int Box(Transform parent, string name, Bounds footprint, Quaternion yaw, float bottom, float top)
        {
            Vector3 size = footprint.size;
            if (size.x < .12f && size.z < .12f)
                return 0; // Tiny trims; the part they belong to is solid already.
            size.x = Mathf.Max(size.x, .2f);
            size.z = Mathf.Max(size.z, .2f);
            var box = new GameObject("Padat " + name);
            box.layer = 2; // Ignore Raycast: the orbit camera keeps looking past it (KarierCrowd still sees it).
            box.transform.SetParent(parent, false);
            Vector3 centre = yaw * new Vector3(footprint.center.x, 0f, footprint.center.z);
            box.transform.SetPositionAndRotation(new Vector3(centre.x, (bottom + top) * .5f, centre.z), yaw);
            var collider = box.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x, top - bottom, size.z);
            return 1;
        }

        private static bool Reachable(Bounds b)
        {
            // Nearest point of the box to the oval centre, tested against the oval plus a margin.
            float x = Mathf.Clamp(BoundaryCenter.x, b.min.x, b.max.x) - BoundaryCenter.x;
            float z = Mathf.Clamp(BoundaryCenter.y, b.min.z, b.max.z) - BoundaryCenter.y;
            float rx = BoundaryRadii.x + ReachMargin, rz = BoundaryRadii.y + ReachMargin;
            return (x * x) / (rx * rx) + (z * z) / (rz * rz) <= 1f;
        }

        // Walkable ground under a prop: the nearest solid surface below its bottom (the world
        // floor, y 0, when nothing solid is below). A lamp arm 7 m up is then far above it.
        private static float Ground(Bounds b)
        {
            var origin = new Vector3(b.center.x, b.min.y + .6f, b.center.z);
            float best = float.MaxValue, ground = 0f;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider is CharacterController || (hit.collider.transform.parent != null &&
                    hit.collider.transform.parent.name == "KarierPadat"))
                    continue;
                if (hit.distance < best)
                {
                    best = hit.distance;
                    ground = hit.point.y;
                }
            }
            return best < float.MaxValue ? ground : Mathf.Min(0f, b.min.y);
        }
    }
}
