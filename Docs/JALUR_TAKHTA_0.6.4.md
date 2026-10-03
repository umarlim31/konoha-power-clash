# 0.6.4 — KARIER "Pemilihan RT": jadi Ketua RT 03, dan atap/pohon tidak menghilang lagi

Branch kerja: `feat/0.6.4-pemilihan-rt`, dibuat dari `feat/0.6.3-hidup-siang-malam`. Branch ini sudah berisi 0.6.0–0.6.3. PR ke `feat/jalur-takhta-first-playable`, **bukan** ke `main`.

Status: compile Unity, EditMode test, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek: sintaks C#, `scripts/source-check.py`, dan review independen.

## Build manual (Unity Build Automation)
1. Branch **`feat/0.6.4-pemilihan-rt`**, Unity **6000.0.60f1**, pre-export **`Konoha.Editor.SpikeProject.prepare`**.
2. APK versi **0.6.4**, kode **50**, label `JALUR TAKHTA 0.6.4  •  SOLO PREVIEW`.

## Masukan owner (0.6.3)
- Atap rumah dan pondok **masih menghilang**, beberapa pohon juga. Owner ingin tidak ada yang menghilang.
- Lanjut ke fase berikutnya (0.6.4).

## Perbaikan: atap, pohon, dan bangunan tidak menghilang (KARIER)
**Kenapa masih terjadi:**
- Sistem "sembunyikan yang menghalangi kamera" (sejak 0.0.9.4) masih menyembunyikan atap, pohon, dan model 3D setiap kali benda itu berada di antara kamera dan hero.
- Deteksi "di dalam ruangan" menghitung ukuran atap saat game berjalan. Di tablet, deteksi ini ternyata tidak pernah menemukan atap model pondok (di editor angkanya benar).

**Sekarang:**
- **Di KARIER, atap, pohon, dan semua model 3D (rumah, ruko, warung, gapura, lampu) tidak pernah disembunyikan.**
- Yang masih boleh memudar hanya gerbang besar, papan nama, dan spanduk.
- Kalau rumah atau gerobak berada di antara kamera dan hero, **kamera maju ke depan bangunan** (seperti game orang-ketiga lain), bukan bangunannya yang hilang. Batang pohon dan tiang yang ramping tidak menarik kamera.
- **Ruangan beratap diukur oleh generator** dan disimpan di scene (pendopo kiri-kanan taman, POLSEK, warung, tenda rumah duka, dan lain-lain). Test scene memastikan pendopo dan POLSEK terhitung ruangan. Masuk ke dalam: kamera turun ke bawah atap.
- MODE PRESIDEN: perilaku memudar tetap seperti sebelumnya (kamera jauh), tetapi deteksi ruangannya ikut lebih andal.

## Yang baru: PEMILIHAN KETUA RT (akhir Level 1)
Sesuai `Docs/VISI_KONOHA_HIDUP_v3.md` §3 "Tujuan Level 1".

### Alurnya
1. Setelah DAFTAR CALON RT, Pak RT mengumumkan **pemilihan 2 hari lagi, jam 09.00–17.00, di POS RONDA**. Simpanan lama yang sudah terdaftar langsung mendapat jadwal.
2. **KAMPANYE**
   - Panah **KAMPANYE** menunjuk titik warga (bapak-bapak pos ronda, ibu-ibu, driver ojol).
   - Tombol **KAMPANYE** biayanya Rp 20rb (kopi + rokok), satu kali per titik per hari.
   - Setelah 3 kampanye hari itu selesai, panah kembali ke tugas harian.
3. **Survei grup WA** di panel kiri: perkiraan suara **KAMU / JURAGAN / HAJI** dalam persen. Survei ini hanya perkiraan, bukan hasil.
4. Rival:
   - **Juragan Kos**: bagi sembako tiap hari, suaranya naik, dan di hari H dia juga menyebar amplop subuh.
   - **Pak Haji**: suaranya tetap, karena sudah lama dihormati.
   - Selama kampanye, preman yang muncul adalah **"tim sukses Juragan Kos"**. Mengusir mereka menaikkan suaramu.
