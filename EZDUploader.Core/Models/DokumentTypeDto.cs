using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Models
{
    public class DokumentTypeDto
    {
        public required WskazanieDokumentuDto Identyfikator { get; set; }
        public required DateTime DataUtworzenia { get; set; }
        public required string Nazwa { get; set; }
        public string? Sygnatura { get; set; }
        public string? Tytul { get; set; }
        public string? DataDokumentu { get; set; }
        public string? Rodzaj { get; set; }
    }
}
