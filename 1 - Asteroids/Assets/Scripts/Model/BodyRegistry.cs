using System.Collections.Generic;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The net ids of the bodies of a shared playfield. The simulator hands out the ids (<see cref="Register"/>, counted
    /// from 1 for every mission); the other clients bind the ids they are told to their puppets (<see cref="Bind"/>).
    /// Either way a body and its id are found through each other until the body leaves.
    /// </summary>
    public class BodyRegistry<T> where T : class
    {
        private readonly Dictionary<uint, T> bodies = new Dictionary<uint, T>();
        private readonly Dictionary<T, uint> ids = new Dictionary<T, uint>();
        private uint last;

        public int Count => bodies.Count;

        public IEnumerable<KeyValuePair<uint, T>> Entries => bodies;

        /// <summary>Gives <paramref name="body"/> the next id; the id it has already when it was registered before.</summary>
        public uint Register(T body)
        {
            if (body == null)
            {
                return 0;
            }
            if (ids.TryGetValue(body, out uint known))
            {
                return known;
            }
            last++;
            bodies[last] = body;
            ids[body] = last;
            return last;
        }

        /// <summary>Binds an id that came from the simulator. False when the id or the body is taken, or the id is zero.</summary>
        public bool Bind(uint id, T body)
        {
            if (id == 0 || body == null || bodies.ContainsKey(id) || ids.ContainsKey(body))
            {
                return false;
            }
            bodies[id] = body;
            ids[body] = id;
            return true;
        }

        public bool TryGet(uint id, out T body)
        {
            return bodies.TryGetValue(id, out body);
        }

        public bool TryGetId(T body, out uint id)
        {
            id = 0;
            return body != null && ids.TryGetValue(body, out id);
        }

        public bool Contains(uint id)
        {
            return bodies.ContainsKey(id);
        }

        /// <summary>Forgets <paramref name="body"/>; <paramref name="id"/> is the id it had.</summary>
        public bool Remove(T body, out uint id)
        {
            if (!TryGetId(body, out id))
            {
                return false;
            }
            ids.Remove(body);
            bodies.Remove(id);
            return true;
        }

        public bool Remove(uint id, out T body)
        {
            if (!bodies.TryGetValue(id, out body))
            {
                return false;
            }
            bodies.Remove(id);
            ids.Remove(body);
            return true;
        }

        /// <summary>Forgets every body and starts counting from 1 again: another mission.</summary>
        public void Clear()
        {
            bodies.Clear();
            ids.Clear();
            last = 0;
        }
    }
}
