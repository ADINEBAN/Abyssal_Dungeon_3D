using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssalDungeon.Multiplayer
{
    [Serializable]
    public class PartyMember
    {
        public GameObject gameObject;
        public MonoBehaviour playerStats; // Mocked
        public MonoBehaviour classData; // Mocked
        public string name;
        public bool isAlive;
    }

    public class PartyManager : MonoBehaviour
    {
        public static PartyManager Instance { get; private set; }

        public List<PartyMember> members = new List<PartyMember>();

        public event Action<PartyMember> OnPartyMemberAdded;
        public event Action<PartyMember> OnPartyMemberDied;
        public event Action<PartyMember> OnPartyMemberRevived;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AddMember(PartyMember member)
        {
            if (members.Count < 4 && !members.Contains(member))
            {
                members.Add(member);
                OnPartyMemberAdded?.Invoke(member);
            }
        }

        public void RemoveMember(int index)
        {
            if (index >= 0 && index < members.Count)
            {
                members.RemoveAt(index);
            }
        }

        public PartyMember GetMember(int index)
        {
            if (index >= 0 && index < members.Count)
            {
                return members[index];
            }
            return null;
        }

        public List<PartyMember> GetAllAliveMembers()
        {
            List<PartyMember> aliveMembers = new List<PartyMember>();
            foreach (var member in members)
            {
                if (member.isAlive && member.gameObject != null)
                {
                    aliveMembers.Add(member);
                }
            }
            return aliveMembers;
        }

        public PartyMember GetWeakestMember()
        {
            // Mocking lowest HP logic
            PartyMember weakest = null;
            foreach (var member in GetAllAliveMembers())
            {
                if (weakest == null)
                {
                    weakest = member;
                }
            }
            return weakest;
        }

        public PartyMember GetIsolatedMember()
        {
            Vector3 center = GetPartyCenter();
            PartyMember isolated = null;
            float maxDist = -1f;

            foreach (var member in GetAllAliveMembers())
            {
                float dist = Vector3.Distance(member.gameObject.transform.position, center);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    isolated = member;
                }
            }
            return isolated;
        }

        public Vector3 GetPartyCenter()
        {
            Vector3 center = Vector3.zero;
            var aliveMembers = GetAllAliveMembers();
            
            if (aliveMembers.Count == 0) return center;

            foreach (var member in aliveMembers)
            {
                center += member.gameObject.transform.position;
            }
            
            return center / aliveMembers.Count;
        }

        public void SpawnPartyMembers(Vector3[] spawnPoints)
        {
            if (MultiplayerManager.Instance == null) return;
            
            int spawnIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                var slot = MultiplayerManager.Instance.slots[i];
                if (slot.isOccupied)
                {
                    Vector3 pos = spawnIndex < spawnPoints.Length ? spawnPoints[spawnIndex] : Vector3.zero;
                    GameObject memberGo = new GameObject(slot.playerName);
                    memberGo.transform.position = pos;
                    
                    if (!slot.isLocal)
                    {
                        var ai = memberGo.AddComponent<AIPartyMember>();
                        ai.Initialize(slot.classIndex);
                    }

                    PartyMember newMember = new PartyMember
                    {
                        gameObject = memberGo,
                        name = slot.playerName,
                        isAlive = true
                    };

                    AddMember(newMember);
                    spawnIndex++;
                }
            }
        }
        
        public void NotifyMemberDied(PartyMember member)
        {
            if (member != null && member.isAlive)
            {
                member.isAlive = false;
                OnPartyMemberDied?.Invoke(member);
            }
        }
        
        public void NotifyMemberRevived(PartyMember member)
        {
            if (member != null && !member.isAlive)
            {
                member.isAlive = true;
                OnPartyMemberRevived?.Invoke(member);
            }
        }
    }
}
