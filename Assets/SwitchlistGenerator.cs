using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SwitchlistGenerator : MonoBehaviour
{
    public Layout layout;

    /// <summary>
    /// Populates stations with cars from scratch. Cars will be randomly placed in stations that export a cargo the car carries
    /// </summary>
    [ContextMenu("Populate Stations")]
    public void PopulateStations()
    {
        layout.CreateStateScaffold();
        UnityEngine.Random.InitState(layout.layoutState.Seed);
        foreach (var item in layout.layout.Cars)
        {
            //Pick a random valid cargo this car carries
            string[] validCargo = GetValidCarTypeCargo(item.Type);
            string cargoType = validCargo[Random.Range(0, validCargo.Length)];
            print(cargoType);

            //Get a station with space that exports this cargo
            string targetStation = GetValidStation(cargoType, item.Number, false);

            //Grab the index of the station in the state array
            var state = layout.layoutState.StationStates;
            int stationIndex = System.Array.IndexOf(state, state.FirstOrDefault(b => b.Name == targetStation));

            //Add the car to that station's present list
            state[stationIndex].CarsPresent.Add(item.Number);
        }
    }

    //TODO: make sure cargo is valid for the car
    [ContextMenu("Generate new switchlist.")]
    public void GenerateNewSwitchlist()
    {
        UnityEngine.Random.InitState(layout.layoutState.Seed);
        foreach (var station in layout.layoutState.StationStates)
        {
            foreach (var car in station.CarsPresent)
            {
                if (Random.value >= layout.industryConfig.IdleChance)
                {
                    var stationRef = layout.layout.Stations.FirstOrDefault(b => b.Name == station.Name);

                    //Pick a random cargo this station exports
                    string cargo = stationRef.CargoExport[Random.Range(0, stationRef.CargoExport.Length)];

                    //Get a station with space that imports this cargo
                    string targetStation = GetValidStation(cargo, car, true);

                }
            }
        }
    }

    /// <param name="cargo">The cargo name</param>
    /// <param name="importing">Check for imports (TRUE) or exports (FALSE)</param>
    /// <returns>A string array of the stations that import or export matching cargo</returns>
    string[] GetAllStationsMatchingCargo(string cargo, bool importing)
    {
        var stations = layout.layout.Stations;
        List<string> matchingStations = new List<string>();
        foreach (var item in stations)
        {
            if (importing)
            {
                if (!item.CargoImport.Contains(cargo))
                    continue;
            }
            else
            {
                if (!item.CargoExport.Contains(cargo))
                    continue;
            }
            matchingStations.Add(item.Name);
        }
        return matchingStations.ToArray();
    }

    /// <param name="stations">A string array of stations to filter</param>
    /// <returns>String array containing only stations with at least one car space</returns>
    string[] FilterFullStations(string[] stations)
    {
        var stationsList = layout.layout.Stations;
        var state = layout.layoutState.StationStates;

        List<string> filteredStations = new List<string>();
        foreach (var item in stations)
        {
            if (stationsList.FirstOrDefault(b => b.Name == item).CarLimit > state.FirstOrDefault(b => b.Name == item).CarsPresent.Count)
                filteredStations.Add(item);
        }
        return filteredStations.ToArray();
    }

    /// <param name="carType">The type of car (fromt industry data)</param>
    /// <returns>String array containing all valid cargo types</returns>
    string[] GetValidCarTypeCargo(string carType)
    {
        return layout.industryConfig.CarTypes.FirstOrDefault(b => b.TypeName == carType).ValidCargo;
    }

    string GetValidStation(string cargoType, string carNumber, bool importing)
    {
        //Get all the stations that service this cargo, that still have space.
        string[] stationList = FilterFullStations(GetAllStationsMatchingCargo(cargoType, importing));

        //Skip this car if we can't put it anywhere.
        if (stationList.Length == 0)
        {
            Debug.LogError("There are no valid stations for car " + carNumber + ". Skipping.");
            return null;
        }
        //Pick a random station from that list
        return stationList[Random.Range(0, stationList.Length)];
    }
}
