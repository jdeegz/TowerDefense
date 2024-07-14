using UnityEngine;
using UnityEngine.UI;

public class Example_FilterAnInputField : MonoBehaviour {

	// This can be anything that has a text field of some kind.
	public InputField textToFilter; 
	private Censor censor;

	void Start () {
		censor = GetComponent<Censor> ();
	}

	void Update () {
		if(Input.GetKey(KeyCode.Return)) { //The if part is optional.
			textToFilter.text = censor.CensorText(textToFilter.text);
		}

		//You can use this anywhere: 
		// player.NickName = censor.CensorText(username.text);

		//The pattern is:			
		// filtered = censor.CensorText(InputTextGoesHere);
	}
}