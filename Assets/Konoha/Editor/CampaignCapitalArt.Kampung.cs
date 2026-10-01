using UnityEngine;

namespace Konoha.Editor
{
    // 0.2.8: everyday Indonesian street furniture that the close street camera now shows at
    // eye level: gapura kampung, becak waiting at the boulevard mouth, banana trees between
    // the kampung houses and PJU street lights along the roads. Static, no colliders, all
    // outside the walking route. Names start with "Kampung".
    internal sealed partial class CampaignCapitalArt
    {
        private void BuildKampung()
        {
            Material merah = flagRed, putih = flagWhite;
            foreach (int side in new[] { -1, 1 })
            {
                GapuraKampung(new Vector3(side * 48f, 0f, -42.5f), side, side < 0 ? "GANG MERDEKA\nRT 03 / RW 07" : "GANG GOTONG ROYONG\nRT 05 / RW 07", merah, putih);
                Becak(new Vector3(side * 9.2f, 0f, -52.7f), side > 0 ? -90f : 90f, side > 0 ? flagBlue : flagGreen);
                for (int k = 1; k < 10; k += 2)
                    Pisang(new Vector3(side * 51f, 0f, -42.5f + k * 11f), k * 37f);
                // PJU along the side roads, on the verge between the capital and the road.
                for (float z = -40f; z <= 60f; z += 20f)
                    Pju(new Vector3(side * 36.2f, 0f, z), side > 0 ? -90f : 90f); // arm (local -z) over the road
                // 0.3.0: ketapang shade trees on the verge between the PJU poles (outside the route).
                foreach (float z in new[] { -30f, 30f, 50f })
                    Ketapang(new Vector3(side * 33.8f, 0f, z), z * 11f + side * 25f);
            }
            // PJU along the near side of the jalan raya, clear of the candi bentar.
            for (float x = -90f; x <= 90f; x += 18f)
                if (Mathf.Abs(x) > 14f)
                    Pju(new Vector3(x, 0f, -54.9f), 0f); // arm (local -z) over the road to the south
        }

        // Village gate over a gang: two painted pillars, a beam with the gang name.
        private void GapuraKampung(Vector3 p, int side, string text, Material red, Material white)
        {
            // Faces the side road (towards the capital): local -z points to -x on the east side.
            int first = root.childCount;
            var gate = Group("Kampung gapura", p, side > 0 ? 90f : -90f);
            foreach (int s in new[] { -1, 1 })
            {
                LocalPart(gate, "Kampung gapura tiang putih", PrimitiveType.Cube, new Vector3(s * 1.7f, .9f, 0f), new Vector3(.55f, 1.8f, .55f), white);
                LocalPart(gate, "Kampung gapura tiang merah", PrimitiveType.Cube, new Vector3(s * 1.7f, 2.6f, 0f), new Vector3(.55f, 1.6f, .55f), red);
                LocalPart(gate, "Kampung gapura kepala tiang", PrimitiveType.Cube, new Vector3(s * 1.7f, 3.5f, 0f), new Vector3(.7f, .2f, .7f), white);
            }
            LocalPart(gate, "Kampung gapura balok", PrimitiveType.Cube, new Vector3(0f, 3.85f, 0f), new Vector3(4.2f, .8f, .35f), white);
            LocalPart(gate, "Kampung gapura list", PrimitiveType.Cube, new Vector3(0f, 4.3f, 0f), new Vector3(4.4f, .12f, .4f), red);
            Label(gate, text, new Vector3(0f, 3.85f, -.2f), .22f, new Color(.6f, .06f, .05f));
            CampaignModelSlots.Apply("GapuraKampung", root, first, p, side > 0 ? 90f : -90f);
        }

