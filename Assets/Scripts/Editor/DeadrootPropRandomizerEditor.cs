using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(DeadrootPropRandomizer))]
public class DeadrootPropRandomizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector UI
        DrawDefaultInspector();

        // Reference to the GridManager target script
        DeadrootPropRandomizer deadrootPropRandomizer = (DeadrootPropRandomizer)target;

        // Add a button and call a method when it is pressed
        if (GUILayout.Button("Replace Deadroots"))
        {
            if (deadrootPropRandomizer.m_deadrootPropPrefabs.Count == 0)
            {
                Debug.Log($"You must assign tree prefabs to Resource Manager.");
                return;
            }
            
            Debug.Log("Updating Deadroots");
            
            ReplaceDeadrootPrefabs(GetDeadrootInScene(), deadrootPropRandomizer.m_deadrootPropPrefabs);
            Debug.Log($"Deadroots Updated.");
            
            Debug.Log($"Trees list cleared.");
        }
    }
    
    public List<DeadrootProp> GetDeadrootInScene()
    {
        List<DeadrootProp> deadroot = new List<DeadrootProp>();
        List<DeadrootProp> allNodes = new List<DeadrootProp>(FindObjectsOfType<DeadrootProp>());

        foreach (DeadrootProp node in allNodes)
        {
            deadroot.Add(node);
        }

        Debug.Log($"Returning a list of Deadroots. Length: {allNodes.Count}.");
        return deadroot;
    }
    
    public void ReplaceDeadrootPrefabs(List<DeadrootProp> deadroots, List<GameObject> prefabs)
    {
        foreach (DeadrootProp node in deadroots)
        {
            //Get the original object transform.
            Transform originalNodeTransform = node.transform;
            
            //Pick a random prefab to spawn.
            int index = Random.Range(0, prefabs.Count);
            
            //Create a new object.
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[index]);

            float randomYRot = Random.Range(0, 360);
            Quaternion newRot = Quaternion.Euler(0, randomYRot, 0);
            
            float randomYPos = Random.Range(0, -.115f);
            Vector3 newPos = new Vector3(originalNodeTransform.position.x, randomYPos, originalNodeTransform.position.z);
            
            
            newObject.transform.rotation = newRot;
            newObject.transform.position = newPos;
            newObject.transform.localScale = originalNodeTransform.localScale;
            newObject.transform.parent = originalNodeTransform.parent;

            DeadrootProp treeProp = newObject.GetComponent<DeadrootProp>();
            int randomInt = Random.Range(0, 4);
            treeProp.ToggleObjects(randomInt == 1);
            
            
            DestroyImmediate(node.gameObject);
        }
        
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
