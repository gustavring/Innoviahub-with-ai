import { useEffect, useState } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import type { HubConnection } from "@microsoft/signalr";
import SensorOverview from "./SensorOverview";

type Device = {
  id: string;
  serial: string;
  temperature?: number;
  co2?: number;
  updatedAt?: string;
  temperatureTime?: string;
  co2Time?: string;
};

type Measurement = {
  tenantSlug: string;
  deviceId: string;
  type: string;
  value: number;
  time: string;
};

const REGISTRY_URL = import.meta.env.VITE_IOT_REGISTRY_URL;
const TENANT_SLUG = import.meta.env.VITE_IOT_TENANT_SLUG;
const REALTIME_URL = import.meta.env.VITE_IOT_REALTIME_URL;
const IOT_ENABLED = import.meta.env.VITE_IOT_ENABLED !== "false";

const IOT_OFFLINE_MESSAGE =
  "IoT-enheterna är offline just nu. Övriga funktioner kan användas som vanligt.";

export default function SensorOverviewContainer() {
  const [devices, setDevices] = useState<Device[]>([]);
  const [loading, setLoading] = useState(IOT_ENABLED);
  const [error, setError] = useState("");
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    if (!IOT_ENABLED) return;

    const controller = new AbortController();
    let connection: HubConnection | undefined;

    async function joinTenant() {
      await connection!.invoke("JoinTenant", TENANT_SLUG);

      if (!controller.signal.aborted) {
        setConnected(true);
        setError("");
      }
    }

    async function fetchDevices() {
      try {
        if (!REGISTRY_URL || !TENANT_SLUG || !REALTIME_URL) {
          throw new Error("IoT-konfiguration saknas.");
        }

        const tenantResponse = await fetch(
          `${REGISTRY_URL}/api/tenants/by-slug/${encodeURIComponent(
            TENANT_SLUG,
          )}`,
          { signal: controller.signal },
        );

        if (!tenantResponse.ok) {
          throw new Error(
            `Kunde inte hämta tenant (${tenantResponse.status}).`,
          );
        }

        const tenant: { id: string } = await tenantResponse.json();

        const devicesResponse = await fetch(
          `${REGISTRY_URL}/api/tenants/${tenant.id}/devices`,
          { signal: controller.signal },
        );

        if (!devicesResponse.ok) {
          throw new Error(
            `Kunde inte hämta enheter (${devicesResponse.status}).`,
          );
        }

        const data: Device[] = await devicesResponse.json();

        if (controller.signal.aborted) return;

        setDevices(
          [...data].sort((a, b) =>
            a.serial.localeCompare(b.serial, "sv", {
              numeric: true,
            }),
          ),
        );
        setLoading(false);

        connection = new HubConnectionBuilder()
          .withUrl(`${REALTIME_URL}/hub/telemetry`)
          .withAutomaticReconnect()
          .build();

        connection.on("measurementReceived", (measurement: Measurement) => {
          if (
            controller.signal.aborted ||
            !measurement ||
            measurement.tenantSlug !== TENANT_SLUG ||
            !Number.isFinite(measurement.value) ||
            typeof measurement.time !== "string" ||
            !Number.isFinite(Date.parse(measurement.time)) ||
            (measurement.type !== "temperature" && measurement.type !== "co2")
          ) {
            return;
          }

          setDevices((current) =>
            current.map((device) => {
              if (device.id !== measurement.deviceId) return device;

              const previousTime = measurement.type === "temperature"
                ? device.temperatureTime
                : device.co2Time;

              if (
                previousTime &&
                Date.parse(measurement.time) < Date.parse(previousTime)
              ) {
                return device;
              }

              const values = measurement.type === "temperature"
                ? { temperature: measurement.value, temperatureTime: measurement.time }
                : { co2: measurement.value, co2Time: measurement.time };

              return {
                ...device,
                ...values,
                updatedAt: !device.updatedAt ||
                  Date.parse(measurement.time) > Date.parse(device.updatedAt)
                    ? measurement.time
                    : device.updatedAt,
              };
            }),
          );
        });

        connection.onreconnecting(() => {
          if (!controller.signal.aborted) setConnected(false);
        });

        connection.onreconnected(async () => {
          if (controller.signal.aborted) return;

          try {
            await joinTenant();
          } catch {
            if (!controller.signal.aborted) {
              setConnected(false);
              setError("Kunde inte återansluta till sensorgruppen. Ladda om sidan.");
            }
          }
        });

        connection.onclose(() => {
          if (!controller.signal.aborted) {
            setConnected(false);
            setError("Realtidsanslutningen är stängd. Kontrollera IoT-tjänsterna och ladda om sidan.");
          }
        });

        await connection.start();

        if (controller.signal.aborted) {
          await connection.stop();
          return;
        }

        await joinTenant();
      } catch (cause) {
        if (controller.signal.aborted) return;

        setError(
          cause instanceof Error
            ? cause.message
            : "Kunde inte ansluta till IoT-systemet.",
        );
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    void fetchDevices();

    return () => {
      controller.abort();
      if (connection) void connection.stop().catch(() => {});
    };
  }, []);

  return (
    <SensorOverview
      devices={devices}
      connected={connected}
      loading={loading}
      error={IOT_ENABLED ? error : IOT_OFFLINE_MESSAGE}
    />
  );
}
