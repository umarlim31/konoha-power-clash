using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    public static class NetworkTeamUtility
    {
        public const int CyanTeam = 0;
        public const int OrangeTeam = 1;
        // Jalur Takhta enemies. Never used by Rebut Kursi.
        public const int SistemTeam = 2;

        private static readonly Vector3[] CyanFormation =
        {
            new Vector3(-10.5f, 0.1f, -3.2f),
            new Vector3(-8.2f, 0.1f, -1.1f),
            new Vector3(-8.2f, 0.1f, 1.1f),
            new Vector3(-10.5f, 0.1f, 3.2f)
        };

        private static readonly Vector3[] OrangeFormation =
        {
            new Vector3(10.5f, 0.1f, 3.2f),
            new Vector3(8.2f, 0.1f, 1.1f),
            new Vector3(8.2f, 0.1f, -1.1f),
            new Vector3(10.5f, 0.1f, -3.2f)
        };

        // The active mode decides human teams (PvP alternates; the campaign unites them).
        // Without a registered mode the original PvP alternation applies.
        public static int GetTeam(ulong clientId)
        {
            ICombatRules rules = CombatRules.Current;
            return rules != null ? rules.GetHumanTeam(clientId) : GetPvpTeam(clientId);
        }

        public static int GetPvpTeam(ulong clientId)
        {
            return (int)(clientId % 2UL);
        }

        public static int GetTeam(NetworkObject networkObject)
        {
            if (networkObject == null)
                return -1;

            // TryGetComponent: an interface lookup must not see the editor's fake-null object.
            if (networkObject.TryGetComponent(out INetworkAiActor ai))
                return ai.Team;

            return GetTeam(networkObject.OwnerClientId);
        }

        // Server-driven actor (PvP bot or campaign enemy), as opposed to a human's hero.
        public static bool IsAiActor(Component actor)
        {
            return actor != null && actor.TryGetComponent(out INetworkAiActor _);
        }

        public static bool IsCombatActor(NetworkObject networkObject)
        {
            return networkObject != null &&
                   networkObject.GetComponent<NetworkPlayerCombat>() != null &&
                   (!networkObject.TryGetComponent(out ICombatActorState state) || state.IsTargetable);
        }

        public static int GetHumanSlot(ulong clientId)
        {
            return (int)(clientId / 2UL) % 4;
        }

        public static string GetTeamName(int team)
        {
            return team == CyanTeam ? "CYAN" : team == OrangeTeam ? "ORANGE" : team == SistemTeam ? "SISTEM" : "NEUTRAL";
        }

        public static Color GetTeamColor(int team)
        {
            return team == CyanTeam
                ? new Color(0.15f, 0.90f, 0.80f)
                : team == OrangeTeam
                    ? new Color(1.00f, 0.55f, 0.18f)
                    : team == SistemTeam
                        ? new Color(0.78f, 0.16f, 0.20f)
                        : new Color(0.72f, 0.78f, 0.84f);
        }

        public static Vector3 GetSpawnPosition(ulong clientId)
        {
            return GetTeamSpawnPosition(GetTeam(clientId), GetHumanSlot(clientId));
        }

        public static Vector3 GetSpawnPosition(NetworkObject networkObject)
        {
            if (networkObject == null)
                return Vector3.zero;

            NetworkBotController bot = networkObject.GetComponent<NetworkBotController>();
            if (bot != null)
                return GetTeamSpawnPosition(bot.Team, bot.Slot);

            return GetSpawnPosition(networkObject.OwnerClientId);
        }

        public static Quaternion GetSpawnRotation(ulong clientId)
        {
            return GetTeamSpawnRotation(GetTeam(clientId));
        }

        public static Quaternion GetSpawnRotation(NetworkObject networkObject)
        {
            if (networkObject == null)
                return Quaternion.identity;

            return GetTeamSpawnRotation(GetTeam(networkObject));
        }

        public static Vector3 GetTeamSpawnPosition(int team, int slot)
        {
            slot = Mathf.Clamp(slot, 0, 3);

            if (team == CyanTeam)
                return CyanFormation[slot];

            if (team == OrangeTeam)
                return OrangeFormation[slot];

            return Vector3.zero;
        }

        public static Quaternion GetTeamSpawnRotation(int team)
        {
            if (team == CyanTeam)
                return Quaternion.LookRotation(Vector3.right);

            if (team == OrangeTeam)
                return Quaternion.LookRotation(Vector3.left);

            return Quaternion.identity;
        }
    }
}
