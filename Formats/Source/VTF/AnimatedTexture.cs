using UnityEngine;
using System.Collections;

namespace uSource.Formats.Source.VTF
{
    public class AnimatedTexture : MonoBehaviour
    {
        public float AnimatedTextureFramerate;
        public Texture2D[] Frames;
        public MeshRenderer Renderer;
        int CFrame = 0;

        void Start()
        {
            if (Renderer == null)
                Renderer = GetComponent<MeshRenderer>();

            AnimatedTextureFramerate = 1f / AnimatedTextureFramerate;

            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            while (true)
            {
                if (CFrame == Frames.Length)
                    CFrame = 0;

                Renderer.sharedMaterial.mainTexture = Frames[CFrame];
                CFrame++;

                yield return new WaitForSeconds(AnimatedTextureFramerate);
            }
        }
    }
}
