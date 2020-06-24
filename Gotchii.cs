using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;

namespace Tamabot{

    [Serializable()]
    public class Gotchii{
        //this class is going to contain the data for each person's pet, then i'm just going to store a list of pets somewhere
        // idk how that really works though so /shrug

        // THESE NEED TO BE PUBLIC SO THEY CAN BE SERIALIZED
        public int play;
        public int poop;
        public int hunger;
        public string name;
        public int petID; // this is going to hold which pet it is, eg. like a white dog, black cat, etc.
        public Int64 lastPlayTime;
        public Int64 lastCleanTime;
        public Int64 lastFeedTime;
        public int exp;
        public Rarity rarity;
        public DateTimeOffset sitterEndTime;
        public int boostsRemaining;
        [NonSerialized()] private static readonly int TWOPOINTFOUR_HOURS = 8640;
        [NonSerialized()] private static DateTimeOffset currentTime;
        // [NonSerialized()] private string[,] pets = {
        //     {"cat", "https://cdn.discordapp.com/attachments/709449720375804105/709463989582692502/EXb2gILUwAAhfcP.png"}, // conk zero
        //     {"dog", "https://cdn.discordapp.com/attachments/709449720375804105/709464988900655174/2Q.png"},
        //     {"birb", "https://cultofthepartyparrot.com/parrots/hd/parrot.gif"},
        //     {"gwa gwa", "https://i.redd.it/mauk7le4i5j41.png"},
        //     {"ferret", "https://thumbs.gfycat.com/BreakableImperturbableAgama-max-1mb.gif"},
        //     {"Ein", "https://cdn.discordapp.com/attachments/709449720375804105/709449791699943534/ein.png"}};
        [NonSerialized()] Random rng = new Random();
        [NonSerialized()] private readonly int barTime = TWOPOINTFOUR_HOURS; // every 2 hours is 7200
        private static string[,] veryRarePets = {
            {"Ein", "https://cdn.discordapp.com/attachments/709449720375804105/709449791699943534/ein.png"},
            {"2004 Satin Silver Metallic Honda Civic Coupe", "https://cdn.discordapp.com/attachments/709449720375804105/713608089466830848/cnex7VQxdr4hwfWwRHE8PiKF96YgObDh44xYXaAeGwWeSSkSYc2WYInfszXKyWdKxfrYq2fLbtAWRdBeWYVzT3swl3kKaZO2.png"},
            {"strawberry", "https://lh3.googleusercontent.com/proxy/KXbqKTqb7S8MqD3VI-xBe8C4KXNgBzzBXOMmDK3R1QFW_TNhfJKiYGyyu5ykqoFD6gltqhvdk9FvCpGOTu0bFEiaoNor6OE7e_izYV_-LNdMrg"},
            {"owlbear", "https://cdn.discordapp.com/attachments/709449720375804105/713227966112595978/636252772225295187.png"},
            {"Ramiel", "https://vignette.wikia.nocookie.net/evangelion/images/0/08/Ramieldesign.png/revision/latest?cb=20130116210135"},
            {"furret but default dancing", "https://media1.tenor.com/images/b57ed8eda56b80d27c1ab2e64666c7c6/tenor.gif?itemid=14704368"},
            {"2002 Gold Dust Metallic Toyota Camry", "https://media.ed.edmunds-media.com/toyota/camry/2002/oem/2002_toyota_camry_sedan_le_fq_oem_1_500.jpg"}
        };
        private static string[,] rarePets = {
            {"gwa gwa", "https://i.redd.it/mauk7le4i5j41.png"},
            {"ferret", "https://thumbs.gfycat.com/BreakableImperturbableAgama-max-1mb.gif"},
            {"red panda", "https://cdn.discordapp.com/attachments/570811751826980868/713227350325723146/images.png"},
            {"turtle duck", "https://cdn.discordapp.com/attachments/709449720375804105/715724158176198656/360.png"},
            {"metroid", "https://cdn.discordapp.com/attachments/709449720375804105/725215568009101341/latest.png"},
            {"gnome", "https://cdn11.bigcommerce.com/s-59t8stv95a/images/stencil/1280x1280/products/1424/1392/agent-double-gnome-7-you-don-t-gno-me-3__63103.1527617597.jpg?c=2&imbypass=on"}
        };
        // ok so like not actual animals are going to be rare and very rare, that's the threshold
        private static string[,] uncommonPets = {
            {"birb", "https://cultofthepartyparrot.com/parrots/hd/parrot.gif"},
            {"fox", "https://cdn.discordapp.com/attachments/709449720375804105/712897628790325308/Vulpes_vulpes_ssp_fulvus_6568085.png"},
            {"tarantula", "https://cdn.discordapp.com/attachments/709449720375804105/712897969489444914/TARANTULAC-e1562523058923.png"},
            {"koala", "https://cdn.discordapp.com/attachments/709449720375804105/713227824755900516/Koala_climbing_tree.png"}, 
            {"fox", "https://cdn.discordapp.com/attachments/709449720375804105/725213483616174120/Vulpes_vulpes_ssp_fulvus_6568085.png"},
            {"bear", "https://cdn.discordapp.com/attachments/709449720375804105/725213959111573514/Image-w-cred-cap_-1200w-_-Brown-Bear-page_-brown-bear-in-fog_2_1.png"},
            {"elephant", "https://cdn.discordapp.com/attachments/709449720375804105/725216039146881024/94351084_2570825139826450_7563146999747313664_n.png"}
        };
        private static string[,] commonPets = {
            {"cat (@cokezerocat)", "https://cdn.discordapp.com/attachments/709449720375804105/709463989582692502/EXb2gILUwAAhfcP.png"}, // conk zero
            {"cheems", "https://cdn.discordapp.com/attachments/709449720375804105/709464988900655174/2Q.png"},
            {"cat (@primcesspamcake)", "https://cdn.discordapp.com/attachments/709449720375804105/712898563931373578/D4y6tCEWAAIyoW4.png"},
            {"frog", "https://cdn.discordapp.com/attachments/709449720375804105/712901994024534026/pet-frog-names.png"},
            {"raccoon", "https://cdn.discordapp.com/attachments/709449720375804105/713226973887070318/raccoon_thumb.png"},
            {"goldfish", "https://cdn.discordapp.com/attachments/709449720375804105/713227250668929105/imageService.png"},
            {"rat", "https://cdn.discordapp.com/attachments/709449720375804105/713227390792237098/tenor.png"}
        };

