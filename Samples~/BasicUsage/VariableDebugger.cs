using UnityEngine;
using ForkTrack;
using ForkTrack.Core;

namespace ForkTrack.Samples.BasicUsage
{
    /// <summary>
    /// Runtime debugger for ForkTrack variables.
    /// Shows all variables and their current values.
    /// </summary>
    public class VariableDebugger : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showOnGUI = true;
        [SerializeField] private Vector2 guiPosition = new Vector2(10, 10);
        [SerializeField] private float guiWidth = 300;

        private void OnGUI()
        {
            if (!showOnGUI || !ForkTrack.IsLoaded)
                return;

            var variables = ForkTrack.GetAllVariables();
            if (variables == null || variables.Count == 0)
                return;

            GUILayout.BeginArea(new Rect(guiPosition.x, guiPosition.y, guiWidth, Screen.height - guiPosition.y - 10));
            GUILayout.BeginVertical("box");

            GUILayout.Label("ForkTrack Variables", GUI.skin.box);

            foreach (var variable in variables)
            {
                GUILayout.BeginHorizontal();

                // Variable name and type
                string typeLabel = variable.type == VariableType.NUMBER ? "[NUM]" : "[BOOL]";
                GUILayout.Label($"{typeLabel} {variable.name}", GUILayout.Width(guiWidth * 0.6f));

                // Current value
                if (variable.type == VariableType.NUMBER)
                {
                    var value = ForkTrack.GetVariable<float>(variable.name);
                    GUILayout.Label(value.ToString("F2"));
                }
                else
                {
                    var value = ForkTrack.GetVariable<bool>(variable.name);
                    GUILayout.Label(value ? "TRUE" : "FALSE");
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Logs all current variable values to console
        /// </summary>
        [ContextMenu("Log All Variables")]
        public void LogAllVariables()
        {
            if (!ForkTrack.IsLoaded)
            {
                Debug.Log("[VariableDebugger] No graph loaded");
                return;
            }

            var variables = ForkTrack.GetAllVariables();
            Debug.Log($"[VariableDebugger] {variables.Count} variables:");

            foreach (var variable in variables)
            {
                if (variable.type == VariableType.NUMBER)
                {
                    var value = ForkTrack.GetVariable<float>(variable.name);
                    Debug.Log($"  {variable.name} (NUMBER) = {value}");
                }
                else
                {
                    var value = ForkTrack.GetVariable<bool>(variable.name);
                    Debug.Log($"  {variable.name} (BOOLEAN) = {value}");
                }
            }
        }

        /// <summary>
        /// Sets a number variable (for testing from Inspector)
        /// </summary>
        public void SetNumberVariable(string name, float value)
        {
            ForkTrack.SetVariable(name, value);
        }

        /// <summary>
        /// Sets a boolean variable (for testing from Inspector)
        /// </summary>
        public void SetBoolVariable(string name, bool value)
        {
            ForkTrack.SetVariable(name, value);
        }
    }
}
