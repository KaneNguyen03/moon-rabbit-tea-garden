using System;
using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    [CreateAssetMenu(menuName = "VuonTra/Rabbit Personalities", fileName = "Personalities")]
    public sealed class RabbitPersonalities : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string Kind;
            public Personality Temper;
            [Tooltip("The id of the brewing station this kind loves; a first stroke of the day there brings extra bond points.")]
            public string Favourite;
        }

        public Entry[] Kinds = new Entry[0];

        public PersonalityBook ToBook()
        {
            var likings = new List<Liking>(Kinds.Length);
            foreach (Entry e in Kinds)
                if (e != null) likings.Add(new Liking { Kind = e.Kind, Personality = e.Temper, Favourite = e.Favourite });
            return new PersonalityBook(likings);
        }
    }
}
