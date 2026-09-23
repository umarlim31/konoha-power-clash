using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    public sealed class NetworkMatchEvents : NetworkBehaviour
    {
        public static NetworkMatchEvents Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
                Instance = null;

            base.OnNetworkDespawn();
        }

        public void ServerReportKnockout(NetworkObject attacker, NetworkObject victim)
        {
            if (!IsServer || victim == null)
                return;

            string victimLabel = GetActorLabel(victim);
            int victimTeam = NetworkTeamUtility.GetTeam(victim);

            bool credited = attacker != null && attacker != victim;
            string attackerLabel = credited ? GetActorLabel(attacker) : string.Empty;
            int attackerTeam = credited ? NetworkTeamUtility.GetTeam(attacker) : -1;

            KnockoutClientRpc(
                attackerLabel,
                victimLabel,
                attackerTeam,
                victimTeam,
                credited);
        }

        [ClientRpc]
        private void KnockoutClientRpc(
            string attacker,
            string victim,
            int attackerTeam,
            int victimTeam,
            bool credited)
        {
            CombatFeedHud.Instance?.PushKnockout(
                attacker,
                victim,
                attackerTeam,
                victimTeam,
                credited);
        }

        public static string GetActorLabel(NetworkObject actor)
        {
            if (actor == null)
                return "ACTOR";

            NetworkHeroKit kit = actor.GetComponent<NetworkHeroKit>();
            string hero = kit != null
                ? NetworkHeroKit.GetHeroName(kit.Hero)
                : "HERO";

            NetworkBotController bot = actor.GetComponent<NetworkBotController>();
            if (bot != null)
                return "BOT " + hero;

            return "P" + actor.OwnerClientId + " " + hero;
        }
    }
}