        private Dictionary<Rarity, string[,]> pets = new Dictionary<Rarity, string[,]>(){
            {Rarity.VeryRare, veryRarePets},
            {Rarity.Rare, rarePets},
            {Rarity.Uncommon, uncommonPets},
            {Rarity.Common, commonPets}
        };

        public enum Rarity {
            Hidden = 0,
            Basic = 1,
            Common = 2,
            Uncommon = 3,
            Rare = 4,
            VeryRare = 5
        }
        private KeyValuePair<Rarity, int>[] ITEM_RARITY_PROBS = {
            new KeyValuePair<Rarity, int>(Rarity.VeryRare, 5),
            new KeyValuePair<Rarity, int>(Rarity.Rare, 15), 
            new KeyValuePair<Rarity, int>(Rarity.Uncommon, 30), 
            new KeyValuePair<Rarity, int>(Rarity.Common, 50)
            //new KeyValuePair<Rarity, int>(Rarity.Basic, 30) 
        };
        // shoutout to Ben Dangelo for this code lol



        public Gotchii(){
            //this.userID = discordID;

            // probably set the name to the default name of the random pet list
            play = 10;
            poop = 10;
            hunger = 10;
            // petID = rng.Next(pets.Length);
            // name = pets[petID, 0]; // sets the default name of the pet
            currentTime = DateTimeOffset.UtcNow;
            lastPlayTime = currentTime.ToUnixTimeSeconds();
            lastFeedTime = currentTime.ToUnixTimeSeconds();
            lastCleanTime = currentTime.ToUnixTimeSeconds();

            // gets a pet based on rarity
            rarity = this.GetRandomRarity();
            // Console.WriteLine("gotchii1");
            petID = rng.Next(pets[rarity].GetLength(0)); // petID is now the position of the pet in the rarity string
            // Console.WriteLine("petID = " + petID + " DebugRarity = " + rarity);
            // Console.WriteLine("Rarity string" + pets[rarity].ToString() + " " + pets[rarity].GetLength(0));
            name = pets[rarity][petID,0];
            Console.WriteLine("DebugRarity = " + rarity);

            sitterEndTime=DateTimeOffset.UtcNow.AddHours(-1000); 
            boostsRemaining = 0;
        }

