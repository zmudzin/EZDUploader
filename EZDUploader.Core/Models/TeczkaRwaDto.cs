using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Models
{
    public class TeczkaRwaDto
    {
        public int ID { get; set; }
        public List<TeczkaRwaDto> TeczkiPodrzedne { get; set; } = new List<TeczkaRwaDto>();
        public int Rok { get; set; }
        public string Symbol { get; set; }
        public string Nazwa { get; set; }
        public string KategoriaArchiwalna { get; set; }
        public string TypProwadzenia { get; set; }
        public int IloscWyjatkow { get; set; }
        public bool UdostepnienieWyjatek { get; set; }
        public int NumerLP { get; set; }
        public int? LP { get; set; }
        public bool Nieaktywny { get; set; }
        public int? TerminDni { get; set; }
        public int? IdJednostki { get; set; }
    }
}
