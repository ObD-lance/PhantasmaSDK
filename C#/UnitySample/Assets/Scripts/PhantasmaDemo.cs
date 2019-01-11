﻿using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.SDK;
using Phantasma.VM.Utils;

/*
 * Phantasma Spook
 * https://github.com/phantasma-io/PhantasmaSpook/tree/master/Docs
 */

public class PhantasmaDemo : MonoBehaviour
{
    private const string _SERVER_ADDRESS = "http://localhost:7077/rpc";

    public KeyPair Key { get; private set; }

    private enum EWALLET_STATE
    {
        INIT,
        SYNC,
        UPDATE,
        READY
    }

    /*
    public enum EventKind
    {
        ChainCreate,
        TokenCreate,
        TokenSend,
        TokenReceive,
        TokenMint,
        TokenBurn,
        TokenEscrow,
        AddressRegister,
        FriendAdd,
        FriendRemove,
        GasEscrow,
        GasPayment,
    }
     */

    public Market       market;
    public List<Sprite> carImages;

    private EWALLET_STATE   _state = EWALLET_STATE.INIT;
    private decimal         _balance;

    public API          PhantasmaApi    { get; private set; }
    public List<Car>    MyCars          { get; set; }

    private static PhantasmaDemo _instance;
    public static PhantasmaDemo Instance
    {
        get { _instance = _instance == null ? FindObjectOfType(typeof(PhantasmaDemo)) as PhantasmaDemo : _instance; return _instance; }
    }

    private void Awake()
    {
        MyCars = new List<Car>();
    }

    private void Start ()
    {
        //GetAccount("P2f7ZFuj6NfZ76ymNMnG3xRBT5hAMicDrQRHE4S7SoxEr"); //TEST

        PhantasmaApi = new API("http://localhost:7077/rpc");
    }

    private IEnumerator SyncBalance()
    {
        yield return new WaitForSeconds(2);
        //var balances = api.GetAssetBalancesOf(this.keys);
        //balance = balances.ContainsKey(assetSymbol) ? balances[assetSymbol] : 0;
        _state = EWALLET_STATE.UPDATE;
    }

    private void Update () {

        switch (_state)
        {
            case EWALLET_STATE.INIT:
                {
                    _state = EWALLET_STATE.SYNC;
                    StartCoroutine(SyncBalance());
                    break;
                }

            case EWALLET_STATE.UPDATE:
                {
                    _state = EWALLET_STATE.READY;
                    CanvasManager.Instance.accountMenu.SetBalance(_balance.ToString(CultureInfo.InvariantCulture));
                    break;
                }
        }		
	}

    public void Login(string address)
    {
        GetAccount(address);
    }

    private void LoggedIn(string address)
    {
        //Debug.Log("logged in");

        //var address = "L2LGgkZAdupN2ee8Rs6hpkc65zaGcLbxhbSDGq8oh6umUxxzeW25";
        //var addressBytes = Encoding.ASCII.GetBytes(address);

        Key = KeyPair.FromWIF(address);

        market.FillMarket();

        CanvasManager.Instance.SetAddress(address);
        CanvasManager.Instance.CloseLogin();
    }

    public void LogOut()
    {
        //Debug.Log("logged out");

        // TODO something else here ?

        CanvasManager.Instance.ClearAddress();
        CanvasManager.Instance.OpenLogin();
    }

    #region Blockchain calls

    public void GetAccount(string address)
    {
        //P2f7ZFuj6NfZ76ymNMnG3xRBT5hAMicDrQRHE4S7SoxEr

        Debug.Log("Get account: " + address);

        StartCoroutine(PhantasmaApi.GetAccount(address, result =>
            {
                CanvasManager.Instance.accountMenu.SetBalance("Name: " + result.name);

                foreach (var balanceSheetResult in result.balances)
                {
                    var amount = decimal.Parse(balanceSheetResult.amount) / (decimal) Mathf.Pow(10f, 8);
                    CanvasManager.Instance.accountMenu.AddBalanceEntry("Chain: " + balanceSheetResult.chain + " - " + amount + " " + balanceSheetResult.symbol);

                    //Debug.Log("balance: " + balanceSheetResult.Chain + " | " + balanceSheetResult.Amount);
                }

                LoggedIn(address);

            },
            (errorType, errorMessage) => { CanvasManager.Instance.loginMenu.SetLoginError(errorType + " - " + errorMessage); }
        ));
    }

    //public void RegisterName()
    //{
    //    StartCoroutine(RegisterNameCoroutine());
    //}

    //private IEnumerator RegisterNameCoroutine()
    //{
    //    var myData = "{ \"jsonrpc\":\"2.0\",\"method\":\"getAccount\",\"params\":[\"P2f7ZFuj6NfZ76ymNMnG3xRBT5hAMicDrQRHE4S7SoxEr\"],\"id\":1}";
    //    var www = UnityWebRequest.Post(_SERVER_ADDRESS, myData);

    //    yield return www.SendWebRequest();

    //    if (www.isNetworkError || www.isHttpError)
    //    {
    //        Debug.Log(www.error);
    //    }
    //    else
    //    {
    //        Debug.Log(www.downloadHandler.text);
    //    }
    //}

    //public void ListTransactions()
    //{
    //    StartCoroutine(ListTransactionsCoroutine());
    //}

    //private IEnumerator ListTransactionsCoroutine()
    //{
    //    var myData = "{ \"jsonrpc\":\"2.0\",\"method\":\"getAccount\",\"params\":[\"P2f7ZFuj6NfZ76ymNMnG3xRBT5hAMicDrQRHE4S7SoxEr\"],\"id\":1}";
    //    var www = UnityWebRequest.Post(_SERVER_ADDRESS, myData);

    //    yield return www.SendWebRequest();

    //    if (www.isNetworkError || www.isHttpError)
    //    {
    //        Debug.Log(www.error);
    //    }
    //    else
    //    {
    //        Debug.Log(www.downloadHandler.text);
    //    }
    //}

    #endregion

    private void CreateToken()
    {
        var script = ScriptUtils.BeginScript()
            .AllowGas(Key.Address, 1, 9999)
            .CallContract("nexus", "CreateToken", Key.Address, "CAR", "Car Demo Token", 10000, 0, TokenFlags.Transferable | TokenFlags.Finite)
            .SpendGas(Key.Address)
            .EndScript();
    }
}
