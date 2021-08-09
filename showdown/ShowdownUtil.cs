﻿using Newtonsoft.Json.Linq;
using pkuManager.Alerts;
using pkuManager.Common;
using pkuManager.pku;
using pkuManager.pkx;
using pkuManager.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using AlertType = pkuManager.pkx.pkxUtil.TagAlerts.AlertType;

namespace pkuManager.showdown
{
    public class ShowdownUtil
    {
        // ----------
        // Showdown Name Stuff
        // ----------

        /// <summary>
        /// A JObject corresponding to <i>just</i> the ShowdownNames.json data for the Showdown format.
        /// Includes Showdown names for non-"" forms and for all appearances.
        /// Also includes names for all CAP species.
        /// </summary>
        public static readonly JObject SHOWDOWN_DATA = DataUtil.getJson("ShowdownNames");

        /// <summary>
        /// <para>
        /// Gets the Showdown name of a given pku. If form is invalid, but can be casted, the first valid casted form will be chosen.
        /// If castable override isn't on, pku with invalid forms will return null.
        /// </para>
        /// e.g. Called on a pku with "Species": "Pikachu", "Forms": ["Cosplay"], and "Appearance": "Libre Outfit" would return "Pikachu-Libre".
        /// </summary>
        /// <param name="pku">The pku whose Showdown name is to be determined.</param>
        /// <returns>The pku's Showdown name, and a bool of whether its form was casted.</returns>
        public static (string name, bool casted) GetShowdownName(pkuObject pku)
        {
            static string GetGenderedShowdownName(JToken subjson, string genderUnchecked)
            {
                //If this is a gender split form (i.e. showdown treats the genders as different forms)
                if ((bool?)DataUtil.TraverseJTokenCaseInsensitive(subjson, "Showdown Gender Split") == true)
                {
                    Gender? gender = pkxUtil.GetGender(genderUnchecked, false);
                    string genderStr = gender == Gender.Female ? "Female" : "Male"; //Default is male
                    return (string)DataUtil.TraverseJTokenCaseInsensitive(subjson, $"Showdown Species {genderStr}");
                }
                else
                    return (string)DataUtil.TraverseJTokenCaseInsensitive(subjson, "Showdown Species");
            }

            // Species must be defined
            if (DataUtil.TraverseJTokenCaseInsensitive(pkxUtil.NATIONALDEX_DATA, pku.Species) == null || //only official species...
                DataUtil.TraverseJTokenCaseInsensitive(SHOWDOWN_DATA, pku.Species) == null)              //and pokestar+cap species are allowed
            return (null, false); //not a valid species

            // Species is valid past this point

            // Try to get a Showdown name based off the pku's species/form/appearance
            string showdownName;
            List<string> castableForms = DexUtil.GetCastableForms(pku);
            foreach (string searchableForm in castableForms)
            {
                JToken formJson = DataUtil.TraverseJTokenCaseInsensitive(SHOWDOWN_DATA, pku.Species, "Forms", searchableForm);
                foreach (string acc in pku.Appearance ?? Array.Empty<string>())
                {
                    JToken appearancejson = DataUtil.TraverseJTokenCaseInsensitive(formJson, "Appearance", acc);
                    showdownName = GetGenderedShowdownName(appearancejson, pku.Gender);
                    if(showdownName != null)
                        return (showdownName, DataUtil.stringEqualsCaseInsensitive(searchableForm, castableForms[0])); //found valid form+appearance
                }

                //all appearances failed for this form, try null appearance
                showdownName = GetGenderedShowdownName(formJson, pku.Gender);
                if (showdownName != null)
                    return (showdownName, DataUtil.stringEqualsCaseInsensitive(searchableForm, castableForms[0])); //found valid form+appearance
            }

            // No showdown name found in ShowdownNames.json
            //TODO: make showdownNames.json have ALL species...
            if (DexUtil.IsFormDefault(pku))
                return (pku.Species, false); //If it's a default form, just assume Showdown name is species name
            else
                return (null, false); //no valid form/appearance found
        }


        // ----------
        // Update Showdown Battle Data Stuff
        // ----------
        private static readonly string GithubDexData = @"https://raw.githubusercontent.com/smogon/pokemon-showdown/master/data/pokedex.ts";
        private static readonly string GithubTextData = @"https://raw.githubusercontent.com/smogon/pokemon-showdown/master/data/text/";
        private static readonly string MoveFile = "moves.ts", AbilityFile = "abilities.ts", ItemFile = "items.ts";

