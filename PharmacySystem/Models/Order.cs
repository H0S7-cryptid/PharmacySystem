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
