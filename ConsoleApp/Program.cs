using System.Text;
using System.Text.Json;
using BKBToolClient.Models;
using BKBToolClient.Services;

internal partial class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Enable code pages for non-UTF8 encodings (e.g., 1252)
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }

        var repoRoot = FindRepoRoot();
        var wwwroot = Path.Combine(repoRoot, "wwwroot");
        var functionsJsonPath = Path.Combine(wwwroot, "config", "functions.json");

        if (!File.Exists(functionsJsonPath))
        {
            Console.Error.WriteLine($"Konfigurationsdatei nicht gefunden: {functionsJsonPath}");
            return 2;
        }

        var functions = await LoadFunctionsAsync(functionsJsonPath);
        if (functions.Count == 0)
        {
            Console.Error.WriteLine("Keine Funktionen in functions.json gefunden.");
            return 3;
        }

        Console.WriteLine("Verfügbare Funktionen:");
        for (int i = 0; i < functions.Count; i++)
            Console.WriteLine($"  [{i + 1}] {functions[i].Name} (id={functions[i].Id})");

        Console.Write("Bitte Funktionsnummer wählen: ");
        var selStr = Console.ReadLine()?.Trim();
        if (!int.TryParse(selStr, out var selIdx) || selIdx < 1 || selIdx > functions.Count)
        {
            Console.Error.WriteLine("Ungültige Auswahl.");
            return 4;
        }

        var selected = functions[selIdx - 1];
        Console.WriteLine($"Gewählt: {selected.Name} ({selected.Id})\n");

        // Testfiles: gruppiere nach Fall_X_ und immer-einzulesende Dateien
        var testFiles = selected.TestFiles ?? new List<string>();
        var caseMap = GroupTestFilesByCase(testFiles);
        var alwaysFiles = caseMap.Always.Select(p => Path.Combine(wwwroot, p.TrimStart('/'))).ToList();

        List<string> chosenFiles = new();
        if (caseMap.Cases.Count > 0)
        {
            Console.WriteLine("Verfügbare Fälle:");
            foreach (var c in caseMap.Cases.Keys.OrderBy(k => k))
                Console.WriteLine($"  Fall {c}");
            Console.Write("Bitte Fall-Nummer wählen (z.B. 1): ");
            var cStr = Console.ReadLine()?.Trim();
            if (!int.TryParse(cStr, out var cNum) || !caseMap.Cases.ContainsKey(cNum))
            {
                Console.Error.WriteLine("Ungültiger Fall.");
                return 5;
            }
            chosenFiles.AddRange(caseMap.Cases[cNum].Select(p => Path.Combine(wwwroot, p.TrimStart('/'))));
        }
        else
        {
            Console.WriteLine("Keine Fall_* Dateien – verwende Standard-Testfiles.");
        }
        chosenFiles.AddRange(alwaysFiles);

        // Mappe ausgewählte Dateien auf RequiredFiles anhand Dateinamen-Heuristik
        var filesForProcessing = await BuildRequiredFilesAsync(selected, chosenFiles);

        // Validierung der Pflichtdateien
        var missing = filesForProcessing.Where(f => f.IsRequired && (f.Content == null || f.Content.Length == 0)).ToList();
        if (missing.Count > 0)
        {
            Console.Error.WriteLine("Fehlende Pflichtdateien:");
            foreach (var m in missing) Console.Error.WriteLine($" - {m.Name} (Key={m.FileKey})");
            return 6;
        }

        // Eingabefelder sammeln (z.B. Mail-Domain)
        var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in selected.InputFields ?? new List<InputField>())
        {
            var def = string.IsNullOrWhiteSpace(f.DefaultValue) ? string.Empty : f.DefaultValue;
            Console.Write($"{f.Label}{(string.IsNullOrWhiteSpace(f.Placeholder) ? "" : " (" + f.Placeholder + ")")}: ");
            var v = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(v)) v = def;
            inputs[f.Key] = v ?? string.Empty;
        }

        // Verarbeitung ausführen
        var svc = new FileProcessingService();
        ProcessingResult result;
        if (string.Equals(selected.Id, "mailadresse-setzen", StringComparison.OrdinalIgnoreCase))
        {
            result = await svc.ProcessMailadresseSetzen(filesForProcessing, inputs, selected);
        }
        else if (string.Equals(selected.Id, "webuntis-co", StringComparison.OrdinalIgnoreCase))
        {
            result = await svc.ProcessWebuntis(filesForProcessing, inputs);
        }
        else
        {
            Console.Error.WriteLine($"Unbekannte Funktion: {selected.Id}");
            return 7;
        }

        // Ergebnis anzeigen und Ausgabedateien ablegen
        Console.WriteLine();
        Console.WriteLine(result.Success ? "✓ Erfolg" : "✗ Fehler");
        if (!string.IsNullOrWhiteSpace(result.Message)) Console.WriteLine(result.Message.Trim());

        var outDir = Path.Combine(repoRoot, "ConsoleOutput", selected.Id);
        Directory.CreateDirectory(outDir);
        foreach (var of in result.OutputFiles ?? new List<OutputFile>())
        {
            var fname = string.IsNullOrWhiteSpace(of.FileName) ? "output.bin" : of.FileName;
            var path = Path.Combine(outDir, fname);
            await File.WriteAllBytesAsync(path, of.Content ?? Array.Empty<byte>());
            Console.WriteLine($"→ geschrieben: {path}");
            if (!string.IsNullOrWhiteSpace(of.Hint)) Console.WriteLine($"   Hinweis: {of.Hint}");
            if (!string.IsNullOrWhiteSpace(of.ProcessingHint)) Console.WriteLine($"   Weiterverarbeitung: {of.ProcessingHint}");
        }

        Console.WriteLine();
        Console.WriteLine($"Fertig. Ausgaben unter: {outDir}");
        return result.Success ? 0 : 1;
    }

    private static string FindRepoRoot()
    {
        // Wir erwarten die Struktur: repo/ConsoleApp/bin/Debug/net10.0
        // Laufzeitverzeichnis -> hoch bis zum Repo-Wurzelordner
        var cwd = Environment.CurrentDirectory;
        var dir = new DirectoryInfo(cwd);
        while (dir != null && dir.Exists)
        {
            // Stop, wenn wir die Konfig finden
            var candidate = Path.Combine(dir.FullName, "wwwroot", "config", "functions.json");
            if (File.Exists(candidate)) return dir.FullName;
            dir = dir.Parent;
        }
        // Fallback: gehe eine Ebene nach oben
        return Directory.GetParent(cwd)?.FullName ?? cwd;
    }

    private static async Task<List<FunctionConfig>> LoadFunctionsAsync(string functionsJsonPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(functionsJsonPath, Encoding.UTF8);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };
            var wrapper = JsonSerializer.Deserialize<FunctionConfigWrapper>(json, opts);
            return wrapper?.Functions ?? new List<FunctionConfig>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler beim Laden der Funktionen: {ex.Message}");
            return new List<FunctionConfig>();
        }
    }

    private static async Task<List<RequiredFile>> BuildRequiredFilesAsync(FunctionConfig config, List<string> selectedPaths)
    {
        // Kopie der Required/Optional-Einträge mit gefüllten Content-Feldern
        var result = config.RequiredFiles.Select(CloneReq).Concat(config.OptionalFiles.Select(CloneReq)).ToList();

        foreach (var path in selectedPaths)
        {
            var fileName = Path.GetFileName(path);
            // Heuristik zur Zuordnung
            string? key = fileName switch
            {
                var s when s.StartsWith("Student_", StringComparison.OrdinalIgnoreCase) => "students",
                var s when s.Contains("SchuelerBasisdaten", StringComparison.OrdinalIgnoreCase) => "basisdaten",
                var s when s.Contains("SchuelerZusatzdaten", StringComparison.OrdinalIgnoreCase) => "zusatzdaten",
                var s when s.Contains("SchuelerAdressen", StringComparison.OrdinalIgnoreCase) => "adressen",
                var s when s.Contains("SchuelerErzieher", StringComparison.OrdinalIgnoreCase) => "erzieher",
                _ => null
            };

            if (key == null) continue;
            var target = result.FirstOrDefault(r => string.Equals(r.FileKey, key, StringComparison.OrdinalIgnoreCase));
            if (target == null) continue;

            try
            {
                var bytes = await File.ReadAllBytesAsync(path);
                target.Content = bytes;
                target.FileName = fileName;
                target.IsUploaded = true;
                target.LineCount = CountLines(bytes);
                target.ColumnCount = 0; // optional
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fehler beim Lesen {path}: {ex.Message}");
            }
        }

        return result;
    }

    private static int CountLines(byte[] bytes)
    {
        try
        {
            var s = Encoding.UTF8.GetString(bytes);
            return s.Split('\n').Length;
        }
        catch { return 0; }
    }

    private static RequiredFile CloneReq(RequiredFile r)
        => new RequiredFile
        {
            Name = r.Name,
            AllowedExtensions = r.AllowedExtensions,
            FileKey = r.FileKey,
            ExampleFileName = r.ExampleFileName,
            SourceHint = r.SourceHint,
            IsOptional = r.IsOptional,
            IsRequired = r.IsRequired,
            MatchingRegex = r.MatchingRegex,
            Delimiter = r.Delimiter,
            Encoding = r.Encoding,
            Quote = r.Quote,
            HasHeader = r.HasHeader
        };

    private static (Dictionary<int, List<string>> Cases, List<string> Always) GroupTestFilesByCase(List<string> testFiles)
    {
        var cases = new Dictionary<int, List<string>>();
        var always = new List<string>();
        foreach (var tf in testFiles)
        {
            var name = Path.GetFileName(tf);
            // Unterstützt "Fall_1_..." und "Fall1_..."
            var match = System.Text.RegularExpressions.Regex.Match(name, @"^Fall[_ ]?(\d+)_", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var num))
            {
                if (!cases.TryGetValue(num, out var list)) { list = new List<string>(); cases[num] = list; }
                list.Add(tf);
            }
            else
            {
                always.Add(tf);
            }
        }
        return (cases, always);
    }

    private class FunctionConfigWrapper
    {
        public List<FunctionConfig> Functions { get; set; } = new();
    }
}
