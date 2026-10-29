import { useEffect, useRef, useState } from "react";
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
  const [userToDelete, setUserToDelete] = useState<User | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState("");
  const deleteDialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = deleteDialogRef.current;

    if (!userToDelete || !dialog) return;

    if (!dialog.open) dialog.showModal();

    return () => {
      dialog.close();
    };
  }, [userToDelete]);

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

  function closeDeleteDialog() {
    if (deleting) return;

    setUserToDelete(null);
    setDeleteError("");
  }

  async function deleteUser() {
    if (!userToDelete || userToDelete.role === "Admin" || deleting) return;

    const token = sessionStorage.getItem("token");

    if (!token) {
      setDeleteError("Du måste vara inloggad som admin.");
      return;
    }

    setDeleting(true);
    setDeleteError("");

    try {
      const response = await fetch(
        `${API_URL}/api/Users/${encodeURIComponent(userToDelete.userId)}`,
        {
          method: "DELETE",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        },
      );

      if (response.status === 401) {
        throw new Error("Din inloggning har gått ut. Logga in igen.");
      }

      if (response.status === 403) {
        throw new Error("Bara admin kan ta bort användare.");
      }

      if (!response.ok) {
        throw new Error(
          (await response.text()) || "Kunde inte ta bort användaren.",
        );
      }

      setUsers((current) =>
        current.filter((user) => user.userId !== userToDelete.userId),
      );
      setUserToDelete(null);
    } catch (cause) {
      setDeleteError(
        cause instanceof Error
          ? cause.message
          : "Kunde inte ansluta till servern. Försök igen.",
      );
    } finally {
      setDeleting(false);
    }
  }

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
            <div className={styles.actions}>Åtgärd</div>
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
                <div className={styles.actions}>
                  {user.role !== "Admin" && (
                    <button
                      type="button"
                      className={styles.deleteButton}
                      aria-label={`Ta bort ${user.email}`}
                      onClick={() => {
                        setDeleteError("");
                        setUserToDelete(user);
                      }}
                    >
                      Ta bort
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      <dialog
        ref={deleteDialogRef}
        className={styles.deleteDialog}
        aria-labelledby="delete-user-title"
        aria-describedby="delete-user-description"
        aria-busy={deleting}
        onCancel={(event) => {
          event.preventDefault();
          closeDeleteDialog();
        }}
      >
        {userToDelete && (
          <>
            <p className={styles.dialogEyebrow}>ANVÄNDARE</p>
            <h2 id="delete-user-title">Ta bort användare?</h2>

            <div id="delete-user-description">
              <p>
                Vill du ta bort <strong>{userToDelete.email}</strong>?
              </p>
              <p className={styles.dialogDescription}>
                Användarens bokningar tas också bort. Åtgärden går inte att ångra.
              </p>
            </div>

            {deleteError && (
              <p className={styles.dialogError} role="alert">
                {deleteError}
              </p>
            )}

            <div className={styles.dialogActions}>
              <button
                type="button"
                className={styles.cancelButton}
                disabled={deleting}
                onClick={closeDeleteDialog}
                autoFocus
              >
                Avbryt
              </button>
              <button
                type="button"
                className={styles.confirmDeleteButton}
                disabled={deleting}
                onClick={deleteUser}
              >
                {deleting ? "Tar bort..." : "Ta bort"}
              </button>
            </div>
          </>
        )}
      </dialog>
    </>
  );
}
