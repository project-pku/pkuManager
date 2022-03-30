﻿global using static pkuManager.Formats.Modules.TagEnums; //TagEnums are global constants

using pkuManager.Alerts;
using pkuManager.Formats.pku;
using pkuManager.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using static pkuManager.Alerts.Alert;
using static pkuManager.Formats.Modules.TagUtil.ExportAlerts;

namespace pkuManager.Formats.Modules;

/// <summary>
/// A utility class with common code and constants for different modules.
/// </summary>
public static class TagUtil
{
    /* ------------------------------------
     * Constants
     * ------------------------------------
    */
    /// <summary>
    /// A map from a <see cref="Language"/> to its translation of "Egg".
    /// </summary>
    public static readonly Dictionary<Language, string> EGG_NICKNAME = new()
    {
        { Language.Japanese, "タマゴ" },
        { Language.English, "Egg" },
        { Language.French, "Œuf" },
        { Language.Italian, "Uovo" },
        { Language.German, "Ei" },
        { Language.Spanish, "Huevo" },
        { Language.Korean, "알" },
        { Language.Chinese_Simplified, "蛋" },
        { Language.Chinese_Traditional, "蛋" },
    };

    /// <summary>
    /// A list of the 6 official Pokémon stats' names in canonical order.
    /// </summary>
    public static readonly string[] STAT_NAMES = new[]
        { "HP", "Attack", "Defense", "Sp. Attack", "Sp. Defense", "Speed" };

    /// <summary>
    /// A list of the 6 contest stats' names in canonical order.
    /// </summary>
    public static readonly string[] CONTEST_STAT_NAMES = new[]
        { "Cool", "Beautiful", "Cute", "Clever", "Tough", "Sheen" };


    /* ------------------------------------
     * Species Methods
     * ------------------------------------
    */
    /// <summary>
    /// Gets the National dex number of the given <paramref name="species"/>.
    /// </summary>
    /// <param name="species">A Pokémon species name, with any capitalization.</param>
    /// <returns>The national dex # of the given <paramref name="species"/>,
    ///          or <see langword="null"/> if it doesn't have one.</returns>
    public static int? GetNationalDex(string species)
        => SPECIES_DEX.ReadDataDex<int?>(species, "Indices", "main-series");

    /// <summary>
    /// Does the same as <see cref="GetNationalDex(string)"/> but with the<br/>
    /// assumption that, <paramref name="species"/> is an official species.
    /// </summary>
    /// <param name="species">An official Pokémon species, will throw an exception otherwise.</param>
    /// <returns>The national dex # of the given <paramref name="species"/></returns>
    public static int GetNationalDexChecked(string species)
    {
        int? dex = GetNationalDex(species);
        return dex is null ? throw new ArgumentException
            ("Must be an official Pokémon species.", nameof(species)) : dex.Value;
    }

    /// <summary>
    /// Gets the English name of the species with the given <paramref name="dex"/> #.
    /// </summary>
    /// <param name="dex">The national dex # of the desired species.</param>
    /// <returns>The English name of the species with the <paramref name="dex"/> #.
    ///          Null if there is no match.</returns>
    public static string GetSpeciesName(int dex)
        => SPECIES_DEX.SearchDataDex<int?>(dex, "$x", "Indices", "main-series");


    /* ------------------------------------
     * Gender Methods
     * ------------------------------------
    */
    /// <summary>
    /// Gets the gender ratio of the given official <paramref name="species"/>.
    /// </summary>
    /// <param name="species">An official species. Will throw an exception otherwise.</param>
    /// <returns>The gender ratio of <paramref name="species"/>.</returns>
    public static GenderRatio GetGenderRatio(DexUtil.SFA sfa)
        => SPECIES_DEX.ReadSpeciesDex<string>(sfa, "Gender Ratio").ToEnum<GenderRatio>().Value;

