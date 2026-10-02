# 0.6.0 — KARIER Level 1 "Warga Biasa": hidup dari nol di Konoha

Branch kerja: `feat/0.6.0-warga-biasa`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.5.0 di-merge. PR kembali ke `feat/jalur-takhta-first-playable`, **bukan** ke `main`.

Arah produk: `Docs/VISI_KONOHA_HIDUP_v3.md`, disetujui owner 2026-10-02 (karier 5 level, avatar = diri sendiri, build berikutnya = fondasi Level 1).

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini:
- pemeriksaan sintaks semua file C#;
- `scripts/source-check.py`;
- review independen terhadap seluruh perubahan (referensi API, aturan C#, angka di test). Hasilnya: tidak ada error compile yang ditemukan. Satu bug runtime dan beberapa hal kecil sudah diperbaiki.

Ada 13 test logika baru (`KarierLifeTests`), 1 test baru untuk Konsorsium, dan test scene ditambah untuk semua objek KARIER, termasuk cek bahwa setiap titik kerja bisa diinjak.

**Jatah build:** versi ini sengaja dibuat cukup untuk **satu build**.

## Build manual Android/tablet (Unity Build Automation)
1. Branch **`feat/0.6.0-warga-biasa`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.6.0**, kode Android **46**. Label bawah: `JALUR TAKHTA 0.6.0  •  SOLO PREVIEW` (format label sengaja tidak diubah, karena dicek oleh script).

## Masalah dari owner (0.5.0)
- Terlalu mudah: mahar dan calo bisa dibayar walaupun tidak punya apa-apa. Uang harus dikumpulkan dulu, dengan cara bersih, kerja keras, atau korupsi.
- Pilar kuning merusak visual. Lebih baik suara atau petunjuk arah kecil.
- Karakter harus lebih realistis.
- Perkelahian harus memancing reaksi warga: ada yang teriak memanggil warga lain, ada yang melerai dan menasihati, lalu polisi datang naik motor atau mobil.
- Level 1 = warga biasa. Naik bertahap sampai presiden. Rasanya seperti hidup di Indonesia: rebahan, sosmed. Karakternya boleh diri sendiri.

## Yang baru

### 1. Menu: tiga mode
| Tombol | Isi |
|---|---|
| **KARIER (BARU)** | Level 1 "Warga Biasa": hidup dari nol sampai bisa mendaftar Ketua RT. Progres **tersimpan** di tablet. |
| **MODE PRESIDEN** | Jalur Takhta yang lama (Gang → Blusukan → Koalisi → Kelurahan → Garda → Kursi), sebagai mode cepat. |
| **REBUT KURSI** | PvP 4v4, tidak diubah. |

### 2. Buat warga sendiri
- Panel kiri bertajuk **BUAT WARGA KONOHA** berisi pilihan berikut. Setiap tombol diketuk untuk berganti pilihan, dan karakter di layar langsung berubah sambil menghadap kamera.
  - **NAMA**: keyboard HP, maksimal 14 huruf.
  - **JENIS**: laki-laki atau perempuan. Perempuan memakai rok panjang.
  - **KULIT**: 4 warna.
  - **KEPALA**: rambut pendek, rambut panjang, peci, jilbab, atau topi.
  - **BADAN**: kurus, sedang, atau berisi.
  - **BAJU**: 7 warna.
- Kalau sudah ada simpanan: **LANJUTKAN HIDUP** atau **MULAI DARI NOL**.
- Keempat hero tidak dipakai di KARIER. Mereka tetap ada di MODE PRESIDEN dan PvP, dan nanti menjadi rival di level atas.

### 3. Uang harus dicari: HP "KONOHA KERJA"
Tombol **HP • KERJA** di kiri atas membuka daftar kerja berikut.

| Kerja | Cara main | Hasil | Efek samping |
|---|---|---|---|
| **OJOL** | Hero **naik motor hijau** (lebih cepat). Jemput penumpang di titik bertanda, lalu antar sebelum waktu habis. Order baru masuk otomatis sampai kamu berhenti lewat HP. | Tarif ± Rp 25–60rb **dipotong aplikasi 20%**. Telat = setengah tarif + bintang 1. | ENERGI −6 per antar |
| **KULI BANGUNAN** | Angkut **5 sak semen** dari TUMPUKAN SEMEN ke PROYEK GORONG-GORONG ("Dana Aspirasi Rp 2 M, dikerjakan 2 orang"). Sak terlihat di pundak, jalan lebih lambat. | Rp 150rb **− uang rokok mandor Rp 25rb** = Rp 125rb | ENERGI −25 |
| **BUZZER HOAKS** | Duduk di WARKOP 3 detik, lalu hoaks tersebar ke grup WA. | **Rp 250rb, cepat** | **CATATAN HITAM +20**, RESTU −4. Mulai catatan 50 ada peluang **PASAL KARET**: polisi datang menjemput. |
| **REBAHAN** | Hero diam 7 detik sambil scroll sosmed (iklan pinjol, #KaburAjaDulu, live jualan...). | ENERGI +45 | Waktu terbuang |

- **Tombol aksi** (hijau, kanan bawah) muncul sesuai tempat:
  - **MAKAN** di warkop: Rp 15rb, ENERGI +35.
  - **SAPA** bapak-bapak pos ronda, ibu-ibu, atau driver ojol: RESTU +5, sekali per kelompok.
  - **DAFTAR CALON RT** di pos ronda, kalau syarat lengkap.
- ENERGI di bawah 15 = **LEMAS**: hero jalan lebih pelan.
- Keadaan awal: **Rp 50.000**, ENERGI 100, RESTU 30, CATATAN HITAM 0.

### 4. Target Level 1: daftar Ketua RT
- Panel kiri: **Syukuran Rp 1.000.000** (nasi kotak untuk satu RT) + **RESTU 50** → **DAFTAR di POS RONDA**.
- Setelah mendaftar, **GRUP WA "RT 03 KONOHA"** muncul berisi Pak RT, Bu Tejo, Juragan Kos (rival yang sudah siap 200 paket sembako), dan Pak Haji.
- Pemilihan Ketua RT sendiri ada di versi berikutnya. Untuk sementara, setelah mendaftar kamu masih bisa terus bekerja.

### 5. Perkelahian = urusan satu kampung
- Sekitar 40 detik setelah mulai, lalu kira-kira tiap 1,5 menit: **PREMAN** (kadang bersama **BOS PREMAN**) memalak warga di mulut gang. Melawan itu opsional. Kalau dicuekin terlalu lama, preman pergi.
- Saat bertarung, meter KERIBUTAN naik dan warga bereaksi bertahap:
  1. **Teriak**: satu warga mengangkat tangan dan berteriak **"WOI! ADA YANG BERANTEM!"**. Warga lain **berlarian datang**, berdiri melingkar, dan **merekam pakai HP**.
  2. **Melerai**: bapak berpeci berdiri di tengah dengan tangan terentang: **"SUDAH, SUDAH! MALU SAMA TETANGGA!"**. Preman **ditahan** selama 3 detik (tidak menyerang dan tidak bisa dipukul).
  3. Kalau keributan berlanjut, **POLISI** datang **naik motor patroli** dari jalan raya: sirene berbunyi, lampu merah-biru berkedip, petugas turun. Preman **DIAMANKAN**, lalu pemain memilih:
     - **KABUR**: CATATAN HITAM +20, RESTU −4.
     - **DAMAI DI TEMPAT**: bayar Rp 100rb + tambahan sesuai catatan hitam. Ini satir pungli: tidak ada kuitansi.
     - **IKUT KE POLSEK**: layar gelap, "semalam di polsek", biaya administrasi Rp 50rb, dan ibu-ibu sudah tahu semua.
- Kalau preman **kalah sebelum polisi datang**: RESTU +8 per preman + "terima kasih" Rp 20rb.
- Kalau hero **pingsan**: biaya puskesmas Rp 50rb dan preman kabur.

### 6. Pengganti pilar kuning (KARIER **dan** MODE PRESIDEN)
- **Panah kecil** di tanah, di samping kaki hero, menunjuk ke tujuan. Panah hilang saat sudah sampai.
- **Lingkaran putus-putus** yang rendah dan tulisan pendek di titik tujuan.
- **Bunyi "ting"** notifikasi HP setiap ada tujuan atau order baru, plus kotak pesan di atas tengah.

### 7. MODE PRESIDEN lebih menantang
- MODAL awal turun dari **60 menjadi 25**. Mahar dan calo harus dibayar dari MODAL hasil perjalanan (bonus gang + setiap anggota yang dikalahkan).
- **PINJAM KONSORSIUM hanya sekali** per perjalanan. Setelah itu tombolnya berubah menjadi "MODAL KURANG: hajar dulu anggotanya".

### 8. Suara baru (sintetis, tanpa file)
Notifikasi HP, sirene polisi, koin, dan teriakan warga.

## Sengaja tidak diubah
- Combat, hero kit, angka musuh, Garda, Koran, dan alur MODE PRESIDEN (selain MODAL awal dan pinjaman sekali).
- PvP.
- Fitur 0.0.8.2: kamera orbit, cubit zoom, KAMERA AWAL, joystick relatif kamera, LOMPAT, kolam & ramp, pemulihan jatuh, batas oval, LIHAT ARENA.
- File di `Assets/Konoha/Art/`.
- Peta: KARIER memakai kota yang sama. Gang, warung, pos ronda, dan taman menjadi kampungnya. Peta kampung khusus menyusul.
- **Tubuh karakter masih bentuk dasar dari kode**, hanya diberi pilihan pembuat karakter. Lompatan ke "realistis" butuh slot model warga 3D (FBX), direncanakan di 0.6.3 (`Docs/VISI_KONOHA_HIDUP_v3.md` §4).
- Belum ada siklus pagi–malam, kerja gorengan/konten/parkir, dan pemilihan RT. Semua itu untuk 0.6.1–0.6.2.
- KARIER hanya solo (host lokal), seperti campaign lainnya. Co-op belum.

## Checklist uji perangkat 0.6.0 (satu build untuk semuanya)
1. **Menu**: label 0.6.0, tiga tombol (KARIER, MODE PRESIDEN, REBUT KURSI).
2. **KARIER → pembuat karakter**: panel di kiri, hero menghadap kamera dari dekat. Ketik nama, lalu ketuk setiap pilihan (jenis, kulit, kepala, badan, baju). **Karakter harus langsung berubah.** Coba jilbab dan rok, lalu tekan **MULAI HIDUP**.
3. HUD harus menampilkan:
   - baris uang, ENERGI, RESTU, CATATAN HITAM;
   - panel target Ketua RT di kiri;
   - tombol **HP • KERJA**;
   - tombol S1/S2/ULT/GANTI HERO **tidak terlihat**. BASIC, DODGE, dan LOMPAT tetap ada.
4. **OJOL**: buka HP → OJOL. Hero harus **duduk di motor hijau** dan jalan lebih cepat. Ikuti **panah kecil** di tanah ke titik jemput. Penumpang naik, antar ke tujuan, lalu pesan tarif dan potongan aplikasi muncul. Berhenti lewat HP.
5. **KULI**: ambil sak di TUMPUKAN SEMEN (sak terlihat di pundak), bawa ke PROYEK, ulangi 5 kali, lalu cek upah dikurangi uang rokok mandor.
6. **BUZZER**: ke WARKOP, tunggu 3 detik. Uang naik, CATATAN HITAM naik. Ulangi beberapa kali sampai catatan di atas 50, dan sesekali polisi datang menjemput (PASAL KARET).
7. **Preman**: tunggu ±40 detik sampai PREMAN muncul di mulut gang, lalu lawan. Cek urutannya: warga teriak → warga berlarian datang & merekam → bapak berpeci melerai (preman diam 3 detik). Kalau lama, motor polisi datang dengan sirene, lalu coba ketiga pilihan di beberapa kejadian.
8. **REBAHAN** saat energi berkurang: hero diam, ada feed sosmed. **MAKAN** di warkop. **SAPA** ketiga kelompok warga.
9. Kumpulkan Rp 1.000.000 + RESTU 50, lalu **DAFTAR CALON RT** di pos ronda. **Grup WA** harus muncul.
10. **Tutup aplikasi, buka lagi → KARIER**: harus muncul "LANJUTKAN HIDUP" dengan uang/restu terakhir, dan avatar yang sama.
11. **MODE PRESIDEN** (buka ulang aplikasi): tidak ada pilar kuning, yang ada panah kecil. MODAL awal 25. Pinjam Konsorsium hanya bisa sekali.
12. Perhatikan FPS saat kerumunan dan polisi datang.

## Risiko
- Versi besar dalam satu langkah, jadi risiko error compile lebih tinggi daripada versi kecil. Semua file sudah lolos pemeriksaan sintaks dan review independen, tetapi pemeriksaan tipe yang sebenarnya baru terjadi di Unity. Kalau build gagal, kirim potongan log error-nya; biasanya bisa diperbaiki dalam satu commit kecil.
- Warga yang datang ke perkelahian dan motor polisi tidak punya collider dan bergerak lurus, jadi kadang **menembus bangunan atau pohon** dalam perjalanan.
- Pose naik motor dan mengangkat sak dibuat dari tubuh bentuk dasar, jadi bisa terlihat sedikit melayang. Mohon kirim screenshot kalau mengganggu.
- Untuk kembali ke menu dari KARIER (misalnya pindah ke MODE PRESIDEN), aplikasi perlu ditutup lalu dibuka lagi. Tombol "kembali ke menu" belum ada.
- Angka uang, energi, dan peluang polisi adalah angka awal, belum diseimbangkan di perangkat.
