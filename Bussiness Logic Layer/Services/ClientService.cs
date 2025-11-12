using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    /// <summary>
    /// Сервіс для управління клієнтами
    /// </summary>
    public class ClientService
    {
        // Список всіх клієнтів у системі
        private List<Client> _clients;

        // Лічильник для генерації унікальних ідентифікаторів
        private int _nextId = 1;

        /// <summary>
        /// Конструктор ініціалізує порожній список клієнтів
        /// </summary>
        public ClientService()
        {
            _clients = new List<Client>();
        }

        /// <summary>
        /// Повертає всіх клієнтів
        /// </summary>
        public List<Client> GetAll() => _clients;

        /// <summary>
        /// Знаходить клієнта за ідентифікатором
        /// </summary>
        public Client GetById(int id) => _clients.FirstOrDefault(c => c.Id == id);

        /// <summary>
        /// Додає нового клієнта до системи з валідацією
        /// </summary>
        public OperationResult Add(string fullName, string phone, string email, string address, ClientType type)
        {
            try
            {
                // Валідація ПІБ
                if (string.IsNullOrWhiteSpace(fullName))
                    return OperationResult.Fail("ПІБ не може бути порожнім");
                // Валідація телефону
                if (string.IsNullOrWhiteSpace(phone))
                    return OperationResult.Fail("Телефон не може бути порожнім");
                // Валідація email
                if (string.IsNullOrWhiteSpace(email))
                    return OperationResult.Fail("Email не може бути порожнім");
                // Валідація адреси
                if (string.IsNullOrWhiteSpace(address))
                    return OperationResult.Fail("Адреса не може бути порожньою");
                // Перевірка унікальності телефону
                if (_clients.Any(c => c.Phone == phone))
                    return OperationResult.Fail("Клієнт з таким телефоном вже існує");
                // Створення нового клієнта з унікальним ID
                var client = new Client(fullName, phone, email, address, type)
                {
                    Id = _nextId++
                };

                // Додавання до списку
                _clients.Add(client);
                return OperationResult.Ok(client);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при додаванні клієнта: {ex.Message}");
            }
        }

        /// <summary>
        /// Оновлює дані існуючого клієнта
        /// </summary>
        public OperationResult Update(Client client)
        {
            try
            {
                // Перевірка, чи клієнт не null
                if (client == null)
                    return OperationResult.Fail("Клієнт не може бути null");

                // Пошук існуючого клієнта
                var existing = GetById(client.Id);
                if (existing == null)
                    return OperationResult.Fail("Клієнта не знайдено");

                // Оновлення всіх полів клієнта
                existing.FullName = client.FullName;
                existing.Phone = client.Phone;
                existing.Email = client.Email;
                existing.Address = client.Address;
                existing.Type = client.Type;
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при оновленні клієнта: {ex.Message}");
            }
        }

        /// <summary>
        /// Видаляє клієнта за ідентифікатором
        /// </summary>

        public OperationResult Delete(int id)
        {
            try
            {
                // Пошук клієнта
                var client = GetById(id);
                if (client == null)
                    return OperationResult.Fail("Клієнта не знайдено");

                // Видалення зі списку
                _clients.Remove(client);
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Помилка при видаленні клієнта: {ex.Message}");
            }
        }

        /// <summary>
        /// Шукає клієнтів за запитом (ПІБ, телефон, email, адреса)
        /// </summary>
        public List<Client> Search(string query)
        {
            try
            {
                // Якщо запит порожній, повертаємо всіх клієнтів
                if (string.IsNullOrWhiteSpace(query))
                    return _clients;

                // Пошук без урахування регістру
                query = query.ToLower();
                return _clients.Where(c =>
                    c.FullName.ToLower().Contains(query) ||
                    c.Phone.Contains(query) ||
                    c.Email.ToLower().Contains(query) ||
                    c.Address.ToLower().Contains(query)
                ).ToList();
            }
            catch (Exception)
            {
                // У разі помилки повертаємо порожній список
                return new List<Client>();
            }
        }

        /// <summary>
        /// Завантажує список клієнтів 
        /// </summary>
        public void LoadClients(List<Client> clients)
        {
            // Заміна поточного списку на новий
            _clients = clients ?? new List<Client>();

            // Оновлення лічильника ID на наступний після максимального
            _nextId = _clients.Any() ? _clients.Max(c => c.Id) + 1 : 1;
        }
    }
}