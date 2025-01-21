using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Models
{
    public class RejestrujSpraweResponse
    {
        public required int IdSprawy { get; set; }
        public required DateTime DataRejestracjiSprawy { get; set; }
        public required int IdTeczki { get; set; }
        public required string SymbolTeczki { get; set; }
        public required string KategoriaArchiwalna { get; set; }
        public required string TypProwadzenia { get; set; }
    }
}