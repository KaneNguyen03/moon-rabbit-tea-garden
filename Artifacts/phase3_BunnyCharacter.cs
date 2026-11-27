using UnityEngine;
using UnityEngine.U2D.Animation;

public enum BunnyJob
{
    Idle = 0,
    Brew = 1,
    Bake = 2,
    Gather = 3,
    Stargaze = 4,
    Doze = 5,
}

[RequireComponent(typeof(Animator))]
public sealed class BunnyCharacter : MonoBehaviour
{
    public const string HatCategory = "Hat";
    public const string OutfitCategory = "Outfit";
    public const string ArmCategory = "NearArm_Job";
    public const string HeadCategory = "Head";
    public const string Nothing = "";

    static readonly int JobParameter = Animator.StringToHash("Job");

    [SerializeField] Animator animator;
    [SerializeField] SpriteLibrary library;
    [SerializeField] SpriteResolver armResolver;
    [SerializeField] SpriteResolver headResolver;
    [SerializeField] SpriteResolver hatResolver;
    [SerializeField] SpriteResolver outfitResolver;
    [SerializeField] SpriteRenderer earsBack;
    [SerializeField] SpriteRenderer earsLop;
    [SerializeField] BunnyBreed breed;

    public BunnyJob Job { get; private set; }
    public BunnyBreed Breed => breed;

    public void SetJob(BunnyJob job)
    {
        Job = job;
        armResolver.SetCategoryAndLabel(ArmCategory, ArmLabel(job));
        headResolver.SetCategoryAndLabel(HeadCategory, job == BunnyJob.Doze ? "HeadDoze" : "HeadAwake");
        animator.SetInteger(JobParameter, (int)job);
    }

    public void SetHat(string label) => Wear(hatResolver, HatCategory, label);

    public void SetOutfit(string label) => Wear(outfitResolver, OutfitCategory, label);

    public void SetBreed(BunnyBreed next)
    {
        breed = next;
        library.spriteLibraryAsset = next.Library;
        earsBack.enabled = !next.LopEars;
        earsLop.enabled = next.LopEars;
        SetJob(Job);
    }

    public static string ArmLabel(BunnyJob job) => "Arm" + job;

    static void Wear(SpriteResolver resolver, string category, string label)
    {
        bool wearing = !string.IsNullOrEmpty(label);
        resolver.GetComponent<SpriteRenderer>().enabled = wearing;
        if (wearing)
            resolver.SetCategoryAndLabel(category, label);
    }
}
