using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BusinessLogicLayer.Models;
using System.Text.Encodings.Web; // encoder
using System.Text.Unicode;

namespace PersistenceLayer
{
    /// <summary>
    /// Клас для збереження та завантаження даних у форматі JSON
    /// </summary>
    public class JsonDataStore
    {
        private readonly string _dataDirectory;
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Конструктор сховища даних
        /// </summary>
        public JsonDataStore(string dataDirectory = "Data")
        {
            _dataDirectory = dataDirectory;
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };

            // Створюємо директорію якщо вона не існує
            if (!Directory.Exists(_dataDirectory))
                Directory.CreateDirectory(_dataDirectory);
        }

        /// <summary>
        /// Зберігає список клієнтів у файл clients.json
        /// </summary>
        public void SaveClients(List<Client> clients)
        {
            var json = JsonSerializer.Serialize(clients, _options);
            File.WriteAllText(Path.Combine(_dataDirectory, "clients.json"), json);
        }

        /// <summary>
        /// Завантажує список клієнтів з файлу clients.json
        /// </summary>
        public List<Client> LoadClients()
        {
            var path = Path.Combine(_dataDirectory, "clients.json");
            if (!File.Exists(path)) return new List<Client>();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<Client>>(json) ?? new List<Client>();
            }
            catch
            {
                return new List<Client>();
            }
        }

        /// <summary>
        /// Зберігає список посилок у файл parcels.json
        /// </summary>
        public void SaveParcels(List<Parcel> parcels)
        {
            var json = JsonSerializer.Serialize(parcels, _options);
            File.WriteAllText(Path.Combine(_dataDirectory, "parcels.json"), json);
        }

        /// <summary>
        /// Завантажує список посилок з файлу parcels.json
        /// </summary>
        public List<Parcel> LoadParcels()
        {
            var path = Path.Combine(_dataDirectory, "parcels.json");
            if (!File.Exists(path)) return new List<Parcel>();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<Parcel>>(json) ?? new List<Parcel>();
            }
            catch
            {
                return new List<Parcel>();
            }
        }

        /// <summary>
        /// Зберігає список операторів у файл operators.json
        /// </summary>
        public void SaveOperators(List<Operator> operators)
        {
            var json = JsonSerializer.Serialize(operators, _options);
            File.WriteAllText(Path.Combine(_dataDirectory, "operators.json"), json);
        }

        /// <summary>
        /// Завантажує список операторів з файлу operators.json
        /// </summary>
        public List<Operator> LoadOperators()
        {
            var path = Path.Combine(_dataDirectory, "operators.json");
            if (!File.Exists(path)) return new List<Operator>();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<Operator>>(json) ?? new List<Operator>();
            }
            catch
            {
                return new List<Operator>();
            }
        }

        /// <summary>
        /// Зберігає список точок доставки у файл delivery_points.json
        /// </summary>
        public void SaveDeliveryPoints(List<DeliveryPoint> points)
        {
            var json = JsonSerializer.Serialize(points, _options);
            File.WriteAllText(Path.Combine(_dataDirectory, "delivery_points.json"), json);
        }

        /// <summary>
        /// Завантажує список точок доставки з файлу delivery_points.json
        /// </summary>
        public List<DeliveryPoint> LoadDeliveryPoints()
        {
            var path = Path.Combine(_dataDirectory, "delivery_points.json");
            if (!File.Exists(path)) return new List<DeliveryPoint>();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<DeliveryPoint>>(json) ?? new List<DeliveryPoint>();
            }
            catch
            {
                return new List<DeliveryPoint>();
            }
        }
    }
}