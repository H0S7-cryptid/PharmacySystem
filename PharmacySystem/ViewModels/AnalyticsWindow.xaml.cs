using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PharmacySystem.Models;
using PharmacySystem.Service;

namespace PharmacySystem.ViewModels
{
    public partial class FullAnalyticsWindow : Window
    {
        private readonly PharmacyAnalyticsController _controller;

        public FullAnalyticsWindow(PharmacyAnalyticsController controller)
        {
            InitializeComponent();
            _controller = controller;

            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            // 1. Установка дат по умолчанию
            DpExpiryThreshold.SelectedDate = DateTime.Now.AddMonths(1);
            DpVolStart.SelectedDate = DateTime.Now.AddMonths(-1);
            DpVolEnd.SelectedDate = DateTime.Now;

            // 2. Заполнение ComboBox для категорий из Enum
            CmbWaitingCategory.ItemsSource = Enum.GetValues(typeof(TypeOfMeds))
                                                 .Cast<TypeOfMeds>()
                                                 .Where(t => t != TypeOfMeds.UNDEF);
            CmbWaitingCategory.SelectedIndex = 0;

            // 3. Текущий год
            TxtPatYear.Text = DateTime.Now.Year.ToString();
        }

        // --- БЛОК 1: СКЛАД ---

        private void BtnRefreshCritical_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = _controller.GetCriticalStockReport();
                DgCritical.ItemsSource = data;
                if (data.Count == 0) ShowInfo("No critical items found.");
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnShowExpiring_Click(object sender, RoutedEventArgs e)
        {
            if (DpExpiryThreshold.SelectedDate == null)
            {
                ShowInfo("Please select a date threshold.");
                return;
            }

            try
            {
                var data = _controller.GetExpiringComponents(DpExpiryThreshold.SelectedDate.Value);
                DgExpiring.ItemsSource = data;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnNextMonthExpiry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = _controller.GetExpiringInNextMonth();
                DgExpiring.ItemsSource = data;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        // --- БЛОК 2: ЛИСТ ОЖИДАНИЯ ---

        private void BtnFilterWaiting_Click(object sender, RoutedEventArgs e)
        {
            if (CmbWaitingCategory.SelectedItem == null) return;

            try
            {
                TypeOfMeds type = (TypeOfMeds)CmbWaitingCategory.SelectedItem;
                var data = _controller.GetWaitingListByCategory(type);
                DgWaiting.ItemsSource = data;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnOverdue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = _controller.GetOverdueUnclaimedOrders();
                DgOverdue.ItemsSource = data;
                if (data.Count == 0) ShowInfo("Great! No overdue orders found.");
            }
            catch (Exception ex) { ShowError(ex); }
        }

        // --- БЛОК 3: АНАЛИТИКА ПРОДАЖ ---

        private void BtnCalcVolume_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtVolName.Text)) { ShowInfo("Enter Medication Name."); return; }
            if (DpVolStart.SelectedDate == null || DpVolEnd.SelectedDate == null) { ShowInfo("Select valid dates."); return; }

            try
            {
                string name = TxtVolName.Text;
                DateTime start = DpVolStart.SelectedDate.Value;
                DateTime end = DpVolEnd.SelectedDate.Value;

                int count = _controller.GetTotalVolumeIssued(name, start, end);

                // Создаем список анонимных объектов для отображения в Grid
                DgVolume.ItemsSource = new[] {
                    new {
                        Medication = name,
                        Count = count,
                        Start = start.ToShortDateString(),
                        End = end.ToShortDateString()
                    }
                };
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnFindPatients_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtPatMedName.Text)) { ShowInfo("Enter Name."); return; }
            if (!int.TryParse(TxtPatYear.Text, out int year)) { ShowInfo("Enter valid Year."); return; }

            try
            {
                var patients = _controller.GetPatientsByMedication(TxtPatMedName.Text, year);
                // Проекция строки в объект для биндинга
                DgPatients.ItemsSource = patients.Select(p => new { Name = p }).ToList();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void BtnTopClients_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtTopLimit.Text, out int limit)) limit = 10;

            try
            {
                // Метод возвращает Dictionary<string, int>, DataGrid отлично биндится к KeyValuePair
                var top = _controller.GetTopFrequentCustomers(limit);
                DgTopClients.ItemsSource = top;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        // --- БЛОК 4: ПРОФИЛЬ КОМПОНЕНТА ---

        private void BtnCompProfile_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtProfileName.Text;
            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                var profile = _controller.GetComponentInfo(name);

                if (profile == null)
                {
                    ShowInfo("Component not found in Warehouse.");
                    ClearProfile();
                    return;
                }

                // Заполнение UI из сложного объекта
                TxtProfName.Text = profile.StockInfo.InnName;
                TxtProfStock.Text = $"{profile.StockInfo.CurrentStock} {profile.StockInfo.MeasureUnit}";
                TxtProfRevenue.Text = $"{profile.TotalRevenueGenerated:C2}";
                TxtProfUsage.Text = $"{profile.TimesUsedInIssuedOrders} times";

                LstProfTechs.ItemsSource = profile.ApplicableTechnologies;
                DgProfDemand.ItemsSource = profile.PendingDemands;
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void ClearProfile()
        {
            TxtProfName.Text = "-";
            TxtProfStock.Text = "";
            TxtProfRevenue.Text = "";
            TxtProfUsage.Text = "";
            LstProfTechs.ItemsSource = null;
            DgProfDemand.ItemsSource = null;
        }

        // --- БЛОК 5: ПРОИЗВОДСТВО ---

        private void BtnRefreshProduction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DgActiveProd.ItemsSource = _controller.GetActiveProductionOrders();
                //DgRawMaterials.ItemsSource = _controller.GetRequiredComponentsForActiveOrders();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        // --- Helpers ---
        private void ShowError(Exception ex) => MessageBox.Show($"Error: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
        private void ShowInfo(string msg) => MessageBox.Show(msg, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}