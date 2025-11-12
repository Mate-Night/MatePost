using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    /// <summary>
    /// Модель посилки з усіма параметрами та історією статусів
    /// </summary>
    public class Parcel
    {
        /// <summary>Унікальний трекінг-номер для відстеження посилки</summary>
        public string TrackingNumber { get; set; }

        /// <summary>ID клієнта-відправника</summary>
        public int SenderId { get; set; }

        /// <summary>ID клієнта-одержувача</summary>
        public int ReceiverId { get; set; }

        /// <summary>Тип посилки (локальна/міжнародна)</summary>
        public ParcelType Type { get; set; }

        /// <summary>Тип вмісту (документ/посилка/крихке)</summary>
        public ContentType ContentType { get; set; }

        /// <summary>Вага посилки в кілограмах</summary>
        public double Weight { get; set; }

        /// <summary>Оціночна вартість вмісту</summary>
        public decimal DeclaredValue { get; set; }

        /// <summary>Поточний статус посилки</summary>
        public ParcelStatus CurrentStatus { get; set; }

        /// <summary>Кур'єрська служба</summary>
        public CourierService Service { get; set; }

        /// <summary>Тип доставки</summary>
        public DeliveryType DeliveryType { get; set; }

        /// <summary>Дата створення посилки</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Історія всіх змін статусів</summary>
        public List<StatusChange> StatusHistory { get; set; }

        /// <summary>Країна відправника</summary>
        public string SenderCountry { get; set; }

        /// <summary>Країна одержувача</summary>
        public string ReceiverCountry { get; set; }

        /// <summary>Страхова вартість</summary>
        public decimal InsuranceValue { get; set; }

        /// <summary>Чи застрахована посилка</summary>
        public bool IsInsured { get; set; }

        /// <summary>Орієнтовний термін доставки в днях</summary>
        public int EstimatedDeliveryDays { get; set; }

        /// <summary>Список повідомлень для клієнта</summary>
        public List<Notification> Notifications { get; set; }

        /// <summary>Пріоритетна обробка для клієнтів зі статусом "Легенда"</summary>
        public bool IsPriorityProcessing { get; set; }

        /// <summary>Безкоштовна доставка (бонус для "Легенди")</summary>
        public bool IsFreeDelivery { get; set; }

        /// <summary>
        /// Конструктор за замовчуванням
        /// </summary>
        public Parcel()
        {
            StatusHistory = new List<StatusChange>();
            Notifications = new List<Notification>();
        }

        /// <summary>
        /// Перевіряє чи потребує посилка підтвердження оператора (вартість > 5000 грн)
        /// </summary>
        public bool RequiresOperatorConfirmation()
        {
            return DeclaredValue > 5000m;
        }

        /// <summary>
        /// Додає нове повідомлення для клієнта про статус посилки
        /// </summary>
        public void AddNotification(ParcelStatus status, string note = "", DelayReason? delayReason = null, int? delayDays = null)
        {
            var notification = new Notification(status, note)
            {
                DelayReason = delayReason,
                DelayDays = delayDays
            };
            Notifications.Add(notification);
        }

        /// <summary>
        /// Додає запис про зміну статусу в історію
        /// </summary>
        public void AddStatusChange(ParcelStatus status, string note = "")
        {
            StatusHistory.Add(new StatusChange(status, note));
        }
    }
}