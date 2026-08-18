let mermaidReadyPromise = null
let mermaidInstance = null
const mermaidModuleUrl = "https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs"
const mermaidThemeConfig = {
  startOnLoad: false,
  theme: "base",
  themeVariables: {
    darkMode: true,
    background: "#0d0e0c",
    primaryColor: "#24251f",
    primaryTextColor: "#eee9dc",
    primaryBorderColor: "#c89a49",
    secondaryColor: "#202019",
    secondaryTextColor: "#eee9dc",
    secondaryBorderColor: "#48483c",
    tertiaryColor: "#141512",
    tertiaryTextColor: "#eee9dc",
    tertiaryBorderColor: "#303129",
    lineColor: "#aaa99e",
    textColor: "#eee9dc",
    mainBkg: "#24251f",
    nodeBorder: "#c89a49",
    clusterBkg: "#141512",
    clusterBorder: "#48483c",
    noteBkgColor: "#202019",
    noteTextColor: "#eee9dc",
    noteBorderColor: "#c89a49",
    edgeLabelBackground: "#141512",
    fontFamily: '"Familjen Grotesk", "Segoe UI", sans-serif',
  },
}

export const ensureMermaidReady = async () => {
  if (mermaidInstance) {
    return mermaidInstance
  }

  if (!mermaidReadyPromise) {
    mermaidReadyPromise = import(mermaidModuleUrl)
      .then((module) => {
        mermaidInstance = module.default
        mermaidInstance.initialize(mermaidThemeConfig)
        return mermaidInstance
      })
      .catch((error) => {
        mermaidReadyPromise = null
        mermaidInstance = null
        throw error
      })
  }

  return mermaidReadyPromise
}

export const renderMermaidDiagram = async (containerId, definition) => {
  const container = document.getElementById(containerId)
  if (!container || !definition) {
    return false
  }

  try {
    const mermaid = await ensureMermaidReady()
    const renderId = `mermaid-svg-${containerId}-${Date.now()}`
    const result = await mermaid.render(renderId, definition)
    container.innerHTML = result.svg
    return true
  } catch (error) {
    console.error("Mermaid render error:", error)
    container.innerHTML = `<pre style="margin:0; overflow:auto;">${definition}</pre>`
    throw error
  }
}

export const renderMermaidToSvg = async (definition, diagramId) => {
  if (!definition) {
    return ""
  }

  const mermaid = await ensureMermaidReady()
  const renderId = diagramId || `mermaid-svg-${Date.now()}`
  const result = await mermaid.render(renderId, definition.trim())
  return result.svg || ""
}

