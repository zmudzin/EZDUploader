using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EZDUploader.Core.Models;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/Rwa/PobierzRwaPoRoczniku")]
    public class PobierzRwaPoRocznikuRequest : IReturn<PobierzRwaPoRocznikuResponse>
    {
        public int Rocznik { get; set; }
    }

    public class PobierzRwaPoRocznikuResponse
    {
        public TeczkaRwaDto Teczki { get; set; }
    }
} 
