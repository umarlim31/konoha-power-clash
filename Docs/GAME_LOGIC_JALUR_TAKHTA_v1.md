# JALUR TAKHTA — Game Logic Spec v1 (MVP)

Status: **Design authority untuk implementasi MVP campaign.** Turunan dari Game Concept Bible v2.0.
Semua angka = **nilai awal**, disetel ulang setelah uji di perangkat. Angka disimpan di satu tempat (`CampaignTuning`), bukan tersebar di kode.

> Fantasi inti: *"Aku bisa melihat Kursi. Aku tahu mau ke mana. Tapi seluruh sistem berdiri di antara aku dan kursi itu."*

---

## 1. Keputusan yang sudah dikunci (owner, 2026-09-28)

1. Urutan produksi: 0.0.8.3 fondasi → 0.0.9 peta rute → 0.0.9.x faksi Majelis & Biro → 0.1.0 Garda + fase bertahan (MVP lengkap). Visual MEGA 3D berjalan paralel.
2. MEGA langsung memakai **jalur model 3D sungguhan (FBX + rig humanoid)**, bukan primitive.
3. Solo-first. Arsitektur harus siap co-op 1–4 pemain.
4. Mode PvP Rebut Kursi 4v4 tetap ada dan tidak boleh rusak.

## 2. Struktur mode

```
MENU UTAMA
├── JALUR TAKHTA (Solo, 1 pemain)            ← MVP
├── BENTUK KOALISI (Co-op 2–4)               ← setelah MVP terbukti seru
└── REBUT KURSI (PvP 4v4)                    ← fondasi sudah ada, dipertahankan
```

Mulai 0.0.9, campaign dan PvP boleh berada dalam satu APK dengan menu pilihan mode. Selama 0.0.8.x, keduanya tetap terpisah seperti sekarang.

## 3. Loop satu run (MVP)

```
PILIH HERO
 → ACT 1  GERBANG RAKYAT     : tutorial gerak + combat kecil (3 Kroni)            [checkpoint 1]
 → ACT 1b PLAZA ASPIRASI     : hub; Kursi terlihat jauh; Gerbang Dalam terkunci   [checkpoint 2]
 → ACT 2  MAJELIS DAUN       : pecahkan Blok Majelis → SEGEL MAJELIS              [checkpoint 3]
 → ACT 3  BIRO PROSEDUR      : sahkan 3 loket di bawah tekanan → SEGEL BIRO       [checkpoint 4]
      (urutan ACT 2 dan ACT 3 bebas; pemain memilih cabang)
 → GERBANG DALAM             : terbuka otomatis jika Segel ≥ SegelDibutuhkan (MVP: 2)
 → ACT 4  GARDA TAKHTA       : Panglima + Guard, arena LOCKDOWN                    [checkpoint 5]
 → KURSI TERBUKA             : tekan DUDUK
 → FASE MEMERINTAH           : kumpulkan Power 0→100 sambil menahan serangan balik
 → HASIL                     : waktu, jumlah Runtuh, arketipe akhir
```

**Aturan emas:** Kursi terlihat sejak spawn (landmark tinggi di ujung peta), tetapi secara fisik tidak bisa dicapai sebelum `CampaignRunState` mengizinkan. Gerbang dikunci oleh state, bukan hanya oleh jarak.

## 4. State machine (sudah ada di `CampaignRunState`, diperluas)

```
GerbangRakyat → PlazaAspirasi → GerbangDalam → GardaTakhta → KursiTerbuka ⇄ Memerintah → Menang
                     ↑ (segel dikumpulkan di sini)                 ↑ LoseSeat()
```
Perluasan yang dibutuhkan:
- `SectorState` per sektor: `Terkunci`, `Tersedia`, `Berlangsung`, `Selesai`.
- `Checkpoint` terakhir yang dicapai; Runtuh akan respawn di sini, **bukan** di awal peta.
- `RuntuhCount`, `RunTime`, dan `ResourceLedger` (lihat §5) untuk layar hasil.
- Semua transisi hanya boleh dipanggil oleh **pemilik otoritas** (host). Klien hanya membaca.

## 5. Resource