5. **Pagi hari H, jam 04.00–09.00:** HP > **SERANGAN FAJAR** (Rp 300rb).
   - Suara +30 dan CATATAN HITAM +20.
   - Ada peluang videonya **viral** dan suaramu malah turun (−40). Peluangnya makin besar kalau catatan hitam tinggi.
6. **Jam 09.00–17.00:** panah **COBLOS** ke POS RONDA, lalu tekan **COBLOS**.
   - Panel **HITUNG SUARA** menampilkan tiga batang suara yang naik satu per satu (60 KK). Bisa dilewati dengan **LANJUT**.
   - Tidak datang sampai jam 17.00: dihitung tanpa suaramu sendiri.
7. **Hasil di Grup WA RT 03:**
   - **Menang:** "Selamat kepada Bpk/Ibu [NAMA], KETUA RT 03 yang baru... iuran sampah tetap Rp 20.000". **LEVEL 1 TAMAT.** Nama di layar menjadi "[NAMA] • KETUA RT 03". Kerja dan tugas harian tetap jalan.
   - **Kalah:** "kotak suara tertukar dengan kotak nasi". **Pemilihan ulang 3 hari lagi**, setengah dari hitungan kampanye tetap terbawa.
   - Kalau melakukan serangan fajar, Panwas RT atau Bu Tejo ikut berkomentar.

### Perhitungan suara (angka awal, `CampaignTuning.Karier`)
- **Kamu:**
  - Ditambah: RESTU, kampanye × 5, ronda × 2 (maks. 10 kali), preman diusir × 3 (maks. 8), serangan fajar +30 (−40 kalau viral).
  - Dikurangi: CATATAN HITAM ÷ 3, tabrak × 4 (maks. 5 kali).
- **Juragan Kos:** 80 + 8 per hari kampanye (maks. 5 hari) + 20 di hari H.
- **Pak Haji:** 75.
- Suara 60 KK dibagi sesuai perbandingan poin. Kalau seri, yang lebih tua menang.
- Gambaran: RESTU 100 dengan kampanye penuh 2–3 hari biasanya menang. Tanpa kampanye biasanya kalah.

## Simpanan
Format **K5** menyimpan jadwal pemilihan, kampanye, serangan fajar, hasil, dan panel hitung yang belum tampil. Simpanan K1–K4 tetap terbaca.

## Sengaja tidak diubah
- Combat dan PvP.
- MODE PRESIDEN: hanya deteksi ruangannya yang lebih andal.
- Fitur 0.0.8.2 dan folder Art.

## Checklist uji perangkat 0.6.4
1. Label 0.6.4. KARIER → LANJUTKAN HIDUP. Pak RT mengumumkan **pemilihan HARI n**, dan panel kiri menjadi **PEMILIHAN RT** dengan survei.
2. Putar kamera mengelilingi rumah, pohon, dan pondok: **tidak ada yang menghilang**. Kamera maju ke depan rumah kalau terhalang.
3. Masuk **pendopo taman** (kiri dan kanan) dan **POLSEK**: atap tetap ada, kamera turun ke dalam.
4. Ikuti panah **KAMPANYE** ke 3 titik warga dan tekan KAMPANYE. Survei harus berubah.
5. TIDUR sampai hari H. Pagi hari (sebelum 09.00): HP > **SERANGAN FAJAR** (opsional).
6. Jam 09.00 ke POS RONDA, tekan **COBLOS** → panel hitung suara → Grup WA hasil. Menang: nama menjadi **KETUA RT 03**. Kalah: muncul tanggal pemilihan ulang.
7. MODE PRESIDEN sekali: rute normal.

## Risiko
- **Kamera bisa terasa "lompat maju"** karena sekarang berhenti di depan rumah dan gerobak, bukan menembus. Kalau terlalu sering, gerobak kecil bisa dikecualikan.
- **Pohon tidak lagi memudar,** jadi kadang hero tertutup daun sesaat (ini konsekuensi permintaan "jangan menghilang"). Paling jarang terjadi di KAMERA DEKAT.
- **Keseimbangan suara (menang/kalah) masih angka awal.** Mohon kabari kalau terlalu mudah atau terlalu sulit.
- **Level 2 (Kepala Desa) belum ada.** Setelah menang, permainan berlanjut dengan kerja dan tugas harian.
