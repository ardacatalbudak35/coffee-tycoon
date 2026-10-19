using System.Collections.Generic;
using UnityEngine;
namespace CafeLife {
 public sealed class CafeActivityArea:MonoBehaviour {
  static readonly List<CafeActivityArea> areas=new List<CafeActivityArea>();
  public CafeTarget target;public float floorY,radius=3.2f;
  void OnEnable(){areas.Add(this);}void OnDisable(){areas.Remove(this);}
  public static CafeTarget Nearby(CafeGame game){CafeTarget best=null;float closest=float.MaxValue;foreach(var area in areas){if(!area||!area.target||Mathf.Abs(game.player.transform.position.y-area.floorY)>1.1f)continue;Vector3 delta=area.transform.position-game.player.transform.position;delta.y=0;float distance=delta.magnitude;if(distance>area.radius||distance>=closest)continue;if(distance>1.5f&&Vector3.Dot(game.player.transform.forward,delta.normalized)<-.1f)continue;closest=distance;best=area.target;}return best;}
 }
}
