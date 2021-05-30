using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace SCOI_5
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int ThreadUse = 1;
        bool _flagFirstStart = true;
        ResultImages results = null;
        List<IFilterFigure> filterFigures;
        TypeFilterFreq typeFilter = TypeFilterFreq.HightFreq; 
        public MainWindow()
        {
            InitializeComponent();

            this.SizeChanged += MainWindow_SizeChanged;

            ThreadSlider.Maximum = ThreadCPP.ThreadCount();
            ThreadSlider.ValueChanged += ThreadSlider_ValueChanged;

            Canv.MouseDown += Canv_MouseDown;
            filterFigures = new List<IFilterFigure>();
        }

        Point p;
        bool canmove = false;
        private void Canv_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (canmove == false && e.LeftButton == MouseButtonState.Pressed)
            {
                var rect = new System.Windows.Shapes.Ellipse() { Width = 15, Height = 15, Stroke = typeFilter == TypeFilterFreq.HightFreq ? Brushes.Red : Brushes.Blue, StrokeThickness = 3};
                rect.MouseDown += Rect_MouseDown;
                rect.PreviewMouseMove += Rect_PreviewMouseMove;
                rect.PreviewMouseDown += Rect_PreviewMouseDown;
                rect.PreviewMouseUp += Rect_PreviewMouseUp;
                rect.MouseWheel += Rect_MouseWheel;
                Canvas.SetTop(rect, e.GetPosition(Canv).Y - 2.5);
                Canvas.SetLeft(rect, e.GetPosition(Canv).X - 2.5);
                Canv.Children.Add(rect);
                double _coefX = (results.FourierImage.Width / PictureResult.Width);
                double _coefY = (results.FourierImage.Height / PictureResult.Height);
                double xpos = ((Canvas.GetLeft(rect) - Canv.Width / 2) * (_coefX > _coefY ? _coefX : _coefY) + rect.Width / 2 * (_coefX > _coefY ? _coefX : _coefY));
                double ypos = ((Canv.Height / 2 - Canvas.GetTop(rect)) * (_coefX > _coefY ? _coefX : _coefY) - rect.Width / 2 * (_coefX > _coefY ? _coefX : _coefY));
                FilterCircle filterEllipse = new FilterCircle() { Radius = (rect.Width / 2) * (_coefX > _coefY ? _coefX : _coefY), Xcenter = xpos, Ycenter = ypos };
                filterFigures.Add(filterEllipse);

                WriteLog(e.GetPosition(Canv).ToString());
            }
        }

        private void Rect_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var delta = e.Delta / 20;
            var el = sender as Ellipse;
            ((Ellipse)sender).Width = ((Ellipse)sender).Height += delta;
            UpdateFilterFigures();
        }

        private void Rect_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Shapes.Ellipse r = sender as System.Windows.Shapes.Ellipse;
                Mouse.Capture(r);
                p = Mouse.GetPosition(r);
                canmove = true;
            }
        }

        private void Rect_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (canmove)
            {
                System.Windows.Shapes.Ellipse r = (System.Windows.Shapes.Ellipse)sender;

                double XposNew = _helper.InBorder(e.GetPosition(null).X - p.X - Canv.Margin.Left, 0, Canv.ActualWidth);
                double YposNew = _helper.InBorder(e.GetPosition(null).Y - p.Y - Canv.Margin.Top, 0, Canv.ActualHeight);

                r.SetValue(Canvas.LeftProperty, XposNew);
                r.SetValue(Canvas.TopProperty, YposNew);
                UpdateFilterFigures();
            }
        }

        private void Rect_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            canmove = false;
        }

        private void Rect_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                System.Windows.Shapes.Ellipse rectangleForDelete = sender as System.Windows.Shapes.Ellipse;
                Canv.Children.Remove(rectangleForDelete);
                UpdateFilterFigures();
            }
        }

        private void UpdateFilterFigures()
        {
            filterFigures.Clear();
            for(int i = 1; i < Canv.Children.Count; i++)
            {
                var fig = Canv.Children[i] as Ellipse;
                double _coefX = (results.FourierImage.Width / PictureResult.Width);
                double _coefY = (results.FourierImage.Height / PictureResult.Height);
                double xpos = ((Canvas.GetLeft(fig) - Canv.Width / 2) * (_coefX > _coefY ? _coefX : _coefY) + fig.Width / 2 * (_coefX > _coefY ? _coefX : _coefY));
                double ypos = ((Canv.Height / 2 - Canvas.GetTop(fig)) * (_coefX > _coefY ? _coefX : _coefY) - fig.Width / 2 * (_coefX > _coefY ? _coefX : _coefY));
                filterFigures.Add(new FilterCircle() { Radius = (fig.Width / 2) * (_coefX > _coefY ? _coefX : _coefY), Xcenter = xpos, Ycenter = ypos});
            }
        }

        private void HideFilter()
        {
            for(int i = 1; i < Canv.Children.Count; i++)
            {
                var fig = Canv.Children[i] as Ellipse;
                fig.Visibility = Visibility.Hidden;
            }
        }

        private void ShowFilter()
        {
            for (int i = 1; i < Canv.Children.Count; i++)
            {
                var fig = Canv.Children[i] as Ellipse;
                fig.Visibility = Visibility.Visible;
            }
        }

        private void ThreadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ThreadUse = (int)e.NewValue;
            ThreadValue.Text = ((int)e.NewValue).ToString();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_flagFirstStart)
            {
                var deltaHeight = e.NewSize.Height - e.PreviousSize.Height;
                var deltaWidth = e.NewSize.Width - e.PreviousSize.Width;
                //Picture.Height += deltaHeight;
                //Picture.Width += deltaWidth;
                PictureResult.Height += deltaHeight;
                PictureResult.Width += deltaWidth;
                Canv.Height += deltaHeight;
                Canv.Width += deltaWidth;
                NavBar.Width += deltaWidth;
                Log.Margin = new Thickness(Log.Margin.Left, Log.Margin.Top + deltaHeight, 0, 0);
                Log.Width += deltaWidth;
                UpdateFilterFigures();
            }
            else
            {
                _flagFirstStart = false;
            }
        }

        //Кнопочки

        private void ApplyClick(object sender, RoutedEventArgs e)
        {
            var start = DateTime.UtcNow;
            results = ImgFT.Calculate((BitmapSource)Picture.Source, filterFigures, typeFilter, ThreadUse);
            WriteLog("Обработано за " + (DateTime.UtcNow - start).TotalMilliseconds + " мс", Brushes.DarkGreen);
            PictureResult.Source = results.FourierImage;
            PictureResult.VerticalAlignment = VerticalAlignment.Center;
            PictureResult.HorizontalAlignment = HorizontalAlignment.Center;
            ShowFilter();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        private void ChangeFilter(object sender, RoutedEventArgs e)
        {
            if (typeFilter == TypeFilterFreq.HightFreq)
            {
                typeFilter = TypeFilterFreq.LowFreq;
                ChangeFilterButton.Content = "ФНЧ -> ФВЧ";
                for(int i = 1; i < Canv.Children.Count; i++)
                {
                    ((Ellipse)(Canv.Children[i])).Stroke = Brushes.Blue;
                }
            }
            else
            {
                typeFilter = TypeFilterFreq.HightFreq;
                ChangeFilterButton.Content = "ФВЧ -> ФНЧ";
                for (int i = 1; i < Canv.Children.Count; i++)
                {
                    ((Ellipse)(Canv.Children[i])).Stroke = Brushes.Red;
                }
            }

        }

        private void ShowFourier(object sender, RoutedEventArgs e)
        {
            if (results != null)
            {
                if (results.FourierImage != null)
                {
                    PictureResult.Source = results.FourierImage;
                    //Canv.Width = results.FourierImage.Width;
                    //Canv.Height = results.FourierImage.Height;
                    PictureResult.VerticalAlignment = VerticalAlignment.Center;
                    PictureResult.HorizontalAlignment = HorizontalAlignment.Center;
                    ShowFilter();
                    return;
                }
            }
            WriteLog("Ошибка", Brushes.DarkRed);
        }

        private void ShowResult(object sender, RoutedEventArgs e)
        {
            if (results != null)
            {
                if (results.NewImage != null)
                {
                    PictureResult.Source = results.NewImage;
                    //Canv.Width = results.NewImage.Width;
                    //Canv.Height = results.NewImage.Height;
                    PictureResult.VerticalAlignment = VerticalAlignment.Top;
                    PictureResult.HorizontalAlignment = HorizontalAlignment.Left;
                    HideFilter();
                    return;
                }
            }
            WriteLog("Ошибка", Brushes.DarkRed);
        }

        private void ShowFilter(object sender, RoutedEventArgs e)
        {
            if (results != null)
            {
                if (results.FilterImage != null)
                {
                    PictureResult.Source = results.FilterImage;
                    //Canv.Width  = results.FilterImage.Width;
                    //Canv.Height = results.FilterImage.Height;
                    PictureResult.VerticalAlignment = VerticalAlignment.Center;
                    PictureResult.HorizontalAlignment = HorizontalAlignment.Center;
                    HideFilter();
                    //ShowFilter();
                    return;
                }
            }
            WriteLog("Ошибка", Brushes.DarkRed);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            OpenFileDialog fileManager = new OpenFileDialog();
            fileManager.Filter = "Файлы jpg|*.jpg|Файлы jpeg|*.jpeg|Файлы png| *.png";
            fileManager.ShowDialog();
            var item = fileManager.FileName;
            if (item != "")
            {
                Picture.Source = new BitmapImage(new Uri(item, UriKind.RelativeOrAbsolute));
            }
        }

        private void FileIsDropped(object sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData("FileDrop");
            try
            {
                foreach (var item in paths)
                {
                    var start = DateTime.UtcNow;

                    Picture.Source = new BitmapImage(new Uri(item, UriKind.RelativeOrAbsolute));
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, Brushes.Red);
            }
        }


        private void SaveAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileManager = new SaveFileDialog();
            fileManager.Filter = "Файлы jpg|*.jpg|Файлы jpeg|*.jpeg|Файлы png| *.png";
            fileManager.ShowDialog();
            var item = fileManager.FileName;
            try
            {
                if (item != "")
                {
                    Picture.Source.Save(item);
                }
                WriteLog("Файл " + item + " успешно сохранен", Brushes.DarkBlue);
            }
            catch
            {
                WriteLog("Не удалось сохранить в указанный файл", Brushes.Red);
            }
        }

        private void CopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(PictureResult.Source as BitmapSource);
            WriteLog("Скопировано", Brushes.DarkOrange);
        }

        private void CutClick(object sender, RoutedEventArgs e)
        {

        }

        private void PasteClick(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                Picture.Source = Clipboard.GetImage();
            }
            else
            {
                WriteLog("Не картинка", Brushes.Red);
            }
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void WriteLog(string message, System.Windows.Media.SolidColorBrush color = null)
        {
            if (color == null)
                color = System.Windows.Media.Brushes.Black;
            var text = new TextBlock() { Text = message, Foreground = color };
            Log.Items.Add(text);
            Log.ScrollIntoView(text);
            Log.SelectedItem = text;
        }

        private void MemoryLog(object sender, RoutedEventArgs e)
        {
            //Показать память
            Process process = Process.GetCurrentProcess();
            long memoryAmount = process.WorkingSet64;
            WriteLog("Памяти скушано - " + (memoryAmount / (1024 * 1024)).ToString(), Brushes.Purple);
        }

        private void ClearLog(object sender, RoutedEventArgs e)
        {
            Log.Items.Clear();
        }
    }
}
