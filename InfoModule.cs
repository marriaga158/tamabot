using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Tamabot;
using Discord.Addons.Interactive;
using Discord;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Linq;

// Keep in mind your module **must** be public and inherit ModuleBase.
// If it isn't, it will not be discovered by AddModulesAsync!
public class InfoModule : InteractiveBase<SocketCommandContext> 
{

    private static Dictionary<string, Gotchii> gotchiiList = new Dictionary<string, Gotchii>(); 
    //private static Dictionary<string, string> map = new Dictionary<string, string>();
    // THIS NEEDS TO BE STATIC OR IT WILL NOT HOLD VALUES ACROSS COMMANDS
    private static Dictionary<string, KeyValuePair<int, DateTimeOffset>> money = new Dictionary<string, KeyValuePair<int, DateTimeOffset>>();
    private static Dictionary<string, KeyValuePair<DateTimeOffset, bool>> cooldown = new Dictionary<string, KeyValuePair<DateTimeOffset, bool>>();

    private static readonly string abandonMsg = "You didn't care for your Tama, so it ran away.";
    private static readonly string dontHaveMsg = "You don't have a Tama! Buy one using !store";
    private static EmbedBuilder ABANDON_EMBED = new EmbedBuilder();
    private static EmbedBuilder DONTHAVE_EMBED = new EmbedBuilder();
    private static EmbedBuilder SUCCESS_EMBED = new EmbedBuilder();
    private static readonly EmbedBuilder NOT_ENOUGH_MONEY = new EmbedBuilder{
        Title = "Oh no!",
        Description = "Not enough money to complete the operation",
        Color = Color.Red
    };
    private static readonly string[] trainFailMsgs = {
        "Your tama turns around and walks away.",
        "Your tama doesn't seem to be listening to you.",
        "Your tama stares at you blankly.",
        "Your tama doesn't respond."
    };
    int commonCost = 125;
    int uncommonCost = 450;
    int rareCost = 1300;
    int veryRareCost = 4000;
    private static readonly int COOLDOWN_SECS = 5;

    // on help
    [Command("tama")]
    [Summary("gives a summary of the user's tama")]
    [Alias("pet", "gotchii", "t")]
    public async Task tama(SocketUser userPass = null){
        var user = userPass ?? Context.User;
        string id = user.Id.ToString();

        // check to see if the user has a tama
        //Console.WriteLine("tama command works");
        Gotchii result;
        if(gotchiiList.TryGetValue(id, out result)){
            if(!result.update()){
                // runs away
                // var eb = new EmbedBuilder();
                // eb.Title = "Oh no!";
                // eb.Description = abandonMsg;
                // eb.Color = Color.Red;
                await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                gotchiiList.Remove(id);
            } else {
                var eb = new EmbedBuilder();
                eb.Color = Color.Gold;
                eb.Title = user.Username + "'s Tama:";
                eb.Description = result.getMetrics();
                var fileLink = result.GetImage();
                eb.ImageUrl = fileLink;
                await Context.Channel.SendMessageAsync("", false, eb.Build());
            }
        } else {
            // var eb = new EmbedBuilder();
            // eb.Title = "Oh no!";
            // eb.Description = dontHaveMsg;
            // eb.Color = Color.Red;
            await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
        }

        writeToFile();
        //Console.WriteLine("this code was reached");
    }

