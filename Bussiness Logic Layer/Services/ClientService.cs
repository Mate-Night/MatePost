using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    public class ClientService
    {
        private List<Client> _clients;
        private int _nextId = 1;

        public ClientService()
        {
            _clients = new List<Client>();
        }

        public List<Client> GetAll() => _clients;

        public Client GetById(int id) => _clients.FirstOrDefault(c => c.Id == id);

        public OperationResult Add(string fullName, string phone, string email, string address, ClientType type)
        {
            var client = new Client(fullName, phone, email, address, type)
            {
                Id = _nextId++
            };
            _clients.Add(client);
            return OperationResult.Ok(client);
        }

        public OperationResult Update(Client client)
        {
            var existing = GetById(client.Id);
            if (existing == null) return OperationResult.Fail();

            existing.FullName = client.FullName;
            existing.Phone = client.Phone;
            existing.Email = client.Email;
            existing.Address = client.Address;
            existing.Type = client.Type;
            return OperationResult.Ok();
        }

        public OperationResult Delete(int id)
        {
            var client = GetById(id);
            if (client == null) return OperationResult.Fail();

            _clients.Remove(client);
            return OperationResult.Ok();
        }

        public List<Client> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return _clients;

            query = query.ToLower();
            return _clients.Where(c =>
                c.FullName.ToLower().Contains(query) ||
                c.Phone.Contains(query) ||
                c.Email.ToLower().Contains(query) ||
                c.Address.ToLower().Contains(query)
            ).ToList();
        }

        public void LoadClients(List<Client> clients)
        {
            _clients = clients ?? new List<Client>();
            _nextId = _clients.Any() ? _clients.Max(c => c.Id) + 1 : 1;
        }
    }
}