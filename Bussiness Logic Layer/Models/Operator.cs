using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    /// <summary>
    /// Модель оператора поштової служби
    /// </summary>
    public class Operator
    {
        /// <summary>Унікальний ідентифікатор оператора</summary>
        public int Id { get; set; }

        /// <summary>Ім'я оператора</summary>
        public string Name { get; set; }

        /// <summary>Кількість оброблених посилок</summary>
        public int ProcessedParcels { get; set; }

        /// <summary>Ефективність роботи у відсотках</summary>
        public double Efficiency { get; set; }

        /// <summary>
        /// Конструктор за замовчуванням
        /// </summary>
        public Operator() { }

        /// <summary>
        /// Конструктор для створення нового оператора
        /// </summary>
        public Operator(string name)
        {
            Name = name;
            ProcessedParcels = 0;
            Efficiency = 100.0;
        }

        /// <summary>
        /// Збільшує лічильник оброблених посилок
        /// </summary>
        public void IncrementProcessed()
        {
            ProcessedParcels++;
        }
    }
}