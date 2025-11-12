using System;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Сервіс для розрахунку вартості доставки
    /// </summary>
    public class CalculationService
    {
        /// <summary>
        /// Розраховує базову вартість доставки посилки
        /// </summary>
        public decimal CalculateDeliveryCost(Parcel parcel)
        {
            try
            {
                if (parcel == null)
                    throw new ArgumentNullException(nameof(parcel));

                // Базова вартість залежить від типу доставки
                decimal baseCost = parcel.Type == ParcelType.Local
                    ? DeliveryConfiguration.LocalBaseCost
                    : DeliveryConfiguration.InternationalBaseCost;

                // Додавання вартості за вагу
                baseCost += (decimal)parcel.Weight * DeliveryConfiguration.PricePerKg;

                // Додавання вартості залежно від способу доставки
                baseCost += parcel.DeliveryType switch
                {
                    DeliveryType.Office => DeliveryConfiguration.OfficeDeliveryCost,
                    DeliveryType.Parcelbox => DeliveryConfiguration.ParcelboxDeliveryCost,
                    DeliveryType.Address => DeliveryConfiguration.AddressDeliveryCost,
                    DeliveryType.Taxi => DeliveryConfiguration.TaxiDeliveryCost,
                    _ => 0m
                };

                // Додаткова плата за крихкий вміст
                if (parcel.ContentType == ContentType.Fragile)
                    baseCost += DeliveryConfiguration.FragileSurcharge;

                // Вартість страхування
                if (parcel.IsInsured)
                    baseCost += parcel.InsuranceValue * DeliveryConfiguration.InsuranceRate;

                return baseCost;
            }
            catch (Exception)
            {
                return 0m;
            }
        }

        /// <summary>
        /// Розраховує імпортне мито для міжнародних посилок
        /// </summary>
        public decimal CalculateImportTax(Parcel parcel)
        {
            try
            {
                if (parcel == null || parcel.Type != ParcelType.International)
                    return 0m;

                // Конвертація оціночної вартості в євро
                decimal valueInEuro = parcel.DeclaredValue / DeliveryConfiguration.EuroToUahRate;

                // Якщо вартість перевищує поріг, нараховується мито
                if (valueInEuro > DeliveryConfiguration.CustomsThresholdEuro)
                    return parcel.DeclaredValue * DeliveryConfiguration.CustomsTaxRate;

                return 0m;
            }
            catch (Exception)
            {
                return 0m;
            }
        }

        /// <summary>
        /// Застосовує знижку клієнта до суми
        /// </summary>
        public decimal ApplyDiscount(decimal amount, Client client, bool useDiscount)
        {
            try
            {
                if (client == null || !useDiscount || !client.CanUseDiscount())
                    return amount;

                decimal discount = client.GetDiscount();

                // Спеціальна знижка для легендарних клієнтів у святковий період
                if (client.IsLegend() && IsHolidayPeriod())
                    discount = DeliveryConfiguration.LegendHolidayDiscount;

                return amount * (1 - discount);
            }
            catch (Exception)
            {
                return amount;
            }
        }

        /// <summary>
        /// Перевіряє, чи зараз святковий період
        /// </summary>
        private bool IsHolidayPeriod()
        {
            var today = DateTime.Now;
            return (today.Month == DeliveryConfiguration.HolidayStartMonth &&
                    today.Day >= DeliveryConfiguration.HolidayStartDay) ||
                   (today.Month == DeliveryConfiguration.HolidayEndMonth &&
                    today.Day <= DeliveryConfiguration.HolidayEndDay);
        }

        /// <summary>
        /// Розраховує фінальну вартість доставки з усіма доплатами та знижками
        /// </summary>
        public decimal CalculateFinalPrice(Parcel parcel, Client sender, bool useDiscount)
        {
            try
            {
                if (parcel == null || sender == null)
                    return 0m;

                if (parcel.IsFreeDelivery)
                    return 0m;

                decimal cost = CalculateDeliveryCost(parcel);
                decimal tax = CalculateImportTax(parcel);
                decimal total = cost + tax;

                return ApplyDiscount(total, sender, useDiscount);
            }
            catch (Exception)
            {
                return 0m;
            }
        }
    }
}