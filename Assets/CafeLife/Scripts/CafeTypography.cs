using UnityEngine;
namespace CafeLife {
 public static class CafeTypography {
  static Font regular,strong;
  public static Font Regular=>regular?regular:regular=Resources.Load<Font>("CafeTypography/CafeSans-Regular");
  public static Font Strong=>strong?strong:strong=Resources.Load<Font>("CafeTypography/CafeSans-Semibold");
 }
}
