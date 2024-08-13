using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IPlayerState
{
    void EnterState(Move player);
    void UpdateState(Move player);
    void ExitState(Move player);
}

public class CharaAnimation : MonoBehaviour
{
    Animator animator;
    void Awake()
    {
        animator = GetComponent<Animator>();
    }
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