| Resource | MVP? | Aturan |
|---|---|---|
| **WIBAWA** | Ya | = HP hero (100, sudah ada di combat). 0 → **RUNTUH**. |
| **PENGARUH** | Ya | 0–100, sudah ada. Ditambah oleh: basic yang kena +4, skill yang kena +6, Kroni tumbang +5, Elite tumbang +15, segel +25. Dipakai untuk Ultimate (100). |
| **SEGEL** | Ya | Akses. Satu per sektor faksi. MVP: butuh 2. Arsitektur siap "3 dari 5". |
| **MODAL** | Tidak (0.2) | Hanya didefinisikan di data. Nantinya: jalan pintas, sponsor buff, suap gerbang. |
| **KONEKSI** | Tidak (0.2) | Hanya didefinisikan di data. Nantinya: rekrut, pecah koalisi, rute alternatif. |

> Alasan menunda Modal/Koneksi: keduanya baru bermakna setelah ada ≥3 faksi dan pilihan rute. Di MVP, keduanya hanya akan menjadi angka yang tidak dipakai.

## 6. Aturan combat di campaign

- **Memakai combat hero yang sudah ada** (Basic, S1, S2, Ultimate, Dodge, Wibawa, Pengaruh, Runtuh). Tidak ada sistem combat kedua.
- **RUNTUH di campaign:** hero jatuh → 4 detik → respawn di checkpoint terakhir dengan Wibawa 100 dan Pengaruh −30% (dibulatkan ke bawah). Musuh sektor yang belum selesai **tidak** ter-reset, tetapi yang terluka pulih 50%.
- **Friendly fire:** tidak ada.
- Musuh PvE adalah tim tersendiri (`Team.Sistem`) yang memusuhi pemain dan tidak saling serang (MVP).

## 7. Model organisasi musuh

Musuh didesain sebagai **organisasi**, bukan jumlah bot.

| Peran | Wibawa | Damage/hit | Kecepatan | Perilaku |
|---|---|---|---|---|
| **Kroni** | 40 | 6 | 3.2 | Kejar target terdekat, serang jarak dekat. Mudah jatuh. |
| **Guard** | 90 | 10 | 2.8 | Jaga titik/formasi; kejar hanya dalam radius jaga 7 m. |
| **Spesialis** | 70 | 5 | 3.0 | Tidak mengejar; memberi efek (aura/debuff/spawn). Bertahan di belakang. |
| **Senior / Letnan** | 160 | 12 | 2.8 | Elite, memberi buff faksi selama hidup. |
| **Pemimpin / Boss** | 320 | 16 | 2.6 | Punya 1 serangan khusus bertelegraf (tanda di tanah 1 detik sebelum kena). |

Setiap unit wajib memiliki: nameplate (peran dan faksi), warna faksi, bar Wibawa jika Elite/Boss, dan **telegraf** untuk serangan khusus. Mobile harus bisa membaca bahaya.

## 8. Faksi MVP

### 8.1 MAJELIS DAUN — "Pecahkan Blok"
**Komposisi solo:** 1 Ketua Majelis (Boss) · 2 Anggota Senior (Senior) · 3 Staf Fraksi (Kroni) · 1 Pengawal Sidang (Guard).
**Warna:** burgundy + emas. **Ruang:** ruang sidang terbuka, podium setengah lingkaran.

**Mekanik VOTING BLOCK:**
- Selama **≥2 Senior hidup**: semua unit Majelis mendapat **pengurangan damage 40%**, dan ring burgundy terlihat di bawah mereka.
- Senior pertama tumbang → notifikasi **"BLOK MAJELIS PECAH!"** → buff hilang.
- Ketua memiliki serangan khusus **"KETOK PALU"**: AoE radius 3 m di depan, telegraf 1 detik, damage 22, cooldown 9 detik.
- Ketua tumbang → sisa Kroni **menyerah** (berlutut, tidak menyerang, hilang 3 detik kemudian).
- Selesai → **SEGEL MAJELIS** + Pengaruh +25.

**Pelajaran untuk pemain:** incar Senior dulu, jangan langsung ke bos. Ini identitas "formasi dan hierarki".

### 8.2 BIRO PROSEDUR — "Sahkan Berkas"
**Komposisi solo:** 1 Kepala Biro (Boss, di balik gerbang) · 1 Pengawas (Spesialis) · 2 Petugas Arsip (Kroni, respawn) · 1 Security (Guard).
**Warna:** abu-beige + biru arsip. **Ruang:** lorong loket dengan tiga pos berjajar dan pintu berlapis di ujung.

