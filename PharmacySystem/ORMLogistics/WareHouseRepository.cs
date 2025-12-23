using PharmacySystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PharmacySystem.ORMLogistics
{
    /// <summary>
    /// Репозиторий для управления запасами компонентов (ингредиентов) на складе.
    /// Реализует логику хранения, обновления остатков и проверки критических норм.
    /// </summary>
    public class WarehouseRepository
    {
        private readonly string _filePath;
        private const string RootName = "Warehouse";

        public WarehouseRepository(string filePath)
        {
            _filePath = filePath;
            InitializeFile();
        }

        /// <summary>
        /// Создает файл, если он отсутствует.
        /// </summary>
        private void InitializeFile()
        {
            if (!File.Exists(_filePath))
            {
                new XDocument(new XElement(RootName)).Save(_filePath);
            }
        }

        #region Сериализация (Маппинг)

        private XElement Serialize(MedicalComponent item)
        {
            return new XElement("Component",
                new XAttribute("Id", item.Id),
                new XAttribute("Name", item.InnName ?? "Неизвестно"),
                new XElement("CurrentStock", item.CurrentStock),
                new XElement("CriticalNorm", item.CriticalNorm),
                new XElement("MeasureUnit", item.MeasureUnit ?? "ед."),
                new XElement("PricePerUnit", item.PricePerUnit),
                new XElement("ExpirationDate", item.ExpirationDate),
                // --- ДОБАВЛЕНО СОХРАНЕНИЕ ТИПА ---
                new XElement("FormType", item.FormType.ToString())
            );
        }

        private MedicalComponent Deserialize(XElement el)
        {
            string idStr = el.Attribute("Id")?.Value;
            string nameStr = el.Attribute("Name")?.Value;
            string dateStr = el.Element("ExpirationDate")?.Value;

            Enum.TryParse(el.Element("FormType")?.Value, out TypeOfMeds parsedType);
            if (!Enum.IsDefined(typeof(TypeOfMeds), parsedType))
                parsedType = TypeOfMeds.UNDEF;

            return new MedicalComponent
            {
                Id = Guid.TryParse(idStr, out Guid guidResult) ? guidResult : Guid.NewGuid(),

                InnName = nameStr ?? "Неизвестно",

                CurrentStock = int.TryParse(el.Element("CurrentStock")?.Value, out int stock) ? stock : 0,
                CriticalNorm = int.TryParse(el.Element("CriticalNorm")?.Value, out int norm) ? norm : 0,
                MeasureUnit = el.Element("MeasureUnit")?.Value ?? "ед.",

                PricePerUnit = decimal.TryParse(el.Element("PricePerUnit")?.Value?.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal price) ? price : 0m,

                ExpirationDate = DateTime.TryParse(dateStr, out DateTime dt) ? dt : DateTime.Now.AddYears(1),

                FormType = parsedType
            };
        }

        #endregion
        #region Основные операции (CRUD)

        /// <summary>
        /// Сохраняет новый компонент или обновляет существующий.
        /// </summary>
        public void Save(MedicalComponent component)
        {
            var doc = XDocument.Load(_filePath);
            var existing = doc.Root.Elements("Component")
                .FirstOrDefault(e => e.Attribute("Id")?.Value == component.Id.ToString());

            if (existing != null)
            {
                existing.ReplaceWith(Serialize(component));
            }
            else
            {
                doc.Root.Add(Serialize(component));
            }
            doc.Save(_filePath);
        }

        /// <summary>
        /// Возвращает полный список всех компонентов на складе.
        /// </summary>
        public List<MedicalComponent> GetAll()
        {
            return XDocument.Load(_filePath)
                .Root.Elements("Component")
                .Select(Deserialize)
                .ToList();
        }

        /// <summary>
        /// Удаляет компонент со склада по его ID.
        /// </summary>
        public void Delete(Guid id)
        {
            var doc = XDocument.Load(_filePath);
            doc.Root.Elements("Component")
                .FirstOrDefault(e => e.Attribute("Id")?.Value == id.ToString())
                ?.Remove();
            doc.Save(_filePath);
        }

        #endregion

        #region Специализированные методы (для запросов ТЗ)

        /// <summary>
        /// Находит компоненты, количество которых достигло критической нормы или закончилось.
        /// (Реализация Запроса №6 из ТЗ)
        /// </summary>
        public List<MedicalComponent> GetCriticalItems()
        {
            return GetAll().Where(c => c.CurrentStock <= c.CriticalNorm).ToList();
        }

        /// <summary>
        /// Уменьшает запас указанного вещества на складе.
        /// Используется при производстве лекарства.
        /// </summary>
        public bool TryConsume(Guid componentId, int amount)
        {
            var components = GetAll();
            var target = components.FirstOrDefault(c => c.Id == componentId);

            if (target == null || target.CurrentStock < amount)
                return false;

            target.CurrentStock -= amount;
            Save(target);
            return true;
        }

        /// <summary>
        /// Получает информацию о конкретном компоненте по названию (для Запроса №11).
        /// </summary>
        public MedicalComponent GetByName(string name)
        {
            return GetAll().FirstOrDefault(c =>
                c.InnName.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
