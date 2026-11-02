using ERechnungPruefung.Models;
using iText.Kernel.Pdf;
using s2industries.ZUGFeRD;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace ERechnungPruefung.Services
{
    public class ERechnungParser
    {
        private const string UblInvoiceNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private static readonly XNamespace _cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        private static readonly XNamespace _cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private static readonly byte[] _pdfSignature = "%PDF-"u8.ToArray();
        private const int PdfHeaderSearchWindow = 1024;

        public ParseResult ParseFile(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return IsPdf(fs) ? ParseFromPdf(fs) : ParseFromXmlStream(fs);
            }
            catch (Exception ex)
            {
                return ParseResult.Fail($"Die Datei konnte nicht gelesen werden: {ex.Message}");
            }
        }

        public static bool IsPdfFile(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return IsPdf(fs);
        }

        private static bool IsPdf(Stream stream)
        {
            long startPos = stream.Position;
            var buffer = new byte[PdfHeaderSearchWindow];
            int read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            stream.Position = startPos;
            return buffer.AsSpan(0, read).IndexOf(_pdfSignature) >= 0;
        }

        private static ParseResult ParseFromXmlStream(Stream stream)
        {
            long startPos = stream.Position;

            if (IsUblInvoice(stream))
            {
                stream.Position = startPos;
                return ParseUbl(stream);
            }

            stream.Position = startPos;
            try
            {
                var descriptor = InvoiceDescriptor.Load(stream);
                return ParseResult.Ok(Map(descriptor));
            }
            catch (Exception ex)
            {
                return ParseResult.Fail(
                    $"Die Rechnungsdaten liegen nicht im Format ZUGFeRD/XRechnung vor ({ex.Message}).");
            }
        }

        private static bool IsUblInvoice(Stream stream)
        {
            try
            {
                var settings = new XmlReaderSettings { CloseInput = false, DtdProcessing = DtdProcessing.Ignore };
                using var xr = XmlReader.Create(stream, settings);
                while (xr.Read())
                    if (xr.NodeType == XmlNodeType.Element)
                        return xr.NamespaceURI == UblInvoiceNs;
            }
            catch { }
            return false;
        }

        private static ParseResult ParseUbl(Stream stream)
        {
            try
            {
                using var buf = new MemoryStream();
                stream.CopyTo(buf);
                buf.Position = 0;

                var doc = XDocument.Load(buf);
                var root = doc.Root;

                var supplier = root?.Element(_cac + "AccountingSupplierParty")?.Element(_cac + "Party");
                var customer = root?.Element(_cac + "AccountingCustomerParty")?.Element(_cac + "Party");
                var totals = root?.Element(_cac + "LegalMonetaryTotal");
                var taxTotal = root?.Element(_cac + "TaxTotal");

                var rechnung = new ERechnung
                {
                    Rechnungsnummer = root?.Element(_cbc + "ID")?.Value,
                    Rechnungsdatum = ParseDate(root?.Element(_cbc + "IssueDate")?.Value),
                    Waehrung = root?.Element(_cbc + "DocumentCurrencyCode")?.Value,
                    Bestellnummer = root?.Element(_cac + "OrderReference")?.Element(_cbc + "ID")?.Value,

                    LieferantName = supplier?.Element(_cac + "PartyName")?.Element(_cbc + "Name")?.Value
                        ?? supplier?.Element(_cac + "PartyLegalEntity")?.Element(_cbc + "RegistrationName")?.Value,
                    LieferantStrasse = supplier?.Element(_cac + "PostalAddress")?.Element(_cbc + "StreetName")?.Value,
                    LieferantPlz = supplier?.Element(_cac + "PostalAddress")?.Element(_cbc + "PostalZone")?.Value,
                    LieferantOrt = supplier?.Element(_cac + "PostalAddress")?.Element(_cbc + "CityName")?.Value,
                    LieferantLand = supplier?.Element(_cac + "PostalAddress")?.Element(_cac + "Country")?.Element(_cbc + "IdentificationCode")?.Value,
                    LieferantUstId = supplier?.Elements(_cac + "PartyTaxScheme")
                        .FirstOrDefault(e => e.Element(_cac + "TaxScheme")?.Element(_cbc + "ID")?.Value == "VAT")
                        ?.Element(_cbc + "CompanyID")?.Value,
                    LieferantSteuernummer = supplier?.Elements(_cac + "PartyTaxScheme")
                        .FirstOrDefault(e => e.Element(_cac + "TaxScheme")?.Element(_cbc + "ID")?.Value == "FC")
                        ?.Element(_cbc + "CompanyID")?.Value,

                    KaeuferName = customer?.Element(_cac + "PartyName")?.Element(_cbc + "Name")?.Value
                        ?? customer?.Element(_cac + "PartyLegalEntity")?.Element(_cbc + "RegistrationName")?.Value,
                    KaeuferStrasse = customer?.Element(_cac + "PostalAddress")?.Element(_cbc + "StreetName")?.Value,
                    KaeuferPlz = customer?.Element(_cac + "PostalAddress")?.Element(_cbc + "PostalZone")?.Value,
                    KaeuferOrt = customer?.Element(_cac + "PostalAddress")?.Element(_cbc + "CityName")?.Value,

                    Nettobetrag = ParseDecimal(totals?.Element(_cbc + "TaxExclusiveAmount")?.Value),
                    Steuerbetrag = ParseDecimal(taxTotal?.Element(_cbc + "TaxAmount")?.Value),
                    Bruttobetrag = ParseDecimal(totals?.Element(_cbc + "PayableAmount")?.Value),

                    Faelligkeitsdatum = ParseDate(root?.Element(_cac + "PaymentMeans")?.Element(_cbc + "PaymentDueDate")?.Value),

                    LieferantIban = root?.Element(_cac + "PaymentMeans")?.Element(_cac + "PayeeFinancialAccount")?.Element(_cbc + "ID")?.Value,
                    LieferantBic = root?.Element(_cac + "PaymentMeans")?.Element(_cac + "PayeeFinancialAccount")?.Element(_cac + "FinancialInstitutionBranch")?.Element(_cbc + "ID")?.Value,
                    LieferantKontoinhaber = supplier?.Element(_cac + "PartyName")?.Element(_cbc + "Name")?.Value
                        ?? supplier?.Element(_cac + "PartyLegalEntity")?.Element(_cbc + "RegistrationName")?.Value,
                    Verwendungszweck = root?.Element(_cac + "PaymentMeans")?.Element(_cbc + "PaymentID")?.Value,

                    Positionen = root?.Elements(_cac + "InvoiceLine").Select(MapUblLine).ToList()
                        ?? new List<ERechnungPosition>()
                };

                return ParseResult.Ok(rechnung);
            }
            catch (Exception ex)
            {
                return ParseResult.Fail($"Die UBL-Rechnung konnte nicht gelesen werden: {ex.Message}");
            }
        }

        private static ParseResult ParseFromPdf(Stream stream)
        {
            List<(string FileName, byte[] Content)> attachments;
            try
            {
                attachments = ReadEmbeddedXmlFiles(stream);
            }
            catch (Exception ex)
            {
                return ParseResult.Fail($"Die PDF konnte nicht gelesen werden: {ex.Message}");
            }

            if (attachments.Count == 0)
                return ParseResult.Fail("Die PDF enthält keine eingebettete XML-Rechnung – keine ZUGFeRD-/Factur-X-Rechnung.");

            foreach (var (_, content) in attachments)
            {
                using var xmlMs = new MemoryStream(content);
                var result = ParseFromXmlStream(xmlMs);
                if (result.Success) return result;
            }

            string fileNames = string.Join(", ", attachments.Select(a => a.FileName));
            return ParseResult.Fail(
                $"Die PDF enthält Rechnungsdaten ({fileNames}), aber nicht im Format ZUGFeRD/XRechnung.");
        }

        private static List<(string FileName, byte[] Content)> ReadEmbeddedXmlFiles(Stream stream)
        {
            using var pdfMs = new MemoryStream();
            stream.CopyTo(pdfMs);
            pdfMs.Position = 0;

            using var reader = new PdfReader(pdfMs);
            using var pdfDoc = new PdfDocument(reader);

            var result = new List<(string, byte[])>();
            var embeddedFiles = pdfDoc.GetCatalog().GetPdfObject()
                .GetAsDictionary(PdfName.Names)
                ?.GetAsDictionary(PdfName.EmbeddedFiles);
            if (embeddedFiles != null)
                CollectXmlFromNameTree(embeddedFiles, result);
            return result;
        }

        private static void CollectXmlFromNameTree(PdfDictionary tree, List<(string, byte[])> result)
        {
            var names = tree.GetAsArray(PdfName.Names);
            if (names != null)
            {
                for (int i = 0; i + 1 < names.Size(); i += 2)
                {
                    string fileName = names.GetAsString(i)?.ToUnicodeString() ?? "";
                    if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var fsObj = names.Get(i + 1);
                    if (fsObj is PdfIndirectReference indRef)
                        fsObj = indRef.GetRefersTo();
                    if (fsObj is not PdfDictionary fsDict) continue;

                    var ef = fsDict.GetAsDictionary(PdfName.EF);
                    var fileStream = ef?.GetAsStream(PdfName.F)
                                   ?? ef?.GetAsStream(new PdfName("UF"));
                    byte[] xmlBytes = fileStream?.GetBytes();
                    if (xmlBytes?.Length > 0)
                        result.Add((fileName, xmlBytes));
                }
            }

            var kids = tree.GetAsArray(PdfName.Kids);
            if (kids != null)
            {
                for (int i = 0; i < kids.Size(); i++)
                {
                    var kid = kids.GetAsDictionary(i);
                    if (kid != null)
                        CollectXmlFromNameTree(kid, result);
                }
            }
        }

        private static ERechnungPosition MapUblLine(XElement line)
        {
            var qtyEl = line.Element(_cbc + "InvoicedQuantity");
            return new ERechnungPosition
            {
                PositionNummer = line.Element(_cbc + "ID")?.Value,
                Bezeichnung = line.Element(_cac + "Item")?.Element(_cbc + "Name")?.Value,
                Beschreibung = line.Element(_cac + "Item")?.Element(_cbc + "Description")?.Value,
                Menge = ParseDecimal(qtyEl?.Value),
                Einheit = TranslateUnitCode(qtyEl?.Attribute("unitCode")?.Value),
                Einzelpreis = ParseDecimal(line.Element(_cac + "Price")?.Element(_cbc + "PriceAmount")?.Value),
                Gesamtpreis = ParseDecimal(line.Element(_cbc + "LineExtensionAmount")?.Value),
                Steuersatz = ParseDecimal(line.Element(_cac + "Item")?.Element(_cac + "ClassifiedTaxCategory")?.Element(_cbc + "Percent")?.Value)
            };
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null;
        }

        private static decimal ParseDecimal(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0m;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
        }

        private static ERechnung Map(InvoiceDescriptor d) => new()
        {
            Rechnungsnummer = d.InvoiceNo,
            Rechnungsdatum = d.InvoiceDate,
            Waehrung = d.Currency.ToString(),
            Bestellnummer = d.OrderNo,

            LieferantName = d.Seller?.Name,
            LieferantStrasse = d.Seller?.Street,
            LieferantPlz = d.Seller?.Postcode,
            LieferantOrt = d.Seller?.City,
            LieferantLand = d.Seller?.Country?.ToString(),
            LieferantUstId = d.SellerTaxRegistration
                ?.FirstOrDefault(t => t.SchemeID == TaxRegistrationSchemeID.VA)?.No,
            LieferantSteuernummer = d.SellerTaxRegistration
                ?.FirstOrDefault(t => t.SchemeID == TaxRegistrationSchemeID.FC)?.No,

            KaeuferName = d.Buyer?.Name,
            KaeuferStrasse = d.Buyer?.Street,
            KaeuferPlz = d.Buyer?.Postcode,
            KaeuferOrt = d.Buyer?.City,

            Nettobetrag = d.TaxBasisAmount ?? 0m,
            Steuerbetrag = d.TaxTotalAmount ?? 0m,
            Bruttobetrag = d.GrandTotalAmount ?? 0m,

            Faelligkeitsdatum = d.PaymentTerms?.FirstOrDefault()?.DueDate,

            LieferantIban = d.GetCreditorFinancialAccounts()?.FirstOrDefault()?.IBAN,
            LieferantBic = d.GetCreditorFinancialAccounts()?.FirstOrDefault()?.BIC,
            LieferantKontoinhaber = d.Seller?.Name,
            Verwendungszweck = d.PaymentReference,

            Positionen = d.TradeLineItems?.Select(MapPosition).ToList() ?? new List<ERechnungPosition>()
        };

        private static ERechnungPosition MapPosition(TradeLineItem item) => new()
        {
            PositionNummer = item.AssociatedDocument?.LineID,
            Bezeichnung = item.Name,
            Beschreibung = item.Description,
            Menge = item.BilledQuantity,
            Einheit = TranslateUnitCode(item.UnitCode?.ToString()),
            Einzelpreis = item.NetUnitPrice,
            Gesamtpreis = item.LineTotalAmount ?? 0m,
            Steuersatz = item.TaxPercent
        };

        private static readonly Dictionary<string, string> _unitCodeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "C62", "Stück" },
            { "H87", "Stück" },
            { "PCE", "Stück" },
            { "ST",  "Stück" },
            { "XPC", "Stück" },
            { "KGM", "kg" },
            { "GRM", "g" },
            { "MGM", "mg" },
            { "TNE", "t" },
            { "LBR", "lb" },
            { "LTR", "l" },
            { "MLT", "ml" },
            { "CLT", "cl" },
            { "MTQ", "m³" },
            { "MTR", "m" },
            { "CMT", "cm" },
            { "MMT", "mm" },
            { "MTK", "m²" },
            { "CMK", "cm²" },
            { "HUR", "h" },
            { "MIN", "min" },
            { "SEC", "s" },
            { "DAY", "Tag" },
            { "WEE", "Woche" },
            { "MON", "Monat" },
            { "ANN", "Jahr" },
            { "XBX", "Karton" },
            { "XCT", "Karton" },
            { "XCS", "Kiste" },
            { "XCR", "Kiste" },
            { "XBU", "Bund" },
            { "XBG", "Beutel" },
            { "XST", "Satz" },
            { "SET", "Satz" },
            { "PR",  "Paar" },
            { "DZN", "Dutzend" },
            { "XPA", "Palette" },
            { "XPX", "Palette" },
        };

        private static string TranslateUnitCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return code;
            return _unitCodeMap.TryGetValue(code, out var text) ? text : code;
        }
    }
}
