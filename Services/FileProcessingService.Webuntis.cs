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
        await Task.Yield();
        var result = new ProcessingResult { Success = true };

        try
        {
            var studentsFile = files.FirstOrDefault(f => f.FileKey == "students");
            var basisFile = files.FirstOrDefault(f => f.FileKey == "basisdaten");
            var zusatzFile = files.FirstOrDefault(f => f.FileKey == "zusatzdaten");
            var erzieherFile = files.FirstOrDefault(f => f.FileKey == "erzieher");
            var adressenFile = files.FirstOrDefault(f => f.FileKey == "adressen");

            // Require at least basis records. Other files are optional fallbacks (zusatz/adressen/erzieher/students)
            if (basisFile?.Content == null)
            {
                return new ProcessingResult
                {
                    Success = false,
                    Message = "F¸r 'Webuntis & Co.' wird mindestens die Datei SchuelerBasisdaten.dat benˆtigt. Bitte laden Sie diese hoch. Optional kˆnnen Sie SchuelerZusatzdaten.dat (f¸r schulische E?Mails), SchuelerAdressen.dat und SchuelerErzieher.dat bereitstellen."
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

                DateTime ParseDateOrMin(string? dt)
                {
                    if (string.IsNullOrWhiteSpace(dt)) return DateTime.MinValue;
                    var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" };
                    if (DateTime.TryParseExact(dt, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return parsed;
                    if (DateTime.TryParse(dt, new CultureInfo("de-DE"), DateTimeStyles.None, out parsed)) return parsed;
                    if (DateTime.TryParse(dt, out parsed)) return parsed;
                    return DateTime.MinValue;
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
                        // If header columns match data columns, parse permissively by position.
                        // Some SchILD exports include header fields with extra spaces or unexpected characters
                        // which may cause header/data length mismatch. In that case try a permissive parse where
                        // we map columns by position up to the min(headerLength, dataLength).
                        var headerLen = bhdr.Length;
                        for (int i = bhdrIdx + 1; i < blines.Count; i++)
                        {
                            var line = blines[i];
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var fields = SplitRow(line, detDel, string.IsNullOrEmpty(basisFile.Quote) ? (char?)null : basisFile.Quote[0]);
                            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            var mapLen = Math.Min(headerLen, fields.Length);
                            for (int c = 0; c < mapLen; c++)
                                dict[bhdr[c].Trim()] = fields[c];
                            // If there are extra header columns without data, fill with empty string
                            for (int c = mapLen; c < headerLen; c++)
                                dict[bhdr[c].Trim()] = string.Empty;
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
            // Build exportStudents after parsing all input records
            var exportStudents = uniqueStudents;
            // Build strongly typed students list from basisRecords (and enrich with zusatz/adressen/erzieher later)
            List<Models.Student> DatStudents = new();
            try
            {
                DatStudents = BuildTypedStudents(basisRecords, zusatzRecords, adressenRecords, erzieherRecords);
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

            var aktSj1 = (DateTime.Now.Month > 7 ? DateTime.Now.Year + 1 : DateTime.Now.Year).ToString();
            // determine mail domain for username extraction (optional input)
            var mailDomain = inputs.TryGetValue("mailDomain", out var md) ? (md ?? string.Empty).Trim() : "@students.berufskolleg-borken.de";
            if (!mailDomain.StartsWith("@")) mailDomain = "@" + mailDomain;

            // Helper used to match person records across different input files by name + birthdate
            bool PersonMatches(IDictionary<string, string> r, string lname, string fname, string bdate)
            {
                if (r == null) return false;
                var rln = GetDictValue(r, "Nachname", "Familienname", "name")?.Trim() ?? string.Empty;
                var rfn = GetDictValue(r, "Vorname", "givenName", "forename")?.Trim() ?? string.Empty;
                var rbd = GetDictValue(r, "Geburtsdatum", "birthDate", "Geburtsdatum")?.Trim() ?? string.Empty;

                if (!string.Equals(rln, (lname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(rfn, (fname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)) return false;

                // Compare birthdates leniently: try parsing common formats, otherwise fallback to string compare
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
            using var msStudents = new System.IO.MemoryStream();
            int studentsRowsWritten = 0;
            using (var sw = new System.IO.StreamWriter(msStudents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
            {
                sw.WriteLine(string.Join(',', webHeaders));
                var schulnummer = inputs.TryGetValue("Schulnummer", out var sn) ? sn : string.Empty;
                // If no students data rows were detected but basisRecords exist, include all basis records
                var includeAllFromBasis = (!students.Any()) && (basisRecords != null && basisRecords.Any());

                foreach (var s in exportStudents)
                {
                    string GetS(string k) => s.TryGetValue(k, out var v) ? v : string.Empty;
                    var lname = GetS("longName");
                    var fname = GetS("foreName");
                    var bdate = GetS("birthDate");
                    var status = GetS("status");

                    // try to find matching basis / zusatz / adressen / erzieher records (use PersonMatches helper)
                    Dictionary<string,string>? sb = null;
                    try { sb = basisRecords.LastOrDefault(r => PersonMatches(r, lname, fname, bdate)); } catch { }

                    Dictionary<string,string>? sz = null;
                    try { sz = zusatzRecords.LastOrDefault(r => PersonMatches(r, lname, fname, bdate)); } catch { }
                    try { if (sz == null) sz = adressenRecords.LastOrDefault(r => PersonMatches(r, lname, fname, bdate)); } catch { }
                    // Fallback: try match by name only if no birthdate-match found
                    try
                    {
                        if (sz == null)
                        {
                            sz = zusatzRecords.LastOrDefault(r =>
                            {
                                var rln = GetDictValue(r, "Nachname", "Familienname", "name").Trim();
                                var rfn = GetDictValue(r, "Vorname", "givenName", "forename").Trim();
                                return string.Equals(rln, (lname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)
                                    && string.Equals(rfn, (fname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
                            });
                        }
                        if (sz == null)
                        {
                            sz = adressenRecords.LastOrDefault(r =>
                            {
                                var rln = GetDictValue(r, "Nachname", "Familienname", "name").Trim();
                                var rfn = GetDictValue(r, "Vorname", "givenName", "forename").Trim();
                                return string.Equals(rln, (lname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)
                                    && string.Equals(rfn, (fname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
                            });
                        }
                    }
                    catch { }
                    Dictionary<string,string>? ad = null; try { ad = adressenRecords.LastOrDefault(r => PersonMatches(r, lname, fname, bdate)); } catch { }
                        Dictionary<string,string>? erz = null;
                        try { erz = erzieherRecords.LastOrDefault(r => PersonMatches(r, lname, fname, bdate)); } catch { }
                        try
                        {
                            if (erz == null)
                            {
                                erz = erzieherRecords.LastOrDefault(r =>
                                {
                                    var rln = GetDictValue(r, "Nachname", "Familienname", "name").Trim();
                                    var rfn = GetDictValue(r, "Vorname", "givenName", "forename").Trim();
                                    return string.Equals(rln, (lname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)
                                        && string.Equals(rfn, (fname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
                                });
                            }
                            // additional fallback: match by last name only
                            if (erz == null)
                            {
                                erz = erzieherRecords.LastOrDefault(r => string.Equals(GetDictValue(r, "Nachname", "Familienname", "name").Trim(), (lname ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
                            }
                        }
                        catch { }

                    int alter = -1; DateTime dob;
                    var dateFormatsMain = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" };
                    // prefer birthdate from basis record (sb) if available, otherwise use bdate from student/export
                    var birthForAge = !string.IsNullOrWhiteSpace(GetDictValue(sb, "Geburtsdatum")) ? GetDictValue(sb, "Geburtsdatum") : bdate;
                    if (DateTime.TryParseExact(birthForAge, dateFormatsMain, CultureInfo.InvariantCulture, DateTimeStyles.None, out dob)) { alter = DateTime.Now.Year - dob.Year; if (DateTime.Now < dob.AddYears(alter)) alter--; }

                    bool includeRecord = false;
                    if (includeAllFromBasis) includeRecord = true;
                    if (string.IsNullOrWhiteSpace(status)) status = GetS("Status");
                    if (status == "2" || status == "6") includeRecord = true;
                    else if (sz != null && sz.TryGetValue("Entlassdatum", out var entl) && DateTime.TryParseExact(entl, new[] { "dd.MM.yyyy", "d.M.yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var entDate) && entDate >= DateTime.Now.AddDays(-42)) includeRecord = true;
                    if (!includeRecord) continue;

                    var email = string.Empty;
                    try { email = GetDictValue(sz, "schulische E-Mail", "MailSchulisch", "schulische E-Mail", "E-Mail"); } catch { }
                    var familienname = !string.IsNullOrWhiteSpace(GetDictValue(sb, "Nachname")) ? GetDictValue(sb, "Nachname") : lname;
                    var vorname = !string.IsNullOrWhiteSpace(GetDictValue(sb, "Vorname")) ? GetDictValue(sb, "Vorname") : fname;
                    var klasse = !string.IsNullOrWhiteSpace(GetDictValue(sb, "Klasse")) ? GetDictValue(sb, "Klasse") : (GetS("klasse") ?? GetS("klasse.name"));
                    var kurzname = !string.IsNullOrWhiteSpace(email) && email.Contains('@') ? email.Split('@')[0] : string.Empty;
                    var geschlecht = (GetS("gender") ?? GetS("Geschlecht")).ToUpperInvariant();
                    var geburtsdatum = !string.IsNullOrWhiteSpace(GetDictValue(sb, "Geburtsdatum")) ? GetDictValue(sb, "Geburtsdatum") : bdate;
                    var eintrittsdatum = string.Empty;
                    string austrittsdatum = string.Empty;
                    if (status == "2" || status == "6") austrittsdatum = "31.07." + aktSj1;
                    else if (s.TryGetValue("ZeugnisdatumLetztesZeugnisInDieserKlasse", out var zd) && !string.IsNullOrWhiteSpace(zd)) austrittsdatum = zd;
                    else if (sz != null && sz.TryGetValue("Entlassdatum", out var ent) && !string.IsNullOrWhiteSpace(ent)) austrittsdatum = ent;

                    var telefon = sz != null && sz.TryGetValue("Telefon-Nr.", out var tel) ? tel : string.Empty;
                    var mobil = string.Empty;
                    var strasse = !string.IsNullOrWhiteSpace(GetDictValue(sb, "Straﬂe", "Strasse")) ? GetDictValue(sb, "Straﬂe", "Strasse") : (GetS("Straﬂe") ?? GetS("street") ?? string.Empty);
                    var plz = !string.IsNullOrWhiteSpace(GetDictValue(sb, "Postleitzahl", "PLZ")) ? GetDictValue(sb, "Postleitzahl", "PLZ") : (GetS("Postleitzahl") ?? GetS("PLZ") ?? GetS("postCode") ?? string.Empty);
                    var ort = !string.IsNullOrWhiteSpace(GetDictValue(sb, "Ort")) ? GetDictValue(sb, "Ort") : (GetS("Ort") ?? GetS("city") ?? string.Empty);
                    var erzName = string.Empty;
                    var erzMobil = string.Empty;
                    var erzTelefon = string.Empty;
                    if (alter < 18 && erz != null)
                    {
                        // Use the Erzieher record fields when available (populate ErzName) only for minors
                        var v1 = GetDictValue(erz, "Vorname 1.Person", "Vorname 1", "Vorname");
                        var n1 = GetDictValue(erz, "Nachname 1.Person", "Nachname 1", "Nachname");
                        var streetErz = GetDictValue(erz, "Straﬂe", "Strasse", "street");
                        var combined = (v1 + (string.IsNullOrWhiteSpace(v1) || string.IsNullOrWhiteSpace(n1) ? string.Empty : " ") + n1).Trim();
                        if (!string.IsNullOrWhiteSpace(combined))
                        {
                            erzName = !string.IsNullOrWhiteSpace(streetErz) ? combined + ", " + streetErz : combined;
                        }
                        erzMobil = GetDictValue(erz, "E-Mail 1. Person", "E-Mail", "Email");
                        erzTelefon = GetDictValue(erz, "Telefon", "Telefon-Nr.");
                    }
                    var volljaehrig = alter >= 18 ? "1" : "0";
                    var betrName = ad != null ? GetDictValue(ad, "Name1", "Name") : string.Empty;
                    var betrStr = ad != null ? GetDictValue(ad, "Straﬂe", "Strasse", "street") : string.Empty;
                    var betrPlz = ad != null ? GetDictValue(ad, "PLZ") : string.Empty;
                    var betrOrt = ad != null ? GetDictValue(ad, "Ort") : string.Empty;
                    var betrTel = ad != null ? GetDictValue(ad, "1. Tel.-Nr.", "Telefon") : string.Empty;
                    var betrTel2 = ad != null ? GetDictValue(ad, "2. Tel.-Nr.") : string.Empty;
                    var betrMail = ad != null ? GetDictValue(ad, "E-Mail", "Email") : string.Empty;
                    var betrBetreuer = ad != null ? ((GetDictValue(ad, "Betreuer Anrede") != string.Empty ? GetDictValue(ad, "Betreuer Anrede") + " " : string.Empty) + (GetDictValue(ad, "Betreuer Vorname") != string.Empty ? GetDictValue(ad, "Betreuer Vorname") + " " : string.Empty) + GetDictValue(ad, "Betreuer Nachname")).Trim() : string.Empty;
                    var schildAddrId = ad != null ? GetDictValue(ad, "SchILD-Adress-ID", "SchILD Adress ID") : string.Empty;
                    var mailSchulisch = !string.IsNullOrWhiteSpace(GetDictValue(sb, "MailSchulisch")) ? GetDictValue(sb, "MailSchulisch") : (sz != null ? GetDictValue(sz, "schulische E-Mail") : string.Empty);
                    var benutzer = string.Empty;
                    if (!string.IsNullOrWhiteSpace(mailSchulisch))
                    {
                        if (mailSchulisch.Contains('@')) benutzer = mailSchulisch.Split('@')[0];
                        else benutzer = mailSchulisch.Replace(mailDomain, string.Empty, StringComparison.OrdinalIgnoreCase);
                    }

                    var row = new List<string>
                    {
                        EscapeCsv(email), EscapeCsv(familienname), EscapeCsv(vorname), EscapeCsv(klasse), EscapeCsv(kurzname), EscapeCsv(geschlecht), EscapeCsv(geburtsdatum), EscapeCsv(eintrittsdatum), EscapeCsv(austrittsdatum),
                        EscapeCsv(telefon), EscapeCsv(mobil), EscapeCsv(strasse), EscapeCsv(plz), EscapeCsv(ort), EscapeCsv(erzName), EscapeCsv(erzMobil), EscapeCsv(erzTelefon), EscapeCsv(volljaehrig),
                        EscapeCsv(betrName), EscapeCsv(betrStr), EscapeCsv(betrPlz), EscapeCsv(betrOrt), EscapeCsv(betrTel), EscapeCsv(betrTel2), EscapeCsv(betrMail), EscapeCsv(betrBetreuer), EscapeCsv(schildAddrId),
                        EscapeCsv(mailSchulisch), EscapeCsv(benutzer)
                    };

                    sw.WriteLine(string.Join(',', row));
                    studentsRowsWritten++;
                }

                sw.Flush();
                msStudents.Position = 0;
            }

            // NOTE: per UI requirements the direct "Webuntis-Stammdaten-Schueler.csv" download is omitted.
            // The ImportNachWebuntis-Stammdaten-Schueler.csv is still produced below and remains available.

            // --- ImportNachWebuntis-Stammdaten-Schueler.csv (Zielformat) ---
            var targetHeaders = new[] {
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

                using (var msTarget = new System.IO.MemoryStream())
                {
                    using (var swt = new System.IO.StreamWriter(msTarget, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
                    {
                        swt.WriteLine(string.Join(',', targetHeaders));

                        // If no students file was provided but basisRecords exist, include all basis records
                        var includeAllFromBasis = (studentsFile?.Content == null) && (basisRecords != null && basisRecords.Any());

                        // Build target list strictly from SchuelerBasisdaten: include only status 2 or 6, deduplicate by name+birthdate
                        var targetBases = new List<Dictionary<string,string>>();
                        try
                        {
                            var basisSelected = basisRecords.Where(b =>
                            {
                                var st = GetDictValue(b, "Status", "status");
                                return st == "2" || st == "6";
                            }).ToList();

                            // group duplicates by Nachname|Vorname|Geburtsdatum
                            var grouped = basisSelected.GroupBy(b => (GetDictValue(b, "Nachname") + "|" + GetDictValue(b, "Vorname") + "|" + GetDictValue(b, "Geburtsdatum")).ToLowerInvariant());
                            var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" };
                            foreach (var g in grouped)
                            {
                                if (g.Count() == 1)
                                {
                                    targetBases.Add(g.First());
                                    continue;
                                }

                                // pick the one with later BeginnBildungsgang from zusatzRecords
                                Dictionary<string,string>? best = null;
                                DateTime bestDate = DateTime.MinValue;
                            foreach (var b in g)
                            {
                                var nach = GetDictValue(b, "Nachname");
                                var vor = GetDictValue(b, "Vorname");
                                var geb = GetDictValue(b, "Geburtsdatum");

                                // Find all matching zusatz records for this person and pick the latest BeginnBildungsgang
                                DateTime dt = DateTime.MinValue;
                                try
                                {
                                    // Prefer matching by external ID if the basis record contains one
                                    var bExtId = GetDictValue(b, "Externe ID-Nr", "Externe ID", "Externe ID Nr", "Externe ID-Nr");
                                    List<Dictionary<string, string>> matches = new();
                                    if (!string.IsNullOrWhiteSpace(bExtId))
                                    {
                                        matches = zusatzRecords.Where(r => string.Equals(GetDictValue(r, "Externe ID-Nr", "Externe ID", "Externe ID Nr"), bExtId, StringComparison.OrdinalIgnoreCase)).ToList();
                                    }
                                    if (matches == null || matches.Count == 0)
                                    {
                                        // fallback to person/birthdate matching
                                        matches = zusatzRecords.Where(r => PersonMatches(r, nach, vor, geb)).ToList();
                                        if (matches == null || matches.Count == 0)
                                        {
                                            // final fallback: name-only
                                            matches = zusatzRecords.Where(r =>
                                                string.Equals(GetDictValue(r, "Nachname").Trim(), nach.Trim(), StringComparison.OrdinalIgnoreCase)
                                                && string.Equals(GetDictValue(r, "Vorname").Trim(), vor.Trim(), StringComparison.OrdinalIgnoreCase)
                                            ).ToList();
                                        }
                                    }

                                    foreach (var zr in matches)
                                    {
                                        var bb = GetDictValue(zr, "BeginnBildungsgang", "Beginn Bildungsgang", "BeginnBildungsgang");
                                        if (!string.IsNullOrWhiteSpace(bb) && DateTime.TryParseExact(bb, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                                        {
                                            if (parsed > dt) dt = parsed;
                                        }
                                    }
                                }
                                catch { }

                                // choose basis record: prefer later BeginnBildungsgang; on tie prefer the later basis record in file
                                if (dt >= bestDate)
                                {
                                    best = b;
                                    bestDate = dt;
                                }
                            }

                                targetBases.Add(best ?? g.First());
                            }
                        }
                        catch
                        {
                            targetBases = basisRecords.Where(b => (GetDictValue(b, "Status") == "2" || GetDictValue(b, "Status") == "6")).ToList();
                        }

                        foreach (var b in targetBases)
                        {
                            var familienname = GetDictValue(b, "Nachname", "Familienname", "name");
                            var vorname = GetDictValue(b, "Vorname", "givenName", "forename");
                            var geburtsdatum = GetDictValue(b, "Geburtsdatum", "birthDate");
                            var status = GetDictValue(b, "Status", "status");

                            // find related zusatz, adressen, erzieher by person
                            Dictionary<string,string>? sz = null; try { sz = zusatzRecords.LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum)); } catch { }
                            Dictionary<string,string>? ad = null; try { ad = adressenRecords.LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum)); } catch { }
                            Dictionary<string,string>? erz = null; try { erz = erzieherRecords.LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum)); } catch { }

                            // compute age
                            int alter = -1; DateTime dtb; var formats2 = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" };
                            if (DateTime.TryParseExact(geburtsdatum, formats2, CultureInfo.InvariantCulture, DateTimeStyles.None, out dtb)) { alter = DateTime.Now.Year - dtb.Year; if (DateTime.Now < dtb.AddYears(alter)) alter--; }

                            // only include if status 2 or 6
                            if (!(status == "2" || status == "6")) continue;

                            var email = sz != null ? GetDictValue(sz, "schulische E-Mail", "MailSchulisch", "E-Mail") : string.Empty;
                            var klasse = GetDictValue(b, "Klasse", "klasse", "Klasse.name");
                            var kurzname = !string.IsNullOrWhiteSpace(email) && email.Contains('@') ? email.Split('@')[0] : string.Empty;
                            var geschlecht = GetDictValue(b, "Geschlecht").ToUpperInvariant();
                            var eintrittsdatum = string.Empty;
                            var austrittsdatum = status == "2" || status == "6" ? "31.07." + aktSj1 : (sz != null ? GetDictValue(sz, "Entlassdatum") : string.Empty);
                            var telefon = sz != null ? GetDictValue(sz, "Telefon-Nr.", "Telefon") : string.Empty;
                            var mobil = string.Empty;
                            var strasse = GetDictValue(b, "Straﬂe", "Strasse", "street");
                            var plz = GetDictValue(b, "PLZ", "Postleitzahl");
                            var ort = GetDictValue(b, "Ort");

                            var erzName = string.Empty; var erzMobil = string.Empty; var erzTelefon = string.Empty;
                            if (alter < 18 && erz != null)
                            {
                                var v1 = GetDictValue(erz, "Vorname 1.Person", "Vorname 1", "Vorname");
                                var n1 = GetDictValue(erz, "Nachname 1.Person", "Nachname 1", "Nachname");
                                var streetErz = GetDictValue(erz, "Straﬂe", "Strasse", "street");
                                var combined = (v1 + (string.IsNullOrWhiteSpace(v1) || string.IsNullOrWhiteSpace(n1) ? string.Empty : " ") + n1).Trim();
                                if (!string.IsNullOrWhiteSpace(combined)) erzName = !string.IsNullOrWhiteSpace(streetErz) ? combined + ", " + streetErz : combined;
                                erzMobil = GetDictValue(erz, "E-Mail 1. Person", "E-Mail", "Email");
                                erzTelefon = GetDictValue(erz, "Telefon", "Telefon-Nr.");
                            }

                            var betrName = ad != null ? GetDictValue(ad, "Name1", "Name") : string.Empty;
                            var betrStr = ad != null ? GetDictValue(ad, "Straﬂe", "Strasse", "street") : string.Empty;
                            var betrPlz = ad != null ? GetDictValue(ad, "PLZ") : string.Empty;
                            var betrOrt = ad != null ? GetDictValue(ad, "Ort") : string.Empty;
                            var betrTel = ad != null ? GetDictValue(ad, "1. Tel.-Nr.", "Telefon") : string.Empty;
                            var betrTel2 = ad != null ? GetDictValue(ad, "2. Tel.-Nr.") : string.Empty;
                            var betrMail = ad != null ? GetDictValue(ad, "E-Mail", "Email") : string.Empty;
                            var betrBetreuer = ad != null ? ((GetDictValue(ad, "Betreuer Anrede") != string.Empty ? GetDictValue(ad, "Betreuer Anrede") + " " : string.Empty) + (GetDictValue(ad, "Betreuer Vorname") != string.Empty ? GetDictValue(ad, "Betreuer Vorname") + " " : string.Empty) + GetDictValue(ad, "Betreuer Nachname")).Trim() : string.Empty;
                            var schildAddrId = ad != null ? GetDictValue(ad, "SchILD-Adress-ID", "SchILD Adress ID") : string.Empty;

                            var mailSchulisch = !string.IsNullOrWhiteSpace(GetDictValue(b, "MailSchulisch")) ? GetDictValue(b, "MailSchulisch") : (sz != null ? GetDictValue(sz, "schulische E-Mail") : string.Empty);
                            var o365 = mailSchulisch ?? string.Empty;
                            var benutzer = string.Empty; if (!string.IsNullOrWhiteSpace(mailSchulisch) && mailSchulisch.Contains('@')) benutzer = mailSchulisch.Split('@')[0];

                            var row = new List<string>
                            {
                                EscapeCsv(email), EscapeCsv(familienname), EscapeCsv(vorname), EscapeCsv(klasse), EscapeCsv(kurzname), EscapeCsv(geschlecht), EscapeCsv(geburtsdatum), EscapeCsv(eintrittsdatum), EscapeCsv(austrittsdatum),
                                EscapeCsv(telefon), EscapeCsv(mobil), EscapeCsv(strasse), EscapeCsv(plz), EscapeCsv(ort), EscapeCsv(erzName), EscapeCsv(erzMobil), EscapeCsv(erzTelefon), EscapeCsv(alter >= 18 ? "1" : "0"),
                                EscapeCsv(betrName), EscapeCsv(betrStr), EscapeCsv(betrPlz), EscapeCsv(betrOrt), EscapeCsv(betrTel), EscapeCsv(betrTel2), EscapeCsv(betrMail), EscapeCsv(betrBetreuer), EscapeCsv(schildAddrId),
                                EscapeCsv(o365), EscapeCsv(benutzer)
                            };

                            swt.WriteLine(string.Join(',', row));
                            targetRowsWritten++;
                        }
                    }

                    msTarget.Position = 0;
                    targetBytes = msTarget.ToArray();
                }
            }
            catch { }
            // If the generation produced no rows (e.g. students present but filtered out),
            // fall back to using basisRecords as source so an import file is produced.
            if (targetRowsWritten == 0 && (basisRecords != null && basisRecords.Any()))
            {
                try
                {
                    using var msTarget2 = new System.IO.MemoryStream();
                    int fallbackRows = 0;
                    using (var swt2 = new System.IO.StreamWriter(msTarget2, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
                    {
                        swt2.WriteLine(string.Join(',', targetHeaders));
                        foreach (var b in basisRecords)
                        {
                            // map basis record to target columns, but prefer values from zusatz/adressen when available
                            string familienname = GetDictValue(b, "Nachname", "Familienname", "name");
                            string vorname = GetDictValue(b, "Vorname", "givenName", "forename");
                            string geburtsdatum = GetDictValue(b, "Geburtsdatum", "birthDate");

                            // try to find matching zusatz/adressen record for richer data
                            Dictionary<string, string>? szMatch = null;
                            try { szMatch = zusatzRecords.LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum)); } catch { }
                            try { if (szMatch == null) szMatch = adressenRecords.LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum)); } catch { }
                            // name-only fallback
                            try
                            {
                                if (szMatch == null)
                                {
                                    szMatch = zusatzRecords.LastOrDefault(r =>
                                        string.Equals(GetDictValue(r, "Nachname", "Familienname", "name").Trim(), familienname.Trim(), StringComparison.OrdinalIgnoreCase)
                                        && string.Equals(GetDictValue(r, "Vorname", "givenName", "forename").Trim(), vorname.Trim(), StringComparison.OrdinalIgnoreCase)
                                    );
                                }
                            }
                            catch { }

                            string email = string.Empty;
                            if (szMatch != null)
                            {
                                email = GetDictValue(szMatch, "schulische E-Mail", "MailSchulisch", "E-Mail");
                            }
                            if (string.IsNullOrWhiteSpace(email))
                            {
                                // fallback to any email field in basis
                                email = GetDictValue(b, "schulische E-Mail", "MailSchulisch", "E-Mail", "Email");
                            }

                            string klasse = GetDictValue(b, "Klasse", "klasse", "Klasse.name");
                            string kurzname = string.Empty;
                            if (!string.IsNullOrWhiteSpace(email) && email.Contains('@')) kurzname = email.Split('@')[0];
                            string geschlecht = GetDictValue(b, "Geschlecht");
                            string eintrittsdatum = string.Empty;
                            string austrittsdatum = string.Empty;

                            // address/phone prefer adressen then basis
                            var adMatch = adressenRecords.LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum));
                            string telefon = GetDictValue(adMatch, "Telefon-Nr.", "Telefon") ?? GetDictValue(b, "Telefon-Nr.", "Telefon");
                            string mobil = GetDictValue(adMatch, "Mobil", "Fax/Mobilnr") ?? string.Empty;
                            string strasse = GetDictValue(adMatch, "Straﬂe", "Strasse", "street") ?? GetDictValue(b, "Straﬂe", "Strasse", "street");
                            string plz = GetDictValue(adMatch, "PLZ", "Postleitzahl") ?? GetDictValue(b, "PLZ", "Postleitzahl");
                            string ort = GetDictValue(adMatch, "Ort") ?? GetDictValue(b, "Ort");

                            string erzName = string.Empty;
                            string erzMobil = string.Empty;
                            string erzTelefon = string.Empty;
                            // compute age / Volljaehrig from Geburtsdatum (needed for erz handling)
                            int alter = -1; DateTime dob;
                            var dateFormats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd" };
                            if (DateTime.TryParseExact(geburtsdatum, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dob))
                            {
                                alter = DateTime.Now.Year - dob.Year; if (DateTime.Now < dob.AddYears(alter)) alter--;
                            }
                            // try to find matching erzieher for fallback rows as well, but only use for minors
                            try
                            {
                                if (alter < 18)
                                {
                                    var erzMatch = erzieherRecords.LastOrDefault(r => PersonMatches(r, familienname, vorname, geburtsdatum));
                                    if (erzMatch == null)
                                    {
                                        erzMatch = erzieherRecords.LastOrDefault(r => string.Equals(GetDictValue(r, "Nachname", "Familienname", "name").Trim(), familienname.Trim(), StringComparison.OrdinalIgnoreCase));
                                    }
                                    if (erzMatch != null)
                                    {
                                        var v1 = GetDictValue(erzMatch, "Vorname 1.Person", "Vorname 1", "Vorname");
                                        var n1 = GetDictValue(erzMatch, "Nachname 1.Person", "Nachname 1", "Nachname");
                                        var streetErz = GetDictValue(erzMatch, "Straﬂe", "Strasse", "street");
                                        var combined = (v1 + (string.IsNullOrWhiteSpace(v1) || string.IsNullOrWhiteSpace(n1) ? string.Empty : " ") + n1).Trim();
                                        if (!string.IsNullOrWhiteSpace(combined))
                                        {
                                            erzName = !string.IsNullOrWhiteSpace(streetErz) ? combined + ", " + streetErz : combined;
                                        }
                                        erzMobil = GetDictValue(erzMatch, "E-Mail 1. Person", "E-Mail", "Email");
                                        erzTelefon = GetDictValue(erzMatch, "Telefon", "Telefon-Nr.");
                                    }
                                }
                            }
                            catch { }
                            string volljaehrig = alter >= 18 ? "1" : "0";

                            // company fields: prefer SchuelerAdressen (adMatch)
                            string betrName = GetDictValue(adMatch, "Name1", "Name");
                            string betrStr = GetDictValue(adMatch, "Straﬂe", "Strasse", "street");
                            string betrPlz = GetDictValue(adMatch, "PLZ");
                            string betrOrt = GetDictValue(adMatch, "Ort");
                            string betrTel = GetDictValue(adMatch, "1. Tel.-Nr.", "Telefon");
                            string betrTel2 = GetDictValue(adMatch, "2. Tel.-Nr.");
                            string betrMail = GetDictValue(adMatch, "E-Mail", "Email");
                            string betrBetreuer = string.Empty;
                            try
                            {
                                var anrede = GetDictValue(adMatch, "Betreuer Anrede");
                                var bv = GetDictValue(adMatch, "Betreuer Vorname");
                                var bn = GetDictValue(adMatch, "Betreuer Nachname");
                                betrBetreuer = (string.IsNullOrWhiteSpace(anrede) ? string.Empty : anrede + " ") + (string.IsNullOrWhiteSpace(bv) ? string.Empty : bv + " ") + (bn ?? string.Empty);
                                betrBetreuer = betrBetreuer.Trim();
                            }
                            catch { }
                            string schildAddrId = GetDictValue(adMatch, "SchILD-Adress-ID", "SchILD Adress ID");

                            // O365 identity and username: prefer schulische E-Mail from zusatz/adressen, otherwise basis email
                            string o365 = !string.IsNullOrWhiteSpace(GetDictValue(szMatch, "schulische E-Mail", "MailSchulisch")) ? GetDictValue(szMatch, "schulische E-Mail", "MailSchulisch") : GetDictValue(b, "MailSchulisch", "schulische E-Mail", "E-Mail");
                            string benutzer = string.Empty;
                            if (string.IsNullOrWhiteSpace(benutzer) && !string.IsNullOrWhiteSpace(o365) && o365.Contains('@')) benutzer = o365.Split('@')[0];

                            var record = new List<string>
                            {
                                EscapeCsv(email),
                                EscapeCsv(familienname),
                                EscapeCsv(vorname),
                                EscapeCsv(klasse),
                                EscapeCsv(kurzname),
                                EscapeCsv(geschlecht),
                                EscapeCsv(geburtsdatum),
                                EscapeCsv(eintrittsdatum),
                                EscapeCsv(austrittsdatum),
                                EscapeCsv(telefon),
                                EscapeCsv(mobil),
                                EscapeCsv(strasse),
                                EscapeCsv(plz),
                                EscapeCsv(ort),
                                EscapeCsv(erzName),
                                EscapeCsv(erzMobil),
                                EscapeCsv(erzTelefon),
                                EscapeCsv(volljaehrig),
                                EscapeCsv(betrName),
                                EscapeCsv(betrStr),
                                EscapeCsv(betrPlz),
                                EscapeCsv(betrOrt),
                                EscapeCsv(betrTel),
                                EscapeCsv(betrTel2),
                                EscapeCsv(betrMail),
                                EscapeCsv(betrBetreuer),
                                EscapeCsv(schildAddrId),
                                EscapeCsv(o365),
                                EscapeCsv(benutzer)
                            };

                            swt2.WriteLine(string.Join(',', record));
                            fallbackRows++;
                        }
                        swt2.Flush();
                        msTarget2.Position = 0;
                    }
                    targetBytes = msTarget2.ToArray();
                    targetRowsWritten = fallbackRows;
                    result.MessageHtml += $"<pre>Fallback: used basisRecords to produce {fallbackRows} rows.</pre>";
                }
                catch { }
            }

            result.OutputFiles.Add(new OutputFile { FileName = "ImportNachWebuntis-Stammdaten-Schueler.csv", Content = targetBytes, Hint = "Import nach Webuntis (CSV)" });
            StoreFile("import_webuntis_students", targetBytes);
            try
            {
                result.MessageHtml += $"<pre>Export rows (students file): {studentsRowsWritten}, (ImportNachWebuntis rows): {targetRowsWritten}</pre>";
                try { result.MessageHtml += $"<pre>uniqueStudents: {uniqueStudents?.Count ?? 0}, students: {students?.Count ?? 0}, basisRecords: {basisRecords?.Count ?? 0}</pre>"; } catch { }
            }
            catch { }
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
                    s.Strasse = GetDictValue(admatch, "Straﬂe", "Strasse", "street");
                    s.PLZ = GetDictValue(admatch, "PLZ", "Postleitzahl");
                    s.Ort = GetDictValue(admatch, "Ort");
                }
                // fallback to primary basis values if still empty
                if (string.IsNullOrWhiteSpace(s.MailSchulisch)) s.MailSchulisch = GetDictValue(primary, "MailSchulisch", "schulische E-Mail", "E-Mail", "Email");
                if (string.IsNullOrWhiteSpace(s.Strasse)) s.Strasse = GetDictValue(primary, "Straﬂe", "Strasse", "street");
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

                // Try to find a matching zusatz record for this specific basis record to get BeginnBildungsgang
                try
                {
                    Dictionary<string, string>? matchZ = null;
                    if (zusatzRecords != null && zusatzRecords.Any())
                    {
                        var bExtId = GetDictValue(b, "Externe ID-Nr", "Externe ID", "Externe ID Nr", "Externe ID-Nr");
                        if (!string.IsNullOrWhiteSpace(bExtId))
                        {
                            matchZ = zusatzRecords.LastOrDefault(r => string.Equals(GetDictValue(r, "Externe ID-Nr", "Externe ID", "Externe ID Nr"), bExtId, StringComparison.OrdinalIgnoreCase));
                        }
                        if (matchZ == null)
                        {
                            matchZ = zusatzRecords.LastOrDefault(r => PersonMatches(r, GetDictValue(b, "Nachname"), GetDictValue(b, "Vorname"), GetDictValue(b, "Geburtsdatum")));
                        }
                        if (matchZ == null)
                        {
                            matchZ = zusatzRecords.LastOrDefault(r => string.Equals(GetDictValue(r, "Nachname").Trim(), GetDictValue(b, "Nachname").Trim(), StringComparison.OrdinalIgnoreCase)
                                && string.Equals(GetDictValue(r, "Vorname").Trim(), GetDictValue(b, "Vorname").Trim(), StringComparison.OrdinalIgnoreCase));
                        }
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
                    a.Strasse = GetDictValue(ad, "Straﬂe", "Strasse", "street");
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
                    e.Strasse = GetDictValue(erz, "Straﬂe", "Strasse", "street");
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
