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
        private int appliedHero = -1;
        private int appliedTeam = -99;
        private float nextRefresh;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            heroKit = GetComponent<NetworkHeroKit>();
            ApplyPresentation(true);
        }

        private void Update()
        {
            if (!IsSpawned || Time.unscaledTime < nextRefresh)
                return;

            nextRefresh = Time.unscaledTime + 0.20f;
            ApplyPresentation(false);
        }

        private void ApplyPresentation(bool force)
        {
            heroKit ??= GetComponent<NetworkHeroKit>();

            int hero = heroKit != null ? (int)heroKit.Hero : 0;
            int team = NetworkTeamUtility.GetTeam(NetworkObject);

            if (force || hero != appliedHero)
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

                ApplyBodySilhouette((PrototypeHero)Mathf.Clamp(hero, 0, 3));
            }

            if (force || team != appliedTeam)
            {
                appliedTeam = team;
                ApplyTeamRing(team);
            }
        }

        private void ApplyBodySilhouette(PrototypeHero hero)
        {
            if (bodyTransform == null)
                return;

            switch (hero)
            {
                case PrototypeHero.Mega:
                    bodyTransform.localScale = new Vector3(1.08f, 1.00f, 1.08f);
                    break;
                case PrototypeHero.Prabowo:
                    bodyTransform.localScale = new Vector3(1.18f, 1.03f, 1.18f);
                    break;
                case PrototypeHero.Abah:
                    bodyTransform.localScale = new Vector3(0.92f, 1.03f, 0.92f);
                    break;
                case PrototypeHero.Jokowi:
                    bodyTransform.localScale = new Vector3(0.97f, 1.00f, 0.97f);
                    break;
            }
        }

        private void ApplyTeamRing(int team)
        {
            if (teamRingRenderer == null)
                return;

            Color color = NetworkTeamUtility.GetTeamColor(team);
            Material material = teamRingRenderer.material;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }
}
