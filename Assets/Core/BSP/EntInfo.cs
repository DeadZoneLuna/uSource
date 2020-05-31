using UnityEngine;
using System.Collections.Generic;
//using System.IO;
using System.Linq;
using System.Globalization;
using System;
//using _Decal;

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

                transform.position = new Vector3(-Single.Parse(Array[0]), Single.Parse(Array[2]), -Single.Parse(Array[1])) * 0.0254f;
            }

            if (Data.Contains("angles"))
            {
                String[] Array = Data[Data.FindIndex(n => n == "angles") + 1].Split(' ');
                Vector3 EulerAngles = new Vector3(Single.Parse(Array[0]), -Single.Parse(Array[1]), -Single.Parse(Array[2]));

                if (Data.Contains("pitch"))
                    EulerAngles.x = -Single.Parse(Data[Data.FindIndex(n => n == "pitch") + 1]);

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

            //if (Classname.Equals("point_viewcontrol"))
            //    gameObject.AddComponent<point_viewcontrol>();

            //3D Skybox
            if (Classname.Equals("sky_camera"))
            {
                //Setup 3DSkybox
                Camera playerCamera = new GameObject("CameraPlayer").AddComponent<Camera>();
                CameraFly camFly = playerCamera.gameObject.AddComponent<CameraFly>();
                camFly.skyScale = float.Parse(Data[Data.FindIndex(n => n == "scale") + 1]);
                camFly.offset3DSky = transform.position;
                if (ConfigLoader.use3DSkybox)
                {
                    Camera skyCamera = gameObject.AddComponent<Camera>();
                    skyCamera.depth = 0f;
                    skyCamera.farClipPlane = 70f;

                    playerCamera.depth = 1f;
                    playerCamera.clearFlags = CameraClearFlags.Depth;
                    camFly.skyCamera = skyCamera.transform;
                }
                //Setup 3DSkybox
            }
            //3D Skybox

            if (Classname.Equals("light_environment"))
            {
                String[] Array = Data[Data.FindIndex(n => n == "_ambient") + 1].Split(' ');
                RenderSettings.ambientLight = new Color32(Byte.Parse(Array[0]), Byte.Parse(Array[1]), Byte.Parse(Array[2]), 255);

                //TODO
            }

            if (Classname.Equals("light_spot"))
            {
                //TODO
            }

            if (Classname.Equals("info_player_terrorist"))
            {
                StudioMDLLoader.Load("player/t_phoenix").SetParent(transform, false);
            }

            if (Classname.Equals("info_player_counterterrorist"))
            {
                StudioMDLLoader.Load("player/ct_urban").SetParent(transform, false);
            }

            if (Classname.Equals("info_player_start"))
            {
                StudioMDLLoader.Load("editor/playerstart").SetParent(transform, false);
            }

            if (Classname.Equals("hostage_entity"))//hostage_entity
            {
                String[] hostages = new[] { "characters/hostage_01", "characters/hostage_02", "characters/hostage_03", "characters/hostage_04" };
                StudioMDLLoader.Load(hostages[UnityEngine.Random.Range(0, hostages.Length)]).SetParent(transform, false);
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

            if (Classname.Contains("prop_") || Classname.Contains("npc_"))
            {
                string ModelName = Data[Data.FindIndex(n => n == "model") + 1];
                StudioMDLLoader.Load(ModelName).SetParent(transform, false);
            }

            if (Classname.Equals("infodecal"))
            {
                //This is just an example, you need to implement a complete decal system.
                if (ConfigLoader.LoadInfoDecals)
                {
                    String TextureName = Data[Data.FindIndex(n => n == "texture") + 1];
                    Material DecalMat = MaterialLoader.Load(TextureName);

                    int x = DecalMat.mainTexture.width;
                    int y = DecalMat.mainTexture.height;

                    float DecalScale = 1;
                    String Value = MaterialLoader.GetParametr("$decalscale");

                    if (!String.IsNullOrEmpty(Value))
                        DecalScale = float.Parse(Value);

                    SpriteRenderer DecalRender = gameObject.AddComponent<SpriteRenderer>();
                    DecalRender.sprite = Sprite.Create((Texture2D)DecalMat.mainTexture, new Rect(0, 0, x, y), new Vector2(0.5f, 0.5f), 1);

                    DecalRender.flipX = true;
                    DecalRender.flipY = true;

                    transform.localScale = new Vector3(ConfigLoader.WorldScale * DecalScale, ConfigLoader.WorldScale * DecalScale, 1);
                }
            }
        }
    }
}