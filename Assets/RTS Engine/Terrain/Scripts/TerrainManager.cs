using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Terrain Manager script created by Oussama Bouanani, SoumiDelRio.
 * This script is part of the Unity RTS Engine */

namespace RTSEngine
{
    public class TerrainManager : MonoBehaviour
    {
        public static TerrainManager Instance;

        public LayerMask GroundTerrainMask; //layers used for the ground terrain objects
        public GameObject FlatTerrain;

        //the map's approximate size (usually width*height).
        public float mapSize = 16900;

        void Awake ()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
            }
        }

        //get the height of the closest terrain tile
        public float SampleHeight (Vector3 Position)
        {
            float Height = 0.0f;
            float Distance = 0.0f;

            LayerMask TerrainLayerMask = GroundTerrainMask; //by default, we'll use the ground terrain mask
            RaycastHit[] Hits = Physics.RaycastAll(new Vector3(Position.x, Position.y+20.0f, Position.z), Vector3.down,50.0f, TerrainLayerMask);

            if(Hits.Length > 0)
            {
                Height = Hits[0].point.y;
                Distance = Vector3.Distance(Position, Hits[0].point);

                if (Hits.Length > 1)
                {
                    for (int i = 1; i < Hits.Length; i++)
                    {
                        if(Distance > Vector3.Distance(Hits[i].point, Position))
                        {
                            Height = Hits[i].point.y;
                            Distance = Vector3.Distance(Position, Hits[i].point);
                        }
                    }
                }
            }

            return Height;
        }

        //determine if an object belongs to the terrain tiles: (only regarding ground terrain objects)
        public bool IsTerrainTile (GameObject Obj)
        {
            return GroundTerrainMask == (GroundTerrainMask | (1 << Obj.layer));
        }
    }
}