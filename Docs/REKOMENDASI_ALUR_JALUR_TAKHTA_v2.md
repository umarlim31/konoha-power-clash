# Rekomendasi Alur Jalur Takhta v2 — "Musim Pemilu"

Status: **DISETUJUI owner 2026-10-02** → dikunci sebagai `Docs/GAME_LOGIC_JALUR_TAKHTA_v2.md`. (Teks di bawah adalah usulan aslinya.) Design authority tetap `Docs/GAME_LOGIC_JALUR_TAKHTA_v1.md` sampai owner menyetujui dokumen ini. Setelah disetujui, isinya dipindahkan ke `GAME_LOGIC_JALUR_TAKHTA_v2.md`.

Dasar usulan ini:
- keluhan owner (2026-10-02): "Mega masuk, penjaga dihabisin, ke Majelis Daun, baru ke Prosedur… aneh, kurang menarik";
- arah Game Concept Bible v2: Modal, Koneksi, 3 dari 5 segel, dan arketipe akhir sudah direncanakan tetapi ditunda.

---

## 1. Kenapa alurnya terasa aneh sekarang

| Masalah | Yang dirasakan pemain |
|---|---|
| **Daftar tugas, bukan cerita.** Gerbang → Majelis → Biro → Garda → Kursi. | "Aku disuruh ke sini, lalu ke sana." Tidak ada alasan kenapa harus ke Majelis atau Biro. |
| **Tidak ada pilihan.** Satu-satunya cara maju adalah memukul. | Setiap run sama, jadi tidak ada alasan main lagi. |
| **Tidak ada akibat.** Apa pun yang dilakukan, akhirnya sama. | Kemenangan terasa hambar dan arketipe selalu "Takhta Besi". |
| **Satirnya hanya di nama.** Majelis = musuh berjas, Biro = loket. | Lucunya sekali lihat, setelah itu jadi combat biasa. |
| **Kroni di Gerbang Rakyat tidak dijelaskan.** | Kenapa calon pemimpin langsung memukuli orang di gerbang? |
| **Tidak ada eskalasi.** Bahaya terbesar ada di Garda. | Fase Memerintah terasa seperti "tunggu bar penuh". |
| **Tidak ada hadiah antar-run.** | Begitu menang sekali, tidak ada yang dikejar. |

Inti masalahnya: **game politik tanpa keputusan politik.** Di dunia nyata, jalan ke kursi kekuasaan penuh pilihan kotor: koalisi, bagi-bagi, ganti aturan. Justru di situ satirnya paling tajam, dan di situ pula pemain jadi candu ("run berikutnya aku coba jalur licik").

---

## 2. Fantasi inti yang baru

> **"Semua jalan menuju Kursi itu kotor. Pilihanmu cuma: kotor yang mana."**

Pemain tidak lagi sekadar menerobos sistem. Pemain **memainkan** sistem: melawannya, merangkulnya, atau membelinya. Setiap pilihan punya tagihan yang datang saat ia sudah duduk di Kursi.

**Prinsip satir:**
1. **Yang disindir adalah perilaku dan sistemnya.** Sasarannya praktik seperti buzzer, bansos jelang pemilu, ordal, dan ketok palu dini hari, bukan tuduhan pidana spesifik terhadap orang nyata.
2. **Keempat hero disindir sama kerasnya.** Tidak ada hero yang "benar". Ini membuat satir terasa adil, lebih lucu, dan bisa dinikmati pemain dari kubu mana pun.
3. **Sindiran ada di mekanik, bukan hanya di tulisan.** Contoh: Biro membuatmu "Salah Loket" sungguhan, dan Bansos memberi suara instan tetapi menambah Utang.

---

## 3. Struktur satu run: "Musim Pemilu" (target 12–15 menit)

```
BABAK 0  PENDAFTARAN CALON  → pilih hero + kartu LATAR BELAKANG
BABAK 1  KAMPANYE           → kumpulkan SUARA di Gerbang Rakyat & kampung
BABAK 2  LOBI               → di Plaza, pilih 2–3 lembaga: LAWAN atau RANGKUL
BABAK 3  SIDANG KILAT       → "ubah aturan main" sebelum gerbang terakhir
BABAK 4  GARDA TAKHTA       → boss (yang ada sekarang, dimodifikasi aturan Babak 3)
BABAK 5  MEMERINTAH         → bertahan dari KRISIS, sementara utang politik ditagih
AKHIR    KORAN KONOHA       → headline satir sesuai cara kamu berkuasa (5+ ending)
```

