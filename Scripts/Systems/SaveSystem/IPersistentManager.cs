using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using FileAccess = Godot.FileAccess;

namespace BaseBuilding.Scripts.Systems.SaveSystem;

public interface IPersistentManager
{
    public void Save()
    {
        _BeforeSave();

        var persistentNodes = _GetPersistentNodes();
        using var saveGame = FileAccess.Open($"{OS.GetUserDataDir()}/{_GetSavePath()}", FileAccess.ModeFlags.Write);
        var saveQueue = new ConcurrentQueue<Save>();

        Parallel.ForEach(persistentNodes, persistentNode => { saveQueue.Enqueue(persistentNode.Serialize()); });

        foreach (var save in saveQueue)
        {
            var jsonBuffer = JsonSerializer.SerializeToUtf8Bytes(save);
            saveGame.StoreBuffer(jsonBuffer);
            saveGame.StoreLine("");
        }

        _AfterSave();
    }

    public void Load()
    {
        if (!FileAccess.FileExists($"{OS.GetUserDataDir()}/{_GetSavePath()}"))
        {
            return;
        }

        _BeforeLoad();

        using var saveGame = FileAccess.Open($"{OS.GetUserDataDir()}/{_GetSavePath()}", FileAccess.ModeFlags.Read);
        {
            while (saveGame.GetPosition() < saveGame.GetLength())
            {
                var jsonString = saveGame.GetLine();
                var persistentNode =
                    IPersistent.DeserializeAndInstantiate(JsonSerializer.Deserialize<Save>(jsonString)!);
                _AddSaveChild(persistentNode);
            }
        }

        _AfterLoad();
    }

    protected void _AddSaveChild(Node child);

    protected IPersistent[] _GetPersistentNodes();

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