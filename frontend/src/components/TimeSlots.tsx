import { useEffect, useState } from "react";
import styles from "./css/TimeSlots.module.css";

const API_URL = import.meta.env.VITE_API_URL;

type TimeSlot = {
  startTime: string;
  endTime: string;
  isAvailable: boolean;
  status: "green" | "yellow" | "red" | "blue" | "locked";
};

type TimeSlotsProps = {
  selectedDate?: Date;
  selectedResourceType: string | null;
  selectedResourceId: number | null;
  onResourceSelect: (resourceId: number | null) => void;
  onSlotSelect: (slot: TimeSlot | null) => void;
  selectedSlot: TimeSlot | null;
  overview?: boolean;
  refreshKey?: number;
};

type Resource = {
  resourceId: number;
  resourceType: string;
};

type Booking = {
  bookingId: number;
  resourceId: number;
  resourceType: string;
  startTime: string;
  endTime: string;
};

const SLOT_START_HOUR = 1;
const SLOT_END_HOUR = 24;
const BOOKING_START_HOUR = 7;
const BOOKING_END_HOUR = 24;

function createSlots(date: Date): TimeSlot[] {
  const slots: TimeSlot[] = [];

  for (let hour = SLOT_START_HOUR; hour < SLOT_END_HOUR; hour++) {
    const start = new Date(date);
    start.setHours(hour, 0, 0, 0);

    const end = new Date(date);
    end.setHours(hour + 1, 0, 0, 0);

    slots.push({
      startTime: start.toISOString(),
      endTime: end.toISOString(),
      isAvailable: false,
      status: "locked",
    });
  }

  return slots;
}

