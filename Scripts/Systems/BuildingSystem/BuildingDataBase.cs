using System.Collections.Generic;
using Godot;

namespace BaseBuilding.scripts.systems.BuildingSystem;

public partial class BuildingDataBase : Node
{
    private const string ResourceExtractorsPath = "res://Gameplay/Buildings/ResourceExtractors/entities/";
    private const string ResourceStoragesPath = "res://Gameplay/Buildings/ResourceStorages/entities/";
    private const string ResourceConvertersPath = "res://Gameplay/Buildings/ResourceConverters/entities/";
    private const string EnergyPath = "res://Gameplay/Buildings/Energy/entities/";
    public readonly List<BuildingResource> Energy = new();
    public readonly List<BuildingResource> ResourceConverters = new();
    public readonly List<BuildingResource> ResourceExtractors = new();
    public readonly List<BuildingResource> ResourceStorages = new();

    public override void _Ready()
    {
        // Task.Factory.StartNew(_loadAllResources);
        _loadAllResources();
    }

    private void _loadAllResources()
    {
        _loadResources(ResourceExtractorsPath, ResourceExtractors);
        _loadResources(ResourceStoragesPath, ResourceStorages);
        _loadResources(ResourceConvertersPath, ResourceConverters);
        _loadResources(EnergyPath, Energy);
    }


    private static void _loadResources<T>(string path, List<T> into) where T : Resource
    {
        var dir = DirAccess.Open(path);
        var resourceDirectories = dir.GetDirectories();
        foreach (var directory in resourceDirectories)
        {
            var resource = ResourceLoader.Load<T>($"{path}{directory}/{directory}.tres");
            into.Add(resource);
        }
    }
}