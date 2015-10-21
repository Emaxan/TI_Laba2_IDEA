using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace TI_Laba2
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Processor.CbArr = new[] {CbCod, CbDecod};
            Processor.UiEl = new UIElement[] { CbCod, CbDecod, BOpenSource, BOpenEndingPath, BOpenKey, BWork }; 
        }

        private void BOpenSource_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                InitialDirectory = @"E:\Programs\VS\_TI\TI_Laba2\",
                Multiselect = false,
                Title = "Открыть исходный файл"
            };

            if (ofd.ShowDialog() != true) return;
            TbSourceText.Text = ofd.FileName;
        }

        private void BOpenEndingPath_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                InitialDirectory = @"E:\Programs\VS\_TI\TI_Laba2\",
                Title = "Сохранить зашифрованный файл"
            };
            if (sfd.ShowDialog() != true) return;
            TbEndingPath.Text = sfd.FileName;
        }

        private void BOpenKey_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                InitialDirectory = @"E:\Programs\VS\_TI\TI_Laba2\",
                Multiselect = false,
                Title = "Открыть ключа"
            };
            if (ofd.ShowDialog() != true) return;
            TbKey.Text = ofd.FileName;
        }

        private void BWork_Click(object sender, RoutedEventArgs e)
        {
            if (!(CbDecod.IsChecked == true || CbCod.IsChecked == true)) return;
            Processor.Status = CbDecod.IsChecked == true ? Status.Decoding : Status.Coding;
            if (TbEndingPath.Text == "" || TbKey.Text == "" || TbSourceText.Text == "") return;
            Processor.EndingPath = TbEndingPath.Text;
            var f = new FileInfo(TbSourceText.Text);
            if (!f.Exists) return;
            f = new FileInfo(TbKey.Text);
            if (!f.Exists) return;

            
            Processor.Key = Processor.PrepareKey(TbKey.Text);
            Processor.Source = TbSourceText.Text;

            Processor.Work();
        }

        private void CbDecod_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var cb in Processor.CbArr)
            {
                cb.IsChecked = Equals(e.OriginalSource, cb);
            }
        }

        private void WMain_KeyDown(object sender, KeyEventArgs e)
        {
            if(Keyboard.Modifiers != ModifierKeys.Control) return;
            switch (e.Key)
            {
                case Key.S:
                    BOpenSource_Click(sender, e);
                    break;
                case Key.E:
                    BOpenEndingPath_Click(sender, e);
                    break;
                case Key.K:
                    BOpenKey_Click(sender, e);
                    break;
                case Key.D:
                    CbDecod.IsChecked = true;
                    break;
                case Key.C:
                    CbCod.IsChecked = true;
                    break;
            }
        }
    }
}