**Mekanik:**
- **3 LOKET** (zona radius 2 m). Berdiri di zona untuk mengisi 3 detik → loket tercap. Progres **berhenti** jika ada musuh di dalam zona (**ANTRIAN**) dan turun 20%/detik jika pemain keluar.
- **STEMPEL TUNDA:** selama Pengawas hidup dan pemain dalam radius 8 m darinya, cooldown skill pemain **+30%**. Ikon debuff tampil di HUD.
- **Petugas Arsip** muncul lagi setiap 12 detik (maksimal 2 hidup) sampai 3 loket selesai.
- 3 loket selesai → pintu Kepala Biro terbuka.
- Kepala Biro punya serangan khusus **"SALAH LOKET"**: memindahkan pemain ke salah satu loket (teleport pendek, telegraf lingkaran 1 detik), cooldown 12 detik.
- Selesai → **SEGEL BIRO** + Pengaruh +25.

**Pelajaran untuk pemain:** bunuh Pengawas dan bersihkan zona dulu. Ini identitas "birokrasi dan penundaan", berbeda dari Majelis.

### 8.3 GARDA TAKHTA — "Gerbang Terakhir"
**Komposisi solo:** 1 Panglima Takhta (Boss) · 2 Guard · bala bantuan 2 Kroni saat Panglima di bawah 50%.
**Warna:** navy gelap + emas + logam.
- **LOCKDOWN:** saat encounter mulai, cincin penghalang menutup area Garda. Tidak bisa kabur, dan terbuka setelah Panglima tumbang.
- **COUNTER PUSH:** setiap 12 detik Panglima melakukan dorongan AoE radius 4 m (telegraf 1,2 detik), mendorong pemain 5 m dan memberi damage 15.
- Panglima tumbang → **KURSI TERBUKA**.

## 9. Fase Memerintah (setelah DUDUK)

- Duduk → hero terkunci di Kursi. Tombol serangan tetap aktif (serangan dari kursi dengan jangkauan Basic +1 m).
- **Power +2/detik** saat duduk. Target 100 (sekitar 50 detik tanpa gangguan).
- **Serangan balik** setiap 15 detik: gelombang 2 Kroni + 1 Guard dari sisa faksi (warna faksi yang sudah dikalahkan, bernarasi "sisa kekuatan lama").
- Keluar dari radius Kursi (didorong atau berdiri) → Power **berhenti**, tidak berkurang. Harus duduk lagi.
- Runtuh saat Memerintah → kehilangan Kursi, respawn di checkpoint 5.
- **KUDETA (kalah):** Runtuh 3× dalam Fase Memerintah → run gagal dan kembali ke checkpoint 5 dengan hitungan reset.
- Power 100 → **MENANG**.

## 10. Layar hasil (MVP)

Tampilkan: waktu run, jumlah Runtuh, Pengaruh terkumpul, dan **arketipe akhir**. MVP hanya memiliki jalur combat sehingga selalu **TAKHTA BESI**. Arketipe lain (Mandat Rakyat, Raja Koalisi, Sultan Modal, Boneka Sistem) muncul setelah Modal dan Koneksi hadir. Tombol: **ULANG** dan **GANTI HERO**.

## 11. Scaling pemain (disiapkan, belum diuji di MVP)

Scaling menambah **komposisi**, bukan hanya HP.

| Pemain | Majelis | Biro | Garda |
|---|---|---|---|
| 1 | Ketua, 2 Senior, 3 Kroni, 1 Guard | Kepala, 1 Pengawas, 2 Arsip, 1 Security | Panglima, 2 Guard, +2 Kroni |
| 2 | +1 Senior (block butuh ≥2 dari 3), +2 Kroni | +1 Security, Arsip maks 3 | +1 Guard |
| 3 | +1 Guard, +2 Kroni | +1 Pengawas (2 aura) | +1 Guard, +2 Kroni bantuan |
| 4 | +1 Senior, +3 Kroni | 4 loket (paralel) | +2 Guard, 2 gelombang bantuan |

Diimplementasikan sebagai fungsi murni: `EncounterComposer.Compose(faction, playerCount) → List<UnitSpawn>` dengan EditMode test.

## 12. Hero di campaign

| Hero | Peran | Nilai di Jalur Takhta |
|---|---|---|
| **MEGA** | Frontline / Tank / Area Control | Masuk duluan, menahan Blok Majelis, perisai tim |
| **GEMOY** | Commander / Sustain | Tahan garis depan, buff, kontrol tempo |
| **ABAH** | Control / Narasi | Zoning, jarak jauh, cocok melawan Spesialis |
| **PAK WI** | Mobilitas / Objective | Cepat merebut loket Biro, membuka jalur |

