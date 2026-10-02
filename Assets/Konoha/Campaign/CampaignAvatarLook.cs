using UnityEngine;

namespace Konoha.Campaign
{
    // 0.6.0 KARIER: the player's own citizen. Lives on the avatar body template built by
    // CampaignRigBuilder.AvatarTemplate; CampaignBodies copies the template onto the local
    // hero and calls Apply whenever the character creator changes something. Skin and shirt
    // colours use one material copy each (no per-frame property blocks).
    public sealed class CampaignAvatarLook : MonoBehaviour
    {
        public Renderer[] skin = new Renderer[0];
        public Renderer[] shirt = new Renderer[0];
        public GameObject hairShort, hairLong, peci, jilbab, topi, rok, perut;

        // Order matches KarierAvatar.SkinNames / ShirtNames.
        public static readonly Color[] SkinColors =
        {
            new Color(.80f, .62f, .47f), new Color(.62f, .43f, .30f), new Color(.44f, .29f, .19f), new Color(.88f, .72f, .56f)
        };
        public static readonly Color[] ShirtColors =
        {
            new Color(.93f, .92f, .88f), new Color(.10f, .10f, .12f), new Color(.66f, .12f, .12f), new Color(.16f, .30f, .62f),
            new Color(.14f, .48f, .26f), new Color(.92f, .74f, .18f), new Color(.48f, .49f, .50f)
        };

        private Material skinMaterial, shirtMaterial;
        private Vector3 baseScale;
        private bool hasBaseScale;

        public void Apply(KarierAvatar look)
        {
            look = look.Clamped();
            if (!hasBaseScale)
            {
                baseScale = transform.localScale;
                hasBaseScale = true;
            }
            skinMaterial = Recolour(skin, skinMaterial, SkinColors[look.Skin % SkinColors.Length]);
            shirtMaterial = Recolour(shirt, shirtMaterial, ShirtColors[look.Shirt % ShirtColors.Length]);

            // Short hair also sits under the peci and the topi; the jilbab hides all hair.
            Toggle(hairShort, look.Hair == 0 || look.Hair == 2 || look.Hair == 4);
            Toggle(hairLong, look.Hair == 1);
            Toggle(peci, look.Hair == 2);
            Toggle(jilbab, look.Hair == 3);
            Toggle(topi, look.Hair == 4);
            Toggle(rok, look.Female);
            Toggle(perut, look.Body == 2);

            float width = look.Body == 0 ? 0.92f : look.Body == 2 ? 1.08f : 1f;
            float height = look.Female ? 0.96f : 1f;
            transform.localScale = new Vector3(baseScale.x * width, baseScale.y * height, baseScale.z * width);
        }

        private static Material Recolour(Renderer[] renderers, Material copy, Color color)
        {
            if (renderers == null || renderers.Length == 0)
                return copy;
            if (copy == null)
            {
                Renderer first = null;
                foreach (Renderer renderer in renderers)
                    if (renderer != null) { first = renderer; break; }
                if (first == null || first.sharedMaterial == null)
                    return null;
                copy = new Material(first.sharedMaterial) { name = first.sharedMaterial.name + " (avatar)" };
                foreach (Renderer renderer in renderers)
                    if (renderer != null) renderer.sharedMaterial = copy;
            }
            copy.SetColor("_BaseColor", color);
            return copy;
        }

        private static void Toggle(GameObject target, bool value)
        {
            if (target != null && target.activeSelf != value)
                target.SetActive(value);
        }

        private void OnDestroy()
        {
            if (skinMaterial != null) Destroy(skinMaterial);
            if (shirtMaterial != null) Destroy(shirtMaterial);
        }
    }
}
