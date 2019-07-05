using System.Collections.Generic;

namespace PocceMod
{
    public class DataSource<T>
    {
        private Queue<T> _queue = new Queue<T>();

        public void Push(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
            }
        }

        public void Push(IEnumerable<T> items)
        {
            lock (_queue)
            {
                foreach (var item in items)
                {
                    _queue.Enqueue(item);
                }
            }
        }

        public T[] Pull()
        {
            lock (_queue)
            {
                var results = new T[_queue.Count];
                _queue.CopyTo(results, 0);
                _queue.Clear();
                return results;
            }
        }
    }
}
