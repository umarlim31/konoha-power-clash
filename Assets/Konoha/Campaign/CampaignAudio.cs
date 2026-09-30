using System;
using System.Collections.Generic;
using Konoha.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    public enum CampaignSound
    {
        UiClick,
        Hit,
        HeroHurt,
        ShieldBlock,
        EnemyDown,
        Skill,
        Ultimate,
        Warning,
        Palu,
        Teleport,
        Stamp,
        Seal,
        BlockBreak,
        Door,
        Restu,
        Runtuh,
        Start,
        Victory,
        Surrender,
        // 0.2.2 Kota Hidup: passing traffic (motor = higher pitch).
        Klakson,
        // 0.2.3 skill effects: kerbau bellow + hooves, lightning crack, dash whoosh, heavy impact.
        Kerbau,
        Petir,
        Wuss,
        Hantam
    }

    // Jalur Takhta sound effects (0.0.9.4). Every clip is synthesised in code at start-up
    // (no audio files, in line with the generated-scene rule), played through a small pool
    // of 2D sources. The component only OBSERVES replicated state and two presentation
    // hooks of the shared combat code, so it works the same on a host or a co-op client
    // and adds no network traffic. PvP has no CampaignAudio, so it stays silent as before.
    public sealed class CampaignAudio : MonoBehaviour
    {
        public const int SampleRate = 22050;
        private const string MutedKey = "konoha.campaign.sfx.muted";

        public static CampaignAudio Instance { get; private set; }

        public Button muteButton;
        // Buttons that click (hero screen, DUDUK/ULANG, camera buttons...). Skill buttons
        // are left out: the skill itself makes a sound.
        public Button[] clickButtons = new Button[0];
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        // Sounds farther than this from the camera focus are not heard.
        public float hearingDistance = 38f;

        private readonly AudioSource[] pool = new AudioSource[10];
        private readonly float[] lastPlayed = new float[Enum.GetValues(typeof(CampaignSound)).Length];
        private readonly Dictionary<CampaignEnemy, int> enemyFlags = new Dictionary<CampaignEnemy, int>();
        private readonly List<CampaignEnemy> staleEnemies = new List<CampaignEnemy>();
        private AudioClip[] clips;
        private Text muteLabel;
        private int nextSource;
        private AudioSource ambience;
        [Range(0f, 1f)] public float ambienceVolume = 0.28f;
        private bool muted;

        // Last observed director state (null until first seen: no sound on the first frame).
        private bool observed;
        private bool lastHeroLocked, lastRestu, lastBlock, lastDoor;
        private int lastSeals, lastStamps, lastRuntuh;
        private CampaignPhase lastPhase;

        private const int FlagTelegraph = 1, FlagDown = 2, FlagSurrender = 4;

        private void Awake()
        {
            Instance = this;
            clips = BuildClips();
            for (int i = 0; i < pool.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                pool[i] = source;
            }
            muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        }

        private void OnEnable()
        {
            NetworkPlayerCombat.DamageFeedbackPlayed += OnDamage;
            NetworkHeroKit.AbilityFxPlayed += OnAbility;
        }

        private void OnDisable()
        {
            NetworkPlayerCombat.DamageFeedbackPlayed -= OnDamage;
            NetworkHeroKit.AbilityFxPlayed -= OnAbility;
        }

        private void Start()
        {
            // 0.2.6: quiet gamelan in the background (Nusantara atmosphere), follows SUARA.
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0f;
            ambience.loop = true;
            ambience.clip = BuildGamelanLoop();
            ambience.volume = ambienceVolume;
            ambience.mute = muted;
            ambience.Play();
            if (muteButton != null)
            {
                muteLabel = muteButton.GetComponentInChildren<Text>(true);
                muteButton.onClick.AddListener(ToggleMute);
            }
            foreach (Button button in clickButtons)
                if (button != null)
                    button.onClick.AddListener(Click);
            RefreshMuteLabel();
        }

        private void OnDestroy()
        {
            if (ambience != null && ambience.clip != null)
                Destroy(ambience.clip);
            if (muteButton != null)
                muteButton.onClick.RemoveListener(ToggleMute);
            foreach (Button button in clickButtons)
                if (button != null)
                    button.onClick.RemoveListener(Click);
            if (Instance == this)
                Instance = null;
        }

        public bool Muted => muted;

        private void Click() => Play(CampaignSound.UiClick, 0.6f);

        private void ToggleMute()
        {
            muted = !muted;
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            if (ambience != null)
                ambience.mute = muted;
            RefreshMuteLabel();
            if (!muted)
                Play(CampaignSound.UiClick, 0.7f);
        }

        private void RefreshMuteLabel()
        {
            if (muteLabel != null)
                muteLabel.text = muted ? "SUARA: MATI" : "SUARA: NYALA";
        }

        // --- Playback -------------------------------------------------------------------

        public void Play(CampaignSound sound, float volume = 1f, float pitch = 1f)
        {
            if (muted || clips == null)
                return;

            int index = (int)sound;
            // The same sound within 50 ms reads as one (crowds, multi-hit skills).
            if (Time.unscaledTime - lastPlayed[index] < 0.05f)
                return;
            lastPlayed[index] = Time.unscaledTime;

            AudioSource source = pool[nextSource];
            nextSource = (nextSource + 1) % pool.Length;
            source.pitch = pitch;
            source.PlayOneShot(clips[index], Mathf.Clamp01(volume * masterVolume));
        }

        // World sound: quieter with distance from the local hero, silent beyond hearingDistance.
        public void PlayAt(CampaignSound sound, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            Vector3 listener = ListenerPoint();
            float distance = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(listener.x, listener.z));
            float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(1f, hearingDistance));
            if (falloff <= 0.02f)
                return;
            Play(sound, volume * (0.35f + 0.65f * falloff), pitch);
        }

        private static Vector3 ListenerPoint()
        {
            NetworkObject hero = LocalHero();
            if (hero != null)
                return hero.transform.position;
            Camera view = Camera.main;
            return view != null ? view.transform.position : Vector3.zero;
        }

        private static NetworkObject LocalHero()
        {
            NetworkManager manager = NetworkManager.Singleton;
            return manager != null && manager.LocalClient != null ? manager.LocalClient.PlayerObject : null;
        }

        // --- Hooks from the shared combat presentation ------------------------------------

        private void OnDamage(NetworkPlayerCombat target, int damage, int absorbed)
        {
            if (target == null)
                return;

            NetworkObject hero = LocalHero();
            if (hero != null && target.NetworkObject == hero)
            {
                Play(damage > 0 ? CampaignSound.HeroHurt : CampaignSound.ShieldBlock, damage > 0 ? 0.9f : 0.7f);
                return;
            }

            float pitch = 0.92f + UnityEngine.Random.value * 0.16f;
            PlayAt(damage > 0 ? CampaignSound.Hit : CampaignSound.ShieldBlock, target.transform.position, 0.75f, pitch);
        }

        private void OnAbility(NetworkHeroKit kit, int slot, Vector3 position)
        {
            if (kit == null || NetworkTeamUtility.IsAiActor(kit))
                return;
            Vector3 where = kit.transform.position;
            if (slot >= 3)
                PlayAt(CampaignSound.Ultimate, where, 1f);
            else
                PlayAt(CampaignSound.Skill, where, 0.8f, slot == 2 ? 0.85f : 1f);
        }

        // --- Observed replicated state ----------------------------------------------------

        private void Update()
        {
            CampaignDirector director = CampaignDirector.Instance;
            if (director != null && director.IsSpawned)
                ObserveDirector(director);
            else
                observed = false;

            ObserveEnemies();
        }

        private void ObserveDirector(CampaignDirector director)
        {
            bool heroLocked = director.HeroLocked;
            bool restu = director.RestuActive;
            bool block = director.MajelisBlock;
            bool door = director.BiroDoorOpen;
            int seals = director.SealCount;
            int stamps = director.LoketStampedCount;
            int runtuh = director.RuntuhCount;
            CampaignPhase phase = director.Phase;

            if (observed)
            {
                if (heroLocked && !lastHeroLocked) Play(CampaignSound.Start);
                if (restu && !lastRestu) Play(CampaignSound.Restu);
                if (!block && lastBlock && director.MajelisStarted) Play(CampaignSound.BlockBreak);
                if (stamps > lastStamps) Play(CampaignSound.Stamp);
                if (door && !lastDoor) Play(CampaignSound.Door, 0.9f);
                if (seals > lastSeals) Play(CampaignSound.Seal);
                if (runtuh > lastRuntuh) Play(CampaignSound.Runtuh);
                if (phase != lastPhase)
                {
                    if (phase == CampaignPhase.GerbangDalam ||
                        (phase == CampaignPhase.KursiTerbuka && lastPhase == CampaignPhase.GardaTakhta))
                        Play(CampaignSound.Door, 0.9f, phase == CampaignPhase.KursiTerbuka ? 1.15f : 1f);
                    else if (phase == CampaignPhase.Memerintah)
                        Play(CampaignSound.Seal, 0.8f, 0.8f);
                    else if (phase == CampaignPhase.Menang)
                        Play(CampaignSound.Victory);
                }
            }

            lastHeroLocked = heroLocked;
            lastRestu = restu;
            lastBlock = block;
            lastDoor = door;
            lastSeals = seals;
            lastStamps = stamps;
            lastRuntuh = runtuh;
            lastPhase = phase;
            observed = true;
        }

        private void ObserveEnemies()
        {
            foreach (CampaignEnemy enemy in CampaignEnemy.Active)
            {
                if (enemy == null || !enemy.IsSpawned)
                    continue;

                int flags = (enemy.IsTelegraphing && !enemy.IsDown ? FlagTelegraph : 0) |
                    (enemy.IsDown ? FlagDown : 0) |
                    (enemy.Surrendered ? FlagSurrender : 0);

                if (enemyFlags.TryGetValue(enemy, out int previous))
                {
                    Vector3 at = enemy.transform.position;
                    bool wasTelegraph = (previous & FlagTelegraph) != 0;
                    bool isTelegraph = (flags & FlagTelegraph) != 0;
                    if (isTelegraph && !wasTelegraph)
                        PlayAt(CampaignSound.Warning, at, 1f);
                    else if (!isTelegraph && wasTelegraph && (flags & FlagDown) == 0)
                        PlayAt(enemy.Special == EnemySpecial.SalahLoket ? CampaignSound.Teleport : CampaignSound.Palu, at, 1f);

                    if ((flags & FlagDown) != 0 && (previous & FlagDown) == 0)
                        PlayAt(CampaignSound.EnemyDown, at, enemy.IsElite ? 1f : 0.75f, enemy.IsElite ? 0.8f : 1f);
                    if ((flags & FlagSurrender) != 0 && (previous & FlagSurrender) == 0)
                        PlayAt(CampaignSound.Surrender, at, 0.7f);
                }
                enemyFlags[enemy] = flags;
            }

            // Forget despawned enemies.
            if (enemyFlags.Count > CampaignEnemy.Active.Count)
            {
                staleEnemies.Clear();
                foreach (CampaignEnemy known in enemyFlags.Keys)
                    if (known == null || !known.IsSpawned)
                        staleEnemies.Add(known);
                foreach (CampaignEnemy stale in staleEnemies)
                    enemyFlags.Remove(stale);
            }
        }

        // --- Synthesis ----------------------------------------------------------------------

        // One clip per CampaignSound, in enum order. Public for the EditMode test.
        // --- Gamelan ambience (0.2.6) --------------------------------------------------------

        public const float GamelanBeat = 0.75f;
        public const int GamelanBeats = 16;

        // One 12 s gongan in slendro: a saron melody (balungan), a bonang ornament at double
        // tempo, kempul halfway and the gong on the first beat. Every note is synthesised once
        // and mixed with wrap-around, so the loop is seamless. Sounds are original, generated.
        public static AudioClip BuildGamelanLoop()
        {
            int length = Mathf.RoundToInt(GamelanBeat * GamelanBeats * SampleRate);
            var mix = new float[length];
            // Slendro: five roughly equal steps of 240 cents.
            float[] scale = new float[10];
            for (int k = 0; k < scale.Length; k++)
                scale[k] = 262f * Mathf.Pow(2f, k * 0.2f);
            int[] balungan = { 2, 3, 2, 1, 0, 1, 2, 3, 4, 3, 2, 1, 2, 1, 0, 1 };

            var saronNotes = new float[scale.Length][];
            var bonangNotes = new float[scale.Length][];
            for (int k = 0; k < scale.Length; k++)
            {
                saronNotes[k] = Metallophone(scale[k], 1.4f, 2.4f, 2.76f, 0.30f);
                bonangNotes[k] = Metallophone(scale[k] * 2f, 0.7f, 4.5f, 1.52f, 0.25f);
            }

            for (int beat = 0; beat < GamelanBeats; beat++)
            {
                int note = balungan[beat];
                int start = Mathf.RoundToInt(beat * GamelanBeat * SampleRate);
                AddWrapped(mix, saronNotes[note], start, 0.26f);
                // Mipil: the bonang alternates this note and the next one, twice per beat.
                int next = balungan[(beat + 1) % GamelanBeats];
                AddWrapped(mix, bonangNotes[note], start, 0.10f);
                AddWrapped(mix, bonangNotes[next], start + Mathf.RoundToInt(GamelanBeat * 0.5f * SampleRate), 0.08f);
            }
            AddWrapped(mix, Gong(68f, 5f), 0, 0.42f);                                      // gong ageng
            AddWrapped(mix, Gong(131f, 2.5f), length / 2, 0.20f);                          // kempul

            float peak = 0f;
            foreach (float v in mix) peak = Mathf.Max(peak, Mathf.Abs(v));
            float gain = peak > 0.9f ? 0.9f / peak : 1f;
            for (int i = 0; i < length; i++) mix[i] *= gain;

            AudioClip clip = AudioClip.Create("Ambience_Gamelan", length, 1, SampleRate, false);
            clip.SetData(mix, 0);
            return clip;
        }

        // Bronze key: fundamental plus one inharmonic partial, fast attack, exponential decay.
        private static float[] Metallophone(float frequency, float seconds, float decay, float partial, float partialLevel)
        {
            int n = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-decay * t);
                data[i] = (Mathf.Sin(2f * Mathf.PI * frequency * t) +
                           partialLevel * Mathf.Sin(2f * Mathf.PI * frequency * partial * t) * Mathf.Exp(-decay * 2f * t)) * env;
            }
            return data;
        }

        // Gong: two slightly detuned low tones beating slowly, long decay.
        private static float[] Gong(float frequency, float seconds)
        {
            int n = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t * 60f) * Mathf.Exp(-0.8f * t);
                data[i] = (Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.7f +
                           Mathf.Sin(2f * Mathf.PI * (frequency + 0.9f) * t) * 0.5f +
                           Mathf.Sin(2f * Mathf.PI * frequency * 2.02f * t) * 0.15f * Mathf.Exp(-2f * t)) * env;
            }
            return data;
        }

        private static void AddWrapped(float[] mix, float[] note, int start, float level)
        {
            for (int i = 0; i < note.Length; i++)
                mix[(start + i) % mix.Length] += note[i] * level;
        }

        public static AudioClip[] BuildClips()
        {
            var result = new AudioClip[Enum.GetValues(typeof(CampaignSound)).Length];
            result[(int)CampaignSound.UiClick] = Make("UiClick", 0.05f, t => Sin(1400f, t) * Decay(t, 90f) * 0.5f);
            result[(int)CampaignSound.Hit] = MakeNoisy("Hit", 0.14f, 11, (t, n) =>
                Sin(Sweep(220f, 70f, t, 0.14f)) * Decay(t, 25f) * 0.7f + n * Decay(t, 60f) * 0.45f);
            result[(int)CampaignSound.HeroHurt] = MakeNoisy("HeroHurt", 0.22f, 12, (t, n) =>
                Soft(Sweep(150f, 55f, t, 0.22f)) * Decay(t, 14f) * 0.75f + n * Decay(t, 35f) * 0.3f);
            result[(int)CampaignSound.ShieldBlock] = Make("ShieldBlock", 0.26f, t =>
                (Sin(1250f, t) + Sin(1870f, t) * 0.6f) * Decay(t, 18f) * 0.3f);
            result[(int)CampaignSound.EnemyDown] = MakeNoisy("EnemyDown", 0.45f, 13, (t, n) =>
                Soft(Sweep(420f, 90f, t, 0.45f)) * Decay(t, 6f) * 0.55f + n * Decay(t, 12f) * 0.25f);
            result[(int)CampaignSound.Skill] = MakeNoisy("Skill", 0.3f, 14, (t, n) =>
                (Sin(Sweep(300f, 1100f, t, 0.3f)) * 0.5f + n * 0.18f) * Bell(t, 0.3f));
            result[(int)CampaignSound.Ultimate] = MakeNoisy("Ultimate", 0.9f, 15, (t, n) =>
                Sin(55f, t) * Decay(t, 4f) * 0.8f + Sin(Sweep(200f, 1400f, t, 0.9f)) * Bell(t, 0.9f) * 0.3f +
                n * Decay(t, 9f) * 0.25f);
            result[(int)CampaignSound.Warning] = Make("Warning", 0.34f, t =>
                (t < 0.12f || (t > 0.2f && t < 0.32f)) ? Soft(880f * t) * 0.35f : 0f);
            result[(int)CampaignSound.Palu] = MakeNoisy("Palu", 0.24f, 16, (t, n) =>
                Sin(180f, t) * Decay(t, 30f) * 0.9f + n * Decay(t, 220f) * 0.6f);
            result[(int)CampaignSound.Teleport] = Make("Teleport", 0.42f, t =>
                Sin(Sweep(1400f, 300f, t, 0.42f) + 3f * Mathf.Sin(2f * Mathf.PI * 18f * t)) * Bell(t, 0.42f) * 0.4f);
            result[(int)CampaignSound.Stamp] = MakeNoisy("Stamp", 0.25f, 17, (t, n) =>
                Sin(90f, t) * Decay(t, 25f) * 0.9f + n * Decay(t, 80f) * 0.5f);
            result[(int)CampaignSound.Seal] = Make("Seal", 0.95f, t => Arpeggio(t, 0.19f, 523.25f, 659.25f, 783.99f, 1046.5f));
            result[(int)CampaignSound.BlockBreak] = MakeNoisy("BlockBreak", 0.6f, 18, (t, n) =>
                n * Decay(t, 5f) * (0.35f + 0.35f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 23f * t))) +
                Sin(Sweep(900f, 300f, t, 0.6f)) * Decay(t, 7f) * 0.2f, highPass: true);
            result[(int)CampaignSound.Door] = MakeNoisy("Door", 0.7f, 19, (t, n) =>
                (Sin(70f, t) * 0.5f + Sin(Sweep(95f, 60f, t, 0.7f)) * 0.3f + n * 0.25f) * Bell(t, 0.7f));
            result[(int)CampaignSound.Restu] = Make("Restu", 1.05f, t =>
                Arpeggio(t, 0.16f, 392f, 493.88f, 587.33f, 783.99f) + Sin(2349f, t) * Bell(t, 1.05f) * 0.06f);
            result[(int)CampaignSound.Runtuh] = Make("Runtuh", 0.95f, t => Arpeggio(t, 0.22f, 440f, 369.99f, 311.13f, 261.63f));
            result[(int)CampaignSound.Start] = Make("Start", 1.6f, t =>
                (Sin(110f, t) + Sin(303.6f, t) * 0.5f + Sin(594f, t) * 0.25f) * Decay(t, 2.5f) * 0.45f);
            result[(int)CampaignSound.Victory] = Make("Victory", 1.8f, t =>
                t < 0.72f
                    ? Arpeggio(t, 0.18f, 523.25f, 659.25f, 783.99f, 1046.5f)
                    : (Tri(523.25f * t) + Tri(659.25f * t) + Tri(783.99f * t) + Tri(1046.5f * t)) * 0.1f *
                      Decay(t - 0.72f, 2.2f));
            result[(int)CampaignSound.Surrender] = Make("Surrender", 0.35f, t =>
                Sin(Sweep(600f, 400f, t, 0.35f)) * Bell(t, 0.35f) * 0.25f);
            // Two short "tin-tin" beeps of a two-tone horn, slightly soft-clipped.
            result[(int)CampaignSound.Klakson] = Make("Klakson", 0.46f, t =>
                (Soft(410f * t) + Soft(517f * t)) * 0.2f *
                Mathf.Min(1f, 3f * (Bell(t, 0.15f) + (t > 0.22f ? Bell(t - 0.22f, 0.22f) : 0f))));
            result[(int)CampaignSound.Kerbau] = MakeNoisy("Kerbau", 1.1f, 21, (t, n) =>
                // Low nasal bellow (two harmonics, vibrato) over drumming hooves.
                (Soft((95f + 6f * Sin(5f, t)) * t) * 0.45f + Sin(190f * t + 0.3f * Sin(3f, t)) * 0.18f) *
                Bell(Mathf.Clamp(t - 0.05f, 0f, 0.85f), 0.85f) +
                n * 0.55f * Mathf.Pow(Mathf.Abs(Sin(7.5f, t)), 8f) * Decay(t, 1.2f));
            result[(int)CampaignSound.Petir] = MakeNoisy("Petir", 0.6f, 22, (t, n) =>
                n * (Decay(t, 9f) + 0.5f * Decay(Mathf.Max(0f, t - 0.07f), 14f)) * 0.8f +
                Sin(Sweep(2200f, 400f, t, 0.6f)) * Decay(t, 12f) * 0.2f, highPass: true);
            result[(int)CampaignSound.Wuss] = MakeNoisy("Wuss", 0.32f, 23, (t, n) =>
                n * Bell(t, 0.32f) * 0.55f + Sin(Sweep(500f, 180f, t, 0.32f)) * Bell(t, 0.32f) * 0.12f);
            result[(int)CampaignSound.Hantam] = MakeNoisy("Hantam", 0.75f, 24, (t, n) =>
                Soft(Sweep(90f, 38f, t, 0.75f)) * Decay(t, 5f) * 0.8f + n * Decay(t, 16f) * 0.5f);
            return result;
        }

        private static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            return MakeNoisy(name, seconds, 1, (t, n) => sample(t));
        }

        // n: white noise in [-1, 1], low-passed (or high-passed) with a one-pole filter.
        private static AudioClip MakeNoisy(string name, float seconds, int seed, Func<float, float, float> sample,
            bool highPass = false)
        {
            int length = Mathf.Max(1, Mathf.CeilToInt(seconds * SampleRate));
            var data = new float[length];
            var random = new System.Random(seed);
            float low = 0f;
            // Short fade-in/out avoids clicks at the clip edges.
            int fade = Mathf.Min(length / 4, SampleRate / 400);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                low += (white - low) * 0.35f;
                float noise = highPass ? white - low : low * 1.6f;
                float value = sample(t, noise);
                if (i < fade) value *= i / (float)fade;
                if (i > length - fade) value *= (length - i) / (float)fade;
                data[i] = Mathf.Clamp(value, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Sfx_" + name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Phase (in cycles) of a linear frequency sweep from f0 to f1 over duration.
        private static float Sweep(float f0, float f1, float t, float duration) =>
            f0 * t + (f1 - f0) * t * t / (2f * duration);

        private static float Sin(float cycles) => Mathf.Sin(2f * Mathf.PI * cycles);
        private static float Sin(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);
        // Rounder than a square, brighter than a sine.
        private static float Soft(float cycles) => (float)Math.Tanh(2.5f * Sin(cycles));
        private static float Tri(float cycles) => 1f - 4f * Mathf.Abs(Mathf.Repeat(cycles + 0.25f, 1f) - 0.5f);
        private static float Decay(float t, float rate) => Mathf.Exp(-rate * Mathf.Max(0f, t));
        private static float Bell(float t, float duration) => Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / duration));

        private static float Arpeggio(float t, float step, params float[] notes)
        {
            float value = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float local = t - i * step;
                if (local < 0f) break;
                value += (Tri(notes[i] * local) * 0.6f + Sin(notes[i], local) * 0.4f) * Decay(local, 3.2f) * 0.22f;
            }
            return value;
        }
    }
}
