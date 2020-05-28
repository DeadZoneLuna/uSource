using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MDLArmatureInfo : MonoBehaviour {

	public GameObject ModelObject;
	public Transform rootNode;
	public Transform[] childNodes;
	//private TextGizmo TG;

	public void OnDrawGizmosSelected()
	{
		if (ModelObject != null)
		{
			for (int i = 0; i < childNodes.Length; i++)
			{
				Gizmos.color = Color.white;
				Gizmos.DrawWireCube(childNodes[i].position, Vector3.one * 0.02f);
				if (childNodes[i].parent != null)
				{
					Gizmos.color = Color.yellow;
					if(ModelObject == GetComponent<SkinnedMeshRenderer>())
						Handles.Label(childNodes[i].position, childNodes[i].name);
					else
						Handles.Label(transform.position, name);

					Gizmos.DrawLine(childNodes[i].position, childNodes[i].parent.position);
				}
			}
		}
	}

	public void PopulateArmature()
	{
		childNodes = rootNode.GetComponentsInChildren<Transform>();
	}
}
