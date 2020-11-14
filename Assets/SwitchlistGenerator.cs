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

            //Get a station with space that exports this cargo
            string targetStation = GetValidStation(cargoType, item.Number, false);

            //Update the state
            MoveCar(item.Number, targetStation);
        }
    }

    [ContextMenu("Generate new switchlist.")]
    public void GenerateNewSwitchlist()
    {
        print("STARTING NEW DAY");
        Random.InitState(layout.layoutState.Seed);

        //Get all the cars on the layout
        var allCars = new List<MovingCar>();
        foreach (var station in layout.layoutState.StationStates)
        {
            foreach (var car in station.CarsPresent)
            {
                var movingCar = new MovingCar();
                movingCar.CarName = car;
                movingCar.PreviousStation = station.Name;
                allCars.Add(movingCar);
            }
        }
        
        //Work out which cars are actually moving today
        allCars = GetMovingCars(allCars);
        //print("Cars moving: " + allCars);
        //Clear moving cars from their current stations (so they're not counted as present)
        foreach (var item in allCars)
        {
            layout.layoutState.StationStates.FirstOrDefault(b => b.Name == item.PreviousStation).CarsPresent.Remove(item.CarName);
        }
        
        //Send the cars to their new destination
        foreach (var car in allCars)
        {
            print(car.CarName + " is starting move logic.");
            //Create the switchmove object
            SwitchMove move = new SwitchMove();
            move.CurrentLocation = car.PreviousStation;
            move.CarNumber = car.CarName;

            var stationRef = layout.layout.Stations.FirstOrDefault(b => b.Name == car.PreviousStation);

            //Jumble the list of exported cargo
            System.Random rnd = new System.Random();
            string[] randomExportCargo = stationRef.CargoExport.OrderBy(x => rnd.Next()).ToArray();
            string cargo = "";
            string targetStation = "";
            foreach (var rndCargo in randomExportCargo)
            {
                print(car.CarName + " is testing export for " + rndCargo);
                //Check if the car takes this cargo
                if (GetValidCarTypeCargo(layout.layout.Cars.FirstOrDefault(b => b.Number == car.CarName).Type).Contains(rndCargo))
                {
                    //Find a random station that will recieve this cargo
                    targetStation = GetValidStation(rndCargo, car.CarName, true);

                    //if there's no recieving stations, skip to the next cargo
                    if (targetStation == "") continue;

                    //Otherwise, we're good to go, so let's proceed with that cargo and station
                    cargo = rndCargo;
                    move.Cargo = rndCargo;
                    print(car.CarName + " selected " + rndCargo);
                    break;
                }
            }



            //If we've failed to find a station that will take valid cargo, find some other station that exports valid cargo for a logistics move
            if (cargo == "" || targetStation == "")
            {
                print(car.CarName + " has no valid export. Starting logistics move search.");
                //Randomly sort the cargo this car carries
                string[] validCargo = GetValidCarTypeCargo(layout.layout.Cars.FirstOrDefault(b => b.Number == car.CarName).Type).OrderBy(x => rnd.Next()).ToArray();
                
                //Check the cargo for free stations
                foreach (var item in validCargo)
                {
                    print(car.CarName + " is testing logistics move for " + item);
                    targetStation = GetValidStation(item, car.CarName, false);
                    
                    if (targetStation != "")
                    {
                        print(car.CarName + " found logistics move to   " + targetStation);
                        move.Cargo = "empty";
                        break;
                    }
                    else
                    {
                        print(car.CarName + " found no valid station for logistics move that exports  " + item);
                    }

                       
                }
            }

            move.TargetLocation = targetStation;
            if (targetStation == "")
            {
                Debug.LogError(car.CarName + " totally failed to find a move and is staying put.");
                MoveCar(car.CarName, car.PreviousStation);
                continue;
            }
            
            MoveCar(car.CarName, targetStation);
            Debug.LogWarning(string.Format("{0} FROM {1} TO {2} CARGO {3}", move.CarNumber, move.CurrentLocation, move.TargetLocation, move.Cargo));
        }

        layout.layoutState.Seed++;
    }


    /// <param name="cars">A list of cars</param>
    /// <returns>The randomly selected cars (based on idlechance) that will move today</returns>
    List<MovingCar> GetMovingCars(List<MovingCar> cars)
    {
        var movingCars = new List<MovingCar>();
        foreach (var car in cars)
        {
            var chance = Random.value;
            if (chance < layout.industryConfig.IdleChance)
            {
                Debug.LogWarning(car.CarName + " is staying put today. (" + chance + ")");
                continue;
            }
            movingCars.Add(car);
        }
        return movingCars;
    }

    [System.Serializable]
    public class MovingCar
    {
        public string CarName, PreviousStation;
    }

    /// <summary>
    /// Updates the layout state with this car at the target station
    /// </summary>
    /// <param name="car">The car being moved</param>
    /// <param name="station">The target station</param>
    void MoveCar(string car, string station)
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
            //Debug.LogError("There are no valid " + importexport + " stations for car " + carNumber + " carrying " + cargoType + ". Skipping.");
            return "";
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
