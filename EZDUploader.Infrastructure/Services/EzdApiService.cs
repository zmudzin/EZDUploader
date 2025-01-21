using ServiceStack;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;
using EZDUploader.Infrastructure.Requests;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Diagnostics;

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
            _client = new HttpClient(handler) { BaseAddress = new Uri(settings.BaseUrl) };
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            Debug.WriteLine($"Inicjalizacja API Service z URL: {settings.BaseUrl}");

            // Jeśli mamy zapisane dane logowania, od razu próbujemy się zalogować
            if (!string.IsNullOrEmpty(settings.Login) && !string.IsNullOrEmpty(settings.Password))
            {
                Debug.WriteLine("Znaleziono zapisane dane logowania, próba automatycznego logowania...");
                SetupAuthHeaders(settings.Login, settings.Password); // Password jest już zakodowane w settings
                IsAuthenticated = true;
            }
        }

        private void SetupAuthHeaders(string login, string password)
        {
            Debug.WriteLine("Konfiguracja nagłówków Basic Auth...");
            _client.DefaultRequestHeaders.Clear();
            var decryptedPassword = DecryptPassword(password);
            var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{login}:{decryptedPassword}"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<TeczkaRwaDto> PobierzRwaPoRoczniku(int rok)
        {
            try
            {
                Debug.WriteLine($"Próba pobrania RWA dla roku {rok}");
                EnsureAuthenticated();

                var request = new
                {
                    Rocznik = rok,
                    CID = 0,
                    IdPracownikaWlasciciela = _currentUserId ?? 0,
                    IdStanowiskaWlasciciela = 0
                };

                var response = await PostAsync<PobierzRwaResponse>("rwa/PobierzRwa", request);
                return response?.Teczki;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd w PobierzRwa: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<int>> PobierzIdentyfikatoryKoszulek(int idPracownika)
        {
            EnsureAuthenticated();
            var request = new PobierzIdentyfikatoryKoszulekRequest { IdPracownikaWlasciciela = idPracownika };
            var response = await PostAsync<PobierzIdentyfikatoryKoszulekResponse>("/Koszulka/PobierzIdentyfikatoryKoszulek", request);
            return response?.Koszulki ?? new List<int>();
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

                // Próba wykonania prostego zapytania testowego
                var response = await _client.PostAsync("/Jednostka/PoId",
                    new StringContent(
                        JsonSerializer.Serialize(new { IdentyfikatorJednostki = 1 }),
                        Encoding.UTF8,
                        "application/json"));

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Odpowiedź serwera: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("Logowanie udane");
                    IsAuthenticated = true;
                    return true;
                }
                else
                {
                    Debug.WriteLine($"Logowanie nieudane. Kod: {response.StatusCode}");
                    IsAuthenticated = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas logowania: {ex.Message}");
                IsAuthenticated = false;
                return false;
            }
        }

        public void Logout()
        {
            IsAuthenticated = false;
            _currentUserId = null;
            _client.DefaultRequestHeaders.Clear();
        }

        private async Task<T> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                Debug.WriteLine($"Wysyłanie żądania do: {_client.BaseAddress}{endpoint}");
                Debug.WriteLine($"Dane: {json}");

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Kod odpowiedzi: {response.StatusCode}");
                Debug.WriteLine($"Treść odpowiedzi: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"API zwróciło błąd {response.StatusCode}: {responseContent}");
                }

                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd HTTP: {ex.Message}");
                Debug.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
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
                Nazwa = nazwa,
                IdPracownikaWlasciciela = idPracownika
            };
            var response = await PostAsync<DodajZalacznikResponse>("/Zalacznik/DodajZalcznik", request);
            return response.ContentId;
        }

        public async Task<bool> TestApi()
        {
            try
            {
                var response = await PostAsync<PobierzRwaResponse>("rwa/PobierzRwa",
                    new { Rocznik = DateTime.Now.Year });
                Debug.WriteLine("Test API udany");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Test API nieudany: {ex.Message}");
                return false;
            }
        }

        public async Task<PismoDto> UtworzKoszulke(string nazwa, int idPracownika)
        {
            EnsureAuthenticated();

            var request = new UtworzKoszulkeReq
            {
                Nazwa = nazwa,
                IdPracownikaWlasciciela = idPracownika
            };

            var response = await PostAsync<UtworzKoszulkeRes>("/Zalacznik/UtworzKoszulke", request);
            if (response?.IdKoszulki > 0)
            {
                return await PobierzKoszulkePoId(response.IdKoszulki);
            }

            throw new Exception("Nie udało się utworzyć koszulki");
        }

        private RejestrujSpraweResponse MapToResponse(RejestrujSpraweRes res)
        {
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

        public async Task<IEnumerable<PismoDto>> PobierzSprawyTeczki(string symbolTeczki, int rok)
        {
            EnsureAuthenticated();
            var request = new PobierzSprawyTeczkiRequest
            {
                TeczkaSymbol = symbolTeczki,
                Rok = rok
            };

            var response = await PostAsync<PismoDto[]>("/Teczka/PobierzSprawy", request);
            return response ?? new PismoDto[0];
        }

        public async Task<PismoDto> PobierzKoszulkePoId(int id)
        {
            EnsureAuthenticated();
            var request = new PobierzKoszulkePoIdRequest { Id = id };
            var response = await PostAsync<PobierzKoszulkePoIdResponse>("/Koszulka/PoId", request);
            return response?.Pismo ?? throw new Exception("Nie znaleziono koszulki");
        }

        public async Task<PismoDto> PobierzKoszulkePoZnakuSprawy(string znak)
        {
            EnsureAuthenticated();
            var request = new PobierzKoszulkePoZnakuSprawyRequest { Znak = znak };
            var response = await PostAsync<PobierzKoszulkePoZnakuSprawyResponse>("/Koszulka/PobierzPoZnakuSprawy", request);
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
            var response = await PostAsync<RejestrujSpraweRes>("/RejestrSpraw/RejestrujSprawe", request);
            return response != null ? MapToResponse(response) : throw new Exception("Nie udało się zarejestrować sprawy");
        }

        public async Task<int> PrzekazKoszulke(int idKoszulki, int idPracownikaDocelowego, int idPracownikaZrodlowego)
        {
            EnsureAuthenticated();
            var request = new PrzekazKoszulkeReq
            {
                IdKoszulki = idKoszulki,
                IdPracownikaDocelowego = idPracownikaDocelowego,
                IdPracownikaZrodlowego = idPracownikaZrodlowego
            };
            var response = await PostAsync<PrzekazKoszulkeRes>("/Koszulka/PrzekazKoszulkeReq", request);
            return response?.IdEtapPisma ?? throw new Exception("Nie udało się przekazać koszulki");
        }
           public async Task<byte[]> PobierzZalacznik(int idZalacznika)
        {
            EnsureAuthenticated();
            var request = new PobierzZalacznikRequest
            {
                IdZalacznia = idZalacznika
            };

            var response = await PostAsync<PobierzZalacznikResponse>("/Zalacznik/PobierzZalacznik", request);
            return response?.zalacznik ?? throw new Exception("Nie znaleziono załącznika");
        }
        private string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword)) return encryptedPassword;
            try
            {
                byte[] data = Convert.FromBase64String(encryptedPassword);
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return encryptedPassword; // W przypadku błędu zwracamy oryginalne hasło
            }
        }

    }



}