using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(TreePropRandomizer))]
public class TreePropRandomizeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector UI
        DrawDefaultInspector();

        // Reference to the GridManager target script
        TreePropRandomizer m_treePropRandomizer = (TreePropRandomizer)target;

        // Add a button and call a method when it is pressed
        if (GUILayout.Button("Replace Trees"))
        {
            if (m_treePropRandomizer.m_treePropPrefabs.Count == 0)
            {
                Debug.Log($"You must assign tree prefabs to Resource Manager.");
                return;
            }
            
            Debug.Log("Updating Trees");
            
            ReplaceTreePrefabs(GetTreesInScene(), m_treePropRandomizer.m_treePropPrefabs);
            Debug.Log($"Trees Updated.");
            
            Debug.Log($"Trees list cleared.");
        }
    }

    public List<TreeProp> GetTreesInScene()
    {
        List<TreeProp> trees = new List<TreeProp>();
        List<TreeProp> allNodes = new List<TreeProp>(FindObjectsOfType<TreeProp>());

        foreach (TreeProp node in allNodes)
        {
            trees.Add(node);
        }
        
        return trees;
    }

    public void ReplaceTreePrefabs(List<TreeProp> trees, List<GameObject> prefabs)
    {
        foreach (TreeProp node in trees)
        {
            //Get the original object transform.
            Transform originalNodeTransform = node.transform;
            
            //Pick a random prefab to spawn.
            int index = Random.Range(0, prefabs.Count);
            
            //Create a new object.
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[index]);

            newObject.transform.position = originalNodeTransform.position;
            float randomYRot = Random.Range(0, 360);
            Quaternion newRot = Quaternion.Euler(0, randomYRot, 0);
            newObject.transform.rotation = newRot;
            newObject.transform.localScale = originalNodeTransform.localScale;
            newObject.transform.parent = originalNodeTransform.parent;

            TreeProp treeProp = newObject.GetComponent<TreeProp>();
            int randomInt = Random.Range(0, 18);
            treeProp.ToggleObjects(randomInt == 1);
            
            
            DestroyImmediate(node.gameObject);
        }
        
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
