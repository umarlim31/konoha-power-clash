# Jalur Takhta 0.2.4 — Keseimbangan 4 hero (mode solo)

Branch kerja: `feat/balance-hero-0.2.4`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.2.3 (tubuh & efek) di-merge berdasarkan hasil uji di tablet: keempat hero sudah dicoba, dan satu run GEMOY menang dalam 3:27. PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`.

Masukan owner: "yang paling lemah adalah PAK WI, buat agar semua kekuatannya sama, balance."

## Build manual Android/tablet (Unity Build Automation)

1. Branch **`feat/balance-hero-0.2.4`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.2.4**, kode Android **33**. Label bawah: `JALUR TAKHTA 0.2.4  •  SOLO PREVIEW`.

## Audit: kenapa PAK WI paling lemah

Di mode solo tidak ada kawan, dan musuh tidak memakai skill. Akibatnya, beberapa bagian kit hero, yang dirancang untuk PvP 4v4, tidak berguna.

| Hero | Basic (damage per detik) | Skill yang tidak berguna di solo |
|---|---|---|
| MEGA | 18 per 0,64 dtk = **28/dtk**, jarak dekat | – (PERISAI RAKYAT hanya bertahan) |
| GEMOY | 24 per 0,88 dtk = **27/dtk** | BARIS! memberi perisai ke sekutu di belakang, padahal tidak ada sekutu |
| ABAH | 17 per 0,72 dtk = **24/dtk**, tapi jarak **6,8 m** (aman) | PIDATO membungkam musuh, padahal musuh tidak punya skill |
| PAK WI | 15 per 0,68 dtk = **22/dtk**, jarak dekat | **Semuanya:** INFRASTRUKTUR hanya mempercepat; BLUSUKAN tanpa damage; PROYEK membangun tembok di titik tetap peta PvP, yang di solo malah muncul di plaza |

PAK WI praktis hanya punya pukulan biasa, dan pukulannya paling lemah di antara keempat hero.

## Perubahan (HANYA mode solo)

| Hero | Sebelum | Sesudah (solo) |
|---|---|---|
| PAK WI basic | 15 damage | **19 damage** |
| PAK WI S1 INFRASTRUKTUR | jalan 8 detik, lari +35% | Sama, ditambah saat PAK WI berdiri di atas jalannya: **damage +25%, damage diterima −20%** |
| PAK WI S2 BLUSUKAN | melesat 6 m | Melesat 6 m, **18 damage ke musuh di lintasan**, dan **perisai 15** |
| PAK WI ULT PROYEK | tembok di titik tetap PvP | **Ledakan 30 damage radius 7 m**, musuh terdorong 3 m, dan **perisai 25** |
| MEGA S2 PERISAI RAKYAT | kader penahan | Sama, ditambah **perisai 15** untuk MEGA |
| GEMOY S2 BARIS! | 10 damage + perisai untuk sekutu | **16 damage** + **perisai 10** untuk GEMOY sendiri |
| ABAH ULT PIDATO | 18 damage + bungkam | **30 damage** + musuh terdorong 3 m (bungkam tetap ada) |

- Layar PILIH HERO menampilkan deskripsi skill yang baru.
- Efek ULT PROYEK kini berupa gelombang besar dan 3 lokasi proyek dengan kerucut di sekitar PAK WI.

### Perkiraan kekuatan (30 detik melawan 3 musuh)

| Hero | Sebelum (solo) | Sesudah |
|---|---|---|
| MEGA | 776 | 808 (−3% dari rata-rata) |
| GEMOY | 844 | 899 (+8%) |
| ABAH | 779 | 788 (−5%, tapi paling aman karena jarak jauh) |
| PAK WI | **463** (−35%) | **824** (−1%) |

- Semua hero kini berada dalam ±10% dari rata-rata.
- Ini hitungan kasar dari angka skill; rumusnya ada di `HeroBalance.EstimatedPressure`.
- Rasa sebenarnya hanya bisa dibuktikan lewat uji di tablet.

## Sengaja tidak diubah

- **PvP 4v4:** memakai angka asli. Test `PvpKeepsTheOriginalNumbers` menjaga ini. Keseimbangan PvP (di mana PAK WI punya kawan) perlu diuji dengan pemain sungguhan, jadi tidak diubah tanpa keputusan owner.
- Cooldown skill, Wibawa (100 untuk semua hero), dan jarak basic.
- Musuh dan aturan sektor. Majelis masih terasa sulit (Runtuh 2 di video MEGA/ABAH); itu bisa ditangani terpisah kalau owner mau.

## Checklist uji perangkat 0.2.4

1. Label `JALUR TAKHTA 0.2.4  •  SOLO PREVIEW`. Layar PILIH HERO menampilkan deskripsi PAK WI yang baru.
2. **PAK WI:**
   - Pasang INFRASTRUKTUR lalu bertarung di atas jalannya: musuh lebih cepat tumbang dan kamu lebih tahan.
   - BLUSUKAN menembus kerumunan: angka damage muncul di musuh yang dilewati, dan perisai terisi.
   - ULT PROYEK di tengah kerumunan: musuh terpental dan terkena 30 damage.
   - Tidak ada lagi tembok proyek yang muncul di plaza.
3. **MEGA:** PERISAI RAKYAT memberi perisai (angka BLOCK saat dipukul).
4. **GEMOY:** BARIS! memberi 16 damage dan perisai.
5. **ABAH:** ULT PIDATO mendorong musuh.
6. Mainkan satu run penuh dengan tiap hero. Catat **waktu** dan **jumlah Runtuh** di layar hasil, lalu kirim keempatnya supaya bisa kubandingkan.
7. PvP (REBUT KURSI): tetap seperti dulu.

## Risiko

- Perkiraan kekuatan tidak sama dengan rasa bermain. Setelah angka waktu dan Runtuh per hero dari owner masuk, angkanya disetel lagi di `HeroBalance` (satu tempat).
- ABAH tetap unggul dalam keamanan karena jarak 6,8 m. Kalau terasa terlalu mudah, damage basic-nya bisa diturunkan sedikit.
