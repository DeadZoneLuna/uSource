using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObjectInfo : MonoBehaviour 
{
	//public System.Text.StringBuilder info;
	[TextArea(0, 50)]
	public string infoOutput;
	public int size = 8;

#if UNITY_EDITOR
	public void OnDrawGizmosSelected()
	{
        var style = new GUIStyle { fontSize = size, fontStyle = FontStyle.Bold };
		style.normal.textColor = Handles.yAxisColor;
		Handles.Label(transform.position + Vector3.up * 2, infoOutput.ToString(), style);
    }
#endif
}
