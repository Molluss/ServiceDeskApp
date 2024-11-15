using System;
using System.Windows.Forms;
using ServiceDeskApp.Localization;

namespace ServiceDeskApp
{
    public partial class MainForm : Form
    {
        private string currentLanguage = "en";

        public MainForm()
        {
            InitializeComponent();
            UpdateUI();
        }

        private void UpdateUI()
        {
            this.Text = LocalizationManager.GetTranslation("WelcomeMessage", currentLanguage);
        }


        private void OnLanguageChange(object sender, EventArgs e)
        {
            currentLanguage = currentLanguage == "en" ? "fr" : "en";
            UpdateUI();
        }

        private void InitializeComponent()
        {
            var testLabel = new Label
            {
                Text = "Test Label",
                Dock = DockStyle.Top,
                AutoSize = true
            };

            this.Controls.Add(testLabel);
        }



    }
}
