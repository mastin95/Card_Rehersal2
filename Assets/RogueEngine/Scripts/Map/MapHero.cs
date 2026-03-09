using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogueEngine
{

    public class MapHero : MonoBehaviour
    {
        void Start()
        {
            
        }

        void Update()
        {
            GameManager client = GameManager.Get();
            World world = client.GetWorld();
            if (world != null)
            {
                MapLocationDot location = MapLocationDot.Get(world.map_location_id);
                if (location != null)
                {
                    transform.position = location.transform.position;
                }
            }
        }
    }
}
