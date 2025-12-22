// Minimal stub — stellt downloadFile bereit und verhindert 404
window.downloadFile = (fileName, base64) => {
  const link = document.createElement('a');
  link.href = 'data:application/octet-stream;base64,' + base64;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
};
// Helper to log a large .NET object (serialized JSON) as an actual JS object in the console
window.debugLogObject = (json) => {
  try {
    const obj = JSON.parse(json);
    console.log(obj);
  }
  catch (e) {
    console.log(json);
  }
};
console.debug('localStorage.js loaded (stub)');