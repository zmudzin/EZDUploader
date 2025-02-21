using System.Windows.Forms;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Models;

namespace EZDUploader.UI.Forms
{
    public partial class KoszulkaSelectionDialog : Form
    {
        private readonly IEzdApiService _ezdService;
        private RadioButton rbExisting;
        private RadioButton rbNew;
        private TextBox txtNewName;
        private ComboBox existingKoszulkiCombo;
        private Button btnOK;
        private Button btnCancel;

        public string NowaNazwaKoszulki { get; private set; }


        public int? SelectedKoszulkaId { get; private set; }

        public KoszulkaSelectionDialog(IEzdApiService ezdService)
        {
            _ezdService = ezdService;
            InitializeComponent(); // To wywołuje kod z Designer.cs
            SetupAdditionalControls(); // Nasza dodatkowa inicjalizacja
            LoadExistingKoszulki();
        }

        private void SetupAdditionalControls()
        {
            // Panel dla lepszej organizacji kontrolek
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            rbNew = new RadioButton
            {
                Text = "Nowa koszulka",
                Location = new Point(10, 20),
                Checked = true
            };

            txtNewName = new TextBox
            {
                Location = new Point(30, 45),
                Width = 320
            };

            rbExisting = new RadioButton
            {
                Text = "Istniejąca koszulka",
                Location = new Point(10, 80)
            };

            existingKoszulkiCombo = new ComboBox
            {
                Location = new Point(30, 105),
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            btnOK = new Button
            {
                Text = "OK",
                Location = new Point(210, 170)
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Anuluj",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 170)
            };

            // Dodajemy kontrolki do panelu
            panel.Controls.AddRange(new Control[] {
                rbNew, txtNewName, rbExisting,
                existingKoszulkiCombo, btnOK, btnCancel
            });

            // Dodajemy panel do formularza
            Controls.Add(panel);

            // Dodajemy event handlery
            rbNew.CheckedChanged += (s, e) => {
                txtNewName.Enabled = rbNew.Checked;
                existingKoszulkiCombo.Enabled = !rbNew.Checked;
            };

            rbExisting.CheckedChanged += (s, e) => {
                existingKoszulkiCombo.Enabled = rbExisting.Checked;
                txtNewName.Enabled = !rbExisting.Checked;
            };

            // Ustawiamy rozmiar i inne właściwości formularza
            Size = new Size(400, 250);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Text = "Wybór koszulki";
        }

        private async void LoadExistingKoszulki()
        {
            try
            {
                var koszulki = await _ezdService.PobierzIdentyfikatoryKoszulek(
                    _ezdService.CurrentUserId.Value);

                foreach (var koszulka in koszulki)
                {
                    existingKoszulkiCombo.Items.Add(new KoszulkaItem(koszulka));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas pobierania listy koszulek: " + ex.Message);
            }
        }

        private async void BtnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (rbNew.Checked)
                {
                    if (string.IsNullOrWhiteSpace(txtNewName.Text))
                    {
                        MessageBox.Show("Wprowadź nazwę koszulki");
                        return;
                    }

                    var words = txtNewName.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (words.Length < 2)
                    {
                        MessageBox.Show("Nazwa koszulki musi składać się z co najmniej dwóch wyrazów");
                        return;
                    }

                    // Zamiast tworzyć koszulkę, zapisujemy tylko nazwę
                    NowaNazwaKoszulki = txtNewName.Text;
                    SelectedKoszulkaId = null;
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    if (existingKoszulkiCombo.SelectedItem == null)
                    {
                        MessageBox.Show("Wybierz koszulkę");
                        return;
                    }

                    var selected = (KoszulkaItem)existingKoszulkiCombo.SelectedItem;
                    SelectedKoszulkaId = selected.Id;
                    NowaNazwaKoszulki = null;
                    DialogResult = DialogResult.OK;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd: " + ex.Message);
            }
        }

        private class KoszulkaItem
        {
            public int Id { get; }
            public string Name { get; }

            public KoszulkaItem(PismoDto pismo)
            {
                Id = pismo.ID;
                Name = pismo.Nazwa;
            }

            public override string ToString() => Name;
        }
    }
}