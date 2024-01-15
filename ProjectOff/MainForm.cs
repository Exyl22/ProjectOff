using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Net;

namespace ProjectOff
{
    public partial class MainForm : Form
    {
        private CancellationTokenSource shutdownToken;
        private const int MaxSeconds = 24 * 60 * 60; // Максимальное количество секунд (24 часа)
        private DataTable presetsDataTable;

        private const string FileName = "presets.xml";
        private const string TableName = "Presets";

        private void SerializeDataTable(DataTable dataTable, string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                dataTable.WriteXml(fs, XmlWriteMode.WriteSchema);
            }
        }

        private DataTable DeserializeDataTable(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open))
                    {
                        DataTable dataTable = new DataTable();
                        dataTable.ReadXml(fs);
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during deserialization: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            return null;
        }
        public MainForm()
        {
            InitializeComponent();

            WebClient webClient = new WebClient();
            var client = new WebClient();
            webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);

            if (!webClient.DownloadString("https://www.dropbox.com/scl/fi/e3dkg7hsmhz3dald7q2n3/Update.txt?rlkey=1caeslgdoknbt853zwk7bskjb&dl=1").Contains("1.0.0"))
            {
                if (MessageBox.Show("Новое обновление уже доступно! Хотите установить более новую версию?", "Обновление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes){
               
                try
                {
                    if(File.Exists(@".\Setup.msi")) { File.Delete(@".\Setup.msi"); }
                    client.DownloadFile("https://www.dropbox.com/scl/fi/r2wlktcj6d9h4kvngv3q0/Setup.zip?rlkey=2m75eql9k4cei4fsroc8tn19n&dl=1", @"Setup.zip");
                    string zipPath = @".\Setup.zip";
                    string extractPath = @".\";
                    ZipFile.ExtractToDirectory(zipPath, extractPath);

                    Process process = new Process();
                    process.StartInfo.FileName = "msiexec";
                    process.StartInfo.Arguments = String.Format("/i Setup.msi");

                    this.Close();
                    process.Start();
                }
                catch
                {

                }
            }
        }

            guna2TextBox1.MaxLength = MaxSeconds.ToString().Length;
            guna2TextBox2.MaxLength = MaxSeconds.ToString().Length;
            presetsDataTable = new DataTable("Presets");
            presetsDataTable.Columns.Add("PresetID", typeof(int));
            presetsDataTable.Columns.Add("Time", typeof(int));
            AddPreset(1, 10);
            AddPreset(2, 30);
            AddPreset(3, 60);
            AddPreset(4, 300);
            AddPreset(5, 600);
            // Привязка DataTable к DataGridView
            guna2DataGridView1.DataSource = presetsDataTable;
            guna2DataGridView1.CellDoubleClick += guna2DataGridView1_CellContentDoubleClick;

            // Загрузка данных из файла
            DataTable loadedDataTable = DeserializeDataTable(FileName);
            if (loadedDataTable != null)
            {
                presetsDataTable = loadedDataTable;
            }
            guna2Button2.Enabled = false;

            // Привязка DataTable к DataGridView
            guna2DataGridView1.DataSource = presetsDataTable;

            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("Файл");
            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Открыть");
            ToolStripMenuItem saveMenuItem = new ToolStripMenuItem("Сохранить");
            
            openMenuItem.Click += открытьToolStripMenuItem_Click;
            saveMenuItem.Click += сохранитьToolStripMenuItem_Click;

            // Кастомизация Menu
            menuStrip1.BackColor = Color.FromArgb(21, 23, 25);
            menuStrip1.ForeColor = Color.White;

            // Фиксация размера приложения
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Width = 655;
            Height = 400;
        }

        private void AddPreset(int id, int time)
        {
            DataRow row = presetsDataTable.NewRow();
            row["PresetID"] = id;
            row["Time"] = time;
            presetsDataTable.Rows.Add(row);
        }
        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(guna2TextBox1.Text, out int seconds))
            {
                // Очистить текст в TextBox
                guna2TextBox1.Clear();

                // Ограничить введенные секунды максимальным значением
                seconds = Math.Min(seconds, MaxSeconds);

                // Вывести уведомление
                MessageBox.Show($"Компьютер будет выключен через {seconds} секунд", "Выключение", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Создать токен отмены
                shutdownToken = new CancellationTokenSource();

                // Заблокировать кнопку отмены до завершения таймера
                guna2Button2.Enabled = true;
                guna2Button1.Enabled = false;

                // Начать отсчет
                for (int i = seconds; i > 0; i--)
                {
                    // Проверка на отмену
                    if (shutdownToken.Token.IsCancellationRequested)
                    {
                        // Вызов метода Invoke для безопасного обновления элемента управления
                        UpdateStatusLabel("Выключение отменено");

                        // Добавить задержку перед сокрытием
                        await Task.Delay(2000);

                        // Сокрыть Label
                        UpdateStatusLabel("");
                        break;
                    }
                    label1.Visible = true;
                    // Обновление Label с отображением времени
                    UpdateStatusLabel($"Осталось времени: {i} секунд");

                    await Task.Delay(1000); // Подождать 1 секунду
                }

                // Выключение компьютера
                if (!shutdownToken.Token.IsCancellationRequested)
                {
                    Process.Start("shutdown", "/s /t 0");
                }

                // Разблокировать кнопку отмены после завершения таймера
                guna2Button1.Enabled = true;
                guna2Button2.Enabled = false;
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректное количество секунд", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void guna2Button2_Click(object sender, EventArgs e)
        {
            // Отменить выключение
            if (shutdownToken != null)
            {
                shutdownToken.Cancel();
            }
        }

        // Метод для безопасного обновления элемента управления из другого потока
        private void UpdateStatusLabel(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => label1.Text = text));
            }
            else
            {
                label1.Text = text;
            }
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            if (int.TryParse(guna2TextBox2.Text, out int time))
            {
                // Генерация уникального ID
                int id = presetsDataTable.Rows.Count + 1;

                // Добавление нового пресета в DataTable
                AddPreset(id, time);

                // Очистка TextBox2
                guna2TextBox2.Clear();

                // Выведите уведомление о добавлении пресета
                MessageBox.Show($"Пресет добавлен в таблицу ({time} секунд)", "Добавление пресета", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректное число", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2DataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Проверка, что была кликнута ячейка с временем (второй столбец)
            if (e.ColumnIndex == 1 && e.RowIndex >= 0 && e.RowIndex < presetsDataTable.Rows.Count)
            {
                // Получение значения времени из выбранной строки
                int selectedTime = (int)presetsDataTable.Rows[e.RowIndex]["Time"];

                // Установка этого времени в TextBox1
                guna2TextBox1.Text = selectedTime.ToString();
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (presetsDataTable != null && presetsDataTable.Rows.Count > 0)
            {
                SerializeDataTable(presetsDataTable, FileName);
            }
        }
     
        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files|*.xml";
            openFileDialog.Title = "Выберите файл с данными";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DataTable loadedDataTable = DeserializeDataTable(openFileDialog.FileName);
                if (loadedDataTable != null)
                {
                    presetsDataTable.Clear();
                    presetsDataTable = loadedDataTable.Copy();
                    guna2DataGridView1.DataSource = presetsDataTable;
                }
            }
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML Files|*.xml";
            saveFileDialog.Title = "Выберите место для сохранения файла с данными";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SerializeDataTable(presetsDataTable, saveFileDialog.FileName);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void версияПриложенияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string version = Application.ProductVersion;
            MessageBox.Show($"Данное приложение находится на стадии не конечного продукта!\nТекущая версия приложения: {version}", "Версия приложения", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
