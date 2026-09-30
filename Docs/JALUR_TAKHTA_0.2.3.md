# Jalur Takhta 0.2.3 — Tubuh manusia & efek serangan

Branch kerja: `feat/tubuh-efek-0.2.3`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.2.2 (Kota Hidup) di-merge berdasarkan hasil uji di tablet. PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# (parser tree-sitter C#), `scripts/source-check.py`, dan review kode kedua.

Permintaan owner: tubuh hero dan musuh berbentuk manusia, efek pukulan yang terasa, Kerbau Rakyat untuk SERUAN IBU, dan setiap skill punya efek, dengan perubahan yang cukup signifikan.

## Build manual Android/tablet (Unity Build Automation)

1. Pilih branch **`feat/tubuh-efek-0.2.3`**, Unity **6000.0.60f1**. Clean Build tidak perlu.
2. Pre-export method tetap **`Konoha.Editor.SpikeProject.prepare`**.
3. APK: versi **0.2.3**, kode Android **32**. Label bawah: `JALUR TAKHTA 0.2.3  •  SOLO PREVIEW`.
4. Kalau muncul "paket tidak valid": hapus dulu aplikasi lama, lalu install lagi.

## Yang baru

### 1. Hero bertubuh manusia
Kapsul diganti tubuh utuh: kepala dengan wajah sederhana (mata, alis, hidung, telinga), rambut, badan, lengan dengan siku, kaki dengan lutut, dan sepatu. Tingginya ±1,9 m.

| Hero | Pakaian |
|---|---|
| MEGA | kebaya merah, kain batik panjang, sanggul, selendang, bros emas |
| GEMOY | kemeja safari krem berkantong, peci, sabuk emas, badan tegap-gempal |
| ABAH | jas hijau tua, kerah koko putih, syal hijau, peci, rambut beruban |
| PAK WI | kemeja putih lengan pendek, celana hitam, rompi proyek oranye dengan pita reflektor |

- Wajah sederhana dan fiktif, tidak meniru wajah tokoh asli (aturan IP).
- **Gerakan tubuh:** bernapas saat diam; berlari dengan ayunan kaki, lutut, dan lengan sesuai kecepatan; memukul bergantian tangan kiri-kanan (ancang-ancang, pukul, tarik); terhuyung ke belakang saat kena; pose khusus saat skill; pose melompat; dan **roboh telentang** saat Runtuh.
- Kalau nanti owner mengunggah model 3D asli (FBX), model itu yang dipakai dan tubuh buatan ini tidak dipasang.

### 2. Musuh bertubuh manusia
- **Pakaian:** jas, dasi, dan sabuk berwarna faksi.
- **Penutup kepala per faksi:** peci (Majelis Daun), topi dinas (Biro Prosedur), baret (Garda Takhta).
- Ukuran tubuh tetap sesuai peran (Ketua dan Panglima lebih besar).
- **Ketua Majelis** memegang palu di tangan kanan, yang diangkat saat KETOK PALU.
- Musuh berlari mengejar, memukul saat menyerang, terhuyung saat kena, berlutut saat menyerah, dan roboh saat tumbang.

### 3. Efek pukulan yang terasa
Setiap pukulan yang kena menghasilkan:
- **percikan** dan **kilat terang** di titik kena;
- **tubuh berkedip putih** (biru jika ditahan perisai);
- **jeda sesaat** (hit-stop): gerakan penyerang dan yang dipukul tertahan sepersekian detik;
- **layar bergetar**: kuat saat hero terkena, ringan saat hero memukul.

Ada juga **busur ayunan** berwarna di depan penyerang: warna khas tiap hero, dan warna faksi untuk musuh.

### 4. Efek khas setiap skill

| Hero | Skill | Efek |
|---|---|---|
| MEGA | SERUAN IBU | **Kerbau Rakyat** (bertanduk lebar, mata merah) menyeruduk di jalur dorongan, dengan debu, gelombang kejut, getar layar, dan suara lenguhan |
| MEGA | PERISAI RAKYAT | 2 **Kader** berbaju merah muncul dari tanah membawa perisai tinggi, selama penghalang aktif (6 detik) |
| MEGA | MONCONG (ULT) | **3 Kerbau Rakyat** menyerbu bersama sejauh 12 m, getar besar |
| GEMOY | CMD LEAP | Hero **melompat melengkung** lalu mendarat dengan hantaman: cincin emas, debu, dan percikan |
| GEMOY | BARIS! | 5 **prajurit** muncul berbaris lalu maju, dengan kipas cahaya emas di depan |
| GEMOY | GARUDA (ULT) | **Sayap emas** terbentang di punggung hero selama 8 detik (burung mitologis, bukan lambang negara) |
| ABAH | NARASI | Gelombang suara berdenyut di area, dan kata-kata melayang: GAGASAN, DATA, DIALOG… |
| ABAH | ELECTRIC DASH | **Petir** menyambar sepanjang lintasan dash, dengan suara geledek |
| ABAH | PIDATO (ULT) | **Podium dan mikrofon** muncul, 3 gelombang suara hingga 7 m |
| PAK WI | INFRASTRUKTUR | **Kerucut lalu lintas** dan papan "PROYEK STRATEGIS KONOHA" di sepanjang jalan |
| PAK WI | BLUSUKAN | Jejak debu sepanjang dash |
| PAK WI | PROYEK (ULT) | Debu konstruksi, kerucut, dan tulisan PROYEK NASIONAL di setiap bangunan proyek |

