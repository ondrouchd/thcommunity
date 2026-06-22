# THcommunity

THcommunity je SaaS pro organizaci sportovních týmů, zájmových skupin a klubů –
podobně jako Týmuj.cz. Trenérům, vedoucím i členům umožňuje na jednom místě
plánovat události, sledovat docházku, spravovat soupisku a komunikovat.

## Hlavní funkce

- **Plánování událostí** – kalendář tréninků a zápasů, kapacity hráčů/brankářů.
- **Docházka a RSVP** – členové potvrzují účast, trenér vidí, s kým může počítat.
- **Čekací listina** – při naplnění kapacity se další přihlášení řadí do fronty.
- **Týmová soupiska a profily** – role (Admin/Trenér/Hráč), pozice, kontakty.
- **Chat** – týmová komunikace v reálném čase, reakce, mazání zpráv.
- **Ankety** – jednoduché hlasování (i anonymní), výsledky v reálném čase.
- **Push notifikace** – upozornění na změny (Web Push, VAPID).
- **Statistiky** – docházka a účast hráče.
- **Vícejazyčnost** – čeština (výchozí) a angličtina.

## Technologie

| Vrstva    | Stack                                                                |
|-----------|----------------------------------------------------------------------|
| Backend   | .NET 8 minimal API, Supabase (PostgREST), JWT, WebPush, Cloudflare R2 |
| Frontend  | React 19, Vite, TypeScript, Tailwind, Zustand, i18next, PWA           |
| Databáze  | PostgreSQL (Supabase) s RLS politikami                                |

## Struktura repozitáře

```
backend/      .NET API (src/THcommunity) + testy (src/THcommunity.Tests)
frontend/     React + Vite aplikace
database/     SQL migrace (schéma, RLS, realtime, opravy)
docker-compose.yml
.env.example  Šablona proměnných prostředí
```

## Předpoklady

- .NET 8 SDK
- Node.js 20+
- Účet Supabase (nebo lokální Postgres kompatibilní s PostgREST)

## Nastavení databáze

Spusťte SQL migrace ze složky `database/` v tomto pořadí:

```
001_initial_schema.sql
002_rls_policies.sql
003_realtime.sql
004_security_fixes.sql
```

## Konfigurace

Zkopírujte `.env.example` na `.env` a doplňte hodnoty. Backend konfiguraci lze
také zadat v `backend/src/THcommunity/appsettings.Local.json`:

- `Supabase` – URL, klíče a JWT secret.
- `Vapid` – `Subject`, `PublicKey`, `PrivateKey` pro Web Push (jinak je push vypnutý).
- `Cloudflare:R2` – úložiště médií (volitelné; bez konfigurace se použije no-op úložiště).
- `AppSettings:Registration:DefaultTeamInviteCode` – volitelný kód týmu, do kterého
  se nově registrovaní uživatelé automaticky přidají. Prázdné = bez automatického týmu.

Frontend čte `VITE_API_URL` (výchozí `http://localhost:5000`) a Supabase klíče.

## Spuštění (vývoj)

Backend:

```bash
cd backend/src/THcommunity
dotnet run
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```

Případně celé prostředí přes Docker:

```bash
docker compose up --build
```

## Testy a build

```bash
# Backend – build a jednotkové testy
cd backend
dotnet build
dotnet test

# Frontend – typecheck a produkční build
cd frontend
npm run build
```

