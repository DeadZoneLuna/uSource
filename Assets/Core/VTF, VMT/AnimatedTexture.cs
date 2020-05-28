using UnityEngine;
using System.Collections;

namespace Engine.Source
{
    public class AnimatedTexture : MonoBehaviour
    {
        public float AnimatedTextureFramerate;
        public Texture2D[] Frames;

        int CFrame = 0;

        void Start()
        {
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            MeshRenderer Renderer = GetComponent<MeshRenderer>();

            while (true)
            {
                if (CFrame == Frames.Length)
                    CFrame = 0;

                Renderer.sharedMaterial.mainTexture = Frames[CFrame];
                CFrame++;

                yield return new WaitForSeconds(1f / AnimatedTextureFramerate);
            }
        }
    }
}
