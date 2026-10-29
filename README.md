# Innovia Hub

Innovia Hub är en webbapplikation för ett coworking- och forskningscenter. Här kan användare boka skrivbord, mötesrum, VR-headset och AI-server.

Det finns också en adminvy och AI-assistenten Hubert, som hjälper till med frågor om resurser, lediga tider och bokningar.

## Publicerad version

Applikationen är publicerad på DigitalOcean.

- Webbplats: https://64.225.99.232.nip.io
- GitHub: https://github.com/gustavring/Innoviahub-with-ai
- Branch för den publicerade versionen: `dev-deploy`

## Tekniker

- React, TypeScript och Vite
- ASP.NET Core Web API (.NET 8)
- PostgreSQL och Entity Framework Core
- ASP.NET Core Identity och JWT
- SignalR
- OpenAI API
- Docker Compose
- Caddy
- DigitalOcean

## Det jag har vidareutvecklat

Efter grupparbetet har jag arbetat vidare med:

- AI-assistenten Hubert som kan svara på frågor, kontrollera tillgänglighet och hjälpa till med bokningar.
- IoT-kopplingen i adminvyn. Tio enheter hämtas från DeviceRegistry och mätvärden uppdateras via SignalR.
- Ett offlineläge för IoT så att resten av sidan fungerar när IoT-tjänsten inte körs.
- Hämtningen av tider och bokningar för att göra flödet stabilare.
- Bokningslistor som visar högst fem bokningar och kan fällas ut med en pil.
- Möjligheten för admin att växla mellan aktuella och gamla bokningar.
- Behörighetskontroller för bokningshistoriken så att bara admin kan hämta den.
- Bekräftelse av lösenord vid registrering.
- Möjligheten för admin att ta bort användare med en bekräftelseruta. Användare med adminrollen kan inte tas bort.
- Styling för bland annat bokningslistor, popupfönster och IoT:s offlineläge.
- Konfiguration av adminkontot via miljövariabler.
- Docker-konfiguration och publicering på DigitalOcean med HTTPS via Caddy.

## Starta projektet lokalt

Du behöver Git och Docker Desktop. För att köra frontend med Vite behöver du även Node.js och npm.

### 1. Klona projektet

```powershell
git clone -b dev-deploy https://github.com/gustavring/Innoviahub-with-ai.git
cd Innoviahub-with-ai
```

### 2. Skapa .env

Kör från projektets rot:

```powershell
Copy-Item .env.example .env
```

Öppna `.env` och fyll i värdena enligt mallen.

För lokal körning av produktionsläget ska adresserna vara:

```dotenv
APP_DOMAIN=http://localhost
PUBLIC_API_URL=http://localhost
```

Du behöver även fylla i:

- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `JWT_KEY`
- `AI_API_KEY`
- `ADMIN_EMAIL`
- `ADMIN_PASSWORD`

JWT-nyckeln ska vara lång och slumpmässig, minst 32 byte. Adminlösenordet behöver uppfylla projektets lösenordskrav.

`.env` innehåller hemliga uppgifter och ska inte laddas upp till GitHub.

Adminkontot skapas när backend startar om kontot inte redan finns. Om kontot redan finns ändras inte dess lösenord genom att ändra `.env`.

Vid lokal körning loggar du in som admin med uppgifterna från `ADMIN_EMAIL` och `ADMIN_PASSWORD`. Övriga användarkonton skapas sedan i adminvyn.

### 3. Starta med samma upplägg som den publicerade sidan

Se till att Docker Desktop är igång. Kör från projektets rot:

```powershell
docker compose -f docker-compose.production.yml up -d --build
```

Öppna:

http://localhost

Detta startar PostgreSQL, backend, frontend och Caddy. Databasmigrationerna körs automatiskt när backend startar.

IoT är avstängt i detta läge och visas som offline.

För att kontrollera tjänsterna:

```powershell
docker compose -f docker-compose.production.yml ps
```

För att stoppa:

```powershell
docker compose -f docker-compose.production.yml down
```

### Alternativ: Vanlig Docker-körning

Stoppa först produktionsläget om det körs.

Kör sedan från projektets rot:

```powershell
docker compose up -d --build
```

Öppna:

http://localhost:3000

Backend och Swagger finns på:

http://localhost:5197/swagger

För att stoppa:

```powershell
docker compose down
```

### Alternativ: Frontend med Vite

Detta läge kan användas när du utvecklar frontend eller vill testa den lokala IoT-kopplingen.

Stoppa först det Docker-läge som körs. Starta sedan backend och databasen från projektets rot:

```powershell
docker compose up -d --build api
```

Starta frontend i en terminal:

