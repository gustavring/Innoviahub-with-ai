# Innovia Hub

Innovia Hub är en webbapplikation för ett coworking- och forskningscenter där användare kan boka resurser och se deras tillgänglighet.

Projektet består av:

- **Backend:** ASP.NET Core Web API (.NET 8)
- **Frontend:** React + TypeScript + Vite
- **Databas:** PostgreSQL
- **Realtidskommunikation:** SignalR
- **Autentisering:** ASP.NET Core Identity + JWT
- **Databasåtkomst:** Entity Framework Core
- **AI-assistent:** Hubert, byggd med OpenAI API
- **Docker:** PostgreSQL körs lokalt i Docker

## Kom igång

### 1. Klona `dev`

Klona projektets `dev`-branch:

```powershell
git clone -b dev https://github.com/Innovia-3/Innovia-3.git
cd Innovia-3
```

### 2. Starta PostgreSQL

Se till att Docker Desktop är startat.

Kör sedan från projektets rotmapp:

```powershell
docker compose up -d
```

Kontrollera att PostgreSQL körs:

```powershell
docker compose ps
```

Containern `innoviahub-postgres` ska ha status `Up`.

### 3. Konfigurera JWT

Gå till backend:

```powershell
cd backend\api
```

Projektet använder .NET User Secrets för JWT-nyckeln:

```powershell
dotnet user-secrets set "Jwt:Key" "DIN_JWT_NYCKEL"
```

JWT-nyckeln för projektet tillhandahålls separat.

### 4. Konfigurera OpenAI API-nyckel för Hubert

Hubert använder OpenAI:s Responses API med modellen `gpt-5-mini` för att tolka användarens frågor och använda funktioner i projektets backend.

För att Hubert ska fungera behöver du en OpenAI API-nyckel.

1. Gå till https://platform.openai.com/api-keys och skapa en API-nyckel.
2. I projektets rotmapp finns en fil som heter `.env.example`. Kopiera filen och döp kopian till `.env`.
3. Öppna `.env` och ersätt platshållarna med dina egna värden.
4. Lägg in din OpenAI API-nyckel vid `AI_API_KEY`:

```dotenv
AI_API_KEY=din_openai_api_nyckel
```

Fyll även i de övriga miljövariablerna för JWT och PostgreSQL enligt projektets konfiguration.

**Viktigt:** `.env` innehåller känsliga uppgifter och ska inte laddas upp till GitHub.

Observera att miljövariablerna måste vara tillgängliga för backend när den startas. ASP.NET Core läser inte automatiskt in en `.env`-fil vid `dotnet run`, så projektet måste ha stöd för detta eller få variablerna via startmiljön.

OpenAI API kan medföra kostnader beroende på användning.

### 5. Uppdatera databasen

På en ny databas behöver EF Core-migrationerna köras:

```powershell
dotnet ef database update
```

Detta skapar databastabellerna och lägger in projektets seedade data.

### 6. Starta backend

Från `backend\api`:

```powershell
dotnet run
```

Backend körs på:

```text
http://localhost:5197
```

Låt terminalen vara igång.

### 7. Starta frontend

Öppna en ny terminal i projektets rotmapp och gå till frontend:

```powershell
cd frontend
npm install
npm run dev
```

Frontend körs på:

```text
http://localhost:5173
```

Öppna adressen i webbläsaren.

## Starta projektet efter första installationen

När databasen och miljövariablerna redan är konfigurerade behöver migrationerna och `npm install` normalt inte köras igen.

Starta PostgreSQL från projektets rotmapp:

```powershell
docker compose up -d
```

Starta backend:

```powershell
cd backend\api
dotnet run
```

Starta frontend i en separat terminal från projektets rotmapp:

```powershell
cd frontend
npm run dev
```

## Kort om systemet

Innovia Hub hanterar fyra typer av bokningsbara resurser:

| Resurs | Antal |
|---|---:|
| Skrivbord | 15 |
| Mötesrum | 4 |
| VR-headset | 4 |
| AI-server | 1 |

Tillgängligheten beräknas utifrån befintliga bokningar och valt tidsintervall.

SignalR används för att uppdatera bokningsinformation i realtid. När en bokning skapas eller tas bort skickar backend eventet `BookingsChanged` till anslutna klienter.

Användare autentiseras med ASP.NET Core Identity och JWT. Systemet har rollerna `User` och `Admin`.

## Hubert – AI-assistent

Hubert är Innovia Hubs AI-assistent och använder OpenAI:s Responses API med function calling för att kommunicera med projektets befintliga backend.

Med Hubert kan användare:

- Ställa frågor om resurser och tillgänglighet.
- Kontrollera vilka resurser som är lediga eller upptagna.
- Boka resurser efter inloggning och bekräftelse.
- Se sina egna bokningar när de är inloggade.
- Som administratör se alla användares bokningar.

Hubert använder befintliga services och repositories för att hämta information och hantera bokningar. AI:n tolkar användarens frågor, medan backend ansvarar för tillgänglighet, bokningar och behörighetskontroller.

För att använda Hubert krävs en giltig OpenAI API-nyckel enligt steg 4.

## Vanliga problem

### Databastabeller saknas

Om backend visar följande fel:

```text
relation "AspNetRoles" does not exist
```

har migrationerna troligen inte körts. Kör:

```powershell
cd backend\api
dotnet ef database update
```

### PostgreSQL kan inte nås

Om backend inte får kontakt med PostgreSQL, kontrollera att Docker är igång:

```powershell
docker compose ps
```

Starta databasen vid behov:

```powershell
docker compose up -d
```

### Frontend saknar dependencies

Om frontend inte startar på grund av saknade paket, kör:

```powershell
cd frontend
npm install
```

### Hubert fungerar inte

Kontrollera att:

- Du har lagt in en giltig OpenAI API-nyckel i `AI_API_KEY`.
- Miljövariabeln är tillgänglig för backend.
- Backend är igång.
- OpenAI-kontot har tillgång till API:t och tillräcklig budget.

Om du har ändrat `.env` kan du behöva starta om berörda tjänster för att ändringarna ska börja gälla.

Dela aldrig API-nycklar, lösenord eller andra hemligheter på GitHub, i felrapporter eller i skärmbilder.