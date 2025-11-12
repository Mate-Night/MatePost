using System;

namespace BusinessLogicLayer.Models
{
    /// <summary>
    /// Модель запису про зміну статусу посилки
    /// </summary>
    public class StatusChange
    {
        /// <summary>Новий статус посилки</summary>
        public ParcelStatus Status { get; set; }

        /// <summary>Час зміни статусу</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>Додаткова примітка до зміни</summary>
        public string Note { get; set; }

        /// <summary>
        /// Конструктор за замовчуванням
        /// </summary>
        public StatusChange() { }

        /// <summary>
        /// Конструктор для створення нового запису про зміну статусу
        /// </summary>
        public StatusChange(ParcelStatus status, string note = "")
        {
            Status = status;
            Timestamp = DateTime.Now;
            Note = note;
        }
    }
}
