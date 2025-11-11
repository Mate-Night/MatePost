using System;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    public class CalculationService
    {
        public decimal CalculateDeliveryCost(Parcel parcel)
        {
            decimal baseCost = parcel.Type == ParcelType.Local ? 50m : 200m;
            baseCost += (decimal)(parcel.Weight * 10);

            baseCost += parcel.DeliveryType switch
            {
                DeliveryType.Office => 0m,
                DeliveryType.Parcelbox => 20m,
                DeliveryType.Address => 50m,
                DeliveryType.Taxi => 150m,
                _ => 0m
            };

            if (parcel.ContentType == ContentType.Fragile)
                baseCost += 30m;

            if (parcel.IsInsured)
                baseCost += parcel.InsuranceValue * 0.02m;

            return baseCost;
        }

        public decimal CalculateImportTax(Parcel parcel)
        {
            if (parcel.Type != ParcelType.International) return 0m;

            const decimal euroThreshold = 150m;
            const decimal eurRate = 41m;
            decimal valueInEuro = parcel.DeclaredValue / eurRate;

            if (valueInEuro > euroThreshold)
                return parcel.DeclaredValue * 0.10m;

            return 0m;
        }

        public decimal ApplyDiscount(decimal amount, Client client, bool useDiscount)
        {
            if (!useDiscount || !client.CanUseDiscount())
                return amount;

            decimal discount = client.GetDiscount();

            if (client.IsLegend() && IsHolidayPeriod())
                discount = 0.35m;

            return amount * (1 - discount);
        }

        private bool IsHolidayPeriod()
        {
            var today = DateTime.Now;
            return (today.Month == 12 && today.Day >= 20) ||
                   (today.Month == 1 && today.Day <= 7);
        }

        public decimal CalculateFinalPrice(Parcel parcel, Client sender, bool useDiscount)
        {
            decimal cost = CalculateDeliveryCost(parcel);
            decimal tax = CalculateImportTax(parcel);
            decimal total = cost + tax;

            return ApplyDiscount(total, sender, useDiscount);
        }
    }
}