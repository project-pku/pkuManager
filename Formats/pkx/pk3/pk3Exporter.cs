﻿using pkuManager.Alerts;
using pkuManager.Common;
using pkuManager.pku;
using pkuManager.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static pkuManager.Alerts.Alert;

namespace pkuManager.Formats.pkx.pk3
{
    /// <summary>
    /// Exports a <see cref="pkuObject"/> to a <see cref="pk3Object"/>.
    /// </summary>
    public class pk3Exporter : Exporter
    {
        /// <summary>
        /// Creates an exporter that will attempt to export <paramref name="pku"/>
        /// to a .pk3 file with the given <paramref name="globalFlags"/>.
        /// </summary>
        /// <inheritdoc cref="Exporter(pkuObject, GlobalFlags, FormatObject)"/>
        public pk3Exporter(pkuObject pku, GlobalFlags globalFlags) : base(pku, globalFlags, new pk3Object()) { }

        /// <summary>
        /// <see cref="Exporter.Data"/> casted as a <see cref="pk3Object"/>.
        /// </summary>
        protected pk3Object pk3 { get => Data as pk3Object; }

        public override bool CanExport()
        {
            // Screen National Dex #
            if (pkxUtil.GetNationalDex(pku.Species) > pk3Object.LAST_DEX_NUM) //Only species gen 3 and below are allowed
                return false;

            // Screen Form
            if (!DexUtil.IsFormDefault(pku) && !DexUtil.CanCastPKU(pku, pk3Object.VALID_FORMS)) // If form isn't default, and uncastable
                return false;

            // Screen Shadow Pokemon
            if (pku.IsShadow())
                return false;

            return true; //compatible with .pk3
        }

        // Working variables
        protected int dex; //official national dex #
        protected int[] moveIndices; //indices of the chosen moves in the pku
        protected Gender? gender;
        protected Nature? nature;
        protected int? unownForm;
        protected string checkedGameName;


        /* ------------------------------------
         * Pre-Processing Methods
         * ------------------------------------
        */

        // Battle Stat Override
        [ExporterDirective(-10)]
        protected virtual void ProcessBattleStatOverride()
        {
            Notes.Add(pkxUtil.PreProcess.ProcessBattleStatOverride(pku, GlobalFlags));
        }


        /* ------------------------------------
         * Tag Processing Methods
         * ------------------------------------
        */

        // Dex # [Implicit]
        [ExporterDirective(0)]
        protected virtual void ProcessDex()
        {
            dex = pkxUtil.GetNationalDexChecked(pku.Species);
        }

        // Species
        [ExporterDirective(10)]
        protected virtual void ProcessSpecies()
        {
            pk3.Species = (ushort)PokeAPIUtil.GetSpeciesIndex(dex, 3);
        }

        // Nature [Implicit]
        [ExporterDirective(20)]
        protected virtual void ProcessNature()
        {
            Alert alert;
            if (pku.Nature is null)
                alert = GetNatureAlert(AlertType.UNSPECIFIED);
            else if (!pkxUtil.GetNature(pku.Nature).HasValue)
                alert = GetNatureAlert(AlertType.INVALID, pku.Nature);
            else
                (nature, alert) = pkxUtil.ProcessTags.ProcessNature(pku);

            Warnings.Add(alert);
        }

        // Gender [Implicit]
        [ExporterDirective(30)]
        protected virtual void ProcessGender()
        {
            GenderRatio gr = PokeAPIUtil.GetGenderRatio(dex);
            bool onlyOneGender = gr is GenderRatio.ALL_GENDERLESS or GenderRatio.ALL_FEMALE or GenderRatio.ALL_MALE;

            Alert alert;
            if (pku.Gender is null && !onlyOneGender) //unspecified and has more than one possible gender
                alert = GetGenderAlert(AlertType.UNSPECIFIED);
            else if (!pkxUtil.GetGender(pku.Gender, false).HasValue && !onlyOneGender)
                alert = GetGenderAlert(AlertType.INVALID, null, pku.Gender);
            else
                (gender, alert) = pkxUtil.ProcessTags.ProcessGender(pku);

            Warnings.Add(alert);
        }

