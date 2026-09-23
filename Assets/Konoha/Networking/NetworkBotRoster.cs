using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Konoha.Networking
{
    public sealed class NetworkBotRoster : NetworkBehaviour
    {
        public const int TeamSize = 4;
        public GameObject botPrefab;

        private float nextReconcileTime;

        private void Update()
        {
            if (!IsServer || !IsSpawned || botPrefab == null)
                return;

            NetworkMatchManager match = NetworkMatchManager.Instance;
            if (match == null)
                return;

            if (match.State != GreyboxMatchState.Waiting &&
                match.State != GreyboxMatchState.Result)
                return;

            if (Time.unscaledTime < nextReconcileTime)
                return;

            nextReconcileTime = Time.unscaledTime + 0.60f;
            Reconcile();
        }

        public void Reconcile()
        {
            if (!IsServer || NetworkManager == null || NetworkManager.SpawnManager == null)
                return;

            ReconcileTeam(NetworkTeamUtility.CyanTeam);
            ReconcileTeam(NetworkTeamUtility.OrangeTeam);
        }

        private void ReconcileTeam(int team)
        {
            var usedSlots = new HashSet<int>();
            var bots = new List<NetworkBotController>();
            int humanCount = 0;

            foreach (NetworkObject networkObject in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null)
                    continue;

                NetworkBotController bot = networkObject.GetComponent<NetworkBotController>();
                if (bot != null)
                {
                    if (bot.Team == team)
                    {
                        bots.Add(bot);
                        usedSlots.Add(bot.Slot);
                    }
                    continue;
                }

                if (!networkObject.IsPlayerObject ||
                    NetworkTeamUtility.GetTeam(networkObject.OwnerClientId) != team)
                    continue;

                humanCount++;
                usedSlots.Add(NetworkTeamUtility.GetHumanSlot(networkObject.OwnerClientId));
            }

            int desiredBots = Mathf.Clamp(TeamSize - humanCount, 0, TeamSize);

            while (bots.Count > desiredBots)
            {
                NetworkBotController bot = bots[bots.Count - 1];
                bots.RemoveAt(bots.Count - 1);

                if (bot != null && bot.NetworkObject != null && bot.NetworkObject.IsSpawned)
                    bot.NetworkObject.Despawn(true);
            }

            while (bots.Count < desiredBots)
            {
                int slot = FindFreeSlot(usedSlots);
                if (slot < 0)
                    break;

                usedSlots.Add(slot);
                NetworkBotController bot = SpawnBot(team, slot);
                if (bot == null)
                    break;

                bots.Add(bot);
            }
        }

        private NetworkBotController SpawnBot(int team, int slot)
        {
            GameObject instance = Instantiate(
                botPrefab,
                NetworkTeamUtility.GetTeamSpawnPosition(team, slot),
                Quaternion.identity);

            NetworkBotController bot = instance.GetComponent<NetworkBotController>();
            NetworkObject networkObject = instance.GetComponent<NetworkObject>();

            if (bot == null || networkObject == null)
            {
                Destroy(instance);
                return null;
            }

            PrototypeHero hero = (PrototypeHero)(slot % 4);
            bot.ConfigureBeforeSpawn(team, slot, hero);
            networkObject.Spawn(true);

            return bot;
        }

        private static int FindFreeSlot(HashSet<int> usedSlots)
        {
            for (int slot = 0; slot < TeamSize; slot++)
            {
                if (!usedSlots.Contains(slot))
                    return slot;
            }

            return -1;
        }
    }
}
