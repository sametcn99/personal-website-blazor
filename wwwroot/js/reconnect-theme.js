// Blazor'ın (.NET 10) reconnect arayüzü kendi Shadow DOM'u içinde render
// edilir; sayfa CSS'i oraya işlemez ve varsayılan tema (beyaz kutu, mavi
// butonlar, mavi dalga halkaları) site tasarımından kopar. Bu modul,
// #components-reconnect-modal host'u body'ye eklendiğinde shadow root'a
// Geist monokrom tema override'ı enjekte eder.
const DIALOG_HOST_ID = "components-reconnect-modal"

const GEIST_RECONNECT_CSS = `
  .components-reconnect-overlay::before {
    background-color: rgba(0, 0, 0, 0.82);
    backdrop-filter: blur(5px);
  }

  .components-reconnect-dialog {
    width: min(22rem, calc(100vw - 2rem));
    padding: 1.7rem 1.8rem;
    border: 1px solid #2e2e2e;
    border-radius: 12px;
    background-color: #0a0a0a;
    box-shadow: 0 30px 90px rgba(0, 0, 0, 0.5);
    color: #ededed;
    font-family: "Familjen Grotesk", "Segoe UI", sans-serif;
    animation: components-reconnect-fadeInOpacity 0.25s ease-out both;
  }

  .components-reconnect-overlay p {
    color: #ededed;
    font-size: 0.95rem;
    line-height: 1.55;
  }

  .components-reconnect-overlay button {
    min-height: 2.5rem;
    padding: 0 1.2rem;
    border: 1px solid transparent;
    border-radius: 6px;
    background-color: #ededed;
    color: #000000;
    font-family: inherit;
    font-size: 0.85rem;
    font-weight: 600;
    cursor: pointer;
  }

  .components-reconnect-overlay button:hover {
    background-color: #ffffff;
  }

  .components-reconnect-overlay button:active {
    background-color: #ededed;
  }

  .components-reconnect-overlay button:focus-visible {
    outline: 2px solid #ffffff;
    outline-offset: 2px;
  }

  /* Mavi genişleyen dalga halkalari -> tek Geist spinner halkasi */
  .components-rejoining-animation {
    width: 44px;
    height: 44px;
  }

  .components-rejoining-animation div {
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    border: 3px solid rgba(255, 255, 255, 0.16);
    border-top-color: #ffffff;
    animation: geist-reconnect-spin 0.85s linear infinite;
  }

  .components-rejoining-animation div:nth-child(2) {
    display: none;
  }

  @keyframes geist-reconnect-spin {
    to {
      transform: rotate(360deg);
    }
  }

  @media (prefers-reduced-motion: reduce) {
    .components-reconnect-dialog {
      animation-duration: 1ms;
    }

    .components-rejoining-animation div {
      animation: none;
    }
  }
`

const styleReconnectHost = (host) => {
  if (!host || !host.shadowRoot || host.dataset.geistThemed === "true") {
    return
  }

  const style = document.createElement("style")
  style.textContent = GEIST_RECONNECT_CSS
  host.shadowRoot.appendChild(style)
  host.dataset.geistThemed = "true"
}

export const installReconnectTheme = () => {
  styleReconnectHost(document.getElementById(DIALOG_HOST_ID))

  // Blazor, host'u body'ye yalnizca ilk kopus aninda ekler.
  const observer = new MutationObserver(() => {
    styleReconnectHost(document.getElementById(DIALOG_HOST_ID))
  })
  observer.observe(document.body, { childList: true })
}
