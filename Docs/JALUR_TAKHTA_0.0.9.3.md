# Jalur Takhta 0.0.9.3 — Faksi Biro Prosedur ("Sahkan Berkas")

Branch kerja: `feat/biro-prosedur-0.0.9.3`, dibuat dari `feat/jalur-takhta-first-playable` (`dea94d1`; 0.0.9.2 + 0.0.9.2.1 sudah di-merge setelah lolos di tablet). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

**Tujuan (GAME_LOGIC §8.2):** segel Biro tidak lagi didapat dengan menekan SAHKAN 3×. Hero harus mengecap **3 LOKET**, mengusir antrian, menghadapi **STEMPEL TUNDA**, lalu mengalahkan **Kepala Biro** yang menunggu di balik pintu kantornya.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# (parser tree-sitter C#) dan `scripts/source-check.py`.

## Build manual Android/tablet (Unity Build Automation)

1. Pilih branch **`feat/biro-prosedur-0.0.9.3`**, Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`**.
3. APK: **KONOHA Jalur Takhta Preview**, versi **0.0.9.3**, kode Android **25** (menimpa 0.0.9.2.1).
4. Label bawah: `JALUR TAKHTA 0.0.9.3  •  SOLO PREVIEW`.

## Ruang Biro (sayap kanan/timur, x +28)

```
                 [GEDUNG BIRO PROSEDUR]
            |  KANTOR KEPALA BIRO  |      tembok 2,4 m (tidak bisa dilompati)
            |   (Kepala Biro)      |
            |______[PINTU]________ |      pintu biru, terbuka setelah 3 loket tercap
  [Security]
      (LOKET 1)          (LOKET 3)
                (LOKET 2)                  lingkaran 2 m, meja loket di belakangnya
   [Arsip]              [Arsip]
                   [PENGAWAS]              lingkaran biru 8 m = STEMPEL TUNDA
