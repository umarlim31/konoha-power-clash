# Jalur Takhta 0.3.0 — Tubuh membulat, jalan polos, suasana lebih alami

Branch kerja: `feat/tubuh-natural-0.3.0`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.2.9 di-merge (screenshot owner menunjukkan ikon bulat tampil). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C#, `scripts/source-check.py`, dan pratinjau siluet tubuh MEGA (digambar ulang dengan Python dari ukuran yang sama).

Permintaan owner:
- bentuk karakter dibuat lebih mirip manusia, terutama rok/celana yang masih kelihatan seperti balok;
- infrastruktur dibuat lebih alami, terasa seperti di Nusantara;
- motif batik di jalanan dihilangkan, jadi jalan tampil biasa seperti yang lain.

## Build manual Android/tablet (Unity Build Automation)
1. Branch **`feat/tubuh-natural-0.3.0`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.3.0**, kode Android **39**. Label bawah: `JALUR TAKHTA 0.3.0  •  SOLO PREVIEW`.

## Yang baru

### Tubuh hero dan musuh lebih membulat
- **Badan** kini berbentuk seperti tubuh: ada pinggang, dada, dan bahu yang melandai. Bentuk lamanya kotak.
- **Kain MEGA** kini berbentuk kain jarik: rapat di mata kaki, melebar di lutut dan pinggul, lalu mengecil di pinggang. Bentuk lamanya tabung lurus seperti balok. Kain bermotif **batik sogan** (cokelat-krem, garis miring). Motif batik hanya dipakai di pakaian.
- MEGA mendapat **ujung kebaya** yang jatuh menutupi pinggul.
- **Paha, betis, dan lengan** kini berbentuk kapsul. Bentuk lamanya tabung bersudut tajam.
- **Pinggul** dan **sepatu** berbentuk oval. Bentuk lamanya kotak.
- Wajah mendapat **dagu, mulut, dan alis melengkung**; mata, telinga, dan tangan sedikit diperhalus.
- Bahu sedikit dirapatkan supaya tidak terlihat seperti bantalan.
- **Pakaian hero:**
  - Rompi proyek PAK WI kini mengikuti bentuk badan.
  - Sabuk GEMOY dan sabuk musuh berbentuk melingkar.
  - Kerah koko ABAH dibuat membulat.
- **Musuh** (Majelis, Biro, Garda) memakai pembentuk tubuh yang sama, jadi ikut membulat.
- Figur **kader** pada efek skill juga kini berlengan dan berbadan bulat.

### Jalan polos tanpa motif
- **Ubin motif biru-emas dihapus** dari boulevard Gerbang Rakyat dan pelataran Istana. Keduanya kini memakai **batu paving yang sama** dengan plaza, sedikit lebih gelap supaya jalurnya tetap terbaca.
- **Hiasan padma** (kelopak perunggu dan titik terakota) di lantai plaza dihapus. Yang tersisa hanya cakram batu dan satu cincin perunggu.

### Suasana lebih alami
- **Lampu taman** berbentuk bola di bawah tudung kubah (tampak seperti jamur) diganti **lentera Jawa**: kotak kaca hangat dengan empat tiang kayu dan atap limas kecil.
- **Pohon ketapang** ditambahkan di tepi jalan samping kiri dan kanan, 6 pohon di antara tiang PJU. Daunnya berlapis mendatar, beberapa sudah memerah, seperti pohon peneduh di jalan dan halaman sekolah. Pohon-pohon ini berada di luar rute, tanpa collider, dan otomatis memudar bila menutupi kamera.

## Sengaja tidak diubah
- Rute, collider, posisi musuh, angka tempur, ikon tombol, kamera JAUH/DEKAT, dan gamelan.
- Sendi dan animasi tubuh. Hanya bentuk bagian tubuh yang diganti, jadi gerakan lari, pukul, terhuyung, dan roboh tetap sama.
- PvP.
- Garis sambungan batu dan list perunggu di jalur utama tetap ada. Keduanya garis polos, bukan motif.

## Checklist uji perangkat 0.3.0
1. Label `JALUR TAKHTA 0.3.0  •  SOLO PREVIEW`.
2. Pilih MEGA dan pakai **KAMERA: DEKAT**. Dari samping dan belakang, kain harus tampak **mengecil di mata kaki dan membulat di pinggul**, bukan balok. Kain juga harus bermotif batik cokelat.
3. Lihat badan, lengan, dan kaki hero lain (GANTI HERO). Seharusnya tidak ada lagi sudut kotak di badan, sepatu, atau pinggul.
4. Lawan musuh Majelis, Biro, dan Garda. Tubuh mereka juga membulat, dan kilatan saat terkena pukulan masih muncul.
5. Boulevard Gerbang Rakyat dan pelataran depan Istana harus **batu polos tanpa motif biru-emas**. Lantai plaza tengah juga tanpa kelopak.
6. Lampu taman di sepanjang boulevard harus berbentuk **lentera kotak beratap**.
7. Pakai LIHAT ARENA dan lihat sisi kiri-kanan: pohon ketapang berlapis di tepi jalan samping.
8. Main sampai menang. Cek tidak ada kedip-kedip baru dan tidak ada patah-patah.

## Risiko
- Saat MEGA berlari lebar, kaki bisa sedikit menembus kain. Jika terlihat mengganggu, kain bisa dibuat lebih lebar atau langkah MEGA diperpendek.
- Bagian bulat memakai sedikit lebih banyak segitiga daripada kotak. Jumlah objeknya sama, jadi dampaknya ke performa seharusnya kecil.
- Sambungan bulat pada mesh badan dan kain bisa menampakkan satu garis tipis di punggung pada cahaya tertentu.
