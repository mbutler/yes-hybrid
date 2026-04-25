using Avalonia.Controls;

namespace YesHybrid.Desktop;

public partial class RulesWindow : Window
{
    public RulesWindow()
    {
        InitializeComponent();
    }

    public RulesWindow(string? markdownOrPlainText, string? loadError) : this()
    {
        if (loadError is not null)
        {
            ErrorBody.Text = loadError;
            ErrorBody.IsVisible = true;
            MdBody.IsVisible = false;
            return;
        }
        ErrorBody.IsVisible = false;
        MdBody.IsVisible = true;
        MdBody.Markdown = string.IsNullOrEmpty(markdownOrPlainText)
            ? "*(Empty rules file.)*"
            : markdownOrPlainText;
    }
}
