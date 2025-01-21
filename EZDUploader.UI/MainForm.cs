using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EZDUploader.Core.Interfaces;
using EZDUploader.Core.Configuration;
using EZDUploader.Core.Models;
using System.Diagnostics;

namespace EZDUploader.UI
{
    public partial class MainForm : Form
    {
        private readonly IEzdApiService _ezdService;
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private SplitContainer splitContainer;
        private TreeView treeView;
        private DataGridView dataGridView;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;
        private ToolStripStatusLabel userStatusLabel;

        public MainForm(IEzdApiService ezdService)
        {
            _ezdService = ezdService;
            InitializeComponent();
            InitializeUI();
            InitializeEvents();

            // Uproszczona logika inicjalizacji
            this.HandleCreated += async (s, e) =>
            {
                try
                {
                    Debug.WriteLine("Inicjalizacja MainForm...");
                    await UpdateUserStatus(); // Pokaże status bazując na _ezdService.IsAuthenticated

                    // Jeśli już jesteśmy zalogowani, załaduj dokumenty
                    if (_ezdService.IsAuthenticated)
                    {
                        Debug.WriteLine("Serwis jest już zalogowany, ładuję dokumenty...");
                        await LoadDocuments();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Błąd podczas inicjalizacji MainForm: {ex.Message}");
                    await UpdateUserStatus();
                }
            };
        }

        private async Task UpdateUserStatus()
        {
            try
            {
                if (_ezdService.IsAuthenticated)
                {
                    Debug.WriteLine("Użytkownik zalogowany, aktualizuję status...");
                    userStatusLabel.Text = $"Zalogowany: {_ezdService.Settings.Login ?? "Token"}";
                    EnableControls(true);
                }
                else
                {
                    Debug.WriteLine("Użytkownik niezalogowany");
                    userStatusLabel.Text = "Niezalogowany";
                    EnableControls(false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd w UpdateUserStatus: {ex.Message}");
            }
        }

        private void EnableControls(bool enabled)
        {
            // Włączanie/wyłączanie kontrolek w zależności od stanu logowania
            treeView.Enabled = enabled;
            dataGridView.Enabled = enabled;
            toolStrip.Enabled = enabled;
        }


        private void InitializeUI()
        {
            this.Size = new Size(1024, 768);
            this.Text = "EZD Uploader";

            // MenuStrip
            menuStrip = new MenuStrip();
            this.Controls.Add(menuStrip);

            // ToolStrip
            toolStrip = new ToolStrip();
            toolStrip.Items.Add("Dodaj").Click += (s, e) => { /* TODO */ };
            toolStrip.Items.Add("Odśwież").Click += (s, e) => { /* TODO */ };
            this.Controls.Add(toolStrip);

            // SplitContainer
            splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.SplitterDistance = 250;

            // TreeView
            treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                ShowLines = true,  // Dodaj to
                HideSelection = false, // Dodaj to
                PathSeparator = " - " // Dodaj to
            };
            splitContainer.Panel1.Controls.Add(treeView);

            // DataGridView
            dataGridView = new DataGridView();
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.ReadOnly = true;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            splitContainer.Panel2.Controls.Add(dataGridView);

            // StatusStrip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Gotowy");
            progressBar = new ToolStripProgressBar();
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(progressBar);
            userStatusLabel = new ToolStripStatusLabel();
            statusStrip.Items.Add(new ToolStripSeparator());
            statusStrip.Items.Add(userStatusLabel);

            this.Controls.Add(splitContainer);
            this.Controls.Add(statusStrip);

            treeView.AfterSelect += treeView_AfterSelect;
            dataGridView.CellDoubleClick += dataGridView_CellDoubleClick;
        }
        private void InitializeEvents()
        {
            // Menu events
            var fileMenu = new ToolStripMenuItem("Plik");
            var settingsMenu = new ToolStripMenuItem("Ustawienia");

            fileMenu.DropDownItems.Add("Wyjście", null, (s, e) => Application.Exit());
            settingsMenu.DropDownItems.Add("Konfiguracja API", null, ConfigureApiSettings);
            settingsMenu.DropDownItems.Add("Konfiguracja API", null, ConfigureApiSettings);
            settingsMenu.DropDownItems.Add(new ToolStripSeparator());
            settingsMenu.DropDownItems.Add("Odśwież połączenie", null, RefreshConnection);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(settingsMenu);

            // TreeView events
            treeView.AfterSelect += async (s, e) => await LoadDocuments(e.Node);

            // DataGridView events
            dataGridView.CellDoubleClick += async (s, e) => await OpenDocument(e.RowIndex);

            // ToolStrip events
            var addButton = new ToolStripButton("Dodaj");
            addButton.Click += async (s, e) => await AddDocuments();
            toolStrip.Items.Add(addButton);

            var refreshButton = new ToolStripButton("Odśwież");
            refreshButton.Click += async (s, e) => await LoadDocuments();
            toolStrip.Items.Add(refreshButton);
        }

        private async Task AddDocuments()
        {
            using var dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SetStatus("Dodawanie dokumentów...", true);
                foreach (var file in dialog.FileNames)
                {
                    try
                    {
                        var bytes = await File.ReadAllBytesAsync(file);
                        await _ezdService.DodajZalacznik(bytes, Path.GetFileName(file), 1); // TODO: proper owner ID
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Błąd przy dodawaniu pliku {file}: {ex.Message}");
                    }
                }
                SetStatus("Gotowy", false);
                await RefreshView();
            }
        }



        private async void RefreshConnection(object sender, EventArgs e)
        {
            try
            {
                SetStatus("Odświeżanie połączenia...", true);
                var savedSettings = ConfigurationManager.LoadSettings();

                if (savedSettings != null)
                {
                    _ezdService.Settings.CopyFrom(savedSettings);
                    await LoadDocuments();
                    SetStatus("Połączenie odświeżone", false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas odświeżania połączenia: {ex.Message}");
                SetStatus("Błąd połączenia", false);
                await UpdateUserStatus();
            }
        }

        private void ConfigureApiSettings(object sender, EventArgs e)
        {
            try
            {
                using var settingsForm = new SettingsForm(_ezdService.Settings);
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    ConfigurationManager.SaveSettings(_ezdService.Settings);
                    RefreshConnection(sender, e); // Automatycznie odświeżamy połączenie
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas konfiguracji: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void SetStatus(string message, bool showProgress)
        {
            statusLabel.Text = message;
            progressBar.Visible = showProgress;
        }


        private async Task RefreshView()
        {
            try
            {
                SetStatus("Odświeżanie widoku...", true);

                // Wyczyść obecny widok
                dataGridView.DataSource = null;

                // Pobierz wybrany węzeł
                var selectedNode = treeView.SelectedNode;
                if (selectedNode == null)
                {
                    return;
                }

                // Przygotuj kolumny dla DataGridView
                SetupDataGridColumns();

                // Pokaż dokumenty w zależności od typu wybranego węzła
                if (selectedNode.Tag is PismoDto koszulka)
                {
                    // Jeśli wybrano koszulkę, pokaż jej dokumenty
                    await ShowKoszulkaDocuments(koszulka);
                }
                else if (selectedNode.Tag is TeczkaRwaDto teczka)
                {
                    // Jeśli wybrano teczkę RWA, pokaż sprawy w teczce
                    await ShowTeczkaDocuments(teczka);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas odświeżania widoku: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetStatus("Gotowy", false);
            }
        }

        private void SetupDataGridColumns()
        {
            dataGridView.AutoGenerateColumns = false;
            dataGridView.Columns.Clear();

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Nazwa",
                DataPropertyName = "Nazwa",
                HeaderText = "Nazwa",
                Width = 200
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DataUtworzenia",
                DataPropertyName = "DataUtworzenia",
                HeaderText = "Data utworzenia",
                Width = 150
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                DataPropertyName = "Status",
                HeaderText = "Status",
                Width = 100
            });
        }

        private async Task ShowKoszulkaDocuments(PismoDto koszulka)
        {
            var dokumenty = await _ezdService.PobierzDokumentyKoszulki(koszulka.ID);

            var dokumentyList = dokumenty.Select(d => new
            {
                Nazwa = d.Nazwa,
                DataUtworzenia = d.DataUtworzenia.ToShortDateString(),
                Rodzaj = d.Rodzaj ?? "-",
                Sygnatura = d.Sygnatura ?? "-"
            }).ToList();

            dataGridView.DataSource = dokumentyList;
        }

        private async Task ShowTeczkaDocuments(TeczkaRwaDto teczka)
        {
            try
            {
                // Pobierz sprawy z teczki dla bieżącego roku
                var sprawy = await _ezdService.PobierzSprawyTeczki(teczka.Symbol, DateTime.Now.Year);

                var sprawyList = sprawy.Select(s => new
                {
                    Nazwa = s.Nazwa,
                    DataUtworzenia = s.DataUtworzenia.ToShortDateString(),
                    Status = s.Zakonczone ? "Zakończona" : "W toku",
                    TerminPisma = s.TerminPisma?.ToShortDateString() ?? "-"
                }).ToList();

                dataGridView.DataSource = sprawyList;

                // Dostosuj szerokość kolumn
                dataGridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas pobierania spraw z teczki: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

              private async Task OpenDocument(int rowIndex)
        {
            if (rowIndex < 0) return;

            try
            {
                SetStatus("Otwieranie dokumentu...", true);
                // TODO: Implementacja otwierania dokumentu
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd otwierania dokumentu: {ex.Message}");
            }
            finally
            {
                SetStatus("Gotowy", false);
            }
        }
        private async Task LoadDocuments()
        {
            try
            {
                Debug.WriteLine("Rozpoczynam ładowanie dokumentów...");
                SetStatus("Ładowanie struktury dokumentów...", true);
                treeView.BeginUpdate();
                treeView.Nodes.Clear();

                // Tworzymy główny węzeł
                var rootNode = treeView.Nodes.Add("EZD PUW");
                Debug.WriteLine("Dodano węzeł główny");

                // Pobieramy i dodajemy strukturę RWA
                var rwaNode = rootNode.Nodes.Add("Jednolity rzeczowy wykaz akt");
                try
                {
                    Debug.WriteLine("Próba pobrania RWA...");
                    Debug.WriteLine($"IsAuthenticated: {_ezdService.IsAuthenticated}");
                    Debug.WriteLine($"BaseUrl: {_ezdService.Settings.BaseUrl}");
                    var rwaResponse = await _ezdService.PobierzRwaPoRoczniku(DateTime.Now.Year);
                    Debug.WriteLine($"Odpowiedź RWA: {rwaResponse != null}");
                    if (rwaResponse?.TeczkiPodrzedne != null)
                    {
                        Debug.WriteLine($"Liczba teczek: {rwaResponse.TeczkiPodrzedne.Count}");
                        foreach (var teczka in rwaResponse.TeczkiPodrzedne)
                        {
                            AddRwaNode(rwaNode.Nodes, teczka);
                        }
                        rwaNode.Expand();
                        treeView.Refresh(); // Dodaj to
                    }
                    else
                    {
                        Debug.WriteLine("Brak teczek w odpowiedzi RWA");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Błąd podczas pobierania RWA: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }

                // Pobieramy i dodajemy koszulki użytkownika
                try
                {
                    if (_ezdService.CurrentUserId.HasValue)
                    {
                        var koszulkiNode = rootNode.Nodes.Add("Moje koszulki");
                        var koszulki = await _ezdService.PobierzIdentyfikatoryKoszulek(_ezdService.CurrentUserId.Value);
                        if (koszulki?.Any() == true)
                        {
                            foreach (var idKoszulki in koszulki)
                            {
                                try
                                {
                                    var koszulka = await _ezdService.PobierzKoszulkePoId(idKoszulki);
                                    if (koszulka != null)
                                    {
                                        var node = koszulkiNode.Nodes.Add(koszulka.Nazwa);
                                        node.Tag = koszulka;
                                        node.ImageIndex = 1; // Ikona koszulki
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Błąd podczas pobierania koszulki {idKoszulki}: {ex.Message}");
                                }
                            }
                        }
                        koszulkiNode.Expand();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Błąd podczas pobierania koszulek: {ex.Message}");
                }

                rootNode.Expand();
                Debug.WriteLine("Zakończono ładowanie dokumentów");
                await RefreshView(); // Odśwież widok dokumentów
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Główny błąd podczas ładowania dokumentów: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Błąd podczas ładowania dokumentów: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                treeView.EndUpdate();
                SetStatus("Gotowy", false);
            }
        }

        private async Task LoadDocuments(TreeNode? node = null)
        {
            try
            {
                await this.Invoke(async () =>
                {
                    Debug.WriteLine("Rozpoczynam ładowanie dokumentów...");
                    SetStatus("Ładowanie struktury dokumentów...", true);
                    treeView.BeginUpdate();
                    treeView.Nodes.Clear();

                    // Tworzymy główny węzeł
                    var rootNode = treeView.Nodes.Add("EZD PUW");
                    Debug.WriteLine("Dodano węzeł główny");

                    // Pobieramy i dodajemy strukturę RWA
                    var rwaNode = rootNode.Nodes.Add("Jednolity rzeczowy wykaz akt");
                    try
                    {
                        Debug.WriteLine("Próba pobrania RWA...");
                        Debug.WriteLine($"IsAuthenticated: {_ezdService.IsAuthenticated}");
                        Debug.WriteLine($"BaseUrl: {_ezdService.Settings.BaseUrl}");
                        var rwaResponse = await _ezdService.PobierzRwaPoRoczniku(DateTime.Now.Year);
                        Debug.WriteLine($"Odpowiedź RWA: {rwaResponse != null}");
                        if (rwaResponse?.TeczkiPodrzedne != null)
                        {
                            Debug.WriteLine($"Liczba teczek: {rwaResponse.TeczkiPodrzedne.Count}");
                            foreach (var teczka in rwaResponse.TeczkiPodrzedne)
                            {
                                AddRwaNode(rwaNode.Nodes, teczka);
                            }
                            rwaNode.Expand();
                            treeView.Refresh();
                        }
                        else
                        {
                            Debug.WriteLine("Brak teczek w odpowiedzi RWA");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Błąd podczas pobierania RWA: {ex.Message}");
                        Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    }

                    // Reszta implementacji bez zmian...

                    rootNode.Expand();
                    Debug.WriteLine("Zakończono ładowanie dokumentów");

                    if (node != null)
                    {
                        await RefreshView(); // Odśwież widok dokumentów tylko jeśli podano węzeł
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd w LoadDocuments: {ex.Message}");
                MessageBox.Show($"Błąd podczas ładowania dokumentów: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                treeView.EndUpdate();
                SetStatus("Gotowy", false);
            }
        }

        private void AddRwaNode(TreeNodeCollection nodes, TeczkaRwaDto teczka)
        {
            try
            {
                if (teczka == null)
                {
                    Debug.WriteLine("AddRwaNode: teczka jest null");
                    return;
                }

                Debug.WriteLine($"AddRwaNode: Dodawanie teczki Symbol:{teczka.Symbol}, Nazwa:{teczka.Nazwa}");
                var nodeName = $"{teczka.Symbol} - {teczka.Nazwa}";
                var node = nodes.Add(nodeName);
                node.Tag = teczka;
                node.ImageIndex = 0;
                node.SelectedImageIndex = 0; // Dodaj to

                if (teczka.TeczkiPodrzedne?.Any() == true)
                {
                    Debug.WriteLine($"AddRwaNode: Dodawanie {teczka.TeczkiPodrzedne.Count} podteczek dla {teczka.Symbol}");
                    foreach (var podteczka in teczka.TeczkiPodrzedne)
                    {
                        AddRwaNode(node.Nodes, podteczka);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd w AddRwaNode: {ex.Message}");
            }
        }
        private async void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (e.Node == null) return;

                await RefreshView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wczytywania danych: {ex.Message}");
            }
        }
        private async Task LoadKoszulkaDocuments(PismoDto koszulka)
        {
            var dokumenty = await _ezdService.PobierzDokumentyKoszulki(koszulka.ID);
            var dokumentyList = dokumenty.Select(d => new
            {
                Nazwa = d.Nazwa,
                DataUtworzenia = d.DataUtworzenia.ToShortDateString(),
                Status = d.Rodzaj ?? "-",
                Id = d.Identyfikator.Identyfikator
            }).ToList();

            dataGridView.DataSource = dokumentyList;
        }

        private async Task LoadTeczkaSprawy(TeczkaRwaDto teczka)
        {
            var sprawy = await _ezdService.PobierzSprawyTeczki(teczka.Symbol, DateTime.Now.Year);
            var sprawyList = sprawy.Select(s => new
            {
                Nazwa = s.Nazwa,
                DataUtworzenia = s.DataUtworzenia.ToShortDateString(),
                Status = s.Zakonczone ? "Zakończona" : "W toku",
                Id = s.ID
            }).ToList();

            dataGridView.DataSource = sprawyList;
        }

        private async void dataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                SetStatus("Pobieranie dokumentu...", true);
                var row = dataGridView.Rows[e.RowIndex];
                var id = Convert.ToInt32(row.Cells["Id"].Value);
                var nazwa = row.Cells["Nazwa"].Value.ToString();

                var dane = await _ezdService.PobierzZalacznik(id);
                await OtworzDokument(dane, nazwa);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas otwierania dokumentu: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetStatus("Gotowy", false);
            }
        }
        private async Task OtworzDokument(byte[] dane, string nazwa)
        {
            try
            {
                // Tworzymy tymczasowy plik
                var tempPath = Path.Combine(Path.GetTempPath(), nazwa);
                await File.WriteAllBytesAsync(tempPath, dane);

                // Otwieramy plik domyślną aplikacją systemu
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas otwierania pliku: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
