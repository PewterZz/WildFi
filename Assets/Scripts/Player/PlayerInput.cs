using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

namespace SimpleFPS
{
	public enum EInputButton
	{
		Jump,
		Fire,
		Reload,
		Pistol,
		Rifle,
		Shotgun,
		Spray,
	}

	public struct NetworkedInput : INetworkInput
	{
		public Vector2        MoveDirection;
		public Vector2        LookRotationDelta;
		public NetworkButtons Buttons;
	}


	[DefaultExecutionOrder(-10)]
	public sealed class PlayerInput : NetworkBehaviour, IBeforeUpdate
	{
		public static float LookSensitivity;

		private NetworkedInput _accumulatedInput;

		public override void Spawned()
		{
			if (HasInputAuthority == false)
				return;

			// Register to Fusion input poll callback.
			var networkEvents = Runner.GetComponent<NetworkEvents>();
			networkEvents.OnInput.AddListener(OnInput);

			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (runner == null)
				return;

			var networkEvents = runner.GetComponent<NetworkEvents>();
			if (networkEvents != null)
			{
				networkEvents.OnInput.RemoveListener(OnInput);
			}
		}

		void IBeforeUpdate.BeforeUpdate()
		{
			// This method is called BEFORE ANY FixedUpdateNetwork() and is used to accumulate input from Keyboard/Mouse.
			// Input accumulation is mandatory - this method is called multiple times before new forward FixedUpdateNetwork() - common if rendering speed is faster than Fusion simulation.

			if (HasInputAuthority == false)
				return;

			// Enter key is used for locking/unlocking cursor in game view.
			var keyboard = Keyboard.current;
			if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
			{
				if (Cursor.lockState == CursorLockMode.Locked)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
				else
				{
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				}
			}

			// Accumulate input only if the cursor is locked.
			if (Cursor.lockState != CursorLockMode.Locked)
				return;

			var mouse = Mouse.current;
			if (mouse != null)
			{
				var mouseDelta = mouse.delta.ReadValue();

				var lookRotationDelta = new Vector2(-mouseDelta.y, mouseDelta.x);
				lookRotationDelta *= LookSensitivity / 60f;

				_accumulatedInput.LookRotationDelta += lookRotationDelta;
				_accumulatedInput.Buttons.Set(EInputButton.Fire, mouse.leftButton.isPressed);
			}

			if (keyboard != null)
			{
				var moveDirection = Vector2.zero;

				if (keyboard.wKey.isPressed) { moveDirection += Vector2.up;    }
				if (keyboard.sKey.isPressed) { moveDirection += Vector2.down;  }
				if (keyboard.aKey.isPressed) { moveDirection += Vector2.left;  }
				if (keyboard.dKey.isPressed) { moveDirection += Vector2.right; }

				_accumulatedInput.MoveDirection = moveDirection.normalized;

				_accumulatedInput.Buttons.Set(EInputButton.Jump, keyboard.spaceKey.isPressed);
				_accumulatedInput.Buttons.Set(EInputButton.Reload, keyboard.rKey.isPressed);
				_accumulatedInput.Buttons.Set(EInputButton.Pistol, keyboard.digit1Key.isPressed || keyboard.numpad1Key.isPressed);
				_accumulatedInput.Buttons.Set(EInputButton.Rifle, keyboard.digit2Key.isPressed || keyboard.numpad2Key.isPressed);
				_accumulatedInput.Buttons.Set(EInputButton.Shotgun, keyboard.digit3Key.isPressed || keyboard.numpad3Key.isPressed);
				_accumulatedInput.Buttons.Set(EInputButton.Spray, keyboard.fKey.isPressed);
			}
		}

		private void OnInput(NetworkRunner runner, NetworkInput networkInput)
		{
			// Fusion polls accumulated input. This callback can be executed multiple times in a row if there is a performance spike.
			networkInput.Set(_accumulatedInput);

			// We have to keep whole input except look rotation delta, this needs to be cleared so it's not used in subsequent OnInput() call.
			_accumulatedInput.LookRotationDelta = default;
		}
	}
}
