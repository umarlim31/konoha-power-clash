# Jalur Takhta 0.0.9.1 — peta rute Kompleks Kekuasaan

Branch kerja: `feat/jalur-takhta-0.0.9.1`, dibuat dari `feat/jalur-takhta-first-playable` (`05088d2`, 0.0.9 sudah di-merge setelah lolos di tablet). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

**Tujuan:** peta berubah dari satu plaza menjadi **perjalanan** sesuai GAME_LOGIC §13. Kursi terlihat dari spawn, tetapi jauh dan terkunci.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# (parser tree-sitter C#), `scripts/source-check.py`, dan hitungan geometri garis pandang (lihat di bawah).

## Build manual Android/tablet (Unity Build Automation)

1. Pilih branch **`feat/jalur-takhta-0.0.9.1`**, Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`**.
3. APK: **KONOHA Jalur Takhta Preview**, versi **0.0.9.1**, kode Android **22** (menimpa 0.0.9).
4. Label bawah: `JALUR TAKHTA 0.0.9.1  •  SOLO PREVIEW`.

## Peta baru (arah utara = menuju Kursi)

```
              [ISTANA TAKHTA]  gedung kubah, z ~58, di atas teras 5 m
              [KURSI]          z 46,5, teras 5 m, pylon bermahkota perunggu
                 |  ramp 24° (satu-satunya jalan naik)
              [GARDA TAKHTA]   lapangan upacara z ~28, dua penutup rendah
   ===========[GERBANG DALAM]=========== tembok z 17, pintu merah TERKUNCI
 [MAJELIS DAUN] --- [PLAZA ASPIRASI + monumen] --- [BIRO PROSEDUR]
    x -28              hub (0, 0)                      x +28
                 |  boulevard + palem
              [GERBANG RAKYAT] gapura z -37, spawn z -44
