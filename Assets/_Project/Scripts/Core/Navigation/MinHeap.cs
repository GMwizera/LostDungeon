using System;
using System.Collections.Generic;

namespace LostDungeon.Core.Navigation
{
    /// <summary>
    /// Binary min-heap: always hands back the item with the lowest priority in O(log n).
    /// A* uses it to pick the most promising cell next. Equal priorities come out in insertion order,
    /// so paths are the same every run.
    /// </summary>
    public class MinHeap<T>
    {
        private readonly List<(T item, int priority, long order)> nodes = new List<(T, int, long)>();
        private long insertCounter;

        public int Count => nodes.Count;

        public void Push(T item, int priority)
        {
            nodes.Add((item, priority, insertCounter++));
            SiftUp(nodes.Count - 1);
        }

        public T Pop()
        {
            if (nodes.Count == 0)
                throw new InvalidOperationException("Heap is empty.");

            T top = nodes[0].item;
            int last = nodes.Count - 1;
            nodes[0] = nodes[last];
            nodes.RemoveAt(last);
            if (nodes.Count > 0)
                SiftDown(0);
            return top;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!Less(index, parent))
                    break;
                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;
                int smallest = index;

                if (left < nodes.Count && Less(left, smallest)) smallest = left;
                if (right < nodes.Count && Less(right, smallest)) smallest = right;
                if (smallest == index)
                    return;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private bool Less(int a, int b) =>
            nodes[a].priority < nodes[b].priority ||
            (nodes[a].priority == nodes[b].priority && nodes[a].order < nodes[b].order);

        private void Swap(int a, int b) => (nodes[a], nodes[b]) = (nodes[b], nodes[a]);
    }
}
