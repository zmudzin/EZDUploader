using System.Windows.Forms;
using System.Drawing;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;

namespace EZDUploader.UI.Forms
{
    public class UploadDialog : Form
    {
        private readonly IEzdApiService _ezdService;
        private readonly IFileUploadService _fileUploadService;
        private readonly IEnumerable<UploadFile> _files;

        private RadioButton _newFolderRadio;
        private RadioButton _existingFolderRadio;
        private TextBox _newFolderNameBox;
        private ComboBox _existingFoldersCombo;
        private Button _uploadButton;
        private Button _cancelButton;
        private ProgressBar _progressBar;
        private Label _statusLabel;

        public UploadDialog(IEzdApiService ezdService, IFileUploadService fileUploadService, 
            IEnumerable<UploadFile> files)
        {
            _ezdService = ezdService;
            _fileUploadService = fileUploadService;
            _files = files;
            
            InitializeComponents();
            LoadExistingFolders();
        }

        private void InitializeComponents()
        {
            Text = "Wyślij pliki do EZD";
            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            _newFolderRadio = new RadioButton
            {
                Text = "Utwórz nową koszulkę",
                Location = new Point(10, 20),
                Checked = true,
                AutoSize = true
            };

            _newFolderNameBox = new TextBox
            {
                Location = new Point(30, 50),
                Width = 320
            };

            var nameInfoLabel = new Label
            {
                Text = "Nazwa koszulki musi składać się z co najmniej dwóch wyrazów",
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
                Text = "Wyślij",
                Location = new Point(210, 220),
                Width = 75,
                DialogResult = DialogResult.None
            };

            _cancelButton = new Button
            {
                Text = "Anuluj",
                Location = new Point(295, 220),
                Width = 75,
                DialogResult = DialogResult.Cancel
            };

            // Events
            _newFolderRadio.CheckedChanged += (s, e) =>
            {
                _newFolderNameBox.Enabled = _newFolderRadio.Checked;
                _existingFoldersCombo.Enabled = !_newFolderRadio.Checked;
            };

            _existingFolderRadio.CheckedChanged += (s, e) =>
            {
                _newFolderNameBox.Enabled = !_existingFolderRadio.Checked;
                _existingFoldersCombo.Enabled = _existingFolderRadio.Checked;
            };

            _uploadButton.Click += uploadButton_Click;

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

                var folders = await _ezdService.PobierzIdentyfikatoryKoszulek(_ezdService.CurrentUserId.Value);
                _existingFoldersCombo.Items.Clear();

                foreach(var folderId in folders)
                {
                    var folder = await _ezdService.PobierzKoszulkePoId(folderId);
                    _existingFoldersCombo.Items.Add(new FolderItem(folder));
                }

                if (_existingFoldersCombo.Items.Count > 0)
                {
                    _existingFoldersCombo.SelectedIndex = 0;
                }
                
                _existingFoldersCombo.Enabled = _existingFolderRadio.Checked;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Błąd podczas pobierania listy koszulek: " + ex.Message,
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void uploadButton_Click(object sender, EventArgs e)
        {
            // Walidacja - czy wybrano sposób dodawania
            if (_newFolderRadio.Checked && string.IsNullOrWhiteSpace(_newFolderNameBox.Text))
            {
                MessageBox.Show("Wprowadź nazwę nowej koszulki",
                    "Walidacja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_existingFolderRadio.Checked && _existingFoldersCombo.SelectedItem == null)
            {
                MessageBox.Show("Wybierz koszulkę z listy",
                    "Walidacja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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

            try
            {
                _uploadButton.Enabled = false;
                _progressBar.Visible = true;
                _statusLabel.Text = "Przygotowanie do wysyłania...";

                int folderId;
                if (_newFolderRadio.Checked)
                {
                    var newFolder = await _ezdService.UtworzKoszulke(
                        _newFolderNameBox.Text,
                        _ezdService.CurrentUserId.Value
                    );
                    folderId = newFolder.ID;
                }
                else
                {
                    var selectedFolder = (FolderItem)_existingFoldersCombo.SelectedItem;
                    folderId = selectedFolder.Id;
                }

                var progress = new Progress<(int fileIndex, int totalFiles, int progress)>(update =>
                {
                    _progressBar.Maximum = update.totalFiles * 100;
                    var newValue = Math.Min(update.fileIndex * 100 + update.progress, _progressBar.Maximum);
                    _progressBar.Value = newValue;
                    _statusLabel.Text = $"Wysyłanie pliku {update.fileIndex} z {update.totalFiles} ({update.progress}%)";
                });

                await _fileUploadService.UploadFiles(folderId, _files, progress);

                DialogResult = DialogResult.OK;
            }
            catch (ArgumentException ex) when (ex.Message.Contains("nazwa koszulki"))
            {
                MessageBox.Show(ex.Message, "Błąd walidacji", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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