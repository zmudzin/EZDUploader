using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack;

namespace EZDUploader.Infrastructure.Requests
{
    [Route("/Pracownik/DodajPracownika")]
    public class DodajPracownikaRequest : IReturn<DodajPracownikaResponse>
    {
        public string Login { get; set; }
        public string Haslo { get; set; }
        public string Imie { get; set; }
        public string Nazwisko { get; set; }
        public string Inicjaly { get; set; }
        public string Stanowisko { get; set; }
        public int IdJednostki { get; set; }
    }

    public class DodajPracownikaResponse
    {
        public int IdPracownika { get; set; }
        public int IdStanowiska { get; set; }
    }
}
