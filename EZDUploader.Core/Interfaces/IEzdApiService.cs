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

        Task<IEnumerable<PismoDto>> PobierzIdentyfikatoryKoszulek(int idPracownika);

        Task<DokumentTypeDto> RejestrujDokument(
            string nazwa,
            int idKoszulki,
            int idZalacznika,
            int idPracownika,
            bool brakDaty = true,
            bool brakZnaku = true);

        Task<bool> AktualizujMetadaneDokumentu(DokumentTypeDto dokument);
        Task<bool> AktualizujMetadaneDokumentu(
        int idDokumentu,
        string tytul,
        string rodzaj,
        string znakPisma,
        DateTime? dataDokumentu,
        bool brakDaty = false,
        bool brakZnaku = false);

        Task<(PismoDto Koszulka, DokumentTypeDto Dokument)> DodajKoszulkeZPlikiem(
            string nazwaKoszulki,
            byte[] plikDane,
            string nazwaPlikuZRozszerzeniem,
            int idPracownika,
            bool brakDaty = true,
            bool brakZnaku = true);

    }
}