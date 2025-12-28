# BKB-Tool-WASM

Dieses Projekt enthält eine Blazor WebAssembly Anwendung zur Verarbeitung von SchILD-NRW Exporten (Mailadressen setzen, Webuntis & Co).

## Konsolen-App (lokales Debuggen der Funktionen)

Zusätzlich zur WASM-App gibt es eine Konsolenanwendung unter `ConsoleApp/`, mit der die gleichen Verarbeitungsfunktionen gegen die vorhandenen Testdateien ausgeführt und schrittweise entwickelt/debuggt werden können.

- Gemeinsame Logik: Die Konsolen-App verlinkt die Quellcodes aus `Models/` und `Services/`. Änderungen an diesen Dateien wirken sofort sowohl in der Konsolen- als auch in der WASM-App.
- Testfälle: Die Konsolen-App liest `wwwroot/config/functions.json` und fragt – falls vorhanden – nach der gewünschten „Fall_#“-Nummer. Testfiles ohne `Fall_#_` Präfix werden immer mitgeladen.
- Ausgaben: Ergebnisse werden in `ConsoleOutput/<funktions-id>/` geschrieben.

### Starten

```bash
cd /workspaces/BKB-Tool-WASM/ConsoleApp
dotnet build
dotnet run
```

Danach im Terminal die gewünschte Funktion und ggf. die Fall-Nummer eingeben. Bei „Mailadresse setzen“ wird zusätzlich die Mail-Domain abgefragt (Standardwert aus der Konfiguration ist vorbelegt).