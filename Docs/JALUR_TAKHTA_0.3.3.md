# Jalur Takhta 0.3.3 — Model 3D benar-benar tampil

Branch kerja: `feat/model-tampil-0.3.3`, dibuat dari `feat/poles-0.3.2`, jadi perubahan 0.3.2 ikut masuk. PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`.

## Masalah yang dilaporkan owner
"Kenapa hasilnya makin sepi?" Screenshot 0.3.2 memperlihatkan kondisi berikut:
- Ibu kota tanpa pohon palem, flamboyan, ketapang, trembesi, pisang, lentera, rumah kampung, ruko, dan kota pesisir.
- Baris `MODEL 3D: 16/16 slot terpasang (197 objek)` tetap tampil.

Screenshot LIHAT ARENA dari 0.3.1 (Uji B) ternyata sudah sama sepinya. Saat itu terlewat dari pemeriksaanku.

## Penyebab
- Di 0.3.1, semua bagian satu model digabung jadi satu mesh. Setelah digabung, format indeks mesh diganti dari 32-bit ke 16-bit. Penggantian ini menghapus daftar segitiganya.
- Akibatnya mesh punya titik dan ukuran, sehingga lolos pengecekan dan terhitung "terpasang", tetapi tidak menggambar apa pun.
- Bentuk versi kode sudah dihapus untuk memberi tempat bagi model. Hasilnya kota menjadi kosong.

## Perbaikan
- **Mesh gabungan kini ditulis langsung.** Format indeks dipilih sebelum data diisi, lalu titik, normal, UV, dan segitiga per bahan diisi tanpa konversi belakangan. Bagian yang tercermin (skala negatif) dibalik arah segitiganya supaya tidak terbalik.
- **Pengaman:**
  - Kalau mesh gabungan kosong, tidak bisa dibaca, atau jumlah segitiganya setelah disimpan tidak sama dengan yang dihitung, model **tidak dipakai**.
  - Bentuk kode **tetap tampil**, dan slot itu tertulis di baris `TIDAK DIPAKAI` beserta alasannya.
  - Kota tidak akan pernah kosong lagi karena kesalahan ini.
- **Mesh disimpan sebagai aset baru** setiap kali generate, tidak ditimpa di atas mesh lama.
- **Baris `MODEL 3D` kini juga menulis jumlah segitiga yang tergambar**, misalnya `16/16 slot terpasang (197 objek, 175rb segitiga)`. Kalau angkanya 0, berarti ada yang salah.

## Sengaja tidak diubah
- Model dan `atur.txt`, rute, collider, combat, kamera, dan PvP.
- Perubahan 0.3.2 (kota pesisir memakai model, baris ringkas) tetap ada.
- Tulisan papan (misalnya PLAZA ASPIRASI dan MAJELIS DAUN) memang terbaca terbalik kalau dilihat dari belakang. Ini perilaku lama, bukan bagian dari perbaikan ini.

## Checklist uji perangkat 0.3.3
1. Pastikan label `JALUR TAKHTA 0.3.3  •  SOLO PREVIEW` dan baris `MODEL 3D: 16/16 slot terpasang (... objek, ...rb segitiga)`. Angka segitiga harus lebih dari 0.
2. Buka LIHAT ARENA. Bandingkan dengan screenshot 0.3.2 yang sepi. Sekarang harus terlihat:
   - pohon palem di boulevard dan taman;
   - flamboyan dan ketapang;
   - trembesi besar di sudut;
   - deretan rumah kampung di luar jalan samping;
   - ruko di seberang jalan raya;
   - rumah dan ruko di tepi teluk.
3. Pakai KAMERA DEKAT dan lihat dari dekat lentera, becak, gerobak bakso, warung kopi, gapura, dan pendopo. Warnanya tidak boleh pink atau putih polos, dan model harus berdiri di tanah.
4. Kalau ada tulisan `TIDAK DIPAKAI`, kirim screenshot baris itu. Bentuk versi kode untuk slot tersebut seharusnya tetap tampil.
5. Main sampai menang dan pastikan tetap lancar.

## Risiko
- Kalau model sekarang tampil, beban gambar naik dibanding build "sepi" 0.3.1/0.3.2. Itu sebabnya FPS 0.3.1/0.3.2 terasa lancar, padahal modelnya tidak tergambar. Totalnya kira-kira 175 ribu segitiga; separuhnya dari ±38 pohon palem. Angka itu masih wajar untuk tablet, tetapi perlu dicek. Kalau FPS turun, palem adalah yang pertama dirampingkan.
