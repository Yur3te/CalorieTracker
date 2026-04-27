using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;


namespace CalorieTracker
{
    public partial class MainWindow : Window
    {
        private int LoggedUserId;

        public MainWindow(int userId)
        {
            InitializeComponent();
            LoggedUserId = userId;
            MainDatePicker.SelectedDate = DateTime.Today;
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new CalorieTrackerDBEntities())
            {
                ProductsDataGrid.ItemsSource = db.Products.OrderBy(p => p.Name).ToList();

                DateTime filterDate = MainDatePicker.SelectedDate ?? DateTime.Today;


                var dataToTable = db.MealItems
                    .Where(mi => mi.Meal.UserId == LoggedUserId &&
                         mi.Meal.Date.Year == filterDate.Year &&
                         mi.Meal.Date.Month == filterDate.Month &&
                         mi.Meal.Date.Day == filterDate.Day)
                    .ToList()
                    .Select(mi => new
                    {
                        Id = mi.Id,
                        Data = mi.Meal.Date,
                        Product = mi.Product.Name,
                        Weight = mi.WeightInGrams,
                        CaloriesCalculated = (int)(((long)mi.WeightInGrams * mi.Product.CaloriesPer100g) / 100)
                    })
                    .ToList();

                MealsDataGrid.ItemsSource = dataToTable;

                int total = dataToTable.Sum(x => x.CaloriesCalculated);
                TotalCaloriesText.Text = total.ToString() + " kcal";
            }
        }
        private void MainDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadData();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void DeleteMealButton_Click(object sender, RoutedEventArgs e)
        {
            if (MealsDataGrid.SelectedItem != null)
            {
                dynamic selected = MealsDataGrid.SelectedItem;
                int idToDelete = selected.Id;

                using (var db = new CalorieTrackerDBEntities())
                {
                    var item = db.MealItems.Find(idToDelete);
                    if (item != null)
                    {
                        db.MealItems.Remove(item);
                        db.SaveChanges();
                        LoadData();
                    }
                }
            }
        }

        private void AddMealButton_Click(object sender, RoutedEventArgs e)
        {
            MealDialog dialog = new MealDialog(LoggedUserId);
            dialog.Owner = this;

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                LoadData();
            }
        }

        private void EditMealButton_Click(object sender, RoutedEventArgs e)
        {
            if (MealsDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Please select a meal to edit.", "Information");
                return;
            }

            dynamic selected = MealsDataGrid.SelectedItem;
            int idToEdit = selected.Id;

            MealDialog dialog = new MealDialog(LoggedUserId, idToEdit);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }
        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewProductNameTextBox.Text))
            {
                MessageBox.Show("Please enter a product name.", "Error");
                return;
            }

            if (!int.TryParse(NewProductKcalTextBox.Text, out int kcal) || kcal < 0 || kcal > 1000)
            {
                MessageBox.Show("Calories must be a valid number between 0 and 1000.", "Error");
                return;
            }

            using (var db = new CalorieTrackerDBEntities())
            {
                var product = new Product
                {
                    Name = NewProductNameTextBox.Text.Trim(),
                    CaloriesPer100g = kcal
                };
                db.Products.Add(product);
                db.SaveChanges();
            }

            NewProductNameTextBox.Clear();
            NewProductKcalTextBox.Clear();
            MessageBox.Show("Product added successfully!", "Success");
            LoadData();
        }

        private void DeleteProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem == null)
            {
                MessageBox.Show("Please select a product to delete.", "Information");
                return;
            }


            dynamic selected = ProductsDataGrid.SelectedItem;
            int idToDelete = selected.Id;

            using (var db = new CalorieTrackerDBEntities())
            {
                bool isUsed = db.MealItems.Any(mi => mi.ProductId == idToDelete);

                if (isUsed)
                {
                    MessageBox.Show("Cannot delete this product because it is already used in a meal journal! \n\nDelete the meals containing this product first.", "Database Constraint Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var product = db.Products.Find(idToDelete);
                if (product != null)
                {
                    db.Products.Remove(product);
                    db.SaveChanges();
                    LoadData();
                }
            }
        }
    }

}
