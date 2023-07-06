using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //to change into Tower data
    public GameObject m_preconstructedTower;
    public TowerButtonUI[] m_towerButtons;

    //to remove
    [SerializeField] private LayerMask m_layerMask;


    void Update()
    {
        //Spawn the preconstructedTower and stick it to the mouse / grid.
        if (m_preconstructedTower)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, m_layerMask))
            {
                Vector3 gridPos = raycastHit.collider.transform.position;
                gridPos.y = .02f;
                m_preconstructedTower.transform.position = gridPos;
            }
        }
    }

    public void SelectTower(TowerButtonUI towerSelected)
    {
        //If we previously had a tower selected, destroy the preconstructed tower visual.
        if (m_preconstructedTower)
        {
            Destroy(m_preconstructedTower);
        }
        
        foreach (TowerButtonUI button in m_towerButtons)
        {
            if (button == towerSelected && !(towerSelected.m_buttonState == TowerButtonUI.ButtonState.IsSelected))
            {
                m_preconstructedTower = Instantiate(button.m_preconstructedTower);
                button.SetButtonState(TowerButtonUI.ButtonState.IsSelected);
                Debug.Log(button.name + " has been selected.");
            }
            else
            {
                button.SetButtonState(TowerButtonUI.ButtonState.CanBuild);
            }
        }
    }

}