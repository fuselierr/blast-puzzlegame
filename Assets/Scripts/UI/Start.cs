using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    [SerializeField] private GameObject titleText;
    [SerializeField] private GameObject startButton;

    // Physics for the button's jump-and-fall
    [SerializeField] private float gravity = 9.8f;      // was 0.1f — way too weak to feel like gravity
    [SerializeField] private float initYvel = 6f;        // was 0.5f — the "jump" launch speed
    float yVel;

    [SerializeField] private float titleSpeed = 4f;       // constant speed for the title exit
    [SerializeField] private float offScreenY = 15f;       // how far up/down counts as "off screen"

    public void EnterMenu()
    {
        StartCoroutine(EnterMenuCoroutine());
    }

    private System.Collections.IEnumerator EnterMenuCoroutine()
    {
        var buttonEffect = startButton.GetComponent<UIButtonPressEffect>();
        if (buttonEffect != null)
        {
            Destroy(buttonEffect);
        }

        yVel = initYvel; // reset the jump velocity each time this plays

        bool titleGone = false;
        bool buttonGone = false;

        while (!buttonGone)
        {
            float dt = Time.deltaTime;

            titleText.transform.position += Vector3.up * titleSpeed * dt;

            // Button: jump up, then gravity pulls it back down and off the bottom
            if (!buttonGone)
            {
                yVel -= gravity * dt;
                startButton.transform.position += Vector3.up * yVel * dt;

                if (startButton.transform.position.y < -offScreenY)
                {
                    buttonGone = true;
                }
            }

            yield return null;
        }

        SceneManager.LoadScene("MainMenu");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        yVel = initYvel;
    }

    // Update is called once per frame
    void Update()
    {

    }
}