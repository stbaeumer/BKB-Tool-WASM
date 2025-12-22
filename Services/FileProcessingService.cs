using BKBToolClient.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;

namespace BKBToolClient.Services;

public partial class FileProcessingService
{
    private readonly ConcurrentDictionary<string, byte[]> _sessionFiles = new();
    private readonly string _sessionId = Guid.NewGuid().ToString();

    public string SessionId => _sessionId;

    public void StoreFile(string key, byte[] content) => _sessionFiles[key] = content;

    public byte[]? GetFile(string key) => _sessionFiles.TryGetValue(key, out var content) ? content : null;

    public void ClearFiles() => _sessionFiles.Clear();

    // Event to allow UI (pages/components) to react when a global "return to start" is requested
    public event Func<Task>? ReturnToStartRequested;

    // Request that all subscribers reset state / return to the start page. Clears stored bytes first.
    public async Task RequestReturnToStartAsync()
    {
        try
        {
            ClearFiles();
            var handlers = ReturnToStartRequested;
            if (handlers == null) return;
            var list = handlers.GetInvocationList();
            foreach (var d in list)
            {
                try
                {
                    if (d is Func<Task> f)
                        await f().ConfigureAwait(false);
                }
                catch { }
            }
        }
        catch { }
    }

    

    public (int Lines, int Columns) AnalyzeFile(byte[] content, string delimiter = "\t", string? quote = null, string encodingName = "utf-8")
    {
        var enc = ResolveEncoding(content, encodingName);
        var text = enc.GetString(content);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToArray();
        int lineCount = lines.Length;
        int columnCount = 0;
        if (lineCount > 0)
        {
            var first = lines.FirstOrDefault() ?? string.Empty;
            // detect actual delimiter used in the file (fallback to configured)
            var actualDelimiter = DetermineDelimiter(content, delimiter);
            char? q = string.IsNullOrEmpty(quote) ? (char?)null : quote![0];
            columnCount = SplitRow(first, actualDelimiter, q).Length;
        }
        return (lineCount, columnCount);
    }

    // Try to detect the delimiter used in the file. If the configured delimiter appears in the
    // sample line we keep it. Otherwise pick the most frequent candidate among common delimiters.
    private string DetermineDelimiter(byte[] content, string configuredDelimiter)
    {
        try
        {
            var (enc, _) = ResolveEncodingForReader(content, null);
            var text = enc.GetString(content);
            var first = text.Split('\n').Select(l => l.TrimEnd('\r')).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty;
            if (string.IsNullOrEmpty(first)) return configuredDelimiter;

            // If configured delimiter is present in the line, honor it
            if (!string.IsNullOrEmpty(configuredDelimiter) && first.Contains(configuredDelimiter))
                return configuredDelimiter;

            // Candidate delimiters
            var candidates = new[] { "\t", "|", ";", "," };
            string best = configuredDelimiter;
            int bestCount = -1;
            foreach (var c in candidates)
            {
                var count = first.Count(ch => ch.ToString() == c);
                if (count > bestCount)
                {
                    bestCount = count;
                    best = c;
                }
            }

            return bestCount > 0 ? best : configuredDelimiter;
        }
        catch
        {
            return configuredDelimiter;
        }
    }

    private Encoding ResolveEncoding(byte[] content, string? preferredEncodingName)
    {
        if (!string.IsNullOrWhiteSpace(preferredEncodingName))
        {
            try { return Encoding.GetEncoding(preferredEncodingName); } catch { }
        }

        try
        {
            var utf8 = new UTF8Encoding(false, false);
            var s = utf8.GetString(content);
            if (!s.Contains('\uFFFD')) return Encoding.UTF8;
        }
        catch { }

        try { return Encoding.GetEncoding(1252); } catch { }
        return Encoding.UTF8;
    }

    // --- Neue/erweiterte Logik aus der Server-Variante (angepasst für WASM / kein CsvHelper) ---

