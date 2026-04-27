using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Configuration;


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
            AiDatePicker.SelectedDate = DateTime.Today;
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

        private async void AskAiButton_Click(object sender, RoutedEventArgs e)
        {
            AskAiButton.IsEnabled = false;
            AiResponseTextBlock.Text = "Trainer is analysing your pethetic diet... Wait...";

            try
            {
                DateTime targetDate = AiDatePicker.SelectedDate ?? DateTime.Today;
                string dietSummary = "";
                int totalKcal = 0;

                using (var db = new CalorieTrackerDBEntities())
                {
                    var meals = db.MealItems
                        .Where(m => m.Meal.UserId == LoggedUserId &&
                                    m.Meal.Date.Year == targetDate.Year &&
                                    m.Meal.Date.Month == targetDate.Month &&
                                    m.Meal.Date.Day == targetDate.Day)
                        .ToList();

                    if (!meals.Any())
                    {
                        AiResponseTextBlock.Text = "You haven't put anything in the journal today, do I have to remind you to eat my regin but other way?";
                        AskAiButton.IsEnabled = true;
                        return;
                    }

                    foreach (var item in meals)
                    {
                        int kcal = (int)(((long)item.WeightInGrams * item.Product.CaloriesPer100g) / 100);
                        totalKcal += kcal;
                        dietSummary += $"- {item.WeightInGrams}g: {item.Product.Name} ({kcal} kcal)\n";
                    }
                }

                string prompt = $"You are a ruthless, foul-mouthed, and highly sarcastic personal trainer. Roast the shit out of my daily diet. " +
                                $"I ate a total of {totalKcal} kcal. Here is the garbage I consumed today:\n{dietSummary}\n" +
                                $"Keep it short (max 4 sentences) and be brutally insulting. " +
                                $"If I didn't eat enough calories, yell at me for being a skinny bitch who will never build any fucking muscle. " +
                                $"If I ate too much or absolute junk, roast me for treating my body like a fucking dumpster. " +
                                $"Swear, be incredibly harsh, and do not hold back.";

                string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    AiResponseTextBlock.Text = "Error: there is no API key in config";
                    AskAiButton.IsEnabled = true;
                    return;
                }
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

                var payload = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } }
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);

                using (HttpClient client = new HttpClient())
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);
                    string responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JObject.Parse(responseString);
                        string aiText = result["candidates"][0]["content"]["parts"][0]["text"].ToString();

                        AiResponseTextBlock.Text = aiText;
                    }
                    else
                    {
                        AiResponseTextBlock.Text = $"Trainer is not available (API Error): {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                AiResponseTextBlock.Text = "Something went wrong: " + ex.Message;
            }
            finally
            {
                AskAiButton.IsEnabled = true;
            }
        }
    }

}
