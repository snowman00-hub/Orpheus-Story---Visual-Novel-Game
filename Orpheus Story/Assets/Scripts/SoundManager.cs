using UnityEngine;

// BGM과 효과음 재생을 전담하고, 볼륨 조절 등 오디오 설정을 관리한다.
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 새로운 BGM이면 교체해서 반복 재생한다.
    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    // 효과음을 한 번 재생한다.
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    // BGM 볼륨을 설정한다 (0~1).
    public void SetBgmVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    // 효과음 볼륨을 설정한다 (0~1).
    public void SetSfxVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}
