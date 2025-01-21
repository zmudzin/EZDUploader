using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/Koszulka/PrzekazKoszulkeReq")]
    public class PrzekazKoszulkeReq : IReturn<PrzekazKoszulkeRes>
    {
        public required int IdKoszulki { get; set; }
        public required int IdPracownikaDocelowego { get; set; }
        public required int IdPracownikaZrodlowego { get; set; }
        public int IdStanowiskaDocelowego { get; set; }
        public int IdStanowiskaZrodlowego { get; set; }
    }

    public class PrzekazKoszulkeRes
    {
        public required int IdEtapPisma { get; set; }
    }
}