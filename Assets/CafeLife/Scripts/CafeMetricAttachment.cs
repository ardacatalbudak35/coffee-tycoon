using UnityEngine;
namespace CafeLife {
 // Bone transforms may contain importer scales. Prop offsets and size stay in metres.
 public sealed class CafeMetricAttachment:MonoBehaviour {
  Transform anchor;Vector3 offset;Quaternion rotation;float size;
  public void Setup(Transform target,Vector3 metricOffset,Quaternion relativeRotation,float worldSize){anchor=target;offset=metricOffset;rotation=relativeRotation;size=worldSize;Apply();}
  public void Apply(){if(!anchor)return;transform.SetPositionAndRotation(anchor.position+anchor.rotation*offset,anchor.rotation*rotation);var parentScale=transform.parent?transform.parent.lossyScale:Vector3.one;transform.localScale=new Vector3(size/Mathf.Max(.00001f,Mathf.Abs(parentScale.x)),size/Mathf.Max(.00001f,Mathf.Abs(parentScale.y)),size/Mathf.Max(.00001f,Mathf.Abs(parentScale.z)));}
  void LateUpdate()=>Apply();
 }
}
