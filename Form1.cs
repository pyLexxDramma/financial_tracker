using System;
using System.Collections.Generic;
using System.IO; // Для работы с файлами
using System.Linq;
using System.Windows.Forms;
using OfficeOpenXml; // Для работы с EPPlus

namespace FinancialTracker
{
    public partial class FinancialTracker : Form
    {
        private decimal balance = 0;
        private List<Transaction> transactions = new List<Transaction>();
        private int nextId = 1;

        public FinancialTracker()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial; // Установка контекста лицензии
            btnAdd.Click += btnAdd_Click;
            btnDelete.Click += btnDelete_Click;
            btnSave.Click += btnSave_Click; // Привязка обработчика для кнопки сохранения
            btnIncomeExpense.Click += btnIncomeExpense_Click; // Привязка обработчика для кнопки доход/расход
        }

        private void FinancialTracker_Load(object sender, EventArgs e)
        {
            dataGridViewTransactions.Columns.Add("Id", "Id");
            dataGridViewTransactions.Columns.Add("Description", "Описание");
            dataGridViewTransactions.Columns.Add("Amount", "Сумма");
            dataGridViewTransactions.Columns.Add("Date", "Дата");

            dataGridViewTransactions.Columns["Id"].ReadOnly = true; // Столбец Id - только для чтения
            LoadTransactionsFromExcel(@"D:\Development\FinancialTracker\transactions.xlsx"); // Загрузка существующих транзакций
            UpdateOverallBalance(); // Обновление общего баланса при загрузке
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string description = textBoxDescription.Text;
            decimal amount;
            if (decimal.TryParse(textBoxAmount.Text, out amount))
            {
                Transaction transaction = new Transaction(nextId++, description, amount, DateTime.Now);
                transactions.Add(transaction);
                UpdateBalance(amount);
                UpdateDataGridView();
                ClearInputFields();
            }
            else
            {
                MessageBox.Show("Введите корректную сумму.");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridViewTransactions.SelectedRows.Count > 0)
            {
                int selectedId = (int)dataGridViewTransactions.SelectedRows[0].Cells["Id"].Value;
                Transaction transactionToDelete = transactions.Find(t => t.Id == selectedId);

                if (transactionToDelete != null)
                {
                    transactions.Remove(transactionToDelete);
                    UpdateBalance(-transactionToDelete.Amount); // Изменение баланса
                    UpdateDataGridView();
                }
            }
            else
            {
                MessageBox.Show("Выберите транзакцию для удаления.");
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveTransactionsToExcel(@"D:\Development\FinancialTracker\transactions.xlsx");
        }

        private void SaveTransactionsToExcel(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet;

                // Проверяем, есть ли уже лист в файле
                if (package.Workbook.Worksheets.Count == 0)
                {
                    worksheet = package.Workbook.Worksheets.Add("Транзакции"); // Создаем новый лист, если его нет
                }
                else
                {
                    worksheet = package.Workbook.Worksheets[0]; // Получаем первый лист
                    worksheet.Cells.Clear(); // Очищаем существующие данные
                }

                // Устанавливаем заголовки
                worksheet.Cells[1, 1].Value = "Id";
                worksheet.Cells[1, 2].Value = "Описание";
                worksheet.Cells[1, 3].Value = "Сумма";
                worksheet.Cells[1, 4].Value = "Дата";

                int row = 2; // Начинаем со второй строки

                foreach (Transaction transaction in transactions)
                {
                    worksheet.Cells[row, 1].Value = transaction.Id;
                    worksheet.Cells[row, 2].Value = transaction.Description;
                    worksheet.Cells[row, 3].Value = transaction.Amount;
                    worksheet.Cells[row, 4].Value = transaction.Date;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "dd.MM.yyyy"; // Формат даты
                    row++;
                }

                package.Save(); // Сохраняем изменения
            }
            MessageBox.Show("Транзакции успешно сохранены в файл Excel.");
        }

        private void LoadTransactionsFromExcel(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                using (ExcelPackage package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets[0]; // Получаем первый лист
                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++) // Начинаем со второй строки
                    {
                        int id = int.Parse(worksheet.Cells[row, 1].Text);
                        string description = worksheet.Cells[row, 2].Text;
                        decimal amount = decimal.Parse(worksheet.Cells[row, 3].Text);
                        DateTime date = DateTime.Parse(worksheet.Cells[row, 4].Text);

                        transactions.Add(new Transaction(id, description, amount, date));
                        nextId = Math.Max(nextId, id + 1); // Обновляем следующий ID
                    }
                }
                UpdateDataGridView();
            }
        }

        private void btnIncomeExpense_Click(object sender, EventArgs e)
        {
            decimal totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => t.Amount < 0).Sum(t => t.Amount);
            decimal totalBalance = totalIncome + totalExpense;

            MessageBox.Show($"Общий доход: {totalIncome:C}\nОбщий расход: {Math.Abs(totalExpense):C}\nОбщий баланс: {totalBalance:C}");
        }

        // Метод для обновления общего баланса
        private void UpdateOverallBalance()
        {
            decimal totalIncome = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => t.Amount < 0).Sum(t => t.Amount);
            balance = totalIncome + totalExpense; // Устанавливаем текущий баланс
            labelBalance.Text = $"Баланс: {balance:C}"; // Обновляем лейбл с балансом
        }

        private void UpdateBalance(decimal amount)
        {
            balance += amount;
            labelBalance.Text = $"Баланс: {balance:C}"; // C - для форматирования валюты
        }

        private void UpdateDataGridView()
        {
            dataGridViewTransactions.Rows.Clear();
            foreach (Transaction transaction in transactions)
            {
                dataGridViewTransactions.Rows.Add(transaction.Id, transaction.Description, transaction.Amount, transaction.Date);
            }
        }

        private void ClearInputFields()
        {
            textBoxDescription.Clear();
            textBoxAmount.Clear();
        }

        private class Transaction
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }

            public Transaction(int id, string description, decimal amount, DateTime date)
            {
                Id = id;
                Description = description;
                Amount = amount;
                Date = date;
            }
        }
    }
}

