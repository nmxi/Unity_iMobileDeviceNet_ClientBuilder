using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TextMeshProUGUI))]
public class HyperlinkUI : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData e)
    {
        var text = GetComponent<TextMeshProUGUI>();
        var pos = Input.mousePosition;
        var canvas = text.canvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var index = TMP_TextUtilities.FindIntersectingLink(text, pos, camera);

        if (index == -1) return;

        var linkInfo = text.textInfo.linkInfo[index];
        var url = linkInfo.GetLinkID();

        Application.OpenURL(url);
    }
}