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

Případně celé prostředí přes Docker – viz níže.

## Spuštění na localhostu přes Docker

Celé řešení (backend + frontend) lze spustit jedním příkazem. Potřebujete jen
nainstalovaný Docker s pluginem Compose.

1. Vytvořte `.env` z šablony a doplňte Supabase údaje (Project Settings → API):

   ```bash
   cp .env.example .env
   # vyplňte SUPABASE_URL, SUPABASE_PUBLISHABLE_KEY, SUPABASE_SECRET_KEY
   ```

2. Sestavte a spusťte:

   ```bash
   docker compose up --build
   ```

3. Otevřete v prohlížeči:

   - Frontend: <http://localhost:3000>
   - Backend API + Swagger: <http://localhost:5000>
   - Health check: <http://localhost:5000/health>

Zastavení: `docker compose down`.

Poznámky:

- Backend naběhne i bez vyplněného Supabase, ale přihlášení a práce s daty
  vyžadují platné Supabase údaje (autentizace, databáze a realtime běží na Supabase).
- `VAPID_*` a `R2_*` proměnné jsou volitelné – bez nich jsou push notifikace,
  resp. ukládání médií, vypnuté.
- CORS na backendu je pro tento běh nastaven na `http://localhost:3000`.

## Lokální běh bez Supabase (DB i auth v Dockeru)

Pokud nechceš zakládat projekt na Supabase, existuje samostatný, plně lokální
stack, který spustí databázi i autentizaci v Dockeru. Nepotřebuješ žádné cloudové
služby ani `.env` – demo klíče jsou součástí compose souboru (slouží **výhradně**
pro lokální vývoj).

Stack obsahuje: Postgres (image `supabase/postgres` s rolemi `anon`/`authenticated`/
`service_role`, schématem `auth`, funkcí `auth.uid()` a publikací `supabase_realtime`),
PostgREST, GoTrue (přihlášení přes magic link / OTP), Inbucket (chytač e-mailů),
nginx bránu, backend a frontend.

Spuštění:

```bash
docker compose -f docker-compose.local.yml up --build
```

Po naběhnutí:

- Frontend: <http://localhost:3000>
- Backend API: <http://localhost:5000> (health: <http://localhost:5000/health>)
- Supabase brána (auth + REST): <http://localhost:8000>
- Inbucket (čtení e-mailů): <http://localhost:9000>

Přihlášení (magic link):

1. Na přihlašovací stránce zadej libovolný e-mail a nech si poslat odkaz.
2. Otevři Inbucket na <http://localhost:9000>, najdi e-mail „Confirm Your Email“
   a klikni na odkaz – GoTrue ověří token a přesměruje tě zpět do aplikace s aktivní relací.
3. Při prvním přihlášení tě aplikace provede registrací profilu.

Migrace z `database/*.sql` se aplikují automaticky při prvním startu (služba `migrate`).
Při dalších spuštěních se přeskočí, pokud už schéma existuje. Pro úplný reset:

```bash
docker compose -f docker-compose.local.yml down -v
```

Poznámky a omezení:

- OAuth (Google/Facebook) lokálně nefunguje (vyžaduje reálné providery) – používej magic link.
- **Realtime není součástí** lokálního stacku (vyžaduje složitější nastavení).
  Chat funguje (odeslání i načtení zpráv přes API), degraduje pouze živá aktualizace bez refreshe.
- Backend ověřuje JWT symetrickým klíčem (HS256, `AppSettings__Supabase__JwtSecret`),
  zatímco proti cloud Supabase používá asymetrické klíče z JWKS – obě cesty fungují současně.
- `AppSettings__Supabase__InternalUrl` (např. `http://gateway:8000`) je adresa brány
  dosažitelná z kontejneru backendu; `Url` zůstává veřejná (`http://localhost:8000`)
  a používá se pro validaci issueru a ve frontendu.

```bash
# Backend – build a jednotkové testy
cd backend
dotnet build
dotnet test

# Frontend – typecheck a produkční build
cd frontend
npm run build
```

