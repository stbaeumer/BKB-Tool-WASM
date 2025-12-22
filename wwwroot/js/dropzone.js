// Einfacher, robuster Dropzone-Stub für Blazor-WASM
window.setupDropzone = (elementRef, serializedCriteria, dotNetRef, configId) => {
  try {
    const el = elementRef instanceof Element ? elementRef : document.getElementById(elementRef.id);
    if (!el) return;
    if (el.dataset.dropzoneInitialized === "1") return;
    el.dataset.dropzoneInitialized = "1";

    // verstecktes Input zur Dateiauswahl bei Klick
    const input = document.createElement("input");
    input.type = "file";
    input.multiple = true;
    input.style.display = "none";
    document.body.appendChild(input);

    const readAndSend = (file) => {
      const reader = new FileReader();
      reader.onload = async (ev) => {
        const arrayBuffer = ev.target.result;
        // ArrayBuffer -> Base64
        const bytes = new Uint8Array(arrayBuffer);
        let binary = "";
        const chunkSize = 0x8000;
        for (let i = 0; i < bytes.length; i += chunkSize) {
          binary += String.fromCharCode.apply(null, bytes.subarray(i, i + chunkSize));
        }
        const base64 = btoa(binary);
        try {
          await dotNetRef.invokeMethodAsync("OnDropFile", file.name, base64);
        } catch (e) {
          console.debug("dotNet invoke failed:", e);
        }
      };
      reader.readAsArrayBuffer(file);
    };

    // Klick -> Datei auswählen
    el.addEventListener("click", (ev) => {
      ev.preventDefault();
      input.value = "";
      input.onchange = () => {
        for (const f of input.files) readAndSend(f);
      };
      input.click();
    });

    // Dragover: verhindern, damit Browser nicht navigiert
    el.addEventListener("dragover", (ev) => {
      ev.preventDefault();
      ev.dataTransfer.dropEffect = "copy";
      el.classList.add("dragover");
    });
    el.addEventListener("dragleave", (ev) => {
      el.classList.remove("dragover");
    });

    // Drop: Dateien verarbeiten und Default verhindern (verhindert Öffnen im Browser)
    el.addEventListener("drop", (ev) => {
      ev.preventDefault();
      ev.stopPropagation();
      el.classList.remove("dragover");
      const files = ev.dataTransfer.files;
      for (let i = 0; i < files.length; i++) {
        readAndSend(files[i]);
      }
    });

    console.debug("dropzone initialized for", configId);
  } catch (ex) {
    console.debug("setupDropzone error:", ex);
  }
};

console.debug('dropzone.js loaded');