    private (Encoding encoding, bool detectBom) ResolveEncodingForReader(byte[] content, string? preferredEncodingName)
    {
        if (!string.IsNullOrWhiteSpace(preferredEncodingName))
        {
            var pe = preferredEncodingName.Trim().ToLowerInvariant();
            try
            {
                if (pe.Contains("utf") && pe.Contains("bom"))
                    return (new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), true);
                if (pe == "utf-8" || pe == "utf8" || (pe.Contains("utf") && (pe.Contains("no") || pe.Contains("nobom") || pe.Contains("no-bom"))))
                    return (new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), true);
                return (Encoding.GetEncoding(preferredEncodingName), false);
            }
            catch { /* fall through to autodetect */ }
        }

        var enc = DetectEncoding(content, null);
        return (enc, true);
    }

    private Encoding DetectEncoding(byte[] content, string? preferredEncodingName)
    {
        if (!string.IsNullOrWhiteSpace(preferredEncodingName))
        {
            try
            {
                var pe = preferredEncodingName.Trim().ToLowerInvariant();
                if (pe.Contains("utf") && pe.Contains("bom"))
                    return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                if (pe == "utf-8" || pe == "utf8" || (pe.Contains("utf") && (pe.Contains("no") || pe.Contains("nobom") || pe.Contains("no-bom"))))
                    return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                return Encoding.GetEncoding(preferredEncodingName);
            }
            catch { }
        }

        try
        {
            var utf8 = new UTF8Encoding(false, false);
            var t = utf8.GetString(content);
            if (!t.Contains('\uFFFD')) return Encoding.UTF8;
        }
        catch { }

        try
        {
            var win1252 = Encoding.GetEncoding(1252);
            var t = win1252.GetString(content);
            if (!t.Contains('\uFFFD')) return win1252;
            return win1252;
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    private string[] SplitRow(string line, string delimiter, char? quote)
    {
        if (!quote.HasValue)
            return line.Split(new string[] { delimiter }, StringSplitOptions.None);

        var parts = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        char q = quote.Value;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == q)
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && line.Substring(i).StartsWith(delimiter))
            {
                parts.Add(sb.ToString());
                sb.Clear();
                i += delimiter.Length - 1;
                continue;
            }

            sb.Append(c);
        }
        parts.Add(sb.ToString());
        return parts.ToArray();
    }

    private int CountCsvDataRows(byte[] content, RequiredFile file)
    {
        try
        {
            var configuredDelimiter = file.Delimiter ?? "\t";
            var delimiter = DetermineDelimiter(content, configuredDelimiter);
            var quote = string.IsNullOrEmpty(file.Quote) ? (char?)null : file.Quote[0];
            var (enc, _) = ResolveEncodingForReader(content, file.Encoding);

            var text = enc.GetString(content);
            var allLines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

            if (file.HasHeader)
            {
                // skip first non-empty line as header
                int headerIdx = allLines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
                if (headerIdx < 0) return 0;
                return allLines.Skip(headerIdx + 1).Count(l => !string.IsNullOrWhiteSpace(l));
            }
            else
            {
                return allLines.Count(l => !string.IsNullOrWhiteSpace(l));
            }
        }
        catch
        {
            return AnalyzeFile(content, file.Delimiter ?? "\t", file.Quote ?? string.Empty, file.Encoding ?? "utf-8").Lines;
        }
    }

    private string BuildPersonKeyFromArray(string[] arr)
    {
        try
        {
            var n = arr.Length > 0 ? arr[0].Trim() : string.Empty;
            var v = arr.Length > 1 ? arr[1].Trim() : string.Empty;
            string? g = null;
            for (int i = 2; i < Math.Min(arr.Length, 6); i++)
            {
                if (!string.IsNullOrWhiteSpace(arr[i]) && arr[i].Any(char.IsDigit)) { g = arr[i].Trim(); break; }
            }
            g ??= arr.Length > 2 ? arr[2].Trim() : string.Empty;
            return (n + "|" + v + "|" + g).Trim('|', ' ');
        }
        catch { return string.Empty; }
    }

    private static string NormalizeNameForEmail(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim();
        // Auflösen deutscher Umlaute in passende ASCII-Form
        s = s.Replace("Ä", "Ae").Replace("ä", "ae").Replace("Ö", "Oe").Replace("ö", "oe").Replace("Ü", "Ue").Replace("ü", "ue").Replace("ß", "ss");
        // Entferne diakritische Marken
        s = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    // Helper to escape values for CSV output
    private static string EscapeCsv(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;
        // If value contains special CSV characters, wrap in quotes and escape internal quotes
        if (input.Contains('"') || input.Contains(',') || input.Contains('\n') || input.Contains('\r'))
            return '"' + input.Replace("\"", "\"\"") + '"';
        return input;
    }

    private string BuildPersonKeyFromRecord(IDictionary<string, object> dict, string[] headers)
    {
        try
        {
            int nach = -1, vor = -1, geb = -1;
            for (int i = 0; i < headers.Length; i++)
            {
                var h = headers[i].ToLowerInvariant();
                if (nach == -1 && (h.Contains("nachname") || (h.Contains("name") && !h.Contains("vor")))) nach = i;
                if (vor == -1 && (h.Contains("vorname") || h.Contains("vor"))) vor = i;
                if (geb == -1 && (h.Contains("geb") || h.Contains("birth") || h.Contains("geburts"))) geb = i;
            }

            var vals = dict.Values.ToArray();
            var n = nach >= 0 && vals.Length > nach ? (vals[nach]?.ToString() ?? string.Empty).Trim() : string.Empty;
            var vname = vor >= 0 && vals.Length > vor ? (vals[vor]?.ToString() ?? string.Empty).Trim() : string.Empty;
            var g = geb >= 0 && vals.Length > geb ? (vals[geb]?.ToString() ?? string.Empty).Trim() : string.Empty;
            return (n + "|" + vname + "|" + g).Trim('|', ' ');
        }
        catch { return string.Empty; }
    }

    private byte[] FilterOnlyChangedRows(byte[] original, byte[] modified, string delimiter, char? quote, string? preferredEncodingName, bool hasHeader)
    {
        var (origEnc, _) = ResolveEncodingForReader(original, preferredEncodingName);
        var (modEnc, _) = ResolveEncodingForReader(modified, preferredEncodingName);

        var origText = origEnc.GetString(original);
        var modText = modEnc.GetString(modified);

        var origLines = origText.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var modLines = modText.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        string[]? origHeaders = null;
        int origDataStart = 0;
        if (hasHeader)
        {
            origHeaders = origLines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Split(new string[] { delimiter }, StringSplitOptions.None) ?? Array.Empty<string>();
            origDataStart = origLines.FindIndex(l => !string.IsNullOrWhiteSpace(l)) + 1;
        }

        var origMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = origDataStart; i < origLines.Count; i++)
        {
            var line = origLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = SplitRow(line, delimiter, quote);
            var key = BuildPersonKeyFromArray(fields);
            var norm = string.Join("\u0001", fields.Select(f => (f ?? string.Empty).Trim()));
            if (!string.IsNullOrWhiteSpace(key)) origMap[key] = norm;
        }

        // Build list of changed rows from modified
        string[]? modHeaders = null;
        int modDataStart = 0;
        var rowsToWrite = new List<string[]>();
        if (hasHeader)
        {
            modHeaders = modLines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Split(new string[] { delimiter }, StringSplitOptions.None) ?? Array.Empty<string>();
            modDataStart = modLines.FindIndex(l => !string.IsNullOrWhiteSpace(l)) + 1;
        }

        for (int i = modDataStart; i < modLines.Count; i++)
        {
            var line = modLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = SplitRow(line, delimiter, quote);
            var key = BuildPersonKeyFromArray(fields);
            if (string.IsNullOrWhiteSpace(key)) continue;
            var norm = string.Join("\u0001", fields.Select(f => (f ?? string.Empty).Trim()));
            if (!origMap.TryGetValue(key, out var origNorm) || !string.Equals(origNorm, norm, StringComparison.Ordinal))
            {
                rowsToWrite.Add(fields);
            }
        }

        // Write output bytes
        using var outMs = new System.IO.MemoryStream();
        var writeEnc = (origEnc != null && origEnc.CodePage == Encoding.UTF8.CodePage) ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true) : origEnc ?? Encoding.UTF8;
        using (var sw = new System.IO.StreamWriter(outMs, writeEnc, leaveOpen: true))
        {
            if (hasHeader && modHeaders != null)
            {
                sw.WriteLine(string.Join(delimiter, modHeaders));
            }
            foreach (var row in rowsToWrite)
            {
                sw.WriteLine(string.Join(delimiter, row));
            }
            sw.Flush();
            outMs.Position = 0;
            return outMs.ToArray();
        }
    }

    // Processing methods moved to separate partial files to reduce file size.

    public async Task<ProcessingResult> PreviewMailadresseSetzen(List<RequiredFile> files, Dictionary<string, string> inputs)
    {
        await Task.Yield();
        var result = new ProcessingResult { Success = true };
        try
        {
            var basis = files.FirstOrDefault(f => f.FileKey == "basisdaten");
            var zusatz = files.FirstOrDefault(f => f.FileKey == "zusatzdaten");

            if (basis?.Content == null || zusatz?.Content == null)
                return new ProcessingResult { Success = false, Message = "Beide Dateien (Basis- und Zusatzdaten) werden benötigt. Bitte laden Sie insbesondere die Datei 'SchuelerZusatzdaten.dat' (Zusatzdaten) hoch, da diese die schulischen E-Mail-Adressen enthält." };

            var domain = inputs.TryGetValue("mailDomain", out var d) ? d.Trim() : "";
            if (string.IsNullOrWhiteSpace(domain))
                return new ProcessingResult { Success = false, Message = "Mail-Domain fehlt." };

            var configuredDelimiter = zusatz.Delimiter ?? "\t";
            var delimiter = DetermineDelimiter(zusatz.Content, configuredDelimiter);
            var quote = string.IsNullOrEmpty(zusatz.Quote) ? (char?)null : zusatz.Quote[0];
            var (enc, _) = ResolveEncodingForReader(zusatz.Content, zusatz.Encoding);
            var text = enc.GetString(zusatz.Content);
            var allLines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

            int headerIdx = -1;
            string[]? headers = null;
            var records = new List<string[]>();

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
                        records.Add(SplitRow(line, delimiter, quote));
                    }
                }
            }
            else
            {
                foreach (var line in allLines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    records.Add(SplitRow(line, delimiter, quote));
                }
            }

            var preview = new StringBuilder();
            preview.AppendLine("<p>Vorschau der zu verarbeitenden Daten:</p>");
            preview.AppendLine("<table class='table table-sm table-bordered'><thead><tr>");
            if (headers != null)
            {
                foreach (var h in headers) preview.AppendLine($"<th>{System.Net.WebUtility.HtmlEncode(h)}</th>");
            }
            else
            {
                preview.AppendLine("<th>#</th>");
            }
            preview.AppendLine("</tr></thead><tbody>");
            foreach (var rec in records.Take(100))
            {
                preview.AppendLine("<tr>");
                for (int c = 0; c < (headers?.Length ?? rec.Length); c++)
                {
                    preview.AppendLine($"<td>{(c < rec.Length ? System.Net.WebUtility.HtmlEncode(rec[c]) : "")}</td>");
                }
                preview.AppendLine("</tr>");
            }
            preview.AppendLine("</tbody></table>");
            result.Message = "Vorschau erstellt.";
            result.MessageHtml = preview.ToString();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Fehler bei der Vorschau-Erstellung: {ex.Message}";
        }
        return result;
    }
}