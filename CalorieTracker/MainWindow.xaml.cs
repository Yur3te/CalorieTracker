using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CalorieTracker
{
    public partial class MainWindow : Window
    {
        private int LoggedUserId;

        public MainWindow(int userId)
        {
            InitializeComponent();
            LoggedUserId = userId;
            LoadData();
        }

        private void LoadData()
        {
            using (var db = new CalorieTrackerDBEntities())
            {
                ProductsComboBox.ItemsSource = db.Products.ToList();

                var dataToTable = db.MealItems
                    .Where(mi => mi.Meal.UserId == LoggedUserId)
                    .OrderByDescending(mi => mi.Meal.Date)
                    .Select(mi => new
                    {
                        Id = mi.Id,
                        Data = mi.Meal.Date,
                        Type = mi.Meal.MealType,
                        Product = mi.Product.Name,
                        Weight = mi.WeightInGrams,
                        CaloriesCalculated = (mi.WeightInGrams * mi.Product.CaloriesPer100g) / 100
                    })
                    .ToList();

                MealsDataGrid.ItemsSource = dataToTable;
            }
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
            using (var db = new CalorieTrackerDBEntities())
            {
                Meal NewMeal = new Meal();
                NewMeal.UserId = LoggedUserId;
                NewMeal.MealType = MealTypeTextBox.Text;
                NewMeal.Date = System.DateTime.Now;

                db.Meals.Add(NewMeal);
                db.SaveChanges();

                MealItem MealDetail = new MealItem();
                MealDetail.MealId = NewMeal.Id; 
                MealDetail.ProductId = (int)ProductsComboBox.SelectedValue;
                MealDetail.WeightInGrams = int.Parse(WeightTextBox.Text);

                db.MealItems.Add(MealDetail);
                db.SaveChanges();

                MessageBox.Show("Meal Added!");
                LoadData();
            }
        }
    }
}