    // on help
    [Command("gatcha", RunMode = RunMode.Async)]
    [Summary("Purchases a tama for the user")]
    [Alias("gatchapon", "random")]
    public async Task buy(){
        // idk why this doesn't work sometimes ugh
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        // warning that this will replace the user's current tama
        var eb = new EmbedBuilder();
        eb.Title = "WARNING";
        eb.Color = Color.Red;
        eb.Description = "Buying a new tama will overwrite your current one. Continue? `[Y/N]`";
        await Context.Channel.SendMessageAsync("", false, eb.Build());
        var response = await NextMessageAsync();
        if (response.ToString().ToLower() == "y"){ // problem isn't due to tolower
            // money check
            if(subtractMoney(id, 100)){
                // assign a new gotchii
                Console.WriteLine("reached 0");
                Gotchii result;
                if(gotchiiList.TryGetValue(id, out result)){
                    // the user already has one
                    Console.WriteLine("reached 2");
                    gotchiiList[id] = new Gotchii();
                } else {
                    Console.WriteLine("reached 3");
                    gotchiiList.Add(id, new Gotchii());
                    // for some reason it doesn't reach this?? idk
                    Console.WriteLine("reached 4");
                }
                
                Console.WriteLine("reached 1");
                eb.Color = Color.Green;
                eb.Title = "Success!";
                eb.Description = "Check out your Tama using !tama\n**-$100**";
                await Context.Channel.SendMessageAsync("", false, eb.Build());
            } else {
                await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
            }
        } else {
            eb.Title = "Cancelled";
            eb.Description = "You have not purchased a tama.";
            eb.Color = Color.Red;
            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        writeToFile();
    }

    // on help
    [Command("feed")]
    [Summary("Feeds the user's tama.")]
    public async Task feed(){
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        Gotchii result;
        if(gotchiiList.TryGetValue(id, out result)){
            // a gotchii exists
            if(result.feed()){
                if(subtractMoney(id, 5)){
                    var eb = new EmbedBuilder();
                    eb.Title = "Success!";
                    eb.Color = Color.Green;
                    eb.Description = "Your Tama has been fed!" + "\n" + "**-$5**";
                    await Context.Channel.SendMessageAsync("", false, eb.Build());
                } else {
                    // not enough money
                    await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
                }
            } else {
                // var eb = new EmbedBuilder();
                // eb.Title = "Oh no!";
                // eb.Description = abandonMsg;
                // eb.Color = Color.Red;
                // await Context.Channel.SendMessageAsync("", false, eb.Build());
                await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                gotchiiList.Remove(id);
            }
        } else {
            // var eb = new EmbedBuilder();
            // eb.Title = "Oh no!";
            // eb.Description = dontHaveMsg;
            // eb.Color = Color.Red;
            // await Context.Channel.SendMessageAsync("", false, eb.Build());
            await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
        }

        writeToFile();
    }

    // on help
    [Command("clean")]
    [Summary("Cleans the user's tama.")]
    public async Task clean(){
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        Gotchii result;
        if(gotchiiList.TryGetValue(id, out result)){
            // a gotchii exists
            if(result.clean()){
                if(subtractMoney(id, 2)){
                    var eb = new EmbedBuilder();
                    eb.Title = "Success!";
                    eb.Color = Color.Green;
                    eb.Description = "Your Tama's room has been cleaned!" + "**-$2**";
                    await Context.Channel.SendMessageAsync("", false, eb.Build());
                } else {
                    await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
                }
            } else {
                // var eb = new EmbedBuilder();
                // eb.Title = "Oh no!";
                // eb.Description = abandonMsg;
                // eb.Color = Color.Red;
                // await Context.Channel.SendMessageAsync("", false, eb.Build());
                await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                gotchiiList.Remove(id);
            }
        } else {
            // var eb = new EmbedBuilder();
            // eb.Title = "Oh no!";
            // eb.Description = dontHaveMsg;
            // eb.Color = Color.Red;
            // await Context.Channel.SendMessageAsync("", false, eb.Build());
            await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
        }

        writeToFile();
    }

    // on help
    [Command("play")]
    [Summary("Plays with the user's tama.")]
    public async Task play(){
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        Gotchii result;
        if(gotchiiList.TryGetValue(id, out result)){
            // a gotchii exists
            if(result.playWith()){
                var eb = new EmbedBuilder();
                eb.Title = "Success!";
                eb.Color = Color.Green;
                eb.Description = "You played with your Tama!";
                await Context.Channel.SendMessageAsync("", false, eb.Build());
            } else {
                // var eb = new EmbedBuilder();
                // eb.Title = "Oh no!";
                // eb.Description = abandonMsg;
                // eb.Color = Color.Red;
                await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                gotchiiList.Remove(id);
            }
        } else {
            // var eb = new EmbedBuilder();
            // eb.Title = "Oh no!";
            // eb.Description = dontHaveMsg;
            // eb.Color = Color.Red;
            await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
        }

        writeToFile();
    }

    // on help
    [Command("setName")]
    [Summary("Sets the name of the user's Tama.")]
    [Alias("nameset", "setname")]
    public async Task setName(string input = null){
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        //var inputName = input ?? Gotchii.pets

        Gotchii result;
        if(gotchiiList.TryGetValue(id, out result)){
            // a gotchii exists
            if(result.update()){
                var eb = new EmbedBuilder();
                eb.Title = "Success!";
                eb.Color = Color.Green;
                eb.Description = "Your Tama's name has been changed.";
                await Context.Channel.SendMessageAsync("", false, eb.Build());
                string inputName = input ?? gotchiiList[id].GetDefaultName();
                gotchiiList[id].changeName(inputName);
            } else {
                // var eb = new EmbedBuilder();
                // eb.Title = "Oh no!";
                // eb.Description = abandonMsg;
                // eb.Color = Color.Red;
                await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                gotchiiList.Remove(id);
            }
        } else {
            // var eb = new EmbedBuilder();
            // eb.Title = "Oh no!";
            // eb.Description = dontHaveMsg;
            // eb.Color = Color.Red;
            await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
        }

        writeToFile();
    }

    [Command("help")]
    [Summary("Sends the user a PM with the commands.")]
    [Alias("commands")]
    public async Task help(){
        // https://stackoverflow.com/questions/52849959/what-is-summary-used-for-in-discord-net implement this one day
        SocketUser user = Context.User;

        await Discord.UserExtensions.SendMessageAsync(user, "```Tamabot Command List: \n \n" +
        "!boosts - Allows you to buy training boosts.\n" +
        "!clean - Cleans your Tama's room. Costs $10 for cleaning supplies.\n" +
        "!daily - Gives you your daily credits. Can be used once every 24 hours.\n" +
        "!gatcha - Gives you a random Tama of random rarity. Overrides your existing Tama if you have one. Costs $100.\n" +
        "!heist - Take part in a daring bank heist. Big risk = big reward\n" +
        "!feed - Feeds your Tama. Costs $25 for food.\n" +
        "!leaderboard - Returns a leaderboard of the highest level Tamas.\n" +
        "!money - Shows your current balance.\n" +
        "!play - Plays with your Tama.\n" +
        "!roulette - Plays roulette. Use !roulette help for more details.\n" +
        "!sell - Sells your Tama. Higher-level Tamas sell for more money.\n" +
        "!setname - Sets the name of your Tama.\n" +
        "!sitter - Hires a sitter for the given number of days.\n" +
        "!store - Lets you buy a new Tama from the store selection.\n" +
        "!tama - Gives you a summary of your Tama.\n" + 
        "!train - Trains your Tama. Gives exp if successful.```");
    }

    // on help
    [Command("daily")]
    [Summary("Gets daily money")]
    public async Task daily(){
        // this is eventually going to do it time-based
        SocketUser user = Context.User;
        string id = user.Id.ToString();
        string dailyMsg = "**You received your 200 daily credits!**";

        KeyValuePair<int, DateTimeOffset> result;
        if(money.TryGetValue(id, out result)){
            if(result.Value.ToUnixTimeSeconds() - DateTimeOffset.UtcNow.ToUnixTimeSeconds() < 0){
                var resultKey = result.Key;
                //var resultValue = result.Value;
                resultKey += 200;
                money[id] = new KeyValuePair<int, DateTimeOffset>(resultKey, DateTimeOffset.UtcNow.AddHours(23));
                await ReplyAsync(dailyMsg);
                writeToFile();
            } else {
                await ReplyAsync("**Your daily resets in " + (result.Value - DateTimeOffset.UtcNow).Hours + " hours, " + (result.Value - DateTimeOffset.UtcNow).Minutes + " minutes, and " + (result.Value - DateTimeOffset.UtcNow).Seconds + " seconds.**");
            }
            // var resultKey = result.Key;
            // var resultValue = result.Value;
            // resultKey += 200;
            // money[id] = new KeyValuePair<int, long>(resultKey, resultValue);
        } else {
            money.Add(id, new KeyValuePair<int, DateTimeOffset>(200, DateTimeOffset.UtcNow.AddHours(24))); // second value is going to be the time eventually
            await ReplyAsync(dailyMsg);
            writeToFile();
        }
    }

    // on help
    [Command("money")]
    [Summary("Shows the amount of money you have")]
    [Alias("balance")]
    public async Task moneyGet(){
        SocketUser user = Context.User;
        string id = user.Id.ToString();
        EmbedBuilder eb = new EmbedBuilder();
        eb.Title = "Current Balance";

        KeyValuePair<int, DateTimeOffset> result;
        if(money.TryGetValue(id, out result)){
            eb.Description = result.Key.ToString();
        } else {
            eb.Description = "0";
        }
        
        await Context.Channel.SendMessageAsync("", false, eb.Build());
    }

    // on help
    [Command("train")]
    [Summary("Trains your Tama.")]
    public async Task train(){
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        Gotchii result;
        if(gotchiiList.TryGetValue(id, out result)){
            if(!result.update()){
                // ran away
                await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                gotchiiList.Remove(id);
                return;
            }
        } else {
            // no gotchii
            await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
            return;
        }

        var trainPair = gotchiiList[id].Train();

        Random rng = new Random();

        KeyValuePair<bool, bool> cooldownResult = checkCooldown(id);
        if(!cooldownResult.Key){
            if(!cooldownResult.Value){
                // send the warning message
                await ReplyAndDeleteAsync("**" + Context.Guild.GetUser(Convert.ToUInt64(id)).ToString() + "**, please wait "+COOLDOWN_SECS+" seconds between attempts!");
                return;
            } else {
                Console.WriteLine("train command on cooldown, message already sent");
                return;
                // break out of the method
            }
        }

        EmbedBuilder eb;
        if(trainPair.Key){
            eb = new EmbedBuilder{
                Title = "Success!",
                Description = "Your Tama paid attention!\n" + "**+" + trainPair.Value + " exp**",
                Color = Color.Green
            };
        } else {
            eb = new EmbedBuilder{
                Title = "Try again!",
                Description = trainFailMsgs[rng.Next(trainFailMsgs.Length)], // chooses a random fail msg from the array
                Color = Color.Red
            };
        }

        if(result.GetBoosts() > 0){
            eb.Description = eb.Description + "\nBoosts remaining: " + result.GetBoosts();
        }

        await Context.Channel.SendMessageAsync("", false, eb.Build());

        writeToFile();
    }

    // on help
    [Command("leaderboard")]
    [Summary("Gives a leaderboard of exp/levels")]
    [Alias("top")]
    public async Task leaderboard(){
        Console.WriteLine("leaderboard command called");

        List<KeyValuePair<string,Gotchii>> sortedList = new List<KeyValuePair<string,Gotchii>>();

        foreach(KeyValuePair<string,Gotchii> entry in gotchiiList){
            sortedList.Add(entry);
        }

        //Console.WriteLine("presort list: " + sortedList.ToString());

        sortedList.Sort(delegate(KeyValuePair<string,Gotchii> x, KeyValuePair<string,Gotchii> y){
          if(x.Value.GetExpLevel().Key > y.Value.GetExpLevel().Key)  {
              return -1;
          } else if(x.Value.GetExpLevel().Key < y.Value.GetExpLevel().Key){
              return 1;
          } else {
              return 0;
          }
        });

        //Console.WriteLine("ayo the sorting worked");

        StringBuilder sb = new StringBuilder();
        for(int i=1; i<=sortedList.Count; i++){
            sb.Append(i + ". " + Context.Guild.GetUser(Convert.ToUInt64(sortedList[i-1].Key)) + ", Level: " + sortedList[i-1].Value.GetExpLevel().Value);
            sb.Append("\n");
        }

        EmbedBuilder eb = new EmbedBuilder();
        eb.Title = "Gotchii Leaderboard";
        eb.Description = sb.ToString();
        eb.Color = Color.Gold;
        await Context.Channel.SendMessageAsync("", false, eb.Build());
    }

    // on help
    [Command("sitter", RunMode = RunMode.Async)]
    [Summary("buys a sitter")]
    public async Task sitterGet(int input = -999999){
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        // argument
        if(input != -999999){
            if(input > 0){
                // input screening
                if(subtractMoney(id, 20 * input)){
                    gotchiiList[id].SitterForDays(input);
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.Title = "Success!";
                    eb.Color = Color.Green;
                    eb.Description = "You have hired a sitter for " + input + " days.";
                    await Context.Channel.SendMessageAsync("", false, eb.Build());

                    writeToFile();
                } else {
                    await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
                }
            } else {
                await ReplyAsync("Please enter an integer > 0.");
            }

            return;
        }

        // check if they have a tama
        Gotchii Gotchresult;
        if(gotchiiList.TryGetValue(id, out Gotchresult)){
            // if they have one check if it's alive
            if(Gotchresult.update()){
                // then do everything

                // this is all the logic for the sitter hiring
                await ReplyAndDeleteAsync("Enter the amount of days you want to hire the sitter for. ($20 per day)");
                var response = await NextMessageAsync();
                int i = 0;
                bool result = int.TryParse(response.Content, out i); 
                if(result){ // if the user replies with a number
                    if(i > 0){
                        // input screening
                        if(subtractMoney(id, 20 * i)){
                            gotchiiList[id].SitterForDays(i);
                            EmbedBuilder eb = new EmbedBuilder();
                            eb.Title = "Success!";
                            eb.Color = Color.Green;
                            eb.Description = "You have hired a sitter for " + i + " days.";
                            await Context.Channel.SendMessageAsync("", false, eb.Build());
                        } else {
                            await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
                        }
                    } else {
                        await ReplyAsync("Please respond with an integer number > 0.");
                    }
                } else {
                    await ReplyAsync("Please respond with an integer number > 0.");
                }
            } else {
                // tama ran away
                await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                gotchiiList.Remove(id);
            }
        } else {
            await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
        }

        writeToFile();
    }

    // on help
    [Command("store", RunMode = RunMode.Async)]
    [Summary("buys a tama but better")]
    [Alias("shop", "buy")]
    public async Task store(){
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        var eb = new EmbedBuilder();
        eb.Title = "WARNING";
        eb.Color = Color.Red;
        eb.Description = "Buying a new tama will overwrite your current one. Continue? `[Y/N]`";
        await Context.Channel.SendMessageAsync("", false, eb.Build());
        var response = await NextMessageAsync();
        if(response.Content.ToLower() == "y"){
            await ReplyAsync("**Choose from: [C]ommon ($" + commonCost + "), [U]ncommon ($"+ uncommonCost + "), [R]are ($"+ rareCost+"), [V]eryRare ($"+ veryRareCost+")**" + "\nCurrent money: " + money[id].Key);
            response = await NextMessageAsync();
            var responseStr = response.Content.ToUpper();

            string nonvalidnum = "Please respond with a valid number.";
            if(responseStr == "C"){
                // common
                await ReplyAsync("```" + Gotchii.GetFormattedCommonPets() + "```");
                response = await NextMessageAsync();

                // ok here begins the fucking garbage
                if(Int32.TryParse(response.Content, out int i)){
                    // input is good, now check for money
                    if(i > 0 && i <= Gotchii.NumOfCommon()){
                        if(subtractMoney(id, commonCost)){
                            // good input and enough money, assign pet
                            eb.Color = Color.Green;
                            eb.Title = "Success!";
                            eb.Description = "Check out your Tama using !tama\n**-$" + commonCost + "**";
                            await Context.Channel.SendMessageAsync("", false, eb.Build());
                            gotchiiAssign(id, Gotchii.Rarity.Common, i-1); // helper command to set gotchiis
                        } else {
                            // not enough money
                            await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
                        }
                    } else {
                        // input is a number, but not in a valid range
                        await ReplyAsync(nonvalidnum);
                    }
                } else {
                    // input is not a number
                    await ReplyAsync(nonvalidnum);
                }
            } else if (responseStr == "U"){
                // uncommon 
                await ReplyAsync("```" + Gotchii.GetFormattedUncommonPets() + "```");
                response = await NextMessageAsync();

                if(Int32.TryParse(response.Content, out int i)){
                    // input is good, now check for money
                    if(i > 0 && i <= Gotchii.NumOfUncommon()){
                        if(subtractMoney(id, uncommonCost)){
                            // good input and enough money, assign pet
                            eb.Color = Color.Green;
                            eb.Title = "Success!";
                            eb.Description = "Check out your Tama using !tama\n**-$" + uncommonCost + "**";
                            await Context.Channel.SendMessageAsync("", false, eb.Build());
                            gotchiiAssign(id, Gotchii.Rarity.Uncommon, i-1); // helper command to set gotchiis
                        } else {
                            // not enough money
                            await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
                        }
                    } else {
                        // input is a number, but not in a valid range
                        await ReplyAsync(nonvalidnum);
                    }
                } else {
                    // input is not a number
                    await ReplyAsync(nonvalidnum);
                }
            } else if (responseStr == "R"){
                // rare
                await ReplyAsync("```" + Gotchii.GetFormattedRarePets() + "```");
                response = await NextMessageAsync();

                if(Int32.TryParse(response.Content, out int i)){
                    // input is good, now check for money
                    if(i > 0 && i <= Gotchii.NumOfRare()){
                        if(subtractMoney(id, rareCost)){
                            // good input and enough money, assign pet
                            eb.Color = Color.Green;
                            eb.Title = "Success!";
                            eb.Description = "Check out your Tama using !tama\n**-$" + rareCost + "**";
                            await Context.Channel.SendMessageAsync("", false, eb.Build());
                            gotchiiAssign(id, Gotchii.Rarity.Rare, i-1); // helper command to set gotchiis
                        } else {
                            // not enough money
                            await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
                        }
                    } else {
                        // input is a number, but not in a valid range
                        await ReplyAsync(nonvalidnum);
                    }
                } else {
                    // input is not a number
                    await ReplyAsync(nonvalidnum);
                }
            } else if (responseStr == "V"){
                // very rare
                await ReplyAsync("```" + Gotchii.GetFormattedVeryRarePets() + "```");
                response = await NextMessageAsync();

                if(Int32.TryParse(response.Content, out int i)){
                    // input is good, now check for money
                    if(i > 0 && i <= Gotchii.NumOfVeryRare()){
                        if(subtractMoney(id, veryRareCost)){
                            // good input and enough money, assign pet
                            eb.Color = Color.Green;
                            eb.Title = "Success!";
                            eb.Description = "Check out your Tama using !tama\n**-$" + veryRareCost + "**";
                            await Context.Channel.SendMessageAsync("", false, eb.Build());
                            gotchiiAssign(id, Gotchii.Rarity.VeryRare, i-1); // helper command to set gotchiis
                        } else {
                            // not enough money
                            await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
                        }
                    } else {
                        // input is a number, but not in a valid range
                        await ReplyAsync(nonvalidnum);
                    }
                } else {
                    // input is not a number
                    await ReplyAsync(nonvalidnum);
                }
            } else {
                // invalid or timeout
                await ReplyAsync("Either an invalid input or you did not respond before the timeout. \nRun the !store command again.");
            }
        } else if (response.Content.ToLower() == "n") {
            eb.Title = "Cancelled";
            eb.Description = "You have not purchased a tama.";
            eb.Color = Color.Red;
            await Context.Channel.SendMessageAsync("", false, eb.Build());
        } else {
            await ReplyAsync("You did not reply before the timeout.");
        }
    }

    // on help
    [Command("sell", RunMode = RunMode.Async)]
    [Summary("sells your tama for money")]
    public async Task sell(){
        int moneyPerLevelBase = 10;
        int commonMod = 1;
        double uncommonMod = 1.5;
        int rareMod = 2;
        double veryRareMod = 2.5;

        SocketUser user = Context.User;
        string id = user.Id.ToString();

        // check if they have a tama
        Gotchii Gotchresult;
        if(gotchiiList.TryGetValue(id, out Gotchresult)){
            // if they have one check if it's alive
            if(Gotchresult.update()){
                // then do everything
                int sellAmount = 0;

                if(gotchiiList[id].rarity == Gotchii.Rarity.Common){
                    sellAmount = moneyPerLevelBase * commonMod * gotchiiList[id].GetExpLevel().Value;
                    sellAmount += (int)(commonCost * 0.5);
                } else if (gotchiiList[id].rarity == Gotchii.Rarity.Uncommon){
                    sellAmount = (int)(moneyPerLevelBase * uncommonMod * gotchiiList[id].GetExpLevel().Value);
                    sellAmount += (int)(uncommonCost * 0.5);
                } else if (gotchiiList[id].rarity == Gotchii.Rarity.Rare){
                    sellAmount = moneyPerLevelBase * rareMod * gotchiiList[id].GetExpLevel().Value;
                    sellAmount += (int)(rareCost * 0.5);
                } else if (gotchiiList[id].rarity == Gotchii.Rarity.Rare){
                    sellAmount = (int)(moneyPerLevelBase * veryRareMod * gotchiiList[id].GetExpLevel().Value);
                    sellAmount += (int)(veryRareCost * 0.5);
                }

                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "WARNING";
                eb.Color = Color.Red;
                eb.Description = "THIS WILL SELL YOUR TAMA FOR $" + sellAmount + ". DO YOU WANT TO PROCEED? [Y/N]";
                await Context.Channel.SendMessageAsync("", false, eb.Build());

                SocketMessage response = await NextMessageAsync();
                Console.WriteLine("response: " + response.Content);
                if(response.Content.ToLower() == "y"){
                    KeyValuePair<int, DateTimeOffset> result;
                    Console.WriteLine("2");
                    money.TryGetValue(id, out result);
                    Console.WriteLine("4");
                    // this assumes that the person already has a money entry because they had a tama in the first place
                    money[id] = new KeyValuePair<int, DateTimeOffset>(result.Key + sellAmount, result.Value);
                    // now I remove the gotchii
                    gotchiiList.Remove(id);
                    await ReplyAsync("Your Tama has been successfuly sold!\n" + "+$" + sellAmount);
                } else if (response.Content.ToLower() == "n"){
                    Console.WriteLine("3");
                    eb.Title = "Cancelled";
                    eb.Description = "You have not sold your tama.";
                    eb.Color = Color.Red;
                    await Context.Channel.SendMessageAsync("", false, eb.Build());
                } else {
                    await ReplyAsync("Either an invalid response or the command timed out. Try again.");
                }
            } else {
                // tama ran away
                await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                gotchiiList.Remove(id);
            }
        } else {
            await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
        }

        writeToFile();
    }

    // not on help for understandable reasons
    [Command("givemoney")]
    [Summary("admin command to give a user money")]
    public async Task givemoney( int moneyToAdd, SocketUser userPass = null){
        var passeduser = userPass ?? Context.User;
        SocketUser user = Context.User;
        string id = user.Id.ToString();
        string passedId = passeduser.Id.ToString();

        if(id == "156563872726253571"){
            addMoney(passedId, moneyToAdd);
            await ReplyAndDeleteAsync(moneyToAdd + " added successfully.");
        } else {
            await ReplyAndDeleteAsync("You are not authorized to use this command.");
        }

        // writeToFile();
        // addMoney already writes to file, redundant
    }

    // on help
    [Command("heist", RunMode = RunMode.Async)]
    [Summary("bank heist")]
    public async Task bankheist(){
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        EmbedBuilder eb = new EmbedBuilder();
        eb.Title = "WARNING";
        eb.Color = Color.Red;
        eb.Description = "**YOU ARE ABOUT TO TAKE PART IN CRIMINAL ACTIVITY.**" + "\n" +
        "You have a slim chance of success. If you succeed, the money in the bank is yours.\n" +
        "The money currently in the bank is: $" + money["bank"].Key + "\n" +
        "If you fail, the police will confiscate your Tama and all of your money.\n" +
        "It costs $200 to buy the equipment necessary, and you need a Tama to be your accomplice.\n" +
        "**CONTINUE?** `[Y/N]`";
        await Context.Channel.SendMessageAsync("", false, eb.Build());

        var response = await NextMessageAsync();
        if(response.Content.ToLower() == "y"){
            // they want to do the heist, check money and tama
            Gotchii result;
            if(gotchiiList.TryGetValue(id, out result)){
                if(result.update()){
                    // they have the tama
                    if(subtractMoney(id, 200)){
                        // run the code to check for success
                        Random rng = new Random();
                        int successval = rng.Next(0, 101);
                        Console.WriteLine("debug success val: " + successval);
                        if(successval < 6){
                            // heist succeeded
                            addMoney(id, money["bank"].Key);
                            eb.Title = "Success!";
                            eb.Color = Color.Green;
                            eb.Description = "You successfully robbed the bank and evaded the police. The money is yours!";
                            await Context.Channel.SendMessageAsync("", false, eb.Build());
                            // subtractMoney("bank", money["bank"].Key);
                            // addMoney("bank", 1000);
                            money["bank"] = new KeyValuePair<int, DateTimeOffset>(1000, money["bank"].Value);
                            // necessary so that it doesn't add the money immediately back
                        } else {
                            // heist failed
                            eb.Title = "Arrested!";
                            eb.Color = Color.Red;
                            eb.Description = "You were caught by the police on your way out of the bank.\n" +
                            "-$" + money[id].Key;
                            await Context.Channel.SendMessageAsync("", false, eb.Build());
                            subtractMoney(id, money[id].Key); // subtract all of the money
                            gotchiiList.Remove(id);
                            writeToFile();
                        }
                    } else {
                        eb.Title = "Oh no!";
                        eb.Description = "Not enough money!";
                        await Context.Channel.SendMessageAsync("", false, eb.Build());
                    }
                } else {
                    await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                    gotchiiList.Remove(id);
                }
            } else {
                await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
            }
        } else if (response.Content.ToLower() == "n"){
            // no heist
            await ReplyAsync("You backed out of the heist.");
        } else {
            // timeout
            await ReplyAsync("Either an invalid response or the command timed out. Try again.");
        }
    }

    // on help
    [Command("roulette")]
    [Summary("what do you think this does dumbass")]
    public async Task roulette(string betType, string bet = null, int betAmount = 0){
        // https://cdn.discordapp.com/attachments/709259316858978365/715037447758807130/roulette_typeofbets.png
        // this is what a roulette table works like
        SocketUser user = Context.User;
        string id = user.Id.ToString();
        string YOU_FUCKED_UP = "Invalid command syntax. Use `!roulette help` for more info.";
        int[] blackNumbers = {2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35};
        int[] redNumbers = {1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36};
        int[] street1 = {1, 2, 3};
        int[] street2 = {4, 5, 6};
        int[] street3 = {7, 8, 9};
        int[] street4 = {10, 11, 12};
        int[] street5 = {13, 14, 15};
        int[] street6 = {16, 17, 18};
        int[] street7 = {19, 20, 21};
        int[] street8 = {22, 23, 24};
        int[] street9 = {25, 26, 27};
        int[] street10 = {28, 29, 30};
        int[] street11 = {31, 32, 33};
        int[] street12 = {34, 35, 36};

        Random rng = new Random();
        int pulledNumber = rng.Next(0, 37);

        EmbedBuilder payout = new EmbedBuilder();
        payout.Title = "Winner!";
        payout.Color = Color.Green;

        EmbedBuilder loser = new EmbedBuilder();
        loser.Title = "Try again!";
        loser.Color = Color.Red;

        int odds;

        KeyValuePair<bool, bool> cooldownResult = checkCooldown(id);
        if(!cooldownResult.Key){
            if(!cooldownResult.Value){
                // send the warning message
                await ReplyAndDeleteAsync("**" + Context.Guild.GetUser(Convert.ToUInt64(id)).ToString() + "**, please wait "+ COOLDOWN_SECS+ " seconds between attempts!");
                return;
            } else {
                Console.WriteLine("roulette command on cooldown, message already sent");
                return;
                // break out of the method
            }
        }

        //Console.WriteLine("betType: " + betType + " bet: " + bet + " betAmount: " + betAmount + " pulledNumber: " + pulledNumber);
        if(betType == "help"){
            // send a help message to their pm
            await Discord.UserExtensions.SendMessageAsync(user, "```Roulette help: \n \n" + 
            "The !roulette command takes 3 arguments: the bet type, the bet choice, and the amount being bet.\n" +
            "You have 4 different bet types to choose from: red/black, dozens, streets, and straights/singles.\n" +
            "Red/Black is simple, half of the numbers are red, and half are black. Pays 1:1.\n" +
            "Example use: !roulette r/b red 500\n\n" +
            "For dozens, there are 3 groups of a dozen numbers each to choose from. Pays 2:1.\n" +
            "Example use: !roulette dozen 2 500\n\n" +
            "For streets, there are 12 streets of 3 numbers each to choose from. Pays 11:1.\n" +
            "Example use: !roulette street 5 500\n\n" +
            "Straights are the easiest, just bet on a single number. Pays 35:1.\n" +
            "Example use: !roulette straight 25 500\n\n" +
            "```");
        } else {
            if(betAmount > 0){
                if(subtractMoney(id, betAmount)){
                    if(betType == "r/b"){
                        // red/black
                        Console.WriteLine("r/b reached");
                        odds = 1;
                        if(bet == "red" || bet == "r"){
                            // they bet red
                            if(redNumbers.Contains(pulledNumber)){
                                // payout
                                payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                addMoney(id, betAmount * odds);
                                await Context.Channel.SendMessageAsync("", false, payout.Build());
                            } else {
                                loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                subtractMoney(id, betAmount);
                                await Context.Channel.SendMessageAsync("", false, loser.Build());
                            }
                        } else if(bet == "black" || bet == "b"){
                            // they bet black
                            //Console.WriteLine("2");
                            if(blackNumbers.Contains(pulledNumber)){
                                // payout
                                payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                addMoney(id, betAmount * odds);
                                await Context.Channel.SendMessageAsync("", false, payout.Build());
                            } else {
                                loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                subtractMoney(id, betAmount);
                                await Context.Channel.SendMessageAsync("", false, loser.Build());
                            }
                        } else {
                            await ReplyAsync(YOU_FUCKED_UP);
                        }
                    } else if(betType == "dozen"){
                        // dozen bet
                        Console.WriteLine("dozen reached");
                        odds = 2;
                        if(Int32.TryParse(bet, out int betInt)){
                            // the betInt is correct
                            if(betInt == 1){
                                if(pulledNumber > 0 && pulledNumber < 13){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 2){
                                if(pulledNumber > 12 && pulledNumber < 25){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 3){
                                if(pulledNumber > 24 && pulledNumber < 37){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else {
                                await ReplyAsync(YOU_FUCKED_UP);
                            }
                        } else {
                            await ReplyAsync(YOU_FUCKED_UP);
                        }
                    } else if(betType == "street"){
                        // street bet
                        Console.WriteLine("street reached");
                        odds = 11;
                        if(Int32.TryParse(bet, out int betInt)){
                            if(betInt == 1){
                                if(street1.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 2){
                                if(street2.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 3){
                                if(street3.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 4){
                                if(street4.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 5){
                                if(street5.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 6){
                                if(street6.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 7){
                                if(street7.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 8){
                                if(street8.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 9){
                                if(street9.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 10){
                                if(street10.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 11){
                                if(street11.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else if(betInt == 12){
                                if(street12.Contains(pulledNumber)){
                                    payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                    addMoney(id, betAmount * odds);
                                    await Context.Channel.SendMessageAsync("", false, payout.Build());
                                } else {
                                    loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                    subtractMoney(id, betAmount);
                                    await Context.Channel.SendMessageAsync("", false, loser.Build());
                                }
                            } else {
                                await ReplyAsync(YOU_FUCKED_UP);
                            }
                        } else {
                            await ReplyAsync(YOU_FUCKED_UP);
                        }
                    } else if(betType == "straight" || betType == "single"){
                        // single number bet
                        Console.WriteLine("straight reached");
                        odds = 35;
                        if(Int32.TryParse(bet, out int betInt)){
                            if(betInt>36){
                                await ReplyAsync(YOU_FUCKED_UP);
                            } else if(betInt == pulledNumber){
                                payout.Description = "The number pulled was " + pulledNumber + ", you win " + betAmount * odds + "!";
                                addMoney(id, betAmount * odds);
                                await Context.Channel.SendMessageAsync("", false, payout.Build());
                            } else {
                                loser.Description = "The number pulled was " + pulledNumber +", try again!";
                                subtractMoney(id, betAmount);
                                await Context.Channel.SendMessageAsync("", false, loser.Build());
                            }
                        } else {
                            await ReplyAsync(YOU_FUCKED_UP);
                        }
                    } else {
                        // invalid input
                        await ReplyAsync(YOU_FUCKED_UP);
                    }
                } else {
                    await Context.Channel.SendMessageAsync("", false, NOT_ENOUGH_MONEY.Build());
                }
            } else if (betAmount == 0){
                await ReplyAndDeleteAsync(YOU_FUCKED_UP);
            } else{
                await ReplyAndDeleteAsync("You can't gamble negative money!");
            }
        } 
        // this code is a crime against object oriented programming
    }

    [Command("boosts", RunMode = RunMode.Async)]
    [Summary("Buys training and other boosts when i do RPG stuff")]
    [Alias("boost")]
    public async Task boosts(){
        SocketUser user = Context.User;
        string id = user.Id.ToString();

        string boosts = "```" +
        "Choose the amount of training boosts you want to buy.\n\n" +
        "[1] 10 boosts - $150\n" + 
        "[2] 25 boosts - $300\n" +
        "[3] 50 boosts - $550\n" +
        "[4] 100 boosts - $1000\n" +
        "```";
        await ReplyAsync(boosts);
        var response = await NextMessageAsync();
        Gotchii result;
        if(response.Content == "1"){
            if(gotchiiList.TryGetValue(id, out result)){
                if(result.update()){
                    if(subtractMoney(id, 150)){
                        result.AddBoost(10);
                        await ReplyAndDeleteAsync("**You have purchased 10 boosts.**");
                    }
                } else {
                    await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                    gotchiiList.Remove(id);
                }
            } else {
                await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
            }
        } else if (response.Content == "2"){
            if(gotchiiList.TryGetValue(id, out result)){
                if(result.update()){
                    if(subtractMoney(id, 300)){
                        result.AddBoost(25);
                        await ReplyAndDeleteAsync("**You have purchased 25 boosts.**");
                    }
                } else {
                    await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                    gotchiiList.Remove(id);
                }
            } else {
                await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
            }
            
        } else if(response.Content == "3"){
            if(gotchiiList.TryGetValue(id, out result)){
                if(result.update()){
                    if(subtractMoney(id, 550)){
                        result.AddBoost(50);
                        await ReplyAndDeleteAsync("**You have purchased 50 boosts.**");
                    }
                } else {
                    await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                    gotchiiList.Remove(id);
                }
            } else {
                await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
            }

        } else if (response.Content == "4"){
            if(gotchiiList.TryGetValue(id, out result)){
                if(result.update()){
                    if(subtractMoney(id, 1000)){
                        result.AddBoost(100);
                        await ReplyAndDeleteAsync("**You have purchased 100 boosts.**");
                    }
                } else {
                    await Context.Channel.SendMessageAsync("", false, ABANDON_EMBED.Build());
                    gotchiiList.Remove(id);
                }
            } else {
                await Context.Channel.SendMessageAsync("", false, DONTHAVE_EMBED.Build());
            }

        } else {

        }
    }

    

    public static void gotchiiAssign(string id, Gotchii.Rarity rarity, int petID){
        Gotchii result;
        if(gotchiiList.TryGetValue(id, out result)){
            // the user already has one
            gotchiiList[id] = new Gotchii(rarity, petID);
        } else {
            gotchiiList.Add(id, new Gotchii(rarity, petID));
        } 

        writeToFile();
    }

    public static void readDictionary(){
        // this reads the dictionary in from a file

        gotchiiList = JsonConvert.DeserializeObject<Dictionary<string, Gotchii>>(File.ReadAllText("gotchii.json"));
        money = JsonConvert.DeserializeObject<Dictionary<string, KeyValuePair<int, DateTimeOffset>>>(File.ReadAllText("money.json"));
    }

    private static void writeToFile(){
        // i'm gonna have to run this at the end of every command ugh
        // File.WriteAllText("gotchii.txt", JsonConvert.SerializeObject(gotchiiList));
        File.WriteAllText("gotchii.json", JsonConvert.SerializeObject(gotchiiList));
        File.WriteAllText("money.json", JsonConvert.SerializeObject(money));
    }

    public static void constructEmbed(){
        ABANDON_EMBED.Title = "Oh no!";
        ABANDON_EMBED.Description = abandonMsg;
        ABANDON_EMBED.Color = Color.Red;
        // also red x

        DONTHAVE_EMBED.Title = "Oh no!";
        DONTHAVE_EMBED.Description = dontHaveMsg;
        DONTHAVE_EMBED.Color = Color.Red;
        // also red x

        SUCCESS_EMBED.Title = "Success!";
        SUCCESS_EMBED.Color = Color.Green;
    }

    private static bool subtractMoney(string userId, int amount){
        KeyValuePair<int, DateTimeOffset> result;
        if(money.TryGetValue(userId, out result)){
            if(result.Key - amount >= 0){ 
                money[userId] = new KeyValuePair<int, DateTimeOffset>(money[userId].Key - amount, money[userId].Value);
                money["bank"] = new KeyValuePair<int, DateTimeOffset>(money["bank"].Key + amount, money[userId].Value);
                Program.updateStatus("$" + money["bank"].Key + " in the bank");
                writeToFile();
                return true;
            } else {
                return false;
            }
        }
        return false;
    }

    private static void addMoney(string id, int amount){
        KeyValuePair<int, DateTimeOffset> result;
        if(money.TryGetValue(id, out result)){
            money[id] = new KeyValuePair<int, DateTimeOffset>(result.Key + amount, result.Value);
        } else {
            money.Add(id, new KeyValuePair<int, DateTimeOffset>(amount, DateTimeOffset.UtcNow.AddHours(-99999)));
        }

        writeToFile();
    }

    /// the first bool is if the cooldown has expired, the second is if a warning has been sent
    public static KeyValuePair<bool, bool> checkCooldown(string id){
        

        KeyValuePair<DateTimeOffset, bool> result;
        if(cooldown.TryGetValue(id, out result)){
            long timeDiff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - result.Key.ToUnixTimeSeconds();
            if(timeDiff < COOLDOWN_SECS){
                // cooldown hasn't ended
                if(!result.Value){
                    // if a warning message hasn't been sent, send one and set list value to true;
                    cooldown[id] = new KeyValuePair<DateTimeOffset, bool>(result.Key, true);
                    return new KeyValuePair<bool, bool>(false, false);
                } else {
                    // don't send another warning message, don't need to update the dictionary
                    return new KeyValuePair<bool, bool>(false, true);
                }
            } else {
                // cooldown has ended
                // update the key in the list with the new current time
                cooldown[id] = new KeyValuePair<DateTimeOffset, bool>(DateTimeOffset.UtcNow, false);
                return new KeyValuePair<bool, bool>(true, false);
            }
        } else {
            // they haven't sent a message yet so no cooldown
            cooldown.Add(id, new KeyValuePair<DateTimeOffset, bool>(DateTimeOffset.UtcNow, false));
            return new KeyValuePair<bool, bool>(true, false);
        }
    }

    // private Gotchii getFromMasterList(SocketUser user){
    //     for(int i=0; i<masterPetList.Capacity; i++){
    //             if(masterPetList[i].getUser() == user){
    //                 return masterPetList[i];
    //                 // there is definitely a better way to do this once this list gets large
    //             }
    //         }

    //     return null;
    // }


// https://stackoverflow.com/questions/36333567/saving-a-dictionaryint-object-in-c-sharp-serialization
    

    
}