export default function TimeSlots({
  selectedDate,
  selectedResourceType,
  onResourceSelect,
  selectedResourceId,
  onSlotSelect,
  selectedSlot,
  overview = false,
  refreshKey = 0,
}: TimeSlotsProps) {
  const [slots, setSlots] = useState<TimeSlot[]>([]);
  const [resources, setResources] = useState<Resource[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [selectedStart, setSelectedStart] = useState<TimeSlot | null>(null);
  const [chooseSpecificResource, setChooseSpecificResource] = useState(false);
  const [myBookings, setMyBookings] = useState<Booking[]>([]);

  useEffect(() => {
    const controller = new AbortController();

    async function fetchResources() {
      if (selectedResourceType === null) {
        setResources([]);
        return;
      }

      try {
        const response = await fetch(
          `${API_URL}/api/Resources/types/${selectedResourceType}`,
          { signal: controller.signal },
        );

        if (!response.ok) {
          throw new Error("Kunde inte hämta resurser.");
        }

        const data: Resource[] = await response.json();
        if (controller.signal.aborted) return;
        setResources(data);
      } catch (error) {
        if (controller.signal.aborted) return;
        console.error("Kunde inte hämta resurs-ID:n:", error);
      }
    }

    fetchResources();
    return () => controller.abort();
  }, [selectedResourceType]);

  useEffect(() => {
    const controller = new AbortController();

    async function fetchMyBookings() {
      const token = sessionStorage.getItem("token");

      if (!token) {
        setMyBookings([]);
        return;
      }

      try {
        const response = await fetch(`${API_URL}/api/Bookings/mine`, {
          signal: controller.signal,
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          throw new Error("Kunde inte hämta mina bokningar.");
        }

        const data: Booking[] = await response.json();
        if (controller.signal.aborted) return;
        setMyBookings(data);
      } catch (error) {
        if (controller.signal.aborted) return;
        console.error("Kunde inte hämta mina bokningar:", error);
      }
    }

    fetchMyBookings();
    return () => controller.abort();
  }, [refreshKey]);

  useEffect(() => {
    setSelectedStart(null);

    if (!overview) {
      onSlotSelect(null);
    }
  }, [selectedDate, selectedResourceId, selectedResourceType, overview, onSlotSelect]);

  useEffect(() => {
    const controller = new AbortController();

    async function checkAvailability() {
      setError("");

      if (!selectedDate) {
        setSlots([]);
        setLoading(false);
        return;
      }

      if (selectedResourceType === null) {
        if (overview) {
          setSlots(createSlots(selectedDate));
        } else {
          setSlots([]);
        }

        setLoading(false);
        return;
      }

      setLoading(true);

      try {
        const newSlots = createSlots(selectedDate);

        const availabilityResults = await Promise.all(
          newSlots.map(async (slot) => {
            const start = new Date(slot.startTime);
            const hour = start.getHours();

            // Tider utanför bokningsfönstret visas,
            // men kan inte bokas.
            if (hour < BOOKING_START_HOUR || hour >= BOOKING_END_HOUR) {
              return {
                ...slot,
                isAvailable: false,
                status: "locked" as const,
              };
            }

            const end = new Date(slot.endTime);

            const isMyBooking = myBookings.some((booking) => {
              const bookingStart = new Date(booking.startTime);
              const bookingEnd = new Date(booking.endTime);

              const sameTime = start < bookingEnd && end > bookingStart;

              const sameResource =
                selectedResourceId !== null
                  ? booking.resourceId === selectedResourceId
                  : booking.resourceType === selectedResourceType;

              return sameTime && sameResource;
            });

            const params = new URLSearchParams({
              startTime: start.toISOString(),
              endTime: end.toISOString(),
            });

            let response;

            if (selectedResourceId !== null) {
              /* användaren har valt ett specifikt ID */
              response = await fetch(
                `${API_URL}/api/Resources/${selectedResourceId}/availability?${params.toString()}`,
                { signal: controller.signal },
              );
            } else {
              /* första lediga -> kontrollera hela resurstypen */
              response = await fetch(
                `${API_URL}/api/Resources/types/${selectedResourceType}/availability?${params.toString()}`,
                { signal: controller.signal },
              );
            }

            if (!response.ok) {
              throw new Error("Kunde inte kontrollera tillgänglighet.");
            }

            const data = await response.json();

            console.log(
              "Availability:",
              selectedResourceType,
              selectedResourceId,
              data,
            );

            if (isMyBooking) {
              return {
                ...slot,
                isAvailable: false,
                status: "blue" as const,
              };
            }

            if (selectedResourceId !== null) {
              return {
                ...slot,
                isAvailable: data.isAvailable,
                status: data.isAvailable
                  ? ("green" as const)
                  : ("red" as const),
              };
            }

            let status: "green" | "yellow" | "red";

            if (data.availableResources === 0) {
              status = "red";
            } else if (data.availableResources < data.totalResources) {
              status = "yellow";
            } else {
              status = "green";
            }

            return {
              ...slot,
              isAvailable: data.availableResources > 0,
              status,
            };
          }),
        );

        if (controller.signal.aborted) return;
        setSlots(availabilityResults);
      } catch (error) {
        if (controller.signal.aborted) return;
        console.error("Fel vid kontroll av tillgänglighet:", error);
        setError("Kunde inte hämta lediga tider.");
        setSlots([]);
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    }

    checkAvailability();
    return () => controller.abort();
  }, [
    selectedDate,
    selectedResourceId,
    selectedResourceType,
    overview,
    refreshKey,
    myBookings,
  ]);

  function formatTime(dateString: string) {
    return new Date(dateString).toLocaleTimeString("sv-SE", {
      hour: "2-digit",
      minute: "2-digit",
    });
  }

  function isEndTime(time: string) {
    const lastSlot = slots[slots.length - 1];
    return lastSlot !== undefined && time === lastSlot.endTime;
  }

  function handleTimeClick(time: string) {
    const selectedTime = new Date(time);
    const selectedHour = selectedTime.getHours();

    /* första klicket = starttid */
    if (!selectedStart) {
      const startSlot = slots.find((slot) => slot.startTime === time);

      if (
        !startSlot ||
        !startSlot.isAvailable ||
        selectedHour < BOOKING_START_HOUR ||
        selectedHour >= BOOKING_END_HOUR
      ) {
        return;
      }

      setSelectedStart(startSlot);
      setError("");
      onSlotSelect(null);
      return;
    }

    /* klick på samma starttid igen = avmarkera */
    if (selectedStart.startTime === time) {
      setSelectedStart(null);
      onSlotSelect(null);
      setError("");
      return;
    }

    /* sluttiden måste ligga efter starttiden */
    if (selectedTime <= new Date(selectedStart.startTime)) {
      return;
    }

    /* sluttiden får inte vara efter bokningsfönstret */
    const lastSlot = slots[slots.length - 1];

    if (!lastSlot || selectedTime > new Date(lastSlot.endTime)) {
      return;
    }

    const slotsInRange = slots.filter(
      (slot) =>
        new Date(slot.startTime) >= new Date(selectedStart.startTime) &&
        new Date(slot.startTime) < selectedTime,
    );

    const allAvailable = slotsInRange.every((slot) => slot.isAvailable);

    if (!allAvailable) {
      setError("En eller flera tider i intervallet är redan bokade.");
      return;
    }

    const selectedRange: TimeSlot = {
      startTime: selectedStart.startTime,
      endTime: time,
      isAvailable: true,
      status: selectedStart.status,
    };

    onSlotSelect(selectedRange);
    setSelectedStart(null);
    setError("");
  }

  if (!selectedDate) {
    return null;
  }

  if (selectedResourceType === null && !overview) {
    return (
      <section className={styles.timeSlotsWrapper}>
        <div className={styles.heading}>
          <p className={styles.eyebrow}>Tider</p>

          <p className={styles.description}>
            Välj en resurs och ett datum för att se tillgängliga tider.
          </p>
        </div>
      </section>
    );
  }
  function formatResourceType(resourceType: string) {
    switch (resourceType) {
      case "VRHeadset":
        return "VR Headset";
      case "AIServer":
        return "AI Server";
      default:
        return resourceType;
    }
  }

  if (overview) {
    return (
      <div className={styles.overviewTimeSlotList}>
        {slots.map((slot) => {
          const time = slot.startTime;

          return (
            <div
              key={time}
              className={`
              ${styles.overviewTimeSlot}
              ${styles[slot.status]}
            `}
            >
              {formatTime(time)}
            </div>
          );
        })}
        {slots.length > 0 && (
          <div
            className={`${styles.overviewTimeSlot} ${
              styles[slots[slots.length - 1].status]
            }`}
          >
            24:00
          </div>
        )}
      </div>
    );
  }

  return (
    <section className={styles.timeSlotsWrapper}>
      <div className={styles.resourceChoice}>
        <button
          type="button"
          className={styles.chooseButton}
          onClick={() => {
            const newValue = !chooseSpecificResource;

            setChooseSpecificResource(newValue);

            if (!newValue) {
              onResourceSelect(null);
              setSelectedStart(null);
              onSlotSelect(null);
            }
          }}
        >
          Välj själv
        </button>

        {chooseSpecificResource && (
          <div className={styles.resourceIdList}>
            {resources.map((resource, index) => (
              <button
                key={resource.resourceId}
                type="button"
                className={
                  selectedResourceId === resource.resourceId
                    ? styles.resourceIdSelected
                    : ""
                }
                onClick={() => {
                  onResourceSelect(resource.resourceId);
                  setSelectedStart(null);
                  onSlotSelect(null);
                }}
              >
                {formatResourceType(resource.resourceType)} {index + 1}
              </button>
            ))}
          </div>
        )}
      </div>

      {loading && (
        <div className={styles.placeholder}>
          <p>Kontrollerar tillgänglighet...</p>
        </div>
      )}

      {!loading && error && (
        <div className={styles.placeholder}>
          <p>{error}</p>
        </div>
      )}

      {!loading && slots.length > 0 && (
        <div className={styles.placeholder}>
          <div className={styles.timeSlotList}>
            {slots.map((slot) => {
              const time = slot.startTime;

              const isSelected =
                selectedStart?.startTime === time ||
                (selectedSlot &&
                  new Date(time) >= new Date(selectedSlot.startTime) &&
                  new Date(time) <= new Date(selectedSlot.endTime));

              return (
                <button
                  key={time}
                  type="button"
                  disabled={!selectedStart && !slot.isAvailable}
                  className={`
                    ${styles.timeButton}
                    ${styles[slot.status]}
                    ${isSelected ? styles.selected : ""}
                    `}
                  onClick={() => handleTimeClick(time)}
                >
                  {formatTime(time)}
                </button>
              );
            })}
            {slots.length > 0 && (
              <button
                type="button"
                disabled={!selectedStart}
                className={`${styles.timeButton} ${
                  styles[slots[slots.length - 1].status]
                } ${
                  selectedSlot && isEndTime(selectedSlot.endTime)
                    ? styles.selected
                    : ""
                }`}
                onClick={() =>
                  handleTimeClick(slots[slots.length - 1].endTime)
                }
              >
                24:00
              </button>
            )}
          </div>
        </div>
      )}
    </section>
  );
}
