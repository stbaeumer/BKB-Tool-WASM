using BKBToolClient.Models;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BKBToolClient.Services;

public partial class FileProcessingService
{
    // Full implementation of ProcessMailadresseSetzen moved from original file.
    public async Task<ProcessingResult> ProcessMailadresseSetzen(List<RequiredFile> files, Dictionary<string, string> inputs, FunctionConfig? config = null)
    {
        await Task.Yield();
        var result = new ProcessingResult { Success = false };

        try
        {
            var basis = files.FirstOrDefault(f => f.FileKey == "basisdaten");
            var zusatz = files.FirstOrDefault(f => f.FileKey == "zusatzdaten");

            if (basis?.Content == null || zusatz?.Content == null)
            {
                result.Message = "Beide Dateien (Basis- und Zusatzdaten) werden benötigt. Bitte stellen Sie sicher, dass insbesondere die Datei 'SchuelerZusatzdaten.dat' (Zusatzdaten) hochgeladen wurde, da diese die schulischen E-Mail-Adressen enthält.";
                return result;
            }

            var domain = inputs.TryGetValue("mailDomain", out var d) ? d.Trim() : "";
            if (string.IsNullOrWhiteSpace(domain))
            {
                result.Message = "Mail-Domain fehlt.";
                return result;
            }

            int basisDataRows = CountCsvDataRows(basis.Content, basis);
            int zusatzDataRows = CountCsvDataRows(zusatz.Content, zusatz);

            if (basisDataRows != zusatzDataRows)
            {
                result.Message = $"Anzahl Datenzeilen unterschiedlich: Basis={basisDataRows}, Zusatz={zusatzDataRows}";
                return result;
            }

            // parse zusatz into records
            var configuredDelimiter = zusatz.Delimiter ?? "\t";
            var delimiter = DetermineDelimiter(zusatz.Content, configuredDelimiter);
            var quote = string.IsNullOrEmpty(zusatz.Quote) ? (char?)null : zusatz.Quote[0];
            var (enc, _) = ResolveEncodingForReader(zusatz.Content, zusatz.Encoding);
            var text = enc.GetString(zusatz.Content);
            var allLines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

            int headerIdx = -1;
            string[]? headers = null;
            var records = new List<object>();

            if (zusatz.HasHeader)
            {
                headerIdx = allLines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                if (headerIdx >= 0)
                {
                    headers = SplitRow(allLines[headerIdx], delimiter, quote);
                    for (int i = headerIdx + 1; i < allLines.Count; i++)
                    {
                        var line = allLines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = SplitRow(line, delimiter, quote);
                        records.Add(fields);
                    }
                }
            }
            else
            {
                foreach (var line in allLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var fields = SplitRow(line, delimiter, quote);
                    records.Add(fields);
                }
            }

            // detect columns
            int emailCol = -1, nachnameCol = -1, vornameCol = -1, gebCol = -1;
            if (headers != null)
            {
                for (int i = 0; i < headers.Length; i++)
                {
                    var h = headers[i].ToLowerInvariant().Trim();
                    if (h.Contains("schulische") && h.Contains("mail")) emailCol = i;
                    if (nachnameCol == -1 && (h.Contains("nachname") || (h.Contains("name") && !h.Contains("vor")))) nachnameCol = i;
                    if (vornameCol == -1 && (h.Contains("vorname") || h.Contains("vor"))) vornameCol = i;
                    if (gebCol == -1 && (h.Contains("geb") || h.Contains("birth") || h.Contains("geburts"))) gebCol = i;
                }
            }

            // infer email col by sampling. If a mailDomain was provided, prefer the column whose values match that domain
            if (emailCol == -1)
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
                int maxCols = headers?.Length ?? records.OfType<string[]>().Select(r => r.Length).DefaultIfEmpty(0).Max();
                var counts = new int[Math.Max(1, maxCols)];
                var domainCounts = new int[Math.Max(1, maxCols)];
                string? normDomain = null;
                try { normDomain = string.IsNullOrWhiteSpace(domain) ? null : (domain.StartsWith("@") ? domain.Substring(1) : domain); } catch { normDomain = null; }

                for (int i = 0; i < Math.Min(200, records.Count); i++)
                {
                    if (records[i] is string[] arr)
                    {
                        for (int c = 0; c < arr.Length; c++)
                        {
                            var v = arr[c];
                            if (string.IsNullOrWhiteSpace(v)) continue;
                            if (emailRegex.IsMatch(v)) counts[c]++;
                            if (normDomain != null && v.IndexOf("@" + normDomain, StringComparison.OrdinalIgnoreCase) >= 0) domainCounts[c]++;
                        }
                    }
                }

                // prefer domain-specific column when present
                if (!string.IsNullOrWhiteSpace(normDomain))
                {
                    int bestDom = Array.IndexOf(domainCounts, domainCounts.Max());
                    if (domainCounts[bestDom] > 0)
                    {
                        emailCol = bestDom;
                    }
                }

                if (emailCol == -1)
                {
                    int best = Array.IndexOf(counts, counts.Max());
                    if (counts[best] > 0) emailCol = best;
                    else emailCol = Math.Min(5, Math.Max(0, counts.Length - 1));
                }
            }

            // Build person keys (Vorname|Nachname|Geburtsdatum) for duplicate handling
            var personKey = new string[records.Count];
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] is string[] arr)
                {
                    var n = nachnameCol >= 0 && arr.Length > nachnameCol ? (arr[nachnameCol] ?? string.Empty).Trim() : string.Empty;
                    var v = vornameCol >= 0 && arr.Length > vornameCol ? (arr[vornameCol] ?? string.Empty).Trim() : string.Empty;
                    var g = gebCol >= 0 && arr.Length > gebCol ? (arr[gebCol] ?? string.Empty).Trim() : string.Empty;
                    personKey[i] = (n + "|" + v + "|" + g).Trim('|', ' ');
                }
                else personKey[i] = string.Empty;
            }

            // collect emails and detect duplicates only when they belong to different persons
            var emailToRows = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < records.Count; i++)
            {
                string? email = null;
                if (records[i] is string[] arr)
                {
                    if (arr.Length > emailCol) email = (arr[emailCol] ?? string.Empty).Trim();
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    if (!emailToRows.TryGetValue(email, out var list)) { list = new List<int>(); emailToRows[email] = list; }
                    list.Add(i);
                }
            }

            var duplicatesAcrossPersons = emailToRows.Where(kv => kv.Value.Count > 1 && kv.Value.Select(idx => personKey[idx]).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1).ToList();
            if (duplicatesAcrossPersons.Any())
            {
                var htmlSb = new StringBuilder();
                htmlSb.AppendLine("<p><strong>Doppelte Einträge in Spalte 'schulische E-Mail' gefunden (bei unterschiedlichen Personen).</strong></p>");
                htmlSb.AppendLine("<table class='table table-sm table-bordered'><thead><tr><th>E-Mail</th><th>Betroffene Zeilen</th></tr></thead><tbody>");
                foreach (var kv in duplicatesAcrossPersons)
                {
                    htmlSb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(kv.Key)}</td><td>{System.Net.WebUtility.HtmlEncode(string.Join(", ", kv.Value.Select(i => (i + 1).ToString())))}</td></tr>");
                }
                htmlSb.AppendLine("</tbody></table>");
                return new ProcessingResult { Success = false, Message = "Doppelte E-Mail-Adressen gefunden.", MessageHtml = htmlSb.ToString() };
            }

            // If every record already has an email (and no cross-person duplicates), nothing to do
            bool allHaveEmail = Enumerable.Range(0, records.Count).All(i => records[i] is string[] a && a.Length > emailCol && !string.IsNullOrWhiteSpace(a[emailCol]));
            if (allHaveEmail)
            {
                var nothingHtml = "<div class='alert alert-info'><strong>Keine Änderungen erforderlich</strong><br/>Alle Schüler besitzen bereits eine eindeutige schulische E-Mail-Adresse.</div>";
                return new ProcessingResult { Success = true, Message = "Keine Änderungen erforderlich. Alle Schüler haben bereits eindeutige schulische E-Mail-Adressen.", MessageHtml = nothingHtml };
            }

            // generate new addresses by grouping identical persons (Vorname|Nachname|Geburtsdatum)
            var existingSet = new HashSet<string>(emailToRows.Keys, StringComparer.OrdinalIgnoreCase);
            var newAddresses = new List<(int RowIdx, string NewEmail, string Name)>();
            var conflicts = new List<(int RowIdx, string Tried, string Reason)>();

            // group indices by personKey
            var groups = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < personKey.Length; i++)
            {
                var k = string.IsNullOrWhiteSpace(personKey[i]) ? "__nokey__" + i : personKey[i];
                if (!groups.TryGetValue(k, out var lst)) { lst = new List<int>(); groups[k] = lst; }
                lst.Add(i);
            }

            foreach (var kv in groups)
            {
                var indices = kv.Value;
                // collect existing distinct emails for this group
                var existingEmails = indices.Select(i => (records[i] as string[]))
                                            .Where(a => a != null && a.Length > emailCol && !string.IsNullOrWhiteSpace(a[emailCol]))
                                            .Select(a => a[emailCol]!.Trim())
                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                            .ToList();

                if (existingEmails.Count > 1)
                {
                    // conflict: different emails for same person
                    var htmlSb = new StringBuilder();
                    htmlSb.AppendLine("<p><strong>Widersprüchliche E-Mail-Adressen für dieselbe Person gefunden.</strong></p>");
                    htmlSb.AppendLine("<table class='table table-sm table-bordered'><thead><tr><th>Person</th><th>Zeilen</th><th>Adressen</th></tr></thead><tbody>");
                    var rows = string.Join(", ", indices.Select(i => (i + 1).ToString()));
                    htmlSb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(kv.Key)}</td><td>{rows}</td><td>{System.Net.WebUtility.HtmlEncode(string.Join(", ", existingEmails))}</td></tr>");
                    htmlSb.AppendLine("</tbody></table>");
                    return new ProcessingResult { Success = false, Message = "Widersprüchliche E-Mail-Adressen für dieselbe Person gefunden.", MessageHtml = htmlSb.ToString() };
                }

                string? adoptedEmail = existingEmails.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(adoptedEmail))
                {
                    // adopt this email for all rows in group that lack it
                    // cross-person duplicates were checked earlier (duplicatesAcrossPersons), so copying within group is safe
                    existingSet.Add(adoptedEmail);
                    foreach (var i in indices)
                    {
                        if (records[i] is string[] arr)
                        {
                            var cur = (arr.Length > emailCol) ? (arr[emailCol] ?? string.Empty).Trim() : string.Empty;
                            if (string.IsNullOrWhiteSpace(cur))
                            {
                                if (arr.Length <= emailCol) Array.Resize(ref arr, emailCol + 1);
                                arr[emailCol] = adoptedEmail;
                                records[i] = arr;
                                newAddresses.Add((i, adoptedEmail, ((nachnameCol >= 0 && arr.Length > nachnameCol ? arr[nachnameCol] : "") + ", " + (vornameCol >= 0 && arr.Length > vornameCol ? arr[vornameCol] : "")).Trim(new char[] { ',', ' '})));
                            }
                        }
                    }
                }
                else
                {
                    // need to generate one address for this group and assign to all rows that lack it
                    // pick representative name/date from first row that has required fields
                    string nach = string.Empty, vor = string.Empty, geb = string.Empty;
                    int representative = -1;
                    foreach (var i in indices)
                    {
                        if (records[i] is string[] arr)
                        {
                            var nn = nachnameCol >= 0 && arr.Length > nachnameCol ? (arr[nachnameCol] ?? string.Empty).Trim() : string.Empty;
                            var vv = vornameCol >= 0 && arr.Length > vornameCol ? (arr[vornameCol] ?? string.Empty).Trim() : string.Empty;
                            var gg = gebCol >= 0 && arr.Length > gebCol ? (arr[gebCol] ?? string.Empty).Trim() : string.Empty;
                            if (!string.IsNullOrWhiteSpace(nn) && !string.IsNullOrWhiteSpace(vv) && !string.IsNullOrWhiteSpace(gg))
                            {
                                nach = nn; vor = vv; geb = gg; representative = i; break;
                            }
                        }
                    }

                    if (representative == -1)
                    {
                        // no sufficient data in group
                        foreach (var i in indices)
                            conflicts.Add((i, string.Empty, "fehlende Angaben (Name/Geburtsdatum)"));
                        continue;
                    }

                    DateTime dob = default; bool parsedDob = false;
                    var tryFormats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd.MM.yy", "d.M.yy", "yyyy-MM-dd", "yyyyMMdd", "ddMMyyyy" };
                    foreach (var fmt in tryFormats)
                    {
                        if (DateTime.TryParseExact(geb, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out dob)) { parsedDob = true; break; }
                    }
                    if (!parsedDob)
                    {
                        if (DateTime.TryParse(geb, CultureInfo.InvariantCulture, DateTimeStyles.None, out dob)) parsedDob = true;
                    }
                    if (!parsedDob)
                    {
                        var digits = new string(geb.Where(char.IsDigit).ToArray());
                        if (digits.Length >= 8)
                        {
                            var s = digits.Substring(0, 8);
                            if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dparsed)) { dob = dparsed; parsedDob = true; }
                            else if (DateTime.TryParseExact(s, "ddMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dparsed)) { dob = dparsed; parsedDob = true; }
                        }
                    }
                    if (!parsedDob)
                    {
                        foreach (var i in indices) conflicts.Add((i, string.Empty, "Geburtsdatum nicht parsbar"));
                        continue;
                    }

                    var nnorm = NormalizeNameForEmail(nach);
                    var vnorm = NormalizeNameForEmail(vor);
                    if (string.IsNullOrEmpty(nnorm) || string.IsNullOrEmpty(vnorm))
                    {
                        foreach (var i in indices) conflicts.Add((i, string.Empty, "fehlende Angaben (Name/Geburtsdatum)"));
                        continue;
                    }

                    var local = (nnorm[0].ToString() + vnorm[0].ToString()).ToLowerInvariant() + dob.ToString("yyMMdd");
                    var candidate = local + (domain.StartsWith("@") ? domain : "@" + domain);

                    if (existingSet.Contains(candidate) || newAddresses.Any(n => n.NewEmail.Equals(candidate, StringComparison.OrdinalIgnoreCase)))
                    {
                        // collision
                        foreach (var i in indices) conflicts.Add((i, candidate, "bereits vorhanden"));
                        continue;
                    }

                    existingSet.Add(candidate);
                    // assign to all rows in group that don't have email
                    foreach (var i in indices)
                    {
                        if (records[i] is string[] arr)
                        {
                            var cur = (arr.Length > emailCol) ? (arr[emailCol] ?? string.Empty).Trim() : string.Empty;
                            if (string.IsNullOrWhiteSpace(cur))
                            {
                                if (arr.Length <= emailCol) Array.Resize(ref arr, emailCol + 1);
                                arr[emailCol] = candidate;
                                records[i] = arr;
                                newAddresses.Add((i, candidate, (nach + ", " + vor).Trim(new char[]{',',' '})));
                            }
                        }
                    }
                }
            }

            // write modified bytes
            using var outMs = new System.IO.MemoryStream();
            var writeEnc = (enc != null && enc.CodePage == Encoding.UTF8.CodePage) ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true) : enc;
            using (var sw = new System.IO.StreamWriter(outMs, writeEnc, leaveOpen: true))
            {
                if (headers != null)
                {
                    sw.WriteLine(string.Join(delimiter, headers));
                }
                foreach (var rec in records)
                {
                    if (rec is string[] arr)
                    {
                        sw.WriteLine(string.Join(delimiter, arr));
                    }
                }
                sw.Flush();
                outMs.Position = 0;
                var modified = outMs.ToArray();

                if (newAddresses.Any())
                {
                    // The exported file must be named exactly 'SchuelerZusatzdaten.dat'
                    var outFileName = "SchuelerZusatzdaten.dat";
                    var outHint = $"{newAddresses.Count} neue Adressen ergänzt (Spalte: {(headers != null ? headers[emailCol] : emailCol.ToString())})";
                    var outProcessingHint = "Datei kann in SchILD-NRW importiert werden. Vorher Backup anlegen.";

                    if (config != null)
                    {
                        var of = config.OutputFiles?.FirstOrDefault(o => o.FileKey == "zusatzdaten_modified");
                        if (of != null)
                        {
                            // Do NOT override the required output filename. Only accept hint/processing hint from config.
                            if (!string.IsNullOrWhiteSpace(of.Hint)) outHint = of.Hint;
                            if (!string.IsNullOrWhiteSpace(of.ProcessingHint)) outProcessingHint = of.ProcessingHint;
                        }
                    }

                    if (conflicts.Any(c => c.Reason == "bereits vorhanden"))
                    {
                        // On collisions with existing/generated addresses, do not provide an output file and instruct the user to fix in SchILD
                        var htmlSb = new StringBuilder();
                        htmlSb.AppendLine("<p><strong>Kollisionen bei der Erzeugung von E-Mail-Adressen gefunden.</strong></p>");
                        htmlSb.AppendLine("<p>Bitte beheben Sie die Konflikte in SchILD und starten Sie die Verarbeitung erneut.</p>");
                        htmlSb.AppendLine("<table class='table table-sm table-bordered'><thead><tr><th>Zeile</th><th>Versuchte Adresse / Grund</th></tr></thead><tbody>");
                        foreach (var c in conflicts.Where(x => x.Reason == "bereits vorhanden"))
                        {
                            htmlSb.AppendLine($"<tr><td>{c.RowIdx + 1}</td><td>{System.Net.WebUtility.HtmlEncode(c.Tried)}</td></tr>");
                        }
                        htmlSb.AppendLine("</tbody></table>");

                        result.Success = false;
                        result.Message = "Kollisionen bei generierten Mailadressen gefunden. Bitte zuerst in SchILD beheben.";
                        result.MessageHtml = htmlSb.ToString();
                        return result;
                    }

                    if (inputs.TryGetValue("onlyChanged", out var oc) && bool.TryParse(oc, out var onlyChanged) && onlyChanged)
                    {
                        try
                        {
                            var filtered = FilterOnlyChangedRows(zusatz.Content, modified, delimiter, quote, zusatz.Encoding, zusatz.HasHeader);
                            modified = filtered;
                            outHint += " (nur neue/veränderte Zeilen enthalten)";
                        }
                        catch { }
                    }

                    result.OutputFiles.Add(new OutputFile
                    {
                        FileName = outFileName,
                        Content = modified,
                        Hint = outHint,
                        ProcessingHint = outProcessingHint
                    });

                    StoreFile("zusatzdaten_modified", modified);
                }

                // compose message
                var msg = new StringBuilder();
                var msgHtml = new StringBuilder();
                if (newAddresses.Any())
                {
                    msg.AppendLine($"{newAddresses.Count} Mailadressen wurden generiert und in den Zusatzdaten ergänzt.");
                    msgHtml.AppendLine("<p><strong>Generierte Mailadressen:</strong></p>");
                    msgHtml.AppendLine("<table class='table table-sm table-striped'><thead><tr><th>Schüler</th><th>Neue E-Mail</th></tr></thead><tbody>");
                    foreach (var na in newAddresses)
                    {
                        msg.AppendLine($" - {na.Name}: {na.NewEmail}");
                        msgHtml.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(na.Name)}</td><td>{System.Net.WebUtility.HtmlEncode(na.NewEmail)}</td></tr>");
                    }
                    msgHtml.AppendLine("</tbody></table>");
                }
                else msg.AppendLine("Keine neuen Mailadressen generiert.");

                if (conflicts.Any())
                {
                    msg.AppendLine();
                    msg.AppendLine("Nicht automatisch ergänzt aufgrund von Konflikten / fehlenden Angaben:");
                    msgHtml.AppendLine("<p><strong>Nicht ergänzt (Konflikte / fehlende Angaben):</strong></p>");
                    msgHtml.AppendLine("<table class='table table-sm table-bordered'><thead><tr><th>Zeile</th><th>Grund / Versuchte Adresse</th></tr></thead><tbody>");
                    foreach (var c in conflicts)
                    {
                        msg.AppendLine($" - Zeile {c.RowIdx + 1}: {c.Reason}{(string.IsNullOrWhiteSpace(c.Tried) ? "" : $" (versuchte Adresse: {c.Tried})")} ");
                        var tried = string.IsNullOrWhiteSpace(c.Tried) ? c.Reason : c.Reason + " (" + c.Tried + ")";
                        msgHtml.AppendLine($"<tr><td>{c.RowIdx + 1}</td><td>{System.Net.WebUtility.HtmlEncode(tried)}</td></tr>");
                    }
                    msgHtml.AppendLine("</tbody></table>");
                    msgHtml.AppendLine("<p>Bitte setzen Sie diese Einträge manuell in SchILD, anschließend Verarbeitung erneut starten.</p>");
                }

                result.Message = msg.ToString();
                result.MessageHtml = msgHtml.Length > 0 ? msgHtml.ToString() : null;
                result.Success = conflicts.Count == 0;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Fehler bei der Mailadressen-Verarbeitung: {ex.Message}";
        }

        await Task.Delay(200);
        return result;
    }
}
