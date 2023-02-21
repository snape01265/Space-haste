using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;
using UnityEngine;

public class Booster : MonoBehaviour
{
    SpaceshipController spaceshipController;

    [Header("Reference and Keys")]
    [SerializeField] private VisualEffect boosterImpactVFX;
    [SerializeField] private VisualEffect boosterLoopVFX;
    [SerializeField] private KeyCode boosterKey = KeyCode.LeftShift;

    [Space]
    [Header("Booster Settings")]
    [SerializeField] private float _boosterSpeed = 400f;
    [SerializeField] private float _normalSpeed = 200f;
    [SerializeField] private float _warpRate = 0.02f;
    [SerializeField] private bool _instantSpeed = true;

    private bool _isBoosterActive;

    private void Awake()
    {
        spaceshipController = GetComponent<SpaceshipController>();
    }

    
    void Start()
    {
        boosterImpactVFX.Stop();
        boosterLoopVFX.Stop();
        boosterLoopVFX.SetFloat("WarpAmount", 0);
    }

    
    void Update()
    {
        if(Input.GetKeyDown(boosterKey))
        {
            _isBoosterActive = true;
            boosterImpactVFX.Play();
            StartCoroutine(ActivateBooster());
        }
        else if (Input.GetKeyUp(boosterKey))
        {
            _isBoosterActive= false;
            boosterImpactVFX.Stop();
            StartCoroutine(ActivateBooster());
        }
    }
    
    IEnumerator ActivateBooster()
    {
        if (_isBoosterActive)
        {
            yield return new WaitForSeconds(0.5f);
            if (_instantSpeed)
            {
                spaceshipController.ChangeSpeedInstantly(_boosterSpeed);
            }
            spaceshipController.ChangeSpeed(_boosterSpeed);
            boosterLoopVFX.Play();

            float _warpAmount = boosterLoopVFX.GetFloat("WarpAmount");
            while(_warpAmount < 1) 
            {
                _warpAmount += _warpRate;
                boosterLoopVFX.SetFloat("WarpAmount", _warpAmount);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
            spaceshipController.ChangeSpeed(_normalSpeed);

            float _warpAmount = boosterLoopVFX.GetFloat("WarpAmount");
            while (_warpAmount >= _warpRate)
            {
                _warpAmount -= _warpRate;
                boosterLoopVFX.SetFloat("WarpAmount", _warpAmount);
                yield return null;
            }
            //boosterLoopVFX.SetFloat("WarpAmount", 0);

            boosterLoopVFX.Stop();
        }
    }
}
