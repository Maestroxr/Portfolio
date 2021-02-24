using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dice : MonoBehaviour
{
    public delegate void DieRolling(float seconds);
    public event DieRolling DieRollingEvent;
    public delegate void DieCast(int reuslt);
    public event DieCast DieCastEvent;


    public void RollDie(float rollForSeconds)
    {
        DieRollingEvent(rollForSeconds);
    }


    public int CastDie()
    {
        int result = Random.Range(1, 6);
        DieCastEvent(result);
        return result;
    }
}