**Kit MEGA diselaraskan ke Character Sheet** (mulai 0.0.9.x, bukan 0.0.8.3):
- Pasif **AKAR RAKYAT**: serangan memberi tanda pada musuh; tanda menambah pertahanan MEGA dan sekutu di dekatnya.
- S1 **SERUAN IBU**: gelombang suara kerucut, damage + slow.
- S2 **PERISAI RAKYAT**: area perisai untuk diri dan sekutu, mengurangi damage.
- Ultimate **IBU PERTIWI**: memanggil roh penjaga (bentuk orisinal Konoha) untuk damage area besar dan pertahanan sekutu.
- (**LANGKAH PERSATUAN**, dash + knock-up, ditunda. Dodge yang ada tetap dipakai.)
- Nama skill lama "BANTENG CHARGE" dan "KADER!" diganti karena terlalu dekat dengan identitas partai nyata.

## 13. Tata letak peta MVP (0.0.9)

Arah utara (+Z) = menuju Kursi. Ukuran sekitar 70 × 110 m, dengan batas oval tak terlihat seperti 0.0.8.2.

```
                 [ISTANA TAKHTA + KURSI di dais tinggi]   z≈+45   ← terlihat dari spawn
                          [ARENA GARDA]                   z≈+30
                        [GERBANG DALAM] (terkunci)        z≈+18
   [MAJELIS DAUN] ——— [PLAZA ASPIRASI + monumen] ——— [BIRO PROSEDUR]   z≈0
      x≈-28                   hub                          x≈+28
                        [GERBANG RAKYAT] (spawn)          z≈-40
```
- Jalur utama lurus spawn → plaza → gerbang dalam. Majelis dan Biro adalah cabang kiri/kanan.
- Monumen burung Konoha di plaza **tidak boleh menutupi** garis pandang ke Kursi dari kamera default.
- Plaza Takhta (arena akhir) mengikuti concept art "Plaza Takhta": kanal, jembatan, taman, dan high-ground. Arena ini juga calon arena PvP Rebut Kursi.
- Tiap sektor punya bahasa visual sendiri (§8) dan papan nama besar.

## 14. Arsitektur teknis (wajib diikuti)

**Keputusan: solo = local host Netcode (NGO `StartHost` tanpa klien).**
Alasan: seluruh combat sudah server-authoritative (`NetworkPlayerCombat`, `NetworkHeroKit` memakai NetworkVariable/ServerRpc). Menjalankan solo sebagai host lokal berarti:
1. Tidak ada combat kedua yang ditulis ulang.
2. Co-op 2–4 cukup berupa klien yang bergabung ke host yang sama.
3. Tidak butuh internet. Host lokal berjalan offline.

Komponen:
| Komponen | Jenis | Tugas |
|---|---|---|
| `CampaignTuning` | static/data | Semua angka dari dokumen ini |
| `CampaignRunState` | C# murni (ada) | State rute, segel, checkpoint, Power |
| `FactionDefinition`, `UnitRoleStats`, `EncounterComposer` | C# murni | Data organisasi + scaling |
| `CampaignDirector` | NetworkBehaviour, **server-only logic** | Menjalankan state, memicu encounter, membuka gerbang |
| `SectorEncounter` (Majelis/Biro/Garda) | NetworkBehaviour | Mekanik khas faksi |
| `PveEnemy` | turunan/konfigurasi bot yang ada | AI musuh berperan (Kroni/Guard/…) |
| `CampaignHud` | MonoBehaviour client | Menampilkan objective, segel, debuff, Power |

Aturan: logika keputusan hanya di server. HUD hanya membaca. Tidak ada `if (solo)` yang tersebar; jumlah pemain dibaca dari `NetworkManager.ConnectedClients`.

## 15. Yang sengaja DITUNDA

Komisi Suara, Konsorsium Modal, Menara Narasi, Modal, Koneksi, hubungan antar-faksi, event emergent, AI ally, arketipe selain Takhta Besi, cinematic pre-match, monetisasi, co-op matchmaking.

## 16. Kriteria "MVP selesai" (0.1.0)

- [ ] Solo dimulai dari menu tanpa membuat room PvP.
- [ ] Kursi terlihat dari spawn, tidak bisa disentuh sebelum Garda tumbang.
- [ ] Majelis dan Biro terasa berbeda (formasi vs prosedur).
- [ ] Runtuh → checkpoint, bukan awal peta.
- [ ] Bisa selesai dari spawn sampai MENANG dalam 8–15 menit.
- [ ] PvP 4v4 tetap bisa di-generate dan dimainkan identik.
- [ ] 30+ FPS di tablet owner (Infinix XPad 20) pada kualitas default.
