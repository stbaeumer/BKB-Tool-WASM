using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BKBToolClient.Models;

namespace BKBToolClient.Services;

public partial class FileProcessingService
{
    /// <summary>
    /// Erzeugt Kurslisten und Schülerzuordnungen aus kurse.dat, GPU002.txt und studentgroupstudents.csv
    /// </summary>
    public async Task<ProcessingResult> ProcessKurse(List<RequiredFile> files, Dictionary<string, string> inputs, FunctionConfig? config = null)
    {
        await Task.Yield();
        var result = new ProcessingResult { Success = false };

        try
        {
            var kurseFile = files.FirstOrDefault(f => f.FileKey == "kurse");
            var gpuFile = files.FirstOrDefault(f => f.FileKey == "gpu002");
            var basisFile = files.FirstOrDefault(f => f.FileKey == "basisdaten");
            var zusatzFile = files.FirstOrDefault(f => f.FileKey == "zusatzdaten");
            var sgsFile = files.FirstOrDefault(f => f.FileKey == "studentgroupstudents");

            if (kurseFile?.Content == null || gpuFile?.Content == null || basisFile?.Content == null ||
                zusatzFile?.Content == null || sgsFile?.Content == null)
            {
                result.Message = "Benötigt: kurse.dat, GPU002.txt, SchuelerBasisdaten, SchuelerZusatzdaten und studentgroupstudents.csv.";
                return result;
            }

            // Parse helper
            (string[] Header, List<string[]> Rows) ParseWithOptionalHeader(RequiredFile file, bool hasHeaderOverride)
            {
                var delimiter = DetermineDelimiter(file.Content!, file.Delimiter ?? "\t");
                var quote = string.IsNullOrEmpty(file.Quote) ? (char?)null : file.Quote[0];
                var (enc, _) = ResolveEncodingForReader(file.Content!, file.Encoding);
                var text = enc.GetString(file.Content!);
                if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF') text = text[1..];
                var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                if (lines.Count == 0) return (Array.Empty<string>(), new List<string[]>());

                var rows = new List<string[]>();
                if (hasHeaderOverride)
                {
                    var header = SplitRow(lines[0], delimiter, quote);
                    for (int i = 1; i < lines.Count; i++) rows.Add(SplitRow(lines[i], delimiter, quote));
                    return (header, rows);
                }

                // No header: compute max column count and synthesize header
                var maxCols = 0;
                foreach (var line in lines)
                {
                    var parts = SplitRow(line, delimiter, quote);
                    maxCols = Math.Max(maxCols, parts.Length);
                    rows.Add(parts);
                }
                var hdr = Enumerable.Range(1, maxCols).Select(i => $"Col{i}").ToArray();
                return (hdr, rows);
            }

            byte[] WriteCsv(string[] header, List<string[]> rows)
            {
                var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                using var ms = new MemoryStream();
                using var sw = new StreamWriter(ms, enc, leaveOpen: true);
                if (header.Length > 0) sw.WriteLine(string.Join(';', header.Select(EscapeCsv)));
                foreach (var r in rows)
                {
                    var line = new List<string>();
                    for (int i = 0; i < header.Length; i++)
                    {
                        var val = i < r.Length ? r[i] : string.Empty;
                        line.Add(EscapeCsv(val));
                    }
                    sw.WriteLine(string.Join(';', line));
                }
                sw.Flush();
                ms.Position = 0;
                return ms.ToArray();
            }

            // Parse inputs
            var (kurseHeader, kurseRows) = ParseWithOptionalHeader(kurseFile, true);
            var (gpuHeader, gpuRows) = ParseWithOptionalHeader(gpuFile, false);
            var (sgsHeader, sgsRows) = ParseWithOptionalHeader(sgsFile, true);

            // Build students from Basis + Zusatzdaten
            List<Dictionary<string, string>> ParseRecords(RequiredFile file, bool keepRowIndex)
            {
                var list = new List<Dictionary<string, string>>();
                if (file.Content == null) return list;
                var delimiter = DetermineDelimiter(file.Content, file.Delimiter ?? "\t");
                var (enc, _) = ResolveEncodingForReader(file.Content, file.Encoding);
                var text = enc.GetString(file.Content);
                if (!string.IsNullOrEmpty(text) && text[0] == '\uFEFF') text = text[1..];
                var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                if (lines.Count == 0) return list;
                var header = SplitRow(lines[0], delimiter, null);
                for (int i = 1; i < lines.Count; i++)
                {
                    var fields = SplitRow(lines[i], delimiter, null);
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int c = 0; c < header.Length; c++) dict[header[c].Trim()] = c < fields.Length ? fields[c] : string.Empty;
                    if (keepRowIndex) dict["__rowIndex"] = (i - 1).ToString();
                    list.Add(dict);
                }
                return list;
            }

            var basisRecords = ParseRecords(basisFile, true);
            var zusatzRecords = ParseRecords(zusatzFile, true);
            var students = BuildTypedStudents(basisRecords, zusatzRecords, new List<Dictionary<string, string>>(), new List<Dictionary<string, string>>());

            Models.Bildungsgang? PickBestBg(Models.Student s)
            {
                return s.Bildungsgaenge
                    .OrderBy(bg => bg.Status != "2") // bevorzugt aktiv
                    .ThenBy(bg => bg.Status != "6") // dann extern
                    .ThenBy(bg => bg.Status != "8")
                    .ThenBy(bg => bg.Status != "9")
                    .FirstOrDefault();
            }

            // Build student assignments from studentgroupstudents.csv
            var assignmentHeader = new[] { "studentId", "Nachname", "Vorname", "Geburtsdatum", "Klasse", "Status", "ExterneId", "Gruppe", "Fach", "Start", "Ende" };
            var assignmentRows = new List<string[]>();
            foreach (var row in sgsRows)
            {
                string GetVal(params string[] keys)
                {
                    foreach (var k in keys)
                    {
                        var idx = Array.FindIndex(sgsHeader, h => h.Equals(k, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0 && idx < row.Length) return row[idx];
                    }
                    return string.Empty;
                }

                var sid = GetVal("studentId");
                var lname = GetVal("name", "Nachname", "lastName");
                var fname = GetVal("forename", "Vorname", "firstName");
                var group = GetVal("studentgroup.name", "studentgroup", "Gruppe");
                var subject = GetVal("subject", "Fach");
                var start = GetVal("startDate", "start");
                var end = GetVal("endDate", "end");

                var match = students.FirstOrDefault(s =>
                    string.Equals(s.Nachname?.Trim() ?? string.Empty, lname.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.Vorname?.Trim() ?? string.Empty, fname.Trim(), StringComparison.OrdinalIgnoreCase));

                var bestBg = match != null ? PickBestBg(match) : null;
                var klasse = bestBg?.Klasse ?? match?.Klasse ?? string.Empty;
                var status = bestBg?.Status ?? match?.Status ?? string.Empty;
                var externeId = match?.AdditionalFields.GetValueOrDefault("Externe ID-Nr")
                    ?? match?.AdditionalFields.GetValueOrDefault("ExterneID")
                    ?? match?.AdditionalFields.GetValueOrDefault("ExterneId")
                    ?? string.Empty;

                assignmentRows.Add(new[]
                {
                    sid,
                    lname,
                    fname,
                    match?.Geburtsdatum ?? string.Empty,
                    klasse,
                    status,
                    externeId,
                    group,
                    subject,
                    start,
                    end
                });
            }

            // Build output CSVs
            var kurseBytes = WriteCsv(kurseHeader, kurseRows);
            var gpuBytes = WriteCsv(gpuHeader, gpuRows);
            var assignBytes = WriteCsv(assignmentHeader, assignmentRows);

            // Store for potential later retrieval
            try
            {
                StoreFile("kurse_export", kurseBytes);
                StoreFile("kurse_gpu002", gpuBytes);
                StoreFile("kurse_studentzuordnungen", assignBytes);
            }
            catch { }

            result.Success = true;
            result.Message = $"Kurse: {kurseRows.Count}, Zuordnungen: {assignmentRows.Count}, GPU002-Zeilen: {gpuRows.Count}";
            result.OutputFiles = new List<OutputFile>
            {
                new OutputFile
                {
                    FileName = config?.OutputFiles.FirstOrDefault(o => o.FileKey == "kurse_export")?.FileName ?? "Kurse.csv",
                    Content = kurseBytes,
                    FileSize = kurseBytes.LongLength,
                    LineCount = Math.Max(0, kurseRows.Count),
                    Hint = config?.OutputFiles.FirstOrDefault(o => o.FileKey == "kurse_export")?.Hint,
                    ProcessingHint = config?.OutputFiles.FirstOrDefault(o => o.FileKey == "kurse_export")?.ProcessingHint
                },
                new OutputFile
                {
                    FileName = config?.OutputFiles.FirstOrDefault(o => o.FileKey == "kurse_studentzuordnungen")?.FileName ?? "Kurse_Studentzuordnungen.csv",
                    Content = assignBytes,
                    FileSize = assignBytes.LongLength,
                    LineCount = Math.Max(0, assignmentRows.Count),
                    Hint = config?.OutputFiles.FirstOrDefault(o => o.FileKey == "kurse_studentzuordnungen")?.Hint,
                    ProcessingHint = config?.OutputFiles.FirstOrDefault(o => o.FileKey == "kurse_studentzuordnungen")?.ProcessingHint
                },
                new OutputFile
                {
                    FileName = config?.OutputFiles.FirstOrDefault(o => o.FileKey == "kurse_gpu002")?.FileName ?? "GPU002_Parsed.csv",
                    Content = gpuBytes,
                    FileSize = gpuBytes.LongLength,
                    LineCount = Math.Max(0, gpuRows.Count),
                    Hint = config?.OutputFiles.FirstOrDefault(o => o.FileKey == "kurse_gpu002")?.Hint,
                    ProcessingHint = config?.OutputFiles.FirstOrDefault(o => o.FileKey == "kurse_gpu002")?.ProcessingHint
                }
            };
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Fehler bei der Kurserstellung: {ex.Message}";
        }

        return result;
    }
}
