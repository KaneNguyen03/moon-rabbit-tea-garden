using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class ShownText
    {
        object _owner;
        string _code, _word;
        long _first, _second, _third;

        public bool Changes(object owner, long first, long second = 0L, string word = null, long third = 0L)
        {
            string code = L10n.Code;
            if (ReferenceEquals(owner, _owner) && first == _first && second == _second && third == _third && string.Equals(word, _word) && string.Equals(code, _code)) return false;
            _owner = owner;
            _first = first;
            _second = second;
            _third = third;
            _word = word;
            _code = code;
            return true;
        }

        public void Forget()
        {
            _owner = null;
        }

        public void Number(Text text, long value)
        {
            if (Changes(text, value)) text.text = value.ToString();
        }

        public void Short(Text text, BigNum value)
        {
            if (Changes(text, value.ShortKey(), 0L, "short")) text.text = value.Short();
        }

        public void Line(Text text, string key)
        {
            if (Changes(text, 0L, 0L, key)) text.text = L10n.T(key);
        }

        public void Line(Text text, string key, long value)
        {
            if (Changes(text, value, 0L, key)) text.text = L10n.T(key, value);
        }

        public void Line(Text text, string key, long first, long second)
        {
            if (Changes(text, first, second, key)) text.text = L10n.T(key, first, second);
        }
    }
}
