import { useEffect, useState } from "react";
import styles from "./css/Hubert.module.css";
import hubertIcon from "../assets/Hubert/HubertOpenEyes.svg";
import hubertBlink from "../assets/Hubert/HubertClosedEyes.svg";
import hubertHappy from "../assets/Hubert/HubertHappyEyes.svg";

export default function Hubert() {
  const [isOpen, setIsOpen] = useState(false);
  const [isClosing, setIsClosing] = useState(false);

  const [isBlinking, setIsBlinking] = useState(false);
  const [isButtonBlinking, setIsButtonBlinking] = useState(false);
  const [isHovering, setIsHovering] = useState(false);
  const [isGreeting, setIsGreeting] = useState(false);

  useEffect(() => {
    if (isOpen) {
      return;
    }

    let blinkTimeout: ReturnType<typeof setTimeout>;
    let blinkDuration: ReturnType<typeof setTimeout>;

    function scheduleButtonBlink() {
      const nextBlink = 3000 + Math.random() * 4000;

      blinkTimeout = setTimeout(() => {
        setIsButtonBlinking(true);

        blinkDuration = setTimeout(() => {
          setIsButtonBlinking(false);
          scheduleButtonBlink();
        }, 150);
      }, nextBlink);
    }

    scheduleButtonBlink();

    return () => {
      clearTimeout(blinkTimeout);
      clearTimeout(blinkDuration);
    };
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen || isGreeting) {
      return;
    }

    let blinkTimeout: ReturnType<typeof setTimeout>;
    let blinkDuration: ReturnType<typeof setTimeout>;

    function scheduleBlink() {
      const nextBlink = 3000 + Math.random() * 4000;

      blinkTimeout = setTimeout(() => {
        setIsBlinking(true);

        blinkDuration = setTimeout(() => {
          setIsBlinking(false);
          scheduleBlink();
        }, 150);
      }, nextBlink);
    }

    scheduleBlink();

    return () => {
      clearTimeout(blinkTimeout);
      clearTimeout(blinkDuration);
      setIsBlinking(false);
    };
  }, [isOpen, isGreeting]);

  const buttonHubertIcon = isHovering
    ? hubertHappy
    : isButtonBlinking
      ? hubertBlink
      : hubertIcon;

  function openHubert() {
    setIsOpen(true);
    setIsGreeting(true);

    setTimeout(() => {
      setIsGreeting(false);
    }, 700);
  }

  function closeHubert() {
    setIsClosing(true);

    setTimeout(() => {
      setIsOpen(false);
      setIsClosing(false);
    }, 300);
  }

  return (
    <>
      <button
        type="button"
        className={styles.hubertButton}
        onClick={openHubert}
        onMouseEnter={() => setIsHovering(true)}
        onMouseLeave={() => setIsHovering(false)}
      >
        <span className={styles.hubertTitle}>Fråga Hubert</span>

        <div className={styles.hubertContent}>
          <img
            src={buttonHubertIcon}
            alt="Hubert"
            className={styles.hubertIcon}
          />
        </div>
      </button>

      {isOpen && (
        <div
          className={`${styles.hubertOverlay} ${
            isClosing ? styles.overlayClosing : ""
          }`}
        >
          <div
            className={`${styles.hubertWindow} ${
              isClosing ? styles.windowClosing : ""
            }`}
          >
            <button
              type="button"
              className={styles.closeButton}
              onClick={closeHubert}
            >
              ×
            </button>

            <div className={styles.windowHeader}>
              <img
                src={
                  isGreeting
                    ? hubertHappy
                    : isBlinking
                      ? hubertBlink
                      : hubertIcon
                }
                alt="Hubert"
                className={`${styles.windowHubertIcon} ${
                  isGreeting ? styles.windowHubertGreeting : ""
                }`}
              />

              <div className={styles.talkingBoxHubert}>
                <p>Hej,</p>
                <p>Hubert här!</p>
              </div>
            </div>

            <div className={styles.chatAndSendWrapper}>
              <div className={styles.chatArea}>
                {/* OBS: Chatten fixas senare */}
              </div>
              <div className={styles.chatInputWrapper}>
                <textarea
                  className={styles.chatInput}
                  placeholder="Skriv till Hubert..."
                  rows={1}
                  onInput={(e) => {
                    const textarea = e.currentTarget;

                    textarea.style.height = "auto";
                    textarea.style.height = `${Math.min(textarea.scrollHeight, 120)}px`;
                  }}
                />

                <button
                  type="button"
                  className={styles.sendButton}
                  aria-label="Skicka meddelande"
                >
                  ↑
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
