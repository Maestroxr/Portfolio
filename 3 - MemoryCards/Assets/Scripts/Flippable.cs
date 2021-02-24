using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio
{
    public enum FlippableState { Hidden, Flipped, Matched }
    /// <summary>
    /// Class used for flippable objects. It can change state and activate sounds/animations.
    /// </summary>
    public class Flippable : MonoBehaviour
    {
        public int FlippableTypeIndex => flippableTypeIndex;
        public FlippableState State { get; private set; }

        [SerializeField] private int flippableTypeIndex;
        [SerializeField] public Image FlippedImage;
        [SerializeField] private Sprite BackSide;
        [SerializeField] private AudioSource Audio;
        [SerializeField] private AudioClip Flipped, Matched;
        private Sprite TypeTexture;
        private bool rotating = false;


        public void SetType(int typeIndex, Sprite texture)
        {
            flippableTypeIndex = typeIndex;
            TypeTexture = texture;
        }


        public void SetState(FlippableState state)
        {
            switch (state)
            {
                case FlippableState.Flipped:
                    FlippedImage.sprite = TypeTexture;
                    Audio.clip = Flipped;
                    Audio.Play();
                    break;
                case FlippableState.Hidden:
                    FlippedImage.sprite = BackSide;
                    FlippedImage.color = Color.white;
                    transform.rotation = Quaternion.identity;
                    break;
                case FlippableState.Matched:
                    FlippedImage.color = Color.green;
                    FlippedImage.sprite = TypeTexture;
                    Audio.clip = Matched;
                    Audio.Play();
                    StartCoroutine(AnimateMatch());
                    break;
            }
            State = state;
        }
        

        public IEnumerator AnimateWin()
        {
            StartCoroutine(Rotate(new Vector3(0, 0, 180), 1));
            while (rotating) yield return null;
            StartCoroutine(Rotate(new Vector3(0, 0, -180), 1));
            while (rotating) yield return null;
        }


        public IEnumerator AnimateLose()
        {
            StartCoroutine(Rotate(new Vector3(90, 0, 0), 2));
            while (rotating) yield return null;
        }


        public IEnumerator AnimateMatch()
        {
            StartCoroutine(Rotate(new Vector3(0, 90, 0), 1));
            while (rotating) yield return null;
            StartCoroutine(Rotate(new Vector3(0, -90, 0), 1));
            while (rotating) yield return null;
        }


        private IEnumerator Rotate(Vector3 angles, float duration)
        {
            rotating = true;
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(angles) * startRotation;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, t / duration);
                yield return null;
            }
            transform.rotation = endRotation;
            rotating = false;
        }


        public void PlayWin()
        {
            StartCoroutine(AnimateWin());
        }


        public void PlayLose()
        {
            StartCoroutine(AnimateLose());
        }
    }
}