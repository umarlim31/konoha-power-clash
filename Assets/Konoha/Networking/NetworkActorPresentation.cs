using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    public sealed class NetworkActorPresentation : NetworkBehaviour
    {
        public Transform bodyTransform;
        public Renderer teamRingRenderer;
        public GameObject[] heroVisuals;

        private NetworkHeroKit heroKit;
        private NetworkPlayerCombat combat;
        private int appliedHero = -1;
        private int appliedTeam = -99;
        private bool appliedKnockedOut;
        private float nextRefresh;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            heroKit = GetComponent<NetworkHeroKit>();
            combat = GetComponent<NetworkPlayerCombat>();
            ApplyPresentation(true);
        }

        private void Update()
        {
            if (!IsSpawned || Time.unscaledTime < nextRefresh)
                return;

            nextRefresh = Time.unscaledTime + 0.12f;
            ApplyPresentation(false);
        }

        private void ApplyPresentation(bool force)
        {
            heroKit ??= GetComponent<NetworkHeroKit>();
            combat ??= GetComponent<NetworkPlayerCombat>();

            int hero = heroKit != null ? (int)heroKit.Hero : 0;
            int team = NetworkTeamUtility.GetTeam(NetworkObject);
            bool knockedOut = combat != null && combat.IsKnockedOut;

            bool heroChanged = hero != appliedHero;
            bool teamChanged = team != appliedTeam;
            bool knockoutChanged = knockedOut != appliedKnockedOut;

            if (force || heroChanged)
            {
                appliedHero = hero;

                if (heroVisuals != null)
                {
                    for (int i = 0; i < heroVisuals.Length; i++)
                    {
                        if (heroVisuals[i] != null)
                            heroVisuals[i].SetActive(i == hero);
                    }
                }

            }

            if (force || heroChanged || knockoutChanged)
                ApplyBodySilhouette((PrototypeHero)Mathf.Clamp(hero, 0, 3), knockedOut);

            if (force || teamChanged || knockoutChanged)
            {
                appliedTeam = team;
                appliedKnockedOut = knockedOut;
                ApplyTeamRing(team, knockedOut);
            }

            if (force || heroChanged || knockoutChanged)
                ApplyHeroAccessoryColor((PrototypeHero)Mathf.Clamp(hero, 0, 3), knockedOut);
        }

        private void ApplyBodySilhouette(PrototypeHero hero, bool knockedOut)
        {
            if (bodyTransform == null)
                return;

            switch (hero)
            {
                case PrototypeHero.Mega:
                    bodyTransform.localScale = new Vector3(1.05f, 1.00f, 1.05f);
                    break;
                case PrototypeHero.Prabowo:
                    bodyTransform.localScale = new Vector3(1.12f, 1.02f, 1.12f);
                    break;
                case PrototypeHero.Abah:
                    bodyTransform.localScale = new Vector3(0.94f, 1.02f, 0.94f);
                    break;
                case PrototypeHero.Jokowi:
                    bodyTransform.localScale = new Vector3(0.98f, 1.00f, 0.98f);
                    break;
            }
            // In the recorded build every fighter kept the same pale cylinder, making
            // the ornaments disappear at match camera distance. Keep fabric colours
            // distinct while the team allegiance remains on the ground ring.
            Renderer body = bodyTransform.GetComponent<Renderer>();
            if (body == null) return;
            Color fabric = hero == PrototypeHero.Mega ? new Color(0.19f, 0.08f, 0.11f) :
                hero == PrototypeHero.Prabowo ? new Color(0.82f, 0.75f, 0.65f) :
                hero == PrototypeHero.Abah ? new Color(0.08f, 0.29f, 0.23f) :
                new Color(0.84f, 0.77f, 0.67f);
            if (knockedOut) fabric = Color.Lerp(fabric, Color.black, 0.72f);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", fabric);
            body.SetPropertyBlock(block);
        }

        private void ApplyHeroAccessoryColor(PrototypeHero hero, bool knockedOut)
        {
            if (heroVisuals == null)
                return;

            int index = Mathf.Clamp((int)hero, 0, heroVisuals.Length - 1);
            GameObject root = heroVisuals[index];

            if (root == null)
                return;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                // Keep each generated material's original palette. Recoloring all pieces
                // with the hero accent turned fabric, metal and ornaments into one flat hue.
                Material source = renderer.sharedMaterial;
                if (source == null) continue;
                string property = source.HasProperty("_BaseColor") ? "_BaseColor" :
                    source.HasProperty("_Color") ? "_Color" : null;
                if (property == null) continue;
                Color original = source.GetColor(property);
                Color target = knockedOut ? Color.Lerp(original, Color.black, 0.72f) : original;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor(property, target);
                renderer.SetPropertyBlock(block);
            }
        }

        private void ApplyTeamRing(int team, bool knockedOut)
        {
            if (teamRingRenderer == null)
                return;

            Color color = NetworkTeamUtility.GetTeamColor(team);
            if (knockedOut)
                color = Color.Lerp(color, Color.black, 0.78f);
            else
                color = Color.Lerp(color, Color.black, 0.18f);

            SetRendererColor(teamRingRenderer, color);
        }

        private static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            Material material = renderer.material;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }
}
