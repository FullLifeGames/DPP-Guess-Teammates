using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPP_Guess_Teammates
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader sr = new StreamReader(@"../../../dppTeams.txt");
            
            List<Team> teams = new List<Team>();

            Team currentTeam = null;

            Dictionary<string, Pokemon> monDict = new Dictionary<string, Pokemon>();

            string lastLine = "";

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                if (line.Contains("==="))
                {
                    currentTeam = new Team();
                    teams.Add(currentTeam);
                }                
                else if(currentTeam != null)
                {
                    if(line != "" && lastLine == "")
                    {
                        string mon = line.Trim();
                        if (mon.Contains("@"))
                        {
                            mon = mon.Substring(0, mon.IndexOf("@")).Trim();
                        }
                        mon = mon.Replace("(M)", "").Replace("(F)", "").Trim();
                        if (mon.Contains("("))
                        {
                            mon = mon.Substring(mon.LastIndexOf("(") + 1);
                            mon = mon.Substring(0, mon.IndexOf(")"));
                        }
                        mon = mon.Trim().ToLower();
                        if (!monDict.ContainsKey(mon))
                        {
                            monDict.Add(mon, new Pokemon(mon, 0));
                        }
                        monDict[mon].occurences++;
                        currentTeam.Pokemon.Add(monDict[mon]);
                    }
                }

                lastLine = line;
            }

            sr.Close();

            sr = new StreamReader(@"../../../dppReplays.txt");

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                if (line.Contains("(Lead)"))
                {
                    currentTeam = new Team();
                    teams.Add(currentTeam);
                    string tempLine = line.Replace("(Lead)", "").Replace(":", "").Trim();
                    foreach(string tempMon in tempLine.Split(','))
                    {
                        string mon = tempMon.Trim();
                        if(mon != "")
                        {
                            if (!monDict.ContainsKey(mon))
                            {
                                monDict.Add(mon, new Pokemon(mon, 0));
                            }
                            monDict[mon].occurences++;
                            currentTeam.Pokemon.Add(monDict[mon]);
                        }
                    }
                }
            }

            sr.Close();

            Dictionary<string, Dictionary<string, int>> encounter = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> encounterTwice = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

            foreach (Team team in teams)
            {
                foreach(Pokemon mon in team.Pokemon)
                {
                    if (!encounter.ContainsKey(mon.name))
                    {
                        encounter.Add(mon.name, new Dictionary<string, int>());
                        encounterTwice.Add(mon.name, new Dictionary<string, Dictionary<string, int>>());
                    }
                    foreach (Pokemon allyMon in team.Pokemon)
                    {
                        if (!encounterTwice[mon.name].ContainsKey(allyMon.name))
                        {
                            encounterTwice[mon.name].Add(allyMon.name, new Dictionary<string, int>());
                        }
                        if (allyMon.name != mon.name)
                        {
                            if (!encounter[mon.name].ContainsKey(allyMon.name))
                            {
                                encounter[mon.name].Add(allyMon.name, 1);
                            }
                            else
                            {
                                encounter[mon.name][allyMon.name]++;
                            }
                            foreach (Pokemon otherAllyMon in team.Pokemon)
                            {
                                if (otherAllyMon.name != allyMon.name && otherAllyMon.name != mon.name)
                                {
                                    if (!encounterTwice[mon.name][allyMon.name].ContainsKey(otherAllyMon.name))
                                    {
                                        encounterTwice[mon.name][allyMon.name].Add(otherAllyMon.name, 1);
                                    }
                                    else
                                    {
                                        encounterTwice[mon.name][allyMon.name][otherAllyMon.name]++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Enter part of Team (mon1, mon2, ...) for prediction (q for exit)");
            string input = Console.ReadLine();
            while(input.Trim().ToLower() != "q")
            {
                ProcessInput(input, encounter, encounterTwice, monDict);
                Console.WriteLine("Enter part of Team (mon1, mon2, ...) for prediction (q for exit)");
                input = Console.ReadLine();
            }
        }

        private static void ProcessInput(string input, Dictionary<string, Dictionary<string, int>> encounter, Dictionary<string, Dictionary<string, Dictionary<string, int>>> encounterTwice, Dictionary<string, Pokemon> monDict)
        {
            Console.WriteLine();
            string[] mons = input.Split(',');
            for (int i = 0; i < mons.Length; i++)
            {
                mons[i] = mons[i].Trim().ToLower();
            }

            mons = mons.Distinct().ToArray();

            Dictionary<string, double> probabilites = new Dictionary<string, double>();

            foreach(KeyValuePair<string, Pokemon> monShare in monDict)
            {
                probabilites.Add(monShare.Key, 1.0);
            }


            for (int i = 0; i < mons.Length; i++)
            {
                string mon = mons[i];
                if (!encounter.ContainsKey(mon))
                {
                    Console.WriteLine("The input was incorrect on " + mon + "!");
                    return;
                }
                Dictionary<string, int> encounterDict = encounter[mon];
                probabilites[mon] = 0.0;
                Pokemon pokemon = monDict[mon];
                foreach(KeyValuePair<string, Pokemon> kv in monDict)
                {
                    if (encounterDict.ContainsKey(kv.Key))
                    {
                        probabilites[kv.Key] *= (1.0 + encounterDict[kv.Key]) / pokemon.occurences;
                    }
                    else
                    {
                        probabilites[kv.Key] *= 1.0 / pokemon.occurences;
                    }
                }
                for(int j = i + 1; j < mons.Length; j++)
                {
                    string otherMon = mons[j];
                    Dictionary<string, int> encounterTwiceDict = encounterTwice[mon][otherMon];
                    probabilites[otherMon] = 0.0;
                    Pokemon otherPokemon = monDict[otherMon];
                    foreach (KeyValuePair<string, Pokemon> kv in monDict)
                    {
                        if (encounterTwiceDict.ContainsKey(kv.Key))
                        {
                            probabilites[kv.Key] *= (1.0 + encounterTwiceDict[kv.Key]) / encounter[mon][otherMon];
                        }
                        else
                        {
                            probabilites[kv.Key] *= 1.0 / encounter[mon][otherMon];
                        }
                    }
                }
            }

            var ordered = probabilites.OrderBy(x => -x.Value);

            int count = 0;
            foreach (KeyValuePair<string, double> kv in ordered)
            {
                Console.WriteLine(kv.Key + " with a " + kv.Value + " percentage.");
                count++;
                if(count == 10)
                {
                    break;
                }
            }
            Console.WriteLine();

            return;
        }
    }
}
