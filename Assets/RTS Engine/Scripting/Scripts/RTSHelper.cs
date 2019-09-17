using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 
 * RTSHelper component created by Oussama Bouanani,  SoumiDelRio
 * This script is part of the RTS Engine
 * */

namespace RTSEngine
{
    public class RTSHelper
    {
        public static void ShuffleList<T>(List<T> inputList)
        {
            if(inputList.Count > 0) //make sure the list already has elements
            {
                //go through the elements of the list:
                for(int i = 0; i < inputList.Count; i++)
                {
                    int swapID = Random.Range(0, inputList.Count); //pick an element to swap with
                    if(swapID != i) //if this isn't the same element
                    {
                        //swap elements:
                        T tempElement = inputList[swapID];
                        inputList[swapID] = inputList[i];
                        inputList[i] = tempElement;
                    }
                }
            }
        }

        //Swap two items:
        public static void Swap<T>(ref T item1, ref T item2)
        {
            T temp = item1;
            item1 = item2;
            item2 = temp;
        }
    }
}

