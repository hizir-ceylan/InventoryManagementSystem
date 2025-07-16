# InventoryManagementSystem

Kurumsal cihaz envanteri yönetimi, değişiklik takibi ve raporlaması için geliştirilen bir sistemdir.

## Özellikler

- Cihaz ekleme, listeleme, güncelleme ve silme
- Donanım & yazılım bilgilerini toplama
- Kullanıcı ve lokasyon yönetimi
- Değişikliklerin otomatik takibi ve raporlanması
- API ile farklı uygulamalardan entegrasyon imkanı
- Kolay kurulum ve dağıtım desteği

## Hızlı Başlangıç

1. **Gereksinimler:**  
   - .NET 8 SDK
   - SQL Server (veya uygun connection string ile desteklenen diğer veritabanları)
   - İsteğe bağlı: Docker

2. **Projeyi Çalıştırma:**
   ```bash
   git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
   cd InventoryManagementSystem/Inventory.Api
   dotnet run
   ```
   Varsayılan olarak API `http://localhost:5093` adresinde çalışır.

3. **API Dokümantasyonu:**  
   Swagger arayüzü ile endpointleri inceleyebilirsiniz:  
   `http://localhost:5093/swagger`

## Örnek API Çağrıları

Cihaz ekleme:
```http
POST /api/devices
Content-Type: application/json

{
  "name": "Ofis Laptopu",
  "macAddress": "AA:BB:CC:DD:EE:FF",
  "ipAddress": "192.168.1.100",
  "deviceType": "Laptop"
}
```

Cihazları listeleme:
```http
GET /api/devices
```

## Katkıda Bulunmak

Projeye katkı sağlamak için:
- Fork’layın ve yeni bir branch oluşturun.
- Değişikliklerinizi ekleyin ve test edin.
- Pull request açın.

Daha fazla teknik detay için:  
➡️ [Detaylı teknik dokümantasyon](docs/technical-documentation.md)

## Lisans

MIT lisansı ile açık kaynak olarak sunulmaktadır.

---

Her türlü soru ve öneriniz için lütfen [issue açın](https://github.com/hizir-ceylan/InventoryManagementSystem/issues) veya iletişime geçin.
