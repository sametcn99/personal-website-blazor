window.__mermaidReadyPromise = null;

window.ensureMermaidReady = async () => {
  if (window.mermaid) {
    return window.mermaid;
  }

  if (!window.__mermaidReadyPromise) {
    window.__mermaidReadyPromise =
      import("https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs")
        .then((module) => {
          const mermaid = module.default;
          mermaid.initialize({
            startOnLoad: false,
            theme: "dark",
          });
          window.mermaid = mermaid;
          return mermaid;
        })
        .catch((error) => {
          window.__mermaidReadyPromise = null;
          throw error;
        });
  }

  return window.__mermaidReadyPromise;
};

window.renderMermaidDiagram = async (containerId, definition) => {
  const container = document.getElementById(containerId);
  if (!container || !definition) {
    return;
  }

  try {
    const mermaid = await window.ensureMermaidReady();
    const renderId = `mermaid-svg-${containerId}-${Date.now()}`;
    const result = await mermaid.render(renderId, definition);
    container.innerHTML = result.svg;
  } catch (error) {
    console.error("Mermaid render error:", error);
    container.innerHTML = `<pre style="margin:0; overflow:auto;">${definition}</pre>`;
  }
};

window.renderMermaidToSvg = async (definition, diagramId) => {
  if (!definition) {
    return "";
  }

  const mermaid = await window.ensureMermaidReady();
  const renderId = diagramId || `mermaid-svg-${Date.now()}`;
  const result = await mermaid.render(renderId, definition.trim());
  return result.svg || "";
};

window.downloadSvgMarkup = (svgMarkup, filenamePrefix = "mermaid-diagram") => {
  if (!svgMarkup) {
    return;
  }

  const parser = new DOMParser();
  const doc = parser.parseFromString(svgMarkup, "image/svg+xml");
  const svgElement = doc.querySelector("svg");

  if (!svgElement) {
    return;
  }

  if (!svgElement.getAttribute("xmlns")) {
    svgElement.setAttribute("xmlns", "http://www.w3.org/2000/svg");
  }

  if (!svgElement.getAttribute("xmlns:xlink")) {
    svgElement.setAttribute("xmlns:xlink", "http://www.w3.org/1999/xlink");
  }

  if (!svgElement.getAttribute("viewBox")) {
    const width = svgElement.getAttribute("width") || "800";
    const height = svgElement.getAttribute("height") || "600";
    svgElement.setAttribute("viewBox", `0 0 ${width} ${height}`);
  }

  const svgString = new XMLSerializer().serializeToString(svgElement);
  const blob = new Blob([svgString], { type: "image/svg+xml;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const timestamp = new Date().toISOString().slice(0, 19).replace(/[:.]/g, "-");
  const link = document.createElement("a");
  link.href = url;
  link.download = `${filenamePrefix}-${timestamp}.svg`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
};

function decorateCodeBlocks() {
  console.log("Decorating code blocks...");
  const codeBlocks = document.querySelectorAll("pre code");

  if (codeBlocks.length === 0) {
    console.log("No code blocks found.");
  }

  codeBlocks.forEach((block) => {
    const pre = block.parentElement;

    if (pre.closest(".code-component-container")) return;

    if (pre.parentNode.classList.contains("code-block-wrapper")) return;

    console.log("Processing block:", block.className);

    let lang = "text";
    block.classList.forEach((cls) => {
      if (cls.startsWith("language-")) {
        lang = cls.replace("language-", "");
      }
    });

    const wrapper = document.createElement("div");
    wrapper.className = "code-block-wrapper";

    const header = document.createElement("div");
    header.className = "code-block-header";

    const langSpan = document.createElement("span");
    langSpan.className = "code-lang";
    langSpan.textContent = lang;

    const copyBtn = document.createElement("button");
    copyBtn.className = "copy-btn";
    copyBtn.title = "Copy to clipboard";
    copyBtn.innerHTML = `
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
            </svg>
        `;
    copyBtn.onclick = () => {
      navigator.clipboard.writeText(block.textContent);
      const originalHTML = copyBtn.innerHTML;
      copyBtn.innerHTML =
        '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="#4ade80" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>';
      setTimeout(() => {
        copyBtn.innerHTML = originalHTML;
      }, 2000);
    };

    header.appendChild(langSpan);
    header.appendChild(copyBtn);

    if (window.hljs) {
      window.hljs.highlightElement(block);
    }

    pre.parentNode.insertBefore(wrapper, pre);
    wrapper.appendChild(header);
    wrapper.appendChild(pre);
  });
}

window.__contentInitState = window.__contentInitState || {
  lastPath: "",
  lastRunAt: 0,
};

window.initContent = () => {
  const currentPath = `${window.location.pathname}${window.location.search}`;
  const now = Date.now();

  // Prevent duplicate init runs caused by rapid consecutive renders.
  if (
    window.__contentInitState.lastPath === currentPath
    && now - window.__contentInitState.lastRunAt < 1000
  ) {
    return;
  }

  window.__contentInitState.lastPath = currentPath;
  window.__contentInitState.lastRunAt = now;

  console.log("Initializing content...");
  decorateCodeBlocks();
};

window.highlightCode = (elementId) => {
  const element = document.getElementById(elementId);
  if (element && window.Prism) {
    window.Prism.highlightElement(element);
  }
};
