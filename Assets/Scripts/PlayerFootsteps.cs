using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerFootsteps : MonoBehaviour{
    public float sprintVolume = 1f;
    public float crouchVolume = 0.2f;

    public float walkNoise = 5f;
    public float sprintNoise = 15f;
    public float crouchNoise = 2f;

    public bool isCrouching;
    public bool isSprinting;

    public float CurrentNoiseRadius { 
        get; 
        private set; 
    }

    CharacterController controller;
    AudioSource source;
    AudioClip sprintClip;
    AudioClip crouchClip;
    int currentState = 0; // 0 = silent, 1 = sprint, 2 = crouch

    void Awake(){
        controller = GetComponent<CharacterController>();
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 1f;

        AudioClip[] sprint = Resources.LoadAll<AudioClip>("Footsteps/Sprint");
        AudioClip[] crouch = Resources.LoadAll<AudioClip>("Footsteps/Crouch");
        if (sprint.Length > 0) sprintClip = sprint[0];
        if (crouch.Length > 0) crouchClip = crouch[0];
    }

    void Update(){
        Vector3 flat = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        bool moving = flat.magnitude > 0.1f && controller.isGrounded;

        int newState = 0;
        if (moving){
            if (isCrouching) newState = 2;
            else if (isSprinting) newState = 1;
        }

        if (!moving) CurrentNoiseRadius = 0f;
        else if (newState == 2) CurrentNoiseRadius = crouchNoise;
        else if (newState == 1) CurrentNoiseRadius = sprintNoise;
        else CurrentNoiseRadius = walkNoise;

        if (newState == currentState) return;
        currentState = newState;

        if (newState == 0){
            source.Stop();
            return;
        }

        source.clip = (newState == 1) ? sprintClip : crouchClip;
        source.volume = (newState == 1) ? sprintVolume : crouchVolume;
        if (source.clip != null) source.Play();
    }
}