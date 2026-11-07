import { installReconnectTheme } from "./reconnect-theme.js"

installReconnectTheme()

document.addEventListener("enhancednavigation", () => {
  window.scrollTo({ top: 0, behavior: "instant" })
})