export const downloadSvgMarkup = (svgMarkup, filenamePrefix = "mermaid-diagram") => {
  if (!svgMarkup) {
    return
  }

  const parser = new DOMParser()
  const doc = parser.parseFromString(svgMarkup, "image/svg+xml")
  const svgElement = doc.querySelector("svg")

  if (!svgElement) {
    return
  }

  if (!svgElement.getAttribute("xmlns")) {
    svgElement.setAttribute("xmlns", "http://www.w3.org/2000/svg")
  }

  if (!svgElement.getAttribute("xmlns:xlink")) {
    svgElement.setAttribute("xmlns:xlink", "http://www.w3.org/1999/xlink")
  }

  if (!svgElement.getAttribute("viewBox")) {
    const width = svgElement.getAttribute("width") || "800"
    const height = svgElement.getAttribute("height") || "600"
    svgElement.setAttribute("viewBox", `0 0 ${width} ${height}`)
  }

  const svgString = new XMLSerializer().serializeToString(svgElement)
  const blob = new Blob([svgString], { type: "image/svg+xml;charset=utf-8" })
  const url = URL.createObjectURL(blob)
  const timestamp = new Date().toISOString().slice(0, 19).replace(/[:.]/g, "-")
  const link = document.createElement("a")
  link.href = url
  link.download = `${filenamePrefix}-${timestamp}.svg`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

export const getElementInnerHtml = (elementId) => {
  const element = document.getElementById(elementId)
  return element?.innerHTML || ""
}

export const hasSvgContent = (elementId) => {
  const element = document.getElementById(elementId)
  return !!element?.querySelector("svg")
}

function decorateCodeBlocks() {
  console.log("Decorating code blocks...")
  const codeBlocks = document.querySelectorAll("pre code")

  if (codeBlocks.length === 0) {
  }

  codeBlocks.forEach((block) => {
    const pre = block.parentElement

    if (pre.closest(".code-component-container")) return

    if (pre.parentNode.classList.contains("code-block-wrapper")) return

    console.log("Processing block:", block.className)

    let lang = "text"
    block.classList.forEach((cls) => {
      if (cls.startsWith("language-")) {
        lang = cls.replace("language-", "")
      }
    })

    const wrapper = document.createElement("div")
    wrapper.className = "code-block-wrapper"

    const header = document.createElement("div")
    header.className = "code-block-header"

    const langSpan = document.createElement("span")
    langSpan.className = "code-lang"
    langSpan.textContent = lang

    const copyBtn = document.createElement("button")
    copyBtn.className = "copy-btn"
    copyBtn.title = "Copy to clipboard"
    copyBtn.innerHTML = `
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
            </svg>
        `
    copyBtn.onclick = () => {
      navigator.clipboard.writeText(block.textContent)
      const originalHTML = copyBtn.innerHTML
      copyBtn.innerHTML =
        '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#4ade80" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>'
      setTimeout(() => {
        copyBtn.innerHTML = originalHTML
      }, 2000)
    }

    header.appendChild(langSpan)
    header.appendChild(copyBtn)

    if (window.hljs) {
      window.hljs.highlightElement(block)
    }

    pre.parentNode.insertBefore(wrapper, pre)
    wrapper.appendChild(header)
    wrapper.appendChild(pre)
  })
}

let contentInitState = {
  lastPath: "",
  lastRunAt: 0,
}

let tocRailCleanup = null
let inlineTocCleanup = null

function scrollToContentHeading(id) {
  if (!id) return false

  const decodedId = decodeURIComponent(id)
  const heading = document.getElementById(id) || document.getElementById(decodedId)
  if (!heading) return false

  const headerHeight = document.querySelector(".site-header")?.getBoundingClientRect().height ?? 0
  const top = heading.getBoundingClientRect().top + window.scrollY - headerHeight - 24
  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches

  window.scrollTo({
    top: Math.max(0, top),
    behavior: reduceMotion ? "auto" : "smooth",
  })
  window.history.pushState(null, "", `${window.location.pathname}${window.location.search}#${encodeURIComponent(decodedId)}`)
  return true
}

function initInlineToc() {
  const toc = document.querySelector(".toc")
  if (!toc) return

  const onClick = (event) => {
    const link = event.target.closest(".toc-link")
    if (!link || !toc.contains(link)) return

    event.preventDefault()
    event.stopPropagation()

    const id = link.dataset.tocTarget || link.getAttribute("href")?.replace(/^#/, "")
    scrollToContentHeading(id)
  }

  toc.addEventListener("click", onClick)
  inlineTocCleanup = () => toc.removeEventListener("click", onClick)
}

function initTocRail() {
  const rail = document.querySelector(".toc-rail")
  const items = Array.from(rail?.querySelectorAll(".toc-rail-item") ?? [])

  if (!rail || items.length === 0) return

  const headings = items.map((item) => ({ item, heading: document.getElementById(item.dataset.tocTarget) })).filter(({ heading }) => heading)

  let activeItem = null
  const setActive = (item) => {
    if (activeItem === item) return
    activeItem?.classList.remove("is-active")
    activeItem = item
    activeItem?.classList.add("is-active")
  }

  const updateActiveHeading = () => {
    const marker = Math.min(window.innerHeight * 0.33, 260)
    let current = headings[0]?.item ?? null

    for (const { item, heading } of headings) {
      if (heading.getBoundingClientRect().top <= marker) current = item
      else break
    }

    setActive(current)
  }

  let frame = 0
  const onScroll = () => {
    if (frame) return
    frame = requestAnimationFrame(() => {
      frame = 0
      updateActiveHeading()
    })
  }

  const onPointerMove = (event) => {
    for (const item of items) {
      const rect = item.getBoundingClientRect()
      const distance = Math.abs(event.clientY - (rect.top + rect.height / 2))
      const proximity = Math.max(0, 1 - distance / 80)
      item.style.setProperty("--toc-proximity", proximity.toFixed(3))
      item.style.setProperty("--toc-scale", (0.48 + proximity * 0.52).toFixed(3))
    }
  }

  const resetProximity = () => {
    items.forEach((item) => {
      item.style.removeProperty("--toc-proximity")
      item.style.removeProperty("--toc-scale")
    })
  }

  const onClick = (event) => {
    const item = event.target.closest(".toc-rail-item")
    if (!item) return

    event.preventDefault()
    const id = item.dataset.tocTarget
    const heading = document.getElementById(id)
    if (!heading) return

    heading.scrollIntoView({ behavior: "smooth", block: "start" })
    window.history.pushState(null, "", `${window.location.pathname}${window.location.search}#${encodeURIComponent(id)}`)
    setActive(item)
  }

  const preventLinkDrag = (event) => event.preventDefault()

  rail.addEventListener("pointermove", onPointerMove)
  rail.addEventListener("pointerleave", resetProximity)
  rail.addEventListener("dragstart", preventLinkDrag)
  rail.addEventListener("click", onClick)
  window.addEventListener("scroll", onScroll, { passive: true })
  window.addEventListener("resize", updateActiveHeading, { passive: true })
  updateActiveHeading()

  tocRailCleanup = () => {
    if (frame) cancelAnimationFrame(frame)
    rail.removeEventListener("pointermove", onPointerMove)
    rail.removeEventListener("pointerleave", resetProximity)
    rail.removeEventListener("dragstart", preventLinkDrag)
    rail.removeEventListener("click", onClick)
    window.removeEventListener("scroll", onScroll)
    window.removeEventListener("resize", updateActiveHeading)
  }
}

export const initContent = () => {
  const currentPath = `${window.location.pathname}${window.location.search}`
  const now = Date.now()

  // Prevent duplicate init runs caused by rapid consecutive renders.
  if (contentInitState.lastPath === currentPath && now - contentInitState.lastRunAt < 1000) {
    return
  }

  contentInitState.lastPath = currentPath
  contentInitState.lastRunAt = now

  decorateCodeBlocks()
  tocRailCleanup?.()
  tocRailCleanup = null
  inlineTocCleanup?.()
  inlineTocCleanup = null
  initTocRail()
  initInlineToc()
}

export const highlightCode = (elementId) => {
  const element = document.getElementById(elementId)
  if (element && globalThis.Prism) {
    globalThis.Prism.highlightElement(element)
  }
}

let monacoReadyPromise = null

export const ensureMonacoReady = async () => {
  if (globalThis.monaco) {
    return globalThis.monaco
  }

  if (!monacoReadyPromise) {
    monacoReadyPromise = new Promise((resolve, reject) => {
      const script = document.createElement("script")
      script.src = "https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs/loader.min.js"
      script.onload = () => {
        globalThis.require.config({
          paths: {
            vs: "https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs",
          },
        })
        globalThis.require(["vs/editor/editor.main"], function () {
          resolve(globalThis.monaco)
        })
      }
      script.onerror = reject
      document.head.appendChild(script)
    })
  }

  return monacoReadyPromise
}

export const renderMonacoEditor = async (containerId, code, language) => {
  const container = document.getElementById(containerId)
  if (!container) return

  try {
    const monaco = await ensureMonacoReady()

    let monacoLang = "plaintext"
    if (language) {
      const searchLang = language.toLowerCase()
      const languages = monaco.languages.getLanguages()

      const match = languages.find(
        (l) =>
          l.id.toLowerCase() === searchLang ||
          (l.aliases && l.aliases.some((a) => a.toLowerCase() === searchLang)) ||
          (l.extensions && l.extensions.some((e) => e.toLowerCase() === `.${searchLang}`)) ||
          (l.extensions && l.extensions.some((e) => e.toLowerCase() === searchLang)),
      )

      if (match) {
        monacoLang = match.id
      } else {
        // Fallbacks for common mappings not directly in aliases
        const fallbackMap = {
          md: "markdown",
          sh: "shell",
          bash: "shell",
          py: "python",
          yml: "yaml",
        }
        monacoLang = fallbackMap[searchLang] || searchLang
      }
    }

    const lines = code.split("\n").length
    const lineHeight = 19
    const editorHeight = Math.min(Math.max(lines * lineHeight + 20, 80), 800)
    container.style.height = `${editorHeight}px`

    // Define a custom theme matching the application design
    monaco.editor.defineTheme("personal-website-theme", {
      base: "vs-dark",
      inherit: true,
      rules: [
        { token: "comment", foreground: "7F9870" },
        { token: "keyword", foreground: "E0B661" },
        { token: "string", foreground: "C7B17A" },
        { token: "number", foreground: "A8AA9F" },
        { token: "type", foreground: "D3C7A2" },
        { token: "delimiter", foreground: "AAA99E" },
      ],
      colors: {
        "editor.background": "#10110f",
        "editor.foreground": "#eee9dc",
        "editorCursor.foreground": "#e0b661",
        "editorLineNumber.foreground": "#74746b",
        "editorLineNumber.activeForeground": "#c89a49",
        "editor.selectionBackground": "#c89a4938",
        "editor.inactiveSelectionBackground": "#c89a4924",
        "editor.lineHighlightBackground": "#20201966",
        "editorWhitespace.foreground": "#48483c",
        "editorIndentGuide.background1": "#303129",
        "editorIndentGuide.activeBackground1": "#48483c",
        "editorGutter.background": "#10110f",
        "minimap.background": "#10110f",
        "scrollbarSlider.background": "#aaa99e20",
        "scrollbarSlider.hoverBackground": "#c89a4940",
        "scrollbarSlider.activeBackground": "#e0b66155",
      },
    })

    // Clear previous content if any
    container.innerHTML = ""

    monaco.editor.create(container, {
      value: code,
      language: monacoLang,
      theme: "personal-website-theme",
      readOnly: true,
      minimap: { enabled: false },
      fontFamily: '"Azeret Mono", Consolas, Monaco, "Courier New", monospace',
      fontLigatures: true,
      fontSize: 14,
      scrollBeyondLastLine: false,
      automaticLayout: true,
      lineNumbers: "on",
      renderLineHighlight: "none",
      domReadOnly: true,
      contextmenu: false,
      scrollbar: {
        vertical: "auto",
        horizontal: "auto",
      },
    })
  } catch (e) {
    console.error("Monaco editor error:", e)
    // Fallback
    container.innerHTML = `<pre style="margin:0; padding:1rem; overflow:auto;"><code>${code}</code></pre>`
  }
}
