using System.Text;
using System.Globalization;
using BKBToolClient.Models;

namespace BKBToolClient.Services;

public partial class FileProcessingService
{
    /// <summary>
    /// Bereinigt ein Fach, indem eine Ziffer am Ende entfernt wird (z.B. M1 -> M).
    /// Wenn das Fach Leerzeichen enthält, wird nicht bereinigt (z.B. M G1 bleibt M G1).
    /// </summary>
    private string BereinigenFach(string? fach)
    {
        if (string.IsNullOrEmpty(fach) || fach.Length <= 1)
            return fach ?? string.Empty;

        // Wenn das Fach Leerzeichen enthält, nicht bereinigen
        if (fach.Contains(' '))
            return fach;

        // Wenn das letzte Zeichen eine Ziffer ist, entfernen
        if (char.IsDigit(fach[^1]))
            return fach.Substring(0, fach.Length - 1);

        return fach;
    }

    /// <summary>
    /// Prüft, ob eine Zeile ein Kurs ist oder zu einem Kurs gehört.
    /// Ein Kurs ist definiert durch:
    /// 1. Field42 (Schülergruppe) ist nicht leer
    /// 2. Field1 (UnterrichtsId) kommt mehrfach vor
    /// 3. Dasselbe Fach (Field7) und dieselbe Klasse (Field5) kommen mehrfach vor mit unterschiedlichen Lehrern
    /// </summary>
    private bool ZeileIstKursOderGehörtZuKurs(List<Dictionary<string, string>> gpu002, Dictionary<string, string> dict)
    {
        // Ein Kurs ist definiert, wenn Field42 (Schülergruppe) nicht leer ist
        if (dict.ContainsKey("Field42") && !string.IsNullOrEmpty(dict["Field42"]))
        {
            return true;
        }

        // Ein Kurs ist auch definiert, wenn Field1 (UnterrichtsId) mehrfach vorkommt
        if (dict.ContainsKey("Field1") && !string.IsNullOrEmpty(dict["Field1"]))
        {
            var field1 = dict["Field1"];
            var count = gpu002.Count(record => 
                record.ContainsKey("Field1") && record["Field1"] == field1);
            
            if (count > 1)
            {
                return true;
            }
        }

        // Ein Kurs ist definiert, wenn dasselbe Fach (Field7) und dieselbe Klasse (Field5) 
        // mehrfach vorkommen mit unterschiedlichen Lehrern
        if (!dict.ContainsKey("Field7") || !dict.ContainsKey("Field5") || 
            !dict.ContainsKey("Field6") || !dict.ContainsKey("Field42"))
        {
            return false;
        }

        var bereinigtesFach = BereinigenFach(dict["Field7"]);
        var klasse = dict["Field5"];
        var lehrer = dict["Field6"];
        var field42 = dict["Field42"];

        var matchingRecord = gpu002.FirstOrDefault(record =>
        {
            if (!record.ContainsKey("Field7") || !record.ContainsKey("Field5") || 
                !record.ContainsKey("Field6") || !record.ContainsKey("Field42"))
                return false;

            return BereinigenFach(record["Field7"]) == bereinigtesFach &&
                   record["Field5"] == klasse &&
                   record["Field42"] == field42 &&
                   record["Field6"] != lehrer;
        });

        return matchingRecord != null;
    }

