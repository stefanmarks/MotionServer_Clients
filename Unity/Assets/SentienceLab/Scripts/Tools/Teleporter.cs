#region Copyright Information
// Sentience Lab VR Framework
// (C) Sentience Lab (sentiencelab@aut.ac.nz), Auckland University of Technology, Auckland, New Zealand 
#endregion Copyright Information

using System;
using System.Collections.Generic;
using UnityEngine;
using SentienceLab.PostProcessing;

namespace SentienceLab
{
	/// <summary>
	/// Class for executing a teleport.
	/// </summary>

	[AddComponentMenu("Locomotion/Teleporter")]
	[DisallowMultipleComponent]

	public class Teleporter : MonoBehaviour
	{
		[Tooltip("Type of the teleport transition")]
		public TransitionType transitionType = TransitionType.MoveLinear;

		[Tooltip("Duration of the teleport transition in seconds")]
		public float transitionTime = 0.1f;

		[Tooltip("Can the user teleport up and down?")]
		public bool allowVerticalMovement = false;

		[Tooltip("Sound to play during teleport (optional)")]
		public AudioSource teleportSound;


		public enum TransitionType
		{
			MoveLinear,
			MoveSmooth,
			Fade,
			// Blink
		}


		void Start()
		{
			transition = null;
		}


		void Update()
		{
			if (transition != null)
			{
				transition.Update(this.transform);
				if (transition.IsFinished())
				{
					transition.Cleanup();
					transition = null;
				}
			}
		}


		/// <summary>
		/// Activates the Teleport function to a specific point.
		/// </summary>
		/// <param name="originPoint">the point to teleport from</param>
		/// <param name="targetPoint">the point to teleport to</param>
		/// 
		public void Activate(Vector3 originPoint, Vector3 targetPoint)
		{
			if (!IsReady()) return;

			if (teleportSound != null)
			{
				teleportSound.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
				teleportSound.Play();
			}

			// calculate offset to target point (relativ to floor level, not camera height)
			originPoint.y = this.transform.position.y;
			Vector3 offset = targetPoint - originPoint;
			if (!allowVerticalMovement)
			{
				offset.y = 0;
			}

			Vector3 startPoint = this.transform.position;
			Vector3 endPoint   = startPoint + offset;

			// activate transition
			switch (transitionType)
			{
				case TransitionType.Fade:
					transition = new Transition_Fade(endPoint, transitionTime, this.gameObject);
					break;

				case TransitionType.MoveLinear:
					transition = new Transition_Move(startPoint, endPoint, transitionTime, false);
					break;

				case TransitionType.MoveSmooth:
					transition = new Transition_Move(startPoint, endPoint, transitionTime, true);
					break;
			}
		}


		/// <summary>
		/// Checks if the Teleporter is ready.
		/// </summary>
		/// <returns><c>true</c> if teleport can be triggered</returns>
		/// 
		public bool IsReady()
		{
			return transition == null;
		}


		public void OnGUI()
		{
			if (transition != null)
			{
				transition.UpdateUI();
			}
		}


		private ITransition transition;


		private interface ITransition
		{
			void Update(Transform offsetObject);
			void UpdateUI();
			bool IsFinished();
			void Cleanup();
		}


		private class Transition_Fade : ITransition
		{
			public Transition_Fade(Vector3 endPoint, float duration, GameObject parent)
			{
				this.endPoint = endPoint;
				this.duration = duration;

				progress = 0;
				moved = false;

				// create fade effect
				fadeEffects = ScreenFade.AttachToAllCameras();
				foreach (ScreenFade fade in fadeEffects)
				{
					fade.FadeColour = Color.black;
				}
			}

			public void Update(Transform offsetObject)
			{
				// move immediately to B when fade is half way (all black)
				progress += Time.deltaTime / duration;
				progress  = Math.Min(1, progress);
				if ((progress >= 0.5f) && !moved)
				{
					offsetObject.position = endPoint;
					moved = true; // only move once
				}
			}

			public void UpdateUI()
			{
				float fadeFactor = 1.0f - Math.Abs(progress * 2 - 1); // from [0....1....0]
				foreach (ScreenFade fade in fadeEffects)
				{
					fade.FadeFactor = fadeFactor;
				}
			}

			public bool IsFinished()
			{
				return progress >= 1; // movement has finished
			}

			public void Cleanup()
			{
				foreach (ScreenFade fade in fadeEffects)
				{
					GameObject.Destroy(fade);
				}
			}


			private Vector3 endPoint;
			private float   duration, progress;
			private bool    moved;
			private List<ScreenFade> fadeEffects;
		}


		private class Transition_Move : ITransition
		{
			public Transition_Move(Vector3 startPoint, Vector3 endPoint, float duration, bool smooth)
			{
				this.startPoint = startPoint;
				this.endPoint = endPoint;
				this.duration = duration;
				this.smooth = smooth;

				progress = 0;
			}

			public void Update(Transform offsetObject)
			{
				// move from A to B
				progress += Time.deltaTime / duration;
				progress = Math.Min(1, progress);
				// linear: lerpFactor = progress. smooth: lerpFactor = sin(progress * PI/2) ^ 2
				float lerpFactor = smooth ? (float)Math.Pow(Math.Sin(progress * Math.PI / 2), 2) : progress;
				offsetObject.position = Vector3.Lerp(startPoint, endPoint, lerpFactor);
			}

			public void UpdateUI()
			{
				// nothing to do
			}

			public bool IsFinished()
			{
				return progress >= 1; // movement has finished
			}

			public void Cleanup()
			{
				// nothing to do
			}

			private Vector3 startPoint, endPoint;
			private float duration, progress;
			private bool smooth;
		}
	}
}