### Babak 0 — Pendaftaran Calon (±30 detik)
Pilih hero, lalu pilih **1 dari 3 kartu Latar Belakang** (acak dari 6). Kartu ini menentukan modal awal dan menyindir privilese:

| Kartu | Efek awal | Satir |
|---|---|---|
| **Anak Pejabat** | Modal +50, satu gerbang "dibukakan Om" (lewati 1 penjaga) | dinasti / orang dalam |
| **Pengusaha Tambang** | Modal +80, Restu −10 | oligarki |
| **Mantan Jenderal** | Wibawa +20, bawahan Garda ragu menyerang | militerisme |
| **Aktivis Kampus** | Restu +20, Modal 0 | idealis tanpa logistik |
| **Artis Viral** | Suara +30 di awal, skill ULT lebih cepat terisi | popularitas > kompetensi |
| **Ustaz Kondang** | Restu +15 di kampung, warga ikut membantu | politik identitas |

### Babak 1 — Kampanye (Gerbang Rakyat → kampung)
Ada resource baru, **SUARA** (0–100). Gerbang ke Plaza dijaga **Komisi Suara Konoha (fiktif)** dan hanya terbuka di Suara 60 ke atas. Kroni di gerbang sekarang punya alasan: mereka **Buzzer Bayaran lawan**.

Cara mendapat Suara (bebas dikombinasikan):
- **Blusukan:** berdiri di zona kerumunan warga untuk mengisi Suara, sementara Buzzer menyerang. Ini combat ringan sambil memegang zona.
- **Bagi Bansos:** bayar Modal untuk Suara instan, tetapi Utang +1. Warga berebut, jadi ada sedikit chaos.
- **Debat Kandidat:** kalahkan "Calon Boneka" (mini-boss) untuk Suara besar.
- **Serangan Fajar** (rahasia, jam pagi di siklus hari): Suara besar, tetapi kalau ketahuan "Pengawas" ada denda Restu.

### Babak 2 — Lobi (Plaza Aspirasi, hub)
Plaza menjadi **hub yang sebenarnya**: 3 lembaga sekarang (maksimal 5 nanti), dan Gerbang Dalam butuh **2 Segel** (nanti 3 dari 5). Urutan bebas dan ikonnya terlihat di peta.

Setiap lembaga punya dua jalur:
- **LAWAN** (combat, mekanik faksi yang sudah ada) → Segel + Pengaruh + Restu ("berani melawan sistem").
- **RANGKUL** (bayar Modal / pakai Koneksi) → Segel **instan** tanpa bertarung, dan anggota faksi menjadi sekutu di Garda. Konsekuensinya: **JATAH +1**, ditagih saat Memerintah.

Lembaga:
| Lembaga | Kalau dilawan | Kalau dirangkul | Satir |
|---|---|---|---|
| **Majelis Daun** (ada) | Pecahkan Blok, Ketua dengan KETOK PALU | "Sudah kita bahas di rapat tertutup." Segel keluar jam 02.00. | legislasi kilat |
| **Biro Prosedur** (ada) | 3 loket, Stempel Tunda, Salah Loket | Jalur "uang pelicin": loket langsung tercap | pungli dan birokrasi |
| **Komisi Suara Konoha** (baru) | Lindungi kotak suara dari "penggelembungan" | "Rekap ulang" menguntungkanmu, tetapi kecurigaan publik naik | sengketa hasil pemilu |
| *Konsorsium Modal* (nanti) | Hentikan proyek mangkrak | Dapat Modal besar, tetapi Utang naik | oligarki proyek |
| *Menara Narasi* (nanti) | Kalahkan buzzer dan influencer | Narasi positif: Restu stabil | media dan buzzer |

**Kartu Kebijakan** (pengikat roguelite): setiap selesai satu lembaga, pilih **1 dari 3 kartu**. Kartu bertumpuk menjadi "build":
- **Pasal Karet:** musuh yang mengejekmu (ada teks ejekan) terkena stun 1 detik.
- **Ordal:** satu loket Biro langsung tercap.
- **Proyek Strategis:** ULT PAK WI meninggalkan "proyek mangkrak" yang menghalangi musuh.
- **Pencitraan:** saat Wibawa di bawah 30%, muncul perisai sekali.
- **Rapat di Hotel:** heal penuh, tetapi Restu −5.
- **Buzzer Sendiri:** satu Kroni lawan berubah memihakmu.
- … (kumpulan kartu bisa terus bertambah tiap versi)

