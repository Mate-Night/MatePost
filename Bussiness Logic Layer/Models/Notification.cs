using System;

namespace BusinessLogicLayer.Models
{
    public class Notification
    {
        public DateTime Timestamp { get; set; }
        public ParcelStatus Status { get; set; }
        public string Note { get; set; }
        public DelayReason? DelayReason { get; set; }
        public int? DelayDays { get; set; }

        public Notification() { }

        public Notification(ParcelStatus status, string note = "")
        {
            Timestamp = DateTime.Now;
            Status = status;
            Note = note;
        }
    }
}