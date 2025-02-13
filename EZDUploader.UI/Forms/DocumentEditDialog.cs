using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using EZDUploader.Core.Models;

namespace EZDUploader.UI.Forms
{
    public class DocumentEditDialog : Form
    {
        private TextBox titleTextBox;
        private DateTimePicker datePicker;
        private CheckBox noBrakDaty;
        private TextBox signatureTextBox;
        private CheckBox noBrakZnaku;
        private ComboBox documentTypeCombo;
        private Button okButton;
        private Button cancelButton;
        private readonly List<UploadFile> _files;

        public DocumentEditDialog(List<UploadFile> files)
        {
            _files = files;
            InitializeComponents();
            LoadFilesData();
            Text = _files.Count == 1
                ? "Edycja dokumentu"
                : $"Edycja {_files.Count} dokumentów";
        }

        private void LoadFilesData()
        {
            if (_files.Count == 1)
            {
                // Edycja pojedynczego pliku
                var file = _files[0];
                titleTextBox.Text = file.FileName;
                datePicker.Value = file.AddedDate;
                noBrakDaty.Checked = file.BrakDaty;
                datePicker.Enabled = !file.BrakDaty;
                signatureTextBox.Text = file.NumerPisma;
                noBrakZnaku.Checked = file.BrakZnaku;
                signatureTextBox.Enabled = !file.BrakZnaku;

                if (!string.IsNullOrEmpty(file.DocumentType))
                {
                    var index = documentTypeCombo.Items.IndexOf(file.DocumentType);
                    documentTypeCombo.SelectedIndex = index >= 0 ? index : 0;
                }
            }
            else
            {
                // Edycja wielu plików - pokazujemy wspólne wartości lub "(różne)"
                titleTextBox.Text = HasSameValue(f => f.FileName) ? _files[0].FileName : "(różne)";
                datePicker.Value = HasSameValue(f => f.AddedDate) ? _files[0].AddedDate : DateTime.Now;

                var allBrakDaty = _files.All(f => f.BrakDaty);
                noBrakDaty.Checked = allBrakDaty;
                datePicker.Enabled = !allBrakDaty;

                signatureTextBox.Text = HasSameValue(f => f.NumerPisma) ? _files[0].NumerPisma : "(różne)";
                var allBrakZnaku = _files.All(f => f.BrakZnaku);
                noBrakZnaku.Checked = allBrakZnaku;
                signatureTextBox.Enabled = !allBrakZnaku;

                if (HasSameValue(f => f.DocumentType))
                {
                    var docType = _files[0].DocumentType;
                    if (!string.IsNullOrEmpty(docType))
                    {
                        var index = documentTypeCombo.Items.IndexOf(docType);
                        documentTypeCombo.SelectedIndex = index >= 0 ? index : 0;
                    }
                }
                else
                {
                    documentTypeCombo.SelectedIndex = 0;
                }
            }
        }

        private bool HasSameValue<T>(Func<UploadFile, T> selector)
        {
            return _files.Select(selector).Distinct().Count() == 1;
        }

        private void InitializeComponents()
        {
            Size = new Size(500, 400);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Tytuł
            var titleLabel = new Label
            {
                Text = "Tytuł dokumentu:",
                Location = new Point(10, 20),
                AutoSize = true
            };

            titleTextBox = new TextBox
            {
                Location = new Point(10, 40),
                Width = 460
            };

            var titleInfo = new Label
            {
                Text = "Tytuł musi składać się z co najmniej dwóch wyrazów",
                Location = new Point(10, 65),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font(Font, FontStyle.Italic)
            };

            // Data
            var dateLabel = new Label
            {
                Text = "Data na piśmie:",
                Location = new Point(10, 100),
                AutoSize = true
            };

            datePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(10, 120),
                Width = 200
            };

            noBrakDaty = new CheckBox
            {
                Text = "Brak daty na piśmie",
                Location = new Point(220, 120),
                AutoSize = true
            };

            noBrakDaty.CheckedChanged += (s, e) => {
                datePicker.Enabled = !noBrakDaty.Checked;
                if (noBrakDaty.Checked)
                    datePicker.Value = DateTime.Now;
            };

            // Znak pisma
            var signatureLabel = new Label
            {
                Text = "Znak pisma:",
                Location = new Point(10, 160),
                AutoSize = true
            };

            signatureTextBox = new TextBox
            {
                Location = new Point(10, 180),
                Width = 200
            };

            noBrakZnaku = new CheckBox
            {
                Text = "Brak znaku pisma",
                Location = new Point(220, 180),
                AutoSize = true
            };

            noBrakZnaku.CheckedChanged += (s, e) => {
                signatureTextBox.Enabled = !noBrakZnaku.Checked;
                if (noBrakZnaku.Checked)
                    signatureTextBox.Text = string.Empty;
            };

            // Rodzaj dokumentu
            var typeLabel = new Label
            {
                Text = "Rodzaj dokumentu:",
                Location = new Point(10, 220),
                AutoSize = true
            };

            documentTypeCombo = new ComboBox
            {
                Location = new Point(10, 240),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            documentTypeCombo.Items.AddRange(new[]
            {
                "Pismo",
                "Notatka",
                "Wniosek",
                "Decyzja",
                "Opinia",
                "Zaświadczenie",
                "Inny"
            });

            // Przyciski
            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(300, 300),
                Width = 80
            };

            cancelButton = new Button
            {
                Text = "Anuluj",
                DialogResult = DialogResult.Cancel,
                Location = new Point(390, 300),
                Width = 80
            };

            okButton.Click += OkButton_Click;

            mainPanel.Controls.AddRange(new Control[]
            {
                titleLabel,
                titleTextBox,
                titleInfo,
                dateLabel,
                datePicker,
                noBrakDaty,
                signatureLabel,
                signatureTextBox,
                noBrakZnaku,
                typeLabel,
                documentTypeCombo,
                okButton,
                cancelButton
            });

            Controls.Add(mainPanel);
            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            // Walidacja tytułu jeśli został zmieniony
            if (titleTextBox.Text != "(różne)")
            {
                var words = titleTextBox.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length < 2)
                {
                    MessageBox.Show("Tytuł musi składać się z co najmniej dwóch wyrazów",
                        "Walidacja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    titleTextBox.Focus();
                    return;
                }
            }

            // Aktualizacja wszystkich plików
            foreach (var file in _files)
            {
                // Aktualizujemy tylko te pola, które nie są oznaczone jako "(różne)"
                if (titleTextBox.Text != "(różne)")
                    file.FileName = titleTextBox.Text;

                if (!noBrakDaty.Checked)
                    file.AddedDate = datePicker.Value;

                file.BrakDaty = noBrakDaty.Checked;
                file.BrakZnaku = noBrakZnaku.Checked;

                if (!noBrakZnaku.Checked && signatureTextBox.Text != "(różne)")
                    file.NumerPisma = signatureTextBox.Text;

                file.DocumentType = documentTypeCombo.SelectedItem?.ToString();
            }
        }
    }
}