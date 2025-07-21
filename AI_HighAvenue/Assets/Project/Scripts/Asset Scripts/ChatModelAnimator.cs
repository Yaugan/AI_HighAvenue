using UnityEngine;

public class ChatModelAnimator : MonoBehaviour
{
    public static ChatModelAnimator Instance { get; private set; }

    [Header("Animation States")]
    [SerializeField] private string idleStateName = "idle";
    [SerializeField] private string thinkingStateName = "rotate";
    [SerializeField] private string speakingStateName = "speaking";

    private Animator animator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("❌ ChatModelAnimator: No Animator component found on " + gameObject.name);
        }
    }

    /// <summary>
    /// Plays the idle animation (default state)
    /// </summary>
    public void PlayIdle()
    {
        if (animator != null)
        {
            animator.Play(idleStateName);
            Debug.Log("🎭 Model: Playing idle animation");
        }
    }

    /// <summary>
    /// Plays the thinking/loading animation (rotate)
    /// </summary>
    public void PlayThinking()
    {
        if (animator != null)
        {
            animator.Play(thinkingStateName);
            Debug.Log("🎭 Model: Playing thinking animation");
        }
    }

    /// <summary>
    /// Plays the speaking animation
    /// </summary>
    public void PlaySpeaking()
    {
        if (animator != null)
        {
            animator.Play(speakingStateName);
            Debug.Log("🎭 Model: Playing speaking animation");
        }
    }

    /// <summary>
    /// Returns to idle state after speaking
    /// </summary>
    public void StopSpeaking()
    {
        if (animator != null)
        {
            animator.Play(idleStateName);
            Debug.Log("🎭 Model: Stopped speaking, returning to idle");
        }
    }
} 