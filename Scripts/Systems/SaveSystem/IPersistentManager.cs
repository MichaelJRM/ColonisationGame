using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace BaseBuilding.Scripts.Systems.SaveSystem;

public interface IPersistentManager
{
    public void Save()
    {
        var persistentNodes = GetPersistentChildren();
        using var saveGame = FileAccess.Open(GetSavePath(), FileAccess.ModeFlags.Write);
        var saveQueue = new ConcurrentQueue<Save>();

        Parallel.ForEach(persistentNodes, persistentNode => { saveQueue.Enqueue(persistentNode.Serialize()); });

        foreach (var save in saveQueue)
        {
            saveGame.StoreLine(JsonSerializer.Serialize(save));
        }
    }

    public void Load()
    {
        if (!FileAccess.FileExists(GetSavePath()))
        {
            return;
        }

        using var saveGame = FileAccess.Open(GetSavePath(), FileAccess.ModeFlags.Read);

        while (saveGame.GetPosition() < saveGame.GetLength())
        {
            var jsonString = saveGame.GetLine();
            var persistentNode = IPersistent.Deserialize(JsonSerializer.Deserialize<Save>(jsonString)!);
            AddSaveChild(persistentNode);
        }
    }

    public void AddSaveChild(Node child);

    public IPersistent[] GetPersistentChildren();

    public string GetSavePath();
}