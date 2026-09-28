# Jalur Takhta 0.0.9 — solo sebagai host lokal + combat asli

Branch kerja: `feat/jalur-takhta-0.0.9`, dibuat dari `feat/jalur-takhta-first-playable` (`06f479b`, 0.0.8.3 sudah di-merge). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

**Urutan versi diubah atas persetujuan owner:** 0.0.8.4 (MEGA 3D) ditunda karena file FBX MEGA belum ada. Versi ini mengerjakan isi Prompt 3 (combat asli di campaign). MEGA 3D tetap bisa dikerjakan kapan saja setelah file tersedia.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# untuk semua file (parser tree-sitter C#) dan `scripts/source-check.py`.

## Build manual Android/tablet (Unity Build Automation)

1. Di target Android Unity Build Automation yang sudah ada, pilih branch **`feat/jalur-takhta-0.0.9`**. Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`** (huruf kecil). Jangan diubah.
3. Jalankan build manual. APK: **KONOHA Jalur Takhta Preview**, versi **0.0.9**, kode Android **21**, package `com.konoha.powerclash.jalurtakhta` (menimpa 0.0.8.3).
4. Label di bawah layar harus `JALUR TAKHTA 0.0.9  •  SOLO PREVIEW`.

Jika build gagal, unduh log lengkap dan kirim bagian yang mengandung `error CS` atau `Exception`.

## Yang berubah

### 1. Satu sistem combat untuk dua mode (`ICombatRules`)
- Komponen combat hero yang sama (`NetworkPlayerCombat`, `NetworkHeroKit`, `NetworkPlayerMovement`, `NetworkWibawaBar`) sekarang bertanya kepada **aturan mode yang aktif** (`CombatRules.Current`), bukan langsung ke `NetworkMatchManager`.
- **Rebut Kursi (PvP):** `NetworkMatchManager` adalah penyedia aturannya dengan jawaban yang persis sama seperti sebelumnya (boleh main, penguasa kursi, ganti hero hanya saat Waiting/Result, spawn tim, respawn 6 detik). Prefab, scene, dan versi PvP tidak berubah.
- **Jalur Takhta:** `CampaignDirector` adalah penyedia aturannya. Semua hero satu tim melawan tim **SISTEM**. Kursi campaign adalah status objektif, jadi hero boleh bertarung saat duduk.
- `INetworkAiActor` menandai aktor yang dikendalikan server (bot PvP, musuh campaign). Aktor seperti ini tidak mengikat HUD pemain, dan damage-nya dikreditkan ke aktor itu, bukan ke pemain host.
- Wibawa maksimum sekarang per aktor (`MaxWibawaValue`). Hero tetap 100; musuh memakai nilai peran §7 (Kroni 40, Guard 90, Pemimpin 320, dst.).

### 2. Solo = host Netcode lokal (keputusan §14)
- `CampaignSession` otomatis menjalankan `StartHost()` saat scene dibuka: loopback `127.0.0.1`, tanpa room, tanpa internet (bisa mode pesawat). Lalu memunculkan `CampaignDirector` dan hero pemain (`NetworkPlayer.prefab`, prefab yang sama dengan PvP).
- Kontrol solo 0.0.8.2 dipertahankan: `CampaignTraversal` sekarang menggerakkan **hero jaringan** (joystick relatif kamera, LOMPAT, batas oval, pemulihan jatuh, ramp kolam). `NetworkPlayerMovement` hanya menerapkan kunci gerak (Runtuh/stun), kecepatan hero, dan DODGE (arahnya sekarang relatif kamera di campaign).
- Karakter offline lama tetap ada di scene sebagai "probe" untuk test editor, lalu disembunyikan saat hero jaringan siap.

### 3. Musuh organisasi dengan combat asli (`CampaignEnemy`)
- Prefab baru `CampaignEnemy.prefab`: memakai `NetworkPlayerCombat` + `NetworkHeroKit`, jadi **semua** skill hero (BASIC, S1, S2, ULT, knockback, stun, Narasi, dll.) langsung bekerja pada musuh.
- Otak sederhana per peran: mengejar hero terdekat dalam 10 m, Guard hanya menjaga radius 7 m dari posnya, serangan jarak dekat tiap 1,6 dtk (angka tuning, §7 tidak menyebut interval), memutari monumen tengah.
- Visual primitif: kapsul + selempang/jambul/bahu berwarna faksi, skala per peran (Pemimpin terbesar), cincin merah SISTEM, papan nama "JUDUL • ORGANISASI", dan bar Wibawa. Musuh yang tumbang redup, lalu hilang setelah 1,4 detik.

### 4. Alur run 0.0.9
1. **Gerbang Rakyat:** 3 Kroni (tutorial). Plaza baru terbuka setelah ketiganya tumbang.
2. **Plaza, Majelis, Biro:** masih mekanik sementara 0.0.8.x (tahan lingkaran / SAHKAN 3×). Mekanik faksi asli masuk di 0.0.9.2 dan 0.0.9.3.
3. **Gerbang Dalam terbuka:** Panglima Takhta (Pemimpin, 320) dan Pengawal Takhta (Guard, 90) langsung berjaga di pos Garda. Fase Garda dimulai saat hero masuk radius 9 m. Kursi terbuka setelah keduanya tumbang.
4. **Duduk:** Kuasa +5/dtk sampai 35 (angka slice lama). Setelah 2,5 dtk datang satu serangan balik (Guard), yang ditarik ke area Kursi.
5. **Menang:** musuh tersisa dihapus. Tombol **ULANG** me-reset run, menghidupkan hero di Gerbang Rakyat, dan memunculkan 3 Kroni lagi.

### 5. Runtuh dan checkpoint (§6)
- Hero yang tumbang bangkit setelah **4 detik** di **checkpoint terakhir** (Gerbang Rakyat → Plaza → Majelis/Biro → Garda), bukan di awal peta.
- Pengaruh tersisa 70% (−30%, dibulatkan ke bawah).
- Musuh yang masih hidup tidak di-reset, tetapi memulihkan 50% dari Wibawa yang hilang.
- Pengaruh bonus saat menjatuhkan musuh: Kroni/Guard +5, Elite/Pemimpin +15 (§5).

### 6. HUD campaign
- Kontrol asli (nama objek sama dengan PvP supaya komponen hero terikat otomatis): **BASIC** (besar, kanan bawah), **ULT** di atasnya, **S1/S2**, **DODGE/LOMPAT**, tombol konteks **SAHKAN/DUDUK/ULANG**, **GANTI HERO** di kiri atas di bawah LIHAT ARENA. KAMERA AWAL, geser kamera, dan cubit zoom tetap.
- Baris hero di kiri atas: `MEGA • WIBAWA 80/100 • PENGARUH 40%` (atau `RUNTUH`/`STUN`).
- Status atas: `SEGEL x/2 | KUASA y/35 | RUNTUH n`. Penunjuk arah mengarah ke Kroni atau pengawal terdekat saat bertarung.

### 7. Test
- `CampaignObjectiveDirectorTests` diperbarui: Plaza terkunci sampai gerbang bersih, sinyal Garda sekali, satu serangan balik, menang, Runtuh tidak me-reset Garda.
- `CampaignPreviewSceneTests`: memeriksa host/prefab/kontrol baru dan bahwa **semua titik spawn musuh dan checkpoint bebas dari geometri** (uji kapsul yang sama dengan rute).
- Assembly test sekarang mereferensikan `Unity.Netcode.Runtime` (dibutuhkan untuk tipe `CampaignEnemy`/`CampaignDirector`).

## Sengaja tidak diubah
- Scene, prefab, HUD, bot, dan aturan PvP 4v4; string versi PvP.
- Mekanik Majelis/Biro (0.0.9.2/0.0.9.3), Fase Memerintah penuh + layar hasil (0.1.0), peta rute baru (0.0.9.1).
- Pemicu animasi Attack/Skill untuk hero jaringan (masuk bersama MEGA 3D). Hero masih primitif, jadi tidak ada perubahan di layar.

## Checklist uji perangkat 0.0.9
1. Label bawah `JALUR TAKHTA 0.0.9  •  SOLO PREVIEW`. Buka juga dengan **mode pesawat aktif**: game harus tetap jalan.
2. Hero MEGA muncul di Gerbang Rakyat dengan cincin tim dan bar Wibawa. Karakter lama (kapsul gelap) **tidak** terlihat.
3. Gerak: joystick relatif kamera, geser layar kanan (orbit), cubit (zoom), KAMERA AWAL, LOMPAT (tanpa lompat di udara), keluar kolam tanpa lompat, jatuh ke bawah dunia lalu kembali. Semua harus sama dengan 0.0.8.3.
4. Tiga Kroni datang. Pukul dengan **BASIC**, coba **S1**, **S2**, **DODGE**. Angka damage muncul, Kroni redup lalu hilang, dan objektif menghitung `Kalahkan Kroni x/3`.
5. Setelah Kroni habis, ke Plaza → Majelis (tahan) → Biro (SAHKAN 3×). Setelah dua segel, **Panglima Takhta** dan **Pengawal Takhta** terlihat berjaga di utara.
6. Isi **ULT** (Pengaruh 100%) lalu pakai pada pengawal.
7. Biarkan hero **tumbang** sekali di pertarungan Garda: bangkit sekitar 4 detik kemudian di dekat jalur timur (bukan di Gerbang Rakyat), Pengaruh berkurang, pengawal tetap ada.
8. Kalahkan Garda → segel Kursi hilang → DUDUK → satu pengawal datang menyerang → pertahankan sampai `JALUR TAKHTA SELESAI` → **ULANG** mengembalikan ke awal dengan 3 Kroni baru.
9. **GANTI HERO**: bisa berganti hero di luar fase duduk; visual dan skill berubah.
10. LIHAT ARENA / KEMBALI MAIN saat bertarung: permainan berhenti dan lanjut tanpa kontrol macet.
11. Perhatikan FPS saat 3 Kroni dan saat Garda bertarung.

Jika ada yang gagal, catat nomor langkah dan lampirkan video pendek.

## Risiko yang diketahui
- **Belum pernah di-compile Unity.** Kode memakai API Netcode yang sama dengan PvP yang sudah terbukti (ServerRpc/ClientRpc, NetworkVariable, AddNetworkPrefab, StartHost), tetapi error compile tetap mungkin muncul.
- Keseimbangan belum disetel: Pemimpin 320 Wibawa + Guard bisa terasa berat untuk solo. Checkpoint dan Pengaruh dimaksudkan untuk membantu. Laporkan jika terasa mustahil.
- Musuh tidak memakai navmesh. Mereka bisa tersangkut di sudut bangunan (ada jalan memutar untuk monumen tengah saja).
- Posisi checkpoint Garda `(4.4, -2)` dipilih di luar radius aggro/jaga. Jika hero bangkit dan langsung diserang, laporkan.
- Nama tombol PvP (`AttackButton`, dll.) dipakai ulang di campaign supaya komponen hero terikat otomatis. Jangan mengganti nama tombol ini tanpa mengubah komponennya.
