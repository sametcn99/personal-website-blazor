const archiveSearchDialogs = new WeakSet()
let bodyScrollLockCount = 0

export const focusElementById = (elementId) => {
  document.getElementById(elementId)?.focus()
}

export const openArchiveSearch = () => {
  const dialog = document.getElementById("archive-search-dialog")
  if (!dialog) {
    window.location.href = "/#search"
    return
  }

  if (!dialog.open) {
    dialog.showModal()
    lockBodyScroll()
  }

  if (!archiveSearchDialogs.has(dialog)) {
    archiveSearchDialogs.add(dialog)
    dialog.addEventListener("close", unlockBodyScroll)
    dialog.addEventListener("click", (event) => {
      if (event.target === dialog) dialog.close()
    })
  }

  requestAnimationFrame(() => document.getElementById("home-search-input")?.focus())
}

export const closeArchiveSearch = () => {
  const dialog = document.getElementById("archive-search-dialog")
  if (dialog?.open) dialog.close()
}

export const closeOtherCustomSelects = (activeId) => {
  document.querySelectorAll(".custom-select.custom-select-open").forEach((select) => {
    if (select.id === activeId) return
    select.querySelector(".custom-select-trigger")?.click()
  })
}

export const scrollToElementId = (elementId) => {
  const element = document.getElementById(elementId) || document.getElementById(decodeURIComponent(elementId))
  if (!element) return

  const headerHeight = document.querySelector(".site-header")?.getBoundingClientRect().height ?? 0
  const top = element.getBoundingClientRect().top + window.scrollY - headerHeight - 24
  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches
  window.scrollTo({ top: Math.max(0, top), behavior: reduceMotion ? "auto" : "smooth" })
  window.history.pushState(null, "", `${window.location.pathname}${window.location.search}#${encodeURIComponent(elementId)}`)
}

export const lockBodyScroll = () => {
  bodyScrollLockCount += 1
  if (bodyScrollLockCount === 1) {
    document.body.style.overflow = "hidden"
    document.documentElement.style.overflow = "hidden"
  }
}

export const unlockBodyScroll = () => {
  bodyScrollLockCount = Math.max(0, bodyScrollLockCount - 1)
  if (bodyScrollLockCount === 0) {
    document.body.style.overflow = ""
    document.documentElement.style.overflow = ""
  }
}

export const downloadFile = (url, fileName) => {
  const link = document.createElement("a")
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
}

export const copyToClipboard = async (text) => {
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(text)
    return "copied"
  }

  const textarea = document.createElement("textarea")
  textarea.value = text
  textarea.style.position = "fixed"
  textarea.style.opacity = "0"
  document.body.appendChild(textarea)
  textarea.select()
  const copied = document.execCommand("copy")
  textarea.remove()
  return copied ? "copied" : "failed"
}

export const shareCurrentPage = async (title, text) => {
  const url = window.location.href
  if (typeof navigator.share === "function") {
    try {
      await navigator.share({ title, text, url })
      return "shared"
    } catch (error) {
      if (error?.name === "AbortError") return "cancelled"
      return "failed"
    }
  }

  try {
    return await copyToClipboard(url)
  } catch {
    return "failed"
  }
}
