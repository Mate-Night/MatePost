using Microsoft.EntityFrameworkCore;
using BusinessLogicLayer.Models;
using IntegrationLayer.Database;

namespace IntegrationLayer.Services
{
    /// <summary>
    /// Сервіс для роботи з базою даних через Entity Framework
    /// </summary>
    public class DatabaseService
    {
        private readonly ApplicationDbContext _context;

        public DatabaseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Client>> GetAllClientsAsync()
        {
            return await _context.Clients.ToListAsync();
        }

        public async Task<Client?> GetClientByIdAsync(int id)
        {
            return await _context.Clients.FindAsync(id);
        }

        public async Task SaveClientAsync(Client client)
        {
            if (client.Id == 0)
                await _context.Clients.AddAsync(client);
            else
                _context.Clients.Update(client);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteClientAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Parcel>> GetAllParcelsAsync()
        {
            return await _context.Parcels
                .Include(p => p.StatusHistory)
                .Include(p => p.Notifications)
                .ToListAsync();
        }

        public async Task<Parcel?> GetParcelByTrackingNumberAsync(string trackingNumber)
        {
            return await _context.Parcels
                .Include(p => p.StatusHistory)
                .Include(p => p.Notifications)
                .FirstOrDefaultAsync(p => p.TrackingNumber == trackingNumber);
        }

        public async Task SaveParcelAsync(Parcel parcel)
        {
            var existing = await _context.Parcels
                .FirstOrDefaultAsync(p => p.TrackingNumber == parcel.TrackingNumber);

            if (existing == null)
                await _context.Parcels.AddAsync(parcel);
            else
                _context.Entry(existing).CurrentValues.SetValues(parcel);

            await _context.SaveChangesAsync();
        }

        public async Task<List<Operator>> GetAllOperatorsAsync()
        {
            return await _context.Operators.ToListAsync();
        }

        public async Task<Operator?> GetOperatorByIdAsync(int id)
        {
            return await _context.Operators.FindAsync(id);
        }

        public async Task SaveOperatorAsync(Operator op)
        {
            if (op.Id == 0)
                await _context.Operators.AddAsync(op);
            else
                _context.Operators.Update(op);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteOperatorAsync(int id)
        {
            var op = await _context.Operators.FindAsync(id);
            if (op != null)
            {
                _context.Operators.Remove(op);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<DeliveryPoint>> GetAllDeliveryPointsAsync()
        {
            return await _context.DeliveryPoints.ToListAsync();
        }

        public async Task<DeliveryPoint?> GetDeliveryPointByIdAsync(int id)
        {
            return await _context.DeliveryPoints.FindAsync(id);
        }

        public async Task SaveDeliveryPointAsync(DeliveryPoint point)
        {
            if (point.Id == 0)
                await _context.DeliveryPoints.AddAsync(point);
            else
                _context.DeliveryPoints.Update(point);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteDeliveryPointAsync(int id)
        {
            var point = await _context.DeliveryPoints.FindAsync(id);
            if (point != null)
            {
                _context.DeliveryPoints.Remove(point);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Міграція даних з JSON файлів до БД
        /// </summary>
        public async Task MigrateFromJsonAsync(
            List<Client> clients,
            List<Parcel> parcels,
            List<Operator> operators,
            List<DeliveryPoint> deliveryPoints)
        {
            try
            {
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();

                if (clients != null && clients.Count > 0)
                {
                    await _context.Clients.AddRangeAsync(clients);
                    await _context.SaveChangesAsync();
                }

                if (operators != null && operators.Count > 0)
                {
                    await _context.Operators.AddRangeAsync(operators);
                    await _context.SaveChangesAsync();
                }

                if (deliveryPoints != null && deliveryPoints.Count > 0)
                {
                    await _context.DeliveryPoints.AddRangeAsync(deliveryPoints);
                    await _context.SaveChangesAsync();
                }

                if (parcels != null && parcels.Count > 0)
                {
                    await _context.Parcels.AddRangeAsync(parcels);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка міграції: {ex.Message}", ex);
            }
        }
    }
}