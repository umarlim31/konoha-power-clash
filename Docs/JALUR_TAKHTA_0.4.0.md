# Jalur Takhta 0.4.0 — Musim Pemilu tahap 1: LAWAN atau RANGKUL

Branch kerja: `feat/musim-pemilu-0.4.0`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.3.4 di-merge. Owner menguji 0.3.4: "orangnya sudah jelas, sudah ada matanya". PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Design authority: `Docs/GAME_LOGIC_JALUR_TAKHTA_v2.md` (§3, §4, §6, §7).

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C# dan `scripts/source-check.py`. Ada 8 test EditMode baru di `CampaignPolitikTests`, tetapi test ini baru benar-benar jalan di Unity.

## Build manual Android/tablet (Unity Build Automation)
1. Branch **`feat/musim-pemilu-0.4.0`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.4.0**, kode Android **44**. Label bawah: `JALUR TAKHTA 0.4.0  •  SOLO PREVIEW`.

## Yang baru
### Resource politik (baris status atas)
Baris status berubah dari `SEGEL | KUASA | RUNTUH` menjadi `SEGEL x/2 | MODAL | JATAH | RESTU`. KUASA dihapus dari baris ini karena DUDUK langsung menang.

| Resource | Awal | Naik | Turun |
|---|---|---|---|
| **MODAL** | 60 | +15 "sumbangan relawan" setelah Gerbang Rakyat; +2 setiap musuh yang **ditumbangkan** (yang menyerah tidak dihitung) | dipakai untuk RANGKUL |
| **JATAH** | 0 | +1 setiap RANGKUL; +3 kalau pakai pinjaman | — (ditagih di versi Krisis 0.4.3) |
| **RESTU** | 50 | +10 setelah Gerbang Rakyat; +15 setiap lembaga yang dilawan | −5 setiap RANGKUL; −10 lagi kalau pakai pinjaman |

Lingkaran emas RESTU RAKYAT di bawah hero tetap ada. Tulisan "• RESTU" di baris hero dihapus supaya tidak tertukar dengan angka RESTU.

### Panel LAWAN / RANGKUL
Saat hero mendekati **Majelis Daun** atau **Biro Prosedur** (±19 m dari aula, sebelum anggotanya menyerang), muncul panel di kiri layar:
- **LAWAN:** panel ditutup, lalu masuk aula dan bertarung seperti biasa. Hasilnya Segel, Pengaruh +25, dan Restu +15.
- **RANGKUL:** bayar Modal, dan Segel **langsung** keluar tanpa bertarung. Semua anggota lembaga berlutut lalu pergi. Pesan satir muncul: "Rapat tertutup pukul 02.00, palu diketok" atau "Jalur khusus dibuka".
  - Harga: **Majelis 45 Modal**, **Biro 35 Modal**.
  - Kalau Modal kurang, tombol berubah menjadi **PINJAM KONSORSIUM**: Segel tetap keluar, Modal habis, Jatah +3, dan Restu −15.
- Kalau panel diabaikan dan hero langsung masuk aula (±9 m), pilihannya otomatis LAWAN.
- RANGKUL masih bisa ditekan selama hero belum masuk aula, walaupun anggota sudah mulai menyerang.

### Garda Takhta "dikondisikan"
Setiap lembaga yang dirangkul mengurangi Wibawa Panglima 15% (dirangkul dua: −30%). Pesannya: *Koalisi sudah "mengkondisikan" PANGLIMA.* Ini pengganti sederhana untuk sekutu bertarung, yang belum aman dibuat di versi ini.

### KORAN KONOHA (layar hasil baru)
Layar menang sekarang berupa halaman depan koran berwarna krem. Isinya:
- judul besar dan sub-judul, sesuai ending;
- 5–7 berita kecil yang dirangkai dari cara bermain: Majelis atau Biro dilawan atau dirangkul, survei RESTU, berapa kali tumbang, sisa Modal, dan satu sindiran khas per hero;
- arketipe di bagian bawah, serta baris statistik (waktu, runtuh, pengaruh, modal, jatah, restu).

| Ending | Cara mendapatkan (0.4.0) | Judul |
|---|---|---|
| **TAKHTA BESI** | Lawan keduanya | "MEGA REBUT KURSI TANPA KOALISI" |
| **RAJA KOALISI** | Rangkul minimal satu, Jatah < 3 | "MEGA DILANTIK, KABINET GEMUK MENANTI" |
| **BONEKA SISTEM** | Jatah ≥ 3 (misalnya rangkul dengan pinjaman) | "MEGA BERKUASA, TAPI SIAPA BOSNYA?" |

### Disclaimer
Di menu awal: *"Karya satir fiksi. Tokoh, lembaga, dan peristiwa Negara Konoha adalah rekaan."*

## Sengaja tidak diubah
- Combat, hero, angka musuh, mekanik Blok Majelis, loket Biro, Lockdown Garda, kamera, model 3D, dan PvP.
- DUDUK tetap langsung menang. Fase Memerintah dan Krisis baru di 0.4.3.
- Kartu Kebijakan (0.4.1), Kampanye Suara (0.4.2), dan Sidang Kilat (0.4.4) belum ada.

## Checklist uji perangkat 0.4.0
Mainkan **3 run** dan kirim screenshot koran di akhir setiap run:
1. **Run A, lawan semua.** Abaikan panel atau tekan LAWAN di kedua lembaga. Koran harus berjudul **TAKHTA BESI**, dan RESTU naik.
2. **Run B, rangkul satu.** Setelah Gerbang Rakyat, cek MODAL (75 plus musuh yang ditumbangkan). Rangkul **Biro** (35), lalu lawan Majelis. Panel harus muncul sebelum anggota Biro menyerang, dan semua anggota Biro harus berlutut lalu hilang. Di Garda muncul pesan "dikondisikan". Koran harus berjudul **RAJA KOALISI**.
3. **Run C, rangkul semua.** Rangkul Majelis (45), lalu Biro. Kalau Modal kurang, tombol berubah menjadi **PINJAM KONSORSIUM**. Koran harus berjudul **BONEKA SISTEM**, dengan sub-judul soal Konsorsium.
4. Pastikan panel tidak menutupi joystick atau tombol skill, dan teks koran terbaca serta tidak terpotong.
5. ULANG dan GANTI HERO masih bekerja, dan MODAL/JATAH/RESTU kembali ke angka awal.

## Risiko
- Jarak panel (19 m) ditebak dari tata letak. Kalau anggota lembaga sudah menyerang sebelum panel muncul, jaraknya akan dinaikkan.
- Tanpa Fase Memerintah, Jatah belum ditagih. Akibat jangka panjangnya baru terasa di 0.4.3.
- Teks koran cukup panjang. Kalau terpotong di layar tablet, ukuran huruf atau jumlah berita akan dikurangi.
