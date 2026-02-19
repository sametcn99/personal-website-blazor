function isEditableTarget(target) {
  if (!target) return false;

  const tagName = target.tagName;
  const editableTags = ["INPUT", "TEXTAREA", "SELECT"];

  if (editableTags.includes(tagName)) return true;
  if (target.isContentEditable) return true;
  if (target.closest('[contenteditable="true"]')) return true;

  return false;
}

window.homeSearchHotkey = {
  keydownHandler: null,

  register(dotNetRef) {
    this.unregister();

    this.keydownHandler = (event) => {
      if (isEditableTarget(event.target)) {
        return;
      }

      const isCharacter =
        event.key.length === 1 &&
        !event.ctrlKey &&
        !event.metaKey &&
        !event.altKey;
      const isBackspace = event.key === "Backspace";
      const isEscape = event.key === "Escape";

      if (!isCharacter && !isBackspace && !isEscape) {
        return;
      }

      event.preventDefault();

      dotNetRef
        .invokeMethodAsync(
          "HandleGlobalKeyInput",
          event.key,
          event.ctrlKey,
          event.metaKey,
          event.altKey,
        )
        .catch(() => {
          this.unregister();
        });
    };

    window.addEventListener("keydown", this.keydownHandler);
  },

  unregister() {
    if (this.keydownHandler) {
      window.removeEventListener("keydown", this.keydownHandler);
      this.keydownHandler = null;
    }
  },
};

window.registerHomeSearchKeyHandler = (dotNetRef) => {
  window.homeSearchHotkey.register(dotNetRef);
};

window.unregisterHomeSearchKeyHandler = () => {
  window.homeSearchHotkey.unregister();
};