        public Gotchii(Rarity rarity, int petID){
            play = 10;
            poop = 10;
            hunger = 10;
            currentTime = DateTimeOffset.UtcNow;
            lastPlayTime = currentTime.ToUnixTimeSeconds();
            lastFeedTime = currentTime.ToUnixTimeSeconds();
            lastCleanTime = currentTime.ToUnixTimeSeconds();

            this.rarity = rarity;
            this.petID = petID;
            name = pets[rarity][petID,0];
            sitterEndTime=DateTimeOffset.UtcNow.AddHours(-1000); 

            boostsRemaining = 0;
        }

        // don't really do these
        private int getPlay(){
            return play;
        }
        private int getHunger(){
            return hunger;
        }

        private int getClean(){
            return poop;
        }

        private string getName(){
            return name;
        }

        /// <summary>Checks if the Tama is still alive</summary>
        public Boolean update(){
            // janky debugging
            // Console.WriteLine("lastPlayTime = " + lastPlayTime);
            // Console.WriteLine("lastFeedTime = " + lastFeedTime);
            // Console.WriteLine("lastCleanTime = " + lastCleanTime);
            // Console.WriteLine("Current Time = " + DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            Console.WriteLine("update called");

            if(sitterEndTime != null){
                // if the sitter time exists
                if(sitterEndTime.CompareTo(DateTimeOffset.UtcNow) > 0){
                    play = 10;
                    hunger = 10;
                    poop = 10;
                    Console.WriteLine("debug sitter time left: " + (sitterEndTime - DateTimeOffset.UtcNow).Days + " days, " + (sitterEndTime - DateTimeOffset.UtcNow).Hours + " hours, " + (sitterEndTime - DateTimeOffset.UtcNow).Minutes + " minutes, and " + (sitterEndTime - DateTimeOffset.UtcNow).Seconds + " seconds.");
                    return true;
                } else {
                    Console.WriteLine("no sitter");
                    play = 10 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastPlayTime)/barTime; // every 10 seconds it's gonna go down by 1
                    hunger = 10 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastFeedTime)/barTime;
                    poop = 10 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastCleanTime)/barTime;

                    if(play < 0 || hunger < 0 || poop < 0){
                        return false;
                    }

