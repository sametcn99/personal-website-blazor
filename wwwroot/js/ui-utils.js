window.focusElementById = (elementId) => {
  const element = document.getElementById(elementId)
  if (element) {
    element.focus()
  }
}

window.openArchiveSearch = () => {
  const dialog = document.getElementById("archive-search-dialog")

  if (!dialog) {
    window.location.href = "/#search"
    return
  }

  if (!dialog.open) {
    dialog.showModal()
    window.lockBodyScroll()
  }

  if (!dialog.dataset.archiveSearchReady) {
    dialog.dataset.archiveSearchReady = "true"
    dialog.addEventListener("close", window.unlockBodyScroll)
    dialog.addEventListener("click", (event) => {
      if (event.target === dialog) dialog.close()
    })
  }

  requestAnimationFrame(() => {
    document.getElementById("home-search-input")?.focus()
  })
}

window.closeArchiveSearch = () => {
  const dialog = document.getElementById("archive-search-dialog")
  if (dialog?.open) dialog.close()
}

window.scrollToElementId = (elementId) => {
  const element = document.getElementById(elementId) || document.getElementById(decodeURIComponent(elementId))
  if (element) {
    const headerHeight = document.querySelector(".site-header")?.getBoundingClientRect().height ?? 0
    const top = element.getBoundingClientRect().top + window.scrollY - headerHeight - 24
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches
    window.scrollTo({ top: Math.max(0, top), behavior: reduceMotion ? "auto" : "smooth" })
    const newUrl = `${window.location.pathname}${window.location.search}#${encodeURIComponent(elementId)}`
    window.history.pushState(null, "", newUrl)
  }
}

window.__bodyScrollLockCount = 0

window.lockBodyScroll = () => {
  window.__bodyScrollLockCount += 1

  if (window.__bodyScrollLockCount === 1) {
    document.body.style.overflow = "hidden"
    document.documentElement.style.overflow = "hidden"
  }
}

window.unlockBodyScroll = () => {
  window.__bodyScrollLockCount = Math.max(0, window.__bodyScrollLockCount - 1)

  if (window.__bodyScrollLockCount === 0) {
    document.body.style.overflow = ""
    document.documentElement.style.overflow = ""
  }
}

window.downloadFile = (url, fileName) => {
  const link = document.createElement("a")
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}

window.sloppyBird =
  window.sloppyBird ||
  (() => {
    const observers = new Map()
    const games = new Map()

    const dispose = (id) => {
      observers.get(id)?.disconnect()
      observers.delete(id)
      games.get(id)?.destroy?.()
      games.delete(id)
    }

    return {
      observe(id) {
        dispose(id)
        const host = document.getElementById(id)
        if (!host) return

        const loadGame = async () => {
          if (games.has(id)) return
          const module = await import("/js/sloppy-bird.js?v=8")
          games.set(id, module.mount(host))
        }

        if (!("IntersectionObserver" in window)) {
          loadGame()
          return
        }

        const observer = new IntersectionObserver(
          async ([entry]) => {
            if (!entry.isIntersecting) return
            observer.disconnect()
            observers.delete(id)
            await loadGame()
          },
          { threshold: 0.1 },
        )

        observers.set(id, observer)
        observer.observe(host)
      },
      dispose,
    }
  })()

// Scroll to top on enhanced navigation (Blazor 8+)
document.addEventListener("enhancednavigation", () => {
  window.scrollTo({ top: 0, behavior: "instant" })
})
