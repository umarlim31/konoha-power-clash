# Jalur Takhta 0.2.2 — Kota Hidup

Branch kerja: `feat/kota-hidup-0.2.2`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.2.1 (Material nyata) di-merge berdasarkan hasil uji di tablet. PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# (parser tree-sitter C#), `scripts/source-check.py`, dan review kode kedua (tidak ditemukan error compile).

Acuan: empat gambar konsep dari owner (tampak depan, kiri, kanan, atas). Susunan arena kita sudah sama dengan gambar itu, jadi rute yang sudah teruji **tidak diubah**. Yang ditambah hanya "isi kota" di sekelilingnya.

## Build manual Android/tablet (Unity Build Automation)

1. Pilih branch **`feat/kota-hidup-0.2.2`**, Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`**.
3. APK: versi **0.2.2**, kode Android **31**. Label bawah: `JALUR TAKHTA 0.2.2  •  SOLO PREVIEW`.
4. Kalau muncul "paket tidak valid" saat install: hapus dulu aplikasi lama, lalu install lagi (sama seperti sebelumnya).

## Yang baru

### 1. Kabel listrik tidak lagi melintang di depan kamera
Dari screenshot 0.2.1: tiga kabel hitam memotong layar tepat di awal permainan.
- Jalur tiang di sepanjang jalan raya dipindah dari trotoar dekat gerbang (z -54,4) ke **seberang jalan** (z -64,95), di belakang kamera awal.
- Kabel yang menyeberang di mulut boulevard dihapus.
- Tiang di kiri-kanan boulevard tetap ada.

### 2. Jalan lingkar dan lalu lintas
- **Jalan lingkar** mengelilingi ibu kota: jalan raya di selatan, jalan samping di timur dan barat (x ±41), dan jalan utara di belakang Istana (z 70). Semuanya berada di luar batas gerak hero.
- **11 kendaraan** berjalan terus-menerus: sedan, **angkot** (biru, hijau, kuning), pikap bermuatan terpal, dan **3 motor dengan pengendara berhelm**. Mereka berjalan di **lajur kiri** seperti di Indonesia, dua arah, dan berbelok halus di tikungan.
- **Klakson** "tin-tin" sesekali dari kendaraan terdekat, dengan suara motor lebih cempreng. Ikut tombol SUARA.

### 3. Warga
- **18 warga berjalan** di trotoar jalan raya, di jalur rumput kiri-kanan boulevard, di taman Plaza Aspirasi, dan di depan rumah kampung.
  - Pakaiannya beragam: kemeja, batik, kaos, gamis dan jilbab, peci, topi. Warna kulitnya sawo matang dan kuning langsat.
  - Saat hero mendekat, warga **menepi**. Saat ada pertarungan dalam ±6 m, warga **balik arah dan bergegas menjauh**.
- **12 warga diam**: penjual kopi dan 2 pembeli di warung, tukang bakso dan 2 pembeli, penonton di dekat paviliun, dan 2 orang di dekat baliho.
  - Mereka bergerak kecil (tangan) dan **menoleh ke arah hero** saat hero lewat.
- Warga tidak bisa diserang, tidak menghalangi hero, dan tidak mengubah aturan permainan.

### 4. Air mancur
- Di dua kolam Plaza Aspirasi masing-masing ada 4 semburan air dengan percikan yang melengkung lalu jatuh kembali ke kolam.
- Satu semburan keluar dari hiasan air mancur perunggu yang sudah ada.

### 5. Bendera dan spanduk
- **Bendera merah-putih** di awal boulevard (4) dan di pintu masuk Plaza (2).
- **Spanduk merah-putih dengan lambang fiksi Konoha** (cincin dan bintang emas) di sepanjang boulevard (6).
- **Keputusan IP:** lambang negara resmi (Garuda Pancasila dengan perisai dan pita) **tidak dipakai**, karena diatur khusus di UU No. 24 Tahun 2009 dan berisiko untuk rilis di Play Store. Monumen tetap **Garuda emas mitologis** yang sudah ada. Ini sesuai aturan IP di CLAUDE.md, dan owner sudah diberi tahu alasannya.

### 6. Kota di sekitar
- **16 ruko dua lantai** di seberang jalan raya, dengan rolling door, kanopi, genteng, dan papan nama fiksi: TOKO KELONTONG, FOTOKOPI 24 JAM, APOTEK WARGA, BIRO JASA*, PANGKAS RAMBUT, dan lain-lain.
- **22 rumah kampung** beratap genteng pelana, dengan teras dan pagar, di sepanjang jalan samping.
- **Teluk** di utara, dengan **jembatan panjang berpilar**, **2 mercusuar** merah-putih, dan **4 perahu layar**. Gunung-gunung yang sudah ada menjadi pulau di teluk.
- Sawah dan 2 pohon trembesi digeser sedikit supaya tidak menabrak jalan dan rumah baru.

## Sengaja tidak diubah

- Rute, titik muncul musuh, checkpoint, aturan, dan angka tempur. Semua benda baru tidak punya collider.
- Kamera, joystick, LOMPAT, dan semua fitur 0.0.8.2.
- Mode PvP 4v4: semua perubahan ada di generator campaign.
- Tampilan hero dan musuh. Karakter realistis dikerjakan berikutnya, sesuai urutan owner: arena dulu, baru karakter.

## Checklist uji perangkat 0.2.2

1. Label `JALUR TAKHTA 0.2.2  •  SOLO PREVIEW`.
2. **Awal permainan (setelah MULAI):** tidak ada kabel hitam yang melintang di layar.
3. Putar kamera ke belakang (geser layar kanan):
   - mobil, angkot, dan motor lewat di jalan raya, berjalan di lajur kiri;
   - ruko di seberang jalan terlihat, dan tiang listrik tidak menembus kanopi ruko.
4. Tunggu 10–20 detik: ada bunyi klakson. Tekan SUARA → MATI: klakson ikut diam.
5. Jalan ke warung kopi dan gerobak bakso: penjual dan pembeli ada, dan mereka menoleh ke hero.
6. Berjalan ke arah warga yang sedang jalan: mereka menepi.
7. Saat melawan 3 Kroni di Gerbang Rakyat: warga di dekatnya menjauh.
8. Plaza: air mancur memancar di dua kolam.
9. Tekan **LIHAT ARENA**:
   - jalan lingkar dengan mobil yang berbelok di tikungan;
   - rumah kampung di kiri-kanan;
   - teluk dengan jembatan dan mercusuar di belakang Istana.
10. Mainkan sampai **DUDUK di Kursi** (MENANG): permainan tetap sama seperti 0.2.1.
11. **FPS:** apakah terasa lebih berat dari 0.2.1? Perhatikan terutama di awal permainan (dekat warung dan jalan raya) dan saat LIHAT ARENA. Kalau berat, sebutkan di bagian mana.

## Risiko

- **Performa tablet adalah risiko utama.**
  - Warga, kendaraan, dan percikan air adalah ±660 bagian bergerak yang tidak bisa digabung seperti dekor diam.
  - Yang sudah dilakukan: bagian yang jauh dari kamera (>60 m) disembunyikan, dan hanya badan yang memberi bayangan.
  - Kalau FPS turun, jumlah warga dan kendaraan tinggal dikurangi di satu tempat (`CampaignCapitalArt.KotaHidup.cs`) atau jarak sembunyinya diperkecil (`cullDistance`).
- Warga dan kendaraan masih berbentuk sederhana (kotak/silinder). Model manusia dan mobil realistis dikerjakan di tahap aset.
- Teluk dan jembatan jauh di utara, sehingga dari kamera main biasa sebagian tertutup kabut. Paling jelas terlihat dari LIHAT ARENA.

## Berikutnya

- **0.2.3:** karakter dan serangan terasa nyata (tubuh manusia untuk hero dan musuh, efek pukulan: percikan, kilat, getar layar, jeda sesaat; SERUAN IBU = Kerbau Rakyat).
- **0.3.0:** jalur aset realistis (model manusia, mobil, kerbau yang diunggah owner) dengan panduan seperti `PANDUAN_TEKSTUR_NYATA.md`.
