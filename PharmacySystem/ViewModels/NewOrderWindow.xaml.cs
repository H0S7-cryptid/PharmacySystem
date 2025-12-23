using PharmacySystem.Models;
using PharmacySystem.Models.CustomerModels;
using PharmacySystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PharmacySystem.ViewModels
{
    public partial class NewOrderWindow : Window
    {
        private readonly OrderProcessingService _orderService;
        // Храним список добавленных ингредиентов
        private List<ProductIngredient> _prescribedItems = new List<ProductIngredient>();
        // Вспомогательный список для отображения в таблице (включает метаданные)
        private List<IngredientDisplayModel> _displayItems = new List<IngredientDisplayModel>();

        public NewOrderWindow(string dbPath)
        {
            InitializeComponent();
            _orderService = new OrderProcessingService(dbPath);
            LoadInitialData();
        }

        private void LoadInitialData()
        {
            // Заполняем формы выпуска для рецепта и для новых компонентов
            var forms = Enum.GetValues(typeof(TypeOfMeds))
                            .Cast<TypeOfMeds>()
                            .Where(t => t != TypeOfMeds.UNDEF)
                            .ToList();

            CmbFormType.ItemsSource = forms;
            CmbFormType.SelectedIndex = 0;

            CmbNewCompFormType.ItemsSource = forms;
            CmbNewCompFormType.SelectedIndex = 0;

            CmbComponents.ItemsSource = _orderService.GetAvailableComponents();
        }

        private void BtnAddIngredient_Click(object sender, RoutedEventArgs e)
        {
            MedicalComponent component = null;

            if (RbSelectExisting.IsChecked == true)
            {
                component = CmbComponents.SelectedItem as MedicalComponent;
                if (component == null)
                {
                    MessageBox.Show("Пожалуйста, выберите компонент из списка.");
                    return;
                }
            }
            else
            {
                string newName = TxtNewCompName.Text.Trim();
                bool isPriceOk = decimal.TryParse(TxtNewCompPrice.Text.Replace('.', ','), out decimal newPrice);

                if (!string.IsNullOrEmpty(newName) && isPriceOk)
                {
                    component = new MedicalComponent
                    {
                        Id = Guid.NewGuid(),
                        InnName = newName,
                        PricePerUnit = newPrice,
                        CurrentStock = 0, // На складе нет
                        MeasureUnit = "ед.",
                        FormType = (TypeOfMeds)CmbNewCompFormType.SelectedItem
                    };
                }
                else
                {
                    MessageBox.Show("Для нового компонента укажите корректное название и цену.");
                    return;
                }
            }

            // Валидация количества и добавление в таблицу
            if (int.TryParse(TxtIngrQty.Text, out int qty) && qty > 0)
            {
                _prescribedItems.Add(new ProductIngredient(component.Id, qty));

                _displayItems.Add(new IngredientDisplayModel
                {
                    Name = component.InnName,
                    Type = component.FormType,
                    QtyPerOne = qty,
                    Price = component.PricePerUnit // Добавили цену в модель отображения
                });

                RefreshIngredientsGrid();

                // Очистка только полей ручного ввода
                TxtIngrQty.Clear();
                TxtNewCompName.Clear();
                TxtNewCompPrice.Clear();
            }
            else
            {
                MessageBox.Show("Введите корректное количество (целое число больше 0).");
            }
        }

        
        private bool ValidateNewComponentInput(out decimal price)
        {
            price = 0;
            if (string.IsNullOrWhiteSpace(TxtNewCompName.Text)) return false;
            return decimal.TryParse(TxtNewCompPrice.Text, out price);
        }

        private void RefreshIngredientsGrid()
        {
            GridIngredients.ItemsSource = null;
            GridIngredients.ItemsSource = _displayItems;
        }

        private void ClearIngredientInputs()
        {
            TxtIngrQty.Clear();
            TxtNewCompName.Clear();
            TxtNewCompPrice.Clear();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_prescribedItems.Any())
            {
                MessageBox.Show("Добавьте хотя бы один ингредиент в состав.");
                return;
            }

            try
            {
                var customer = new Customer
                {
                    FullName = TxtPatientName.Text,
                    Age = int.TryParse(TxtAge.Text, out int age) ? age : 0,
                    Phone = TxtPhone.Text,
                    Address = new Address("", TxtCity.Text, TxtStreet.Text, TxtHouseApt.Text)
                };

                var recipe = new Recipe
                {
                    PatientFullName = customer.FullName,
                    PatientPhoneNumber = customer.Phone,
                    MedicationName = TxtMedName.Text,
                    FormType = (TypeOfMeds)CmbFormType.SelectedItem, // Выбранная форма выпуска
                    PrescribedIngredients = _prescribedItems,
                    TotalQuantity = int.TryParse(TxtTotalQty.Text, out int tQty) ? tQty : 1
                };

                string resultMessage = _orderService.ProcessOrder(customer, recipe);
                MessageBox.Show(resultMessage, "Заказ оформлен");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения заказа: " + ex.Message);
            }
        }

        // Вспомогательный класс для отображения в таблице
        public class IngredientDisplayModel
        {
            public string Name { get; set; }
            public TypeOfMeds Type { get; set; }
            public int QtyPerOne { get; set; }
            public decimal Price { get; set; } // Новое свойство
        }
    }
}