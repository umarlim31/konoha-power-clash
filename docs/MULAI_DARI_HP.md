# Memulai KONOHA 0.0.1 dari HP atau tablet

**Paket ini berisi source proyek. APK belum tersedia.**

Kode joystick, kamera, karakter sementara, arena, HUD dan otomasi build sudah ditulis. Unity belum mengimpor atau mengompilasinya, sehingga error pertama dari Unity masih mungkin perlu diperbaiki. Jangan menyebutnya Android PASS.

## Yang dibutuhkan berikutnya

1. **Repository GitHub khusus KONOHA.** Simpan source dan riwayat perubahan di sana.
2. **Komputer cloud/remote dengan Unity berlisensi dan Android Build Support.** HP/tablet menjadi perangkat untuk mengendalikan workflow dan mencoba APK. Komputer pribadi Windows tidak dibutuhkan.
3. **Hubungan runner build dengan repository.** Workflow yang disiapkan menggunakan runner Linux di komputer cloud tadi. Tanpa runner tersebut, tombol Run workflow akan menunggu dan tidak menghasilkan APK.

Repository dan mesin cloud belum dibuat/dihubungkan dalam sesi ini. Tidak ada langganan, biaya cloud, atau akun yang diaktifkan otomatis.

## Isi paket

- `source/`: source proyek Unity dan dokumen teknis.
- `repository.bundle`: riwayat Git yang dapat dipulihkan di komputer cloud.
- `DELIVERY.json`: identitas commit yang disertakan.

Jika source diimpor tanpa bundle, buat commit awal sebelum build. Skrip menolak source yang belum disimpan dalam commit agar setiap APK memiliki identitas yang jelas.

## Setelah runner tersedia

1. Buka GitHub dari browser HP/tablet.
2. Buka repository KONOHA -> Actions -> KONOHA Android 0.0.1.
3. Pilih Run workflow.
4. Jika gagal, unduh artifact log; gunakan pesan error Unity yang sebenarnya untuk perbaikan.
5. Jika berhasil, unduh artifact APK, ekstrak, lalu instal `KONOHA_0.0.1.apk` pada Android uji.
6. Buka game, coba joystick, tabrakan dinding, kamera, dan tombol DEBUG.
7. Rekam pengujian dan catat identitas BUILD/COMMIT serta tipe perangkat sesuai `DEVICE_GATE.md`.

Panduan konfigurasi teknis runner ada di `CLOUD_BUILD.md`.

## Titik berhenti

Tetap di 0.0.1 sampai APK terbukti berjalan pada Android nyata. Build 0.0.2 baru dimulai setelah gate tersebut lolos dan review menyetujui kelanjutan. Unity masih ENGINE CANDIDATE LOCK.
