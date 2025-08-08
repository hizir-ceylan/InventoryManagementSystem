using System;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities
{
    /// <summary>
    /// Sistem güncellemeleri entity'si
    /// Windows ve Office güncellemelerini takip eder
    /// </summary>
    public class SystemUpdate
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Güncellemenin ait olduğu cihaz ID'si
        /// </summary>
        public Guid DeviceId { get; set; }

        /// <summary>
        /// Güncelleme türü (Windows, Office, .NET vb.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string UpdateType { get; set; } = string.Empty;

        /// <summary>
        /// Güncelleme başlığı
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Güncelleme açıklaması
        /// </summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// KB numarası (Knowledge Base)
        /// </summary>
        [MaxLength(20)]
        public string? KBNumber { get; set; }

        /// <summary>
        /// Mevcut yüklü sürüm
        /// </summary>
        [MaxLength(100)]
        public string? CurrentVersion { get; set; }

        /// <summary>
        /// Güncel sürüm
        /// </summary>
        [MaxLength(100)]
        public string? LatestVersion { get; set; }

        /// <summary>
        /// Güncelleme boyutu (MB)
        /// </summary>
        public double? SizeInMB { get; set; }

        /// <summary>
        /// Güncelleme durumu
        /// </summary>
        public UpdateStatus Status { get; set; }

        /// <summary>
        /// Güncelleme öncelik seviyesi
        /// </summary>
        public UpdatePriority Priority { get; set; }

        /// <summary>
        /// Güncelleme tespit edilme tarihi
        /// </summary>
        public DateTime DetectedDate { get; set; }

        /// <summary>
        /// Son kontrol tarihi
        /// </summary>
        public DateTime LastChecked { get; set; }

        /// <summary>
        /// Güncelleme yayın tarihi
        /// </summary>
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// Güncelleme otomatik olarak yüklenebilir mi
        /// </summary>
        public bool CanAutoInstall { get; set; }

        /// <summary>
        /// Yeniden başlatma gerekli mi
        /// </summary>
        public bool RequiresRestart { get; set; }

        /// <summary>
        /// Microsoft güvenlik bülteni ID'si
        /// </summary>
        [MaxLength(50)]
        public string? SecurityBulletinId { get; set; }

        /// <summary>
        /// Güncellemenin ait olduğu cihaz
        /// </summary>
        public virtual Device? Device { get; set; }

        /// <summary>
        /// Kayıt oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Son güncelleme tarihi
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Güncelleme durumu enum'u
    /// </summary>
    public enum UpdateStatus
    {
        /// <summary>Güncelleme mevcut, yüklenmemiş</summary>
        Available = 0,
        
        /// <summary>Güncelleme indirildi, yükleme bekliyor</summary>
        Downloaded = 1,
        
        /// <summary>Güncelleme yüklendi</summary>
        Installed = 2,
        
        /// <summary>Güncelleme başarısız</summary>
        Failed = 3,
        
        /// <summary>Güncelleme gizlendi/atlandı</summary>
        Hidden = 4,
        
        /// <summary>Güncelleme yükleme bekliyor</summary>
        PendingInstall = 5,
        
        /// <summary>Yeniden başlatma bekliyor</summary>
        PendingRestart = 6
    }

    /// <summary>
    /// Güncelleme öncelik seviyesi
    /// </summary>
    public enum UpdatePriority
    {
        /// <summary>Düşük öncelik</summary>
        Low = 0,
        
        /// <summary>Normal öncelik</summary>
        Normal = 1,
        
        /// <summary>Yüksek öncelik</summary>
        High = 2,
        
        /// <summary>Kritik güncelleme</summary>
        Critical = 3,
        
        /// <summary>Güvenlik güncellemesi</summary>
        Security = 4
    }
}