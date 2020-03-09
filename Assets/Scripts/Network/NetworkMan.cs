using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public PlayerUnit playerPrefab;
    Dictionary<string, PlayerUnit> playerUnits = new Dictionary<string, PlayerUnit>();
    List<Player> newPlayers = new List<Player>();
    List<Player> disconnectedPlayers = new List<Player>();
    List<PlayerPacketData> currentPlayers;

    public UdpClient udp;
    public string serverIp = "3.219.69.41";
    public int serverPort = 12345;
    public string clientId;

    public float numUpdatePerSecond = 10.0f;
    public float estimatedLag = 200.0f; // in mili seconds

    public static NetworkMan Instance { get; private set; } = null;

    private void Awake()
    {
        if( Instance != null && Instance != this )
            Destroy( gameObject );
        else
            Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();

        udp.Connect( serverIp, serverPort );

        Byte[] sendBytes = Encoding.ASCII.GetBytes("{\'message\':\'connect\'}");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1.0f, 1.0f / numUpdatePerSecond );
    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        SERVER_CONNECTED,
        NEW_CLIENT,
        UPDATE,
        CLIENT_DROPPED,
        CLIENT_FIRE,
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    }

    [Serializable]
    public struct receivedColor
    {
        public float R;
        public float G;
        public float B;

        public static implicit operator Color( receivedColor value )
        {
            return new Color( value.R, value.G, value.B );
        }
    }

    [Serializable]
    public struct receivedPos
    {
        public float x;
        public float y;
        public float z;

        public override string ToString()
        {
            return String.Format( "[ {0}, {1}, {2} ]", x, y, z );
        }

        public static implicit operator Vector3( receivedPos value )
        {
            return new Vector3( value.x, value.y, value.z );
        }
    }

    [Serializable]
    public struct receivedRotation
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public static implicit operator Quaternion( receivedRotation value )
        {
            return new Quaternion( value.x, value.y, value.z, value.w );
        }
    }

    [Serializable]
    public class Player{
        public string id;
        public receivedColor color;
        public receivedPos pos;
        public receivedRotation rotation;
        public float health;
        public string action;
    }

    [Serializable]
    public class PlayerPacketData
    {
        public string id;
        public string message;
        public Vector3 pos;
        public Quaternion rotation;

        public float health;
    }

    [Serializable]
    public class NewPlayer{
        public commands cmd;
        public Player player;
    }

    [Serializable]
    public class GameState{
        public commands cmd;
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;

    Dictionary<string, Player> prevPlayerData = new Dictionary<string, Player>();
    GameState previousGameState = null;
    float previousTime = 0;
    float latestTime = 0;
    float currentTime;
    
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                case commands.SERVER_CONNECTED:
                    {
                        NewPlayer newPlayer = JsonUtility.FromJson<NewPlayer>( returnData );
                        clientId = newPlayer.player.id;
                        newPlayers.Add( newPlayer.player );
                        break;
                    }
                case commands.NEW_CLIENT:
                    {
                        NewPlayer newPlayer = JsonUtility.FromJson<NewPlayer>( returnData );
                        newPlayers.Add( newPlayer.player );
                        break;
                    }
                case commands.UPDATE:
                    previousTime = latestTime;
                    previousGameState = lastestGameState;

                    latestTime = currentTime;
                    lastestGameState = JsonUtility.FromJson<GameState>( returnData );
                    break;
                case commands.CLIENT_DROPPED:
                    NewPlayer droppedPlayer = JsonUtility.FromJson<NewPlayer>( returnData );
                    disconnectedPlayers.Add( droppedPlayer.player );
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers(  ){
        if( newPlayers.Count > 0 )
        {
            foreach( var newPlayer in newPlayers )
            {
                PlayerUnit player = Instantiate( playerPrefab );
                player.transform.position = new Vector3( newPlayer.pos.x, newPlayer.pos.y, newPlayer.pos.z );
                player.SetId( newPlayer.id, clientId == newPlayer.id );
                playerUnits.Add( newPlayer.id, player );
            }
            newPlayers.Clear();
        }
    }

    void UpdatePlayers(){
        if( lastestGameState != null & lastestGameState.players.Length > 0 )
        {
            foreach( var player in lastestGameState.players )
            {
                if( playerUnits.ContainsKey( player.id ) )
                {
                    bool prevPlayerExist = false;
                    Player prevPlayer = null;
                    if( previousGameState != null )
                    {
                        prevPlayerExist = Array.Exists( previousGameState.players, p => p.id == player.id );
                        prevPlayer = Array.Find( previousGameState.players, p => p.id == player.id );
                    }

                    playerUnits[player.id].SetColor( player.color );

                    if( player.id != clientId )
                    {
                        Vector3 nextPos = player.pos;
                        Quaternion nextRotation = player.rotation;
                        if( CanvasManager.Instance.reconciliation.isOn )
                        {

                        }
                        if( CanvasManager.Instance.interpolation.isOn && prevPlayerExist )
                        {
                            float t = ( Time.time - latestTime ) / ( latestTime - previousTime );
                            nextPos = Vector3.Lerp( prevPlayer.pos, player.pos, t );
                            nextRotation = Quaternion.Lerp( prevPlayer.rotation, player.rotation, t );
                            Debug.Log( String.Format( "Interpolate {0} to {1} by {2}, next = {3}", prevPlayer.pos, player.pos, t, nextPos ) );
                        }

                        playerUnits[player.id].transform.position = nextPos;
                        playerUnits[player.id].transform.rotation = nextRotation;
                        playerUnits[player.id].SetHealth(player.health);
                    }
                    if( player.action == "fire" )
                    {
                        playerUnits[player.id].FireBullet();
                    }
                }
            }
        }
    }

    void DestroyPlayers(){
        if( disconnectedPlayers.Count > 0 )
        {
            foreach( var droppedPlayer in disconnectedPlayers )
            {
                if( playerUnits.ContainsKey( droppedPlayer.id ) )
                {
                    Destroy( playerUnits[droppedPlayer.id].gameObject );
                    playerUnits.Remove( droppedPlayer.id );
                }
            }
            disconnectedPlayers.Clear();
        }
    }
    
    void HeartBeat(){

        if( clientId != null && playerUnits[clientId].IsAlive )
        {
            PlayerPacketData data = new PlayerPacketData();
            data.id = clientId;
            data.pos = playerUnits[clientId].transform.position;
            data.rotation = playerUnits[clientId].transform.rotation;
            data.health = playerUnits[clientId].currentHealth;
            string messageData = JsonUtility.ToJson( data );
            Byte[] sendBytes = Encoding.ASCII.GetBytes(messageData);
            udp.Send( sendBytes, sendBytes.Length );
        }
    }

    public void SendAction( string message, Transform clientTransform )
    {
        if (clientId != null )
        {
            PlayerPacketData data = new PlayerPacketData();
            data.id = clientId;
            data.message = message;
            string messageData = JsonUtility.ToJson(data);

            Byte[] sendBytes = Encoding.ASCII.GetBytes(messageData);
            udp.Send(sendBytes, sendBytes.Length);
        }
    }

    void Update(){
        currentTime = Time.time;
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}