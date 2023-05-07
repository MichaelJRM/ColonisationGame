using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace BaseBuilding.Scripts.Systems.SaveSystem;

public interface IPersistentManager
{
    public void Save()
    {
        _BeforeSave();
        var persistentNodes = _GetPersistentChildren();
        using var saveGame = FileAccess.Open(_GetSavePath(), FileAccess.ModeFlags.Write);
        var saveQueue = new ConcurrentQueue<Save>();

        Parallel.ForEach(persistentNodes, persistentNode => { saveQueue.Enqueue(persistentNode.Serialize()); });

        foreach (var save in saveQueue)
        {
            var jsonString = JsonSerializer.Serialize(save);
            saveGame.StoreLine(jsonString);
        }

        _AfterSave();
    }

    public void Load()
    {
        if (!FileAccess.FileExists(_GetSavePath()))
        {
            return;
        }

        _BeforeLoad();

        using var saveGame = FileAccess.Open(_GetSavePath(), FileAccess.ModeFlags.Read);

        while (saveGame.GetPosition() < saveGame.GetLength())
        {
            var jsonString = saveGame.GetLine();
            var persistentNode = IPersistent.Deserialize(JsonSerializer.Deserialize<Save>(jsonString)!);
            _AddSaveChild(persistentNode);
        }

        _AfterLoad();
    }

    protected void _AddSaveChild(Node child);

    protected IPersistent[] _GetPersistentChildren();

    protected string _GetSavePath();

    protected void _BeforeSave()
    {
    }

    protected void _AfterSave()
    {
    }

    protected void _BeforeLoad()
    {
    }

    protected void _AfterLoad()
    {
    }
}