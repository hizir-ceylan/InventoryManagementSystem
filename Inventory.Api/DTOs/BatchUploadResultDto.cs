using System.Collections.Generic;

namespace Inventory.Api.DTOs
{
    public class BatchUploadResultDto
    {
        public int TotalDevices { get; set; }
        public int SuccessfulUploads { get; set; }
        public int FailedUploads { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}