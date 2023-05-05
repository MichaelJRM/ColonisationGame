using System.Collections.Generic;
using System.Linq;
using Godot;


namespace BaseBuilding.Scripts.Systems.SaveSystem;

public abstract partial class PersistentNode : Node, IPersistent
{
    protected bool LoadedFromSave = false;

    public IPersistent[] GetPersistentChildren()
    {
        return GetChildren().OfType<IPersistent>().ToArray();
    }

    public string GetSceneFilePath()
    {
        return SceneFilePath;
    }

    public abstract Dictionary<string, string> Save();

    public void BeforeLoad()
    {
        LoadedFromSave = true;
    }

    public abstract void Load(Dictionary<string, string> data);

    public void AfterLoad()
    {
    }
}