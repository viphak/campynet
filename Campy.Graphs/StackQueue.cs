using System.Collections.Generic;

namespace Campy.Graphs
{
    /// <summary>
    /// StackQueue - a data structure that has both Stack and Queue interfaces.
    /// </summary>
    /// <typeparam name="NAME"></typeparam>
    public class StackQueue<T>
    {
        private List<T> items = new List<T>();

        public StackQueue()
        {
        }

        public StackQueue(T value)
        {
            items.Add(value);
        }

        public int Size()
        {
            return items.Count;
        }

        public int Count
        {
            get { return items.Count; }
        }

        public T Pop()
        {
            if (items.Count > 0)
            {
                int index = items.Count - 1;
                T cur = items[index];
                items.RemoveAt(index);
                return cur;
            }
            else
            {
                return default(T);
            }
        }

        public T Top()
        {
            if (items.Count > 0)
            {
                int index = items.Count - 1;
                T cur = items[index];
                return cur;
            }
            else
            {
                return default(T);
            }
        }

        public T Peek(int n)
        {
            if (n >= items.Count)
                return default(T);
            T cur = items[n];
            return cur;
        }

        public void Push(T value)
        {
            items.Add(value);
        }

        public void Push(IEnumerable<T> collection)
        {
            items.AddRange(collection);
        }

        public void PushMultiple(params T[] values)
        {
            int count = values.Length;
            for (int i = 0; i < count; i++)
            {
                Push(values[i]);
            }
        }

        public void EnqueueTop(T value)
        {
            // Same as "Push(value)".
            Push(value);
        }

        public void EnqueueBottom(T value)
        {
            // "Push" a value on the bottom of the stack.
            items.Insert(0, value);
        }

        public T DequeueTop()
        {
            // Same as "Pop()".
            return Pop();
        }

        public T DequeueBottom()
        {
            // Remove item from bottom of stack.
            if (items.Count > 0)
            {
                T cur = items[0];
                items.RemoveAt(0);
                return cur;
            }
            else
            {
                return default(T);
            }
        }

        public bool Contains(T item)
        {
            return items.Contains(item);
        }

        public System.Collections.Generic.IEnumerator<T> GetEnumerator()
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                yield return items[i];
            }
        }
    }
}
