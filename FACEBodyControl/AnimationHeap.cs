using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FACEBodyControl
{
    public struct HeapElement
    {
        public TimeSpan Due;
        public Task Task;
    }

    /** 
     * Heap implementation, it is used to define a shared timer which is responsible 
     * for orchestrating the animation. This class is considered to be private to the library. 
     */
    public class AnimationHeap
    {
        private List<HeapElement> data;
        public List<HeapElement> Data
        {
            get { return data; }
        }

        // Current position of the heap index.
        private int pos = 0;

        /// <summary>
        /// Create a new AnimationHeap structure
        /// </summary>
        public AnimationHeap()
        {
            data = new List<HeapElement>();
        }

        /// <summary>
        /// Function to compute the left node of the tree. 
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        private int left(int idx)
        {
            return (2 * (idx + 1) - 1);
        }

        // Function to compute the right node of the tree.
        private int right(int idx)
        {
            return (2 * (idx + 1));
        }

        // Function to compute the parent node of a node.
        private int parent(int idx)
        {
            return (int)(Math.Floor((double)(((idx + 1) / 2) - 1)));
        }

        // Function to swap the content of two nodes.
        private void swap(int a, int b)
        {
            /*
            var tmp = data[a];
            //data[a] = data[b];
            data.Insert(a, data.ElementAt(b));
            //data[b] = tmp;
            data.Insert(b, tmp);
            */
            var tmp = data[a];
            data.Insert(a, data.ElementAt(b));
            data.RemoveAt(a + 1);
            data.Insert(b, tmp);
            data.RemoveAt(b + 1);
        }

        // Function to percolate up a node in the heap.
        internal void percolateUp(int p)
        {
            var par = parent(p);
            //while ((p > 0) && (data[p].Due < data[par].Due))
            while ((p > 0) && (data[p].Due.TotalMilliseconds < data[par].Due.TotalMilliseconds))
            {
                swap(p, par);
                p = par;
                par = parent(p);
            }
        }

        // Function to percolate down a node in the heap.
        internal void percolateDown(int p)
        {
            int l = left(p), r = right(p);
            if (l < pos)
            {
                if (r < pos)
                {
                    if (data[l].Due.TotalMilliseconds > data[r].Due.TotalMilliseconds)
                    {
                        swap(p, r);
                        percolateDown(r);
                    }
                    else
                    {
                        swap(p, l);
                        percolateDown(l);
                    }
                }
                else
                {
                    swap(p, l);
                    percolateDown(l);
                }
            }
            else if (p != pos - 1)
            {
                swap(p, pos - 1);
                percolateUp(p);
            }
        }

        /**
         * The number of elements contained in the heap.
         * @type int
         */
        public int Count() { return pos; }

        /**
         * Insert a Task into the heap.
         * @param {AnimeTask} el Task to be inserted.
         */
        public void Insert(HeapElement el)
        {
            //data[pos] = el;
            data.Insert(pos, el);
            percolateUp(pos++);
        }

        /**
         * Read the top of the heap. If the heap is empty null is returned.
         * @type AnimeTask
         */
        internal HeapElement Top()
        {
            if (pos != 0)
                return data[0];
            return new HeapElement(); // Check
        }

        /**
         * Remove the top element from the heap and returns it. If the heap
         * is empty null is returned and the heap is left unchanged.
         * @type AnimeTask
         */
        public HeapElement Remove()
        {
            var ret = new HeapElement();
            if (pos == 0) return ret;
            ret = data[0];

            percolateDown(0);
            //data[pos--] = new HeapElement();
            //data.Insert(pos--, new HeapElement());
            pos--;
            data.RemoveAt(pos);

            return ret;
        }

        /**
         * Remove a specific task from the heap.
         * @param {AnimeTask} t The task to remove. 
         * @type AnimeTask
         */
        internal TimeSpan RemoveTask(Task t)
        {
            //long ret = -1;
            TimeSpan ret = new TimeSpan(); // check: long or DateTime ?????
            for (int i = 0; i < pos; i++)
            {
                if (data[i].Task == t)
                {
                    ret = data[i].Due;
                    percolateDown(i);
                    //data[pos--] = new HeapElement();
                    data.Insert(pos--, new HeapElement());
                    break;
                }
            }
            return ret;
        }

        /**
         * Debug function to convert the heap into a string.
         * @private
         * @type String
         */
        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < pos; i++)
                ret += " <i>" + i + ":</i> " + data[i].Due;
            return ret;
        }
    }

}