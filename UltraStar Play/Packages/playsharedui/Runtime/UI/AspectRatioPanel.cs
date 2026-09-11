using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class AspectRatioPanel : VisualElement
{
	private int aspectRatioX = 16;
	[UxmlAttribute("aspect-ratio-x")]
	public int AspectRatioX
	{
		get => aspectRatioX;
		set => aspectRatioX = Mathf.Max(1, value);
	}

	private int aspectRatioY = 9;
	[UxmlAttribute("aspect-ratio-y")]
	public int AspectRatioY
	{
		get => aspectRatioY;
		set => aspectRatioY = Mathf.Max(1, value);
	}

	private int balanceX = 50;
	[UxmlAttribute("balance-x")]
	public int BalanceX
	{
		get => balanceX;
		set => balanceX = Mathf.Clamp(value, 0, 100);
	}

	private int balanceY = 50;
	[UxmlAttribute("balance-y")]
	public int BalanceY
	{
		get => balanceY;
		set => balanceY = Mathf.Clamp(value, 0, 100);
	}

	public AspectRatioPanel()
	{
		style.position = Position.Absolute;
		style.left = 0;
		style.top = 0;
		style.right = StyleKeyword.Undefined;
		style.bottom = StyleKeyword.Undefined;
		RegisterCallback<AttachToPanelEvent>( OnAttachToPanelEvent );
	}


	void OnAttachToPanelEvent( AttachToPanelEvent e )
	{
		parent?.RegisterCallback<GeometryChangedEvent>( OnGeometryChangedEvent );
		FitToParent();
	}


	void OnGeometryChangedEvent( GeometryChangedEvent e )
	{
		FitToParent();
	}


	void FitToParent()
	{
        if (parent == null)
        {
            return;
        }
		
        float parentW = parent.resolvedStyle.width;
		float parentH = parent.resolvedStyle.height;
        if (float.IsNaN(parentW) || float.IsNaN(parentH))
        {
            return;
        }

		style.position = Position.Absolute;
		style.left = 0;
		style.top = 0;
		style.right = StyleKeyword.Undefined;
		style.bottom = StyleKeyword.Undefined;

		if (AspectRatioX <= 0.0f || AspectRatioY <= 0.0f)
		{
			style.width = parentW;
			style.height = parentH;
			return;
		}

		var ratio = Mathf.Min( parentW / AspectRatioX, parentH / AspectRatioY );
		var targetW = Mathf.Floor( AspectRatioX * ratio );
		var targetH = Mathf.Floor( AspectRatioY * ratio );
		style.width = targetW;
		style.height = targetH;

		var marginX = parentW - targetW;
		var marginY = parentH - targetH;
		style.left = Mathf.Floor( marginX * BalanceX / 100.0f );
		style.top = Mathf.Floor( marginY * BalanceY / 100.0f );
	}
}
