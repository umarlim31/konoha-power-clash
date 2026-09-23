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
        public GameObject panelRoot;
        public float lifetime = 5.5f;
        public int maxEntries = 3;

        private readonly List<FeedEntry> entries = new List<FeedEntry>();

        private void Awake()
        {
            Instance = this;
            if (display != null)
                display.supportRichText = true;

            if (panelRoot != null)
                panelRoot.SetActive(false);
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

            if (panelRoot != null)
                panelRoot.SetActive(true);

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

                if (panelRoot != null)
                    panelRoot.SetActive(false);

                return;
            }

            if (panelRoot != null)
            {
                RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
                if (panelRect != null)
                    panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 22f + entries.Count * 24f);
            }

            RectTransform displayRect = display.GetComponent<RectTransform>();
            if (displayRect != null)
                displayRect.sizeDelta = new Vector2(displayRect.sizeDelta.x, 10f + entries.Count * 24f);

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
