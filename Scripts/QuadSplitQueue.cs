using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetaryTerrain
{
    public class QuadSplitQueue
    {
        public List<Quad> queue;
        private SortingClass sortingClass;
        private Planet planet;

        public bool stop; // can be set true if quadSplitQueue should stop splitting quads

        //returns true when quadSplitQueue is currently not splitting quads, i.e. zero to a couple frames after stop is set to true
        public bool isAnyCurrentlySplitting
        {
            get
            {
                for (int i = 0; (i < planet.quadsSplittingSimultaneously && queue.Count > i); i++)
                    if (queue[i].isSplitting)
                        return true;
                return false;
            }
        }

        public bool Update()
        {
            //Quad Split Queue
            if (queue.Count > 0 && !stop) //Check if quads are in the queue
            {
                //Sorting quadSplitQueue based on quads level and distance to the camera. Quads of lowest distance to the camera and level are split first.
                if (queue.Count > planet.quadsSplittingSimultaneously)
                    queue.Sort(planet.quadsSplittingSimultaneously - 1, queue.Count - planet.quadsSplittingSimultaneously, sortingClass);

                for (int i = 0; i < planet.quadsSplittingSimultaneously; i++)
                {
                    if (queue.Count > i)
                    {
                        if (queue[i] == null)
                            queue.RemoveAt(i);

                        if (!queue[i].isSplitting && queue[i].coroutine == null && !queue[i].hasSplit)
                            queue[i].coroutine = planet.StartCoroutine(queue[i].Split());

                        if (queue[i].hasSplit) //Wait until quad has split, then spot is freed
                        { 
                            queue[i].inSplitQueue = false;
                            queue.RemoveAt(i);
                        }
                    }
                    else break;
                }
                return true;
            }
            return false;
        }

        internal void Add(Quad q)
        {
            if (!queue.Contains(q) && !q.hasSplit)
            {
                queue.Add(q);
                q.inSplitQueue = true;
            }
        }

        internal void Remove(Quad q)
        {
            if (queue.Contains(q) && !q.isSplitting)
            {
                queue.Remove(q);
                q.inSplitQueue = false;
            }
        }

        public QuadSplitQueue(Planet planet)
        {
            this.planet = planet;
            queue = new List<Quad>();
            sortingClass = new SortingClass();
        }

        private class SortingClass : IComparer<Quad>
        {
            public int Compare(Quad x, Quad y)
            {
                if (x.level > y.level)
                    return 1;
                if (x.distance > y.distance && x.level == y.level)
                    return 1;

                return -1;
            }
        }
    }
}
