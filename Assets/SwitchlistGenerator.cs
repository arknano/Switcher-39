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
        Random.InitState(layout.layoutState.Seed);
        foreach (var item in layout.layout.Cars)
        {
            //Pick a random valid cargo this car carries
            string[] validCargo = GetValidCarTypeCargo(item.Type);
            string cargoType = validCargo[Random.Range(0, validCargo.Length)];
            print(cargoType);

            //Get a station with space that exports this cargo
            string targetStation = GetValidStation(cargoType, item.Number, false);

            //Update the state
            MoveCar(item.Number, targetStation);
        }
    }

    [ContextMenu("Generate new switchlist.")]
    public void GenerateNewSwitchlist()
    {
        Random.InitState(layout.layoutState.Seed);
        foreach (var station in layout.layoutState.StationStates)
        {
            foreach (var car in station.CarsPresent)
            {
                //Early exit if the car isn't moving today
                var chance = Random.value;
                if (chance < layout.industryConfig.IdleChance)
                {
                    Debug.LogError(car + " is staying put today. (" +chance+")");
                    continue;
                }

                //Create the switchmove object
                SwitchMove move = new SwitchMove();
                move.CurrentLocation = station.Name;
                move.CarNumber = car;

                var stationRef = layout.layout.Stations.FirstOrDefault(b => b.Name == station.Name);

                //Jumble the list of exported cargo
                System.Random rnd = new System.Random();
                string[] randomExportCargo = stationRef.CargoExport.OrderBy(x => rnd.Next()).ToArray();
                string cargo = "";
                foreach (var rndCargo in randomExportCargo)
                {
                    //Check if the car takes this cargo
                    if (GetValidCarTypeCargo(layout.layout.Cars.FirstOrDefault(b => b.Number == car).Type).Contains(rndCargo))
                    {
                        //If it does, proceed with this cargo
                        cargo = rndCargo;
                        move.Cargo = rndCargo;
                        break;
                    }
                }

                string targetStation = "";

                //If the car doesn't take any of the export cargo, find some other station that exports valid cargo for a logistics move
                if (cargo == "")
                {
                    //Pick a random valid cargo this car carries
                    string[] validCargo = GetValidCarTypeCargo(layout.layout.Cars.FirstOrDefault(b => b.Number == car).Type);
                    string cargoType = validCargo[Random.Range(0, validCargo.Length)];

                    //Get a station with space that exports this cargo
                    targetStation = GetValidStation(cargoType, car, false);
                    move.Cargo = "empty";
                }
                //otherwise, proceed with the cargo we picked earlier
                else
                {
                    targetStation = GetValidStation(cargo, car, true);
                }

                move.TargetLocation = targetStation;
                Debug.LogWarning(string.Format("{0} FROM {1} TO {2} CARGO {3}", move.CarNumber, move.CurrentLocation, move.TargetLocation, cargo));
                MoveCar(car, targetStation, station.Name);
            }
        }
        layout.layoutState.Seed++;
    }

    /// <summary>
    /// Updates the layout state with this car at the target station
    /// </summary>
    /// <param name="car">The car being moved</param>
    /// <param name="station">The target station</param>
    void MoveCar(string car, string station, string previousStation)
    {
        //Grab the index of the station in the state array
        var state = layout.layoutState.StationStates;
        int stationIndex = System.Array.IndexOf(state, state.FirstOrDefault(b => b.Name == station));

        //Add the car to that station's present list
        state[stationIndex].CarsPresent.Add(car);
    }

    string GetValidStation(string cargoType, string carNumber, bool importing)
    {
        //Get all the stations that service this cargo, that still have space.
        string[] stationList = FilterFullStations(GetAllStationsMatchingCargo(cargoType, importing));

        //Skip this car if we can't put it anywhere.
        if (stationList.Length == 0)
        {
            string importexport = importing ? "import" : "export";
            Debug.LogError("There are no valid " + importexport + " stations for car " + carNumber + ". Skipping.");
            return null;
        }
        //Pick a random station from that list
        return stationList[Random.Range(0, stationList.Length)];
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

    [System.Serializable]
    public class SwitchMove
    {
        public string CarNumber, CurrentLocation, TargetLocation, Cargo;
    }
}
