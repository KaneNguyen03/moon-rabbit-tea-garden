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
        [SerializeField] RabbitView view = RabbitView.Front;

        public RabbitView View => view;

        void Awake() => SetView(view);

        public void SetView(RabbitView next)
        {
            view = next;
            front.SetActive(next == RabbitView.Front);
            threeQuarter.SetActive(next != RabbitView.Front);
            threeQuarter.transform.localScale = new Vector3(next == RabbitView.ThreeQuarterRight ? -1f : 1f, 1f, 1f);
        }

        public void SetWork(RabbitWork work) => animator.SetInteger(WorkId, (int)work);
    }
}
