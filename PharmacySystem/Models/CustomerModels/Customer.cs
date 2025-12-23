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
        public int Age { get; set; }
        public string Phone { get; set; } = string.Empty;
        public Address Address { get; set; }

        public Customer()
        {
            Address = new Address();
        }
    }
}