```

| Unit | Jumlah | Catatan |
|---|---|---|
| Kepala Biro (Pemimpin) | 1 | Terkunci di kantor, **tidak bisa diserang** ("TERKUNCI") sampai pintu terbuka |
| Pengawas (Spesialis) | 1 | Tidak mengejar. Memancarkan STEMPEL TUNDA |
| Petugas Arsip (Kroni) | 2 | Muncul lagi tiap 12 detik (maks. 2 hidup) sampai 3 loket tercap |
| Security (Guard) | 1 | Berjaga di dekat Loket 1 |

## Aturan

1. **Masuk radius 9 m dari gedung Biro:** muncul pesan "BIRO PROSEDUR! Cap 3 LOKET…". Checkpoint pindah ke Biro.
2. **LOKET:** berdiri di dalam lingkaran selama **3 detik** membuat loket tercap. Loket boleh dicap dalam urutan apa pun.
   - Lingkaran: biru = belum, emas = sedang diisi, **merah = ANTRIAN**, hijau = tercap. Papan di atas meja menampilkan "LOKET n" beserta persen, ANTRIAN, atau TERCAP.
   - **ANTRIAN:** jika ada anggota Biro di dalam lingkaran, progres **berhenti** (tidak berkurang). Usir atau kalahkan dia dulu.
   - Loket yang ditinggal **turun 20% per detik**. Loket yang sudah tercap tetap tercap.
3. **STEMPEL TUNDA:** selama Pengawas hidup dan hero berada ≤8 m darinya, **cooldown S1/S2 +30%**. Tanda lingkaran biru besar di bawah Pengawas, dan tulisan merah "STEMPEL TUNDA" di bawah baris hero.
   - Loket 2 dan 3 berada di dalam jangkauan Pengawas; Loket 1 tidak. Kalahkan Pengawas dulu agar skill kembali normal.
4. **Petugas Arsip** muncul lagi tiap 12 detik selama jumlahnya kurang dari 2, sampai 3 loket tercap.
5. **3 loket tercap:** pintu kantor terbuka, dan Kepala Biro keluar serta bisa diserang.
6. **SALAH LOKET (Kepala Biro):** jika hero dalam jarak 7 m, muncul lingkaran merah (2,5 m) **di bawah hero**. Jika setelah 1 detik hero masih di dalam lingkaran, hero **dipindah ke loket yang paling jauh**. Tidak ada damage. Cooldown 12 detik. Keluar dari lingkaran atau DODGE = aman.
7. **Kepala Biro tumbang (dan 3 loket tercap):** sisa anggota Biro menyerah, lalu **SEGEL BIRO + Pengaruh +25**.

Majelis dan Biro boleh dikerjakan dalam urutan apa pun. Gerbang Dalam terbuka setelah 2 segel.

## Yang dihapus

- Tombol **SAHKAN** dan objektif "SAHKAN 3×". Tombol aksi sekarang hanya **DUDUK** (di Kursi) dan **ULANG**.

## Teknis (untuk sesi berikutnya)

- `BiroEncounter` (logika murni + EditMode test): loket, antrian, peluruhan, respawn arsip, dan status selesai.
- `ICombatRules.GetCooldownMultiplier(actor)`: **satu titik** pengubah cooldown skill di `NetworkHeroKit` (`ModeCooldownMultiplier`). PvP selalu mengembalikan 1.
- `NetworkHeroKit.ServerTeleport(tujuan)`: dipakai SALAH LOKET. Tambahan saja; PvP tidak memanggilnya.
- `CampaignEnemy`: jenis serangan khusus (`EnemySpecial`: KETOK PALU / SALAH LOKET), status TERKUNCI (tidak bisa ditarget), area STEMPEL TUNDA, dan daftar musuh aktif (HUD tidak lagi mencari ke seluruh scene).
- `CampaignStage.Steer`: musuh masuk dan keluar kantor Kepala Biro hanya lewat pintunya.
- Angka baru (asumsi tuning, belum ada di §8.2): radius lingkaran SALAH LOKET 2,5 m, jarak pemicu 7 m, jeda pertama 3 detik, leash aula 13 m.

## Sengaja tidak diubah

Majelis Daun, Restu Rakyat, layar Pilih Hero, Garda Takhta, fase Memerintah, kamera, dan semua perilaku PvP.

## Checklist uji perangkat 0.0.9.3

1. Label `JALUR TAKHTA 0.0.9.3  •  SOLO PREVIEW`.
2. Setelah Gerbang Rakyat, pergi ke **BIRO (kanan)**. Harus terlihat 3 lingkaran loket dengan meja, kantor berpintu biru, dan lingkaran biru besar di bawah Pengawas.
3. Kepala Biro di dalam kantor bertuliskan **TERKUNCI**; skill dan serangan tidak bisa melukainya.
4. Berdiri di Loket 1: lingkaran berubah emas, persen naik, dan tercap setelah ±3 detik. Keluar di tengah jalan: persen turun pelan.
5. Biarkan Petugas Arsip masuk ke lingkaran loket yang sedang diisi: lingkaran merah, "ANTRIAN", progres berhenti.
6. Dekati Pengawas: muncul **"STEMPEL TUNDA: cooldown skill +30%"**. Kalahkan dia: tulisan dan lingkaran birunya hilang.
7. Kalahkan satu Petugas Arsip, lalu tunggu ±12 detik: ada Arsip baru.
8. 3 loket tercap: pintu terbuka dan Kepala Biro keluar. Coba **SALAH LOKET**: diam di lingkaran → pindah ke loket terjauh; keluar dari lingkaran → aman.
9. Kepala Biro tumbang: sisa anggota menyerah, lalu **SEGEL BIRO**, Pengaruh +25. Jika Majelis sudah, Gerbang Dalam terbuka.
10. Tidak ada tombol SAHKAN lagi. Tombol DUDUK di Kursi dan ULANG tetap ada.
11. Coba urutan **Biro dulu, baru Majelis**: harus tetap bisa menang.
12. **PvP 4v4 sekali:** cooldown skill harus sama seperti sebelumnya.

## Risiko

- Belum di-compile Unity. Bagian yang paling mungkin error: `CampaignDirector.cs` dan `CampaignEnemy.cs`.
- Kepala Biro bisa tersangkut di sudut kantor, walau sudah diberi penunjuk jalan lewat pintu.
- Lingkaran STEMPEL TUNDA berdiameter 16 m cukup besar di lantai. Jika mengganggu, bisa dibuat lebih samar.
- Tingkat kesulitan Biro belum diuji; semua angka ada di `CampaignTuning.Biro`.
