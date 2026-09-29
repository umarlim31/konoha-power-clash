# Jalur Takhta 0.0.9.2.1 — Restu Rakyat, kamera stabil, pilih hero di awal

Branch kerja: `feat/jalur-takhta-0.0.9.2.1`, dibuat dari `feat/majelis-daun-0.0.9.2` (`c493aa2`). Versi ini perbaikan dari hasil uji 0.0.9.2 di tablet, jadi PR ini sekaligus membawa 0.0.9.2. PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# (parser tree-sitter C#) dan `scripts/source-check.py`.

## Build manual Android/tablet (Unity Build Automation)

1. Pilih branch **`feat/jalur-takhta-0.0.9.2.1`**, Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`**.
3. APK: **KONOHA Jalur Takhta Preview**, versi **0.0.9.2.1**, kode Android **24** (menimpa 0.0.9.2).
4. Label bawah: `JALUR TAKHTA 0.0.9.2.1  •  SOLO PREVIEW`.

## Masukan dari uji 0.0.9.2

- Majelis Daun terlalu sulit (RUNTUH 4 dan 9 di screenshot).
- Kamera kadang zoom-in sendiri saat memakai kemampuan.
- Hero bisa diganti kapan saja; seharusnya dipilih di awal dan terkunci.
- (Temuan tambahan) Teks objektif Majelis terpotong ("…Kalahkan ANGGOTA").

## 1. RESTU RAKYAT (hadiah Gerbang Rakyat)

Setelah 3 Kroni Gerbang Rakyat kalah, hero mendapat **RESTU RAKYAT** untuk sisa perjalanan:

| Efek | Nilai |
|---|---|
| Damage hero ke musuh | **+30%** |
| Damage yang diterima hero | **−25%** |
| Saat didapat | Wibawa penuh + **Pengaruh +30** |

- Tetap aktif setelah Runtuh. Hilang saat ULANG (harus direbut lagi).
- Penanda: cincin emas di kaki hero dan tulisan "• RESTU" di baris hero. Objektif gerbang menulis "hadiah RESTU".
- Tetap terasa pentingnya Senior: pukulan ke anggota yang dilindungi Blok menjadi 1,30 × 0,60 = **0,78** dari damage normal. Blok masih perlu dipecahkan, tapi tidak lagi terasa mustahil.

## 1b. Musuh bergiliran menyerang (tambahan dariku)

Penyebab utama Majelis terasa mustahil adalah semua anggota memukul **bersamaan**. Tujuh anggota yang mengepung bisa memberi sekitar 40 damage/detik, jadi Wibawa 100 habis dalam ±2,5 detik.

- Mulai versi ini, **pukulan biasa dari musuh yang berbeda ke hero yang sama diberi jarak minimal 0,5 detik**. Kerumunan menyerang bergiliran, tidak serentak.
- Batas damage biasa menjadi sekitar 15–20 damage/detik. Setelah RESTU (−25%) sekitar 11–15 damage/detik, jadi hero bertahan sekitar 7 detik saat dikepung penuh. Masih berbahaya, tapi bisa dilawan dengan DODGE dan skill.
- KETOK PALU tidak ikut aturan ini (tetap 22 damage dengan peringatan 1 detik).
- Aturan ini juga berlaku di Gerbang Rakyat dan Garda Takhta.

## 2. Kamera tidak zoom-in sendiri

Penyebab yang ditemukan di kode:

1. **Kotak penghalang dari skill** (S2 PERISAI RAKYAT milik MEGA membuat dua pagar kader setinggi 2 m) punya collider. Kamera orbit menganggapnya tembok, lalu langsung maju mendekati hero.
2. **Musuh yang berkerumun** di antara kamera dan hero juga dianggap tembok, sehingga kamera tersedot maju saat bertarung.
3. **Batang palem, lampu, dan tiang bendera** yang tipis ikut menarik kamera.
4. **Jari kedua yang menempel sebentar** di area kamera (misalnya ibu jari meleset dari tombol skill) langsung dibaca sebagai cubit-zoom.

Perbaikan:

- Kamera orbit sekarang **hanya mundur-maju karena bangunan/tembok padat**. Karakter, pagar skill, dan benda tipis (< 0,7 m) diabaikan.
- Kotak dari skill ditaruh di layer *Ignore Raycast*. Kotak tetap menghalangi gerak, tapi kamera tidak lagi bereaksi padanya.
- **Zona mati cubit:** zoom baru jalan setelah dua jari benar-benar menjauh/mendekat sekitar 3,5% tinggi layar.
- Kamera PvP tidak memakai kode orbit ini, jadi tidak berubah.

## 3. Pilih hero di awal (terkunci per perjalanan)

- Saat game dibuka muncul layar **PILIH HERO**: MEGA, GEMOY, ABAH, PAK WI. Ada deskripsi singkat skill tiap hero dan tombol **MULAI**.
- Selama layar ini terbuka, dunia menunggu: hero dan musuh tidak bergerak dan tidak bertarung.
- Setelah MULAI, hero **terkunci**. Tombol "HERO …" di kiri atas hanya menjadi penanda (tidak bisa ditekan).
- Hero hanya bisa diganti setelah **ULANG**, baik karena menang maupun di mulai ulang berikutnya. Nanti, saat ada level berikutnya, layar yang sama dipakai di awal level baru.
- Pengecekan dilakukan di host (`CanSelectHero`), jadi tidak bisa diakali dari sisi klien untuk co-op nanti.

## Perbaikan kecil

- Teks objektif dipendekkan supaya tidak terpotong, misalnya: "MAJELIS • BLOK −40%! Kalahkan SENIOR (2 lagi)".
- Pesan di tengah layar tampil 3,2 detik (sebelumnya 2,2 detik), karena pesan RESTU dan SIDANG lebih panjang.
- Koreksi catatan 0.0.9.2: hero yang bisa **stun** Ketua adalah **MEGA** (S1 SERUAN IBU), bukan ABAH. PIDATO milik ABAH hanya membungkam.

## Teknis (untuk sesi berikutnya)

- `ICombatRules.GetDamageMultiplier(attacker, target)` baru. PvP (`NetworkMatchManager`) selalu mengembalikan 1, sedangkan campaign menerapkan RESTU (`RestuRakyat`, logika murni + test).
- `CampaignDirector`: `heroLocked` dan `restu` (NetworkVariable), `RequestStartRun()`. `AllowsGameplay` hanya bernilai benar setelah MULAI; `CanSelectHero` hanya sebelum MULAI.
- `CampaignHeroSelect` (UI baru), `NetworkHeroKit.TrySelectHero(hero)`.
- `CampaignEnemy`: jarak antar-pukulan per hero (`Encounters.TargetHitSpacingSeconds`).
- `MobileCombatCamera.IgnoredByOrbit`, dan zona mati cubit di `CampaignCameraDrag`.

## Sengaja tidak diubah

Aturan Majelis (Blok −40%, KETOK PALU 22/9 detik), jumlah dan status musuh, Biro (masih sementara sampai 0.0.9.3), Garda, Kursi, dan semua perilaku PvP.

## Checklist uji perangkat 0.0.9.2.1

1. Label `JALUR TAKHTA 0.0.9.2.1  •  SOLO PREVIEW`.
2. **Layar PILIH HERO** muncul di awal. Tekan tiap hero: warna tombol, deskripsi, dan nama hero di baris kiri atas ikut berganti. Coba gerakkan joystick: hero tidak boleh bergerak sebelum MULAI.
3. Tekan **MULAI**. Tombol "HERO …" di kiri atas tidak bisa ditekan lagi.
4. Kalahkan 3 Kroni gerbang: harus muncul pesan **RESTU RAKYAT**, cincin emas di kaki, "• RESTU", Wibawa penuh, dan Pengaruh +30.
5. Majelis Daun: berapa kali Runtuh sampai segel didapat? (Target: 0–2 kali.) Musuh seharusnya terasa bergiliran menyerang.
6. **Kamera:** pakai semua skill MEGA (terutama S2 PERISAI RAKYAT), GEMOY CMD LEAP, dan dash, sambil dikepung musuh. Kamera tidak boleh tiba-tiba mendekat. Cubit-zoom dengan dua jari harus tetap berfungsi.
7. Menang lalu tekan **ULANG**: layar PILIH HERO muncul lagi, dan hero boleh diganti.
8. Teks objektif Majelis tidak lagi terpotong.

## Risiko

- Belum di-compile Unity. Perubahan menyentuh kode bersama (`CombatRules`, `NetworkPlayerCombat`, `NetworkHeroKit`), jadi setelah build, **cek juga mode PvP 4v4 sekali**: damage, skill, dan ganti hero di lobi harus sama seperti sebelumnya.
- Mungkin sekarang terlalu mudah. Semua angka ada di `CampaignTuning.Restu` dan `Encounters.TargetHitSpacingSeconds`.
- Karena kamera mengabaikan benda tipis, kamera bisa sesekali menembus batang palem (tampak menembus sesaat). Ini sengaja: lebih baik daripada zoom mendadak.
- ULT PROYEK milik PAK WI di campaign masih membuat bangunan di koordinat arena PvP (dekat monumen). Belum disentuh; perlu ditangani terpisah.
