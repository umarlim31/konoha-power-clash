using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    // Mode rules consulted by the shared hero combat components (NetworkPlayerCombat,
    // NetworkHeroKit, NetworkPlayerMovement). Rebut Kursi PvP: NetworkMatchManager, with
    // exactly the behaviour it had before this interface existed. Jalur Takhta:
    // Konoha.Campaign.CampaignDirector. Exactly one provider is registered at a time.
    public interface ICombatRules
    {
        bool AllowsGameplay { get; }
        bool CanSelectHero { get; }
        bool IsRuler(ulong clientId);
        bool IsRuler(NetworkObject actor);
        int GetHumanTeam(ulong clientId);

        // Server only. The actor's Wibawa reached zero. Return false when the actor must
        // stay down (the provider removes it); true starts the normal respawn.
        bool ServerOnActorKnockedOut(NetworkObject actor, NetworkObject attacker);
        float GetRespawnDelay(NetworkObject actor, float defaultDelay);
        // Evaluated on the owning peer when its respawn ticket changes.
        Vector3 GetRespawnPosition(NetworkObject actor);
        Quaternion GetRespawnRotation(NetworkObject actor);
        // Server only, after Wibawa has been restored.
        void ServerOnActorRespawned(NetworkObject actor);
    }

    public static class CombatRules
    {
        private static ICombatRules current;

        // A destroyed provider (scene change, shutdown) reads as none.
        public static ICombatRules Current
        {
            get
            {
                if (current is Object unityObject && unityObject == null)
                    current = null;
                return current;
            }
        }

        public static void Register(ICombatRules rules)
        {
            current = rules;
        }

        public static void Unregister(ICombatRules rules)
        {
            if (ReferenceEquals(current, rules))
                current = null;
        }
    }

    // Marks server-driven actors (PvP bots, campaign enemies). They never bind the local
    // HUD, and their damage is credited to the actor object, not to a client.
    public interface INetworkAiActor
    {
        int Team { get; }
    }
}
