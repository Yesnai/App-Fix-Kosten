using System;
using System.Collections.Generic;

namespace AppFixKosten
{
    // Klasse für eine Rechnung
    public class Rechnung
    {
        public string Name { get; set; }
        public List<RechnungsVersion> Verlauf { get; set; }
        public DateTime ErstellungsDatum { get; set; }
        public bool HatRückstellung { get; set; }

        public Rechnung()
        {
            Name = string.Empty;
            Verlauf = new List<RechnungsVersion>();
            ErstellungsDatum = DateTime.Now;
            HatRückstellung = false;
        }
    }
}