using PharmacySystem.Data;
using PharmacySystem.ORMLogistics;
using PharmacySystem.ORMLogistics.Logs;
using PharmacySystem.ViewModels;
using PharmacySystem.Service;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PharmacySystem
{
    public partial class MainWindow : Window
    {
        private readonly string _baseDataPath;
        private readonly PharmacyAnalyticsController _analyticsController;
        private readonly IssuedJournalRepository _journalRepo;

        public MainWindow()
        {
            InitializeComponent();

            // Безопасное определение пути к данным
            _baseDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(_baseDataPath))
                Directory.CreateDirectory(_baseDataPath);

            var warehouseRepo = new WarehouseRepository(Path.Combine(_baseDataPath, "warehouse.xml"));
            var techRepo = new TechnologyRepository(Path.Combine(_baseDataPath, "technology.xml"));
            var waitingRepo = new WaitingListRepository(Path.Combine(_baseDataPath, "waiting.xml"));
            _journalRepo = new IssuedJournalRepository(Path.Combine(_baseDataPath, "issued.xml"));

            _analyticsController = new PharmacyAnalyticsController(
                warehouseRepo,
                waitingRepo,
                _journalRepo,
                techRepo
            );

            RefreshDashboard();
        }

        /// <summary>
        /// Обновляет таблицу последних операций на главном экране
        /// </summary>
        private void RefreshDashboard()
        {
            try
            {
                var recentOrders = _journalRepo.GetAll()
                    .OrderByDescending(x => x.IssueDate)
                    .Take(20)
                    .ToList();

                MainLogGrid.ItemsSource = null;
                MainLogGrid.ItemsSource = recentOrders;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Dashboard sync: {ex.Message}");
            }
        }

        private void BtnCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            string pathArg = _baseDataPath.EndsWith("\\") ? _baseDataPath : _baseDataPath + "\\";

            var orderWin = new NewOrderWindow(pathArg);
            orderWin.Owner = this;
            if (orderWin.ShowDialog() == false)
            {
                RefreshDashboard();
            }
        }

        private void BtnOpenAnalytics_Click(object sender, RoutedEventArgs e)
        {
            var analyticsWin = new FullAnalyticsWindow(_analyticsController);
            analyticsWin.Owner = this;
            analyticsWin.Show(); // Аналитику можно держать открытой параллельно
        }

        private void BtnWarehouse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inventoryWin = new InventoryWindow(new 
                    WarehouseRepository(Path.Combine(_baseDataPath, "warehouse.xml")))
                {
                    Owner = this
                };

                inventoryWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open inventory: {ex.Message}",
                                "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnWaitingList_Click(object sender, RoutedEventArgs e)
        {
            var analyticsWin = new FullAnalyticsWindow(_analyticsController);
            analyticsWin.Owner = this;

            var waitingTab = analyticsWin.MainTabControl.Items
                .Cast<TabItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == "Waiting");

            if (waitingTab != null)
            {
                analyticsWin.MainTabControl.SelectedItem = waitingTab;
            }

            analyticsWin.Show();
        }
    }
}