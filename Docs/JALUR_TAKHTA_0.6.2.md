# 0.6.2 — KARIER "Misi Terarah": selalu tahu harus apa, atap tidak hilang, tidak tembus tembok

Branch kerja: `feat/0.6.2-misi-terarah`, dibuat dari `feat/0.6.1-hidup-di-kampung`. Branch ini sudah berisi 0.6.0 dan 0.6.1. PR ke `feat/jalur-takhta-first-playable`, **bukan** ke `main`.

Status: compile Unity, EditMode test, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini:
- pemeriksaan sintaks C#;
- `scripts/source-check.py`;
- review independen. Tidak ada error compile yang ditemukan. Temuan runtime sudah diperbaiki.

## Build manual (Unity Build Automation)
1. Branch **`feat/0.6.2-misi-terarah`**, Unity **6000.0.60f1**, pre-export **`Konoha.Editor.SpikeProject.prepare`**.
2. APK versi **0.6.2**, kode **48**, label `JALUR TAKHTA 0.6.2  •  SOLO PREVIEW`.

## Masukan owner (0.6.1)
- Masuk rumah atau pondok, atapnya **masih hilang**. Owner ingin tetap di dalam dengan kamera dekat.
- Karakter masih menembus tembok. Seharusnya mereka berbelok.
- Karakter harus lebih realistis.
- Misi membingungkan: isinya hanya preman dan sapa warga, sering tidak tahu harus apa. Misi harus berjalan lancar dan jelas langkah berikutnya.

## Kenapa atap masih hilang di 0.6.1
Lantai pendopo agak tinggi, sehingga atapnya hanya sekitar 1,7 m di atas kepala. Deteksi "di dalam bangunan" di 0.6.1 baru aktif kalau atap minimal 1,9 m di atas hero, jadi tidak pernah aktif di pendopo dan atap tetap disembunyikan.

## Yang baru

### 1. Atap tidak hilang, kamera masuk ke dalam
- Deteksi sekarang aktif mulai 1,2 m.
- Kamera di dalam dibuat lebih rendah dan dekat: 2,8 m, pitch 6°, setinggi dada.
- **Atap di atas hero tidak pernah disembunyikan** selama kamera berada di bawahnya.
- Keluar dari bangunan, kamera kembali seperti semula.

### 2. Tidak ada yang menembus tembok
- **Warga pejalan kaki:** saat scene dibuat, setiap jalur jalan dicek terhadap tembok, tiang, dan bangunan padat. Jalur yang menabrak dipotong sampai bagian yang bebas saja, atau warganya berdiri diam. Test scene memastikan tidak ada jalur yang menembus benda padat.
- **Warga yang menyingkir dari hero:** tidak lagi bergeser masuk ke tembok; kalau terhalang, ia tetap di jalurnya.
- **Kerumunan dan polisi:** sudah menghindari tembok sejak 0.6.1.

### 3. Misi terarah: 12 langkah, selalu ada petunjuk
Setiap misi menampilkan **instruksi langkah sekarang** di baris atas dan panel kiri. Panduannya:
- **panah kecil** menunjuk tempatnya;
- tombol **HP • KERJA berkedip kuning** kalau langkahnya dilakukan lewat HP.

| No | Misi | Petunjuk |
|---|---|---|
| 1 | KENALAN PAK RT | Panah ke pos ronda, tombol SAPA. Pak RT menjelaskan syarat jadi Ketua RT |
| 2 | CARI KERJA | HP berkedip, pilih OJOL, lalu panah jemput/antar (+Rp 20rb) |
| 3 | JAJAN SIOMAY | Panah ke gerobak siomay |
| 4 | GOWES KE PLAZA | Panah ke sewa sepeda, lalu panah ke plaza |
| 5 | KULI HARIAN | HP berkedip, pilih KULI, lalu panah tumpukan/proyek |
| 6 | KENALAN WARGA | Panah ke ibu-ibu dan driver ojol |
| 7 | PAHLAWAN GANG | Preman **baru muncul di misi ini**, panah ke mulut gang. Kalau polisi yang membawa preman setelah kamu berkelahi, misi tetap selesai |
| 8 | TAWARAN GELAP | WA nomor asing: TERIMA atau TOLAK |
| 9 | ISTIRAHAT | HP berkedip, pilih REBAHAN |
| 10 | TABUNGAN Rp 500rb | HP berkedip, kerja bebas (bersih atau kotor) |
| 11 | RESTU 50 | Sapa, antar tepat waktu, usir preman |
| 12 | DAFTAR CALON RT | Kumpulkan Rp 1 juta, lalu panah ke pos ronda |

