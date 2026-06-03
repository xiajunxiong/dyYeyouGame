using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System;
public enum EnemyState
{
	/// <summary>
	/// 待机状态
	/// </summary>
	Idle,
	/// <summary>
	/// 行走状态
	/// </summary>
	Walk,
	/// <summary>
	/// 攻击状态
	/// </summary>
	Attack,
    /// <summary>
	/// 选择目标状态
	/// </summary>
	SelectTarget,
	/// <summary>
	/// 狂暴攻击
	/// </summary>
    FrenziedAttack1
}

public class Enemy : MonoBehaviour
{
    public EnemyState currentState;

	public float moveSpeed = 2f;

	// 敌人的攻击次数
	public int attackCount;
    // 敌人的当前攻击次数
	public int currentAttackCount;
    // 敌人的最大攻击力
    public int maxAttackCount;
    // 敌人的最小攻击力
    public int minAttackCount;
	// 筛子UI
	public GameObject diceUIGameObject;
	public Animator diceAnim;
    // 房间死亡
    public GameObject roomDeathAnimNode;
    public float roomDeathAnimTime = 5f;
    // 黑夜图片
    public GameObject nightGameObject;
	public float nightTime = 2f;
    // 开局预警动画
    public GameObject startEarlyWarningAnim;
    public float earlyWarningTime = 5f;
    // 狂暴攻击1
    public int frenzyAttack1;
	public int frenzyAttack2;
	public int frenzyAttack3;

	// 当前选择的目标房间
	public Room targetRoom;
    // 是否进入选择状态
    public bool isSelectingTarget = false;
	[Header("设置不同状态的图片大小")]
    public Vector3 idleZise = Vector3.one;
	public Vector3 walkZise = Vector3.one;
    public Vector3 attackZise = Vector3.one;
    public Vector3 frenziedAttack1Zise = Vector3.one;
    private AudioSource audioSource;
    private Animator anim;
    private SpriteRenderer sprite;

    public AudioClip audioClip0;
    public AudioClip audioClip1;
    //public static Enemy ins;
    //private void Awake()
    //{
    //    ins = this;
    //}
    private void Start()
	{
		anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        string fullFolderPath0 = Path.Combine(Application.streamingAssetsPath, "音乐/挠门.mp3");
        string fullFolderPath1 = Path.Combine(Application.streamingAssetsPath, "音乐/破门瞬间.mp3");
        StartCoroutine(LoadSingleAudio(fullFolderPath0, (clip) => { audioClip0 = clip; }));
        StartCoroutine(LoadSingleAudio(fullFolderPath1, (clip) => { audioClip1 = clip; }));
        currentState = EnemyState.Idle;
        currentAttackCount = 0;
        attackCount = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.ghostAttackCount;
		minAttackCount = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.ghostMinAttack;
		maxAttackCount = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.ghostMaxAttack;
		frenzyAttack1 = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.zombieRageAttack1;
		frenzyAttack2 = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.zombieRageAttack2;
		frenzyAttack3 = DY_JsonDataManager.Instance.localGameInitData.gameConfiguration.zombieRageAttack3;
        nightGameObject.SetActive(false);
        sprite = GetComponent<SpriteRenderer>();
        sprite.enabled = false;
        GameTime.ins.OnTimeStateChanged += OnTimeChanged;
    }

    private void OnTimeChanged(TimeState state)
    {
        if(state == TimeState.Day)
        {
            StopAttack();
            sprite.enabled = false;
        }
        else
        {
            sprite.enabled = true;
        }
    }

