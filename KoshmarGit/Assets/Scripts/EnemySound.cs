using UnityEngine;

public class EnemySound : MonoBehaviour
{
    public AudioClip sound;
    public float repeatInterval = 15f;
    public float volume = 1f;

    private float timer;

    void Start()
    {
        PlaySound();
        timer = repeatInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            PlaySound();
            timer = repeatInterval;
        }
    }

    void PlaySound()
    {
        if (sound != null)
        {
            AudioSource.PlayClipAtPoint(sound, transform.position, volume);
        }
    }
}
