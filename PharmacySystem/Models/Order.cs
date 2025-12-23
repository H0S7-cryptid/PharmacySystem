using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PharmacySystem.Models
{
    /// <summary>
    /// Заказ клиента. Центральная сущность, связывающая клиента, рецепт и склад.
    /// </summary>
    public class Order : BaseEntity
    {
        // --- Связи (Foreign Keys) ---
        public Guid CustomerId { get; set; }
        public Guid MedicalProductId { get; set; }

        // --- Данные из рецепта ---
        public string DoctorFullName { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;

        /// <summary>
        /// Количество порций/упаковок лекарства.
        /// </summary>
        public int Quantity { get; set; } = 1;

        // --- Управление состоянием ---
        public OrderStatus Status { get; set; } = OrderStatus.PendingIngredients;

        // --- Временные метки ---

        /// <summary>
        /// Дата приема рецепта/заказа.
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Планируемая дата готовности.
        /// Если статус PendingIngredients - это примерная дата поставки сырья.
        /// Если InProduction - это CreatedDate + ProductionTime лекарства.
        /// </summary>
        public DateTime? EstimatedReadyDate { get; set; }

        /// <summary>
        /// Фактическая дата выдачи на руки (заполняется при смене статуса на Issued).
        /// </summary>
        public DateTime? ActualIssueDate { get; set; }

        // --- Финансы ---

        /// <summary>
        /// Итоговая цена для клиента. 
        /// Рассчитывается как (Стоимость ингредиентов * Qty) + LaborCost.
        /// </summary>
        public decimal TotalPrice { get; set; }

        public XElement ToXElement() => new XElement("Order",
            GetIdAttribute(),
            new XElement("CustomerId", CustomerId),
            new XElement("ProductId", MedicalProductId),
            new XElement("Doctor", DoctorFullName),
            new XElement("Diagnosis", Diagnosis),
            new XElement("Quantity", Quantity),
            new XElement("Status", Status),
            new XElement("CreatedDate", CreatedDate),
            new XElement("EstimatedDate", EstimatedReadyDate?.ToString() ?? ""),
            new XElement("IssueDate", ActualIssueDate?.ToString() ?? ""),
            new XElement("TotalPrice", TotalPrice)
        );

        public static Order FromXElement(XElement x) => new Order
        {
            Id = Guid.Parse(x.Attribute("Id")?.Value),
            CustomerId = Guid.Parse(x.Element("CustomerId").Value),
            MedicalProductId = Guid.Parse(x.Element("ProductId").Value),
            DoctorFullName = x.Element("Doctor")?.Value,
            Diagnosis = x.Element("Diagnosis")?.Value,
            Quantity = int.Parse(x.Element("Quantity")?.Value ?? "1"),
            Status = (OrderStatus)Enum.Parse(typeof(OrderStatus), x.Element("Status").Value),
            CreatedDate = DateTime.Parse(x.Element("CreatedDate").Value),
            EstimatedReadyDate = string.IsNullOrEmpty(x.Element("EstimatedDate")?.Value) ? (DateTime?)null : DateTime.Parse(x.Element("EstimatedDate").Value),
            ActualIssueDate = string.IsNullOrEmpty(x.Element("IssueDate")?.Value) ? (DateTime?)null : DateTime.Parse(x.Element("IssueDate").Value),
            TotalPrice = decimal.Parse(x.Element("TotalPrice")?.Value ?? "0")
        };
    }

    public class ProductionOrderEntry : BaseEntity
    {
        public Guid OrderId { get; set; }
        public string MedicationName { get; set; } = string.Empty;

        // Статус в производстве (ТЗ требует пометки, если не все компоненты есть)
        public bool IsPausedDueToComponents { get; set; }

        public DateTime StartProductionTime { get; set; }
        public DateTime TargetReadyTime { get; set; } // Время, к которому больной должен прийти

        // Список зарезервированных компонентов (для запроса №9 из ТЗ)
        public List<string> ReservedComponentsNames { get; set; } = new List<string>();
    }
}