        // Becak: the passenger seat in front under a folding hood, the driver pedals behind.
        private void Becak(Vector3 p, float yaw, Material paint)
        {
            int first = root.childCount;
            var becak = Group("Kampung becak", p, yaw);
            LocalPart(becak, "Kampung becak kursi", PrimitiveType.Cube, new Vector3(0f, .75f, .45f), new Vector3(1f, .5f, .7f), paint);
            LocalPart(becak, "Kampung becak sandaran", PrimitiveType.Cube, new Vector3(0f, 1.1f, .1f), new Vector3(1f, .7f, .12f), paint);
            LocalPart(becak, "Kampung becak jok", PrimitiveType.Cube, new Vector3(0f, 1.02f, .45f), new Vector3(.9f, .06f, .6f), flagRed);
            LocalPart(becak, "Kampung becak kap", PrimitiveType.Cube, new Vector3(0f, 1.75f, .3f), new Vector3(1.05f, .06f, .8f), rubber)
                .transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            foreach (int s in new[] { -1, 1 })
            {
                LocalPart(becak, "Kampung becak rangka kap", PrimitiveType.Cube, new Vector3(s * .5f, 1.45f, .05f), new Vector3(.04f, .6f, .04f), chrome);
                LocalPart(becak, "Kampung becak roda", PrimitiveType.Cylinder, new Vector3(s * .58f, .35f, .45f), new Vector3(.7f, .03f, .7f), rubber)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            LocalPart(becak, "Kampung becak rangka", PrimitiveType.Cube, new Vector3(0f, .55f, -.45f), new Vector3(.08f, .08f, 1.1f), chrome);
            LocalPart(becak, "Kampung becak sadel", PrimitiveType.Cube, new Vector3(0f, 1.1f, -.8f), new Vector3(.25f, .08f, .35f), rubber);
            LocalPart(becak, "Kampung becak tiang sadel", PrimitiveType.Cube, new Vector3(0f, .8f, -.8f), new Vector3(.05f, .6f, .05f), chrome);
            LocalPart(becak, "Kampung becak roda belakang", PrimitiveType.Cylinder, new Vector3(0f, .35f, -1f), new Vector3(.7f, .03f, .7f), rubber)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            LocalPart(becak, "Kampung becak pijakan", PrimitiveType.Cube, new Vector3(0f, .38f, .95f), new Vector3(.9f, .05f, .3f), paint);
            CampaignModelSlots.Apply("Becak", root, first, p, yaw);
        }

        // Banana tree: a soft green trunk and long drooping leaves.
        private void Pisang(Vector3 p, float twist)
        {
            int first = root.childCount;
            Cylinder("Kampung pisang batang", p + Vector3.up * 1.3f, new Vector3(.22f, 1.3f, .22f), canopyLight);
            for (int i = 0; i < 7; i++)
            {
                float yaw = twist + i * 51f;
                var leaf = Block("Kampung pisang daun", p + Vector3.up * 2.55f + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * .75f,
                    new Vector3(.5f, .03f, 1.7f), i % 2 == 0 ? canopy : canopyLight);
                leaf.transform.rotation = Quaternion.Euler(28f + (i % 3) * 8f, yaw, 0f);
            }
            CampaignModelSlots.Apply("PohonPisang", root, first, p, twist);
        }

        // PJU street light: steel pole with a curved arm over the road and a lamp head.
        private void Pju(Vector3 p, float yaw)
        {
            int first = root.childCount;
            var lamp = Group("Kampung PJU", p, yaw);
            LocalPart(lamp, "Kampung PJU tiang", PrimitiveType.Cylinder, new Vector3(0f, 3.6f, 0f), new Vector3(.14f, 3.6f, .14f), steel);
            LocalPart(lamp, "Kampung PJU lengan", PrimitiveType.Cube, new Vector3(0f, 7.25f, -.8f), new Vector3(.08f, .08f, 1.7f), steel)
                .transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            LocalPart(lamp, "Kampung PJU lampu", PrimitiveType.Cube, new Vector3(0f, 7.35f, -1.65f), new Vector3(.3f, .12f, .6f), steel);
            LocalPart(lamp, "Kampung PJU kaca", PrimitiveType.Cube, new Vector3(0f, 7.28f, -1.65f), new Vector3(.24f, .03f, .5f), porcelain);
            CampaignModelSlots.Apply("LampuPJU", root, first, p, yaw);
        }

        private Material ketapangRed;

        // Ketapang (Terminalia catappa): a straight trunk with flat, layered tiers of leaves,
        // a few of them turning red. The typical shade tree of Indonesian roads and schoolyards.
        private void Ketapang(Vector3 p, float twist)
        {
            if (ketapangRed == null)
            {
                ketapangRed = Surface("KetapangDaunTua", new Color(.58f, .22f, .10f), .1f);
                UnityEditor.EditorUtility.SetDirty(ketapangRed);
            }
            int first = root.childCount;
            Cylinder("NusantaraTall batang ketapang", p + Vector3.up * 3.1f, new Vector3(.32f, 3.1f, .32f), bark);
            float[] heights = { 3.1f, 4.1f, 5.0f, 5.8f };
            float[] reach = { 2.5f, 2.0f, 1.4f, .7f };
            var random = new System.Random(Mathf.RoundToInt(p.x * 17f + p.z * 5f));
            for (int tier = 0; tier < heights.Length; tier++)
            {
                int clumps = tier == heights.Length - 1 ? 1 : 4;
                for (int i = 0; i < clumps; i++)
                {
                    float angle = twist + tier * 45f + i * 90f;
                    var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    float radius = clumps == 1 ? 0f : reach[tier] * (.6f + (float)random.NextDouble() * .25f);
                    if (tier < 2 && clumps > 1)
                        Stick("NusantaraTall dahan ketapang", p + Vector3.up * (heights[tier] - .25f), p + Vector3.up * heights[tier] + direction * radius, .08f, bark);
                    float size = reach[tier] * (clumps == 1 ? 1.6f : 1.05f);
                    Material leaves = tier == 1 && i == 2 ? ketapangRed : ((tier + i) % 2 == 0 ? canopy : canopyLight);
                    Ellipsoid("NusantaraTall tajuk ketapang", p + Vector3.up * heights[tier] + direction * radius,
                        new Vector3(size, .42f, size), leaves);
                }
            }
            CampaignModelSlots.Apply("PohonKetapang", root, first, p, twist);
        }
    }
}
