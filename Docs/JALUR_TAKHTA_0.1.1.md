# Jalur Takhta 0.1.1 — Majelis selalu muncul, DUDUK = MENANG

Branch kerja: `feat/jalur-takhta-0.1.1`, dibuat dari `feat/jalur-takhta-first-playable` (`216c15a`; 0.0.9.4 + 0.1.0 sudah di-merge setelah terpasang dan dimainkan di tablet). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`.

## Build manual Android/tablet (Unity Build Automation)

1. Pilih branch **`feat/jalur-takhta-0.1.1`**, Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`**.
3. APK: versi **0.1.1**, kode Android **28**. Label: `JALUR TAKHTA 0.1.1  •  SOLO PREVIEW`.
4. Jika pemasangan menolak dengan pesan "paket tidak valid", **hapus dulu aplikasi lama**, lalu pasang lagi. Penyebabnya: tanda tangan debug berbeda antar-mesin build (belum ada keystore tetap).

## Masukan dari uji 0.1.0

1. **Majelis kosong sampai Biro selesai.** Penyebabnya, fase Plaza baru dimulai saat hero menyentuh lingkaran kecil (2,4 m) di tengah plaza. Hero yang lewat taman samping langsung ke Majelis tidak pernah menyentuhnya, sehingga Majelis dan Biro belum dibuka. Keduanya baru muncul setelah hero kebetulan lewat tengah plaza.
   - **Perbaikan:** setelah Gerbang Rakyat bersih, berjalan lebih dari ±15 m ke utara (melewati taman gapura) juga dihitung sebagai "sampai di Plaza". Majelis dan Biro kini sudah siap sebelum hero tiba di sana.
2. **Musuh terus muncul setelah Gerbang Dalam, dan tidak jelas kapan berakhir.** Itu gelombang serangan balik Fase Memerintah (tiap 15 detik sampai Kuasa 100).
   - **Keputusan owner:** musuh boleh terus datang, tetapi **berhasil DUDUK di Kursi = MENANG** (level selesai).
   - Mulai 0.1.1: setelah Panglima tumbang, **KURSI TERBUKA**. Gelombang pasukan datang tiap 15 detik (yang pertama 6 detik kemudian) **sampai hero duduk**. DUDUK → **TAKHTA DIKUASAI!** → semua musuh pergi → layar hasil.
   - Fase Memerintah §9 (Kuasa 100, BERDIRI, KUDETA) **tidak dihapus**, hanya dimatikan lewat `CampaignTuning.Memerintah.SeatWinsRun = true`. Fase ini bisa dipakai lagi untuk level berikutnya. EditMode test tetap mengujinya.

## Catatan desain

Ini menyimpang dari `GAME_LOGIC_JALUR_TAKHTA_v1.md` §9, atas keputusan owner. Akibatnya, satu run menjadi lebih pendek (±1 menit lebih cepat dari rencana), dan ketegangan akhir berpindah dari "bertahan di Kursi" menjadi "tembus ke Kursi". Target 8–15 menit perlu diukur ulang dari layar hasil.

## Layar hasil

Tombolnya masih ULANG dan GANTI HERO, ditambah catatan "LEVEL 2 segera hadir". **Level 2 belum dibuat.** Isinya perlu diputuskan bersama sebagai fase berikutnya.

## Checklist uji perangkat 0.1.1

1. Label `JALUR TAKHTA 0.1.1  •  SOLO PREVIEW`.
2. Kalahkan 3 Kroni gerbang, lalu **langsung lewat taman kiri ke Majelis** (jangan lewat tengah plaza). Anggota Majelis harus sudah ada.
3. Selesaikan Majelis dan Biro, lalu kalahkan Panglima → KURSI TERBUKA. Objektif: "Naik ramp, DUDUK = MENANG".
4. Tunggu sebentar di bawah: ±6 detik kemudian gelombang pertama datang, lalu tiap 15 detik.
5. Naik ramp, tekan **DUDUK** → fanfare, musuh hilang, lalu layar hasil muncul. **Kirim screenshot layar hasil**, karena waktu run perlu dicatat.
6. ULANG dan GANTI HERO tetap berfungsi.

## Risiko

- Belum di-compile Unity.
- Run bisa terasa terlalu cepat berakhir. Kalau begitu, pilihan berikutnya: Kuasa lebih kecil (misalnya 30 detik bertahan), atau level 2.
