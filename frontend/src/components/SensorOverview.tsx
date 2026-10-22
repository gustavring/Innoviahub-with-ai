import { useEffect, useState } from "react";
import styles from "./css/SensorOverview.module.css";

type SensorDevice = {
  id: string;
  serial: string;
  temperature?: number;
  co2?: number;
  updatedAt?: string;
};

type SensorOverviewProps = {
  devices: SensorDevice[];
  connected: boolean;
  loading?: boolean;
  error?: string;
};

export default function SensorOverview({
  devices,
  connected,
  loading = false,
  error = "",
}: SensorOverviewProps) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const interval = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(interval);
  }, []);

  return (
    <section className={styles.panel}>
      <div className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Sensorer</p>
          <p className={styles.description}>
            {devices.length} registrerade enheter
          </p>
        </div>

        <span className={styles.connection} role="status">
          <span
            className={`${styles.dot} ${
              connected ? styles.active : styles.disconnected
            }`}
          />
          {connected ? "Realtid ansluten" : "Realtid frånkopplad"}
        </span>
      </div>

      {loading && (
        <p className={styles.message}>Hämtar enheter...</p>
      )}

      {error && (
        <p className={styles.error} role="alert">
          {error}
        </p>
      )}

      {!loading && !error && devices.length === 0 && (
        <p className={styles.message}>Inga enheter hittades.</p>
      )}

      <div className={styles.grid}>
        {devices.map((device) => {
          const age = device.updatedAt
            ? now - Date.parse(device.updatedAt)
            : Infinity;
          const isFresh = connected && age >= 0 && age < 30_000;

          return (
          <article className={styles.card} key={device.id}>
            <span
              className={`${styles.deviceDot} ${
                isFresh ? styles.active : styles.waiting
              }`}
              role="img"
              aria-label={
                isFresh
                  ? "Enheten skickar aktuella mätvärden"
                  : "Väntar på aktuella mätvärden"
              }
            />
            <h3>{device.serial}</h3>

            <dl className={styles.metrics}>
              <div>
                <dt>Temperatur</dt>
                <dd>
                  {device.temperature !== undefined
                    ? `${device.temperature.toLocaleString("sv-SE", {
                        maximumFractionDigits: 1,
                      })} °C`
                    : "Väntar…"}
                </dd>
              </div>

              <div>
                <dt>CO₂</dt>
                <dd>
                  {device.co2 !== undefined
                    ? `${device.co2.toLocaleString("sv-SE", {
                        maximumFractionDigits: 0,
                      })} ppm`
                    : "Väntar…"}
                </dd>
              </div>
            </dl>

            <p className={styles.updated}>
              {device.updatedAt
                ? `Senaste mätning ${new Date(
                    device.updatedAt,
                  ).toLocaleTimeString("sv-SE", {
                    hour: "2-digit",
                    minute: "2-digit",
                    second: "2-digit",
                  })}`
                : "Väntar på första mätningen"}
            </p>
          </article>
          );
        })}
      </div>
    </section>
  );
}
