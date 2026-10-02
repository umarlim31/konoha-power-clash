# 0.6.1 — KARIER "Hidup di Kampung": misi panjang, jajan, sewa kendaraan, borgol & POLSEK

Branch kerja: `feat/0.6.1-hidup-di-kampung`, dibuat dari `feat/0.6.0-warga-biasa`. Branch 0.6.0 sudah dimainkan di tablet, tetapi belum di-merge. PR kembali ke `feat/jalur-takhta-first-playable`, **bukan** ke `main`. PR ini sudah berisi 0.6.0.

Arah produk: `Docs/VISI_KONOHA_HIDUP_v3.md`.

Status: compile Unity, EditMode test, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini:
- pemeriksaan sintaks C#;
- `scripts/source-check.py`;
- review independen terhadap seluruh perubahan. Tidak ada error compile yang ditemukan. Satu test yang akan gagal dan beberapa bug kecil yang ditemukan review sudah diperbaiki.

Ada 4 test logika baru (polisi tetap datang, sel + tebus, jajan & sewa, rantai misi), dan test scene ditambah untuk POLSEK, sewa, jajan, dan kamera interior.

## Build manual Android/tablet (Unity Build Automation)
1. Branch **`feat/0.6.1-hidup-di-kampung`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.6.1**, kode Android **47**. Label bawah: `JALUR TAKHTA 0.6.1  •  SOLO PREVIEW`.

## Masukan owner (0.6.0)
- Nama di atas kepala masih "MEGA • YOU", padahal seharusnya nama karakter buatan sendiri.
- Lingkungan harus lebih hidup dan nyata, termasuk warga yang merekam kejadian.
- Semua yang ada di dunia ingin bisa dicoba: naik motor atau sepeda, beli siomay atau salome. Sambil menunggu preman, harus ada yang bisa dikerjakan.
- Saat masuk pondok atau rumah, atap jangan hilang. Kamera seharusnya masuk ke dalam.
- Tidak boleh ada karakter yang menembus tembok.
- Polisi datang untuk preman pertama, tetapi tidak untuk preman berikutnya.
- Kalau tidak menyogok, pemain harus benar-benar diborgol lalu dibawa polisi, bukan sekadar "keesokan harinya…".
- Karakter butuh misi yang panjang.

## Yang baru

### 1. Nama sendiri di atas kepala
Label di atas hero KARIER sekarang menampilkan nama buatanmu, misalnya **UMAR**. MODE PRESIDEN dan PvP tetap memakai "HERO • YOU".
- Saat pembuat karakter terbuka, tombol skill hero (Seruan Ibu, Perisai Rakyat, Moncong, GANTI HERO) dan tulisan "MENYIAPKAN JALUR TAKHTA..." tidak tampil lagi. Panelnya juga dibuat lebih pekat.

### 2. Polisi selalu datang (terlambat, seperti biasa)
- Polisi muncul untuk preman kedua dan seterusnya juga.
- Kenapa dulu tidak muncul: di 0.6.0 polisi hanya dipanggil kalau perkelahian berlangsung cukup lama. Preman kedua kalah cepat, jadi polisi tidak dipanggil.
- Aturan baru: begitu warga berteriak, ada yang menelepon polisi, dan polisi **pasti datang ±14 detik kemudian**, walaupun perkelahian sudah selesai. Satirnya: polisi datang setelah semuanya beres. Kerumunan menunggu sampai polisi datang.
- Pilihan baru **SAKSI WARGA**: muncul kalau kamu yang mengalahkan preman **dan** RESTU-mu minimal 40. Warga membelamu ("Dia yang ngusir preman, Pak!"), kamu bebas, dan RESTU +3. Kalau RESTU rendah, warga diam saja.

