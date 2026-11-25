using UnityEngine;
using UnityEngine.U2D.Animation;
using VuonTra.Core;

namespace VuonTra.Game.Bunny
{
    /// <summary>
    /// One rabbit as a layered paper doll (owner's review 23 Nov): the BunnyCharacter prefab, built by
    /// VuonTra.EditorTools.BunnyBuilder, with a SortingGroup, an Animator and a SpriteLibrary. This only chooses what
    /// is shown and which state plays: the kind's parts, the hat, the charm and the trade's tool through
    /// SpriteResolvers, and the work, doze, squish and hop states through Animator parameters. Every movement lives in
    /// the animation clips; nothing here moves a transform.
    /// </summary>
    public sealed class BunnyFigure : MonoBehaviour
    {
        public const string BodyCategory = "Body", HeadCategory = "Head", EarsCategory = "Ears", ArmCategory = "Arm",
            PawCategory = "Paw", HatCategory = "Hat", CharmCategory = "Charm", PropCategory = "Prop", NoneLabel = "none";
        public static readonly int JobParam = Animator.StringToHash("Job"), DozeParam = Animator.StringToHash("Doze"),
            SquishParam = Animator.StringToHash("Squish"), HopParam = Animator.StringToHash("Hop");

        [SerializeField] Animator _animator;
        [SerializeField] SpriteLibrary _library;
        [SerializeField] SpriteResolver _body, _head, _ears, _arm, _paw, _offPaw, _hat, _hatOnBody, _charm, _prop;
        [Tooltip("Where hats and charms rest for a kind drawn on the master; a kind with a shape of its own is moved from these once, when chosen.")]
        [SerializeField] Vector3 _hatHome, _hatOnBodyHome, _charmHome;
        [SerializeField] SpriteRenderer[] _fur = new SpriteRenderer[0];
        [SerializeField] SpriteRenderer _hatRenderer, _earsRenderer;
        [Tooltip("Sorting orders: the ears stand behind the head, and come in front of a hat they pass through.")]
        [SerializeField] int _earsBehind = 2, _earsOver = 6;

        /// <summary>What it shows now, for tests and the station's own checks.</summary>
        public string Kind { get; private set; }
        public string Hat { get; private set; } = NoneLabel;
        public string Charm { get; private set; } = NoneLabel;
        public RabbitTrade Job { get; private set; }
        public bool Dozing { get; private set; }
        public Animator Animator => _animator;

        /// <summary>
        /// The kind's own body, head, ears and arm; a kind with a shape of its own (no head part yet) shows its whole
        /// drawing as the body, and its hat and charm are placed once on its own head and neck (from its head numbers)
        /// and stand still with the body, since its head does not nod apart.
        /// </summary>
        public void SetKind(string kindId)
        {
            Kind = kindId;
            RabbitKind kind = RabbitKinds.Find(kindId);
            _own = kind != null && kind.Shape == RabbitShape.Own;
            PlaceForKind(kind);
            Resolve(_body, BodyCategory, kindId);
            Resolve(_head, HeadCategory, kindId);
            Resolve(_ears, EarsCategory, kindId);
            Resolve(_arm, ArmCategory, kindId);
            Resolve(_paw, PawCategory, kindId);
            Resolve(_offPaw, PawCategory, kindId);
        }

        /// <summary>A hat by its item id, or none. <paramref name="earsOver"/>: the ears come through it (a knitted hat).</summary>
        public void SetHat(string itemId, bool earsOver)
        {
            Hat = Has(HatCategory, itemId) ? itemId : NoneLabel;
            Resolve(_hat, HatCategory, _own ? NoneLabel : Hat);
            Resolve(_hatOnBody, HatCategory, _own ? Hat : NoneLabel);
            if (_earsRenderer != null) _earsRenderer.sortingOrder = earsOver && Hat != NoneLabel ? _earsOver : _earsBehind;
        }

