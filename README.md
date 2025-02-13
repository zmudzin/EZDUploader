# EZD Uploader - Elektroniczne Zarządzanie Dokumentami

## Opis Systemu

Aplikacja EZD Uploader to narzędzie wspomagające elektroniczne zarządzanie dokumentami (EZD) w administracji publicznej. Została stworzona z myślą o uproszczeniu procesu rejestracji i przesyłania dokumentów w systemie EZD PUW.

## Główne Funkcje

### Zarządzanie Dokumentami
- Dodawanie plików z dysku lub przez przeciąganie
- Edycja metadanych dokumentów
- Dodawanie tytułów, dat i znaków pism
- Klasyfikacja rodzajów dokumentów

### Integracja z API EZD
- Autoryzacja przez token lub login/hasło
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

### Ustawienia Połączenia
Przed pierwszym użyciem skonfiguruj:
- Adres serwera API
- Metodę autoryzacji (token/login)
- Dane uwierzytelniające

## Obsługiwane Rodzaje Dokumentów
- Pismo
- Notatka
- Wniosek
- Decyzja
- Opinia
- Zaświadczenie
- Inny

## Instrukcja Użycia

1. Dodaj pliki do listy
2. Uzupełnij metadane dokumentów
3. Wybierz koszulkę/sprawę
4. Wyślij dokumenty

## Bezpieczeństwo

- Walidacja plików przed wysłaniem
- Szyfrowane połączenie z API
- Obsługa różnych metod uwierzytelniania

## Technologie

- Język: C#
- Framework: .NET
- Biblioteki: ServiceStack, Windows Forms

