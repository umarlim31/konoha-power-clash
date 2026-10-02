# Visi v3 — "HIDUP DI KONOHA": dari Warga Biasa sampai Presiden

Status: **USULAN untuk diputuskan owner** (2026-10-02). Belum ada yang dikunci. Kalau disetujui, dokumen ini menjadi arah produk baru dan `GAME_LOGIC_JALUR_TAKHTA_v2.md` turun menjadi aturan untuk **level terakhir (Presiden)**.

Masukan owner setelah memainkan 0.5.0:
- Game masih terlalu mudah, karena mahar dan calo bisa dibayar walaupun hero tidak punya apa-apa. Uang seharusnya dikumpulkan dulu, entah dengan cara bersih, korupsi, atau kerja keras.
- Pilar kuning merusak visual. Lebih baik petunjuk arah kecil dan suara.
- Karakter harus lebih realistis.
- Perkelahian harus memancing reaksi warga: ada yang melerai dan menasihati, ada yang teriak memanggil warga lain, warga berbondong-bondong datang, lalu polisi datang naik mobil atau motor.
- Level 1: semua karakter warga biasa. Lalu naik bertahap, level demi level, sampai jadi presiden dan bisa mengatur Konoha.
- Rasanya seperti hidup di Indonesia: kaum rebahan, main sosmed, dan sebagainya. Mungkin bisa jadi "simulator hidup di Indonesia".
- Mungkin karakternya bisa diri owner sendiri.

---

## 1. Rekomendasi utama

> **Jadikan game ini "KARIER POLITIK" bertahap: 5 level dari warga biasa sampai presiden. Setiap level punya peta kecil yang padat, misi cari uang dengan cara bersih atau kotor, dan pemilihan di akhir level.**
> **Jangan jadikan open-world life sim penuh** seperti GTA atau The Sims.

Kenapa:
| | Open-world life sim penuh | **Karier bertahap per level (rekomendasi)** |
|---|---|---|
| Rasa "hidup di Indonesia" | Sangat kuat | **Kuat**, karena setiap level punya kehidupan sendiri |
| Ukuran pekerjaan | Raksasa: puluhan sistem, ratusan aset, tim besar | **Bisa dicicil**: satu level = beberapa build |
| Cocok untuk owner (tablet, tanpa Editor, jatah build terbatas) | Tidak | **Ya** |
| Arah yang jelas untuk pemain | Mudah bingung | **Jelas**: target level selalu terlihat (jadi Ketua RT, Kades, dst.) |
| Satir | Tersebar | **Tajam**: setiap level menyindir tingkat kekuasaan yang berbeda |
| Memakai kerja yang sudah ada | Sebagian | **Hampir semua**: ibu kota sekarang menjadi level Presiden |

Rasa "simulator kehidupan" tetap ada: siklus hari, kerja, sosmed, tetangga, ronda, dan polisi. Bedanya, semuanya diarahkan ke **satu tujuan per level**, supaya pemain tidak bingung dan game bisa selesai dibuat.

---

## 2. Tangga karier (5 level)

| Level | Jabatan yang dikejar | Peta | Cari uang & suara | Satir utama | Sumber |
|---|---|---|---|---|---|
| **1 — WARGA BIASA** | **Ketua RT** | Kampung + jalan raya (sudah ada) | Ojol, kuli, jualan gorengan, konten kreator, buzzer hoaks, jaga parkir liar | Rebahan, sosmed, hoaks grup WA, "no viral no justice", tetangga kepo | **Baru** |
| **2 — KETUA RT → KEPALA DESA** | Kades | Desa + balai desa | Dana desa (bersih: bangun jalan; kotor: proyek fiktif), tengkulak, BUMDes | Dana desa, pungli surat, proyek asal jadi | Baru |
| **3 — CALEG** | Anggota Dewan | Kota (ruko, pasar, kantor partai) | Kampanye, baliho, bansos, **mahar** ke partai, serangan fajar | Mahar politik, baliho, politik uang | **Sudah ada sebagian** (Rekomendasi Koalisi, Kelurahan/Calo) |
| **4 — KEPALA DAERAH** | Walikota/Gubernur | Kota besar | APBD, investor, proyek strategis | Banjir, macet, pajak naik, mobil dinas mewah | Baru |
| **5 — PRESIDEN** | Kursi Istana, lalu **mengatur Konoha** | Ibu kota (sudah ada) | Koalisi, Konsorsium, kartu kebijakan | Kabinet gemuk, ubah aturan, krisis nasional | **Sudah ada** (Musim Pemilu + Garda + Kursi + Koran) |

Setiap level selesai dengan **pemilihan**. Hasilnya muncul sebagai pengumuman satir: **Grup WA RT** di Level 1, **Koran Kampung** di Level 2, lalu **Koran Konoha** di level berikutnya.
Uang, Restu, Jatah, Catatan Hitam, dan Followers **dibawa ke level berikutnya**. Utang dan dosa dari level awal bisa menghantui di level atas, misalnya Komisi Antirasuah membuka "kasus lama".

