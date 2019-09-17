using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine
{
    [System.Serializable]
    public abstract class Range<T>
    {
        //max and min values:
        public T min;
        public T max;

        protected Range(T min, T max)
        {
            this.min = min;
            this.max = max;
        }

        //get a random value between min and maxs
        public abstract T getRandomValue();
    }

    [System.Serializable]
    public class FloatRange : Range<float>
    {
        public FloatRange(float min, float max) : base(min, max) { }

        //get a random float value between min and maxss
        public override float getRandomValue()
        {
            return Random.Range(min, max);
        }
    }

    [System.Serializable]
    public class IntRange : Range<int>
    {
        public IntRange(int min, int max) : base(min, max) { }

        //get a random float value between min and maxss
        public override int getRandomValue()
        {
            return Random.Range(min, max);
        }
    }
}