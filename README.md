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

## Provoz na Azure / AWS místo Supabase

Řešení je cloud-agnostické – vedle Supabase ho lze provozovat na **Azure** nebo
**AWS**. Architektura má tři poskytovatelsky závislé části; každou lze vyměnit
nezávisle a podpora Supabase zůstává zachovaná:

| Část | Supabase | Azure | AWS |
|------|----------|-------|-----|
| Databáze | Supabase Postgres | Azure Database for PostgreSQL | AWS RDS / Aurora PostgreSQL |
| Autentizace (JWT) | Supabase Auth (GoTrue) | Azure Entra (AD) **nebo** self-hosted GoTrue | AWS Cognito **nebo** self-hosted GoTrue |
| Úložiště médií | Cloudflare R2 | S3-kompatibilní endpoint | AWS S3 (nativně) |
| Realtime | Supabase Realtime | — (viz omezení) | — (viz omezení) |

Backend zůstává stejný; přepíná se konfigurací (`AppSettings__Auth__*`,
`AppSettings__Storage__*`). Když je `Auth:Provider` prázdný, chová se jako dřív
(Supabase).

### Varianta A – self-hosted stack nad managed Postgres

Stejné Supabase-kompatibilní služby (GoTrue + PostgREST) jako v lokálním stacku,
ale databáze běží jako managed PostgreSQL v cloudu. Funguje celá aplikace včetně
frontendu a přihlášení přes magic link.

1. Založ managed PostgreSQL (Azure Database for PostgreSQL Flexible Server nebo
   AWS RDS/Aurora PostgreSQL).
2. Zkopíruj příslušný vzor a vyplň hodnoty:

   ```bash
   cp .env.aws.example .env.cloud      # nebo .env.azure.example
   ```

3. Vygeneruj `ANON_KEY` a `SERVICE_KEY` podepsané `JWT_SECRET` (HS256):

   ```bash
   python - <<'PY'
   import base64, hmac, hashlib, json
   secret = b"super-secret-jwt-token-with-at-least-32-characters-long"  # = JWT_SECRET
   b = lambda d: base64.urlsafe_b64encode(d).rstrip(b"=")
   def mint(role):
       seg = b(json.dumps({"alg":"HS256","typ":"JWT"}).encode()) + b"." + \
             b(json.dumps({"role":role,"iss":"supabase-demo","iat":1641769200,"exp":1893456000}).encode())
       return (seg + b"." + b(hmac.new(secret, seg, hashlib.sha256).digest())).decode()
   print("ANON_KEY   =", mint("anon"))
   print("SERVICE_KEY=", mint("service_role"))
   PY
   ```

4. Spusť stack:

   ```bash
   docker compose --env-file .env.cloud -f docker-compose.cloud.yml up --build
   ```

Jednorázová služba `migrate` nejdřív spustí `database/000_cloud_bootstrap.sql`
(vytvoří Supabase role `anon`/`authenticated`/`service_role`/`authenticator`/
`supabase_auth_admin`, schéma `auth`, funkce `auth.uid()`/`auth.role()` a publikaci
`supabase_realtime`), poté aplikuje migrace `001`–`004`. Bootstrap vyžaduje roli
s oprávněním zakládat role a `BYPASSRLS` (`rds_superuser` na AWS RDS,
`azure_pg_admin` na Azure Flexible Server). Pro Realtime nastav na serveru
`wal_level = logical`.

### Varianta B – nativní Azure Entra / AWS Cognito

Backend umí ověřovat JWT přímo z poskytovatele identity přes standardní OpenID
Connect discovery (klíče se načítají a rotují automaticky). Nastav na backendu:

**AWS Cognito**

```
AppSettings__Auth__Provider=cognito
AppSettings__Auth__Region=eu-central-1
AppSettings__Auth__UserPoolId=eu-central-1_XXXXXXXXX
AppSettings__Auth__Audience=<app-client-id>
```

**Azure Entra (AD)**

```
AppSettings__Auth__Provider=azuread
AppSettings__Auth__TenantId=<tenant-id>
AppSettings__Auth__Audience=<application-client-id>
# Pro Entra External ID / B2C user flow místo TenantId:
# AppSettings__Auth__MetadataAddress=https://<tenant>.ciamlogin.com/<tenant>/v2.0/.well-known/openid-configuration
```

Případně libovolný OIDC poskytovatel: `AppSettings__Auth__Provider=oidc` +
`Authority` (nebo `MetadataAddress`), `Audience`, volitelně `Issuer`. Při nativní
auth nepotřebuješ služby `auth`/`rest`/`gateway` pro ověřování API; frontend by pak
získával token přes SDK daného poskytovatele (MSAL pro Entra, Amplify pro Cognito).

### Úložiště médií

Backend komunikuje protokolem S3, takže funguje s libovolným S3-kompatibilním
úložištěm:

```
# AWS S3 (nativně)
AppSettings__Storage__Provider=s3
AppSettings__Storage__Region=eu-central-1
AppSettings__Storage__BucketName=thcommunity-media
AppSettings__Storage__PublicUrl=https://thcommunity-media.s3.eu-central-1.amazonaws.com
AppSettings__Storage__AccessKeyId=...
AppSettings__Storage__SecretAccessKey=...

# Cloudflare R2 / MinIO / S3-kompatibilní endpoint (Azure Blob přes S3 bránu)
AppSettings__Storage__ServiceUrl=https://<endpoint>
```

Když je sekce `Storage` prázdná, použijí se zpětně kompatibilní proměnné
`Cloudflare__R2*`.

### Omezení

- **Realtime** (živá aktualizace chatu) není v self-hosted stacku nasazený; chat
  funguje přes API, degraduje jen aktualizace bez refreshe.
- Nativní Azure Blob přes oficiální SDK není implementován – použij S3-kompatibilní
  endpoint, nebo AWS S3 / R2.

