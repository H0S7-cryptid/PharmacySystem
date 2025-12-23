using PharmacySystem.Models.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Xml.Linq;

namespace PharmacySystem.Models
{
    /// <summary>
    /// Компонент (ингредиент) на складе.
    /// </summary>
    public class MedicalComponent : BaseEntity
    {
        /// <summary>
        /// Название вещества (МНН).
        /// </summary>
        public string InnName { get; set; } = string.Empty;

        /// <summary>
        /// Единица измерения (мг, мл, г, шт).
        /// </summary>
        public string MeasureUnit { get; set; } = "ед.";

        /// <summary>
        /// Критическая норма остатка. Если CurrentStock ниже этого значения -> нужно дозаказать.
        /// </summary>
        public int CriticalNorm { get; set; }

        /// <summary>
        /// Текущий остаток на складе.
        /// </summary>
        public int CurrentStock { get; set; }

        /// <summary>
        /// Закупочная цена за единицу измерения (нужна для расчета себестоимости).
        /// </summary>
        public decimal PricePerUnit { get; set; }

        /// <summary>
        /// Вспомогательное свойство: требуется ли пополнение запасов.
        /// </summary>
        public bool IsStockCritical => CurrentStock <= CriticalNorm;

        public DateTime ExpirationDate { get; set; }

        public TypeOfMeds FormType { get; set; } = TypeOfMeds.UNDEF;
    }

    /// <summary>
    /// Элемент формулы лекарства.
    /// Связывает ID компонента со склада с требуемым количеством.
    /// </summary>
    public class ProductIngredient
    {
        public Guid ComponentId { get; set; } // Ссылка на MedicalComponent
        public int QuantityRequired { get; set; } // Сколько единиц нужно на 1 порцию лекарства

        public ProductIngredient() { }

        public ProductIngredient(Guid componentId, int quantity)
        {
            ComponentId = componentId;
            QuantityRequired = quantity;
        }
    }

    /// <summary>
    /// Полное досье на медикамент (компонент).
    /// </summary>
    public class MedicationCompleteProfile
    {
        // Данные со склада (Сырье/Ингредиент)
        public MedicalComponent StockInfo { get; set; }

        // Данные из листа ожидания (кому мы его задолжали)
        public List<WaitingListEntry> PendingDemands { get; set; } = new List<WaitingListEntry>();

        // Аналитика использования
        public int TimesUsedInIssuedOrders { get; set; }
        public decimal TotalRevenueGenerated { get; set; }

        public List<TechnologyEntry> ApplicableTechnologies { get; set; } = new List<TechnologyEntry>();
    }
}