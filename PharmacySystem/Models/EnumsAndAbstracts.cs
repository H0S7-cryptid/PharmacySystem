using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PharmacySystem.Models
{
    /// <summary>
    /// Базовый класс для всех объектов, имеющих уникальный идентификатор.
    /// Позволяет однозначно отличать объекты, даже если их имена совпадают.
    /// </summary>
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        protected XAttribute GetIdAttribute() => new XAttribute("Id", Id.ToString());
    }

    /// <summary>
    /// Статус жизненного цикла заказа (от обращения клиента до выдачи).
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Не хватает ингредиентов. Заказ в "Листе ожидания".
        /// </summary>
        PendingIngredients,

        /// <summary>
        /// Ингредиенты есть, зарезервированы. Ожидает оплаты или начала работы.
        /// </summary>
        Reserved,

        /// <summary>
        /// Находится в процессе изготовления (в справочнике заказов в производстве).
        /// </summary>
        InProduction,

        /// <summary>
        /// Лекарство готово (или собрано), ждет клиента.
        /// </summary>
        Ready,

        /// <summary>
        /// Заказ выдан клиенту (архив).
        /// </summary>
        Issued
    }

    /// <summary>
    /// Тип лекарственной формы.
    /// </summary>
    public enum TypeOfMeds
    {
        Tablets,    // Таблетки
        Ointments,  // Мази
        Mixtures,   // Микстуры
        Solutions,  // Растворы
        Tinctures,  // Настойки
        Powders,    // Порошки
        UNDEF = -1
    }
}