        // Form [Implicit]
        [ExporterDirective(40)]
        protected virtual void ProcessForm()
        {
            Alert alert = null; //to return
            if (pku.Forms is not null)
            {
                string properFormName = DexUtil.GetSearchableFormName(pku).ToLowerInvariant();
                if (dex is 201 && pku.Forms.Length is 1 && Regex.IsMatch(properFormName, "[a-z!?]")) //unown
                {
                    if (properFormName[0] is '?')
                        unownForm = 26;
                    else if (properFormName[0] is '!')
                        unownForm = 27;
                    else //all other letters
                        unownForm = properFormName[0] - 97;
                }
                else if (dex is 386 && properFormName is "normal" or "attack" or "defense" or "speed") //deoxys
                    alert = GetFormAlert(AlertType.NONE, null, true);
                else if (dex is 351 && properFormName is "sunny" or "rainy" or "snowy") //castform
                    alert = GetFormAlert(AlertType.IN_BATTLE, pku.Forms);
            }

            Warnings.Add(alert);
        }

        // ID
        [ExporterDirective(50)]
        protected virtual void ProcessID()
        {
            Alert alert;
            (pk3.ID, alert) = pkxUtil.ProcessTags.ProcessID(pku);
            Warnings.Add(alert);
        }

        // PID [Requires: Gender, Form, Nature, ID] [ErrorResolver]
        [ExporterDirective(60)]
        protected virtual void ProcessPID()
        {
            var (pids, alert) = pkxUtil.ProcessTags.ProcessPID(pku, pk3.ID, false, gender, nature, unownForm);
            PIDResolver = new ErrorResolver<uint>(alert, pids, x => pk3.PID = x);
            if (alert is RadioButtonAlert)
                Errors.Add(alert);
            else
                Warnings.Add(alert);
        }

        // Egg
        [ExporterDirective(70)]
        protected virtual void ProcessEgg()
        {
            pk3.Egg = pku.IsEgg();
        }

        // Language [Requires: Egg]
        [ExporterDirective(80)]
        protected virtual void ProcessLanguage()
        {
            var (language, alert) = pkxUtil.ProcessTags.ProcessLanguage(pku, pk3Object.LANGUAGE_ENCODING.Values.ToArray());
            pk3.Language = pk3.Egg ? pk3Object.EGG_LANGUAGE_ID : pk3Object.EncodeLanguage(language);
            Warnings.Add(alert);
        }

        // Nickname [Requires: Language]
        [ExporterDirective(90)]
        protected virtual void ProcessNickname()
        {
            Alert alert;
            Language checkedLang = pk3Object.DecodeLanguage(pk3.Language).Value;
            (pk3.Nickname, alert, _, _) = pkxUtil.ProcessTags.ProcessNickname(pku, 3, false, checkedLang, pk3Object.MAX_NICKNAME_CHARS, 1, c => pk3Object.EncodeCharacter(c, checkedLang));
            Warnings.Add(alert);
        }

        // OT [Requires: Language]
        [ExporterDirective(100)]
        protected virtual void ProcessOT()
        {
            Alert alert;
            Language checkedLang = pk3Object.DecodeLanguage(pk3.Language).Value;
            (pk3.OT, alert) = pkxUtil.ProcessTags.ProcessOT(pku, false, pk3Object.MAX_OT_CHARS, 1, c => pk3Object.EncodeCharacter(c, checkedLang));
            Warnings.Add(alert);
        }

        // Trash Bytes [Requires: Nickname, OT]
        [ExporterDirective(110)]
        protected virtual void ProcessTrash()
        {
            Alert alert;

            //Get Nickname trash
            byte[] nicknameTrash = null;
            if (pku.Trash_Bytes?.Gen is 3 && pku.Trash_Bytes?.Nickname?.Length > 0) //If trash bytes exist and are gen 3
                nicknameTrash = pku.Trash_Bytes.Nickname;

            //Get OT trash
            byte[] otTrash = null;
            if (pku.Trash_Bytes?.Gen is 3 && pku.Trash_Bytes?.OT?.Length > 0) //If trash bytes exist and are gen 3
                otTrash = pku.Trash_Bytes.OT;

            (pk3.Nickname, pk3.OT, alert) = pkxUtil.ProcessTags.ProcessTrash(pk3.Nickname, nicknameTrash, pk3.OT, otTrash, new byte[] { 0xFF });
            Warnings.Add(alert);
        }

