# MemGuard

MemGuard, Windows sistemlerde RAM kullanımını izlemek, gereksiz bellek yükünü azaltmak ve genel sistem akıcılığını artırmak için geliştirilmiş modern bir masaüstü uygulamasıdır.

Bu proje, Intech Yazılım tarafından geliştirilmektedir.

Web sitesi: [www.intechyazilim.com.tr](https://www.intechyazilim.com.tr)

## Öne Çıkan Özellikler

- Anlık RAM kullanımını takip etme
- Tek tıkla bellek optimizasyonu
- Çalışan süreçleri görüntüleme ve yönetme
- Windows başlangıç uygulamalarını yönetme
- Otomatik optimize etme eşiği ve aralık ayarlama
- Tema desteği
- Türkçe, İngilizce, İspanyolca ve Fransızca dil desteği
- Sistem tepsisi entegrasyonu

## Teknolojiler

- .NET 8
- WPF
- Windows Forms `NotifyIcon`
- PowerShell tabanlı kurulum yardımcıları
- Inno Setup

## Proje Yapısı

- `Views/` : Uygulama ekranları
- `ViewModels/` : MVVM mantığı
- `Services/` : bellek, süreç, tema, ayar ve başlangıç servisleri
- `Controls/` : özel grafik ve UI bileşenleri
- `Localization/` : çoklu dil desteği
- `Styles/` : tema ve kontrol stilleri
- `installer/` : kurulum scriptleri ve kurulum markalama dosyaları
- `Assets/` : logo, ikon ve görsel varlıklar

## Geliştirme

Projeyi açmak için:

```powershell
dotnet build
```

Yayın almak için:

```powershell
dotnet publish MemGuard.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Kurulum dosyası üretmek için:

```powershell
powershell -ExecutionPolicy Bypass -File installer\Build-Installer.ps1
```

## Katkı

Geri bildirimler, hata raporları ve katkılar memnuniyetle karşılanır.

## Geliştiriciler

- Göktuğ Muhammed Ali Tilki
- Efe Özcan

## Yayıncı

Intech Yazılım
