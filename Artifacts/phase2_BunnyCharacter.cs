using UnityEngine;
using UnityEngine.U2D.Animation;

public enum BunnyJob
{
    Idle = 0,
    Brew = 1,
}

[RequireComponent(typeof(Animator))]
public sealed class BunnyCharacter : MonoBehaviour
{
    public const string HatCategory = "Hat";
    public const string ArmCategory = "NearArm_Job";
    public const string NoHat = "";

    static readonly int JobParameter = Animator.StringToHash("Job");

    [SerializeField] Animator animator;
    [SerializeField] SpriteLibrary library;
    [SerializeField] SpriteResolver armResolver;
    [SerializeField] SpriteResolver hatResolver;
    [SerializeField] SpriteRenderer hatRenderer;

    public BunnyJob Job { get; private set; }

    public void SetJob(BunnyJob job)
    {
        Job = job;
        armResolver.SetCategoryAndLabel(ArmCategory, ArmLabel(job));
        animator.SetInteger(JobParameter, (int)job);
    }

    public void SetHat(string label)
    {
        bool wearing = !string.IsNullOrEmpty(label);
        hatRenderer.enabled = wearing;
        if (wearing)
            hatResolver.SetCategoryAndLabel(HatCategory, label);
    }

    public void SetBreed(SpriteLibraryAsset breed)
    {
        library.spriteLibraryAsset = breed;
        SetJob(Job);
    }

    public static string ArmLabel(BunnyJob job) => job == BunnyJob.Brew ? "ArmBrew" : "ArmIdle";
}
