using UnityEngine;
using UnityEditor;
using AIBusinessTycoon.Managers;

namespace AIBusinessTycoon.Editor
{
    /// <summary>
    /// Unity Editor script to automatically create and configure the EventManager GameObject.
    /// Usage: GameObject > AI Business Tycoon > Create Event Manager
    /// </summary>
    public static class EventManagerSetup
    {
        [MenuItem("GameObject/AI Business Tycoon/Create Event Manager", false, 10)]
        private static void CreateEventManager(MenuCommand menuCommand)
        {
            // Check if EventManager already exists
            EventManager existing = Object.FindObjectOfType<EventManager>();
            
            if (existing != null)
            {
                Debug.LogWarning("[EventManagerSetup] EventManager already exists in the scene!");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            
            // Create EventManager GameObject
            GameObject managerObj = new GameObject("EventManager");
            
            // Add EventManager component
            EventManager manager = managerObj.AddComponent<EventManager>();
            
            // Register for undo
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Event Manager");
            
            // Select the created object
            Selection.activeGameObject = managerObj;
            
            Debug.Log("[EventManagerSetup] EventManager created successfully! Configure polling settings in Inspector.");
        }
        
        [MenuItem("GameObject/AI Business Tycoon/Create Event Manager", true)]
        private static bool ValidateCreateEventManager()
        {
            // Only enable if EventManager doesn't exist
            return Object.FindObjectOfType<EventManager>() == null;
        }
    }
}
