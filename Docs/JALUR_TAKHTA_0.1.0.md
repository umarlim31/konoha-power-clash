# Jalur Takhta 0.1.0 — MVP

Branch kerja: `feat/jalur-takhta-mvp-0.1.0`, dibuat dari `feat/jalur-takhta-0.0.9.4` (`117ba94`). 0.0.9.4 (kamera + suara) ikut di dalamnya. PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

(Prompt 7 menyebut nama `JALUR_TAKHTA_0.1.0_MVP.md`; berkas ini memakai `JALUR_TAKHTA_0.1.0.md` karena `scripts/source-check.py` mewajibkan nama catatan = versi.)

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# (parser tree-sitter C#) dan `scripts/source-check.py`.

## Build manual Android/tablet (Unity Build Automation)

1. Pilih branch **`feat/jalur-takhta-mvp-0.1.0`**, Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`**.
3. APK: **KONOHA Jalur Takhta Preview**, versi **0.1.0**, kode Android **27**.
4. Label bawah: `JALUR TAKHTA 0.1.0  •  SOLO PREVIEW`. Menu utama menampilkan "Versi 0.1.0".
5. APK kini berisi **dua scene**: campaign (dibuka pertama) dan PvP 4v4.

## Yang baru

### 1. Menu mode (layar pertama)
- **JALUR TAKHTA (Solo):** host lokal offline dimulai hanya setelah tombol ini ditekan. Tidak ada room PvP. Setelah itu tampil layar PILIH HERO.
- **REBUT KURSI (PvP 4v4):** membuka scene PvP yang **tidak diubah**. Objek jaringan campaign dihapus dulu, supaya hanya ada satu NetworkManager.
- Untuk kembali ke menu dari PvP: tutup lalu buka lagi aplikasinya. Tombol kembali belum ada, karena PvP sengaja tidak disentuh.

### 2. Garda Takhta (§8.3)
- Komposisi solo: **Panglima Takhta** + **2 Pengawal**. **Bala bantuan 2 Kroni** datang sekali saat Panglima di bawah 50% Wibawa.
- **LOCKDOWN:** saat hero masuk ±7 m dari pos Garda, cincin penghalang (tiang + pita merah-emas) menutup lapangan, termasuk jalan ramp. Cincin terbuka lagi saat Panglima tumbang, atau saat hero Runtuh (hero bangkit di luar cincin, di checkpoint 5, lalu masuk lagi).
- Cincin hanya menutup selama Panglima berada di dalamnya, supaya hero tidak pernah terkunci jauh dari Panglima. Jam DORONGAN pertama dimulai saat cincin menutup.
- **DORONGAN BALIK (COUNTER PUSH):** tiap 12 detik, jika hero ≤3,5 m, lingkaran 4 m muncul di sekitar Panglima (peringatan 1,2 detik). Hero yang masih di dalam didorong 5 m dan kena 15 damage.
- **Panglima tumbang** → sisa pasukan menyerah → **KURSI TERBUKA**. Pengawal tidak wajib dikalahkan.
- Checkpoint 5 dipindah ke (0, 18,5) supaya tidak menempel ke dinding cincin.

### 3. Fase Memerintah (§9)
- **DUDUK** menempatkan hero tepat di depan Kursi. Joystick dan LOMPAT terkunci; serangan dan skill tetap aktif, dan jangkauan **Basic +1 m**.
- Tombol berubah menjadi **BERDIRI** (keluar dari Kursi; Kuasa berhenti, tidak berkurang).
- **Kuasa +2/detik**, target **100** (±50 detik tanpa gangguan).
- **Serangan balik tiap 15 detik:** 2 Kroni + 1 Pengawal, bergantian berwarna Majelis dan Biro ("sisa kekuatan lama"), maksimal 6 hidup. Mereka naik lewat ramp.
- Terdorong keluar dari Kursi → Kuasa berhenti, lalu tekan DUDUK lagi.
- **Runtuh saat memerintah:** Kursi lepas dan hero bangkit di checkpoint 5. HUD: "Runtuh n/3".
- **KUDETA:** Runtuh ke-3 selama memerintah → Kuasa kembali 0, hitungan direset, dan serangan balik dibersihkan. Kursi harus direbut lagi.

### 4. Layar hasil (§10)
- Muncul 1,6 detik setelah "TAKHTA DIKUASAI!".
- Isinya: hero, **waktu run**, **jumlah Runtuh**, **Pengaruh terkumpul** (semua perolehan; yang terpakai untuk ULT tidak mengurangi), dan **ARKETIPE: TAKHTA BESI**.
- Tombol: **ULANG** (hero sama, langsung mulai lagi dari Gerbang Rakyat) dan **GANTI HERO** (kembali ke layar PILIH HERO).

### 5. Pemulihan Wibawa (tambahan, dari hasil uji RUNTUH 9–12)
- Tidak kena damage selama **4 detik** → Wibawa pulih **10/detik**.
- Tanpa pemulihan, setiap pertarungan menggerus pertarungan berikutnya. Nilai ada di `CampaignTuning.Recovery`.

### 6. Perbaikan bug layar PILIH HERO
- Komponen layar ini sebelumnya menempel di panelnya sendiri. Begitu panel disembunyikan (setelah MULAI), komponennya berhenti bekerja, sehingga layar tidak pernah muncul lagi setelah ULANG. Sekarang pengontrolnya terpisah dari panel.

## Audit §16 "MVP selesai"

| Kriteria | Di kode | Harus diuji owner |
|---|---|---|
| Solo dimulai dari menu tanpa membuat room PvP | ✅ Menu → host lokal loopback; PvP hanya lewat REBUT KURSI | Buka APK → JALUR TAKHTA |
| Kursi terlihat dari spawn, tidak bisa disentuh sebelum Garda tumbang | ✅ Test garis pandang (0.0.9.1); segel Kursi aktif sampai KURSI TERBUKA | Screenshot spawn |
| Majelis dan Biro terasa berbeda | ✅ Blok/Senior/KETOK PALU vs loket/antrian/STEMPEL/SALAH LOKET | Pendapatmu |
| Runtuh → checkpoint, bukan awal peta | ✅ Checkpoint 1–5 | Runtuh di tiap sektor |
| Spawn → MENANG dalam 8–15 menit | ⚠️ Perkiraan kasar 8–12 menit (belum pernah diukur) | **Catat waktu di layar hasil** |
| PvP 4v4 tetap identik | ✅ Scene PvP tidak diubah; kait campaign di kode bersama bernilai netral di PvP | Main REBUT KURSI sekali |
| 30+ FPS di Infinix XPad 20 | ❓ Tidak bisa diukur di sini | Perhatikan saat Garda + serangan balik |

## Skenario uji lengkap: spawn → MENANG

1. Buka APK: **menu mode**. Label bawah 0.1.0. Tekan **JALUR TAKHTA**.
2. **PILIH HERO** → MULAI (gong).
3. **Gerbang Rakyat:** kalahkan 3 Kroni → RESTU RAKYAT.
4. **Plaza:** pilih Majelis (kiri) atau Biro (kanan).
5. **Majelis:** jatuhkan Senior (BLOK PECAH), hindari KETOK PALU, tumbangkan Ketua → SEGEL MAJELIS.
6. **Biro:** cap 3 loket (usir ANTRIAN), kalahkan Pengawas bila STEMPEL TUNDA mengganggu, pintu terbuka, hindari SALAH LOKET, tumbangkan Kepala Biro → SEGEL BIRO.
7. **Gerbang Dalam** terbuka → masuk lapangan Garda → **LOCKDOWN** menutup.
8. Lawan Panglima: hindari **DORONGAN BALIK**. Di bawah 50% → **BALA BANTUAN**. Panglima tumbang → sisa menyerah → **KURSI TERBUKA**, cincin terbuka.
9. Naik ramp → **DUDUK**. Joystick terkunci, tombol berubah jadi BERDIRI.
10. Bertahan: serangan balik di detik 15, 30, 45. Coba **BERDIRI** lalu DUDUK lagi: Kuasa berhenti lalu lanjut.
11. **Kuasa 100** → fanfare → **layar hasil**. Catat waktu dan jumlah Runtuh.
12. Tekan **ULANG**: langsung main lagi dengan hero sama. Menangkan lagi (atau cukup sampai Gerbang Rakyat), lalu coba **GANTI HERO**: layar PILIH HERO muncul.
13. (Opsional) Uji **KUDETA**: saat memerintah, sengaja Runtuh 3× → pesan KUDETA, Kuasa 0.
14. Tutup aplikasi → buka → **REBUT KURSI**: PvP 4v4 harus sama seperti dulu.

## Sengaja tidak diubah / ditunda

- Scene dan kode PvP, kecuali kait netral (pengali cooldown/damage/jangkauan = 1/0 di PvP).
- Model 3D MEGA (menunggu FBX), Modal/Koneksi dan arketipe lain, co-op.
- Tombol kembali dari PvP ke menu.

## Risiko

- Build ini **besar**: 0.0.9.4 + seluruh 0.1.0. Kalau gagal compile, kirim log. Jika 0.0.9.4 sudah di-build lebih dulu, lokasi error lebih mudah dipersempit.
- Perpindahan scene ke PvP (REBUT KURSI) baru pertama kali dicoba. Jika PvP bermasalah saat dibuka dari menu, itu kemungkinan terkait NetworkManager yang tersisa.
- Keseimbangan Garda + Memerintah belum diuji: Panglima 320 Wibawa, DORONGAN, dan gelombang tiap 15 detik.
- Duduk memakai teleport ke depan Kursi (belum ada animasi duduk).
