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
            var humanSlots = new HashSet<int>();
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
                        bots.Add(bot);

                    continue;
                }

                if (!networkObject.IsPlayerObject ||
                    NetworkTeamUtility.GetTeam(networkObject.OwnerClientId) != team)
                    continue;

                humanCount++;
                humanSlots.Add(NetworkTeamUtility.GetHumanSlot(networkObject.OwnerClientId));
            }

            var retainedBots = new List<NetworkBotController>();
            var usedBotSlots = new HashSet<int>();

            foreach (NetworkBotController bot in bots)
            {
                if (bot == null)
                    continue;

                bool slotConflict = humanSlots.Contains(bot.Slot) || usedBotSlots.Contains(bot.Slot);

                if (slotConflict)
                {
                    if (bot.NetworkObject != null && bot.NetworkObject.IsSpawned)
                        bot.NetworkObject.Despawn(true);

                    continue;
                }

                usedBotSlots.Add(bot.Slot);
                retainedBots.Add(bot);
            }

            int desiredBots = Mathf.Clamp(TeamSize - humanCount, 0, TeamSize);

            while (retainedBots.Count > desiredBots)
            {
                int last = retainedBots.Count - 1;
                NetworkBotController bot = retainedBots[last];
                retainedBots.RemoveAt(last);

                if (bot != null && bot.NetworkObject != null && bot.NetworkObject.IsSpawned)
                    bot.NetworkObject.Despawn(true);
            }

            var usedSlots = new HashSet<int>(humanSlots);
            foreach (NetworkBotController bot in retainedBots)
            {
                if (bot != null)
                    usedSlots.Add(bot.Slot);
            }

            while (retainedBots.Count < desiredBots)
            {
                int slot = FindFreeSlot(usedSlots);
                if (slot < 0)
                    break;

                usedSlots.Add(slot);
                NetworkBotController bot = SpawnBot(team, slot);
                if (bot == null)
                    break;

                retainedBots.Add(bot);
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
