using UnityEngine;
using UnityEngine.UI;

namespace Konoha.Campaign
{
    // 0.4.0 Musim Pemilu: the LAWAN / RANGKUL panel shown when the hero approaches an
    // institution that is still undecided (GAME_LOGIC v2 §4, Babak 2). LAWAN just closes the
    // panel (walk into the hall to fight); RANGKUL asks the host to buy the seal with Modal,
    // or with a PINJAMAN KONSORSIUM when Modal is short.
    public sealed class CampaignLobiPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text titleText;
        public Text bodyText;
        public Button lawanButton;
        public Button rangkulButton;
        public Text rangkulLabel;

        private int dismissed = -1;
        private int shown = -1;

        private void Start()
        {
            if (lawanButton != null) lawanButton.onClick.AddListener(Lawan);
            if (rangkulButton != null) rangkulButton.onClick.AddListener(Rangkul);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (lawanButton != null) lawanButton.onClick.RemoveListener(Lawan);
            if (rangkulButton != null) rangkulButton.onClick.RemoveListener(Rangkul);
        }

        private void Lawan() => dismissed = shown;

        private void Rangkul()
        {
            CampaignDirector director = CampaignDirector.Instance;
            if (director == null || shown < 0)
                return;
            int cost = CampaignObjectiveDirector.RangkulCost((CampaignSector)shown);
            director.RequestRangkul(director.Modal < cost);
            dismissed = shown;
        }

        private void Update()
        {
            CampaignDirector director = CampaignDirector.Instance;
            int offer = director != null && director.IsSpawned && director.HeroLocked &&
                director.Phase == CampaignPhase.PlazaAspirasi ? director.OfferSector : -1;
            if (offer < 0)
                dismissed = -1;
            bool show = offer >= 0 && offer != dismissed;
            shown = show ? offer : -1;
            if (panel != null && panel.activeSelf != show)
                panel.SetActive(show);
            if (!show)
                return;

            var sector = (CampaignSector)offer;
            int cost = CampaignObjectiveDirector.RangkulCost(sector);
            bool majelis = sector == CampaignSector.MajelisDaun;
            if (titleText != null)
                titleText.text = majelis ? "REKOMENDASI KOALISI" : "BERKAS DI KELURAHAN";
            if (bodyText != null)
                bodyText.text = (majelis
                        ? "Tanpa rekomendasi partai, kamu bukan calon.\nLAWAN para elite di markas, atau BAYAR MAHAR ke Ketum."
                        : "Fotokopi KTP, cap RT/RW, legalisir. Antre panjang,\n\"sistem sedang gangguan\". LAWAN antreannya, atau BAYAR CALO.") +
                    "\nMODAL kamu: " + director.Modal + "  •  Jatah ditagih saat berkuasa.";
            if (rangkulLabel != null)
                rangkulLabel.text = director.Modal >= cost
                    ? (majelis ? "BAYAR MAHAR" : "BAYAR CALO") + "\n-" + cost + " MODAL • JATAH +1"
                    : "PINJAM KONSORSIUM\nJATAH +" + (1 + CampaignTuning.Politik.LoanExtraJatah) + " • RESTU " +
                        (CampaignTuning.Politik.RestuLoan + CampaignTuning.Politik.RestuRangkul);
        }
    }
}
