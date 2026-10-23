# Implementation – IoT-koppling i Innovia Hub

## Vad jag har gjort

Jag har kopplat IoT-tjänsten till adminvyn för G-kraven.
De tio registrerade enheterna hämtas från DeviceRegistry.
Temperatur, CO₂ och senaste mättid uppdateras via SignalR.

IoT-projektets installation och ändringar beskrivs i dess
egen `Implementation.md`.

## Ändrade filer

- `frontend/.env.development`
  Adresser till DeviceRegistry och Realtime.Hub samt
  tenantens slug, `innovia`.

- `frontend/src/components/SensorOverviewContainer.tsx`
  Hämtar tenantens ID och enheter från DeviceRegistry.
  Ansluter till Realtime.Hub, anropar JoinTenant och
  lyssnar på measurementReceived.

- `frontend/src/components/SensorOverview.tsx`
  Visar enheterna och deras senaste mätvärden.

- `frontend/src/pages/AdminPage.tsx`
  Kopplar in sensoröversikten i adminvyn.

## Hur det fungerar

Enhetslistan hämtas via:

```text
GET /api/tenants/by-slug/innovia
GET /api/tenants/{tenantId}/devices
```

React ansluter sedan till `/hub/telemetry` och anropar
`JoinTenant("innovia")`.

När ett mätvärde kommer matchas meddelandets deviceId
mot enhetens GUID. Temperatur och CO₂ uppdateras separat
utan att sidan laddas om.

Klienten går med i tenantgruppen igen efter återanslutning.
Anslutningen stängs när komponenten lämnas.

## Körning och kontroller

IoT-tjänsterna startas enligt IoT-projektets dokumentation.
Innovia startas enligt projektets README, med frontend
via `npm run dev` på `http://localhost:5173`.

Logga in som admin och öppna adminvyn.

Jag har kontrollerat att:

- Alla tio enheter hämtas och visas.
- Temperatur och CO₂ uppdateras via SignalR.
- Alla tio får nya värden minst var tionde sekund.
- Reload och navigering tillbaka till adminvyn fungerar.

Vid sidladdning visas ”Väntar…” tills nästa mätvärde kommer.
Historiska värden hämtas inte till korten.

Integrationen är testad med lokal Vite-körning.
Dockerfrontend behöver motsvarande IoT-konfiguration
vid byggtillfället.
