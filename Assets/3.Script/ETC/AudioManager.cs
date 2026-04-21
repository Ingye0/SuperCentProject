using UnityEngine;

// 게임 전체 사운드를 관리하는 싱글턴
// - 닭 죽는 소리
// - 무기 휘두르는 소리
// - 플레이어 인벤토리 입출력 소리
// - 프라이어 루프 소리
// - 배경음악
public class AudioManager : MonoBehaviour
{
    // 어디서든 AudioManager.I 로 접근 가능
    public static AudioManager I { get; private set; }

    [Header("오디오 소스")]
    [SerializeField] private AudioSource sfxSource;        // 일반 효과음 재생용
    [SerializeField] private AudioSource fryerLoopSource;  // 프라이어 루프 전용
    [SerializeField] private AudioSource bgmSource;        // 배경음악 재생용

    [Header("효과음 클립")]
    [SerializeField] private AudioClip chickenDeadClip;     // 닭 죽는 소리
    [SerializeField] private AudioClip weaponSwingClip;     // 무기 휘두르는 소리
    [SerializeField] private AudioClip inventoryActionClip; // 플레이어 인벤토리 입출력 공용 소리

    [Header("프라이어 루프")]
    [SerializeField] private AudioClip fryerLoopClip;       // 지글지글 소리

    [Header("배경음악")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private bool playBgmOnStart = true;
    [SerializeField] private bool loopBgm = true;

    private void Awake()
    {
        // 이미 다른 AudioManager가 있으면 제거
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        // 일반 효과음 소스 자동 생성
        if (sfxSource == null)
        {
            GameObject obj = new GameObject("SFXSource");
            obj.transform.SetParent(transform);

            sfxSource = obj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        // 프라이어 루프 소스 자동 생성
        if (fryerLoopSource == null)
        {
            GameObject obj = new GameObject("FryerLoopSource");
            obj.transform.SetParent(transform);

            fryerLoopSource = obj.AddComponent<AudioSource>();
            fryerLoopSource.playOnAwake = false;
            fryerLoopSource.loop = true;
        }

        // 배경음악 소스 자동 생성
        if (bgmSource == null)
        {
            GameObject obj = new GameObject("BGMSource");
            obj.transform.SetParent(transform);

            bgmSource = obj.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = loopBgm;
        }

        if (playBgmOnStart)
            PlayBgm();
    }

    // 공용 1회 효과음 재생
    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null)
            return;

        if (clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    // 닭 죽는 소리
    public void PlayChickenDead()
    {
        PlaySfx(chickenDeadClip);
    }

    // 무기 휘두르는 소리
    public void PlayWeaponSwing()
    {
        PlaySfx(weaponSwingClip);
    }

    // 플레이어 인벤토리 입출력 소리
    public void PlayPop()
    {
        PlaySfx(inventoryActionClip);
    }

    // 프라이어 루프 시작
    // 이미 재생 중이면 중복 재생하지 않음
    public void StartFryerLoop()
    {
        if (fryerLoopSource == null)
            return;

        if (fryerLoopClip == null)
            return;

        if (fryerLoopSource.isPlaying)
            return;

        fryerLoopSource.clip = fryerLoopClip;
        fryerLoopSource.loop = true;
        fryerLoopSource.Play();
    }

    // 프라이어 루프 정지
    public void StopFryerLoop()
    {
        if (fryerLoopSource == null)
            return;

        if (!fryerLoopSource.isPlaying)
            return;

        fryerLoopSource.Stop();
    }

    // 배경음악 재생
    public void PlayBgm()
    {
        if (bgmSource == null)
            return;

        if (bgmClip == null)
            return;

        if (bgmSource.isPlaying && bgmSource.clip == bgmClip)
            return;

        bgmSource.clip = bgmClip;
        bgmSource.loop = loopBgm;
        bgmSource.Play();
    }

    // 배경음악 정지
    public void StopBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
    }
}