        // Markings
        [ExporterDirective(120)]
        protected virtual void ProcessMarkings()
        {
            List<MarkingIndex> markings = pkxUtil.GetMarkings(pku.Markings);
            pk3.MarkingCircle = markings.Contains(MarkingIndex.BlueCircle);
            pk3.MarkingSquare = markings.Contains(MarkingIndex.BlueSquare);
            pk3.MarkingTriangle = markings.Contains(MarkingIndex.BlueTriangle);
            pk3.MarkingHeart = markings.Contains(MarkingIndex.BlueHeart);
        }

        // Item
        [ExporterDirective(100)]
        protected virtual void ProcessItem()
        {
            var temp = pkxUtil.ProcessTags.ProcessItem(pku, 3);
            pk3.Item = (ushort)temp.Item1;
            Warnings.Add(temp.Item2);
        }

        // Experience [ErrorResolver]
        [ExporterDirective(110)]
        protected virtual void ProcessExperience()
        {
            var (options, alert) = pkxUtil.ProcessTags.ProcessEXP(pku);
            ExperienceResolver = new ErrorResolver<uint>(alert, options, x => pk3.Experience = x);
            if (alert is RadioButtonAlert)
                Errors.Add(alert);
            else
                Warnings.Add(alert);
        }

        // Moves
        [ExporterDirective(120)]
        protected virtual void ProcessMoves()
        {
            int[] moves;
            Alert alert;
            (moves, moveIndices, alert) = pkxUtil.ProcessTags.ProcessMoves(pku, pk3Object.LAST_MOVE_ID);
            pk3.Move_1 = (ushort)moves[0];
            pk3.Move_2 = (ushort)moves[1];
            pk3.Move_3 = (ushort)moves[2];
            pk3.Move_4 = (ushort)moves[3];
            Warnings.Add(alert);
        }

        // PP-Ups [Requires: Moves]
        [ExporterDirective(130)]
        protected virtual void ProcessPPUps()
        {
            var (ppups, alert) = pkxUtil.ProcessTags.ProcessPPUps(pku, moveIndices);
            pk3.PP_Up_1 = (byte)ppups[0];
            pk3.PP_Up_2 = (byte)ppups[1];
            pk3.PP_Up_3 = (byte)ppups[2];
            pk3.PP_Up_4 = (byte)ppups[3];
            Warnings.Add(alert);
        }

        // PP [Requires: Moves, PP-Ups]
        [ExporterDirective(140)]
        protected virtual void ProcessPP()
        {
            pk3.PP_1 = pk3Object.CalculatePP(pk3.Move_1, pk3.PP_Up_1);
            pk3.PP_2 = pk3Object.CalculatePP(pk3.Move_2, pk3.PP_Up_2);
            pk3.PP_3 = pk3Object.CalculatePP(pk3.Move_3, pk3.PP_Up_3);
            pk3.PP_4 = pk3Object.CalculatePP(pk3.Move_4, pk3.PP_Up_4);
        }

        // Friendship
        [ExporterDirective(150)]
        protected virtual void ProcessFriendship()
        {
            Alert alert;
            (pk3.Friendship, alert) = ((byte, Alert))pkxUtil.ProcessTags.ProcessFriendship(pku);
            Warnings.Add(alert);
        }

        // EVs
        [ExporterDirective(160)]
        protected virtual void ProcessEVs()
        {
            var (evs, alert) = pkxUtil.ProcessTags.ProcessEVs(pku);
            pk3.EV_HP = (byte)evs[0];
            pk3.EV_Attack = (byte)evs[1];
            pk3.EV_Defense = (byte)evs[2];
            pk3.EV_Sp_Attack = (byte)evs[3];
            pk3.EV_Sp_Defense = (byte)evs[4];
            pk3.EV_Speed = (byte)evs[5];
            Warnings.Add(alert);
        }

        // Contest Stats
        [ExporterDirective(170)]
        protected virtual void ProcessContestStats()
        {
            var (contest, alert) = pkxUtil.ProcessTags.ProcessContest(pku);
            pk3.Cool = (byte)contest[0];
            pk3.Beauty = (byte)contest[1];
            pk3.Cute = (byte)contest[2];
            pk3.Smart = (byte)contest[3];
            pk3.Tough = (byte)contest[4];
            pk3.Sheen = (byte)contest[5];
            Warnings.Add(alert);
        }

