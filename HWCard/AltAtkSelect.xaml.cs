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

namespace HWCard
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class AltAtkSelect : Window
    {
        public AltAtkSelect(List<string> options)
        {
            InitializeComponent();
            lst_opt.Items.Clear();
            foreach (string option in options)
            {
                lst_opt.Items.Add(option);
            }
        }
        public int select;

        private void lst_opt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            select = lst_opt.SelectedIndex;
            if(select == -1)
            {
                btn_ok.IsEnabled = false;
            }
            else
            {
                btn_ok.IsEnabled = true;
            }
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
