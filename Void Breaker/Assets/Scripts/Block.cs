using UnityEngine;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    public enum BlockType
    {
        Normal,
        Core,
        Infinite,
        BuffCard
    }

    public BlockType blockType;
    public int hp;

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

    public void RefreshView()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        switch (blockType)
        {
            case BlockType.Normal:
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
                if (valueText != null)
                    valueText.text = "CORE";

                if (spriteRenderer != null && GM != null)
                    spriteRenderer.color = GM.coreColor;
                break;

            case BlockType.Infinite:
                if (valueText != null)
                    valueText.text = "∞";

                if (spriteRenderer != null && GM != null)
                    spriteRenderer.color = GM.infiniteBlockColor;
                break;

            case BlockType.BuffCard:
                if (valueText != null)
                    valueText.text = "CARD";

                if (spriteRenderer != null && GM != null)
                    spriteRenderer.color = GM.buffCardColor;
                break;
        }
    }
}