        // Pokérus
        [ExporterDirective(180)]
        protected virtual void ProcessPokerus()
        {
            Alert alert;
            (pk3.PKRS_Strain, pk3.PKRS_Days, alert) = ((byte, byte, Alert))pkxUtil.ProcessTags.ProcessPokerus(pku);
            Warnings.Add(alert);
        }

        // Origin Game
        [ExporterDirective(190)]
        protected virtual void ProcessOriginGame()
        {
            Alert alert;
            (pk3.Origin_Game, checkedGameName, alert) = ((byte, string, Alert))pkxUtil.ProcessTags.ProcessOriginGame(pku, 3);
            Warnings.Add(alert);
        }

        // Met Location [Requires: Origin Game]
        [ExporterDirective(200)]
        protected virtual void ProcessMetLocation()
        {
            Alert alert;
            (pk3.Met_Location, alert) = ((byte, Alert))pkxUtil.ProcessTags.ProcessMetLocation(pku, checkedGameName, (g, l) => pk3Object.EncodeMetLocation(g, l), pk3Object.GetDefaultLocationName(checkedGameName));
            Warnings.Add(alert);
        }

        // Met Level
        [ExporterDirective(210)]
        protected virtual void ProcessMetLevel()
        {
            Alert alert;
            (pk3.Met_Level, alert) = ((byte, Alert))pkxUtil.ProcessTags.ProcessMetLevel(pku);
            Warnings.Add(alert);
        }

        // Ball
        [ExporterDirective(220)]
        protected virtual void ProcessBall()
        {
            Alert alert;
            (pk3.Ball, alert) = ((byte, Alert))pkxUtil.ProcessTags.ProcessBall(pku, Ball.Premier);
            Warnings.Add(alert);
        }

        // OT Gender
        [ExporterDirective(230)]
        protected virtual void ProcessOTGender()
        {
            var (gender, alert) = pkxUtil.ProcessTags.ProcessOTGender(pku);
            pk3.OT_Gender = gender is Gender.Female; //male otherwise
            Warnings.Add(alert);
        }

        // IVs
        [ExporterDirective(240)]
        protected virtual void ProcessIVs()
        {
            var (ivs, alert) = pkxUtil.ProcessTags.ProcessIVs(pku);
            pk3.IV_HP = (byte)ivs[0];
            pk3.IV_Attack = (byte)ivs[1];
            pk3.IV_Defense = (byte)ivs[2];
            pk3.IV_Sp_Attack = (byte)ivs[3];
            pk3.IV_Sp_Defense = (byte)ivs[4];
            pk3.IV_Speed = (byte)ivs[5];
            Warnings.Add(alert);
        }

        // Ability Slot
        [ExporterDirective(250)]
        protected virtual void ProcessAbilitySlot()
        {
            Alert alert = null;
            int defaultAbilityID = (int)pk3Object.PK3_ABILITY_DATA["" + dex]["1"];
            string defaultAbility = PokeAPIUtil.GetAbility(defaultAbilityID);
            if (pku.Ability is not null) //ability specified
            {
                int? abilityID = PokeAPIUtil.GetAbilityIndex(pku.Ability);
                if (abilityID is null or > 76) //unofficial ability OR gen4+ ability
                {
                    pk3.Ability_Slot = false;
                    alert = pkxUtil.TagAlerts.GetAbilityAlert(AlertType.INVALID, pku.Ability, defaultAbility);
                }
                else //gen 3- ability
                {
                    (bool slot1valid, bool slot2valid) = pk3Object.IsAbilityValid(dex, abilityID.Value);
                    if (slot1valid) //ability corresponds to slot 1
                        pk3.Ability_Slot = false;
                    else if (slot2valid) //ability corresponds to slot 2
                        pk3.Ability_Slot = true;
                    else //ability is impossible on this species, fallback on slot 1
                    {
                        pk3.Ability_Slot = false;
                        alert = pkxUtil.TagAlerts.GetAbilityAlert(AlertType.MISMATCH, pku.Ability, defaultAbility);
                    }
                }
            }
            else //ability unspecified
            {
                pk3.Ability_Slot = false;
                alert = pkxUtil.TagAlerts.GetAbilityAlert(AlertType.UNSPECIFIED, pku.Ability, defaultAbility);
            }
            Warnings.Add(alert);
        }