---

## 3. Level 1 "WARGA BIASA" secara detail (target build berikutnya)

### Tokoh: **kamu sendiri**
- **Pembuat karakter:** nama, laki-laki atau perempuan, warna kulit, rambut/jilbab/peci, dan warna baju. Pemain menjadi **dirinya sendiri** sebagai warga Konoha. Ini menjawab ide "pakai diriku sendiri" **tanpa** merusak satir: dunianya tetap satir, tokohnya kamu.
- Wajah foto asli (scan wajah) **tidak** disarankan sekarang. Teknologinya berat untuk pipeline kita, dan perlu diverifikasi.
- Keempat hero (MEGA, GEMOY, ABAH, PAK WI) muncul di level atas sebagai **rival dan tokoh politik**. Mereka juga tetap bisa dimainkan di mode Presiden dan PvP.

### Resource
| | Arti | Naik | Turun |
|---|---|---|---|
| **DUIT (Rp)** | Uang | Kerja (bersih/kotor) | Makan, kontrakan, suap, kampanye |
| **ENERGI** | Tenaga harian | Tidur, makan | Kerja, berkelahi |
| **RESTU WARGA** | Nama baik di kampung | Membantu, melayat, ronda, kerja bakti | Hoaks ketahuan, berkelahi, pungli |
| **FOLLOWERS** | Pengaruh sosmed | Konten viral | Konten basi, dibongkar netizen |
| **CATATAN HITAM** | Risiko hukum | Kerja kotor, suap, kabur dari polisi | (sulit hilang) |

### Cara cari uang (aplikasi di HP: "KONOHA KERJA")
| Kerja | Cara main | Bayaran | Efek samping |
|---|---|---|---|
| **Ojol** | Ambil pesanan, lalu antar ke titik tujuan dalam batas waktu | Kecil, stabil | Energi berkurang; rating bintang |
| **Kuli bangunan** | Angkut semen dari truk ke proyek (berdiri di zona) | Sedang | Energi turun banyak |
| **Jualan gorengan** | Pagi hari, layani pembeli yang datang ke lapak | Kecil | Restu naik sedikit |
| **Konten kreator** | Rekam kejadian (motor jatuh, demo, warga ribut), lalu posting | Tidak pasti; followers naik | Restu turun kalau mengonten musibah |
| **Buzzer bayaran** | Sebar hoaks di grup WA warkop | **Besar dan cepat** | Catatan Hitam naik; bisa kena "Pasal Karet" |
| **Jaga parkir liar** | Berdiri di trotoar dan tagih motor yang parkir | Sedang | Catatan Hitam naik; bisa ribut dengan preman |

Dengan begini, **"bayar petugas" baru bisa dilakukan kalau uangnya memang sudah dikumpulkan**. Uang kotor membuat jalan lebih cepat tetapi meninggalkan Catatan Hitam. Itu inti satirnya.

### Kehidupan kampung (siklus hari ±8 menit nyata)
- **Pagi:** ibu-ibu ngerumpi di tukang sayur, anak sekolah, macet motor.
- **Siang:** kerja, ojol, warkop penuh bapak-bapak.
- **Sore:** main bola di lapangan, layangan.
- **Malam:** ronda, kafe sosmed, rebahan.
- **Acara acak:** rumah duka (melayat, Restu naik), hajatan dangdut, kerja bakti, arisan, motor jatuh, maling ayam, debt collector.
- **Rebahan & sosmed:** di rumah bisa "scroll HP". Energi pulih, tetapi waktu habis, dan kadang tergoda ikut menyebar hoaks (pilihan).

### Perkelahian = urusan satu kampung
Kalau terjadi perkelahian:
1. Warga terdekat **teriak** ("WOI! ADA YANG BERANTEM!"). Warga lain **berbondong-bondong datang** dan merekam pakai HP.
2. Satu atau dua warga **melerai**: berdiri di antara kedua pihak dan menasihati ("Sudah, sudah, malu sama tetangga!"). Perkelahian berhenti sebentar.
3. Kalau tetap berlanjut, **meter POLISI** naik. **Polisi datang naik motor atau mobil patroli** dengan sirene, lalu pemain memilih:
   - **KABUR:** Catatan Hitam naik, dan dikejar sebentar.
   - **DAMAI DI TEMPAT:** bayar Duit. Satir pungli.
   - **IKUT KE POLSEK:** waktu dan Duit hilang.
4. Melayani dengan sabar (tidak membalas) membuat Restu naik.

Combat hero tetap ada, tetapi di Level 1 **berkelahi punya akibat sosial**. Combat penuh baru menjadi inti di level atas, misalnya Garda Istana.

### Arahan pemain (pengganti pilar kuning)
- **Panah kecil di tanah** di sekitar kaki hero yang menunjuk ke tujuan, plus jarak di HUD.
- **Suara petunjuk:** bunyi notifikasi HP dan pesan singkat bergaya chat ("Pak RT: Mas, ditunggu di pos ronda").
- **Daftar langkah** tetap ada, dan bisa disembunyikan.