### 3. IKUT KE POLSEK = benar-benar ditangkap
1. Polisi berjalan ke arahmu, lalu **KLIK, borgol terpasang**: tangan hero terikat di belakang.
2. Kamu **digiring berjalan** ke motor patroli.
3. Kamu **dibonceng motor patroli** dengan sirene, menyusuri boulevard ke **POLSEK KONOHA**.
4. POLSEK adalah gedung baru di sisi timur gang: dinding padat, pintu menghadap boulevard, meja petugas, poster "PELAYANAN CEPAT\* (\*syarat & ketentuan berlaku)", dan **sel berjeruji**.
5. Kamu digiring masuk ke sel, lalu **pintu jeruji tertutup**.
6. Panel sel menampilkan **hitung mundur 25 detik**, atau tombol **TEBUS** (Rp 100rb + catatan hitam, "tanpa kuitansi").
7. Jeruji terbuka, lalu kamu berjalan keluar sendiri.
8. Polisi pergi naik motornya.

### 4. Misi panjang (9 misi, menuju Ketua RT)
Panel kiri sekarang menampilkan **MISI x/9** dan progresnya. Setiap misi selesai memberi pesan, suara, dan hadiah kecil, lalu misi berikutnya dibuka dengan pesan bergaya WA.

| No | Misi | Isi |
|---|---|---|
| 1 | KERJA PERTAMA | 1 antar ojol atau 1 set kuli (+Rp 20rb). Pesan dari ibu kos: kontrakan nunggak. |
| 2 | KENALAN TETANGGA | Sapa 3 kelompok warga |
| 3 | JAJAN DULU | Beli siomay, salome, bakso, atau makan di warkop |
| 4 | COBA KENDARAAN | Sewa sepeda atau motor |
| 5 | PAHLAWAN GANG | Usir preman |
| 6 | TAWARAN GELAP | WA dari nomor asing: "sebar pesan ini, Rp 250rb". **TERIMA** (duit + catatan hitam) atau **TOLAK** (blokir & lapor RT, RESTU +6) |
| 7 | TABUNGAN | Kumpulkan Rp 500rb |
| 8 | RESTU WARGA | RESTU 50 |
| 9 | DAFTAR CALON RT | Rp 1jt + daftar di pos ronda |

Panah kecil ikut menunjuk tujuan misi: kelompok yang belum disapa, gerobak siomay, dan sewa sepeda. Simpanan 0.6.0 tetap bisa dilanjutkan.

### 5. Jajan & kendaraan, semua bisa dicoba
- **Gerobak SIOMAY** dan **SALOME** di kiri-kanan boulevard menuju plaza, lengkap dengan penjualnya. **BAKSO** di gerobak bakso dekat spawn. **MAKAN** di warkop.
  - Tombol aksi menampilkan harga.
  - Hero **makan dengan piring di tangan**: tangan naik-turun ke mulut.
  - Kalau energi penuh, muncul "masih kenyang".
- **SEWA SEPEDA** (Rp 5rb, rak sepeda di dekat gerbang): hero **mengayuh** dan jalan lebih cepat.
- **SEWA MOTOR** (Rp 15rb, di pangkalan ojol): naik motor merah, jalan lebih cepat lagi.
- Tombol **TURUN** mengembalikan kendaraan. Mengambil kerja atau rebahan otomatis mengembalikan kendaraan.

### 6. Kamera masuk ke dalam bangunan
Di bawah atap (pendopo, warung, pos ronda, rumah, POLSEK), atap **tidak lagi hilang**. Kamera pindah ke dalam: dekat, rendah, dan di bawah atap. Saat kamu keluar, kamera kembali seperti semula. Ini berlaku di KARIER dan MODE PRESIDEN.

### 7. Tidak ada lagi yang menembus tembok
- Warga yang datang ke keributan, petugas polisi, dan motor patroli sekarang **mengecek tembok dan benda padat**.
- Warga hanya datang dari arah yang punya jalan lurus bebas ke tempat kejadian. Mereka berbelok menghindari halangan, dan berhenti kalau memang tidak ada jalan.
- Motor polisi berhenti sebelum menabrak, lalu petugas berjalan sisanya.
- Catatan: hiasan yang memang tidak punya collider (misalnya warung dan pondok dekorasi) bisa dilewati semua orang, termasuk hero. Jadi aturannya sama untuk semua.

