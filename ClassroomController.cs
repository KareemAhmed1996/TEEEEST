using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ClassroomController : MonoBehaviour
{
    public GameObject GirlPrefabPlayer;
    public GameObject GirlPrefabOtherPlayer;
    public ChatController ChatHandler;
    public GameObject WarningAlert;
    public AgoraHandler AgoraController;

    private GameObject PlayerCharacter;
    private CharacterControlPlayer PlayerCharacter_controller;

    public List<GameObject> OtherPlayerCharacter = new List<GameObject>();

    public Button MicButton;



    private Queue<System.Action> actionQueue = new Queue<System.Action>();


    public GameObject GlobalSeatColliders;
    public Button SitDownB;
    public Button StandUpB;

    //private float last_x=0, last_z=0;



    //private int count_test = 1;


    void Start()
    {

        //yield return null;
        //yield return new WaitForSeconds(0.1f);
        //https://github.com/heroiclabs/nakama-examples/blob/master/unity/nakama-showreel/Assets/Nakama/Example/Matchmake.cs
        //https://github.com/PimDeWitte/UnityMainThreadDispatcher

        //actionQueue = new Queue<System.Action>(1024);
        //actionQueue = new Queue<System.Action>();


        NakamaSocket.Closed += OnClosed;
        NakamaSocket.ReceivedError += OnReceivedError;
        NakamaSocket.ReceivedMatchPresence += OnReceivedMatchPresence;
        NakamaSocket.ReceivedMatchState += OnReceivedMatchState;

        ChatHandler.OnSendChatMessage += _OnSendChatMessage;



        /*
        NakamaSocket.Connected += () =>
        {
            Debug.LogFormat("Connected");
            NakamaSocket.join_match(
                (_state, _IMatch) =>
                {

                    if (!_state)
                    {
                        Debug.LogFormat("join_match error: {0}", _state);
                    }
                    else
                    {
                        Debug.LogFormat("join_match sssss: {0}", _IMatch);

                        GetPlayersClassroomMatch();


                    }
                }
            );

            InvokeRepeating("SendingMyPosition", 1f, 0.1f);
        };

        NakamaSocket.TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1aWQiOiIwNDlhMjMzOC1kZjc5LTRlM2MtYTQ1Ny05ZmNlOTI1Nzg1YWYiLCJ1c24iOiJzdHVkZW50MSIsImV4cCI6MTU4OTM0MTQ0MX0.k7yKp7TkRML18nxoJy77Qa8nztTttpC4OuDmc-kjDBk";
        NakamaSocket.MATCH_ID = "8eeb84c8-a858-4835-bc10-3265cff11175.nakama4class";
        NakamaSocket.USERNAME = "student1";
        NakamaSocket.USERID = "049a2338-df79-4e3c-a457-9fce925785af";
        NakamaSocket.Connect();
        */

        AgoraHandler.OnJoinChannelSuccess += ClosedOnJoinChannelSuccess;
        AgoraHandler.OnLeaveChannel += OnLeaveChannel;




        PlayerCharacter = Instantiate(GirlPrefabPlayer) as GameObject;
        //PlayerCharacter.transform.position = new Vector3(4f, 10f, 4f);
        PlayerCharacter.transform.position = new Vector3(2.6f, 10f, -2.7f);
        PlayerCharacter_controller = PlayerCharacter.GetComponent<CharacterControlPlayer>();

        PlayerCharacter_controller.SetName(NakamaSocket.USERNAME);
        PlayerCharacter_controller.SetId(NakamaSocket.USERID);
        PlayerCharacter_controller.ChatInputText = ChatHandler.SendMessageT;
        PlayerCharacter_controller.SitDownB = SitDownB;




        GetPlayersClassroomMatch();
        InvokeRepeating("SendingMyPosition", 1f, 0.1f);

        AgoraHandler.Connect();
        AgoraHandler.JoinChannel(NakamaSocket.AGORA_TOKEN, NakamaSocket.CLASSROOM_ID, NakamaSocket.USER_ID_UINT);

        /*
        Debug.Log("AGORA_TOKEN" + NakamaSocket.AGORA_TOKEN);
        Debug.Log("CLASSROOM_ID" + NakamaSocket.CLASSROOM_ID);
        Debug.Log("USER_ID_UINT" + NakamaSocket.USER_ID_UINT);
        */
        SitDownB.gameObject.SetActive(false);
        StandUpB.gameObject.SetActive(false);

        SitDownB.onClick.AddListener(() => {
            if (PlayerCharacter_controller.seatNumber == 0) return;
            //Debug.Log("Try to sit on => "+PlayerCharacter_controller.seatNumber);


            NakamaSocket._socket.SendMatchStateAsync(NakamaSocket.MATCH_ID, 10, JsonUtility.ToJson(
                new SeatNumberJson()
                {
                    n = PlayerCharacter_controller.seatNumber
                }
            ));

        });

        StandUpB.onClick.AddListener(() => {
            //Debug.Log("Try to sit on => "+PlayerCharacter_controller.seatNumber);

            NakamaSocket._socket.SendMatchStateAsync(NakamaSocket.MATCH_ID, 11, "");
        });


        MicButton.onClick.AddListener(() => {
            if (!AgoraHandler.IsOnline)
            {
                MicButton.transform.Find("disconnected").GetComponent<RawImage>().enabled = true;
                MicButton.transform.Find("mic_on").GetComponent<RawImage>().enabled = false;
                MicButton.transform.Find("mic_off").GetComponent<RawImage>().enabled = false;

                //AgoraHandler.Connect();

                return;
            }

            if (AgoraHandler.IsMute)
            {
                MicButton.transform.Find("disconnected").GetComponent<RawImage>().enabled = false;
                MicButton.transform.Find("mic_on").GetComponent<RawImage>().enabled = true;
                MicButton.transform.Find("mic_off").GetComponent<RawImage>().enabled = false;

                AgoraHandler.setMic(true);
            }
            else
            {
                MicButton.transform.Find("disconnected").GetComponent<RawImage>().enabled = false;
                MicButton.transform.Find("mic_on").GetComponent<RawImage>().enabled = false;
                MicButton.transform.Find("mic_off").GetComponent<RawImage>().enabled = true;
                AgoraHandler.setMic(false);
            }
        });

        //must make timer to check if we recived the players or still!
    }
    string get_player_state_seat(SeatJsonData[] _seats, string _id)
    {
        string _found_s = "0";


        for (int _i3 = 0; _i3 < _seats.Length; _i3++)
        {
            if (_id == _seats[_i3].i)
            {
                _found_s = _seats[_i3].s;
                break;
            }
        }

        return _found_s;
    }
    void ReAssignSeats(SeatJsonData[] _seats)
    {
        Debug.Log("ReAssignSeats Start");

        string _general_player_seat = get_player_state_seat(_seats, NakamaSocket.USERID);
        if (_general_player_seat == "0")
        {
            PlayerCharacter_controller.standUp();
            //SitDownB.gameObject.SetActive(false);
            StandUpB.gameObject.SetActive(false);
        }
        else
        {
            Transform _SeatCollider_g = GlobalSeatColliders.transform.Find("SeatCollider_" + _general_player_seat);
            if (_SeatCollider_g != null)
            {

                //PlayerCharacter_controller.sit(true);
                PlayerCharacter_controller.setDown(_SeatCollider_g.position.x, _SeatCollider_g.position.z, _SeatCollider_g.rotation.eulerAngles.y);
                SitDownB.gameObject.SetActive(false);
                StandUpB.gameObject.SetActive(true);
            }
        }

        for (int _i2 = 0; _i2 < OtherPlayerCharacter.Count; _i2++)
        {
            _general_player_seat = get_player_state_seat(_seats, OtherPlayerCharacter[_i2].GetComponent<CharacterControlOtherPlayer>().player_id);
            if (_general_player_seat == "0")
            {
                OtherPlayerCharacter[_i2].GetComponent<CharacterControlOtherPlayer>().standUp();
            }
            else
            {
                Transform _SeatCollider_g = GlobalSeatColliders.transform.Find("SeatCollider_" + _general_player_seat);
                if (_SeatCollider_g != null)
                {
                    //cmd_set
                    OtherPlayerCharacter[_i2].GetComponent<CharacterControlOtherPlayer>().setDown(_SeatCollider_g.position.x, _SeatCollider_g.position.z, _SeatCollider_g.rotation.eulerAngles.y);

                }

            }
        }
    }

    void _OnSendChatMessage(string _s)
    {
        //Debug.Log("SendMessage click 222 => "+ _s);
        NakamaSocket._socket.SendMatchStateAsync(NakamaSocket.MATCH_ID, 2, JsonUtility.ToJson(
            new MessageJson() { m = _s }
        ));
        ChatHandler.insertTextChat("<color=green>" + NakamaSocket.USERNAME + ":</color> " + _s);
    }

    void ClosedOnJoinChannelSuccess()
    {
        MicButton.transform.Find("disconnected").GetComponent<RawImage>().enabled = false;
        MicButton.transform.Find("mic_on").GetComponent<RawImage>().enabled = true;
        MicButton.transform.Find("mic_off").GetComponent<RawImage>().enabled = false;

    }
    void OnLeaveChannel()
    {
        MicButton.transform.Find("disconnected").GetComponent<RawImage>().enabled = true;
        MicButton.transform.Find("mic_on").GetComponent<RawImage>().enabled = false;
        MicButton.transform.Find("mic_off").GetComponent<RawImage>().enabled = false;
    }

    void SendingMyPosition()
    {

        if (PlayerCharacter == null || PlayerCharacter_controller == null) return;
        if (!NakamaSocket.SocketState || !PlayerCharacter_controller.player_changed) return;

        NakamaSocket._socket.SendMatchStateAsync(NakamaSocket.MATCH_ID, 1, JsonUtility.ToJson(
            new PositionJson() { x = PlayerCharacter.transform.position.x, z = PlayerCharacter.transform.position.z, r = PlayerCharacter.transform.eulerAngles.y }
        ));

        PlayerCharacter_controller.player_changed = false;

    }

    void GetPlayersClassroomMatch()
    {
        NakamaSocket.get_presences_match(
            _state =>
            {
                Debug.LogFormat("get_presences_match : {0}", _state);
            }
        );
    }


    void unload_other_player_data(int _key)
    {
        //Debug.Log("delete key => "+_key);
        //Debug.Log("OtherPlayerCharacter before length => " + OtherPlayerCharacter.Count);


        Destroy(OtherPlayerCharacter[_key]);
        //OtherPlayerCharacter.Remove(OtherPlayerCharacter[_key]);
        OtherPlayerCharacter.RemoveAt(_key);

        //Debug.Log("OtherPlayerCharacter after length => " + OtherPlayerCharacter.Count);

    }
    void load_other_player_data(string user_id, string user_name, string _seatNumber)
    {

        int _t_length = OtherPlayerCharacter.Count;

        OtherPlayerCharacter.Add(Instantiate(GirlPrefabOtherPlayer));
        //OtherPlayerCharacter[_t_length].transform.position = new Vector3(Random.Range(0.0f, 9.0f), 0.142f, Random.Range(0.0f, 9.0f));
        OtherPlayerCharacter[_t_length].transform.position = new Vector3(2.6f, 10f, -2.7f);
        OtherPlayerCharacter[_t_length].GetComponent<CharacterControlOtherPlayer>().SetName(user_name);
        OtherPlayerCharacter[_t_length].GetComponent<CharacterControlOtherPlayer>().SetId(user_id);

        if (_seatNumber != "" && _seatNumber != "0")
        {
            Transform _SeatCollider_g = GlobalSeatColliders.transform.Find("SeatCollider_" + _seatNumber);
            if (_SeatCollider_g != null)
            {
                OtherPlayerCharacter[_t_length].GetComponent<CharacterControlOtherPlayer>().setDown(_SeatCollider_g.position.x, _SeatCollider_g.position.z, _SeatCollider_g.rotation.eulerAngles.y);
            }
        }

        //OtherPlayerCharacter[_t_length] = Instantiate(GirlPrefab) as GameObject;
    }
    void move_other_players(float _x, float _z, float _r, string _id)
    {
        for (int _i2 = 0; _i2 < OtherPlayerCharacter.Count; _i2++)
        {
            if (OtherPlayerCharacter[_i2].GetComponent<CharacterControlOtherPlayer>().player_id == _id)
            {
                OtherPlayerCharacter[_i2].GetComponent<CharacterControlOtherPlayer>().SetRotation(_r);
                OtherPlayerCharacter[_i2].GetComponent<CharacterControlOtherPlayer>().SetPosition(_x, _z);

                break;
            }
        }
    }
    void find_other_players(OpCode_51_Json_sub[] _players, bool _load)
    {
        int _found = -1;


        for (int _i2 = 0; _i2 < _players.Length; _i2++)
        {
            if (_players[_i2].i == NakamaSocket.USERID)
            {
                if (_load && _players[_i2].s != "" && _players[_i2].s != "0")
                {
                    Transform _SeatCollider_g = GlobalSeatColliders.transform.Find("SeatCollider_" + _players[_i2].s);
                    if (_SeatCollider_g != null)
                    {
                        //PlayerCharacter_controller.sit(true);
                        PlayerCharacter_controller.setDown(_SeatCollider_g.position.x, _SeatCollider_g.position.z, _SeatCollider_g.rotation.eulerAngles.y);
                        SitDownB.gameObject.SetActive(false);
                        StandUpB.gameObject.SetActive(true);
                    }
                }
                continue;
            }

            _found = -1;
            for (int _i3 = 0; _i3 < OtherPlayerCharacter.Count; _i3++)
            {
                if (OtherPlayerCharacter[_i3].GetComponent<CharacterControlOtherPlayer>().player_id == _players[_i2].i)
                {
                    _found = _i3;
                    break;
                }

            }
            if (_found == -1 && _load)
            {
                load_other_player_data(_players[_i2].i, _players[_i2].u, _players[_i2].s);
            }
            else if (_found != -1 && !_load)
            {
                unload_other_player_data(_found);
            }
        }

    }

    void OnClosed()
    {
        Debug.Log("OnClosed");
        StartCoroutine(Alert("Socket closed, Please Login again.", false));

        remove_listen();
        AgoraHandler.LeaveChannel();
        StartCoroutine(LoadNextSceneDelay("Login"));
    }
    void OnReceivedError(string _e)
    {
        Debug.Log("OnReceivedError => " + _e);
        if (_e == "401 Unauthorized")
        {
            //StartCoroutine(Alert("Token Expired or wrong, Please Login again."));
        }
    }

    void OnReceivedMatchPresence(Nakama.IMatchPresenceEvent _e)
    {

        IEnumerator<Nakama.IUserPresence> _Joins = _e.Joins.GetEnumerator();
        List<OpCode_51_Json_sub> _Joins_presenc = new List<OpCode_51_Json_sub>();


        IEnumerator<Nakama.IUserPresence> _Leaves = _e.Leaves.GetEnumerator();
        List<OpCode_51_Json_sub> _Leaves_presenc = new List<OpCode_51_Json_sub>();

        while (_Joins.MoveNext())
        {
            _Joins_presenc.Add(new OpCode_51_Json_sub() { i = _Joins.Current.UserId, u = _Joins.Current.Username, s = "0" });
        }
        while (_Leaves.MoveNext())
        {
            _Leaves_presenc.Add(new OpCode_51_Json_sub() { i = _Leaves.Current.UserId, u = _Leaves.Current.Username, s = "0" });
        }

        lock (actionQueue)
        {
            actionQueue.Enqueue(() => {
                find_other_players(_Joins_presenc.ToArray(), true);
                find_other_players(_Leaves_presenc.ToArray(), false);
            });
        }


    }


    void OnReceivedMatchState(Nakama.IMatchState _e)
    {
        //Debug.Log("OnReceivedMatchState 1 => "+_e);

        if (_e.OpCode == 1)
        {
            var _s = System.Text.Encoding.UTF8.GetString(_e.State);
            PositionJson _p = JsonUtility.FromJson<PositionJson>(_s);

            lock (actionQueue)
            {
                actionQueue.Enqueue(() => {
                    move_other_players(_p.x, _p.z, _p.r, _e.UserPresence.UserId);
                    //StartCoroutine(load_other_players(myObject.players));
                });
            }

        }
        else if (_e.OpCode == 2)
        {
            var _s = System.Text.Encoding.UTF8.GetString(_e.State);
            MessageJson _p = JsonUtility.FromJson<MessageJson>(_s);

            ChatHandler.insertTextChat("<color=green>" + _e.UserPresence.Username + ":</color> " + _p.m);
        }
        else if (_e.OpCode == 31)
        {
            var _State = System.Text.Encoding.UTF8.GetString(_e.State);

            if (_State.Length <= 5)
            {
                return;
            }

            _State = "{\"players\":" + _State + "}";


            OpCode_51_Json myObject = JsonUtility.FromJson<OpCode_51_Json>(_State);

            lock (actionQueue)
            {
                actionQueue.Enqueue(() => {
                    find_other_players(myObject.players, true);
                    //StartCoroutine(load_other_players(myObject.players));
                });
            }

        }
        else if (_e.OpCode == 13)
        {
            var _State = System.Text.Encoding.UTF8.GetString(_e.State);

            if (_State == "{}" || _State == "")
            {
                _State = "[]";
            }

            _State = "{\"seats\":" + _State + "}";

            Seats_Json myObject = JsonUtility.FromJson<Seats_Json>(_State);

            lock (actionQueue)
            {
                actionQueue.Enqueue(() => {
                    ReAssignSeats(myObject.seats);
                    //StartCoroutine(load_other_players(myObject.players));
                });
            }

        }
        else if (_e.OpCode == 44)
        {
            Debug.Log("OpCode ALERT => 44");
            var _s = System.Text.Encoding.UTF8.GetString(_e.State);
            Debug.Log(_s);

        }
        else if (_e.OpCode == 99)
        {
            Debug.Log("OpCode => 99");
            var _s = System.Text.Encoding.UTF8.GetString(_e.State);
            Debug.Log(_s);

        }
    }

    // Update is called once per frame
    void Update()
    {
        /*
        for (int i = 0, l = actionQueue.Count; i < l; i++)
        {
            actionQueue.Dequeue()();
        }
        */
        lock (actionQueue)
        {
            while (actionQueue.Count > 0)
            {
                actionQueue.Dequeue().Invoke();
            }
        }





    }

    IEnumerator Alert(string m, bool _succsu)
    {

        if (_succsu)
        {
            WarningAlert.GetComponent<Image>().color = new Color32(0x28, 0xa7, 0x45, 0xe6);
        }
        else
        {
            WarningAlert.GetComponent<Image>().color = new Color32(0xdc, 0x35, 0x45, 0xd9);
        }
        WarningAlert.GetComponentInChildren<Text>().text = m;
        WarningAlert.SetActive(true);
        yield return new WaitForSeconds(2);

        if (WarningAlert != null)
        {
            WarningAlert.SetActive(false);
        }


    }
    void remove_listen()
    {
        NakamaSocket.Closed -= OnClosed;
        NakamaSocket.ReceivedError -= OnReceivedError;
        NakamaSocket.ReceivedMatchPresence -= OnReceivedMatchPresence;
        NakamaSocket.ReceivedMatchState -= OnReceivedMatchState;
    }
    IEnumerator LoadNextSceneDelay(string _scene_name)
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(_scene_name);
    }
    void OnApplicationQuit()
    {
        //https://docs.unity3d.com/ScriptReference/Application.CancelQuit.html

        //CancelInvoke("SendingMyPosition");

        AgoraHandler.ApplicationQuit();

        if (NakamaSocket._socket != null)
        {
            //NakamaSocket._socket.LeaveMatchAsync(NakamaSocket.MATCH_ID);
        }

    }
}
