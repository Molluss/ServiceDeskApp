public static class TemplateEditor
{
    public static (string Name, string Text, string? FolderPath) ShowDialog(string baseDirectory, string name = "", string text = "")
    {
        Form editorForm = new Form
        {
            Width = 500,
            Height = 400,
            Text = "Template Editor"
        };

        Label nameLabel = new Label() { Text = "Template Name", Left = 20, Top = 20, Width = 100 };
        TextBox nameBox = new TextBox() { Left = 20, Top = 45, Width = 440, Text = name };

        Label textLabel = new Label() { Text = "Template Text", Left = 20, Top = 85, Width = 100 };
        TextBox textBox = new TextBox() { Left = 20, Top = 110, Width = 440, Height = 100, Multiline = true, Text = text };

        Label folderLabel = new Label() { Text = "Select Folder", Left = 20, Top = 220, Width = 100 };
        TreeView folderTreeView = new TreeView() { Left = 20, Top = 245, Width = 440, Height = 100 };

        // Populate the TreeView with the directory structure
        PopulateFolderTree(folderTreeView, baseDirectory);

        Button confirmation = new Button() { Text = "Save", Left = 360, Width = 100, Top = 360, DialogResult = DialogResult.OK };
        editorForm.Controls.Add(nameLabel);
        editorForm.Controls.Add(nameBox);
        editorForm.Controls.Add(textLabel);
        editorForm.Controls.Add(textBox);
        editorForm.Controls.Add(folderLabel);
        editorForm.Controls.Add(folderTreeView);
        editorForm.Controls.Add(confirmation);
        editorForm.AcceptButton = confirmation;

        if (editorForm.ShowDialog() == DialogResult.OK)
        {
            string selectedFolderPath = folderTreeView.SelectedNode?.Tag as string ?? baseDirectory;
            return (nameBox.Text, textBox.Text, selectedFolderPath);
        }

        return (null, null, null);
    }

    private static void PopulateFolderTree(TreeView treeView, string baseDirectory)
    {
        if (!Directory.Exists(baseDirectory))
        {
            MessageBox.Show($"The directory '{baseDirectory}' does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var rootDirectoryInfo = new DirectoryInfo(baseDirectory);
        var rootNode = CreateDirectoryNode(rootDirectoryInfo);
        treeView.Nodes.Add(rootNode);
        treeView.ExpandAll();
    }

    private static TreeNode CreateDirectoryNode(DirectoryInfo directoryInfo)
    {
        var directoryNode = new TreeNode(directoryInfo.Name) { Tag = directoryInfo.FullName };

        foreach (var directory in directoryInfo.GetDirectories())
        {
            directoryNode.Nodes.Add(CreateDirectoryNode(directory));
        }

        return directoryNode;
    }
}
