using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace AppFixKosten
{
    // Klasse zur Verwaltung von Rechnungen
    public class RechnungsChef
    {
        private List<Rechnung> Rechnungen { get; set; }
        private List<Kontostand> Kontostände { get; set; }
        private string DatenDatei { get; } = "bills.json";
        private string KontostandDatei { get; } = "balance.json";
        private const int MinJahr = 2000;
        private const int MaxJahr = 9999;

        public RechnungsChef()
        {
            Rechnungen = new List<Rechnung>();
            Kontostände = new List<Kontostand>();
        }

        public void Start()
        {
            LadeRechnungen();
            LadeKontostände();
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Rechnungsverwaltung");
                Console.WriteLine("1. Wiederkehrende Rechnung hinzufügen");
                Console.WriteLine("2. Einmaleinzahlung hinzufügen");
                Console.WriteLine("3. Monatliche/Jährliche Übersicht anzeigen");
                Console.WriteLine("4. Rechnung ändern");
                Console.WriteLine("5. Rechnung löschen");
                Console.WriteLine("6. Beenden");
                Console.Write("Auswahl: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        NeueWiederkehrendeRechnung();
                        break;
                    case "2":
                        NeueEinmalZahlung();
                        break;
                    case "3":
                        ZeigeÜbersicht();
                        break;
                    case "4":
                        RechnungBearbeiten();
                        break;
                    case "5":
                        RechnungLöschen();
                        break;
                    case "6":
                        SpeichereRechnungen();
                        SpeichereKontostände();
                        return;
                    default:
                        Console.WriteLine("Ungültige Eingabe. Drücke eine Taste...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private bool BestätigeAktion(string prompt)
        {
            Console.Write($"{prompt} (j/n): ");
            return Console.ReadLine()?.Trim().ToLower() == "j";
        }

        private void NeueWiederkehrendeRechnung()
        {
            Console.Clear();
            Console.Write("Name der Rechnung: ");
            string? name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Ungültiger Name. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }

            Console.Write("Betrag (in EUR): ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal betrag) || betrag <= 0)
            {
                Console.WriteLine("Ungültiger Betrag. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }

            Console.Write("Rhythmus (0 = einmalig, 1 = monatlich, 3 = vierteljährlich, 12 = jährlich): ");
            if (!int.TryParse(Console.ReadLine(), out int rhythmusWert) || !Enum.IsDefined(typeof(ZahlungsRhythmus), rhythmusWert))
            {
                Console.WriteLine("Ungültiger Rhythmus. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }
            ZahlungsRhythmus rhythmus = (ZahlungsRhythmus)rhythmusWert;

            Console.Write("Abbuchungsdatum (TT.MM.JJJJ): ");
            if (!DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out DateTime fälligkeitsDatum))
            {
                Console.WriteLine("Ungültiges Datum. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }

            var rechnung = new Rechnung
            {
                Name = name,
                Verlauf = new List<RechnungsVersion>
                {
                    new RechnungsVersion
                    {
                        Betrag = betrag,
                        Rhythmus = rhythmus,
                        FälligkeitsDatum = fälligkeitsDatum,
                        GültigAb = DateTime.Now
                    }
                }
            };
            Rechnungen.Add(rechnung);

            SpeichereRechnungen();
            Console.WriteLine("Wiederkehrende Rechnung hinzugefügt.");

            if (rhythmus == ZahlungsRhythmus.Vierteljährlich || rhythmus == ZahlungsRhythmus.Jährlich)
            {
                VorschlageRückstellung(rechnung);
            }

            Console.WriteLine("Drücke eine Taste, um zum Hauptmenü zurückzukehren...");
            Console.ReadKey();
        }

        private void NeueEinmalZahlung()
        {
            Console.Clear();
            Console.Write("Name der Einmaleinzahlung: ");
            string? name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Ungültiger Name. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }

            Console.Write("Betrag (in EUR): ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal betrag) || betrag <= 0)
            {
                Console.WriteLine("Ungültiger Betrag. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }

            Console.Write("Einzahlungsdatum (TT.MM.JJJJ): ");
            if (!DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out DateTime fälligkeitsDatum))
            {
                Console.WriteLine("Ungültiges Datum. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }

            var rechnung = new Rechnung
            {
                Name = name,
                Verlauf = new List<RechnungsVersion>
                {
                    new RechnungsVersion
                    {
                        Betrag = betrag,
                        Rhythmus = ZahlungsRhythmus.Einmalig,
                        FälligkeitsDatum = fälligkeitsDatum,
                        GültigAb = DateTime.Now
                    }
                }
            };
            Rechnungen.Add(rechnung);
            SpeichereRechnungen();
            Console.WriteLine("Einmaleinzahlung hinzugefügt.");

            Console.WriteLine("Drücke eine Taste, um zum Hauptmenü zurückzukehren...");
            Console.ReadKey();
        }

        private void RechnungBearbeiten()
        {
            Console.Clear();
            if (Rechnungen.Count == 0)
            {
                Console.WriteLine("Keine Rechnungen vorhanden. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Wähle eine Rechnung zum Ändern (sortiert nach Name):");
            var sortierteRechnungen = Rechnungen.OrderBy(r => r.Name).ToList();
            for (int i = 0; i < sortierteRechnungen.Count; i++)
            {
                var version = HoleAktuelleVersion(sortierteRechnungen[i]);
                string rhythmusText = version.Rhythmus == ZahlungsRhythmus.Einmalig ? "einmalig" :
                                     version.Rhythmus == ZahlungsRhythmus.Monatlich ? "monatlich" :
                                     version.Rhythmus == ZahlungsRhythmus.Vierteljährlich ? "vierteljährlich" : "jährlich";
                Console.WriteLine($"{i + 1}. {sortierteRechnungen[i].Name} ({version.Betrag:F2} EUR, Rhythmus: {rhythmusText}, Abbuchungsdatum: {version.FälligkeitsDatum:dd.MM.yyyy})");
            }

            Console.Write("Auswahl (Nummer): ");
            if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > sortierteRechnungen.Count)
            {
                Console.WriteLine("Ungültige Auswahl. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }

            var rechnung = sortierteRechnungen[index - 1];
            var aktuelleVersion = HoleAktuelleVersion(rechnung);
            Console.WriteLine("\nAktuelle Werte:");
            Console.WriteLine($"Name: {rechnung.Name}");
            Console.WriteLine($"Betrag: {aktuelleVersion.Betrag:F2} EUR");
            Console.WriteLine($"Rhythmus: {aktuelleVersion.Rhythmus}");
            Console.WriteLine($"Abbuchungsdatum: {aktuelleVersion.FälligkeitsDatum:dd.MM.yyyy}");
            Console.WriteLine("\nNeue Werte (Enter, um unverändert zu lassen):");

            Console.Write("Neuer Name: ");
            string? neuerName = Console.ReadLine();
            if (!string.IsNullOrEmpty(neuerName))
                rechnung.Name = neuerName;

            var neueVersion = new RechnungsVersion
            {
                Betrag = aktuelleVersion.Betrag,
                Rhythmus = aktuelleVersion.Rhythmus,
                FälligkeitsDatum = aktuelleVersion.FälligkeitsDatum,
                GültigAb = DateTime.Now
            };

            Console.Write("Neuer Betrag (in EUR): ");
            string? betragEingabe = Console.ReadLine();
            if (!string.IsNullOrEmpty(betragEingabe) && decimal.TryParse(betragEingabe, out decimal neuerBetrag) && neuerBetrag > 0)
                neueVersion.Betrag = neuerBetrag;

            Console.Write("Neuer Rhythmus (0 = einmalig, 1 = monatlich, 3 = vierteljährlich, 12 = jährlich): ");
            string? rhythmusEingabe = Console.ReadLine();
            if (!string.IsNullOrEmpty(rhythmusEingabe) && int.TryParse(rhythmusEingabe, out int neuerRhythmusWert) && Enum.IsDefined(typeof(ZahlungsRhythmus), neuerRhythmusWert))
                neueVersion.Rhythmus = (ZahlungsRhythmus)neuerRhythmusWert;

            Console.Write("Neues Abbuchungsdatum (TT.MM.JJJJ): ");
            string? fälligkeitsDatumEingabe = Console.ReadLine();
            if (!string.IsNullOrEmpty(fälligkeitsDatumEingabe) && DateTime.TryParseExact(fälligkeitsDatumEingabe, "dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out DateTime neuesFälligkeitsDatum))
                neueVersion.FälligkeitsDatum = neuesFälligkeitsDatum;

            Console.Write("Änderung gültig ab (TT.MM.JJJJ): ");
            string? gültigAbEingabe = Console.ReadLine();
            if (!DateTime.TryParseExact(gültigAbEingabe, "dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"), DateTimeStyles.None, out DateTime gültigAb))
            {
                Console.WriteLine("Ungültiges Datum. Änderung wird ab sofort angewendet.");
                gültigAb = DateTime.Now;
            }
            neueVersion.GültigAb = gültigAb;

            // Nur neue Version hinzufügen, wenn Änderungen vorgenommen wurden
            if (neueVersion.Betrag != aktuelleVersion.Betrag ||
                neueVersion.Rhythmus != aktuelleVersion.Rhythmus ||
                neueVersion.FälligkeitsDatum != aktuelleVersion.FälligkeitsDatum ||
                neueVersion.GültigAb != aktuelleVersion.GültigAb)
            {
                rechnung.Verlauf.Add(neueVersion);
                rechnung.Verlauf.Sort((a, b) => a.GültigAb.CompareTo(b.GültigAb));
            }

            SpeichereRechnungen();
            Console.WriteLine("Rechnung geändert.");

            // Rückstellungen prüfen
            if (neueVersion.Rhythmus == ZahlungsRhythmus.Vierteljährlich || neueVersion.Rhythmus == ZahlungsRhythmus.Jährlich)
            {
                VorschlageRückstellung(rechnung);
            }
            else if (aktuelleVersion.Rhythmus == ZahlungsRhythmus.Vierteljährlich || aktuelleVersion.Rhythmus == ZahlungsRhythmus.Jährlich)
            {
                if (BestätigeAktion("Rückstellungen für vergangene Monate neu berechnen?"))
                {
                    rechnung.HatRückstellung = false;
                    VorschlageRückstellung(rechnung);
                }
            }

            Console.WriteLine("Drücke eine Taste, um zum Hauptmenü zurückzukehren...");
            Console.ReadKey();
        }

        private void VorschlageRückstellung(Rechnung rechnung)
        {
            if (rechnung == null || string.IsNullOrEmpty(rechnung.Name))
                return;

            var aktuelleVersion = HoleAktuelleVersion(rechnung);
            if (aktuelleVersion.Rhythmus != ZahlungsRhythmus.Vierteljährlich && aktuelleVersion.Rhythmus != ZahlungsRhythmus.Jährlich)
                return;

            decimal monatlicheRate;
            int vergangeneMonate;
            DateTime berechnungsStart;
            DateTime berechnungsEnde;
            DateTime nächsteFälligkeit = HoleNächsteFälligkeit(rechnung, rechnung.ErstellungsDatum);

            if (aktuelleVersion.Rhythmus == ZahlungsRhythmus.Jährlich)
            {
                monatlicheRate = aktuelleVersion.Betrag / 12;
                DateTime letzteFälligkeit = HoleVorherigeFälligkeit(rechnung, rechnung.ErstellungsDatum);
                berechnungsStart = letzteFälligkeit.AddMonths(1);
                berechnungsEnde = rechnung.ErstellungsDatum;

                vergangeneMonate = ((berechnungsEnde.Year - berechnungsStart.Year) * 12) + berechnungsEnde.Month - berechnungsStart.Month;
                if (berechnungsEnde.Day >= berechnungsStart.Day)
                    vergangeneMonate++;

                if (vergangeneMonate <= 0)
                    return;

                decimal differenz = monatlicheRate * vergangeneMonate;

                Console.WriteLine($"\nFür die Rechnung '{rechnung.Name}' (nächste Fälligkeit: {nächsteFälligkeit:dd.MM.yyyy}) fehlen Rückstellungen für {vergangeneMonate} Monate.");
                Console.WriteLine($"Vorschlag: Einmaleinzahlung von {differenz:F2} EUR am {rechnung.ErstellungsDatum:dd.MM.yyyy} hinzufügen.");
                if (BestätigeAktion("Einmalzahlung hinzufügen?"))
                {
                    Rechnungen.Add(new Rechnung
                    {
                        Name = $"Rückstellung {rechnung.Name}",
                        Verlauf = new List<RechnungsVersion>
                        {
                            new RechnungsVersion
                            {
                                Betrag = differenz,
                                Rhythmus = ZahlungsRhythmus.Einmalig,
                                FälligkeitsDatum = rechnung.ErstellungsDatum,
                                GültigAb = rechnung.ErstellungsDatum
                            }
                        },
                        ErstellungsDatum = rechnung.ErstellungsDatum
                    });
                    rechnung.HatRückstellung = true;
                    SpeichereRechnungen();
                    Console.WriteLine("Einmaleinzahlung hinzugefügt.");
                }
                else
                {
                    Console.WriteLine("Einmalzahlung nicht hinzugefügt.");
                }
            }
            else // Vierteljährlich
            {
                monatlicheRate = aktuelleVersion.Betrag / 3;
                berechnungsStart = new DateTime(rechnung.ErstellungsDatum.Year, rechnung.ErstellungsDatum.Month, 1).AddMonths(1);
                berechnungsEnde = nächsteFälligkeit;

                vergangeneMonate = ((berechnungsEnde.Year - berechnungsStart.Year) * 12) + berechnungsEnde.Month - berechnungsStart.Month;
                if (berechnungsEnde.Day >= aktuelleVersion.FälligkeitsDatum.Day)
                    vergangeneMonate++;

                vergangeneMonate = Math.Min(vergangeneMonate, 3);

                if (vergangeneMonate <= 0)
                    return;

                decimal differenz = aktuelleVersion.Betrag - (monatlicheRate * vergangeneMonate);

                if (differenz <= 0)
                    return;

                Console.WriteLine($"\nFür die Rechnung '{rechnung.Name}' (nächste Fälligkeit: {nächsteFälligkeit:dd.MM.yyyy}) fehlen {differenz:F2} EUR zur vollen Summe.");
                Console.WriteLine($"Vorschlag: Einmaleinzahlung von {differenz:F2} EUR am {rechnung.ErstellungsDatum:dd.MM.yyyy} hinzufügen.");
                if (BestätigeAktion("Einmalzahlung hinzufügen?"))
                {
                    Rechnungen.Add(new Rechnung
                    {
                        Name = $"Rückstellung {rechnung.Name}",
                        Verlauf = new List<RechnungsVersion>
                        {
                            new RechnungsVersion
                            {
                                Betrag = differenz,
                                Rhythmus = ZahlungsRhythmus.Einmalig,
                                FälligkeitsDatum = rechnung.ErstellungsDatum,
                                GültigAb = rechnung.ErstellungsDatum
                            }
                        },
                        ErstellungsDatum = rechnung.ErstellungsDatum
                    });
                    rechnung.HatRückstellung = true;
                    SpeichereRechnungen();
                    Console.WriteLine("Einmaleinzahlung hinzugefügt.");
                }
                else
                {
                    Console.WriteLine("Einmalzahlung nicht hinzugefügt.");
                }
            }
        }

        private void RechnungLöschen()
        {
            Console.Clear();
            if (Rechnungen.Count == 0)
            {
                Console.WriteLine("Keine Rechnungen vorhanden.");
                Console.WriteLine("Drücke eine Taste, um zum Hauptmenü zurückzukehren...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Wähle eine Rechnung zum Löschen (sortiert nach Name):");
            var sortierteRechnungen = Rechnungen.OrderBy(r => r.Name).ToList();
            for (int i = 0; i < sortierteRechnungen.Count; i++)
            {
                var version = HoleAktuelleVersion(sortierteRechnungen[i]);
                string rhythmusText = version.Rhythmus == ZahlungsRhythmus.Einmalig ? "einmalig" :
                                     version.Rhythmus == ZahlungsRhythmus.Monatlich ? "monatlich" :
                                     version.Rhythmus == ZahlungsRhythmus.Vierteljährlich ? "vierteljährlich" : "jährlich";
                Console.WriteLine($"{i + 1}. {sortierteRechnungen[i].Name} ({version.Betrag:F2} EUR, Rhythmus: {rhythmusText}, Abbuchungsdatum: {version.FälligkeitsDatum:dd.MM.yyyy})");
            }
            Console.WriteLine("0. Zurück ins Menü");
            Console.Write("Auswahl: ");

            string? eingabe = Console.ReadLine();
            if (eingabe == "0")
                return;

            if (!int.TryParse(eingabe, out int index) || index < 1 || index > sortierteRechnungen.Count)
            {
                Console.WriteLine("Ungültige Auswahl. Drücke eine Taste, um zum Hauptmenü zurückzukehren...");
                Console.ReadKey();
                return;
            }

            var rechnung = sortierteRechnungen[index - 1];
            if (rechnung == null || string.IsNullOrEmpty(rechnung.Name))
            {
                Console.WriteLine("Ungültige Rechnung. Drücke eine Taste...");
                Console.ReadKey();
                return;
            }

            if (!BestätigeAktion($"Rechnung '{rechnung.Name}' wirklich löschen?"))
            {
                Console.WriteLine("Löschvorgang abgebrochen.");
                Console.WriteLine("Drücke eine Taste, um zum Hauptmenü zurückzukehren...");
                Console.ReadKey();
                return;
            }

            // Prüfen, ob eine Rückstellung existiert
            var rückstellungsRechnung = Rechnungen.FirstOrDefault(r => r.Name == $"Rückstellung {rechnung.Name}");
            if (rückstellungsRechnung != null)
            {
                if (BestätigeAktion($"Dazugehörige Rückstellung '{rückstellungsRechnung.Name}' ebenfalls löschen?"))
                {
                    Rechnungen.Remove(rückstellungsRechnung);
                    Console.WriteLine($"Rückstellung '{rückstellungsRechnung.Name}' gelöscht.");
                }
            }

            Rechnungen.Remove(rechnung);
            SpeichereRechnungen();
            Console.WriteLine($"Rechnung '{rechnung.Name}' gelöscht.");
            Console.WriteLine("Drücke eine Taste, um zum Hauptmenü zurückzukehren...");
            Console.ReadKey();
        }

        private void ZeigeÜbersicht()
        {
            Console.Clear();
            Console.Write("Jahr eingeben (z.B. 2025 oder 2025-2026, leer für aktuelles Jahr): ");
            string? jahrEingabe = Console.ReadLine();
            int startJahr;
            int endeJahr;

            // Parse year input (single year or range)
            if (string.IsNullOrEmpty(jahrEingabe))
            {
                startJahr = endeJahr = DateTime.Now.Year;
            }
            else if (jahrEingabe.Contains("-"))
            {
                var jahre = jahrEingabe.Split('-');
                if (jahre.Length == 2 &&
                    int.TryParse(jahre[0], out int start) && start >= MinJahr && start <= MaxJahr &&
                    int.TryParse(jahre[1], out int ende) && ende >= start && ende <= MaxJahr)
                {
                    startJahr = start;
                    endeJahr = ende;
                }
                else
                {
                    Console.WriteLine("Ungültiger Jahresbereich. Verwende aktuelles Jahr.");
                    startJahr = endeJahr = DateTime.Now.Year;
                }
            }
            else if (int.TryParse(jahrEingabe, out int jahr) && jahr >= MinJahr && jahr <= MaxJahr)
            {
                startJahr = endeJahr = jahr;
            }
            else
            {
                Console.WriteLine("Ungültiges Jahr. Verwende aktuelles Jahr.");
                startJahr = endeJahr = DateTime.Now.Year;
            }

            Console.Write("Monat eingeben (1-12, leer für alle Monate): ");
            string? monatEingabe = Console.ReadLine();
            int gewählterMonat = 0;
            bool zeigeAlleMonate = string.IsNullOrEmpty(monatEingabe) || !int.TryParse(monatEingabe, out gewählterMonat) || gewählterMonat < 1 || gewählterMonat > 12;
            int startMonat = zeigeAlleMonate ? 1 : gewählterMonat;
            int endeMonat = zeigeAlleMonate ? 12 : gewählterMonat;

            // Define colors for different years
            ConsoleColor[] jahrFarben = new ConsoleColor[]
            {
                ConsoleColor.Yellow,
                ConsoleColor.Cyan,
                ConsoleColor.Magenta,
                ConsoleColor.Green,
                ConsoleColor.White
            };

            decimal kontostand = 0;

            if (Rechnungen.Count == 0)
            {
                Console.WriteLine("Keine Rechnungen vorhanden. Kontostand bleibt 0.");
            }

            for (int anzeigeJahr = startJahr; anzeigeJahr <= endeJahr; anzeigeJahr++)
            {
                // Reset balance for each year
                if (anzeigeJahr >= 2025)
                {
                    var vorherigerKontostand = Kontostände.FirstOrDefault(k => k.Jahr == anzeigeJahr - 1);
                    kontostand = vorherigerKontostand?.Betrag ?? 0;
                }

                // Select color for the current year
                ConsoleColor jahrFarbe = jahrFarben[(anzeigeJahr - startJahr) % jahrFarben.Length];

                // Year header
                Console.ForegroundColor = jahrFarbe;
                Console.WriteLine($"\nÜbersicht für {anzeigeJahr}:");
                Console.ResetColor();
                Console.WriteLine("==============================");

                for (int monat = startMonat; monat <= endeMonat; monat++)
                {
                    decimal monatlicheEinzahlungen = 0;
                    decimal monatlicheRückstellungen = 0;
                    decimal monatlicheAusgaben = 0;

                    List<(string Name, string Beschreibung)> eingänge = new List<(string Name, string Beschreibung)>();
                    List<(string Name, string Beschreibung)> ausgaben = new List<(string Name, string Beschreibung)>();

                    // Month header
                    Console.ForegroundColor = jahrFarbe;
                    Console.WriteLine($"\nMonat {monat:D2}/{anzeigeJahr}{(monat == DateTime.Now.Month && anzeigeJahr == DateTime.Now.Year ? " (Aktueller Monat)" : "")}:");
                    Console.WriteLine("-----");
                    Console.ResetColor();

                    foreach (var rechnung in Rechnungen)
                    {
                        if (rechnung == null || string.IsNullOrEmpty(rechnung.Name))
                            continue;

                        var version = HoleVersionFürDatum(rechnung, new DateTime(anzeigeJahr, monat, 1));
                        bool überspringeRechnung = version.Rhythmus == ZahlungsRhythmus.Vierteljährlich &&
                            new DateTime(anzeigeJahr, monat, 1) < new DateTime(rechnung.ErstellungsDatum.Year, rechnung.ErstellungsDatum.Month, 1);
                        if (überspringeRechnung)
                            continue;

                        int tageImMonat = DateTime.DaysInMonth(anzeigeJahr, monat);
                        int fälligkeitTag = Math.Min(version.FälligkeitsDatum.Day, tageImMonat);
                        DateTime fälligkeitImMonat = new DateTime(anzeigeJahr, monat, fälligkeitTag);

                        // Prüfen, ob der aktuelle Monat/Jahr nach oder gleich ErstellungsDatum ist
                        bool istNachErstellung = new DateTime(anzeigeJahr, monat, 1) >= new DateTime(rechnung.ErstellungsDatum.Year, rechnung.ErstellungsDatum.Month, 1);

                        if (version.Rhythmus == ZahlungsRhythmus.Einmalig)
                        {
                            if (version.FälligkeitsDatum.Year == anzeigeJahr && version.FälligkeitsDatum.Month == monat)
                            {
                                monatlicheEinzahlungen += version.Betrag;
                                eingänge.Add((rechnung.Name, $"+{version.Betrag:F2} EUR (Einmaleinzahlung am {version.FälligkeitsDatum.Day}. eingezahlt)"));
                            }
                        }
                        else if (istNachErstellung)
                        {
                            int rückstellungsIntervall = version.Rhythmus == ZahlungsRhythmus.Monatlich ? 1 : version.Rhythmus == ZahlungsRhythmus.Vierteljährlich ? 3 : 12;
                            decimal monatlicheRate = version.Betrag / rückstellungsIntervall;

                            bool überspringeRückstellung = false;
                            if (version.Rhythmus == ZahlungsRhythmus.Vierteljährlich)
                            {
                                DateTime rückstellungsStart = new DateTime(rechnung.ErstellungsDatum.Year, rechnung.ErstellungsDatum.Month, 1).AddMonths(1);
                                überspringeRückstellung = new DateTime(anzeigeJahr, monat, 1) < rückstellungsStart ||
                                    (rechnung.HatRückstellung && new DateTime(anzeigeJahr, monat, 1) < new DateTime(rechnung.ErstellungsDatum.Year, rechnung.ErstellungsDatum.Month, 1));
                            }
                            else if (version.Rhythmus == ZahlungsRhythmus.Jährlich)
                            {
                                überspringeRückstellung = rechnung.HatRückstellung &&
                                    new DateTime(anzeigeJahr, monat, 1) < new DateTime(rechnung.ErstellungsDatum.Year, rechnung.ErstellungsDatum.Month, 1);
                            }

                            if (!überspringeRückstellung)
                            {
                                monatlicheRückstellungen += monatlicheRate;
                                eingänge.Add((rechnung.Name, $"+{monatlicheRate:F2} EUR (Rückstellung für Rechnung)"));
                            }

                            bool istFällig = IstRechnungFällig(rechnung, anzeigeJahr, monat);
                            if (istFällig)
                            {
                                monatlicheAusgaben += version.Betrag;
                                ausgaben.Add((rechnung.Name, $"-{version.Betrag:F2} EUR (am {fälligkeitTag}. abgebucht)"));
                            }
                        }
                    }

                    eingänge = eingänge.OrderBy(x => x.Name).ToList();
                    ausgaben = ausgaben.OrderBy(x => x.Name).ToList();

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Eingänge:");
                    Console.ResetColor();
                    Console.WriteLine("--------");
                    if (eingänge.Count == 0)
                    {
                        Console.WriteLine("  Keine Eingänge.");
                    }
                    else
                    {
                        foreach (var eingang in eingänge)
                        {
                            Console.Write($"{eingang.Name,-30} | ");
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("+");
                            Console.ResetColor();
                            Console.WriteLine($"{eingang.Beschreibung.Substring(1),-39}");
                        }
                    }

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Abbuchungen:");
                    Console.ResetColor();
                    Console.WriteLine("-----------");
                    if (ausgaben.Count == 0)
                    {
                        Console.WriteLine("  Keine Abbuchungen.");
                    }
                    else
                    {
                        foreach (var ausgabe in ausgaben)
                        {
                            Console.Write($"{ausgabe.Name,-30} | ");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("-");
                            Console.ResetColor();
                            Console.WriteLine($"{ausgabe.Beschreibung.Substring(1),-39}");
                        }
                    }

                    // Berechne den Kontostand
                    kontostand += monatlicheEinzahlungen + monatlicheRückstellungen - monatlicheAusgaben;

                    // Ausgabe der Zusammenfassung
                    Console.WriteLine();
                    Console.Write($"{"Einmaleinzahlungen",-30} | ");
                    if (monatlicheEinzahlungen == 0)
                        Console.WriteLine($"{monatlicheEinzahlungen:F2} EUR");
                    else if (monatlicheEinzahlungen > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("+");
                        Console.ResetColor();
                        Console.WriteLine($"{monatlicheEinzahlungen:F2} EUR");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("-");
                        Console.ResetColor();
                        Console.WriteLine($"{Math.Abs(monatlicheEinzahlungen):F2} EUR");
                    }

                    Console.Write($"{"Rückstellungen",-30} | ");
                    if (monatlicheRückstellungen == 0)
                        Console.WriteLine($"{monatlicheRückstellungen:F2} EUR");
                    else if (monatlicheRückstellungen > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("+");
                        Console.ResetColor();
                        Console.WriteLine($"{monatlicheRückstellungen:F2} EUR");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("-");
                        Console.ResetColor();
                        Console.WriteLine($"{Math.Abs(monatlicheRückstellungen):F2} EUR");
                    }

                    Console.Write($"{"Abgebucht",-30} | ");
                    if (monatlicheAusgaben == 0)
                        Console.WriteLine($"{monatlicheAusgaben:F2} EUR");
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("-");
                        Console.ResetColor();
                        Console.WriteLine($"{monatlicheAusgaben:F2} EUR");
                    }

                    Console.Write($"{"Kontostand",-30} | ");
                    if (kontostand == 0)
                        Console.WriteLine($"{kontostand:F2} EUR");
                    else if (kontostand >= 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("+");
                        Console.ResetColor();
                        Console.WriteLine($"{kontostand:F2} EUR");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("-");
                        Console.ResetColor();
                        Console.WriteLine($"{Math.Abs(kontostand):F2} EUR");
                    }

                    // Kontostand für Dezember speichern (ab 2025)
                    if (monat == 12 && anzeigeJahr >= 2025)
                    {
                        var bestehenderKontostand = Kontostände.FirstOrDefault(k => k.Jahr == anzeigeJahr);
                        if (bestehenderKontostand != null)
                        {
                            bestehenderKontostand.Betrag = kontostand;
                        }
                        else
                        {
                            Kontostände.Add(new Kontostand { Jahr = anzeigeJahr, Betrag = kontostand });
                        }
                        SpeichereKontostände();
                    }
                }
            }

            Console.WriteLine("\nDrücke eine Taste, um zum Hauptmenü zurückzukehren...");
            Console.ReadKey(true);
        }

        private bool IstRechnungFällig(Rechnung rechnung, int jahr, int monat)
        {
            if (rechnung == null || string.IsNullOrEmpty(rechnung.Name))
                return false;

            var aktuelleVersion = HoleAktuelleVersion(rechnung);
            if (aktuelleVersion.Rhythmus == ZahlungsRhythmus.Einmalig)
                return false;

            var version = HoleVersionFürDatum(rechnung, new DateTime(jahr, monat, 1));
            bool istNachErstellung = new DateTime(jahr, monat, 1) >= new DateTime(rechnung.ErstellungsDatum.Year, rechnung.ErstellungsDatum.Month, 1);
            if (!istNachErstellung)
                return false;

            if (version.Rhythmus == ZahlungsRhythmus.Vierteljährlich &&
                new DateTime(jahr, monat, 1) < new DateTime(rechnung.ErstellungsDatum.Year, rechnung.ErstellungsDatum.Month, 1))
                return false;

            int fälligkeitMonat = version.FälligkeitsDatum.Month;
            int fälligkeitJahr = version.FälligkeitsDatum.Year;

            if (version.Rhythmus == ZahlungsRhythmus.Monatlich)
            {
                return true;
            }
            else if (version.Rhythmus == ZahlungsRhythmus.Vierteljährlich)
            {
                int monateSeitStart = ((jahr - fälligkeitJahr) * 12 + monat - fälligkeitMonat) % 3;
                return monateSeitStart == 0;
            }
            else if (version.Rhythmus == ZahlungsRhythmus.Jährlich)
            {
                return monat == fälligkeitMonat;
            }

            return false;
        }

        private DateTime HoleNächsteFälligkeit(Rechnung rechnung, DateTime referenzDatum)
        {
            var version = HoleAktuelleVersion(rechnung);
            DateTime fälligkeit = version.FälligkeitsDatum;

            if (version.Rhythmus == ZahlungsRhythmus.Einmalig)
                return fälligkeit;

            int intervall = version.Rhythmus == ZahlungsRhythmus.Monatlich ? 1 :
                           version.Rhythmus == ZahlungsRhythmus.Vierteljährlich ? 3 : 12;

            while (fälligkeit < referenzDatum)
            {
                fälligkeit = fälligkeit.AddMonths(intervall);
            }

            return fälligkeit;
        }

        private DateTime HoleVorherigeFälligkeit(Rechnung rechnung, DateTime referenzDatum)
        {
            var version = HoleAktuelleVersion(rechnung);
            DateTime fälligkeit = version.FälligkeitsDatum;

            if (version.Rhythmus == ZahlungsRhythmus.Einmalig)
                return fälligkeit;

            int intervall = version.Rhythmus == ZahlungsRhythmus.Monatlich ? 1 :
                           version.Rhythmus == ZahlungsRhythmus.Vierteljährlich ? 3 : 12;

            while (fälligkeit <= referenzDatum)
            {
                fälligkeit = fälligkeit.AddMonths(intervall);
            }

            return fälligkeit.AddMonths(-intervall);
        }

        private RechnungsVersion HoleAktuelleVersion(Rechnung rechnung)
        {
            return rechnung.Verlauf.OrderByDescending(v => v.GültigAb).First();
        }

        private RechnungsVersion HoleVersionFürDatum(Rechnung rechnung, DateTime datum)
        {
            return rechnung.Verlauf
                .Where(v => v.GültigAb <= datum)
                .OrderByDescending(v => v.GültigAb)
                .FirstOrDefault() ?? HoleAktuelleVersion(rechnung);
        }

        private void LadeRechnungen()
        {
            if (File.Exists(DatenDatei))
            {
                try
                {
                    string json = File.ReadAllText(DatenDatei);
                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };
                    Rechnungen = JsonSerializer.Deserialize<List<Rechnung>>(json, options) ?? new List<Rechnung>();
                }
                catch
                {
                    Console.WriteLine("Fehler beim Laden der Rechnungen. Starte mit leerer Liste.");
                    Rechnungen = new List<Rechnung>();
                }
            }
        }

        private void SpeichereRechnungen()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                string json = JsonSerializer.Serialize(Rechnungen, options);
                File.WriteAllText(DatenDatei, json);
            }
            catch
            {
                Console.WriteLine("Fehler beim Speichern der Rechnungen.");
            }
        }

        private void LadeKontostände()
        {
            if (File.Exists(KontostandDatei))
            {
                try
                {
                    string json = File.ReadAllText(KontostandDatei);
                    Kontostände = JsonSerializer.Deserialize<List<Kontostand>>(json) ?? new List<Kontostand>();
                }
                catch
                {
                    Console.WriteLine("Fehler beim Laden der Kontostände. Starte mit leerer Liste.");
                    Kontostände = new List<Kontostand>();
                }
            }
        }

        private void SpeichereKontostände()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Kontostände, options);
                File.WriteAllText(KontostandDatei, json);
            }
            catch
            {
                Console.WriteLine("Fehler beim Speichern der Kontostände.");
            }
        }
    }
}