        /// <summary>A charm at the neck by its item id, or none.</summary>
        public void SetCharm(string itemId)
        {
            Charm = Has(CharmCategory, itemId) ? itemId : NoneLabel;
            Resolve(_charm, CharmCategory, Charm);
        }

        /// <summary>The trade it works at: the tool in its paw and the work state (Brew, Bake, Gather, Stargaze), or Idle for none.</summary>
        public void SetJob(RabbitTrade trade)
        {
            Job = trade;
            Resolve(_prop, PropCategory, trade == RabbitTrade.None ? NoneLabel : trade.ToString());
            if (_animator != null) _animator.SetInteger(JobParam, (int)trade);
        }

        public void SetDozing(bool dozing)
        {
            Dozing = dozing;
            if (_animator != null) _animator.SetBool(DozeParam, dozing);
        }

        /// <summary>A tap landed.</summary>
        public void Squish() { if (_animator != null) _animator.SetTrigger(SquishParam); }

        /// <summary>A cycle finished: a jump for joy.</summary>
        public void Hop() { if (_animator != null) _animator.SetTrigger(HopParam); }

        /// <summary>How fast the clips play: 1, 2 in a speed burst, 0 under reduced motion (the rabbit holds its pose).</summary>
        public void SetTempo(float tempo) { if (_animator != null) _animator.speed = tempo; }

        /// <summary>The coat: as drawn (white, no material), or dyed with the fur material and the dye's colour.</summary>
        public void SetFur(Color color, Material dye)
        {
            foreach (SpriteRenderer r in _fur)
            {
                if (r == null) continue;
                r.color = color;
                if (dye != null) r.sharedMaterial = dye;
                else if (_defaultMaterial != null) r.sharedMaterial = _defaultMaterial;
            }
        }

        [SerializeField] Material _defaultMaterial;
        bool _own;

        // The master's head on the 512 canvas: middle of the skull 150, top 86, width 140 drawing pixels (320 x 343),
        // drawn at 1.2 and moved by (64, 89); in units from the canvas's bottom middle at 100 pixels per unit.
        const float MasterX = 150f, MasterTop = 86f, MasterWidth = 140f, Scale = 1.2f, Ppu = 100f;

        /// <summary>A kind with a shape of its own: its hat and charm moved and sized from the master's head to its own. Placement, set once.</summary>
        void PlaceForKind(RabbitKind kind)
        {
            Transform hat = _hatOnBody != null ? _hatOnBody.transform : null, charm = _charm != null ? _charm.transform : null;
            if (kind == null || !_own)
            {
                if (hat != null) { hat.localPosition = _hatOnBodyHome; hat.localScale = Vector3.one; }
                if (charm != null) { charm.localPosition = _charmHome; charm.localScale = Vector3.one; }
                return;
            }
            float s = kind.HeadWidth * 343f / MasterWidth;
            // The master's crown, from the canvas's bottom middle, and this kind's.
            Vector3 crown = new Vector3((MasterX * Scale + 64f - 256f) / Ppu, (512f - (MasterTop * Scale + 89f)) / Ppu, 0f);
            Vector3 own = new Vector3((kind.HeadX * 320f * Scale + 64f - 256f) / Ppu, (512f - ((1f - kind.HeadTop) * 343f * Scale + 89f)) / Ppu, 0f);
            if (hat != null) { hat.localScale = new Vector3(s, s, 1f); hat.localPosition = _hatOnBodyHome + own - crown * s; }
            if (charm != null) { charm.localPosition = _charmHome + own - crown; charm.localScale = Vector3.one; }
        }

        bool Has(string category, string label)
        {
            return label != null && _library != null && _library.spriteLibraryAsset != null && _library.spriteLibraryAsset.GetSprite(category, label) != null;
        }

        void Resolve(SpriteResolver resolver, string category, string label)
        {
            if (resolver == null) return;
            string use = Has(category, label) ? label : NoneLabel;
            resolver.SetCategoryAndLabel(category, use);
            resolver.ResolveSpriteToSpriteRenderer();
        }
    }
}
