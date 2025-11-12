using System;

namespace BusinessLogicLayer.Models
{
    /// <summary>
    /// Модель повідомлення для клієнта про статус посилки
    /// </summary>
    public class Notification
    {
        /// <summary>Час створення повідомлення</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Статус посилки на момент повідомлення</summary>
        public ParcelStatus Status { get; set; }

        /// <summary>Текст повідомлення</summary>
        public string Note { get; set; }

        /// <summary>Причина затримки (якщо є)</summary>
        public DelayReason? DelayReason { get; set; }

        /// <summary>Кількість днів затримки</summary>
        public int? DelayDays { get; set; }

        /// <summary>
        /// Конструктор за замовчуванням
        /// </summary>
        public Notification() { }

        /// <summary>
        /// Конструктор для створення нового повідомлення
        /// </summary>
        public Notification(ParcelStatus status, string note = "")
        {
            Timestamp = DateTime.Now;
            Status = status;
            Note = note;
        }
    }
}