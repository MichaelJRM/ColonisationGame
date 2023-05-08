using System.Linq;
using System.Threading.Tasks;
using BaseBuilding.scripts.systems.BuildingSystem;
using Godot;

namespace BaseBuilding.Scripts.Systems.SaveSystem;

public partial class SaveSystem : Node
{
    public void Save()
    {
        GD.Print("Saving game...");
        var root = GetTree().Root;
        var persistentSystems = root.GetChildren().OfType<IPersistentManager>().ToArray();

        Parallel.ForEach(persistentSystems, persistentManager => { persistentManager.Save(); });

        GD.Print("Game saved");
    }

    public void Load()
    {
        GD.Print("Loading game...");
        var root = GetTree().Root;

        var buildingSystem = root.GetNode<BuildingSystem>("BuildingSystem");
        ((IPersistentManager)buildingSystem).Load();

        var pipeSystem = root.GetNode<scripts.systems.PipeSystem.PipeSystem>("PipeSystem");
        ((IPersistentManager)pipeSystem).Load();

        var energySystem = root.GetNode<EnergySystem.EnergySystem>("EnergySystem");
        ((IPersistentManager)energySystem).Load();

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