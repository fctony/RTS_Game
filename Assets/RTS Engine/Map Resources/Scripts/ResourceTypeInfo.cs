using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSEngine;

[CreateAssetMenu(fileName = "NewResourceType", menuName = "RTS Engine/Resource Type", order = 2)]
public class ResourceTypeInfo : ScriptableObject {

    public string Name; //Resrouce name

    public int StartingAmount; //The amount that each team will start with.

    public Sprite Icon; //Resource Icon.

    public Color MinimapIconColor; //the color of the minimap's icon for this resource

    //Audio clips:
    public AudioClip SelectionAudio; //Audio played when the player selects this resource.
    public List<AudioClip> CollectionAudio = new List<AudioClip>(); //Audio played each time the unit collects some of this resource.
}
