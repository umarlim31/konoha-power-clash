using System.Collections.Generic;
using System.Globalization;
using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // 0.2.9: round icon buttons for the Jalur Takhta controls (BASIC, skills, ULT, DODGE,
    // LOMPAT, DUDUK). Each button keeps its Button and its original caption Text (the shared
    // combat code still writes names and cooldowns there); the caption is hidden and read back
    // to drive the new look: a circular plate tinted by the hero colour, a white icon per
    // hero skill, a gold ring, a radial cooldown sweep with seconds, and a small name below.
    public sealed class CampaignSkillHud : MonoBehaviour
    {
        public enum Slot { Basic, Skill1, Skill2, Ultimate, Dodge, Jump, Seat }

        [System.Serializable]
        public sealed class Entry
        {
            public Slot slot;
            public Button button;
        }

        public Entry[] buttons = new Entry[0];

        private sealed class View
        {
            public Entry entry;
            public Text caption;
            public Image icon, sweep;
            public Text seconds, name;
            public float longest;
            public CampaignIconPainter.Icon shown = CampaignIconPainter.Icon.Circle;
        }

        private readonly List<View> views = new List<View>();
        private readonly Dictionary<CampaignIconPainter.Icon, Sprite> sprites = new Dictionary<CampaignIconPainter.Icon, Sprite>();
        private Font font;

        private void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite circle = SpriteFor(CampaignIconPainter.Icon.Circle);
            Sprite ring = SpriteFor(CampaignIconPainter.Icon.Ring);
            foreach (Entry entry in buttons)
            {
                if (entry == null || entry.button == null)
                    continue;
                var rect = (RectTransform)entry.button.transform;
                Image plate = entry.button.GetComponent<Image>();
                if (plate != null)
                {
                    plate.sprite = circle;
                    plate.type = Image.Type.Simple;
                    plate.preserveAspect = true;
                }
                // The caption stays first in the hierarchy (NetworkHeroKit binds the first Text).
                Text caption = entry.button.GetComponentInChildren<Text>(true);
                if (caption != null)
                    caption.color = new Color(0f, 0f, 0f, 0f);

                var view = new View { entry = entry, caption = caption };
                view.icon = Child<Image>(rect, "Ikon", new Vector2(.5f, .5f), Vector2.zero, rect.sizeDelta * .62f);
                view.icon.preserveAspect = true;
                view.icon.raycastTarget = false;
                Image ringImage = Child<Image>(rect, "Cincin", new Vector2(.5f, .5f), Vector2.zero, rect.sizeDelta);
                ringImage.sprite = ring;
                ringImage.color = new Color(.95f, .78f, .38f, .95f);
                ringImage.raycastTarget = false;
                ringImage.preserveAspect = true;
                view.sweep = Child<Image>(rect, "Cooldown", new Vector2(.5f, .5f), Vector2.zero, rect.sizeDelta * .94f);
                view.sweep.sprite = circle;
                view.sweep.type = Image.Type.Filled;
                view.sweep.fillMethod = Image.FillMethod.Radial360;
                view.sweep.fillOrigin = (int)Image.Origin360.Top;
                view.sweep.fillClockwise = false;
                view.sweep.color = new Color(0f, 0f, 0f, .58f);
                view.sweep.raycastTarget = false;
                view.sweep.preserveAspect = true;
                view.seconds = Label(rect, "Detik", new Vector2(.5f, .5f), Vector2.zero, Mathf.RoundToInt(rect.sizeDelta.y * .3f));
                view.name = Label(rect, "Nama", new Vector2(.5f, 0f), new Vector2(0f, -11f), 14);
                views.Add(view);
            }
        }

        private void Update()
        {
            PrototypeHero hero = LocalHero();
            foreach (View view in views)
            {
                if (view.entry.button == null)
                    continue;
                string text = view.caption != null ? view.caption.text : "";
                Parse(text, out string name, out float remaining, out float ultPercent, out bool ultReady);

                CampaignIconPainter.Icon icon = IconFor(view.entry.slot, hero);
                if (icon != view.shown)
                {
                    view.shown = icon;
                    view.icon.sprite = SpriteFor(icon);
                }

                if (view.entry.slot == Slot.Ultimate)
                {
                    view.sweep.fillAmount = ultReady ? 0f : 1f - Mathf.Clamp01(ultPercent / 100f);
                    view.seconds.text = ultReady ? "" : Mathf.RoundToInt(ultPercent) + "%";
                    view.name.text = UltimateName(hero);
                }
                else
                {
                    if (remaining <= 0f) view.longest = 0f;
                    else view.longest = Mathf.Max(view.longest, remaining);
                    view.sweep.fillAmount = remaining > 0f && view.longest > 0f ? remaining / view.longest : 0f;
                    view.seconds.text = remaining > 0f ? remaining.ToString(remaining < 10f ? "0.0" : "0", CultureInfo.InvariantCulture) : "";
                    view.name.text = name;
                }
                bool usable = view.entry.button.interactable;
                view.icon.color = usable ? Color.white : new Color(1f, 1f, 1f, .45f);
            }
        }

        // "SERUAN\nIBU\n9,1" -> name "SERUAN IBU", 9.1 s. "ULT 63%" -> 63 %, "ULT\nMONCONG" -> ready.
        public static void Parse(string text, out string name, out float remaining, out float ultPercent, out bool ultReady)
        {
            name = "";
            remaining = 0f;
            ultPercent = 0f;
            ultReady = false;
            if (string.IsNullOrEmpty(text))
                return;
            if (text.StartsWith("ULT"))
            {
                ultReady = text.StartsWith("ULT\n");
                int percent = text.IndexOf('%');
                if (!ultReady && percent > 3)
                    float.TryParse(text.Substring(3, percent - 3).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out ultPercent);
                name = ultReady ? text.Substring(4).Replace('\n', ' ') : "ULT";
                return;
            }
            string[] lines = text.Split('\n');
            int nameLines = lines.Length;
            string last = lines[lines.Length - 1].Trim().Replace(',', '.');
            if (lines.Length > 1 && float.TryParse(last, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
            {
                remaining = seconds;
                nameLines--;
            }
            name = string.Join(" ", lines, 0, nameLines).Trim();
        }

        public static CampaignIconPainter.Icon IconFor(Slot slot, PrototypeHero hero)
        {
            switch (slot)
            {
                case Slot.Basic: return CampaignIconPainter.Icon.Fist;
                case Slot.Dodge: return CampaignIconPainter.Icon.Dodge;
                case Slot.Jump: return CampaignIconPainter.Icon.Jump;
                case Slot.Seat: return CampaignIconPainter.Icon.Seat;
            }
            int index = slot == Slot.Skill1 ? 0 : slot == Slot.Skill2 ? 1 : 2;
            switch (hero)
            {
                case PrototypeHero.Mega:
                    return new[] { CampaignIconPainter.Icon.Kerbau, CampaignIconPainter.Icon.Shield, CampaignIconPainter.Icon.KerbauStampede }[index];
                case PrototypeHero.Prabowo:
                    return new[] { CampaignIconPainter.Icon.Leap, CampaignIconPainter.Icon.Baris, CampaignIconPainter.Icon.Wings }[index];
                case PrototypeHero.Abah:
                    return new[] { CampaignIconPainter.Icon.Speech, CampaignIconPainter.Icon.Lightning, CampaignIconPainter.Icon.Microphone }[index];
                default:
                    return new[] { CampaignIconPainter.Icon.Cone, CampaignIconPainter.Icon.Footsteps, CampaignIconPainter.Icon.HardHat }[index];
            }
        }

        private static string UltimateName(PrototypeHero hero)
        {
            switch (hero)
            {
                case PrototypeHero.Mega: return "MONCONG";
                case PrototypeHero.Prabowo: return "GARUDA";
                case PrototypeHero.Abah: return "PIDATO";
                default: return "PROYEK";
            }
        }

        private Sprite SpriteFor(CampaignIconPainter.Icon icon)
        {
            if (!sprites.TryGetValue(icon, out Sprite sprite))
            {
                sprite = CampaignIconPainter.ToSprite(icon);
                sprites.Add(icon, sprite);
            }
            return sprite;
        }

        private static PrototypeHero LocalHero()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null || manager.LocalClient.PlayerObject == null)
                return PrototypeHero.Mega;
            NetworkHeroKit kit = manager.LocalClient.PlayerObject.GetComponent<NetworkHeroKit>();
            return kit != null ? kit.Hero : PrototypeHero.Mega;
        }

        private static T Child<T>(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size) where T : Graphic
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            return rect.gameObject.AddComponent<T>();
        }

        private Text Label(RectTransform parent, string name, Vector2 anchor, Vector2 offset, int size)
        {
            Text text = Child<Text>(parent, name, anchor, offset, new Vector2(parent.sizeDelta.x * 1.6f, size * 1.6f));
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .85f);
            return text;
        }

        private void OnDestroy()
        {
            foreach (Sprite sprite in sprites.Values)
            {
                if (sprite == null) continue;
                Destroy(sprite.texture);
                Destroy(sprite);
            }
        }
    }
}
