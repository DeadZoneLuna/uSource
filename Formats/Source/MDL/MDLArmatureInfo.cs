using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using uSource.Formats.Source.MDL;

namespace uSource
{
	public class MDLArmatureInfo : MonoBehaviour
	{
		//public MdlSpec.AniInfo[] aniDescs;
		public StudioStruct.SeqInfo[] seqDescs;
		public GameObject ModelObject;
		public Transform rootNode;
		public Transform[] boneNodes;
		public Transform[] attachmentNodes;
		//float size = 0.1f;
		//private TextGizmo TG;
#if UNITY_EDITOR
		public void OnDrawGizmosSelected()
		{
			for (int i = 0; i < boneNodes.Length; i++)
			{
				Gizmos.color = Color.white;
				Gizmos.DrawWireCube(boneNodes[i].position, Vector3.one * 0.025f);

				if (boneNodes[i].parent != null)
				{
					Gizmos.color = Color.yellow;
					//Handles.Label(boneNodes[i].position, boneNodes[i].name);
					Gizmos.DrawLine(boneNodes[i].position, boneNodes[i].parent.position);
				}

				/*Handles.color = Color.blue;
				Handles.DrawWireArc(targetObject.transform.position, Vector3.up, Vector3.forward, 360, targetObject.range);
				Handles.DrawWireArc(targetObject.transform.position, Vector3.forward, Vector3.up, 360, targetObject.range);
				Handles.DrawWireArc(targetObject.transform.position, Vector3.left, Vector3.up, 360, targetObject.range);


				Handles.color = Color.yellow;
				Handles.DrawWireArc(targetObject.transform.position, Vector3.up, Vector3.forward, 360, targetObject.m_MaxSize);
				Handles.DrawWireArc(targetObject.transform.position, Vector3.forward, Vector3.up, 360, targetObject.m_MaxSize);
				Handles.DrawWireArc(targetObject.transform.position, Vector3.left, Vector3.up, 360, targetObject.m_MaxSize);*/
			}

			/*for (int i = 0; i < attachmentNodes.Length; i++)
			{
				Handles.Label(attachmentNodes[i].position, attachmentNodes[i].name);

				Handles.color = Handles.xAxisColor;
				Handles.ArrowHandleCap(0, attachmentNodes[i].position, attachmentNodes[i].rotation * Quaternion.Euler(0, 90, 0), size, EventType.Repaint);

				Handles.color = Handles.yAxisColor;
				Handles.ArrowHandleCap(0, attachmentNodes[i].position, attachmentNodes[i].rotation * Quaternion.Euler(-90, 0, 0), size, EventType.Repaint);

				Handles.color = Handles.zAxisColor;
				Handles.ArrowHandleCap(0, attachmentNodes[i].position, attachmentNodes[i].rotation, size, EventType.Repaint);

				Handles.color = new Color32(255, 0, 0, 50);
				Handles.CubeHandleCap(0, attachmentNodes[i].position, attachmentNodes[i].rotation, size, EventType.Repaint);
				//Gizmos.DrawWireCube(attachmentNodes[i].position, Vector3.one * 0.05f);
			}*/
		}
#endif
	}
}