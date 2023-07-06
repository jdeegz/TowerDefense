using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;
 using UnityEngine.UIElements;
 using Object = System.Object;

 [Serializable]
 public class Cell : MonoBehaviour
 {
     public CellObject cellObject;

     //Populated by the Cell Object
     [HideInInspector] public String cellType;

     //Bools frequently accessed
     [HideInInspector] public bool isBlocker;
     [HideInInspector] public bool isExplored;
     public bool isVisible;

     [HideInInspector] public GameObject interactable;
     public GameObject m_residentTower;

     [SerializeField] private Color m_baseColor;
     [SerializeField] private float m_highlightBrightness;
     private Material m_baseMaterial;
     private Color m_highlightColor;


     
     void Start()
     {
         //Adjust the colors of the cells.
         m_baseMaterial = GetComponent<MeshRenderer>().material;
         m_highlightColor = m_baseColor + new Color(m_highlightBrightness, m_highlightBrightness, m_highlightBrightness);
     }

     void Update()
     {
         //This allows us to see the changes we make in editor mode, and prevents this from running during play mode. Sweet!
         if (!Application.isPlaying)
         {
             isBlocker = cellObject.isBlocker;
             cellType = cellObject.cellType;
             
         }

         /*if (isVisible && resident)
         {p
             if (!charactersState.visibleCharacters.Contains(resident))
             {
                 charactersState.visibleCharacters.Add(resident);
             }
         }
         else
         {
             if (charactersState.visibleCharacters.Contains(resident))
             {
                 charactersState.visibleCharacters.Remove(resident);
             }
         }*/
     }

     public void AddInteractable(GameObject obj)
     {
         interactable = obj;
         /*isBlocker = obj.GetComponent<Interactable>().isBlocker;*/
     }

     public void RemoveInteractable(GameObject obj)
     {
         if (interactable == obj)
         {
             interactable = null;
             /*isBlocker = !obj.GetComponent<Interactable>().isBlocker;*/

         }
     }

     public void AddResident(GameObject obj)
     {
         m_residentTower = obj;

     }

     public void RemoveResident(GameObject obj)
     {
         m_residentTower = null;
     }

     void OnMouseEnter()
     {
         m_baseMaterial.color = m_highlightColor;
     }

     void OnMouseExit()
     {
         m_baseMaterial.color = m_baseColor;
     }

     void OnMouseDown()
     {
         if (m_residentTower != null) return;
         
         GameObject towerPrefab = BuildManager.m_buildManager.GetSelectedTowerPrefab();
         m_residentTower = Instantiate(towerPrefab, transform.position, Quaternion.identity);
     }
 }