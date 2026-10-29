import { useEffect, useMemo, useState } from "react";
import styles from "./css/Bookings.module.css";
import * as signalR from "@microsoft/signalr";

const API_URL = import.meta.env.VITE_API_URL;

type Booking = {
  bookingId: number;
  startTime: string;
  endTime: string;
  resourceId: number;
  resourceType: string;
  userId: string;
  userEmail: string;
};

export default function AdminBookings() {
  const [bookings, setBookings] = useState<Booking[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showAll, setShowAll] = useState(false);
  const [showPassed, setShowPassed] = useState(false);

  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  const passedBookings = useMemo(
    () => bookings.filter((b) => Date.parse(b.endTime) <= now)
      .sort((a, b) => Date.parse(b.endTime) - Date.parse(a.endTime)),
    [bookings, now],
  );
  const activeBookings = useMemo(
    () => bookings.filter((b) => Date.parse(b.endTime) > now)
      .sort((a, b) => Date.parse(a.startTime) - Date.parse(b.startTime)),
    [bookings, now],
  );

  useEffect(() => {
    async function getBookings() {
      try {
        setLoading(true);
        setError("");

        const token = sessionStorage.getItem("token");

        if (!token) {
          throw new Error("Ingen inloggningstoken hittades.");
        }

        const response = await fetch(`${API_URL}/api/Bookings`, {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        console.log("GET /api/Bookings status:", response.status);

        if (response.status === 401) {
          throw new Error("Du är inte inloggad eller token har gått ut.");
        }

        if (response.status === 403) {
          throw new Error("Du har inte behörighet att läsa bokningarna.");
        }

        if (!response.ok) {
          throw new Error(
            `Kunde inte hämta bokningar. Status: ${response.status}`,
          );
        }

        const data: Booking[] = await response.json();

        console.log("Bokningar från backend:", data);

        setBookings(data);
      } catch (error) {
        console.error("Fel vid hämtning av bokningar:", error);

        if (error instanceof Error) {
          setError(error.message);
        } else {
          setError("Ett okänt fel uppstod.");
        }
      } finally {
        setLoading(false);
      }
    }

    getBookings();

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/Hubs/Booking`)
      .withAutomaticReconnect()
      .build();

    connection.on("BookingsChanged", () => {
      getBookings();
    });

    connection
      .start()
      .then(() => {
        console.log("SignalR ansluten!");
      })
      .catch((error) => {
        if (
          error instanceof Error &&
          error.message.includes("stopped during negotiation")
        ) {
          return;
        }

        console.error("SignalR-fel:", error);
      });

    return () => {
      connection.stop();
    };
  }, []);

  function formatDate(dateString: string) {
    return new Date(dateString).toLocaleDateString("sv-SE");
  }

  function formatTime(dateString: string, isEndTime = false) {
    const time = new Date(dateString).toLocaleTimeString("sv-SE", {
      timeZone: "Europe/Stockholm",
      hour: "2-digit",
      minute: "2-digit",
      hourCycle: "h23",
    });

    return isEndTime && time === "00:00" ? "24:00" : time;
  }

  const deleteBooking = async (id: number) => {
    const token = sessionStorage.getItem("token");

    const response = await fetch(`${API_URL}/api/Bookings/${id}`, {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });
    if (response.ok) {
      setBookings((current) => current.filter((booking) => booking.bookingId !== id));
    }
  };

  const selectedBookings = showPassed ? passedBookings : activeBookings;
  const visibleBookings = showAll ? selectedBookings : selectedBookings.slice(0, 5);

  return (
    <section id="bookings" className={styles.bookingsWrapper}>
      <div className={styles.heading}>
        <div className={styles.headerAndButton}>
          <p className={styles.eyebrow}>Bokningar</p>

          <div className={styles.buttonGroup}>
            <button type="button" aria-pressed={showPassed} onClick={() => {
              setNow(Date.now());
              setShowPassed((current) => !current);
              setShowAll(false);
            }}>
              {showPassed
                ? "Visa aktuella bokningar"
                : "Visa gamla bokningar"}
            </button>

          </div>
        </div>

        <div className={styles.info}>
          <div className={styles.resource}>Resurs</div>
          <div className={styles.user}>Användare</div>
          <div className={styles.date}>Datum</div>
          <div className={styles.time}>Tid</div>
        </div>
      </div>

      {loading && (
        <div className={styles.placeholder}>
          <p>Hämtar bokningar...</p>
        </div>
      )}

      {!loading && error && (
        <div className={styles.placeholder}>
          <p>{error}</p>
        </div>
      )}

      {!loading && !error && selectedBookings.length === 0 && (
        <div className={styles.placeholder}>
          <p>{showPassed ? "Inga gamla bokningar" : "Inga aktuella bokningar"}</p>
        </div>
      )}

      {!loading && !error && selectedBookings.length > 0 && (
        <div id="admin-booking-list" className={styles.bookingList}>
          {visibleBookings.map((booking) => (
            <div key={booking.bookingId} className={styles.bookingRow}>
              <div className={styles.resource}>{booking.resourceType}</div>

              <div className={styles.user}>{booking.userEmail}</div>

              <div className={styles.date}>{formatDate(booking.startTime)}</div>

              <div className={styles.time}>
                {formatTime(booking.startTime)}
                {" – "}
                {formatTime(booking.endTime, true)}
              </div>
              <button
                className={styles.deleteButton}
                onClick={() => deleteBooking(booking.bookingId)}
              >
                Ta bort
              </button>
            </div>
          ))}
        </div>
      )}
      {!loading && !error && selectedBookings.length > 5 && (
        <button type="button" className={styles.expandButton}
          aria-expanded={showAll} aria-controls="admin-booking-list"
          aria-label={showAll ? "Visa färre bokningar" : "Visa alla bokningar"}
          onClick={() => setShowAll((current) => !current)}>
          <span aria-hidden="true" className={`${styles.chevron} ${showAll ? styles.chevronUp : ""}`} />
        </button>
      )}
    </section>
  );
}
