using System;

namespace AppFixKosten
{
    // Einstiegspunkt des Programms
    class Program
    {
        static void Main(string[] args)
        {
            var chef = new RechnungsChef();
            chef.Start();
        }
    }
}