using System;
using System.Collections.Generic;

namespace ERechnungPruefung.Models
{
    public class ERechnung
    {
        public string Rechnungsnummer { get; set; }
        public DateTime? Rechnungsdatum { get; set; }
        public string Waehrung { get; set; }
        public string Bestellnummer { get; set; }

        public string LieferantName { get; set; }
        public string LieferantStrasse { get; set; }
        public string LieferantPlz { get; set; }
        public string LieferantOrt { get; set; }
        public string LieferantLand { get; set; }
        public string LieferantUstId { get; set; }
        public string LieferantSteuernummer { get; set; }

        public string KaeuferName { get; set; }
        public string KaeuferStrasse { get; set; }
        public string KaeuferPlz { get; set; }
        public string KaeuferOrt { get; set; }

        public decimal Nettobetrag { get; set; }
        public decimal Steuerbetrag { get; set; }
        public decimal Bruttobetrag { get; set; }

        public DateTime? Faelligkeitsdatum { get; set; }

        public string LieferantIban { get; set; }
        public string LieferantBic { get; set; }
        public string LieferantKontoinhaber { get; set; }
        public string Verwendungszweck { get; set; }

        public List<ERechnungPosition> Positionen { get; set; } = new();
    }

    public class ERechnungPosition
    {
        public string PositionNummer { get; set; }
        public string Bezeichnung { get; set; }
        public string Beschreibung { get; set; }
        public decimal Menge { get; set; }
        public string Einheit { get; set; }
        public decimal Einzelpreis { get; set; }
        public decimal Gesamtpreis { get; set; }
        public decimal Steuersatz { get; set; }
    }

    public record ParseResult(bool Success, ERechnung Rechnung, string Message)
    {
        public static ParseResult Ok(ERechnung rechnung) => new(true, rechnung, null);
        public static ParseResult Fail(string message) => new(false, null, message);
    }
}
