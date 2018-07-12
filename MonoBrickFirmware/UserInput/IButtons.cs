using System;
using System.Threading;

namespace MonoBrickFirmware.UserInput
{
    public enum LedColor
    {
        Off,
        Green,
        Red,
        Orange,
    }

    public enum LedEffect
    {
        Normal = 0,
        Flash = 3,
        Pulse = 6,
    }

	public interface IButtons
	{
		Buttons.ButtonStates GetStates ();
		void WaitForKeyRelease (CancellationToken token);
		void WaitForKeyRelease ();
		Buttons.ButtonStates GetKeypress (CancellationToken token);
		Buttons.ButtonStates GetKeypress ();
		void LedPattern (LedColor color, LedEffect effect);
	}

   
}

