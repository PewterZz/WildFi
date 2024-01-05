using UnityEngine;

namespace SimpleFPS
{
	public class UIDisconnectedView : MonoBehaviour
	{
		public void GoToMenu()
		{
			var gameUI = GetComponentInParent<GameUI>(true);
			gameUI.GoToMenu();
		}

		private void Update()
		{
			// Make sure the cursor stays unlocked.
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}
}