- Semua dash (SERUAN IBU, ELECTRIC DASH, BLUSUKAN) kini tampak meluncur, bukan berpindah tiba-tiba.
- Nama skill muncul melayang di atas hero.
- Kotak datar berwarna di tanah dari versi prototipe tidak dipakai lagi di mode solo.
- Suara baru: lenguhan kerbau, petir, desir dash, dan dentuman ULT.

## Sengaja tidak diubah

- **Angka dan aturan tempur:** damage, jangkauan, cooldown, dan efek skill. Semua perubahan hanya tampilan.
- **PvP 4v4 tetap sama.**
  - Prefab hero bersama tidak diubah; tubuh manusia dipasang saat game berjalan, hanya di mode solo.
  - Kode tempur bersama hanya mendapat dua "kait" presentasi yang tidak dipakai di PvP, dan satu saklar efek lama yang otomatis kembali NYALA saat pindah ke PvP.
- Kamera, kontrol, dan semua fitur 0.0.8.2. Getar layar hanya memutar pandangan sedikit dan tidak menggeser posisi kamera, jadi orbit, zoom, dan KAMERA AWAL tidak terpengaruh.
- Kota Hidup 0.2.2.

## Checklist uji perangkat 0.2.3

1. Label `JALUR TAKHTA 0.2.3  •  SOLO PREVIEW`.
2. Pilih **MEGA** → MULAI: hero berbentuk manusia berkebaya merah. Jalan dan lari: kaki dan tangan berayun.
3. Pukul Kroni di Gerbang Rakyat:
   - hero memukul bergantian tangan;
   - ada busur merah, percikan, kilat, dan musuh berkedip putih lalu terhuyung;
   - layar bergetar sedikit.
4. Biarkan Kroni memukulmu: mereka juga memukul, dan layar bergetar lebih kuat.
5. **SERUAN IBU:** Kerbau Rakyat menyeruduk, dan hero meluncur di belakangnya.
6. **PERISAI RAKYAT:** 2 Kader berperisai muncul, lalu tenggelam setelah ±6 detik.
7. Isi ULT sampai 100% → **MONCONG:** 3 kerbau menyerbu.
8. Kalahkan musuh: mereka roboh telentang. Menyerah: mereka berlutut.
9. Ulangi (ULANG / GANTI HERO) dengan **GEMOY, ABAH, PAK WI** dan coba tiap skill sesuai tabel di atas. Perhatikan lompatan CMD LEAP, petir ELECTRIC DASH, dan kerucut INFRASTRUKTUR.
10. Majelis: Ketua memegang palu dan KETOK PALU tetap berfungsi. Biro: pegawai bertopi dinas. Garda: pasukan berbaret.
11. Runtuh: hero roboh telentang, lalu bangkit normal di checkpoint.
12. Main sampai DUDUK di Kursi dan MENANG.
13. Tutup aplikasi → buka → **REBUT KURSI (PvP):** harus tampil seperti dulu (kapsul, efek lama).
14. **FPS saat pertarungan ramai** (Majelis, Garda + serangan balik): terasa lebih berat dari 0.2.2 atau tidak?

## Risiko

- **Ini perubahan tampilan terbesar sejauh ini** dan belum di-compile Unity. Kalau build gagal, kirim log errornya.
- **Performa:**
  - Setiap tubuh terdiri dari ±30 bagian (kapsul lama hanya 1–6). Saat 10+ musuh hadir, jumlah objek yang digambar naik.
  - Efek memakai bahan tanpa pencahayaan dan kumpulan partikel yang dipakai ulang supaya ringan.
  - Kalau berat, jumlah bagian tubuh musuh bisa dikurangi (misalnya tanpa alis dan telinga).
- **Lompatan dan dash tampak** adalah perkiraan visual. Posisi sebenarnya tetap diatur kode tempur, jadi di jarak pendek yang terhalang dinding tubuh bisa terlihat meluncur lebih pendek.
- Kerbau dan tubuh masih dari bentuk dasar (bola, silinder, kotak). Versi realistis menunggu model 3D di tahap aset.

## Berikutnya

- Setelah hasil uji 0.2.3: penyesuaian (kekuatan getar, ukuran efek, performa).
- **0.3.0:** jalur aset realistis (model manusia, animasi, mobil, dan kerbau yang diunggah owner) dengan panduan unggah dari tablet.