        // Ribbons
        [ExporterDirective(260)]
        protected virtual void ProcessRibbons()
        {
            (HashSet<Ribbon> ribbons, Alert a) = pkxUtil.ProcessTags.ProcessRibbons(pku, pk3Object.IsValidRibbon);

            //In other words, if the pku has a contest ribbon at level x, but not at level x-1 (when x-1 exists).
            if (ribbons.Contains(Ribbon.Cool_Super_G3) && !ribbons.Contains(Ribbon.Cool_G3) ||
                ribbons.Contains(Ribbon.Cool_Hyper_G3) && !ribbons.Contains(Ribbon.Cool_Super_G3) ||
                ribbons.Contains(Ribbon.Cool_Master_G3) && !ribbons.Contains(Ribbon.Cool_Hyper_G3) ||
                ribbons.Contains(Ribbon.Beauty_Super_G3) && !ribbons.Contains(Ribbon.Beauty_G3) ||
                ribbons.Contains(Ribbon.Beauty_Hyper_G3) && !ribbons.Contains(Ribbon.Beauty_Super_G3) ||
                ribbons.Contains(Ribbon.Beauty_Master_G3) && !ribbons.Contains(Ribbon.Beauty_Hyper_G3) ||
                ribbons.Contains(Ribbon.Cute_Super_G3) && !ribbons.Contains(Ribbon.Cute_G3) ||
                ribbons.Contains(Ribbon.Cute_Hyper_G3) && !ribbons.Contains(Ribbon.Cute_Super_G3) ||
                ribbons.Contains(Ribbon.Cute_Master_G3) && !ribbons.Contains(Ribbon.Cute_Hyper_G3) ||
                ribbons.Contains(Ribbon.Smart_Super_G3) && !ribbons.Contains(Ribbon.Smart_G3) ||
                ribbons.Contains(Ribbon.Smart_Hyper_G3) && !ribbons.Contains(Ribbon.Smart_Super_G3) ||
                ribbons.Contains(Ribbon.Smart_Master_G3) && !ribbons.Contains(Ribbon.Smart_Hyper_G3) ||
                ribbons.Contains(Ribbon.Tough_Super_G3) && !ribbons.Contains(Ribbon.Tough_G3) ||
                ribbons.Contains(Ribbon.Tough_Hyper_G3) && !ribbons.Contains(Ribbon.Tough_Super_G3) ||
                ribbons.Contains(Ribbon.Tough_Master_G3) && !ribbons.Contains(Ribbon.Tough_Hyper_G3))
            {
                a = AddContestRibbonAlert(a);
            }

            //Add contest ribbons
            pk3.Cool_Ribbon_Rank = pk3Object.GetRibbonRank(Ribbon.Cool_G3, ribbons);
            pk3.Beauty_Ribbon_Rank = pk3Object.GetRibbonRank(Ribbon.Beauty_G3, ribbons);
            pk3.Cute_Ribbon_Rank = pk3Object.GetRibbonRank(Ribbon.Cute_G3, ribbons);
            pk3.Smart_Ribbon_Rank = pk3Object.GetRibbonRank(Ribbon.Smart_G3, ribbons);
            pk3.Tough_Ribbon_Rank = pk3Object.GetRibbonRank(Ribbon.Tough_G3, ribbons);

            //Add other ribbons
            pk3.Champion_Ribbon = ribbons.Contains(Ribbon.Champion);
            pk3.Winning_Ribbon = ribbons.Contains(Ribbon.Winning);
            pk3.Victory_Ribbon = ribbons.Contains(Ribbon.Victory);
            pk3.Artist_Ribbon = ribbons.Contains(Ribbon.Artist);
            pk3.Effort_Ribbon = ribbons.Contains(Ribbon.Effort);
            pk3.Battle_Champion_Ribbon = ribbons.Contains(Ribbon.Battle_Champion);
            pk3.Regional_Champion_Ribbon = ribbons.Contains(Ribbon.Regional_Champion);
            pk3.National_Champion_Ribbon = ribbons.Contains(Ribbon.National_Champion);
            pk3.Country_Ribbon = ribbons.Contains(Ribbon.Country);
            pk3.National_Ribbon = ribbons.Contains(Ribbon.National);
            pk3.Earth_Ribbon = ribbons.Contains(Ribbon.Earth);
            pk3.World_Ribbon = ribbons.Contains(Ribbon.World);

            Warnings.Add(a);
        }

