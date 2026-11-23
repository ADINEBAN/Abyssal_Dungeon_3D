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
    public enum GamePlayMode
    {
        SinglePlayer = 0,
        MultiplayerCoOp = 1
    }

    /// <summary>
    /// Architecture Manager supporting both:
    /// - SinglePlayer: 100% standalone offline mode (1 Human Player + 3 Smart AI Teammates).
    /// - MultiplayerCoOp: 2–4 Players via LAN/IP/RoomCode where joining human players automatically
    ///   replace AI teammate slots (e.g., 2 Humans + 2 AI, 3 Humans + 1 AI, or 4 Humans).
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        private static GameModeManager _inst;
        public static GameModeManager I
        {
            get
            {
                if (_inst == null)
                {
                    _inst = FindObjectOfType<GameModeManager>();
                    if (_inst == null)
                    {
                        var go = new GameObject("[GameModeManager]");
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                        _inst = go.AddComponent<GameModeManager>();
                    }
                }
                return _inst;
            }
        }

        public GamePlayMode CurrentMode { get; private set; } = GamePlayMode.SinglePlayer;
        public bool InstancedLootEnabled { get; set; } = true;

        public static bool IsMultiplayerCoOp =>
            (_inst != null && _inst.CurrentMode == GamePlayMode.MultiplayerCoOp) ||
            (LanRoomBridge.HasInstance && (LanRoomBridge.I.IsHosting || LanRoomBridge.I.IsConnectedClient));

        public static bool IsSinglePlayer => !IsMultiplayerCoOp;

        private void Awake()
        {
            if (_inst != null && _inst != this) { BakedAssetRegistry.DestroySafe(gameObject); return; }
            _inst = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Switches to 100% standalone offline Single Player mode (1 Human + 3 Role-Based AI Companions).
        /// </summary>
        public void SetSinglePlayerMode()
        {
            CurrentMode = GamePlayMode.SinglePlayer;
            if (LanRoomBridge.HasInstance)
                LanRoomBridge.I.StopNetwork();

            var s = GameSession.I;
            s.EnsureDefaultParty();
            s.Party[0].active = true;
            s.Party[0].isHuman = true;
            s.Party[0].playerName = s.Profile.heroName;
            s.Party[0].classId = s.Profile.selectedClass;
            s.Party[0].buildId = s.Profile.selectedBuild;
            s.Party[0].ready = true;

            // Fill slots 1..3 with Role-Based Smart AI Teammates (Lyra - Knight, Kael - Mage, Seraphina - Ranger)
            RefillRemainingSlotsWithAI();
            s.NotifyPartyChanged();
        }

        /// <summary>
        /// Starts a Multiplayer Co-op Host Room (2–4 players), generating a Room Code and listening on the given port.
        /// Remaining unfilled slots stay as Smart AI companions until human players join.
        /// </summary>
        public bool StartCoOpHost(int port = 7777)
        {
            CurrentMode = GamePlayMode.MultiplayerCoOp;
            InstancedLootEnabled = true;
            var s = GameSession.I;
            s.EnsureDefaultParty();
            string ip = LanRoomBridge.GetLocalIPv4();
            string lastOctet = "01";
            if (!string.IsNullOrEmpty(ip))
            {
                string[] parts = ip.Split('.');
                if (parts.Length == 4) lastOctet = parts[3];
            }
            s.RoomCode = $"ABYSS-{lastOctet}-{port}";
            bool ok = LanRoomBridge.I.StartHost(port);
            s.NotifyPartyChanged();
            return ok;
        }

        /// <summary>
        /// Joins an existing Multiplayer Co-op Room via IPv4 address or Room Code (e.g. "127.0.0.1" or "ABYSS-105-7777").
        /// </summary>
        public bool JoinCoOpRoom(string ipOrRoomCode, int defaultPort = 7777)
        {
            CurrentMode = GamePlayMode.MultiplayerCoOp;
            InstancedLootEnabled = true;
            ResolveEndpoint(ipOrRoomCode, defaultPort, out string resolvedIp, out int resolvedPort);
            bool ok = LanRoomBridge.I.JoinHost(resolvedIp, resolvedPort);
            GameSession.I.NotifyPartyChanged();
            return ok;
        }

        /// <summary>
        /// Ensures any slot in 1..3 that is not occupied by a human player is filled by a role-balanced AI teammate
        /// (Arthur/Cleric, Lyra/Knight, Kael/Mage, Seraphina/Ranger).
        /// </summary>
        public void RefillRemainingSlotsWithAI()
        {
            var s = GameSession.I;
            if (s == null || s.Party == null) return;

            int leaderClass = s.Party[0] != null ? s.Party[0].classId : s.Profile.selectedClass;
            // Core 4-hero tactical roster: Knight (0), Mage (5), Ranger (4), Cleric (9)
            int[] preferredClasses = { 0, 5, 4, 9 };
            var usedClasses = new HashSet<int> { leaderClass };
            for (int i = 1; i < 4; i++)
            {
                if (s.Party[i] != null && s.Party[i].active && s.Party[i].isHuman)
                    usedClasses.Add(s.Party[i].classId);
            }

            for (int i = 1; i < 4; i++)
            {
                if (s.Party[i] == null) s.Party[i] = new PartySlotInfo();
                if (s.Party[i].isHuman) continue;

                int pickCls = preferredClasses[i % preferredClasses.Length];
                for (int c = 0; c < preferredClasses.Length; c++)
                {
                    if (!usedClasses.Contains(preferredClasses[c]))
                    {
                        pickCls = preferredClasses[c];
                        usedClasses.Add(pickCls);
                        break;
                    }
                }

                s.Party[i].active = true;
                s.Party[i].isHuman = false;
                s.Party[i].classId = pickCls;
                s.Party[i].buildId = 0;
                s.Party[i].level = Mathf.Max(1, s.Profile.level);
                s.Party[i].playerName = GetDefaultAICompanionName(pickCls, i);
                s.Party[i].ready = true;
            }
        }

        public static string GetDefaultAICompanionName(int classId, int slotIndex)
        {
            switch (classId)
            {
                case 0: return "Lyra (AI Knight)";
                case 5: return "Kael (AI Mage)";
                case 4: return "Seraphina (AI Ranger)";
                case 9: return "Arthur (AI Cleric)";
                case 1: return "Gideon (AI Guardian)";
                case 8: return "Sylvan (AI Druid)";
                default:
                    string[] fallback = { "Arthur (AI)", "Lyra (AI)", "Kael (AI)", "Seraphina (AI)" };
                    return fallback[Mathf.Clamp(slotIndex, 0, 3)];
            }
        }

        public static int GetHumanPlayerCount()
        {
            var s = GameSession.I;
            if (s == null || s.Party == null) return 1;
            int count = 0;
            for (int i = 0; i < 4; i++)
                if (s.Party[i] != null && s.Party[i].active && s.Party[i].isHuman) count++;
            return Mathf.Max(1, count);
        }

        public static int GetAICompanionCount()
        {
            var s = GameSession.I;
            if (s == null || s.Party == null) return 3;
            int count = 0;
            for (int i = 0; i < 4; i++)
                if (s.Party[i] != null && s.Party[i].active && !s.Party[i].isHuman) count++;
            return count;
        }

        private static void ResolveEndpoint(string input, int defaultPort, out string ip, out int port)
        {
            ip = "127.0.0.1";
            port = defaultPort;
            if (string.IsNullOrWhiteSpace(input)) return;

            string trimmed = input.Trim();
            // Support Room Code format: "ABYSS-<lastOctet>-<port>" or "ABYSS-<port>"
            if (trimmed.StartsWith("ABYSS", StringComparison.OrdinalIgnoreCase))
            {
                string[] tokens = trimmed.Split('-');
                string localIp = LanRoomBridge.GetLocalIPv4();
                string[] ipParts = localIp.Split('.');
                if (tokens.Length >= 3 && ipParts.Length == 4 && int.TryParse(tokens[1], out int octet))
                {
                    ip = $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}.{Mathf.Clamp(octet, 1, 254)}";
                    if (int.TryParse(tokens[2], out int parsedP)) port = parsedP;
                }
                else
                {
                    ip = "127.0.0.1";
                }
                return;
            }

            if (trimmed.Contains(":"))
            {
                string[] parts = trimmed.Split(':');
                ip = string.IsNullOrWhiteSpace(parts[0]) ? "127.0.0.1" : parts[0].Trim();
                if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out int p))
                    port = p;
            }
            else
            {
                ip = trimmed;
            }
        }
    }

    /// <summary>
    /// Smoothly interpolates remote network player position, rotation, and walk/run animation blend
    /// between 12.5Hz network ticks so Co-op partners move fluidly at 60+ FPS without stepping.
    /// </summary>
    public class RemoteNetInterpolator : MonoBehaviour
    {
        private ActorUnit _unit;
        private Vector3 _targetPos;
        private Quaternion _targetRot = Quaternion.identity;
        private float _targetAnimSpeed;
        private float _smoothedAnimSpeed;
        private bool _initialized;
        private static readonly int HashSpeed = Animator.StringToHash("Speed");

        private void Awake()
        {
            _unit = GetComponent<ActorUnit>();
            _targetPos = transform.position;
            _targetRot = transform.rotation;
        }

        public void PushNetworkState(Vector3 pos, float yaw, float moveSpeedRatio)
        {
            if (!_initialized || Vector3.Distance(transform.position, pos) > 7.5f)
            {
                if (_unit != null && _unit.Controller != null) _unit.Controller.enabled = false;
                transform.position = pos;
                if (_unit != null && _unit.Controller != null) _unit.Controller.enabled = true;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                _initialized = true;
            }
            _targetPos = pos;
            _targetRot = Quaternion.Euler(0f, yaw, 0f);
            _targetAnimSpeed = Mathf.Clamp01(moveSpeedRatio);
        }

        private void Update()
        {
            if (!_initialized) return;
            if (_unit != null && _unit.IsDead) return;

            float dt = Time.deltaTime;
            Vector3 prev = transform.position;
            Vector3 next = Vector3.Lerp(prev, _targetPos, Mathf.Clamp01(dt * 15f));
            transform.position = next;
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, Mathf.Clamp01(dt * 18f));

            float planarStepSpeed = Vector2.Distance(new Vector2(prev.x, prev.z), new Vector2(next.x, next.z)) / Mathf.Max(0.0001f, dt);
            float maxSpd = _unit != null ? Mathf.Max(1f, _unit.EffectiveMoveSpeed()) : 6f;
            float inferredRatio = Mathf.Clamp01(planarStepSpeed / maxSpd);
            float desiredAnim = Mathf.Max(_targetAnimSpeed, inferredRatio);

            _smoothedAnimSpeed = Mathf.Lerp(_smoothedAnimSpeed, desiredAnim, Mathf.Clamp01(dt * 12f));
            if (_unit != null && _unit.Anim != null)
            {
                _unit.Anim.SetFloat(HashSpeed, _smoothedAnimSpeed < 0.03f ? 0f : _smoothedAnimSpeed);
            }
        }
    }

    [Serializable]
    public class LobbyNetMsg
    {
        // "join", "slot", "sync", "start", "hero_pos", "hero_skill", "hero_action", "boss_sync", "puzzle_pillar", "loot_drop"
        public string op;
        public int slot;
        public string name;
        public int classId;
        public int buildId;
        public bool ready;
        public int floor = 1;
        public PartySlotInfo[] party;

        // Real-time Dungeon Co-op Synchronization payload
        public float px, py, pz, yaw;
        public float aimX, aimY, aimZ;
        public float hp, maxHp, mana, maxMana, shield, moveRatio;
        public int skillSlot = -1;
        public string actionKind; // "basic", "heavy", "dodge"
        public int roomIdx = -1;
        public int pillarIdx = -1;
        public int minRarity = 0;
        public float luckBonus = 0f;
        public float bossHp = -1f;
        public int bossPhase = 1;
    }

    /// <summary>
    /// Real TCP LAN / Co-op Lobby & Dungeon Bridge (127.0.0.1 or LAN IPv4, default port 7777)
    /// that syncs party slots, class selection, ready state, smooth interpolated movement,
    /// skill animations, party HP/MP bars, puzzle pillar states, and Instanced Loot,
    /// while preserving 100% standalone offline Single Player operation.
    /// </summary>
    public class LanRoomBridge : MonoBehaviour
    {
        private static LanRoomBridge _inst;
        public static bool HasInstance => _inst != null;

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
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                        _inst = go.AddComponent<LanRoomBridge>();
                    }
                }
                return _inst;
            }
        }

        public bool IsHosting { get; private set; }
        public bool IsConnectedClient { get; private set; }
        public int LocalSlotIndex { get; private set; } = 0;
        public int ActivePort { get; private set; } = 7777;
        public string StatusText = "Chế độ: Chơi Đơn (1 Người Chơi + 3 AI Đồng Đội)";

        private TcpListener _listener;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private readonly Dictionary<TcpClient, int> _clientSlotMap = new Dictionary<TcpClient, int>();
        private TcpClient _clientConn;
        private StreamWriter _clientWriter;
        private readonly ConcurrentQueue<string> _inbox = new ConcurrentQueue<string>();
        private volatile bool _running;

        private void Awake()
        {
            if (_inst != null && _inst != this) { BakedAssetRegistry.DestroySafe(gameObject); return; }
            _inst = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit() => StopNetwork();
        private void OnDestroy() { if (_inst == this) StopNetwork(); }

        public int ConnectedClientCount
        {
            get { lock (_clients) return _clients.Count; }
        }

        public bool StartHost(int port = 7777)
        {
            StopNetwork();
            try
            {
                ActivePort = port;
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _running = true;
                IsHosting = true;
                LocalSlotIndex = 0;
                string ip = GetLocalIPv4();
                StatusText = $"HOST CO-OP ĐANG MỞ — IP: {ip}:{port} | Mã phòng: {GameSession.I.RoomCode}";
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
                ActivePort = port;
                string targetIp = string.IsNullOrWhiteSpace(hostIp) ? "127.0.0.1" : hostIp.Trim();
                _clientConn = new TcpClient();
                var ar = _clientConn.BeginConnect(targetIp, port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2.0)))
                {
                    _clientConn.Close();
                    StatusText = $"Không tìm thấy phòng Co-op tại {targetIp}:{port}";
                    return false;
                }
                _clientConn.EndConnect(ar);
                _running = true;
                IsConnectedClient = true;
                var stream = _clientConn.GetStream();
                _clientWriter = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                ThreadPool.QueueUserWorkItem(_ => ReadStreamLoop(stream, null));

                SendToHost(new LobbyNetMsg
                {
                    op = "join",
                    name = GameSession.I.Profile.heroName,
                    classId = GameSession.I.Profile.selectedClass,
                    buildId = GameSession.I.Profile.selectedBuild,
                    ready = true
                });
                StatusText = $"ĐÃ KẾT NỐI CO-OP ({targetIp}:{port}) — Loot Riêng: BẬT";
                return true;
            }
            catch (Exception e)
            {
                StatusText = "Lỗi kết nối Co-op: " + e.Message;
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
                _clientSlotMap.Clear();
            }
            try { _clientConn?.Close(); } catch { }
            _clientConn = null;
            _clientWriter = null;
            StatusText = "Chế độ: Chơi Đơn (1 Người Chơi + 3 AI Đồng Đội)";
        }

        private void AcceptLoop()
        {
            while (_running && _listener != null)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    client.NoDelay = true;
                    lock (_clients) _clients.Add(client);
                    var stream = client.GetStream();
                    ThreadPool.QueueUserWorkItem(_ => ReadStreamLoop(stream, client));
                }
                catch { break; }
            }
        }

        private void ReadStreamLoop(NetworkStream stream, TcpClient sourceClient)
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
            finally
            {
                if (IsHosting && sourceClient != null)
                {
                    lock (_clients)
                    {
                        _clients.Remove(sourceClient);
                    }
                }
            }
        }

        public void BroadcastFromHost()
        {
            if (!IsHosting) return;
            var msg = new LobbyNetMsg
            {
                op = "sync",
                name = GameSession.I.RoomCode,
                floor = GameSession.I.Profile.selectedFloor,
                party = GameSession.I.Party
            };
            BroadcastRaw(JsonUtility.ToJson(msg));
        }

        public void BroadcastStartDungeon()
        {
            if (!IsHosting) return;
            var msg = new LobbyNetMsg
            {
                op = "start",
                name = GameSession.I.RoomCode,
                floor = GameSession.I.Profile.selectedFloor,
                party = GameSession.I.Party
            };
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
            int humans = GameModeManager.GetHumanPlayerCount();
            int ais = GameModeManager.GetAICompanionCount();
            if (IsHosting)
                return $"CO-OP HOST ({humans} Người + {ais} AI | Mã: {GameSession.I.RoomCode} | Loot Riêng)";
            if (IsConnectedClient)
                return $"CO-OP CLIENT (Slot #{LocalSlotIndex + 1} | {humans} Người + {ais} AI | Loot Riêng)";
            if (GameModeManager.IsMultiplayerCoOp)
                return $"CHẾ ĐỘ CO-OP ({humans} Người + {ais} AI | Loot Riêng)";
            return $"CHƠI ĐƠN OFFLINE (1 Người + {ais} AI Đồng Đội Thông Minh)";
        }

        public void BroadcastHeroSkill(int slotIdx, int skillIndex, Vector3 aimPoint = default)
        {
            if (!IsHosting && !IsConnectedClient) return;
            var msg = new LobbyNetMsg
            {
                op = "hero_skill",
                slot = slotIdx,
                skillSlot = skillIndex,
                aimX = aimPoint.x,
                aimY = aimPoint.y,
                aimZ = aimPoint.z
            };
            if (IsHosting) BroadcastRaw(JsonUtility.ToJson(msg));
            else SendToHost(msg);
        }

        public void BroadcastHeroAction(int slotIdx, string actionKind, Vector3 aimDir, Vector3 aimPoint)
        {
            if (!IsHosting && !IsConnectedClient) return;
            var msg = new LobbyNetMsg
            {
                op = "hero_action",
                slot = slotIdx,
                actionKind = actionKind,
                px = aimDir.x,
                py = aimDir.y,
                pz = aimDir.z,
                aimX = aimPoint.x,
                aimY = aimPoint.y,
                aimZ = aimPoint.z
            };
            if (IsHosting) BroadcastRaw(JsonUtility.ToJson(msg));
            else SendToHost(msg);
        }

        public void BroadcastPuzzlePillarActivated(int roomIdx, int pillarIdx)
        {
            if (!IsHosting && !IsConnectedClient) return;
            var msg = new LobbyNetMsg
            {
                op = "puzzle_pillar",
                roomIdx = roomIdx,
                pillarIdx = pillarIdx
            };
            if (IsHosting) BroadcastRaw(JsonUtility.ToJson(msg));
            else SendToHost(msg);
        }

        public void BroadcastInstancedLootDrop(Vector3 worldPos, int floor, int minRarity, float luckBonus)
        {
            if (!IsHosting) return;
            var msg = new LobbyNetMsg
            {
                op = "loot_drop",
                px = worldPos.x,
                py = worldPos.y,
                pz = worldPos.z,
                floor = floor,
                minRarity = minRarity,
                luckBonus = luckBonus
            };
            BroadcastRaw(JsonUtility.ToJson(msg));
        }

        private void TickDungeonStateSync()
        {
            if (!IsHosting && !IsConnectedClient) return;
            var dw = DungeonWorldBuilder.Instance;
            if (dw == null || dw.LocalHero == null) return;

            _netTickTimer -= Time.unscaledDeltaTime;
            if (_netTickTimer > 0f) return;
            _netTickTimer = 0.065f; // ~15.4 Hz smooth network tick

            if (IsHosting)
            {
                // Host broadcasts its own hero + all AI companion states so clients see all 4 party members smoothly
                for (int i = 0; i < dw.PartyUnits.Count; i++)
                {
                    var u = dw.PartyUnits[i];
                    if (u == null) continue;
                    bool isHostOrAI = (i == LocalSlotIndex) || (i < GameSession.I.Party.Length && !GameSession.I.Party[i].isHuman);
                    if (!isHostOrAI) continue;

                    BroadcastRaw(JsonUtility.ToJson(BuildHeroPosMsg(i, u)));
                }

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
                SendToHost(BuildHeroPosMsg(LocalSlotIndex, dw.LocalHero));
            }
        }

        private static LobbyNetMsg BuildHeroPosMsg(int slotIndex, ActorUnit h)
        {
            float moveRatio = 0f;
            if (h.Anim != null)
                moveRatio = h.Anim.GetFloat("Speed");

            return new LobbyNetMsg
            {
                op = "hero_pos",
                slot = slotIndex,
                px = h.transform.position.x,
                py = h.transform.position.y,
                pz = h.transform.position.z,
                yaw = h.transform.eulerAngles.y,
                hp = h.Hp,
                maxHp = h.MaxHp,
                mana = h.Mana,
                maxMana = h.MaxMana,
                shield = h.Shield,
                moveRatio = moveRatio
            };
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
                    var interp = unit.GetComponent<RemoteNetInterpolator>();
                    if (interp != null)
                    {
                        interp.PushNetworkState(new Vector3(msg.px, msg.py, msg.pz), msg.yaw, msg.moveRatio);
                    }
                    else
                    {
                        unit.transform.position = Vector3.Lerp(unit.transform.position, new Vector3(msg.px, msg.py, msg.pz), 0.55f);
                        unit.transform.rotation = Quaternion.Euler(0f, msg.yaw, 0f);
                    }

                    if (msg.maxHp > 1f) unit.MaxHp = msg.maxHp;
                    if (msg.maxMana > 1f) unit.MaxMana = msg.maxMana;
                    unit.Hp = Mathf.Clamp(msg.hp, 1f, unit.MaxHp);
                    unit.Mana = Mathf.Clamp(msg.mana, 0f, unit.MaxMana);
                    unit.Shield = Mathf.Max(0f, msg.shield);
                }
            }
            else if (msg.op == "hero_skill" && msg.slot >= 0 && msg.slot < dw.PartyUnits.Count && msg.slot != LocalSlotIndex)
            {
                var unit = dw.PartyUnits[msg.slot];
                if (unit != null && !unit.IsDead)
                {
                    unit.SkillCds[Mathf.Clamp(msg.skillSlot, 0, 3)] = 0f;
                    unit.Mana = unit.MaxMana;
                    Vector3 aimPt = new Vector3(msg.aimX, msg.aimY, msg.aimZ);
                    if (aimPt.sqrMagnitude < 0.01f) aimPt = unit.transform.position + unit.transform.forward * 4f;
                    unit.TryUseSkill(msg.skillSlot, aimPt);
                }
            }
            else if (msg.op == "hero_action" && msg.slot >= 0 && msg.slot < dw.PartyUnits.Count && msg.slot != LocalSlotIndex)
            {
                var unit = dw.PartyUnits[msg.slot];
                if (unit != null && !unit.IsDead)
                {
                    Vector3 aimDir = new Vector3(msg.px, msg.py, msg.pz);
                    Vector3 aimPt = new Vector3(msg.aimX, msg.aimY, msg.aimZ);
                    if (msg.actionKind == "basic")
                    {
                        unit.BasicAttackCd = 0f;
                        unit.TryBasicAttack(aimDir, aimPt);
                    }
                    else if (msg.actionKind == "heavy")
                    {
                        unit.HeavyAttackCd = 0f;
                        unit.BasicAttackCd = 0f;
                        unit.TryHeavyAttack(aimDir, aimPt);
                    }
                    else if (msg.actionKind == "dodge")
                    {
                        unit.DodgeCd = 0f;
                        unit.TryDodge(aimDir);
                    }
                }
            }
            else if (msg.op == "puzzle_pillar")
            {
                dw.ApplyRemotePuzzlePillarActivation(msg.roomIdx, msg.pillarIdx);
            }
            else if (msg.op == "loot_drop" && IsConnectedClient)
            {
                // Instanced Loot: Client rolls its own independent personal item at the drop position!
                EquipItem personalItem = LootRoller.RollItem(Mathf.Max(1, msg.floor), msg.minRarity, msg.luckBonus);
                LootOrbPickup.Spawn(new Vector3(msg.px, msg.py, msg.pz), personalItem);
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
                            // Automatically replace an AI slot (1..3) with the joining human player
                            int targetSlot = 1;
                            for (int i = 1; i < 4; i++)
                            {
                                if (!GameSession.I.Party[i].active || !GameSession.I.Party[i].isHuman)
                                {
                                    targetSlot = i;
                                    break;
                                }
                            }

                            string cleanName = string.IsNullOrWhiteSpace(msg.name) ? $"Hunter {targetSlot + 1}" : msg.name.Trim();
                            GameSession.I.Party[targetSlot] = new PartySlotInfo
                            {
                                active = true,
                                isHuman = true,
                                playerName = cleanName,
                                classId = Mathf.Clamp(msg.classId, 0, 9),
                                buildId = Mathf.Clamp(msg.buildId, 0, 2),
                                level = Mathf.Max(1, GameSession.I.Profile.level),
                                ready = true
                            };
                            StatusText = $"CO-OP HOST ({GameModeManager.GetHumanPlayerCount()} Người + {GameModeManager.GetAICompanionCount()} AI) | Mã: {GameSession.I.RoomCode}";
                            GameSession.I.NotifyPartyChanged();
                            BroadcastFromHost();
                        }
                        else if (msg.op == "slot" && msg.slot >= 1 && msg.slot < 4)
                        {
                            var s = GameSession.I.Party[msg.slot];
                            s.isHuman = true;
                            if (!string.IsNullOrWhiteSpace(msg.name))
                                s.playerName = msg.name.Trim();
                            s.classId = Mathf.Clamp(msg.classId, 0, 9);
                            s.buildId = Mathf.Clamp(msg.buildId, 0, 2);
                            s.ready = msg.ready;
                            GameSession.I.NotifyPartyChanged();
                            BroadcastFromHost();
                        }
                        else if (msg.op == "hero_pos" || msg.op == "hero_skill" || msg.op == "hero_action" || msg.op == "puzzle_pillar")
                        {
                            ApplyRemoteDungeonSync(msg);
                            BroadcastRaw(json);
                        }
                    }
                    else if (IsConnectedClient)
                    {
                        if (msg.op == "hero_pos" || msg.op == "boss_sync" || msg.op == "hero_skill" ||
                            msg.op == "hero_action" || msg.op == "puzzle_pillar" || msg.op == "loot_drop")
                        {
                            ApplyRemoteDungeonSync(msg);
                        }
                        else if (msg.op == "sync" && msg.party != null && msg.party.Length == 4)
                        {
                            GameSession.I.Party = msg.party;
                            if (!string.IsNullOrEmpty(msg.name)) GameSession.I.RoomCode = msg.name;
                            GameSession.I.Profile.selectedFloor = msg.floor;
                            for (int i = 1; i < 4; i++)
                            {
                                if (msg.party[i] != null && msg.party[i].isHuman &&
                                    msg.party[i].playerName.StartsWith(GameSession.I.Profile.heroName, StringComparison.OrdinalIgnoreCase))
                                {
                                    LocalSlotIndex = i;
                                }
                            }
                            GameSession.I.NotifyPartyChanged();
                        }
                        else if (msg.op == "start")
                        {
                            if (msg.party != null && msg.party.Length == 4) GameSession.I.Party = msg.party;
                            if (!string.IsNullOrEmpty(msg.name)) GameSession.I.RoomCode = msg.name;
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
