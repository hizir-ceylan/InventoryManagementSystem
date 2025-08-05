using Inventory.Domain.Entities;
using Inventory.Shared.Helpers;

namespace Inventory.Api.DTOs
{
    public class ChangeLogDto
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangeDateFormatted { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;

        public static ChangeLogDto FromEntity(ChangeLog changeLog)
        {
            return new ChangeLogDto
            {
                Id = changeLog.Id,
                DeviceId = changeLog.DeviceId,
                ChangeDate = changeLog.ChangeDate,
                ChangeDateFormatted = TimezoneHelper.FormatInTurkeyTime(changeLog.ChangeDate),
                ChangeType = changeLog.ChangeType ?? "-",
                OldValue = changeLog.OldValue ?? "-",
                NewValue = changeLog.NewValue ?? "-",
                ChangedBy = changeLog.ChangedBy ?? "Sistem"
            };
        }
    }

    public class DeviceStatusDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime? LastSeen { get; set; }
        public string LastSeenFormatted { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool AgentInstalled { get; set; }
        public string ManagementType { get; set; } = string.Empty;

        public static DeviceStatusDto FromEntity(Device device)
        {
            return new DeviceStatusDto
            {
                Id = device.Id,
                Name = device.Name ?? "Bilinmeyen",
                IpAddress = device.IpAddress ?? "-",
                DeviceType = device.DeviceType.ToString(),
                Model = device.Model ?? "-",
                Location = device.Location ?? "-",
                LastSeen = device.LastSeen,
                LastSeenFormatted = device.LastSeen.HasValue 
                    ? TimezoneHelper.FormatInTurkeyTime(device.LastSeen.Value)
                    : "Hiç görülmedi",
                Status = TimezoneHelper.IsActiveInLast12Hours(device.LastSeen) 
                    ? "Çalışıyor" 
                    : "Kapalı",
                IsActive = TimezoneHelper.IsActiveInLast12Hours(device.LastSeen),
                AgentInstalled = device.AgentInstalled,
                ManagementType = device.ManagementType.ToString()
            };
        }
    }
}