﻿using System.Linq;
using System.Text.Json;
using Godot;

namespace BaseBuilding.Scripts.Systems.SaveSystem;

public abstract partial class PersistentArea3D<T> : Area3D, IPersistent
{
    protected bool LoadedFromSave = false;
    public T? SaveContent { get; private set; } = default!;

    public IPersistent[] GetPersistentChildren()
    {
        return GetChildren().OfType<IPersistent>().ToArray();
    }

    public string GetSceneFilePath()
    {
        return SceneFilePath;
    }

    public void ProcessContent(JsonElement saveContent)
    {
        SaveContent = saveContent.Deserialize<T>()!;
    }

    public abstract object Save();

    public void BeforeLoad()
    {
        LoadedFromSave = true;
    }

    public abstract void Load();


    public void AfterLoad()
    {
    }

    public void ClearSaveContent()
    {
        SaveContent = default(T);
    }
}