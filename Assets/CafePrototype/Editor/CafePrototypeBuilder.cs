using UnityEditor;

// Compatibility for the menu used in the original prototype.
public static class CafePrototypeBuilder
{
    [MenuItem("Cafe Prototype/Create Demo Scene")]
    public static void CreateDemoScene() => CafeLife.CafeLifeBuilder.Build();
}
