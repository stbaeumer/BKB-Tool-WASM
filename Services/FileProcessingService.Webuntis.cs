using BKBToolClient.Models;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Linq;

namespace BKBToolClient.Services;

public partial class FileProcessingService
{
    private static string GetDictValue(IDictionary<string, string>? d, params string[] keys)
    {
        if (d == null) return string.Empty;
        foreach (var k in keys)
        {
            if (d.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v)) return v!;
        }
        foreach (var kv in d)
        {
            var lower = kv.Key.ToLowerInvariant();
            foreach (var k in keys)
            {
                if (lower.Contains(k.ToLowerInvariant())) return kv.Value ?? string.Empty;
            }
        }
        return string.Empty;
    }

    // Compatibility alias used in older code paths
    private static string GetValue(IDictionary<string, string>? d, params string[] keys)
        => GetDictValue(d, keys);

    private class WebuntisStats
    {
        public int Unchanged { get; set; }
        public int Added { get; set; }
        public int Removed { get; set; }
        public int CsvCount { get; set; }
        public int NewCount { get; set; }
    }

    private class WebuntisNewStudent
    {
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;
        public string Geburtsdatum { get; set; } = string.Empty;
    }

    private class WebuntisDeletedStudent
    {
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;
        public string Geburtsdatum { get; set; } = string.Empty;
    }

    // Reusable matcher for other helper methods (matches by Nachname, Vorname, Geburtsdatum)
    private bool PersonMatches(IDictionary<string, string>? r, string lname, string fname, string bdate)
    {
        if (r == null) return false;
        var rln = GetDictValue(r, "Nachname", "Familienname", "name")?.Trim() ?? string.Empty;
        var rfn = GetDictValue(r, "Vorname", "givenName", "forename")?.Trim() ?? string.Empty;
        var rbd = GetDictValue(r, "Geburtsdatum", "birthDate", "Geburtsdatum")?.Trim() ?? string.Empty;

        if (!string.Equals(rln, (lname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(rfn, (fname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)) return false;

        var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" };
        if (!string.IsNullOrWhiteSpace(rbd) && !string.IsNullOrWhiteSpace(bdate))
        {
            if (DateTime.TryParseExact(rbd, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1) &&
                DateTime.TryParseExact(bdate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
            {
                return d1.Date == d2.Date;
            }
        }

        return string.Equals(rbd, (bdate ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProcessingResult> ProcessWebuntis(List<RequiredFile> files, Dictionary<string, string> inputs)
    {
        Console.WriteLine("ProcessWebuntis: CALLED");
        await Task.Yield();
        var result = new ProcessingResult { Success = true };

        // Globale CSV-Helfer für die ganze Methode
        string CsvSafe(string? v)
        {
            var s = v ?? string.Empty;
            if (s.Contains('"')) s = s.Replace("\"", "\"\"");
            if (s.IndexOfAny(new[] { ',', '\n', '\r', '"' }) >= 0) s = $"\"{s}\"";
            return s;
        }
        DateTime ParseDateOrMin(string? dt)
        {
            if (string.IsNullOrWhiteSpace(dt)) return DateTime.MinValue;
            var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" };
            if (DateTime.TryParseExact(dt, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return parsed;
            if (DateTime.TryParse(dt, new CultureInfo("de-DE"), DateTimeStyles.None, out parsed)) return parsed;
            if (DateTime.TryParse(dt, out parsed)) return parsed;
            return DateTime.MinValue;
        }
        string BoolJN(bool b) => b ? "J" : "N";

        try
        {
            Console.WriteLine("ProcessWebuntis: In try block");
            var studentsFile = files.FirstOrDefault(f => f.FileKey == "students");
            var basisFile = files.FirstOrDefault(f => f.FileKey == "basisdaten");
            var zusatzFile = files.FirstOrDefault(f => f.FileKey == "zusatzdaten");
            var erzieherFile = files.FirstOrDefault(f => f.FileKey == "erzieher");
            var adressenFile = files.FirstOrDefault(f => f.FileKey == "adressen");

            Console.WriteLine($"ProcessWebuntis: Files found - students={studentsFile?.FileName}, basisdaten={basisFile?.FileName}");

            // Require at least basis records. Other files are optional fallbacks (zusatz/adressen/erzieher/students)
            if (basisFile?.Content == null)
            {
                Console.WriteLine("ProcessWebuntis: No basisdaten file, returning error");
                return new ProcessingResult
                {
                    Success = false,
                    Message = "Für 'Webuntis & Co.' wird mindestens die Datei SchuelerBasisdaten.dat benötigt. Bitte laden Sie diese hoch. Optional können Sie SchuelerZusatzdaten.dat (für schulische E-Mails), SchuelerAdressen.dat und SchuelerErzieher.dat bereitstellen."
                };
            }

            // Parse students CSV (optional; may be absent if only basis/adress files are provided)
            var students = new List<Dictionary<string, string>>();
            if (studentsFile?.Content != null)
            {
                var studDel = studentsFile.Delimiter ?? ",";
                var studDelimiter = DetermineDelimiter(studentsFile.Content, studDel);
                var studQuote = string.IsNullOrEmpty(studentsFile.Quote) ? (char?)null : studentsFile.Quote[0];
                var (studEnc, _) = ResolveEncodingForReader(studentsFile.Content, studentsFile.Encoding);
                var studText = studEnc.GetString(studentsFile.Content);
                if (!string.IsNullOrEmpty(studText) && studText[0] == '\uFEFF') studText = studText.Substring(1);
                var studLines = studText.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                var studHeaderIdx = studLines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                if (studHeaderIdx >= 0)
                {
                    var studHeaders = SplitRow(studLines[studHeaderIdx], studDelimiter, studQuote);
                    for (int i = studHeaderIdx + 1; i < studLines.Count; i++)
                    {
                        var line = studLines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = SplitRow(line, studDelimiter, studQuote);
                        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for (int c = 0; c < studHeaders.Length; c++)
                        {
                            var key = studHeaders[c].Trim();
                            var val = c < fields.Length ? fields[c]?.Trim() ?? string.Empty : string.Empty;
                            dict[key] = val;
                        }
                        students.Add(dict);
                    }
                }
            }

            // Build uniqueStudents according to the rule
            List<Dictionary<string, string>> uniqueStudents;
            {
                static string GetField(IDictionary<string, string> d, Func<string, bool> match)
                {
                    var k = d.Keys.FirstOrDefault(x => match(x));
                    if (k != null && d.TryGetValue(k, out var v)) return v ?? string.Empty;
                    return string.Empty;
                }

                string Get(IDictionary<string, string> d, string key)
                    => d.TryGetValue(key, out var v) ? v : GetField(d, k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));

                string GetStatus(IDictionary<string, string> d)
                {
                    var k = d.Keys.FirstOrDefault(x => x.IndexOf("status", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (k != null && d.TryGetValue(k, out var v)) return v ?? string.Empty;
                    return string.Empty;
                }

                string GetBeginn(IDictionary<string, string> d)
                {
                    var k = d.Keys.FirstOrDefault(x => x.IndexOf("beginn", StringComparison.OrdinalIgnoreCase) >= 0 && x.IndexOf("bildung", StringComparison.OrdinalIgnoreCase) >= 0)
                            ?? d.Keys.FirstOrDefault(x => x.IndexOf("beginn", StringComparison.OrdinalIgnoreCase) >= 0)
                            ?? d.Keys.FirstOrDefault(x => x.IndexOf("bildung", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (k != null && d.TryGetValue(k, out var v)) return v ?? string.Empty;
                    return string.Empty;
                }

                uniqueStudents = students
                    .GroupBy(s => (Get(s, "foreName") + "|" + Get(s, "longName") + "|" + Get(s, "birthDate")).ToLowerInvariant())
                    .Select(g =>
                    {
                        var list = g.ToList();
                        if (list.Count == 1) return list[0];

                        var activeOrGuest = list.Where(s => {
                            var st = GetStatus(s);
                            return st == "2" || st == "6";
                        }).ToList();

                        if (activeOrGuest.Count > 1)
                        {
                            return activeOrGuest.OrderByDescending(s => ParseDateOrMin(GetBeginn(s))).First();
                        }

                        return list.OrderBy(s => { if (int.TryParse(GetStatus(s), out var i)) return i; return int.MaxValue; }).First();
                    })
                    .OrderBy(s => (s.TryGetValue("klasse", out var kk) ? kk : (s.TryGetValue("klasse.name", out var kkn) ? kkn : string.Empty)))
                    .ThenBy(s => (s.TryGetValue("longName", out var ln) ? ln : (s.TryGetValue("name", out var n) ? n : string.Empty)))
                    .ThenBy(s => (s.TryGetValue("foreName", out var fn) ? fn : (s.TryGetValue("forename", out var f2) ? f2 : string.Empty)))
                    .ToList();
            }

            // Zentraler Key-Helper für Vergleiche (Student_.csv vs. andere Quellen)
            string NormalizeDateForKey(string? dt)
            {
                var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" };
                if (!string.IsNullOrWhiteSpace(dt) && DateTime.TryParseExact(dt.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return parsed.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                return (dt ?? string.Empty).Trim().ToLowerInvariant();
            }
            string MakeKey(string? ln, string? fn, string? bd)
                => ($"{(ln ?? string.Empty).Trim().ToLowerInvariant()}|{(fn ?? string.Empty).Trim().ToLowerInvariant()}|{NormalizeDateForKey(bd)}");
            var studentCsvKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (uniqueStudents != null)
            {
                foreach (var s in uniqueStudents)
                {
                    s.TryGetValue("longName", out var ln);
                    s.TryGetValue("foreName", out var fn);
                    s.TryGetValue("birthDate", out var bd);
                    studentCsvKeys.Add(MakeKey(ln, fn, bd));
                }
            }

            // Parse Zusatz- and Basisdaten into dictionaries for lookups
            List<Dictionary<string, string>> zusatzRecords = new();
            try
            {
                if (zusatzFile?.Content != null)
                {
                    var (zenc, _) = ResolveEncodingForReader(zusatzFile.Content, zusatzFile.Encoding);
                    var ztext = zenc.GetString(zusatzFile.Content);
                    if (!string.IsNullOrEmpty(ztext) && ztext[0] == '\uFEFF') ztext = ztext.Substring(1);
                    var zlines = ztext.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                    var zhdrIdx = zlines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                    if (zhdrIdx >= 0)
                    {
                        var zhdr = SplitRow(zlines[zhdrIdx], DetermineDelimiter(zusatzFile.Content, zusatzFile.Delimiter ?? "\t"), string.IsNullOrEmpty(zusatzFile.Quote) ? (char?)null : zusatzFile.Quote[0]);
                        for (int i = zhdrIdx + 1; i < zlines.Count; i++)
                        {
                            var line = zlines[i];
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var fields = SplitRow(line, DetermineDelimiter(zusatzFile.Content, zusatzFile.Delimiter ?? "\t"), string.IsNullOrEmpty(zusatzFile.Quote) ? (char?)null : zusatzFile.Quote[0]);
                            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for (int c = 0; c < zhdr.Length; c++) dict[zhdr[c].Trim()] = c < fields.Length ? fields[c] : string.Empty;
                            // store data row index (first data row = 0)
                            dict["__rowIndex"] = (i - zhdrIdx - 1).ToString(CultureInfo.InvariantCulture);
                            zusatzRecords.Add(dict);
                        }
                    }
                }
            }
            catch { }
            

            // Parse Adressen (SchuelerAdressen)
            List<Dictionary<string, string>> adressenRecords = new();
            try
            {
                if (adressenFile?.Content != null)
                {
                    var (aenc, _) = ResolveEncodingForReader(adressenFile.Content, adressenFile.Encoding);
                    var atext = aenc.GetString(adressenFile.Content);
                    if (!string.IsNullOrEmpty(atext) && atext[0] == '\uFEFF') atext = atext.Substring(1);
                    var alines = atext.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                    var ahdrIdx = alines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                    if (ahdrIdx >= 0)
                    {
                        var ahdr = SplitRow(alines[ahdrIdx], DetermineDelimiter(adressenFile.Content, adressenFile.Delimiter ?? "\t"), string.IsNullOrEmpty(adressenFile.Quote) ? (char?)null : adressenFile.Quote[0]);
                        for (int i = ahdrIdx + 1; i < alines.Count; i++)
                        {
                            var line = alines[i];
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var fields = SplitRow(line, DetermineDelimiter(adressenFile.Content, adressenFile.Delimiter ?? "\t"), string.IsNullOrEmpty(adressenFile.Quote) ? (char?)null : adressenFile.Quote[0]);
                            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for (int c = 0; c < ahdr.Length; c++) dict[ahdr[c].Trim()] = c < fields.Length ? fields[c] : string.Empty;
                            adressenRecords.Add(dict);
                        }
                    }
                }
            }
            catch { }

            // Parse Erzieher (SchuelerErzieher)
            List<Dictionary<string, string>> erzieherRecords = new();
            try
            {
                if (erzieherFile?.Content != null)
                {
                    var (eenc, _) = ResolveEncodingForReader(erzieherFile.Content, erzieherFile.Encoding);
                    var etext = eenc.GetString(erzieherFile.Content);
                    if (!string.IsNullOrEmpty(etext) && etext[0] == '\uFEFF') etext = etext.Substring(1);
                    var elines = etext.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                    var ehdrIdx = elines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                    if (ehdrIdx >= 0)
                    {
                        var ehdr = SplitRow(elines[ehdrIdx], DetermineDelimiter(erzieherFile.Content, erzieherFile.Delimiter ?? "\t"), string.IsNullOrEmpty(erzieherFile.Quote) ? (char?)null : erzieherFile.Quote[0]);
                        for (int i = ehdrIdx + 1; i < elines.Count; i++)
                        {
                            var line = elines[i];
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var fields = SplitRow(line, DetermineDelimiter(erzieherFile.Content, erzieherFile.Delimiter ?? "\t"), string.IsNullOrEmpty(erzieherFile.Quote) ? (char?)null : erzieherFile.Quote[0]);
                            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for (int c = 0; c < ehdr.Length; c++) dict[ehdr[c].Trim()] = c < fields.Length ? fields[c] : string.Empty;
                            erzieherRecords.Add(dict);
                        }
                    }
                }
            }
            catch { }

            List<Dictionary<string, string>> basisRecords = new();
            // Build exportStudents after parsing all input records
            try
            {
                if (basisFile?.Content != null)
                {
                    var (benc, _) = ResolveEncodingForReader(basisFile.Content, basisFile.Encoding);
                    var btext = benc.GetString(basisFile.Content);
                    if (!string.IsNullOrEmpty(btext) && btext[0] == '\uFEFF') btext = btext.Substring(1);
                    var blines = btext.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                    var bhdrIdx = blines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                    if (bhdrIdx >= 0)
                    {
                        var detDel = DetermineDelimiter(basisFile.Content, basisFile.Delimiter ?? "\t");
                        var bhdr = SplitRow(blines[bhdrIdx], detDel, string.IsNullOrEmpty(basisFile.Quote) ? (char?)null : basisFile.Quote[0]);
                        var headerLen = bhdr.Length;
                        for (int i = bhdrIdx + 1; i < blines.Count; i++)
                        {
                            var line = blines[i];
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var fields = SplitRow(line, detDel, string.IsNullOrEmpty(basisFile.Quote) ? (char?)null : basisFile.Quote[0]);
                            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            var mapLen = Math.Min(headerLen, fields.Length);
                            for (int c = 0; c < mapLen; c++) dict[bhdr[c].Trim()] = fields[c];
                            for (int c = mapLen; c < headerLen; c++) dict[bhdr[c].Trim()] = string.Empty;
                            // store data row index (first data row = 0)
                            dict["__rowIndex"] = (i - bhdrIdx - 1).ToString(CultureInfo.InvariantCulture);
                            basisRecords.Add(dict);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    result.MessageHtml += "<pre>basis parse error: " + System.Net.WebUtility.HtmlEncode(ex.ToString()) + "</pre>";
                }
                catch { }
            }
            
            result.MessageHtml += $"<pre>✓ BASIS PARSED: {basisRecords?.Count ?? 0} records</pre>";
            
            // Build exportStudents after parsing all input records
            var exportStudents = uniqueStudents;
            // Build strongly typed students list from basisRecords (and enrich with zusatz/adressen/erzieher later)
            List<Models.Student> DatStudents = new();
            try
            {
                DatStudents = BuildTypedStudents(basisRecords ?? new(), zusatzRecords ?? new(), adressenRecords ?? new(), erzieherRecords ?? new());
                result.MessageHtml += $"<pre>DatStudents: {DatStudents.Count} students built.</pre>";
                // Also build typed students from the optional students CSV and print to dev console
                try
                {
                    var CsvStudents = BuildTypedStudentsFromStudentCsv(students);
                    result.MessageHtml += $"<pre>CsvStudents: {CsvStudents.Count} students built from students CSV.</pre>";
                    try { Console.WriteLine("CsvStudents: " + JsonSerializer.Serialize(CsvStudents)); } catch { Console.WriteLine($"CsvStudents count: {CsvStudents.Count}"); }
                }
                catch (Exception ex)
                {
                    result.MessageHtml += $"<pre>BuildTypedStudentsFromStudentCsv error: {System.Net.WebUtility.HtmlEncode(ex.Message)}</pre>";
                }
            }
            catch (Exception ex)
            {
                result.MessageHtml += $"<pre>BuildTypedStudents error: {System.Net.WebUtility.HtmlEncode(ex.Message)}</pre>";
            }

            // Compare CSV (uniqueStudents) vs new DatStudents to determine status counts
            try
            {
                var csvKeys = studentCsvKeys;
                var datKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var activeStudentKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Nur aktive Studenten (Status 2 oder 6)
                
                if (DatStudents != null)
                {
                    foreach (var ds in DatStudents)
                    {
                        var key = MakeKey(ds.Nachname, ds.Vorname, ds.Geburtsdatum);
                        datKeys.Add(key);
                        
                        // Prüfe ob dieser Student aktiv ist
                        var hasActiveStatus = ds.Bildungsgaenge?.Any(bg => bg.Status == "2" || bg.Status == "6") ?? false;
                        if (hasActiveStatus)
                        {
                            activeStudentKeys.Add(key);
                        }
                    }
                }

                // Capture preview of newly added students for UI (limit 10 to keep output small)
                var addedStudentsPreview = new List<WebuntisNewStudent>();
                var deletedStudentsPreview = new List<WebuntisDeletedStudent>();
                
                if (DatStudents != null && DatStudents.Count > 0)
                {
                    string GetLatestKlasse(Models.Student ds)
                    {
                        if (ds?.Bildungsgaenge != null && ds.Bildungsgaenge.Count > 0)
                        {
                            var latestBg = ds.Bildungsgaenge
                                .OrderByDescending(b => ParseDateOrMin(b.BeginnBildungsgang))
                                .FirstOrDefault(b => !string.IsNullOrWhiteSpace(b.Klasse));
                            if (latestBg != null && !string.IsNullOrWhiteSpace(latestBg.Klasse))
                                return latestBg.Klasse;
                        }
                        return ds?.Klasse ?? string.Empty;
                    }

                    foreach (var ds in DatStudents)
                    {
                        var key = MakeKey(ds.Nachname, ds.Vorname, ds.Geburtsdatum);
                        if (!csvKeys.Contains(key))
                        {
                            // Neuer Student (nicht in CSV)
                            var preview = new WebuntisNewStudent
                            {
                                Nachname = ds.Nachname ?? string.Empty,
                                Vorname = ds.Vorname ?? string.Empty,
                                Klasse = GetLatestKlasse(ds),
                                Geburtsdatum = ds.GeburtsdatumParsed?.ToString("dd.MM.yyyy") ?? (ds.Geburtsdatum ?? string.Empty)
                            };

                            addedStudentsPreview.Add(preview);
                            if (addedStudentsPreview.Count >= 10) break;
                        }
                    }

                    try
                    {
                        StoreFile("webuntis_added_students", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(addedStudentsPreview)));
                    }
                    catch { }
                }
                else
                {
                    try { StoreFile("webuntis_added_students", Encoding.UTF8.GetBytes("[]")); } catch { }
                }

                // Collect deleted students (in CSV, but not active in basis data OR completely missing)
                if (uniqueStudents != null && uniqueStudents.Count > 0)
                {
                    foreach (var s in uniqueStudents)
                    {
                        string GetS(string k) => s.TryGetValue(k, out var v) ? v : string.Empty;
                        var lname = GetS("longName");
                        var fname = GetS("foreName");
                        var bdate = GetS("birthDate");
                        var key = MakeKey(lname, fname, bdate);

                        // Student ist "gelöscht" wenn:
                        // 1. Er ist in CSV, aber nicht in activeStudentKeys (entweder gar nicht in Basis oder nur mit inaktivem Status)
                        bool shouldBeMarkedAsDeleted = !activeStudentKeys.Contains(key);
                        
                        if (shouldBeMarkedAsDeleted)
                        {
                            // Finde Klasse aus Basisdaten falls vorhanden, sonst aus CSV
                            Dictionary<string, string>? sb = null;
                            try { sb = basisRecords?.LastOrDefault(r => PersonMatches(r, lname, fname, bdate)); } catch { }

                            var klasse = sb != null ? GetDictValue(sb, "Klasse") : (GetS("klasse") ?? GetS("klasse.name"));

                            var deleted = new WebuntisDeletedStudent
                            {
                                Nachname = lname,
                                Vorname = fname,
                                Klasse = klasse,
                                Geburtsdatum = bdate
                            };

                            deletedStudentsPreview.Add(deleted);
                            if (deletedStudentsPreview.Count >= 10) break;
                        }
                    }

                    try
                    {
                        StoreFile("webuntis_deleted_students", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(deletedStudentsPreview)));
                        
                        // Zeige gelöschte Studenten in der Ausgabe an (ENTFERNT - wird jetzt nach Statistik angezeigt)
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error storing deleted students: {ex}");
                    }
                }
                else
                {
                    try { StoreFile("webuntis_deleted_students", Encoding.UTF8.GetBytes("[]")); } catch { }
                }

                // Berechne Statistiken basierend auf activeStudentKeys statt datKeys
                int unchanged = activeStudentKeys.Intersect(csvKeys).Count();
                int added = activeStudentKeys.Except(csvKeys).Count();
                int removed = csvKeys.Except(activeStudentKeys).Count(); // Nutze activeStudentKeys statt datKeys

                var statsObj = new WebuntisStats
                {
                    Unchanged = unchanged,
                    Added = added,
                    Removed = removed,
                    CsvCount = csvKeys.Count,
                    NewCount = activeStudentKeys.Count // Nur aktive zählen
                };

                try { StoreFile("webuntis_stats", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(statsObj))); } catch { }
                result.MessageHtml += $"<pre>Vergleich: unverändert={unchanged}, neu={added}, gelöscht={removed} (CSV={csvKeys.Count}, Neu aktiv={activeStudentKeys.Count})</pre>";
                
                // Zeige gelöschte Studenten nach der Statistik an
                if (deletedStudentsPreview.Count > 0)
                {
                    result.MessageHtml += $"<pre>❌ {deletedStudentsPreview.Count} Student(en) werden als gelöscht markiert:</pre>";
                    foreach (var ds in deletedStudentsPreview)
                    {
                        try
                        {
                            result.MessageHtml += $"<pre>  - {ds.Nachname}, {ds.Vorname} ({ds.Geburtsdatum}) - Klasse: {ds.Klasse}</pre>";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error formatting deleted student: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.MessageHtml += $"<pre>Vergleich CSV/Neu fehlgeschlagen: {System.Net.WebUtility.HtmlEncode(ex.Message)}</pre>";
            }

            if (exportStudents == null || !exportStudents.Any())
            {
                if (students != null && students.Any())
                {
                    exportStudents = students;
                }
                else if (basisRecords != null && basisRecords.Any())
                {
                    exportStudents = basisRecords.Select(b =>
                    {
                        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        d["longName"] = GetDictValue(b, "Nachname", "Familienname", "name");
                        d["foreName"] = GetDictValue(b, "Vorname", "givenName", "forename");
                        d["birthDate"] = GetDictValue(b, "Geburtsdatum", "Geburtsdatum");
                        d["status"] = GetDictValue(b, "Status", "status");
                        d["klasse"] = GetDictValue(b, "Klasse", "klasse", "Klasse.name");
                        foreach (var kv in b) if (!d.ContainsKey(kv.Key)) d[kv.Key] = kv.Value;
                        return d;
                    }).ToList();
                }
                else
                {
                    exportStudents = new List<Dictionary<string, string>>();
                }
            }

            // Build Webuntis students CSV (Zielformat per Vorgabe)
            var webHeaders = new[] {
                "E-Mail","Familienname","Vorname","Klasse","Kurzname","Geschlecht","Geburtsdatum","Eintrittsdatum","Austrittsdatum",
                "Telefon","Mobil","Strasse","PLZ","Ort","ErzName","ErzMobil","ErzTelefon","Volljaehrig",
                "BetriebName","BetriebStrasse","BetriebPlz","BetriebOrt","BetriebTelefon","BetriebTelefon2","BetriebMail","BetriebBetreuer","SchildAdressId",
                "O365Identitaet","Benutzername"
            };

            byte[] targetBytes = Array.Empty<byte>();
            int targetRowsWritten = 0;
            try
            {
                var aktSj0 = (DateTime.Now.Month > 7 ? DateTime.Now.Year : DateTime.Now.Year - 1).ToString();
                var todayFormatted = DateTime.Now.ToString("dd.MM.yyyy");
                
                result.MessageHtml += $"<pre>DEBUG: DatStudents={DatStudents?.Count ?? 0}, basisRecords={basisRecords?.Count ?? 0}, exportStudents={exportStudents?.Count ?? 0}</pre>";
                Console.WriteLine($"ProcessWebuntis: Starting CSV generation. DatStudents count={DatStudents?.Count ?? 0}, targetRowsWritten={targetRowsWritten}");

                // Liste für Studenten, die Austrittsdatum erhalten
                var studentsWithExitDate = new List<WebuntisDeletedStudent>();

                // Hilfsfunktion: Prüft ob Student aus CSV aktiven Status in Basis hat
                bool HasActiveStatusInBasis(string? csvEmail)
                {
                    if (string.IsNullOrWhiteSpace(csvEmail)) return false;
                    
                    // Suche in DatStudents nach übereinstimmender E-Mail und prüfe Status
                    var matchingStudent = DatStudents?.FirstOrDefault(ds => 
                        !string.IsNullOrWhiteSpace(ds.MailSchulisch) && 
                        ds.MailSchulisch.Equals(csvEmail, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingStudent == null) return false;
                    
                    // Prüfe ob mindestens ein Bildungsgang Status 2 oder 6 hat
                    return matchingStudent.Bildungsgaenge?.Any(bg => bg.Status == "2" || bg.Status == "6") ?? false;
                }

                using (var msTarget = new System.IO.MemoryStream())
                {
                    using (var swt = new System.IO.StreamWriter(msTarget, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
                    {
                        swt.WriteLine(string.Join(',', webHeaders));
                        Console.WriteLine($"ProcessWebuntis: CSV header written");

                        // Generate CSV from DatStudents (with latest Bildungsgang per student)
                        if (DatStudents != null && DatStudents.Count > 0)
                        {
                            result.MessageHtml += $"<pre>Processing {DatStudents.Count} DatStudents...</pre>";
                            Console.WriteLine($"ProcessWebuntis: Processing {DatStudents.Count} DatStudents for CSV...");
                            try
                            {
                                foreach (var student in DatStudents)
                                {
                                    var latestBg = student.Bildungsgaenge
                                        .OrderByDescending(b => ParseDateOrMin(b.BeginnBildungsgang))
                                        .FirstOrDefault();
                                    var klasse = latestBg?.Klasse ?? student.Klasse ?? string.Empty;
                                    var kurzname = student.AdditionalFields.TryGetValue("Kurzname", out var kn) ? kn : string.Empty;
                                    var geschlecht = student.AdditionalFields.TryGetValue("Geschlecht", out var gsch) ? gsch : string.Empty;
                                    var eintritt = latestBg?.BeginnBildungsgang ?? string.Empty;
                                    var austritt = student.AdditionalFields.TryGetValue("Abmeldedatum", out var abm) ? abm : string.Empty;

                                    // Prüfe ob Student in CSV existiert und dort kein/zukünftiges Austrittsdatum hat
                                    var csvStudent = uniqueStudents?.FirstOrDefault(s => 
                                    {
                                        var csvEmail = s.TryGetValue("address.email", out var em) ? em : string.Empty;
                                        return !string.IsNullOrWhiteSpace(csvEmail) && 
                                               !string.IsNullOrWhiteSpace(student.MailSchulisch) &&
                                               csvEmail.Equals(student.MailSchulisch, StringComparison.OrdinalIgnoreCase);
                                    });

                                    if (csvStudent != null)
                                    {
                                        var csvExitDate = csvStudent.TryGetValue("exitDate", out var ed) ? ed : string.Empty;
                                        bool hasFutureOrNoExit = string.IsNullOrWhiteSpace(csvExitDate);
                                        
                                        if (!hasFutureOrNoExit && DateTime.TryParseExact(csvExitDate, 
                                            new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" }, 
                                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var exitDt))
                                        {
                                            hasFutureOrNoExit = exitDt.Date > DateTime.Now.Date;
                                        }

                                        // Wenn in CSV ohne/zukünftiges Austrittsdatum, aber nicht aktiv (Status ≠ 2 und ≠ 6)
                                        if (hasFutureOrNoExit)
                                        {
                                            var hasActiveStatus = student.Bildungsgaenge?.Any(bg => bg.Status == "2" || bg.Status == "6") ?? false;
                                            if (!hasActiveStatus)
                                            {
                                                austritt = todayFormatted;
                                                
                                                // Füge zur Vorschau-Liste hinzu (max 10)
                                                if (studentsWithExitDate.Count < 10)
                                                {
                                                    studentsWithExitDate.Add(new WebuntisDeletedStudent
                                                    {
                                                        Nachname = student.Nachname ?? string.Empty,
                                                        Vorname = student.Vorname ?? string.Empty,
                                                        Klasse = klasse,
                                                        Geburtsdatum = student.GeburtsdatumParsed?.ToString("dd.MM.yyyy") ?? student.Geburtsdatum ?? string.Empty
                                                    });
                                                }
                                            }
                                        }
                                    }

                                    var studentAddresses = student?.Adresses ?? new List<Models.Adresse>();
                                    var home = studentAddresses.FirstOrDefault(a =>
                                        a.AdditionalFields.TryGetValue("Adressart", out var art) && art.Equals("Privat", StringComparison.OrdinalIgnoreCase))
                                               ?? studentAddresses.FirstOrDefault();
                                    var betrieb = studentAddresses.FirstOrDefault(a =>
                                        a.AdditionalFields.TryGetValue("Adressart", out var art) && art.Equals("Betrieb", StringComparison.OrdinalIgnoreCase));

                                    var erz = student.Erziehers.FirstOrDefault();

                                    var row = new List<string>
                                    {
                                        CsvSafe(student.MailSchulisch),
                                        CsvSafe(student.Nachname),
                                        CsvSafe(student.Vorname),
                                        CsvSafe(klasse),
                                        CsvSafe(kurzname),
                                        CsvSafe(geschlecht),
                                        CsvSafe(student.GeburtsdatumParsed?.ToString("dd.MM.yyyy") ?? student.Geburtsdatum),
                                        CsvSafe(eintritt),
                                        CsvSafe(austritt),
                                        CsvSafe(home?.Telefon),
                                        CsvSafe(home?.Telefon2),
                                        CsvSafe(home?.Strasse ?? student.Strasse),
                                        CsvSafe(home?.PLZ ?? student.PLZ),
                                        CsvSafe(home?.Ort ?? student.Ort),
                                        CsvSafe($"{erz?.Nachname1} {erz?.Vorname1}".Trim()),
                                        CsvSafe(erz?.Telefon),
                                        CsvSafe(erz?.Telefon),
                                        CsvSafe(BoolJN(student.Volljaehrig)),
                                        CsvSafe(betrieb?.Name1),
                                        CsvSafe(betrieb?.Strasse),
                                        CsvSafe(betrieb?.PLZ),
                                        CsvSafe(betrieb?.Ort),
                                        CsvSafe(betrieb?.Telefon),
                                        CsvSafe(betrieb?.Telefon2),
                                        CsvSafe(betrieb?.Mail),
                                        CsvSafe(string.Join(" ", new[]{betrieb?.BetreuerAnrede, betrieb?.BetreuerVorname, betrieb?.BetreuerNachname}.Where(x=>!string.IsNullOrWhiteSpace(x)))),
                                        CsvSafe(betrieb?.SchildAdressId),
                                        CsvSafe(student.MailSchulisch),
                                        CsvSafe(student.MailSchulisch)
                                    };

                                    swt.WriteLine(string.Join(',', row));
                                    targetRowsWritten++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"ProcessWebuntis: Exception in DatStudents loop: {ex}");
                                result.MessageHtml += $"<pre>Error in DatStudents loop: {System.Net.WebUtility.HtmlEncode(ex.Message)}</pre>";
                            }
                        }
                        else
                        {
                            // Fallback: use original basis-record based logic if DatStudents is empty
                            var targetBases = new List<Dictionary<string,string>>();
                            try
                            {
                                var basisSelected = basisRecords;
                                // ...existing grouping and selection logic...
                            }
                            catch
                            {
                                targetBases = (basisRecords ?? new()).Where(b => (GetDictValue(b, "Status") == "2" || GetDictValue(b, "Status") == "6")).ToList();
                            }

                            foreach (var b in targetBases)
                            {
                                var familienname = GetDictValue(b, "Nachname", "Familienname", "name");
                                var vorname = GetDictValue(b, "Vorname", "givenName", "forename");
                                var geburtsdatum = GetDictValue(b, "Geburtsdatum", "birthDate");
                                var status = GetDictValue(b, "Status", "status");
                                var klasse = GetDictValue(b, "Klasse", "klasse", "Klasse.name");
                                var kurzname = GetDictValue(b, "Kurzname");
                                var geschlecht = GetDictValue(b, "Geschlecht");
                                var eintritt = GetDictValue(b, "BeginnBildungsgang", "Beginn Bildungsgang", "Aufnahmedatum");
                                var austritt = GetDictValue(b, "Abmeldedatum");

                                var admatch = (adressenRecords ?? new()).LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum));
                                var erzmatch = (erzieherRecords ?? new()).LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum));
                                var betrieb = (adressenRecords ?? new()).LastOrDefault(r =>
                                    PersonMatches(r, familienname, vorname, geburtsdatum) &&
                                    string.Equals(GetDictValue(r, "Adressart"), "Betrieb", StringComparison.OrdinalIgnoreCase));

                                var row = new List<string>
                                {
                                    CsvSafe(GetDictValue(b, "schulische E-Mail", "E-Mail", "Email")),
                                    CsvSafe(familienname),
                                    CsvSafe(vorname),
                                    CsvSafe(klasse),
                                    CsvSafe(kurzname),
                                    CsvSafe(geschlecht),
                                    CsvSafe(geburtsdatum),
                                    CsvSafe(eintritt),
                                    CsvSafe(austritt),
                                    CsvSafe(GetDictValue(admatch, "1. Tel.-Nr.", "Telefon")),
                                    CsvSafe(GetDictValue(admatch, "2. Tel.-Nr.")),
                                    CsvSafe(GetDictValue(admatch, "Straße", "Strasse")),
                                    CsvSafe(GetDictValue(admatch, "PLZ")),
                                    CsvSafe(GetDictValue(admatch, "Ort")),
                                    CsvSafe($"{GetDictValue(erzmatch, "Nachname 1.Person")} {GetDictValue(erzmatch, "Vorname 1.Person")}".Trim()),
                                    CsvSafe(GetDictValue(erzmatch, "Telefon")),
                                    CsvSafe(GetDictValue(erzmatch, "Telefon")),
                                    CsvSafe(BoolJN(string.Equals(GetDictValue(b, "Volljaehrig"), "J", StringComparison.OrdinalIgnoreCase))),
                                    CsvSafe(GetDictValue(betrieb, "Name1")),
                                    CsvSafe(GetDictValue(betrieb, "Straße", "Strasse")),
                                    CsvSafe(GetDictValue(betrieb, "PLZ")),
                                    CsvSafe(GetDictValue(betrieb, "Ort")),
                                    CsvSafe(GetDictValue(betrieb, "1. Tel.-Nr.", "Telefon")),
                                    CsvSafe(GetDictValue(betrieb, "2. Tel.-Nr.")),
                                    CsvSafe(GetDictValue(betrieb, "E-Mail", "Email")),
                                    CsvSafe(string.Join(" ", new[]{GetDictValue(betrieb, "Betreuer Anrede"), GetDictValue(betrieb, "Betreuer Vorname"), GetDictValue(betrieb, "Betreuer Nachname")}.Where(x=>!string.IsNullOrWhiteSpace(x)))),
                                    CsvSafe(GetDictValue(betrieb, "SchILD-Adress-ID")),
                                    CsvSafe(GetDictValue(b, "schulische E-Mail", "E-Mail", "Email")),
                                    CsvSafe(GetDictValue(b, "schulische E-Mail", "E-Mail", "Email"))
                                };

                                swt.WriteLine(string.Join(',', row));
                                targetRowsWritten++;
                            }
                        }
                    }

                    msTarget.Position = 0;
                    targetBytes = msTarget.ToArray();
                    Console.WriteLine($"ProcessWebuntis: Generated {targetRowsWritten} rows, {targetBytes.Length} bytes");
                }
                
                // Speichere die Liste der Studenten mit gesetztem Austrittsdatum
                try
                {
                    StoreFile("webuntis_exit_date_set", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(studentsWithExitDate)));
                    if (studentsWithExitDate.Count > 0)
                    {
                        result.MessageHtml += $"<pre>ℹ {studentsWithExitDate.Count} Student(en) erhalten automatisch Austrittsdatum={todayFormatted}</pre>";
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcessWebuntis CSV generation error: {ex}");
            }

            // If the generation produced no rows (e.g. students present but filtered out),
            // fall back to using basisRecords as source so an import file is produced.
            if (targetRowsWritten == 0 && (basisRecords != null && basisRecords.Any()))
            {
                result.MessageHtml += $"<pre>Fallback: Processing {basisRecords.Count} basisRecords because main process wrote 0 rows...</pre>";
                Console.WriteLine($"ProcessWebuntis Fallback: DatStudents empty or wrote 0 rows. Using {basisRecords?.Count ?? 0} basisRecords");
                try
                {
                    using var msTarget2 = new System.IO.MemoryStream();
                    int fallbackRows = 0;
                    using (var swt2 = new System.IO.StreamWriter(msTarget2, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
                    {
                        swt2.WriteLine(string.Join(',', webHeaders));
                        Console.WriteLine($"ProcessWebuntis Fallback: CSV header written");
                        
                        foreach (var b in basisRecords)
                        {
                            var familienname = GetDictValue(b, "Nachname", "Familienname", "name");
                            var vorname = GetDictValue(b, "Vorname", "givenName", "forename");
                            var geburtsdatum = GetDictValue(b, "Geburtsdatum", "birthDate");
                            var klasse = GetDictValue(b, "Klasse", "klasse", "Klasse.name");
                            var kurzname = GetDictValue(b, "Kurzname");
                            var geschlecht = GetDictValue(b, "Geschlecht");
                            var eintritt = GetDictValue(b, "BeginnBildungsgang", "Beginn Bildungsgang", "Aufnahmedatum");
                            var austritt = GetDictValue(b, "Abmeldedatum");

                            var admatch = (adressenRecords ?? new()).LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum));
                            var erzmatch = (erzieherRecords ?? new()).LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum));
                            var betrieb = (adressenRecords ?? new()).LastOrDefault(r =>
                                PersonMatches(r, familienname, vorname, geburtsdatum) &&
                                string.Equals(GetDictValue(r, "Adressart"), "Betrieb", StringComparison.OrdinalIgnoreCase));

                            var row = new List<string>
                            {
                                CsvSafe(GetDictValue(b, "schulische E-Mail", "E-Mail", "Email")),
                                CsvSafe(familienname),
                                CsvSafe(vorname),
                                CsvSafe(klasse),
                                CsvSafe(kurzname),
                                CsvSafe(geschlecht),
                                CsvSafe(geburtsdatum),
                                CsvSafe(eintritt),
                                CsvSafe(austritt),
                                CsvSafe(GetDictValue(admatch, "1. Tel.-Nr.", "Telefon")),
                                CsvSafe(GetDictValue(admatch, "2. Tel.-Nr.")),
                                CsvSafe(GetDictValue(admatch, "Straße", "Strasse")),
                                CsvSafe(GetDictValue(admatch, "PLZ")),
                                CsvSafe(GetDictValue(admatch, "Ort")),
                                CsvSafe($"{GetDictValue(erzmatch, "Nachname 1.Person")} {GetDictValue(erzmatch, "Vorname 1.Person")}".Trim()),
                                CsvSafe(GetDictValue(erzmatch, "Telefon")),
                                CsvSafe(GetDictValue(erzmatch, "Telefon")),
                                CsvSafe(BoolJN(string.Equals(GetDictValue(b, "Volljaehrig"), "J", StringComparison.OrdinalIgnoreCase))),
                                CsvSafe(GetDictValue(betrieb, "Name1")),
                                CsvSafe(GetDictValue(betrieb, "Straße", "Strasse")),
                                CsvSafe(GetDictValue(betrieb, "PLZ")),
                                CsvSafe(GetDictValue(betrieb, "Ort")),
                                CsvSafe(GetDictValue(betrieb, "1. Tel.-Nr.", "Telefon")),
                                CsvSafe(GetDictValue(betrieb, "2. Tel.-Nr.")),
                                CsvSafe(GetDictValue(betrieb, "E-Mail", "Email")),
                                CsvSafe(string.Join(" ", new[]{GetDictValue(betrieb, "Betreuer Anrede"), GetDictValue(betrieb, "Betreuer Vorname"), GetDictValue(betrieb, "Betreuer Nachname")}.Where(x=>!string.IsNullOrWhiteSpace(x)))),
                                CsvSafe(GetDictValue(betrieb, "SchILD-Adress-ID")),
                                CsvSafe(GetDictValue(b, "schulische E-Mail", "E-Mail", "Email")),
                                CsvSafe(GetDictValue(b, "schulische E-Mail", "E-Mail", "Email"))
                            };

                            swt2.WriteLine(string.Join(',', row));
                            fallbackRows++;
                        }
                        
                        swt2.Flush();
                        msTarget2.Position = 0;
                    }
                    targetBytes = msTarget2.ToArray();
                    targetRowsWritten = fallbackRows;
                    result.MessageHtml += $"<pre>Fallback: used basisRecords to produce {fallbackRows} rows.</pre>";
                    Console.WriteLine($"ProcessWebuntis Fallback: Generated {fallbackRows} rows, {targetBytes.Length} bytes");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ProcessWebuntis Fallback error: {ex}");
                }
            }

            // Only add the output file if it has actual data rows (not just the header)
            if (targetRowsWritten > 0 && targetBytes != null && targetBytes.Length > 0)
            {
                int totalLines = targetRowsWritten + 1;
                result.OutputFiles.Add(new OutputFile 
                { 
                    FileName = "ImportNachWebuntis-Stammdaten-Schueler.csv", 
                    Content = targetBytes, 
                    Hint = "Import nach Webuntis (CSV)",
                    LineCount = totalLines,
                    FileSize = targetBytes.Length
                });
                StoreFile("import_webuntis_students", targetBytes);
                result.Success = true;
                Console.WriteLine($"ProcessWebuntis: OutputFile added successfully. targetRowsWritten={targetRowsWritten}, targetBytes.Length={targetBytes.Length}");
                try
                {
                    result.MessageHtml += $"<pre>✓ ImportNachWebuntis-Stammdaten-Schueler.csv erstellt mit {targetRowsWritten} Zeilen ({targetBytes.Length} Bytes)</pre>";
                    result.MessageHtml += $"<pre>Export rows: {targetRowsWritten}</pre>";
                    try { result.MessageHtml += $"<pre>uniqueStudents: {uniqueStudents?.Count ?? 0}, students: {students?.Count ?? 0}, basisRecords: {basisRecords?.Count ?? 0}</pre>"; } catch { }
                }
                catch { }
            }
            else
            {
                result.Success = false;
                result.Message = "Keine Daten zum Exportieren gefunden. Bitte überprüfen Sie die hochgeladenen Dateien (insbesondere SchuelerBasisdaten.dat).";
                result.MessageHtml += $"<pre>⚠ Keine Ausgabedaten erstellt. targetRowsWritten: {targetRowsWritten}, targetBytes.Length: {targetBytes?.Length ?? 0}</pre>";
                Console.WriteLine($"ProcessWebuntis: No output file added. targetRowsWritten={targetRowsWritten}, targetBytes.Length={targetBytes?.Length ?? 0}");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Fehler bei Webuntis-Export: " + ex.Message;
        }

        await Task.Delay(100);
        return result;
    }

    private List<Models.Student> BuildTypedStudents(List<Dictionary<string, string>> basisRecords, List<Dictionary<string, string>> zusatzRecords, List<Dictionary<string, string>> adressenRecords, List<Dictionary<string, string>> erzieherRecords)
    {
        Console.WriteLine($"Enter BuildTypedStudents: basisRecords={(basisRecords==null?"null":basisRecords.Count.ToString())}, zusatzRecords={(zusatzRecords==null?"null":zusatzRecords.Count.ToString())}, adressenRecords={(adressenRecords==null?"null":adressenRecords.Count.ToString())}, erzieherRecords={(erzieherRecords==null?"null":erzieherRecords.Count.ToString())}");
        var list = new List<Models.Student>();
        if (basisRecords == null || basisRecords.Count == 0)
        {
            Console.WriteLine("BuildTypedStudents: no basis records -> returning empty list");
            return list;
        }

        // Group basis records by unique student identity (Nachname|Vorname|Geburtsdatum)
        var groups = basisRecords.GroupBy(b => (GetDictValue(b, "Nachname") + "|" + GetDictValue(b, "Vorname") + "|" + GetDictValue(b, "Geburtsdatum")).Trim().ToLowerInvariant());

        foreach (var g in groups)
        {
            var records = g.ToList();
            var primary = records.First();
            var s = new Models.Student();
            s.Nachname = GetDictValue(primary, "Nachname", "Familienname", "name");
            s.Vorname = GetDictValue(primary, "Vorname", "givenName", "forename");
            s.Geburtsdatum = GetDictValue(primary, "Geburtsdatum", "birthDate");
            if (DateTime.TryParseExact(s.Geburtsdatum, new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var gd))
            {
                s.GeburtsdatumParsed = gd;
                var age = DateTime.Now.Year - gd.Year; if (DateTime.Now < gd.AddYears(age)) age--;
                s.Volljaehrig = age >= 18;
            }
            else
            {
                s.GeburtsdatumParsed = null;
                s.Volljaehrig = false;
            }

            // aggregate additional fields from all basis records (first wins)
            foreach (var b in records)
            {
                foreach (var kv in b)
                {
                    if (!s.AdditionalFields.ContainsKey(kv.Key)) s.AdditionalFields[kv.Key] = kv.Value;
                }
            }

            // Try to enrich student-level info from matching zusatz/adressen (first matching record)
            try
            {
                Dictionary<string, string>? zmatch = null;
                if (zusatzRecords != null && zusatzRecords.Any())
                {
                    zmatch = zusatzRecords.LastOrDefault(r => PersonMatches(r, s.Nachname, s.Vorname, s.Geburtsdatum));
                    if (zmatch == null)
                    {
                        zmatch = zusatzRecords.LastOrDefault(r => string.Equals(GetDictValue(r, "Nachname", "Familienname", "name").Trim(), s.Nachname.Trim(), StringComparison.OrdinalIgnoreCase)
                            && string.Equals(GetDictValue(r, "Vorname", "givenName", "forename").Trim(), s.Vorname.Trim(), StringComparison.OrdinalIgnoreCase));
                    }
                }

                Dictionary<string, string>? admatch = null;
                if (adressenRecords != null && adressenRecords.Any())
                {
                    admatch = adressenRecords.LastOrDefault(r => PersonMatches(r, s.Nachname, s.Vorname, s.Geburtsdatum));
                    if (admatch == null)
                    {
                        admatch = adressenRecords.LastOrDefault(r => string.Equals(GetDictValue(r, "Nachname", "Familienname", "name").Trim(), s.Nachname.Trim(), StringComparison.OrdinalIgnoreCase)
                            && string.Equals(GetDictValue(r, "Vorname", "givenName", "forename").Trim(), s.Vorname.Trim(), StringComparison.OrdinalIgnoreCase));
                    }
                }

                if (zmatch != null)
                {
                    var mail = GetDictValue(zmatch, "schulische E-Mail", "MailSchulisch", "E-Mail", "Email");
                    if (!string.IsNullOrWhiteSpace(mail)) s.MailSchulisch = mail;
                    s.Telefon = GetDictValue(zmatch, "Telefon-Nr.", "Telefon");
                }
                if (admatch != null)
                {
                    s.Strasse = GetDictValue(admatch, "Straße", "Strasse", "street");
                    s.PLZ = GetDictValue(admatch, "PLZ", "Postleitzahl");
                    s.Ort = GetDictValue(admatch, "Ort");
                }
                // fallback to primary basis values if still empty
                if (string.IsNullOrWhiteSpace(s.MailSchulisch)) s.MailSchulisch = GetDictValue(primary, "MailSchulisch", "schulische E-Mail", "E-Mail", "Email");
                if (string.IsNullOrWhiteSpace(s.Strasse)) s.Strasse = GetDictValue(primary, "Straße", "Strasse", "street");
                if (string.IsNullOrWhiteSpace(s.PLZ)) s.PLZ = GetDictValue(primary, "PLZ", "Postleitzahl");
                if (string.IsNullOrWhiteSpace(s.Ort)) s.Ort = GetDictValue(primary, "Ort");
            }
            catch { }

            // For each basis record in the group, add a Bildungsgang entry
            foreach (var b in records)
            {
                var bg = new Models.Bildungsgang();
                bg.Status = GetDictValue(b, "Status", "status");
                bg.Klasse = GetDictValue(b, "Klasse", "klasse", "Klasse.name");
                bg.Jahrgang = GetDictValue(b, "Jahrgang");
                bg.Schulgliederung = GetDictValue(b, "Schulgliederung");
                bg.OrgForm = GetDictValue(b, "OrgForm");
                bg.Klassenart = GetDictValue(b, "Klassenart");
                bg.Fachklasse = GetDictValue(b, "Fachklasse");

                // 1:1 Zuordnung per Zeilenindex zwischen Basis und Zusatzdaten
                try
                {
                    var rowIdx = GetDictValue(b, "__rowIndex");
                    Dictionary<string, string>? matchZ = null;
                    if (!string.IsNullOrWhiteSpace(rowIdx) && zusatzRecords != null && zusatzRecords.Count > 0)
                    {
                        matchZ = zusatzRecords.FirstOrDefault(r => string.Equals(GetDictValue(r, "__rowIndex"), rowIdx, StringComparison.Ordinal));
                    }
                    if (matchZ != null)
                    {
                        bg.BeginnBildungsgang = GetDictValue(matchZ, "BeginnBildungsgang", "Beginn Bildungsgang");
                      }
                }
                catch { }

                s.Bildungsgaenge.Add(bg);
            }

            // collect addresses matching the student (unique by SchildAdressId or combined fields)
            try
            {
                var matchesAd = adressenRecords.Where(r => PersonMatches(r, s.Nachname, s.Vorname, s.Geburtsdatum)).ToList();
                foreach (var ad in matchesAd)
                {
                    var a = new Models.Adresse();
                    a.Name1 = GetDictValue(ad, "Name1", "Name");
                    a.Strasse = GetDictValue(ad, "Straße", "Strasse", "street");
                    a.PLZ = GetDictValue(ad, "PLZ", "Postleitzahl");
                    a.Ort = GetDictValue(ad, "Ort");
                    a.Telefon = GetDictValue(ad, "1. Tel.-Nr.", "Telefon");
                    a.Telefon2 = GetDictValue(ad, "2. Tel.-Nr.");
                    a.Mail = GetDictValue(ad, "E-Mail", "Email");
                    a.BetreuerAnrede = GetDictValue(ad, "Betreuer Anrede");
                    a.BetreuerVorname = GetDictValue(ad, "Betreuer Vorname");
                    a.BetreuerNachname = GetDictValue(ad, "Betreuer Nachname");
                    a.SchildAdressId = GetDictValue(ad, "SchILD-Adress-ID", "SchILD Adress ID");
                    foreach (var kv in ad) if (!a.AdditionalFields.ContainsKey(kv.Key)) a.AdditionalFields[kv.Key] = kv.Value;
                    // avoid duplicates
                    if (!s.Adresses.Any(x => !string.IsNullOrWhiteSpace(a.SchildAdressId) && x.SchildAdressId == a.SchildAdressId))
                        s.Adresses.Add(a);
                }
            }
            catch { }

            // collect erzieher records
            try
            {
                var matchesErz = erzieherRecords.Where(r => PersonMatches(r, s.Nachname, s.Vorname, s.Geburtsdatum)).ToList();
                foreach (var erz in matchesErz)
                {
                    var e = new Models.Erzieher();
                    e.Vorname1 = GetDictValue(erz, "Vorname 1.Person", "Vorname 1", "Vorname");
                    e.Nachname1 = GetDictValue(erz, "Nachname 1.Person", "Nachname 1", "Nachname");
                    e.Telefon = GetDictValue(erz, "Telefon", "Telefon-Nr.");
                    e.Email = GetDictValue(erz, "E-Mail", "Email");
                    e.Strasse = GetDictValue(erz, "Straße", "Strasse", "street");
                    foreach (var kv in erz) if (!e.AdditionalFields.ContainsKey(kv.Key)) e.AdditionalFields[kv.Key] = kv.Value;
                    // avoid exact duplicates by email+telefon
                    if (!s.Erziehers.Any(x => !string.IsNullOrWhiteSpace(e.Email) && x.Email == e.Email))
                        s.Erziehers.Add(e);
                }
            }
            catch { }

            list.Add(s);
        }

        Console.WriteLine($"Exit BuildTypedStudents: built {list.Count} students");
        return list;
    }

    // Build typed Students directly from the parsed students CSV dictionaries
    private List<Models.Student> BuildTypedStudentsFromStudentCsv(List<Dictionary<string,string>> studentsCsv)
    {
        var list = new List<Models.Student>();
        if (studentsCsv == null || studentsCsv.Count == 0) return list;

        foreach (var s in studentsCsv)
        {
            try
            {
                var st = new Models.Student();
                st.Nachname = GetDictValue(s, "longName", "Nachname", "Familienname", "name");
                st.Vorname = GetDictValue(s, "foreName", "Vorname", "givenName", "forename");
                st.Geburtsdatum = GetDictValue(s, "birthDate", "Geburtsdatum");
                if (DateTime.TryParseExact(st.Geburtsdatum, new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var gd))
                {
                    st.GeburtsdatumParsed = gd;
                    var age = DateTime.Now.Year - gd.Year; if (DateTime.Now < gd.AddYears(age)) age--;
                    st.Volljaehrig = age >= 18;
                }
                st.MailSchulisch = GetDictValue(s, "MailSchulisch", "schulische E-Mail", "E-Mail");
                st.Klasse = GetDictValue(s, "klasse", "Klasse", "Klasse.name");
                st.Status = GetDictValue(s, "status", "Status");
                foreach (var kv in s) if (!st.AdditionalFields.ContainsKey(kv.Key)) st.AdditionalFields[kv.Key] = kv.Value;
                list.Add(st);
            }
            catch { }
        }

        return list;
    }

    // Public helper to build students from a stored students CSV file
    public List<Models.Student>? BuildStudentsFromStoredStudentsCsv()
    {
        var studentsBytes = GetFile("students");
        if (studentsBytes == null) return null;

        try
        {
            var (enc, _) = ResolveEncodingForReader(studentsBytes, null);
            var text = enc.GetString(studentsBytes);
            if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF') text = text.Substring(1);
            var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            var hdrIdx = lines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
            
            if (hdrIdx < 0) return new List<Models.Student>();
            
            var records = new List<Dictionary<string, string>>();
            var hdr = SplitRow(lines[hdrIdx], DetermineDelimiter(studentsBytes, "\t"), null);
            
            for (int i = hdrIdx + 1; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = SplitRow(line, DetermineDelimiter(studentsBytes, "\t"), null);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < hdr.Length; c++)
                {
                    dict[hdr[c].Trim()] = c < fields.Length ? fields[c] : string.Empty;
                }
                records.Add(dict);
            }
            
            return BuildTypedStudentsFromStudentCsv(records);
        }
        catch
        {
            return null;
        }
    }

    // Public helper to build students from files previously stored via StoreFile
    public List<Models.Student> BuildStudentsFromStoredFiles()
    {
        var basisBytes = GetFile("basisdaten");
        var zusatzBytes = GetFile("zusatzdaten");
        var adressenBytes = GetFile("adressen");
        var erzieherBytes = GetFile("erzieher");

        Console.WriteLine($"Enter BuildStudentsFromStoredFiles: basisBytes={(basisBytes==null?"null":basisBytes.Length.ToString())}, zusatzBytes={(zusatzBytes==null?"null":zusatzBytes.Length.ToString())}, adressenBytes={(adressenBytes==null?"null":adressenBytes.Length.ToString())}, erzieherBytes={(erzieherBytes==null?"null":erzieherBytes.Length.ToString())}");

        var basisRecords = new List<Dictionary<string, string>>();
        var zusatzRecords = new List<Dictionary<string, string>>();
        var adressenRecords = new List<Dictionary<string, string>>();
        var erzieherRecords = new List<Dictionary<string, string>>();

        try
        {
            if (zusatzBytes != null)
            {
                var (enc, _) = ResolveEncodingForReader(zusatzBytes, null);
                var text = enc.GetString(zusatzBytes);
                if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF') text = text.Substring(1);
                var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                var hdrIdx = lines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                if (hdrIdx >= 0)
                {
                    var hdr = SplitRow(lines[hdrIdx], DetermineDelimiter(zusatzBytes, "\t"), null);
                    for (int i = hdrIdx + 1; i < lines.Count; i++)
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = SplitRow(line, DetermineDelimiter(zusatzBytes, "\t"), null);
                        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for (int c = 0; c < hdr.Length; c++) dict[hdr[c].Trim()] = c < fields.Length ? fields[c] : string.Empty;
                        // store data row index (first data row = 0)
                        dict["__rowIndex"] = (i - hdrIdx - 1).ToString(CultureInfo.InvariantCulture);
                        zusatzRecords.Add(dict);
                    }
                }
            }
        }
        catch { }

        try
        {
            if (adressenBytes != null)
            {
                var (enc, _) = ResolveEncodingForReader(adressenBytes, null);
                var text = enc.GetString(adressenBytes);
                if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF') text = text.Substring(1);
                var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                var hdrIdx = lines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                if (hdrIdx >= 0)
                {
                    var hdr = SplitRow(lines[hdrIdx], DetermineDelimiter(adressenBytes, "\t"), null);
                    for (int i = hdrIdx + 1; i < lines.Count; i++)
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = SplitRow(line, DetermineDelimiter(adressenBytes, "\t"), null);
                        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for (int c = 0; c < hdr.Length; c++) dict[hdr[c].Trim()] = c < fields.Length ? fields[c] : string.Empty;
                        adressenRecords.Add(dict);
                    }
                }
            }
        }
        catch { }

        try
        {
            if (erzieherBytes != null)
            {
                var (enc, _) = ResolveEncodingForReader(erzieherBytes, null);
                var text = enc.GetString(erzieherBytes);
                if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF') text = text.Substring(1);
                var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                var hdrIdx = lines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                if (hdrIdx >= 0)
                {
                    var hdr = SplitRow(lines[hdrIdx], DetermineDelimiter(erzieherBytes, "\t"), null);
                    for (int i = hdrIdx + 1; i < lines.Count; i++)
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = SplitRow(line, DetermineDelimiter(erzieherBytes, "\t"), null);
                        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for (int c = 0; c < hdr.Length; c++) dict[hdr[c].Trim()] = c < fields.Length ? fields[c] : string.Empty;
                        erzieherRecords.Add(dict);
                    }
                }
            }
        }
        catch { }

        try
        {
            if (basisBytes != null)
            {
                var (enc, _) = ResolveEncodingForReader(basisBytes, null);
                var text = enc.GetString(basisBytes);
                if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF') text = text.Substring(1);
                var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
                var hdrIdx = lines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                if (hdrIdx >= 0)
                {
                    var detDel = DetermineDelimiter(basisBytes, "\t");
                    var hdr = SplitRow(lines[hdrIdx], detDel, null);
                    var headerLen = hdr.Length;
                    for (int i = hdrIdx + 1; i < lines.Count; i++)
                    {
                        var line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = SplitRow(line, detDel, null);
                        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        var mapLen = Math.Min(headerLen, fields.Length);
                        for (int c = 0; c < mapLen; c++) dict[hdr[c].Trim()] = fields[c];
                        for (int c = mapLen; c < headerLen; c++) dict[hdr[c].Trim()] = string.Empty;
                        // store data row index (first data row = 0)
                        dict["__rowIndex"] = (i - hdrIdx - 1).ToString(CultureInfo.InvariantCulture);
                        basisRecords.Add(dict);
                    }
                }
            }
        }
        catch { }

        var students = BuildTypedStudents(basisRecords, zusatzRecords, adressenRecords, erzieherRecords);
        Console.WriteLine($"Exit BuildStudentsFromStoredFiles: returning {students.Count} students");
        return students;
    }
}
