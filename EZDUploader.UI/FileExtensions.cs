using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EZDUploader.Core.Models;

namespace EZDUploader.UI
{
    public static class FileExtensions
    {
        public static IEnumerable<UploadFile> ToUploadFiles(string[] paths)
        {
            return paths.Select(path => new UploadFile
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                FileSize = new FileInfo(path).Length,
                AddedDate = DateTime.Now,
                Status = UploadStatus.Pending
            });
        }
    }
}
