namespace CssGenerator.Models;

public class WebPart
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Html { get; set; } = string.Empty;

    public override string ToString() => Name;
}
