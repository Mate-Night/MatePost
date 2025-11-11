using System;

namespace BusinessLogicLayer.Models
{
    public class StatusChange
    {
        public ParcelStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
        public string Note { get; set; }

        public StatusChange() { }

        public StatusChange(ParcelStatus status, string note = "")
        {
            Status = status;
            Timestamp = DateTime.Now;
            Note = note;
        }
    }
}