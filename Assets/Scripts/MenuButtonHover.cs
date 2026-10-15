using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Button Text")]
    public TMP_Text buttonText;

    [Header("Selection Bar")]
    public RectTransform selectionBar;

    [Header("Colours")]
    public Color normalColour = Color.white;
    public Color selectedColour = new Color(0.18f, 0.85f, 0.78f, 1f);

    [Header("Menu Sounds")]
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    void Start()
    {
        if (buttonText != null)
        {
            buttonText.color = normalColour;
        }

        if (selectionBar != null)
        {
            selectionBar.gameObject.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null)
        {
            buttonText.color = selectedColour;
        }

        if (selectionBar != null)
        {
            Vector3 newPosition = selectionBar.position;
            newPosition.y = transform.position.y;

            selectionBar.position = newPosition;
            selectionBar.gameObject.SetActive(true);
        }

        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null)
        {
            buttonText.color = normalColour;
        }

        if (selectionBar != null)
        {
            selectionBar.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}