using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mirror;
using UnityEngine;

public class FlagHandler : NetworkBehaviour
{
    public Transform flagHolder;
    [SyncVar]
    [SerializeField]
    private bool isBot;

    [SyncVar]
    [SerializeField]
    private string botTeam;

    public string BotTeam => botTeam;
    [Server]
    public void SetBotTeam(string team)
    {
        isBot = true;
        botTeam = team;
    }
    public string Team
    {
        get
        {
            if (isBot)
                return botTeam;

            return GetComponent<player>()?.MatchPlayer?.Team;
        }
    }
    public FlagAudioManager audioManager;
    public player _player;
    public static FlagHandler local;

    private void Start()
    {
        audioManager = FindAnyObjectByType<FlagAudioManager>();
        _player = GetComponent<player>();
        if (audioManager == null)
        {
            Debug.LogError("[FlagHandler] AudioManager is NULL! Sound will not play.");
        }
        else
        {
            Debug.Log("[FlagHandler] AudioManager successfully assigned.");
        }
    }

    [SyncVar(hook = nameof(OnHeldFlagChanged))]
    public GameObject heldFlag = null;

    private void OnHeldFlagChanged(GameObject oldFlag, GameObject newFlag)
    {
        if (oldFlag != null)
        {
            oldFlag.transform.SetParent(null, true);
            oldFlag.GetComponent<Rigidbody>().isKinematic = false;
        }

        if (newFlag != null)
        {
            newFlag.transform.SetParent(flagHolder, false);
            newFlag.transform.localPosition = Vector3.zero;
            newFlag.transform.localRotation = Quaternion.identity;
            newFlag.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (heldFlag == null)
        {
            if (
                other.CompareTag("BlueFlag")
                && Team == "Red"
                && GameManager.instance.canPickUpBlueFlag
                && !_player.isDead
            )
            {
                CmdPickFlag(other.gameObject);
            }
            else if (
                other.CompareTag("RedFlag")
                && Team == "Blue"
                && GameManager.instance.canPickUpRedFlag
                && !_player.isDead
            )
            {
                CmdPickFlag(other.gameObject);
            }
        }

        if (
            other.CompareTag("BlueFlag")
            && Team == "Blue"
            && GameManager.instance.isStolenBlue
            && GameManager.instance.canPickUpBlueFlag
        )
        {
            CmdReturnFlag(other.gameObject, "Blue");
        }
        else if (
            other.CompareTag("RedFlag")
            && Team == "Red"
            && GameManager.instance.isStolenRed
            && GameManager.instance.canPickUpRedFlag
        )
        {
            CmdReturnFlag(other.gameObject, "Red");
        }
        if (heldFlag != null)
        {
            if (
                other.CompareTag("BL")
                && Team == "Blue"
                && heldFlag.CompareTag("RedFlag")
                && !GameManager.instance.isStolenBlue
            )
            {
                CmdCaptureFlag("Blue");
            }
            else if (
                other.CompareTag("RL")
                && Team == "Red"
                && heldFlag.CompareTag("BlueFlag")
                && !GameManager.instance.isStolenRed
            )
            {
                CmdCaptureFlag("Red");
            }
        }
    }

    [Server]
    public void CmdPickFlag(GameObject flag)
    {
        if (flag == null || heldFlag != null)
            return;

        heldFlag = flag;
        flag.transform.SetParent(flagHolder);
        flag.transform.localPosition = Vector3.zero;
        flag.transform.localRotation = Quaternion.identity;
        flag.GetComponent<Rigidbody>().isKinematic = true;

        if (flag.CompareTag("RedFlag"))
        {
            SetFlagStolen("Red", true);
            GameManager.instance.canPickUpRedFlag = false;
        }
        else if (flag.CompareTag("BlueFlag"))
        {
            SetFlagStolen("Blue", true);
            GameManager.instance.canPickUpBlueFlag = false;
        }

        RpcPickFlag(flag);
        Debug.Log($"[FlagHandler] Flag picked up: {flag.tag}");

        if (audioManager != null)
        {
            Debug.Log($"[FlagHandler] Playing sound for flag pickup ({Team})");
            audioManager.RpcPlayFlagSound("FlagTaken", Team);
        }
        else
        {
            Debug.LogError(
                "[FlagHandler] AudioManager is NULL when attempting to play FlagTaken sound."
            );
        }
    }

    [ClientRpc]
    private void RpcPickFlag(GameObject flag)
    {
        if (flag == null)
            return;

        flag.transform.SetParent(flagHolder);
        flag.transform.localPosition = Vector3.zero;
        flag.transform.localRotation = Quaternion.identity;
        flag.GetComponent<Rigidbody>().isKinematic = true;
    }

    [Server]
    public void CmdReturnFlag(GameObject flag, string flagColor)
    {
        SetFlagStolen(flagColor, false);

        flag.transform.SetParent(null);
        flag.transform.position = flag.GetComponent<Flag>().originalPosition;
        flag.GetComponent<Rigidbody>().isKinematic = false;

        if (heldFlag == flag)
            heldFlag = null;

        RpcReturnFlag(flag);
        Debug.Log($"[FlagHandler] Flag returned: {flagColor}");

        if (audioManager != null)
        {
            Debug.Log($"[FlagHandler] Playing sound for flag return ({Team})");
            audioManager.RpcPlayFlagSound("FlagReturned", Team);
        }
        else
        {
            Debug.LogError(
                "[FlagHandler] AudioManager is NULL when attempting to play FlagReturned sound."
            );
        }
    }

    [ClientRpc]
    private void RpcReturnFlag(GameObject flag)
    {
        flag.transform.SetParent(null);
        flag.transform.position = flag.GetComponent<Flag>().originalPosition;
        flag.GetComponent<Rigidbody>().isKinematic = false;
    }

    [Server]
    public void ServerDropFlag(Vector3 dropPosition)
    {
        if (heldFlag == null)
        {
            Debug.Log("[FlagHandler] CmdDropFlag called but no flag is held.");
            return;
        }

        Debug.Log($"[FlagHandler] Dropping flag: {heldFlag.name}");

        GameObject flagToDrop = heldFlag;
        heldFlag = null;
        flagToDrop.transform.SetParent(null);
        flagToDrop.transform.position = dropPosition;
        flagToDrop.GetComponent<Rigidbody>().isKinematic = false;
        StartCoroutine(PickupCooldown());
        RpcDropFlag(flagToDrop, dropPosition);

        if (audioManager != null)
        {
            Debug.Log($"[FlagHandler] Playing sound for flag drop ({Team})");
            audioManager.RpcPlayFlagSound("FlagDropped", Team);
        }
        else
        {
            Debug.LogError(
                "[FlagHandler] AudioManager is NULL when attempting to play FlagDropped sound."
            );
        }
    }

    [ClientRpc]
    private void RpcDropFlag(GameObject flag, Vector3 dropPosition)
    {
        if (flag == null)
        {
            Debug.Log("[FlagHandler] RpcDropFlag received null flag!");
            return;
        }

        Debug.Log($"[FlagHandler] RpcDropFlag received: {flag.name}");

        flag.transform.SetParent(null);
        flag.transform.position = dropPosition;
        flag.GetComponent<Rigidbody>().isKinematic = false;
        if (flag.CompareTag("BlueFlag"))
        {
            GameManager.instance.isDroppedBlue = true;
        }
        else
        {
            GameManager.instance.isDroppedRed = true;
        }
        StartCoroutine(PickupCooldown());
    }

    [Server]
    public void CmdCaptureFlag(string team)
    {
        if (heldFlag == null)
            return;

        heldFlag = null;

        foreach (GameObject flag in GameObject.FindGameObjectsWithTag("RedFlag"))
        {
            flag.transform.SetParent(null, true);
            flag.transform.position = flag.GetComponent<Flag>().originalPosition;
            flag.GetComponent<Rigidbody>().isKinematic = false;
        }

        foreach (GameObject flag in GameObject.FindGameObjectsWithTag("BlueFlag"))
        {
            flag.transform.SetParent(null, true);
            flag.transform.position = flag.GetComponent<Flag>().originalPosition;
            flag.GetComponent<Rigidbody>().isKinematic = false;
        }

        GameManager.instance.SetFlagStolen("Red", false);
        GameManager.instance.SetFlagStolen("Blue", false);
        GameManager.instance.SetFlagDropped("Red", false);
        GameManager.instance.SetFlagDropped("Blue", false);
        GameManager.instance.canPickUpRedFlag = true;
        GameManager.instance.canPickUpBlueFlag = true;

        RpcResetFlags();

        WinningConditions.Instance.AddScore(team);

        if (audioManager != null)
            audioManager.RpcPlayFlagSound("FlagCaptured", Team);
    }

    [ClientRpc]
    private void RpcResetFlags()
    {
        foreach (GameObject flag in GameObject.FindGameObjectsWithTag("RedFlag"))
        {
            flag.transform.SetParent(null, true);
            flag.transform.position = flag.GetComponent<Flag>().originalPosition;
            flag.GetComponent<Rigidbody>().isKinematic = false;
        }

        foreach (GameObject flag in GameObject.FindGameObjectsWithTag("BlueFlag"))
        {
            flag.transform.SetParent(null, true);
            flag.transform.position = flag.GetComponent<Flag>().originalPosition;
            flag.GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    [Server]
    private void SetFlagStolen(string flagColor, bool state)
    {
        if (GameManager.instance == null)
            return;


        GameManager.instance.SetFlagStolen(flagColor, state);
    }

    private IEnumerator WaitAndSendFlagStolen(string teamName, bool stolen)
    {
        while (!NetworkClient.ready)
        {
            Debug.Log("[FlagHandler] Waiting for NetworkClient to be ready...");
            yield return new WaitForSeconds(0.5f);
        }

        GameManager.instance.SetFlagStolen(teamName, stolen);
    }

    private IEnumerator PickupCooldown()
    {
        GameManager.instance.canPickUpRedFlag = false;
        GameManager.instance.canPickUpBlueFlag = false;
        yield return new WaitForSeconds(0.1f);
        GameManager.instance.canPickUpRedFlag = true;
        GameManager.instance.canPickUpBlueFlag = true;
    }

    public static Transform GetFlagTransform(string team)
    {
        if (team == "Red")
            return GameObject.FindWithTag("RedFlag")?.transform;
        else if (team == "Blue")
            return GameObject.FindWithTag("BlueFlag")?.transform;

        return null;
    }

    public bool HasFlag()
    {
        if (heldFlag != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
