using System;
using System.Collections.Generic;

namespace BKBToolClient.Models
{
    public class Student
    {
        public string Nachname { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Geburtsdatum { get; set; } = string.Empty;
        public DateTime? GeburtsdatumParsed { get; set; }
        public bool Volljaehrig { get; set; }
        // Bildungsgaenge contains the status/klasse/etc. information per occurrence in basisdata
        public List<Bildungsgang> Bildungsgaenge { get; set; } = new List<Bildungsgang>();

        // Backwards-compatible properties: map to the first Bildungsgang when available.
        public string Status
        {
            get => Bildungsgaenge.FirstOrDefault()?.Status ?? string.Empty;
            set
            {
                if (Bildungsgaenge.Count == 0) Bildungsgaenge.Add(new Bildungsgang());
                Bildungsgaenge[0].Status = value ?? string.Empty;
            }
        }

        public string Klasse
        {
            get => Bildungsgaenge.FirstOrDefault()?.Klasse ?? string.Empty;
            set
            {
                if (Bildungsgaenge.Count == 0) Bildungsgaenge.Add(new Bildungsgang());
                Bildungsgaenge[0].Klasse = value ?? string.Empty;
            }
        }
        public string Strasse { get; set; } = string.Empty;
        public string PLZ { get; set; } = string.Empty;
        public string Ort { get; set; } = string.Empty;
        public string MailSchulisch { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Mobil { get; set; } = string.Empty;
        public string Geschlecht { get; set; } = string.Empty;

        public List<Adresse> Adresses { get; set; } = new List<Adresse>();
        public List<Erzieher> Erziehers { get; set; } = new List<Erzieher>();

        // store any additional raw fields
        public Dictionary<string, string> AdditionalFields { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class Bildungsgang
    {
        public string Status { get; set; } = string.Empty;
        public string Jahrgang { get; set; } = string.Empty;
        public string Klasse { get; set; } = string.Empty;
        public string Schulgliederung { get; set; } = string.Empty;
        public string OrgForm { get; set; } = string.Empty;
        public string Klassenart { get; set; } = string.Empty;
        public string Fachklasse { get; set; } = string.Empty;
        public string BeginnBildungsgang { get; set; } = string.Empty; // from Zusatzdaten
    }

    public class Adresse
    {
        public string Name1 { get; set; } = string.Empty;
        public string Strasse { get; set; } = string.Empty;
        public string PLZ { get; set; } = string.Empty;
        public string Ort { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Telefon2 { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
        public string BetreuerAnrede { get; set; } = string.Empty;
        public string BetreuerVorname { get; set; } = string.Empty;
        public string BetreuerNachname { get; set; } = string.Empty;
        public string SchildAdressId { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalFields { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class Erzieher
    {
        public string Vorname1 { get; set; } = string.Empty;
        public string Nachname1 { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Strasse { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalFields { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
