using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

public class CustomEventsExample : MonoBehaviour {

	//when the object is enabled, we want to let the Custom Events component know that we want to access an event and use it.
	void OnEnabled () 
	{
		//in this example, we want to make sure of the event that is called when a unit is created.
		CustomEvents.UnitCreated += OnUnitCreated; //here we CustomEvents.UnitCreated += NAME_OF_CALLBACK; 
		//(in this case the name of the callback is "OnUnitCreated", see below).
	}

	//when the object is disabled, we want to let the Custom Events component know that we no longer to access the event that we mentioned earlier.
	void OnDisabled ()
	{
		CustomEvents.UnitCreated -= OnUnitCreated; //here we CustomEvents.UnitCreated -= NAME_OF_CALLBACK;
	}
		
	//this is the callback that we mentioned above and it will be called each time a unit is created
	//to know which params we need to have in the callback, check the attributes of the Custom Events script ...
	//... the attributes are at the beginning of the script, search for the custom event that is being used (in this case "UnitCreated" ...
	//... then see the params defined in the delegate that has the same event handler as the custom event used ...
    //... in our case, the event handler of the "UnitCreated" is "UnitEventHandler" and we have only one paramater.
	void OnUnitCreated (Unit NewUnit) 
	{
		//for example we checked if the unit has the same faction ID as the local player.
		if (NewUnit.FactionID == GameManager.PlayerFactionID) {
			print ("Local player created a new Unit"); //print a message to the player and let him know that he created a new unit.
		}
	}
}
