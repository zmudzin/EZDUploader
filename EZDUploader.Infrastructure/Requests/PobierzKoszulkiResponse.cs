using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EZDUploader.Core.Models;

namespace EZDUploader.Infrastructure.Requests
{
    public class PobierzKoszulkiResponse : ResponseBaseDto
    {
        public List<PismoDto> Pisma { get; set; }
        public string ResultMessage { get; set; }
    }
}