    /// <summary>
    /// Returns the gender of a Pokémon with the given <paramref name="pid"/> as determined by Gens 3-5. 
    /// </summary>
    /// <param name="gr">The gender ratio of the Pokémon.</param>
    /// <param name="pid">The PID of the Pokémon.</param>
    /// <returns>The gender of a Pokémon with the given gender ratio
    ///          and <paramref name="pid"/> in Gens 3-5.</returns>
    public static Gender GetPIDGender(GenderRatio gr, uint pid) => gr switch
    {
        GenderRatio.All_Female => Gender.Female,
        GenderRatio.All_Male => Gender.Male,
        GenderRatio.All_Genderless => Gender.Genderless,
        GenderRatio x when (int)x > pid % 256 => Gender.Female,
        _ => Gender.Male
    };


    /* ------------------------------------
     * Unown Form Methods
     * ------------------------------------
    */
    /// <summary>
    /// Gets the form ID of an Unown with the given PID in Gen 3.
    /// </summary>
    /// <param name="pid">The Unown's PID.</param>
    /// <returns>The Unown form ID determined by the PID.</returns>
    public static int GetUnownFormIDFromPID(uint pid)
    {
        uint formID = 0;
        formID.SetBits(pid.GetBits(0, 2), 0, 2); //first two bits of byte 0
        formID.SetBits(pid.GetBits(8, 2), 2, 2); //first two bits of byte 1
        formID.SetBits(pid.GetBits(16, 2), 4, 2); //first two bits of byte 2
        formID.SetBits(pid.GetBits(24, 2), 6, 2); //first two bits of byte 3

        return (int)formID % 28;
    }

    /// <summary>
    /// Gets the Unown form name the given ID corresponds to.
    /// </summary>
    /// <param name="id">An Unown form ID.</param>
    /// <returns>The name of the Unown form with <paramref name="id"/>. Null if ID is invalid.</returns>
    public static string GetUnownFormName(int id) => id switch
    {
        < 0 or > 27 => null, //invalid id
        26 => "!",
        27 => "?",
        _ => "" + (char)('A' + id) //A-Z
    };

    /// <summary>
    /// Gets the Unown form ID the given name corresponds to.
    /// </summary>
    /// <param name="name">An Unown form name (i.e. A-Z,!,?).</param>
    /// <returns>The ID of the Unown form with <paramref name="name"/>. Null if name is invalid.</returns>
    public static int? GetUnownFormIDFromName(string name)
    {
        if (name?.Length != 1)
            return null; //must be 1 letter long
        return name[0] switch
        {
            '!' => 26,
            '?' => 27,
            >= 'A' and <= (char)('A' + 27) => name[0] - 'A',
            _ => null
        };
    }


    /* ------------------------------------
     * Misc. Methods
     * ------------------------------------
    */
    /// <summary>
    /// Generates a PID that satisfies the given constraints in all generations.<br/>
    /// If a constraint is null, it will be ignored.
    /// </summary>
    /// <param name="shiny">The desired shinyness.</param>
    /// <param name="tid">The Pokémon's TID.</param>
    /// <param name="gender">The desired gender.</param>
    /// <param name="gr">The species' gender ratio.</param>
    /// <param name="nature">The desired nature.</param>
    /// <param name="unownForm">The desired form, if the species is Unown.</param>
    /// <returns>A random PID satisfying all the given constraints.</returns>
    public static uint GenerateRandomPID(bool shiny, uint tid, Gender? gender = null,
        GenderRatio? gr = null, Nature? nature = null, int? unownForm = null)
    {
        //Notice no option for ability slot.
        //Getting legality is the user's problem. Preserving legality is pku's problem.
        while (true)
        {
            uint pid = DataUtil.GetRandomUInt(); //Generate new PID candidate

            // Unown Form Check
            if (unownForm is not null)
            {
                if (unownForm != GetUnownFormIDFromPID(pid))
                    continue;
            }

            // Gender Check
            if (gender is not null && gr is null)
                throw new ArgumentException($"If {nameof(gender)} is specified, a gender ratio must also be specified.", nameof(gr));
            else if (gender is not null) //gr is not null
            {
                if (gr is not (GenderRatio.All_Male or GenderRatio.All_Female or GenderRatio.All_Genderless))
                {
                    Gender pidGender = GetPIDGender(gr.Value, pid);

                    //Male but pid is Female
                    if ((gender, pidGender) is (Gender.Male, not Gender.Male))
                        continue;

                    //Female but pid is Male
                    if ((gender, pidGender) is (Gender.Female, not Gender.Female))
                        continue;
                }
            }

            // Nature Check
            if (nature is not null)
            {
                if ((int)nature != pid % 25)
                    continue;
            }

            // Shiny Check
            if ((pid / 65536 ^ pid % 65536 ^ tid / 65536 ^ tid % 65536) < 8 != shiny) //In gen 6+ that 8->16.
                continue;                                                             //No harm keeping it 8 for backwards compat.

            return pid; // everything checks out
        }
    }

