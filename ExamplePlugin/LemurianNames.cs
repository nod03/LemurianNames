using BepInEx;
using R2API;
using RoR2;
using RoR2.Stats;
using System.IO;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Reflection;
using System.Runtime.CompilerServices;
using IL.RoR2.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using R2API.Networking.Interfaces;
using R2API.Networking;
using RoR2.Networking;
using Facepunch.Steamworks;

namespace LemurianNames
{
    [BepInDependency(NetworkingAPI.PluginGUID)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LemurianNames : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "bouncyshield";
        public const string PluginName = "LemurianNames";
        public const string PluginVersion = "1.0.2";

        public void Awake()
        {
            Log.Init(Logger);

            NetworkingAPI.RegisterMessageType<Sink>();

            On.DevotedLemurianController.InitializeDevotedLemurian += NameFriend;
            On.RoR2.DevotionInventoryController.EvolveDevotedLumerian += UpdateNameFriend;
            On.RoR2.CharacterBody.GetDisplayName += EnforceNameFriend;
        }

        private System.Random random = new();

        private string[] prefixes =
        [
            "Fat", "Big", "Large", "Huge", "Jumbo", "Heavy", "Serious", "Prime", "Super", "King",
            "Epic", "Grand", "Elder", "Evil", "Solid"
        ];

        private string[] fnames =
        [
            "James", "Jamie", "Michael", "Mike", "Mikey", "Robert", "Bob", "Bobby", "John", "Johnny",
            "David", "Dave", "Davie", "William", "Bill", "Will", "Billy", "Richard", "Dick", "Joseph",
            "Joe", "Joey", "Thomas", "Tom", "Tommy", "Christopher", "Chris", "Charles", "Charlie", "Daniel",
            "Dan", "Danny", "Matthew", "Matt", "Anthony", "Ant", "Tony", "Mark", "Donald", "Don",
            "Steven", "Steve", "Andrew", "Andy", "Paul", "Joshua", "Josh", "Kenneth", "Ken", "Kevin",
            "Kev", "Brian", "Timothy", "Tim", "Ronald", "Ron", "George", "Jason", "Edward", "Ed",
            "Eddie", "Jeffrey", "Geoffrey", "Jeff", "Geoff", "Ryan", "Jacob", "Nicholas", "Nick", "Gary",
            "Eric", "Jonathan", "Stephen", "Larry", "Justin", "Scott", "Brandon", "Benjamin", "Ben", "Samuel",
            "Sam", "Gregory", "Greg", "Alexander", "Alex", "Patrick", "Pat", "Frank", "Raymond", "Ray",
            "Jack", "Dennis", "Jerry", "Tyler", "Aaron", "Jose", "Adam", "Nathan", "Henry", "Zachary",
            "Zach", "Douglas", "Doug", "Peter", "Pete", "Kyle", "Noah", "Ethan", "Jeremy", "Christian",
            "Walter", "Walt", "Keith", "Austin", "Roger", "Terry", "Sean", "Gerald", "Carl", "Dylan",
            "Harold", "Jordan", "Bryan", "Lawrence", "Arthur", "Gabriel", "Gabe", "Bruce", "Logan", "Billy",
            "Joe", "Alan", "Juan", "Elijah", "Willie", "Randy", "Mason", "Vincent", "Vince", "Liam",
            "Lee", "Roy", "Bobby", "Bradley", "Brad", "Russell", "Russ", "Lucas", "Luke", "Guy",
            "Friend", "Burger", "Martin", "Hugo", "Mateo", "Leo", "Manuel", "Pablo", "Enzo",
            "Alvaro", "Mario", "Adrian", "Diego", "Bruno", "Oliver", "Marco", "Antonio", "Miguel", "Javier", 
            "Luca", "Liam", "Gael", "Carlos", "Jose", "Sergio", "Jorge", "Dario", "Hector", "Rodrigo", "Paul", 
            "Jesus", "Guillermo", "Jaime", "Luis", "Ian", "Mohamed", "Pedro", "Amir", "Ruben", "Ivan", "Saul", 
            "Raul", "Joel", "Alonso", "Fernando", "Isaac", "Fabio", "Ignacio", "Nacho", "Max", "Leonardo", 
            "Donatello", "Raphael", "Michelangelo", "Snake"
        ];

        public static Dictionary<CharacterMaster, string> friends = [];

        private bool rc = false;
        private void NameFriend(On.DevotedLemurianController.orig_InitializeDevotedLemurian orig, DevotedLemurianController self, ItemIndex x, DevotionInventoryController y)
        {
            orig.Invoke(self, x, y);
            CharacterMaster lemurianMaster = self._lemurianMaster;
            string text;
            if (!rc)
            {
                text = "Rodrigo";
                rc = true;
            }
            else
            {
                text = fnames[random.Next(fnames.Length)];
            }
            friends.Add(lemurianMaster, text);
            if (NetworkServer.active)
            {
                NetMessageExtensions.Send((INetMessage)(object)new Sink(lemurianMaster, text), (NetworkDestination)1);
            }
        }

        private bool kc = false;
        private void UpdateNameFriend(On.RoR2.DevotionInventoryController.orig_EvolveDevotedLumerian orig, DevotionInventoryController self, DevotedLemurianController x)
        {
            orig.Invoke(self, x);
            if (x.DevotedEvolutionLevel == 2)
            {
                CharacterMaster lemurianMaster = x._lemurianMaster;
                string text;
                if (!kc)
                {
                    text = "King " + friends[lemurianMaster];
                    kc = true;
                }
                else
                {
                    text = prefixes[random.Next(prefixes.Length)] + " " + friends[lemurianMaster];
                }
                if (text == "King Burger") { text = "Burger King"; }
                friends[lemurianMaster] = text;
                if (NetworkServer.active)
                {
                    NetMessageExtensions.Send((INetMessage)(object)new Sink(lemurianMaster, text), (NetworkDestination)1);
                }
            }
        }

        private string EnforceNameFriend(On.RoR2.CharacterBody.orig_GetDisplayName orig, CharacterBody self)
        {
            try
            {
                return friends[self._master];
            }
            catch
            {
                return orig.Invoke(self);
            }
        }

        public class Sink : INetMessage, ISerializableObject
        {
            private NetworkInstanceId index;

            private string name;

            public Sink()
            {
            }

            public Sink(CharacterMaster charMas, string name)
            {
                index = charMas.networkIdentity.netId;
                this.name = name;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(index);
                writer.Write(name);
            }

            public void Deserialize(NetworkReader reader)
            {
                index = reader.ReadNetworkId();
                name = reader.ReadString();
            }

            public void OnReceived()
            {
                if (!NetworkServer.active)
                {
                    Log.Info($"Recieved {index} to {name} from host");
                    CharacterMaster key = CharacterMaster.instancesList.Find((CharacterMaster x) => x.networkIdentity.netId == index);
                    if (friends.ContainsKey(key))
                    {
                        friends[key] = name;
                    }
                    else
                    {
                        friends.Add(key, name);
                    }
                }
            }
        }
    }
}