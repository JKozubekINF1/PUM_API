# MiniStrava

System informatyczny tworzony w ramach zajęć "Programowanie urządzeń mobilnych" na uczelni Collegium Witelona Uczelnia Państwowa prowadzonych przez mgr inż. Kamila Piecha.

## Linki do repozytoriów projektu:

- [API](https://github.com/JKozubekINF1/PUM_API)
- [Web (Admin)](https://github.com/JKozubekINF1/PUM_ADMIN)
- [Mobile](https://github.com/JKozubekINF1/PUM_Mobile)

## Wymagania aplikacji

System służy do rejestrowania i analizy aktywności fizycznych użytkowników (takich jak: bieganie, jazda na rowerze i marsz) z wykorzystaniem danych GPS urządzenia mobilnego.

| ID | Opis funkcjonalności | API | Web | Mobile
| -- | -------------------- | --- | --- | --- |
| 01 | Użytkownik może pobrać aplikację na systemy Android i iOS. | | | X
| 02 | Użytkownik może zarejestrować konto i zalogować się w aplikacji. | X | | X
| 03 | Użytkownik może zresetować swoje hasło (np. gdy zapomni hasła). | X | | X
| 04 | Użytkownik po zalogowaniu może ustawić swoje dane profilowe takie jak: imię, nazwisko, data urodzenia, płeć, wzrost, waga i avatar. | X | | X
| 05 | Po zalogowaniu użytkownik może rozpocząć nową aktywność, zakończyć ją i zapisać dane. | X | | X
| 06 | W trakcie aktywności aplikacja rejestruje czas, dystans, tempo, prędkość i ślad GPS. | | | X
| 07 | Po zakończeniu aktywności użytkownik może nadać nazwę, dodać notatkę, zdjęcie i oznaczyć typ aktywności (rower, bieg, spacer). | X | | X
| 08 | Historia aktywności wyświetlana jest w formie listy. | | | X
| 09 | Kliknięcie na aktywność otwiera szczegóły (trasa, czas, tempo, notatki, zdjęcie). | X | | X
| 10 | Użytkownik może filtrować i sortować swoje aktywności (np. po dacie, dystansie, typie). | | | X
| 11 | Aplikacja wyświetla podstawowe statystyki użytkownika (liczba treningów, łączny dystans, średnia prędkość). | X | | X
| 12 | Aplikacja obsługuje ranking użytkowników (np. sumaryczny dystans tygodniowy). | X | | X
| 13 | Aplikacja umożliwia synchronizację danych z serwerem (po zalogowaniu). | X | | X
| 14 | Aplikacja mobilna korzysta z REST API. | X | | X
| 15 | Interfejs aplikacji dostępny jest w języku polskim i angielskim. | | | X
| 16 | Interfejs przyjazny użytkownikowi (UX mobilny). | | | X
| 17 | Aplikacja mobilna działa w tle podczas aktywności. | | | X
| 18 | Administrator może zalogować się do panelu webowego. | X | X |
| 19 | Panel umożliwia przeglądanie listy użytkowników i aktywności. | X | X |
| 20 | Administrator może filtrować (po użytkowniku, dacie dodania, długości trasy, typie aktywności), wyszukiwać i usuwać aktywności. | X | X |
| 21 | Administrator widzi globalne statystyki (liczba użytkowników, aktywności, łączny dystans). | X | X |
| 22 | Backend udostępnia REST API zgodne ze specyfikacją OpenAPI 3.0. | X | |
| 23 | Dokumentacja API dostępna jest pod endpointem /api/documentation (OpenAPI/Swagger). | X | |
| 24 | API umożliwia eksport aktywności do pliku .gpx. | X | |
| 25 | Komunikacja API zabezpieczona przez HTTPS. | X | |
| 26 | Po wdrożeniu systemu baza danych zawiera przykładowych 5 użytkowników oraz przykładowe aktywności testowe. Konto administratora tworzone jest przy pierwszym uruchomieniu systemu, a hasło ustalane przy pierwszym logowaniu. | X | |

# Instrukcja uruchomienia

Do dodania
