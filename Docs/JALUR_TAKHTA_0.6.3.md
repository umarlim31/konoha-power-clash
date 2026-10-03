# 0.6.3 — KARIER "Siang & Malam": atap tetap, dunia padat, tabrak lari, hari berganti

Branch kerja: `feat/0.6.3-hidup-siang-malam`, dibuat dari `feat/0.6.2-misi-terarah`. Branch ini sudah berisi 0.6.0–0.6.2. PR ke `feat/jalur-takhta-first-playable`, **bukan** ke `main`.

Status: compile Unity, EditMode test, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini:
- pemeriksaan sintaks C#;
- `scripts/source-check.py`;
- review independen.

## Build manual (Unity Build Automation)
1. Branch **`feat/0.6.3-hidup-siang-malam`**, Unity **6000.0.60f1**, pre-export **`Konoha.Editor.SpikeProject.prepare`**.
2. APK versi **0.6.3**, kode **49**, label `JALUR TAKHTA 0.6.3  •  SOLO PREVIEW`.

## Masukan owner (0.6.2)
- Masuk pondok, **atapnya masih hilang**. Atap harus tetap ada dan tampilan pindah ke dalam ruangan.
- Hero **bisa menembus apa saja**. Rumah hanya bisa dimasuki lewat pintu; kalau tidak ada pintu, tidak bisa masuk.
- Naik motor ojol **bisa menabrak dan menembus apa saja**. Kalau menabrak orang, warga harus berdatangan ("tabrak lari!") dan polisi mengejar. Tertangkap berarti sel, kecuali menyogok.
- Karakter harus lebih natural.
- Lanjutkan siklus siang–malam dan kerja tambahan.
- Setelah semua misi selesai bingung harus apa. Perlu pemberitahuan "selamat, tugas selesai, boleh tidur/istirahat/kerja lagi".

## Kenapa atap masih hilang di 0.6.2
Pondok di taman adalah **model 3D**. Bagian atasnya (yang boleh memudar) ikut membawa potongan ujung tiang, sehingga batas bawah "atap" tercatat **di lantai**. Akibatnya deteksi "berdiri di bawah atap" tidak pernah aktif, dan atap disembunyikan seperti biasa.

Sekarang generator mencatat tinggi bawah atap yang sebenarnya (garis potong model). Sementara hero di dalam, kamera **dijaga tetap di bawah atap**: sudut kamera, zoom, dan tinggi pandang dibatasi setiap frame.

## Yang baru

### 1. Atap tetap ada, kamera masuk ke ruangan
- Pondok/pendopo model 3D, warung, POLSEK, dan bangunan beratap lain: atap tidak hilang.
- Kamera turun ke dalam, rendah dan dekat. Selama di dalam, kamera tidak bisa diputar naik menembus atap.
- Keluar ruangan, kamera kembali seperti semula.

### 2. Dunia padat di KARIER (tidak bisa ditembus)
- Rumah, ruko, gerobak (jajan dan sayur), pikap, rak sepeda, motor parkir, pohon (batangnya saja), tiang lampu, pagar, bangku, kursi rumah duka, dan dinding taman sekarang **padat**.
- Rumah tanpa ruang dalam tidak bisa dimasuki. Yang bisa dimasuki hanya bangunan berpintu/terbuka: pendopo, POLSEK, pos ronda.
- Naik motor, sepeda, atau ojol juga tertahan benda padat.
- Tempat yang dituju KARIER (zona kerja, gerobak jajan, sewa, POLSEK, titik misi, spawn) dijamin tetap bebas. Test scene memeriksanya.
- **Hanya di KARIER.** MODE PRESIDEN tidak berubah (rutenya sudah teruji).
- Warga yang jalurnya menabrak benda padat dipotong jalurnya, atau berdiri diam. Ini juga terlihat di MODE PRESIDEN (satu-satunya perubahan di sana: beberapa pejalan kaki lebih sedikit).

### 3. TABRAK saat berkendara
- Melaju kencang ke arah warga → warga **terjatuh**, warga sekitar berkerumun dan teriak **"TABRAK! TABRAK! JANGAN KABUR!"**.
- Pilihan:
  - **TANGGUNG JAWAB**: antar ke puskesmas, ganti rugi Rp 150.000, RESTU −1.
  - **KABUR**: CATATAN HITAM +15, RESTU −6, motor patroli **mengejar** dengan sirene, sedikit lebih lambat dari motormu. Selama panel terbuka, hero berhenti (tabrakan menghentikan motor), jadi kamu sempat membaca pilihan.
- Bertahan **35 detik** tanpa tertangkap → **LOLOS**, tapi catatan hitam tetap.
- Tertangkap → pilih **DAMAI** (sogok Rp 300.000 + catatan) atau **IKUT KE POLSEK** (borgol → dibonceng → sel, seperti 0.6.1). Tidak bisa kabur dua kali.
- Saat hero berkendara, warga menghindar lebih awal dan lebih jauh, jadi tabrakan hanya terjadi kalau memang menyeruduk.

### 4. Siang dan malam
- **1 hari Konoha = 10 menit** main. Mulai jam 07.00. Jam dan hari tampil di baris nama ("HARI 2, 19.30 MALAM") dan di HP.
- Langit, matahari, dan cahaya berubah: pagi hangat, siang terang, senja oranye, malam biru gelap berbulan. Lampu jalan dan lentera **menyala di malam hari**.
- Malam: dua dari tiga pejalan kaki pulang, preman lebih sering muncul, ojol dapat **tarif malam +25%**.
- **TIDUR** (HP, jam 19.00–04.00): layar gelap, bangun jam 06.00 dengan ENERGI penuh, hari berikutnya.
- REBAHAN sekarang memakan 2 jam waktu.

