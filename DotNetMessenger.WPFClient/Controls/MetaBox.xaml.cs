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

namespace DotNetMessenger.WPFClient.Controls
{
    /// <summary>
    /// Логика взаимодействия для MetaBox.xaml
    /// </summary>
    public partial class MetaBox : UserControl
    {
        public string MetaTitle
        {
            get => (string)GetValue(MetaTitleProperty);
            set => SetValue(MetaTitleProperty, value);
        }
        public string MetaSecondaryInfo
        {
            get => (string)GetValue(MetaSecondaryInfoProperty);
            set => SetValue(MetaSecondaryInfoProperty, value);
        }
        public byte[] MetaImage
        {
            get => (byte[])GetValue(MetaImageProperty);
            set => SetValue(MetaImageProperty, value);
        }

        public static readonly DependencyProperty MetaTitleProperty =
            DependencyProperty.Register(
                nameof(MetaTitle), typeof(string),
                typeof(MetaBox)
            );

        public static readonly DependencyProperty MetaSecondaryInfoProperty =
            DependencyProperty.Register(
                nameof(MetaSecondaryInfo), typeof(string),
                typeof(MetaBox)
            );

        public static readonly DependencyProperty MetaImageProperty =
            DependencyProperty.Register(
                nameof(MetaImage), typeof(byte[]),
                typeof(MetaBox)
            );

        public MetaBox()
        {
            InitializeComponent();
        }
    }
}
