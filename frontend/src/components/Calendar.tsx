import { useMemo, useState } from "react";
import TimeSlots from "./TimeSlots";
import styles from "../components/css/Calendar.module.css";

type CalendarProps = {
  selectedDate?: Date;
  selectedResourceType: string | null;
  onDateSelect: (date: Date) => void;
  refreshKey: number;
};

const ignoreSelection = () => {};

export default function Calendar({
  selectedDate,
  selectedResourceType,
  onDateSelect,
  refreshKey,
}: CalendarProps) {
  const today = new Date();

  //datumet bestämmer vilken vecka kalendern visar
  const [displayDate, setDisplayDate] = useState(new Date());

  // Behåll samma datumobjekt tills veckan ändras.
  const weekDays = useMemo(() => {
    const monday = new Date(displayDate);
    monday.setHours(0, 0, 0, 0);

    const day = monday.getDay();
    monday.setDate(monday.getDate() + (day === 0 ? -6 : 1 - day));

    return Array.from({ length: 7 }, (_, index) => {
      const date = new Date(monday);
      date.setDate(monday.getDate() + index);
      return date;
    });
  }, [displayDate]);

  const mondayOfWeek = weekDays[0];

  const getFirstMondayOfMonth = (year: number, month: number) => {
    const date = new Date(year, month, 1);

    const dayOfWeek = date.getDay();

    const daysUntilMonday = dayOfWeek === 0 ? 1 : (8 - dayOfWeek) % 7;

    date.setDate(date.getDate() + daysUntilMonday);

    return date;
  };

  const previousWeek = () => {
    const newDate = new Date(displayDate);
    newDate.setDate(displayDate.getDate() - 7);

    setDisplayDate(newDate);
  };

  const nextWeek = () => {
    const newDate = new Date(displayDate);
    newDate.setDate(displayDate.getDate() + 7);

    setDisplayDate(newDate);
  };

  const previousMonth = () => {
    const newDate = getFirstMondayOfMonth(
      mondayOfWeek.getFullYear(),
      mondayOfWeek.getMonth() - 1,
    );

    setDisplayDate(newDate);
  };

  const nextMonth = () => {
    const newDate = getFirstMondayOfMonth(
      mondayOfWeek.getFullYear(),
      mondayOfWeek.getMonth() + 1,
    );

    setDisplayDate(newDate);
  };

  return (
    <div className={styles.calendar}>
      <div className={styles.header}>
        <div className={styles.navigation}>
          <button type="button" onClick={previousMonth} title="Previous month">
            &lt;&lt;
          </button>

          <button type="button" onClick={previousWeek} title="Previous week">
            &lt;
          </button>

          <h2>
            {mondayOfWeek.toLocaleDateString("sv-SE", {
              month: "long",
              year: "numeric",
            })}
          </h2>

          <button type="button" onClick={nextWeek} title="Next week">
            &gt;
          </button>

          <button type="button" onClick={nextMonth} title="Next month">
            &gt;&gt;
          </button>
        </div>
      </div>

      <div className={styles.week}>
        {weekDays.map((date) => {
          const isWeekend = date.getDay() === 0 || date.getDay() === 6;

          const isSelected =
            selectedDate?.toDateString() === date.toDateString();

          const isToday =
            date.getFullYear() === today.getFullYear() &&
            date.getMonth() === today.getMonth() &&
            date.getDate() === today.getDate();

          return (
            <button
              type="button"
              key={date.toISOString()}
              className={`
                ${styles.dayRow}
                ${isWeekend ? styles.weekend : ""}
                ${isSelected ? styles.selected : ""}
                ${isToday ? styles.today : ""}
              `}
              onClick={() => {
                if (selectedResourceType !== null) {
                  onDateSelect(date);
                }
              }}
            >
              <div className={styles.day}>
                <span className={styles.dayName}>
                  {date.toLocaleDateString("sv-SE", {
                    weekday: "short",
                  })}
                </span>

                <span className={styles.dayNumber}>{date.getDate()}</span>

                {date.getDate() === 1 && (
                  <span className={styles.monthName}>
                    {date
                      .toLocaleDateString("sv-SE", {
                        month: "short",
                      })
                      .replace(".", "")}
                  </span>
                )}
              </div>

              <div className={styles.dayContent}>
                <TimeSlots
                  selectedDate={date}
                  selectedResourceType={selectedResourceType}
                  selectedResourceId={null}
                  onResourceSelect={ignoreSelection}
                  onSlotSelect={ignoreSelection}
                  selectedSlot={null}
                  overview={true}
                  refreshKey={refreshKey}
                />
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
}
