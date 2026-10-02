using System;

namespace Konoha.Campaign
{
    // 0.6.0: which solo mode runs in this scene. The mode menu sets it before the offline
    // local host starts; KARIER (Level 1 "Warga Biasa") then replaces the Jalur Takhta
    // route, the hero screen and the hero body (the player's own citizen, see
    // CampaignAvatarLook). MODE PRESIDEN (Jalur Takhta) and PvP leave it false.
    public static class CampaignKarier
    {
        public const string AvatarKey = "konoha.karier.avatar";
        public const string SaveKey = "konoha.karier.v1";

        public static bool Active { get; set; }
        public static KarierAvatar Avatar { get; private set; } = KarierAvatar.Default;
        // Bumped on every change so the body copy re-applies the look once.
        public static int AvatarVersion { get; private set; }

        // Host: the local hero collapsed (CampaignDirector) during KARIER.
        public static event Action HeroRuntuh;

        public static void SetAvatar(KarierAvatar avatar)
        {
            Avatar = avatar.Clamped();
            AvatarVersion++;
        }

        public static void RaiseHeroRuntuh() => HeroRuntuh?.Invoke();
    }
}
