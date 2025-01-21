using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EZDUploader.Core.Configuration;

namespace EZDUploader.UI
{
    public partial class SettingsForm : Form
    {
        private readonly ApiSettings _settings;
        private TextBox textBoxApiUrl;
        private RadioButton radioToken;
        private RadioButton radioLogin;
        private TableLayoutPanel panelLogin;
        private TableLayoutPanel panelToken;
        private TextBox textBoxToken;
        private TextBox textBoxLogin;
        private TextBox textBoxPassword;

        public SettingsForm(ApiSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            SetupUI();
            LoadSettings();
        }

        private void SetupUI()
        {
            this.Text = "Konfiguracja API";
            this.Size = new Size(400, 330);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10),
                AutoSize = true
            };

            // URL API
            mainPanel.Controls.Add(new Label { Text = "URL API:", Dock = DockStyle.Fill });
            textBoxApiUrl = new TextBox { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(textBoxApiUrl);

            // Typ autentykacji
            var authGroupBox = new GroupBox { Text = "Typ autentykacji", Dock = DockStyle.Fill };
            var authPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown
            };

            radioToken = new RadioButton { Text = "Token aplikacji", AutoSize = true };
            radioLogin = new RadioButton { Text = "Login i hasło", AutoSize = true };
            authPanel.Controls.AddRange(new Control[] { radioToken, radioLogin });
            authGroupBox.Controls.Add(authPanel);
            mainPanel.Controls.Add(authGroupBox);

            // Panel dla tokena
            panelToken = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Visible = true
            };
            panelToken.Controls.Add(new Label { Text = "Token:", Dock = DockStyle.Fill });
            textBoxToken = new TextBox { Dock = DockStyle.Fill };
            panelToken.Controls.Add(textBoxToken);
            mainPanel.Controls.Add(panelToken);

            // Panel logowania
            panelLogin = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Visible = false
            };

            var loginLabel = new Label { Text = "Login:", Dock = DockStyle.Fill };
            textBoxLogin = new TextBox { Dock = DockStyle.Fill };
            var passwordLabel = new Label { Text = "Hasło:", Dock = DockStyle.Fill };
            textBoxPassword = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true
            };

            panelLogin.Controls.Add(loginLabel, 0, 0);
            panelLogin.Controls.Add(textBoxLogin, 1, 0);
            panelLogin.Controls.Add(passwordLabel, 0, 1);
            panelLogin.Controls.Add(textBoxPassword, 1, 1);

            mainPanel.Controls.Add(panelLogin);

            // Panel przycisków
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(5),
                AutoSize = true
            };

            var saveButton = new Button
            {
                Text = "Zapisz",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                MinimumSize = new Size(80, 30)
            };
            saveButton.Click += SaveButton_Click;

            var cancelButton = new Button
            {
                Text = "Anuluj",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                MinimumSize = new Size(80, 30)
            };

            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(new Label { Width = 10 });
            buttonPanel.Controls.Add(saveButton);

            radioToken.CheckedChanged += RadioButton_CheckedChanged;
            radioLogin.CheckedChanged += RadioButton_CheckedChanged;

            this.Controls.Add(mainPanel);
            this.Controls.Add(buttonPanel);
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            panelToken.Visible = radioToken.Checked;
            panelLogin.Visible = radioLogin.Checked;
        }

        private void LoadSettings()
        {
            textBoxApiUrl.Text = _settings.BaseUrl;

            if (_settings.AuthType == AuthenticationType.Token)
            {
                radioToken.Checked = true;
                textBoxToken.Text = _settings.ApplicationToken;
            }
            else
            {
                radioLogin.Checked = true;
                textBoxLogin.Text = _settings.Login;
                textBoxPassword.Text = _settings.Password;
            }
        }

        private bool ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(textBoxApiUrl.Text))
            {
                MessageBox.Show("URL API nie może być pusty");
                return false;
            }

            if (radioToken.Checked && string.IsNullOrWhiteSpace(textBoxToken.Text))
            {
                MessageBox.Show("Token aplikacji nie może być pusty");
                return false;
            }

            if (radioLogin.Checked &&
                (string.IsNullOrWhiteSpace(textBoxLogin.Text) ||
                 string.IsNullOrWhiteSpace(textBoxPassword.Text)))
            {
                MessageBox.Show("Login i hasło nie mogą być puste");
                return false;
            }

            return true;
        }

        private void SaveSettings()
        {
            _settings.BaseUrl = textBoxApiUrl.Text;
            _settings.AuthType = radioToken.Checked ?
                AuthenticationType.Token :
                AuthenticationType.LoginPassword;

            if (_settings.AuthType == AuthenticationType.Token)
            {
                _settings.ApplicationToken = textBoxToken.Text;
            }
            else
            {
                _settings.Login = textBoxLogin.Text;
                _settings.Password = textBoxPassword.Text;
            }
        }
        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (ValidateSettings())
            {
                SaveSettings();
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
