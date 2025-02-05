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
        string GetFileValidationError(string fileName);
    }

    public class FileValidator : IFileValidator
    {
        public bool ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var nazwaPliku = Path.GetFileNameWithoutExtension(fileName);
            return nazwaPliku.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2;
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
    }
}
