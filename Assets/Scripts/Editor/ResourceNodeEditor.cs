using System.Collections.Generic;
using Unity.Plastic.Antlr3.Runtime.Misc;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(ResourceManager))]
public class ResourceNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector UI
        DrawDefaultInspector();

        // Reference to the GridManager target script
        ResourceManager m_resourceManager = (ResourceManager)target;

        // Add a button and call a method when it is pressed
        if (GUILayout.Button("Replace Trees"))
        {
            if (m_resourceManager.m_treePrefabs.Count == 0)
            {
                Debug.Log($"You must assign tree prefabs to Resource Manager.");
                return;
            }
            
            Debug.Log("Updating Trees");
            
            ReplaceTreePrefabs(GetTreesInScene(), m_resourceManager.m_treePrefabs);
            Debug.Log($"Trees Updated.");
            
            Debug.Log($"Trees list cleared.");
        }

        if (GUILayout.Button("Remove Duplicate Trees"))
        {
            RemoveDuplicates(GetTreesInScene());
        }
    }

    public List<ResourceNode> GetTreesInScene()
    {
        List<ResourceNode> trees = new List<ResourceNode>();
        List<ResourceNode> allNodes = new List<ResourceNode>(FindObjectsOfType<ResourceNode>());

        foreach (ResourceNode node in allNodes)
        {
            if (node.m_nodeData.m_resourceType == ResourceManager.ResourceType.Wood)
            {
                trees.Add(node);
            }
        }
        
        return trees;
    }

    public void ReplaceTreePrefabs(List<ResourceNode> trees, List<GameObject> prefabs)
    {
        foreach (ResourceNode node in trees)
        {
            //Get the original object transform.
            Transform originalNodeTransform = node.transform;
            
            //Pick a random prefab to spawn.
            int index = Random.Range(0, prefabs.Count);
            
            //Create a new object.
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[index]);

            newObject.transform.position = originalNodeTransform.position;
            float randomYRot = Random.Range(0, 359);
            Quaternion newRot = newObject.transform.rotation;
            newRot.y = randomYRot;
            newObject.transform.rotation = newRot;
            newObject.transform.localScale = originalNodeTransform.localScale;
            newObject.transform.parent = originalNodeTransform.parent;
            
            DestroyImmediate(node.gameObject);
        }
        
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public void RemoveDuplicates(List<ResourceNode> nodes)
    {
        Debug.Log($"Removing Duplicate Nodes.");
        HashSet<Vector3> uniquePos = new HashSet<Vector3>();
        List<GameObject> dupeObjs = new ListStack<GameObject>();

        for (int i = nodes.Count - 1; i >= 0; --i)
        {
            if (uniquePos.Contains(nodes[i].transform.position))
            {
                dupeObjs.Add(nodes[i].gameObject);
            }
            else
            {
                uniquePos.Add(nodes[i].transform.position);
            }
        }

        if (dupeObjs.Count == 0)
        {
            Debug.Log($"No Duplicate Objects found.");
            Debug.Log($"Duplicate Removal complete.");
            return;
        }
        

        for (int x = dupeObjs.Count - 1; x >= 0; --x)
        {
            Debug.Log($"Removing duplicate object: {dupeObjs[x].name}");
            DestroyImmediate(dupeObjs[x]);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Duplicate Removal complete.");
    }
}