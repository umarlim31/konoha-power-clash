# 0.6.5 — KARIER "Kota Luas": jalan sampai jalan raya, kamera tidak mepet wajah

Branch kerja: `feat/0.6.5-kota-luas`, dibuat dari `feat/0.6.4-pemilihan-rt`. Branch ini sudah berisi 0.6.0–0.6.5. PR ke `feat/jalur-takhta-first-playable`, **bukan** ke `main`.

Status: compile Unity, EditMode test, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek: sintaks C#, `scripts/source-check.py`, dan review independen.

## Build manual (Unity Build Automation)
1. Branch **`feat/0.6.5-kota-luas`**, Unity **6000.0.60f1**, pre-export **`Konoha.Editor.SpikeProject.prepare`**.
2. APK versi **0.6.5**, kode **51**, label `JALUR TAKHTA 0.6.5  •  SOLO PREVIEW`.

## Masukan owner (video 0.6.4)
- Perbaiki bagian yang menurutku perlu diperbaiki.
- Batas gerak diperluas: boleh berjalan sampai jalan raya.

## Yang terlihat di video dan sudah diperbaiki
- **Atap dan pohon sudah tidak hilang** (perbaikan 0.6.4 berhasil).
- **Kamera sering mepet ke wajah hero.** Penyebabnya: sejak 0.6.4 kamera berhenti di depan rumah/gerobak di belakang hero. Kalau hero membelakangi bangunan, kamera terdorong sampai 0,8 m dari wajah.
  - **Sekarang:** kalau ada benda padat kurang dari 2,6 m di belakang hero, **kamera naik** (melihat dari atas melewati bangunan). Begitu jalur kosong lagi, kamera pelan-pelan kembali ke sudut pilihanmu. Di dalam ruangan, kamera tetap di bawah atap seperti 0.6.4.
- **Malam terlalu gelap di tablet**: cahaya bulan dan langit malam dibuat sedikit lebih terang.

## Yang baru: kota lebih luas (KARIER)
- Batas gerak tak terlihat KARIER sekarang berupa **persegi panjang bersudut bulat sampai jalan lingkar (kiri, kanan, utara) dan jalan raya (selatan)**. Kamu bisa menyeberang ke trotoar seberang.
- **Lantai pijakan diperluas** sampai jalan-jalan itu. Ada test yang memastikan ada lantai di bawah jalan.
- **Benda padat ikut diperluas**: rumah kampung, ruko, tiang PJU, dan pohon di tepi jalan tidak bisa ditembus.
- **Lalu lintas:** mobil, angkot, dan motor **berhenti dan membunyikan klakson** kalau kamu berdiri di depannya, tidak menembus badanmu. Kendaraan di belakangnya ikut berhenti dan mengantre.
- Kerumunan, polisi, dan pengejaran juga memakai batas baru.
- **MODE PRESIDEN tetap memakai batas oval lama** (fitur 0.0.8.2 tidak berubah).

## Sengaja tidak diubah
Combat, PvP, MODE PRESIDEN, aturan pemilihan RT, folder Art.

## Checklist uji perangkat 0.6.5
1. Label 0.6.5. KARIER → LANJUTKAN HIDUP.
2. Jalan ke barat/timur sampai **jalan lingkar**, dan ke selatan sampai **jalan raya** depan ruko. Harus bisa menyeberang ke trotoar dan tidak jatuh.
3. Berdiri di tengah jalan: mobil berhenti dan klakson.
4. Berdiri membelakangi rumah/gerobak/tembok: **kamera naik**, tidak menempel ke wajah. Menjauh: kamera turun lagi.
5. Masuk pendopo: kamera tetap di bawah atap.
6. Malam hari: jalan dan warga lebih terbaca.
7. MODE PRESIDEN sekali: batas oval dan rute seperti biasa.

## Risiko
- Area baru di pinggir kota belum pernah dimainkan. Mungkin ada lubang kecil atau tempat tersangkut. Mohon screenshot.
- Kamera yang naik bisa terasa "loncat" di gang sempit. Kecepatan naik dan turunnya bisa disetel.
- Antrean kendaraan dibuat sederhana: di tikungan, mobil bisa tampak saling menempel sebentar.
