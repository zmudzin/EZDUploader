using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Models;

namespace EZDUploader.Core.Interfaces
{
    public interface IEzdApiService
    {
        ApiSettings Settings { get; }
        bool IsAuthenticated { get; }
        int? CurrentUserId { get; }

        void SetupTokenAuth(string token);
        Task<bool> LoginAsync(string username, string password);

        // Operacje na koszulkach/sprawach
        Task<PismoDto> UtworzKoszulke(string nazwa, int idPracownika);
        Task<PismoDto> PobierzKoszulkePoId(int id);
        Task<PismoDto> PobierzKoszulkePoZnakuSprawy(string znak);

        // Operacje na załącznikach
        Task<int> DodajZalacznik(byte[] dane, string nazwa, int idPracownika);

        // Operacje na sprawach
        Task<RejestrujSpraweResponse> RejestrujSprawe(string teczkaSymbol, int idKoszulki, int idPracownika, string uwagi = "");

        // Operacje przekazywania
        Task<int> PrzekazKoszulke(int idKoszulki, int idPracownikaDocelowego, int idPracownikaZrodlowego);

        Task<TeczkaRwaDto> PobierzRwaPoRoczniku(int rok);
        Task<IEnumerable<int>> PobierzIdentyfikatoryKoszulek(int idPracownika);

        Task<IEnumerable<DokumentTypeDto>> PobierzDokumentyKoszulki(int idKoszulki);
        Task<IEnumerable<PismoDto>> PobierzSprawyTeczki(string symbolTeczki, int rok);
        Task<byte[]> PobierzZalacznik(int idZalacznika);

        Task<DokumentTypeDto> RejestrujDokument(string nazwa, int idKoszulki, int idZalacznika, int idPracownika);

        Task<(PismoDto Koszulka, DokumentTypeDto Dokument)> DodajKoszulkeZPlikiem(
        string nazwaKoszulki,
        byte[] plikDane,
        string nazwaPlikuZRozszerzeniem,
        int idPracownika);

    }
}