using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Models;
using EZDUploader.Core.Validators;
using EZDUploader.UI.Forms;

namespace EZDUploader.UI
{

    public partial class MainForm : Form
    {
        private readonly IFileUploadService _fileUploadService;
        private readonly IEzdApiService _ezdService;
        private readonly IFileValidator _fileValidator;

        // Kontrolki UI
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;
        private ToolStripStatusLabel userStatusLabel;
        private ListView filesListView;
        private TextBox caseIdTextBox;
        private Label progressLabel;
        private Button uploadButton;
        private ContextMenuStrip contextMenu;

        public MainForm(IFileUploadService fileUploadService, IEzdApiService ezdService, IFileValidator fileValidator)
        {
            InitializeComponent();  // Najpierw wywołujemy metodę designera
            _fileUploadService = fileUploadService;
            _ezdService = ezdService;
            _fileValidator = fileValidator;
            filesListView = new ListView();
            InitializeUI();        // Potem nasza inicjalizacja
            InitializeEvents();

            this.HandleCreated += async (s, e) =>
            {
                try
                {
                    Debug.WriteLine("Inicjalizacja MainForm...");
                    await UpdateUserStatus();

                    if (_ezdService.IsAuthenticated)
                    {
                        Debug.WriteLine("Serwis jest już zalogowany");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Błąd podczas inicjalizacji MainForm: {ex.Message}");
                    await UpdateUserStatus();
                }
            };
        }

        private void menuItemSettings_Click(object sender, EventArgs e)
        {
            var currentSettings = ConfigurationManager.LoadSettings();
            using var settingsForm = new SettingsForm(currentSettings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                ConfigurationManager.SaveSettings(currentSettings);
            }
        }

        private void InitializeUI()
        {
            this.Size = new Size(1024, 768);
            this.Text = "EZD Uploader";



            // MenuStrip musi być pierwszy
            menuStrip = new MenuStrip();
            this.Controls.Add(menuStrip);

            // ToolStrip drugi
            toolStrip = new ToolStrip();
            var addButton = new ToolStripButton("Dodaj");
            addButton.Click += async (s, e) => await AddDocuments();
            toolStrip.Items.Add(addButton);
            this.Controls.Add(toolStrip);

            // Panel główny
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Panel zarządzania uploadem
            var uploadPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5),
                BackColor = SystemColors.Control  // Dodajemy kolor tła dla widoczności
            };

            var caseIdLabel = new Label
            {
                Text = "ID Koszulki:",
                Location = new Point(5, 10),
                AutoSize = true
            };

            caseIdTextBox = new TextBox
            {
                Width = 100,
                Location = new Point(80, 8)
            };

            uploadButton = new Button
            {
                Text = "Wyślij do sprawy",
                Location = new Point(190, 7),
                Width = 120
            };
            uploadButton.Click += uploadButton_Click;

            uploadPanel.Controls.AddRange(new Control[] { caseIdLabel, caseIdTextBox, uploadButton });

            // Lista plików
            filesListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                AllowDrop = true,
                FullRowSelect = true,
                GridLines = true,
                BackColor = SystemColors.Window  // Dodajemy kolor tła dla widoczności
            };


            // Podpięcie zdarzeń drag&drop
            filesListView.DragEnter += FilesListView_DragEnter;
            filesListView.DragDrop += FilesListView_DragDrop;

            // StatusStrip musi być na samym końcu
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Gotowy");
            progressBar = new ToolStripProgressBar();
            userStatusLabel = new ToolStripStatusLabel("Nie zalogowano");

            statusStrip.Items.AddRange(new ToolStripItem[]
            {
        statusLabel,
        progressBar,
        new ToolStripSeparator(),
        userStatusLabel
            });

            // Bardzo ważna kolejność dodawania kontrolek do formularza:
            mainPanel.Controls.Add(filesListView);  // Najpierw lista (wypełni całą przestrzeń)
            mainPanel.Controls.Add(uploadPanel);    // Potem panel uploadu (będzie na górze)

            // Dodajemy kontrolki do formularza w odpowiedniej kolejności:
            this.Controls.Add(mainPanel);      // Panel główny
            this.Controls.Add(statusStrip);    // Status na końcu

