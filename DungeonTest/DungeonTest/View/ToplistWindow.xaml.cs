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

namespace DungeonTest.View
{
    /// <summary>
    /// Interaction logic for ToplistWindow.xaml
    /// </summary>
    public partial class ToplistWindow : Window
    {
        public ToplistWindow()
        {
            InitializeComponent();
        }

        public void DragWindow(object sender, MouseButtonEventArgs args)
        {
            DragMove();
        }
    }
}
