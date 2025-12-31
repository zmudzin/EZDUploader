# Instrukcja: Dodawanie i Modyfikacja Rodzajów Dokumentów

Aplikacja EZD Uploader obsługuje **dwa sposoby** dodawania i modyfikacji rodzajów dokumentów:

## Metoda 1: Plik JSON (documentTypes.json) ⭐ **REKOMENDOWANA**

### Lokalizacja pliku:
Plik `documentTypes.json` znajduje się w katalogu, w którym uruchomiono aplikację (zwykle `bin\Debug\net8.0-windows\` lub `bin\Release\net8.0-windows\`).

### Format pliku:
```json
{
  "DocumentTypes": [
    {
      "Id": 1,
      "Name": "Pismo"
    },
    {
      "Id": 2,
      "Name": "Notatka"
    },
    {
      "Id": 3,
      "Name": "Wniosek"
    },
    {
      "Id": 4,
      "Name": "Decyzja"
    },
    {
      "Id": 5,
      "Name": "Opinia"
    },
    {
      "Id": 6,
      "Name": "Zaświadczenie"
    },
    {
      "Id": 7,
      "Name": "Inny"
    }
  ]
}
```

### Jak dodać nowy rodzaj dokumentu:
1. Otwórz plik `documentTypes.json` w edytorze tekstu (Notatnik, Visual Studio Code, itp.)
2. Dodaj nowy wpis w tablicy `DocumentTypes`:
   ```json
   {
     "Id": 8,
     "Name": "Faktura"
   }
   ```
3. Zapisz plik
4. **Uruchom ponownie aplikację** - nowe rodzaje będą dostępne

### Przykład rozszerzonej listy:
```json
{
  "DocumentTypes": [
    { "Id": 1, "Name": "Pismo" },
    { "Id": 2, "Name": "Notatka" },
    { "Id": 3, "Name": "Wniosek" },
    { "Id": 4, "Name": "Decyzja" },
    { "Id": 5, "Name": "Opinia" },
    { "Id": 6, "Name": "Zaświadczenie" },
    { "Id": 7, "Name": "Inny" },
    { "Id": 8, "Name": "Faktura" },
    { "Id": 9, "Name": "Skarga" },
    { "Id": 10, "Name": "Nota księgowa" },
    { "Id": 11, "Name": "Odwołanie" },
    { "Id": 12, "Name": "Zawiadomienie" },
    { "Id": 13, "Name": "Umowa" },
    { "Id": 14, "Name": "Postanowienie" },
    { "Id": 15, "Name": "Zażalenie" },
    { "Id": 16, "Name": "Sprawozdanie" },
    { "Id": 17, "Name": "Rachunek" },
    { "Id": 18, "Name": "Protokół" },
    { "Id": 19, "Name": "Notatka służbowa" },
    { "Id": 20, "Name": "Wezwanie" }
  ]
}
```

---

## Metoda 2: Plik CSV (RodzajeDokumentów.csv)

### Lokalizacja pliku:
Plik `RodzajeDokumentów.csv` należy umieścić w katalogu, w którym uruchomiono aplikację (obok pliku .exe).

### Format pliku CSV:
Plik musi być zakodowany w **UTF-8** i używać średnika (`;`) jako separatora.

**Struktura:**
- **Kolumna 1 (indeks 0)**: ID dokumentu (liczba całkowita)
- **Kolumna 2 (indeks 1)**: (pomijana)
- **Kolumna 3 (indeks 2)**: (pomijana)
- **Kolumna 4 (indeks 3)**: Nazwa rodzaju dokumentu ⭐ **UŻYWANA**

### Przykładowy plik CSV:
```csv
Id;Kolumna2;Kolumna3;Nazwa
1;;;Pismo
2;;;Notatka
3;;;Wniosek
4;;;Decyzja
5;;;Opinia
6;;;Zaświadczenie
7;;;Inny
8;;;Faktura
9;;;Skarga
10;;;Nota księgowa
```

**UWAGA**: Pierwszy wiersz (nagłówek) jest pomijany przez aplikację.

### Jak utworzyć plik CSV z bazy danych:
Zgodnie z README, można wyeksportować dane z bazy SQL:
```sql
SELECT *
FROM SlownikiAplikacji
WHERE IdSlownika = '8'
```

Następnie wyeksportuj wynik do CSV z separatorami średnikami (`;`), upewniając się, że:
- ID jest w pierwszej kolumnie
- Nazwa jest w czwartej kolumnie
- Plik jest zakodowany w UTF-8

---

## Priorytet ładowania:

Aplikacja ładuje rodzaje dokumentów w następującej kolejności:

1. **Najpierw**: Sprawdza czy istnieje plik `RodzajeDokumentów.csv`
   - Jeśli istnieje i zawiera poprawne dane → używa go
   - Jeśli istnieje ale jest pusty/błędny → przechodzi do kroku 2

2. **Następnie**: Sprawdza czy istnieje plik `documentTypes.json`
   - Jeśli istnieje → używa go
   - Jeśli nie istnieje → tworzy domyślny plik z 7 podstawowymi rodzajami

3. **W ostateczności**: Używa wbudowanych domyślnych rodzajów dokumentów

---

## Ważne uwagi:

1. **Restart aplikacji**: Po modyfikacji plików JSON lub CSV **wymagany jest restart aplikacji**, aby zmiany zostały załadowane.

2. **Kodowanie plików**:
   - JSON: automatycznie obsługiwane
   - CSV: **musi być UTF-8** (ważne dla polskich znaków: ą, ć, ę, ł, ń, ó, ś, ź, ż)

3. **ID dokumentów**:
   - Muszą być unikalne (nie mogą się powtarzać)
   - Powinny być liczbami całkowitymi
   - Zalecane: używać kolejnych numerów

4. **Nazwy dokumentów**:
   - Nie mogą być puste
   - Powinny być unikalne (zalecane)
   - Obsługują polskie znaki

5. **Lokalizacja plików**:
   - Pliki muszą być w tym samym katalogu co plik wykonywalny aplikacji
   - Dla aplikacji uruchomionej z Visual Studio: `bin\Debug\net8.0-windows\`
   - Dla aplikacji skompilowanej: katalog z plikiem `.exe`

---

## Przykład praktyczny:

### Krok 1: Znajdź katalog aplikacji
- Jeśli uruchamiasz z Visual Studio: `EZDUploader\EZDUploader.UI\bin\Debug\net8.0-windows\`
- Jeśli uruchamiasz skompilowaną wersję: katalog z `EZDUploader.UI.exe`

### Krok 2: Edytuj documentTypes.json
Otwórz plik `documentTypes.json` i dodaj nowe rodzaje:
```json
{
  "DocumentTypes": [
    { "Id": 1, "Name": "Pismo" },
    { "Id": 2, "Name": "Notatka" },
    { "Id": 8, "Name": "Faktura" },
    { "Id": 9, "Name": "Skarga" }
  ]
}
```

### Krok 3: Zapisz i zrestartuj aplikację
- Zapisz plik
- Zamknij aplikację
- Uruchom ponownie
- Nowe rodzaje będą dostępne w menu wyboru rodzaju dokumentu

---

## Rozwiązywanie problemów:

### Problem: Zmiany nie są widoczne
**Rozwiązanie**: Upewnij się, że:
- Zrestartowałeś aplikację
- Plik jest w odpowiednim katalogu
- Format pliku jest poprawny (sprawdź składnię JSON)

### Problem: Błąd przy ładowaniu CSV
**Rozwiązanie**: Sprawdź:
- Czy plik jest zakodowany w UTF-8
- Czy używa średników (`;`) jako separatorów
- Czy pierwsza kolumna zawiera liczby (ID)
- Czy czwarta kolumna zawiera nazwy

### Problem: Polskie znaki nie wyświetlają się poprawnie
**Rozwiązanie**: 
- Dla CSV: Upewnij się, że plik jest zapisany w UTF-8
- Dla JSON: Zwykle działa automatycznie, ale sprawdź kodowanie pliku

---

## Podsumowanie:

✅ **Najłatwiejsza metoda**: Edycja pliku `documentTypes.json`  
✅ **Dla masowego importu**: Użyj pliku CSV `RodzajeDokumentów.csv`  
✅ **Wymagany restart**: Po każdej zmianie plików  
✅ **Lokalizacja**: Katalog z plikiem wykonywalnym aplikacji

