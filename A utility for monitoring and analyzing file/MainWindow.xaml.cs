using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace A_utility_for_monitoring_and_analyzing_file
{
    public partial class MainWindow : Window
    {
        private string _directoryPath;
        private FileSystemWatcher _watcher;

        public MainWindow()
        {
            InitializeComponent();
            _directoryPath = string.Empty;
            _watcher = null;
        }

        // Рекурсивное сканирование каталога
        private async Task ScanDirectoryAsync(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            var files = Directory.GetFiles(directoryPath);
            var directories = Directory.GetDirectories(directoryPath);

            // Подсчёт файлов и каталогов
            int fileCount = files.Length;
            int dirCount = directories.Length;

            long totalSize = files.Sum(file => new FileInfo(file).Length);

            // Отображение структуры каталога
            ObservableCollection<FileInfo> fileInfos = new ObservableCollection<FileInfo>();
            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                fileInfos.Add(fileInfo);
            }

            // Отображение в DataGrid (или TreeView)
            FilesDataGrid.ItemsSource = fileInfos;

            // Пример вывода статистики
            FileCountLabel.Content = $"Files: {fileCount}";
            DirectoryCountLabel.Content = $"Directories: {dirCount}";
            TotalSizeLabel.Content = $"Total Size: {totalSize / (1024 * 1024)} MB";

            await Task.Delay(100); // Асинхронность, чтобы интерфейс не блокировался
        }

        // Слежение за изменениями в каталоге
        private void StartMonitoring(string directoryPath)
        {
            _watcher = new FileSystemWatcher(directoryPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _watcher.Created += (sender, e) => UpdateUI();
            _watcher.Changed += (sender, e) => UpdateUI();
            _watcher.Deleted += (sender, e) => UpdateUI();
            _watcher.Renamed += (sender, e) => UpdateUI();

            _watcher.EnableRaisingEvents = true;
        }

        // Обновление UI после изменений
        private async void UpdateUI()
        {
            await ScanDirectoryAsync(_directoryPath);
        }

        // Выбор первого каталога
        private void SelectDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            var result = dialog.ShowDialog();
            if (result == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                _directoryPath = dialog.FileName;
                DirectoryPathTextBox.Text = _directoryPath;
                ScanDirectoryAsync(_directoryPath);
                StartMonitoring(_directoryPath);
            }
        }

        // Выбор второго каталога для сравнения
        private void SelectDirectoryButton2_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            var result = dialog.ShowDialog();
            if (result == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                string directoryPath2 = dialog.FileName;
                DirectoryPathTextBox2.Text = directoryPath2;
            }
        }

        // Сравнение двух каталогов
        private void CompareDirectoriesButton_Click(object sender, RoutedEventArgs e)
        {
            string dir1 = DirectoryPathTextBox.Text;
            string dir2 = DirectoryPathTextBox2.Text;

            // Проверка на пустоту директорий
            if (string.IsNullOrEmpty(dir1) || string.IsNullOrEmpty(dir2))
            {
                MessageBox.Show("Пожалуйста, выберите оба каталога для сравнения.");
                return;
            }

            // Получение файлов из обоих каталогов
            var dir1Files = Directory.GetFiles(dir1);
            var dir2Files = Directory.GetFiles(dir2);

            // Сравнение файлов по имени, размеру и дате изменения
            var uniqueToDir1 = dir1Files.Except(dir2Files).Select(file => new FileInfo(file).Name);
            var uniqueToDir2 = dir2Files.Except(dir1Files).Select(file => new FileInfo(file).Name);

            var commonFiles = dir1Files.Intersect(dir2Files)
                .Select(file => new FileInfo(file))
                .Where(file1 => new FileInfo(file1.FullName).Length != new FileInfo(dir2Files.First(f => Path.GetFileName(f) == Path.GetFileName(file1.FullName))).Length);


            // Вывод различий
            DifferencesListBox.Items.Clear();
            foreach (var file in uniqueToDir1)
            {
                DifferencesListBox.Items.Add($"Файл {file} только в первом каталоге.");
            }

            foreach (var file in uniqueToDir2)
            {
                DifferencesListBox.Items.Add($"Файл {file} только во втором каталоге.");
            }

            foreach (var file in commonFiles)
            {
                DifferencesListBox.Items.Add($"Файл {file} отличается по размеру или дате.");
            }
        }

        // Экспорт структуры каталога в файл
        private void ExportToFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                using (StreamWriter sw = new StreamWriter(dialog.FileName))
                {
                    // Запись информации о каталоге или сравнении
                    sw.WriteLine("Directory Structure:");
                    foreach (var item in FilesDataGrid.ItemsSource)
                    {
                        var file = item as FileInfo;
                        if (file != null)
                        {
                            sw.WriteLine($"{file.Name}, {file.Length}, {file.LastWriteTime}");
                        }
                    }
                }
            }
        }
    }
}
