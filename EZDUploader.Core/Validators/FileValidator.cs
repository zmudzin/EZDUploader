using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Validators
{
    public interface IFileValidator
    {
        bool ValidateFileName(string fileName);
        bool ValidateFileSize(string filePath);  // nowa metoda
        bool ValidateDocumentType(string documentType);
        string GetFileValidationError(string fileName);
        string GetFileSizeValidationError(string filePath); // nowa metoda
        string GetDocumentTypeValidationError();  // nowa metoda
    }

    public class FileValidator : IFileValidator
    {
        private const int MAX_FILE_SIZE_MB = 25;

        public bool ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var nazwaPliku = Path.GetFileNameWithoutExtension(fileName);
            return nazwaPliku.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2;
        }

        public bool ValidateFileSize(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length <= MAX_FILE_SIZE_MB * 1024 * 1024;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetFileValidationError(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Nazwa pliku nie może być pusta";

            var nazwaPliku = Path.GetFileNameWithoutExtension(fileName);
            if (nazwaPliku.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
                return "Nazwa pliku musi składać się z co najmniej dwóch wyrazów";

            return null;
        }

        public string GetFileSizeValidationError(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "Ścieżka pliku nie może być pusta";

            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > MAX_FILE_SIZE_MB * 1024 * 1024)
                    return $"Plik jest zbyt duży. Maksymalny rozmiar to {MAX_FILE_SIZE_MB}MB";

                return null;
            }
            catch (Exception ex)
            {
                return $"Błąd podczas sprawdzania rozmiaru pliku: {ex.Message}";
            }
        }
        public bool ValidateDocumentType(string documentType)
        {
            return !string.IsNullOrWhiteSpace(documentType);
        }

        public string GetDocumentTypeValidationError()
        {
            return "Wymagane jest wybranie rodzaju dokumentu";
        }
    }
}
