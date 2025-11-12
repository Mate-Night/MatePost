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
        private List<Client> _clients;
        private int _nextId = 1;

        // Посилання на ParcelService для підрахунку посилок
        private ParcelService _parcelService;

        /// <summary>
        /// Конструктор ініціалізує порожній список клієнтів
        /// </summary>
        public ClientService()
        {
            _clients = new List<Client>();
        }

        /// <summary>
        /// Встановлює посилання на ParcelService для динамічного підрахунку
        /// </summary>
        public void SetParcelService(ParcelService parcelService)
        {
            _parcelService = parcelService;
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
        /// Динамічно підраховує кількість посилок клієнта
        /// </summary>
        public int GetClientParcelCount(int clientId)
        {
            if (_parcelService == null)
                return 0;

            return _parcelService.GetAll()
                .Count(p => p.SenderId == clientId);
        }

        /// <summary>
        /// Оновлює статус лояльності клієнта на основі кількості посилок
        /// </summary>
        public void UpdateClientLoyaltyStatus(int clientId)
        {
            var client = GetById(clientId);
            if (client == null)
                return;

            int parcelCount = GetClientParcelCount(clientId);
            client.UpdateLoyaltyStatus(parcelCount);
        }

        /// <summary>
        /// Додає нового клієнта до системи з валідацією
        /// </summary>
        public OperationResult Add(string fullName, string phone, string email, string address, ClientType type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName))
                    return OperationResult.Fail("ПІБ не може бути порожнім");

                if (string.IsNullOrWhiteSpace(phone))
                    return OperationResult.Fail("Телефон не може бути порожнім");

                if (string.IsNullOrWhiteSpace(email))
                    return OperationResult.Fail("Email не може бути порожнім");

                if (string.IsNullOrWhiteSpace(address))
                    return OperationResult.Fail("Адреса не може бути порожньою");

                if (_clients.Any(c => c.Phone == phone))
                    return OperationResult.Fail("Клієнт з таким телефоном вже існує");

                var client = new Client(fullName, phone, email, address, type)
                {
                    Id = _nextId++
                };

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
                if (client == null)
                    return OperationResult.Fail("Клієнт не може бути null");

                var existing = GetById(client.Id);
                if (existing == null)
                    return OperationResult.Fail("Клієнта не знайдено");

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
                var client = GetById(id);
                if (client == null)
                    return OperationResult.Fail("Клієнта не знайдено");

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
                if (string.IsNullOrWhiteSpace(query))
                    return _clients;

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
                return new List<Client>();
            }
        }

        /// <summary>
        /// Завантажує список клієнтів
        /// </summary>
        public void LoadClients(List<Client> clients)
        {
            _clients = clients ?? new List<Client>();
            _nextId = _clients.Any() ? _clients.Max(c => c.Id) + 1 : 1;
        }
    }
}