### Babak 3 — Sidang Kilat (sebelum Gerbang Dalam)
Satir paling tajam ada di sini: **ubah aturan main**. Pilih 1 dari 3, dan pilihan itu mengubah aturan pertarungan Garda dan Memerintah:
- **Revisi Batas Usia:** "calon pendamping" ikut sebagai NPC sekutu di Garda, tetapi Restu −10.
- **Perpanjang Masa Jabatan:** target Power Memerintah lebih kecil, tetapi krisis lebih sering.
- **Tunda Pemilu:** krisis Demo Mahasiswa pasti muncul.
- **Taat Konstitusi** (pilihan jujur): tanpa bonus, Restu +15, dan mengarah ke ending terbaik.

### Babak 4 — Garda Takhta
Boss yang sudah ada, ditambah efek pilihan sebelumnya:
- lembaga yang dirangkul mengirim sekutu;
- lembaga yang dilawan mengirim sisa pasukan;
- kartu Latar Belakang ikut berpengaruh (misalnya Mantan Jenderal membuat Guard ragu).

### Babak 5 — Memerintah: krisis, bukan menunggu bar
Power tetap naik saat duduk, tetapi setiap 15–20 detik datang **KRISIS** (3 dari kumpulan, acak):
- **Demo Mahasiswa:** kerumunan besar mendekati Istana. Pilih **DENGAR** (berdiri di podium beberapa detik: Power berhenti, Restu naik) atau **BUBARKAN** (cepat, Restu turun tajam, dan muncul "Viral").
- **Koalisi Minta Jatah:** setiap JATAH muncul sebagai NPC penagih. Bayar dengan Modal, atau tolak dan dia **berkhianat** (menjadi musuh elite).
- **Harga Beras Naik:** Restu turun pelan sampai kamu menyelesaikan "Operasi Pasar" (zona).
- **Banjir Ibu Kota:** sebagian plaza tergenang dan musuh datang lewat air.
- **Skandal Viral:** kalau Utang tinggi, muncul **Komisi Antirasuah (fiktif)**. Kalahkan atau "klarifikasi" lewat konferensi pers (zona + bertahan).
- **Proyek Mangkrak Ambruk:** rintangan di tengah arena.

### Akhir — "KORAN KONOHA"
Hasil run ditampilkan sebagai **halaman depan koran satir**, dengan headline sesuai cara kamu berkuasa:

| Ending | Syarat | Headline contoh |
|---|---|---|
| **MANDAT RAKYAT** | Restu tinggi, Jatah dan Utang rendah, Taat Konstitusi | "Langka! Pejabat Konoha Lunasi Janji Kampanye" |
| **TAKHTA BESI** | semua lembaga dilawan, krisis dibubarkan | "Stabilitas Terjaga, Kata Pemerintah" |
| **RAJA KOALISI** | mayoritas lembaga dirangkul | "Kabinet Gemuk Dilantik: 74 Menteri, 3 Wamen per Kursi" |
| **SULTAN MODAL** | Modal sangat tinggi | "Ibu Kota Pindah ke Lahan Milik Sendiri" |
| **BONEKA SISTEM** | Jatah atau Utang melewati batas | "Presiden Konoha Akui Tidak Tahu Siapa Bosnya" |
| **DINASTI** (rahasia) | menang dengan Revisi Batas Usia + Anak Pejabat | "Estafet Kepemimpinan Berjalan Mulus, Lagi" |
| **KUDETA** (kalah) | Runtuh 3× saat Memerintah | "Kursi Kosong, Rapat Darurat Digelar" |

---

## 4. Yang membuat candu (alasan main lagi)

1. **Run pendek dan berbeda tiap kali.** Kombinasi Latar Belakang, Kartu Kebijakan, Lawan/Rangkul, dan Krisis membuat setiap run tidak sama.
2. **Koleksi ending.** "Arsip Sejarah Konoha" menampilkan koran untuk setiap ending yang pernah dicapai. Ending rahasia memancing rasa penasaran.
3. **Unlock per ending.** Contoh: ending Dinasti membuka kostum MEGA "Baju Kebesaran", dan ending Mandat Rakyat membuka kartu Latar Belakang baru.
4. **Berita Hari Ini** (mutator harian): misalnya "Hari ini: harga BBM naik, semua Modal −20%". Ada alasan membuka game setiap hari.
5. **Skor Elektabilitas + papan peringkat** (nanti), dihitung dari waktu, ending, dan sedikit Runtuh.
6. **Meter VIRAL:** combo pukulan beruntun mengisi meter. Saat penuh: slow-mo singkat, kamera dramatis, dan teks satir ("TRENDING #1: #KonohaBerduka").
7. **Achievement bernada sindiran:** "Sudah Sesuai Prosedur" (selesaikan Biro tanpa salah loket), "Masuk Angin" (Runtuh di sidang), "Lupa Janji" (ending Boneka 3×).

