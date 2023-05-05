using System.Collections.Generic;
using Godot;

namespace BaseBuilding.Scripts.Systems.SaveSystem;

public interface IPersistent
{
    public Save Serialize()
    {
        var saveData = new Save
        {
            SceneFilePath = GetSceneFilePath(),
            Content = Save()
        };
        var persistentChildren = GetPersistentChildren();
        var childrenSaveData = new Save[persistentChildren.Length];

        for (var i = 0; i < persistentChildren.Length; i++)
        {
            var persistent = persistentChildren[i];
            childrenSaveData[i] = persistent.Serialize();
        }

        saveData.Children = childrenSaveData;
        return saveData;
    }

    public IPersistent[] GetPersistentChildren();

    public string GetSceneFilePath();

    public static Node Deserialize(Save save)
    {
        var scene = GD.Load<PackedScene>(save.SceneFilePath);
        var instance = scene.Instantiate<Node>();
        var persistentInstance = (IPersistent)instance;
        persistentInstance.BeforeLoad();
        persistentInstance.Load(save.Content);
        persistentInstance.AfterLoad();

        var children = save.Children;
        foreach (var child in children)
        {
            var childInstance = Deserialize(child);
            instance.AddChild(childInstance);
        }

        return instance;
    }

    public Dictionary<string, string> Save();


    public void BeforeLoad();

    public void Load(Dictionary<string, string> data);

    public void AfterLoad();
}