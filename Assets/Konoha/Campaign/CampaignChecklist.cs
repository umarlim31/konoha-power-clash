using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // 0.5.0 Jalan Nyaleg: the steps of the run, always on the left of the HUD, so a new
    // player knows where they are and what comes next. Done steps are gold, the current
    // ones white with an arrow, later ones grey. Hidden while the LAWAN / RANGKUL panel is up.
    public sealed class CampaignChecklist : MonoBehaviour
    {
        public GameObject panel;
        public Text listText;
        public GameObject hideWhenActive;

        private readonly StringBuilder builder = new StringBuilder(256);
        private string lastText;

        private void Update()
        {
            CampaignDirector director = CampaignDirector.Instance;
            bool show = director != null && director.IsSpawned && director.HeroLocked &&
                director.Phase != CampaignPhase.Menang && (hideWhenActive == null || !hideWhenActive.activeSelf);
            if (panel != null && panel.activeSelf != show)
                panel.SetActive(show);
            if (!show || listText == null)
                return;

            CampaignPhase phase = director.Phase;
            bool gate = director.GateCleared;
            bool blusukan = director.BlusukanDone;
            bool koalisi = director.HasSeal(CampaignSector.MajelisDaun);
            bool berkas = director.HasSeal(CampaignSector.BiroProsedur);
            bool garda = phase >= CampaignPhase.KursiTerbuka;

            builder.Length = 0;
            builder.Append("<b>JALAN NYALEG</b>\n");
            Line(1, "Usir preman di gang", gate, !gate);
            Line(2, "Blusukan: sapa warga (" + director.SuaraCount + "/" + director.BlusukanCount + ")", gate && blusukan, gate && !blusukan);
            bool plaza = phase >= CampaignPhase.PlazaAspirasi;
            Line(3, "Rekomendasi koalisi" + Suffix(director.Path(CampaignSector.MajelisDaun), "mahar"), koalisi, plaza && !koalisi);
            Line(4, "Berkas di kelurahan" + Suffix(director.Path(CampaignSector.BiroProsedur), "calo"), berkas, plaza && !berkas);
            Line(5, "Pelantikan: tembus Garda Istana", garda, phase == CampaignPhase.GerbangDalam || phase == CampaignPhase.GardaTakhta);
            Line(6, "DUDUK di Kursi", false, phase == CampaignPhase.KursiTerbuka || phase == CampaignPhase.Memerintah);

            string text = builder.ToString();
            if (text != lastText)
            {
                lastText = text;
                listText.text = text;
            }
        }

        private static string Suffix(SectorPath path, string bought) =>
            path == SectorPath.Dirangkul ? "  (" + bought + ")" : path == SectorPath.Dilawan ? "  (dilawan)" : string.Empty;

        private void Line(int number, string label, bool done, bool current)
        {
            if (done)
                builder.Append("<color=#F2C35A>■ ").Append(number).Append(". ").Append(label).Append("</color>\n");
            else if (current)
                builder.Append("<color=#FFFFFF>► ").Append(number).Append(". ").Append(label).Append("</color>\n");
            else
                builder.Append("<color=#9A9A9A>□ ").Append(number).Append(". ").Append(label).Append("</color>\n");
        }
    }
}