        private static List<string> PullShowdownGMaxSpecies()
        {
            // Download file to string
            string fileString;
            using WebClient client = new WebClient();
            fileString = client.DownloadString(GithubDexData);

            // Convert typescript constant declaration to JSON:

            // Remove first line of text and replace it with '{'
            fileString = '{' + fileString.Substring(fileString.IndexOf("\n") + 1);

            // Remove last line of text and add a '}' to the end
            fileString = fileString.Remove(fileString.TrimEnd().LastIndexOf("\n")) + '}';

            //Create final trimmed dex object
            JObject fileObject = JObject.Parse(fileString);
            List<string> listOfNames = new List<string>();
            foreach (var c in fileObject)
            {
                if (DataUtil.TraverseJTokenCaseInsensitive(c.Value, "canGigantamax") != null) //has GMax
                {
                    listOfNames.Add((string)DataUtil.TraverseJTokenCaseInsensitive(c.Value, "name"));
                    string[] cosmeticForms = DataUtil.TraverseJTokenCaseInsensitive(c.Value, "cosmeticFormes")?.ToObject<string[]>();
                    if (cosmeticForms?.Length > 0)
                        listOfNames.AddRange(cosmeticForms);
                }
            }
            listOfNames.RemoveAll(x => x == null); //get rid of null entries (there shouldn't be any but just in case)
            return listOfNames;
        }
        private static List<string> PullShowdownDataHelper(string url)
        {
            // Download file to string
            string fileString;
            using WebClient client = new WebClient();
            fileString = client.DownloadString(url);

            // Convert typescript constant declaration to JSON:

            // Remove first line of text and replace it with '{'
            fileString = '{' + fileString.Substring(fileString.IndexOf("\n") + 1);

            // Remove last line of text and add a '}' to the end
            fileString = fileString.Remove(fileString.TrimEnd().LastIndexOf("\n")) + '}';

            //Create final trimmed move/item/ability object
            JObject fileObject = JObject.Parse(fileString);
            List<string> listOfNames = new List<string>();
            foreach (var c in fileObject)
                listOfNames.Add((string)DataUtil.TraverseJTokenCaseInsensitive(c.Value, "name"));
            return listOfNames;
        }

        /// <summary>
        /// Pulls the current item, move, and ability data from the 
        /// <see href="https://github.com/smogon/pokemon-showdown/tree/master/data/text">Showdown Github</see>.
        /// </summary>
        /// <returns></returns>
        public static JObject PullShowdownData()
        {
            List<string> moves = PullShowdownDataHelper(GithubTextData + MoveFile);
            List<string> items = PullShowdownDataHelper(GithubTextData + ItemFile);
            List<string> abilities = PullShowdownDataHelper(GithubTextData + AbilityFile);
            List<string> gmax = PullShowdownGMaxSpecies();
            JObject showdownData = new JObject
            {
                { "moves", JToken.FromObject(moves) },
                { "items", JToken.FromObject(items) },
                { "abilities", JToken.FromObject(abilities) },
                { "gmax", JToken.FromObject(gmax) }
            };
            return showdownData;
        }


        // ----------
        // Check Showdown Battle Data Stuff
        // ----------
        private static readonly JObject SHOWDOWN_BATTLE_DATA = DataUtil.getJson("ShowdownData");

        private static bool IsDatumValid(string type, string datum)
        {
            if (datum == null)
                return true;
            return Array.Exists(DataUtil.TraverseJTokenCaseInsensitive(SHOWDOWN_BATTLE_DATA, type).ToObject<string[]>(), (x) =>
            {
                return x.ToLowerInvariant() == datum.ToLowerInvariant();
            });
        }

        public static bool IsItemValid(string item)
        {
            return IsDatumValid("items", item);
        }

        public static bool IsMoveValid(string move)
        {
            return IsDatumValid("moves", move);
        }

        public static bool IsAbilityValid(string ability)
        {
            return IsDatumValid("abilities", ability);
        }

        public static bool IsGMaxValid(string species)
        {
            return IsDatumValid("gmax", species);
        }


        // ----------
        // Exporter Stuff
        // ----------

        /// <summary>
        /// Showdown-specific alert generation methods. See <see cref="pkxUtil.TagAlerts"/> for more.
        /// </summary>
        public static class TagAlerts
        {
            public static Alert GetNicknameAlert(AlertType at)
            {
                if (at == AlertType.INVALID)
                    return new Alert("Nickname", $"Showdown does not recoginize leading spaces in nicknames.");
                else
                    throw pkxUtil.TagAlerts.InvalidAlertType(at);
            }

            public static Alert GetLevelAlert(AlertType at)
            {
                //override pkx's unspecified level of 1 to 100
                if (at == AlertType.UNSPECIFIED)
                    return new Alert("Level", "No level specified, using the default: 100.");
                else
                    return pkxUtil.TagAlerts.GetLevelAlert(at);
            }

            public static Alert GetFriendshipAlert(AlertType at)
            {
                //override pkx's unspecified friendship of 0 to 255
                if (at == AlertType.UNSPECIFIED)
                    return pkxUtil.TagAlerts.getNumericalAlert("Friendship", at, 255);
                else
                    return pkxUtil.TagAlerts.GetFriendshipAlert(at);
            }

            public static Alert GetNatureAlert(AlertType at, string invalidNature = null)
            {
                Alert a = new Alert("Nature", $"Using the default: None (Showdown uses Serious when no nature is specified.)");
                if (at == AlertType.INVALID)
                {
                    if (invalidNature == null)
                        throw new ArgumentException("If INVALID AlertType given, invalidNature must also be given.");
                    a.message = $"The Nature \"{invalidNature}\" is not valid in this format. " + a.message;
                    return a;
                }
                else if (at == AlertType.UNSPECIFIED)
                {
                    a.message = $"No nature was specified. " + a.message;
                    return a;
                }
                else
                    throw pkxUtil.TagAlerts.InvalidAlertType(at);
            }

