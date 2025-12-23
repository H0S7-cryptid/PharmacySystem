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
    /// Репозиторий Справочника технологий.
    /// Хранит стандартизированные методы изготовления лекарственных форм на основе экспертных данных.
    /// </summary>
    public class TechnologyRepository
    {
        private readonly string _filePath;
        private const string RootName = "TechnologyHandbook";

        public TechnologyRepository(string filePath)
        {
            _filePath = filePath;
            InitializeFile();
            // Если файл пуст, можно вызвать метод первичного наполнения на основе отчета
            if (GetAll().Count == 0) SeedInitialData();
        }

        private void InitializeFile()
        {
            if (!File.Exists(_filePath))
            {
                new XDocument(new XElement(RootName)).Save(_filePath);
            }
        }

        #region Сериализация (Маппинг)

        private XElement Serialize(TechnologyEntry entry)
        {
            return new XElement("Technology",
                new XAttribute("Id", entry.Id),
                new XElement("Name", entry.MedicineName),
                new XElement("Category", entry.MedicationType.ToString()),
                new XElement("TechName", entry.TechName),
                new XElement("MethodSteps", entry.PreparationMethod),
                new XElement("ProdTime", entry.StandardProductionTime.TotalDays)
            );
        }

        private TechnologyEntry Deserialize(XElement el)
        {
            return new TechnologyEntry
            {
                Id = Guid.Parse(el.Attribute("Id")?.Value ?? Guid.NewGuid().ToString()),
                MedicineName = el.Element("Name")?.Value ?? string.Empty,
                MedicationType = (TypeOfMeds)Enum.Parse(typeof(TypeOfMeds), el.Element("Category")?.Value ?? "UNDEF"),
                TechName = el.Element("TechName")?.Value ?? string.Empty,
                PreparationMethod = el.Element("MethodSteps")?.Value ?? string.Empty,
                StandardProductionTime = TimeSpan.FromDays(double.Parse(el.Element("ProdTime")?.Value ?? "0"))
            };
        }

        #endregion

        #region CRUD операции

        public void Save(TechnologyEntry entry)
        {
            var doc = XDocument.Load(_filePath);
            var existing = doc.Root.Elements("Technology")
                .FirstOrDefault(e => e.Attribute("Id")?.Value == entry.Id.ToString());

            if (existing != null) existing.ReplaceWith(Serialize(entry));
            else doc.Root.Add(Serialize(entry));

            doc.Save(_filePath);
        }

        public List<TechnologyEntry> GetAll()
        {
            return XDocument.Load(_filePath)
                .Root.Elements("Technology")
                .Select(Deserialize)
                .ToList();
        }

        public TechnologyEntry GetByType(TypeOfMeds type)
        {
            return GetAll().FirstOrDefault(t => t.MedicationType == type);
        }

        #endregion

        /// <summary>
        /// Первичное наполнение справочника на основе экспертного отчета от 16.12.2025.
        /// Содержит только сухие технические данные.
        /// </summary>
        private void SeedInitialData()
        {
            var initialData = new List<TechnologyEntry>
            {
                new TechnologyEntry {
                    MedicationType = TypeOfMeds.Powders,
                    MedicineName = "Порошки",
                    TechName = "Диспергирование и Гомогенизация",
                    PreparationMethod = "1. Сушка сырья; \n2. Измельчение (шаровые/молотковые мельницы); \n3. Просеивание (фракционирование); \n4. Смешивание (тритурация по принципу 'от меньшего к большему'); \n5. Дозирование.",
                    StandardProductionTime = TimeSpan.FromDays(1)
                },
                new TechnologyEntry {
                    MedicationType = TypeOfMeds.Solutions,
                    MedicineName = "Растворы",
                    TechName = "Массо-объемное растворение",
                    PreparationMethod = "1. Подготовка растворителя (дистилляция/фильтрация); \n2. Растворение (термостат/перемешивание); \n3. Солюбилизация; \n4. Мембранная фильтрация (0.22 мкм); \n5. Стандартизация pH.",
                    StandardProductionTime = TimeSpan.FromDays(3)
                },
                new TechnologyEntry {
                    MedicationType = TypeOfMeds.Mixtures,
                    MedicineName = "Микстуры",
                    TechName = "Компаундирование гетерогенных сред",
                    PreparationMethod = "1. Изготовление концентратов; \n2. Смешивание водных фаз; \n3. Добавление настоек/сиропов; \n4. Введение летучих веществ; \n5. Эмульгирование/Суспендирование; \n6. Процеживание.",
                    StandardProductionTime = TimeSpan.FromDays(2)
                },
                new TechnologyEntry {
                    MedicationType = TypeOfMeds.Tinctures,
                    MedicineName = "Настойки",
                    TechName = "Экстракция (Мацерация/Перколяция)",
                    PreparationMethod = "1. Измельчение растительного сырья (3-5 мм); \n2. Замачивание; \n3. Перколяция (непрерывный проток экстрагента); \n4. Отстаивание (t=8-10°C); \n5. Фильтрация; 6. Спиртометрия.",
                    StandardProductionTime = TimeSpan.FromDays(2)
                },
                new TechnologyEntry {
                    MedicationType = TypeOfMeds.Ointments,
                    MedicineName = "Мази",
                    TechName = "Диспергирование в вязкой среде",
                    PreparationMethod = "1. Сплавление основы; \n2. Введение веществ (растворение/эмульгирование); \n3. Диспергирование нерастворимых фаз; \n4. Гомогенизация (РПА); \n5. Контролируемое охлаждение.",
                    StandardProductionTime = TimeSpan.FromDays(2)
                },
                new TechnologyEntry {
                    MedicationType = TypeOfMeds.Tablets,
                    MedicineName = "Таблетки",
                    TechName = "Таблетирование (Грануляция)",
                    PreparationMethod = "1. Смешивание API с эксципиентами; \n2. Влажная грануляция (увлажнение/протирка); \n3. Сушка гранулята; \n4. Прессование (таблетпресс); \n5. Нанесение пленочной оболочки.",
                    StandardProductionTime = TimeSpan.FromMinutes(0)
                }
            };

            foreach (var item in initialData) Save(item);
        }
    }
}