    IEnumerator LoadSingleAudio(string filePath, Action<AudioClip> onLoaded)
    {
        string url = $"file:///{filePath}";
        using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                onLoaded?.Invoke(clip);
            }
            else
            {
                Debug.LogError("加载失败：" + req.error);
            }
        }
    }

    void Update()
	{
		switch (currentState)
		{
			case EnemyState.Idle:
				Idle();
				break;
			case EnemyState.Walk:
				Walk();
                break;
			case EnemyState.Attack:
				Attack();
				break;
			case EnemyState.SelectTarget:
				
                break;

        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            startAttack();
        }
    }
    // 敌人状态切换函数
    public void ChangeState(EnemyState newState)
	{
        currentState = newState;
		switch (newState)
		{
			case EnemyState.Idle:
                transform.localScale = idleZise;
                anim.Play("动态效果");
                break;
            case EnemyState.SelectTarget:
				transform.localScale = idleZise;
                anim.Play("动态效果");
                SelectTarget();
                break;
			case EnemyState.Walk:
                transform.localScale = walkZise;

                if (transform.position.x < targetRoom.doorObj.transform.position.x)
				{
					GetComponent<SpriteRenderer>().flipX = true;
				}
				else
				{
                    GetComponent<SpriteRenderer>().flipX = false;
                }
				anim.Play("向左走");
				break;
			case EnemyState.Attack:
                transform.localScale = attackZise;

                anim.Play("攻击");
				break;
			case EnemyState.FrenziedAttack1:
                transform.localScale = frenziedAttack1Zise;

                anim.Play("狂暴攻击1");
				break;
		}
    }

	public void startAttack()
	{
        //sprite.enabled = true;
        StartCoroutine(DelayAttack());
    }

    IEnumerator DelayAttack()
    {
        nightGameObject.SetActive(true);
        Image nightImage = nightGameObject.GetComponent<Image>();
        float startTime = Time.time;
        Color startColor = nightImage.color;
        startColor.a = 0f;
        nightImage.color = startColor;

        while (nightImage.color.a < 1f)
        {
            float progress = Mathf.Clamp01((Time.time - startTime) / nightTime);
            Color newColor = nightImage.color;
            newColor.a = progress;
            nightImage.color = newColor;
            yield return null;
        }
        Color finalColor = nightImage.color;
        finalColor.a = 1f;
        nightImage.color = finalColor;
        startEarlyWarningAnim.SetActive(true);
        yield return new WaitForSeconds(earlyWarningTime);
        startEarlyWarningAnim.SetActive(false);

        isSelectingTarget = true;
    }

    #region 敌人行为函数
    public void Idle()
	{
		if (isSelectingTarget)
		{
            ChangeState(EnemyState.SelectTarget);
        }

    }

    // 停止攻击
    public void StopAttack()
    {
        StopAllCoroutines();

        nightGameObject.SetActive(false);
        startEarlyWarningAnim.SetActive(false);
        diceUIGameObject.SetActive(false);
        roomDeathAnimNode.SetActive(false);

        isSelectingTarget = false;
        targetRoom = null;
        ChangeState(EnemyState.Idle);
    }

    // 重置敌人所有状态为初始状态
    public void ResetEnemy()
    {
        StopAllCoroutines();

        nightGameObject.SetActive(false);
        startEarlyWarningAnim.SetActive(false);
        diceUIGameObject.SetActive(false);
        roomDeathAnimNode.SetActive(false);

        isSelectingTarget = false;
        currentAttackCount = 0;
        targetRoom = null;
        diceAnim.Play("Idle");
        sprite.enabled = false;
        transform.localScale = idleZise;
        if (anim != null)
        {
            anim.Play("动态效果");
        }

        ChangeState(EnemyState.Idle);

    }

    public void Walk()
    {
        if (targetRoom == null)
        {
            ChangeState(EnemyState.SelectTarget);
            return;
        }
        Vector3 targetPosition = targetRoom.doorObj.transform.position;
        targetPosition.z -= 1.5f;
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.1f)
        {
			
            ChangeState(UnityEngine.Random.value > 0.5f ? EnemyState.Attack : EnemyState.FrenziedAttack1);
			
        }
        Vector3 direction = (targetPosition - transform.position).normalized;
        //if (direction != Vector3.zero)
        //{
        //    Quaternion targetRotation = Quaternion.LookRotation(direction);
        //    transform.rotation = targetRotation;
        //}

        transform.position = newPos;
    }

    public void Attack()
	{
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }
	public void SelectTarget()
	{

		var room = RoomManager.ins.GetRandomRoom();
		if (room == null)
		{
			ChangeState(EnemyState.Idle);
			isSelectingTarget =false;
            GameTime.ins.ResetGame();
            GameTime.ins.StartDayTime();
            return;
        }
		StartCoroutine(DiceState(room));
    }

	IEnumerator DiceState(Room room)
    {
        diceUIGameObject.SetActive(true);
		yield return new WaitForSeconds(1f);
        diceAnim.Play(room.doorObj.name);
        yield return new WaitForSeconds(2.5f);
        diceUIGameObject.SetActive(false);
        targetRoom = room;
        ChangeState(EnemyState.Walk);
        diceAnim.Play("Idle");
    }

    // 攻击动画回调
    public void OnAttackAnimationEvent()
	{
		if(targetRoom != null && targetRoom.player.Count > 0)
		{
			int attackDamage = UnityEngine.Random.Range(minAttackCount, maxAttackCount + 1);
			RoomManager.ins.ReduceHpForRoom(targetRoom, attackDamage);
            audioSource.clip = audioClip0;
            audioSource.Play();
            if (targetRoom.hp <= 0)
            {
                isSelectingTarget = false;
                roomDeathAnimNode.SetActive(true);
                ChangeState(EnemyState.Idle);
                audioSource.clip = audioClip1;
                audioSource.Play();
                GameTime.ins.isTimePaused = true;
                Invoke("RoomDie", roomDeathAnimTime);
            }
            currentAttackCount++;
			if (currentAttackCount >= attackCount)
			{
                currentAttackCount = 0;
                ChangeState(EnemyState.SelectTarget);
            }
        }
		else
		{
            ChangeState(EnemyState.SelectTarget);
        }
    }

    // 房间被破坏
    private void RoomDie()
    {
        roomDeathAnimNode.SetActive(false);
        isSelectingTarget = true;
        GameTime.ins.isTimePaused = false;
    }

    #endregion

	// 游戏结束
	public void GameOver()
	{

    }
}
