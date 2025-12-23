using PharmacySystem.Data;
using PharmacySystem.Models;
using PharmacySystem.Models.CustomerModels;
using PharmacySystem.Models.Logs;
using PharmacySystem.ORMLogistics;
using PharmacySystem.ORMLogistics.Logs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PharmacySystem.Services
{
    public class OrderProcessingService
    {
        private readonly WarehouseRepository _warehouse;
        private readonly TechnologyRepository _tech;
        private readonly WaitingListRepository _waiting;
        private readonly IssuedJournalRepository _issued;

        public OrderProcessingService(string dbPath)
        {
            _warehouse = new WarehouseRepository(dbPath + "warehouse.xml");
            _tech = new TechnologyRepository(dbPath + "technology.xml");
            _waiting = new WaitingListRepository(dbPath + "waiting.xml");
            _issued = new IssuedJournalRepository(dbPath + "issued.xml");
        }

        public List<MedicalComponent> GetAvailableComponents() => _warehouse.GetAll();

        public string ProcessOrder(Customer customer, Recipe recipe)
        {
            bool isAvailable = true;
            decimal totalCost = 0;

            // Проверка наличия и расчет
            foreach (var ingr in recipe.PrescribedIngredients)
            {
                var comp = _warehouse.GetAll().FirstOrDefault(c => c.Id == ingr.ComponentId);
                int totalNeeded = ingr.QuantityRequired * recipe.TotalQuantity;

                if (comp == null || comp.CurrentStock < totalNeeded)
                    isAvailable = false;
                else
                    totalCost += comp.PricePerUnit * totalNeeded;
            }

            if (isAvailable)
            {
                // Списание
                foreach (var ingr in recipe.PrescribedIngredients)
                    _warehouse.TryConsume(ingr.ComponentId, ingr.QuantityRequired * recipe.TotalQuantity);

                _issued.Add(new IssuedOrderEntry
                {
                    Id = Guid.NewGuid(),
                    PatientFullName = customer.FullName,
                    MedicationName = recipe.MedicationName,
                    Quantity = recipe.TotalQuantity,
                    FinalPrice = totalCost,
                    IssueDate = DateTime.Now
                });

                return $"Заказ успешно выдан! Итого: {totalCost} руб.";
            }
            else
            {
                // Расчет времени ожидания
                var techInfo = _tech.GetByType(recipe.FormType);
                DateTime estimatedDate = DateTime.Now.AddDays(3);

                if (techInfo != null && (techInfo.MedicationType == TypeOfMeds.Tablets || techInfo.MedicationType == TypeOfMeds.Tinctures))
                {
                    // Логика парсинга времени из вашего исходника
                    if (DateTime.TryParse(techInfo.StandardProductionTime.ToString(), out DateTime techTime))
                        estimatedDate = techTime.AddDays(3);
                }

                _waiting.Add(new WaitingListEntry
                {
                    Id = Guid.NewGuid(),
                    CustomerFullName = customer.FullName,
                    PhoneNumber = customer.Phone,
                    RequestedMedicationName = recipe.MedicationName,
                    MedicationCategory = recipe.FormType,
                    OrderDate = DateTime.Now,
                    EstimatedArrivalDate = estimatedDate,
                    DeliveryAddress = customer.Address
                });

                return "Недостаточно компонентов. Заказ добавлен в лист ожидания.";
            }
        }
    }
}