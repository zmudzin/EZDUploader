// UtworzKoszulkeReq.cs
using EZDUploader.Core.Models;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{


    [Route("/Koszulka/UtworzKoszulkeReq")]

    public class UtworzKoszulkeReq : IReturn<UtworzKoszulkeRes>
    {
        public required string Nazwa { get; set; }
        public required int IdPracownikaWlasciciela { get; set; }
        public int IdStanowiskaWlasciciela { get; set; }
    }

    public class UtworzKoszulkeRes
    {
        public int IdKoszulki { get; set; }
    }
}