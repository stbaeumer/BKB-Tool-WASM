using BKBToolClient.Models;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Linq;

namespace BKBToolClient.Services;

/// <summary>
/// Extension Methods für Lernabschnittsdaten Verarbeitung
/// </summary>
public static class LernabschnittsdatenExtensions
{
    /// <summary>
    /// Berechnet die Fehlstunden für einen Schüler basierend auf der Absence-Liste.
    /// Fehlstunden über der maximalen Anzahl pro Tag werden genullt.
    /// Fehlzeiten, die weniger als X Tage in der Vergangenheit liegen, werden ignoriert.
    /// </summary>
    public static string GetFehlstd(this Student student, List<dynamic>? absencesPerStudent, Dictionary<string, string> inputs)
    {
        try
        {
            if (absencesPerStudent == null || absencesPerStudent.Count == 0)
                return "";

            var fehlzeitenWaehrendDerLetztenTagBleibenUnberuecksichtigt = 
                int.TryParse(inputs.GetValueOrDefault("fehlzeitenWaehrendDerLetztenTagBleibenUnberuecksichtigt"), out var days) 
                ? days 
                : 0;
            
            var maximaleFehlstunden = 
                int.TryParse(inputs.GetValueOrDefault("maximaleFehlstundenProTag"), out var maxStd) 
                ? maxStd 
                : 6;

            var fehlstd = 0;

            foreach (var record in absencesPerStudent)
            {
                var dict = (IDictionary<string, object>)record;

                // Schüler-Name und Vorname matchen
                if (!dict.ContainsKey("Schüler*innen") || dict["Schüler*innen"] == null ||
                    string.IsNullOrEmpty(dict["Schüler*innen"].ToString()))
                    continue;

                var schuelerInfo = dict["Schüler*innen"].ToString() ?? "";
                if (!schuelerInfo.Contains(student.Nachname ?? ""))
                    continue;
                if (!schuelerInfo.Contains(student.Vorname ?? ""))
                    continue;

                // Klasse matchen
                if (!dict.ContainsKey("Klasse") || dict["Klasse"] == null ||
                    string.IsNullOrEmpty(dict["Klasse"].ToString()) ||
                    !dict["Klasse"].ToString()!.Contains(student.Klasse ?? ""))
                    continue;

                // Fehlstunden auslesen
                if (!dict.ContainsKey("Fehlstd.") || dict["Fehlstd."] == null ||
                    string.IsNullOrEmpty(dict["Fehlstd."].ToString()) ||
                    !int.TryParse(dict["Fehlstd."].ToString(), out var fehlstdValue))
                    continue;

                // Datum prüfen: Fehlzeiten in den letzten X Tagen ignorieren
                if (!dict.ContainsKey("Datum") || dict["Datum"] == null ||
                    !DateTime.TryParseExact(dict["Datum"].ToString()!, "dd.MM.yy", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var absenceDate))
                    continue;

                if (absenceDate.AddDays(fehlzeitenWaehrendDerLetztenTagBleibenUnberuecksichtigt) >= DateTime.Now)
                    continue;

                // Webuntis zählt bei ganztägigen Veranstaltungen 24 Fehlstunden.
                // Weil das Fehlen außerhalb von Unterricht nicht auf das Zeugnis kommt, wird es genullt.
                var webuntisFehlst = fehlstdValue;
                
                if (webuntisFehlst > maximaleFehlstunden)
                {
                    // Zu viele Fehlstunden an diesem Tag - genullt
                    continue;
                }

                fehlstd += Math.Min(maximaleFehlstunden, webuntisFehlst);
            }

            return fehlstd == 0 ? "" : fehlstd.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler in GetFehlstd: {ex}");
            return "";
        }
    }

    /// <summary>
    /// Berechnet die unentschuldigten Fehlstunden für einen Schüler basierend auf der Absence-Liste.
    /// Fehlstunden über der maximalen Anzahl pro Tag werden genullt.
    /// Fehlzeiten, die weniger als X Tage in der Vergangenheit liegen, werden ignoriert.
    /// </summary>
    public static string GetUnentFehlstd(this Student student, List<dynamic>? absencesPerStudent, Dictionary<string, string> inputs)
    {
        try
        {
            if (absencesPerStudent == null || absencesPerStudent.Count == 0)
                return "";

            var fehlzeitenWaehrendDerLetztenTagBleibenUnberuecksichtigt = 
                int.TryParse(inputs.GetValueOrDefault("fehlzeitenWaehrendDerLetztenTagBleibenUnberuecksichtigt"), out var days) 
                ? days 
                : 0;

            var maximaleFehlstunden = 
                int.TryParse(inputs.GetValueOrDefault("maximaleFehlstundenProTag"), out var maxStd) 
                ? maxStd 
                : 6;

            var fehlstd = 0;

            foreach (var record in absencesPerStudent)
            {
                var dict = (IDictionary<string, object>)record;

                // Schüler-Name und Vorname matchen
                if (!dict.ContainsKey("Schüler*innen") || dict["Schüler*innen"] == null ||
                    string.IsNullOrEmpty(dict["Schüler*innen"].ToString()))
                    continue;

                var schuelerInfo = dict["Schüler*innen"].ToString() ?? "";
                if (!schuelerInfo.Contains(student.Nachname ?? ""))
                    continue;
                if (!schuelerInfo.Contains(student.Vorname ?? ""))
                    continue;

                // Klasse matchen
                if (!dict.ContainsKey("Klasse") || dict["Klasse"] == null ||
                    string.IsNullOrEmpty(dict["Klasse"].ToString()) ||
                    !dict["Klasse"].ToString()!.Contains(student.Klasse ?? ""))
                    continue;

                // Status muss "nicht entsch." enthalten
                if (!dict.ContainsKey("Status") || dict["Status"] == null ||
                    string.IsNullOrEmpty(dict["Status"].ToString()) ||
                    !dict["Status"].ToString()!.Contains("nicht entsch."))
                    continue;

                // Fehlstunden auslesen
                if (!dict.ContainsKey("Fehlstd.") || dict["Fehlstd."] == null ||
                    string.IsNullOrEmpty(dict["Fehlstd."].ToString()) ||
                    !int.TryParse(dict["Fehlstd."].ToString(), out var fehlstdValue))
                    continue;

                // Datum prüfen: Fehlzeiten in den letzten X Tagen ignorieren
                if (!dict.ContainsKey("Datum") || dict["Datum"] == null ||
                    !DateTime.TryParseExact(dict["Datum"].ToString()!, "dd.MM.yy", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var absenceDate))
                    continue;

                if (absenceDate.AddDays(fehlzeitenWaehrendDerLetztenTagBleibenUnberuecksichtigt) >= DateTime.Now)
                    continue;

                // Webuntis zählt bei ganztägigen Veranstaltungen 24 Fehlstunden.
                // Weil das Fehlen außerhalb von Unterricht nicht auf das Zeugnis kommt, wird es genullt.
                var webuntisFehlst = fehlstdValue;

                if (webuntisFehlst > maximaleFehlstunden)
                {
                    // Zu viele Fehlstunden an diesem Tag - genullt
                    continue;
                }
                else
                {
                    fehlstd += webuntisFehlst;
                }
            }

            return fehlstd == 0 ? "" : fehlstd.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fehler in GetUnentFehlstd: {ex}");
            return "";
        }
    }
}
