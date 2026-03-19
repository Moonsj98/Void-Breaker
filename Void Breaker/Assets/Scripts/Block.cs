using UnityEngine;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    public enum BlockType
    {
        Normal,
        Core,
        Infinite,
        BuffCard,
        Debuff
    }

    public BlockType blockType;
    public int hp;

    [Header("Debuff")]
    public StatType debuffStatType = StatType.Cost;
    public int debuffAmount = 1;

    [Header("References")]
    public Text valueText;
    public SpriteRenderer spriteRenderer;
    public Animator animator;

    GameManager GM;

    public void Setup(GameManager gameManager, BlockType type, int value)
    {
        GM = gameManager;
        blockType = type;
        hp = value;
        RefreshView();
    }

    public void SetupDebuff(GameManager gameManager, int value, StatType statType, int amount)
    {
        GM = gameManager;
        blockType = BlockType.Debuff;
        hp = value;
        debuffStatType = statType;
        debuffAmount = Mathf.Max(1, amount);
        RefreshView();
    }

    void Start()
    {
        if (GM == null)
            GM = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (valueText == null)
        {
            Text txt = GetComponentInChildren<Text>();
            if (txt != null)
                valueText = txt;
        }

        RefreshView();
    }

    public void OnHit(int damage)
    {
        if (GM == null) return;

        switch (blockType)
        {
            case BlockType.Normal:
                HitNormalBlock(damage);
                break;

            case BlockType.Core:
                GM.GameClear();
                break;

            case BlockType.Infinite:
                HitInfiniteBlock();
                break;

            case BlockType.BuffCard:
                HitBuffCard();
                break;

            case BlockType.Debuff:
                HitDebuffBlock(damage);
                break;
        }
    }

    void HitNormalBlock(int damage)
    {
        hp -= damage;

        PlayBlockSound();

        if (hp > 0)
        {
            RefreshView();

            if (animator != null)
                animator.SetTrigger("shock");
        }
        else
        {
            if (GM.P_ParticleRed != null)
                Destroy(Instantiate(GM.P_ParticleRed, transform.position, Quaternion.identity), 1f);

            Destroy(gameObject);
        }
    }

    void HitInfiniteBlock()
    {
        PlayBlockSound();

        if (animator != null)
            animator.SetTrigger("shock");
    }

    void HitBuffCard()
    {
        GM.OpenBuffSelection();
        Destroy(gameObject);
    }

    void HitDebuffBlock(int damage)
    {
        hp -= damage;
        PlayBlockSound();

        if (hp > 0)
        {
            RefreshView();

            if (animator != null)
                animator.SetTrigger("shock");
            return;
        }

        GM.ModifyStat(debuffStatType, -Mathf.Abs(debuffAmount));

        if (GM.P_ParticleRed != null)
            Destroy(Instantiate(GM.P_ParticleRed, transform.position, Quaternion.identity), 1f);

        Destroy(gameObject);
    }

    void PlayBlockSound()
    {
        if (GM.S_Block == null) return;

        for (int i = 0; i < GM.S_Block.Length; i++)
        {
            if (GM.S_Block[i] == null) continue;
            if (GM.S_Block[i].isPlaying) continue;

            GM.S_Block[i].Play();
            break;
        }
    }

    string GetDebuffText()
    {
        string prefix = "↓" + debuffAmount;

        switch (debuffStatType)
        {
            case StatType.Cost:
                return prefix + "C";
            case StatType.AttackPower:
                return prefix + "ATK";
            case StatType.RemainShotCount:
                return prefix + "SHOT";
        }

        return prefix;
    }

    public void RefreshView()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        switch (blockType)
        {
            case BlockType.Normal:
                if (spriteRenderer != null && GM != null && GM.normalBlockSprite != null)
                    spriteRenderer.sprite = GM.normalBlockSprite;

                if (valueText != null)
                    valueText.text = hp.ToString();

                if (spriteRenderer != null && GM != null)
                {
                    Color curColor;
                    if (GM.blockColor != null && GM.blockColor.Length > 0)
                    {
                        if (hp >= GM.cost + 1)
                            curColor = GM.blockColor[0];
                        else if (hp == GM.cost)
                            curColor = GM.blockColor[Mathf.Min(3, GM.blockColor.Length - 1)];
                        else
                            curColor = GM.blockColor[GM.blockColor.Length - 1];

                        spriteRenderer.color = curColor;
                    }
                }
                break;

            case BlockType.Core:
                if (spriteRenderer != null && GM != null && GM.coreBlockSprite != null)
                    spriteRenderer.sprite = GM.coreBlockSprite;

                if (valueText != null)
                    valueText.text = "";

                if (spriteRenderer != null)
                    spriteRenderer.color = Color.white;
                break;

            case BlockType.Infinite:
                if (spriteRenderer != null && GM != null && GM.infiniteBlockSprite != null)
                    spriteRenderer.sprite = GM.infiniteBlockSprite;

                if (valueText != null)
                    valueText.text = "∞";

                if (spriteRenderer != null)
                    spriteRenderer.color = Color.white;
                break;

            case BlockType.BuffCard:
                if (valueText != null)
                    valueText.text = "";

                if (spriteRenderer != null)
                    spriteRenderer.color = Color.white;
                break;

            case BlockType.Debuff:
                if (spriteRenderer != null && GM != null && GM.normalBlockSprite != null)
                    spriteRenderer.sprite = GM.normalBlockSprite;

                if (valueText != null)
                    valueText.text = hp + "\n" + GetDebuffText();

                if (spriteRenderer != null && GM != null)
                    spriteRenderer.color = GM.debuffBlockColor;
                break;
        }
    }
}