using System;

namespace Inventory.Domain.Entities
{
    public class ChangeLog
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangeType { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string ChangedBy { get; set; }
    }
}