    public static bool IsPIDShiny(uint pid, uint tid, bool gen6Plus)
        => (tid / 65536 ^ tid % 65536 ^ pid / 65536 ^ pid % 65536) < (gen6Plus ? 16 : 8);

    /// <summary>
    /// Calculates the PP of the given <paramref name="move"/> with the given number of
    /// <paramref name="ppups"/>, under the given <paramref name="format"/>.
    /// </summary>
    /// <param name="move">The name of move.</param>
    /// <param name="ppups">The number of PP Ups the move has.</param>
    /// <param name="format">The format the move is being considered under.</param>
    /// <returns>The PP <paramref name="move"/> would have with <paramref name="ppups"/> PP Ups.</returns>
    public static int CalculatePP(string move, int ppups, string format)
        => (5 + ppups) * MOVE_DEX.GetIndexedValue<int?>(format, move, "Base PP").Value / 5;


    /* ------------------------------------
     * Alert Generator Methods
     * ------------------------------------
    */
    /// <summary>
    /// Alert generating methods for <see cref="ExportTags"/> methods.
    /// </summary>
    public static class ExportAlerts
    {
        // ----------
        // Pokemon Attribute Alert Methods
        // ----------
        public static Alert GetAbilityAlert(AlertType at, string invalidAbility = null, string defaultAbility = "None")
        {
            if (at is AlertType.MISMATCH or AlertType.INVALID && invalidAbility is null)
                throw new ArgumentNullException(nameof(invalidAbility), "Must give the invalid ability for MISMATCH and INVALID alerts.");
            return new("Ability", at switch
            {
                AlertType.UNSPECIFIED => $"No ability was specified, using the default ability: {defaultAbility}.",
                AlertType.MISMATCH => $"This species cannot have the ability {invalidAbility} in this format. Using the default ability: {defaultAbility}.",
                AlertType.INVALID => $"The ability {invalidAbility} is not supported by this format, using the default ability: {defaultAbility}.",
                _ => throw InvalidAlertType(at)
            });
        }

        public static Alert GetRibbonAlert()
            => new("Ribbons", "Some of the pku's ribbons are not valid in this format. Ignoring them.");
    }


    /* ------------------------------------
     * Tag Processing Methods
     * ------------------------------------
    */
    /// <summary>
    /// Generalized methods for processing attributes of pkx files.
    /// </summary>
    public static class ExportTags
    {
        public static (HashSet<Ribbon>, Alert) ProcessRibbons(pkuObject pku, Func<Ribbon, bool> isValidRibbon)
        {
            bool anyInvalid = false;
            HashSet<Ribbon> ribbons = pku.Ribbons.ToEnumSet<Ribbon>();
            if (pku.Ribbons is not null)
                anyInvalid = pku.Ribbons.Distinct(StringComparer.InvariantCultureIgnoreCase).Count() > ribbons.Count;

            int oldCount = ribbons.Count;
            ribbons.RemoveWhere(x => !isValidRibbon(x)); //removes invalid ribbons from set
            anyInvalid = oldCount > ribbons.Count || anyInvalid;

            return (ribbons, anyInvalid ? GetRibbonAlert() : null);
        }

        //TODO Gen 4: implement this for gen4+, slot/ability independent in all gens, EXCEPT for gen 3
        //public static (int abilityID, int slot, Alert) ProcessAbility(PKUObject pku, int maxAbility)
    }
}

public static class TagEnums
{
    /* ------------------------------------
     * Default Enums
     * ------------------------------------
    */
    public const Language DEFAULT_LANGUAGE = Language.English;

    /// <summary>
    /// The default gender for Pokémon and trainers.
    /// </summary>
    public const Gender DEFAULT_GENDER = Gender.Male;

    public const Nature DEFAULT_NATURE = Nature.Hardy;


