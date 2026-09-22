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
            int team = GetTeam(clientId);
            int slot = (int)(clientId / 2UL) % 4;

            float x = team == CyanTeam ? -9f : 9f;
            float z = -6f + slot * 4f;
            return new Vector3(x, 0.1f, z);
        }
    }
}
