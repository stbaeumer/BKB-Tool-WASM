using System.Text;
using System.Globalization;
using BKBToolClient.Models;

namespace BKBToolClient.Services;

public partial class FileProcessingService
{
    public async Task<ProcessingResult> ProcessLernabschnittsdaten(List<RequiredFile> files, Dictionary<string, string> inputs, FunctionConfig config)
    {
        await Task.Yield();
        var result = new ProcessingResult { Success = false };

        var lernFile = files.FirstOrDefault(f => f.FileKey == "lernabschnittsdaten");
        var absenceFile = files.FirstOrDefault(f => f.FileKey == "absence");

        if (lernFile?.Content == null || absenceFile?.Content == null)
        {
            result.Message = "Benötigt: SchuelerLernabschnitssdaten und AbsencePerStudent.";
            return result;
        }

        // Parse AbsencePerStudent (tab-delimited)
        var absDelimiter = DetermineDelimiter(absenceFile.Content, absenceFile.Delimiter ?? "\t");
        var absQuote = string.IsNullOrEmpty(absenceFile.Quote) ? (char?)null : absenceFile.Quote[0];
        var (absEnc, _) = ResolveEncodingForReader(absenceFile.Content, absenceFile.Encoding);
        var absLines = absEnc.GetString(absenceFile.Content).Split('\n').Select(l => l.TrimEnd('\r')).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (absLines.Count == 0)
        {
            result.Message = "AbsencePerStudent enthält keine Daten.";
            return result;
        }
        var absHeader = SplitRow(absLines[0], absDelimiter, absQuote);
        var absRecords = new List<Dictionary<string, object>>();
        for (int i = 1; i < absLines.Count; i++)
        {
            var fields = SplitRow(absLines[i], absDelimiter, absQuote);
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (int c = 0; c < absHeader.Length && c < fields.Length; c++)
            {
                dict[absHeader[c]] = fields[c];
            }
            absRecords.Add(dict);
        }

        // Parse Lernabschnittsdaten
        var lernDelimiter = DetermineDelimiter(lernFile.Content, lernFile.Delimiter ?? "\t");
        var lernQuote = string.IsNullOrEmpty(lernFile.Quote) ? (char?)null : lernFile.Quote[0];
        var (lernEnc, _) = ResolveEncodingForReader(lernFile.Content, lernFile.Encoding);
        var lernLines = lernEnc.GetString(lernFile.Content).Split('\n').Select(l => l.TrimEnd('\r')).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (lernLines.Count == 0)
        {
            result.Message = "SchuelerLernabschnitssdaten enthalten keine Daten.";
            return result;
        }
        var header = SplitRow(lernLines[0], lernDelimiter, lernQuote);
        int idxNach = Array.FindIndex(header, h => h.Equals("Nachname", StringComparison.OrdinalIgnoreCase));
        int idxVor = Array.FindIndex(header, h => h.Equals("Vorname", StringComparison.OrdinalIgnoreCase));
        int idxKlasse = Array.FindIndex(header, h => h.Equals("Klasse", StringComparison.OrdinalIgnoreCase));
        int idxSum = Array.FindIndex(header, h => h.Equals("SummeFehlstd", StringComparison.OrdinalIgnoreCase));
        int idxSumUnent = Array.FindIndex(header, h => h.Equals("SummeFehlstd_unentschuldigt", StringComparison.OrdinalIgnoreCase));
        if (idxNach < 0 || idxVor < 0 || idxKlasse < 0 || idxSum < 0 || idxSumUnent < 0)
        {
            result.Message = "Spalten nicht gefunden: Nachname, Vorname, Klasse, SummeFehlstd, SummeFehlstd_unentschuldigt.";
            return result;
        }

        var changes = new List<(string Name, string Klasse, string AltSum, string NeuSum, string AltUnent, string NeuUnent)>();
        var totalRows = Math.Max(lernLines.Count - 1, 0);
        var changedRows = 0;
        var newFehlzeitRows = 0;
        var outputLines = new List<string> { lernLines[0] };
        var absDyn = absRecords.Cast<dynamic>().ToList();

        for (int i = 1; i < lernLines.Count; i++)
        {
            var fields = SplitRow(lernLines[i], lernDelimiter, lernQuote);
            if (fields.Length < header.Length)
            {
                // pad missing columns
                Array.Resize(ref fields, header.Length);
            }

            var origNach = fields[idxNach] ?? string.Empty;
            var origVor = fields[idxVor] ?? string.Empty;
            var klasse = fields[idxKlasse] ?? string.Empty;
            var altSum = fields[idxSum] ?? string.Empty;
            var altUnent = fields[idxSumUnent] ?? string.Empty;

            var student = new Student
            {
                Nachname = origNach,
                Vorname = origVor,
                Klasse = klasse
            };

            var neuSumRaw = student.GetFehlstd(absDyn, inputs);
            var neuUnentRaw = student.GetUnentFehlstd(absDyn, inputs);
            var neuSum = string.IsNullOrWhiteSpace(neuSumRaw) ? "0" : neuSumRaw.Trim();
            var neuUnent = string.IsNullOrWhiteSpace(neuUnentRaw) ? "0" : neuUnentRaw.Trim();

            bool changed = !string.Equals(altSum?.Trim() ?? string.Empty, neuSum, StringComparison.Ordinal) ||
                           !string.Equals(altUnent?.Trim() ?? string.Empty, neuUnent, StringComparison.Ordinal);

            var hadSumBefore = !string.IsNullOrWhiteSpace(altSum?.Trim()) && altSum.Trim() != "0";
            var hasSumNow = !string.IsNullOrWhiteSpace(neuSum) && neuSum != "0";
            var hadUnentBefore = !string.IsNullOrWhiteSpace(altUnent?.Trim()) && altUnent.Trim() != "0";
            var hasUnentNow = !string.IsNullOrWhiteSpace(neuUnent) && neuUnent != "0";
            var isNewFehlzeit = (!hadSumBefore && hasSumNow) || (!hadUnentBefore && hasUnentNow);

            fields[idxSum] = neuSum;
            fields[idxSumUnent] = neuUnent;
            fields[idxNach] = $"{origNach}#{klasse}";

            if (changed)
            {
                changedRows++;
            }

            if (isNewFehlzeit)
            {
                newFehlzeitRows++;
            }

            if (changed && changes.Count < 10)
            {
                var displayName = $"{origNach} {origVor} ({klasse})";
                changes.Add((displayName, klasse, altSum ?? string.Empty, neuSum, altUnent ?? string.Empty, neuUnent));
            }

            outputLines.Add(string.Join(lernDelimiter, fields));
        }

        var outputBytes = lernEnc.GetBytes(string.Join("\n", outputLines));
        var outCfg = config.OutputFiles.FirstOrDefault(of => of.FileKey == "lernabschnittsdaten_modified");
        var outName = outCfg?.FileName ?? "SchuelerLernabschnitssdaten.dat";

        result.Success = true;
        result.OutputFiles = new List<OutputFile>
        {
            new OutputFile
            {
                FileName = outName,
                Content = outputBytes,
                FileSize = outputBytes.LongLength,
                LineCount = outputLines.Count - 1,
                Hint = outCfg?.Hint,
                ProcessingHint = outCfg?.ProcessingHint
            }
        };

        var summary = new StringBuilder();
        summary.Append("<div class=\"mb-2\"><strong>Statistik:</strong>")
               .Append($"<div>Zeilen gesamt: {totalRows}</div>")
               .Append($"<div>Zeilen mit neuen Fehlzeiten: {newFehlzeitRows}</div>")
               .Append($"<div>Zeilen mit geänderten Fehlzeiten: {changedRows}</div>")
               .Append("</div>");

        if (changes.Count == 0)
        {
            summary.Append("<div><strong>Änderungen:</strong> Keine Änderungen erforderlich.</div>");
            result.Message = "Keine Änderungen erforderlich.";
            result.MessageHtml = summary.ToString();
        }
        else
        {
            summary.Append("<div><strong>Änderungen (max. 10):</strong><ul>");
            foreach (var c in changes)
            {
                summary.Append("<li>")
                  .Append(System.Net.WebUtility.HtmlEncode(c.Name))
                  .Append(": Fehlstd ")
                  .Append(System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(c.AltSum) ? "(leer)" : c.AltSum))
                  .Append(" → ")
                  .Append(System.Net.WebUtility.HtmlEncode(c.NeuSum))
                  .Append(", unentsch. ")
                  .Append(System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(c.AltUnent) ? "(leer)" : c.AltUnent))
                  .Append(" → ")
                  .Append(System.Net.WebUtility.HtmlEncode(c.NeuUnent))
                  .Append("</li>");
            }
            if (lernLines.Count - 1 > changes.Count)
            {
                summary.Append("<li class=\"text-muted\">Weitere Änderungen wurden übernommen.</li>");
            }
            summary.Append("</ul></div>");
            result.Message = "Fehlzeiten eingetragen.";
            result.MessageHtml = summary.ToString();
        }

        return result;
    }
}
