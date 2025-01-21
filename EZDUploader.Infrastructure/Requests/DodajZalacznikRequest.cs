using EZDUploader.Infrastructure.Requests;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/Zalacznik/DodajZalcznik")]
    public class DodajZalacznikRequest : IReturn<DodajZalacznikResponse>
    {
        public required byte[] Dane { get; set; }
        public required string Nazwa { get; set; }
        public required int IdPracownikaWlasciciela { get; set; }
    }

    public class DodajZalacznikResponse
    {
        public int ContentId { get; set; }
        public string Nazwa { get; set; } = string.Empty;
    }
}