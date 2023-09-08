using UnityEngine.EventSystems;

public class NoMouseEvents : StandaloneInputModule
{
	public override void Process()
	{
		bool selectedObject = this.SendUpdateEventToSelectedObject();
		if (this.eventSystem.sendNavigationEvents)
		{
			if (!selectedObject)
				selectedObject |= this.SendMoveEventToSelectedObject();
			if (!selectedObject)
				this.SendSubmitEventToSelectedObject();
		}
		//this.ProcessMouseEvent();
	}
}
