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
            C = Save(),
            Nrp = GetNodeRelativePath(),
            I = InstantiateOnLoad()
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

    protected IPersistent[] GetPersistentChildren();

    protected string GetSceneFilePath();

    protected string GetNodeRelativePath();

    public static Node DeserializeAndInstantiate(Save save)
    {
        var scene = GD.Load<PackedScene>(save.Sfp);
        var instance = scene.Instantiate<Node>();
        var persistentInstance = (IPersistent)instance;
        persistentInstance.ProcessContent((JsonElement)save.C);
        persistentInstance.BeforeLoad();
        persistentInstance.Load();
        persistentInstance.AfterLoad();
        DeserializeChildren(instance, save);
        return instance;
    }

    public static void Deserialize(Node instance, Save save)
    {
        var persistentInstance = (IPersistent)instance;
        persistentInstance.ProcessContent((JsonElement)save.C);
        persistentInstance.BeforeLoad();
        persistentInstance.Load();
        persistentInstance.AfterLoad();
        DeserializeChildren(instance, save);
    }

    private static void DeserializeChildren(Node instance, Save save)
    {
        var children = save.Ch;
        foreach (var child in children)
        {
            if (child.I)
            {
                var childInstance = DeserializeAndInstantiate(child);
                instance.AddChild(childInstance);
            }
            else
            {
                var childNode = instance.GetNode<IPersistent>(child.Nrp);
                Deserialize((Node)childNode, child);
            }
        }
    }


    protected void ProcessContent(JsonElement saveContent);

    public object Save();

    public void BeforeLoad();

    public void Load();

    public void AfterLoad();

    public void ClearSaveContent();

    protected bool InstantiateOnLoad();
}