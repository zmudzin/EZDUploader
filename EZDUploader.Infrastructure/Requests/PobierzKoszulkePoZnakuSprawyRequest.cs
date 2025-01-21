using ServiceStack;
using EZDUploader.Core.Models;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/Koszulka/PobierzPoZnakuSprawy")]
    public class PobierzKoszulkePoZnakuSprawyRequest : IReturn<PobierzKoszulkePoZnakuSprawyResponse>
    {
        public required string Znak { get; set; }
    }

    public class PobierzKoszulkePoZnakuSprawyResponse
    {
        public required PismoDto Pismo { get; set; }
    }
}