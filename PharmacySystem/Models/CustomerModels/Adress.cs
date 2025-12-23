using System;
using System.Xml.Linq;

namespace PharmacySystem.Models.CustomerModels
{
    /// <summary>
    /// Представляет полный адрес проживания клиента.
    /// </summary>
    /// <summary>
    /// Адрес проживания или доставки.
    /// </summary>
    public class Address
    {
        public string Region { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string House { get; set; } = string.Empty;
        public string Apartment { get; set; } = string.Empty;

        public Address() { }

        public Address(string redion, string city, string street, string house, string apartment = "")
        {
            City = city;
            Street = street;
            House = house;
            Apartment = apartment;
        }

        public override string ToString() => $"{City}, {Street}, {House}, кв. {Apartment}";

        public XElement ToXElement() => new XElement("Address",
            new XElement("Region", Region), new XElement("City", City),
            new XElement("Street", Street), new XElement("House", House),
            new XElement("Apartment", Apartment));

        public static Address FromXElement(XElement x) => x == null ? new Address() : new Address(
            x.Element("Region")?.Value, x.Element("City")?.Value,
            x.Element("Street")?.Value, x.Element("House")?.Value,
            x.Element("Apartment")?.Value);
    }
}
