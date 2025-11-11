using System;
using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    public class DeliveryPoint
    {
        public int Id { get; set; }
        public DeliveryType Type { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string OrganizationName { get; set; }

        public DeliveryPoint() { }

        public DeliveryPoint(DeliveryType type, string address, string postalCode, string orgName = null)
        {
            Type = type;
            Address = address;
            PostalCode = postalCode;
            OrganizationName = orgName;
        }
    }
}