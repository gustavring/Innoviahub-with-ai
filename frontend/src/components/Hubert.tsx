import { useEffect, useRef, useState } from "react";
import styles from "./css/Hubert.module.css";
import hubertIcon from "../assets/Hubert/HubertOpenEyes.svg";
import hubertBlink from "../assets/Hubert/HubertClosedEyes.svg";
import hubertHappy from "../assets/Hubert/HubertHappyEyes.svg";
import hubertThinking from "../assets/Hubert/HubertLookDownEyes.svg";

export default function Hubert() {
  const [isOpen, setIsOpen] = useState(false);
  const [isClosing, setIsClosing] = useState(false);

  const [isBlinking, setIsBlinking] = useState(false);
  const [isButtonBlinking, setIsButtonBlinking] = useState(false);
  const [isHovering, setIsHovering] = useState(false);
  const [isGreeting, setIsGreeting] = useState(false);
  const [isGreetingMessage, setIsGreetingMessage] = useState(false);
  const [isThinking, setIsThinking] = useState(false);

  const [message, setMessage] = useState("");

  const [messages, setMessages] = useState<
    { sender: "user" | "hubert"; text: string }[]
  >([]);

  const chatAreaRef = useRef<HTMLDivElement>(null);

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
    if (!isOpen || isGreeting || isThinking) {
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
  }, [isOpen, isGreeting, isThinking]);

  useEffect(() => {
    const chatArea = chatAreaRef.current;

    if (chatArea) {
      chatArea.scrollTo({
        top: chatArea.scrollHeight,
        behavior: "smooth",
      });
    }
  }, [messages, isThinking]);

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

    if (messages.length === 0) {
      setIsGreetingMessage(true);

      setTimeout(() => {
        setIsGreetingMessage(false);

        setMessages([
          {
            sender: "hubert",
            text: "Hej, Hubert här. Vad kan jag hjälpa dig med idag?",
          },
        ]);
      }, 1200);
    }
  }

  function closeHubert() {
    setIsClosing(true);

    setTimeout(() => {
      setIsOpen(false);
      setIsClosing(false);
    }, 300);
  }

  async function sendMessage() {
    if (!message.trim()) {
      return;
    }

    const userMessage = message;

    setMessages((prev) => [...prev, { sender: "user", text: userMessage }]);

    setMessage("");

    /* Hubert börjar tänka */
    setIsThinking(true);

    try {
      const response = await fetch("http://localhost:5197/api/Hubert", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          message: userMessage,
        }),
      });

      if (!response.ok) {
        const errorText = await response.text();

        throw new Error(`Hubert API-fel (${response.status}): ${errorText}`);
      }

      const data = await response.json();

      setMessages((prev) => [
        ...prev,
        { sender: "hubert", text: data.message },
      ]);

      setIsThinking(false);
      setIsGreeting(true);

      /* efter 700 ms går Hubert tillbaka till vanligt läge */
      setTimeout(() => {
        setIsGreeting(false);
      }, 700);
    } catch (error) {
      console.error(error);

      setIsThinking(false);
    }
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
                  isThinking
                    ? hubertThinking
                    : isGreeting
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
            </div>

            <div className={styles.chatAndSendWrapper}>
              <div className={styles.chatArea} ref={chatAreaRef}>
                {messages.map((chatMessage, index) => (
                  <div
                    key={index}
                    className={
                      chatMessage.sender === "user"
                        ? styles.userMessage
                        : styles.hubertMessage
                    }
                  >
                    {chatMessage.text}
                  </div>
                ))}
                {(isThinking || isGreetingMessage) && (
                  <div className={styles.thinkingMessage}>
                    <span></span>
                    <span></span>
                    <span></span>
                  </div>
                )}
              </div>
              <div className={styles.chatInputWrapper}>
                <textarea
                  className={styles.chatInput}
                  placeholder="Skriv till Hubert..."
                  rows={1}
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" && !e.shiftKey) {
                      e.preventDefault();
                      sendMessage();
                    }
                  }}
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
                  onClick={sendMessage}
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
