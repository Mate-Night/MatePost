using System.Collections.Generic;
using System.Linq;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Services
{
    public class DeliveryPointService
    {
        private List<DeliveryPoint> _deliveryPoints;
        private int _nextId = 1;

        public DeliveryPointService()
        {
            _deliveryPoints = new List<DeliveryPoint>();
        }

        public List<DeliveryPoint> GetAll() => _deliveryPoints;

        public DeliveryPoint GetById(int id) => _deliveryPoints.FirstOrDefault(d => d.Id == id);

        public OperationResult Add(DeliveryType type, string address, string postalCode, string orgName = null)
        {
            var point = new DeliveryPoint(type, address, postalCode, orgName)
            {
                Id = _nextId++
            };
            _deliveryPoints.Add(point);
            return OperationResult.Ok(point);
        }

        public OperationResult Delete(int id)
        {
            var point = GetById(id);
            if (point == null) return OperationResult.Fail();

            _deliveryPoints.Remove(point);
            return OperationResult.Ok();
        }

        public void LoadDeliveryPoints(List<DeliveryPoint> points)
        {
            _deliveryPoints = points ?? new List<DeliveryPoint>();
            _nextId = _deliveryPoints.Any() ? _deliveryPoints.Max(d => d.Id) + 1 : 1;
        }
    }
}