---

## 5. Bank satir (unsur game ↔ fenomena yang disindir)

| Unsur game | Fenomena |
|---|---|
| Buzzer Bayaran, Menara Narasi | buzzer politik, perang tagar |
| Bansos jelang pemilu, Serangan Fajar | politik uang |
| Ordal, Anak Pejabat | nepotisme |
| Loket, Stempel Tunda, Salah Loket, uang pelicin | birokrasi dan pungli |
| Ketok Palu jam 02.00, rapat tertutup | legislasi kilat |
| Revisi Batas Usia, Perpanjang Masa Jabatan | mengubah aturan demi kekuasaan |
| Kabinet gemuk, Koalisi Minta Jatah | bagi-bagi kursi |
| Proyek mangkrak, jalan rusak | proyek tanpa perencanaan |
| Konferensi pers klarifikasi, "sudah sesuai prosedur" | budaya klarifikasi pejabat |
| Flexing, Rapat di Hotel | gaya hidup pejabat |
| Survei pesanan, baliho raksasa | pencitraan |
| Harga beras, banjir ibu kota, demo mahasiswa | krisis yang dihadapi rakyat |

**Hero:** setiap hero mendapat sindiran khas dengan bobot setara, misalnya ucapan saat skill, reaksi krisis, dan headline ending yang berbeda per hero.

---

## 6. Catatan risiko (singkat)

Owner sudah menyatakan siap menanggung konsekuensinya, jadi ini hanya agar satirnya awet:
- **Pertahankan bingkai fiksi** (Negara Konoha, nama lembaga fiktif, tanpa lambang partai dan logo lembaga nyata, tanpa wajah 1:1). Ini juga syarat Play Store untuk konten satir.
- **Sindir praktik dan perilaku, jangan menuduh orang nyata melakukan pidana tertentu.** Satir jadi lebih kuat (semua orang mengenali fenomenanya) dan lebih sulit dipersoalkan.
- **Tambahkan disclaimer** di layar awal: "Karya satir fiksi. Tokoh, lembaga, dan peristiwa di dalamnya adalah rekaan."

---

## 7. Prioritas pengerjaan

Setiap versi tetap kecil dan bisa diuji.

| Versi | Isi | Kategori |
|---|---|---|
| **0.4.0** | Hub Lobi: **LAWAN / RANGKUL** untuk Majelis dan Biro (resource **MODAL** + **JATAH**), HUD jalur, layar **Koran Konoha** dengan 3 ending awal (Takhta Besi, Raja Koalisi, Boneka) | MVP alur baru: perubahan rasa terbesar |
| **0.4.1** | **Kartu Kebijakan** (pilih 1 dari 3 setelah tiap lembaga, ±10 kartu) + kartu **Latar Belakang** | Penting: kecanduan |
| **0.4.2** | **Kampanye Suara** di Babak 1 (Blusukan, Bansos, Buzzer) + **Komisi Suara Konoha** sebagai lembaga ketiga | Penting |
| **0.4.3** | **Krisis** di Fase Memerintah (Demo, Jatah, Beras, Skandal) + ending Mandat Rakyat dan Sultan Modal | Penting |
| **0.4.4** | **Sidang Kilat** (ubah aturan) + ending Dinasti | Tambahan |
| **0.5.x** | Arsip ending, achievement, Berita Hari Ini, meter VIRAL, kostum | Meta / retensi |
| Nanti | Konsorsium Modal, Menara Narasi, co-op Bentuk Koalisi, papan peringkat | Setelah inti terbukti seru |

Yang **tidak** berubah: combat hero, peta yang sudah ada (dipakai ulang sebagai hub), Garda Takhta, kamera, PvP 4v4.

---

## 8. Keputusan yang dibutuhkan dari owner

1. Setuju dengan struktur **Musim Pemilu** (Babak 0–5 + Koran Konoha)?
2. Mulai dari **0.4.0 (Lawan/Rangkul + Koran)** seperti rekomendasi, atau ada babak lain yang ingin didahulukan?
3. Bayar RANGKUL dengan **Modal saja** (lebih sederhana; rekomendasiku untuk 0.4.0), atau langsung **Modal + Koneksi**?
4. Seberapa tajam sindiran personal untuk tiap hero: **sedang** (sindiran gaya dan kebiasaan) atau **tajam** (sindiran kebijakan dan rekam jejak, tetap tanpa tuduhan pidana)?
