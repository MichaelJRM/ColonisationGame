using System.Collections.Generic;

namespace BaseBuilding.Scripts.Systems.SaveSystem;

enum SaveId
{
    BuildingSystem,
}

public class Save
{
    public string SceneFilePath { get; set; }
    public Dictionary<string, string> Content { get; set; }
    public Save[] Children { get; set; }
}