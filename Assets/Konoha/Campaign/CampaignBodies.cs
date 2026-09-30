using System.Collections.Generic;
using Konoha.Character;
using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;

namespace Konoha.Campaign
{
    // 0.2.3: gives every hero in Jalur Takhta a human body and wires the combat "feel".
    //  - Heroes: a copy of the matching body template (MEGA, GEMOY, ABAH, PAK WI) is attached
    //    to the shared hero prefab at runtime, and the prototype capsule/ornaments are hidden.
    //    A rigged FBX hero (HeroVisualCatalog) keeps priority and gets no procedural body.
    //    The shared hero prefab is NOT changed, so PvP looks exactly as before.
    //  - Punches, hits, flinches, hit-stop, camera shake and skill effects are driven from
    //    the presentation hooks of the shared combat code (no listeners in PvP).
    public sealed class CampaignBodies : MonoBehaviour
    {
        // Index = PrototypeHero (Mega, Prabowo/GEMOY, Abah, Jokowi/PAK WI). Inactive in the scene.
        public CampaignHumanoid[] heroTemplates = new CampaignHumanoid[4];
        public CampaignCombatFx fx;

        private sealed class HeroBody
        {
            public CampaignHumanoid rig;
            public int hero = -1;
            public NetworkPlayerCombat combat;
            public readonly List<Renderer> hidden = new List<Renderer>();
        }

        private sealed class RecentAttack
        {
            public Transform attacker;
            public float time;
        }

        private readonly Dictionary<NetworkHeroKit, HeroBody> heroes = new Dictionary<NetworkHeroKit, HeroBody>();
        private readonly Dictionary<NetworkPlayerCombat, RecentAttack> recent = new Dictionary<NetworkPlayerCombat, RecentAttack>();
        private readonly List<NetworkHeroKit> stale = new List<NetworkHeroKit>();
        private float nextScan;

        private void OnEnable()
        {
            NetworkHeroKit.LegacyAbilityFx = false;
            NetworkPlayerCombat.DamageFeedbackPlayed += OnDamage;
            NetworkPlayerCombat.BasicAttackResolved += OnBasicAttack;
            NetworkHeroKit.AbilityCast += OnAbility;
            CampaignEnemy.AttackLaunched += OnEnemyAttack;
        }

        private void OnDisable()
        {
            // PvP (loaded after this scene) keeps its original prototype effects.
            NetworkHeroKit.LegacyAbilityFx = true;
            NetworkPlayerCombat.DamageFeedbackPlayed -= OnDamage;
            NetworkPlayerCombat.BasicAttackResolved -= OnBasicAttack;
            NetworkHeroKit.AbilityCast -= OnAbility;
            CampaignEnemy.AttackLaunched -= OnEnemyAttack;
        }

        // LateUpdate: after NetworkActorPresentation (Update) may have re-enabled the capsule.
        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + .3f;
                Scan();
            }

