using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using DotNetMessenger.Model;
using DotNetMessenger.Model.Enums;

namespace DotNetMessenger.WPFClient.Controls
{
    /// <summary>
    /// Логика взаимодействия для AttachmentControl.xaml
    /// </summary>
    public partial class AttachmentControl : UserControl
    {
        public Attachment Attachment
        {
            get => (Attachment) GetValue(AttachmentProperty);
            set
            {
                SetValue(AttachmentProperty, value);
                AttachedFileIcon = value.Type == AttachmentTypes.Image ? value.File : null;
                AttachedName = value.FileName;
            }
        }
        public static readonly DependencyProperty AttachmentProperty =
            DependencyProperty.Register(
                nameof(Attachment), typeof(Attachment),
                typeof(AttachmentControl)
            );

        public byte[] AttachedFileIcon
        {
            get => (byte[]) GetValue(AttachedFileIconProperty);
            set => SetValue(AttachedFileIconProperty, value);
        }
        public static readonly DependencyProperty AttachedFileIconProperty =
            DependencyProperty.Register(
                nameof(AttachedFileIcon), typeof(byte[]),
                typeof(AttachmentControl)
            );

        public string AttachedName
        {
            get => (string) GetValue(AttachedFileProperty);
            set => SetValue(AttachedFileProperty, value);
        }
        public static readonly DependencyProperty AttachedFileProperty =
            DependencyProperty.Register(
                nameof(AttachedName), typeof(string),
                typeof(AttachmentControl)
            );

        public AttachmentControl()
        {
            InitializeComponent();
        }

        public AttachmentControl(Attachment attachment) : this()
        {
            Attachment = attachment;
        }

        private void AttachmentButtonHolder_OnClick(object sender, RoutedEventArgs e)
        {
            var attachment = Attachment;

            var filename = attachment.FileName;
            if (!GetImageFromDialog(ref filename)) return;

            try
            {
                File.WriteAllBytes(filename, attachment.File);
                if (attachment.Type == AttachmentTypes.Image)
                {
                    Task.Run(() => Process.Start(filename));
                }
            }
            catch
            {
                MessageBox.Show("Could not save file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool GetImageFromDialog(ref string filename)
        {
            // Create OpenFileDialog 
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".*",
                Filter = "Any files (*.*)|*.*",
                FileName = filename
            };

            // Display OpenFileDialog by calling ShowDialog method 
            var result = dlg.ShowDialog();

            if (result != true) return false;
            filename = dlg.FileName;
            return true;
        }
    }
}
