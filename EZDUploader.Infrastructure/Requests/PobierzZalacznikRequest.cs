using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/Zalacznik/PobierzZalacznik")]
    public class PobierzZalacznikRequest : IReturn<PobierzZalacznikResponse>
    {
        public required int IdZalacznia { get; set; }
    }

    public class PobierzZalacznikResponse
    {
        public required byte[] zalacznik { get; set; }
        public ZalacznikDto zalacznikDto { get; set; }
        public string Nazwa { get; set; }
    }

    public class ZalacznikDto
    {
        public int ContentId { get; set; }
        public string Nazwa { get; set; }
    }
}