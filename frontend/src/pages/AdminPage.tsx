import { useState } from "react";
import Navbar from "../components/Navbar";
import styles from "./css/AdminPage.module.css";
import UserList from "../components/UserList";
import AdminBookings from "../components/AdminBookings";
import ResourceStatus from "../components/ResourceStatus";
import RegisterUser from "../components/RegisterUser";
import Hubert from "../components/Hubert";

export default function LandingPage() {
  const [showRegister, setShowRegister] = useState(false);

  return (
    <>
      <Navbar />
      <main className={styles.adminPage}>
        {showRegister ? (
          <RegisterUser onBack={() => setShowRegister(false)} />
        ) : (
          <>
            <div className={styles.resourceStatusAndHubertWrapper}>
              <ResourceStatus />
              <Hubert />
            </div>

            <div className={styles.adminBookingViewWrapper}>
              <AdminBookings />
            </div>

            <div className={styles.userListAndButtonWrapper}>
              <div className={styles.buttonWrapper}>
                <button
                  className={styles.registerButton}
                  type="button"
                  onClick={() => setShowRegister(true)}
                >
                  Registrera användare
                </button>
                <button className={styles.lockResource}>
                  Lås en resurs
                </button>
              </div>
              <UserList />
            </div>
          </>
        )}
      </main>
    </>
  );
}
