using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PharmacySystem.Models;
using PharmacySystem.ORMLogistics;

namespace PharmacySystem.ViewModels
{
    public partial class InventoryWindow : Window
    {
        private readonly WarehouseRepository _warehouse;

        public InventoryWindow(WarehouseRepository warehouse)
        {
            InitializeComponent();
            _warehouse = warehouse;
            LoadInventoryData();
        }

        private void LoadInventoryData()
        {
            var rawData = _warehouse.GetAll();
            var displayData = rawData.Select(c => new InventoryRowViewModel(c)).ToList();

            InventoryDataGrid.ItemsSource = displayData;
            TxtTotalCount.Text = displayData.Count.ToString();
            TxtCriticalCount.Text = displayData.Count(x => x.IsCritical).ToString();
        }

        // Логика докупки конкретного товара
        private void RestockItem_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var viewModel = button?.DataContext as InventoryRowViewModel;

            if (viewModel != null)
            {
                // Логика: Добавляем 50 единиц
                var component = _warehouse.GetAll().FirstOrDefault(c => c.Id == viewModel.SourceId);
                if (component != null)
                {
                    component.CurrentStock += 50;
                    _warehouse.Save(component); // Сохраняем в XML

                    LoadInventoryData(); // Перезагружаем интерфейс
                    MessageBox.Show($"{component.InnName} restocked by 50 units.", "Stock Update");
                }
            }
        }

        // Логика автоматической докупки всех дефицитных позиций
        private void RestockAll_Click(object sender, RoutedEventArgs e)
        {
            var criticalItems = _warehouse.GetAll().Where(c => c.IsStockCritical).ToList();

            if (!criticalItems.Any())
            {
                MessageBox.Show("No critical items found.", "Inventory Info");
                return;
            }

            foreach (var item in criticalItems)
            {
                // Пополняем до двойной критической нормы
                item.CurrentStock += (item.CriticalNorm * 2);
                _warehouse.Save(item);
            }

            LoadInventoryData();
            MessageBox.Show("All critical items have been restocked.", "Bulk Operation Complete");
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadInventoryData();
    }

    public class InventoryRowViewModel
    {
        public Guid SourceId { get; set; }
        public string InnName { get; set; }
        public int CurrentStock { get; set; }
        public string MeasureUnit { get; set; }
        public int CriticalNorm { get; set; }
        public decimal PricePerUnit { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsCritical { get; set; }
        public TypeOfMeds FormType { get; set; }

        public string StatusText => IsCritical ? "LOW STOCK" : "OK";
        public Brush StatusColor => IsCritical
            ? (Brush)new BrushConverter().ConvertFrom("#EA4335")
            : (Brush)new BrushConverter().ConvertFrom("#34A853");

        public InventoryRowViewModel(MedicalComponent c)
        {
            SourceId = c.Id;
            InnName = c.InnName;
            CurrentStock = c.CurrentStock;
            MeasureUnit = c.MeasureUnit;
            CriticalNorm = c.CriticalNorm;
            PricePerUnit = c.PricePerUnit;
            ExpirationDate = c.ExpirationDate;
            IsCritical = c.IsStockCritical;

            // --- КОПИРОВАНИЕ ЗНАЧЕНИЯ ---
            FormType = c.FormType;
        }
    }
}