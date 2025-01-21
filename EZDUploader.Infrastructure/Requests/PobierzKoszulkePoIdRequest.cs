using EZDUploader.Core.Models;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/Koszulka/PoId")]
    public class PobierzKoszulkePoIdRequest : IReturn<PobierzKoszulkePoIdResponse>
    {
        public required int Id { get; set; }
    }

    public class PobierzKoszulkePoIdResponse
    {
        public required PismoDto Pismo { get; set; }
    }
}
