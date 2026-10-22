import { useLocation, useNavigate } from "react-router-dom";
import styles from "./css/Navbar.module.css";
import { getSession } from "../auth";

export default function Navbar() {
  const navigate = useNavigate();
  const location = useLocation();
  const session = getSession();
  const isLoggedIn = session !== null;
  const isAdmin = session?.isAdmin ?? false;

  const handleLogout = () => {
    sessionStorage.removeItem("token");
    navigate("/login", { replace: true });
  };

  return (
    <nav className={styles.navbar}>
      <h1 className={styles.logo}>Innovia<span>Hub</span></h1>

      <div className={styles.buttonWrapper}>
        {isLoggedIn && location.pathname === "/" && (
          <button
            className={styles.loginButton}
            onClick={() =>
              document.getElementById("bookings")?.scrollIntoView()
            }
          >
            Mina bokningar
          </button>
        )}

        {isAdmin && (
          <button
            className={styles.loginButton}
            onClick={() =>
              navigate(location.pathname === "/admin" ? "/" : "/admin")
            }
          >
            {location.pathname === "/admin" ? "Startsida" : "Admin-vy"}
          </button>
        )}

        <button
          className={styles.loginButton}
          onClick={() => (isLoggedIn ? handleLogout() : navigate("/login"))}
        >
          {isLoggedIn ? "Logga ut" : "Logga in"}
        </button>
      </div>
    </nav>
  );
}
