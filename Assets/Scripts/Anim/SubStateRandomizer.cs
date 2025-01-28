using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestStateMachineBehavior : StateMachineBehaviour
{
    public string m_parameterName = "IdleIndex";
    public int m_clipArrayLength = 1;
    public int m_lastClipIndex;
    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        /*int newClipIndex = 0;
        if (m_clipArrayLength >= 1)
        { 
            newClipIndex = GetDifferentRandomNumber(0, m_clipArrayLength);
        }
        
        animator.SetInteger(m_parameterName, newClipIndex);
        Debug.Log($"New Clip Idex: {newClipIndex}, Array Length: {m_clipArrayLength}");*/ 
    }

    // OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int newClipIndex = 0;
        if (m_clipArrayLength >= 1)
        { 
            newClipIndex = GetDifferentRandomNumber(0, m_clipArrayLength);
        }
        
        animator.SetInteger(m_parameterName, newClipIndex);
        //Debug.Log($"New Clip Idex: {newClipIndex}, Array Length: {m_clipArrayLength}"); 
    }

    // OnStateMove is called before OnStateMove is called on any state inside this state machine
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateIK is called before OnStateIK is called on any state inside this state machine
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMachineEnter is called when entering a state machine via its Entry Node
    /*override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        int newClipIndex = 0;
        if (m_clipArrayLength >= 1)
        { 
            newClipIndex = GetDifferentRandomNumber(0, m_clipArrayLength);
        }
        
        animator.SetInteger(m_parameterName, newClipIndex);
        Debug.Log($"New Clip Idex: {newClipIndex}, Array Length: {m_clipArrayLength}");
        
    }

    // OnStateMachineExit is called when exiting a state machine via its Exit Node
    override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        int newClipIndex = 0;
        if (m_clipArrayLength >= 1)
        { 
            newClipIndex = GetDifferentRandomNumber(0, m_clipArrayLength);
        }
        
        animator.SetInteger(m_parameterName, newClipIndex);
        Debug.Log($"New Clip Idex: {newClipIndex}, Array Length: {m_clipArrayLength}");
    }*/
    
    public int GetDifferentRandomNumber(int min, int max)
    {
        int newRandomNumber;

        do
        {
            newRandomNumber = Random.Range(min, max);
        } while (newRandomNumber == m_lastClipIndex);

        m_lastClipIndex = newRandomNumber;

        return newRandomNumber;
    }
}
