using System.Collections.Generic;

namespace YoutubePlaylistDownloader.Objects
{
    class FixedQueue<T> : Queue<T>
    {
        private readonly int size;

        public FixedQueue(int size) : base()
        {
            this.size = size;
        }

        public new void Enqueue(T item)
        {
            while (base.Count >= size)
                base.Dequeue();

            base.Enqueue(item);
        }

    }
}
