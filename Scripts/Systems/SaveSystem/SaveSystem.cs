using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace BaseBuilding.Scripts.Systems.SaveSystem;

public partial class SaveSystem : Node
{
    public void Save()
    {
        var root = GetTree().Root;
        var persistentSystems = root.GetChildren().OfType<IPersistentManager>().ToArray();


        Parallel.ForEach(persistentSystems, persistentManager => { persistentManager.Save(); });

        GD.Print("Game saved");
    }

    public void Load()
    {
        var root = GetTree().Root;
        var persistentSystems = root.GetChildren().OfType<IPersistentManager>().ToArray();

        Parallel.ForEach(persistentSystems, persistentManager => { persistentManager.Load(); });

        GD.Print("Game loaded");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event.IsActionPressed("save_game"))
        {
            Save();
        }

        if (@event.IsActionPressed("load_game"))
        {
            Load();
        }
    }
}