            foreach (KeyValuePair<NetworkHeroKit, HeroBody> pair in heroes)
            {
                HeroBody body = pair.Value;
                // NetworkActorPresentation re-enables the capsule on hero/knockout changes.
                foreach (Renderer renderer in body.hidden)
                    if (renderer != null && renderer.enabled)
                        renderer.enabled = false;
                if (body.rig != null && body.combat != null)
                    body.rig.SetDown(body.combat.IsKnockedOut);
            }
        }

        // --- Hero bodies ------------------------------------------------------------------

        private void Scan()
        {
            stale.Clear();
            foreach (KeyValuePair<NetworkHeroKit, HeroBody> pair in heroes)
                if (pair.Key == null || !pair.Key.IsSpawned)
                    stale.Add(pair.Key);
            foreach (NetworkHeroKit kit in stale)
            {
                if (heroes.TryGetValue(kit, out HeroBody body) && body.rig != null)
                    Destroy(body.rig.gameObject);
                heroes.Remove(kit);
            }

            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.SpawnManager == null)
                return;
            foreach (NetworkObject networkObject in manager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null || networkObject.GetComponent<CampaignEnemy>() != null)
                    continue;
                NetworkHeroKit kit = networkObject.GetComponent<NetworkHeroKit>();
                if (kit != null)
                    Ensure(kit);
            }
        }

        private void Ensure(NetworkHeroKit kit)
        {
            int hero = Mathf.Clamp((int)kit.Hero, 0, 3);
            if (!heroes.TryGetValue(kit, out HeroBody body))
            {
                body = new HeroBody { combat = kit.GetComponent<NetworkPlayerCombat>() };
                heroes.Add(kit, body);
            }
            if (body.hero == hero)
                return;

            if (body.rig != null)
                Destroy(body.rig.gameObject);
            body.rig = null;
            body.hero = hero;
            // Show what the previous hero hid (an owner FBX hero must stay visible).
            foreach (Renderer renderer in body.hidden)
                if (renderer != null)
                    renderer.enabled = true;
            body.hidden.Clear();

            var presentation = kit.GetComponent<NetworkActorPresentation>();
            GameObject visual = presentation != null && presentation.heroVisuals != null && hero < presentation.heroVisuals.Length
                ? presentation.heroVisuals[hero] : null;
            if (HeroAnimatorDriver.UsesModel(visual))
                return; // The owner's rigged 3D hero stays in charge.

            CampaignHumanoid template = heroTemplates != null && hero < heroTemplates.Length ? heroTemplates[hero] : null;
            if (template == null)
                return;

            GameObject copy = Instantiate(template.gameObject, kit.transform, false);
            copy.name = "CampaignBody " + NetworkHeroKit.GetHeroName(kit.Hero);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            copy.SetActive(true);
            body.rig = copy.GetComponent<CampaignHumanoid>();
            body.rig.motionSource = kit.transform;

            if (presentation != null)
            {
                if (presentation.bodyTransform != null)
                    AddHidden(body, presentation.bodyTransform.GetComponent<Renderer>());
                // Only this hero's prototype ornaments (the others are inactive anyway).
                if (visual != null)
                    foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                        AddHidden(body, renderer);
            }
            Transform facing = kit.transform.Find("FacingMarker");
            if (facing != null)
                AddHidden(body, facing.GetComponent<Renderer>());
        }

        private static void AddHidden(HeroBody body, Renderer renderer)
        {
            if (renderer == null)
                return;
            renderer.enabled = false;
            body.hidden.Add(renderer);
        }

        private CampaignHumanoid Rig(Component actor)
        {
            if (actor == null)
                return null;
            CampaignEnemy enemy = actor.GetComponent<CampaignEnemy>();
            if (enemy != null)
                return enemy.body;
            NetworkHeroKit kit = actor.GetComponent<NetworkHeroKit>();
            return kit != null && heroes.TryGetValue(kit, out HeroBody body) ? body.rig : null;
        }

        private static bool IsLocalHero(Component actor)
        {
            NetworkManager manager = NetworkManager.Singleton;
            return actor != null && manager != null && manager.LocalClient != null &&
                   manager.LocalClient.PlayerObject != null && actor.gameObject == manager.LocalClient.PlayerObject.gameObject;
        }

        // --- Combat hooks -------------------------------------------------------------

        private void OnBasicAttack(NetworkPlayerCombat attacker, NetworkPlayerCombat target)
        {
            if (attacker == null)
                return;
            CampaignHumanoid rig = Rig(attacker);
            if (rig != null) rig.PlayAttack(.3f);
            NetworkHeroKit kit = attacker.GetComponent<NetworkHeroKit>();
            if (fx != null)
                fx.Slash(attacker.transform, kit != null ? CampaignCombatFx.HeroColor(kit.Hero) : Color.white, Random.value < .5f);
            Remember(target, attacker.transform);
        }

        private void OnEnemyAttack(CampaignEnemy enemy, NetworkPlayerCombat target)
        {
            if (enemy == null)
                return;
            if (enemy.body != null) enemy.body.PlayAttack(.34f);
            if (fx != null)
            {
                FactionColor accent = FactionDefinition.Get(enemy.Faction).AccentColor;
                fx.Slash(enemy.transform, new Color(accent.R, accent.G, accent.B), Random.value < .5f, 1.1f);
            }
            Remember(target, enemy.transform);
        }

        private void Remember(NetworkPlayerCombat target, Transform attacker)
        {
            if (target == null)
                return;
            if (!recent.TryGetValue(target, out RecentAttack entry))
            {
                entry = new RecentAttack();
                recent.Add(target, entry);
            }
            entry.attacker = attacker;
            entry.time = Time.time;
        }

        private void OnDamage(NetworkPlayerCombat target, int damage, int absorbed)
        {
            if (target == null)
                return;
            Vector3 chest = target.transform.position + Vector3.up * 1.25f;
            Transform attacker = recent.TryGetValue(target, out RecentAttack entry) && Time.time - entry.time < .4f
                ? entry.attacker : null;
            Vector3 from = attacker != null ? attacker.position : target.transform.position + target.transform.forward * 2f;
            Vector3 direction = CampaignCombatFx.Flat(chest - from);
            bool shieldOnly = damage <= 0 && absorbed > 0;
            float strength = Mathf.Clamp01((damage + absorbed) / 30f);

            CampaignHumanoid rig = Rig(target);
            if (rig != null)
            {
                rig.PlayHit(from);
                rig.Flash(shieldOnly ? CampaignCombatFx.ShieldColor : Color.white, .07f);
                rig.Freeze(.05f + .05f * strength);
            }
            CampaignHumanoid attackerRig = attacker != null ? Rig(attacker) : null;
            if (attackerRig != null) attackerRig.Freeze(.05f + .04f * strength);

            if (fx != null)
                fx.Impact(chest - direction * .3f, direction, shieldOnly ? CampaignCombatFx.ShieldColor : CampaignCombatFx.HitColor, strength);

            if (IsLocalHero(target))
                CampaignCameraShake.Shake(.25f + .45f * strength, .22f);
            else if (attacker != null && IsLocalHero(attacker))
                CampaignCameraShake.Shake(.12f + .2f * strength, .14f);
        }

        private void OnAbility(NetworkHeroKit kit, int slot, Vector3 position, Vector3 direction)
        {
            if (kit == null || kit.GetComponent<CampaignEnemy>() != null)
                return;
            CampaignHumanoid rig = Rig(kit);
            Vector3 from = rig != null ? rig.LastPosition : kit.transform.position;
            if (rig != null)
            {
                rig.PlaySkill(slot);
                if (slot == 1 && kit.Hero == PrototypeHero.Prabowo)
                    rig.ExpectTravel(CampaignHumanoid.Travel.Leap);
                else if ((slot == 1 && kit.Hero == PrototypeHero.Mega) ||
                         (slot == 2 && (kit.Hero == PrototypeHero.Abah || kit.Hero == PrototypeHero.Jokowi)))
                    rig.ExpectTravel(CampaignHumanoid.Travel.Dash);
            }
            if (fx != null)
                fx.Skill(kit, kit.Hero, slot, position, direction, from);
        }
    }
}