                    return true;
                }
            } else {
                Console.WriteLine("no sitter time detected");
                play = 10 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastPlayTime)/barTime; // every 10 seconds it's gonna go down by 1
                hunger = 10 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastFeedTime)/barTime;
                poop = 10 - (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastCleanTime)/barTime;

                if(play < 0 || hunger < 0 || poop < 0){
                    return false;
                }

                return true;
            }
            
        }

        public string getMetrics(){
            // have a check at the top here for if it's still alive
            return getName() + "\n" + 
            "Hunger: " + getHunger() + "\n" + 
            "Cleanliness: " + getClean() + "\n" 
            + "Happiness: " + getPlay()  + "\n" 
            + "Level: " + (int)Math.Pow(exp, (1.0/2.0)) + "\n" 
            + "Rarity: " + rarity + "\n"
            + "Sitter time left: " + sitterString();
        }

        public string sitterString(){
            if(sitterEndTime.CompareTo(DateTimeOffset.UtcNow) > 0){
                return (sitterEndTime - DateTimeOffset.UtcNow).Days + " days, " + (sitterEndTime - DateTimeOffset.UtcNow).Hours + " hours, " + (sitterEndTime - DateTimeOffset.UtcNow).Minutes + " minutes, and " + (sitterEndTime - DateTimeOffset.UtcNow).Seconds + " seconds.";
            } else {
                return "No sitter hired.";
            }
        }

        public Boolean feed(){
            if(this.update()){
                hunger = 10;
                // reset feedTime
                lastFeedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return true;
            }
            return false; // feed failed; gotchii ran away
        }

        public Boolean playWith(){
            if(this.update()){
                play = 10;
                lastPlayTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return true;
            }
            return false;
        }

        public Boolean clean(){
            if(this.update()){
                poop = 10;
                lastCleanTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return true;
            }
            return false;
        }

        public void changeName(string name){
            this.name = name;
        }

        public string GetImage(){
            return(pets[this.rarity][this.petID, 1]);
        }

        public KeyValuePair<bool, int> Train(){
            if(boostsRemaining>0){
                boostsRemaining--;
                if(rng.Next(100) < 50){ // boosted to 50%
                    var expAmt = rng.Next(10, 25);
                    exp += expAmt;
                    return new KeyValuePair<bool, int>(true, expAmt);
                }
            } else {
                if(rng.Next(100) < 33){
                    var expAmt = rng.Next(10, 25);
                    exp += expAmt;
                    return new KeyValuePair<bool, int>(true, expAmt);
                }
            }

            return new KeyValuePair<bool, int>(false, 0);
        }

        private Rarity GetRandomRarity(){
            int cumulative = 0;

            for (int i = 0; i < ITEM_RARITY_PROBS.Length; i++) {
                cumulative += ITEM_RARITY_PROBS[i].Value;

                if (rng.Next(0,101) < cumulative) {
                    return ITEM_RARITY_PROBS[i].Key;
                }
            }

            return Rarity.Common;
        }

        public void SitterForDays(int days){
            //Console.WriteLine("sitterfordays input: " + days);
            if(sitterEndTime.CompareTo(DateTimeOffset.UtcNow) < 0){
                sitterEndTime = DateTimeOffset.UtcNow.AddDays(days);
            } else {
                sitterEndTime = sitterEndTime.AddDays(days);
            }
        }
        
        public KeyValuePair<int, int> GetExpLevel(){
            return new KeyValuePair<int, int>(exp, (int)Math.Pow(exp, (1.0/2.0)));
        }

        public static string GetFormattedCommonPets(){
            StringBuilder sb = new StringBuilder();
            sb.Append("Respond with a number from the following list: \n");
            for(int i = 0; i < commonPets.GetLength(0); i++){
                sb.Append((i + 1) + ". " + commonPets[i,0] + "\n");
            }

            return sb.ToString();
        }

        public static string GetFormattedUncommonPets(){
            StringBuilder sb = new StringBuilder();
            sb.Append("Respond with a number from the following list: \n");
            for(int i = 0; i < uncommonPets.GetLength(0); i++){
                sb.Append((i + 1) + ". " + uncommonPets[i,0] + "\n");
            }

            return sb.ToString();
        }

        public static string GetFormattedRarePets(){
            StringBuilder sb = new StringBuilder();
            sb.Append("Respond with a number from the following list: \n");
            for(int i = 0; i < rarePets.GetLength(0); i++){
                sb.Append((i + 1) + ". " + rarePets[i,0] + "\n");
            }

            return sb.ToString();
        }

        public static string GetFormattedVeryRarePets(){
            StringBuilder sb = new StringBuilder();
            sb.Append("Respond with a number from the following list: \n");
            for(int i = 0; i < veryRarePets.GetLength(0); i++){
                sb.Append((i + 1) + ". " + veryRarePets[i,0] + "\n");
            }

            return sb.ToString();
        }

        public static int NumOfCommon(){
            return commonPets.GetLength(0);
        }
        
        public static int NumOfUncommon(){
            return uncommonPets.GetLength(0);
        }
        public static int NumOfRare(){
            return rarePets.GetLength(0);
        }
        public static int NumOfVeryRare(){
            return veryRarePets.GetLength(0);
        }

        public string GetDefaultName(){
            return pets[this.rarity][this.petID,0];
        }

        public void AddBoost(int boostAmt){
            boostsRemaining += boostAmt;
        }

        public int GetBoosts(){
            return this.boostsRemaining;
        }
    }
}