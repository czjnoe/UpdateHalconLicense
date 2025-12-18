using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace UpdateHalconLicense
{
    /// <summary>
    /// GitManagerPage.xaml 的交互逻辑
    /// </summary>
    public partial class GitManagerPage : Window
    {
        public ObservableCollection<ProxyItem> ProxyList = new ObservableCollection<ProxyItem>();

        public GitManagerPage(List<string> proxyList)
        {
            InitializeComponent();
            ProxyList = new ObservableCollection<ProxyItem>(proxyList.Select(p => new ProxyItem { Proxy = p }));
            dataGrid.ItemsSource = ProxyList;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ProxyList.Add(new ProxyItem { Proxy = "" });
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectItem = dataGrid.SelectedItem as ProxyItem;
            if (selectItem is not null)
            {
                ProxyList.Remove(selectItem);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }

    public class ProxyItem : INotifyPropertyChanged
    {
        private string _proxy;
        public string Proxy
        {
            get => _proxy;
            set
            {
                _proxy = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
