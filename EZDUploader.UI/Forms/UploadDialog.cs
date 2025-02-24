using System.Windows.Forms;
using System.Drawing;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;
using EZDUploader.Core.Validators;

namespace EZDUploader.UI.Forms
{
    public class UploadDialog : Form
    {
        private readonly IEzdApiService _ezdService;
        private readonly IFileUploadService _fileUploadService;
        private readonly IFileValidator _fileValidator;
        private readonly IEnumerable<UploadFile> _files;

        private RadioButton _newFolderRadio;
        private RadioButton _existingFolderRadio;
        private TextBox _newFolderNameBox;
        private ComboBox _existingFoldersCombo;
        private Button _uploadButton;
        private Button _cancelButton;
        private ProgressBar _progressBar;
        private Label _statusLabel;

        public UploadDialog(IEzdApiService ezdService,
     IFileUploadService fileUploadService,
     IFileValidator fileValidator,
     IEnumerable<UploadFile> files)
        {
            _ezdService = ezdService;
            _fileUploadService = fileUploadService;
            _fileValidator = fileValidator;
            _files = files;

            // Najpierw inicjalizujemy komponenty
            InitializeComponents();

            // Potem sprawdzamy konfigurację API
            if (string.IsNullOrEmpty(_ezdService.Settings.BaseUrl))
            {
                MessageBox.Show(
                    "Nie skonfigurowano połączenia z API EZD.\nSkonfiguruj połączenie w ustawieniach aplikacji.",
                    "Brak konfiguracji",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.Cancel;
                // Nie wywołujemy Close() ani return - pozwalamy formularzowi zakończyć inicjalizację
            }
            else
            {
                LoadExistingFolders();
            }
        }

        private static IEnumerable<UploadFile> ConvertToUploadFiles(string[] filePaths)
        {
            return filePaths.Select(path => new UploadFile
            {
                FilePath = path,
                FileName = Path.GetFileName(path)
            });
        }

        private void InitializeComponents()
        {
            Text = "Wyślij pliki do EZD";
            Size = new Size(400, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Sprawdzamy wybrane koszulki
            var groupedFiles = _files
                .Where(f => f.KoszulkaId.HasValue)
                .GroupBy(f => f.KoszulkaId.Value)
                .ToList();

            // Informacja o wybranych koszulkach
            if (groupedFiles.Any())
            {
                var selectedFoldersInfo = new Label
                {
                    Text = groupedFiles.Count == 1
                        ? "Wybrano koszulkę dla dokumentów"
                        : $"Wybrano {groupedFiles.Count} różne koszulki dla dokumentów",
                    Location = new Point(10, 5),
                    AutoSize = true,
                    ForeColor = Color.DarkBlue,
                    Font = new Font(Font, FontStyle.Bold)
                };
                panel.Controls.Add(selectedFoldersInfo);
            }

            _newFolderRadio = new RadioButton
            {
                Text = "Utwórz nową koszulkę",
                Location = new Point(10, 20),
                Checked = !groupedFiles.Any(),
                AutoSize = true
            };

            _newFolderNameBox = new TextBox
            {
                Location = new Point(30, 50),
                Width = 320
            };

            if (groupedFiles.Any())
            {
                _newFolderNameBox.Text = groupedFiles.Count == 1
                    ? "Dokumenty zostaną wysłane do wybranej koszulki"
                    : $"Dokumenty zostaną wysłane do {groupedFiles.Count} koszulek";
                _newFolderNameBox.ReadOnly = true;
                _newFolderNameBox.BackColor = SystemColors.Control;
                _newFolderNameBox.ForeColor = Color.DarkBlue;
            }

            var nameInfoLabel = new Label
            {
                Text = groupedFiles.Any()
                    ? ""
                    : "Nazwa koszulki musi składać się z co najmniej dwóch wyrazów",
                Location = new Point(30, 75),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font(Font, FontStyle.Italic)
            };

            _existingFolderRadio = new RadioButton
            {
                Text = "Wybierz istniejącą koszulkę",
                Location = new Point(10, 90),
                AutoSize = true
            };

            _existingFoldersCombo = new ComboBox
            {
                Location = new Point(30, 120),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            _progressBar = new ProgressBar
            {
                Location = new Point(10, 180),
                Width = 360,
                Height = 20,
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            _statusLabel = new Label
            {
                Location = new Point(10, 210),
                Width = 360,
                Height = 20,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _uploadButton = new Button
            {
                Text = groupedFiles.Any()
                    ? (groupedFiles.Count == 1 ? "Wyślij do wybranej koszulki" : "Wyślij do wybranych koszulek")
                    : "Wyślij",
                Location = new Point(210, 250),
                Width = 75,
                DialogResult = DialogResult.None
            };

            _cancelButton = new Button
            {
                Text = "Anuluj",
                Location = new Point(295, 250),
                Width = 75,
                DialogResult = DialogResult.Cancel
            };

            // Events
            _newFolderRadio.CheckedChanged += (s, e) =>
            {
                if (!groupedFiles.Any())
                {
                    _newFolderNameBox.Enabled = _newFolderRadio.Checked;
                    _existingFoldersCombo.Enabled = !_newFolderRadio.Checked;
                }
            };

            _existingFolderRadio.CheckedChanged += (s, e) =>
            {
                if (!groupedFiles.Any())
                {
                    _newFolderNameBox.Enabled = !_existingFolderRadio.Checked;
                    _existingFoldersCombo.Enabled = _existingFolderRadio.Checked;
                }
            };

            _uploadButton.Click += uploadButton_Click;

            // Jeśli są już wybrane koszulki, wyłączamy możliwość wyboru
            if (groupedFiles.Any())
            {
                _newFolderRadio.Enabled = false;
                _existingFolderRadio.Enabled = false;
                _existingFoldersCombo.Enabled = false;
            }

            panel.Controls.AddRange(new Control[] {
        _newFolderRadio,
        _newFolderNameBox,
        nameInfoLabel,
        _existingFolderRadio,
        _existingFoldersCombo,
        _progressBar,
        _statusLabel,
        _uploadButton,
        _cancelButton
    });

            Controls.Add(panel);
        }

        private async void LoadExistingFolders()
        {
            try
            {
                _existingFoldersCombo.Items.Clear();
                _existingFoldersCombo.Items.Add("Ładowanie...");
                _existingFoldersCombo.SelectedIndex = 0;
                _existingFoldersCombo.Enabled = false;

                var koszulki = await _ezdService.PobierzIdentyfikatoryKoszulek(_ezdService.CurrentUserId.Value);
                _existingFoldersCombo.Items.Clear();

                foreach (var koszulka in koszulki)
                {
                    _existingFoldersCombo.Items.Add(new FolderItem(koszulka));
                }

                if (_existingFoldersCombo.Items.Count > 0)
                {
                    _existingFoldersCombo.SelectedIndex = 0;
                }

                _existingFoldersCombo.Enabled = _existingFolderRadio.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas pobierania listy koszulek: " + ex.Message,
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void uploadButton_Click(object sender, EventArgs e)
        {
            foreach (var file in _files)
            {
                if (file.Status == UploadStatus.Failed)
                {
                    file.Status = UploadStatus.Pending;
                    file.ErrorMessage = null;
                }
            }

            // Walidacja plików
            foreach (var file in _files)
            {
                var validationError = _fileValidator.GetFileValidationError(file.FileName);
                if (validationError != null)
                {
                    MessageBox.Show($"Plik {file.FileName}: {validationError}",
                        "Błąd walidacji", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Sprawdzamy czy wszystkie pliki mają przypisaną koszulkę lub nową nazwę koszulki
            var filesWithoutKoszulka = _files
                .Where(f => !f.KoszulkaId.HasValue && string.IsNullOrEmpty(f.NowaKoszulkaNazwa))
                .ToList();

            if (filesWithoutKoszulka.Any())
            {
                // Walidacja nazwy nowej koszulki
                if (_newFolderRadio.Checked)
                {
                    var words = _newFolderNameBox.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length < 2)
                    {
                        MessageBox.Show("Nazwa koszulki musi składać się z co najmniej dwóch wyrazów",
                            "Walidacja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                var result = MessageBox.Show(
                    $"Znaleziono {filesWithoutKoszulka.Count} plików bez wybranej koszulki.\n" +
                    "Czy chcesz użyć aktualnie wybranej koszulki dla tych plików?",
                    "Brak koszulek",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    if (_newFolderRadio.Checked)
                    {
                        var newName = _newFolderNameBox.Text;
                        foreach (var file in filesWithoutKoszulka)
                        {
                            file.NowaKoszulkaNazwa = newName;
                        }
                    }
                    else
                    {
                        var selectedFolder = (FolderItem)_existingFoldersCombo.SelectedItem;
                        foreach (var file in filesWithoutKoszulka)
                        {
                            file.KoszulkaId = selectedFolder.Id;
                        }
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Wybierz koszulki dla wszystkich plików przed wysłaniem.",
                        "Informacja",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return;
                }
            }

            try
            {
                _uploadButton.Enabled = false;
                _progressBar.Visible = true;
                _statusLabel.Text = "Przygotowanie do wysyłania...";

                // Modyfikujemy zachowanie przycisku anulowania
                _cancelButton.Click += (s, ev) =>
                {
                    _fileUploadService.CancelUpload();
                    _statusLabel.Text = "Kończenie aktualnego pliku...";
                    _cancelButton.Enabled = false;
                };

                var progress = new Progress<(int fileIndex, int totalFiles, int progress)>(update =>
                {
                    _progressBar.Maximum = update.totalFiles * 100;
                    var newValue = Math.Min(update.fileIndex * 100 + update.progress, _progressBar.Maximum);
                    _progressBar.Value = newValue;
                    _statusLabel.Text = $"Wysyłanie pliku {update.fileIndex} z {update.totalFiles} ({update.progress}%)";
                });

                await _fileUploadService.UploadFiles(_files, progress);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas wysyłania: " + ex.Message,
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = "Wystąpił błąd";
            }
            finally
            {
                _uploadButton.Enabled = true;
                _progressBar.Visible = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Anuluj wysyłkę tylko jeśli użytkownik zamknął okno przed zakończeniem
            if (DialogResult != DialogResult.OK)
            {
                _fileUploadService.CancelUpload();
            }
        }

        private class FolderItem
        {
            public int Id { get; }
            public string Name { get; }

            public FolderItem(PismoDto folder)
            {
                Id = folder.ID;
                Name = folder.Nazwa;
            }

            public override string ToString() => Name;
        }
    }
}