    /* ------------------------------------
     * Common Enums
     * ------------------------------------
    */
    /// <summary>
    /// An official nature a Pokémon can have.<br/>
    /// Index numbers correspond to those used in the official games.
    /// </summary>
    public enum Nature
    {
        Hardy,
        Lonely,
        Brave,
        Adamant,
        Naughty,
        Bold,
        Docile,
        Relaxed,
        Impish,
        Lax,
        Timid,
        Hasty,
        Serious,
        Jolly,
        Naive,
        Modest,
        Mild,
        Quiet,
        Bashful,
        Rash,
        Calm,
        Gentle,
        Sassy,
        Careful,
        Quirky
    }

    /// <summary>
    /// A gender a Pokémon, or trainer, can have.<br/>
    /// Note that OT genders can only be male or female, not genderless.<br/>
    /// Index numbers correspond to those used in the official games.
    /// </summary>
    public enum Gender
    {
        Male,
        Female,
        Genderless
    }

    /// <summary>
    /// A gender ratio a Pokémon species can have.<br/>
    /// Index numbers correspond to the gender threshold use to determine a Pokémon's gender.
    /// </summary>
    public enum GenderRatio
    {
        All_Male = 0,
        Male_7_Female_1 = 31,
        Male_3_Female_1 = 63,
        Male_1_Female_1 = 127,
        Male_1_Female_3 = 191,
        Male_1_Female_7 = 225,
        All_Female = 254,
        All_Genderless = 255
    }

    /// <summary>
    /// An official language a Pokémon can have.
    /// Index numbers correspond to those used in the official games.
    /// </summary>
    public enum Language
    {
        /// <summary>
        /// Unset language ID.<br/>
        /// Note that Gen 5 Japanese in-game trades use this value. Great...
        /// </summary>
        //None = 0,

        /// <summary>
        /// Japanese (日本語)
        /// </summary>
        Japanese = 1,

        /// <summary>
        /// English (US/UK/AU)
        /// </summary>
        English = 2,

        /// <summary>
        /// French (Français)
        /// </summary>
        French = 3,

        /// <summary>
        /// Italian (Italiano)
        /// </summary>
        Italian = 4,

        /// <summary>
        /// German (Deutsch)
        /// </summary>
        German = 5,

        /// <summary>
        /// Unused language ID reserved for Korean in Gen 3 but never used.
        /// </summary>
        //Korean_Gen_3 = 6,

        /// <summary>
        /// Spanish (Español)
        /// </summary>
        Spanish = 7,

        /// <summary>
        /// Korean (한국어)
        /// </summary>
        Korean = 8,

        /// <summary>
        /// Chinese Simplified (简体中文)
        /// </summary>
        Chinese_Simplified = 9,

        /// <summary>
        /// Chinese Traditional (繁體中文)
        /// </summary>
        Chinese_Traditional = 10
    }

    /// <summary>
    /// A marking a Pokémon can have displayed on their summary screen.
    /// </summary>
    public enum Marking
    {
        // Blue markings (also refer to the black markings present in Gens 3-5)
        Blue_Circle,
        Blue_Triangle,
        Blue_Square,
        Blue_Heart,
        Blue_Star,
        Blue_Diamond,

        // Pink markings
        Pink_Circle,
        Pink_Triangle,
        Pink_Square,
        Pink_Heart,
        Pink_Star,
        Pink_Diamond,

        Favorite //Only in LGPE
    }

    /// <summary>
    /// A Ribbon or Mark a Pokémon can have.<br/>
    /// Index numbers correspond to those used in the official games, except the
    /// Contest and Battle Tower ribbons from Gens 3-4.<br/>
    /// These are given negative index numbers since they don't exist in new formats.
    /// </summary>
    public enum Ribbon
    {
        // Contest Gen 3
        Cool_G3 = -49, //Indexing starts at -49 because memory ribbons replaced old contest/battle ribbons
        Cool_Super_G3,
        Cool_Hyper_G3,
        Cool_Master_G3,
        Beauty_G3,
        Beauty_Super_G3,
        Beauty_Hyper_G3,
        Beauty_Master_G3,
        Cute_G3,
        Cute_Super_G3,
        Cute_Hyper_G3,
        Cute_Master_G3,
        Smart_G3,
        Smart_Super_G3,
        Smart_Hyper_G3,
        Smart_Master_G3,
        Tough_G3,
        Tough_Super_G3,
        Tough_Hyper_G3,
        Tough_Master_G3,

