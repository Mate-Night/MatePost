using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    public class Operator
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProcessedParcels { get; set; }
        public double Efficiency { get; set; }

        public Operator() { }

        public Operator(string name)
        {
            Name = name;
            ProcessedParcels = 0;
            Efficiency = 100.0;
        }

        public void IncrementProcessed()
        {
            ProcessedParcels++;
        }
    }
}