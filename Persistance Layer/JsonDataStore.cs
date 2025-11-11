using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BusinessLogicLayer.Models;

namespace PersistenceLayer
{
    public class JsonDataStore
    {
        private readonly string _dataDirectory;
        private readonly JsonSerializerOptions _options;

        public JsonDataStore(string dataDirectory = "Data")
        {
            _dataDirectory = dataDirectory;
            _options = new JsonSerializerOptions { WriteIndented = true };

            if (!Directory.Exists(_dataDirectory))
                Directory.CreateDirectory(_dataDirectory);
        }

        public void SaveClients(List<Client> clients)
        {
            var json = JsonSerializer.Serialize(clients, _options);
            File.WriteAllText(Path.Combine(_dataDirectory, "clients.json"), json);
        }

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

        public void SaveParcels(List<Parcel> parcels)
        {
            var json = JsonSerializer.Serialize(parcels, _options);
            File.WriteAllText(Path.Combine(_dataDirectory, "parcels.json"), json);
        }

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

        public void SaveOperators(List<Operator> operators)
        {
            var json = JsonSerializer.Serialize(operators, _options);
            File.WriteAllText(Path.Combine(_dataDirectory, "operators.json"), json);
        }

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

        public void SaveDeliveryPoints(List<DeliveryPoint> points)
        {
            var json = JsonSerializer.Serialize(points, _options);
            File.WriteAllText(Path.Combine(_dataDirectory, "delivery_points.json"), json);
        }

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