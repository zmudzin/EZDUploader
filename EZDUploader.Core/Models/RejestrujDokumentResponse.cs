using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Models
{
    public class RejestrujDokumentResponse
    {
        public long IdDokumentu { get; set; }
        public long IdZawartosci { get; set; }
        public int ResultStatus { get; set; }
        public string ResultMessage { get; set; }
        public int CID { get; set; }
    }
}