### Tujuan Level 1: menang pemilihan **Ketua RT**
- **Syarat mendaftar:** Restu Warga ≥ 50 **dan** Duit Rp 2 juta untuk "syukuran" (nasi kotak). Ini satir: jabatan RT pun butuh modal.
- **Rival:** *Juragan Kos* yang membagi sembako dan menyewa preman, serta *Pak Haji* yang sudah lama dihormati.
- **Hari pemilihan:** warga memilih. Suara dihitung dari Restu, Followers, dan siapa yang pernah dibantu. Bisa juga "serangan fajar" (Duit, Catatan Hitam naik).
- **Ending Level 1** ditampilkan di **Grup WA "RT 03 KONOHA"**. Contoh: *"Selamat kpd Bpk/Ibu [NAMA] Ketua RT baru 🙏 iuran sampah tetap Rp 20rb ya"*.

---

## 4. Karakter lebih realistis: jujur soal batasnya
- Tubuh saat ini dibuat dari **bentuk dasar yang digenerate kode**. Bentuk itu bisa terus dihaluskan (proporsi, tangan, rambut, pakaian), tetapi **tidak akan pernah benar-benar realistis**.
- Lompatan besar hanya bisa dicapai dengan **model karakter 3D sungguhan** (FBX dengan rig humanoid dan animasi). Jalurnya sudah ada untuk hero (`Docs/PANDUAN_MEGA_3D.md`). Usulannya: perluas jalur yang sama menjadi **slot karakter warga**, yaitu 2–4 model dasar (laki-laki, perempuan, anak, berjilbab) dengan variasi warna baju.
- Sumber model rig gratis yang bisa dicek: Quaternius dan Kenney (CC0, gaya low-poly). Animasi bisa dari Mixamo (gratis dengan akun Adobe). Cara mengunduhnya dari tablet perlu dicoba dulu; nanti aku tulis panduannya.

## 5. Masalah jatah build Unity (penting)
Kuota gratis Unity Build Automation sudah terpakai 75%. Kalau habis, pengembangan berhenti. Ada dua usulan:
1. **Hemat:** satu build per versi besar (seperti 0.5.0), dan setiap perubahan diperiksa ketat sebelum build.
2. **Cari jalur build kedua:** build Android lewat **GitHub Actions + GameCI** dengan lisensi Unity Personal. Akun GitHub gratis untuk repo privat mendapat kuota menit bulanan. Ini **perlu diverifikasi dulu**: syarat lisensi Unity untuk CI, berapa menit satu build Android, dan apakah cukup. Kalau layak, owner cukup memasukkan beberapa *secret* di pengaturan GitHub dari tablet, dan aku menyiapkan workflow-nya. Workflow lama `android-spike.yml` sudah usang dan akan diganti.

## 6. Urutan pengerjaan yang diusulkan
| Versi | Isi | Build |
|---|---|---|
| **0.6.0** | **Fondasi Level 1:** menu baru (Karier / Presiden / PvP), pembuat karakter sederhana, Duit + Energi + Restu + Catatan Hitam, HP "KONOHA KERJA" dengan **3 kerja** (Ojol, Kuli, Buzzer Hoaks), **reaksi warga saat berkelahi + polisi datang**, panah kecil + suara (pilar dihapus), simpan progres | 1 |
| 0.6.1 | Siklus hari (pagi–malam), tidur/rebahan, 3 kerja lain (gorengan, konten, parkir), acara kampung (melayat, ronda, kerja bakti) | 1 |
| 0.6.2 | **Pemilihan Ketua RT** + rival + Grup WA ending, sehingga Level 1 bisa selesai | 1 |
| 0.6.3 | Slot **model warga 3D** (kalau owner punya modelnya) + penghalusan tubuh | 1 |
| 0.7.x | Level 2 Kepala Desa | 2–3 |
| 0.8.x | Level 3 Caleg (memakai ulang Koalisi/Kelurahan), Level 4 Kepala Daerah | 3–5 |
| 0.9.x | Level 5 Presiden: ibu kota sekarang + Krisis + mengatur Konoha | 2–3 |

Mode **Jalur Takhta** yang sekarang tetap ada di menu sebagai **"Mode Presiden (cepat)"**, jadi kerja selama ini tidak hilang.

## 7. Keputusan yang dibutuhkan dari owner
1. Setuju arah **Karier bertahap (5 level)**, bukan open-world life sim penuh?
2. Tokoh Level 1 = **avatar buatanmu sendiri** (pembuat karakter), dengan keempat hero sebagai rival dan tokoh di level atas?
3. Build berikutnya = **0.6.0 Fondasi Level 1** seperti tabel di atas?
4. Mau aku cek kemungkinan **jalur build kedua (GitHub Actions)** supaya tidak bergantung pada kuota Unity?
