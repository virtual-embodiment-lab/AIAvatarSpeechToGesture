using UnityEngine;
using UnityEditor;

namespace CrazyMinnow.AmplitudeWebGL
{
	[CustomEditor(typeof(Amplitude))]
	public class AmplitudeEditor : Editor
	{
		private Amplitude instance;
		private Texture inspLogo;

		public void OnEnable()
		{
			instance = target as Amplitude;
			inspLogo = (Texture2D)Resources.Load("Amplitude");
		}

		public override void OnInspectorGUI()
		{
			GUILayout.Space(5);
			GUILayout.Box(new GUIContent(inspLogo), new GUIStyle(), new GUILayoutOption[] { GUILayout.Height(35) });

			EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Space(5);

                EditorGUI.BeginChangeCheck();
                instance.audioSource = (AudioSource)EditorGUILayout.ObjectField(
                    new GUIContent("AudioSource", "Link to the AudioSource component you wish to analyze"),
                    instance.audioSource, typeof(AudioSource), true);
                EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                instance.dataType = (Amplitude.DataType)EditorGUILayout.EnumPopup("Data Type", instance.dataType);
                EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                instance.sampleSize = EditorGUILayout.IntPopup("Sample Size", instance.sampleSize, 
                    instance.sampleSizeNames, instance.sampleSizeVals);
                EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                instance.boost = EditorGUILayout.Slider(
					new GUIContent("Boost", "Adjust the boost of the amplitude levels."), 
					instance.boost, 0f, 1f);
                EditorGUI.EndChangeCheck();

                if (instance.dataType == Amplitude.DataType.Amplitude)
                {
	                EditorGUI.BeginChangeCheck();
				    instance.absoluteValues = EditorGUILayout.Toggle(
					    new GUIContent("Absolute Values", "Force the output array to use absolute (positive) values only."),
					    instance.absoluteValues);
				    EditorGUI.EndChangeCheck();
                }
                else
                {
                    instance.absoluteValues = false;
                }

				GUILayout.Space(5);
			}
			EditorGUILayout.EndVertical();
			PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
		}
	}
}