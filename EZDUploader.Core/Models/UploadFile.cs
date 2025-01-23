using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Models
{
    public class UploadFile
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime AddedDate { get; set; }
        public UploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public double Progress { get; set; }
        public int? IdKoszulki { get; set; }
    }

    public enum UploadStatus
    {
        Pending,
        Uploading,
        Completed,
        Failed
    }
}
