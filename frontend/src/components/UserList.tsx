import { useEffect, useState } from "react";
import styles from "./css/UserList.module.css";

const API_URL = import.meta.env.VITE_API_URL;

type User = {
  userId: string;
  email: string;
  role: string;
};

export default function UserList() {
  const [users, setUsers] = useState<User[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [showAll, setShowAll] = useState(false);

  useEffect(() => {
    const getUsers = async () => {
      try {
        setLoading(true);
        setError("");

        const token = sessionStorage.getItem("token");

        if (!token) {
          throw new Error("Ingen inloggningstoken hittades.");
        }

        const response = await fetch(`${API_URL}/api/Users`, {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        if (response.status === 401) {
          throw new Error("Du är inte inloggad.");
        }

        if (response.status === 403) {
          throw new Error("Du måste vara admin för att se användarna.");
        }

        if (!response.ok) {
          throw new Error("Kunde inte hämta användare.");
        }

        const data: User[] = await response.json();

        console.log("Användare från backend:", data);

        setUsers(data);
      } catch (error) {
        if (error instanceof Error) {
          setError(error.message);
        }
      } finally {
        setLoading(false);
      }
    };

    getUsers();
  }, []);

  const visibleUsers = showAll ? users : users.slice(0, 3);

  return (
    <>
      <section className={styles.userListWrapper}>
        <div className={styles.heading}>
          <div className={styles.headerAndButton}>
            <p className={styles.eyebrow}>Användare</p>
            {users.length > 3 && (
              <button type="button" onClick={() => setShowAll(!showAll)}>
                {showAll ? "Visa färre medlemmar ↑" : "Visa alla medlemmar →"}
              </button>
            )}
          </div>
          <div className={styles.info}>
            <div className={styles.email}>E-post</div>
            <div className={styles.role}>Roll</div>
          </div>
        </div>

        {loading && (
          <div className={styles.placeholder}>
            <p>Hämtar användare...</p>
          </div>
        )}

        {!loading && error && (
          <div className={styles.placeholder}>
            <p>{error}</p>
          </div>
        )}

        {!loading && !error && users.length === 0 && (
          <div className={styles.placeholder}>
            <p>Inga användare hittades</p>
          </div>
        )}

        {!loading && !error && users.length > 0 && (
          <div className={styles.userList}>
            {visibleUsers.map((user) => (
              <div key={user.userId} className={styles.userRow}>
                <div className={styles.email}>{user.email}</div>
                <div className={styles.role}>
                  {user.role === "Admin" ? "Admin" : "Användare"}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </>
  );
}
