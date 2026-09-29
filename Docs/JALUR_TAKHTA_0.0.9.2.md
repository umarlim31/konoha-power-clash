# Jalur Takhta 0.0.9.2 — Faksi Majelis Daun ("Pecahkan Blok")

Branch kerja: `feat/majelis-daun-0.0.9.2`, dibuat dari `feat/jalur-takhta-first-playable` (`feaf128`, 0.0.9.1 sudah di-merge setelah lolos di tablet). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

**Tujuan (GAME_LOGIC §8.1):** segel Majelis tidak lagi didapat dengan berdiri di lingkaran. Hero harus menghadapi sidang Majelis Daun sungguhan: pecahkan **Blok Majelis** dengan menjatuhkan **Anggota Senior** lebih dulu, hindari **KETOK PALU** Ketua, lalu tumbangkan Ketua.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# (parser tree-sitter C#) dan `scripts/source-check.py`.

## Build manual Android/tablet (Unity Build Automation)

1. Pilih branch **`feat/majelis-daun-0.0.9.2`**, Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`**.
3. APK: **KONOHA Jalur Takhta Preview**, versi **0.0.9.2**, kode Android **23** (menimpa 0.0.9.1).
4. Label bawah: `JALUR TAKHTA 0.0.9.2  •  SOLO PREVIEW`.

## Sidang Majelis Daun (solo)

Anggota sidang muncul di depan gedung Majelis (sayap kiri/barat, x −28) begitu hero mencapai Plaza Aspirasi. Mereka tidak mengejar keluar dari aula: di luar radius 11 m dari gedung, mereka pulang ke posnya.

| Unit | Jumlah | Wibawa | Catatan |
|---|---|---|---|
| Ketua Majelis (Pemimpin) | 1 | 320 | Membawa palu; punya KETOK PALU |
| Anggota Senior | 2 | 160 | Selama ≥2 berdiri, Blok Majelis aktif |
| Staf Fraksi (Kroni) | 3 | 40 | Menyerah saat Ketua tumbang |
| Pengawal Sidang (Guard) | 1 | 90 | |

Alur pertarungan:

1. **Masuk radius 9 m dari gedung:** muncul pesan "SIDANG MAJELIS! … incar ANGGOTA SENIOR dulu". Checkpoint pindah ke Majelis.
2. **Blok Majelis:** selama 2 Senior masih berdiri, *semua* anggota Majelis menerima damage **−40%**. Penandanya cincin burgundy lebar di kaki mereka dan tulisan "• BLOK" di papan nama.
3. **Senior pertama tumbang:** muncul "BLOK MAJELIS PECAH!", cincin burgundy hilang, dan damage kembali penuh. Blok tidak terbentuk lagi, termasuk setelah hero Runtuh.
4. **KETOK PALU (Ketua):** jika hero berada dalam 4 m, Ketua berhenti dan mengangkat palu. Di tanah muncul lingkaran merah tua berjari-jari 3 m, 1,8 m di depannya, dan lingkaran oranye di dalamnya membesar selama **1 detik**. Hero yang masih di dalam lingkaran saat itu kena **22 damage**. Cooldown **9 detik**; ketukan pertama paling cepat 3 detik setelah Ketua mulai bertarung.
   - Keluar dari lingkaran (jalan atau DODGE) = aman.
   - **Stun** Ketua saat ia mengangkat palu (misalnya skill ABAH) membatalkan ketukan.
5. **Ketua tumbang:** Staf Fraksi berlutut dengan warna pucat bertuliskan MENYERAH, lalu hilang setelah 3 detik. Mereka tidak bisa diserang lagi.
6. **Sidang selesai** (Ketua tumbang dan tidak ada Senior/Pengawal tersisa): **SEGEL MAJELIS** + **Pengaruh +25**.

Segel Majelis dan Biro boleh dikerjakan dalam urutan apa pun. Biro Prosedur masih memakai mekanik sementara (SAHKAN 3×) sampai 0.0.9.3.

## HUD

- Objektif Majelis bertahap: "BLOK aktif (−40%)! Kalahkan ANGGOTA SENIOR (n tersisa)" → "Blok pecah! Tumbangkan KETUA MAJELIS (awas KETOK PALU)" → "Kalahkan sisa pejabat (n)".
- Bar progres = anggota sidang yang sudah jatuh/menyerah.
- Penunjuk arah menunjuk **SENIOR** terdekat selama blok aktif, lalu **KETUA**.
- Sebelum memilih sektor: "PLAZA ASPIRASI • Rebut 2 segel: MAJELIS (kiri) & BIRO (kanan)".

## Perubahan lain

- **Checkpoint Majelis/Biro** dipindah ke koridor sayap di (∓14, −5), di luar radius aula. Hero yang bangkit tidak lagi jatuh di tengah sidang.
- **Papan "MENUJU ISTANA TAKHTA"** dipindah ke bawah lintel gapura Gerbang Rakyat (5,15 m). Posisi lama (z −49, 1,9 m) berada di antara kamera spawn dan hero.
- Cincin merah SISTEM di kaki musuh dinaikkan sedikit supaya tidak tenggelam di lantai koridor.

## Teknis (untuk sesi berikutnya)

- `MajelisEncounter` (logika murni, EditMode test): status blok, Ketua tumbang, dan sidang selesai.
- `ICombatActorState` (Networking): pengali damage masuk dan status "bisa ditarget" per aktor. Hanya musuh campaign yang memakainya. Hero/bot PvP tidak punya komponen ini, jadi perilaku PvP tidak berubah.
- `CampaignEnemy`: leash area, blok, menyerah, KETOK PALU dengan peringatan (state di-replikasi lewat NetworkVariable), serta visual palu, cincin blok, dan lingkaran peringatan.
- `CampaignObjectiveDirector`: mekanik "tahan di lingkaran" dihapus. Diganti event `MajelisRequested`, `MajelisEngaged`, dan `CompleteMajelis()`.
- Angka baru (asumsi tuning, belum ada di §8.1): offset lingkaran 1,8 m, jarak pemicu 4 m, jeda ketukan pertama 3 detik, radius mulai sidang 9 m, leash 11 m.

## Sengaja tidak diubah

Gerbang Rakyat, Biro (masih sementara), Garda Takhta, fase Memerintah, peta 0.0.9.1, kamera, dan semua bagian PvP.

## Checklist uji perangkat 0.0.9.2

1. Label `JALUR TAKHTA 0.0.9.2  •  SOLO PREVIEW`. Dari spawn, papan "MENUJU ISTANA TAKHTA" harus tergantung di gapura dan tidak lagi menutupi hero.
2. Kalahkan 3 Kroni gerbang, lalu masuk plaza. Objektif menjadi "Rebut 2 segel…".
3. Ikuti penunjuk ke **MAJELIS (kiri)**. Terlihat 7 anggota berwarna burgundy-emas, dan Ketua (paling besar) memegang palu.
4. Pukul **Ketua** dulu saat blok aktif: angka damage harus terasa lebih kecil (−40%). Screenshot papan nama "• BLOK" dan cincin burgundy.
5. Jatuhkan satu **Senior**: harus muncul "BLOK MAJELIS PECAH!" dan cincin burgundy hilang.
6. Dekati Ketua: palu terangkat dan lingkaran merah muncul. Coba (a) keluar dari lingkaran → tidak kena; (b) diam di dalam → Wibawa berkurang 22.
7. (Opsional) Pakai **ABAH**, stun Ketua saat lingkaran muncul: ketukan batal.
8. Tumbangkan Ketua: Staf Fraksi tersisa berlutut "MENYERAH" lalu hilang. Kalahkan sisa Senior/Pengawal → "SEGEL MAJELIS diperoleh! Pengaruh +25".
9. Lari menjauh (>11 m dari gedung) saat bertarung: anggota sidang harus kembali ke posnya, tidak mengejar ke plaza.
10. Sengaja Runtuh di Majelis: hero bangkit di koridor (x −14), tidak di tengah sidang.
11. Setelah Biro (SAHKAN 3×), Gerbang Dalam terbuka dan Garda, Kursi, serta menang tetap berjalan seperti 0.0.9.1. Tekan ULANG: sidang Majelis muncul lagi lengkap.
12. FPS saat bertarung dengan 7 anggota sekaligus.

## Risiko

- Belum di-compile Unity. Kalau ada error, kemungkinan besar di `CampaignEnemy.cs` / `CampaignDirector.cs` (lihat log tahap compile).
- Tingkat kesulitan belum diuji: 7 musuh sekaligus bisa terlalu berat untuk solo. Semua angka ada di `CampaignTuning.Majelis`.
- Lingkaran peringatan memakai disk tipis di atas lantai. Di dekat tangga gedung, disk bisa tampak terpotong.
- Musuh masih bisa tersangkut di sudut gedung karena belum ada navmesh.
