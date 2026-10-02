using System;
using UnityEngine;

namespace Konoha.Campaign
{
    // 0.2.9: paints the round skill-button icons in code (no image files, like the rest of
    // the generated content). Each icon is a white silhouette with soft (anti-aliased) edges,
    // built from signed-distance shapes on a 128 x 128 canvas, y up. Pure C#, testable.
    // IP: original pictograms only; SERUAN IBU is a kerbau (water buffalo), never a bull head.
    public static class CampaignIconPainter
    {
        public const int Size = 128;

        public enum Icon
        {
            Kerbau, KerbauStampede, Shield, Leap, Baris, Wings,
            Speech, Lightning, Microphone, Cone, Footsteps, HardHat,
            Fist, Dodge, Jump, Seat, Circle, Ring
        }

        public sealed class Canvas
        {
            public readonly float[] alpha = new float[Size * Size];

            // Coverage from a signed distance (negative inside), 1.2 px soft edge.
            private void Apply(Func<float, float, float> distance, bool erase)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        float d = distance(x + .5f, y + .5f);
                        float coverage = Mathf.Clamp01(.5f - d / 1.2f);
                        if (coverage <= 0f) continue;
                        int i = y * Size + x;
                        alpha[i] = erase ? Mathf.Min(alpha[i], 1f - coverage) : Mathf.Max(alpha[i], coverage);
                    }
                }
            }

            public Canvas Disc(float cx, float cy, float r, bool erase = false) =>
                Do((x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, erase);

            public Canvas RingShape(float cx, float cy, float r, float width, bool erase = false) =>
                Do((x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - width * .5f, erase);

            public Canvas Ellipse(float cx, float cy, float rx, float ry, float degrees = 0f, bool erase = false)
            {
                float c = Mathf.Cos(degrees * Mathf.Deg2Rad), s = Mathf.Sin(degrees * Mathf.Deg2Rad);
                float k = Mathf.Min(rx, ry);
                return Do((x, y) =>
                {
                    float px = (x - cx) * c + (y - cy) * s, py = -(x - cx) * s + (y - cy) * c;
                    float q = Mathf.Sqrt((px / rx) * (px / rx) + (py / ry) * (py / ry));
                    return (q - 1f) * k;
                }, erase);
            }

            public Canvas Box(float cx, float cy, float w, float h, float degrees = 0f, float round = 0f, bool erase = false)
            {
                float c = Mathf.Cos(degrees * Mathf.Deg2Rad), s = Mathf.Sin(degrees * Mathf.Deg2Rad);
                return Do((x, y) =>
                {
                    float px = Mathf.Abs((x - cx) * c + (y - cy) * s) - (w * .5f - round);
                    float py = Mathf.Abs(-(x - cx) * s + (y - cy) * c) - (h * .5f - round);
                    float outside = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) + Mathf.Max(py, 0f) * Mathf.Max(py, 0f));
                    return outside + Mathf.Min(Mathf.Max(px, py), 0f) - round;
                }, erase);
            }

            // Capsule (thick line with round caps) from a to b.
            public Canvas Line(float ax, float ay, float bx, float by, float width, bool erase = false) =>
                Do((x, y) =>
                {
                    float vx = bx - ax, vy = by - ay, wx = x - ax, wy = y - ay;
                    float t = Mathf.Clamp01((wx * vx + wy * vy) / Mathf.Max(.0001f, vx * vx + vy * vy));
                    float dx = wx - vx * t, dy = wy - vy * t;
                    return Mathf.Sqrt(dx * dx + dy * dy) - width * .5f;
                }, erase);

            public Canvas Triangle(float ax, float ay, float bx, float by, float cx, float cy, bool erase = false) =>
                Do((x, y) =>
                {
                    float e0 = Edge(ax, ay, bx, by, x, y), e1 = Edge(bx, by, cx, cy, x, y), e2 = Edge(cx, cy, ax, ay, x, y);
                    float sign = Edge(ax, ay, bx, by, cx, cy) > 0f ? 1f : -1f;
                    // Distance to the nearest edge, negative inside.
                    float inside = Mathf.Min(e0 * sign, Mathf.Min(e1 * sign, e2 * sign));
                    return -inside;
                }, erase);

            private static float Edge(float ax, float ay, float bx, float by, float x, float y)
            {
                float vx = bx - ax, vy = by - ay;
                float length = Mathf.Max(.0001f, Mathf.Sqrt(vx * vx + vy * vy));
                return ((x - ax) * vy - (y - ay) * vx) / -length;
            }

            private Canvas Do(Func<float, float, float> distance, bool erase)
            {
                Apply(distance, erase);
                return this;
            }

            public float Coverage()
            {
                float sum = 0f;
                foreach (float a in alpha) sum += a;
                return sum / alpha.Length;
            }
        }

        public static Canvas Paint(Icon icon)
        {
            var c = new Canvas();
            switch (icon)
            {
                case Icon.Kerbau: KerbauHead(c, 64f, 52f, 1f); break;
                case Icon.KerbauStampede:
                    KerbauHead(c, 78f, 52f, .8f);
                    foreach (float y in new[] { 36f, 56f, 76f })
                        c.Line(8f, y, 34f - Mathf.Abs(y - 56f) * .3f, y, 7f);
                    break;
                case Icon.Shield:
                    c.Box(64f, 82f, 70f, 44f, 0f, 10f).Triangle(29f, 66f, 99f, 66f, 64f, 14f);
                    c.Triangle(64f, 92f, 48f, 62f, 80f, 62f, true).Triangle(64f, 50f, 48f, 80f, 80f, 80f, true);
                    break;
                case Icon.Leap:
                    for (int i = 0; i < 10; i++)
                    {
                        float t0 = i / 10f, t1 = (i + 1) / 10f;
                        c.Line(Leap(t0).x, Leap(t0).y, Leap(t1).x, Leap(t1).y, 8f);
                    }
                    c.Triangle(100f, 34f, 82f, 46f, 104f, 54f);
                    foreach (float dx in new[] { -16f, 0f, 16f })
                        c.Line(98f + dx * .9f, 22f, 98f + dx * 1.6f, 12f, 5f);
                    break;
                case Icon.Baris:
                    foreach (float x in new[] { 34f, 64f, 94f })
                    {
                        float s = x == 64f ? 1.12f : 1f;
                        c.Disc(x, 84f * (s > 1f ? 1.02f : 1f), 9f * s).Box(x, 92f * (s > 1f ? 1.02f : 1f), 20f * s, 6f * s, 0f, 2f);
                        c.Box(x, 52f, 20f * s, 34f * s, 0f, 5f).Box(x - 5f * s, 26f, 7f * s, 20f, 0f, 3f).Box(x + 5f * s, 26f, 7f * s, 20f, 0f, 3f);
                    }
                    break;
                case Icon.Wings:
                    c.Ellipse(64f, 58f, 10f, 22f).Disc(64f, 86f, 9f);
                    foreach (int side in new[] { -1, 1 })
                        for (int f = 0; f < 4; f++)
                        {
                            float angle = (20f + f * 18f) * Mathf.Deg2Rad;
                            float length = 46f - f * 7f;
                            c.Line(64f + side * 8f, 64f - f * 4f, 64f + side * (8f + Mathf.Cos(angle) * length), 64f - f * 4f + Mathf.Sin(angle) * length, 9f - f);
                        }
                    break;
                case Icon.Speech:
                    c.Ellipse(64f, 72f, 50f, 34f).Triangle(36f, 48f, 58f, 46f, 26f, 18f);
                    foreach (float y in new[] { 84f, 72f, 60f })
                        c.Line(42f, y, y == 60f ? 70f : 86f, y, 6f, true);
                    break;
                case Icon.Lightning:
                    c.Triangle(78f, 122f, 34f, 58f, 70f, 60f).Triangle(54f, 72f, 94f, 70f, 46f, 6f);
                    break;
                case Icon.Microphone:
                    // Holder (lower half of a ring) first, then the head on top of it.
                    c.RingShape(64f, 76f, 26f, 6f).Box(64f, 96f, 70f, 42f, 0f, 0f, true);
                    c.Line(64f, 72f, 64f, 98f, 28f);
                    foreach (float y in new[] { 94f, 86f, 78f })
                        c.Line(54f, y, 74f, y, 2.5f, true);
                    c.Line(64f, 50f, 64f, 26f, 6f).Box(64f, 22f, 40f, 7f, 0f, 3f);
                    break;
                case Icon.Cone:
                    c.Triangle(64f, 116f, 32f, 28f, 96f, 28f).Box(64f, 22f, 88f, 11f, 0f, 3f);
                    c.Box(64f, 54f, 90f, 10f, 0f, 0f, true).Box(64f, 82f, 90f, 8f, 0f, 0f, true);
                    break;
                case Icon.Footsteps:
                    Footprint(c, 46f, 40f, 12f);
                    Footprint(c, 82f, 84f, 12f);
                    break;
                case Icon.HardHat:
                    c.Disc(64f, 52f, 36f).Box(64f, 30f, 80f, 44f, 0f, 0f, true).Box(64f, 52f, 100f, 11f, 0f, 4f).Box(64f, 74f, 9f, 32f, 0f, 3f, true).Box(64f, 76f, 4f, 30f, 0f, 1f);
                    break;
                case Icon.Fist:
                    c.Box(64f, 66f, 60f, 48f, 0f, 14f).Box(64f, 30f, 36f, 28f, 0f, 6f).Line(36f, 62f, 58f, 50f, 15f);
                    foreach (float x in new[] { 52f, 66f, 80f })
                        c.Line(x, 90f, x, 74f, 3f, true);
                    break;
                case Icon.Dodge:
                    foreach (float x in new[] { 40f, 74f })
                        c.Line(x, 98f, x + 26f, 64f, 13f).Line(x + 26f, 64f, x, 30f, 13f);
                    break;
                case Icon.Jump:
                    c.Triangle(64f, 116f, 30f, 76f, 98f, 76f).Box(64f, 54f, 22f, 46f, 0f, 3f).Box(64f, 16f, 80f, 8f, 0f, 3f);
                    break;
                case Icon.Seat:
                    c.Box(64f, 56f, 64f, 12f, 0f, 3f).Box(40f, 86f, 12f, 66f, 0f, 3f).Box(40f, 30f, 10f, 40f, 0f, 2f).Box(88f, 30f, 10f, 40f, 0f, 2f).Box(64f, 108f, 40f, 10f, 0f, 4f);
                    break;
                case Icon.Circle:
                    c.Disc(64f, 64f, 62f);
                    break;
                case Icon.Ring:
                    c.RingShape(64f, 64f, 59f, 6f);
                    break;
            }
            return c;
        }

        // Kerbau: long face, broad muzzle, ears to the side and wide horns sweeping out and up
        // (the swept-back crescent of the Indonesian water buffalo).
        private static void KerbauHead(Canvas c, float cx, float cy, float s)
        {
            c.Ellipse(cx, cy + 4f * s, 16f * s, 25f * s).Ellipse(cx, cy - 17f * s, 15f * s, 11f * s);
            // Horn: a thick crescent sweeping far out and back, tips curling inward.
            float[] hx = { 10f, 28f, 44f, 54f, 56f, 50f };
            float[] hy = { 26f, 30f, 34f, 42f, 54f, 62f };
            float[] hw = { 13f, 12f, 10f, 8f, 6f, 4f };
            foreach (int side in new[] { -1, 1 })
            {
                c.Ellipse(cx + side * 24f * s, cy + 12f * s, 11f * s, 5f * s, side * 25f);
                for (int i = 0; i < hx.Length - 1; i++)
                    c.Line(cx + side * hx[i] * s, cy + hy[i] * s, cx + side * hx[i + 1] * s, cy + hy[i + 1] * s, hw[i] * s);
                c.Disc(cx + side * 7f * s, cy + 10f * s, 3f * s, true);
                c.Disc(cx + side * 6f * s, cy - 20f * s, 2.6f * s, true);
            }
        }

        private static Vector2 Leap(float t) =>
            new Vector2(Mathf.Lerp(18f, 94f, t), 30f + 72f * 4f * t * (1f - t) * .9f);

        private static void Footprint(Canvas c, float x, float y, float rotation)
        {
            c.Ellipse(x, y, 11f, 20f, rotation).Ellipse(x - 3f, y - 24f, 8f, 9f, rotation);
            for (int i = 0; i < 4; i++)
                c.Disc(x - 9f + i * 6f + rotation * .1f, y + 26f - Mathf.Abs(i - 1.5f) * 2f, 3.4f - i * .3f);
        }

        // Unity texture with the white silhouette in its alpha channel.
        public static Texture2D ToTexture(Canvas canvas, string name)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(canvas.alpha[i] * 255f));
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        public static Sprite ToSprite(Icon icon)
        {
            Texture2D texture = ToTexture(Paint(icon), "Ikon " + icon);
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(.5f, .5f), 100f);
        }
    }
}
