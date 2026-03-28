using Microsoft.Win32;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Path = System.IO.Path;

namespace Labwork2Example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataTable? _dataTable1;
        private DataTable? _dataTable2;
        private string _defaultDataDirectoryPath = @"f:\Temp\Labwork2Data\";

        public MainWindow()
        {
            InitializeComponent();

            if (!Path.Exists(_defaultDataDirectoryPath))
            {
                _defaultDataDirectoryPath = "";
            }
        }

        private bool TryParseRow(IReadOnlyList<string> row, object[] values, out int firstErrorPosition)
        {
            firstErrorPosition = -1;
            for (int i = 0; i < row.Count; i++)
            {
                if (!int.TryParse(row[i], out var value))
                {
                    firstErrorPosition = i;
                    return false;
                }

                values[i] = value;
            }

            return true;
        }

        private DataTable? ReadMatrixToDataTable()
        {
            // var initialDirectory =
            //     Directory.GetParent(Directory.GetParent(Directory.GetParent(Environment.CurrentDirectory)?.FullName!)
            //         ?.FullName!)?.FullName + @"\DataFiles";
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = _defaultDataDirectoryPath,
                CheckFileExists = true,
                Filter = "Text files (*.txt)|*.txt",
                FileName = ""
            };
            if (openFileDialog.ShowDialog() != true) return null;
            var resultTable = new DataTable();
            try
            {
                // First row handling
                using var reader = new StreamReader(openFileDialog.FileName);
                var firstRowArray = reader.ReadLine()?.Split(["\n", " ", "\t"], StringSplitOptions.RemoveEmptyEntries);
                if (firstRowArray is null or { Length: 0 })
                {
                    throw new Exception("First line format is incompatible");
                }

                int columnCount = firstRowArray.Length;
                var values = new object[columnCount];

                if (!TryParseRow(firstRowArray, values, out var errorPosition))
                {
                    throw new Exception($"Invalid value in the column #{errorPosition + 1} in the first line");
                }

                for (var i = 0; i < columnCount; i++)
                    resultTable.Columns.Add($"Col{i + 1}", typeof(int));

                resultTable.Rows.Add(values);

                // Other rows processing
                int rowCounter = 1;
                while (!reader.EndOfStream)
                {
                    rowCounter++;
                    var currentRowArray = reader.ReadLine()?.Split([" ", "\t"], StringSplitOptions.RemoveEmptyEntries);
                    if (currentRowArray?.Length != columnCount)
                    {
                        throw new Exception($"Line #{rowCounter} format is incompatible");
                    }

                    if (!TryParseRow(currentRowArray, values, out errorPosition))
                    {
                        throw new Exception(
                            $"Invalid value in the column #{errorPosition + 1} in the row #{rowCounter}");
                    }

                    resultTable.Rows.Add(values);
                }
            }

            catch (Exception e)
            {
                resultTable = null;
                MessageBox.Show(e.Message, "Reading error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return resultTable;
        }

        private void LoadMatrix_Click(object sender, RoutedEventArgs e)
        {
            var dataTable = ReadMatrixToDataTable();
            if (dataTable is null) return;
            if (sender == LoadMatrix1Button || sender == LoadMatrix1MenuItem)
            {
                _dataTable1 = dataTable;
                DataGrid1.ItemsSource = _dataTable1.DefaultView;
            }
            else
            {
                _dataTable2 = dataTable;
                DataGrid2.ItemsSource = _dataTable2.DefaultView;
            }

            Task1MenuItem.IsEnabled = Task1Button.IsEnabled = Task3MenuItem.IsEnabled = Task3Button.IsEnabled = Task1Button.IsEnabled = Task3Button.IsEnabled = _dataTable1 is not null && _dataTable2 is not null;
            SaveMatrix1MenuItem.IsEnabled = SaveMatrix1Button.IsEnabled = _dataTable1 is not null;
            SaveMatrix2MenuItem.IsEnabled = SaveMatrix2Button.IsEnabled = Task2Button.IsEnabled = _dataTable2 is not null;
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e) =>
            e.Row.Header = $"{e.Row.GetIndex() + 1,-4}";

        private static int[,]? DataTableToMatrix(DataTable? dataTable)
        {
            if (dataTable is null) return null;
            var (m, n) = (dataTable.Rows.Count, dataTable.Columns.Count);
            var matrix = new int[m, n];
            try
            {
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        matrix[i, j] = Convert.ToInt32(dataTable.Rows[i][j]);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return matrix;
        }

        private bool AreSizesCorrect(int[,] matrix1, int[,] matrix2, out int m1, out int n1, out int m2, out int n2)
        {
            (m1, n1) = (matrix1.GetLength(0), matrix1.GetLength(1));
            (m2, n2) = (matrix2.GetLength(0), matrix2.GetLength(1));
            if ((m1, n1) != (m2, n2) && IdenticalSizesCheckBox.IsChecked == true)
            {
                MessageBox.Show("Matrices have different dimensions!", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void Task1_Click(object sender, RoutedEventArgs e)
        {
            var (matrix1, matrix2) = (DataTableToMatrix(_dataTable1), DataTableToMatrix(_dataTable2));
            if (matrix1 is null || matrix2 is null) return;
            if(!AreSizesCorrect(matrix1, matrix2, out int m1, out int n1, out int m2, out var n2)) return;

            var defaultStyle = new Style(typeof(DataGridCell));
            var highlightedStyle = new Style(typeof(DataGridCell));
            highlightedStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.LightGreen));

            var (m, n) = (Math.Min(m1, m2), Math.Min(n1, n2));
            for (int j = 0; j < n; j++)
            {
                var selectionRequired = true;
                for (int i = 0; i < m; i++)
                {
                    if (matrix1[i, j] < matrix2[i, j])
                    {
                        selectionRequired = false;
                        break;
                    }
                }

                DataGrid1.Columns[j].CellStyle = selectionRequired
                    ? highlightedStyle
                    : defaultStyle;
            }
        }

        // Віділити курсивом та підкреслити елементи 2-ї матриці,
        // які більші за суму усіх інших елементів свого стовпчика та суму усіх інших елементів свого рядка.
        private void Task2_Click(object sender, RoutedEventArgs e)
        {
            var matrix = DataTableToMatrix(_dataTable2);
            if (matrix is null) return;
            var (m, n) = (matrix.GetLength(0), matrix.GetLength(1));
            var (rowSums, columnSums) = (new int[m], new int[n]);
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    rowSums[i] += matrix[i, j];
                    columnSums[j] += matrix[i, j];
                }
            }

            for (int i = 0; i < m; i++)
            {
                var row = (DataGridRow)DataGrid2.ItemContainerGenerator.ContainerFromIndex(i);
                if (row == null) continue;
                for (int j = 0; j < n; j++)
                {
                    if (DataGrid2.Columns[j].GetCellContent(row) is TextBlock cellContent)
                    {
                        (cellContent.TextDecorations, cellContent.FontStyle) =
                            2 * matrix[i, j] > Math.Max(rowSums[i], columnSums[j])
                                ? (TextDecorations.Underline, FontStyles.Italic)
                                : (null, FontStyles.Normal);
                    }

                    // Similar example demonstrating the cell access:

                    //var cellContent = DataGrid2.Columns[j].GetCellContent(row);
                    //if(cellContent?.Parent is DataGridCell cell)
                    //{
                    //    cell.Background = 2 * matrix[i, j] > Math.Max(rowSums[i], columnSums[j])
                    //        ? Brushes.DarkSalmon
                    //        : Brushes.White;
                    //}
                }
            }
        }

        private void Task3_Click(object sender, RoutedEventArgs e)
        {
            var (matrix1, matrix2) = (DataTableToMatrix(_dataTable1), DataTableToMatrix(_dataTable2));
            if (matrix1 is null || matrix2 is null) return;
            if(!AreSizesCorrect(matrix1, matrix2, out _, out _, out _, out _)) return;
            {
                var counts1 = GetUniqueCounts(matrix1);
                var counts2 = GetUniqueCounts(matrix2);
                var chartWindow = new ChartWindow(counts1, counts2);
                chartWindow.ShowDialog();
            }

            int[] GetUniqueCounts(int[,] matrix)
            {
                var uniqueCounts = new int[matrix.GetLength(0)];
                var n = matrix.GetLength(1);
                for (int i = 0; i < uniqueCounts.Length; i++)
                {
                    var rowElements = new HashSet<int>();
                    for (int j = 0; j < n; j++)
                    {
                        rowElements.Add(matrix[i, j]);
                    }

                    uniqueCounts[i] = rowElements.Count;
                }

                return uniqueCounts;
            }
        }

        private void SaveMatrix_Click(object sender, RoutedEventArgs e)
        {
            var dataTable = sender == SaveMatrix1Button || sender == SaveMatrix1MenuItem
                ? _dataTable1
                : _dataTable2;
            var saveFileDialog = new SaveFileDialog
            {
                InitialDirectory = _defaultDataDirectoryPath,
                Filter = "Text files (*.txt)|*.txt",
                DefaultExt = ".txt",
                FileName = ""
            };
            if (dataTable is null || saveFileDialog.ShowDialog() != true) return;
            using var writer = new StreamWriter(saveFileDialog.FileName);
            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                var currentLine = string.Join(" ", dataTable.Rows[i].ItemArray);
                writer.WriteLine(currentLine);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}