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

namespace BlackStar.View
{
    /// <summary>
    /// Menu.xaml 的交互逻辑
    /// </summary>
    public partial class Menu : Window
    {
        public Menu()
        {
            InitializeComponent();

            MessageOP.MessageOf<NotifyMessage>().Subscribe(OnNotifyMessage);
        }

        private void OnNotifyMessage(string message)
        {
            if(message == "Done")
            {
                this.bt1.IsEnabled = true;
                this.bt2.IsEnabled = true;
                this.bt3.IsEnabled = true;
                this.bt4.IsEnabled = true;
                this.bt5.IsEnabled = true;
                this.bt6.IsEnabled = true;
                this.bt7.IsEnabled = true;
            }
        }

        private void Bt_Click(object sender, RoutedEventArgs e)
        {
            string content = ((Button)sender).Content.ToString();
            MessageOP.MessageOf<DualMessage>().Publish(("Sample", content));
            this.bt1.IsEnabled = false;
            this.bt2.IsEnabled = false;
            this.bt3.IsEnabled = false;
            this.bt4.IsEnabled = false;
            this.bt5.IsEnabled = false;
            this.bt6.IsEnabled = false;
            this.bt7.IsEnabled = false;
        }
    }
}
