using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Networking
{
    public sealed class CombatFeedHud : MonoBehaviour
    {
        private sealed class FeedEntry
        {
            public string text;
            public float createdAt;
        }

        public static CombatFeedHud Instance { get; private set; }

        public Text display;
        public float lifetime = 5.5f;
        public int maxEntries = 4;

        private readonly List<FeedEntry> entries = new List<FeedEntry>();

        private void Awake()
        {
            Instance = this;
            if (display != null)
                display.supportRichText = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            bool changed = false;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (now - entries[i].createdAt > lifetime)
                {
                    entries.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
                Refresh();
        }

        public void PushKnockout(
            string attacker,
            string victim,
            int attackerTeam,
            int victimTeam,
            bool credited)
        {
            string victimColor = TeamHex(victimTeam);
            string line;

            if (credited && !string.IsNullOrWhiteSpace(attacker))
            {
                string attackerColor = TeamHex(attackerTeam);
                line = "<color=#" + attackerColor + ">" + attacker +
                       "</color>  >  <color=#" + victimColor + ">" + victim +
                       "</color>  <b>RUNTUH</b>";
            }
            else
            {
                line = "<color=#" + victimColor + ">" + victim +
                       "</color>  <b>WIBAWA RUNTUH</b>";
            }

            entries.Insert(0, new FeedEntry
            {
                text = line,
                createdAt = Time.unscaledTime
            });

            while (entries.Count > Mathf.Max(1, maxEntries))
                entries.RemoveAt(entries.Count - 1);

            Refresh();
        }

        private void Refresh()
        {
            if (display == null)
                return;

            if (entries.Count == 0)
            {
                display.text = string.Empty;
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                    builder.Append("\n");

                builder.Append(entries[i].text);
            }

            display.text = builder.ToString();
        }

        private static string TeamHex(int team)
        {
            return team == NetworkTeamUtility.CyanTeam
                ? "37E6D3"
                : team == NetworkTeamUtility.OrangeTeam
                    ? "FF9A32"
                    : "D5DCE5";
        }
    }
}
