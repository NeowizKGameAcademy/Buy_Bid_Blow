using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BuyBidBlow
{
    public class InteractionHUDController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private DetectObject detect;

        [Header("Texts")]
        [SerializeField] private GameObject Prompt;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private TextMeshProUGUI InteractionText;

        private void Update()
        {
            if (detect.isDetected)
            {
                Prompt.SetActive(true);

                switch (detect.detectingLayer)
                {
                    case 7:
                        keyText.text = "E";
                        InteractionText.text = "Trade";
                        break;
                }
            }
            else
            {
                Prompt.SetActive(false);
            }
        }
    }
}
