using System.Windows.Forms;

public static class Prompt
{
    public static string ShowDialog(string text, string caption, string defaultValue = "")
    {
        Form prompt = new Form()
        {
            Width = 400,
            Height = 150,
            Text = caption
        };
        Label textLabel = new Label() { Left = 20, Top = 20, Text = text };
        TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 350, Text = defaultValue };
        Button confirmation = new Button() { Text = "Ok", Left = 250, Width = 100, Top = 80, DialogResult = DialogResult.OK };
        prompt.Controls.Add(textLabel);
        prompt.Controls.Add(textBox);
        prompt.Controls.Add(confirmation);
        prompt.AcceptButton = confirmation;

        return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
    }
}
