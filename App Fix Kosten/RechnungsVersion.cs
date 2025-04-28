using System;

namespace AppFixKosten
{
    // Klasse für eine Version einer Rechnung
    public class RechnungsVersion
    {
        public decimal Betrag { get; set; }
        public ZahlungsRhythmus Rhythmus { get; set; }
        public DateTime FälligkeitsDatum { get; set; }
        public DateTime GültigAb { get; set; }
    }
}