```

- **Batas oval tak terlihat** diperbesar dari 26×29 m menjadi 34×58 m (sekitar 68×116 m). Lantai collision juga diperluas.
- **Plaza lama dipertahankan** sebagai Plaza Aspirasi: monumen, kanal (ramp keluar tetap), taman, dan paviliun. Kursi tidak lagi di tengah plaza.
- **Majelis Daun dan Biro Prosedur** dipindah ke sayap x ±28, dihubungkan oleh koridor z −5. Semua penghalang padat di koridor itu dihapus. Bendera dan pita memakai warna faksi (burgundy-emas / krem-biru).
- **Gerbang Dalam:** tembok selebar kompleks dengan pintu merah padat. Pintu terbuka (berayun ke samping) begitu dua segel didapat. Tembok memakai layer *Ignore Raycast* supaya kamera orbit tidak tersedot mendekat saat hero berdiri di sampingnya.
- **Garda Takhta:** lapangan upacara biru tua. Panglima dan Pengawal berjaga di sini, bukan lagi di dalam plaza.
- **Istana Takhta:** teras batu 5 m, ramp berlantai garis ukir dan balustrade, Kursi di atas cincin perunggu dan karpet merah, dua pylon 11 m bermahkota di belakangnya, dan gedung kubah di belakang teras.
- **Papan nama** besar per sektor: GERBANG RAKYAT, PLAZA ASPIRASI, MAJELIS DAUN, BIRO PROSEDUR, GERBANG DALAM, GARDA TAKHTA, ISTANA TAKHTA, plus penunjuk arah "< MAJELIS DAUN" dan "BIRO PROSEDUR >".

## Kursi terlihat dari spawn (§13)

- **Pitch kamera awal campaign** turun dari 38° menjadi **22°**, dan batas bawah pitch menjadi 16° (PvP tetap 38°/25°). Pada 38°, tepi atas layar ada 13° di bawah horizon, sehingga Istana di kejauhan tidak pernah masuk layar.
- **Monumen Garuda** diperkecil ke skala 0,85 supaya tidak menutupi Kursi.
- Hitungan geometri dari kamera spawn: garis pandang ke Kursi melewati monumen pada 7,17 m (puncak patung 6,42 m), pintu Gerbang Dalam pada 6,74 m (pintu 4,6 m), dan tepi teras pada 6,03 m (teras 5 m). Kursi berada 1,6° di bawah horizon, sementara tepi atas layar ada di 3° di atas horizon.
- Kabut dibuat lebih tipis dan jauh (60–175 m, sebelumnya 43–105 m), dan far clip menjadi 190 m, supaya Istana di jarak sekitar 110 m masih terbaca.

## Titik baru

| Hal | Posisi |
|---|---|
| Spawn / checkpoint 1 | (0, −44) |
| 3 Kroni tutorial | sekitar z −30, di balik gapura; tidak mengejar sampai hero mendekat |
| Checkpoint 2 Plaza | (0, −6,5) |
| Checkpoint 3/4 Majelis/Biro | (∓28, −5) |
| Checkpoint 5 Garda | (0, 19,5), tepat di dalam Gerbang Dalam, di luar radius aggro/jaga |
| Panglima / Pengawal | (0, 30) / (3,5, 28) |
| Serangan balik | muncul di (−6, 30), naik lewat ramp ke Kursi |

Musuh punya **penunjuk jalan sederhana** (`CampaignStage.Steer`): ke teras hanya lewat ramp, dan melewati tembok hanya lewat gerbang. Ini tetap tanpa navmesh.

## LIHAT ARENA
Kamera overview dinaikkan dan dijauhkan (fokus (0, 2, 4), offset (0, 62, −70)) supaya seluruh rute terlihat.

## Test
- Rute collision lengkap dengan ketinggian: spawn → gapura → plaza → Majelis → Biro → gerbang dalam → Garda → ramp → teras.
- Semua titik spawn musuh dan checkpoint bebas dari geometri.
- **Garis pandang:** raycast dari kamera spawn default ke Kursi tidak terhalang, Kursi di dalam frame, dan garis melewati puncak patung yang diukur dari bounds renderer.
- Lompat tidak bisa melewati segel Kursi di teras maupun pintu Gerbang Dalam yang terkunci.
- Papan nama, gerbang, dan batas oval ada.

## Perkiraan performa
Penambahan sekitar +320 renderer statis (gapura dan boulevard ~160, Garda ~105, Istana ~90, Gerbang Dalam ~50), dikurangi ~110 dari taman/pot di koridor yang dihapus. Semuanya ditandai *Batching Static* dan memakai material yang sama. Karena far clip lebih jauh, lebih banyak objek bisa tergambar sekaligus. **Belum diukur di perangkat**, jadi FPS perlu dicek (langkah 9 di bawah).

## Sengaja tidak diubah
Combat, musuh, objektif Majelis/Biro sementara, fase Memerintah, dan semua bagian PvP (kamera PvP memakai nilai default yang sama).

## Checklist uji perangkat 0.0.9.1
1. Label `JALUR TAKHTA 0.0.9.1  •  SOLO PREVIEW`.
2. **Dari spawn, tanpa memutar kamera:** apakah Kursi, pylon bermahkota, atau kubah Istana terlihat di atas layar? Screenshot layar pertama.
3. Jalan lewat gapura GERBANG RAKYAT dan kalahkan 3 Kroni. Apakah pitch kamera baru enak untuk bertarung? (Bisa diubah dengan geser layar kanan; KAMERA AWAL kembali ke 22°.)
4. Plaza → ikuti penunjuk ke **Majelis (kiri, barat)**, lalu ke **Biro (kanan, timur)**. Jalur koridor harus bebas tersangkut.
5. Sebelum dua segel: pintu merah Gerbang Dalam tertutup dan tidak bisa dilompati. Sesudahnya: pintu terbuka ke samping.
6. Kalahkan Garda di lapangan biru. Tumbang sekali: hero harus bangkit tepat di dalam gerbang, tidak langsung diserang.
7. Naik **ramp** ke teras, lalu duduk di Kursi. Pengawal serangan balik harus naik lewat ramp, bukan tersangkut di dinding teras.
8. **LIHAT ARENA** menampilkan seluruh rute.
9. **FPS** saat berjalan di boulevard menghadap Istana dan saat bertarung di lapangan Garda. Laporkan jika terasa patah-patah.

## Risiko
- Belum di-compile Unity. Perubahan kali ini sebagian besar ada di generator editor (geometri), jadi kalau ada error, kemungkinan besar muncul di tahap *pre-export* di log.
- Pitch 22° bisa terasa rendah untuk bertarung. Mudah disetel (`resetPitch`) begitu ada masukan.
- Waktu jalan dari spawn ke Kursi sekitar 20–25 detik tanpa pertarungan. Belum dinilai terlalu lama atau tidak.
- Musuh bisa tersangkut di sudut tembok/menara yang tidak tercakup penunjuk jalan.
