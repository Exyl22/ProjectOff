using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace ProjectOff
{
    public partial class MainForm : Form
    {
        private System.Windows.Forms.Timer hideLabelTimer;
        private CancellationTokenSource shutdownToken;

        public MainForm()
        {
            InitializeComponent();
            hideLabelTimer = new System.Windows.Forms.Timer();
            hideLabelTimer.Interval = 5000; // 5 секунд
            hideLabelTimer.Tick += HideLabelTimer_Tick;
            label1.Visible = false; // Изначально скрываем лейбл
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(guna2TextBox1.Text, out int seconds))
            {
                // Очистить текстбокс
                guna2TextBox1.Clear();

                // Вывести уведомление в MessageBox и лейбле
                ShowNotification($"Компьютер будет выключен через {seconds} секунд", "Выключение", MessageBoxIcon.Information);

                // Создать токен отмены
                shutdownToken = new CancellationTokenSource();

                // Начать отсчет
                Thread countdownThread = new Thread(() =>
                {
                    for (int i = seconds; i > 0; i--)
                    {
                        // Проверка на отмену
                        if (shutdownToken.Token.IsCancellationRequested)
                        {
                            // Вызов метода для отмены уведомления в лейбле
                            CancelNotification();
                            return;
                        }

                        // Обновление Label с отображением времени
                        UpdateStatusLabel($"Осталось времени: {i} секунд");

                        Thread.Sleep(1000); // Подождать 1 секунду
                    }

                    // Выключение компьютера
                    Process.Start("shutdown", "/s /t 0");
                });
                countdownThread.Start();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректное количество секунд", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            // Проверка на заполнение текстбокса перед отменой
            if (string.IsNullOrWhiteSpace(guna2TextBox1.Text))
            {
                MessageBox.Show("Пожалуйста, заполните текстбокс", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Отменить выключение
            if (shutdownToken != null)
            {
                shutdownToken.Cancel();
                // Вывести уведомление в MessageBox и лейбле
                ShowNotification("Выключение отменено", "Отмена", MessageBoxIcon.Information);
                // Активировать текстбокс после отмены
                guna2TextBox1.Enabled = true;
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

        // Метод для вывода уведомления в MessageBox и лейбле
        private void ShowNotification(string message, string caption, MessageBoxIcon icon)
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, icon);
            UpdateStatusLabel(message);
            label1.Visible = true; // Показать лейбл
            hideLabelTimer.Start(); // Запустить таймер для автоматического скрытия лейбла
            // Деактивировать текстбокс во время отсчета
            guna2TextBox1.Enabled = false;
        }

        // Метод для отмены уведомления в лейбле
        private void CancelNotification()
        {
            UpdateStatusLabel("Выключение отменено");
            hideLabelTimer.Start(); // Запустить таймер для автоматического скрытия лейбла
            // Деактивировать текстбокс после отмены
            guna2TextBox1.Enabled = false;
        }

        // Обработчик события таймера для автоматического скрытия лейбла
        private void HideLabelTimer_Tick(object sender, EventArgs e)
        {
            label1.Visible = false; // Скрыть лейбл
            hideLabelTimer.Stop(); // Остановить таймер
            // Активировать текстбокс после скрытия лейбла
            guna2TextBox1.Enabled = true;
        }
    }
}