            public static Alert GetGMaxAlert(AlertType at)
            {
                if (at == AlertType.INVALID)
                    return new Alert("Gigantamax Factor", "This species does not have a Gigantamax form in this format. Setting Gigantamax factor to false.");
                else
                    throw pkxUtil.TagAlerts.InvalidAlertType(at);
            }
        }

        /// <summary>
        /// Showdown-specific tag processing methods. See <see cref="pkxUtil.ProcessTags"/> for more.
        /// </summary>
        public static class ProcessTags
        {
            public static (string, Alert) ProcessShowdownName(pkuObject pku)
            {
                (string showdownName, bool casted) = GetShowdownName(pku);
                if (showdownName == null)
                    throw new ArgumentException("Expected a pku with a valid Showdown species here.");

                return (showdownName, casted ? pkxUtil.TagAlerts.GetFormAlert(AlertType.CASTED, pku.Forms) : null);
            }

            public static (string, Alert) ProcessNickname(pkuObject pku)
            {
                // Research:
                //  - Practically no character limit
                //  - Can use parenthesis (only checks at the end of first line)
                //  - Empty nickname interpreted as no nickname
                //  - Leading spaces are ignored

                if (pku.Nickname == null || pku.Nickname == "") //if null/empty
                    return (null, null);
                else if (pku.Nickname[0] == ' ') //if first character is a space
                    return (pku.Nickname, TagAlerts.GetNicknameAlert(AlertType.INVALID));
                else //nothing else to check
                    return (pku.Nickname, null);
            }

            public static (string, Alert) ProcessItem(pkuObject pku)
            {
                Alert a = null;
                bool itemValid = IsItemValid(pku.Item);
                if (pku.Item != null && !itemValid) //check for invalid alert
                    a = pkxUtil.TagAlerts.GetItemAlert(AlertType.INVALID, pku.Item);

                return (itemValid ? pku.Item : null, a);
            }

            public static (string, Alert) ProcessAbility(pkuObject pku)
            {
                Alert a = null;
                bool abilityValid = IsAbilityValid(pku.Ability);
                if (pku.Ability != null && !abilityValid) //check for invalid alert
                    a = pkxUtil.TagAlerts.GetAbilityAlert(AlertType.INVALID, pku.Ability, "None (Showdown will pick one).");

                return (abilityValid ? pku.Ability : null, a);
            }

            public static (int, Alert) ProcessLevel(pkuObject pku)
            {
                return pkxUtil.ProcessTags.ProcessNumericTag(pku.Level, pkxUtil.TagAlerts.GetLevelAlert, false, 100, 1, 100);
            }

            public static (int, Alert) ProcessFriendship(pkuObject pku)
            {
                return pkxUtil.ProcessTags.ProcessNumericTag(pku.Friendship, TagAlerts.GetFriendshipAlert, false, 255, 0, 255);
            }

            public static (int[], Alert) ProcessIVs(pkuObject pku)
            {
                int?[] vals = { pku.IVs?.HP, pku.IVs?.Attack, pku.IVs?.Defense, pku.IVs?.Sp_Attack, pku.IVs?.Sp_Defense, pku.IVs?.Speed };
                return pkxUtil.ProcessTags.ProcessMultiNumericTag(pku.IVs != null, vals, pkxUtil.TagAlerts.GetIVsAlert, 31, 0, 31, false);
            }

            public static (Nature?, Alert) ProcessNature(pkuObject pku)
            {
                Nature? natureTest = pkxUtil.GetNature(pku.Nature);
                if (natureTest.HasValue)
                    return pkxUtil.ProcessTags.ProcessNature(pku);
                else if (pku.Nature == null)
                    return (null, TagAlerts.GetNatureAlert(AlertType.UNSPECIFIED));
                else
                    return (null, TagAlerts.GetNatureAlert(AlertType.INVALID, pku.Nature));
            }

            public static (bool, Alert) ProcessGMax(pkuObject pku, string checkedShowdownSpecies)
            {
                if (pku.Gigantamax_Factor == true)
                {
                    if (IsGMaxValid(checkedShowdownSpecies))
                        return (true, null);
                    else
                        return (false, TagAlerts.GetGMaxAlert(AlertType.INVALID));
                }
                else
                    return (false, null);
            }

            public static (string[], Alert) ProcessMoves(pkuObject pku)
            {
                //doesnt allow gmax moves, but showdown doesn't allow them either
                List<string> moves = new List<string>();
                (int[] moveIDs, _, Alert alert) = pkxUtil.ProcessTags.ProcessMoves(pku, pkxUtil.LAST_MOVE_INDEX_GEN8);
                for (int i = 0; i < 4; i++)
                {
                    if (moveIDs[i] != 0)
                        moves.Add(PokeAPIUtil.GetMoveName(moveIDs[i]));
                }
                return (moves.ToArray(), alert);
            }
        }
    }
}
