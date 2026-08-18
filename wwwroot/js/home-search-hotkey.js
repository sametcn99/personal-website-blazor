function isEditableTarget(target) {
  if (!target) return false

  const tagName = target.tagName
  if (["INPUT", "TEXTAREA", "SELECT"].includes(tagName)) return true
  if (target.isContentEditable) return true
  return !!target.closest('[contenteditable="true"]')
}

let keydownHandler = null

export const register = (dotNetRef) => {
  unregister()

  keydownHandler = (event) => {
    if (event.defaultPrevented || isEditableTarget(event.target) || event.target?.closest?.(".sloppy-bird-game")) return

    const isCharacter = event.key.length === 1 && event.key !== " " && !event.ctrlKey && !event.metaKey && !event.altKey
    const isBackspace = event.key === "Backspace"
    const isEscape = event.key === "Escape"
    if (!isCharacter && !isBackspace && !isEscape) return

    event.preventDefault()
    dotNetRef.invokeMethodAsync("HandleGlobalKeyInput", event.key, event.ctrlKey, event.metaKey, event.altKey).catch(unregister)
  }

  window.addEventListener("keydown", keydownHandler)
}

export const unregister = () => {
  if (!keydownHandler) return
  window.removeEventListener("keydown", keydownHandler)
  keydownHandler = null
}
