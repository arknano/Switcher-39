using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Layout : MonoBehaviour
{
    public TextAsset layoutJSON, stateJSON, industryJSON;
    public LayoutConfig layout;
    public LayoutState layoutState;
    public IndustryConfig industryConfig;

    [System.Serializable]
    public class IndustryConfig
    {
        public float IdleChance;
        public CarType[] CarTypes;
    }

    [System.Serializable]
    public class CarType
    {
        public string TypeName;
        public string[] ValidCargo;
    }

    [System.Serializable]
    public class LayoutState
    {
        public int Seed;
        public StationState[] StationStates;
    }

    [System.Serializable]
    public class StationState
    {
        public string Name;
        public List<string> CarsPresent = new List<string>();

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
        public int CarLimit;
        public string[] CargoImport, CargoExport;
    }

    [System.Serializable]
    public class Car
    {
        public string Number;
        [Multiline]
        public string Description;
        public string Type;
    }

    [ContextMenu("Load Data")]
    public void LoadData()
    {
        layout = JsonUtility.FromJson<LayoutConfig>(layoutJSON.text);
        layoutState = JsonUtility.FromJson<LayoutState>(stateJSON.text);
        industryConfig = JsonUtility.FromJson<IndustryConfig>(industryJSON.text);
    }

    [ContextMenu("Save Data")]
    public void SaveData()
    {
        File.WriteAllText(AssetDatabase.GetAssetPath(layoutJSON), JsonUtility.ToJson(layout));
        File.WriteAllText(AssetDatabase.GetAssetPath(stateJSON), JsonUtility.ToJson(layoutState));
        File.WriteAllText(AssetDatabase.GetAssetPath(industryJSON), JsonUtility.ToJson(industryConfig));
    }

    [ContextMenu("Clear Loaded Data")]
    public void ResetData()
    {
        layout = null;
        layoutState = null;
    }

    [ContextMenu("------------")]public void Nothing(){}

    [ContextMenu("Reset State")]
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