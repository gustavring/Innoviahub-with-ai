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

Kontrollera att PostgreSQL kör:

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

Hubert använder OpenAI API för att förstå användarens frågor och kommunicera med backend.

För att Hubert ska fungera behöver du en egen OpenAI API-nyckel.

1. Gå till https://platform.openai.com/api-keys
2. Skapa en API-nyckel.
3. Lägg till nyckeln i backend.

Från `backend\api`, kör:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "DIN_OPENAI_API_NYCKEL"
```

API-nyckeln sparas lokalt via .NET User Secrets och ska inte läggas upp på GitHub.

**Viktigt:** Detta förutsätter att backend är konfigurerad att läsa nyckeln från `OpenAI:ApiKey`. Om projektet använder ett annat konfigurationsnamn måste samma namn användas här.

Alternativt kan nyckeln anges som en miljövariabel i PowerShell:

```powershell
$env:OpenAI__ApiKey = "DIN_OPENAI_API_NYCKEL"
```

Denna miljövariabel gäller för den aktuella terminalsessionen. Starta backend från samma terminal.

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

Öppna en ny terminal och gå till frontend:

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

När databasen redan är konfigurerad behöver migrationerna, `npm install` och konfigurationen av API-nycklar normalt inte göras igen.

Starta PostgreSQL från projektets rot:

```powershell
docker compose up -d
```

Starta backend:

```powershell
cd backend\api
dotnet run
```

Starta frontend i en separat terminal:

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

Tillgänglighet beräknas utifrån befintliga bokningar och valt tidsintervall.

SignalR används för att uppdatera bokningsinformation i realtid. När en bokning skapas eller tas bort skickar backend eventet `BookingsChanged` till anslutna klienter.

Användare autentiseras med ASP.NET Core Identity och JWT. Systemet har rollerna `User` och `Admin`.

## Hubert – AI-assistent

Hubert är Innovia Hubs AI-assistent och använder OpenAI:s Responses API med function calling för att kommunicera med projektets befintliga backend.

Med Hubert kan användare:

- Ställa frågor om tillgängliga resurser.
- Kontrollera vilka resurser som är lediga eller upptagna.
- Boka resurser genom att först bekräfta bokningsförslaget.
- Se sina egna bokningar.
- Som administratör se alla användares bokningar.

Hubert använder befintliga services och repositories för att hämta information från databasen. AI:n tolkar användarens frågor, medan backend hanterar bokningar, tillgänglighet och behörighetskontroller.

För att använda Hubert krävs en konfigurerad OpenAI API-nyckel enligt steg 4.

## Vanliga problem

### Databastabeller saknas

Om backend ger:

```text
relation "AspNetRoles" does not exist
```

har migrationerna inte körts. Kör:

```powershell
cd backend\api
dotnet ef database update
```

### PostgreSQL kan inte nås

Om backend inte får kontakt med PostgreSQL, kontrollera Docker:

```powershell
docker compose ps
```

Starta databasen vid behov:

```powershell
docker compose up -d
```

### Frontend saknar dependencies

Kör:

```powershell
cd frontend
npm install
```

### Hubert fungerar inte

Kontrollera att:

- En giltig OpenAI API-nyckel har konfigurerats.
- Backend är igång.
- Backend läser API-nyckeln från rätt konfigurationsnamn.
- OpenAI-kontot har tillgång till API:t och tillräcklig budget.

Om du använder .NET User Secrets kan du kontrollera vilka konfigurationsnycklar som finns genom att köra:

```powershell
cd backend\api
dotnet user-secrets list
```

Dela aldrig din API-nyckel eller andra hemligheter i GitHub, felrapporter eller skärmbilder.