            filesListView.Columns.Clear();
            filesListView.Columns.AddRange(new[]
            {
    new ColumnHeader { Text = "Tytuł", Width = 300 },
    new ColumnHeader { Text = "Rozmiar", Width = 100 },
    new ColumnHeader { Text = "Data na piśmie", Width = 150 },
    new ColumnHeader { Text = "Znak pisma", Width = 150 },
    new ColumnHeader { Text = "Rodzaj", Width = 150 }
});
            filesListView.LabelEdit = true;
            filesListView.DoubleClick += FilesListView_DoubleClick;
            filesListView.MouseClick += FilesListView_MouseClick;
            InitializeContextMenu();
        }
        private void InitializeEvents()
        {
            var fileMenu = new ToolStripMenuItem("Plik");
            var settingsMenu = new ToolStripMenuItem("Ustawienia");

            fileMenu.DropDownItems.Add("Wyjście", null, (s, e) => Application.Exit());
            settingsMenu.DropDownItems.Add("Konfiguracja API", null, ConfigureApiSettings);
            settingsMenu.DropDownItems.Add(new ToolStripSeparator());
            settingsMenu.DropDownItems.Add("Odśwież połączenie", null, RefreshConnection);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(settingsMenu);

            this.Load += (s, e) =>
            {
                var settings = ConfigurationManager.LoadSettings();
                if (!string.IsNullOrEmpty(settings.BaseUrl))
                {
                    RefreshConnection(null, null);
                }
            };
        }

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Edytuj", null, EditDocument_Click);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Wyślij do EZD", null, SendToEzd_Click);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Usuń", null, RemoveFiles_Click);
            filesListView.ContextMenuStrip = contextMenu;

            // Aktualizuj dostępność menu kontekstowego
            filesListView.SelectedIndexChanged += (s, e) =>
            {
                bool hasSelection = filesListView.SelectedItems.Count > 0;
                foreach (ToolStripItem item in contextMenu.Items)
                {
                    item.Enabled = hasSelection;
                }
            };
        }

        private async void RemoveFiles_Click(object sender, EventArgs e)
        {
            if (filesListView.SelectedItems.Count == 0) return;

            var message = filesListView.SelectedItems.Count == 1
                ? "Czy na pewno chcesz usunąć wybrany plik?"
                : $"Czy na pewno chcesz usunąć {filesListView.SelectedItems.Count} wybranych plików?";

            if (MessageBox.Show(message, "Potwierdzenie", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var files = filesListView.SelectedItems.Cast<ListViewItem>()
                    .Select(item => (UploadFile)item.Tag);
                await _fileUploadService.RemoveFiles(files);
                RefreshFilesList();
            }
        }

        private void RenameFiles_Click(object sender, EventArgs e)
        {
            if (filesListView.SelectedItems.Count == 0) return;

            var firstFile = (UploadFile)filesListView.SelectedItems[0].Tag;
            using var dialog = new InputDialog("Zmiana nazwy", "Podaj nową nazwę pliku:", firstFile.FileName);

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem item in filesListView.SelectedItems)
                {
                    var file = (UploadFile)item.Tag;
                    file.FileName = dialog.InputText;
                }
                RefreshFilesList();
            }
        }

        private void ChangeDates_Click(object sender, EventArgs e)
        {
            if (filesListView.SelectedItems.Count == 0) return;

            var firstFile = (UploadFile)filesListView.SelectedItems[0].Tag;
            using var dialog = new DateOptionsDialog("Zmiana daty", firstFile.AddedDate, firstFile.BrakDaty);

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem item in filesListView.SelectedItems)
                {
                    var file = (UploadFile)item.Tag;
                    file.AddedDate = dialog.SelectedDate;
                    file.BrakDaty = dialog.BrakDaty;
                }
                RefreshFilesList();
            }
        }

        private void ChangeSignature_Click(object sender, EventArgs e)
        {
            if (filesListView.SelectedItems.Count == 0) return;

            var firstFile = (UploadFile)filesListView.SelectedItems[0].Tag;
            using var dialog = new SignatureOptionsDialog("Znak pisma", firstFile.NumerPisma);

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem item in filesListView.SelectedItems)
                {
                    var file = (UploadFile)item.Tag;
                    file.NumerPisma = dialog.SignatureNumber;
                }
                RefreshFilesList();
            }
        }

        private class DateOptionsDialog : Form
        {
            private DateTimePicker datePicker;
            private CheckBox noBrakDaty;
            public DateTime SelectedDate => datePicker.Value;
            public bool BrakDaty => noBrakDaty.Checked;

            public DateOptionsDialog(string title, DateTime defaultDate, bool brakDaty = true)
            {
                Text = title;
                Size = new Size(300, 200);
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;

                datePicker = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Value = defaultDate,
                    Location = new Point(10, 20),
                    Width = 260
                };

                noBrakDaty = new CheckBox
                {
                    Text = "Brak daty na piśmie",
                    Location = new Point(10, 50),
                    Checked = brakDaty
                };

                noBrakDaty.CheckedChanged += (s, e) => datePicker.Enabled = !noBrakDaty.Checked;

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(100, 110)
                };

                var cancelButton = new Button
                {
                    Text = "Anuluj",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(190, 110)
                };

                AcceptButton = okButton;
                CancelButton = cancelButton;
                Controls.AddRange(new Control[] { datePicker, noBrakDaty, okButton, cancelButton });

                // Ustaw początkowy stan kontrolki daty
                datePicker.Enabled = !brakDaty;
            }
        }

        private int GetClickedColumn()
        {
            Point mousePos = filesListView.PointToClient(MousePosition);
            int x = mousePos.X;
            int total = 0;

            for (int i = 0; i < filesListView.Columns.Count; i++)
            {
                total += filesListView.Columns[i].Width;
                if (x <= total) return i;
            }

            return -1;
        }


        private void ChangeTypes_Click(object sender, EventArgs e)
        {
            if (filesListView.SelectedItems.Count == 0) return;

            using var dialog = new ComboBoxDialog("Wybierz rodzaj dokumentu", new[]
            {
        "Pismo", "Notatka", "Wniosek", "Decyzja", "Opinia", "Zaświadczenie", "Inny"
    });

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (ListViewItem item in filesListView.SelectedItems)
                {
                    var file = (UploadFile)item.Tag;
                    file.DocumentType = dialog.SelectedValue;
                }
                RefreshFilesList();
            }
        }

        private void ConfigureApiSettings(object sender, EventArgs e)
        {
            var currentSettings = ConfigurationManager.LoadSettings();
            using var settingsForm = new SettingsForm(currentSettings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                ConfigurationManager.SaveSettings(currentSettings);
            }
        }

        private void FilesListView_MouseClick(object sender, MouseEventArgs e)
        {
            var hitTest = filesListView.HitTest(e.Location);
            if (hitTest.Item == null) return;

            var columnIndex = GetClickedColumn();

        }

        private void EditDocument_Click(object sender, EventArgs e)
        {
            if (filesListView.SelectedItems.Count == 0) return;

            var selectedFiles = filesListView.SelectedItems.Cast<ListViewItem>()
                .Select(item => (UploadFile)item.Tag)
                .ToList();

            using var dialog = new DocumentEditDialog(selectedFiles);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RefreshFilesList();
            }
        }

        private void FilesListView_DoubleClick(object sender, EventArgs e)
        {
            if (filesListView.SelectedItems.Count == 0) return;

            var file = (UploadFile)filesListView.SelectedItems[0].Tag;
            var clickedColumn = GetClickedColumn();

            switch (clickedColumn)
            {
                case 0: // Tytuł
                case 1: // Rozmiar
                    if (!string.IsNullOrEmpty(file.FilePath))
                    {
                        try
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = file.FilePath,
                                UseShellExecute = true
                            };
                            Process.Start(startInfo);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Nie można otworzyć pliku: {ex.Message}", "Błąd",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    break;

                case 2: // Data na piśmie
                    using (var dialog = new DateOptionsDialog("Zmiana daty", file.AddedDate, file.BrakDaty))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            foreach (ListViewItem item in filesListView.SelectedItems)
                            {
                                var selectedFile = (UploadFile)item.Tag;
                                selectedFile.AddedDate = dialog.SelectedDate;
                                selectedFile.BrakDaty = dialog.BrakDaty;
                            }
                            RefreshFilesList();
                        }
                    }
                    break;

                case 3: // Znak pisma
                    using (var dialog = new SignatureOptionsDialog("Znak pisma", file.NumerPisma))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            foreach (ListViewItem item in filesListView.SelectedItems)
                            {
                                var selectedFile = (UploadFile)item.Tag;
                                selectedFile.NumerPisma = dialog.SignatureNumber;
                            }
                            RefreshFilesList();
                        }
                    }
                    break;

                case 4: // Rodzaj dokumentu
                    using (var dialog = new ComboBoxDialog("Wybierz rodzaj dokumentu", new[]
                    {
                "Pismo", "Notatka", "Wniosek", "Decyzja", "Opinia", "Zaświadczenie", "Inny"
            }))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            foreach (ListViewItem item in filesListView.SelectedItems)
                            {
                                var selectedFile = (UploadFile)item.Tag;
                                selectedFile.DocumentType = dialog.SelectedValue;
                            }
                            RefreshFilesList();
                        }
                    }
                    break;
            }
        }

        // Pomocniczy formularz do wyboru typu dokumentu
        public class ComboBoxDialog : Form
        {
            private ComboBox comboBox;
            public string SelectedValue => comboBox.SelectedItem?.ToString();

            public ComboBoxDialog(string title, string[] items)
            {
                Text = title;
                Size = new Size(300, 150);
                StartPosition = FormStartPosition.CenterParent;

                comboBox = new ComboBox
                {
                    Dock = DockStyle.Top,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Dodaj elementy do ComboBox
                comboBox.Items.AddRange(new[]
                {
            "Pismo",
            "Notatka",
            "Wniosek",
            "Decyzja",
            "Opinia",
            "Zaświadczenie",
            "Inny"
        });

                comboBox.SelectedIndex = 0;

                var btnOk = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Dock = DockStyle.Bottom
                };

                Controls.AddRange(new Control[] { comboBox, btnOk });
            }
        }

        private async void SendToEzd_Click(object sender, EventArgs e)
        {
            if (filesListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Wybierz pliki do wysłania", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedFiles = filesListView.SelectedItems.Cast<ListViewItem>()
                .Select(item => (UploadFile)item.Tag)
                .Where(f => f.Status != UploadStatus.Completed)
                .ToList();

            // Walidacja przed otwarciem dialogu
            foreach (var file in selectedFiles)
            {
                var validationError = _fileValidator.GetFileValidationError(file.FileName);
                if (validationError != null)
                {
                    MessageBox.Show($"Błąd walidacji pliku {file.FileName}: {validationError}",
                        "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            using var dialog = new Forms.UploadDialog(_ezdService, _fileUploadService, _fileValidator, selectedFiles);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RefreshFilesList();
                SetStatus("Pliki zostały wysłane", false);
                await Task.Delay(2000);
                SetStatus("Gotowy", false);
            }
        }

        private async Task UpdateUserStatus()
        {
            if (_ezdService.IsAuthenticated)
            {
                userStatusLabel.Text = "Połączono";
                userStatusLabel.ForeColor = Color.Green;
            }
            else
            {
                userStatusLabel.Text = "Nie zalogowano";
                userStatusLabel.ForeColor = Color.Red;
            }
        }

        private async void RefreshConnection(object sender, EventArgs e)
        {
            try
            {
                var settings = ConfigurationManager.LoadSettings();
                if (settings.AuthType == AuthenticationType.Token)
                {
                    _ezdService.SetupTokenAuth(settings.ApplicationToken);
                }
                else
                {
                    await _ezdService.LoginAsync(settings.Login, settings.Password);
                }
                await UpdateUserStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd połączenia: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task AddDocuments()
        {
            using var dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SetStatus("Dodawanie dokumentów...", true);
                await _fileUploadService.AddFiles(dialog.FileNames);
                RefreshFilesList();
                SetStatus("Gotowy", false);
            }
        }

        // Dodaj te metody do klasy MainForm:

        private void FilesListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                statusLabel.Text = "Upuść pliki aby je dodać...";
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void EnableControls(bool enabled)
        {
            filesListView.Enabled = enabled;
            uploadButton.Enabled = enabled;
            caseIdTextBox.Enabled = enabled;
            toolStrip.Enabled = enabled;
        }

        private async void FilesListView_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                    if (files != null && files.Length > 0)
                    {
                        EnableControls(false);
                        SetStatus("Dodawanie plików...", true);
                        progressBar.Value = 0;
                        progressBar.Maximum = files.Length;

                        await _fileUploadService.AddFiles(files);

                        // Odśwież listę po dodaniu
                        RefreshFilesList();
                        SetStatus($"Dodano {files.Length} plików", false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas dodawania plików: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Wystąpił błąd podczas dodawania plików", false);
            }
            finally
            {
                EnableControls(true);
                progressBar.Value = 0;
            }
        }

        private void RefreshFilesList()
        {
            filesListView.BeginUpdate();
            filesListView.Items.Clear();

            foreach (var file in _fileUploadService.Files)
            {
                var item = new ListViewItem(file.FileName);
                item.SubItems.Add(FormatFileSize(file.FileSize));
                item.SubItems.Add(file.BrakDaty ? "Brak daty" : file.AddedDate.ToString("yyyy-MM-dd"));
                item.SubItems.Add(file.BrakZnaku ? "Brak znaku" : (file.NumerPisma ?? "-"));
                item.SubItems.Add(file.DocumentType ?? "-");
                item.Tag = file;

                switch (file.Status)
                {
                    case UploadStatus.Completed:
                        item.ForeColor = Color.Green;
                        break;
                    case UploadStatus.Failed:
                        item.ForeColor = Color.Red;
                        break;
                    case UploadStatus.Uploading:
                        item.ForeColor = Color.Blue;
                        break;
                }

                filesListView.Items.Add(item);
            }
            filesListView.EndUpdate();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        private string GetStatusText(UploadStatus status)
        {
            return status switch
            {
                UploadStatus.Pending => "Oczekuje",
                UploadStatus.Uploading => "Wysyłanie",
                UploadStatus.Completed => "Zakończono",
                UploadStatus.Failed => "Błąd",
                _ => "Nieznany"
            };
        }

        private void SetStatus(string message, bool showProgress)
        {
            statusLabel.Text = message;
            progressBar.Visible = showProgress;
        }
        private async void uploadButton_Click(object sender, EventArgs e)
        {
            if (!_fileUploadService.Files.Any())
            {
                MessageBox.Show("Brak plików do wysłania", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var dialog = new Forms.UploadDialog(
                 _ezdService,
                 _fileUploadService,
                 _fileValidator,
                 _fileUploadService.Files.Where(f => f.Status != UploadStatus.Completed)
             );

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RefreshFilesList();
                    SetStatus("Pliki zostały wysłane", false);
                    await Task.Delay(2000); // Pokazuj komunikat sukcesu przez 2 sekundy
                    SetStatus("Gotowy", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wysyłania: {ex.Message}", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Wystąpił błąd podczas wysyłania", false);
            }
        }

        private class InputDialog : Form
        {
            private TextBox textBox;
            public string InputText => textBox.Text;

            public InputDialog(string title, string prompt, string defaultValue = "")
            {
                Text = title;
                Size = new Size(400, 150);
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;

                var label = new Label
                {
                    Text = prompt,
                    Location = new Point(10, 15),
                    AutoSize = true
                };

                textBox = new TextBox
                {
                    Text = defaultValue,
                    Location = new Point(10, 35),
                    Width = 360
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(200, 70)
                };

                var cancelButton = new Button
                {
                    Text = "Anuluj",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(290, 70)
                };

                AcceptButton = okButton;
                CancelButton = cancelButton;
                Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            }
        }

        private class DatePickerDialog : Form
        {
            private DateTimePicker datePicker;
            public DateTime SelectedDate => datePicker.Value;

            public DatePickerDialog(string title, DateTime defaultDate)
            {
                Text = title;
                Size = new Size(300, 150);
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;

                datePicker = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Value = defaultDate,
                    Location = new Point(10, 20),
                    Width = 260
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(100, 60)
                };

                var cancelButton = new Button
                {
                    Text = "Anuluj",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(190, 60)
                };

                AcceptButton = okButton;
                CancelButton = cancelButton;
                Controls.AddRange(new Control[] { datePicker, okButton, cancelButton });
            }
        }

        private class SignatureOptionsDialog : Form
        {
            private TextBox signatureBox;
            private CheckBox noSignatureCheck;
            public string SignatureNumber => noSignatureCheck.Checked ? null : signatureBox.Text;

            public SignatureOptionsDialog(string title, string currentSignature = null)
            {
                Text = title;
                Size = new Size(400, 200);
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;

                noSignatureCheck = new CheckBox
                {
                    Text = "Brak znaku pisma",
                    Location = new Point(10, 20),
                    Checked = string.IsNullOrEmpty(currentSignature),
                    AutoSize = true
                };

                signatureBox = new TextBox
                {
                    Text = currentSignature,
                    Location = new Point(10, 50),
                    Width = 360,
                    Enabled = !string.IsNullOrEmpty(currentSignature)
                };

                noSignatureCheck.CheckedChanged += (s, e) =>
                    signatureBox.Enabled = !noSignatureCheck.Checked;

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(200, 110)
                };

                var cancelButton = new Button
                {
                    Text = "Anuluj",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(290, 110)
                };

                AcceptButton = okButton;
                CancelButton = cancelButton;
                Controls.AddRange(new Control[] {
            noSignatureCheck,
            signatureBox,
            okButton,
            cancelButton
        });
            }
        }

    }
}