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
using ProjectOff.Forms;

namespace ProjectOff
{
    public partial class MainForm : Form
    {
        private CancellationTokenSource shutdownToken;
        private const int MaxSeconds = 24 * 60 * 60; // Максимальное количество секунд (24 часа)
        private DataTable presetsDataTable;
        private PresetManager presetManager = new PresetManager();

        public MainForm()
        {
            InitializeComponent();
            WebClient webClient = new WebClient();
            var client = new WebClient();
            webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);

            if (!webClient.DownloadString("https://www.dropbox.com/scl/fi/e3dkg7hsmhz3dald7q2n3/Update.txt?rlkey=1caeslgdoknbt853zwk7bskjb&dl=1").Contains("1.0.6"))
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
            guna2ComboBox1.SelectedIndex = 0;

            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("Файл");
            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Открыть");
            ToolStripMenuItem saveMenuItem = new ToolStripMenuItem("Сохранить");

            openMenuItem.Click += открытьToolStripMenuItem_Click;
            saveMenuItem.Click += сохранитьToolStripMenuItem_Click;

            menuStrip1.BackColor = Color.FromArgb(21, 23, 25);
            menuStrip1.ForeColor = Color.White;

            FormBorderStyle = FormBorderStyle.FixedSingle;
            Width = 660;
            Height = 390;
            guna2DataGridView1.Columns[0].Visible = false;
            guna2DataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Центрирование заголовков столбцов
            guna2DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Центрирование заголовка строк
            guna2DataGridView1.RowHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Центрирование заголовка столбца
            foreach (DataGridViewColumn column in guna2DataGridView1.Columns)
            {
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

        }

        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            string selectedAction = guna2ComboBox1.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedAction))
            {
                MessageBox.Show("Пожалуйста, выберите действие в комбобоксе", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (int.TryParse(guna2TextBox1.Text, out int seconds))
            {
                guna2TextBox1.Clear();
                seconds = Math.Min(seconds, MaxSeconds);

                MessageBox.Show($"Таймер на {seconds} секунд запущен!", selectedAction, MessageBoxButtons.OK, MessageBoxIcon.Information);

                shutdownToken = new CancellationTokenSource();
                guna2Button2.Enabled = true;
                guna2Button1.Enabled = false;

                for (int i = seconds; i > 0; i--)
                {
                    if (shutdownToken.Token.IsCancellationRequested)
                    {
                        UpdateStatusLabel($"{selectedAction} отменено");
                        await Task.Delay(2000);
                        UpdateStatusLabel("");
                        break;
                    }

                    label1.Visible = true;
                    if (selectedAction == "Выключение")
                    {
                        UpdateStatusLabel($"Осталось секунд до выключения ПК: {i}");
                    }
                    else if (selectedAction == "Перезагрузка")
                    {
                        UpdateStatusLabel($"Осталось секунд до перезагрузки ПК: {i}");
                    }
                    else if (selectedAction == "Спящий режим")
                    {
                        UpdateStatusLabel($"Осталось секунд до спящего режима ПК: {i}");
                    }

                    await Task.Delay(1000);
                }

                if (!shutdownToken.Token.IsCancellationRequested)
                {
                    switch (selectedAction)
                    {
                        case "Выключение":
                            ShutdownManager.Shutdown();
                            break;
                        case "Перезагрузка":
                            ShutdownManager.Restart();
                            break;
                        case "Спящий режим":
                            ShutdownManager.Sleep();
                            break;
                        default:
                            break;
                    }
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

        private void UpdateStatusLabel(string format, params object[] args)
        {
            string statusMessage = string.Format(format, args);
            if (label1 != null)
            {
                if (label1.InvokeRequired)
                {
                    Invoke(new Action(() => label1.Text = statusMessage));
                }
                else
                {
                    label1.Text = statusMessage;
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
                if (presetsDataTable.Rows[e.RowIndex]["Time"] != DBNull.Value)
                {
                    int selectedTime = (int)presetsDataTable.Rows[e.RowIndex]["Time"];
                    guna2TextBox1.Text = selectedTime.ToString();
                }
                else
                {
                    MessageBox.Show("Ваша ячейка пустая! Пожалуйста, выберите ячейку со значением.", "Пустая ячейка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
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
            InfoForm infoForm = new InfoForm();
            infoForm.ShowDialog();
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            if (guna2DataGridView1.SelectedRows.Count > 0)
            {
                DialogResult result = MessageBox.Show("Вы уверены, что хотите удалить выбранную строку?", "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in guna2DataGridView1.SelectedRows)
                    {
                        if (!string.IsNullOrWhiteSpace(row.Cells["Time"].Value?.ToString()))
                        {
                            presetsDataTable.Rows.RemoveAt(row.Index);
                            MessageBox.Show("Выбранная строка удалена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Невозможно удалить пустую строку", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите строку для удаления", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
