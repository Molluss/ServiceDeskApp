using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class SettingsForm : Form
{
    private TextBox localPathTextBox;
    private ComboBox sourceTypeComboBox;
    private CheckBox darkModeCheckBox;

    public SettingsForm()
    {
        this.Text = "Paramètres";
        this.Size = new System.Drawing.Size(400, 300);
        this.StartPosition = FormStartPosition.CenterParent;

        var darkModeLabel = new Label
        {
            Text = "Mode Sombre :",
            Dock = DockStyle.Top
        };
        darkModeCheckBox = new CheckBox { Dock = DockStyle.Top };

        var sourceTypeLabel = new Label
        {
            Text = "Source des Templates :",
            Dock = DockStyle.Top
        };
        sourceTypeComboBox = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Items = { "HTTP", "Réseau", "OneDrive" }
        };

        var localPathLabel = new Label
        {
            Text = "Emplacement Local :",
            Dock = DockStyle.Top
        };
        localPathTextBox = new TextBox { Dock = DockStyle.Top };

        var saveButton = new Button
        {
            Text = "Sauvegarder",
            Dock = DockStyle.Bottom
        };
        saveButton.Click += SaveSettings;

        this.Controls.Add(saveButton);
        this.Controls.Add(localPathTextBox);
        this.Controls.Add(localPathLabel);
        this.Controls.Add(sourceTypeComboBox);
        this.Controls.Add(sourceTypeLabel);
        this.Controls.Add(darkModeCheckBox);
        this.Controls.Add(darkModeLabel);
    }

    private void SaveSettings(object sender, EventArgs e)
    {
        var config = new
        {
            DarkMode = darkModeCheckBox.Checked,
            SourceType = sourceTypeComboBox.SelectedItem?.ToString(),
            LocalPath = localPathTextBox.Text
        };

        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
        MessageBox.Show("Paramètres sauvegardés avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
        this.Close();
    }
}
