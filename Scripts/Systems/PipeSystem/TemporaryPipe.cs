using Godot;

namespace BaseBuilding.scripts.systems.PipeSystem;

public partial class TemporaryPipe : Pipe
{
    [Export] public MeshInstance3D MeshInstance3D { get; private set; } = null!;

    public void CreateAndAssignMesh(float? size = null)
    {
        MeshInstance3D.Mesh = CreateMesh(size);
    }
}