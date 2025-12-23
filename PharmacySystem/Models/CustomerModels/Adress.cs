using System;
using System.Xml.Linq;

namespace PharmacySystem.Models.CustomerModels
{
    /// <summary>
    /// Адрес проживания или доставки.
    /// </summary>
    public class Address
    {
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string House { get; set; } = string.Empty;
        public string Apartment { get; set; } = string.Empty;

        public Address() { }
        
        public Address(string region, string city, string street, string house, string apartment = "")
        {
            City = city;
            Street = street;
            House = house;
            Apartment = apartment;
        }
    }
}
