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
        public string DocumentType { get; set; } // Rodzaj dokumentu
        public UploadStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public string SignatureNumber { get; set; }
        public string NumerPisma { get; set; }
        public bool BrakDaty { get; set; } = true;  // domyślnie true
        public bool BrakZnaku { get; set; } = true; // domyślnie true
    }

    public enum UploadStatus
    {
        Pending,
        Uploading,
        Completed,
        Failed
    }
}
