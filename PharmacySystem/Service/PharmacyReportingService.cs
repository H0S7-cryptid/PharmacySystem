using PharmacySystem.Data;
using PharmacySystem.Models;
using PharmacySystem.Models.Logs;
using PharmacySystem.ORMLogistics;
using PharmacySystem.ORMLogistics.Logs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PharmacySystem.Service
{

    /// <summary>
    /// Главный контроллер аналитики и управления.
    /// Агрегирует данные из всех репозиториев для предоставления отчетности.
    /// </summary>
    public class PharmacyAnalyticsController
    {
        private readonly WarehouseRepository _warehouse;
        private readonly WaitingListRepository _waitingList;
        private readonly IssuedJournalRepository _issuedJournal;
        private readonly TechnologyRepository _technologies;

        public PharmacyAnalyticsController(
            WarehouseRepository warehouse,
            WaitingListRepository waitingList,
            IssuedJournalRepository issuedJournal,
            TechnologyRepository technologies)
        {
            _warehouse = warehouse;
            _waitingList = waitingList;
            _issuedJournal = issuedJournal;
            _technologies = technologies;
        }

        #region БЛОК 1 -- Работа с медикаментами и складом.

        /// <summary>
        /// Формирует отчет о медикаментах, запасы которых достигли критической отметки или исчерпаны.
        /// </summary>
        /// <returns>Список компонентов, требующих немедленного дозаказа</returns>
        public List<MedicalComponent> GetCriticalStockReport()
        {
            List<MedicalComponent> allComponents = _warehouse.GetAll();

            // Условие: Текущий запас (CurrentStock) меньше или равен Критической норме (CriticalNorm)
            // Мы также включаем сюда позиции, которые полностью отсутствуют (CurrentStock == 0)
            var criticalItems = allComponents
                .Where(c => c.CurrentStock <= c.CriticalNorm)
                .OrderBy(c => c.CurrentStock)
                .ToList();

            return criticalItems;
        }

        /// <summary>
        /// Собирает полную информацию о конкретном компоненте из всех реестров.
        /// </summary>
        /// <param name="componentName">Название медикамента (МНН)</param>
        public MedicationCompleteProfile GetComponentInfo(string componentName)
        {
            // Поиск компонента на складе
            var component = _warehouse.GetAll().FirstOrDefault(c =>
                c.InnName.Equals(componentName, StringComparison.OrdinalIgnoreCase));

            if (component == null) return null;

            var profile = new MedicationCompleteProfile
            {
                StockInfo = component
            };

            profile.PendingDemands = _waitingList.GetAll()
                .Where(w => w.RequestedMedicationName.Contains(componentName))
                .ToList();

            var history = _issuedJournal.GetAll()
                .Where(h => h.MedicationName.Contains(componentName));

            profile.TimesUsedInIssuedOrders = history.Count();
            profile.TotalRevenueGenerated = history.Sum(h => h.FinalPrice);

            var relevantTypes = new HashSet<TypeOfMeds>();

            if (component.FormType != TypeOfMeds.UNDEF)
            {
                relevantTypes.Add(component.FormType);
            }

            foreach (var type in profile.PendingDemands.Select(d => d.MedicationCategory)) relevantTypes.Add(type);
            foreach (var type in history.Select(h => h.Category)) relevantTypes.Add(type);

            foreach (var type in relevantTypes)
            {
                var tech = _technologies.GetByType(type);
                if (tech != null)
                {
                    profile.ApplicableTechnologies.Add(tech);
                }
            }

            // Фоллбэк (если ничего не нашли)
            if (!profile.ApplicableTechnologies.Any())
            {
                if (component.MeasureUnit.ToLower() == "мл")
                    profile.ApplicableTechnologies.Add(_technologies.GetByType(TypeOfMeds.Solutions));
            }

            return profile;
        }

        /// <summary>
        /// Формирует отчет по компонентам, срок годности которых истекает к заданной дате.
        /// Позволяет проводить своевременную инвентаризацию и списание (согласно ТЗ).
        /// </summary>
        /// <param name="thresholdDate">Контрольная дата (все, что истекает ДО нее, попадет в отчет)</param>
        /// <returns>Список компонентов с истекающим сроком</returns>
        public List<MedicalComponent> GetExpiringComponents(DateTime thresholdDate)
        {
            List<MedicalComponent> allComponents = _warehouse.GetAll();

            var expiringItems = allComponents
                .Where(c => c.ExpirationDate <= thresholdDate)
                .OrderBy(c => c.ExpirationDate)
                .ToList();

            return expiringItems;
        }

        /// <summary>
        /// Перегрузка метода для быстрого получения списка позиций, 
        /// срок которых истекает в ближайшие 30 дней.
        /// </summary>
        public List<MedicalComponent> GetExpiringInNextMonth()
        {
            return GetExpiringComponents(DateTime.Now.AddMonths(1));
        }

        #endregion

        #region БЛОК 2 -- работа с пациентами.

        /// <summary>
        /// Получить список пациентов, чьи заказы уже готовы (согласно расчетам), 
        /// но не были забраны в установленный срок.
        /// </summary>
        public List<WaitingListEntry> GetOverdueUnclaimedOrders()
        {
            // Берем текущее время
            DateTime now = DateTime.Now;

            // Извлекаем все записи из листа ожидания
            // По ТЗ: заказ считается "не забранным вовремя", если текущая дата 
            // больше, чем EstimatedArrivalDate (дата предполагаемой выдачи).
            return _waitingList.GetAll()
                .Where(entry => entry.EstimatedArrivalDate < now)
                .OrderBy(entry => entry.EstimatedArrivalDate) // Самые "старые" заказы вверху
                .ToList();
        }

        /// <summary>
        /// Получить перечень покупателей, ожидающих прибытия медикаментов, 
        /// сгруппированных по конкретной категории (типу) лекарств.
        /// </summary>
        /// <param name="category">Тип лекарственной формы (Мази, Порошки и т.д.)</param>
        public List<WaitingListEntry> GetWaitingListByCategory(TypeOfMeds category)
        {
            // Фильтруем записи листа ожидания по совпадению категории
            var list = _waitingList.GetAll()
                .Where(entry => entry.MedicationCategory == category)
                .ToList();

            Console.WriteLine($"[INFO] По категории {category} ожидают {list.Count} чел.");

            return list;
        }

        #endregion

        #region БЛОК 3 -- аналитические методы для сбора данных о продаже.

        /// <summary>
        /// Подсчитывает суммарный объем (количество единиц/упаковок) конкретного лекарства, 
        /// выданного клиентам за указанный период.
        /// </summary>
        /// <param name="medicationName">Название препарата</param>
        /// <param name="start">Начало периода (включительно)</param>
        /// <param name="end">Конец периода (включительно)</param>
        /// <returns>Общее количество выданных единиц</returns>
        public int GetTotalVolumeIssued(string medicationName, DateTime start, DateTime end)
        {
            var issuedOrders = _issuedJournal.GetAll();

            // - Название должно совпадать (без учета регистра)
            // - Дата выдачи должна попадать в диапазон
            int totalVolume = issuedOrders
                .Where(order =>
                    order.MedicationName.Equals(medicationName, StringComparison.OrdinalIgnoreCase) &&
                    order.IssueDate >= start &&
                    order.IssueDate <= end)
                .Sum(order => order.Quantity);

            return totalVolume;
        }
        /// <summary>
        /// Возвращает список имен всех пациентов, которые заказывали указанное лекарство 
        /// в течение определенного года.
        /// </summary>
        /// <param name="medicationName">Название искомого лекарства</param>
        /// <param name="year">Год, за который проводится выборка</param>
        /// <returns>Список уникальных имен пациентов</returns>
        public List<string> GetPatientsByMedication(string medicationName, int year)
        {
            var finalResult = 
                _issuedJournal.GetAll()
                .Where(order =>
                    order.MedicationName.Equals(medicationName, StringComparison.OrdinalIgnoreCase) &&
                    order.IssueDate.Year == year)
                .Select(order => order.PatientFullName)
                .OrderBy(name => name) // Сортируем по алфавиту для удобства чтения
                .ToList()
                .Union(_waitingList.GetAll()
                        .Where(order =>
                            order.RequestedMedicationName.Equals(medicationName, StringComparison.OrdinalIgnoreCase) &&
                            order.EstimatedArrivalDate.Year == year)
                        .Select(order => order.CustomerFullName)
                        .OrderBy(name => name)
                        .ToList()).ToList();

            return finalResult;
        }
        /// <summary>
        /// Формирует рейтинг клиентов, которые чаще всего пользовались услугами аптеки.
        /// </summary>
        /// <param name="limit">Количество позиций в ТОПе (по умолчанию 10)</param>
        /// <returns>Словарь, где ключ — ФИО пациента, значение — общее количество его заказов</returns>
        public Dictionary<string, int> GetTopFrequentCustomers(int limit = 10)
        {
            var allIssuedOrders = _issuedJournal.GetAll();

            var topCustomers = allIssuedOrders
                .GroupBy(order => order.PatientFullName)
                // Создаем анонимный объект с именем и количеством
                .Select(group => new
                {
                    Name = group.Key,
                    Count = group.Count()
                })
                // Сортируем по убыванию (самые активные в начале)
                .OrderByDescending(x => x.Count)
                // Берем заданное количество результатов
                .Take(limit)
                // Преобразуем в словарь для удобства отображения на фронтенде
                .ToDictionary(x => x.Name, x => x.Count);

            return topCustomers;
        }

        #endregion

        #region БЛОК 4 -- работа с технологиями и производством.

        /// <summary>
        /// Возвращает технологическую карту для конкретной формы лекарства.
        /// </summary>
        public TechnologyEntry GetTechnologyCard(TypeOfMeds type)
        {
            return _technologies.GetByType(type);
        }

        /// <summary>
        /// Возвращает список заказов, которые в данный момент находятся на стадии изготовления.
        /// </summary>
        public List<WaitingListEntry> GetActiveProductionOrders()
        {
            return _waitingList.GetAll()
                .OrderBy(e => e.EstimatedArrivalDate)
                .ToList();
        }

        /// <summary>
        /// Рассчитывает полную себестоимость изготовления препарата по рецепту.
        /// Включает стоимость ингредиентов и стоимость работы по технологии.
        /// </summary>
        public decimal CalculateProductionCost(Recipe recipe)
        {
            decimal totalCost = 0;

            foreach (var ingredient in recipe.PrescribedIngredients)
            {
                var component = _warehouse.GetAll()
                    .FirstOrDefault(c => c.Id == ingredient.ComponentId);

                if (component != null)
                {
                    totalCost += component.PricePerUnit * ingredient.QuantityRequired * recipe.TotalQuantity;
                }
            }

            // Берем среднюю наценку за технологическую сложность формы
            var tech = _technologies.GetByType(recipe.FormType);
            if (tech != null)
            {
                // Для примера: 10 у.е. за каждые 30 минут работы
                decimal laborRate = 10m;
                totalCost += (decimal)(tech.StandardProductionTime.TotalMinutes / 30) * laborRate;
            }

            return Math.Round(totalCost, 2);
        }

        #endregion
    }
}