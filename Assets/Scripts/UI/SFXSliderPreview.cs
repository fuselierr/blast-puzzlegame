using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SFXSliderPreview : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] private Slider sfxSlider;

    public void OnPointerUp(PointerEventData eventData)
    {
        SoundManager.Instance.PlayTimerTick();
    }
}
