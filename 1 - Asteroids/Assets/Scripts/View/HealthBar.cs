using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Portfolio
{
    // Solely created for changing appearance of text
    public class HealthBar : ProgressBar
    {
        protected override void UpdateValue(float val)
        {
            base.UpdateValue(val);
            txtTitle.text = Title + "\n" + (int)(val * 10f);
        }
    }
}