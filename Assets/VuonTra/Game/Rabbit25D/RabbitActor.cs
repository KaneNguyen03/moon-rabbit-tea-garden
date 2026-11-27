using UnityEngine;

namespace VuonTra.Game
{
    public enum RabbitView
    {
        Front,
        ThreeQuarterLeft,
        ThreeQuarterRight,
    }

    public enum RabbitWork
    {
        Idle,
        MilkTea,
        TeaPicking,
        TeaBrewing,
    }

    public sealed class RabbitActor : MonoBehaviour
    {
        public const string WorkParameter = "Work";
        static readonly int WorkId = Animator.StringToHash(WorkParameter);

        [SerializeField] Animator animator;
        [SerializeField] GameObject front;
        [SerializeField] GameObject threeQuarter;
        [SerializeField] GameObject shadow;
        [SerializeField] RabbitView view = RabbitView.Front;

        public RabbitView View => view;
        public Animator Animator => animator;
        public GameObject Shadow => shadow;

        void Awake() => SetView(view);

        public void SetView(RabbitView next)
        {
            view = next;
            front.SetActive(next == RabbitView.Front);
            threeQuarter.SetActive(next != RabbitView.Front);
            threeQuarter.transform.localScale = new Vector3(next == RabbitView.ThreeQuarterRight ? -1f : 1f, 1f, 1f);
        }

        public RabbitWork Work { get; private set; }

        public void SetWork(RabbitWork work)
        {
            Work = work;
            animator.SetInteger(WorkId, (int)work);
        }

        public Bounds Extent()
        {
            GameObject shown = view == RabbitView.Front ? front : threeQuarter;
            Bounds extent = default;
            bool any = false;
            foreach (SpriteRenderer part in shown.GetComponentsInChildren<SpriteRenderer>())
            {
                if (part.sprite == null) continue;
                if (any) extent.Encapsulate(part.bounds);
                else extent = part.bounds;
                any = true;
            }
            return extent;
        }
    }
}
