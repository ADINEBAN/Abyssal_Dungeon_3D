using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace AbyssGame
{
    [Serializable]
    public class LobbyNetMsg
    {
        public string op; // "join", "slot", "sync", "start", "hero_pos", "hero_skill", "boss_sync", "room_clear"
        public int slot;
        public string name;
        public int classId;
        public int buildId;
        public bool ready;
        public int floor = 1;
        public PartySlotInfo[] party;

        // Phase 10: Real-time Dungeon Co-op Synchronization payload
        public float px, py, pz, yaw;
        public float hp, mana;
        public int skillSlot = -1;
        public int roomIdx = -1;
        public float bossHp = -1f;
        public int bossPhase = 1;
    }

    /// <summary>
    /// Optional real TCP LAN Lobby Host/Client (127.0.0.1 or LAN IP, default port 7777)
    /// that syncs party slots, class selection, ready state, and Dungeon start across instances,
    /// while remaining 100% compatible with offline AI party play.
    /// </summary>
    public class LanRoomBridge : MonoBehaviour
    {
        private static LanRoomBridge _inst;
        public static LanRoomBridge I
        {
            get
            {
                if (_inst == null)
                {
                    _inst = FindObjectOfType<LanRoomBridge>();
                    if (_inst == null)
                    {
                        var go = new GameObject("[LanRoomBridge]");
                        DontDestroyOnLoad(go);
                        _inst = go.AddComponent<LanRoomBridge>();
                    }
                }
                return _inst;
            }
        }

        public bool IsHosting { get; private set; }
        public bool IsConnectedClient { get; private set; }
        public int LocalSlotIndex { get; private set; } = 0;
        public string StatusText = "Chế độ: Tổ đội Local + AI Hunter (Sẵn sàng)";

        private TcpListener _listener;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private TcpClient _clientConn;
        private StreamWriter _clientWriter;
        private readonly ConcurrentQueue<string> _inbox = new ConcurrentQueue<string>();
        private volatile bool _running;

        private void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(gameObject); return; }
            _inst = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit() => StopNetwork();
        private void OnDestroy() { if (_inst == this) StopNetwork(); }

        public bool StartHost(int port = 7777)
        {
            StopNetwork();
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _running = true;
                IsHosting = true;
                LocalSlotIndex = 0;
                StatusText = $"Đang mở phòng LAN tại cổng {port} (IP máy: {GetLocalIPv4()})";
                ThreadPool.QueueUserWorkItem(_ => AcceptLoop());
                return true;
            }
            catch (Exception e)
            {
                StatusText = "Không thể mở cổng LAN: " + e.Message;
                return false;
            }
        }

        public bool JoinHost(string hostIp, int port = 7777)
        {
            StopNetwork();
            try
            {
                _clientConn = new TcpClient();
                var ar = _clientConn.BeginConnect(hostIp.Trim(), port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2.0)))
                {
                    _clientConn.Close();
                    StatusText = $"Không tìm thấy phòng LAN tại {hostIp}:{port}";
                    return false;
                }
                _clientConn.EndConnect(ar);
                _running = true;
                IsConnectedClient = true;
                var stream = _clientConn.GetStream();
                _clientWriter = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                ThreadPool.QueueUserWorkItem(_ => ReadStreamLoop(stream));

                SendToHost(new LobbyNetMsg
                {
                    op = "join",
                    name = GameSession.I.Profile.heroName,
                    classId = GameSession.I.Profile.selectedClass,
                    buildId = GameSession.I.Profile.selectedBuild,
                    ready = false
                });
                StatusText = $"Đã kết nối phòng LAN {hostIp}:{port}";
                return true;
            }
            catch (Exception e)
            {
                StatusText = "Lỗi kết nối LAN: " + e.Message;
                return false;
            }
        }

        public void StopNetwork()
        {
            _running = false;
            IsHosting = false;
            IsConnectedClient = false;
            LocalSlotIndex = 0;
            try { _listener?.Stop(); } catch { }
            _listener = null;
            lock (_clients)
            {
                foreach (var c in _clients) { try { c.Close(); } catch { } }
                _clients.Clear();
            }
            try { _clientConn?.Close(); } catch { }
            _clientConn = null;
            _clientWriter = null;
            StatusText = "Chế độ: Tổ đội Local + AI Hunter";
        }

        private void AcceptLoop()
        {
            while (_running && _listener != null)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    lock (_clients) _clients.Add(client);
                    var stream = client.GetStream();
                    ThreadPool.QueueUserWorkItem(_ => ReadStreamLoop(stream));
                }
                catch { break; }
            }
        }

        private void ReadStreamLoop(NetworkStream stream)
        {
            try
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (_running)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;
                        _inbox.Enqueue(line);
                    }
                }
            }
            catch { }
        }

        public void BroadcastFromHost()
        {
            if (!IsHosting) return;
            var msg = new LobbyNetMsg
            {
                op = "sync",
                floor = GameSession.I.Profile.selectedFloor,
                party = GameSession.I.Party
            };
            BroadcastRaw(JsonUtility.ToJson(msg));
        }

        public void BroadcastStartDungeon()
        {
            if (!IsHosting) return;
            var msg = new LobbyNetMsg { op = "start", floor = GameSession.I.Profile.selectedFloor, party = GameSession.I.Party };
            BroadcastRaw(JsonUtility.ToJson(msg));
        }

        public void NotifyClientSlotUpdate(bool ready)
        {
            if (!IsConnectedClient) return;
            SendToHost(new LobbyNetMsg
            {
                op = "slot",
                slot = LocalSlotIndex,
                name = GameSession.I.Profile.heroName,
                classId = GameSession.I.Profile.selectedClass,
                buildId = GameSession.I.Profile.selectedBuild,
                ready = ready
            });
        }

        private void SendToHost(LobbyNetMsg m)
        {
            try { _clientWriter?.WriteLine(JsonUtility.ToJson(m)); } catch { }
        }

        private void BroadcastRaw(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");
            lock (_clients)
            {
                for (int i = _clients.Count - 1; i >= 0; i--)
                {
                    try { _clients[i].GetStream().Write(bytes, 0, bytes.Length); }
                    catch { _clients.RemoveAt(i); }
                }
            }
        }

        private float _netTickTimer;

        public string GetCoopModeBanner()
        {
            if (IsHosting) return $"CO-OP LAN HOST (Cổng 7777 | { _clients.Count } Client kết nối)";
            if (IsConnectedClient) return $"CO-OP LAN CLIENT (Slot #{LocalSlotIndex + 1})";
            return $"SINGLE-PLAYER + AI PARTY ({GameSession.I.ActivePartyCount()} Hunters)";
        }

        public void BroadcastHeroSkill(int slotIdx, int skillIndex)
        {
            if (!IsHosting && !IsConnectedClient) return;
            var msg = new LobbyNetMsg { op = "hero_skill", slot = slotIdx, skillSlot = skillIndex };
            if (IsHosting) BroadcastRaw(JsonUtility.ToJson(msg));
            else SendToHost(msg);
        }

        private void TickDungeonStateSync()
        {
            if (!IsHosting && !IsConnectedClient) return;
            var dw = DungeonWorldBuilder.Instance;
            if (dw == null || dw.LocalHero == null) return;

            _netTickTimer -= Time.unscaledDeltaTime;
            if (_netTickTimer > 0f) return;
            _netTickTimer = 0.08f; // ~12.5 Hz state sync

            var h = dw.LocalHero;
            var posMsg = new LobbyNetMsg
            {
                op = "hero_pos",
                slot = LocalSlotIndex,
                px = h.transform.position.x,
                py = h.transform.position.y,
                pz = h.transform.position.z,
                yaw = h.transform.eulerAngles.y,
                hp = h.Hp,
                mana = h.Mana
            };

            if (IsHosting)
            {
                BroadcastRaw(JsonUtility.ToJson(posMsg));
                if (AbyssalBossAI.ActiveBoss != null)
                {
                    var bossUnit = AbyssalBossAI.ActiveBoss.GetComponent<ActorUnit>();
                    if (bossUnit != null)
                    {
                        BroadcastRaw(JsonUtility.ToJson(new LobbyNetMsg
                        {
                            op = "boss_sync",
                            bossHp = bossUnit.Hp,
                            bossPhase = AbyssalBossAI.ActiveBoss.Phase
                        }));
                    }
                }
            }
            else if (IsConnectedClient)
            {
                SendToHost(posMsg);
            }
        }

        private void ApplyRemoteDungeonSync(LobbyNetMsg msg)
        {
            var dw = DungeonWorldBuilder.Instance;
            if (dw == null) return;

            if (msg.op == "hero_pos" && msg.slot >= 0 && msg.slot < dw.PartyUnits.Count && msg.slot != LocalSlotIndex)
            {
                var unit = dw.PartyUnits[msg.slot];
                if (unit != null && !unit.IsDead)
                {
                    unit.transform.position = Vector3.Lerp(unit.transform.position, new Vector3(msg.px, msg.py, msg.pz), 0.55f);
                    unit.transform.rotation = Quaternion.Euler(0f, msg.yaw, 0f);
                    unit.Hp = Mathf.Clamp(msg.hp, 1f, unit.MaxHp);
                    unit.Mana = Mathf.Clamp(msg.mana, 0f, unit.MaxMana);
                }
            }
            else if (msg.op == "boss_sync" && IsConnectedClient && AbyssalBossAI.ActiveBoss != null)
            {
                var bossUnit = AbyssalBossAI.ActiveBoss.GetComponent<ActorUnit>();
                if (bossUnit != null && msg.bossHp > 0f)
                {
                    bossUnit.Hp = Mathf.Min(bossUnit.Hp, msg.bossHp);
                }
            }
        }

        private void Update()
        {
            TickDungeonStateSync();
            while (_inbox.TryDequeue(out string json))
            {
                try
                {
                    var msg = JsonUtility.FromJson<LobbyNetMsg>(json);
                    if (msg == null) continue;

                    if (IsHosting)
                    {
                        if (msg.op == "join")
                        {
                            // Assign an AI/empty slot (1..3) to the joining human player
                            int targetSlot = 1;
                            for (int i = 1; i < 4; i++)
                                if (!GameSession.I.Party[i].active || !GameSession.I.Party[i].isHuman) { targetSlot = i; break; }

                            GameSession.I.Party[targetSlot] = new PartySlotInfo
                            {
                                active = true, isHuman = true, playerName = msg.name + " (LAN)",
                                classId = msg.classId, buildId = msg.buildId, level = 1, ready = msg.ready
                            };
                            GameSession.I.NotifyPartyChanged();
                            BroadcastFromHost();
                        }
                        else if (msg.op == "slot" && msg.slot >= 1 && msg.slot < 4)
                        {
                            var s = GameSession.I.Party[msg.slot];
                            s.classId = msg.classId;
                            s.buildId = msg.buildId;
                            s.ready = msg.ready;
                            GameSession.I.NotifyPartyChanged();
                            BroadcastFromHost();
                        }
                        else if (msg.op == "hero_pos" || msg.op == "hero_skill")
                        {
                            ApplyRemoteDungeonSync(msg);
                            BroadcastRaw(json);
                        }
                    }
                    else if (IsConnectedClient)
                    {
                        if (msg.op == "hero_pos" || msg.op == "boss_sync" || msg.op == "hero_skill")
                        {
                            ApplyRemoteDungeonSync(msg);
                        }
                        else if (msg.op == "sync" && msg.party != null && msg.party.Length == 4)
                        {
                            GameSession.I.Party = msg.party;
                            GameSession.I.Profile.selectedFloor = msg.floor;
                            for (int i = 1; i < 4; i++)
                                if (msg.party[i] != null && msg.party[i].isHuman && msg.party[i].playerName.StartsWith(GameSession.I.Profile.heroName))
                                    LocalSlotIndex = i;
                            GameSession.I.NotifyPartyChanged();
                        }
                        else if (msg.op == "start")
                        {
                            if (msg.party != null && msg.party.Length == 4) GameSession.I.Party = msg.party;
                            GameSession.I.Profile.selectedFloor = msg.floor;
                            GameSession.GoToScene("Dungeon");
                        }
                    }
                }
                catch { }
            }
        }

        public static string GetLocalIPv4()
        {
            try
            {
                foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                        return ip.ToString();
            }
            catch { }
            return "127.0.0.1";
        }
    }
}

