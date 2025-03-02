﻿using EZDUploader.Core.Models;

namespace EZDUploader.Core.Interfaces
{
    public interface IFileUploadService
    {
        IReadOnlyList<UploadFile> Files { get; }
        Task AddFiles(string[] filePaths);
        Task RemoveFiles(IEnumerable<UploadFile> files);
        Task UploadFiles(IEnumerable<UploadFile> files, IProgress<(int fileIndex, int totalFiles, int progress)> progress = null);
        void CancelUpload();
        void ClearFiles();
    }
}