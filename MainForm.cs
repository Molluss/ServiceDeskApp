using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Text.RegularExpressions;

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


        public MainForm()
        {
            isDarkMode = LoadThemePreference();
            InitializeComponent();
            ApplyTheme();
            LoadFavorites();
            LoadTemplates();

            Log("Application started successfully.");
        }

        private void InitializeComponent()
        {
            this.Text = "Service Desk Templates";
            this.Size = new System.Drawing.Size(400, 600);

            // Initialiser la barre de recherche
            searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                PlaceholderText = "Search templates..."
            };
            searchBox.TextChanged += OnSearchTextChanged;

            // Initialiser les boutons Ajouter et Editer
            addButton = new Button { Text = "Add Template", Dock = DockStyle.Bottom };
            addButton.Click += OnAddTemplate;

            editButton = new Button { Text = "Edit Template", Dock = DockStyle.Bottom };
            editButton.Click += OnEditTemplate;

            // Initialiser l'arborescence des templates
            templateTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 12) // Augmenter la taille de la police
            };
            templateTreeView.NodeMouseDoubleClick += OnTemplateNodeDoubleClick;
            // Forcer le mode de dessin
            templateTreeView.DrawMode = TreeViewDrawMode.OwnerDrawText;
            templateTreeView.DrawNode += TemplateTreeView_DrawNode;

            // Ajouter les contrôles au formulaire
            this.Controls.Add(templateTreeView);
            this.Controls.Add(addButton);
            this.Controls.Add(editButton);
            this.Controls.Add(searchBox);

            var configureSignatureButton = new Button
            {
                Text = "Configure Signature",
                Dock = DockStyle.Top
            };
            configureSignatureButton.Click += OnConfigureSignatureClick;
            this.Controls.Add(configureSignatureButton);

            var toggleThemeButton = new Button
            {
                Text = "Toggle Dark Mode",
                Dock = DockStyle.Top
            };
            toggleThemeButton.Click += OnToggleThemeClick;
            this.Controls.Add(toggleThemeButton);

            syncButton = new Button
            {
                Text = "Sync Templates",
                Dock = DockStyle.Top
            };
            syncButton.Click += async (sender, e) => await SyncTemplatesAsync();
            this.Controls.Add(syncButton);




        }

        private void LoadTemplates()
        {
            if (!Directory.Exists(templateDirectory))
            {
                MessageBox.Show($"Template directory not found: {templateDirectory}");
                return;
            }

            templateTreeView.Nodes.Clear(); // Réinitialiser l’arborescence
            var rootDirectoryInfo = new DirectoryInfo(templateDirectory);
            var rootNode = CreateDirectoryNode(rootDirectoryInfo);
            templateTreeView.Nodes.Add(rootNode);
            templateTreeView.ExpandAll(); // Déplier tous les nœuds
        }


        private TreeNode CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            var directoryNode = new TreeNode(directoryInfo.Name);

            foreach (var directory in directoryInfo.GetDirectories())
            {
                directoryNode.Nodes.Add(CreateDirectoryNode(directory));
            }

            foreach (var file in directoryInfo.GetFiles("*.txt"))
            {
                var fileNode = new TreeNode(Path.GetFileNameWithoutExtension(file.Name))
                {
                    Tag = file.FullName
                };
                directoryNode.Nodes.Add(fileNode);
            }

            return directoryNode;
        }

        private void OnTemplateNodeDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is string filePath && File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                content = ReplaceSignaturePlaceholder(content); // Remplacement de {signature}
                Clipboard.SetText(content);
                MessageBox.Show($"Template '{e.Node.Text}' copied to clipboard with signature!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);


                // Charger les favoris actuels
                var favorites = LoadFavoriteTemplates();

                // Ajouter ou supprimer des favoris
                if (favorites.Contains(filePath))
                {
                    favorites.Remove(filePath);
                    e.Node.BackColor = System.Drawing.Color.White;
                    RemoveFromFavoritesNode(e.Node);
                }
                else
                {
                    favorites.Add(filePath);
                    e.Node.BackColor = System.Drawing.Color.LightBlue;
                    AddToFavoritesNode(e.Node);
                }

                // Sauvegarder les favoris mis à jour
                SaveFavoriteTemplates(favorites);
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
                string logPath = Path.Combine(@"C:\Temp\template", "sync.log");
                string logDirectory = Path.GetDirectoryName(logPath);

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to write to log: {ex.Message}", "Log Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                // Si un enfant correspond, déployer le nœud parent
                if (ExpandMatchingNodes(child, searchTerm))
                {
                    match = true;
                }
            }

            // Déployer ou replier en fonction de la correspondance
            if (match)
            {
                node.BackColor = System.Drawing.Color.LightYellow;
                node.Expand(); // Déployer le nœud si une correspondance est trouvée
            }
            else
            {
                node.BackColor = System.Drawing.Color.White;
                node.Collapse(); // Replier le nœud s'il ne correspond pas
            }

            return match; // Retourner si ce nœud ou ses enfants correspondent
        }

        private void ResetTree(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                node.BackColor = System.Drawing.Color.White;
                node.Collapse(); // Replier tous les nœuds
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
        private void TemplateTreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            // Appliquer les couleurs en fonction de l'état du nœud
            var backgroundColor = isDarkMode ? System.Drawing.Color.FromArgb(45, 45, 48) : System.Drawing.Color.White;
            var highlightColor = isDarkMode ? System.Drawing.Color.FromArgb(60, 60, 60) : System.Drawing.Color.LightYellow;
            var textColor = isDarkMode ? System.Drawing.Color.White : System.Drawing.Color.Black;

            if (!string.IsNullOrEmpty(searchBox.Text) && e.Node.Text.ToLower().Contains(searchBox.Text.ToLower()))
            {
                e.Graphics.FillRectangle(new SolidBrush(highlightColor), e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(backgroundColor), e.Bounds);
            }

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
