using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.WinForms; // Namespace pour WebView2
using System;
using System.Windows.Forms;

namespace ServiceDeskApp
{
    public partial class MainForm : Form
    {
        private Button addButton;
        private Button editButton;
        private TreeView templateTreeView;
        private TextBox searchBox;
        private TreeNode favoritesNode;
        private const string favoritesFilePath = @"C:\temp\template\favorites.json";
        private string templateDirectory = @"C:\temp\template";
        private string userConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServiceDeskApp", "user_config.json");
        private Button syncButton;
        private WebView2 webView;
        private WebView2 webView2Control; // Déclaration ajoutée ici
        private string logFilePath = @"C:\Temp\WebView2Log.txt"; // Chemin du fichier log
        private TextBox generatedTemplateTextBox;
        private string httpPath = "http://37.114.37.21:8001/";
        private string onedrivePath = @"C:\Users\Utilisateur\OneDrive\Templates";
        private string networkPath = @"\\Serveur\Partage\Templates";

        public MainForm()
        {
            
            isDarkMode = LoadThemePreference();
            InitializeComponent();
            LoadSettings();
            ApplyTheme();
            LoadFavorites();
            LoadTemplates();
            InitializeLogging(); // Initialiser le système de log
            Log("Application started.");
            //InitializeWebView2(); // Initialiser WebView2
            //InitializeTicketFields();
            InitializeTicketTemplateField();
        }

        private void InitializeComponent()
        {
            this.Text = "Service Desk App";
            this.Size = new System.Drawing.Size(1200, 800); // Main window size
            this.StartPosition = FormStartPosition.CenterScreen;

            // SplitContainer to separate Templates and NRP
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 600 // Split between left (Templates) and right (NRP)
            };

