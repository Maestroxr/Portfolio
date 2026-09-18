using UnityEngine;

namespace Portfolio.Monopoly
{
    public class Dice : MonoBehaviour
    {
        public delegate void DieRolling(float seconds);
        public event DieRolling DieRollingEvent;
        public delegate void DieCast(int reuslt);
        public event DieCast DieCastEvent;


        public void RollDie(float rollForSeconds)
        {
            DieRollingEvent?.Invoke(rollForSeconds);
        }


        public int CastDie()
        {
            int result = Random.Range(1, 7);
            DieCastEvent?.Invoke(result);
            return result;
        }
    }
}
