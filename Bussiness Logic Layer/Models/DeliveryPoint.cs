using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    /// <summary>
    /// Модель точки доставки
    /// </summary>
    public class DeliveryPoint
    {
        /// <summary>Унікальний ідентифікатор точки</summary>
        public int Id { get; set; }

        /// <summary>Тип точки доставки</summary>
        public DeliveryType Type { get; set; }

        /// <summary>Адреса точки</summary>
        public string Address { get; set; }

        /// <summary>Поштовий індекс</summary>
        public string PostalCode { get; set; }

        /// <summary>Назва організації</summary>
        public string OrganizationName { get; set; }

        /// <summary>
        /// Конструктор за замовчуванням
        /// </summary>
        public DeliveryPoint() { }

        /// <summary>
        /// Конструктор для створення нової точки доставки
        /// </summary>
        public DeliveryPoint(DeliveryType type, string address, string postalCode, string orgName = null)
        {
            Type = type;
            Address = address;
            PostalCode = postalCode;
            OrganizationName = orgName;
        }
    }
}
