using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class UnityUtils {

    /// <summary>
    /// Search the active scene for objects of the given type.
    /// </summary>
    public static List<T> FindObjectsOfType<T>(bool includeInactive = false) where T : Object {
        var scene = SceneManager.GetActiveScene();
        if (scene == null) {
            // No active scene.
            return null;
        }

        var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        if (rootObjects == null || rootObjects.Length == 0) {
            // No root objects.
            return null;
        }
        
        var list = new List<T>();
        foreach(var rootObject in rootObjects) {
            rootObject.GetComponentsInChildren<T>(includeInactive, list);
        }

        return list;
    }

    public static T FindObjectOfType<T>(bool includeInactive = false) where T : Object  {
        var scene = SceneManager.GetActiveScene();
        if (scene == null) {
            // No active scene.
            return null;
        }

        var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        if (rootObjects == null || rootObjects.Length == 0) {
            // No root objects.
            return null;
        }

        foreach(var rootObject in rootObjects) {
            var obj = rootObject.GetComponentInChildren<T>(includeInactive);
            if (obj != null) {
                // Found an object of the given type.
                return obj;
            }
        }

        // No object of given type was found.
        return null;
    }
}