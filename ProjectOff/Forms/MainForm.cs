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
using ProjectOff.Classes;

namespace ProjectOff
{
    public partial class MainForm : Form
    {
        private CancellationTokenSource shutdownToken;
        private const int MaxSeconds = 24 * 60 * 60; // Максимальное количество секунд (24 часа)
        private DataTable presetsDataTable;
        private PresetManager presetManager = new PresetManager();

        private const string FileName = "presets.xml";

        public MainForm()
        {
            InitializeComponent();

            WebClient webClient = new WebClient();
            var client = new WebClient();
            webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);

            if (!webClient.DownloadString("https://www.dropbox.com/scl/fi/e3dkg7hsmhz3dald7q2n3/Update.txt?rlkey=1caeslgdoknbt853zwk7bskjb&dl=1").Contains("1.0.3.0"))
            {
                if (MessageBox.Show("Новое обновление уже доступно! Хотите установить более новую версию?", "Обновление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        if (File.Exists(@".\Setup.msi")) { File.Delete(@".\Setup.msi"); }
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
            presetsDataTable = presetManager.LoadPresets();
            guna2DataGridView1.DataSource = presetsDataTable;
            guna2DataGridView1.CellDoubleClick += guna2DataGridView1_CellContentDoubleClick;

            guna2Button2.Enabled = false;

            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("Файл");
            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Открыть");
            ToolStripMenuItem saveMenuItem = new ToolStripMenuItem("Сохранить");

            openMenuItem.Click += открытьToolStripMenuItem_Click;
            saveMenuItem.Click += сохранитьToolStripMenuItem_Click;

            menuStrip1.BackColor = Color.FromArgb(21, 23, 25);
            menuStrip1.ForeColor = Color.White;

            FormBorderStyle = FormBorderStyle.FixedSingle;
            Width = 655;
            Height = 400;
        }

        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(guna2TextBox1.Text, out int seconds))
            {
                guna2TextBox1.Clear();
                seconds = Math.Min(seconds, MaxSeconds);
                MessageBox.Show($"Компьютер будет выключен через {seconds} секунд", "Выключение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                shutdownToken = new CancellationTokenSource();
                guna2Button2.Enabled = true;
                guna2Button1.Enabled = false;

                for (int i = seconds; i > 0; i--)
                {
                    if (shutdownToken.Token.IsCancellationRequested)
                    {
                        UpdateStatusLabel("Выключение отменено");
                        await Task.Delay(2000);
                        UpdateStatusLabel("");
                        break;
                    }
                    label1.Visible = true;
                    UpdateStatusLabel($"Осталось времени: {i} секунд");
                    await Task.Delay(1000);
                }

                if (!shutdownToken.Token.IsCancellationRequested)
                {
                    Process.Start("shutdown", "/s /t 0");
                }

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
            if (shutdownToken != null)
            {
                shutdownToken.Cancel();
            }
        }

        private void UpdateStatusLabel(string text)
        {
            if (label1 != null)
            {
                if (label1.InvokeRequired)
                {
                    Invoke(new Action(() => label1.Text = text));
                }
                else
                {
                    label1.Text = text;
                }
            }
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            if (int.TryParse(guna2TextBox2.Text, out int time))
            {
                int id = presetsDataTable.Rows.Count + 1;
                presetManager.AddPreset(presetsDataTable, id, time);
                guna2TextBox2.Clear();
                MessageBox.Show($"Пресет добавлен в таблицу ({time} секунд)", "Добавление пресета", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректное число", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2DataGridView1_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1 && e.RowIndex >= 0 && e.RowIndex < presetsDataTable.Rows.Count)
            {
                int selectedTime = (int)presetsDataTable.Rows[e.RowIndex]["Time"];
                guna2TextBox1.Text = selectedTime.ToString();
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (presetsDataTable != null && presetsDataTable.Rows.Count > 0)
            {
                presetManager.SavePresets(presetsDataTable);
            }
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files|*.xml";
            openFileDialog.Title = "Выберите файл с данными";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DataTable loadedDataTable = presetManager.DeserializeDataTable(openFileDialog.FileName);
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
                presetManager.SerializeDataTable(presetsDataTable, saveFileDialog.FileName);
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
            MessageBox.Show($"Текущая версия приложения: {version}\nПриложение находится на стадии тестирования.", "Версия приложения", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