```powershell
cd frontend
npm ci
npm run dev
```

Öppna:

http://localhost:5173

IoT-tjänsterna behöver startas separat för att sensorvärden ska visas.

Kör ett av de lokala uppläggen åt gången. Använd inte `down -v` om du vill behålla innehållet i databasen.

## Så testar du sidan

Öppna:

https://64.225.99.232.nip.io

Inloggningsuppgifter för att testa som vanlig användare och admin lämnas separat vid inlämningen. Det går inte att registrera sig själv som besökare. Bara admin kan skapa användarkonton.

### Bokning

1. Logga in.
2. Välj en resurs, ett datum och en ledig tid.
3. Genomför bokningen.
4. Kontrollera att bokningen visas i bokningslistan.
5. Prova att ta bort bokningen.

Om det finns fler än fem aktuella bokningar visas en pil längst ned. Den visar resten av bokningarna och kan sedan användas för att fälla ihop listan igen.

### Realtidsuppdateringar

Öppna sidan i två olika webbläsare eller använd ett privat fönster. Logga in med två olika konton.

Skapa eller ta bort en bokning i det ena fönstret. Tillgängligheten ska uppdateras i det andra utan att sidan laddas om.

### Hubert

Klicka på Hubert för att öppna chatten.

Du kan exempelvis fråga:

- ”Vilka resurser finns?”
- ”Finns det ett ledigt mötesrum i morgon klockan 10?”
- ”Visa mina bokningar.”

Hubert kan också hjälpa till att boka en ledig resurs. Bokningen behöver bekräftas innan den genomförs.

Hubert behöver en giltig OpenAI API-nyckel och tillgång till API:t.

### Admin

Som admin kan du:

- Se användarnas bokningar.
- Växla mellan aktuella och gamla bokningar.
- Fälla ut bokningslistan om det finns fler än fem bokningar.
- Registrera användare.
- Ta bort användare genom att bekräfta i popupfönstret.
- Se sensoröversikten, endast lokalt.

Användare med adminrollen har ingen knapp för att tas bort. Detta skyddas även i backend.

### Registrera användare

1. Logga in som admin och öppna adminvyn.
2. Klicka på **Registrera användare**.
3. Fyll i användarens e-postadress och lösenord.
4. Skriv samma lösenord i **Bekräfta lösenord**.
5. Klicka på **Registrera**. När kontot har skapats visas ett meddelande.
6. Klicka på **Tillbaka till admin** för att gå tillbaka till adminvyn.

Om lösenorden inte matchar visas ett fel och kontot skapas inte. Lösenordet behöver också uppfylla projektets lösenordskrav.

Det nya kontot kan sedan logga in med e-postadressen och lösenordet som admin angav. Registreringen ger inte kontot adminbehörighet. Bara admin får använda registreringen, vilket kontrolleras även i backend.

## IoT

Lokalt kan plattformen kopplas till den separata IoT-tjänsten.

Tio enheter hämtas från DeviceRegistry. Temperatur, CO₂ och senaste mättid uppdateras via SignalR.

IoT-servern är inte publicerad. I den publicerade versionen visas därför att sensorerna är offline. Resten av plattformen kan fortfarande användas.

Mer information om kopplingen finns i `Implementation.md`. Installation och start av IoT-tjänsterna beskrivs i IoT-projektets dokumentation.

## Driftsättning

Applikationen körs på en Ubuntu-server hos DigitalOcean.

Docker Compose startar PostgreSQL, backend, frontend och Caddy. Caddy hanterar HTTPS och skickar anropen vidare till rätt tjänst.

Servern använder branchen `dev-deploy`. När koden uppdateras hämtas ändringarna från GitHub och de ändrade tjänsterna byggs om.

Serverns miljövariabler ligger i en separat `.env` och finns inte på GitHub.

Databasen sparas i en Docker-volym så att innehållet finns kvar när containrarna byggs om.

## Kända problem och begränsningar

- Hämtningen och visningen av lediga tider kan ibland kännas lite buggig. Jag har gjort förbättringar i hämtningen, men den delen behöver fortfarande förbättras.
- När en resurstyp är vald gör kalendern cirka 119 tillgänglighetsanrop för en vecka. Hämtningen kan köras igen när dina bokningar har hämtats, vilket kan göra laddningen långsam.
- Hämtningsfel kan göra att kalendern visas tom.
- När admin tar bort en användare tas kontot bort, men en redan utfärdad inloggningstoken spärras inte direkt. Den kan fortfarande godtas tills den går ut, normalt efter två timmar från inloggningen.
- Knappen **Lås en resurs** finns i adminvyn men har ännu ingen funktion.
