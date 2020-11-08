using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneUpdate : MonoBehaviour
{
    public Layout state;
    public Text[] stationTexts;

    [ContextMenu("Update Stations")]
    public void UpdateStations()
    {
        for (int i = 0; i < stationTexts.Length; i++)
        {
            Text item = (Text)stationTexts[i];
            item.text = state.layout.Stations[i].Name;
        }
    }
}
