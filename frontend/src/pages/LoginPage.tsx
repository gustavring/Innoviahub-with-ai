import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import Input from "../components/Input";
import styles from "../pages/css/LoginPage.module.css";

const API_URL = import.meta.env.VITE_API_URL;

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const navigate = useNavigate();

  useEffect(() => {
    if (!error) return;

    const timeout = window.setTimeout(() => {
      setError("");
    }, 5000);

    return () => window.clearTimeout(timeout);
  }, [error]);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    setError("");
    setLoading(true);

    try {
      const response = await fetch(`${API_URL}/api/Auth/login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email,
          password,
        }),
      });

      if (!response.ok) {
        if (response.status === 401) {
          setError("Fel email eller lösenord.");
        } else {
          setError("Något gick fel. Försök igen.");
        }

        return;
      }

      const data = await response.json();

      sessionStorage.setItem("token", data.token);

      navigate("/", { replace: true });
    } catch {
      setError("Kunde inte ansluta till servern. Försök igen.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Navbar />

      <main className={styles.loginPage}>
        {error && (
          <div className={styles.errorToast} role="alert">
            {error}
          </div>
        )}
        <section className={styles.loginCard}>
          <div className={styles.heading}>
            <p className={styles.eyebrow}>Innovia</p>

            <h1>Logga in</h1>

            <p>Logga in för att boka resurser och hantera dina bokningar.</p>
          </div>

          <form className={styles.loginForm} onSubmit={handleSubmit}>
            <Input
              label="Email:"
              type="email"
              value={email}
              placeholder="Skriv din email"
              onChange={setEmail}
            />

            <Input
              label="Lösenord:"
              type="password"
              value={password}
              placeholder="Skriv ditt lösenord"
              onChange={setPassword}
            />

            <button
              className={styles.backButton}
              type="button"
              onClick={() => navigate("/")}
            >
              Tillbaka
            </button>

            <button
              className={styles.loginButton}
              type="submit"
              disabled={loading}
            >
              {loading ? "Loggar in..." : "Logga in"}
            </button>
          </form>
        </section>
      </main>
    </>
  );
}
