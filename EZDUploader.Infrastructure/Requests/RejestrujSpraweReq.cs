using EZDUploader.Infrastructure.Requests;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/RejestrSpraw/RejestrujSprawe")]
    public class RejestrujSpraweReq : IReturn<RejestrujSpraweRes>
    {
        public required string TeczkaSymbol { get; set; }
        public required int IdKoszulki { get; set; }
        public required int IdPracownikaWlasciciela { get; set; }
        public string Uwagi { get; set; } = string.Empty;
    }

    public class RejestrujSpraweRes
    {
        public int IdSprawy { get; set; }
        public DateTime DataRejestracjiSprawy { get; set; }
        public int IdTeczki { get; set; }
        public string SymbolTeczki { get; set; }
        public string KategoriaArchiwalna { get; set; }
        public string TypProwadzenia { get; set; }
    }
}