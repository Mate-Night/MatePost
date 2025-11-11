using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    public class Parcel
    {
        public string TrackingNumber { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public ParcelType Type { get; set; }
        public ContentType ContentType { get; set; }
        public double Weight { get; set; }
        public decimal DeclaredValue { get; set; }
        public ParcelStatus CurrentStatus { get; set; }
        public CourierService Service { get; set; }
        public DeliveryType DeliveryType { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<StatusChange> StatusHistory { get; set; }
        public string SenderCountry { get; set; }
        public string ReceiverCountry { get; set; }
        public decimal InsuranceValue { get; set; }
        public bool IsInsured { get; set; }
        public int EstimatedDeliveryDays { get; set; }
        public List<Notification> Notifications { get; set; }

        public Parcel()
        {
            StatusHistory = new List<StatusChange>();
            Notifications = new List<Notification>();
        }

        public bool RequiresOperatorConfirmation()
        {
            return DeclaredValue > 5000m;
        }

        public void AddNotification(ParcelStatus status, string note = "", DelayReason? delayReason = null, int? delayDays = null)
        {
            var notification = new Notification(status, note)
            {
                DelayReason = delayReason,
                DelayDays = delayDays
            };
            Notifications.Add(notification);
        }

        public void AddStatusChange(ParcelStatus status, string note = "")
        {
            StatusHistory.Add(new StatusChange(status, note));
        }
    }
}