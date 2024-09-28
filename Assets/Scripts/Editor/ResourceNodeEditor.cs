using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

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
            if (m_resourceManager.m_treesInScene == null) m_resourceManager.m_treesInScene = new List<ResourceNode>();
            
            m_resourceManager.m_treesInScene.Clear();
            m_resourceManager.m_treesInScene = GetTreesInScene();

            ReplaceTreePrefabs(m_resourceManager.m_treesInScene, m_resourceManager.m_treePrefabs);
            Debug.Log($"Trees Updated.");
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
            newObject.transform.rotation = originalNodeTransform.rotation;
            newObject.transform.localScale = originalNodeTransform.localScale;
            newObject.transform.parent = originalNodeTransform.parent;
            
            DestroyImmediate(node.gameObject);
        }
        
    }
}