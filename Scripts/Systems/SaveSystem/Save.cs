namespace BaseBuilding.Scripts.Systems.SaveSystem;

public class Save
{
#pragma warning disable CS8618
    /// <summary>
    /// Scene file path
    /// </summary>
    public string Sfp { get; set; }

    /// <summary>
    /// Node relative path
    /// </summary>
    public string Nrp { get; set; }

    /// <summary>
    /// Content 
    /// </summary>
    public object C { get; set; }

    /// <summary>
    /// Children content
    /// </summary>
    public Save[] Ch { get; set; } = System.Array.Empty<Save>();

    /// <summary>
    /// Instantiate on load
    /// </summary>
    public bool I { get; set; } = true;
#pragma warning restore CS8618
}