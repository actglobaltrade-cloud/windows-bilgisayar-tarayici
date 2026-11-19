# Güvenli Windows Asistanı

Bu klasör, önceki `WindowsAssistant` uygulamasından ayrı olan isteğe bağlı bir Windows Forms uygulamasının kaynak kodunu içerir. Önceki uygulamanın dosyaları veya ayarları bu projeye dahil edilmez ya da değiştirilmez.

## Gizlilik ve ağ kullanımı

- Tarama özeti yalnızca Windows adı/sürümü/build/mimarisi; donanım üreticisi/modeli ve RAM; işlemci çekirdek ve mantıksal işlemci sayıları; yerel disklerin toplam kapasitesi/boş alanı; grafik bağdaştırıcısı adları; birincil ekran çözünürlüğü; saat dilimi ve geçerli bölge dilini içerir.
- Bilgisayar/kullanıcı adları, seri numaraları, IP/DNS adresleri, dosya yolları, başlangıç komutları ve kurulu uygulama listeleri sorgulanmaz.
- Toplanan özet gönderimden önce uygulamada gösterilir. Gönderim için hem onay kutusunun işaretlenmesi hem de ikinci onay penceresinin kabul edilmesi gerekir.
- Onay verilirse özet, HTTPS üzerinden Google Gemini API'ye gönderilir. API anahtarı uygulama açılırken istenir, API isteğinin başlığında kullanılır ve diske kaydedilmez. Google'ın API koşulları ve veri işleme kuralları geçerlidir.
- Rapor otomatik kaydedilmez; kullanıcı isterse bir metin dosyasına kaydedebilir. API veya tarama hataları uygulamada gösterilir.
- Uygulama PowerShell çalıştırmaz, yürütme ilkesini değiştirmez, sistem ayarlarını otomatik değiştirmez ve yönetici izni istemez. Bir güvenlik tarayıcısı değildir.

## Derleme ve kurulum

Windows'ta .NET Framework 4.x C# derleyicisi ve Windows Forms/Management bileşenleri gerekir. `Build-GuvenliWindowsAsistani.bat` dosyasını çalıştırmak `GuvenliWindowsAsistani.exe` uygulamasını derler ve bunu içeren `GuvenliWindowsAsistani-Setup.exe` kurulum paketini oluşturur. Üretilen EXE dosyaları kaynak deposunda tutulmaz.

Kurulum, uygulamayı kullanıcı profilindeki `Programs\GuvenliWindowsAsistani` klasörüne kurar ve Başlat menüsüne kısayol ekler. Masaüstü kısayolu isteğe bağlıdır. Yönetici izni istemez.

Derlenen dosyalar kod imzasızdır; Windows SmartScreen uyarı gösterebilir. Dağıtımdan önce ikili dosyaları inceleyin ve güvenilir bir kod imzalama sertifikasıyla imzalamayı değerlendirin. Bu proje için henüz bir açık kaynak lisansı seçilmemiştir; lisans eklenmeden yeniden dağıtım koşulları varsayılmamalıdır.
