using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/Teczka/PobierzSprawy")]
    public class PobierzSprawyTeczkiRequest : IReturn<PismoDto[]>
    {
        public required string TeczkaSymbol { get; set; }
        public required int Rok { get; set; }
    }
}