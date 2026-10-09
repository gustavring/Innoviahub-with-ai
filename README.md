# Innovia Hub

Innovia Hub är en webbapplikation för ett coworking- och forskningscenter där användare kan boka resurser och se deras tillgänglighet.

Projektet innehåller även **Hubert**, en AI-assistent som hjälper användare med frågor om resurser, tillgänglighet och bokningar.

Projektet finns på GitHub: [Innoviahub-with-ai](https://github.com/gustavring/Innoviahub-with-ai), branch `dev-2.0`.

## Tekniker

- **Backend:** ASP.NET Core Web API (.NET 8)
- **Frontend:** React, TypeScript och Vite
- **Databas:** PostgreSQL och Entity Framework Core
- **Autentisering:** ASP.NET Core Identity och JWT
- **Realtid:** SignalR
- **AI:** OpenAI API (Hubert)
- **Docker:** Backend, frontend och PostgreSQL

## Kom igång

Du kan starta hela projektet med Docker eller köra backend och frontend separat för lokal utveckling.

### 1. Klona projektet

```powershell
git clone -b dev-2.0 https://github.com/gustavring/Innoviahub-with-ai.git
cd Innoviahub-with-ai
```

### 2. Konfigurera miljövariabler

Kopiera `.env.example` till `.env` i projektets rotmapp:

```powershell
Copy-Item .env.example .env
```

Öppna `.env` och fyll i dina värden:

```dotenv
JWT_KEY=ange-en-lang-slumpmassig-hemlig-nyckel-har
POSTGRES_DB=innoviahub
POSTGRES_USER=postgres
POSTGRES_PASSWORD=ditt_databaslosenord
AI_API_KEY=din_openai_api_nyckel
```

Använd en lång, slumpmässigt genererad JWT-nyckel, helst minst 32 byte.

Du kan skapa en OpenAI API-nyckel på https://platform.openai.com/api-keys.

**Viktigt:** `.env` innehåller känsliga uppgifter och ska inte laddas upp till GitHub.

### 3. Starta med Docker (rekommenderas)

Se till att Docker Desktop är igång.

Kör från projektets rotmapp:

```powershell
docker compose up -d --build
```

Detta startar PostgreSQL, backend och frontend. Databasmigrationerna körs automatiskt när backend startar.

Öppna webbapplikationen på:

**http://localhost:3000**

Backend körs på `http://localhost:5197`.

För att stoppa projektet:

```powershell
docker compose down
```

### 4. Alternativ: Kör backend och frontend separat

Vid lokal utveckling kan du köra PostgreSQL i Docker och starta backend och frontend var för sig.

**Starta PostgreSQL** från projektets rotmapp:

```powershell
docker compose up -d postgres
```

**Starta backend** i en terminal från projektets rotmapp:

```powershell
cd backend\api
dotnet user-secrets set "Jwt:Key" "DIN_JWT_NYCKEL"
$env:AI_API_KEY = "DIN_OPENAI_API_NYCKEL"
dotnet run
```

Använd samma säkra JWT-nyckel som du har angett i `.env`.

Backend körs normalt på `http://localhost:5197`.

Backend använder `appsettings.json` för databasanslutningen och JWT-inställningarna.

**Viktigt:** Vid lokal körning måste databasuppgifterna i `appsettings.json` stämma med uppgifterna i `.env`. Annars kan backend inte ansluta till PostgreSQL.

**Starta frontend** i en ny terminal från projektets rotmapp:

```powershell
cd frontend
npm install
npm run dev
```

Öppna webbapplikationen på:

**http://localhost:5173**

Observera att `dotnet run` inte automatiskt läser `.env`. Därför anges JWT-nyckeln och OpenAI API-nyckeln separat vid lokal körning.

## Starta projektet nästa gång

**Med Docker**, från projektets rotmapp:

```powershell
docker compose up -d
```

Öppna `http://localhost:3000`.

**Vid lokal utveckling:** Starta PostgreSQL med Docker, backend med `dotnet run` och frontend med `npm run dev`. Se till att `AI_API_KEY` är tillgänglig i backendterminalen.

Du behöver normalt inte köra `npm install` igen. Databasmigrationerna körs automatiskt vid backendstart.

## Om systemet

Innovia Hub har fyra typer av bokningsbara resurser:

| Resurs | Antal |
|---|---:|
| Skrivbord | 15 |
| Mötesrum | 4 |
| VR-headset | 4 |
| AI-server | 1 |

Systemet använder JWT för autentisering och SignalR för att uppdatera bokningar i realtid.

Det finns två roller: `User` och `Admin`.

## Hubert – AI-assistent

Hubert använder OpenAI:s Responses API med modellen `gpt-5-mini`.

Med Hubert kan användare:

- Ställa frågor om resurser och tillgänglighet.
- Kontrollera vilka resurser som är lediga.
- Boka resurser efter inloggning och bekräftelse.
- Se sina egna bokningar när de är inloggade.
- Som administratör se alla användares bokningar.

Hubert tolkar användarens frågor, medan backend kontrollerar tillgänglighet, skapar bokningar och hanterar behörigheter.

För att Hubert ska fungera behövs en giltig OpenAI API-nyckel.

## Vanliga problem

**Docker startar inte**

Kontrollera att Docker Desktop är igång och kör:

```powershell
docker compose up -d --build
```

Visa loggarna vid behov:

```powershell
docker compose logs
```

**Databasen fungerar inte**

Kontrollera att PostgreSQL körs:

```powershell
docker compose ps
```

Kontrollera även databasuppgifterna i `.env` och, vid lokal körning, i `appsettings.json`.

**Hubert fungerar inte**

Kontrollera att `AI_API_KEY` är korrekt, att backend har tillgång till nyckeln och att OpenAI-kontot har API-åtkomst.

Om du ändrat `.env` vid Docker-körning kan du behöva återskapa containrarna:

```powershell
docker compose up -d --force-recreate
```

**Frontend startar inte lokalt**

Gå till `frontend` och kör:

```powershell
npm install
```

---

Dela aldrig API-nycklar, lösenord eller andra hemligheter på GitHub.