# Time-Blocker & Notes (v1.0 MVP)

Aplicație web pentru planificarea zilei pe intervale orare (time-blocking), cu notițe atașate fiecărui interval.

- ASP.NET Core 10: Razor Pages + Minimal API (JSON)
- ASP.NET Core Identity (cookie auth, pagini proprii)
- Entity Framework Core 10 + SQLite
- Bootstrap 5.3 (CDN) + JavaScript vanilla (ES modules)
- Teste: xUnit + SQLite in-memory

## Cerințe

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- `dotnet-ef`, în una din variante:
  - global: `dotnet tool install --global dotnet-ef`
  - local (manifestul `dotnet-tools.json` din repo): `dotnet tool restore`

## Comenzi

```bash
dotnet restore
dotnet ef database update --project src/TimeBlocker.Web
dotnet run --project src/TimeBlocker.Web
dotnet test
```

Aplicația pornește pe `https://localhost:7260`. Cookie-ul de autentificare este `Secure`, deci aplicația trebuie accesată prin HTTPS. Dacă certificatul de dezvoltare nu este de încredere: `dotnet dev-certs https --trust`.

În mediul `Development` migrările se aplică automat la pornire.

Pentru o migrare nouă:

```bash
dotnet ef migrations add <Nume> --project src/TimeBlocker.Web -o Data/Migrations
```

## Baza de date

- Locație: `src/TimeBlocker.Web/App_Data/timeblocker.db` (directorul `App_Data` este creat automat la pornire și este ignorat în git).
- Backup: oprește aplicația, apoi copiază fișierul `timeblocker.db` (împreună cu `timeblocker.db-wal` și `timeblocker.db-shm`, dacă există). Restaurarea se face copiind fișierele înapoi, tot cu aplicația oprită.

## API

Toate rutele cer autentificare (401 fără sesiune). POST/PUT/DELETE cer header-ul `X-CSRF-TOKEN` (token-ul din `<meta name="csrf-token">`).

| Metodă | Rută | Succes | Erori |
|---|---|---|---|
| GET | `/api/timeblocks?date=yyyy-MM-dd` | 200 | 400 |
| GET | `/api/timeblocks/{id}` | 200 | 404 |
| POST | `/api/timeblocks` | 201 + `Location` | 400, 409 |
| PUT | `/api/timeblocks/{id}` | 200 | 400, 404, 409 |
| DELETE | `/api/timeblocks/{id}` | 204 | 404 |

Corp POST/PUT:

```json
{ "title": "Ședință", "date": "2026-10-15", "startTime": "10:00", "endTime": "11:00", "notes": "https://example.com" }
```

## Presupuneri

- **P1:** Orele sunt stocate ca oră locală (`DateOnly` + `TimeOnly`), fără conversie de fus orar. Ziua curentă este determinată de browser și trimisă explicit ca `?date=YYYY-MM-DD`.
- **P2:** Un interval nu poate trece de miezul nopții (`EndTime > StartTime`, aceeași zi). Ultima oră de sfârșit posibilă este 23:55 (minutele sunt multipli de 5).
- **P3:** Suprapunerile de intervale pentru același utilizator și aceeași zi sunt respinse (HTTP 409). Capetele care se ating (10:00–11:00 și 11:00–12:00) nu sunt suprapunere.
- **P4:** Fără confirmare email și fără resetare parolă în v1.0.
- **P5:** „Timp real” = operațiile CRUD actualizează timeline-ul fără reîncărcarea paginii (fetch + re-randare). Fără WebSockets/SignalR.
