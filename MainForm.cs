using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json;

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

        public MainForm()
        {
            InitializeComponent();
            LoadFavorites();
            LoadTemplates();
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
            templateTreeView = new TreeView { Dock = DockStyle.Fill };
            templateTreeView.NodeMouseDoubleClick += OnTemplateNodeDoubleClick;

            // Ajouter les contrôles au formulaire
            this.Controls.Add(templateTreeView);
            this.Controls.Add(addButton);
            this.Controls.Add(editButton);
            this.Controls.Add(searchBox);
        }

        private void LoadTemplates()
        {
            if (!Directory.Exists(templateDirectory))
            {
                MessageBox.Show($"Template directory not found: {templateDirectory}");
                return;
            }

            templateTreeView.Nodes.Clear();
            var rootDirectoryInfo = new DirectoryInfo(templateDirectory);
            var rootNode = CreateDirectoryNode(rootDirectoryInfo);
            templateTreeView.Nodes.Add(rootNode);
            templateTreeView.ExpandAll();
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
                Clipboard.SetText(content);
                MessageBox.Show($"Template '{e.Node.Text}' copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);

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

            foreach (TreeNode node in templateTreeView.Nodes)
            {
                FilterNode(node, searchTerm);
            }
        }

        private bool FilterNode(TreeNode node, string searchTerm)
        {
            bool match = node.Text.ToLower().Contains(searchTerm);

            // Filtrer les enfants récursivement
            foreach (TreeNode child in node.Nodes)
            {
                match |= FilterNode(child, searchTerm);
            }

            // Surligner les correspondances et déplier les nœuds
            if (match)
            {
                node.BackColor = System.Drawing.Color.LightYellow;
                node.Expand(); // Déplier le nœud si une correspondance est trouvée
            }
            else
            {
                node.BackColor = System.Drawing.Color.White;
                node.Collapse(); // Replier le nœud s'il ne correspond pas
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
    }
}
