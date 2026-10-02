using System.Collections.Generic;

namespace Konoha.Campaign
{
    public sealed class KoranEdition
    {
        public string Masthead = "KORAN KONOHA";
        public string Edition;
        public string Headline;
        public string Subhead;
        public string Archetype;
        public string[] News = new string[0];
    }

    // 0.4.0 result screen as a satirical front page (GAME_LOGIC v2 §4). Pure C#: the headline
    // follows the ending, the small stories follow how the run was played. Fiction of Negara
    // Konoha: institutions are invented, the four heroes are roasted equally, no real names.
    public static class KoranKonoha
    {
        public static KoranEdition Compose(CampaignEnding ending, string hero, SectorPath majelis, SectorPath biro,
            int modal, int jatah, int restu, bool tookLoan, int runtuh, int seconds)
        {
            hero = string.IsNullOrEmpty(hero) ? "CALON" : hero.ToUpperInvariant();
            var edition = new KoranEdition { Edition = "EDISI KHUSUS • " + Clock(seconds) + " MENUJU KURSI" };
            var news = new List<string>();
            switch (ending)
            {
                case CampaignEnding.BonekaSistem:
                    edition.Headline = hero + " BERKUASA, TAPI SIAPA BOSNYA?";
                    edition.Subhead = tookLoan
                        ? "Konsorsium pemberi pinjaman mengaku \"hanya membantu\". Izin tambang menyusul minggu depan."
                        : "Para pemilik jatah sudah antre di pintu Istana sebelum pelantikan selesai.";
                    edition.Archetype = "BONEKA SISTEM";
                    news.Add("Utang politik menumpuk: " + jatah + " pihak menagih kursi komisaris.");
                    news.Add("Kebijakan pertama: pemutihan izin usaha para donatur kampanye.");
                    break;
                case CampaignEnding.RajaKoalisi:
                    edition.Headline = hero + " DILANTIK, KABINET GEMUK MENANTI";
                    edition.Subhead = "Kursi menteri ditambah menjadi 74 agar semua \"teman perjuangan\" kebagian.";
                    edition.Archetype = "RAJA KOALISI";
                    news.Add("Jatah politik: " + jatah + " lembaga menunggu balas budi di rapat tertutup.");
                    break;
                default:
                    edition.Headline = hero + " REBUT KURSI TANPA KOALISI";
                    edition.Subhead = "\"Stabilitas terjaga,\" kata juru bicara yang juga merangkap komandan lapangan.";
                    edition.Archetype = "TAKHTA BESI";
                    news.Add("Semua lembaga ditaklukkan lewat adu kuat; pengamat diminta \"tidak menggiring opini\".");
                    break;
            }

            if (majelis == SectorPath.Dirangkul)
                news.Add("Rekomendasi koalisi terbit pukul 02.00 setelah \"mahar\" diterima; Ketum: \"itu uang saksi\".");
            else if (majelis == SectorPath.Dilawan)
                news.Add("Ketum koalisi dilawan terbuka; elite partai mendadak \"sejak awal mendukung\".");
            if (biro == SectorPath.Dirangkul)
                news.Add("Berkas pencalonan beres lewat calo dalam 5 menit. Warga lain: \"sistem sedang gangguan, balik besok\".");
            else if (biro == SectorPath.Dilawan)
                news.Add("Tiga loket kelurahan ditaklukkan; Pak Lurah mengaku \"sudah sesuai prosedur\".");

            news.Add(restu >= 70
                ? "Survei: rakyat puas " + restu + "%. Yang tidak puas sedang \"diberi pembinaan\"."
                : restu >= 40
                    ? "Survei: kepuasan rakyat " + restu + "%. Pemerintah: \"angka itu hoaks yang sistematis\"."
                    : "Survei: kepuasan rakyat tinggal " + restu + "%. Tagar #KaburAjaDulu kembali trending.");
            news.Add(runtuh > 0
                ? hero + " sempat tumbang " + runtuh + " kali; tim mengaku \"hanya kelelahan bekerja untuk rakyat\"."
                : "Tidak sekali pun tumbang; tim medis Istana kecewa tidak kebagian panggung.");
            news.Add(modal > 0
                ? "Sisa dana kampanye " + modal + " Modal; Komisi Antirasuah \"masih mempelajari\"."
                : "Kas kampanye habis tak bersisa. Laporan keuangan menyusul \"setelah situasi kondusif\".");
            news.Add(HeroLine(hero));

            edition.News = news.ToArray();
            return edition;
        }

        // Each hero gets one jab of equal weight.
        public static string HeroLine(string hero)
        {
            switch (hero)
            {
                case "MEGA": return "Partai menyebut kemenangan ini \"hasil kerja keras petugas\", bukan kerja keras calon.";
                case "GEMOY": return "Perayaan diisi joget dan makan siang gratis; menunya dirahasiakan demi keamanan.";
                case "ABAH": return "Pidato kemenangan berlangsung dua jam; kata \"perubahan\" disebut 47 kali.";
                case "PAK WI": return "Kemenangan diresmikan dengan gunting pita proyek yang belum selesai dibangun.";
                default: return "Pelantikan digelar meriah; anggaran perayaan masuk pos \"efisiensi\".";
            }
        }

        public static string Clock(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }
    }
}
