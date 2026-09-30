# Jalur Takhta 0.2.5 — Nusantara Megah

Branch kerja: `feat/nusantara-megah-0.2.5`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.2.4 (keseimbangan hero) di-merge berdasarkan hasil uji di tablet ("hasilnya udah bagus"). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C#, `scripts/source-check.py`, dan review kode kedua.

Acuan: tiga gambar target owner, yaitu ibu kota megah dengan boulevard bermotif, banyak spanduk merah-putih, menara beratap Nusantara, air mancur dan air terjun, pohon berbunga, serta teluk dengan jembatan, pulau, dan gunung di belakang. **Rute dan tabrakan yang sudah teruji tidak diubah.**

## Build manual Android/tablet (Unity Build Automation)

1. Branch **`feat/nusantara-megah-0.2.5`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.2.5**, kode Android **34**. Label bawah: `JALUR TAKHTA 0.2.5  •  SOLO PREVIEW`.

## Yang baru

1. **Ubin motif biru-emas**
   - Ada di boulevard Gerbang Rakyat dan di pelataran depan Gerbang Dalam.
   - Coraknya ubin navy dengan bintang delapan emas, garis emas, dan lingkaran kawung krem di sudut. Satu ubin berukuran 3×3 m.
2. **Spanduk merah-putih dengan lambang fiksi Konoha** (cincin dan bintang emas, bukan lambang negara):
   - 12 spanduk bergantung di tembok Gerbang Dalam, 6 di tiap sayap;
   - 4 spanduk panjang di depan tiang-tiang Istana.
3. **Menara beratap Nusantara**
   - Ada 6 menara: 4 di sisi teras Istana dan 2 di ujung tembok Gerbang Dalam.
   - Bentuknya batang gading dengan pita perunggu, jendela, dan paviliun beratap genteng bertingkat, dengan bendera merah-putih kecil di puncak.
   - Menara padat (tidak bisa ditembus), tapi kamera tetap bisa melihat menembusnya seperti tembok, jadi tidak ada zoom tiba-tiba.
   - Menara menempel rapat ke teras, jadi tidak ada celah sempit tempat hero bisa tersangkut.
4. **Air terjun Istana:** dua tirai air jatuh dari tepi teras Istana ke dua kolam, dengan air yang benar-benar mengalir dan buih yang berdenyut.
5. **Pohon flamboyan merah** (6) dan **semak bugenvil ungu** (6) di sepanjang boulevard.
6. **Langit dan cakrawala**
   - **Langit tropis:** biru lebih cerah, udara lebih jernih (kabut mulai lebih jauh), dan 16 gugus awan.
   - **Latar belakang:** kota pesisir beratap genteng di tepi teluk dan 5 pulau berpalem.
   - **Gunung:** dimundurkan supaya berdiri di belakang teluk. Sebelumnya gunung terlalu dekat dan menutupi jembatan dan teluk dari 0.2.2.

## Sengaja tidak diubah

- Rute, titik musuh, checkpoint, tabrakan jalan, aturan tempur, dan keseimbangan hero 0.2.4.
- Monumen tetap Garuda emas mitologis fiksi, dengan skala yang sama (garis pandang dari spawn ke Kursi tetap terjaga).
- PvP.

## Checklist uji perangkat 0.2.5

1. Label `JALUR TAKHTA 0.2.5  •  SOLO PREVIEW`.
2. Awal permainan: boulevard berubin **biru-emas bermotif**, dengan pohon flamboyan merah dan semak bugenvil di kiri-kanan. Langit lebih biru.
3. Masuk Plaza dan lihat ke utara: tembok Gerbang Dalam penuh **spanduk merah-putih berlambang bintang emas**, dan ada menara di kedua ujungnya.
4. Lewati Gerbang Dalam: **air terjun** dari teras Istana (kiri dan kanan tangga) mengalir ke kolam, dan menara mengapit Istana. Di depan tiang Istana ada spanduk panjang.
5. Tekan **LIHAT ARENA**: teluk, jembatan panjang, pulau berpalem, kota pesisir, dan gunung di belakang semuanya terlihat.
6. Berjalan di sisi teras Istana dekat menara: hero tidak tersangkut. Kamera tidak zoom sendiri di dekat menara.
7. Main sampai MENANG seperti biasa.
8. **FPS:** apakah lebih berat dari 0.2.4? Perhatikan terutama saat LIHAT ARENA dan di sekitar Istana.

## Risiko

- **Jarak pandang lebih jauh** (kabut 90–330 m, batas kamera 340 m), jadi lebih banyak benda digambar. Semua dekor baru diam dan digabung oleh Unity (static batching), dan bayangannya dimatikan untuk benda jauh. Kalau FPS turun, kabut bisa dikembalikan ke nilai 0.2.4 lewat satu baris.
- **Ubin motif** adalah tekstur buatan kode. Kalau terlalu ramai atau terlalu gelap di layar, warnanya bisa disetel.
- **Gunung yang dimundurkan** mengubah siluet cakrawala dari spawn. Mohon kirim screenshot supaya bisa dinilai.
