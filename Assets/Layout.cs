using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Layout : MonoBehaviour
{
    public TextAsset layoutJSON, stateJSON;
    public LayoutConfig layout;
    public LayoutState layoutState;


    [System.Serializable]
    public class LayoutState
    {
        public StationState[] StationStates;
    }

    [System.Serializable]
    public class StationState
    {
        public string Name;
        public string[] CarsPresent;

        public StationState(string name)
        {
            Name = name;
        }
    }
    
    [System.Serializable]
    public class LayoutConfig
    {
        public string LayoutName;
        public Station[] Stations;
        public Car[] Cars;
    }
    
    [System.Serializable]
    public class Station
    {
        public string Name;
        public string[] CargoImport, CargoExport;
    }

    [System.Serializable]
    public class Car
    {
        public string Number;
        [Multiline]
        public string Description;
        public string[] ValidCargo;
    }

    [ContextMenu("Load Data")]
    public void LoadData()
    {
        layout = JsonUtility.FromJson<LayoutConfig>(layoutJSON.text);
        layoutState = JsonUtility.FromJson<LayoutState>(stateJSON.text);
    }

    [ContextMenu("Save Data")]
    public void SaveData()
    {
        File.WriteAllText(AssetDatabase.GetAssetPath(layoutJSON), JsonUtility.ToJson(layout));
        File.WriteAllText(AssetDatabase.GetAssetPath(stateJSON), JsonUtility.ToJson(layoutState));
    }

    [ContextMenu("Clear Loaded Data")]
    public void ResetData()
    {
        layout = null;
        layoutState = null;
    }

    [ContextMenu("Create State Scaffold")]
    public void CreateStateScaffold()
    {
        layoutState = new LayoutState();
        List<StationState> stations = new List<StationState>();
        foreach (var item in layout.Stations)
        {
            stations.Add(new StationState(item.Name));
        }
        layoutState.StationStates = stations.ToArray();
    }
}