        // Fateful Encounter [ErrorResolver]
        [ExporterDirective(270)]
        protected virtual void ProcessFatefulEncounter()
        {
            Alert alert = null;
            bool[] options;
            if (dex is 151 or 386 && pku.Catch_Info?.Fateful_Encounter is not true) //Mew or Deoxys w/ no fateful encounter
            {
                options = new[] { false, true };
                alert = GetFatefulEncounterAlert(dex is 151);
            }
            else
                options = new[] { pku.Catch_Info?.Fateful_Encounter is true };

            FatefulEncounterResolver = new ErrorResolver<bool>(alert, options, x => pk3.Fateful_Encounter = x);
            if (alert is RadioButtonAlert)
                Errors.Add(alert);
            else
                Warnings.Add(alert);
        }


        /* ------------------------------------
         * Error Resolvers
         * ------------------------------------
        */

        // PID ErrorResolver
        [ExporterDirective(280, true)]
        protected virtual ErrorResolver<uint> PIDResolver { get; set; }

        // Experience ErrorResolver
        [ExporterDirective(290, true)]
        protected virtual ErrorResolver<uint> ExperienceResolver { get; set; }

        // Fateful Encounter ErrorResolver
        [ExporterDirective(300, true)]
        protected virtual ErrorResolver<bool> FatefulEncounterResolver { get; set; }


        /* ------------------------------------
         * pk3 Exporting Alerts
         * ------------------------------------
        */

        // Adds gen 3 contest ribbon alert to an existing pkxUtil ribbon alert (or null), if needed.
        public static Alert AddContestRibbonAlert(Alert ribbonAlert)
        {
            string msg = "This pku has a Gen 3 contest ribbon of some category with rank super or higher, " +
                "but doesn't have the ribbons below that rank. This is impossible in this format, adding those ribbons.";
            if (ribbonAlert is not null)
                ribbonAlert.Message += $"\r\n\r\n{ msg}";
            else
                ribbonAlert = new Alert("Ribbons", msg);
            return ribbonAlert;
        }

        public static Alert GetNatureAlert(AlertType at, string invalidNature = null) => at switch
        {
            AlertType.UNSPECIFIED => new Alert("Nature", "No nature specified, using the nature decided by the PID."),
            AlertType.INVALID => new Alert("Nature", $"The nature \"{invalidNature}\" is not valid in this format. Using the nature decided by the PID."),
            _ => pkxUtil.TagAlerts.GetNatureAlert(at, invalidNature)
        };

        // Changes the UNSPECIFIED & INVALID AlertTypes from the pkxUtil method to account for PID-Nature dependence.
        public static Alert GetGenderAlert(AlertType at, Gender? correctGender = null, string invalidGender = null) => at switch
        {
            AlertType.UNSPECIFIED => new Alert("Gender", "No gender specified, using the gender decided by the PID."),
            AlertType.INVALID => new Alert("Gender", $"The gender \"{invalidGender}\" is not valid in this format. Using the gender decided by the PID."),
            _ => pkxUtil.TagAlerts.GetGenderAlert(at, correctGender, invalidGender)
        };

        public static RadioButtonAlert GetFatefulEncounterAlert(bool isMew)
        {
            string pkmn = isMew ? "Mew" : "Deoxys";
            string msg = $"This {pkmn} was not met in a fateful encounter. " +
                $"Note that, in the Gen 3 games, {pkmn} will only obey the player if it was met in a fateful encounter.";

            (string, string)[] choices =
            {
                ("Keep Fateful Encounter",$"Fateful Encounter: false\n{pkmn} won't obey."),
                ("Set Fateful Encounter",$"Fateful Encounter: true\n{pkmn} will obey.")
            };

            return new RadioButtonAlert("Fateful Encounter", msg, choices);
        }

        public static Alert GetFormAlert(AlertType at, string[] invalidForm = null, bool isDeoxys = false)
        {
            return isDeoxys ? new Alert("Form", "Note that in generation 3, Deoxys' form depends on what game it is currently in.") 
                : pkxUtil.TagAlerts.GetFormAlert(at, invalidForm);
        }
    }
}