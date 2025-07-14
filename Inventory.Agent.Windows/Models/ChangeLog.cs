using System;

namespace Inventory.Agent.Windows.Models
{
    public class ChangeLogDto
    {
        public Guid Id { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangeType { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string ChangedBy { get; set; }
    }
}