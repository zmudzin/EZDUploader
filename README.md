# EZD Uploader - Elektroniczne Zarządzanie Dokumentami

## Opis Systemu

Aplikacja EZD Uploader to narzędzie wspomagające elektroniczne zarządzanie dokumentami (EZD) w administracji publicznej. Została stworzona z myślą o uproszczeniu procesu rejestracji i przesyłania dokumentów w systemie EZD PUW.

## Główne Funkcje

### Zarządzanie Dokumentami
- Dodawanie plików z dysku lub przez przeciąganie
- Edycja metadanych dokumentów
- Dodawanie tytułów, rodzajów, dat i znaków pism
- Klasyfikacja rodzajów dokumentów
- Możliwość edycji masowej metadanych
- Sortowanie plików
- Otwieranie dodanych plików do listy w domyślnej aplikacji

### Integracja z API EZD
- Autoryzacja przez token (niesprawdzone) lub login/hasło
- Przesyłanie plików do wybranej koszulki/sprawy
- Walidacja dokumentów przed wysłaniem

### Interfejs Użytkownika
- Intuicyjna lista plików
- Podgląd statusu wysyłki
- Możliwość edycji wielu dokumentów jednocześnie

## Wymagania Techniczne

### Środowisko
- .NET Framework (minimum 4.7.2)
- Windows 7 lub nowszy

### Zależności
- ServiceStack.Client
- ServiceStack.HttpClient

## Konfiguracja

### Ustawienia aplikacji
Przed pierwszym użyciem skonfiguruj:
- Adres serwera API (bez / na końcu adresu : http://ezd-api.pl)
- Metodę autoryzacji (token/login)
- Dane uwierzytelniające
- uruchomić ponownie aplikację
- Opjonalnie dodać registry.reg celem dodania do menu kontekstowego Wysyłki do EZD (domyślny katalog dla aplikacji to C:\EZDUploader, maksymalna ilość plików którą można w ten sposób dodac to 100)
- Po zakończeniu wstępnej konfiguracji można edytować documentTypes.json, można dodać CSV z rodzajami dokumentów


## Obsługiwane Rodzaje Dokumentów

### Domyślne rodzaje dokumentów:
- Pismo
- Notatka
- Wniosek
- Decyzja
- Opinia
- Zaświadczenie
- Inny
- **Faktura** ⭐
- **Zamówienie** ⭐
- Skarga
- Nota księgowa
- Odwołanie
- Zawiadomienie
- Umowa
- Postanowienie
- Zażalenie
- Sprawozdanie
- Rachunek
- Protokół
- Notatka służbowa
- Wezwanie
- Upoważnienie
- Pełnomocnictwo
- Akt notarialny
- Deklaracja
- Informacja
- Powiadomienie
- Oferta
- Zgłoszenie
- Petycja
- Formularz
- Zapytanie
- Oświadczenie
- Uchwała
- Zgoda
- Zarządzenie

**Łącznie: 36 standardowych rodzajów dokumentów**

### Dodawanie i modyfikacja rodzajów dokumentów:

Aplikacja obsługuje **dwa sposoby** dodawania rodzajów dokumentów:

#### Metoda 1: Plik JSON (documentTypes.json) ⭐ **REKOMENDOWANA**

1. Znajdź plik `documentTypes.json` w katalogu aplikacji (obok pliku .exe)
2. Otwórz plik w edytorze tekstu
3. Dodaj nowe rodzaje w formacie:
   ```json
   {
     "DocumentTypes": [
       { "Id": 1, "Name": "Pismo" },
       { "Id": 2, "Name": "Notatka" },
       { "Id": 8, "Name": "Faktura" }
     ]
   }
   ```
4. Zapisz plik i **zrestartuj aplikację**

#### Metoda 2: Plik CSV (RodzajeDokumentów.csv)

1. Utwórz plik `RodzajeDokumentów.csv` w katalogu aplikacji
2. Format CSV (separator: średnik `;`, kodowanie: UTF-8):
   ```csv
   Id;Kolumna2;Kolumna3;Nazwa
   1;;;Pismo
   2;;;Notatka
   8;;;Faktura
   ```
   - **Kolumna 1**: ID (liczba)
   - **Kolumna 4**: Nazwa rodzaju dokumentu
3. Zapisz plik w UTF-8 i **zrestartuj aplikację**

**Uwaga**: Jeśli istnieje plik CSV, aplikacja użyje go zamiast JSON.

**Szczegółowa instrukcja**: Zobacz plik `INSTRUKCJA_RODZAJE_DOKUMENTOW.md` w katalogu projektu.

**Import z bazy danych**: Możesz wyeksportować dane z SQL:
```sql
SELECT *
FROM SlownikiAplikacji
WHERE IdSlownika = '8'
```
i zapisać jako CSV z separatorami średnikami (`;`).

## Instrukcja Użycia

1. Dodaj pliki do listy
2. Uzupełnij metadane dokumentów
3. Wybierz koszulkę/sprawę
4. Wyślij dokumenty

## Bezpieczeństwo

- Walidacja plików przed wysłaniem (Walidacja nazwy oraz rodzaju dokumentu)
- Szyfrowane połączenie z API
- Obsługa różnych metod uwierzytelniania

## Technologie

- Język: C#
- Framework: .NET
- Biblioteki: ServiceStack, Windows Forms