- **Preman tidak lagi muncul terus-menerus.** Tidak ada preman sebelum misi 7. Setelah itu preman muncul sesekali (sekitar 2,5 menit sekali). RESTU per preman turun dari +8 menjadi +4, supaya RESTU 50 tidak terlalu cepat tercapai.
- **Simpanan lama:** simpanan 0.6.0 dan 0.6.1 tetap terbaca, tetapi misi mulai dari misi 1. Misi yang syaratnya sudah terpenuhi selesai sendiri satu per satu.

### 4. Warga lebih nyata dan beragam
- Tinggi dan postur badan sedikit berbeda tiap orang.
- **Jilbab 5 warna** (dulu semua ungu).
- Sebagian warga **beruban**.
- **Anak-anak** ikut berjalan di boulevard dan taman.
- Kerumunan perkelahian dicampur: bapak-bapak, ibu-ibu, pemuda.
- Karakter buatanmu mendapat **poni, kerah baju, dan ikat pinggang**.
- Jujur soal batasnya: bentuknya masih bentuk dasar dari kode. Lompatan ke "realistis" butuh model 3D manusia (FBX), lihat bagian berikutnya.

## Sengaja tidak diubah
- Combat, PvP, MODE PRESIDEN (selain kamera), fitur 0.0.8.2, folder Art.
- Rumah kampung, ruko, dan gedung tetap padat. Yang bisa dimasuki adalah bangunan terbuka (pendopo, warung, pos ronda) dan POLSEK.

## Checklist uji perangkat 0.6.2
1. Label 0.6.2. KARIER → MULAI DARI NOL (supaya misi dari awal). Baris atas: **MISI 1 • Sapa PAK RT**, dan panah menunjuk pos ronda.
2. Ikuti misi 1 sampai 6 tanpa bertanya-tanya. Tiap langkah harus jelas dari baris atas dan panah, dan **HP berkedip** di misi 2 dan 5.
3. Pastikan **tidak ada preman** sebelum misi 7. Di misi 7 preman muncul di mulut gang.
4. Masuk **pendopo**, **warung**, dan **POLSEK**: atap harus tetap ada dan kamera masuk ke dalam.
5. Perhatikan warga yang berjalan di sekitar bangunan dan tiang: **tidak ada yang menembus**.
6. Lihat ragam warga: jilbab berwarna-warni, ada yang beruban, ada anak-anak.

## Risiko
- Kalau sebuah jalur warga terpotong pendek, warga itu kini berdiri diam. Kota bisa terasa sedikit lebih sepi di titik tertentu.
- Kamera interior bisa ikut aktif di bawah pohon besar dari model 3D.
- Angka misi dan hadiah masih angka awal.

## Rekomendasi agar karakter benar-benar realistis (butuh keputusan owner)
Tubuh bentuk dasar sudah mendekati batasnya. Langkah berikutnya yang paling berdampak adalah **slot model warga 3D**, memakai jalur yang sama dengan hero (`Docs/PANDUAN_MEGA_3D.md`). Owner menyiapkan 2–4 model manusia berformat FBX dengan rig Humanoid (laki-laki, perempuan, berjilbab, anak). Generator lalu memakainya untuk avatar dan warga, dengan animasi jalan, lari, dan pukul. Sumber gratis yang bisa dicek, dengan lisensi yang perlu diverifikasi: Mixamo (karakter + animasi, akun Adobe), Quaternius, dan Kenney (CC0, gaya low-poly).
