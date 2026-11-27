using UnityEngine;
using UnityEngine.U2D.Animation;

[CreateAssetMenu(menuName = "Vuon Tra/Bunny Breed")]
public sealed class BunnyBreed : ScriptableObject
{
    [SerializeField] string displayName;
    [SerializeField] SpriteLibraryAsset library;
    [SerializeField] bool lopEars;

    public string DisplayName => displayName;
    public SpriteLibraryAsset Library => library;
    public bool LopEars => lopEars;

    public void Configure(string name, SpriteLibraryAsset spriteLibrary, bool lop)
    {
        displayName = name;
        library = spriteLibrary;
        lopEars = lop;
    }
}
