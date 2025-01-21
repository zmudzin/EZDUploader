using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Models
{
    public class WskazanieDokumentuDto
    {
        public required int Identyfikator { get; set; }
        public string? IdentyfikatorDokumentu { get; set; }
    }
}
