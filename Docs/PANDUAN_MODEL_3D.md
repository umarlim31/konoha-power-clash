# Panduan model 3D bangunan & properti — dari tablet

Sejak versi 0.3.1, pohon, lampu, kendaraan parkir, warung, rumah, dan gedung di Jalur Takhta bisa diganti dengan **model 3D buatan orang lain** (file FBX atau OBJ). Semuanya diunggah lewat GitHub web dari tablet.

Selama folder masih kosong, game tetap memakai bentuk buatan kode, jadi tidak ada yang rusak. Begitu file ada di folder yang benar, build berikutnya otomatis memakai model tersebut di **semua** tempat untuk slot itu.

## Yang tetap aman
- **Rute dan tabrakan tidak berubah.** Model hanya mengganti tampilan. Dinding dan batang yang tadinya bisa ditabrak tetap bisa ditabrak di posisi yang sama.
- Model otomatis **diperkecil atau diperbesar** supaya masuk ke kotak ukuran slot, berdiri di tanah, dan berada di tengah titiknya.
- Bahan (material) model diubah otomatis ke shader game, jadi model tidak tampil **pink**. Daun yang memakai gambar transparan dibuat tembus pandang dengan benar.
- Tekstur di folder model otomatis dibatasi maksimal 1024 piksel supaya ringan di tablet.
- Model yang **terlalu berat** (segitiganya melebihi batas slot) atau gagal dibaca **tidak dipakai**, dan bentuk kode tetap tampil.
- Di layar game, di atas label versi, muncul baris kecil **`MODEL 3D: ...`**. Isinya slot mana yang terpasang (dan berapa buah), serta slot mana yang **TIDAK DIPAKAI** beserta alasannya. Baris ini tidak muncul kalau belum ada model sama sekali.

## Daftar slot

| Folder (slot) | Mengganti | Ukuran kotak (lebar × tinggi × panjang, meter) | Batas segitiga per model | Jumlah di peta |
|---|---|---|---|---|
| `Lentera` | lentera taman di boulevard & plaza | 0,5 × 2,3 × 0,5 (termasuk tiang) | 2.000 | ±22 |
| `PohonPalem` | pohon kelapa/palem | 4,5 × (tinggi asli + 0,9) × 4,5 | 3.000 | ±40 |
| `PohonKetapang` | pohon ketapang di tepi jalan samping | 6 × 6,4 × 6 | 12.000 | 6 |
| `PohonFlamboyan` | flamboyan di boulevard | 5,5 × 5,2 × 5,5 | 12.000 | 6 |
| `PohonTrembesi` | trembesi besar | 11 × 7,6 × 11 | 20.000 | 4 |
| `PohonPisang` | pohon pisang di kampung | 3 × 3,4 × 3 | 4.000 | ±10 |
| `Becak` | becak di mulut boulevard | 1,2 × 1,9 × 2,3 | 10.000 | 2 |
| `MotorBebek` | motor parkir di depan warung | 0,7 × 1,2 × 1,4 | 8.000 | 3 |
| `GerobakBakso` | gerobak bakso + payung | 2,2 × 3,1 × 2,2 | 10.000 | 1 |
| `WarungKopi` | warung kopi | 4,6 × 3,3 × 4 | 20.000 | 1 |
| `LampuPJU` | lampu jalan (tiang + lengan) | 0,6 × 7,6 × 2,2 | 2.000 | ±22 |
| `GapuraKampung` | gapura gang kampung | 4,5 × 4,5 × 0,8 | 10.000 | 2 |
| `RumahKampung` | rumah kampung di luar jalan samping | 7 × 5,3 × 6 | 5.000 | 22 |
| `Ruko` | ruko dua lantai di seberang jalan raya | 7,8 × 7,7 × 8,4 | 6.000 | 16 |
| `GedungLama` | gedung lama berjendela krepyak | 4,4 × (tinggi asli + 2,2) × 4,4 | 15.000 | 6 |
| `Pendopo` | pendopo di taman sayap | 5,4 × 5,3 × 5,4 | 20.000 | 2 |

Istana, Gerbang Rakyat, Gerbang Dalam, monumen, menara, dan Kursi **sengaja tidak punya slot**. Bangunan-bangunan itu bagian dari rute, dan bentuknya menentukan tempat berjalan.

**Kenapa batas segitiga kecil?** Satu model dipakai berkali-kali. Contohnya, 22 rumah × 5.000 segitiga = 110.000 segitiga hanya untuk rumah. Cari model berlabel **low poly**. Di Sketchfab, jumlah segitiga tertera di halaman model ("Triangles").

