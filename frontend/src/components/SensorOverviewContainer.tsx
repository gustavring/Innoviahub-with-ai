import { useEffect, useState } from "react";
import SensorOverview from "./SensorOverview";

type Device = {
  id: string;
  serial: string;
};

const REGISTRY_URL = import.meta.env.VITE_IOT_REGISTRY_URL;
const TENANT_SLUG = import.meta.env.VITE_IOT_TENANT_SLUG;

export default function SensorOverviewContainer() {
  const [devices, setDevices] = useState<Device[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();

    async function fetchDevices() {
      try {
        if (!REGISTRY_URL || !TENANT_SLUG) {
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
      } catch (cause) {
        if (controller.signal.aborted) return;

        setError(
          cause instanceof Error
            ? cause.message
            : "Kunde inte hämta IoT-enheterna.",
        );
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    void fetchDevices();

    return () => controller.abort();
  }, []);

  return (
    <SensorOverview
      devices={devices}
      connected={false}
      loading={loading}
      error={error}
    />
  );
}
