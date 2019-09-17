using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace RTSEngine
{
    public class MovementManager : MonoBehaviour {

        public static MovementManager Instance;

        public LayerMask GroundUnitLayerMask; //a layer mask that contains ground unit-related layers.
        public LayerMask AirUnitLayerMask; //a layer mask that contains air unit-related layers.

        //The stopping distance when a unit moves to an empty space of the map:
        public float MvtStoppingDistance = 0.1f;

        //Mvt target effect:
        public MvtTargetEffect MvtTargetEffectObj;

        //Other components:
        GameManager GameMgr;
        UIManager UIMgr;
        TerrainManager TerrainMgr;
        SelectionManager SelectionMgr;

        void Awake ()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else if(Instance != this)
            {
                Destroy(this);
            }
        }

        void Start ()
        {
            GameMgr = GameManager.Instance;
            UIMgr = GameMgr.UIMgr;
            TerrainMgr = GameMgr.TerrainMgr;
            SelectionMgr = GameMgr.SelectionMgr;
        }

        public class UnitTypes
        {
            public string Code;
            public float Radius;
            public int RangeTypeID;
            public List<Unit> UnitsList;
        }

        public void Move(Unit RefUnit, Vector3 Destination, float CircleRadius, GameObject TargetObj, InputTargetMode TargetMode)
        {
            //if this is an online game
            if (GameManager.MultiplayerGame == true)
            {
                //and this is the owner of the unit
                if (GameMgr.IsLocalPlayer(RefUnit.gameObject) == true)
                {
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Unit;
                    NewInputAction.TargetMode = (byte)TargetMode; //initially, this is set to none

                    //source
                    NewInputAction.Source = RefUnit.gameObject;
                    NewInputAction.Target = TargetObj;

                    NewInputAction.InitialPos = RefUnit.transform.position;
                    NewInputAction.TargetPos = Destination;

                    NewInputAction.Value = (int)CircleRadius;

                    //sent input
                    InputManager.SendInput(NewInputAction);
                }
            }
            else //single player game
            {
                //directly move the unit
                MoveLocal(RefUnit, Destination, CircleRadius, TargetObj, TargetMode);
            }
        }

        //move one single unit to a destination
        public void MoveLocal(Unit RefUnit, Vector3 Destination, float CircleRadius, GameObject TargetObj, InputTargetMode TargetMode)
        {
            if (RefUnit.CanBeMoved == true) //making sure the unit can be moved
            {
                Destination = new Vector3(Destination.x, TerrainMgr.SampleHeight(Destination), Destination.z);
                Vector3 MvtPos = Vector3.zero;

                //Make the ref unit's target collider pos so it won't be detected as invalid destination
                RefUnit.TargetPosColl.isTrigger = false;

                //trigger if we can move directly to the chosen destination (assuming that there's no target object to move to).
                if (IsDestinationClear(Destination, RefUnit, out MvtPos) && TargetObj == null)
                {
                    RefUnit.CheckUnitPathLocal(MvtPos, null, Destination, MvtStoppingDistance, InputTargetMode.None);
                }
                else
                {
                    //the positions that the units will fill
                    List<Vector3> Positions = CircleFormation(Destination, CircleRadius, RefUnit);

                    //get a valid position for the unit to move to
                    while (Positions.Count == 0)
                    {
                        Positions = CircleFormation(Destination, CircleRadius, RefUnit);
                        CircleRadius += RefUnit.NavAgent.radius;
                    }

                    RefUnit.CheckUnitPathLocal(Positions[GetClosestPosID(Positions, RefUnit.transform.position)], TargetObj, Destination, MvtStoppingDistance, TargetMode);
                }

                //trigger the ref unit's target collider pos so it will be detected again:
                RefUnit.TargetPosColl.isTrigger = true;
            }
            else if (RefUnit.FactionID == GameManager.PlayerFactionID) //if the unit that can't be moved belongs to the local player's faction
            {
                UIMgr.ShowPlayerMessage("Can't move selected unit!", UIManager.MessageTypes.Error);
            }

            if (GameMgr.Events)
                GameMgr.Events.OnUnitMoveAttempt(RefUnit); //custom event call
        }

        public void Move(List<Unit> UnitsList, Vector3 Destination, float CircleRadius, GameObject TargetObj, InputTargetMode TargetMode)
        {
            //if this is an online game
            if (GameManager.MultiplayerGame == true)
            {
                //and this is the owner of the unit
                if (GameMgr.IsLocalPlayer(UnitsList[0].gameObject) == true)
                {
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Group;
                    NewInputAction.TargetMode = (byte)TargetMode; //initially, this is set to none

                    //source
                    NewInputAction.GroupSourceID = InputManager.UnitListToString(UnitsList);
                    NewInputAction.Target = TargetObj;

                    NewInputAction.TargetPos = Destination;

                    NewInputAction.Value = (int)CircleRadius;

                    //sent input
                    InputManager.SendInput(NewInputAction);
                }
            }
            else //single player game
            {
                //directly move the unit
                MoveLocal(UnitsList, Destination, CircleRadius, TargetObj, TargetMode);
            }
        }

        //move a large amount of units
        public void MoveLocal(List<Unit> UnitsList, Vector3 Destination, float CircleRadius, GameObject TargetObj, InputTargetMode TargetMode)
        {
            //go through the unit list and trigger the target position collider
            for (int i = 0; i < UnitsList.Count; i++)
            {
                UnitsList[i].TargetPosColl.isTrigger = false;
            }

            if (UnitsList.Count > 0)
            { //make sure, at least a unit has been selected
              //sort the units by their types
                List<UnitTypes> SelectedUnitTypes = SetSelectedUnitsTypes(UnitsList, false);

                //sort the unit groups depending on the unit's radius
                SortSelectedUnitTypes(UnitSortType.Radius, ref SelectedUnitTypes);

                //go through all unit types:
                foreach (UnitTypes UnitGroup in SelectedUnitTypes)
                {
                    List<Unit> CurrentUnitsList = UnitGroup.UnitsList;
                    //sort the units in this group based on their distance from the destination
                    SortUnitsByDistance(ref CurrentUnitsList, Destination);
                    bool UnitMoved = false;

                    //the positions that the units will fill
                    List<Vector3> Positions = new List<Vector3>();

                    //go through the list
                    for (int i = 0; i < CurrentUnitsList.Count; i++)
                    {
                        if (CurrentUnitsList[i].CanBeMoved == true) //making sure that the unit can be moved
                        {
                            //get a valid position for the unit to move to
                            while (Positions.Count == 0)
                            {
                                Positions = CircleFormation(Destination, CircleRadius, CurrentUnitsList[i]);
                                CircleRadius += CurrentUnitsList[i].NavAgent.radius;
                            }

                            UnitMoved = true;
                            int ID = GetClosestPosID(Positions, CurrentUnitsList[i].transform.position);
                            //Inform the units about the target position to go to and they'll see if there's a valid path to go there:
                            CurrentUnitsList[i].CheckUnitPathLocal(Positions[ID], TargetObj, Destination, MvtStoppingDistance, TargetMode);

                            Positions.RemoveAt(ID);

                            //trigger the target pos collider
                            CurrentUnitsList[i].TargetPosColl.isTrigger = true;
                        }

                        if (GameMgr.Events)
                            GameMgr.Events.OnUnitMoveAttempt(CurrentUnitsList[i]); //custom event call
                    }

                    //if units have moved
                    if (UnitMoved == true)
                    {
                        //mvt order audio:
                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, CurrentUnitsList[0].MvtOrderAudio, false);

                        //Show the mvt target effect:
                        if (MvtTargetEffectObj != null)
                        {
                            MvtTargetEffectObj.transform.position = Destination;
                            MvtTargetEffectObj.Activate();
                        }
                    }
                    else
                    {
                        //display a message to let the player know that the units couldn't be moved:
                        UIMgr.ShowPlayerMessage("Can't move selected unit(s)", UIManager.MessageTypes.Error);
                    }
                }
            }
        }

        //Sort a list of units depending on the distance between each unit and a target destination:
        public void SortUnitsByDistance(ref List<Unit> UnitsList, Vector3 Destination)
        {
            //only if there are more than one unit in the list
            if (UnitsList.Count > 1)
            {
                //go through all units
                for (int i = 0; i < UnitsList.Count - 1; i++)
                {
                    if (UnitsList[i] != null)
                    {
                        //calculate the distance between this unit and the destination
                        float Distance = Vector3.Distance(UnitsList[i].transform.position, Destination);
                        int current = i;
                        //go through the rest of units in the list
                        for (int j = i + 1; j < UnitsList.Count; j++)
                        {
                            //compare the distance to the destination
                            if (UnitsList[j] != null)
                            {
                                if (Distance > Vector3.Distance(UnitsList[j].transform.position, Destination))
                                {
                                    current = j;
                                    Distance = Vector3.Distance(UnitsList[j].transform.position, Destination);
                                }
                            }
                        }
                        //swap units
                        if (current != i)
                        {
                            Unit SwapUnit = UnitsList[current];
                            UnitsList[current] = UnitsList[i];
                            UnitsList[i] = SwapUnit;
                        }


                    }
                }
            }
        }

        //Set selected units types:
        public List<UnitTypes> SetSelectedUnitsTypes(List<Unit> UnitsList, bool ForceAttackComp)
        {
            List<UnitTypes> SelectedUnitTypes = new List<UnitTypes>();

            //First, make lists of the selected units based on their type (code):
            if (UnitsList.Count > 0)
            {
                for (int i = 0; i < UnitsList.Count; i++)
                {
                    if (UnitsList[i] != null)
                    {
                        //if the attack component is required to have don't include a unit that doesn't have it)
                        if (ForceAttackComp == false || UnitsList[i].AttackMgr != null)
                        {
                            int j = 0;
                            bool Found = false;
                            //group units from the same type together if the unit group is already there
                            while (j < SelectedUnitTypes.Count && Found == false)
                            {
                                if ((SelectedUnitTypes[j].Code == UnitsList[i].Code && ForceAttackComp == false) || (UnitsList[i].AttackMgr != null && (SelectedUnitTypes[j].RangeTypeID == UnitsList[i].AttackMgr.RangeTypeID && ForceAttackComp == true)))
                                {
                                    //Add it to this list type:
                                    SelectedUnitTypes[j].UnitsList.Add(UnitsList[i]);
                                    Found = true;
                                }
                                j++;
                            }

                            //if this is the first unit of this type to be added to the list
                            if (Found == false)
                            {
                                //create a new group for it
                                UnitTypes NewUnitType = new UnitTypes();
                                //set the new group settings
                                NewUnitType.Code = UnitsList[i].Code;
                                NewUnitType.Radius = UnitsList[i].NavAgent.radius;
                                if (UnitsList[i].AttackMgr)
                                {
                                    NewUnitType.RangeTypeID = UnitsList[i].AttackMgr.RangeTypeID;
                                }
                                NewUnitType.UnitsList = new List<Unit>();
                                //add the unit
                                NewUnitType.UnitsList.Add(UnitsList[i]);

                                SelectedUnitTypes.Add(NewUnitType);
                            }
                        }
                    }
                }
            }

            return SelectedUnitTypes;
        }

        //list of types that we use to sort a unit types
        public enum UnitSortType { UnitStopDistance, BuildingStopDistance, Radius }

        //sort unit types depending on the their attack stop distances or radius
        public void SortSelectedUnitTypes(UnitSortType SortBy, ref List<UnitTypes> SelectedUnitTypes) //SortBy = 1 means sort by min unit stopping distance, SortBy = 2, means sory by min building stopping distance
        {
            //only if there are more than three units in the unit type
            if (SelectedUnitTypes.Count > 3)
            {
                //divide the selected units into lists depending on their codes.
                for (int i = 0; i < SelectedUnitTypes.Count - 1; i++)
                {
                    int current = i;
                    for (int j = i + 1; j < SelectedUnitTypes.Count; j++)
                    {

                        //depending on the sepcified sorting type, sort the unit types
                        switch (SortBy)
                        {
                            case UnitSortType.UnitStopDistance:
                                if (AttackManager.Instance.RangeTypes[SelectedUnitTypes[current].RangeTypeID].UnitStoppingDistance > AttackManager.Instance.RangeTypes[SelectedUnitTypes[j].RangeTypeID].UnitStoppingDistance)
                                {
                                    current = j;
                                }
                                break;
                            case UnitSortType.BuildingStopDistance:
                                if (AttackManager.Instance.RangeTypes[SelectedUnitTypes[current].RangeTypeID].BuildingStoppingDistance > AttackManager.Instance.RangeTypes[SelectedUnitTypes[current].RangeTypeID].BuildingStoppingDistance)
                                {
                                    current = j;
                                }
                                break;
                            case UnitSortType.Radius:
                                if (SelectedUnitTypes[current].Radius > SelectedUnitTypes[j].Radius)
                                {
                                    current = j;
                                }
                                break;
                            default:
                                Debug.LogError("Invalid unit sort type!");
                                break;
                        }
                    }

                    //swap units
                    if (current != i)
                    {
                        UnitTypes SwapUnitType = SelectedUnitTypes[current];
                        SelectedUnitTypes[current] = SelectedUnitTypes[i];
                        SelectedUnitTypes[i] = SwapUnitType;
                    }
                }
            }

        }

        //should the unit change its current target and attack the new one, attack automatically, both or none?
        public enum AttackModes { Change, Assigned, Full, None }

        public void LaunchAttack (Unit RefUnit, GameObject TargetObj, AttackModes AttackMode)
        {
            //if this is an online game
            if (GameManager.MultiplayerGame == true)
            {
                //and this is the owner of the unit
                if (GameMgr.IsLocalPlayer(RefUnit.gameObject) == true)
                {
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Unit;
                    NewInputAction.TargetMode = (byte)InputTargetMode.Attack;

                    //source
                    NewInputAction.Source = RefUnit.gameObject;
                    NewInputAction.Target = TargetObj;

                    NewInputAction.InitialPos = RefUnit.transform.position;

                    NewInputAction.Value = (int)AttackMode;

                    //sent input
                    InputManager.SendInput(NewInputAction);
                }
            }
            else //single player game
            {
                //directly move the unit
                LaunchAttackLocal(RefUnit, TargetObj, AttackMode);
            }
        }

        //make a unit attack a target
        public void LaunchAttackLocal (Unit RefUnit, GameObject TargetObj, AttackModes AttackMode)
        {
            //attack stopping distance:
            float AttackStoppingDistance = 0.0f;
            //save the circle's radius here
            float CircleRadius = 0.0f;
            //the positions that the units will fill
            List<Vector3> Positions = new List<Vector3>();

            //set the initial radius distance depending on the unit's distance from the target.
            if (TargetObj.GetComponent<Unit>())
            {
                CircleRadius = TargetObj.GetComponent<Unit>().NavAgent.radius + AttackManager.Instance.RangeTypes[RefUnit.AttackMgr.RangeTypeID].UnitStoppingDistance;
            }
            else if (TargetObj.GetComponent<Building>())
            {
                CircleRadius = TargetObj.GetComponent<Building>().Radius + AttackManager.Instance.RangeTypes[RefUnit.AttackMgr.RangeTypeID].BuildingStoppingDistance;
            }

            AttackStoppingDistance = CircleRadius;

            //trigger the target pos collider for the ref unit so that doesn't get detected as invalid destination
            RefUnit.TargetPosColl.isTrigger = false;

            //get a valid position for the unit to move to
            while (Positions.Count == 0)
            {
                Positions = CircleFormation(TargetObj.transform.position, CircleRadius, RefUnit);
                CircleRadius += RefUnit.NavAgent.radius * 2f;
            }

            //Make the units attack the building/unit
            RefUnit.AttackMgr.SetAttackTargetLocal(TargetObj);

            if (Vector3.Distance(TargetObj.transform.position, RefUnit.transform.position) > AttackStoppingDistance)
            {
                RefUnit.CheckUnitPathLocal(Positions[GetClosestPosID(Positions, RefUnit.transform.position)], TargetObj, TargetObj.transform.position, MvtStoppingDistance, InputTargetMode.Attack);
            }
            else
            {
                RefUnit.CheckUnitPathLocal(RefUnit.transform.position, TargetObj, TargetObj.transform.position, MvtStoppingDistance, InputTargetMode.Attack);
            }

            RefUnit.TargetPosColl.isTrigger = true;

            //flash the selection plane of the target object:
            if (RefUnit.FactionID == GameManager.PlayerFactionID && (AttackMode == AttackModes.Full || AttackMode == AttackModes.Assigned))
            {
                SelectionMgr.FlashSelection(TargetObj, false);
                AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, RefUnit.AttackMgr.AttackOrderSound, false);
            }
        }

        public void LaunchAttack(List<Unit> UnitsList, GameObject TargetObj, AttackModes AttackMode)
        {
            //if this is an online game
            if (GameManager.MultiplayerGame == true)
            {
                //and this is the owner of the unit
                if (GameMgr.IsLocalPlayer(UnitsList[0].gameObject) == true)
                {
                    //send input action to the input manager
                    InputVars NewInputAction = new InputVars();
                    //mode:
                    NewInputAction.SourceMode = (byte)InputSourceMode.Group;
                    NewInputAction.TargetMode = (byte)InputTargetMode.Attack;

                    //source
                    NewInputAction.GroupSourceID = InputManager.UnitListToString(UnitsList);
                    NewInputAction.Target = TargetObj;

                    NewInputAction.Value = (int)AttackMode;

                    //sent input
                    InputManager.SendInput(NewInputAction);
                }
            }
            else //single player game
            {
                //directly move the unit
                LaunchAttackLocal(UnitsList, TargetObj, AttackMode);
            }
        }

        //make a list of units launch an attack on a target object:
        public void LaunchAttackLocal(List<Unit> UnitsList, GameObject TargetObj, AttackModes AttackMode)
        {
            //change the target?
            bool ChangeTarget = (AttackMode == AttackModes.Full || AttackMode == AttackModes.Change) ? true : false;
            //assigned attack only?
            bool AssignedAttackOnly = (AttackMode == AttackModes.Full || AttackMode == AttackModes.Assigned) ? true : false;

            //this will hold all different types of the selected units with their main settings
            List<UnitTypes> SelectedUnitTypes = SetSelectedUnitsTypes(UnitsList, true); //true to let the function know that we need to remove all units who don't have an attack component

            if (SelectedUnitTypes.Count > 0)
            {
                //sort unit types depending on their attack stopping distances
                SortSelectedUnitTypes((TargetObj.GetComponent<Unit>()) ? UnitSortType.UnitStopDistance : UnitSortType.BuildingStopDistance, ref SelectedUnitTypes); //sort the selected units by type

                Unit MainAttackUnit = null; //This is the unit that will trigger the attack order audio clip
                //attack stopping distance:
                float AttackStoppingDistance = 0.0f;
                //save the circle's radius here
                float CircleRadius = 0.0f;
                //the positions that the units will fill
                List<Vector3> Positions = new List<Vector3>();

                //go through each unit type list:
                foreach (UnitTypes UnitGroup in SelectedUnitTypes)
                {
                    Positions.Clear();
                    List<Unit> CurrentUnitsList = UnitGroup.UnitsList;

                    SortUnitsByDistance(ref CurrentUnitsList, TargetObj.transform.position); //sort the selected units by distance so the closer units get there first

                    //set the stopping distance depending on the unit's distance from the target.
                    if (TargetObj.GetComponent<Unit>())
                    {
                        CircleRadius = TargetObj.GetComponent<Unit>().NavAgent.radius + AttackManager.Instance.RangeTypes[UnitGroup.RangeTypeID].UnitStoppingDistance;
                    }
                    else if (TargetObj.GetComponent<Building>())
                    {
                        CircleRadius = TargetObj.GetComponent<Building>().Radius + AttackManager.Instance.RangeTypes[UnitGroup.RangeTypeID].BuildingStoppingDistance;
                    }

                    AttackStoppingDistance = CircleRadius;

                    //go through the unit list and trigger the target position collider
                    for (int i = 0; i < CurrentUnitsList.Count; i++)
                    {
                        CurrentUnitsList[i].TargetPosColl.isTrigger = false;
                    }

                    for (int i = 0; i < CurrentUnitsList.Count; i++)
                    {
                        //if this unit is valid
                        if (CurrentUnitsList[i] != null)
                        {
                            //make sure that the unit can attack when the target is assigned or if that is not forced:
                            if (AssignedAttackOnly == false || CurrentUnitsList[i].AttackMgr.AttackOnAssign == true)
                            {
                                //make sure that this unit can attack when invisible, if it's invisible:
                                if (CurrentUnitsList[i].IsInvisible == false || CurrentUnitsList[i].InvisibilityMgr.CanAttack == true)
                                {
                                    //either we can change the unit's target or the unit doesn't have a target to begin with
                                    if (CurrentUnitsList[i].AttackMgr.AttackTarget == null || ChangeTarget == true)
                                    {
                                        //check if the unit can actually attack the target:
                                        if (AttackManager.Instance.CanAttackTarget(CurrentUnitsList[i].AttackMgr, TargetObj) == true)
                                        {
                                            //get a valid position for the unit to move to
                                            while (Positions.Count == 0)
                                            {
                                                Positions = CircleFormation(TargetObj.transform.position, CircleRadius, CurrentUnitsList[i]);
                                                CircleRadius += CurrentUnitsList[i].NavAgent.radius * 2f;
                                            }

                                            //Make the units attack the building/unit
                                            CurrentUnitsList[i].AttackMgr.SetAttackTargetLocal(TargetObj);

                                            //at least one of the selected units acceped the attack command
                                            MainAttackUnit = CurrentUnitsList[0]; //this unit will play the attack order audio clip

                                            if (Vector3.Distance(TargetObj.transform.position, CurrentUnitsList[i].transform.position) > AttackStoppingDistance)
                                            {
                                                int ID = GetClosestPosID(Positions, CurrentUnitsList[i].transform.position);
                                                CurrentUnitsList[i].CheckUnitPathLocal(Positions[ID], TargetObj, TargetObj.transform.position, MvtStoppingDistance, InputTargetMode.Attack);

                                                Positions.RemoveAt(ID);
                                            }
                                            else
                                            {
                                                CurrentUnitsList[i].CheckUnitPathLocal(CurrentUnitsList[i].transform.position, TargetObj, TargetObj.transform.position, MvtStoppingDistance, InputTargetMode.Attack);
                                            }

                                            CurrentUnitsList[i].TargetPosColl.isTrigger = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }


                if (MainAttackUnit)
                {
                    if (MainAttackUnit.FactionID == GameManager.PlayerFactionID && (AttackMode == AttackModes.Full || AttackMode == AttackModes.Assigned))
                    {
                        SelectionMgr.FlashSelection(TargetObj, false);
                        AudioManager.PlayAudio(GameMgr.GeneralAudioSource.gameObject, MainAttackUnit.AttackMgr.AttackOrderSound, false);
                    }
                }
                else
                {
                    if(UnitsList[0].FactionID == GameManager.PlayerFactionID)
                        UIMgr.ShowPlayerMessage("Can't attack with selected unit(s)!", UIManager.MessageTypes.Error);
                }
            }
        }

        public List<Vector3> CircleFormation(Vector3 Origin, float CircleRadius, Unit RefUnit)
        {
            List<Vector3> UnitPositions = new List<Vector3>();

            float Perimeter = 2 * Mathf.PI * CircleRadius;
            int UnitAmount = Mathf.FloorToInt(Perimeter / (RefUnit.NavAgent.radius * 2f));
            float Angle = 360f / UnitAmount;

            float CurrentAngle = 0.0f;

            int i = 0;

            while (i < UnitAmount)
            {
                CurrentAngle += Angle;

                float X = Origin.x + CircleRadius * Mathf.Sin(Mathf.Deg2Rad * CurrentAngle);
                float Z = Origin.z + CircleRadius * Mathf.Cos(Mathf.Deg2Rad * CurrentAngle);

                Vector3 ResultPos = new Vector3(X, TerrainMgr.SampleHeight(new Vector3(X, Origin.y, Z)), Z);
                Vector3 MvtPos = Vector3.zero;

                //only if the position can be reached by the unit:
                if (IsDestinationClear(ResultPos, RefUnit, out MvtPos))
                {
                    UnitPositions.Add(MvtPos);
                }

                i++;
            }
            return UnitPositions;
        }

        //check if a destination is clear for the unit to move to
        public bool IsDestinationClear(Vector3 Destination, Unit RefUnit, out Vector3 MvtPos)
        {
            MvtPos = Vector3.zero;

            //check if there
            Collider[] UnitsInRange = Physics.OverlapSphere(Destination, RefUnit.NavAgent.radius, (RefUnit.FlyingUnit == false) ? GroundUnitLayerMask : AirUnitLayerMask);

            NavMeshHit Hit;

            if(NavMesh.SamplePosition(Destination, out Hit, RefUnit.NavAgent.radius, RefUnit.NavAgent.areaMask) && UnitsInRange.Length == 0)
            {
                MvtPos = Hit.position;
                return true;
            }

            return false;
        }

        //a method to get the closest position inside a list to another position
        public int GetClosestPosID (List<Vector3> Positions, Vector3 RefPos)
        {
            int ID = 0;
            float Distance = Vector3.Distance(Positions[0], RefPos);

            //if there's more than one position in the list
            if (Positions.Count > 1)
            {
                //go through all positions
                for (int i = 1; i < Positions.Count; i++)
                {
                    //if this is closer to the ref position
                    if (Vector3.Distance(Positions[i], RefPos) < Distance)
                    {
                        Distance = Vector3.Distance(Positions[i], RefPos);
                        ID = i;
                    }
                }
            }

            return ID;
        }

    }

}