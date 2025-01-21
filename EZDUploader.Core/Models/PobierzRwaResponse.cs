using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZDUploader.Core.Models
{
    public class PobierzRwaResponse : ResponseBaseDto
    {
        public TeczkaRwaDto Teczki { get; set; }
    }

    public class ResponseBaseDto
    {
        public int ResultStatus { get; set; }
        public string ResultMessage { get; set; }
        public int CID { get; set; }
        public string Token { get; set; }
        public string Odpowiedz { get; set; }
        public string TypOdpowiedzi { get; set; }
    }
}
