using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Сервіс для управління операторами
    /// </summary>
    public class OperatorService
    {
        // Список всіх операторів у системі
        private List<Operator> _operators;

        // Лічильник для генерації унікальних ідентифікаторів
        private int _nextId = 1;

        /// <summary>
        /// Конструктор ініціалізує порожній список операторів
        /// </summary>
        public OperatorService()
        {
            _operators = new List<Operator>();
        }

        /// <summary>
        /// Повертає всіх операторів
        /// </summary>
        public List<Operator> GetAll() => _operators;

        /// <summary>
        /// Знаходить оператора за ідентифікатором
        /// </summary>
        public Operator GetById(int id) => _operators.FirstOrDefault(o => o.Id == id);

        /// <summary>
        /// Додає нового оператора
        /// </summary>
        public OperationResult Add(string name)
        {
            try
            {
                // Перевірка, чи ім'я не порожнє
                if (string.IsNullOrWhiteSpace(name))
                    return OperationResult.Fail("Ім'я оператора не може бути порожнім");

                // Створення нового оператора з унікальним ID
                var op = new Operator(name)
                {
                    Id = _nextId++
                };

                // Додавання до списку
                _operators.Add(op);
                return OperationResult.Ok(op);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при додаванні оператора: {ex.Message}");
            }
        }

        /// <summary>
        /// Видаляє оператора за ідентифікатором
        /// </summary>
        public OperationResult Delete(int id)
        {
            try
            {
                // Пошук оператора
                var op = GetById(id);
                if (op == null)
                    return OperationResult.Fail("Оператора не знайдено");

                // Видалення зі списку
                _operators.Remove(op);
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при видаленні оператора: {ex.Message}");
            }
        }

        /// <summary>
        /// Завантажує список операторів 
        /// </summary>
        public void LoadOperators(List<Operator> operators)
        {
            // Заміна поточного списку на новий
            _operators = operators ?? new List<Operator>();

            // Оновлення лічильника ID на наступний після максимального
            _nextId = _operators.Any() ? _operators.Max(o => o.Id) + 1 : 1;
        }
    }
}