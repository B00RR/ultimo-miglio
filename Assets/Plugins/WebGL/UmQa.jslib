mergeInto(LibraryManager.library, {
  // Ponte di lettura per il playtest (?qa=1). Non disegna nulla: pubblica lo stato che il browser già ha in gioco.
  UmQaSet__deps: [],
  UmQaSet: function (ptr) {
    var raw = UTF8ToString(ptr);
    try { window.umQa = JSON.parse(raw); }
    catch (e) { window.umQa = { errore: String(e), raw: raw }; }
    window.umQaAt = Date.now();
  }
});
