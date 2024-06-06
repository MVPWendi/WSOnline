using Assets.UIs;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private GameObject Menu;
    [SerializeField]
    private GameObject JoinMenu;
    [SerializeField]
    private GameObject HostMenu;

    private MenuState State = MenuState.HostMenu;

    [SerializeField]
    private Button HostButton;
    [SerializeField]
    private Button JoinButton;
    [SerializeField]
    private Button MainButton;
    [SerializeField]
    public Button ExitButton;


    [SerializeField]
    public Button HostConfirm;

    [SerializeField]
    public Button JoinConfirm;

    [SerializeField]
    public TMP_InputField JoinIP;

    private string JoinIPText;

    private bool IsHidden = false;
    void Start()
    {
        ChangeState(MenuState.HostMenu);
        JoinButton.onClick.AddListener(() => OnButtonClick(MenuState.JoinMenu));
        HostButton.onClick.AddListener(() => OnButtonClick(MenuState.HostMenu));
        World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<GameUISystem>().SetUIReferences(this);
    }
    private void ChangeState(MenuState state)
    {
        State = state;
        switch (State)
        {
            case MenuState.JoinMenu:
                JoinMenu.SetActive(true);
                HostMenu.SetActive(false);
                break;
            case MenuState.HostMenu:
                HostMenu.SetActive(true);
                JoinMenu.SetActive(false);
                break;
        }
    }
    
    public void OnText()
    {
        JoinIPText = JoinIP.text;
        Debug.Log($"Entered IPAdress: {JoinIPText}");
    }
    private void OnButtonClick(MenuState state)
    {
        ChangeState (state);
    }


    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {         
            IsHidden = !IsHidden;
            Menu.SetActive(IsHidden);
        }
    }
}
public enum MenuState
{
    JoinMenu = 2,
    HostMenu = 3
}
