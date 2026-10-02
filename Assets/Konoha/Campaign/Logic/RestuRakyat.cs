namespace Konoha.Campaign
{
    // RESTU RAKYAT (0.0.9.2.1): the people of the Gerbang Rakyat back the hero for the rest
    // of the run. Pure rule; the host decides who is blessed and applies the multiplier
    // through ICombatRules.GetDamageMultiplier.
    public static class RestuRakyat
    {
        // attackerIsBlessedHero: a human hero with the blessing hits someone.
        // targetIsBlessedHero: a human hero with the blessing is being hit.
        // Heroes never hit each other in the campaign, so both flags are not expected together.
        public static float DamageMultiplier(bool attackerIsBlessedHero, bool targetIsBlessedHero)
        {
            float multiplier = 1f;
            if (attackerIsBlessedHero) multiplier *= CampaignTuning.Restu.HeroDamageMultiplier;
            if (targetIsBlessedHero) multiplier *= CampaignTuning.Restu.HeroDamageTakenMultiplier;
            return multiplier;
        }
    }
}
