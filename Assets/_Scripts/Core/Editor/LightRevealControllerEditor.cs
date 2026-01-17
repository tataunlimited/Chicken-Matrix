using UnityEngine;
using UnityEditor;

namespace _Scripts.Core.Editor
{
    [CustomEditor(typeof(LightRevealController))]
    public class LightRevealControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            LightRevealController controller = (LightRevealController)target;
            SpriteMask spriteMask = controller.GetComponent<SpriteMask>();
            SpriteRenderer spriteRenderer = controller.GetComponent<SpriteRenderer>();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Component Status", EditorStyles.boldLabel);

            // Check SpriteMask
            if (spriteMask != null)
            {
                if (spriteMask.sprite == null)
                {
                    EditorGUILayout.HelpBox("Sprite Mask has no sprite assigned! Add a Sprite Renderer with a sprite, or assign a sprite to the Sprite Mask manually.", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Sprite Mask is using: {spriteMask.sprite.name}", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Missing Sprite Mask component!", MessageType.Error);
            }

            // Check SpriteRenderer
            if (spriteRenderer != null)
            {
                if (spriteRenderer.sprite == null)
                {
                    EditorGUILayout.HelpBox("Sprite Renderer has no sprite assigned!", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Sprite Renderer is using: {spriteRenderer.sprite.name}", MessageType.Info);
                }
            }

            EditorGUILayout.Space();

            // Quick fix button
            if (GUILayout.Button("Copy Sprite from Renderer to Mask"))
            {
                if (spriteRenderer != null && spriteRenderer.sprite != null && spriteMask != null)
                {
                    spriteMask.sprite = spriteRenderer.sprite;
                    EditorUtility.SetDirty(spriteMask);
                }
                else
                {
                    Debug.LogWarning("Cannot copy sprite - either Sprite Renderer or its sprite is null");
                }
            }
        }
    }
}
