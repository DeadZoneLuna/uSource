using UnityEngine;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Globalization;
using System;


//			ANGLES
//			X pitch +down/-up
//			Y yaw +left/-right
//			Z roll +right/-left
namespace Engine.Source
{
    public class EntInfo : MonoBehaviour
    {
        public List<string> Data;

		//public Vector3 Origin;
		public Quaternion Angles; // QAngle
		void OnDrawGizmos()
        {
            Gizmos.DrawCube(transform.position, Vector3.one / 5f);
		}

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(transform.position, Vector3.one / 5f);
        }

		public void Configure(List<String> Data)
        {

            this.Data = Data;

            String Classname = Data[Data.FindIndex(n => n == "classname") + 1], Targetname = Data[Data.FindIndex(n => n == "targetname") + 1];
			name = Classname;

			if (Data.Contains("origin"))
			{
				String[] Array = Data[Data.FindIndex(n => n == "origin") + 1].Split(' ');

				while (Array.Length != 3)
				{
					Int32 TempIndex = Data.FindIndex(n => n == "origin") + 1;
					Array = Data[Data.FindIndex(TempIndex, n => n == "origin") + 1].Split(' ');
				}

				transform.position = new Vector3(-Single.Parse(Array[0]), Single.Parse(Array[2]), -Single.Parse(Array[1])) * ConfigLoader.WorldScale;
			}

            //Angles aren't parse correctly, so there may be problems with the rotantion of the entity.
            if (Data.Contains("angles"))
			{
                string[] Array = Data[Data.FindIndex(n => n == "angles") + 1].Split(new char[] { ' ' });
                Vector3 EulerAngles = new Vector3(-float.Parse(Array[2]), -float.Parse(Array[1]), -float.Parse(Array[0]));

                if (Data.Contains("pitch"))
                    EulerAngles.x = -float.Parse(Data[Data.FindIndex(n => n == "pitch") + 1]);

                transform.eulerAngles = EulerAngles;
            }

			if (Classname.Contains("trigger"))
            {
                for (Int32 i = 0; i < transform.childCount; i++)
                {
                    GameObject Child = transform.GetChild(i).gameObject;
					Child.SetActive(false);
                    Child.AddComponent<BoxCollider>().isTrigger = true;
                }
            }

            if (Classname.Equals("point_viewcontrol"))
                gameObject.AddComponent<point_viewcontrol>();

            if (Classname.Equals("light_environment"))
            {
				//TODO
			}

			if(Classname.Equals("light_spot"))
			{
				//TODO
			}

			if (Data.Contains("rendermode"))
            {
                if (Data[Data.FindIndex(n => n == "rendermode") + 1] == "10")
                {
                    for (Int32 i = 0; i < transform.childCount; i++)
                    {
                        GameObject Child = transform.GetChild(i).gameObject;
                        Child.GetComponent<Renderer>().enabled = false;
                    }
                }
            }

            if(Classname.Contains("prop_") || Classname.Contains("npc_"))
            {
                string ModelName = Data[Data.FindIndex(n => n == "model") + 1];
                StudioMDLLoader.Load(ModelName).SetParent(transform, false);
            }

            if (Classname.Equals("infodecal"))
            {
				//This is just an example, we need a complete decal system!
				/*String TextureName = Data[Data.FindIndex(n => n == "texture") + 1];
                Material DecalMat = MaterialLoader.Load(TextureName);

                int x = DecalMat.mainTexture.width;
                int y = DecalMat.mainTexture.height;

                float DecalScale = 1;
                String Value = MaterialLoader.GetParametr("$decalscale");

                if (!String.IsNullOrEmpty(Value))
                    DecalScale = float.Parse(Value);

                SpriteRenderer DecalRender = gameObject.AddComponent<SpriteRenderer>();
				DecalRender.sprite = Sprite.Create((Texture2D)DecalMat.mainTexture, new Rect(0, 0, x, y), new Vector2(0.5f, 0.5f), 1);
				transform.localScale = new Vector3(Configuration.WorldScale * DecalScale, Configuration.WorldScale * DecalScale, Configuration.WorldScale * DecalScale);//, Configuration.WorldScale * 1);*/
			}
		}

	}

}
