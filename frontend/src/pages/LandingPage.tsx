import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import Navbar from "../components/Navbar";
import styles from "./css/LandingPage.module.css";
import Calendar from "../components/Calendar";
import TimeSlots from "../components/TimeSlots";
import Resources from "../components/Resources";
import Bookings from "../components/Bookings";
import Hubert from "../components/Hubert";

const API_URL = import.meta.env.VITE_API_URL;

type TimeSlot = {
  startTime: string;
  endTime: string;
  isAvailable: boolean;
  status: "green" | "yellow" | "red" | "blue" | "locked";
};

export default function LandingPage() {

  const [selectedDate, setSelectedDate] = useState<Date>();

  const [selectedResourceType, setSelectedResourceType] = useState<
    string | null
  >(null);

  const [selectedResourceId, setSelectedResourceId] = useState<number | null>(
    null,
  );

  const [selectedSlot, setSelectedSlot] = useState<TimeSlot | null>(null);

  const [bookingLoading, setBookingLoading] = useState(false);
  const [bookingMessage, setBookingMessage] = useState("");
  const [bookingError, setBookingError] = useState("");
  const [bookingsRefreshKey, setBookingsRefreshKey] = useState(0);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/Hubs/Booking`)
      .withAutomaticReconnect()
      .build();

    connection.on("BookingsChanged", () => {
      console.log("BookingsChanged mottaget i LandingPage");

      setBookingsRefreshKey((current) => current + 1);
    });

    connection
      .start()
      .then(() => {
        console.log("SignalR ansluten i LandingPage!");
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

  async function handleBooking() {
    if (selectedResourceType === null || !selectedSlot) {
      setBookingError("Välj en resurs och en tid först.");
      return;
    }

    const token = localStorage.getItem("token");

    if (!token) {
      setBookingError("Du måste vara inloggad för att boka.");
      return;
    }

    setBookingLoading(true);
    setBookingError("");
    setBookingMessage("");

    try {
      /*
       * BookingController tolkar inkommande tider som svensk lokal tid.
       * Därför skickar vi tiderna utan Z/UTC-offset.
       */
      const start = new Date(selectedSlot.startTime);
      const end = new Date(selectedSlot.endTime);

      const toSwedishLocalDateTime = (date: Date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        const hours = String(date.getHours()).padStart(2, "0");
        const minutes = String(date.getMinutes()).padStart(2, "0");
        const seconds = String(date.getSeconds()).padStart(2, "0");

        return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
      };

      const bookingData =
        selectedResourceId !== null
          ? {
              resourceId: selectedResourceId,
              startTime: toSwedishLocalDateTime(start),
              endTime: toSwedishLocalDateTime(end),
            }
          : {
              resourceType: selectedResourceType,
              startTime: toSwedishLocalDateTime(start),
              endTime: toSwedishLocalDateTime(end),
            };

      const bookingUrl =
        selectedResourceId !== null
          ? `${API_URL}/api/Bookings`
          : `${API_URL}/api/Bookings/automatic`;

      const response = await fetch(bookingUrl, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(bookingData),
      });

      if (response.status === 401) {
        throw new Error(
          "Du är inte inloggad eller din inloggning har gått ut.",
        );
      }

      if (response.status === 409) {
        throw new Error("Tiden är redan bokad. Välj en annan tid.");
      }

      if (!response.ok) {
        const message = await response.text();

        throw new Error(message || "Bokningen kunde inte genomföras.");
      }

      setBookingMessage("Bokningen är genomförd!");

      setSelectedSlot(null);
    } catch (error) {
      console.error("Fel vid bokning:", error);

      if (error instanceof Error) {
        setBookingError(error.message);
      } else {
        setBookingError("Ett okänt fel uppstod.");
      }
    } finally {
      setBookingLoading(false);
    }
  }

  return (
    <>
      <Navbar />

      <main className={styles.landingPage}>
        <div className={styles.resourcesAndHubertWrapper}>
          <Resources
            selectedResourceType={selectedResourceType}
            onResourceTypeSelect={(resourceType) => {
              setSelectedResourceType(resourceType);
              setSelectedResourceId(null);
              setSelectedSlot(null);
              setBookingMessage("");
              setBookingError("");
            }}
          />

          <Hubert />
        </div>

        <div className={styles.bookingCalendarWrapper}>
          {!selectedDate ? (
            <Calendar
              selectedDate={selectedDate}
              selectedResourceType={selectedResourceType}
              refreshKey={bookingsRefreshKey}
              onDateSelect={(date) => {
                setSelectedDate(date);
                setSelectedSlot(null);
                setBookingMessage("");
                setBookingError("");
              }}
            />
          ) : (
            <div className={styles.timeSlotsView}>
              <div className={styles.selectedDate}>
                <p>Valt datum</p>

                <h2>
                  {selectedDate.toLocaleDateString("sv-SE", {
                    weekday: "long",
                    day: "numeric",
                    month: "long",
                  })}
                </h2>

                <button
                  type="button"
                  className={styles.backButton}
                  onClick={() => {
                    setSelectedDate(undefined);
                    setSelectedSlot(null);
                  }}
                >
                  ← Tillbaka till kalendern
                </button>
              </div>

              <TimeSlots
                selectedDate={selectedDate}
                selectedResourceType={selectedResourceType}
                selectedResourceId={selectedResourceId}
                onResourceSelect={setSelectedResourceId}
                selectedSlot={selectedSlot}
                onSlotSelect={setSelectedSlot}
                refreshKey={bookingsRefreshKey}
              />
            </div>
          )}

          {selectedSlot && (
            <section className={styles.bookingAction}>
              <p>
                Vald tid:{" "}
                {new Date(selectedSlot.startTime).toLocaleTimeString("sv-SE", {
                  hour: "2-digit",
                  minute: "2-digit",
                })}
                {" – "}
                {new Date(selectedSlot.endTime).toLocaleTimeString("sv-SE", {
                  hour: "2-digit",
                  minute: "2-digit",
                })}
              </p>

              <button
                type="button"
                onClick={handleBooking}
                disabled={bookingLoading}
              >
                {bookingLoading ? "Bokar..." : "Boka tid"}
              </button>
            </section>
          )}

          {bookingMessage && (
            <div className={styles.successMessage}>{bookingMessage}</div>
          )}

          {bookingError && (
            <div className={styles.errorMessage}>{bookingError}</div>
          )}

          <Bookings key={bookingsRefreshKey} />
        </div>
      </main>
    </>
  );
}
