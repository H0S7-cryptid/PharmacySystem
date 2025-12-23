using PharmacySystem.Models.CustomerModels;
using System;
using System.Xml.Linq;

namespace PharmacySystem.Models.CustomerModels
{
    /// <summary>
    /// Клиент аптеки.
    /// Содержит данные, необходимые для рецепта (ФИО, Возраст) и связи (Телефон).
    /// </summary>
    public class Customer : BaseEntity
    {
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; } // Важно для рецепта (ТЗ)
        public string Phone { get; set; } = string.Empty;
        public Address Address { get; set; }

        public Customer()
        {
            Address = new Address();
        }
        public XElement ToXElement() => new XElement("Customer",
            GetIdAttribute(),
            new XElement("FullName", FullName),
            new XElement("Phone", Phone),
            new XElement("Age", Age),
            Address.ToXElement());

        public static Customer FromXElement(XElement x) => new Customer
        {
            Id = Guid.Parse(x.Attribute("Id")?.Value ?? Guid.NewGuid().ToString()),
            FullName = x.Element("FullName")?.Value ?? "",
            Phone = x.Element("Phone")?.Value ?? "",
            Age = int.Parse(x.Element("Age")?.Value ?? "0"),
            Address = Address.FromXElement(x.Element("Address"))
        };
    }
}