### 8. Lingkungan lebih hidup
- **Perekam lebih nyata:** HP dipegang dua tangan setinggi mata, **layar HP menyala**, sedikit goyang. Muncul juga komentar di atas kepala: "REKAM! REKAM!", "VIRALIN!", "UPLOAD KE GRUP!", "NO VIRAL NO JUSTICE", "LIVE IG DULU"...
- **Obrolan warga:** sesekali warga di dekatmu berkomentar, misalnya "Cabe naik lagi, Bu!", "Pinjol nelpon terus...", "Nanti malam ronda, jangan lupa"...

## Sengaja tidak diubah
- Combat, hero, angka musuh, MODE PRESIDEN (selain kamera interior), dan PvP. Label nama di PvP tidak berubah.
- Fitur 0.0.8.2.
- Folder Art.
- Tubuh karakter masih bentuk dasar dari kode. Slot model warga 3D direncanakan di 0.6.3.
- Belum ada siklus pagi–malam, kerja gorengan/konten/parkir, dan pemilihan RT. Siklus hari dan kerja tambahan bergeser ke 0.6.2; pemilihan RT ke 0.6.3. Lihat `Docs/PROMPTS_CLAUDE_CODE.md`.

## Checklist uji perangkat 0.6.1
1. Label 0.6.1. Pilih KARIER: **tidak ada** tombol skill hero di belakang panel karakter. Setelah MULAI HIDUP, **nama buatanmu tampil di atas kepala**.
2. Panel kiri menampilkan **MISI 1/9 KERJA PERTAMA**. Selesaikan satu ojol atau kuli: muncul "MISI SELESAI", lalu misi 2.
3. Ikuti misi berikutnya: sapa warga, beli **SIOMAY** (lihat piring dan gerakan makan), **SEWA SEPEDA** (lihat gerakan mengayuh), lalu tekan **TURUN**. Coba juga **SEWA MOTOR** di pangkalan ojol, **SALOME**, dan **BAKSO**.
4. **Preman pertama** dan **preman kedua**: kalahkan dengan cepat, lalu tunggu. Polisi **harus tetap datang ±14 detik** setelah warga teriak, **dua-duanya**.
5. Saat polisi datang dan RESTU ≥ 40 setelah kamu mengalahkan preman, harus ada tombol **SAKSI WARGA**.
6. Pilih **IKUT KE POLSEK**: polisi berjalan ke arahmu → borgol (tangan di belakang) → digiring ke motor → dibonceng → masuk gedung POLSEK → masuk sel → jeruji tertutup → hitung mundur, atau TEBUS → jeruji terbuka → keluar. Cek juga **kamera masuk ke dalam POLSEK**.
7. **Misi 6 TAWARAN GELAP**: panel WA dengan TERIMA atau TOLAK.
8. Masuk ke pendopo atau pos ronda: atap tetap terlihat dan kamera masuk ke dalam.
9. Perhatikan warga yang datang ke keributan dan petugas polisi: **tidak menembus tembok POLSEK, gedung, atau menara**. Perhatikan juga layar HP yang menyala dan komentar warga.
10. Tutup aplikasi, buka lagi, lalu LANJUTKAN HIDUP: misi dan uang tetap.

## Risiko
- Versi besar, jadi risiko error compile tetap ada. Kalau build gagal, kirim potongan log error-nya.
- Lokasi POLSEK (sisi timur gang, sekitar x 18–24, z −28 sampai −34) dipilih dari analisis kode tanpa melihat peta. Kalau gedungnya bertabrakan dengan hiasan lain, kirim screenshot.
- Kamera interior juga aktif di bawah tajuk pohon dari model 3D, karena namanya tidak bisa dibedakan dari atap model. Kamera akan mendekat sebentar di bawah pohon besar.
- Selama dibonceng ke POLSEK, motor patroli melaju lurus dan bisa melintas dekat pohon atau tiang (tidak menabrak, hanya terlihat mepet).
- Angka harga, waktu sel, dan jeda polisi masih angka awal.
