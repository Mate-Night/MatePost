using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    /// <summary>
    /// Модель клієнта з системою лояльності та бонусами
    /// </summary>
    public class Client
    {
        /// <summary>Унікальний ідентифікатор клієнта</summary>
        public int Id { get; set; }

        /// <summary>Повне ім'я клієнта</summary>
        public string FullName { get; set; }

        /// <summary>Номер телефону</summary>
        public string Phone { get; set; }

        /// <summary>Електронна пошта</summary>
        public string Email { get; set; }

        /// <summary>Адреса клієнта</summary>
        public string Address { get; set; }

        /// <summary>Тип клієнта (фізична особа/організація)</summary>
        public ClientType Type { get; set; }

        /// <summary>Статус лояльності</summary>
        public LoyaltyStatus Status { get; set; }

        /// <summary>Дата останнього використання знижки</summary>
        public DateTime? LastDiscountUsed { get; set; }

        /// <summary>Дата останнього використання безкоштовної доставки</summary>
        public DateTime? LastFreeDelivery { get; set; }

        /// <summary>
        /// Конструктор за замовчуванням
        /// </summary>
        public Client() { }

        /// <summary>
        /// Конструктор для створення нового клієнта
        /// </summary>
        public Client(string fullName, string phone, string email, string address, ClientType type)
        {
            FullName = fullName;
            Phone = phone;
            Email = email;
            Address = address;
            Type = type;
            Status = LoyaltyStatus.Beginner;
        }

        /// <summary>
        /// Повертає розмір знижки на основі статусу лояльності
        /// </summary>
        public decimal GetDiscount()
        {
            return Status switch
            {
                LoyaltyStatus.Beginner => DeliveryConfiguration.BeginnerDiscount,
                LoyaltyStatus.Active => DeliveryConfiguration.ActiveDiscount,
                LoyaltyStatus.Pro => DeliveryConfiguration.ProDiscount,
                LoyaltyStatus.Legend => DeliveryConfiguration.LegendDiscount,
                _ => 0m
            };
        }

        /// <summary>
        /// Перевіряє чи може клієнт використати знижку (раз на місяць)
        /// </summary>
        public bool CanUseDiscount()
        {
            if (!LastDiscountUsed.HasValue) return true;
            return (DateTime.Now - LastDiscountUsed.Value).TotalDays >= 30;
        }

        /// <summary>
        /// Відмічає використання знижки, оновлює дату
        /// </summary>
        public void UseDiscount()
        {
            LastDiscountUsed = DateTime.Now;
        }

        /// <summary>
        /// Перевіряє чи може клієнт використати безкоштовну доставку (раз на рік для Легенд)
        /// </summary>
        public bool CanUseFreeDelivery()
        {
            if (!IsLegend()) return false;
            if (!LastFreeDelivery.HasValue) return true;

            return (DateTime.Now - LastFreeDelivery.Value).TotalDays >= 365;
        }

        /// <summary>
        /// Відмічає використання безкоштовної доставки
        /// </summary>
        public void UseFreeDelivery()
        {
            LastFreeDelivery = DateTime.Now;
        }

        /// <summary>
        /// Оновлює статус лояльності на основі кількості відправлених посилок
        /// </summary>
        public void UpdateLoyaltyStatus(int parcelCount)
        {
            Status = parcelCount switch
            {
                >= DeliveryConfiguration.LegendStatusThreshold => LoyaltyStatus.Legend,
                >= DeliveryConfiguration.ProStatusThreshold => LoyaltyStatus.Pro,
                >= DeliveryConfiguration.ActiveStatusThreshold => LoyaltyStatus.Active,
                _ => LoyaltyStatus.Beginner
            };
        }

        /// <summary>
        /// Перевіряє чи є клієнт "Легендою доставки"
        /// </summary>
        public bool IsLegend() => Status == LoyaltyStatus.Legend;
    }
}