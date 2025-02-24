﻿using ServiceStack;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;
using EZDUploader.Infrastructure.Requests;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Diagnostics;
using EZDUploader.Infrastructure.Converters;

namespace EZDUploader.Infrastructure.Services
{
    public class EzdApiService : IEzdApiService
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        public ApiSettings Settings { get; }
        public bool IsAuthenticated { get; private set; }
        private int? _currentUserId;
        public int? CurrentUserId => _currentUserId;

        public EzdApiService(ApiSettings settings)
        {
            Settings = settings;
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _client = new HttpClient(handler);
            if (!string.IsNullOrEmpty(settings.BaseUrl))
            {
                _client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new Converters.MicrosoftDateTimeConverter() }
            };
        }


        public async Task<IEnumerable<PismoDto>> PobierzIdentyfikatoryKoszulek(int idPracownika)
        {
            EnsureAuthenticated();
            var request = new
            {
                IdPracownikaWlasciciela = idPracownika
            };

            try
            {
                var response = await PostAsync<PobierzKoszulkiResponse>("Koszulka/PobierzKoszulki", request);
                return response?.Pisma ?? new List<PismoDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas pobierania koszulek: {ex.Message}");
                return new List<PismoDto>();
            }
        }

        public async Task<bool> AktualizujMetadaneDokumentu(int idDokumentu, string rodzaj, DateTime dataDokumentu)
        {
            EnsureAuthenticated();

            var request = new
            {
                Dokument = new DokumentSystemowyTypeDto
                {
                    Identyfikator = new WskazanieDokumentuDto
                    {
                        Identyfikator = idDokumentu
                    },
                    Rodzaj = rodzaj,
                    DataDokumentu = dataDokumentu.ToString("yyyy-MM-dd")
                }
            };

            try
            {
                var response = await PostAsync<AktualizujDokumentRes>("api3/AktualizujDokument", request);
                return response?.IdDokumentu > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas aktualizacji metadanych dokumentu: {ex}");
                throw;
            }
        }

        public async Task<bool> AktualizujMetadaneDokumentu(
        int idDokumentu,
        string tytul,
        string rodzaj,
        string znakPisma,
        DateTime? dataDokumentu,
        bool brakDaty = false,
        bool brakZnaku = false)
        {
            EnsureAuthenticated();

            // Walidacja tytułu
            if (string.IsNullOrEmpty(tytul) || tytul.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
            {
                throw new ArgumentException("Tytuł musi składać się z co najmniej dwóch wyrazów", nameof(tytul));
            }

            var request = new
            {
                Dokument = new DokumentSystemowyTypeDto
                {
                    Identyfikator = new WskazanieDokumentuDto
                    {
                        Identyfikator = idDokumentu
                    },
                    Tytul = tytul,
                    Rodzaj = rodzaj,
                    Sygnatura = brakZnaku ? null : znakPisma,
                    DataDokumentu = brakDaty ? null : dataDokumentu?.ToString("yyyy-MM-dd"),
                    MetaBrakDaty = brakDaty,
                    MetaBrakZnaku = brakZnaku,
                    Metadane = true
                }
            };

            try
            {
                var response = await PostAsync<AktualizujDokumentRes>("api3/AktualizujDokument", request);
                return response?.IdDokumentu > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas aktualizacji metadanych dokumentu: {ex}");
                throw;
            }
        }

        public void SetupTokenAuth(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    Debug.WriteLine("Token jest pusty");
                    IsAuthenticated = false;
                    return;
                }

                Debug.WriteLine("Konfiguracja autoryzacji tokenem...");
                _client.DefaultRequestHeaders.Clear();
                var (authParam, authToken) = GenerateAuthTokens(token);
                Debug.WriteLine($"Wygenerowano tokeny - authParam: {authParam}");

                _client.DefaultRequestHeaders.Add("authParam", authParam);
                _client.DefaultRequestHeaders.Add("authToken", authToken);
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                IsAuthenticated = true;
                Debug.WriteLine("Konfiguracja tokena zakończona");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas konfiguracji tokena: {ex.Message}");
                IsAuthenticated = false;
                throw;
            }
        }

        private (string authParam, string authToken) GenerateAuthTokens(string applicationToken)
        {
            var authParam = Guid.NewGuid().ToString();
            var tokenBase = $"{authParam}{applicationToken}{DateTime.Now:yyyyMMddhh}";
            using var sha256 = SHA256.Create();
            var tokenBytes = Encoding.ASCII.GetBytes(tokenBase);
            var hashBytes = sha256.ComputeHash(tokenBytes);
            var authToken = string.Join("", hashBytes.Select(b => b.ToString("x2")));
            return (authParam, authToken);
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                Debug.WriteLine($"Próba logowania dla użytkownika: {username}");
                SetupAuthHeaders(username, password);

                var parameters = new Dictionary<string, string> { { "IdentyfikatorJednostki", "1" } };
                var encodedContent = new FormUrlEncodedContent(parameters);
                var response = await _client.PostAsync("/Jednostka/PoId", encodedContent);

                if (response.IsSuccessStatusCode)
                {
                    IsAuthenticated = true;
                    _currentUserId = 1; // TODO: Pobierz prawdziwe ID z API
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd logowania: {ex.Message}");
                return false;
            }
        }

        private void SetupAuthHeaders(string login, string password)
        {
            Debug.WriteLine("Konfiguracja Basic Auth...");
            Debug.WriteLine($"Login: {login}");

            _client.DefaultRequestHeaders.Clear();
            var decryptedPassword = DecryptPassword(password);
            Debug.WriteLine($"Odkodowane hasło: {decryptedPassword}");

            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}:{decryptedPassword}"));
            Debug.WriteLine($"Base64 auth string: {base64Auth}");

            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

            Debug.WriteLine("Headers:");
            foreach (var header in _client.DefaultRequestHeaders)
            {
                Debug.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
        }

        public void Logout()
        {
            IsAuthenticated = false;
            _currentUserId = null;
            _client.DefaultRequestHeaders.Clear();
        }

        private RejestrujSpraweResponse MapToResponse(RejestrujSpraweRes res)
        {
            if (res == null) return null;

            return new RejestrujSpraweResponse
            {
                IdSprawy = res.IdSprawy,
                DataRejestracjiSprawy = res.DataRejestracjiSprawy,
                IdTeczki = res.IdTeczki,
                SymbolTeczki = res.SymbolTeczki,
                KategoriaArchiwalna = res.KategoriaArchiwalna,
                TypProwadzenia = res.TypProwadzenia
            };
        }

        private async Task<T> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                endpoint = endpoint.TrimStart('/');
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                Debug.WriteLine($"Pełny URL: {_client.BaseAddress}{endpoint}");
                Debug.WriteLine($"Dane: {json}");

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

                // Dodanie nagłówków autoryzacyjnych
                foreach (var header in _client.DefaultRequestHeaders)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Kod odpowiedzi: {response.StatusCode}");
                Debug.WriteLine($"Treść odpowiedzi: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    if (endpoint.Contains("PobierzIdentyfikatoryKoszulekRequest") &&
                        response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return default(T);
                    }
                    throw new HttpRequestException(
                        $"API zwróciło błąd {response.StatusCode}: {responseContent}");
                }

                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Szczegóły błędu: {ex}");
                throw;
            }
        }

        public async Task<(PismoDto Koszulka, DokumentTypeDto Dokument)> DodajKoszulkeZPlikiem(
            string nazwaKoszulki,
            byte[] plikDane,
            string nazwaPlikuZRozszerzeniem,
            int idPracownika,
            bool brakDaty = true,
            bool brakZnaku = true)
        {
            EnsureAuthenticated();

            try
            {
                // 1. Sprawdzenie nazwy koszulki
                if (nazwaKoszulki.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
                {
                    throw new ArgumentException("Nazwa koszulki musi składać się z co najmniej dwóch wyrazów", nameof(nazwaKoszulki));
                }

                // 2. Utworzenie koszulki
                var koszulka = await UtworzKoszulke(nazwaKoszulki, idPracownika);
                Debug.WriteLine($"Utworzono koszulkę o ID: {koszulka.ID}");

                // 3. Dodanie załącznika
                var idZalacznika = await DodajZalacznik(plikDane, nazwaPlikuZRozszerzeniem, idPracownika);
                Debug.WriteLine($"Dodano załącznik o ID: {idZalacznika}");

                // 4. Rejestracja dokumentu z flagami
                var dokument = await RejestrujDokument(
                    nazwaPlikuZRozszerzeniem,
                    koszulka.ID,
                    idZalacznika,
                    idPracownika,
                    brakDaty,
                    brakZnaku
                );
                Debug.WriteLine($"Zarejestrowano dokument");

                return (koszulka, dokument);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas dodawania koszulki z plikiem: {ex}");
                throw;
            }
        }

        private void EnsureAuthenticated()
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Użytkownik nie jest zalogowany.");
        }
        public async Task<int> DodajZalacznik(byte[] dane, string nazwa, int idPracownika)
        {
            EnsureAuthenticated();

            var request = new DodajZalacznikRequest
            {
                Dane = dane,
                Nazwa = nazwa,  // używamy oryginalnej nazwy bez walidacji
                IdPracownikaWlasciciela = idPracownika
            };

            var response = await PostAsync<DodajZalacznikResponse>(
                "Zalacznik/DodajZalcznik",
                request
            );

            return response.ContentId;
        }


        public async Task<PismoDto> UtworzKoszulke(string nazwa, int idPracownika)
        {
            EnsureAuthenticated();

            var request = new
            {
                Nazwa = nazwa,
                IdPracownikaWlasciciela = idPracownika
            };

            var response = await PostAsync<UtworzKoszulkeRes>("/api1/UtworzKoszulke", request);

            if (response?.IdKoszulki > 0)
            {
                return new PismoDto
                {
                    ID = response.IdKoszulki,
                    Nazwa = nazwa,
                    DataUtworzenia = DateTime.Now,
                    // Dodajemy brakujące wymagane pola
                    Zawieszone = false,
                    Zakonczone = false,
                    KoszulkaWrazliwa = false,
                    CzyZarchiwizowany = false
                };
            }
            throw new Exception("Nie udało się utworzyć koszulki");
        }

        public async Task<DokumentTypeDto> RejestrujDokument(
    string nazwa,
    int idKoszulki,
    int idZalacznika,
    int idPracownika,
    bool brakDaty = true,
    bool brakZnaku = true)
        {
            EnsureAuthenticated();

            var request = new
            {
                Dokument = new
                {
                    Nazwa = nazwa,
                    Tytul = nazwa,
                    MetaBrakDaty = brakDaty,
                    MetaBrakZnaku = brakZnaku,
                    Metadane = true,
                    Lokalizacja = new
                    {
                        IdentyfikatorKontenera = "EZD.TEST.FLAT.MAIN",
                        IdentyfikatorZawartosci = idZalacznika.ToString(),
                        NazwaZawartosci = nazwa,
                        IdZalacznika = idZalacznika
                    }
                },
                Koszulka = new
                {
                    IdKoszulki = idKoszulki,
                    IdSprawy = 0,
                    ZnakSprawy = ""
                }
            };

            var response = await PostAsync<RejestrujDokumentResponse>("api3/RejestrujDokument", request);

            return new DokumentTypeDto
            {
                Identyfikator = new WskazanieDokumentuDto { Identyfikator = (int)response.IdDokumentu },
                DataUtworzenia = DateTime.Now,
                Nazwa = nazwa
            };
        }

        public async Task<bool> AktualizujMetadaneDokumentu(DokumentTypeDto dokument)
        {
            EnsureAuthenticated();
            var request = new { Dokument = dokument };
            var response = await PostAsync<dynamic>("api3/AktualizujDokument", request);
            return true;
        }

        public async Task<IEnumerable<DokumentTypeDto>> PobierzDokumentyKoszulki(int idKoszulki)
        {
            EnsureAuthenticated();
            var request = new PobierzIdentyfikatoryDokumentowKoszulkiRequest
            {
                IdKoszulki = idKoszulki
            };

            var response = await PostAsync<PobierzIdentyfikatoryDokumentowKoszulkiResponse>(
                "/Dokument/PobierzIdentyfikatoryDokumentowKoszulki", request);

            if (response?.dokumenty == null || !response.dokumenty.Any())
                return new List<DokumentTypeDto>();

            var dokumenty = new List<DokumentTypeDto>();
            foreach (var idDokumentu in response.dokumenty)
            {
                // TODO: Dodać metodę do pobierania szczegółów dokumentu
                // var dokument = await PobierzDokument(idDokumentu);
                // dokumenty.Add(dokument);
            }

            return dokumenty;
        }

        public async Task<PismoDto> PobierzKoszulkePoId(int id)
        {
            EnsureAuthenticated();
            var request = new PobierzKoszulkePoIdRequest { Id = id };
            var response = await PostAsync<PobierzKoszulkePoIdResponse>("Koszulka/PoId", request);
            return response?.Pismo ?? throw new Exception("Nie znaleziono koszulki");
        }

        public async Task<PismoDto> PobierzKoszulkePoZnakuSprawy(string znak)
        {
            EnsureAuthenticated();
            var request = new PobierzKoszulkePoZnakuSprawyRequest { Znak = znak };
            var response = await PostAsync<PobierzKoszulkePoZnakuSprawyResponse>("Koszulka/PobierzPoZnakuSprawy", request);
            return response?.Pismo ?? throw new Exception("Nie znaleziono sprawy");
        }

        public async Task<RejestrujSpraweResponse> RejestrujSprawe(string teczkaSymbol, int idKoszulki, int idPracownika, string uwagi = "")
        {
            EnsureAuthenticated();
            var request = new RejestrujSpraweReq
            {
                TeczkaSymbol = teczkaSymbol,
                IdKoszulki = idKoszulki,
                IdPracownikaWlasciciela = idPracownika,
                Uwagi = uwagi
            };
            var response = await PostAsync<RejestrujSpraweRes>("RejestrSpraw/RejestrujSprawe", request);
            return response != null ? MapToResponse(response) : throw new Exception("Nie udało się zarejestrować sprawy");
        }

        private string DecryptPassword(string password)
        {
            // Usuwamy dekodowanie, ponieważ hasło jest już w plain text
            return password;
        }

    }



}