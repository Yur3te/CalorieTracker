using System;
using System.Linq;
using System.Windows;

namespace CalorieTracker
{
    public partial class MealDialog : Window
    {
        private int loggedUserId;
        private int? mealItemId;

        public MealDialog(int userId, int? passedmealItemId = null)
        {
            InitializeComponent();
            loggedUserId = userId;
            mealItemId = passedmealItemId;

            LoadProducts();

            MealDatePicker.SelectedDate = DateTime.Today;

            if (mealItemId != null)
            {
                LoadExistingData();
                this.Title = "Edit Meal";
            }
        }

        private void LoadProducts()
        {
            using (var db = new CalorieTrackerDBEntities())
            {
                ProductsComboBox.ItemsSource = db.Products.ToList();
            }
        }

        private void LoadExistingData()
        {
            using (var db = new CalorieTrackerDBEntities())
            {
                var item = db.MealItems.Find(mealItemId);
                if (item != null)
                {
                    MealDatePicker.SelectedDate = item.Meal.Date;
                    ProductsComboBox.SelectedValue = item.ProductId;
                    WeightTextBox.Text = item.WeightInGrams.ToString();
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a product.", "Validation Error");
                return;
            }

            if (!int.TryParse(WeightTextBox.Text, out int weight) || weight <= 0 || weight > 10000)
            {
                MessageBox.Show("Weight must be a valid number between 0g and 10000g.", "Validation Error");
                return;
            }

            DateTime selectedDate = MealDatePicker.SelectedDate ?? DateTime.Today;

            using (var db = new CalorieTrackerDBEntities())
            {
                if (mealItemId == null)
                {
                    var newMeal = new Meal { UserId = loggedUserId, MealType = "Wpis", Date = selectedDate };
                    db.Meals.Add(newMeal);
                    db.SaveChanges();

                    var newItem = new MealItem { MealId = newMeal.Id, ProductId = (int)ProductsComboBox.SelectedValue, WeightInGrams = weight };
                    db.MealItems.Add(newItem);
                }
                else
                {
                    var itemToUpdate = db.MealItems.Find(mealItemId);
                    if (itemToUpdate != null)
                    {
                        itemToUpdate.WeightInGrams = weight;
                        itemToUpdate.ProductId = (int)ProductsComboBox.SelectedValue;

                        var parentMeal = db.Meals.Find(itemToUpdate.MealId);
                        if (parentMeal != null)
                        {
                            parentMeal.Date = selectedDate;
                        }
                    }
                }
                db.SaveChanges();
            }
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}