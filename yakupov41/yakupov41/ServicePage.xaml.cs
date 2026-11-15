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
namespace yakupov41
{
    public partial class ServicePage : Page
    {
        private User _currentUser;
        private List<OrderProduct> _selectedOrderProducts = new List<OrderProduct>();
        private List<Product> _selectedProducts = new List<Product>();
        private int _newOrderID;

        public ServicePage(User user)
        {
            InitializeComponent();
            _currentUser = user;

            if (user == null)
            {
                FIOTB.Text = "Гость";
                RoleTB.Text = "Гость";
            }
            else
            {
                FIOTB.Text = $"{user.UserSurname?.Trim()} {user.UserName?.Trim()} {user.UserPatronymic?.Trim()}".Trim();
                switch (user.UserRoleID)
                {
                    case 1:
                        RoleTB.Text = "Администратор";
                        break;
                    case 2:
                        RoleTB.Text = "Клиент";
                        break;
                    case 3:
                        RoleTB.Text = "Менеджер";
                        break;
                }
            }

            var orders = yakupov41Entities.GetContext().Order.ToList();
            _newOrderID = orders.Any() ? orders.Max(o => o.OrderID) + 1 : 1;

            ComboType.SelectedIndex = 0;
            UpdateServices();
        }

        private void UpdateServices()
        {
            var allProducts = yakupov41Entities.GetContext().Product.ToList();
            int totalCount = allProducts.Count;
            var currentServices = allProducts;

            if (ComboType.SelectedIndex == 1)
            {
                currentServices = currentServices.Where(p => p.ProductDiscountAmount >= 0 && p.ProductDiscountAmount < 10).ToList();
            }
            else if (ComboType.SelectedIndex == 2)
            {
                currentServices = currentServices.Where(p => p.ProductDiscountAmount >= 10 && p.ProductDiscountAmount < 15).ToList();
            }
            else if (ComboType.SelectedIndex == 3)
            {
                currentServices = currentServices.Where(p => p.ProductDiscountAmount >= 15 && p.ProductDiscountAmount <= 100).ToList();
            }

            if (!string.IsNullOrWhiteSpace(TBoxSearch.Text))
            {
                currentServices = currentServices
                    .Where(p => p.ProductName.ToLower().Contains(TBoxSearch.Text.ToLower()))
                    .ToList();
            }

            if (RButtonDown.IsChecked == true)
            {
                currentServices = currentServices.OrderByDescending(p => p.ProductCost).ToList();
            }
            else if (RButtonUp.IsChecked == true)
            {
                currentServices = currentServices.OrderBy(p => p.ProductCost).ToList();
            }

            ProductListView.ItemsSource = currentServices;
            TBCount.Text = $"кол-во {currentServices.Count} из {totalCount}";
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ProductListView.SelectedItem is Product prod)
            {
                var existingOrderProd = _selectedOrderProducts
                    .FirstOrDefault(p => p.ProductArticleNumber == prod.ProductArticleNumber);

                if (existingOrderProd == null)
                {
                    _selectedProducts.Add(prod);
                    _selectedOrderProducts.Add(new OrderProduct
                    {
                        OrderID = _newOrderID,
                        ProductArticleNumber = prod.ProductArticleNumber,
                        Quantity = 1
                    });
                }
                else
                {
                    existingOrderProd.Quantity++;
                }

                OrderBtn.Visibility = Visibility.Visible;
                ProductListView.SelectedIndex = -1;
            }
        }

        private void OrderBtn_Click(object sender, RoutedEventArgs e)
        {
            OrderWindow orderWindow = new OrderWindow(_selectedOrderProducts, _selectedProducts, _currentUser, _newOrderID);
            orderWindow.ShowDialog();

            if (orderWindow.IsOrderSaved)
            {
                _selectedOrderProducts.Clear();
                _selectedProducts.Clear();
                var orders = yakupov41Entities.GetContext().Order.ToList();
                _newOrderID = orders.Any() ? orders.Max(o => o.OrderID) + 1 : 1;
            }

            OrderBtn.Visibility = _selectedProducts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e) => UpdateServices();
        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateServices();
        private void RButtonUp_Checked(object sender, RoutedEventArgs e) => UpdateServices();
        private void RButtonDown_Checked(object sender, RoutedEventArgs e) => UpdateServices();
    }
}