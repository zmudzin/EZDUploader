using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/Dokument/PobierzIdentyfikatoryDokumentowKoszulki")]
    public class PobierzIdentyfikatoryDokumentowKoszulkiRequest : IReturn<PobierzIdentyfikatoryDokumentowKoszulkiResponse>
    {
        public required int IdKoszulki { get; set; }
    }

    public class PobierzIdentyfikatoryDokumentowKoszulkiResponse
    {
        public List<long> dokumenty { get; set; } = new();
    }
}