using System.Text.Json;
using Godot;

namespace BaseBuilding.Scripts.Systems.SaveSystem;

public interface IPersistent
{
    public Save Serialize()
    {
        var saveData = new Save
        {
            Sfp = GetSceneFilePath(),
            C = Save()
        };
        var persistentChildren = GetPersistentChildren();
        var childrenSaveData = new Save[persistentChildren.Length];

        for (var i = 0; i < persistentChildren.Length; i++)
        {
            var persistent = persistentChildren[i];
            childrenSaveData[i] = persistent.Serialize();
        }

        saveData.Ch = childrenSaveData;
        return saveData;
    }

    public IPersistent[] GetPersistentChildren();

    public string GetSceneFilePath();

    public static Node Deserialize(Save save)
    {
        var scene = GD.Load<PackedScene>(save.Sfp);
        var instance = scene.Instantiate<Node>();
        var persistentInstance = (IPersistent)instance;
        persistentInstance.ProcessContent((JsonElement)save.C);
        persistentInstance.BeforeLoad();
        persistentInstance.Load();
        persistentInstance.AfterLoad();

        var children = save.Ch;
        foreach (var child in children)
        {
            var childInstance = Deserialize(child);
            instance.AddChild(childInstance);
        }

        return instance;
    }

    protected void ProcessContent(JsonElement saveContent);

    public object Save();

    public void BeforeLoad();

    public void Load();

    public void AfterLoad();

    public void ClearSaveContent();
}