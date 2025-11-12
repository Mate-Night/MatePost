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
                // Перевірка, чи посилка не null
                if (parcel == null)
                    throw new ArgumentNullException(nameof(parcel));

                // Базова вартість залежить від типу доставки (місцева/міжнародна)
                decimal baseCost = parcel.Type == ParcelType.Local ? 50m : 200m;

                // Додавання вартості за вагу (10 грн за кг)
                baseCost += (decimal)(parcel.Weight * 10);

                // Додавання вартості залежно від способу доставки
                baseCost += parcel.DeliveryType switch
                {
                    DeliveryType.Office => 0m,// У відділення - безкоштовно
                    DeliveryType.Parcelbox => 20m,// У поштомат - 20 грн
                    DeliveryType.Address => 50m,// За адресою - 50 грн
                    DeliveryType.Taxi => 150m,// Таксі - 150 грн
                    _ => 0m
                };

                // Додаткова плата за крихкий вміст
                if (parcel.ContentType == ContentType.Fragile)
                    baseCost += 30m;

                // Вартість страхування (2% від оціночної вартості)
                if (parcel.IsInsured)
                    baseCost += parcel.InsuranceValue * 0.02m;

                return baseCost;
            }
            catch (Exception)
            {
                // У разі помилки повертаємо 0
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
                // Мито застосовується тільки для міжнародних посилок
                if (parcel == null || parcel.Type != ParcelType.International)
                    return 0m;

                // Поріг безмитного ввезення - 150 євро
                const decimal euroThreshold = 150m;
                // Курс євро до гривні
                const decimal eurRate = 41m;

                // Конвертація оціночної вартості в євро
                decimal valueInEuro = parcel.DeclaredValue / eurRate;

                // Якщо вартість перевищує поріг, нараховується 10% мито
                if (valueInEuro > euroThreshold)
                    return parcel.DeclaredValue * 0.10m;

                return 0m;
            }
            catch (Exception)
            {
                // У разі помилки повертаємо 0
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
                // Перевірка умов для застосування знижки
                if (client == null || !useDiscount || !client.CanUseDiscount())
                    return amount;

                // Отримання базової знижки клієнта
                decimal discount = client.GetDiscount();

                // Спеціальна знижка для легендарних клієнтів у святковий період
                if (client.IsLegend() && IsHolidayPeriod())
                    discount = 0.35m;

                // Повернення суми зі знижкою
                return amount * (1 - discount);
            }
            catch (Exception)
            {
                // У разі помилки повертаємо початкову суму
                return amount;
            }
        }

        /// <summary>
        /// Перевіряє, чи зараз святковий період 
        /// </summary>
        private bool IsHolidayPeriod()
        {
            var today = DateTime.Now;
            // Святковий період: 20 грудня - 7 січня
            return (today.Month == 12 && today.Day >= 20) ||
                   (today.Month == 1 && today.Day <= 7);
        }

        /// <summary>
        /// Розраховує фінальну вартість доставки з усіма доплатами та знижками
        /// </summary>
        public decimal CalculateFinalPrice(Parcel parcel, Client sender, bool useDiscount)
        {
            try
            {
                // Перевірка вхідних параметрів
                if (parcel == null || sender == null)
                    return 0m;

                // Якщо доставка безкоштовна, повертаємо 0
                if (parcel.IsFreeDelivery)
                    return 0m;

                // Розрахунок базової вартості доставки
                decimal cost = CalculateDeliveryCost(parcel);

                // Розрахунок імпортного мита
                decimal tax = CalculateImportTax(parcel);

                // Загальна сума
                decimal total = cost + tax;

                // Застосування знижки та повернення фінальної ціни
                return ApplyDiscount(total, sender, useDiscount);
            }
            catch (Exception)
            {
                // У разі помилки повертаємо 0
                return 0m;
            }
        }
    }
}