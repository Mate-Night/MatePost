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

        /// <summary>Загальна кількість відправлених посилок</summary>
        public int TotalParcels { get; set; }

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
            TotalParcels = 0;
        }

        /// <summary>
        /// Повертає розмір знижки на основі статусу лояльності
        /// </summary>
        public decimal GetDiscount()
        {
            return Status switch
            {
                LoyaltyStatus.Beginner => 0.05m,// 5%
                LoyaltyStatus.Active => 0.10m, // 10%
                LoyaltyStatus.Pro => 0.15m,// 15%
                LoyaltyStatus.Legend => 0.20m,// 20%
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

            // Перевіряємо чи минув рік з моменту останнього використання
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
        /// Збільшує лічильник посилок та оновлює статус лояльності
        /// </summary>
        public void IncrementParcels()
        {
            TotalParcels++;
            UpdateLoyaltyStatus();
        }

        /// <summary>
        /// Оновлює статус лояльності на основі кількості відправлених посилок
        /// </summary>
        public void UpdateLoyaltyStatus()
        {
            Status = TotalParcels switch
            {
                >= 200 => LoyaltyStatus.Legend,// 200+ посилок
                >= 51 => LoyaltyStatus.Pro,// 51-200 посилок
                >= 11 => LoyaltyStatus.Active, // 11-50 посилок
                _ => LoyaltyStatus.Beginner // 0-10 посилок
            };
        }

        /// <summary>
        /// Перевіряє чи є клієнт "Легендою доставки"
        /// </summary>
        public bool IsLegend() => Status == LoyaltyStatus.Legend;
    }
}