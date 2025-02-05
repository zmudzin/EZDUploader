using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Models
{
    public class DokumentSystemowyTypeDto
    {
        public WskazanieDokumentuDto Identyfikator { get; set; }
        public string Rodzaj { get; set; }
        public string DataDokumentu { get; set; }
        public string Tytul { get; set; }
        public string Sygnatura { get; set; }  // Znak pisma
        public bool? MetaBrakDaty { get; set; }
        public bool? MetaBrakZnaku { get; set; }
        public string Nazwa { get; set; }
        public bool Metadane { get; set; } = true;
    }
}