### 5. Kerja tambahan
| Kerja | Jam | Cara | Hasil |
|---|---|---|---|
| **PARKIR** (liar) | 08.00–21.00 | Berdiri di depan KANTOR KELURAHAN, 6 motor (±4 dtk per motor). Pergi sebentar hanya menjeda | Rp 2.000/motor − setoran 40% "bos parkir", RESTU −1 |
| **RONDA MALAM** | 21.00–03.00 | Panah ke MULUT GANG → WARKOP → TAMAN BARAT → lapor POS RONDA | Rp 30.000, RESTU +3 |
| KULI | 07.00–17.00 | (sama seperti sebelumnya; malam proyek tutup) | |

HP kini dua kolom: OJOL, KULI, PARKIR, RONDA, BUZZER, REBAHAN, TIDUR. Pekerjaan yang tutup ditandai jam bukanya.

### 6. Setelah semua misi: ucapan selamat + TUGAS HARIAN
- Begitu misi 12 selesai (atau saat memuat simpanan yang sudah tamat), **Grup WA Pak RT** mengucapkan selamat dan menjelaskan pilihanmu: tugas harian, kerja lagi, rebahan/tidur. Muncul sekali saja.
- Panel kiri berganti menjadi **TUGAS HARIAN • HARI n**, berisi 3 tugas per hari:
  1. antar 2 penumpang OJOL;
  2. KULI (hari genap) atau PARKIR (hari ganjil);
  3. JAJAN 2x (hari genap) atau RONDA MALAM (hari ganjil).
- Tiap tugas Rp 15.000 + RESTU +1, bonus **Rp 30.000** kalau ketiganya beres. Panah atau HP berkedip menunjuk tugas berikutnya. Kalau tugas belum buka (misalnya ronda di siang hari), petunjuk menyarankan tugas lain atau TIDUR.
- Misi KULI di malam hari kini memberi petunjuk: "Proyek tutup malam. HP > TIDUR".

### 7. Warga lebih natural
- Sesekali **berhenti beberapa detik**: cek HP atau menoleh kiri-kanan, lalu jalan lagi. Mulai dan berhenti jalan perlahan, tidak langsung kencang.
- Warga yang berdiri memindahkan tumpuan kaki.
- Warga yang ditabrak terbaring memegangi kaki, lalu bangun lagi.
- Batasnya jujur: bentuk tubuh masih bentuk dasar dari kode. Lompatan besar tetap butuh model 3D manusia (slot model warga, rencana 0.6.5).

## Simpanan
- Format simpanan **K4** (hari, jam, hitungan parkir/ronda/tabrak, tugas harian, ucapan sudah dilihat).
- Simpanan 0.6.0–0.6.2 tetap terbaca. Kalau semua misi sudah selesai, ucapan selamat muncul sekali.

## Sengaja tidak diubah
- Combat, PvP, MODE PRESIDEN (rute, cahaya siang tetap, tanpa benda padat baru), fitur 0.0.8.2, folder Art.
- Mobil di jalan lingkar tetap tanpa tabrakan (di luar area main oval).

## Checklist uji perangkat 0.6.3
1. Label 0.6.3. KARIER → LANJUTKAN HIDUP (simpanan lama yang sudah 12/12). **Grup WA ucapan selamat** muncul. Panel kiri menjadi **TUGAS HARIAN • HARI 1**.
2. Masuk **pondok/pendopo taman** (yang atapnya dulu hilang) dan **POLSEK**. Atap tetap ada, kamera di dalam. Coba geser layar ke atas: kamera tidak menembus atap.
3. Jalan ke rumah, gerobak siomay/salome, pohon, tiang lampu: **tidak bisa ditembus**. Pastikan semua titik misi/kerja tetap bisa dicapai (gerobak, sewa, POLSEK, pos ronda, tumpukan semen, proyek).
4. Sewa MOTOR, lalu sengaja tabrak warga yang berjalan:
   - pilih **TANGGUNG JAWAB** → uang berkurang Rp 150.000;
   - tabrak lagi lalu **KABUR**/tancap gas → motor polisi mengejar, hitung mundur di baris atas. Coba lolos 35 detik, dan coba juga berhenti sampai tertangkap → DAMAI atau POLSEK.
5. Tunggu senja (sekitar jam 18.00, ±6–7 menit main): langit oranye lalu gelap, **lampu jalan menyala**, warga berkurang. HP > **TIDUR** → layar gelap → HARI 2, jam 06.00.
6. HP > **PARKIR** (siang) di depan KANTOR KELURAHAN. Malam (setelah 21.00) HP > **RONDA MALAM**, ikuti panah 3 titik lalu kembali ke POS RONDA.
7. Amati pejalan kaki: kadang berhenti dan cek HP, lalu jalan lagi.
8. **MODE PRESIDEN** sekali: rute dan musuh seperti 0.6.2 (tidak ada benda padat baru, tetap siang).

## Risiko
- Benda padat baru dibuat otomatis dari bentuk dekorasi. Ada kemungkinan satu-dua jalan sempit tertutup (pagar/bangku berdekatan). Hero masih bisa LOMPAT. Mohon screenshot kalau ada jalur yang buntu.
- Warga yang jalurnya terpotong benda padat berdiri diam atau hilang, sehingga beberapa sudut terasa lebih sepi.
- Polisi mengejar dengan kecepatan sedikit di bawah motor (mengikuti kecepatanmu). Kalau terlalu mudah atau terlalu sulit lolos, angkanya mudah disetel.
- Cahaya malam di layar tablet bisa terasa terlalu gelap atau terlalu terang. Angkanya juga mudah disetel.
- Angka kerja, tugas harian, dan denda masih angka awal.
