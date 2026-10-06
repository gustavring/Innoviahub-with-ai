import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import styles from "./css/Navbar.module.css";

export default function Navbar() {
  const navigate = useNavigate();
  const location = useLocation();
  const token = localStorage.getItem("token");
  const [isLoggedIn, setIsLoggedIn] = useState(!!token);

  const payload = token
    ? JSON.parse(
        atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")),
      )
    : null;

  const isAdmin =
    payload?.[
      "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    ] === "Admin";

  const handleLogout = () => {
    localStorage.removeItem("token");
    setIsLoggedIn(false);
    navigate("/login");
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
