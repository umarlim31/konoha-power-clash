# Jalur Takhta 0.5.0 — "Jalan Nyaleg": alur jelas dan warga yang hidup

Branch kerja: `feat/jalan-nyaleg-0.5.0`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.4.0 di-merge (owner sudah memainkan dan membaca tiga koran). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Design authority: `Docs/GAME_LOGIC_JALUR_TAKHTA_v2.md`, bagian **Amandemen 0.5.0**.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`. Ada 2 test baru untuk Blusukan, dan test yang sudah ada disesuaikan dengan nama baru.

**Catatan jatah build:** owner melaporkan kuota gratis Unity Build Automation sudah terpakai 75%. Versi ini sengaja dibuat besar supaya cukup **satu build** untuk menguji semuanya.

## Build manual Android/tablet (Unity Build Automation)
1. Branch **`feat/jalan-nyaleg-0.5.0`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.5.0**, kode Android **45**. Label bawah: `JALUR TAKHTA 0.5.0  •  SOLO PREVIEW`.

## Masalah dari owner (0.4.0)
- Pemain masuk dan tidak tahu harus ke mana. Arahan yang ada pun terasa kurang seru dan kaku.
- "Majelis Daun" dan "Biro Prosedur" membingungkan dan tidak terasa Indonesia. Owner: boleh dihilangkan, asal diganti alur yang lebih menarik.
- Suasana harus menggambarkan kehidupan warga Indonesia di kota dan kampung: warga berkumpul dan kepo, ibu-ibu ngerumpi, motor bonceng tiga.

## Yang baru
### 1. Alur "Jalan Nyaleg": tahap yang dikenal semua orang
| No | Tahap | Isi | Pengganti |
|---|---|---|---|
| 1 | **Gang** | Usir **Preman Bayaran** yang memalak warga, lalu dapat RESTU + 15 MODAL | Kroni Gerbang Rakyat |
| 2 | **Blusukan** (baru) | Sapa 3 kerumunan warga yang ditandai **lingkaran emas** (berdiri 2 detik di dalamnya): **bapak-bapak pos ronda**, **ibu-ibu di tukang sayur**, **pangkalan ojol**. Setiap kelompok punya keluhan satir, dan SUARA 3/3 membuka Plaza. | — |
| 3 | **Rekomendasi Koalisi** | Markas koalisi di sayap kiri. **LAWAN**: Elite Partai saling melindungi (−40% damage) sampai Elite tumbang, lalu Ketum mengetok palu. Atau **BAYAR MAHAR** (45 Modal). | Majelis Daun |
| 4 | **Berkas di Kantor Kelurahan** | Sayap kanan, loket **FOTOKOPI KTP**, **CAP RT/RW**, **LEGALISIR**. Ada penyerobot antrean, **Petugas Istirahat** (cooldown +30%: "JAM ISTIRAHAT"), dan **Pak Lurah** yang memindahkanmu ke loket lain sambil bilang **"BALIK BESOK!"**. Atau **BAYAR CALO** (35 Modal). | Biro Prosedur |
| 5 | **Pelantikan** | Tembus **Garda Istana** dan Panglima | Garda Takhta |
| 6 | **DUDUK di Kursi** | Lalu Koran Konoha (berita disesuaikan: mahar, calo, Pak Lurah) | — |

Semua nama di layar ikut diganti: papan nama gedung, papan loket, nameplate musuh, pesan, arah, panel pilihan, dan koran.

### 2. Arahan yang jelas
- **Daftar langkah JALAN NYALEG** selalu tampil di kiri: ■ selesai (emas), ► sedang dikerjakan (putih), □ belum (abu-abu). Daftar ini disembunyikan sementara saat panel pilihan muncul.
- **Pilar cahaya emas** setinggi ±28 m berdiri di tujuan berikutnya (preman, kelompok warga, markas, loket, Pak Lurah, Panglima, Kursi) dan terlihat dari jauh.
- Lingkaran emas Blusukan diberi tulisan "SAPA POS RONDA" dan berubah menjadi "MENYAPA... 60%" saat hero berdiri di dalamnya.

### 3. Kehidupan warga Indonesia
- **Warga kepo:** saat ada perkelahian di dekat mereka, warga **berhenti, menoleh, dan merekam pakai HP** (tangan terangkat). Mereka baru menjauh kalau perkelahian sudah terlalu dekat.
- **Rumah duka** di kampung sisi barat: bendera kuning, tenda biru, kursi plastik merah-hijau, papan karangan bunga "TURUT BERDUKA CITA", dan pelayat berbaju gelap.
- **Ibu-ibu ngerumpi** berjilbab mengelilingi **gerobak tukang sayur**.
- **Bapak-bapak di pos ronda** (gubuk beratap genteng, kentongan, papan "POS RONDA RT 03").
- **Pangkalan ojol**: 3 motor dan driver berjaket hijau.
- **Motor bonceng tiga** (bapak, anak di depan, ibu berjilbab duduk menyamping) melintas di jalan lingkar.
- **Motor jatuh** di pinggir jalan raya, dikerumuni warga yang sibuk merekam.

## Sengaja tidak diubah
- Combat, hero, angka musuh, mekanik solidaritas dan loket, Lockdown Garda, kamera, model 3D, LAWAN/RANGKUL, Modal/Jatah/Restu, dan Koran (selain isi beritanya).
- Nama kode internal (MajelisDaun/BiroProsedur) tetap, supaya test dan arsitektur stabil.
- PvP.

## Checklist uji perangkat 0.5.0 (satu build untuk semuanya)
1. Label 0.5.0. Setelah MULAI, **daftar JALAN NYALEG** tampil di kiri dan **pilar cahaya** berdiri di preman.
2. Kalahkan 3 Preman Bayaran. Pesan "PREMAN KABUR!" muncul, lalu 3 **lingkaran emas** menyala. Pilar berpindah ke kelompok warga terdekat.
3. Berdiri di setiap lingkaran sampai 100%. Setiap kelompok memunculkan keluhan satir, SUARA bertambah, dan daftar langkah mencentang "Blusukan".
4. Ke Plaza: papan bertuliskan **MARKAS KOALISI** (kiri) dan **KANTOR KELURAHAN** (kanan). Panel memberi pilihan **BAYAR MAHAR** / **BAYAR CALO**.
5. Lawan salah satunya. Nameplate musuh harus **Elite Partai / Ketum Koalisi** atau **Staf Kelurahan / Petugas Istirahat / Pak Lurah**, dan loket bertuliskan **FOTOKOPI KTP / CAP RT/RW / LEGALISIR**.
6. Selama bertarung di dekat warga: warga **berhenti dan merekam dengan HP**.
7. Pakai KAMERA DEKAT dan cari: **rumah duka** (kampung barat, dekat jalan samping), **ibu-ibu & tukang sayur**, **pos ronda**, **pangkalan ojol**, **motor bonceng tiga** di jalan lingkar, dan **motor jatuh** di tepi jalan raya selatan (kanan dari spawn).
8. Main sampai menang dan baca korannya. Pastikan FPS tetap lancar.

## Risiko
- Versi besar dalam satu langkah: risiko error compile lebih tinggi daripada versi kecil. Semua file sudah lolos pemeriksaan sintaks, tetapi pemeriksaan tipe baru terjadi di Unity. Kalau build gagal, kirim potongan log error-nya; biasanya bisa diperbaiki dalam satu commit kecil.
- Tambahan ±22 warga dan beberapa set properti. Warga di luar 60 m disembunyikan, jadi dampak FPS seharusnya kecil.
- Pejalan kaki di jalan samping barat melintas di tengah tenda rumah duka (tidak ada tabrakan).
- Pilar cahaya dibuat padat (tidak transparan) demi performa. Kalau terlalu mencolok, bisa diperkecil.
