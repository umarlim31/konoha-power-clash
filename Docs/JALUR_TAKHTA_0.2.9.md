# Jalur Takhta 0.2.9 — Tombol skill ikon bulat

Branch kerja: `feat/ikon-skill-0.2.9`, dibuat dari `feat/jalur-takhta-first-playable` setelah 0.2.8 di-merge (screenshot owner menunjukkan KAMERA DEKAT berjalan). PR ke `feat/jalur-takhta-first-playable`, bukan ke `main`.

Status: compile Unity, EditMode test di Unity, APK, dan gameplay **BELUM DIVERIFIKASI**. Yang sudah dicek di sini: pemeriksaan sintaks C#, `scripts/source-check.py`, dan pratinjau ikon (digambar ulang dengan Python dari rumus yang sama).

Permintaan owner: tulisan tombol seperti SERUAN IBU dan PERISAI RAKYAT diganti **ikon bulat berlogo**, contohnya kepala banteng untuk SERUAN IBU, dan tombol lain menyesuaikan. Pesan owner terpotong di kata "dan…", jadi lanjutannya menunggu.

**Keputusan IP:** kepala banteng (moncong putih) adalah lambang partai politik sungguhan, dan aturan proyek melarangnya. Sejak 0.2.3, SERUAN IBU memakai **Kerbau Rakyat**. Karena itu ikonnya adalah **kepala kerbau bertanduk lebar melengkung ke belakang**, yang bentuknya jelas berbeda dari banteng.

## Build manual Android/tablet (Unity Build Automation)
1. Branch **`feat/ikon-skill-0.2.9`**, Unity **6000.0.60f1**. Pre-export tetap **`Konoha.Editor.SpikeProject.prepare`**.
2. APK: versi **0.2.9**, kode Android **38**. Label bawah: `JALUR TAKHTA 0.2.9  •  SOLO PREVIEW`.

## Yang baru

### Tombol bulat bergaya MOBA
- **BASIC** berupa lingkaran besar di pojok kanan bawah. S1, S2, dan ULT melingkar di sekitarnya, sedangkan DODGE, LOMPAT, dan DUDUK berada di sebelah kiri.
- **Isi setiap tombol:**
  - pelat bulat berwarna hero;
  - ikon putih;
  - cincin emas;
  - nama kecil di bawahnya.
- **Saat cooldown:** muncul **sapuan gelap melingkar** yang menyusut, dan **sisa detik** tampil di tengah.
- **ULT:** sapuan mengikuti persentase PENGARUH, dan angkanya tampil di tengah (misalnya 63%).
- Saat tombol tidak bisa dipakai (cooldown, stun, Runtuh), ikonnya meredup.

### Ikon per hero (digambar dari kode, tanpa file gambar)
| Hero | S1 | S2 | ULT |
|---|---|---|---|
| MEGA | kepala **kerbau** (SERUAN IBU) | **perisai** berbintang (PERISAI RAKYAT) | kerbau dengan garis laju (MONCONG) |
| GEMOY | **lompatan** melengkung dan hantaman (CMD LEAP) | tiga **prajurit** (BARIS!) | **sayap** terbentang (GARUDA, burung mitologis) |
| ABAH | **balon bicara** (NARASI) | **petir** (ELECTRIC DASH) | **mikrofon** (PIDATO) |
| PAK WI | **kerucut lalu lintas** (INFRASTRUKTUR) | **jejak kaki** (BLUSUKAN) | **helm proyek** (PROYEK) |

Tombol umum: **kepalan** (BASIC), **panah ganda** (DODGE), **panah atas** (LOMPAT), **kursi** (DUDUK).

## Sengaja tidak diubah
- Fungsi tombol, cooldown, angka, dan kode tempur bersama. Tombol tetap tombol yang sama; hanya tampilannya yang diganti, dan teks lama disembunyikan lalu dibaca ulang untuk sapuan cooldown.
- Tombol kanan atas (LIHAT ARENA, KAMERA AWAL, SUARA, KAMERA) dan joystick.
- PvP.

## Checklist uji perangkat 0.2.9
1. Label `JALUR TAKHTA 0.2.9  •  SOLO PREVIEW`. Tombol aksi di kanan bawah kini berbentuk **lingkaran berikon**.
2. MEGA: S1 berikon kerbau, S2 berikon perisai. Tekan S1: sapuan gelap dan detik muncul, lalu habis.
3. ULT: angka persen naik saat memukul. Ketika 100%, ikon kerbau-laju menyala terang.
4. GANTI HERO ke GEMOY, ABAH, dan PAK WI: ikon berganti sesuai tabel.
5. Tombol mudah ditekan dengan jempol? Ada yang terlalu kecil atau saling menutupi?
6. Dekat Kursi: tombol DUDUK (ikon kursi) muncul dan tetap bisa ditekan.
7. Main sampai menang.

## Risiko
- Ukuran dan posisi tombol berubah, jadi jempol perlu menyesuaikan. Posisi atau ukurannya bisa disetel kalau terasa kurang pas.
- Ikon adalah siluet sederhana buatan kode. Kalau owner ingin ikon bergambar penuh, ikon PNG bisa diunggah dengan jalur slot yang sama seperti tekstur foto.