    /// <summary>
    /// Verarbeitet Kursunterrichte aus GPU002.txt
    /// </summary>
    public async Task<ProcessingResult> ProcessKursunterrichte(
        List<RequiredFile> files, 
        Dictionary<string, string> inputs,
        FunctionConfig? config = null)
    {
        await Task.Yield();
        var result = new ProcessingResult { Success = false };

        try
        {
            var gpuFile = files.FirstOrDefault(f => f.FileKey == "gpu002");
            var kurseFile = files.FirstOrDefault(f => f.FileKey == "kurse");
            var studentgroupFile = files.FirstOrDefault(f => f.FileKey == "studentgroupstudents");

            if (gpuFile?.Content == null)
            {
                result.Message = "GPU002.txt wird benötigt.";
                return result;
            }

            // Parse GPU002
            var gpu002 = ParseGpu002(gpuFile);

            // Ordne die GPU002 aufsteigend nach Field6 (Lehrer)
            // Dadurch wird die erste Lehrkraft im Alphabet zum Kursleiter
            gpu002 = gpu002.OrderBy(record => 
                record.ContainsKey("Field6") ? record["Field6"] : string.Empty).ToList();

            var kursunterrichte = new List<Kursunterricht>();
            var nichtKursunterrichte = new List<Kursunterricht>();

            foreach (var record in gpu002)
            {
                // Ohne eingetragenen Lehrer wird die Zeile übersprungen
                if (!record.ContainsKey("Field6") || string.IsNullOrEmpty(record["Field6"]))
                    continue;

                var unterrichtsId = record.ContainsKey("Field1") ? record["Field1"] : string.Empty;
                var fach = BereinigenFach(record.ContainsKey("Field7") ? record["Field7"] : string.Empty);
                var schuelergruppe = record.ContainsKey("Field42") ? record["Field42"] : string.Empty;
                var klasse = record.ContainsKey("Field5") ? record["Field5"] : string.Empty;
                var lehrer = record["Field6"];
                
                if (!record.ContainsKey("Field2") || !int.TryParse(record["Field2"], out int wochenstundenLehrkraft))
                    wochenstundenLehrkraft = 0;

                // Unterricht mit 0 Wochenstunden werden nicht berücksichtigt
                if (wochenstundenLehrkraft == 0)
                    continue;

                // Prüfen, ob es ein Kurs ist
                bool istKurs = ZeileIstKursOderGehörtZuKurs(gpu002, record);

                if (istKurs)
                {
                    // Suche nach einem bestehenden Kurs mit identischer UntisID
                    var kurs = kursunterrichte.FirstOrDefault(k => 
                        k.UnterrichtsIds.Contains(unterrichtsId));

                    if (kurs == null)
                    {
                        // Suche nach Kurs mit gleichem Fach und Schülergruppe
                        kurs = kursunterrichte.FirstOrDefault(k =>
                            BereinigenFach(k.Fach) == BereinigenFach(fach) &&
                            k.Schülergruppe == schuelergruppe &&
                            (k.Klassen.Contains(klasse) ||
                                (k.KursBez.StartsWith(lehrer) && k.KursBez.Contains(unterrichtsId))));
                    }

                    if (kurs == null)
                    {
                        // Neuen Kurs erstellen
                        var kursBez = $"{lehrer}-{unterrichtsId}";
                        kurs = new Kursunterricht
                        {
                            KursBez = kursBez,
                            Fach = fach,
                            Schülergruppe = schuelergruppe,
                            Klassen = new List<string> { klasse },
                            Kursleiter = lehrer,
                            KursleiterWochenstunden = wochenstundenLehrkraft,
                            Wochenstunden = wochenstundenLehrkraft,
                            UnterrichtsIds = new List<string> { unterrichtsId },
                            Lehrkräfte = new List<string> { lehrer },
                            LehrkräfteWochenstunden = new List<int> { wochenstundenLehrkraft }
                        };
                        kursunterrichte.Add(kurs);
                    }
                    else
                    {
                        // Bestehenden Kurs aktualisieren
                        if (!kurs.UnterrichtsIds.Contains(unterrichtsId))
                            kurs.UnterrichtsIds.Add(unterrichtsId);
                        
                        if (!kurs.Klassen.Contains(klasse))
                            kurs.Klassen.Add(klasse);

                        if (!kurs.Lehrkräfte.Contains(lehrer))
                        {
                            kurs.Lehrkräfte.Add(lehrer);
                            kurs.LehrkräfteWochenstunden.Add(wochenstundenLehrkraft);
                        }
                        else
                        {
                            var idx = kurs.Lehrkräfte.IndexOf(lehrer);
                            kurs.LehrkräfteWochenstunden[idx] += wochenstundenLehrkraft;
                        }

                        kurs.Wochenstunden += wochenstundenLehrkraft;
                    }
                }
                else
                {
                    // Nicht-Kursunterricht
                    var nichtKurs = nichtKursunterrichte.FirstOrDefault(u =>
                        BereinigenFach(u.Fach) == BereinigenFach(fach) &&
                        u.Kursleiter == lehrer &&
                        u.Schülergruppe == schuelergruppe &&
                        u.Klassen.Contains(klasse));

                    if (nichtKurs == null)
                    {
                        nichtKurs = new Kursunterricht
                        {
                            Fach = fach,
                            Schülergruppe = schuelergruppe,
                            Klassen = new List<string> { klasse },
                            Kursleiter = lehrer,
                            KursleiterWochenstunden = wochenstundenLehrkraft,
                            Wochenstunden = wochenstundenLehrkraft,
                            UnterrichtsIds = new List<string> { unterrichtsId },
                            Lehrkräfte = new List<string> { lehrer },
                            LehrkräfteWochenstunden = new List<int> { wochenstundenLehrkraft }
                        };
                        nichtKursunterrichte.Add(nichtKurs);
                    }
                    else
                    {
                        if (!nichtKurs.UnterrichtsIds.Contains(unterrichtsId))
                            nichtKurs.UnterrichtsIds.Add(unterrichtsId);
                        
                        nichtKurs.KursleiterWochenstunden += wochenstundenLehrkraft;
                        nichtKurs.Wochenstunden += wochenstundenLehrkraft;
                    }
                }
            }

            // Kurse.dat erstellen
            if (!inputs.TryGetValue("abschnitt", out var abschnittStr) || !int.TryParse(abschnittStr, out var abschnitt) || abschnitt < 1 || abschnitt > 4)
            {
                result.Message = "Ungültiger Abschnitt. Zulässig sind die Werte 1-4.";
                return result;
            }

            // Schuljahr berechnen (1.8. - 31.7.)
            var today = DateTime.Now;
            var schuljahr = today.Month >= 8 ? today.Year : today.Year - 1;

            var kurseDatLines = new List<string>();
            
            // Header für Kurse.dat
            var header = "KursBez|Klasse|Jahr|Abschnitt|Jahrgang|Fach|Kursart|Wochenstd.|Wochenstd. KL|Kursleiter|Epochenunterricht|Schulnr";
            kurseDatLines.Add(header);

            // Kurse in die Datei schreiben
            foreach (var kurs in kursunterrichte.OrderBy(k => k.KursBez))
            {
                var fields = new List<string>();
                
                // KursBez (max 20 Zeichen)
                fields.Add(kurs.KursBez.Length > 20 ? kurs.KursBez.Substring(0, 20) : kurs.KursBez);
                
                // Klasse (leer, da Schülergruppen verwendet)
                fields.Add("");
                
                // Jahr
                fields.Add(schuljahr.ToString());
                
                // Abschnitt
                fields.Add(abschnitt.ToString());
                
                // Jahrgang (leer)
                fields.Add("");
                
                // Fach
                fields.Add(kurs.Fach);
                
                // Kursart (Standard: "LK" für Leistungskurs, kann aus GPU002 erweitert werden)
                fields.Add("LK");
                
                // Wochenstunden
                fields.Add(kurs.Wochenstunden.ToString());
                
                // Wochenstunden Kursleiter
                fields.Add(kurs.KursleiterWochenstunden.ToString());
                
                // Kursleiter
                fields.Add(kurs.Kursleiter);
                
                // Epochenunterricht (leer)
                fields.Add("");
                
                // Schulnr (leer)
                fields.Add("");

                // Zusatzkräfte hinzufügen
                if (kurs.Lehrkräfte.Count > 1)
                {
                    for (int i = 1; i < kurs.Lehrkräfte.Count; i++)
                    {
                        fields.Add(kurs.LehrkräfteWochenstunden[i].ToString()); // Wochenstd. ZK
                        fields.Add(EscapeCsv(kurs.Lehrkräfte[i])); // Zusatzkraft
                    }
                }

                kurseDatLines.Add(string.Join("|", fields));
            }

            var kurseDatBytes = Encoding.UTF8.GetBytes(string.Join("\n", kurseDatLines));

            result.Success = true;
            result.OutputFiles = new List<OutputFile>
            {
                new OutputFile
                {
                    FileName = "Kurse.dat",
                    Content = kurseDatBytes,
                    FileSize = kurseDatBytes.LongLength,
                    LineCount = kurseDatLines.Count - 1,
                    Hint = "Neue Kurse.dat zum Import in SchILD-NRW"
                }
            };

            result.Message = $"{kursunterrichte.Count} Kursunterrichte und {nichtKursunterrichte.Count} Nicht-Kursunterrichte verarbeitet.";

            return result;
        }
        catch (Exception ex)
        {
            result.Message = $"Fehler bei der Verarbeitung: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Parsed GPU002 Datei und gibt Liste von Dictionaries zurück
    /// </summary>
    private List<Dictionary<string, string>> ParseGpu002(RequiredFile gpuFile)
    {
        var delimiter = DetermineDelimiter(gpuFile.Content!, gpuFile.Delimiter ?? "|");
        var quote = string.IsNullOrEmpty(gpuFile.Quote) ? (char?)null : gpuFile.Quote[0];
        var (enc, _) = ResolveEncodingForReader(gpuFile.Content!, gpuFile.Encoding);
        var text = enc.GetString(gpuFile.Content!);
        if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF')
            text = text[1..];

        var lines = text.Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var result = new List<Dictionary<string, string>>();

            string StripOuterQuotes(string? val)
            {
                if (string.IsNullOrEmpty(val)) return string.Empty;
                var trimmed = val.Trim();
                if (trimmed.Length >= 2 && trimmed.StartsWith('"') && trimmed.EndsWith('"'))
                    return trimmed[1..^1];
                return trimmed;
            }

            foreach (var line in lines)
            {
                var fields = SplitRow(line, delimiter, quote);
                var dict = new Dictionary<string, string>();

                for (int i = 0; i < fields.Length; i++)
                {
                    dict[$"Field{i + 1}"] = StripOuterQuotes(fields[i]);
                }

                result.Add(dict);
            }

        return result;
    }

    /// <summary>
    /// Hilfsklasse für Kursunterrichte
    /// </summary>
    private class Kursunterricht
    {
        public string KursBez { get; set; } = string.Empty;
        public string Fach { get; set; } = string.Empty;
        public string Schülergruppe { get; set; } = string.Empty;
        public List<string> Klassen { get; set; } = new();
        public string Kursleiter { get; set; } = string.Empty;
        public int KursleiterWochenstunden { get; set; }
        public int Wochenstunden { get; set; }
        public List<string> UnterrichtsIds { get; set; } = new();
        public List<string> Lehrkräfte { get; set; } = new();
        public List<int> LehrkräfteWochenstunden { get; set; } = new();
    }
}
