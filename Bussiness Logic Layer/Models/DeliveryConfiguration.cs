using System;

namespace BusinessLogicLayer.Models
{
    /// <summary>
    /// Конфігурація тарифів та констант системи доставки
    /// </summary>
    public static class DeliveryConfiguration
    {
        // === БАЗОВІ ТАРИФИ ===

        /// <summary>Базова вартість локальної доставки (грн)</summary>
        public const decimal LocalBaseCost = 50m;

        /// <summary>Базова вартість міжнародної доставки (грн)</summary>
        public const decimal InternationalBaseCost = 200m;

        /// <summary>Вартість за 1 кг ваги (грн)</summary>
        public const decimal PricePerKg = 10m;

        // === ДОПЛАТИ ЗА ТИП ДОСТАВКИ ===

        /// <summary>Доплата за доставку у відділення (грн)</summary>
        public const decimal OfficeDeliveryCost = 0m;

        /// <summary>Доплата за доставку у поштомат (грн)</summary>
        public const decimal ParcelboxDeliveryCost = 20m;

        /// <summary>Доплата за адресну доставку (грн)</summary>
        public const decimal AddressDeliveryCost = 50m;

        /// <summary>Доплата за доставку таксі (грн)</summary>
        public const decimal TaxiDeliveryCost = 150m;

        // === ДОДАТКОВІ ПОСЛУГИ ===

        /// <summary>Доплата за крихкий вміст (грн)</summary>
        public const decimal FragileSurcharge = 30m;

        /// <summary>Відсоток страхування від оціночної вартості</summary>
        public const decimal InsuranceRate = 0.02m;

        // === МИТНІ ЗБОРИ ===

        /// <summary>Поріг безмитного ввезення (євро)</summary>
        public const decimal CustomsThresholdEuro = 150m;

        /// <summary>Курс євро до гривні</summary>
        public const decimal EuroToUahRate = 41m;

        /// <summary>Відсоток митного збору</summary>
        public const decimal CustomsTaxRate = 0.10m;

        // === СИСТЕМА ЗНИЖОК ===

        /// <summary>Знижка для початківців (5%)</summary>
        public const decimal BeginnerDiscount = 0.05m;

        /// <summary>Знижка для активних клієнтів (10%)</summary>
        public const decimal ActiveDiscount = 0.10m;

        /// <summary>Знижка для професіоналів (15%)</summary>
        public const decimal ProDiscount = 0.15m;

        /// <summary>Знижка для легенд (20%)</summary>
        public const decimal LegendDiscount = 0.20m;

        /// <summary>Святкова знижка для легенд (35%)</summary>
        public const decimal LegendHolidayDiscount = 0.35m;

        // === СВЯТКОВИЙ ПЕРІОД ===

        /// <summary>Місяць початку святкового періоду</summary>
        public const int HolidayStartMonth = 12;

        /// <summary>День початку святкового періоду</summary>
        public const int HolidayStartDay = 20;

        /// <summary>Місяць закінчення святкового періоду</summary>
        public const int HolidayEndMonth = 1;

        /// <summary>День закінчення святкового періоду</summary>
        public const int HolidayEndDay = 7;

        // === СИСТЕМА ЛОЯЛЬНОСТІ ===

        /// <summary>Мінімум посилок для статусу "Активний"</summary>
        public const int ActiveStatusThreshold = 11;

        /// <summary>Мінімум посилок для статусу "Про"</summary>
        public const int ProStatusThreshold = 51;

        /// <summary>Мінімум посилок для статусу "Легенда"</summary>
        public const int LegendStatusThreshold = 200;

        // === ОБМЕЖЕННЯ ВАГИ ===

        /// <summary>Максимальна вага для поштомату (кг)</summary>
        public const double MaxWeightParcelbox = 30.0;

        /// <summary>Максимальна вага для відділення (кг)</summary>
        public const double MaxWeightOffice = 100.0;

        /// <summary>Максимальна вага для адресної доставки (кг)</summary>
        public const double MaxWeightAddress = 50.0;

        /// <summary>Максимальна вага для таксі (кг)</summary>
        public const double MaxWeightTaxi = 20.0;
    }
}