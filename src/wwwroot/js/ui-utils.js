window.focusElementById = (elementId) => {
  const element = document.getElementById(elementId);
  if (element) {
    element.focus();
  }
};

window.__bodyScrollLockCount = 0;

window.lockBodyScroll = () => {
  window.__bodyScrollLockCount += 1;

  if (window.__bodyScrollLockCount === 1) {
    document.body.style.overflow = "hidden";
    document.documentElement.style.overflow = "hidden";
  }
};

window.unlockBodyScroll = () => {
  window.__bodyScrollLockCount = Math.max(0, window.__bodyScrollLockCount - 1);

  if (window.__bodyScrollLockCount === 0) {
    document.body.style.overflow = "";
    document.documentElement.style.overflow = "";
  }
};

window.downloadFile = (url, fileName) => {
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
};
