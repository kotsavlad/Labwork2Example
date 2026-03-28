using System.Windows;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Labwork2Example
{
    /// <summary>
    /// Interaction logic for ChartWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window
    {
        public ChartWindow(int[] counts1, int[] counts2)
        {
            InitializeComponent();

            var values1 = new ChartValues<ObservablePoint>();
            for (int i = 0; i < counts1.Length; i++)
            {
                values1.Add(new ObservablePoint(i + 1, counts1[i]));
            }

            var values2 = new ChartValues<ObservablePoint>();
            for (int i = 0; i < counts2.Length; i++)
            {
                values2.Add(new ObservablePoint(i + 1, counts2[i]));
            }

            var mySeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Matrix 1",
                    Values = values1
                },
                new ColumnSeries
                {
                    Title = "Matrix 1",
                    Values = values2
                }
            };

            MyChart.Series = mySeries;
        }
    }
}