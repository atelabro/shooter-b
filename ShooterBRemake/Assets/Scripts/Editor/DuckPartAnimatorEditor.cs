using UnityEngine;
using UnityEditor;

namespace ShooterB
{
    [CustomEditor(typeof(DuckPartAnimator))]
    public class DuckPartAnimatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            DuckPartAnimator duck = (DuckPartAnimator)target;

            if (!duck.IsActive)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Enter play mode and spawn this duck type to edit its config here.", MessageType.Info);
                return;
            }

            DuckPartLibrary library = duck.PartLibrary;
            if (library == null) return;
            if (!library.TryGetConfig(duck.CurrentDuckType, out DuckPartConfig config)) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(duck.CurrentDuckType.ToString(), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            config.leftWingPivotOffset  = EditorGUILayout.Vector2Field("L Wing Pivot",   config.leftWingPivotOffset);
            config.rightWingPivotOffset = EditorGUILayout.Vector2Field("R Wing Pivot",   config.rightWingPivotOffset);
            config.leftWingOffset       = EditorGUILayout.Vector2Field("L Wing Offset",  config.leftWingOffset);
            config.rightWingOffset      = EditorGUILayout.Vector2Field("R Wing Offset",  config.rightWingOffset);

            EditorGUILayout.Space();

            config.flapSpeed     = EditorGUILayout.FloatField("Flap Speed",     config.flapSpeed);
            config.flapAmplitude = EditorGUILayout.FloatField("Flap Amplitude", config.flapAmplitude);
            config.phaseOffset   = EditorGUILayout.FloatField("Phase Offset",   config.phaseOffset);

            EditorGUILayout.Space();

            config.torsoBobAmount = EditorGUILayout.FloatField("Torso Bob Amount", config.torsoBobAmount);
            config.torsoBobSpeed  = EditorGUILayout.FloatField("Torso Bob Speed",  config.torsoBobSpeed);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(library, "Edit Duck Part Config");
                library.SetConfig(duck.CurrentDuckType, config);
                EditorUtility.SetDirty(library);
            }
        }

        void OnSceneGUI()
        {
            DuckPartAnimator duck = (DuckPartAnimator)target;
            if (!duck.IsActive) return;

            DuckPartLibrary library = duck.PartLibrary;
            if (library == null) return;
            if (!library.TryGetConfig(duck.CurrentDuckType, out DuckPartConfig config)) return;

            Vector3 leftWorld = duck.transform.TransformPoint(
                new Vector3(config.leftWingPivotOffset.x, config.leftWingPivotOffset.y, 0f));
            Vector3 rightWorld = duck.transform.TransformPoint(
                new Vector3(config.rightWingPivotOffset.x, config.rightWingPivotOffset.y, 0f));

            EditorGUI.BeginChangeCheck();

            Handles.color = Color.cyan;
            Vector3 newLeft = Handles.FreeMoveHandle(leftWorld, 0.15f, Vector3.zero, Handles.DotHandleCap);
            Handles.Label(leftWorld + Vector3.up * 0.2f, "L pivot");

            Handles.color = Color.yellow;
            Vector3 newRight = Handles.FreeMoveHandle(rightWorld, 0.15f, Vector3.zero, Handles.DotHandleCap);
            Handles.Label(rightWorld + Vector3.up * 0.2f, "R pivot");

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(library, "Move Wing Pivot");

                if (newLeft != leftWorld)
                {
                    Vector3 l = duck.transform.InverseTransformPoint(newLeft);
                    config.leftWingPivotOffset = new Vector2(l.x, l.y);
                }

                if (newRight != rightWorld)
                {
                    Vector3 r = duck.transform.InverseTransformPoint(newRight);
                    config.rightWingPivotOffset = new Vector2(r.x, r.y);
                }

                library.SetConfig(duck.CurrentDuckType, config);
                EditorUtility.SetDirty(library);
            }
        }
    }
}
