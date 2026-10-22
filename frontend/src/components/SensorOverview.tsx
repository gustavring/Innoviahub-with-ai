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
  return (
    <section className={styles.panel}>
      <div className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Sensorer</p>
          <h2>Sensoröversikt</h2>
          <p className={styles.description}>
            {devices.length} registrerade enheter
          </p>
        </div>

        <span className={styles.connection} role="status">
          <span
            className={`${styles.dot} ${
              connected ? styles.connected : ""
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
        {devices.map((device) => (
          <article className={styles.card} key={device.id}>
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
        ))}
      </div>
    </section>
  );
}
