using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterB
{
    public enum DuckTrophyCity
    {
        Skopje,
        Paris,
        London,
        NewYork,
        LosAngeles,
        Tokyo,
        Cairo,
        Kyoto,
        Rio
    }

    public enum DuckTrophyClass
    {
        Normal,
        Elite,
        Boss
    }

    [Serializable]
    public struct DuckTrophyEntry
    {
        public Constants.DuckType duckType;
        public DuckTrophyCity city;
        public DuckTrophyClass trophyClass;
        public int sortOrder;
    }

    [CreateAssetMenu(fileName = "DuckTrophyCatalog", menuName = "ShooterB/Duck Trophy Catalog")]
    public class DuckTrophyCatalog : ScriptableObject
    {
        public const int CityCompletionRewardCoins = 50;

        [Header("Preview Libraries")]
        public DuckFrameLibrary frameLibrary;
        public DuckPartLibrary partLibrary;

        [Header("Entries")]
        public DuckTrophyEntry[] entries;

        public IReadOnlyList<DuckTrophyEntry> Entries => entries ?? new DuckTrophyEntry[0];

        public static readonly DuckTrophyCity[] CampaignCityOrder =
        {
            DuckTrophyCity.Skopje,
            DuckTrophyCity.Paris,
            DuckTrophyCity.London,
            DuckTrophyCity.NewYork,
            DuckTrophyCity.LosAngeles,
            DuckTrophyCity.Tokyo,
            DuckTrophyCity.Cairo,
            DuckTrophyCity.Kyoto,
            DuckTrophyCity.Rio
        };

        public static string GetCityDisplayName(DuckTrophyCity city)
        {
            switch (city)
            {
                case DuckTrophyCity.NewYork: return "New York";
                case DuckTrophyCity.LosAngeles: return "Los Angeles";
                case DuckTrophyCity.Rio: return "Rio de Janeiro";
                default: return city.ToString();
            }
        }

        public static string GetCityKey(DuckTrophyCity city)
        {
            switch (city)
            {
                case DuckTrophyCity.NewYork: return "NewYork";
                case DuckTrophyCity.LosAngeles: return "LosAngeles";
                case DuckTrophyCity.Rio: return "Rio";
                default: return city.ToString();
            }
        }

        public static string GetCityLocalizationKey(DuckTrophyCity city)
        {
            return $"trophies.city.{GetCityKey(city).ToLowerInvariant()}";
        }
    }
}