## Dari mana mencari model
- **Quaternius** (quaternius.com) dan **Kenney** (kenney.nl): model low poly berlisensi **CC0**, bebas dipakai termasuk untuk komersial, dan ringan. Paling cocok untuk pohon, lampu, dan kendaraan.
- **Poly Haven** (polyhaven.com/models): CC0, lebih realistis, tetapi sebagian modelnya berat. Periksa jumlah segitiganya dulu.
- **Sketchfab** (sketchfab.com): pilih filter *Downloadable*. Lisensi **CC BY** boleh dipakai asal nama pembuatnya dicantumkan; tulis kreditnya di `atur.txt` (lihat bawah), nanti aku pindahkan ke layar kredit. **Jangan** pakai model berlisensi *NonCommercial* atau *NoDerivatives* kalau game ini mau dijual.
- **Aturan satire proyek tetap berlaku:** jangan memakai model gedung nyata yang ditiru persis, lambang negara resmi, atau logo partai/lembaga nyata.

**Format:**
- **FBX** paling disarankan. **OBJ** juga bisa.
- GLB/GLTF dan file Blender (.blend) **belum** bisa dipakai.
- Di Sketchfab, pilih unduhan **"FBX"** (Autoconverted). Hasilnya file ZIP berisi model dan folder `textures`. Ekstrak dulu ZIP-nya di tablet (aplikasi Files → tekan lama → Extract).

## Cara mengunggah lewat GitHub web (tablet)

Unggah ke branch **`feat/jalur-takhta-first-playable`**, sama seperti tekstur foto.

Contoh untuk slot `Lentera`:

1. Buka `github.com/umarlim31/konoha-power-clash`, pindah ke branch `feat/jalur-takhta-first-playable`, lalu masuk ke folder `Assets/Konoha`.
2. **Add file → Create new file.** Di kolom nama, ketik `Art/Models/Lentera/README.md`. Setiap garis miring otomatis membuat folder. Isi dengan satu kalimat sumber model, misalnya `Lentera dari Quaternius (CC0)`, lalu **Commit changes**.
3. Buka folder `Assets/Konoha/Art/Models/Lentera` → **Add file → Upload files**. Pilih **file model** (`.fbx` atau `.obj`, plus `.mtl` kalau ada) dan **semua file teksturnya** (`.png`/`.jpg`) dari folder `textures` sekaligus, lalu **Commit changes**.

**Ketentuan file:**
- **Nama model:** sebaiknya diganti menjadi `<Slot>.fbx` (misalnya `Lentera.fbx`). Kalau di folder itu **hanya ada satu** file model, namanya bebas.
- Nama folder harus **persis** seperti di tabel (huruf besar-kecil berpengaruh): `Lentera`, `PohonPalem`, `PohonKetapang`, dan seterusnya.
- Maksimal **25 MB per file**. Model low poly biasanya di bawah 5 MB.
- **Tidak harus sekaligus.** Mulailah dari 1–2 slot, build, dan lihat hasilnya.

## Menyetel ukuran dan arah: file `atur.txt` (opsional)

Kalau model terlalu kecil, terlalu besar, menghadap ke arah yang salah, atau melayang, buat file `atur.txt` di folder slot itu (**Add file → Create new file**, nama `Art/Models/<Slot>/atur.txt`). Isinya satu pengaturan per baris:

```
skala=1.2
putar=180
naik=0.1
ukuran=asli
kredit=Lentera oleh NamaPembuat (CC BY 4.0)
```

| Baris | Arti |
|---|---|
| `skala=1.2` | 20% lebih besar setelah dipaskan ke kotak (`0.8` = 20% lebih kecil). |
| `putar=180` | Putar model (derajat). Pakai `180` kalau bagian depan model menghadap ke belakang, atau `90` / `-90` kalau menyamping. |
| `naik=0.1` | Naikkan 10 cm. Nilai negatif untuk menurunkan model yang melayang atau pondasinya terlalu tinggi. |
| `ukuran=asli` | Jangan dipaskan ke kotak; pakai ukuran asli dari file (1 unit = 1 meter). |
| `kredit=...` | Catatan kredit pembuat. Tidak memengaruhi game. |

Koma desimal juga boleh (`skala=1,2`).

## Setelah mengunggah
Beri tahu aku slot mana yang sudah diunggah. Aku akan menggabungkannya ke branch kerja, lalu kamu build seperti biasa (pre-export tetap `Konoha.Editor.SpikeProject.prepare`). Di dalam game:
1. Baca baris `MODEL 3D: ...` di atas label versi. Pastikan slotmu tertulis dengan jumlahnya, misalnya `Lentera x22`.
2. Kalau tertulis **TIDAK DIPAKAI**, alasannya ikut tertulis:
   - `terlalu berat` → cari model yang lebih ringan.
   - `belum ter-import` → biasanya format file tidak didukung atau file rusak.
   - `gagal dipasang` → kirim screenshot ke aku.
3. Datangi tempatnya dan cek ukuran, arah, dan warnanya. Kalau perlu, perbaiki lewat `atur.txt`.