            // Left section: Templates
            templateTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 12),
                DrawMode = TreeViewDrawMode.OwnerDrawText
            };
            templateTreeView.DrawNode += TemplateTreeView_DrawNode;

            // Search bar
            searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                PlaceholderText = "Search for a template...",
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            searchBox.TextChanged += OnSearchTextChanged; // Link the search event

            var syncTemplatesButton = new Button
            {
                Text = "Synchronize Templates",
                Dock = DockStyle.Bottom
            };
            syncTemplatesButton.Click += async (s, e) => await SyncTemplatesAsync();

            var templatesPanel = new Panel { Dock = DockStyle.Fill };
            templatesPanel.Controls.Add(templateTreeView);
            templatesPanel.Controls.Add(syncTemplatesButton);
            templatesPanel.Controls.Add(searchBox);

            splitContainer.Panel1.Controls.Add(templatesPanel);

            // Right section: NRP
            var ticketNumberLabel = new Label
            {
                Text = "Ticket Number:",
                Dock = DockStyle.Top,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            var ticketNumberTextBox = new TextBox { Dock = DockStyle.Top };

            var ticketSubjectLabel = new Label
            {
                Text = "Ticket Subject:",
                Dock = DockStyle.Top,
                Font = new System.Drawing.Font("Segoe UI", 10)
            };
            var ticketSubjectTextBox = new TextBox { Dock = DockStyle.Top };

            var generateMessageButton = new Button
            {
                Text = "Generate Message",
                Dock = DockStyle.Top
            };
            generateMessageButton.Click += (s, e) => GenerateNRPMessage(ticketNumberTextBox.Text, ticketSubjectTextBox.Text);

            var settingsButton = new Button
            {
                Text = "Settings",
                Dock = DockStyle.Top
            };
            settingsButton.Click += OpenSettingsMenu; // Opens the settings menu

            var nrpPanel = new Panel { Dock = DockStyle.Fill };
            nrpPanel.Controls.Add(settingsButton);
            nrpPanel.Controls.Add(generateMessageButton);
            nrpPanel.Controls.Add(ticketSubjectTextBox);
            nrpPanel.Controls.Add(ticketSubjectLabel);
            nrpPanel.Controls.Add(ticketNumberTextBox);
            nrpPanel.Controls.Add(ticketNumberLabel);

            splitContainer.Panel2.Controls.Add(nrpPanel);

            // Add the SplitContainer to the main window
            this.Controls.Add(splitContainer);

            templateTreeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
            templateTreeView.DrawNode += TemplateTreeView_DrawNode;


        }



        // Méthode pour générer le message NRP
        private void GenerateNRPMessage(string ticketNumber, string ticketSubject)
        {
            if (string.IsNullOrEmpty(ticketNumber) || string.IsNullOrEmpty(ticketSubject))
            {
                MessageBox.Show("Please fill in the ticket number and subject.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Load a predefined template
            var templatePath = Path.Combine("C:\\temp\\template\\NRP", "message_template.txt");
            if (!File.Exists(templatePath))
            {
                MessageBox.Show("Template not found. Please check the location.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var templateContent = File.ReadAllText(templatePath);
            var message = templateContent
                .Replace("{ticketNumber}", ticketNumber)
                .Replace("{ticketSubject}", ticketSubject);

            Clipboard.SetText(message);
            MessageBox.Show("Message generated and copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private async void InitializeWebView2()
        {
            webView2Control = new WebView2
            {
                Dock = DockStyle.Fill
            };

            try
            {
                Log("Initializing WebView2...");
                await webView2Control.EnsureCoreWebView2Async();
                webView2Control.Source = new Uri("https://ceva-ism.ivanticloud.com");

                // Ajouter un gestionnaire d'événement pour détecter la fin du chargement de la page
                webView2Control.NavigationCompleted += OnNavigationCompleted;

                this.Controls.Add(webView2Control);
                Log("WebView2 initialized and added to the form.");
            }
            catch (Exception ex)
            {
                Log($"Error initializing WebView2: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'initialisation de WebView2 : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void InitializeLogging()
        {
            try
            {
                // Créer le répertoire si nécessaire
                string logDirectory = Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Écrire une entête dans le fichier de log
                File.WriteAllText(logFilePath, $"Log initialized on {DateTime.Now}\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation du système de log : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void LoadTemplates()
        {
            if (!Directory.Exists(templateDirectory))
            {
                MessageBox.Show($"Le répertoire des templates est introuvable : {templateDirectory}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            templateTreeView.Nodes.Clear(); // Réinitialiser l’arborescence
            try
            {
                // Charger les fichiers et dossiers
                var rootDirectoryInfo = new DirectoryInfo(templateDirectory);
                var rootNode = CreateDirectoryNode(rootDirectoryInfo);
                templateTreeView.Nodes.Add(rootNode);
                templateTreeView.ExpandAll(); // Déplier tous les nœuds par défaut
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des templates : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private TreeNode CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            var directoryNode = new TreeNode(directoryInfo.Name); // Nom du dossier comme nœud principal

            try
            {
                // Ajouter les sous-dossiers
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    directoryNode.Nodes.Add(CreateDirectoryNode(directory));
                }

                // Ajouter les fichiers
                foreach (var file in directoryInfo.GetFiles("*.txt"))
                {
                    var fileNode = new TreeNode(file.Name) // Utilisez file.Name pour le nom du fichier
                    {
                        Tag = file.FullName // Stockez le chemin complet dans le Tag pour un accès futur
                    };
                    directoryNode.Nodes.Add(fileNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la lecture du répertoire {directoryInfo.FullName} : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return directoryNode;
        }


        private void OnTemplateNodeDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is string filePath && File.Exists(filePath))
            {
                try
                {
                    // Lire le contenu du fichier template
                    var content = File.ReadAllText(filePath);

                    // Copier le contenu dans le presse-papiers
                    Clipboard.SetText(content);

                    // Afficher un message de confirmation
                    MessageBox.Show($"Template '{e.Node.Text}' copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    // En cas d'erreur, afficher un message
                    MessageBox.Show($"Error copying template: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Si aucun fichier n'est associé au nœud
                MessageBox.Show("No template file is associated with this item.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            var searchTerm = searchBox.Text.ToLower();

            if (string.IsNullOrEmpty(searchTerm))
            {
                ResetTree(templateTreeView.Nodes);
                return;
            }

            foreach (TreeNode node in templateTreeView.Nodes)
            {
                ExpandMatchingNodes(node, searchTerm); // Appel corrigé
            }
        }


        private bool FilterNode(TreeNode node, string searchTerm)
        {
            bool match = node.Text.ToLower().Contains(searchTerm.ToLower());

            // Filtrer les enfants récursivement
            foreach (TreeNode child in node.Nodes)
            {
                match |= FilterNode(child, searchTerm);
            }

            // Appliquer des couleurs adaptées en fonction du mode et des résultats
            if (match)
            {
                node.BackColor = isDarkMode ? System.Drawing.Color.FromArgb(60, 60, 60) : System.Drawing.Color.LightYellow;
                node.ForeColor = isDarkMode ? System.Drawing.Color.White : System.Drawing.Color.Black;
                node.Expand(); // Déplier les nœuds correspondants
            }
            else
            {
                node.BackColor = isDarkMode ? System.Drawing.Color.FromArgb(45, 45, 48) : System.Drawing.Color.White;
                node.ForeColor = isDarkMode ? System.Drawing.Color.White : System.Drawing.Color.Black;
                node.Collapse(); // Replier les nœuds qui ne correspondent pas
            }

            return match;
        }




        private void OnAddTemplate(object sender, EventArgs e)
        {
            var (name, text, folderPath) = TemplateEditor.ShowDialog(templateDirectory);
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(text) && folderPath != null)
            {
                var filePath = Path.Combine(folderPath, name + ".txt");
                File.WriteAllText(filePath, text);
                LoadTemplates();
            }
        }


        private void OnEditTemplate(object sender, EventArgs e)
        {
            if (templateTreeView.SelectedNode != null && templateTreeView.SelectedNode.Tag is string filePath && File.Exists(filePath))
            {
                var existingText = File.ReadAllText(filePath);
                var directoryPath = Path.GetDirectoryName(filePath); // Get the folder of the template

                // Pass the correct directory and existing values to the editor
                var (name, text, folderPath) = TemplateEditor.ShowDialog(directoryPath ?? templateDirectory, templateTreeView.SelectedNode.Text, existingText);

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(text) && folderPath != null)
                {
                    var newFilePath = Path.Combine(folderPath, name + ".txt");

                    // Check if the file is moved to a new location
                    if (!filePath.Equals(newFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Write the new file
                        File.WriteAllText(newFilePath, text);

                        // Delete the old file
                        File.Delete(filePath);
                    }
                    else
                    {
                        // If the file wasn't moved, just update its content
                        File.WriteAllText(filePath, text);
                    }

                    // Reload the templates to reflect changes
                    LoadTemplates();
                }
            }
            else
            {
                MessageBox.Show("The selected file or folder does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private List<string> LoadFavoriteTemplates()
        {
            if (!File.Exists(favoritesFilePath))
            {
                return new List<string>();
            }

            var json = File.ReadAllText(favoritesFilePath);
            return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
        }

        private void SaveFavoriteTemplates(List<string> favorites)
        {
            var json = JsonConvert.SerializeObject(favorites, Formatting.Indented);
            File.WriteAllText(favoritesFilePath, json);
        }

        private void LoadFavorites()
        {
            favoritesNode = new TreeNode("Favorites");
            templateTreeView.Nodes.Add(favoritesNode);

            var favoriteTemplates = LoadFavoriteTemplates();
            foreach (var favoritePath in favoriteTemplates)
            {
                if (File.Exists(favoritePath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(favoritePath);
                    var fileNode = new TreeNode(fileName)
                    {
                        Tag = favoritePath,
                        BackColor = System.Drawing.Color.LightBlue // Surligne les favoris
                    };
                    favoritesNode.Nodes.Add(fileNode);
                }
            }
        }

        private void AddToFavoritesNode(TreeNode templateNode)
        {
            var favoriteNode = new TreeNode(templateNode.Text)
            {
                Tag = templateNode.Tag,
                BackColor = System.Drawing.Color.LightBlue
            };
            favoritesNode.Nodes.Add(favoriteNode);
        }

        private void RemoveFromFavoritesNode(TreeNode templateNode)
        {
            foreach (TreeNode node in favoritesNode.Nodes)
            {
                if (node.Tag?.ToString() == templateNode.Tag?.ToString())
                {
                    favoritesNode.Nodes.Remove(node);
                    break;
                }
            }
        }
        private string LoadUserSignature()
        {
            if (!File.Exists(userConfigPath))
            {
                return "Cordialement,\nVotre nom"; // Signature par défaut
            }

            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(userConfigPath));
            return config.TryGetValue("signature", out var signature) ? signature : "Cordialement,\nVotre nom";
            MessageBox.Show($"Loaded Signature: {signature}", "Debug");

        }
        private void SaveUserSignature(string signature)
        {
            var config = new Dictionary<string, string> { { "signature", signature } };
            var configDirectory = Path.GetDirectoryName(userConfigPath);

            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            File.WriteAllText(userConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        private void OnConfigureSignatureClick(object sender, EventArgs e)
        {
            using (var form = new Form())
            {
                form.Text = "Configure Signature";
                form.Size = new System.Drawing.Size(400, 300);

                // Zone de texte multi-lignes
                var textBox = new TextBox
                {
                    Multiline = true,
                    Dock = DockStyle.Fill,
                    Text = LoadUserSignature(), // Charger la signature actuelle
                    ScrollBars = ScrollBars.Vertical // Ajouter une barre de défilement verticale
                };
                form.Controls.Add(textBox);

                // Bouton "OK"
                var okButton = new Button
                {
                    Text = "OK",
                    Dock = DockStyle.Bottom
                };
                okButton.Click += (s, args) =>
                {
                    SaveUserSignature(textBox.Text); // Sauvegarder la nouvelle signature
                    MessageBox.Show("Signature updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    form.Close();
                };
                form.Controls.Add(okButton);

                // Afficher la fenêtre
                form.ShowDialog();
            }
        }
        private string ReplaceSignaturePlaceholder(string templateContent)
        {
            var signature = LoadUserSignature();
            return templateContent.Replace("{signature}", signature);
        }
        private bool isDarkMode = false; // Par défaut, mode clair

        private void OnToggleThemeClick(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;
            SaveThemePreference(isDarkMode);
            ApplyTheme();
        }
        private void ApplyTheme()
        {
            var backgroundColor = isDarkMode ? System.Drawing.Color.FromArgb(45, 45, 48) : System.Drawing.Color.White;
            var foregroundColor = isDarkMode ? System.Drawing.Color.White : System.Drawing.Color.Black;
            var borderColor = isDarkMode ? System.Drawing.Color.FromArgb(64, 64, 64) : System.Drawing.Color.Silver;

            this.BackColor = backgroundColor;
            this.ForeColor = foregroundColor;

            foreach (Control control in this.Controls)
            {
                control.BackColor = backgroundColor;
                control.ForeColor = foregroundColor;

                if (control is Button)
                {
                    var button = (Button)control;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = borderColor;
                    button.FlatAppearance.BorderSize = 1; // Ajuste l'épaisseur de la bordure
                }
                else if (control is TextBox)
                {
                    var textBox = (TextBox)control;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                }
                templateTreeView.BackColor = backgroundColor;
                templateTreeView.ForeColor = foregroundColor;

            }
        }

        private void SaveThemePreference(bool darkMode)
        {
            var config = new Dictionary<string, object> { { "darkMode", darkMode } };
            var configDirectory = Path.GetDirectoryName(userConfigPath);

            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            File.WriteAllText(userConfigPath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private bool LoadThemePreference()
        {
            if (!File.Exists(userConfigPath)) return false;

            var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(userConfigPath));
            return config != null && config.TryGetValue("darkMode", out var darkMode) && Convert.ToBoolean(darkMode);
        }
        private async Task SyncTemplatesRecursiveAsync(string baseUrl, string localPath)
        {
            using var client = new HttpClient();

            try
            {
                Log($"Fetching: {baseUrl}");
                var response = await client.GetStringAsync(baseUrl); // Récupérer la page HTML
                var entries = ParseFilesAndFoldersFromHtml(response); // Analyser les entrées

                foreach (var (name, isDirectory) in entries)
                {
                    if (isDirectory) // Si c'est un dossier
                    {
                        var subFolderUrl = NormalizeUrl(baseUrl, name);
                        var subFolderPath = Path.Combine(localPath, name.TrimEnd('/'));
                        Log($"Creating folder: {subFolderPath}");

                        // Créer le dossier local s'il n'existe pas
                        if (!Directory.Exists(subFolderPath))
                        {
                            Directory.CreateDirectory(subFolderPath);
                        }

                        // Appel récursif pour synchroniser le contenu du dossier
                        await SyncTemplatesRecursiveAsync(subFolderUrl, subFolderPath);
                    }
                    else // Si c'est un fichier
                    {
                        var fileUrl = NormalizeUrl(baseUrl, name);
                        var filePath = Path.Combine(localPath, name);
                        Log($"Downloading file: {filePath}");

                        try
                        {
                            var fileContent = await client.GetByteArrayAsync(fileUrl);
                            await File.WriteAllBytesAsync(filePath, fileContent);
                        }
                        catch (HttpRequestException ex)
                        {
                            Log($"Error downloading file: {fileUrl}. Status code: {ex.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
                MessageBox.Show($"An error occurred while syncing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private List<(string Name, bool IsDirectory)> ParseFilesAndFoldersFromHtml(string htmlContent)
        {
            var entries = new List<(string Name, bool IsDirectory)>();
            var regex = new Regex(@"<a href=""([^""]+)"">([^<]+)</a>", RegexOptions.IgnoreCase);

            foreach (Match match in regex.Matches(htmlContent))
            {
                string href = match.Groups[1].Value; // Le lien
                string name = match.Groups[2].Value; // Le nom affiché

                // Ignorer les liens inutiles comme "../" ou des fichiers non pertinents
                if (name.Equals("web.config", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("[To Parent Directory]", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("../"))
                {
                    continue;
                }

                // Vérifiez si c'est un dossier (les dossiers terminent par "/")
                bool isDirectory = href.EndsWith("/");
                entries.Add((name, isDirectory));
            }

            return entries;
        }

        private string NormalizeUrl(string baseUrl, string relativePath)
        {
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            return new Uri(new Uri(baseUrl), relativePath).ToString();
        }


        private void Log(string message)
        {
            try
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'écriture dans le log : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SyncTemplatesAsync()
        {
            string serverUrl = "http://37.114.37.21:8001/"; // URL du serveur
            string localPath = @"C:\Temp\template";

            Log($"Starting synchronization from {serverUrl} to {localPath}");
            await SyncTemplatesRecursiveAsync(serverUrl, localPath);

            Log("Synchronization completed successfully.");

            // Actualiser l’arborescence après la synchronisation
            LoadTemplates();
            MessageBox.Show("Synchronization completed and templates reloaded!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private bool IsCloudTemplate(string filePath)
        {
            string metadataPath = Path.Combine(@"C:\Temp\template", "server_metadata.json");

            if (!File.Exists(metadataPath))
                return false;

            var cloudTemplates = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(metadataPath));
            return cloudTemplates != null && cloudTemplates.Contains(filePath);
        }
        private TreeNode CreateFileNode(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var isCloud = IsCloudTemplate(filePath);

            return new TreeNode(fileName)
            {
                Tag = filePath,
                ForeColor = isCloud ? System.Drawing.Color.Blue : System.Drawing.Color.Green,
                ToolTipText = isCloud ? "Cloud Template" : "Local Template"
            };
        }
        private List<string> ParseFilesFromHtml(string htmlContent)
        {
            var files = new List<string>();
            var regex = new Regex(@"<a href=""([^""]+\.(txt|json|xml))"">", RegexOptions.IgnoreCase);

            foreach (Match match in regex.Matches(htmlContent))
            {
                files.Add(match.Groups[1].Value);
            }

            return files;
        }
        private void ResetTreeView(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.BackColor = System.Drawing.Color.White;
                node.ForeColor = System.Drawing.Color.Black;
                ResetTreeView(node.Nodes);
            }
        }
        private bool ExpandMatchingNodes(TreeNode node, string searchTerm)
        {
            bool match = node.Text.ToLower().Contains(searchTerm);

            foreach (TreeNode child in node.Nodes)
            {
                if (ExpandMatchingNodes(child, searchTerm))
                {
                    match = true;
                }
            }

            if (match)
            {
                node.BackColor = System.Drawing.Color.LightYellow;
                node.Expand(); // Déplier les nœuds correspondants
            }
            else
            {
                node.BackColor = isDarkMode ? System.Drawing.Color.FromArgb(45, 45, 48) : System.Drawing.Color.White;
                node.ForeColor = isDarkMode ? System.Drawing.Color.White : System.Drawing.Color.Black;
                node.Collapse(); // Replier les nœuds qui ne correspondent pas
            }

            return match;
        }


        private void ResetTree(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.BackColor = isDarkMode ? System.Drawing.Color.FromArgb(45, 45, 48) : System.Drawing.Color.White;
                node.ForeColor = isDarkMode ? System.Drawing.Color.White : System.Drawing.Color.Black;
                node.Collapse();
                ResetTree(node.Nodes);
            }
        }

        private void ExpandParentNodes(TreeNode node)
        {
            if (node.Parent != null)
            {
                node.Parent.Expand();
                ExpandParentNodes(node.Parent);
            }
        }
        private void ResetTreeViewColors(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.BackColor = isDarkMode ? System.Drawing.Color.FromArgb(45, 45, 48) : System.Drawing.Color.White;
                node.ForeColor = isDarkMode ? System.Drawing.Color.White : System.Drawing.Color.Black;
                if (node.Nodes.Count > 0)
                {
                    ResetTreeViewColors(node.Nodes); // Réinitialiser récursivement
                }
            }
        }

        /*private void InitializeTicketFields()
        {
            // Label et champ pour le numéro de ticket
            var ticketNumberLabel = new Label
            {
                Text = "Ticket Number:",
                Dock = DockStyle.Top
            };
            ticketNumberTextBox = new TextBox
            {
                PlaceholderText = "Enter ticket number...",
                Dock = DockStyle.Top
            };

            // Label et champ pour le sujet du ticket
            var ticketSubjectLabel = new Label
            {
                Text = "Ticket Subject:",
                Dock = DockStyle.Top
            };
            ticketSubjectTextBox = new TextBox
            {
                PlaceholderText = "Enter ticket subject...",
                Dock = DockStyle.Top
            };

            // Bouton pour sauvegarder les informations
            saveTicketInfoButton = new Button
            {
                Text = "Save Ticket Info",
                Dock = DockStyle.Top
            };
            saveTicketInfoButton.Click += SaveTicketInfoButton_Click;

            // Ajouter les contrôles au formulaire
            this.Controls.Add(saveTicketInfoButton);
            this.Controls.Add(ticketSubjectTextBox);
            this.Controls.Add(ticketSubjectLabel);
            this.Controls.Add(ticketNumberTextBox);
            this.Controls.Add(ticketNumberLabel);
        }
       
        private void SaveTicketInfoButton_Click(object sender, EventArgs e)
        {
            var ticketNumber = ticketNumberTextBox.Text;
            var ticketSubject = ticketSubjectTextBox.Text;

            if (string.IsNullOrEmpty(ticketNumber) || string.IsNullOrEmpty(ticketSubject))
            {
                MessageBox.Show("Please fill in both the ticket number and subject.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Générer la template remplie
            var filledTemplate = LoadAndFillTemplate(ticketNumber, ticketSubject);
            if (!string.IsNullOrEmpty(filledTemplate))
            {
                generatedTemplateTextBox.Text = filledTemplate;
                MessageBox.Show("Template generated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

         */

        private void InitializeTicketTemplateField()
        {
            // Label pour la template générée
            var templateLabel = new Label
            {
                Text = "Generated Template:",
                Dock = DockStyle.Top
            };

            // Zone de texte pour afficher la template générée
            generatedTemplateTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill
            };

            // Ajouter le contrôle au formulaire
            this.Controls.Add(generatedTemplateTextBox);
            this.Controls.Add(templateLabel);
        }
        private string LoadAndFillTemplate(string ticketNumber, string ticketSubject)
        {
            var templatePath = Path.Combine(templateDirectory, "ticket_template.txt");

            if (!File.Exists(templatePath))
            {
                MessageBox.Show($"Template file not found: {templatePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }

            var templateContent = File.ReadAllText(templatePath);

            // Remplacer les espaces réservés dans la template
            return templateContent
                .Replace("{ticketNumber}", ticketNumber)
                .Replace("{ticketSubject}", ticketSubject);
        }
        private void OpenSettingsMenu(object sender, EventArgs e)
        {
            using (var settingsForm = new Form())
            {
                settingsForm.Text = "Settings";
                settingsForm.Size = new System.Drawing.Size(500, 400);
                settingsForm.StartPosition = FormStartPosition.CenterParent;

                // Option: Dark Mode
                var darkModeCheckbox = new CheckBox
                {
                    Text = "Enable Dark Mode",
                    Checked = isDarkMode,
                    Dock = DockStyle.Top
                };
                darkModeCheckbox.CheckedChanged += (s, args) =>
                {
                    isDarkMode = darkModeCheckbox.Checked;
                    ApplyTheme();
                };

                // Option: Source selection
                var sourceLabel = new Label
                {
                    Text = "Select File Source:",
                    Dock = DockStyle.Top
                };

                var sourceComboBox = new ComboBox
                {
                    Dock = DockStyle.Top,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Items = { "HTTP", "OneDrive", "Local Network" },
                    SelectedIndex = 0 // Default to HTTP
                };

                sourceComboBox.SelectedIndexChanged += (s, args) =>
                {
                    UpdateActiveSource(sourceComboBox.SelectedItem.ToString());
                };


                // Option: Configure source paths
                var localPathLabel = new Label
                {
                    Text = "Local Path:",
                    Dock = DockStyle.Top
                };
                var localPathTextBox = new TextBox
                {
                    Dock = DockStyle.Top,
                    Text = @"C:\Temp\template" // Default path
                };

                var httpUrlLabel = new Label
                {
                    Text = "HTTP URL:",
                    Dock = DockStyle.Top
                };
                var httpUrlTextBox = new TextBox
                {
                    Dock = DockStyle.Top,
                    Text = "http://example.com/templates" // Default HTTP URL
                };

                var onedrivePathLabel = new Label
                {
                    Text = "OneDrive Path:",
                    Dock = DockStyle.Top
                };
                var onedrivePathTextBox = new TextBox
                {
                    Dock = DockStyle.Top,
                    Text = @"C:\Users\YourUser\OneDrive\Templates" // Default OneDrive path
                };

                // Save button
                var saveButton = new Button
                {
                    Text = "Save Settings",
                    Dock = DockStyle.Bottom
                };
                saveButton.Click += (s, args) =>
                {
                    SaveSettings(sourceComboBox.SelectedItem.ToString(), localPathTextBox.Text, httpUrlTextBox.Text, onedrivePathTextBox.Text);
                    MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    settingsForm.Close();
                };

                // Add controls to the settings form
                settingsForm.Controls.Add(saveButton);
                settingsForm.Controls.Add(onedrivePathTextBox);
                settingsForm.Controls.Add(onedrivePathLabel);
                settingsForm.Controls.Add(httpUrlTextBox);
                settingsForm.Controls.Add(httpUrlLabel);
                settingsForm.Controls.Add(localPathTextBox);
                settingsForm.Controls.Add(localPathLabel);
                settingsForm.Controls.Add(sourceComboBox);
                settingsForm.Controls.Add(sourceLabel);
                settingsForm.Controls.Add(darkModeCheckbox);

                settingsForm.ShowDialog();
            }
        }





        private void DebugTreeViewNodes()
        {
            foreach (TreeNode node in templateTreeView.Nodes)
            {
                DebugNode(node);
            }
        }

        private void DebugNode(TreeNode node)
        {
            Console.WriteLine($"Node Text: {node.Text}, Tag: {node.Tag}");
            foreach (TreeNode childNode in node.Nodes)
            {
                DebugNode(childNode);
            }
        }
        private async void OnNavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                Log("Navigation completed successfully.");
            }
            else
            {
                Log($"Navigation failed. WebErrorStatus: {e.WebErrorStatus}");
            }
        }
        private void SaveSettings(string activeSource, string localPath, string httpUrl, string onedrivePath)
        {
            var settings = new
            {
                ActiveSource = activeSource,
                LocalPath = localPath,
                HttpUrl = httpUrl,
                OneDrivePath = onedrivePath,
                IsDarkMode = isDarkMode
            };

            var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServiceDeskApp", "settings.json");

            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            File.WriteAllText(settingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }



        private void LoadSettings()
        {
            var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ServiceDeskApp", "settings.json");

            if (!File.Exists(settingsPath))
            {
                return; // Use default values
            }

            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(settingsPath));

            if (settings != null)
            {
                if (settings.TryGetValue("ActiveSource", out var activeSource))
                {
                    UpdateActiveSource(activeSource.ToString());
                }

                if (settings.TryGetValue("LocalPath", out var localPath))
                {
                    templateDirectory = localPath.ToString();
                }

                if (settings.TryGetValue("HttpUrl", out var httpUrl))
                {
                    // Use this value for HTTP sync
                }

                if (settings.TryGetValue("OneDrivePath", out var onedrivePath))
                {
                    // Use this value for OneDrive sync
                }

                if (settings.TryGetValue("IsDarkMode", out var darkMode))
                {
                    isDarkMode = Convert.ToBoolean(darkMode);
                    ApplyTheme();
                }
            }
        }
        private void UpdateActiveSource(string selectedSource)
        {
            switch (selectedSource)
            {
                case "HTTP":
                    // Logic for switching to HTTP source
                    MessageBox.Show("HTTP source selected. Ensure URL is configured properly.", "Source Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case "OneDrive":
                    // Logic for switching to OneDrive source
                    MessageBox.Show("OneDrive source selected. Ensure OneDrive path is configured.", "Source Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case "Local Network":
                    // Logic for switching to local network source
                    MessageBox.Show("Local network source selected. Ensure network path is configured.", "Source Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                default:
                    MessageBox.Show("Unknown source selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }
        private void TemplateTreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            // Couleurs basées sur le mode sombre ou clair
            var backgroundColor = isDarkMode ? System.Drawing.Color.FromArgb(45, 45, 48) : System.Drawing.Color.White;
            var highlightColor = isDarkMode ? System.Drawing.Color.FromArgb(85, 85, 85) : System.Drawing.Color.LightYellow;
            var textColor = isDarkMode ? System.Drawing.Color.White : System.Drawing.Color.Black;

            // Vérifiez si le nœud correspond à la recherche ou est sélectionné
            if (searchBox != null && !string.IsNullOrEmpty(searchBox.Text) && e.Node.Text.ToLower().Contains(searchBox.Text.ToLower()))
            {
                e.Graphics.FillRectangle(new SolidBrush(highlightColor), e.Bounds); // Surlignage pour correspondance
            }
            else if ((e.State & TreeNodeStates.Selected) != 0) // Surlignage de sélection
            {
                e.Graphics.FillRectangle(new SolidBrush(System.Drawing.Color.DarkSlateBlue), e.Bounds); // Couleur de surlignage pour le nœud sélectionné
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(backgroundColor), e.Bounds);
            }

            // Dessin du texte
            TextRenderer.DrawText(
                e.Graphics,
                e.Node.Text,
                e.Node.NodeFont ?? templateTreeView.Font,
                e.Bounds,
                textColor,
                TextFormatFlags.GlyphOverhangPadding
            );

            // Empêcher le dessin par défaut
            e.DrawDefault = false;
        }



    }
}