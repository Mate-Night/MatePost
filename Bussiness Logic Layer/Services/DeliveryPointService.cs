using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Сервіс для управління точками доставки
    /// </summary>
    public class DeliveryPointService
    {
        // Список всіх точок доставки у системі
        private List<DeliveryPoint> _deliveryPoints;

        // Лічильник для генерації унікальних ідентифікаторів
        private int _nextId = 1;

        /// <summary>
        /// Конструктор - ініціалізує порожній список точок доставки
        /// </summary>
        public DeliveryPointService()
        {
            _deliveryPoints = new List<DeliveryPoint>();
        }

        /// <summary>
        /// Повертає всі точки доставки
        /// </summary>
        public List<DeliveryPoint> GetAll() => _deliveryPoints;

        /// <summary>
        /// Знаходить точку доставки за ідентифікатором
        /// </summary>
        public DeliveryPoint GetById(int id) => _deliveryPoints.FirstOrDefault(d => d.Id == id);

        /// <summary>
        /// Додає нову точку доставки
        /// </summary>
        public OperationResult Add(DeliveryType type, string address, string postalCode, string orgName = null)
        {
            try
            {
                // Перевірка, чи адреса не порожня
                if (string.IsNullOrWhiteSpace(address))
                    return OperationResult.Fail("Адреса не може бути порожньою");

                // Перевірка, чи поштовий індекс не порожній
                if (string.IsNullOrWhiteSpace(postalCode))
                    return OperationResult.Fail("Поштовий індекс не може бути порожнім");

                // Створення нової точки доставки з унікальним ID
                var point = new DeliveryPoint(type, address, postalCode, orgName)
                {
                    Id = _nextId++
                };

                // Додавання до списку
                _deliveryPoints.Add(point);
                return OperationResult.Ok(point);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при додаванні точки доставки: {ex.Message}");
            }
        }

        /// <summary>
        /// Видаляє точку доставки за ідентифікатором
        /// </summary>
        public OperationResult Delete(int id)
        {
            try
            {
                // Пошук точки доставки
                var point = GetById(id);
                if (point == null)
                    return OperationResult.Fail("Точку доставки не знайдено");

                // Видалення зі списку
                _deliveryPoints.Remove(point);
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при видаленні точки доставки: {ex.Message}");
            }
        }

        /// <summary>
        /// Завантажує список точок доставки (наприклад, з файлу)
        /// </summary>
        public void LoadDeliveryPoints(List<DeliveryPoint> points)
        {
            // Заміна поточного списку на новий
            _deliveryPoints = points ?? new List<DeliveryPoint>();

            // Оновлення лічильника ID на наступний після максимального
            _nextId = _deliveryPoints.Any() ? _deliveryPoints.Max(d => d.Id) + 1 : 1;
        }
    }
}