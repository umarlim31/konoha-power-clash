using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    public static class NetworkTeamUtility
    {
        public const int CyanTeam = 0;
        public const int OrangeTeam = 1;

        public static int GetTeam(ulong clientId)
        {
            return (int)(clientId % 2UL);
        }

        public static int GetTeam(NetworkObject networkObject)
        {
            if (networkObject == null)
                return -1;

            NetworkBotController bot = networkObject.GetComponent<NetworkBotController>();
            if (bot != null)
                return bot.Team;

            return GetTeam(networkObject.OwnerClientId);
        }

        public static bool IsCombatActor(NetworkObject networkObject)
        {
            return networkObject != null &&
                   networkObject.GetComponent<NetworkPlayerCombat>() != null;
        }

        public static int GetHumanSlot(ulong clientId)
        {
            return (int)(clientId / 2UL) % 4;
        }

        public static string GetTeamName(int team)
        {
            return team == CyanTeam ? "CYAN" : team == OrangeTeam ? "ORANGE" : "NEUTRAL";
        }

        public static Color GetTeamColor(int team)
        {
            return team == CyanTeam
                ? new Color(0.15f, 0.90f, 0.80f)
                : team == OrangeTeam
                    ? new Color(1.00f, 0.55f, 0.18f)
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

        public static Vector3 GetTeamSpawnPosition(int team, int slot)
        {
            slot = Mathf.Clamp(slot, 0, 3);

            float x = team == CyanTeam ? -9f : 9f;
            float z = -6f + slot * 4f;
            return new Vector3(x, 0.1f, z);
        }
    }
}
