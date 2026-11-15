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
using System.Windows.Shapes;

namespace yakupov41
{
    public partial class OrderWindow : Window
    {
        private List<OrderProduct> _selectedOrderProducts;
        private List<Product> _selectedProducts;
        private User _currentUser;
        private int _orderID;

        public bool IsOrderSaved { get; private set; } = false;

        public OrderWindow(List<OrderProduct> selectedOrderProducts, List<Product> selectedProducts, User currentUser, int orderID)
        {
            InitializeComponent();
            _selectedOrderProducts = selectedOrderProducts;
            _selectedProducts = selectedProducts;
            _currentUser = currentUser;
            _orderID = orderID;

            PickupCombo.ItemsSource = yakupov41Entities.GetContext().PickUpPoints.ToList();

            if (_currentUser != null)
            {
                ClientTB.Text = $"{_currentUser.UserSurname?.Trim()} {_currentUser.UserName?.Trim()} {_currentUser.UserPatronymic?.Trim()}".Trim();
            }
            else
            {
                ClientTB.Text = "Гость";
            }

            TBOrderID.Text = _orderID.ToString();

            foreach (Product p in _selectedProducts)
            {
                var op = _selectedOrderProducts.FirstOrDefault(q => q.ProductArticleNumber == p.ProductArticleNumber);
                p.Quantity = op != null ? op.Quantity : 1;
            }

            ShoeListView.ItemsSource = _selectedProducts;
            OrderDP.SelectedDate = DateTime.Now;
            SetDeliveryDate();
            UpdateOrderSummary();
        }

        private void SetDeliveryDate()
        {
            if (OrderDP.SelectedDate == null) return;

            bool allInStockMoreThanThree = _selectedProducts.All(p =>
                p.ProductQuantityInStock > 3 && p.Quantity <= p.ProductQuantityInStock);

            int daysToAdd = allInStockMoreThanThree ? 3 : 6;
            DeliveryDP.SelectedDate = OrderDP.SelectedDate.Value.AddDays(daysToAdd);
        }

        private void UpdateOrderSummary()
        {
            decimal totalFull = 0;
            decimal totalDiscount = 0;
            foreach (var prod in _selectedProducts)
            {
                decimal lineCost = prod.ProductCost * prod.Quantity;
                decimal lineDiscount = lineCost * prod.ProductDiscountAmount / 100m;
                totalFull += lineCost;
                totalDiscount += lineDiscount;
            }

            decimal totalToPay = totalFull - totalDiscount;
            TBTotalSum.Text = $"Сумма заказа: {totalToPay:0.00} руб.";
            TBTotalDiscount.Text = $"Сумма скидки: {totalDiscount:0.00} руб.";
        }

        private void RemoveProductFromOrder(Product prod)
        {
            _selectedProducts.Remove(prod);
            var selectedOP = _selectedOrderProducts.FirstOrDefault(p => p.ProductArticleNumber == prod.ProductArticleNumber);
            if (selectedOP != null)
            {
                _selectedOrderProducts.Remove(selectedOP);
            }

            ShoeListView.ItemsSource = null;
            ShoeListView.ItemsSource = _selectedProducts;
            SetDeliveryDate();
            UpdateOrderSummary();

            if (_selectedProducts.Count == 0)
            {
                MessageBox.Show("Заказ пуст.");
                Close();
            }
        }

        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Product prod)
            {
                prod.Quantity++;
                var selectedOP = _selectedOrderProducts.FirstOrDefault(p => p.ProductArticleNumber == prod.ProductArticleNumber);
                if (selectedOP != null)
                {
                    selectedOP.Quantity = prod.Quantity;
                }

                SetDeliveryDate();
                UpdateOrderSummary();
                ShoeListView.Items.Refresh();
            }
        }

        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Product prod)
            {
                if (prod.Quantity > 1)
                {
                    prod.Quantity--;
                    var selectedOP = _selectedOrderProducts.FirstOrDefault(p => p.ProductArticleNumber == prod.ProductArticleNumber);
                    if (selectedOP != null)
                    {
                        selectedOP.Quantity = prod.Quantity;
                    }
                    SetDeliveryDate();
                    UpdateOrderSummary();
                    ShoeListView.Items.Refresh();
                }
                else
                {
                    RemoveProductFromOrder(prod);
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is Product prod)
            {
                RemoveProductFromOrder(prod);
            }
        }

        private void OrderDP_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            SetDeliveryDate();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProducts.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один товар в заказ.");
                return;
            }

            if (!(PickupCombo.SelectedItem is PickUpPoints selectedPickup))
            {
                MessageBox.Show("Выберите пункт выдачи!");
                return;
            }

            if (OrderDP.SelectedDate == null || DeliveryDP.SelectedDate == null)
            {
                MessageBox.Show("Укажите даты заказа и доставки!");
                return;
            }

            try
            {
                var context = yakupov41Entities.GetContext();
                int actualOrderID = context.Order.Any() ? context.Order.Max(o => o.OrderID) + 1 : 1;
                int receiveCode = new Random().Next(100, 1000);

                Order newOrder = new Order
                {
                    OrderID = actualOrderID,
                    OrderDate = OrderDP.SelectedDate.Value,
                    OrderDeliveryDate = DeliveryDP.SelectedDate.Value,
                    OrderPickupPointID = selectedPickup.OrderPickupID,
                    OrderClient = _currentUser?.UserID,
                    OrderReceiveCode = receiveCode,
                    OrderStatus = "Новый"
                };

                context.Order.Add(newOrder);

                foreach (var item in _selectedOrderProducts)
                {
                    context.OrderProduct.Add(new OrderProduct
                    {
                        OrderID = actualOrderID,
                        ProductArticleNumber = item.ProductArticleNumber,
                        Quantity = item.Quantity
                    });
                }

                context.SaveChanges();
                IsOrderSaved = true;
                MessageBox.Show($"Заказ №{actualOrderID} успешно сохранён! Код получения: {receiveCode}");
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении заказа: " + ex.Message);
            }
        }
    }
}