        // Contest Gen 4
        Cool_G4,
        Cool_Great_G4,
        Cool_Ultra_G4,
        Cool_Master_G4,
        Beauty_G4,
        Beauty_Great_G4,
        Beauty_Ultra_G4,
        Beauty_Master_G4,
        Cute_G4,
        Cute_Great_G4,
        Cute_Ultra_G4,
        Cute_Master_G4,
        Smart_G4,
        Smart_Great_G4,
        Smart_Ultra_G4,
        Smart_Master_G4,
        Tough_G4,
        Tough_Great_G4,
        Tough_Ultra_G4,
        Tough_Master_G4,

        // Battle Gen 3
        Winning,
        Victory,

        // Battle Gen 4
        Ability,
        Great_Ability,
        Double_Ability,
        Multi_Ability,
        Pair_Ability,
        World_Ability,

        // -1 reserved for no match

        // Everything else
        Kalos_Champion = 0,
        Champion,
        Sinnoh_Champion,
        Best_Friends,
        Training,
        Skillful_Battler,
        Expert_Battler,
        Effort,
        Alert,
        Shock,
        Downcast,
        Careless,
        Relax,
        Snooze,
        Smile,
        Gorgeous,
        Royal,
        Gorgeous_Royal,
        Artist,
        Footprint,
        Record,
        Legend,
        Country,
        National,
        Earth,
        World,
        Classic,
        Premier,
        Event, //History Ribbon in Gen 4
        Birthday, //Green Ribbon in Gen 4
        Special, //Blue Ribbon in Gen 4
        Souvenir, //Festival Ribbon in Gen 4
        Wishing, //Carnival Ribbon in Gen 4
        Battle_Champion, //Marine Ribbon in Gen 3/4
        Regional_Champion, //Land Ribbon in Gen 3/4
        National_Champion, //Sky Ribbon in Gen 3/4
        World_Champion, //Red Ribbon in Gen 4
        Contest_Memory,
        Battle_Memory,
        Hoenn_Champion,
        Contest_Star,
        Coolness_Master,
        Beauty_Master,
        Cuteness_Master,
        Cleverness_Master,
        Toughness_Master,
        Alola_Champion,
        Battle_Royale_Master,
        Battle_Tree_Great,
        Battle_Tree_Master,
        Galar_Champion,
        Tower_Master,
        Master_Rank,

        // Marks Gen 8
        Lunchtime_Mark,
        SleepyTime_Mark,
        Dusk_Mark,
        Dawn_Mark,
        Cloudy_Mark,
        Rainy_Mark,
        Stormy_Mark,
        Snowy_Mark,
        Blizzard_Mark,
        Dry_Mark,
        Sandstorm_Mark,
        Misty_Mark,
        Destiny_Mark,
        Fishing_Mark,
        Curry_Mark,
        Uncommon_Mark,
        Rare_Mark,
        Rowdy_Mark,
        AbsentMinded_Mark,
        Jittery_Mark,
        Excited_Mark,
        Charismatic_Mark,
        Calmness_Mark,
        Intense_Mark,
        ZonedOut_Mark,
        Joyful_Mark,
        Angry_Mark,
        Smiley_Mark,
        Teary_Mark,
        Upbeat_Mark,
        Peeved_Mark,
        Intellectual_Mark,
        Ferocious_Mark,
        Crafty_Mark,
        Scowling_Mark,
        Kindly_Mark,
        Flustered_Mark,
        PumpedUp_Mark,
        ZeroEnergy_Mark,
        Prideful_Mark,
        Unsure_Mark,
        Humble_Mark,
        Thorny_Mark,
        Vigor_Mark,
        Slump_Mark,
    }

    /// <summary>
    /// An EXP growth type a Pokémon species can have.
    /// Index numbers correspond to those used in the official games.
    /// </summary>
    public enum GrowthRate
    {
        Medium_Fast,
        Erratic,
        Fluctuating,
        Medium_Slow,
        Fast,
        Slow
    }
}