using System.Collections.Generic;

namespace Inventory.Agent.Windows.Models
{
    public class DetailedDiff
    {
        public Dictionary<string, FieldDiff> Diff { get; set; } = new Dictionary<string, FieldDiff>();
    }

    public class FieldDiff
    {
        public List<string> Removed { get; set; } = new List<string>();
        public List<string> Added { get; set; } = new List<string>();
        public List<object> ChangedValues { get; set; } = new List<object>();
    }

    public class ChangedValue
    {
        public string Field { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    public class ChangedObjectValue
    {
        public string Identifier { get; set; }
        public List<ChangedValue> Changes { get; set; } = new List<ChangedValue>();
    }
}