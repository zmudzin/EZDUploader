using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Models
{
    public class StanowiskoDto
    {
        public int Id { get; set; }
        public int? IdOrginal { get; set; }
        public int IdJednostki { get; set; }
        public int? IdJednostkiOrginal { get; set; }
        public string Nazwa { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime? DataUtworzenia { get; set; }
        public bool Ukryty { get; set; }
    }
}