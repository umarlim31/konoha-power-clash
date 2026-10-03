using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Konoha.Editor
{
    // 0.6.3 KARIER night: a warm glow (unlit sphere, no realtime light) at the head of every
    // PJU street light and taman lentera, under one root that KarierDayNight switches on after
    // dark. Works with the code-built lamps and with the owner's models (LampuPJU, Lentera):
    // the head is found from the drawn shape of each lamp, not from fixed numbers.
    internal sealed partial class CampaignCapitalArt
    {
        internal GameObject BuildLampGlows()
        {
            var glows = new GameObject("KarierLampuMalam");
            Material warm = CampaignRigBuilder.Unlit("KarierLampuMalam", new Color(1f, .86f, .52f));
            int count = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string name = t.name;
                bool pju = name == "Kampung PJU" || name == "Model LampuPJU";
                bool lentera = name == "Model Lentera" || name == "Lentera kaca";
                if (!pju && !lentera)
                    continue;
                if (!DrawnBounds(t, out Bounds drawn))
                    continue;
                Vector3 head;
                if (pju)
                {
                    // Pole at the group origin, arm towards the drawn centre: the head is at the far end.
                    Vector3 pole = t.position;
                    Vector3 reach = drawn.center - pole;
                    reach.y = 0f;
                    head = pole + reach * 1.8f;
                    head.y = drawn.max.y - .25f;
                }
                else
                {
                    head = name == "Lentera kaca" ? drawn.center : new Vector3(drawn.center.x, drawn.max.y - .35f, drawn.center.z);
                }
                var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(glow.GetComponent<Collider>());
                glow.name = "Karier lampu menyala";
                glow.transform.SetParent(glows.transform, false);
                glow.transform.position = head;
                glow.transform.localScale = Vector3.one * (pju ? .42f : .3f);
                var renderer = glow.GetComponent<Renderer>();
                renderer.sharedMaterial = warm;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                count++;
            }
            glows.SetActive(false);
            Debug.Log("KARIER lampu malam: " + count + " lampu.");
            return glows;
        }

        // 0.6.4: every drawn roof of the capital (code roofs too), for the interior camera rooms.
        internal List<Renderer> RoofRenderers()
        {
            var result = new List<Renderer>();
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (renderer.GetComponent<TextMesh>() != null)
                    continue;
                string lower = renderer.name.ToLowerInvariant();
                if (lower.Contains("atap") || lower.Contains("genteng") || lower.Contains("roof") || lower.EndsWith(" atas"))
                    result.Add(renderer);
            }
            return result;
        }

        private static bool DrawnBounds(Transform t, out Bounds bounds)
        {
            bounds = new Bounds();
            bool any = false;
            foreach (Renderer renderer in t.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer.GetComponent<TextMesh>() != null)
                    continue;
                if (!any) { bounds = renderer.bounds; any = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return any;
        }
    }
}
